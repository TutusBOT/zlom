using UnityEngine;

public class CursedVase : Valuable
{
    [Header("Vase Properties")]
    [SerializeField]
    private string curseSoundId = "curse_release";

    [SerializeField]
    private GameObject curseEffectPrefab;
    private float curseRadius = 15f;
    private float stressAmount = 10f;

    public override void OnStartServer()
    {
        base.OnStartServer();

        size = ValuableSize.Large;
        breakThreshold = 0f;
        initialCashValue = 1000;
        minDamageMultiplier = 5f;
        maxDamageMultiplier = 10f;
    }

    protected override void Break()
    {
        base.Break();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(curseSoundId, transform.position);
        }

        if (curseEffectPrefab != null)
        {
            Instantiate(curseEffectPrefab, transform.position + Vector3.up, Quaternion.identity);
        }

        ApplyStressToNearbyPlayers();
    }

    private void ApplyStressToNearbyPlayers()
    {
        var nearbyPlayers = PlayerManager.Instance.GetPlayersInRange(
            transform.position,
            curseRadius
        );

        foreach (Player player in nearbyPlayers)
        {
            StressController stressController = player.GetComponent<StressController>();

            if (stressController != null)
            {
                stressController.AddStress(stressAmount);
                Debug.Log($"Applied {stressAmount} stress to player");
            }
            else
            {
                Debug.LogWarning(
                    $"Player {player.name} does not have a StressController component!"
                );
            }
        }
    }
}
