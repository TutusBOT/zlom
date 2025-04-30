using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour, IUpgradeable
{
    [Header("Health Settings")]
    [SerializeField]
    private float maxHealth = 100f;

    [SerializeField]
    private bool godModeOnStart = false;

    [Header("Audio")]
    [SerializeField]
    private AudioClip damageSound;

    [SerializeField]
    private AudioClip healSound;

    [SerializeField]
    private AudioClip deathSound;

    private readonly SyncVar<float> _syncedCurrentHealth = new SyncVar<float>();
    private readonly SyncVar<bool> _syncedIsDead = new SyncVar<bool>();

    private bool _isInvulnerable = false;
    private float _baseMaxHealth;

    public float CurrentHealth => _syncedCurrentHealth.Value;
    public float MaxHealth => maxHealth;
    public bool IsDead => _syncedIsDead.Value;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;
    public event Action OnRespawn;

    private void Awake()
    {
        _syncedCurrentHealth.OnChange += OnHealthValueChanged;
        _syncedIsDead.OnChange += OnDeadStateChanged;

        _baseMaxHealth = maxHealth;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        _syncedCurrentHealth.Value = maxHealth;
        _syncedIsDead.Value = false;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner && godModeOnStart)
            _isInvulnerable = true;
    }

    public void TakeDamage(float amount, GameObject instigator = null)
    {
        if (!IsServerInitialized)
        {
            TakeDamageServerRpc(amount);
            return;
        }

        ProcessDamage(amount);
    }

    [ServerRpc]
    private void TakeDamageServerRpc(float amount)
    {
        ProcessDamage(amount);
    }

    private void ProcessDamage(float amount)
    {
        if (!IsServerInitialized)
            return;

        if (_syncedIsDead.Value || _isInvulnerable)
            return;

        float newHealth = Mathf.Max(0, _syncedCurrentHealth.Value - amount);
        _syncedCurrentHealth.Value = newHealth;

        if (newHealth <= 0 && !_syncedIsDead.Value)
        {
            Die();
        }
        else
        {
            PlayDamageEffectObserversRpc();
        }
    }

    public void Heal(float amount)
    {
        if (!IsServerInitialized)
        {
            HealServerRpc(amount);
            return;
        }

        ProcessHeal(amount);
    }

    [ServerRpc]
    private void HealServerRpc(float amount)
    {
        ProcessHeal(amount);
    }

    private void ProcessHeal(float amount)
    {
        if (!IsServerInitialized)
            return;

        if (_syncedIsDead.Value)
            return;

        // Apply healing
        float newHealth = Mathf.Min(maxHealth, _syncedCurrentHealth.Value + amount);
        _syncedCurrentHealth.Value = newHealth;

        PlayHealEffectObserversRpc();
    }

    private void Die()
    {
        if (!IsServerInitialized)
            return;

        _syncedIsDead.Value = true;

        // Handle any server-side death logic
        // ...
    }

    private void OnHealthValueChanged(float oldValue, float newValue, bool asServer)
    {
        OnHealthChanged?.Invoke(newValue, maxHealth);
    }

    private void OnDeadStateChanged(bool oldValue, bool newValue, bool asServer)
    {
        if (newValue == true)
        {
            OnDeath?.Invoke();

            PlayDeathEffectObserversRpc();

            // Handle any visual/gameplay changes for death
            if (IsOwner)
            {
                // Player-specific death logic (disable input, etc.)
                // ...
            }
        }
        else if (oldValue == true && newValue == false)
        {
            // Player was just respawned
            OnRespawn?.Invoke();

            // Handle any visual/gameplay changes for respawn
            if (IsOwner)
            {
                // Player-specific respawn logic (re-enable input, etc.)
                // ...
            }
        }
    }

    [ObserversRpc]
    private void PlayDamageEffectObserversRpc()
    {
        if (damageSound != null)
            AudioManager.Instance.PlaySoundAtPosition(damageSound, transform.position);

        // Add camera shake, blood effect, etc.
        if (IsOwner)
        {
            // Player feedback for taking damage (screen flash, controller rumble, etc.)
        }
    }

    [ObserversRpc]
    private void PlayHealEffectObserversRpc()
    {
        if (healSound != null)
            AudioManager.Instance.PlaySoundAtPosition(healSound, transform.position);

        // Add heal particle effects
    }

    [ObserversRpc]
    private void PlayDeathEffectObserversRpc()
    {
        if (deathSound != null)
            AudioManager.Instance.PlaySoundAtPosition(deathSound, transform.position);

        // Death animation, particle effects, etc.
    }

    public void SetInvulnerable(bool invulnerable)
    {
        if (!IsOwner && !IsServerInitialized)
            return;

        _isInvulnerable = invulnerable;
    }

    public float GetHealthPercentage()
    {
        return _syncedCurrentHealth.Value / maxHealth;
    }

    public bool CanHandleUpgrade(UpgradeType type)
    {
        return type == UpgradeType.Health;
    }

    public void ApplyUpgrade(UpgradeType type, int level, float value)
    {
        if (type == UpgradeType.Health)
        {
            maxHealth = _baseMaxHealth + value;
            _syncedCurrentHealth.Value = Mathf.Min(_syncedCurrentHealth.Value + value, maxHealth);
        }
    }
}
