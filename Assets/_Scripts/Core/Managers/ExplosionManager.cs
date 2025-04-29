using System.Collections.Generic;
using UnityEngine;

public class ExplosionManager : MonoBehaviour
{
    public static ExplosionManager Instance { get; private set; }

    [System.Serializable]
    public class ExplosionConfiguration
    {
        public string id;
        public float force = 500f;
        public float radius = 3f;
        public float upwardModifier = 1f;
        public float damage = 20f;
        public GameObject visualEffectPrefab;
        public string soundId;
        public float cameraShakeIntensity = 0.3f;
        public float cameraShakeDuration = 0.2f;
        public bool affectValuables = true;
        public bool affectPlayer = true;
    }

    [Header("Explosion Configurations")]
    [SerializeField]
    private List<ExplosionConfiguration> explosionConfigs = new List<ExplosionConfiguration>();

    [SerializeField]
    private LayerMask defaultAffectedLayers;

    private Dictionary<string, ExplosionConfiguration> configLookup =
        new Dictionary<string, ExplosionConfiguration>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            foreach (ExplosionConfiguration config in explosionConfigs)
            {
                configLookup[config.id] = config;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void CreateExplosion(string configId, Vector3 position, LayerMask? affectedLayers = null)
    {
        Debug.Log($"Creating explosion with config '{configId}' at {position}");
        if (!configLookup.TryGetValue(configId, out ExplosionConfiguration config))
        {
            Debug.LogWarning($"Explosion configuration '{configId}' not found!");
            return;
        }

        LayerMask layers = affectedLayers ?? defaultAffectedLayers;

        // Visual effect
        if (config.visualEffectPrefab != null)
        {
            Instantiate(config.visualEffectPrefab, position, Quaternion.identity);
        }

        // Sound effect
        if (!string.IsNullOrEmpty(config.soundId) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(config.soundId, position);
        }

        ApplyExplosionForceAndDamage(position, config, layers);

        // TODO: Camera shake
        // if (config.cameraShakeDuration > 0 && config.cameraShakeIntensity > 0){}
    }

    private void ApplyExplosionForceAndDamage(
        Vector3 position,
        ExplosionConfiguration config,
        LayerMask layers
    )
    {
        // Find all affected objects
        Collider[] hitColliders = Physics.OverlapSphere(position, config.radius, layers);

        foreach (Collider hit in hitColliders)
        {
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(config.force, position, config.radius, config.upwardModifier);
            }

            // Get distance for damage falloff
            float distance = Vector3.Distance(position, hit.transform.position);
            float damagePercent = 1f - (distance / config.radius);
            float actualDamage = config.damage * damagePercent;

            if (actualDamage <= 0)
                continue;

            // TODO: Apply damage to player
            if (config.affectPlayer && hit.CompareTag("Player"))
            {
                Debug.Log($"Player hit by explosion! Damage: {actualDamage}");
            }

            // Apply damage to valuables
            if (config.affectValuables)
            {
                Valuable valuable = hit.GetComponent<Valuable>();
                if (valuable != null)
                {
                    // If you have damage methods on valuables:
                    // valuable.TakeDamage(actualDamage);

                    Debug.Log($"Valuable {valuable.name} damaged by {actualDamage}");
                }
            }
        }
    }

    public void CreateSimpleExplosion(
        Vector3 position,
        float force,
        float radius,
        GameObject visualEffect = null,
        string soundId = null
    )
    {
        if (visualEffect != null)
        {
            Instantiate(visualEffect, position, Quaternion.identity);
        }

        if (!string.IsNullOrEmpty(soundId) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(soundId, position);
        }

        // Apply physics only
        Collider[] hitColliders = Physics.OverlapSphere(position, radius, defaultAffectedLayers);
        foreach (Collider hit in hitColliders)
        {
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(force, position, radius, 1f);
            }
        }
    }
}
