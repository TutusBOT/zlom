using UnityEngine;

public class FinishLevelButton : MonoBehaviour
{
    private bool _isEnabled = false;

    private void Start()
    {
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
            return;
        SceneController.Instance.LoadScene("Train");
    }

    private void OnQuotaCompleted()
    {
        _isEnabled = true;
    }
}
