using FishNet.Object;
using UnityEngine;
using UnityEngine.UI;

public class StressDisplayUI : NetworkBehaviour
{
    [SerializeField]
    private Slider stressBar;

    private StressController stressController;

    public override void OnStartClient()
    {
        base.OnStartClient();

        BootstrapNetworkManager.OnLocalPlayerSpawned += OnLocalPlayerSpawned;
    }

    private void OnLocalPlayerSpawned(NetworkObject player)
    {
        stressController = player.GetComponent<StressController>();
        if (stressController == null)
        {
            Debug.LogError("StressController not found on local player object.");
            return;
        }

        stressController.OnStressValueChanged += UpdateStressDisplay;
        UpdateStressDisplay(stressController.CurrentStress, stressController.MaxStress);
    }

    void OnDestroy()
    {
        BootstrapNetworkManager.OnLocalPlayerSpawned -= OnLocalPlayerSpawned;
        if (stressController != null)
            stressController.OnStressValueChanged -= UpdateStressDisplay;
    }

    private void UpdateStressDisplay(float currentStress, float maxStress)
    {
        if (stressBar != null)
        {
            stressBar.value = currentStress / maxStress;
        }
    }
}
