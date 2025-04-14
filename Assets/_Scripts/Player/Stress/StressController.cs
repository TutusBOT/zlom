using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.UI;

public class StressController : NetworkBehaviour
{
    #region Enums and Types

    public enum AfflictionType
    {
        None,
        Paranoid,
        Fearful,
    }

    [System.Serializable]
    public class AfflictionData
    {
        public AfflictionType type;
        public string ttsQuote;
        public AudioClip voiceClip; // Optional pre-recorded alternative to TTS
        public GameObject visualEffectPrefab; // For visual indicators
    }

    #endregion

    #region Serialized Fields
    [SerializeField]
    private Player player;

    [Header("Stress Settings")]
    [SerializeField]
    private float maxStress = 100f;

    [SerializeField]
    private float stressDecayRate = 1f;

    [SerializeField]
    private float safeRoomDecayMultiplier = 5f;

    [Header("Visual Feedback")]
    [SerializeField]
    private Image stressVignette;

    [SerializeField]
    private float minVignetteAlpha = 0f;

    [SerializeField]
    private float maxVignetteAlpha = 0.8f;

    [SerializeField]
    private AnimationCurve stressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField]
    private Transform cameraTransform;

    [Header("Audio Feedback")]
    [SerializeField]
    private AudioClip heartbeatSound;

    [SerializeField]
    private AudioClip breathingSound;

    [SerializeField]
    private float minBreathingVolume = 0f;

    [SerializeField]
    private float maxBreathingVolume = 1f;

    [SerializeField]
    private float minHeartRate = 0.5f;

    [SerializeField]
    private float maxHeartRate = 2f;

    [Header("Stress Sources Configuration")]
    [SerializeField]
    private float monsterVisibleStressRate = 5f;

    [SerializeField]
    private float monsterProximityThreshold = 10f;

    [SerializeField]
    private float darknessBuildupDelay = 30f;

    [SerializeField]
    private float darknessStressRate = 2f;

    [SerializeField]
    private float isolationDistance = 15f;

    [SerializeField]
    private float isolationStressRate = 1f;

    [SerializeField]
    private float damageStressAmount = 15f;

    [SerializeField]
    private float lightBreakStressAmount = 15f;

    [Header("Afflictions")]
    [SerializeField]
    private List<AfflictionData> afflictions = new List<AfflictionData>();

    [SerializeField]
    private float afflictionCooldown = 5f; // Time before applying effects after triggering

    [SerializeField]
    private GameObject shakyEyesPrefab; // Visual indicator for other players
    #endregion

    #region Networked State

    // Current stress level - synced to all clients but with local smoothing
    private readonly SyncVar<float> _syncedStressValue = new SyncVar<float>(0f);

    // Current affliction - controlled by server
    private readonly SyncVar<AfflictionType> _currentAffliction = new SyncVar<AfflictionType>(
        AfflictionType.None
    );

    // Visible to others - are we melting down?
    private readonly SyncVar<bool> _visibleMeltdown = new SyncVar<bool>(false);

    #endregion

    #region Private State

    // Local-only smoothed stress value for visuals
    private float _localStressValue = 0f;
    private float _stressSmoothingRate = 3f;

    // Stress source tracking
    private float _timeInDarkness = 0f;
    private float _timeIsolated = 0f;
    private bool _isInSafeRoom = false;
    private bool _isInDarkness = false;
    private Coroutine _stressCheckCoroutine;

    // Affliction state
    private float _afflictionTimer = 0f;
    private bool _isAfflictionActive = false;
    private IAffliction _currentAfflictionImplementation;

    // Visual state
    private GameObject _meltdownVisualIndicator;
    private float _cameraShakeIntensity = 0f;
    private Vector3 _originalCameraPosition;
    private bool _isStageComplete = false;

    #endregion

    #region Initialization and Updates

    private void Awake()
    {
        if (player == null)
            Debug.LogError("Player reference is not assigned in the inspector.");

        // Initial visual state
        if (stressVignette != null)
            stressVignette.color = new Color(
                stressVignette.color.r,
                stressVignette.color.g,
                stressVignette.color.b,
                0
            );
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            _localStressValue = _syncedStressValue.Value;

            // Get camera reference properly
            if (cameraTransform == null)
                cameraTransform = GetComponentInChildren<Camera>()?.transform;

            if (cameraTransform != null)
                _originalCameraPosition = cameraTransform.localPosition;

            _stressCheckCoroutine = StartCoroutine(ContinuousStressCheckCoroutine());
        }
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        // Register callbacks for SyncVars
        _syncedStressValue.OnChange += OnStressValueChanged;
        _currentAffliction.OnChange += OnAfflictionChanged;
        _visibleMeltdown.OnChange += OnMeltdownVisibilityChanged;
    }

    private void OnDestroy()
    {
        if (_stressCheckCoroutine != null)
        {
            StopCoroutine(_stressCheckCoroutine);
            _stressCheckCoroutine = null;
        }
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        UpdateStressVisuals();

        // Handle affliction effects if active
        if (_isAfflictionActive && _currentAffliction.Value != AfflictionType.None)
            ApplyAfflictionEffects();

        DebugControls();
    }

    private void LateUpdate()
    {
        if (!IsOwner || cameraTransform == null)
            return;

        // Apply camera shake based on stress
        if (_cameraShakeIntensity > 0)
        {
            Vector3 shakeOffset = Random.insideUnitSphere * _cameraShakeIntensity;
            cameraTransform.localPosition = _originalCameraPosition + shakeOffset;
        }
    }

    #endregion

    #region Stress Management

    /// <summary>
    /// Adds stress from external sources
    /// </summary>
    public void AddStress(float amount, string source = "unknown")
    {
        if (!IsOwner || _isStageComplete)
            return;

        // Apply locally first for immediate feedback
        _localStressValue = Mathf.Clamp(_localStressValue + amount, 0, maxStress);

        // Then sync to server occasionally (only on significant changes)
        if (Mathf.Abs(_localStressValue - _syncedStressValue.Value) >= 5f)
        {
            UpdateStressServerRpc(_localStressValue);
        }

        // If stress maxed out and no affliction yet, trigger affliction
        if (_localStressValue >= maxStress && _currentAffliction.Value == AfflictionType.None)
        {
            TriggerAfflictionServerRpc();
        }
    }

    /// <summary>
    /// Coroutine that checks continuous stress sources at fixed intervals
    /// </summary>
    private IEnumerator ContinuousStressCheckCoroutine()
    {
        // Wait for next frame before starting
        yield return null;

        WaitForSeconds waitOneSecond = new WaitForSeconds(1.0f);

        while (true)
        {
            // Apply darkness stress
            if (_isInDarkness)
            {
                _timeInDarkness += 1.0f; // Add one second

                if (_timeInDarkness > darknessBuildupDelay)
                {
                    AddStress(darknessStressRate, "darkness");
                }
            }
            else
            {
                _timeInDarkness = 0;
            }

            // Apply isolation stress
            // if (player.IsIsolated(isolationDistance))
            // {
            //     _timeIsolated += 1.0f; // Add one second
            //     if (_timeIsolated > 5f) // Short grace period
            //     {
            //         AddStress(isolationStressRate, "isolation");
            //     }
            // }
            // else
            // {
            //     _timeIsolated = 0;
            // }

            // In safe rooms, reduce stress
            if (_isInSafeRoom && _currentAffliction.Value == AfflictionType.None)
            {
                AddStress(-stressDecayRate * safeRoomDecayMultiplier, "safeRoom");
            }

            // Wait for next check interval
            yield return waitOneSecond;
        }
    }

    /// <summary>
    /// Updates stress visual effects (vignette, camera shake, etc)
    /// </summary>
    private void UpdateStressVisuals()
    {
        // Smoothly interpolate local stress value toward synced value
        _localStressValue = Mathf.Lerp(
            _localStressValue,
            _syncedStressValue.Value,
            Time.deltaTime * _stressSmoothingRate
        );

        // Calculate stress factor (0-1)
        float stressFactor = _localStressValue / maxStress;

        // Apply stress curve for non-linear effects
        float curvedStress = stressCurve.Evaluate(stressFactor);

        // Update vignette
        if (stressVignette != null)
        {
            Color c = stressVignette.color;
            c.a = Mathf.Lerp(minVignetteAlpha, maxVignetteAlpha, curvedStress);
            stressVignette.color = c;
        }

        // Update audio effects
        if (breathingSource != null)
        {
            breathingSource.volume = Mathf.Lerp(
                minBreathingVolume,
                maxBreathingVolume,
                curvedStress
            );
        }

        if (heartbeatSource != null)
        {
            heartbeatSource.pitch = Mathf.Lerp(minHeartRate, maxHeartRate, curvedStress);
            heartbeatSource.volume = Mathf.Lerp(0.2f, 1f, curvedStress);
        }

        // Camera shake intensity
        _cameraShakeIntensity = curvedStress * 0.05f;
    }

    #endregion

    #region Affliction Handling

    /// <summary>
    /// Apply the current affliction's effects
    /// </summary>
    private void ApplyAfflictionEffects()
    {
        if (_currentAfflictionImplementation != null && _isAfflictionActive)
        {
            _currentAfflictionImplementation.Update();
        }
    }

    /// <summary>
    /// Handles the stage completion event
    /// </summary>
    public void OnStageComplete()
    {
        _isStageComplete = true;

        if (_currentAffliction.Value != AfflictionType.None)
        {
            ResetAfflictionServerRpc();
        }
    }

    #endregion

    #region Network RPCs and Callbacks

    [ServerRpc]
    private void UpdateStressServerRpc(float newStressValue)
    {
        // Server validates and applies the new stress value
        _syncedStressValue.Value = Mathf.Clamp(newStressValue, 0, maxStress);
    }

    [ServerRpc]
    private void TriggerAfflictionServerRpc()
    {
        Debug.Log(
            $"TriggerAfflictionServerRpc called. Current affliction: {_currentAffliction.Value}"
        );

        if (_currentAffliction.Value == AfflictionType.None)
        {
            // Only select afflictions we have implementations for
            List<AfflictionType> implementedTypes = new List<AfflictionType>
            {
                AfflictionType.Paranoid,
                AfflictionType.Fearful,
            };

            int randomIndex = Random.Range(0, implementedTypes.Count);
            AfflictionType selectedType = implementedTypes[randomIndex];

            Debug.Log($"Selected affliction: {selectedType}");

            // Set the affliction - this triggers OnAfflictionChanged
            _currentAffliction.Value = selectedType;

            // Make meltdown visible to others
            _visibleMeltdown.Value = true;

            // The quote will come from the implementation in OnAfflictionChanged
        }
    }

    [ServerRpc]
    private void ResetAfflictionServerRpc()
    {
        // Reset affliction state on stage completion
        _currentAffliction.Value = AfflictionType.None;
        _visibleMeltdown.Value = false;
        _syncedStressValue.Value = 0f;
    }

    [ObserversRpc]
    private void TriggerAfflictionQuoteClientRpc(string quote)
    {
        Debug.Log(quote);
        if (IsOwner)
        {
            // TTSManager.Instance.SpeakTextAtPosition(quote, transform.position);
            // Implement TTSManager to handle text-to-speech
        }

        StartCoroutine(ActivateAfflictionAfterDelay());
    }

    private IEnumerator ActivateAfflictionAfterDelay()
    {
        yield return new WaitForSeconds(afflictionCooldown);
        _isAfflictionActive = true;

        if (_currentAfflictionImplementation != null)
        {
            _currentAfflictionImplementation.OnAfflictionActivated();
        }
    }

    private void OnStressValueChanged(float oldValue, float newValue, bool asServer)
    {
        // This is called on all clients when the synced stress value changes
        if (!asServer && IsOwner)
        {
            // Don't overwrite local value completely to avoid jerky visuals
            // Instead, move local value closer to synced value
            float diff = newValue - _localStressValue;
            _localStressValue += diff * 0.5f;
        }
    }

    private void OnAfflictionChanged(
        AfflictionType oldValue,
        AfflictionType newValue,
        bool asServer
    )
    {
        Debug.Log($"OnAfflictionChanged: {oldValue} -> {newValue}, asServer: {asServer}");

        // Clean up old implementation first
        if (_currentAfflictionImplementation != null)
        {
            Debug.Log(
                $"Cleaning up previous implementation of type {_currentAfflictionImplementation.Type}"
            );
            _currentAfflictionImplementation.OnAfflictionDeactivated();
            _currentAfflictionImplementation = null;
        }

        // Create new implementation if needed
        if (newValue != AfflictionType.None)
        {
            Debug.Log($"Creating implementation for {newValue}");
            _currentAfflictionImplementation = AfflictionFactory.CreateAffliction(
                newValue,
                this,
                player
            );

            if (_currentAfflictionImplementation != null)
            {
                Debug.Log(
                    $"Successfully created implementation of type {_currentAfflictionImplementation.Type}"
                );

                // Verify types match
                if (_currentAfflictionImplementation.Type != newValue)
                {
                    Debug.LogError(
                        $"Type mismatch! SyncVar: {newValue}, Implementation: {_currentAfflictionImplementation.Type}"
                    );
                }

                // Only server should trigger the RPC
                if (asServer)
                {
                    string quote = _currentAfflictionImplementation.QuoteText;
                    TriggerAfflictionQuoteClientRpc(quote);
                }
            }
            else
            {
                Debug.LogError($"Failed to create implementation for {newValue}");
            }
        }
    }

    private void OnMeltdownVisibilityChanged(bool oldValue, bool newValue, bool asServer)
    {
        if (newValue && !IsOwner)
        {
            if (_meltdownVisualIndicator == null && _currentAfflictionImplementation != null)
            {
                _meltdownVisualIndicator = _currentAfflictionImplementation.CreateVisualIndicator(
                    transform
                );

                // If the affliction doesn't provide a visual indicator, fall back to the default
                if (_meltdownVisualIndicator == null && shakyEyesPrefab != null)
                {
                    _meltdownVisualIndicator = Instantiate(shakyEyesPrefab, transform);
                }
            }
        }
        else if (!newValue && _meltdownVisualIndicator != null)
        {
            Destroy(_meltdownVisualIndicator);
            _meltdownVisualIndicator = null;
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Set whether the player is in darkness
    /// </summary>
    public void SetDarknessState(bool inDarkness)
    {
        _isInDarkness = inDarkness;
    }

    /// <summary>
    /// Set whether the player is in a safe room
    /// </summary>
    public void SetSafeRoomState(bool inSafeRoom)
    {
        _isInSafeRoom = inSafeRoom;
    }

    /// <summary>
    /// Handle player taking damage
    /// </summary>
    public void OnTakeDamage(float damage)
    {
        AddStress(damageStressAmount * damage, "damage");
    }

    /// <summary>
    /// Handle flashlight breaking
    /// </summary>
    public void OnLightSourceBroken()
    {
        AddStress(lightBreakStressAmount, "lightBreak");
    }

    /// <summary>
    /// Handle witnessing teammate death
    /// </summary>
    public void OnTeammateKilled(GameObject teammate)
    {
        AddStress(30f, "teammateKilled");
    }

    /// <summary>
    /// Gets current affliction for other systems
    /// </summary>
    public AfflictionType GetCurrentAffliction()
    {
        return _currentAffliction.Value;
    }

    /// <summary>
    /// Gets current stress level for UI/other systems (0-1)
    /// </summary>
    public float GetStressLevel()
    {
        return _localStressValue / maxStress;
    }

    #endregion

    #region Debug

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void DebugControls()
    {
        // Debug controls to manually adjust stress (remove in production)
        if (Input.GetKeyDown(KeyCode.PageUp))
        {
            AddStress(10f, "debug");
        }

        if (Input.GetKeyDown(KeyCode.PageDown))
        {
            AddStress(-10f, "debug");
        }

        if (Input.GetKeyDown(KeyCode.End))
        {
            // Max out stress to trigger affliction
            AddStress(maxStress, "debug");
        }
    }
#endif
    #endregion
}
