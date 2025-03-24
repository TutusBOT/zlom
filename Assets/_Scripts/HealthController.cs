using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class HealthController : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    public UnityEvent OnDeath; // Event triggered on death
    public UnityEvent<float> OnHealthChanged;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        // Trigger the health change event
        OnHealthChanged?.Invoke(currentHealth);
        Debug.Log(currentHealth);

        // Optionally, handle death or low health effects
        if (currentHealth <= 0f)
        {
            Die();
            Debug.Log("Player died!");
        }
    }

    private void Die()
    {
        isDead = true;
        OnDeath?.Invoke(); // Trigger death event

        // Disable movement if PlayerController exists
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.canMove = false;
        }

        StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(3); // Delay before respawn

        transform.position = Vector3.zero; // Respawn position (change if needed)
        currentHealth = maxHealth;
        isDead = false;

        // Re-enable movement if PlayerController exists
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.canMove = true;
        }
    }

    public bool IsAlive()
    {
        return !isDead;
    }
}
