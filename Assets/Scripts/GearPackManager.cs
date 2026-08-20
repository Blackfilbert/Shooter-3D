using System;
using System.Collections.Generic;
using UnityEngine;

public static class GearPackManager
{
    private const int UncommonPackVictoryInterval = 5;
    private const int WeaponDropStartCompletedLevelIndex = 9;

    public static event Action<GearPackRarity, int> PackCountChanged;

    private static readonly Queue<GearPackReward> PendingRewards = new Queue<GearPackReward>();
    private static GearPackOpeningPopup _activePopup;

    public static int GetPackCount(GearPackRarity rarity)
    {
        return SaveManager.GetGearPackData(rarity).Count;
    }

    public static GearPackRarity AddVictoryPack()
    {
        return AddVictoryPack(null);
    }

    public static GearPackRarity AddVictoryPack(GearPacksConfig config)
    {
        return AddVictoryPack(config, out _);
    }

    public static GearPackRarity AddVictoryPack(GearPacksConfig config, out LevelCompletionReward reward)
    {
        Debug.Log($"[RewardDebug] GearPackManager.AddVictoryPack start. config={config != null}, victoryCountBefore={SaveManager.Data.GearPackVictoryCount}");
        SaveManager.Data.GearPackVictoryCount++;
        GearPackRarity rarity = SaveManager.Data.GearPackVictoryCount % UncommonPackVictoryInterval == 0
            ? GearPackRarity.Uncommon
            : GearPackRarity.Common;

        reward = new LevelCompletionReward
        {
            PackRarity = rarity,
            PackCount = 1
        };

        AddPack(rarity, 1);
        Debug.Log($"[RewardDebug] GearPackManager.AddVictoryPack end. rarity={rarity}, victoryCountAfter={SaveManager.Data.GearPackVictoryCount}, count={GetPackCount(rarity)}");

        return rarity;
    }

    public static void AddPack(GearPackRarity rarity, int amount)
    {
        if (amount <= 0)
            return;

        SaveManager.GearPackSaveData packData = SaveManager.GetGearPackData(rarity);
        int previousCount = packData.Count;
        packData.Count += amount;
        SaveManager.Save();
        Debug.Log($"[RewardDebug] GearPackManager.AddPack. rarity={rarity}, amount={amount}, previous={previousCount}, current={packData.Count}");
        PackCountChanged?.Invoke(rarity, packData.Count);
    }

    public static void AddPack(GearPacksConfig config, GearPackRarity rarity, int amount)
    {
        if (amount <= 0)
            return;

        for (int i = 0; i < amount; i++)
        {
            if (TryOpenDroppedPack(config, rarity) == false)
                AddPack(rarity, 1);
        }
    }

    public static bool OpenOwnedPack(GearPacksConfig config, GearPackRarity rarity, out GearPackReward reward)
    {
        reward = default;
        Debug.Log($"[RewardDebug] OpenOwnedPack start. config={config != null}, ui={Global.UIController != null}, rarity={rarity}, count={GetPackCount(rarity)}");

        if (config == null || Global.UIController == null)
        {
            Debug.Log("[RewardDebug] OpenOwnedPack failed. missing config or UIController.");
            return false;
        }

        SaveManager.GearPackSaveData packData = SaveManager.GetGearPackData(rarity);

        if (packData.Count <= 0)
        {
            Debug.Log("[RewardDebug] OpenOwnedPack failed. count <= 0.");
            return false;
        }

        reward = GenerateReward(config, rarity);

        if (TryEnqueueRewardPopup(reward) == false)
        {
            Debug.Log("[RewardDebug] OpenOwnedPack failed. popup enqueue failed.");
            return false;
        }

        packData.Count--;
        ApplyReward(reward);
        SaveManager.Save();
        PackCountChanged?.Invoke(rarity, packData.Count);
        Debug.Log($"[RewardDebug] OpenOwnedPack done. rarity={rarity}, count={packData.Count}, rewards={GetRewardDebugText(reward)}");
        return true;
    }

