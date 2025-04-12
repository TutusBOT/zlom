using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class BlisterheartRelic : Valuable
{
    [Header("Relic Properties")]
    [SerializeField]
    private string curseSoundId = "curse_release";

    [SerializeField]
    private GameObject curseEffectPrefab;

    [Header("Heat Properties")]
    [SerializeField]
    private float maxHoldTime = 3.0f;

    [SerializeField]
    private float damageAmount = 10f;

    [SerializeField]
    private float cooldownTime = 5.0f;

    [SerializeField]
    private string heatupSoundId = "metal_heating";

    [SerializeField]
    private string burnSoundId = "burn_sound";

    [SerializeField]
    private Color normalColor = Color.white;

    [SerializeField]
    private Color heatedColor = Color.red;

    private readonly SyncVar<float> _heldTime = new SyncVar<float>(0f);
    private readonly SyncVar<bool> _isCoolingDown = new SyncVar<bool>(false);
    private readonly SyncVar<float> _cooldownRemaining = new SyncVar<float>(0f);
    private readonly SyncVar<float> _heatIntensity = new SyncVar<float>(0f);

    private GameObject heatGlowEffect;
    private Material[] materials;

    public override void OnStartServer()
    {
        base.OnStartServer();
        size = ValuableSize.Medium;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Store all materials for color changing
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            materials = r.materials;
        }

        // Create heat glow effect
        if (curseEffectPrefab != null && heatGlowEffect == null)
        {
            heatGlowEffect = Instantiate(curseEffectPrefab, transform);
            heatGlowEffect.transform.localPosition = Vector3.zero;
            ParticleSystem ps = heatGlowEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var emission = ps.emission;
                emission.enabled = false; // Start with no emission
            }
        }

        // Initialize heat visuals based on current state
        UpdateHeatVisuals(_heatIntensity.Value);
    }

    protected override void Update()
    {
        base.Update();

        // Server-side logic
        if (IsServerInitialized)
        {
            if (_isBeingHeld.Value)
            {
                _heldTime.Value += Time.deltaTime;

                // Calculate heat percentage (0-1)
                _heatIntensity.Value = Mathf.Clamp01(_heldTime.Value / maxHoldTime);

                // Play heating sound at intervals via RPC
                if (_heldTime.Value % 0.75f < 0.05f)
                {
                    PlayHeatSoundRpc(_heatIntensity.Value * 0.5f);
                }

                if (_heldTime.Value >= maxHoldTime)
                {
                    BurnPlayer();
                }
            }
            else if (_isCoolingDown.Value)
            {
                // Cool down over time
                _cooldownRemaining.Value -= Time.deltaTime;
                _heatIntensity.Value = Mathf.Clamp01(_cooldownRemaining.Value / cooldownTime);

                if (_cooldownRemaining.Value <= 0)
                {
                    _isCoolingDown.Value = false;
                    _heatIntensity.Value = 0f;
                }
            }
        }

        // Client-side visuals - all clients update visuals based on synced values
        UpdateHeatVisuals(_heatIntensity.Value);
    }

    [ObserversRpc]
    private void PlayHeatSoundRpc(float volume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(heatupSoundId, transform.position, volume);
        }
    }

    public override void OnPickedUp()
    {
        base.OnPickedUp();

        if (IsServerInitialized)
        {
            if (_isCoolingDown.Value)
            {
                _heldTime.Value = _heatIntensity.Value * maxHoldTime;
            }
            else
            {
                _heldTime.Value = 0f;
            }
        }
    }

    public override void OnDropped()
    {
        base.OnDropped();

        if (IsServerInitialized)
        {
            _isCoolingDown.Value = true;
            _cooldownRemaining.Value = cooldownTime * _heatIntensity.Value;
        }
    }

    [Server]
    private void BurnPlayer()
    {
        // Force drop on all clients
        ForceDrop();

        // Play burn effects on all clients
        PlayBurnEffectsRpc();

        // Find the player that was holding this (likely the owner)
        foreach (var conn in NetworkManager.ServerManager.Clients.Values)
        {
            var player = conn.FirstObject;
            if (
                player != null
                && Vector3.Distance(player.transform.position, transform.position) < 2f
            )
            {
                // Apply damage to the player's health controller
                HealthController healthController = player.GetComponent<HealthController>();
                if (healthController != null)
                {
                    // healthController.TakeDamageServerRpc(damageAmount);
                    Debug.Log(
                        $"Player {conn.ClientId} burned by BlisterheartRelic! Damage: {damageAmount}"
                    );
                }
            }
        }

        _heldTime.Value = 0f;
        _isCoolingDown.Value = true;
        _cooldownRemaining.Value = cooldownTime;
    }

    [ObserversRpc]
    private void PlayBurnEffectsRpc()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(burnSoundId, transform.position);
        }

        // You could also add particle effects here
    }

    // Method to force all clients to drop the item
    [ObserversRpc]
    private void ForceDrop()
    {
        // Find local pickup controller on each client and drop
        // SmoothPickUp pickupController = FindObjectOfType<PickUpItem>();
        // if (pickupController != null)
        // {
        //     pickupController.ForceDrop();
        // }
    }

    private void UpdateHeatVisuals(float intensity)
    {
        if (materials != null)
        {
            foreach (Material mat in materials)
            {
                if (mat != null)
                {
                    mat.color = Color.Lerp(normalColor, heatedColor, intensity);

                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", Color.red * intensity * 2f);
                    }
                }
            }
        }

        // Update particle effect intensity
        if (heatGlowEffect != null)
        {
            ParticleSystem ps = heatGlowEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var emission = ps.emission;
                emission.enabled = intensity > 0.2f;

                var main = ps.main;
                main.startColor = new ParticleSystem.MinMaxGradient(
                    Color.Lerp(Color.yellow, Color.red, intensity),
                    Color.red
                );

                main.startSize = Mathf.Lerp(0.1f, 0.3f, intensity);
            }
        }
    }
}
