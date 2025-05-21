using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class FlashlightController : NetworkBehaviour
{
    [Header("Light Components")]
    [SerializeField]
    private Light spotLight; // The main flashlight beam

    [Header("Battery Settings")]
    [SerializeField]
    private float maxBatteryLevel = 100f;

    [SerializeField]
    private float drainRate = 5f; // Units per second

    [SerializeField]
    private float flashDrainAmount = 15f; // Cost per flash

    [SerializeField]
    private float rechargeRate = 10f; // Units per second when cranking

    [Header("Flash Settings")]
    [SerializeField]
    private float flashIntensityMultiplier = 3f; // How much brighter flash is

    [SerializeField]
    private float flashDuration = 0.2f; // How long flash lasts

    [SerializeField]
    private float flashCooldown = 1f; // Time before can flash again

    private float _currentBatteryLevel;
    private bool _isOn = false;
    private bool _isRecharging = false;
    private bool _canFlash = true;
    private float _normalIntensity;
    private float _flashCooldownTimer = 0f;
    private float _flickerTimer = 0f;
    private float _turnOnCooldown = 0.2f;
    private float _turnOffCooldown = 0.2f;
    private bool _isAnimating = false;
    private Coroutine _animationCoroutine = null;

    [Header("Networking")]
    private readonly SyncVar<bool> _syncedIsOn = new SyncVar<bool>(false);
    private readonly SyncVar<float> _syncedBatteryLevel = new SyncVar<float>(100f);

    [Header("Audio")]
    [SerializeField]
    private AudioClip toggleOnSound;

    [SerializeField]
    private AudioClip toggleOffSound;

    [SerializeField]
    private AudioClip flashSound;

    [SerializeField]
    private AudioClip batteryLowSound;

    [SerializeField]
    private AudioSource crankingAudioSource;

    [Header("Visual Effects")]
    [SerializeField]
    private float lowBatteryThreshold = 20f;

    [SerializeField]
    private float flickerThreshold = 10f;
    private Transform _playerCameraTransform;
    private float _rotationSyncTimer = 0.05f;
    private Quaternion _lastSyncedRotation;
    private Quaternion _targetRotation;
    private float _rotationLerpSpeed = 15f;

    private void Awake()
    {
        _syncedIsOn.OnChange += OnFlashlightStateChanged;
        _syncedBatteryLevel.OnChange += OnBatteryLevelChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner)
            return;

        // Find the player camera - it's already created by PlayerController
        _playerCameraTransform = GetComponentInChildren<Camera>()?.transform;
        if (_playerCameraTransform == null)
        {
            Debug.LogError("FlashlightController couldn't find camera");
            return;
        }

        // Create flashlight hierarchy as child of camera
        GameObject flashlightHolder = new GameObject("FlashlightHolder");
        flashlightHolder.transform.SetParent(_playerCameraTransform, false);

        // Create spotlight if not assigned
        if (spotLight == null)
        {
            GameObject spotlightObj = new GameObject("Spotlight");
            spotLight = spotlightObj.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.range = 10f;
            spotLight.spotAngle = 45f;
            spotLight.intensity = 1.5f;
        }

        spotLight.transform.SetParent(flashlightHolder.transform, false);
        _normalIntensity = spotLight.intensity;

        // Make sure it starts disabled
        spotLight.enabled = false;
    }

    void Start()
    {
        if (spotLight)
        {
            _normalIntensity = spotLight.intensity;
        }

        _currentBatteryLevel = maxBatteryLevel;

        SetLightState(false);

        if (crankingAudioSource != null)
        {
            crankingAudioSource.loop = true;
            crankingAudioSource.playOnAwake = false;
            crankingAudioSource.Stop();
        }
    }

    private void LateUpdate()
    {
        // Only run on non-owner clients who need to interpolate
        if (IsOwner || spotLight == null || _targetRotation == Quaternion.identity)
            return;

        spotLight.transform.rotation = Quaternion.Slerp(
            spotLight.transform.rotation,
            _targetRotation,
            _rotationLerpSpeed * Time.deltaTime
        );
    }

    private void UpdateFlashlightOrientation()
    {
        if (!IsOwner || spotLight == null)
            return;

        Transform lightParent = spotLight.transform.parent;
        if (lightParent != null)
        {
            lightParent.rotation = _playerCameraTransform.rotation;
        }
        else
        {
            // If no parent, just rotate the light directly
            spotLight.transform.rotation = _playerCameraTransform.rotation;
        }

        // Only sync occasionally to reduce network traffic
        _rotationSyncTimer -= Time.deltaTime;
        if (
            _rotationSyncTimer <= 0f
            && Quaternion.Angle(_lastSyncedRotation, spotLight.transform.rotation) > 5f
        )
        {
            _lastSyncedRotation = spotLight.transform.rotation;
            SyncLightRotationServerRpc(spotLight.transform.rotation);
            _rotationSyncTimer = 0.1f; // Update 10 times per second
        }
    }

    [ServerRpc]
    private void SyncLightRotationServerRpc(Quaternion rotation)
    {
        SyncLightRotationObserversRpc(rotation);
    }

    [ObserversRpc]
    private void SyncLightRotationObserversRpc(Quaternion rotation)
    {
        if (IsOwner)
            return;

        _targetRotation = rotation;
    }

    void Update()
    {
        if (!IsOwner)
            return;

        HandleInput();
        UpdateBattery();
        UpdateFlashCooldown();
        UpdateFlashlightOrientation();
        DetectFlashlightHits();
    }

    private void HandleInput()
    {
        // Toggle flashlight on/off
        if (InputBindingManager.Instance.IsActionTriggered(InputActions.Flashlight))
        {
            ToggleFlashlight();
        }

        // Flash
        if (
            InputBindingManager.Instance.IsActionTriggered(InputActions.Flash)
            && _canFlash
            && _isOn
        )
        {
            UseFlash();
        }

        _isRecharging = InputBindingManager.Instance.IsActionPressed(
            InputActions.RechargeFlashlight
        );
    }

    private void UpdateBattery()
    {
        if (!IsOwner)
            return;

        float oldBatteryLevel = _currentBatteryLevel;

        // Drain battery when on
        if (_isOn && !_isRecharging)
        {
            _currentBatteryLevel -= drainRate * Time.deltaTime;
        }

        // Recharge when cranking
        if (_isRecharging)
        {
            _currentBatteryLevel += rechargeRate * Time.deltaTime;

            // Play recharge sound
            if (crankingAudioSource != null && !crankingAudioSource.isPlaying)
            {
                AudioManager.Instance.PlayLoopOnExistingSource(crankingAudioSource, 0.7f);
            }

            // If flashlight is on, turn it off
            if (_isOn)
            {
                SetLightState(false);
            }
        }
        else if (crankingAudioSource != null && crankingAudioSource.isPlaying)
        {
            // Stop recharge sound when not recharging
            AudioManager.Instance.StopLoopOnExistingSource(crankingAudioSource);
        }

        // Clamp battery level
        _currentBatteryLevel = Mathf.Clamp(_currentBatteryLevel, 0f, maxBatteryLevel);

        // Check for low battery warning
        if (
            _currentBatteryLevel <= lowBatteryThreshold
            && oldBatteryLevel > lowBatteryThreshold
            && batteryLowSound != null
        )
        {
            AudioManager.Instance.PlaySoundAtPosition(batteryLowSound, transform.position);
        }

        // Turn off if battery depleted
        if (_currentBatteryLevel <= 0 && _isOn)
        {
            SetLightState(false);
        }

        // Update synced battery level occasionally to avoid too much network traffic
        if (Mathf.Abs(_syncedBatteryLevel.Value - _currentBatteryLevel) >= 1f)
        {
            _syncedBatteryLevel.Value = _currentBatteryLevel;
        }

        if (_isOn && _currentBatteryLevel <= flickerThreshold)
        {
            HandleLowBatteryFlicker();
        }
    }

    private void HandleLowBatteryFlicker()
    {
        // Only flicker occasionally
        _flickerTimer -= Time.deltaTime;
        if (_flickerTimer <= 0)
        {
            // Random chance to flicker based on battery level
            // Lower battery = more frequent flickers
            float flickerChance = 1f - (_currentBatteryLevel / flickerThreshold);

            if (Random.value < flickerChance * 0.3f)
            {
                StartCoroutine(FlickerEffect());
            }

            // Reset timer with some randomness
            _flickerTimer = Random.Range(0.1f, 0.5f);
        }
    }

    private IEnumerator FlickerEffect()
    {
        if (spotLight == null)
            yield break;

        float originalIntensity = spotLight.intensity;

        // Random flicker pattern
        int flickerCount = Random.Range(1, 4);

        for (int i = 0; i < flickerCount; i++)
        {
            // Dim or turn off
            spotLight.intensity = originalIntensity * Random.Range(0.1f, 0.7f);
            yield return new WaitForSeconds(Random.Range(0.01f, 0.05f));

            // Restore
            spotLight.intensity = originalIntensity;
            yield return new WaitForSeconds(Random.Range(0.01f, 0.05f));
        }
    }

    private void OnBatteryLevelChanged(float oldValue, float newValue, bool asServer)
    {
        if (!IsOwner)
        {
            _currentBatteryLevel = newValue;
        }
    }

    private void UpdateFlashCooldown()
    {
        if (!_canFlash)
        {
            _flashCooldownTimer -= Time.deltaTime;
            if (_flashCooldownTimer <= 0)
            {
                _canFlash = true;
            }
        }
    }

    public void ToggleFlashlight()
    {
        Debug.Log($"Flashlight toggled {_isOn} {_isAnimating}");
        if ((!_isOn && _currentBatteryLevel <= 0) || _isAnimating)
            return;

        if (_animationCoroutine != null)
            StopCoroutine(_animationCoroutine);

        _animationCoroutine = StartCoroutine(_isOn ? TurnOffAnimation() : TurnOnAnimation());
    }

    private IEnumerator TurnOnAnimation()
    {
        _isAnimating = true;
        Debug.Log("Turn on animation started");

        yield return new WaitForSeconds(_turnOnCooldown);

        if (IsOwner && toggleOnSound)
            AudioManager.Instance.PlaySoundAtPosition(toggleOnSound, transform.position);
        SetLightState(true);

        _isAnimating = false;
        _animationCoroutine = null;
        Debug.Log("Turn on animation ended");
    }

    private IEnumerator TurnOffAnimation()
    {
        _isAnimating = true;
        Debug.Log("Turn off animation started");

        // Turn off light immediately
        if (IsOwner && toggleOffSound)
            AudioManager.Instance.PlaySoundAtPosition(toggleOffSound, transform.position);

        // Immediately turn off the light
        SetLightState(false);

        // But keep the animation going to prevent immediate re-toggling
        yield return new WaitForSeconds(_turnOffCooldown);

        // Now animation is complete, allow toggling again
        _isAnimating = false;
        _animationCoroutine = null;
        Debug.Log("Turn off animation ended");
    }

    private void SetLightState(bool state)
    {
        if (!IsOwner && !IsServerInitialized)
            return;

        // Call the server RPC to change state for everyone
        if (IsOwner && !IsServerInitialized)
            SetLightStateServerRpc(state);
        else if (IsServerInitialized)
            UpdateLightState(state);
    }

    [ServerRpc]
    private void SetLightStateServerRpc(bool state)
    {
        // Update the state on server
        UpdateLightState(state);
    }

    private void UpdateLightState(bool state)
    {
        // Server-side logic for updating the light state
        _isOn = state;
        _syncedIsOn.Value = state; // This will trigger the sync to all clients
    }

    private void OnFlashlightStateChanged(bool oldValue, bool newValue, bool asServer)
    {
        // Update local light state to match network state
        _isOn = newValue;

        // Update the actual light components
        if (spotLight)
            spotLight.enabled = newValue;

        if (!IsOwner)
        {
            if (newValue && toggleOnSound)
                AudioManager.Instance.PlaySoundAtPosition(toggleOnSound, transform.position);
            else if (!newValue && toggleOffSound)
                AudioManager.Instance.PlaySoundAtPosition(toggleOffSound, transform.position);
        }
    }

    private void UseFlash()
    {
        Debug.Log("Flashlight used flash");
        if (_currentBatteryLevel < flashDrainAmount || !IsOwner)
            return;

        _currentBatteryLevel -= flashDrainAmount;
        _syncedBatteryLevel.Value = _currentBatteryLevel;

        _canFlash = false;
        _flashCooldownTimer = flashCooldown;

        UseFlashServerRpc();
    }

    [ServerRpc]
    private void UseFlashServerRpc()
    {
        UseFlashObserversRpc();
    }

    [ObserversRpc]
    private void UseFlashObserversRpc()
    {
        if (IsOwner)
            return;

        StartCoroutine(FlashEffect());

        if (flashSound != null)
            AudioManager.Instance.PlaySoundAtPosition(flashSound, transform.position);
    }

    private IEnumerator FlashEffect()
    {
        spotLight.intensity *= flashIntensityMultiplier;

        yield return new WaitForSeconds(flashDuration);

        if (_isOn)
            spotLight.intensity = _normalIntensity;
        else
            spotLight.enabled = false;
    }

    public float GetBatteryPercentage()
    {
        return _currentBatteryLevel / maxBatteryLevel * 100f;
    }

    [Header("Hit Detection")]
    [SerializeField]
    private LayerMask hitDetectionLayers;
    private readonly HashSet<GameObject> _hitObjectsThisFrame = new HashSet<GameObject>();

    private void DetectFlashlightHits()
    {
        if (!_isOn || spotLight == null)
            return;

        _hitObjectsThisFrame.Clear();

        Vector3 rayOrigin = spotLight.transform.position;
        Vector3 rayDirection = spotLight.transform.forward;
        float effectiveRange = spotLight.range;
        float coneRadius = Mathf.Tan(spotLight.spotAngle * 0.5f * Mathf.Deg2Rad) * effectiveRange;

        // First, find all potential hits in the cone
        RaycastHit[] hits = Physics.SphereCastAll(
            rayOrigin,
            coneRadius,
            rayDirection,
            effectiveRange,
            hitDetectionLayers
        );

        // Create a list of valid hits after filtering
        List<(RaycastHit hit, float intensity)> validHits = new List<(RaycastHit, float)>();

        _lastOrigin = rayOrigin;
        _lastDirection = rayDirection;
        _lastConeRadius = coneRadius;
        _lastRange = effectiveRange;
        _debugHitPoints.Clear();

        foreach (RaycastHit hit in hits)
        {
            if (_hitObjectsThisFrame.Contains(hit.collider.gameObject))
                continue;

            // Calculate direction and angle to the hit point
            Vector3 hitPos = hit.point;
            Vector3 dirToHit = (hitPos - rayOrigin).normalized;
            float angleToHit = Vector3.Angle(rayDirection, dirToHit);

            // Skip if outside the cone angle
            if (angleToHit > spotLight.spotAngle * 0.5f)
                continue;

            // More accurate occlusion check - cast a ray DIRECTLY to the hit point
            if (
                Physics.Raycast(
                    rayOrigin,
                    dirToHit,
                    out RaycastHit directHit,
                    hit.distance,
                    Physics.AllLayers
                )
            )
            {
                // If we hit something different first, this object is occluded
                if (directHit.collider != hit.collider && !directHit.collider.isTrigger)
                {
                    Debug.DrawLine(rayOrigin, directHit.point, Color.red, 0.1f);
                    Debug.Log(
                        $"Hit occluded by {directHit.collider.name} at distance {directHit.distance} vs original distance {hit.distance}"
                    );
                    _debugHitPoints.Add((directHit.point, directHit.normal, Color.red));
                    continue; // Skip this hit
                }
            }

            // Object passes all checks - it's a valid hit
            // Calculate intensity based on angle from center of beam
            float normalizedAngle = angleToHit / (spotLight.spotAngle * 0.5f);
            float intensityFactor = 1.0f - normalizedAngle;

            // Debug visualization of valid hit
            Debug.DrawLine(rayOrigin, hit.point, Color.green, 0.1f);

            // Add to valid hits
            validHits.Add((hit, intensityFactor));
        }

        // Process all valid hits
        foreach (var (hit, intensity) in validHits)
        {
            ProcessHit(hit, intensity);
        }
    }

    private void ProcessHit(RaycastHit hit, float intensityFactor)
    {
        GameObject hitObject = hit.collider.gameObject;
        if (_hitObjectsThisFrame.Contains(hitObject))
            return;

        _hitObjectsThisFrame.Add(hitObject);

        // Try to get component on the hit object first
        IFlashlightDetectable detectable = hitObject.GetComponent<IFlashlightDetectable>();

        // If not found on the direct hit object, try to find it on any parent
        if (detectable == null)
            detectable = hitObject.GetComponentInParent<IFlashlightDetectable>();

        // If still not found, exit
        if (detectable == null)
            return;

        // Apply distance falloff to intensity
        float distanceFactor =
            hit.distance <= 5f
                ? 1f
                : Mathf.Lerp(1f, 0.5f, (hit.distance - 5f) / (spotLight.range - 5f));
        float baseIntensity = 0.3f;

        float finalIntensity =
            (baseIntensity + intensityFactor * 0.7f) * distanceFactor * spotLight.intensity;

        Debug.Log($"Flashlight hit {intensityFactor} {distanceFactor} {finalIntensity}");

        // Special case for flash ability
        if (_flashCooldownTimer > flashCooldown - flashDuration)
            finalIntensity *= flashIntensityMultiplier;

        detectable.OnFlashlightHit(this, hit.point, hit.normal, finalIntensity);
    }

    [Header("Debug")]
    [SerializeField]
    private bool showDebugGizmos = true;
    private Vector3 _lastOrigin;
    private Vector3 _lastDirection;
    private float _lastConeRadius;
    private float _lastRange;
    private List<(Vector3 point, Vector3 normal, Color color)> _debugHitPoints =
        new List<(Vector3, Vector3, Color)>();

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || !_isOn || !Application.isPlaying)
            return;

        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);

        if (_lastConeRadius > 0 && _lastRange > 0)
        {
            DrawWireframeCone(_lastOrigin, _lastDirection, _lastConeRadius, _lastRange);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(_lastOrigin, _lastDirection * _lastRange);
        }

        foreach (var (point, normal, color) in _debugHitPoints)
        {
            Gizmos.color = color;
            Gizmos.DrawSphere(point, 0.1f);
            Gizmos.DrawRay(point, normal * 0.5f);
        }
    }

    private void DrawWireframeCone(Vector3 origin, Vector3 direction, float radius, float length)
    {
        int segments = 16;
        Vector3 center = origin + direction * length;
        Vector3 forward = direction;
        Vector3 up = Vector3.up;

        if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.99f)
            up = Vector3.right;

        Vector3 right = Vector3.Cross(up, forward).normalized;
        up = Vector3.Cross(forward, right).normalized;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = ((float)i / segments) * 2 * Mathf.PI;
            float angle2 = ((float)(i + 1) / segments) * 2 * Mathf.PI;

            Vector3 pos1 = center + (up * Mathf.Sin(angle1) + right * Mathf.Cos(angle1)) * radius;
            Vector3 pos2 = center + (up * Mathf.Sin(angle2) + right * Mathf.Cos(angle2)) * radius;

            Gizmos.DrawLine(pos1, pos2);
            Gizmos.DrawLine(origin, pos1);
        }
    }
}
