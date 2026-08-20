using System;

public static class InventoryManager
{
    private const int BaseStat = 10;
    private const int WeaponStatGrowth = 1;
    private const int ClothesStatGrowth = 2;
    private const int BaseUpgradeCost = 100;

    public static event Action InventoryChanged;
    public static event Action<InventorySlotType, string> EquipmentChanged;

    public static SaveManager.InventoryItemSaveData[] Items => SaveManager.GetInventoryData().Items;
    public static SaveManager.EquippedItemSaveData[] EquippedItems => SaveManager.GetInventoryData().EquippedItems;

    public static bool TryGetInventoryItem(int itemIndex, out SaveManager.InventoryItemSaveData itemData)
    {
        SaveManager.InventoryItemSaveData[] items = SaveManager.GetInventoryData().Items;

        if (itemIndex < 0 || itemIndex >= items.Length || items[itemIndex] == null)
        {
            itemData = null;
            return false;
        }

        itemData = items[itemIndex];
        return true;
    }

    public static void AddItem(string itemId, InventorySlotType slotType)
    {
        AddItem(itemId, slotType, GearRarity.Common, 1);
    }

    public static void AddItem(string itemId, InventorySlotType slotType, GearRarity rarity, int level = 1)
    {
        if (string.IsNullOrEmpty(itemId))
            return;

        SaveManager.InventorySaveData inventoryData = SaveManager.GetInventoryData();
        SaveManager.InventoryItemSaveData itemData = new SaveManager.InventoryItemSaveData
        {
            ItemId = itemId,
            SlotType = slotType,
            Rarity = rarity,
            Level = Math.Max(1, level)
        };

        int length = inventoryData.Items.Length;
        Array.Resize(ref inventoryData.Items, length + 1);
        inventoryData.Items[length] = itemData;

        SaveManager.Save();
        InventoryChanged?.Invoke();
    }

    public static void AddItemCopies(string itemId, InventorySlotType slotType, GearRarity rarity, int amount, int level = 1)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
            return;

        for (int i = 0; i < amount; i++)
            AddItemWithoutSave(itemId, slotType, rarity, level);

