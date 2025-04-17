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

        var players = PlayerManager.Instance.GetAllPlayers();

        foreach (var player in players)
        {
            if (player.IsOwner)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
                playerHealth.OnHealthChanged += UpdateHealthDisplay;
                UpdateHealthDisplay(playerHealth.CurrentHealth, playerHealth.MaxHealth);
                break;
            }
        }
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateHealthDisplay;
    }

    private void UpdateHealthDisplay(float currentHealth, float maxHealth)
    {
        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
    }
}
