using System;
using UnityEngine;

public static class EnergyManager
{
    public const int UnlockLevelNumber = 10;
    public const int MaxEnergy = 10;
    public const int RefillSeconds = 3600;

    private const int EnergyCostPerLevel = 1;

    public static bool IsUnlocked => IsUnlockedByCompletedLevel(SaveManager.CompletedLevelIndex);

    public static bool RequiresEnergy(int levelIndex)
    {
        return levelIndex + 1 > UnlockLevelNumber;
    }

    public static int GetCount()
    {
        EnsureEnergyState();
        return SaveManager.GetCurrencyData(CurrencyType.Energy).Count;
    }

    public static int GetSecondsUntilNextRefill()
    {
        EnsureEnergyState();

        SaveManager.CurrencySaveData energyData = SaveManager.GetCurrencyData(CurrencyType.Energy);

        if (energyData.Count >= MaxEnergy)
            return 0;

        long nowTicks = GetUtcNowTicks();
        long lastRefillTicks = SaveManager.Data.EnergyLastRefillTicks > 0 ? SaveManager.Data.EnergyLastRefillTicks : nowTicks;
        long refillTicks = TimeSpan.FromSeconds(RefillSeconds).Ticks;
        long elapsedTicks = Math.Max(0, nowTicks - lastRefillTicks);
        long remainingTicks = refillTicks - elapsedTicks % refillTicks;
        return Math.Max(0, (int)Math.Ceiling((double)remainingTicks / TimeSpan.TicksPerSecond));
    }

    public static void Add(int amount)
    {
        if (amount <= 0)
            return;

        EnsureEnergyState();

        SaveManager.CurrencySaveData energyData = SaveManager.GetCurrencyData(CurrencyType.Energy);
        int count = Mathf.Clamp(energyData.Count + amount, 0, MaxEnergy);

        if (energyData.Count == count)
            return;

        energyData.Count = count;
        SaveManager.Data.EnergyLastRefillTicks = GetUtcNowTicks();
        SaveManager.Save();
        CurrencyManager.NotifyCurrencyChanged(CurrencyType.Energy, energyData.Count);
    }

    public static bool Spend(int amount)
    {
        if (amount <= 0)
            return true;

        EnsureEnergyState();

        SaveManager.CurrencySaveData energyData = SaveManager.GetCurrencyData(CurrencyType.Energy);

        if (energyData.Count < amount)
            return false;

        energyData.Count -= amount;

        SaveManager.Save();
        CurrencyManager.NotifyCurrencyChanged(CurrencyType.Energy, energyData.Count);
        return true;
    }

    public static bool TrySpendForLevel(int levelIndex)
    {
        if (RequiresEnergy(levelIndex) == false)
            return true;

        return Spend(EnergyCostPerLevel);
    }

    public static bool HasEnergyForLevel(int levelIndex)
    {
        return RequiresEnergy(levelIndex) == false || GetCount() >= EnergyCostPerLevel;
    }

    private static void EnsureEnergyState()
    {
        SaveManager.SaveData data = SaveManager.Data;
        SaveManager.CurrencySaveData energyData = SaveManager.GetCurrencyData(CurrencyType.Energy);

        if (IsUnlockedByCompletedLevel(data.CompletedLevelIndex) == false)
        {
            if (energyData.Count != 0)
            {
                energyData.Count = 0;
                SaveManager.Save();
                CurrencyManager.NotifyCurrencyChanged(CurrencyType.Energy, energyData.Count);
            }

            return;
        }

        if (data.EnergyUnlocked == false)
        {
            data.EnergyUnlocked = true;
            data.EnergyLastRefillTicks = GetUtcNowTicks();
            energyData.Count = MaxEnergy;
            SaveManager.Save();
            CurrencyManager.NotifyCurrencyChanged(CurrencyType.Energy, energyData.Count);
            return;
        }

        Regenerate(data, energyData);
    }

    private static void Regenerate(SaveManager.SaveData data, SaveManager.CurrencySaveData energyData)
    {
        energyData.Count = Mathf.Clamp(energyData.Count, 0, MaxEnergy);

        if (energyData.Count >= MaxEnergy)
            return;

        long nowTicks = GetUtcNowTicks();
        long lastRefillTicks = data.EnergyLastRefillTicks > 0 ? data.EnergyLastRefillTicks : nowTicks;
        long elapsedTicks = Math.Max(0, nowTicks - lastRefillTicks);
        long refillTicks = TimeSpan.FromSeconds(RefillSeconds).Ticks;
        int regenerated = (int)(elapsedTicks / refillTicks);

        if (regenerated <= 0)
            return;

        energyData.Count = Mathf.Min(MaxEnergy, energyData.Count + regenerated);
        data.EnergyLastRefillTicks = energyData.Count >= MaxEnergy ? nowTicks : lastRefillTicks + regenerated * refillTicks;
        SaveManager.Save();
        CurrencyManager.NotifyCurrencyChanged(CurrencyType.Energy, energyData.Count);
    }

    private static bool IsUnlockedByCompletedLevel(int completedLevelIndex)
    {
        return completedLevelIndex + 1 >= UnlockLevelNumber;
    }

    private static long GetUtcNowTicks()
    {
        return DateTime.UtcNow.Ticks;
    }
}
