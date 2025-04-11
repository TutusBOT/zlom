using System;
using System.Collections.Generic;
using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using TMPro;
using UnityEngine;

// public enum ValuableSize { Small, Medium, Large }

public class Valuable2 : NetworkBehaviour
{
    public int initialCashValue = 100;
    private readonly SyncVar<float> _currentCashValue = new(100f);

    public float minDamageMultiplier = 0.05f;
    public float maxDamageMultiplier = 0.5f;
    public float breakThreshold = 5f;
    public ValuableSize size;
    public string breakSoundId = "glass_break";
    public string damageSoundId = "glass_damage";

    public static event Action<GameObject> OnItemBroke;
    protected readonly SyncVar<bool> _isBeingHeld = new(false);

    [Header("Value Display")]
    [SerializeField] private GameObject valueDisplayPrefab;
    private GameObject activeValueDisplay;
    private TextMeshProUGUI valueText;
    private float tooltipDisplayTime = 1f;
    private float tooltipTimer = 0f;

    private Rigidbody rb;
    private Dictionary<NetworkConnection, Vector3> grabbers = new();

    public float sharedLiftForce = 15f;
    public float maxVelocity = 8f;

    private float SizeScale => size switch
    {
        ValuableSize.Small => 0.7f,
        ValuableSize.Medium => 1.0f,
        ValuableSize.Large => 1.5f,
        _ => 1.0f,
    };

    public override void OnStartServer()
    {
        base.OnStartServer();
        _currentCashValue.Value = initialCashValue;
        rb = GetComponent<Rigidbody>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        NetworkTransform netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null)
        {
            netTransform.SetSynchronizePosition(true);
            netTransform.SetSynchronizeRotation(true);
            netTransform.SetSynchronizeScale(false);
            netTransform.SetInterval(1);
            netTransform.SetSendToOwner(true);
        }

        rb = GetComponent<Rigidbody>();
    }

   private void FixedUpdate()
{
    if (!IsServer || grabbers.Count == 0 || rb == null)
        return;

    Vector3 avgTarget = Vector3.zero;
    foreach (var kvp in grabbers)
        avgTarget += kvp.Value;
    avgTarget /= grabbers.Count;

    Vector3 toTarget = avgTarget - rb.position;
    Vector3 velocityDiff = Vector3.zero - rb.linearVelocity;

    float stiffness = 200f; // spring
    float damping = 20f;    // damp oscillation

    Vector3 force = (toTarget * stiffness) + (velocityDiff * damping);

    rb.AddForce(force, ForceMode.Force);

    if (rb.linearVelocity.magnitude > maxVelocity)
        rb.linearVelocity = rb.linearVelocity.normalized * maxVelocity;
}

    protected virtual void Update()
    {
        if (_isBeingHeld.Value)
        {
            tooltipTimer += Time.deltaTime;

            if (tooltipTimer >= tooltipDisplayTime)
            {
                HideTooltip();
                return;
            }

            if (activeValueDisplay != null)
            {
                activeValueDisplay.transform.position = transform.position + Vector3.up * (1.0f * SizeScale);
                if (Camera.main != null)
                    activeValueDisplay.transform.forward = Camera.main.transform.forward;

                if (valueText != null)
                    valueText.text = $"${Mathf.RoundToInt(_currentCashValue.Value)}";
            }
        }
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (!IsServerInitialized)
            return;

        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce >= breakThreshold)
        {
            float damagePercent = Mathf.Lerp(minDamageMultiplier, maxDamageMultiplier, impactForce / 20f);
            ApplyDamage(damagePercent);
        }
    }

    void ApplyDamage(float damagePercent)
    {
        int damageAmount = Mathf.RoundToInt(initialCashValue * damagePercent);
        _currentCashValue.Value -= damageAmount;

        PlayDamageEffectsRpc();

        if (_currentCashValue.Value <= 0)
        {
            Break();
        }
    }

    [ObserversRpc]
    void PlayDamageEffectsRpc()
    {
        ParticleManager.Instance?.PlayEffect("valuable_damage", transform.position, SizeScale);
        AudioManager.Instance?.PlaySound(damageSoundId, transform.position);
    }

    protected virtual void Break()
    {
        if (!IsServerInitialized) return;

        PlayBreakEffectsRpc();
        DestroyValuable();
    }

    [ObserversRpc]
    void PlayBreakEffectsRpc()
    {
        ParticleManager.Instance?.PlayEffect("valuable_break", transform.position, SizeScale);
        AudioManager.Instance?.PlaySound(breakSoundId, transform.position);
    }

    public void DestroyValuable()
    {
        OnItemBroke?.Invoke(gameObject);
        HideTooltip();
        Despawn();
    }

    public float GetCurrentValue() => _currentCashValue.Value;

    public virtual void OnPickedUp()
    {
        if (!IsServerInitialized)
            PickUpServerRpc();
        else
            _isBeingHeld.Value = true;

        tooltipTimer = 0f;

        GetComponent<NetworkTransform>()?.ForceSend();
        if (IsOwner || IsClientInitialized)
            ShowValueTooltip();
    }

    [ServerRpc(RequireOwnership = false)]
    public void PickUpServerRpc()
    {
        _isBeingHeld.Value = true;
    }

    public virtual void OnDropped()
    {
        if (!IsServerInitialized)
            DropServerRpc();
        else
            _isBeingHeld.Value = false;

        HideTooltip();
    }

    [ServerRpc(RequireOwnership = false)]
    public void DropServerRpc()
    {
        _isBeingHeld.Value = false;
    }

    private void ShowValueTooltip()
    {
        if (valueDisplayPrefab != null && activeValueDisplay == null)
        {
            activeValueDisplay = Instantiate(valueDisplayPrefab);
            activeValueDisplay.transform.position = transform.position + Vector3.up * (1.5f * SizeScale);
            valueText = activeValueDisplay.GetComponentInChildren<TextMeshProUGUI>();
            valueText.text = $"${Mathf.RoundToInt(_currentCashValue.Value)}";
        }
    }

    private void HideTooltip()
    {
        if (activeValueDisplay != null)
        {
            Destroy(activeValueDisplay);
            activeValueDisplay = null;
            valueText = null;
        }
    }

    // -------- Shared Lifting Methods -------- //

    public void AddGrabber(NetworkConnection conn, float distance)
    {
        if (!grabbers.ContainsKey(conn))
        {
            Vector3 defaultTarget = transform.position + transform.forward * distance;
            grabbers.Add(conn, defaultTarget);
            _isBeingHeld.Value = true;
        }
    }

    public void RemoveGrabber(NetworkConnection conn)
    {
        if (grabbers.ContainsKey(conn))
        {
            grabbers.Remove(conn);
            if (grabbers.Count == 0)
                _isBeingHeld.Value = false;
        }
    }

    public void UpdateGrabPosition(NetworkConnection conn, Vector3 target)
    {
        if (grabbers.ContainsKey(conn))
        {
            grabbers[conn] = target;
        }
    }
}
