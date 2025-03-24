using UnityEngine;

public class DamageZone : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damageAmount = 20f; // Set damage in Inspector
    [SerializeField] private float damageCooldown = 1f; // Time between damage ticks

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Triggered");
        if (other.CompareTag("Player")) // Ensure the player has the correct tag
        {
            HealthController health = other.GetComponent<HealthController>();
            if (health != null)
            {
                health.TakeDamage(damageAmount);
                InvokeRepeating(nameof(ApplyDamage), damageCooldown, damageCooldown);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CancelInvoke(nameof(ApplyDamage)); // Stop applying damage when the player leaves
        }
    }

    private void ApplyDamage()
    {
        // Find player and apply damage again
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            HealthController health = player.GetComponent<HealthController>();
            if (health != null)
            {
                health.TakeDamage(damageAmount);
            }
        }
    }
}
