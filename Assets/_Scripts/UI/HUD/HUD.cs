using FishNet.Object;
using UnityEngine;

public class HUD : NetworkBehaviour
{
    private static HUD _instance;
    public static HUD Instance => _instance;

    [SerializeField]
    private HealthDisplayUI healthDisplayUI;

    [SerializeField]
    private MoneyDisplay moneyDisplayUI;

    [SerializeField]
    private StressDisplayUI stressDisplayUI;

    [SerializeField]
    private StaminaDisplayUI staminaDisplayUI;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    public void ToggleStatsDisplay(bool isActive)
    {
        if (healthDisplayUI != null)
            healthDisplayUI.gameObject.SetActive(isActive);

        if (moneyDisplayUI != null)
            moneyDisplayUI.gameObject.SetActive(isActive);

        if (stressDisplayUI != null)
            stressDisplayUI.gameObject.SetActive(isActive);

        if (staminaDisplayUI != null)
            staminaDisplayUI.gameObject.SetActive(isActive);
    }
}
