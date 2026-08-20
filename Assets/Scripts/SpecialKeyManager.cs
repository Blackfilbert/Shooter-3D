using System;
using UnityEngine;

public static class SpecialKeyManager
{
    public const int MaxKeys = 1;

    public static int GetCount()
    {
        return GetCount(CurrencyType.SpecialKey);
    }

    public static int GetCount(CurrencyType currencyType)
    {
        currencyType = GetKeyType(currencyType);
        EnsureKeyState(currencyType);
        return SaveManager.GetCurrencyData(GetKeyType(currencyType)).Count;
    }

    public static void Add(int amount)
    {
        Add(CurrencyType.SpecialKey, amount);
    }

    public static void Add(CurrencyType currencyType, int amount)
    {
        if (amount <= 0)
            return;

        currencyType = GetKeyType(currencyType);
        EnsureKeyState(currencyType);

        SaveManager.CurrencySaveData keyData = SaveManager.GetCurrencyData(currencyType);
        int count = Mathf.Clamp(keyData.Count + amount, 0, MaxKeys);

        if (keyData.Count == count)
            return;

        keyData.Count = count;
        SaveManager.Save();
        CurrencyManager.NotifyCurrencyChanged(currencyType, keyData.Count);
    }

    public static bool Spend(int amount)
    {
        return Spend(CurrencyType.SpecialKey, amount);
    }

    public static bool Spend(CurrencyType currencyType, int amount)
    {
        if (amount <= 0)
            return true;

        currencyType = GetKeyType(currencyType);
        EnsureKeyState(currencyType);

        SaveManager.CurrencySaveData keyData = SaveManager.GetCurrencyData(currencyType);

        if (keyData.Count < amount)
            return false;

        keyData.Count -= amount;
        SaveManager.Save();
        CurrencyManager.NotifyCurrencyChanged(currencyType, keyData.Count);
        return true;
    }

    public static bool HasKey()
    {
        return HasKey(CurrencyType.SpecialKey);
    }

    public static bool HasKey(CurrencyType currencyType)
    {
        return GetCount(currencyType) > 0;
    }

    public static bool TrySpend()
    {
        return TrySpend(CurrencyType.SpecialKey);
    }

    public static bool TrySpend(CurrencyType currencyType)
    {
        return Spend(currencyType, 1);
    }

    public static bool IsSpecialKey(CurrencyType currencyType)
    {
        return currencyType == CurrencyType.SpecialKey || currencyType == CurrencyType.SpecialKey2;
    }

    private static void EnsureKeyState()
    {
        EnsureKeyState(CurrencyType.SpecialKey);
    }

    private static void EnsureKeyState(CurrencyType currencyType)
    {
        SaveManager.SaveData data = SaveManager.Data;
        SaveManager.CurrencySaveData keyData = SaveManager.GetCurrencyData(currencyType);
        long todayTicks = DateTime.Now.Date.Ticks;

        keyData.Count = Mathf.Clamp(keyData.Count, 0, MaxKeys);

        if (GetRefreshDateTicks(data, currencyType) == todayTicks)
            return;

        SetRefreshDateTicks(data, currencyType, todayTicks);

        if (keyData.Count <= 0)
            keyData.Count = MaxKeys;

        SaveManager.Save();
        CurrencyManager.NotifyCurrencyChanged(currencyType, keyData.Count);
    }

    private static CurrencyType GetKeyType(CurrencyType currencyType)
    {
        return IsSpecialKey(currencyType) ? currencyType : CurrencyType.SpecialKey;
    }

    private static long GetRefreshDateTicks(SaveManager.SaveData data, CurrencyType currencyType)
    {
        return currencyType == CurrencyType.SpecialKey2
            ? data.SpecialKey2RefreshDateTicks
            : data.SpecialKeyRefreshDateTicks;
    }

    private static void SetRefreshDateTicks(SaveManager.SaveData data, CurrencyType currencyType, long ticks)
    {
        if (currencyType == CurrencyType.SpecialKey2)
            data.SpecialKey2RefreshDateTicks = ticks;
        else
            data.SpecialKeyRefreshDateTicks = ticks;
    }
}
