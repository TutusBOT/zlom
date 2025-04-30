using FishNet;
using FishNet.Object;
using UnityEngine;

public class Soulstone : Valuable
{
    [Header("Soulstone Properties")]
    [SerializeField]
    private float maxSeparationDistance = 3f;

    [SerializeField]
    private float warningDistance = 2f;

    [SerializeField]
    private string explosionSoundId = "stone_explosion";

    [SerializeField]
    private GameObject soulstonePrefab;

    [Header("Visual Settings")]
    [SerializeField]
    private Color normalColor = new Color(0.8f, 0.2f, 0.2f, 0.5f);

    [SerializeField]
    private Color warningColor = new Color(1f, 0.1f, 0.1f, 1f);

    [SerializeField]
    private GameObject explosionEffectPrefab;

    private Soulstone pairedStone;
    private bool isFirstStone = true;
    private bool hasSpawnedPartner = false;
    private bool isExploding = false;
    private Material material;
    private Light stoneLight;
    private float pulseTime = 0f;

    private void Awake()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            material = renderer.material;

        stoneLight = GetComponentInChildren<Light>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        size = ValuableSize.Small;

        Debug.Log($"Soulstone spawned: {gameObject.name}");
        if (isFirstStone && !hasSpawnedPartner && soulstonePrefab != null)
        {
            Debug.Log("Spawning partner stone...");
            SpawnPartner();
        }

        SetNormalAppearance();

        InvokeRepeating("CheckDistance", 1f, 0.5f);
    }

    protected override void Update()
    {
        base.Update();
        if (pairedStone != null && !isExploding)
        {
            UpdateVisuals();
        }
    }

    private void SpawnPartner()
    {
        hasSpawnedPartner = true;

        // Find a position slightly offset from this stone
        Vector3 offset = Random.insideUnitSphere * 0.3f;
        offset.y = 0.1f;
        Vector3 spawnPos = transform.position + offset;
        Debug.Log($"Spawning partner at {spawnPos}");

        GameObject partnerObj = Instantiate(soulstonePrefab, spawnPos, Quaternion.identity);
        Soulstone partner = partnerObj.GetComponent<Soulstone>();

        if (partner != null)
        {
            Debug.Log($"Partner spawned: {partner}");
            partner.isFirstStone = false;

            partner.hasSpawnedPartner = true;

            partner.pairedStone = this;
            pairedStone = partner;
        }

        NetworkObject networkObject = partnerObj.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            Debug.Log($"Spawning partner network object {networkObject}");
            InstanceFinder.ServerManager.Spawn(networkObject);
        }
    }

    private void CheckDistance()
    {
        if (pairedStone == null)
        {
            if (isFirstStone)
            {
                Debug.Log("Soulstone has no partner!");
                Explode();
            }
            return;
        }

        float distance = Vector3.Distance(transform.position, pairedStone.transform.position);

        if (distance > maxSeparationDistance)
        {
            Debug.Log("Soulstone too far apart!");
            Explode();
        }
    }

    private void UpdateVisuals()
    {
        float distance = Vector3.Distance(transform.position, pairedStone.transform.position);

        if (distance > warningDistance)
        {
            float dangerLevel = Mathf.InverseLerp(warningDistance, maxSeparationDistance, distance);

            // Make them pulse faster as they get farther apart
            pulseTime += Time.deltaTime * Mathf.Lerp(1f, 5f, dangerLevel);
            float pulseFactor = (Mathf.Sin(pulseTime * Mathf.PI * 2) + 1) * 0.5f;

            UpdatePulse(pulseFactor);
        }
        else
        {
            SetNormalAppearance();
        }
    }

    private void UpdatePulse(float pulseFactor)
    {
        if (material != null)
        {
            material.color = Color.Lerp(normalColor, warningColor, pulseFactor);

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", warningColor * pulseFactor * 2f);
            }
        }

        if (stoneLight != null)
        {
            stoneLight.color = warningColor;
            stoneLight.intensity = Mathf.Lerp(0.5f, 2.5f, pulseFactor);
        }
    }

    private void SetNormalAppearance()
    {
        if (material != null)
        {
            material.color = normalColor;

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", normalColor * 0.5f);
            }
        }

        if (stoneLight != null)
        {
            stoneLight.color = normalColor;
            stoneLight.intensity = 0.5f;
        }
    }

    public void Explode()
    {
        if (isExploding)
            return;

        isExploding = true;

        if (pairedStone != null && !pairedStone.isExploding)
        {
            pairedStone.Explode();
        }

        if (ExplosionManager.Instance != null)
        {
            ExplosionManager.Instance.CreateExplosion("soulstone_explosion", transform.position);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(explosionSoundId, transform.position, 1f);
        }

        Break();
    }

    protected override void Break()
    {
        if (pairedStone != null && !isExploding)
        {
            Explode();
        }

        base.Break();
    }
}
