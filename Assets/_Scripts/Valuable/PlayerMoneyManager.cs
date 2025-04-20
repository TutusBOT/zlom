using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class PlayerMoneyManager : NetworkBehaviour
{
    public static PlayerMoneyManager Instance { get; private set; }

    [Header("Money Settings")]
    private readonly SyncVar<int> _currentMoney = new SyncVar<int>();

    [Header("Quota Settings")]
    private readonly SyncVar<int> _currentQuota = new SyncVar<int>();

    [SerializeField]
    private int initialQuota = 1000;

    [SerializeField]
    private int quotaIncreaseAmount = 500;

    public delegate void MoneyChangedHandler(int newAmount);
    public event MoneyChangedHandler OnMoneyChanged;

    public delegate void QuotaChangedHandler(int current, int target);
    public event QuotaChangedHandler OnQuotaChanged;

    public delegate void QuotaCompletedHandler();
    public event QuotaCompletedHandler OnQuotaCompleted;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        _currentMoney.OnChange += OnMoneyValueChanged;
        _currentQuota.OnChange += OnQuotaValueChanged;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        _currentQuota.Value = initialQuota;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        _currentMoney.OnChange -= OnMoneyValueChanged;
        _currentQuota.OnChange -= OnQuotaValueChanged;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            AddMoneyServerRpc(100);
            Debug.Log("Added test money!");
        }
    }

    private void OnMoneyValueChanged(int oldValue, int newValue, bool asServer)
    {
        OnMoneyChanged?.Invoke(newValue);

        if (newValue >= _currentQuota.Value && IsServerInitialized)
        {
            CompleteQuota();
        }
    }

    private void OnQuotaValueChanged(int oldValue, int newValue, bool asServer)
    {
        OnQuotaChanged?.Invoke(_currentMoney.Value, newValue);
    }

    public int GetCurrentMoney() => _currentMoney.Value;

    public int GetCurrentQuota() => _currentQuota.Value;

    [ServerRpc(RequireOwnership = false)]
    public void AddMoneyServerRpc(int amount)
    {
        if (amount > 0)
        {
            _currentMoney.Value += amount;
            Debug.Log($"Added ${amount}. Current money: ${_currentMoney.Value}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpendMoneyServerRpc(int amount)
    {
        if (amount > 0 && _currentMoney.Value >= amount)
        {
            _currentMoney.Value -= amount;
        }
    }

    private void CompleteQuota()
    {
        QuotaCompletedClientRpc();

        _currentQuota.Value += quotaIncreaseAmount;
    }

    [ObserversRpc]
    private void QuotaCompletedClientRpc()
    {
        OnQuotaCompleted?.Invoke();
    }
}
