using System;
using UnityEngine;

public static class SaveManager
{
    private const string SaveKey = "ShooterSave";

    private static SaveData _data;
    private static bool _isLoaded;

    public static SaveData Data
    {
        get
        {
            Load();
            return _data;
        }
    }

    public static int SelectedLevelIndex => Data.SelectedLevelIndex;
    public static int CompletedLevelIndex => Data.CompletedLevelIndex;
    public static bool HasSelectedLevel => Data.HasSelectedLevel;
    public static long TotalPlaytimeSeconds => Data.TotalPlaytimeSeconds;
    public static int SpecialLevelIndex => Data.SpecialLevelIndex;
    public static bool HasLevelCompletionActiveObjectsCount => Data.LevelCompletionActiveObjectsCount >= 0;
    public static int LevelCompletionActiveObjectsCount => Data.LevelCompletionActiveObjectsCount;
    public static int LevelCompletionObjectLevelIndex => Data.LevelCompletionObjectLevelIndex;
    public static bool InventoryEquipTutorialCompleted => Data.InventoryEquipTutorialCompleted;

    public static void Load()
    {
        if (_isLoaded)
            return;

        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        _data = string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json);

        if (_data == null)
            _data = new SaveData();

        EnsureCurrencyData();
        EnsureInventoryData();
        EnsureGearPackData();
        EnsureProfileData();
        _isLoaded = true;
    }

    public static void Save()
    {
        Load();

        string json = JsonUtility.ToJson(_data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    public static void SetSelectedLevel(int levelIndex)
    {
        Load();

        _data.SelectedLevelIndex = Mathf.Max(0, levelIndex);
        _data.HasSelectedLevel = true;
        Save();
    }

    public static void CompleteLevel(int levelIndex)
    {
        Load();

        _data.CompletedLevelIndex = Mathf.Max(_data.CompletedLevelIndex, levelIndex);
        Save();
    }

    public static int AdvanceSpecialLevelIndex(int levelsCount)
    {
        Load();

        if (levelsCount <= 0)
            return _data.SpecialLevelIndex;

        _data.SpecialLevelIndex = (_data.SpecialLevelIndex + 1) % levelsCount;
        Save();
        return _data.SpecialLevelIndex;
    }

    public static long AddPlaytimeSeconds(float seconds)
    {
        Load();

        int roundedSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));

        if (roundedSeconds <= 0)
            return _data.TotalPlaytimeSeconds;

        _data.TotalPlaytimeSeconds += roundedSeconds;
        Save();
        return _data.TotalPlaytimeSeconds;
    }

    public static void SetLevelCompletionActiveObjectsCount(int count)
    {
        Load();

        _data.LevelCompletionActiveObjectsCount = Mathf.Max(0, count);
        _data.LevelCompletionObjectLevelIndex = _data.CompletedLevelIndex;
        Save();
    }

    public static void CompleteInventoryEquipTutorial()
    {
        Load();

        if (_data.InventoryEquipTutorialCompleted)
            return;

        _data.InventoryEquipTutorialCompleted = true;
        Save();
    }

    public static void Reset()
    {
        _data = new SaveData();
        EnsureCurrencyData();
        EnsureInventoryData();
        EnsureGearPackData();
        EnsureProfileData();
        _isLoaded = true;
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }

    [Serializable]
    public class SaveData
    {
        public int SelectedLevelIndex;
        public int CompletedLevelIndex = -1;
        public bool HasSelectedLevel;
        public CurrencySaveData[] Currencies = Array.Empty<CurrencySaveData>();
        public InventorySaveData Inventory = new InventorySaveData();
        public GearPackSaveData[] GearPacks = Array.Empty<GearPackSaveData>();
        public int GearPackVictoryCount;
        public int EraProgressStep;
        public int PendingEraTransitions;
        public bool EnergyUnlocked;
        public long EnergyLastRefillTicks;
        public long TotalPlaytimeSeconds;
        public int SpecialLevelIndex;
        public long SpecialKeyRefreshDateTicks;
        public long SpecialKey2RefreshDateTicks;
        public int LevelCompletionActiveObjectsCount = -1;
        public int LevelCompletionObjectLevelIndex = -1;
        public bool InventoryEquipTutorialCompleted;
        public ProfileSaveData Profile = new ProfileSaveData();
    }

    [Serializable]
    public class CurrencySaveData
    {
        public CurrencyType Type;
        public int Count;
    }

    [Serializable]
    public class InventorySaveData
    {
        public InventoryItemSaveData[] Items = Array.Empty<InventoryItemSaveData>();
        public EquippedItemSaveData[] EquippedItems = Array.Empty<EquippedItemSaveData>();
    }

    [Serializable]
    public class InventoryItemSaveData
    {
        public string ItemId;
        public InventorySlotType SlotType;
        public GearRarity Rarity = GearRarity.Common;
        public int Level = 1;
    }

    [Serializable]
    public class EquippedItemSaveData
    {
        public InventorySlotType SlotType;
        public string ItemId;
        public GearRarity Rarity = GearRarity.Common;
        public int Level = 1;
    }

    [Serializable]
    public class GearPackSaveData
    {
        public GearPackRarity Rarity;
        public int Count;
    }

    [Serializable]
    public class ProfileSaveData
    {
        public int Level = 1;
        public int Experience;
        public int RareBoosters;
        public int LastBalancedDamage;
        public int DamageUpgradeImpactDelta;
        public int DamageUpgradeImpactStep;
    }

    private static void EnsureCurrencyData()
    {
        CurrencyType[] currencyTypes = (CurrencyType[])Enum.GetValues(typeof(CurrencyType));

        if (_data.Currencies == null)
            _data.Currencies = Array.Empty<CurrencySaveData>();

        for (int i = 0; i < currencyTypes.Length; i++)
        {
            if (TryGetCurrencyData(currencyTypes[i], out _) == false)
                AddCurrencyData(currencyTypes[i]);
        }
    }

    public static CurrencySaveData GetCurrencyData(CurrencyType currencyType)
    {
        Load();

        if (TryGetCurrencyData(currencyType, out CurrencySaveData currencyData))
            return currencyData;

        return AddCurrencyData(currencyType);
    }

    public static InventorySaveData GetInventoryData()
    {
        Load();
        EnsureInventoryData();
        return _data.Inventory;
    }

    public static GearPackSaveData GetGearPackData(GearPackRarity rarity)
    {
        Load();

        if (TryGetGearPackData(rarity, out GearPackSaveData gearPackData))
            return gearPackData;

        return AddGearPackData(rarity);
    }

    public static ProfileSaveData GetProfileData()
    {
        Load();
        EnsureProfileData();
        return _data.Profile;
    }

    private static bool TryGetCurrencyData(CurrencyType currencyType, out CurrencySaveData currencyData)
    {
        for (int i = 0; i < _data.Currencies.Length; i++)
        {
            if (_data.Currencies[i] != null && _data.Currencies[i].Type == currencyType)
            {
                currencyData = _data.Currencies[i];
                return true;
            }
        }

        currencyData = null;
        return false;
    }

    private static CurrencySaveData AddCurrencyData(CurrencyType currencyType)
    {
        CurrencySaveData currencyData = new CurrencySaveData
        {
            Type = currencyType,
            Count = 0
        };

        int length = _data.Currencies.Length;
        Array.Resize(ref _data.Currencies, length + 1);
        _data.Currencies[length] = currencyData;

        return currencyData;
    }

    private static void EnsureInventoryData()
    {
        if (_data.Inventory == null)
            _data.Inventory = new InventorySaveData();

        if (_data.Inventory.Items == null)
            _data.Inventory.Items = Array.Empty<InventoryItemSaveData>();

        if (_data.Inventory.EquippedItems == null)
            _data.Inventory.EquippedItems = Array.Empty<EquippedItemSaveData>();

        for (int i = 0; i < _data.Inventory.Items.Length; i++)
        {
            if (_data.Inventory.Items[i] != null)
                _data.Inventory.Items[i].Level = Mathf.Max(1, _data.Inventory.Items[i].Level);
        }

        for (int i = 0; i < _data.Inventory.EquippedItems.Length; i++)
        {
            if (_data.Inventory.EquippedItems[i] != null)
                _data.Inventory.EquippedItems[i].Level = Mathf.Max(1, _data.Inventory.EquippedItems[i].Level);
        }

        InventorySlotType[] slotTypes = (InventorySlotType[])Enum.GetValues(typeof(InventorySlotType));

        for (int i = 0; i < slotTypes.Length; i++)
        {
            if (TryGetEquippedItemData(slotTypes[i], out _) == false)
                AddEquippedItemData(slotTypes[i]);
        }
    }

    private static bool TryGetEquippedItemData(InventorySlotType slotType, out EquippedItemSaveData equippedItemData)
    {
        for (int i = 0; i < _data.Inventory.EquippedItems.Length; i++)
        {
            if (_data.Inventory.EquippedItems[i] != null && _data.Inventory.EquippedItems[i].SlotType == slotType)
            {
                equippedItemData = _data.Inventory.EquippedItems[i];
                return true;
            }
        }

        equippedItemData = null;
        return false;
    }

    private static EquippedItemSaveData AddEquippedItemData(InventorySlotType slotType)
    {
        EquippedItemSaveData equippedItemData = new EquippedItemSaveData
        {
            SlotType = slotType,
            ItemId = string.Empty
        };

        int length = _data.Inventory.EquippedItems.Length;
        Array.Resize(ref _data.Inventory.EquippedItems, length + 1);
        _data.Inventory.EquippedItems[length] = equippedItemData;

        return equippedItemData;
    }

    private static void EnsureGearPackData()
    {
        GearPackRarity[] rarities = (GearPackRarity[])Enum.GetValues(typeof(GearPackRarity));

        if (_data.GearPacks == null)
            _data.GearPacks = Array.Empty<GearPackSaveData>();

        for (int i = 0; i < rarities.Length; i++)
        {
            if (TryGetGearPackData(rarities[i], out _) == false)
                AddGearPackData(rarities[i]);
        }
    }

    private static bool TryGetGearPackData(GearPackRarity rarity, out GearPackSaveData gearPackData)
    {
        for (int i = 0; i < _data.GearPacks.Length; i++)
        {
            if (_data.GearPacks[i] != null && _data.GearPacks[i].Rarity == rarity)
            {
                gearPackData = _data.GearPacks[i];
                return true;
            }
        }

        gearPackData = null;
        return false;
    }

    private static GearPackSaveData AddGearPackData(GearPackRarity rarity)
    {
        GearPackSaveData gearPackData = new GearPackSaveData
        {
            Rarity = rarity,
            Count = 0
        };

        int length = _data.GearPacks.Length;
        Array.Resize(ref _data.GearPacks, length + 1);
        _data.GearPacks[length] = gearPackData;

        return gearPackData;
    }

    private static void EnsureProfileData()
    {
        if (_data.Profile == null)
            _data.Profile = new ProfileSaveData();

        _data.Profile.Level = Mathf.Max(1, _data.Profile.Level);
        _data.Profile.Experience = Mathf.Max(0, _data.Profile.Experience);
        _data.Profile.RareBoosters = Mathf.Max(0, _data.Profile.RareBoosters);
        _data.Profile.LastBalancedDamage = Mathf.Max(0, _data.Profile.LastBalancedDamage);
        _data.Profile.DamageUpgradeImpactDelta = Mathf.Max(0, _data.Profile.DamageUpgradeImpactDelta);
        _data.Profile.DamageUpgradeImpactStep = Mathf.Clamp(_data.Profile.DamageUpgradeImpactStep, 0, ProfileManager.DamageUpgradeImpactSteps);
        _data.EraProgressStep = Mathf.Clamp(_data.EraProgressStep, 0, EraTransitionManager.StepsPerCycle - 1);
        _data.PendingEraTransitions = Mathf.Max(0, _data.PendingEraTransitions);
    }
}
