using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Handles the display of player money and quota information
/// </summary>
public class MoneyDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private TextMeshProUGUI moneyText;

    [SerializeField]
    private TextMeshProUGUI quotaText;

    [SerializeField]
    private GameObject quotaCompletedNotification;

    [Header("Display Settings")]
    [SerializeField]
    private float notificationDuration = 3f;

    private void Start()
    {
        if (PlayerMoneyManager.Instance == null)
        {
            Debug.LogWarning("MoneyDisplay: No PlayerMoneyManager instance found!");
            return;
        }

        UpdateMoneyDisplay(PlayerMoneyManager.Instance.GetCurrentMoney());
        UpdateQuotaDisplay(
            PlayerMoneyManager.Instance.GetCurrentMoney(),
            PlayerMoneyManager.Instance.GetCurrentQuota()
        );

        PlayerMoneyManager.Instance.OnMoneyChanged += UpdateMoneyDisplay;
        PlayerMoneyManager.Instance.OnQuotaChanged += UpdateQuotaDisplay;
        PlayerMoneyManager.Instance.OnQuotaCompleted += ShowQuotaCompletedNotification;
    }

    private void OnDestroy()
    {
        if (PlayerMoneyManager.Instance != null)
        {
            PlayerMoneyManager.Instance.OnMoneyChanged -= UpdateMoneyDisplay;
            PlayerMoneyManager.Instance.OnQuotaChanged -= UpdateQuotaDisplay;
            PlayerMoneyManager.Instance.OnQuotaCompleted -= ShowQuotaCompletedNotification;
        }
    }

    private void UpdateMoneyDisplay(int currentMoney)
    {
        if (moneyText != null)
        {
            moneyText.text = $"${currentMoney}";
        }
    }

    private void UpdateQuotaDisplay(int currentMoney, int targetQuota)
    {
        if (quotaText != null)
        {
            quotaText.text = $"/${targetQuota}";
        }
    }

    private void ShowQuotaCompletedNotification()
    {
        if (quotaCompletedNotification != null)
        {
            quotaCompletedNotification.SetActive(true);
            StartCoroutine(HideNotificationAfterDelay());
        }
    }

    private IEnumerator HideNotificationAfterDelay()
    {
        yield return new WaitForSeconds(notificationDuration);

        if (quotaCompletedNotification != null)
        {
            quotaCompletedNotification.SetActive(false);
        }
    }
}
