using System.Collections.Generic;
using UnityEngine;

public class DamageZone : MonoBehaviour
{
    [Tooltip("Damage applied per second")]
    [SerializeField]
    private float damagePerSecond = 10f;

    [Tooltip("How often to apply damage")]
    [SerializeField]
    private float damageInterval = 0.5f;

    [Header("Visual")]
    [SerializeField]
    private Color zoneColor = new Color(1, 0, 0, 0.2f);

    private List<PlayerHealth> playersInZone = new List<PlayerHealth>();
    private float damageTimer;

    private void Start()
    {
        // Set zone visuals
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = zoneColor;
        }

        // Make sure we have a trigger collider
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning("DamageZone collider set to trigger mode");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health != null && !playersInZone.Contains(health))
        {
            playersInZone.Add(health);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health != null)
        {
            playersInZone.Remove(health);
        }
    }

    private void Update()
    {
        if (playersInZone.Count == 0)
            return;

        damageTimer -= Time.deltaTime;

        if (damageTimer <= 0f)
        {
            damageTimer = damageInterval;

            // Apply damage to all players in zone
            foreach (PlayerHealth health in playersInZone)
            {
                // Calculate damage for this interval
                float damage = damagePerSecond * damageInterval;

                // Apply damage (NetworkBehaviour handles server authority)
                health.TakeDamage(damage, gameObject);
            }
        }
    }
}
