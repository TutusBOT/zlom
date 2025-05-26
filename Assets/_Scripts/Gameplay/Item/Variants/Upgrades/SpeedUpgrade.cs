using UnityEngine;

public class SpeedUpgrade : Item
{
    protected override void ExecuteItemAction()
    {
        if (player == null)
        {
            Debug.LogWarning("Player reference is not set. Cannot apply speed upgrade.");
            return;
        }

        PlayerUpgrades playerUpgrades = player.GetComponent<PlayerUpgrades>();
        if (playerUpgrades == null)
        {
            Debug.LogWarning("PlayerUpgrades component not found on player");
            return;
        }

        playerUpgrades.ApplyUpgrade(UpgradeType.Speed);
    }
}
