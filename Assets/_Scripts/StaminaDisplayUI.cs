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

        BootstrapNetworkManager.OnLocalPlayerSpawned += OnLocalPlayerSpawned;
    }

    private void OnLocalPlayerSpawned(NetworkObject player)
    {
        playerController = player.GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("PlayerController not found on local player object.");
            return;
        }

        playerController.OnStaminaChanged += UpdateStaminaDisplay;
        UpdateStaminaDisplay(playerController.CurrentStamina, playerController.MaxStamina);
    }

    void OnDestroy()
    {
        BootstrapNetworkManager.OnLocalPlayerSpawned -= OnLocalPlayerSpawned;
        if (playerController != null)
            playerController.OnStaminaChanged -= UpdateStaminaDisplay;
    }

    private void UpdateStaminaDisplay(float currentStamina, float maxStamina)
    {
        if (staminaText != null)
        {
            staminaText.text = $"{Mathf.CeilToInt(currentStamina)}/{Mathf.CeilToInt(maxStamina)}";
        }
    }
}
