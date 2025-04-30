using System;
using System.Collections;
using FishNet.Component.Transforming;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using TMPro;
using UnityEngine;

public enum ValuableSize
{
    Small,
    Medium,
    Large,
}

public class Valuable : NetworkBehaviour
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
    private readonly SyncVar<bool> _isInvulnerable = new(false);
    private float invulnerabilityDuration = 2f;

    [Header("Value Display")]
    [SerializeField]
    private GameObject valueDisplayPrefab;
    private GameObject activeValueDisplay;
    private TextMeshProUGUI valueText;
    private float tooltipDisplayTime = 1f;
    private float tooltipTimer = 0f;

    private float SizeScale
    {
        get
        {
            return size switch
            {
                ValuableSize.Small => 0.7f,
                ValuableSize.Medium => 1.0f,
                ValuableSize.Large => 1.5f,
                _ => 1.0f,
            };
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        NetworkTransform netTransform = GetComponent<NetworkTransform>();
        if (netTransform == null)
        {
            Debug.LogError(
                $"NetworkTransform missing on valuable {gameObject.name}! Add it to the prefab."
            );
            return;
        }

        // Configure for continuous physics synchronization
        netTransform.SetSynchronizePosition(true);
        netTransform.SetSynchronizeRotation(true);
        netTransform.SetSynchronizeScale(false);

        // Update more frequently when being manipulated - improves smoothness
        netTransform.SetInterval(1);

        // Make sure settings prioritize smooth movement
        netTransform.SetSendToOwner(true);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        _currentCashValue.Value = initialCashValue;

        _isInvulnerable.Value = true;
        StartCoroutine(InvulnerabilityTimer());
    }

    protected virtual void Update()
    {
        if (!_isBeingHeld.Value)
            return;

        tooltipTimer += Time.deltaTime;

        if (tooltipTimer >= tooltipDisplayTime)
        {
            HideTooltip();
            return;
        }

        if (activeValueDisplay != null)
        {
            activeValueDisplay.transform.position =
                transform.position + Vector3.up * (1.0f * SizeScale);

            if (Camera.main != null)
            {
                activeValueDisplay.transform.forward = Camera.main.transform.forward;
            }

            if (valueText != null)
            {
                valueText.text = $"${Mathf.RoundToInt(_currentCashValue.Value)}";
            }
        }
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (!IsServerInitialized)
            return;

        if (_isInvulnerable.Value)
            return;

        float impactForce = collision.relativeVelocity.magnitude;
        Debug.Log($"Impact force: {impactForce}");

        if (impactForce >= breakThreshold)
        {
            float damagePercent = Mathf.Lerp(
                minDamageMultiplier,
                maxDamageMultiplier,
                impactForce / 20f
            );
            Debug.Log($"Damage percent: {damagePercent}");
            ApplyDamage(damagePercent);
        }
    }

    void ApplyDamage(float damagePercent)
    {
        int damageAmount = Mathf.RoundToInt(initialCashValue * damagePercent);
        _currentCashValue.Value -= damageAmount;

        PlayDamageEffectsRpc();

        Debug.Log($"Item hit! Lost {damageAmount} value. Remaining: {_currentCashValue.Value}");

        if (_currentCashValue.Value <= 0)
        {
            Break();
            return;
        }
    }

    [ObserversRpc]
    void PlayDamageEffectsRpc()
    {
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayEffect("valuable_damage", transform.position, SizeScale);
        }

        AudioManager.Instance.PlaySound(damageSoundId, transform.position);
    }

    protected virtual void Break()
    {
        if (!IsServerInitialized)
        {
            Debug.LogWarning(
                "Attempted to break an item from client-side, use BreakServerRpc instead"
            );
            return;
        }

        Debug.Log($"Item broken! Value lost: {initialCashValue}");
        PlayBreakEffectsRpc();
        DestroyValuable();
    }

    [ObserversRpc]
    void PlayBreakEffectsRpc()
    {
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayEffect("valuable_break", transform.position, SizeScale);
        }

        AudioManager.Instance.PlaySound(breakSoundId, transform.position);
    }

    public void DestroyValuable()
    {
        OnItemBroke?.Invoke(gameObject);
        HideTooltip();
        Despawn();
    }

    public float GetCurrentValue()
    {
        return _currentCashValue.Value;
    }

    public virtual void OnPickedUp()
    {
        if (!IsServerInitialized)
            PickUpServerRpc();
        else
            _isBeingHeld.Value = true;

        tooltipTimer = 0f;

        if (IsOwner || IsClientInitialized)
        {
            ShowValueTooltip();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void PickUpServerRpc()
    {
        _isBeingHeld.Value = true;
    }

    private void ShowValueTooltip()
    {
        Debug.Log($"Showing tooltip for {gameObject.name} {activeValueDisplay}");
        if (valueDisplayPrefab != null && activeValueDisplay == null)
        {
            activeValueDisplay = Instantiate(valueDisplayPrefab);

            activeValueDisplay.transform.position =
                transform.position + Vector3.up * (1.5f * SizeScale);

            valueText = activeValueDisplay.GetComponentInChildren<TextMeshProUGUI>();

            if (valueText != null)
            {
                valueText.text = $"${Mathf.RoundToInt(_currentCashValue.Value)}";
            }
        }
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

    private void HideTooltip()
    {
        if (activeValueDisplay != null)
        {
            Destroy(activeValueDisplay);
            activeValueDisplay = null;
            valueText = null;
        }
    }

    private IEnumerator InvulnerabilityTimer()
    {
        yield return new WaitForSeconds(invulnerabilityDuration);
        _isInvulnerable.Value = false;
    }
}
