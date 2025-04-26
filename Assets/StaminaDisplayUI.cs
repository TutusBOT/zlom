using FishNet.Object;
using TMPro;
using UnityEngine;

public class StaminaDisplayUI : NetworkBehaviour
{
    [SerializeField]
    private TextMeshProUGUI staminaText; // Reference to the UI Text object

    private PlayerController playerController;

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Get the PlayerController for the local player
        var players = PlayerManager.Instance.GetAllPlayers();
        foreach (var player in players)
        {
            if (player.IsOwner)
            {
                playerController = player.GetComponent<PlayerController>();
                break;
            }
        }

        // Update stamina display initially
        UpdateStaminaDisplay(playerController.CurrentStamina, playerController.MaxStamina);
    }

    void Update()
    {
        // Only update for the local player
        if (playerController != null)
        {
            // Update the stamina text
            UpdateStaminaDisplay(playerController.CurrentStamina, playerController.MaxStamina);
        }
    }

    // Update the UI text displaying current and max stamina
    private void UpdateStaminaDisplay(float currentStamina, float maxStamina)
    {
        if (staminaText != null)
        {
            staminaText.text = $"{Mathf.CeilToInt(currentStamina)}/{Mathf.CeilToInt(maxStamina)}"; // Shows the current/max stamina
        }
    }
}
