using FishNet.Object;
using TMPro;
using UnityEngine;

public class StaminaDisplayUI : NetworkBehaviour
{
    [SerializeField]
    private TextMeshProUGUI staminaText;

    private PlayerController playerController;

    public override void OnStartClient()
    {
        base.OnStartClient();

        var players = PlayerManager.Instance.GetAllPlayers();
        foreach (var player in players)
        {
            if (player.IsOwner)
            {
                playerController = player.GetComponent<PlayerController>();
                break;
            }
        }

        UpdateStaminaDisplay(playerController.CurrentStamina, playerController.MaxStamina);
    }

    void Update()
    {
        if (playerController != null)
        {
            UpdateStaminaDisplay(playerController.CurrentStamina, playerController.MaxStamina);
        }
    }

    private void UpdateStaminaDisplay(float currentStamina, float maxStamina)
    {
        if (staminaText != null)
        {
            staminaText.text = $"{Mathf.CeilToInt(currentStamina)}/{Mathf.CeilToInt(maxStamina)}";
        }
    }
}
