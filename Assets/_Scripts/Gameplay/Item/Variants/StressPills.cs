using FishNet.Object;
using UnityEngine;

public class StressPills : Item
{
    [Header("Stress Reduction Settings")]
    [SerializeField]
    private float stressReductionAmount = 25f;

    protected override void ExecuteItemAction()
    {
        if (player == null)
        {
            Debug.LogWarning("StressPills used but no player reference found!");
            return;
        }

        StressController stressController = player.GetStressController();
        CalmPlayerServerRpc(stressController);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CalmPlayerServerRpc(StressController stressController)
    {
        if (stressController == null)
        {
            Debug.LogWarning(
                "StressPills used but no StressController component found on player object!"
            );
            return;
        }

        stressController.ReduceStress(stressReductionAmount);

        if (destroyAfterUse)
        {
            DespawnItem();
        }
    }
}