    public static bool TryOpenPendingRewardPopups()
    {
        return TryOpenNextRewardPopup();
    }

    public static bool TryOpenPendingRewardPopups(GearPacksConfig config)
    {
        Debug.Log($"[RewardDebug] TryOpenPendingRewardPopups(config) start. config={config != null}, ui={Global.UIController != null}, pending={PendingRewards.Count}");
        MaterializeOwnedPacks(config);
        bool result = TryOpenNextRewardPopup();
        Debug.Log($"[RewardDebug] TryOpenPendingRewardPopups(config) done. result={result}, pending={PendingRewards.Count}");
        return result;
    }

    public static bool BuyPack(GearPacksConfig config, GearPackRarity rarity, out GearPackReward reward)
    {
        reward = default;

        if (config == null || Global.UIController == null)
            return false;

        int price = config.GetShopPrice(rarity);

        if (CurrencyManager.GetCount(CurrencyType.Soft) < price)
            return false;

        reward = GenerateReward(config, rarity);

        if (CurrencyManager.Spend(CurrencyType.Soft, price) == false)
            return false;

        if (TryEnqueueRewardPopup(reward) == false)
        {
            CurrencyManager.Add(CurrencyType.Soft, price);
            return false;
        }

        ApplyReward(reward);
        SaveManager.Save();
        return true;
    }

    private static GearPackReward GenerateReward(GearPacksConfig config, GearPackRarity rarity)
    {
        GearPackReward reward = new GearPackReward
        {
            PackRarity = rarity,
            Items = new GearPackItemReward[Mathf.Max(1, (int)rarity + 1)]
        };

        for (int i = 0; i < reward.Items.Length; i++)
        {
            InventorySlotType? requiredSlotType = ShouldForceWeaponDrop(i) ? InventorySlotType.Weapon : null;
            InventorySlotType? excludedSlotType = ShouldExcludeWeaponDrop() ? InventorySlotType.Weapon : null;

            if (config.TryGetRandomItem(rarity, requiredSlotType, excludedSlotType, out GearPackItemEntry itemEntry) == false)
                continue;

            reward.Items[i] = new GearPackItemReward
            {
                ItemId = itemEntry.ItemId,
                SlotType = itemEntry.SlotType,
                Rarity = itemEntry.Rarity,
                Cards = UnityEngine.Random.Range(Mathf.Max(1, config.MinItemCards), Mathf.Max(config.MinItemCards, config.MaxItemCards) + 1)
            };
        }

        int rarityMultiplier = (int)rarity + 1;
        int soft = config.SoftPerRarity * rarityMultiplier + UnityEngine.Random.Range(-config.SoftRandomRange, config.SoftRandomRange + 1);
        reward.Soft = Mathf.Max(0, soft);
        return reward;
    }

    private static bool ShouldExcludeWeaponDrop()
    {
        return SaveManager.CompletedLevelIndex < WeaponDropStartCompletedLevelIndex;
    }

    private static bool ShouldForceWeaponDrop(int itemIndex)
    {
        return itemIndex == 0 && ShouldExcludeWeaponDrop() == false;
    }

    private static void ApplyReward(GearPackReward reward)
    {
        if (reward.Items != null)
        {
            for (int i = 0; i < reward.Items.Length; i++)
            {
                if (string.IsNullOrEmpty(reward.Items[i].ItemId))
                    continue;

                InventoryManager.AddItemCopies(reward.Items[i].ItemId, reward.Items[i].SlotType, reward.Items[i].Rarity, reward.Items[i].Cards);
            }
        }

        CurrencyManager.Add(CurrencyType.Soft, reward.Soft);
    }

    private static bool TryOpenDroppedPack(GearPacksConfig config, GearPackRarity rarity)
    {
        return TryOpenDroppedPack(config, rarity, out _);
    }

