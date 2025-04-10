using UnityEngine;

public class CursedVase : Valuable
{
    [Header("Vase Properties")]
    [SerializeField]
    private string curseSoundId = "curse_release";

    [SerializeField]
    private GameObject curseEffectPrefab;

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

        Debug.Log("THE CURSE HAS BEEN RELEASED! Player stress would increase here.");
        // TODO: Implement curse effect on player - probably increase stress or spawn enemies
    }
}
