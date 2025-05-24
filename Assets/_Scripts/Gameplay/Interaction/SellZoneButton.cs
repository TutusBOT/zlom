using UnityEngine;

public class SellZoneButton : HoldButton
{
    public void SellValuables()
    {
        int currentQuota = EconomyManager.Instance.GetCurrentQuota();
        int moneyInZone = SellZone.Instance.GetTotalValueInZone();
        if (moneyInZone < currentQuota)
            {
                ResetButtonServerRpc();
                Debug.Log("Nie masz piendziedzy");
                return;
            }
        SellZone.Instance.TrySellItems();
    }
}
