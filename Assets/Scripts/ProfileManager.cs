using System;

public static class ProfileManager
{
    public const int DamageUpgradeImpactSteps = 2;

    private const int DefaultLevelExperience = 5;
    private const int DefaultGearUpgradeExperience = 10;
    private const int AimStabilityPerLevel = 20;

    public static event Action ProfileChanged;
    public static event Action<int> LevelUp;

    private static int _pendingLevelUpLevel;
    private static GeneralParameters _generalParameters;

    public static int Level => SaveManager.GetProfileData().Level;
    public static int Experience => SaveManager.GetProfileData().Experience;
    public static int RequiredExperience => GetRequiredExperience(Level);
    public static int RareBoosters => SaveManager.GetProfileData().RareBoosters;
    public static int AimStabilityBonus => Level * AimStabilityPerLevel;
    public static bool HasPendingLevelUp => _pendingLevelUpLevel > 0;

    public static void SetGeneralParameters(GeneralParameters generalParameters)
    {
        if (generalParameters != null)
            _generalParameters = generalParameters;
    }

    public static void AddVictoryExperience()
    {
        AddExperience(GetLevelExperience());
    }

    public static void AddGearUpgradeExperience()
    {
        AddExperience(GetGearUpgradeExperience());
    }

    public static void AddExperience(int amount)
    {
        if (amount <= 0)
            return;

        SaveManager.ProfileSaveData profileData = SaveManager.GetProfileData();
        profileData.Experience += amount;

        while (profileData.Experience >= GetRequiredExperience(profileData.Level))
        {
            profileData.Experience -= GetRequiredExperience(profileData.Level);
            profileData.Level++;
            profileData.RareBoosters++;
            _pendingLevelUpLevel = profileData.Level;
            LevelUp?.Invoke(profileData.Level);
        }

        SaveManager.Save();
        ProfileChanged?.Invoke();
    }

    public static bool SpendRareBooster(int amount = 1)
    {
        if (amount <= 0)
            return true;

        SaveManager.ProfileSaveData profileData = SaveManager.GetProfileData();

        if (profileData.RareBoosters < amount)
            return false;

        profileData.RareBoosters -= amount;
        SaveManager.Save();
        ProfileChanged?.Invoke();
        return true;
    }

    public static int GetRequiredExperience(int level)
    {
        return Math.Max(1, 54 * Math.Max(1, level) - 24);
    }

    private static int GetLevelExperience()
    {
        return _generalParameters != null ? _generalParameters.LevelExperience : DefaultLevelExperience;
    }

    private static int GetGearUpgradeExperience()
    {
        return _generalParameters != null ? _generalParameters.GearUpgradeExperience : DefaultGearUpgradeExperience;
    }

    public static bool TryConsumePendingLevelUp(out int level)
    {
        level = _pendingLevelUpLevel;

        if (_pendingLevelUpLevel <= 0)
            return false;

        _pendingLevelUpLevel = 0;
        return true;
    }

    public static int ConsumeDamageBalanceOffset(int currentDamage)
    {
        SaveManager.ProfileSaveData profileData = SaveManager.GetProfileData();
        currentDamage = Math.Max(1, currentDamage);

        if (profileData.LastBalancedDamage <= 0)
        {
            profileData.LastBalancedDamage = currentDamage;
            SaveManager.Save();
            return 0;
        }

        if (currentDamage > profileData.LastBalancedDamage)
        {
            profileData.DamageUpgradeImpactDelta = currentDamage - profileData.LastBalancedDamage;
            profileData.DamageUpgradeImpactStep = 0;
            profileData.LastBalancedDamage = currentDamage;
        }

        int balancedDamage = profileData.LastBalancedDamage;

        if (profileData.DamageUpgradeImpactDelta > 0 && profileData.DamageUpgradeImpactStep < DamageUpgradeImpactSteps)
        {
            profileData.DamageUpgradeImpactStep++;
            float impact = (float)profileData.DamageUpgradeImpactStep / (DamageUpgradeImpactSteps + 1);
            balancedDamage -= Math.Max(0, (int)Math.Round(profileData.DamageUpgradeImpactDelta * (1f - impact)));
        }

        if (profileData.DamageUpgradeImpactStep >= DamageUpgradeImpactSteps)
            profileData.DamageUpgradeImpactDelta = 0;

        SaveManager.Save();
        return balancedDamage - currentDamage;
    }
}
