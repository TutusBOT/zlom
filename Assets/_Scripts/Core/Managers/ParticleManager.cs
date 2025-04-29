using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }

    [System.Serializable]
    public class ParticleEntry
    {
        public string id;
        public GameObject particlePrefab;
    }

    public List<ParticleEntry> particleEffects = new List<ParticleEntry>();
    private Dictionary<string, GameObject> effectsMap = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        foreach (var effect in particleEffects)
        {
            effectsMap[effect.id] = effect.particlePrefab;
        }
    }

    public void PlayEffect(string effectId, Vector3 position, float scale = 1f)
    {
        if (!effectsMap.TryGetValue(effectId, out GameObject prefab))
        {
            Debug.LogWarning($"Particle effect '{effectId}' not found!");
            return;
        }

        GameObject effect = Instantiate(prefab, position, Quaternion.identity);

        ParticleSystem ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startSizeMultiplier *= scale;
        }

        float lifetime = ps != null ? ps.main.duration + ps.main.startLifetimeMultiplier : 2f;
        Destroy(effect, lifetime);
    }
}
