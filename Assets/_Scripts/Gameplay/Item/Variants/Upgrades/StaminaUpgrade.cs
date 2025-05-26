using UnityEngine;

public class StaminaUpgrade : Item
{
    protected override void ExecuteItemAction()
    {
        if (player == null)
        {
            Debug.LogWarning("Player reference is not set. Cannot apply stamina upgrade.");
            return;
        }

        PlayerUpgrades playerUpgrades = player.GetComponent<PlayerUpgrades>();
        if (playerUpgrades == null)
        {
            Debug.LogWarning("PlayerUpgrades component not found on player");
            return;
        }

        playerUpgrades.ApplyUpgrade(UpgradeType.Stamina);
    }
}