    private static bool TryOpenDroppedPack(GearPacksConfig config, GearPackRarity rarity, out GearPackReward reward)
    {
        if (config == null)
        {
            reward = default;
            return false;
        }

        reward = GenerateReward(config, rarity);
        ApplyReward(reward);
        SaveManager.Save();
        PendingRewards.Enqueue(reward);
        return true;
    }

    private static void MaterializeOwnedPacks(GearPacksConfig config)
    {
        if (config == null || Global.UIController == null)
        {
            Debug.Log($"[RewardDebug] MaterializeOwnedPacks skipped. config={config != null}, ui={Global.UIController != null}");
            return;
        }

        bool changed = false;
        GearPackRarity[] rarities = (GearPackRarity[])Enum.GetValues(typeof(GearPackRarity));

        for (int i = 0; i < rarities.Length; i++)
        {
            SaveManager.GearPackSaveData packData = SaveManager.GetGearPackData(rarities[i]);

            while (packData.Count > 0)
            {
                Debug.Log($"[RewardDebug] MaterializeOwnedPacks opening owned pack. rarity={rarities[i]}, countBefore={packData.Count}");
                GearPackReward reward = GenerateReward(config, rarities[i]);
                packData.Count--;
                ApplyReward(reward);
                PendingRewards.Enqueue(reward);
                PackCountChanged?.Invoke(rarities[i], packData.Count);
                changed = true;
            }
        }

        if (changed)
        {
            SaveManager.Save();
            Debug.Log("[RewardDebug] MaterializeOwnedPacks saved changes.");
        }
    }

    private static bool TryEnqueueRewardPopup(GearPackReward reward)
    {
        if (_activePopup == null && Global.UIController == null)
            return false;

        PendingRewards.Enqueue(reward);

        if (_activePopup != null)
            return true;

        if (TryOpenNextRewardPopup())
            return true;

        PendingRewards.Clear();
        return false;
    }

    private static bool TryOpenNextRewardPopup()
    {
        if (_activePopup != null || PendingRewards.Count <= 0 || Global.UIController == null)
        {
            Debug.Log($"[RewardDebug] TryOpenNextRewardPopup skipped. active={_activePopup != null}, pending={PendingRewards.Count}, ui={Global.UIController != null}");
            return _activePopup != null;
        }

        GearPackOpeningPopup popup = Global.UIController.Show<GearPackOpeningPopup>();

        if (popup == null)
        {
            Debug.Log("[RewardDebug] TryOpenNextRewardPopup failed. popup=null.");
            return false;
        }

        _activePopup = popup;
        _activePopup.Completed += OnActivePopupCompleted;
        GearPackReward reward = PendingRewards.Dequeue();
        Debug.Log($"[RewardDebug] TryOpenNextRewardPopup opening. pendingAfterDequeue={PendingRewards.Count}, rewards={GetRewardDebugText(reward)}");
        _activePopup.Open(reward);
        return true;
    }

    private static string GetRewardDebugText(GearPackReward reward)
    {
        return $"pack={reward.PackRarity}, items={(reward.Items != null ? reward.Items.Length : 0)}, soft={reward.Soft}";
    }

    private static void OnActivePopupCompleted()
    {
        if (_activePopup != null)
            _activePopup.Completed -= OnActivePopupCompleted;

        _activePopup = null;
        TryOpenNextRewardPopup();
    }
}

public enum GearPackRarity
{
    Common,
    Uncommon
}

public struct LevelCompletionReward
{
    public GearPackRarity PackRarity;
    public int PackCount;
    public int Soft;
    public int Hard;
}

public struct GearPackReward
{
    public GearPackRarity PackRarity;
    public GearPackItemReward[] Items;
    public int Soft;
}

public struct GearPackItemReward
{
    public string ItemId;
    public InventorySlotType SlotType;
    public GearRarity Rarity;
    public int Cards;
}
