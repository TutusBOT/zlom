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

public class Valuable : NetworkBehaviour, IPickable
{
    public int initialCashValue = 100;

    private readonly SyncVar<float> _currentCashValue = new(100f);

    public float minDamageMultiplier = 0.05f;
    public float maxDamageMultiplier = 0.5f;
    public float breakThreshold = 5f;
    public ValuableSize size;
    public string breakSoundId = "glass_break";
    public string damageSoundId = "glass_damage";

    protected readonly SyncVar<bool> _isBeingHeld = new(false);
    private readonly SyncVar<bool> _isInvulnerable = new(false);
    private float _invulnerabilityDuration = 2f;
    protected Player _player;

    [Header("Value Display")]
    [SerializeField]
    private GameObject valueDisplayPrefab;
    private GameObject _activeValueDisplay;
    private TextMeshProUGUI _valueText;
    private float _tooltipDisplayTime = 1f;
    private float _tooltipTimer = 0f;

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

        _tooltipTimer += Time.deltaTime;

        if (_tooltipTimer >= _tooltipDisplayTime)
        {
            HideTooltip();
            return;
        }

        if (_activeValueDisplay != null)
        {
            _activeValueDisplay.transform.position =
                transform.position + Vector3.up * (1.0f * SizeScale);

            if (Camera.main != null)
            {
                _activeValueDisplay.transform.forward = Camera.main.transform.forward;
            }

            if (_valueText != null)
            {
                _valueText.text = $"${Mathf.RoundToInt(_currentCashValue.Value)}";
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
        IPickable.RaiseItemDestroyed(gameObject);
        HideTooltip();
        Despawn();
    }

    public float GetCurrentValue()
    {
        return _currentCashValue.Value;
    }

    public virtual void OnPickedUp(Player player)
    {
        if (!IsServerInitialized)
            PickUpServerRpc();
        else
            _isBeingHeld.Value = true;

        _tooltipTimer = 0f;
        _player = player;

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

    public bool CanBePickedUp()
    {
        return true;
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    private void ShowValueTooltip()
    {
        Debug.Log($"Showing tooltip for {gameObject.name} {_activeValueDisplay}");
        if (valueDisplayPrefab != null && _activeValueDisplay == null)
        {
            _activeValueDisplay = Instantiate(valueDisplayPrefab);

            _activeValueDisplay.transform.position =
                transform.position + Vector3.up * (1.5f * SizeScale);

            _valueText = _activeValueDisplay.GetComponentInChildren<TextMeshProUGUI>();

            if (_valueText != null)
            {
                _valueText.text = $"${Mathf.RoundToInt(_currentCashValue.Value)}";
            }
        }
    }

    public virtual void OnDropped()
    {
        if (!IsServerInitialized)
            DropServerRpc();
        else
            _isBeingHeld.Value = false;

        _player = null;

        HideTooltip();
    }

    [ServerRpc(RequireOwnership = false)]
    public void DropServerRpc()
    {
        _isBeingHeld.Value = false;
    }

    private void HideTooltip()
    {
        if (_activeValueDisplay != null)
        {
            Destroy(_activeValueDisplay);
            _activeValueDisplay = null;
            _valueText = null;
        }
    }

    private IEnumerator InvulnerabilityTimer()
    {
        yield return new WaitForSeconds(_invulnerabilityDuration);
        _isInvulnerable.Value = false;
    }
}
