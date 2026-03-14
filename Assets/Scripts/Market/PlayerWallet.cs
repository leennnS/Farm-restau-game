using System;
using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    [SerializeField] private int startingMoney = 250;

    public int CurrentMoney { get; private set; }

    public event Action<int> OnMoneyChanged;

    private void Awake()
    {
        CurrentMoney = startingMoney;
        OnMoneyChanged?.Invoke(CurrentMoney);
    }

    public bool CanAfford(int amount)
    {
        return CurrentMoney >= amount;
    }

    public bool Spend(int amount)
    {
        if (amount < 0)
            return false;

        if (CurrentMoney < amount)
            return false;

        CurrentMoney -= amount;
        OnMoneyChanged?.Invoke(CurrentMoney);
        return true;
    }

    public void AddMoney(int amount)
    {
        if (amount < 0)
            return;

        CurrentMoney += amount;
        OnMoneyChanged?.Invoke(CurrentMoney);
    }

    public void SetMoney(int amount)
    {
        CurrentMoney = Mathf.Max(0, amount);
        OnMoneyChanged?.Invoke(CurrentMoney);
    }
}