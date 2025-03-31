using UnityEngine;
using TMPro; // For TextMeshPro UI

public class PlayerMoneyManager : MonoBehaviour
{
    public static PlayerMoneyManager Instance { get; private set; }
    
    [Header("Settings")]
    [SerializeField] private int currentMoney = 0;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI moneyText;
    
    // Event for other systems to subscribe to
    public delegate void MoneyChangedHandler(int newAmount);
    public event MoneyChangedHandler OnMoneyChanged;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Initialize UI
        UpdateMoneyDisplay();
    }

    void Update()
{
    if (Input.GetKeyDown(KeyCode.M))
    {
        AddMoney(100);
        Debug.Log("Added test money!");
    }
}
    
    public int GetCurrentMoney()
    {
        return currentMoney;
    }
    
    public void AddMoney(int amount)
    {
        if (amount > 0)
        {
            currentMoney += amount;
            OnMoneyChanged?.Invoke(currentMoney);
            UpdateMoneyDisplay();
            
            // Optional: Money gain particle/sound effect
            Debug.Log($"Added ${amount}. Current money: ${currentMoney}");
        }
    }
    
    public bool SpendMoney(int amount)
    {
        if (amount > 0 && currentMoney >= amount)
        {
            currentMoney -= amount;
            OnMoneyChanged?.Invoke(currentMoney);
            UpdateMoneyDisplay();
            return true;
        }
        return false;
    }
    
    private void UpdateMoneyDisplay()
    {
        if (moneyText != null)
        {
            moneyText.text = $"${currentMoney}";
        }
    }
}