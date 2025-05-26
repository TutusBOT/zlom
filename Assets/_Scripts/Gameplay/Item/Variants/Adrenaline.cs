using FishNet.Object;
using UnityEngine;

public class Adrenaline : Item
{
    [SerializeField]
    private float duration = 15f;

    protected override void ExecuteItemAction()
    {
        if (player == null)
        {
            Debug.LogWarning("Adrenaline used but no player reference found!");
            return;
        }

        PlayerController playerController = player.GetPlayerController();
        UseAdrenaline(playerController);
    }

    [ServerRpc(RequireOwnership = false)]
    private void UseAdrenaline(PlayerController playerController)
    {
        if (playerController == null)
        {
            Debug.LogWarning(
                "Adrenaline used but no PlayerController component found on player object!"
            );
            return;
        }

        playerController.ActivateAdrenaline(duration);

        if (destroyAfterUse)
        {
            DespawnItem();
        }
    }
}