        SaveManager.Save();
        InventoryChanged?.Invoke();
    }

    public static bool RemoveItem(string itemId)
    {
        SaveManager.InventorySaveData inventoryData = SaveManager.GetInventoryData();
        int itemIndex = FindInventoryItemIndex(itemId);

        if (itemIndex < 0)
            return false;

        RemoveInventoryItemAt(inventoryData, itemIndex);
        SaveManager.Save();
        InventoryChanged?.Invoke();
        return true;
    }

    public static bool Equip(string itemId)
    {
        SaveManager.InventorySaveData inventoryData = SaveManager.GetInventoryData();
        int itemIndex = FindInventoryItemIndex(itemId);

        if (itemIndex < 0)
            return false;

        return EquipAt(itemIndex);
    }

    public static bool EquipAt(int itemIndex)
    {
        SaveManager.InventorySaveData inventoryData = SaveManager.GetInventoryData();

        if (itemIndex < 0 || itemIndex >= inventoryData.Items.Length || inventoryData.Items[itemIndex] == null)
            return false;

        SaveManager.InventoryItemSaveData itemData = inventoryData.Items[itemIndex];
        SaveManager.EquippedItemSaveData equippedItemData = GetEquippedItemData(itemData.SlotType);

        if (string.IsNullOrEmpty(equippedItemData.ItemId) == false)
            AddItemWithoutSave(equippedItemData.ItemId, equippedItemData.SlotType, equippedItemData.Rarity, equippedItemData.Level);

        equippedItemData.ItemId = itemData.ItemId;
        equippedItemData.Rarity = itemData.Rarity;
        equippedItemData.Level = itemData.Level;
        RemoveInventoryItemAt(inventoryData, itemIndex);

        SaveManager.Save();
        InventoryChanged?.Invoke();
        EquipmentChanged?.Invoke(equippedItemData.SlotType, equippedItemData.ItemId);
        return true;
    }

    public static bool Unequip(InventorySlotType slotType)
    {
        SaveManager.EquippedItemSaveData equippedItemData = GetEquippedItemData(slotType);

        if (string.IsNullOrEmpty(equippedItemData.ItemId))
            return false;

        AddItemWithoutSave(equippedItemData.ItemId, equippedItemData.SlotType, equippedItemData.Rarity, equippedItemData.Level);
        equippedItemData.ItemId = string.Empty;
        equippedItemData.Rarity = GearRarity.Common;
        equippedItemData.Level = 1;

        SaveManager.Save();
        InventoryChanged?.Invoke();
        EquipmentChanged?.Invoke(slotType, string.Empty);
        return true;
    }

    public static bool TryGetEquippedItem(InventorySlotType slotType, out string itemId)
    {
        SaveManager.EquippedItemSaveData equippedItemData = GetEquippedItemData(slotType);
        itemId = equippedItemData.ItemId;
        return string.IsNullOrEmpty(itemId) == false;
    }

    public static bool TryGetEquippedItemData(InventorySlotType slotType, out SaveManager.EquippedItemSaveData itemData)
    {
        itemData = GetEquippedItemData(slotType);
        return string.IsNullOrEmpty(itemData.ItemId) == false;
    }

    public static InventoryItemStats GetItemStats(SaveManager.InventoryItemSaveData itemData)
    {
        if (itemData == null)
            return new InventoryItemStats();

        return GetItemStats(itemData.SlotType, itemData.Rarity, itemData.Level);
    }

    public static InventoryItemStats GetItemStats(SaveManager.EquippedItemSaveData itemData)
    {
        if (itemData == null || string.IsNullOrEmpty(itemData.ItemId))
            return new InventoryItemStats();

        return GetItemStats(itemData.SlotType, itemData.Rarity, itemData.Level);
    }

    public static InventoryItemStats GetEquippedItemStats(InventorySlotType slotType)
    {
        SaveManager.EquippedItemSaveData equippedItemData = GetEquippedItemData(slotType);

        if (string.IsNullOrEmpty(equippedItemData.ItemId))
            return new InventoryItemStats();

        return GetItemStats(equippedItemData.SlotType, equippedItemData.Rarity, equippedItemData.Level);
    }

    public static InventoryItemStats GetTotalEquippedStats()
    {
        SaveManager.EquippedItemSaveData[] equippedItems = SaveManager.GetInventoryData().EquippedItems;
        InventoryItemStats totalStats = new InventoryItemStats();

        for (int i = 0; i < equippedItems.Length; i++)
        {
            if (equippedItems[i] == null || string.IsNullOrEmpty(equippedItems[i].ItemId))
                continue;

            InventoryItemStats stats = GetItemStats(equippedItems[i].SlotType, equippedItems[i].Rarity, equippedItems[i].Level);
            totalStats.Damage += stats.Damage;
            totalStats.AimStability += stats.AimStability;
            totalStats.Accuracy += stats.Accuracy;
            totalStats.Health += stats.Health;
        }

        totalStats.AimStability += ProfileManager.AimStabilityBonus;
        return totalStats;
    }

    public static InventoryItemStats GetBalanceStats()
    {
        InventoryItemStats balanceStats = GetTotalEquippedStats();
        InventorySlotType[] slotTypes = (InventorySlotType[])Enum.GetValues(typeof(InventorySlotType));

        for (int i = 0; i < slotTypes.Length; i++)
        {
            InventoryItemStats equippedStats = GetEquippedItemStats(slotTypes[i]);
            InventoryItemStats bestStats = GetBestAvailableStatsForSlot(slotTypes[i]);
            AddStatsDifference(ref balanceStats, bestStats, equippedStats);
        }

        return balanceStats;
    }

    public static int GetBalanceWeaponDamage()
    {
        return GetBestAvailableStatsForSlot(InventorySlotType.Weapon).Damage;
    }

    public static bool UpgradeItem(string itemId)
    {
        SaveManager.InventorySaveData inventoryData = SaveManager.GetInventoryData();
        int itemIndex = FindInventoryItemIndex(itemId);

        if (itemIndex < 0)
            return false;

        SaveManager.InventoryItemSaveData itemData = inventoryData.Items[itemIndex];
        return TryUpgradeInventoryItem(itemData, itemIndex);
    }

    public static bool UpgradeItemAt(int itemIndex)
    {
        SaveManager.InventorySaveData inventoryData = SaveManager.GetInventoryData();

        if (itemIndex < 0 || itemIndex >= inventoryData.Items.Length || inventoryData.Items[itemIndex] == null)
            return false;

        return TryUpgradeInventoryItem(inventoryData.Items[itemIndex], itemIndex);
    }

    public static bool UpgradeEquippedItem(InventorySlotType slotType)
    {
        SaveManager.EquippedItemSaveData equippedItemData = GetEquippedItemData(slotType);

        if (string.IsNullOrEmpty(equippedItemData.ItemId))
            return false;

        return TryUpgradeEquippedItem(equippedItemData);
    }

    public static int GetRequiredCopiesForUpgrade(int level)
    {
        return Math.Max(1, level) + 1;
    }

    public static int GetAvailableUpgradeCopiesForItemAt(int itemIndex)
    {
        if (TryGetInventoryItem(itemIndex, out SaveManager.InventoryItemSaveData itemData) == false)
            return 0;

        return CountUpgradeCopies(itemData, itemIndex);
    }

    public static int GetAvailableUpgradeCopiesForEquippedItem(InventorySlotType slotType)
    {
        SaveManager.EquippedItemSaveData itemData = GetEquippedItemData(slotType);

        if (string.IsNullOrEmpty(itemData.ItemId))
            return 0;

        return CountUpgradeCopies(itemData);
    }

    public static bool CanUpgradeItemAt(int itemIndex)
    {
        if (TryGetInventoryItem(itemIndex, out SaveManager.InventoryItemSaveData itemData) == false)
            return false;

        return GetAvailableUpgradeCopiesForItemAt(itemIndex) >= GetRequiredCopiesForUpgrade(itemData.Level)
            && CurrencyManager.GetCount(CurrencyType.Soft) >= GetUpgradeSoftCost(itemData.Level);
    }

    public static bool CanUpgradeEquippedItem(InventorySlotType slotType)
    {
        SaveManager.EquippedItemSaveData itemData = GetEquippedItemData(slotType);

        if (string.IsNullOrEmpty(itemData.ItemId))
            return false;

        return GetAvailableUpgradeCopiesForEquippedItem(slotType) >= GetRequiredCopiesForUpgrade(itemData.Level)
            && CurrencyManager.GetCount(CurrencyType.Soft) >= GetUpgradeSoftCost(itemData.Level);
    }

    public static bool IsBestUnequippedItemForSlot(int itemIndex)
    {
        if (TryGetInventoryItem(itemIndex, out SaveManager.InventoryItemSaveData itemData) == false)
            return false;

        return TryGetBestUnequippedItemForSlot(itemData.SlotType, out int bestItemIndex) && bestItemIndex == itemIndex;
    }

    public static bool TryGetBestUnequippedItemForSlot(InventorySlotType slotType, out int itemIndex)
    {
        return TryGetBestUnequippedItemForSlot(slotType, true, out itemIndex);
    }

    public static bool TryGetBestInventoryItemForSlot(InventorySlotType slotType, out int itemIndex)
    {
        return TryGetBestUnequippedItemForSlot(slotType, false, out itemIndex);
    }

    private static bool TryGetBestUnequippedItemForSlot(InventorySlotType slotType, bool mustBeatEquipped, out int itemIndex)
    {
        SaveManager.InventoryItemSaveData[] items = SaveManager.GetInventoryData().Items;
        int bestGearScore = mustBeatEquipped ? GetEquippedItemStats(slotType).GearScore : -1;
        itemIndex = -1;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null || items[i].SlotType != slotType)
                continue;

            int gearScore = GetItemStats(items[i]).GearScore;

            if (gearScore <= bestGearScore)
                continue;

            bestGearScore = gearScore;
            itemIndex = i;
        }

        return itemIndex >= 0;
    }

    public static int GetUpgradeSoftCost(int level)
    {
        int cost = BaseUpgradeCost;
        int steps = Math.Max(1, level) - 1;

        for (int i = 0; i < steps; i++)
            cost *= 2;

        return cost;
    }

    public static int GetGearScore(InventoryItemStats stats)
    {
        return stats.Accuracy + stats.AimStability + stats.Health + stats.Damage * 2;
    }

    private static InventoryItemStats GetBestAvailableStatsForSlot(InventorySlotType slotType)
    {
        SaveManager.EquippedItemSaveData equippedItem = GetEquippedItemData(slotType);
        InventoryItemStats bestStats = GetItemStats(equippedItem);

        if (string.IsNullOrEmpty(equippedItem.ItemId) == false && CanUpgradeEquippedItem(slotType))
            bestStats = GetHigherGearScoreStats(bestStats, GetItemStats(slotType, equippedItem.Rarity, equippedItem.Level + 1));

        SaveManager.InventoryItemSaveData[] items = SaveManager.GetInventoryData().Items;

        for (int i = 0; i < items.Length; i++)
        {
            SaveManager.InventoryItemSaveData item = items[i];

            if (item == null || item.SlotType != slotType)
                continue;

            bestStats = GetHigherGearScoreStats(bestStats, GetItemStats(item));

            if (CanUpgradeItemAt(i))
                bestStats = GetHigherGearScoreStats(bestStats, GetItemStats(item.SlotType, item.Rarity, item.Level + 1));
        }

        return bestStats;
    }

    private static InventoryItemStats GetHigherGearScoreStats(InventoryItemStats currentStats, InventoryItemStats candidateStats)
    {
        return candidateStats.GearScore > currentStats.GearScore ? candidateStats : currentStats;
    }

    private static void AddStatsDifference(ref InventoryItemStats totalStats, InventoryItemStats addedStats, InventoryItemStats removedStats)
    {
        totalStats.Damage += addedStats.Damage - removedStats.Damage;
        totalStats.AimStability += addedStats.AimStability - removedStats.AimStability;
        totalStats.Accuracy += addedStats.Accuracy - removedStats.Accuracy;
        totalStats.Health += addedStats.Health - removedStats.Health;
    }

    private static SaveManager.EquippedItemSaveData GetEquippedItemData(InventorySlotType slotType)
    {
        SaveManager.InventorySaveData inventoryData = SaveManager.GetInventoryData();

        for (int i = 0; i < inventoryData.EquippedItems.Length; i++)
        {
            if (inventoryData.EquippedItems[i] != null && inventoryData.EquippedItems[i].SlotType == slotType)
                return inventoryData.EquippedItems[i];
        }

        SaveManager.EquippedItemSaveData equippedItemData = new SaveManager.EquippedItemSaveData
        {
            SlotType = slotType,
            ItemId = string.Empty
        };

        int length = inventoryData.EquippedItems.Length;
        Array.Resize(ref inventoryData.EquippedItems, length + 1);
        inventoryData.EquippedItems[length] = equippedItemData;
        return equippedItemData;
    }

    private static int FindInventoryItemIndex(string itemId)
    {
        SaveManager.InventoryItemSaveData[] items = SaveManager.GetInventoryData().Items;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].ItemId == itemId)
                return i;
        }

        return -1;
    }

    private static InventoryItemStats GetItemStats(InventorySlotType slotType, GearRarity rarity, int level)
    {
        InventoryItemStats stats = new InventoryItemStats();
        int scaledStat = GetScaledStat(slotType, rarity, level);

        if (slotType == InventorySlotType.Weapon)
        {
            stats.Damage = scaledStat;
        }
        else
        {
            stats.AimStability = scaledStat;
            stats.Health = scaledStat;
        }

        return stats;
    }

    private static int GetScaledStat(InventorySlotType slotType, GearRarity rarity, int level)
    {
        int growth = slotType == InventorySlotType.Weapon ? WeaponStatGrowth : ClothesStatGrowth;
        int rarityMultiplier = GetRarityMultiplier(rarity);
        return (BaseStat + growth * Math.Max(1, level)) * rarityMultiplier;
    }

    private static int GetRarityMultiplier(GearRarity rarity)
    {
        switch (rarity)
        {
            case GearRarity.Uncommon:
                return 2;
            case GearRarity.Rare:
                return 3;
            case GearRarity.Epic:
                return 4;
            default:
                return 1;
        }
    }

    private static bool TryUpgradeInventoryItem(SaveManager.InventoryItemSaveData itemData, int itemIndex)
    {
        int requiredCopies = GetRequiredCopiesForUpgrade(itemData.Level);
        int softCost = GetUpgradeSoftCost(itemData.Level);

        if (CountUpgradeCopies(itemData, itemIndex) < requiredCopies)
            return false;

        if (CurrencyManager.Spend(CurrencyType.Soft, softCost) == false)
            return false;

        RemoveUpgradeCopies(itemData, requiredCopies, itemIndex);
        itemData.Level++;

        SaveManager.Save();
        ProfileManager.AddGearUpgradeExperience();
        InventoryChanged?.Invoke();
        return true;
    }

    private static bool TryUpgradeEquippedItem(SaveManager.EquippedItemSaveData itemData)
    {
        int requiredCopies = GetRequiredCopiesForUpgrade(itemData.Level);
        int softCost = GetUpgradeSoftCost(itemData.Level);

        if (CountUpgradeCopies(itemData) < requiredCopies)
            return false;

        if (CurrencyManager.Spend(CurrencyType.Soft, softCost) == false)
            return false;

        RemoveUpgradeCopies(itemData, requiredCopies);
        itemData.Level++;

        SaveManager.Save();
        ProfileManager.AddGearUpgradeExperience();
        InventoryChanged?.Invoke();
        EquipmentChanged?.Invoke(itemData.SlotType, itemData.ItemId);
        return true;
    }

    private static int CountUpgradeCopies(SaveManager.InventoryItemSaveData itemData, int excludedIndex)
    {
        SaveManager.InventoryItemSaveData[] items = SaveManager.GetInventoryData().Items;
        int count = 0;

        for (int i = 0; i < items.Length; i++)
        {
            if (i == excludedIndex)
                continue;

            if (IsSameItem(items[i], itemData))
                count++;
        }

        return count;
    }

    private static int CountUpgradeCopies(SaveManager.EquippedItemSaveData itemData)
    {
        SaveManager.InventoryItemSaveData[] items = SaveManager.GetInventoryData().Items;
        int count = 0;

        for (int i = 0; i < items.Length; i++)
        {
            if (IsSameItem(items[i], itemData))
                count++;
        }

        return count;
    }

    private static void RemoveUpgradeCopies(SaveManager.InventoryItemSaveData itemData, int count, int excludedIndex)
    {
        SaveManager.InventorySaveData inventoryData = SaveManager.GetInventoryData();

        for (int i = inventoryData.Items.Length - 1; i >= 0 && count > 0; i--)
        {
            if (i == excludedIndex)
                continue;

            if (IsSameItem(inventoryData.Items[i], itemData) == false)
                continue;

            RemoveInventoryItemAt(inventoryData, i);
            count--;
        }
    }

    private static void RemoveUpgradeCopies(SaveManager.EquippedItemSaveData itemData, int count)
    {
        SaveManager.InventorySaveData inventoryData = SaveManager.GetInventoryData();

        for (int i = inventoryData.Items.Length - 1; i >= 0 && count > 0; i--)
        {
            if (IsSameItem(inventoryData.Items[i], itemData) == false)
                continue;

            RemoveInventoryItemAt(inventoryData, i);
            count--;
        }
    }

    private static bool IsSameItem(SaveManager.InventoryItemSaveData itemData, SaveManager.InventoryItemSaveData sourceItemData)
    {
        return itemData != null
            && sourceItemData != null
            && itemData.ItemId == sourceItemData.ItemId
            && itemData.SlotType == sourceItemData.SlotType
            && itemData.Rarity == sourceItemData.Rarity;
    }

    private static bool IsSameItem(SaveManager.InventoryItemSaveData itemData, SaveManager.EquippedItemSaveData sourceItemData)
    {
        return itemData != null
            && sourceItemData != null
            && itemData.ItemId == sourceItemData.ItemId
            && itemData.SlotType == sourceItemData.SlotType
            && itemData.Rarity == sourceItemData.Rarity;
    }

    private static void AddItemWithoutSave(string itemId, InventorySlotType slotType, GearRarity rarity, int level)
    {
        SaveManager.InventorySaveData inventoryData = SaveManager.GetInventoryData();
        SaveManager.InventoryItemSaveData itemData = new SaveManager.InventoryItemSaveData
        {
            ItemId = itemId,
            SlotType = slotType,
            Rarity = rarity,
            Level = Math.Max(1, level)
        };

        int length = inventoryData.Items.Length;
        Array.Resize(ref inventoryData.Items, length + 1);
        inventoryData.Items[length] = itemData;
    }

    private static void RemoveInventoryItemAt(SaveManager.InventorySaveData inventoryData, int index)
    {
        for (int i = index; i < inventoryData.Items.Length - 1; i++)
            inventoryData.Items[i] = inventoryData.Items[i + 1];

        Array.Resize(ref inventoryData.Items, inventoryData.Items.Length - 1);
    }
}

public enum InventorySlotType
{
    Body,
    Weapon,
    Pants,
    Shoes
}

public enum GearRarity
{
    Common,
    Uncommon,
    Rare,
    Epic
}

[Serializable]
public struct InventoryItemStats
{
    public int Damage;
    public int AimStability;
    public int Accuracy;
    public int Health;

    public int GearScore => InventoryManager.GetGearScore(this);
}
