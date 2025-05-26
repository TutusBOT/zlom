using UnityEngine;

public class RangeUpgrade : Item
{
    protected override void ExecuteItemAction()
    {
        if (player == null)
        {
            Debug.LogWarning("Player reference is not set. Cannot apply range upgrade.");
            return;
        }

        PlayerUpgrades playerUpgrades = player.GetComponent<PlayerUpgrades>();
        if (playerUpgrades == null)
        {
            Debug.LogWarning("PlayerUpgrades component not found on player");
            return;
        }

        playerUpgrades.ApplyUpgrade(UpgradeType.Range);
    }
}
