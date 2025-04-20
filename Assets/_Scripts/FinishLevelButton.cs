using FishNet.Managing.Scened;
using UnityEngine;

public class FinishLevelButton : MonoBehaviour
{
    private bool _isEnabled = false;

    private void Start()
    {
        if (PlayerMoneyManager.Instance != null)
        {
            PlayerMoneyManager.Instance.OnQuotaCompleted += OnQuotaCompleted;
        }
        else
        {
            Debug.LogWarning(
                "PlayerMoneyManager.Instance is null. Button won't update based on quota."
            );
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (PlayerMoneyManager.Instance != null)
        {
            PlayerMoneyManager.Instance.OnQuotaCompleted -= OnQuotaCompleted;
        }
    }

    public void OnClick()
    {
        if (!_isEnabled)
            return;
        SceneController.Instance.LoadScene("Shop");
    }

    private void OnQuotaCompleted()
    {
        _isEnabled = true;
    }
}
