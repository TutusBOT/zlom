using UnityEngine;

public class FinishLevelButton : HoldButton
{
    private bool _isEnabled = false;

    protected override void Start()
    {
        base.Start();
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnQuotaCompleted += OnQuotaCompleted;
        }
        else
        {
            Debug.LogWarning(
                "EconomyManager.Instance is null. Button won't update based on quota."
            );
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnQuotaCompleted -= OnQuotaCompleted;
        }
    }

    public void OnClick()
    {
        if (!_isEnabled)
        {
            ResetButtonServerRpc();
            return;
        }
        BootstrapNetworkManager.ChangeNetworkScene("Train", new string[] { "Dungeon3D" });
    }

    private void OnQuotaCompleted()
    {
        _isEnabled = true;
    }
}

