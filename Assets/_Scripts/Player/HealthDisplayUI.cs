using FishNet.Object;
using TMPro;
using UnityEngine;

public class HealthDisplayUI : NetworkBehaviour
{
    [SerializeField]
    private TextMeshProUGUI healthText;

    private PlayerHealth playerHealth;

    public override void OnStartClient()
    {
        base.OnStartClient();

        BootstrapNetworkManager.OnLocalPlayerSpawned += OnLocalPlayerSpawned;
    }

    private void OnLocalPlayerSpawned(NetworkObject player)
    {
        playerHealth = player.GetComponent<PlayerHealth>();
        playerHealth.OnHealthChanged += UpdateHealthDisplay;
        UpdateHealthDisplay(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    void OnDestroy()
    {
        BootstrapNetworkManager.OnLocalPlayerSpawned -= OnLocalPlayerSpawned;
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateHealthDisplay;
    }

    private void UpdateHealthDisplay(float currentHealth, float maxHealth)
    {
        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
    }
}
