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

    private float heldTime = 0f;
    private bool isCoolingDown = false;
    private float cooldownRemaining = 0f;
    private GameObject heatGlowEffect;
    private Material[] materials;
    private float heatIntensity = 0f;

    protected override void Start()
    {
        base.Start();

        size = ValuableSize.Medium;

        // Store all materials for color changing
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            materials = r.materials;
        }

        // Create heat glow effect
        if (curseEffectPrefab != null)
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
    }

    protected override void Update()
    {
        base.Update();
        if (isBeingHeld)
        {
            heldTime += Time.deltaTime;

            // Calculate heat percentage (0-1)
            heatIntensity = Mathf.Clamp01(heldTime / maxHoldTime);

            UpdateHeatVisuals(heatIntensity);

            // Play heating sound at intervals
            if (heldTime % 0.75f < 0.05f && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(
                    heatupSoundId,
                    transform.position,
                    heatIntensity * 0.5f
                );
            }

            if (heldTime >= maxHoldTime)
            {
                BurnPlayer();
            }
        }
        else if (isCoolingDown)
        {
            // Cool down over time
            cooldownRemaining -= Time.deltaTime;
            heatIntensity = Mathf.Clamp01(cooldownRemaining / cooldownTime);

            UpdateHeatVisuals(heatIntensity);

            if (cooldownRemaining <= 0)
            {
                isCoolingDown = false;
                heatIntensity = 0f;
                UpdateHeatVisuals(0f);
            }
        }
    }

    public override void OnPickedUp()
    {
        base.OnPickedUp();

        if (isCoolingDown)
        {
            heldTime = heatIntensity * maxHoldTime;
        }
        else
        {
            heldTime = 0f;
        }
    }

    public override void OnDropped()
    {
        base.OnDropped();

        isCoolingDown = true;
        cooldownRemaining = cooldownTime * heatIntensity;
    }

    private void BurnPlayer()
    {
        ObjectPickup pickupController = FindFirstObjectByType<ObjectPickup>();
        if (pickupController != null)
        {
            pickupController.ForceDrop();
        }

        // Play burn sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(burnSoundId, transform.position);
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // TODO: Implement player damage logic
            Debug.Log($"Player burned by BlisterheartRelic! Damage: {damageAmount}");
        }

        heldTime = 0f;
        isCoolingDown = true;
        cooldownRemaining = cooldownTime;
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
