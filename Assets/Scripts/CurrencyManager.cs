using System;

public static class CurrencyManager
{
    public static event Action<CurrencyType, int> CurrencyChanged;

    public static int GetCount(CurrencyType currencyType)
    {
        if (currencyType == CurrencyType.Energy)
            return EnergyManager.GetCount();

        if (SpecialKeyManager.IsSpecialKey(currencyType))
            return SpecialKeyManager.GetCount(currencyType);

        return SaveManager.GetCurrencyData(currencyType).Count;
    }

    public static void Add(CurrencyType currencyType, int amount)
    {
        if (currencyType == CurrencyType.Energy)
        {
            EnergyManager.Add(amount);
            return;
        }

        if (SpecialKeyManager.IsSpecialKey(currencyType))
        {
            SpecialKeyManager.Add(currencyType, amount);
            return;
        }

        if (amount <= 0)
            return;

        SaveManager.CurrencySaveData currencyData = SaveManager.GetCurrencyData(currencyType);
        currencyData.Count += amount;
        SaveManager.Save();
        NotifyCurrencyChanged(currencyType, currencyData.Count);
    }

    public static bool Spend(CurrencyType currencyType, int amount)
    {
        if (currencyType == CurrencyType.Energy)
            return EnergyManager.Spend(amount);

        if (SpecialKeyManager.IsSpecialKey(currencyType))
            return SpecialKeyManager.Spend(currencyType, amount);

        if (amount <= 0)
            return true;

        SaveManager.CurrencySaveData currencyData = SaveManager.GetCurrencyData(currencyType);

        if (currencyData.Count < amount)
            return false;

        currencyData.Count -= amount;
        SaveManager.Save();
        NotifyCurrencyChanged(currencyType, currencyData.Count);
        return true;
    }

    internal static void NotifyCurrencyChanged(CurrencyType currencyType, int count)
    {
        CurrencyChanged?.Invoke(currencyType, count);
    }
}

public enum CurrencyType
{
    Soft = 0,
    Hard = 1,
    Energy = 2,
    SpecialKey = 3,
    SpecialKey2 = 4
}
