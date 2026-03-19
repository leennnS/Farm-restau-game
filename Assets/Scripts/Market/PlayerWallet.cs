using System;
using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    [SerializeField] private int startingMoney = 250;

    public int CurrentMoney => MoneyManager.Instance.CurrentMoney;

    public event Action<int> OnMoneyChanged;

    private void Awake()
    {
        // Migrate old inspector value only if there is no saved money yet.
        if (!PlayerPrefs.HasKey("GlobalMoney"))
            MoneyManager.Instance.SetMoney(startingMoney);

        MoneyManager.Instance.OnMoneyChanged += HandleGlobalMoneyChanged;
        OnMoneyChanged?.Invoke(CurrentMoney);
    }

    private void OnDestroy()
    {
        if (MoneyManager.HasInstance)
            MoneyManager.Instance.OnMoneyChanged -= HandleGlobalMoneyChanged;
    }

    private void HandleGlobalMoneyChanged(int newAmount)
    {
        OnMoneyChanged?.Invoke(newAmount);
    }

    public bool CanAfford(int amount)
    {
        return MoneyManager.Instance.CanAfford(amount);
    }

    public bool Spend(int amount)
    {
        return MoneyManager.Instance.SpendMoney(amount);
    }

    public void AddMoney(int amount)
    {
        MoneyManager.Instance.AddMoney(amount);
    }

    public void SetMoney(int amount)
    {
        MoneyManager.Instance.SetMoney(amount);
    }
}