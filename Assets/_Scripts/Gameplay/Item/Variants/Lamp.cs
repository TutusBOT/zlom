using System;
using FishNet.Object;
using UnityEngine;

public class Lamp : Item, ILightSource
{
    [Header("Lamp Settings")]
    [SerializeField]
    private Light lampLight;

    [SerializeField]
    private float lightRange = 8f;

    [SerializeField]
    private float lightIntensity = 1.5f;

    [SerializeField]
    private Color lightColor = Color.white;

    [SerializeField]
    private LayerMask affectedLayers;

    [Header("Feedback")]
    [SerializeField]
    private AudioClip toggleSound;

    [SerializeField]
    private GameObject lightEffectPrefab;

    [Header("Battery")]
    [SerializeField]
    private bool hasBattery = true;

    [SerializeField]
    private float batteryDuration = 300f; // In seconds

    [SerializeField]
    private SphereCollider _lightTrigger;

    private bool _isOn = false;
    private float _batteryRemaining;
    private GameObject _lightEffect;

    public override void OnStartClient()
    {
        base.OnStartClient();
        InitializeLamp();
    }

    private void InitializeLamp()
    {
        // Initialize battery
        _batteryRemaining = batteryDuration;

        // Create light trigger collider
        if (_lightTrigger == null)
        {
            _lightTrigger = gameObject.AddComponent<SphereCollider>();
            _lightTrigger.isTrigger = true;
            _lightTrigger.radius = lightRange;
            _lightTrigger.enabled = false;
        }

        // Create light if not assigned
        if (lampLight == null)
        {
            GameObject lightObj = new GameObject("LampLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.up * 0.1f;
            lampLight = lightObj.AddComponent<Light>();
            lampLight.type = LightType.Point;
        }

        // Configure light settings
        lampLight.range = lightRange;
        lampLight.intensity = lightIntensity;
        lampLight.color = lightColor;
        lampLight.shadows = LightShadows.Soft;
        lampLight.enabled = false;

        // Create visual light effect if prefab exists
        if (lightEffectPrefab != null && _lightEffect == null)
        {
            _lightEffect = Instantiate(lightEffectPrefab, transform);
            _lightEffect.transform.localPosition = Vector3.zero;
            _lightEffect.SetActive(false);
        }
    }

    protected override void ExecuteItemAction()
    {
        // Toggle lamp on/off when used
        ToggleLampServerRpc(!_isOn);
    }

    private void Update()
    {
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                isOnCooldown = false;
            }
        }

        Debug.Log($"Item: {itemName}, Cooldown: {isOnCooldown}, Timer: {cooldownTimer}");
        if (!isOnCooldown && hasBattery && isBeingHeld && Input.GetKeyDown(KeyCode.E))
        {
            UseItem();
        }

        if (_isOn && hasBattery)
        {
            _batteryRemaining -= Time.deltaTime;
            if (_batteryRemaining <= 0)
            {
                _batteryRemaining = 0;
                ToggleLampServerRpc(false);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleLampServerRpc(bool turnOn)
    {
        // Don't allow turning on if battery dead
        if (turnOn && hasBattery && _batteryRemaining <= 0)
            return;

        _isOn = turnOn;

        // Update all clients
        ToggleLampClientRpc(_isOn);
    }

    [ObserversRpc]
    private void ToggleLampClientRpc(bool isOn)
    {
        _isOn = isOn;

        // Update light components
        if (lampLight != null)
            lampLight.enabled = _isOn;

        if (_lightTrigger != null)
            _lightTrigger.enabled = _isOn;

        if (_lightEffect != null)
            _lightEffect.SetActive(_isOn);

        // Play toggle sound
        if (toggleSound != null)
        {
            AudioSource.PlayClipAtPoint(toggleSound, transform.position);
        }

        // If turning off, notify objects that were in light
        if (!_isOn)
        {
            NotifyObjectsLeavingLight();
        }
    }

    private void NotifyObjectsLeavingLight()
    {
        // Find all colliders within the light's range
        Collider[] collidersInLight = Physics.OverlapSphere(
            transform.position,
            lightRange,
            affectedLayers
        );

        foreach (Collider col in collidersInLight)
        {
            if (col.gameObject == gameObject)
                continue; // Skip self

            // Get the proper target (check for LightDetector in hierarchy)
            GameObject targetObject = GetTargetGameObject(col.gameObject);

            // Notify that this object is no longer in light
            LightSourceEvents.NotifyObjectExitedLight(targetObject, this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isOn)
            return;

        GameObject targetObject = GetTargetGameObject(other.gameObject);
        LightSourceEvents.NotifyObjectEnteredLight(targetObject, this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_isOn)
            return;

        GameObject targetObject = GetTargetGameObject(other.gameObject);
        LightSourceEvents.NotifyObjectExitedLight(targetObject, this);
    }

    // Helper method to find the right target in the hierarchy
    private GameObject GetTargetGameObject(GameObject original)
    {
        // First check if this object has a LightDetector
        if (original.GetComponent<LightDetector>() != null)
            return original;

        // Check if parent has a LightDetector
        if (
            original.transform.parent != null
            && original.transform.parent.GetComponent<LightDetector>() != null
        )
            return original.transform.parent.gameObject;

        // If not, check up the hierarchy for any LightDetector
        Transform current = original.transform.parent;
        while (current != null)
        {
            if (current.GetComponent<LightDetector>() != null)
                return current.gameObject;
            current = current.parent;
        }

        // If no LightDetector found in hierarchy, return the original object
        return original;
    }

    // Public methods for external control
    public void TurnOn() => ToggleLampServerRpc(true);

    public void TurnOff() => ToggleLampServerRpc(false);

    public float GetBatteryPercentage() =>
        hasBattery ? (_batteryRemaining / batteryDuration) * 100 : 100;

    public bool IsLampOn() => _isOn;
}
