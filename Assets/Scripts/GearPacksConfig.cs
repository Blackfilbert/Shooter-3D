using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GearPacksConfig", menuName = "Configs/Gear Packs Config")]
public class GearPacksConfig : ScriptableObject
{
    [SerializeField] private GearVisualsConfig[] _items = Array.Empty<GearVisualsConfig>();
    [SerializeField] private GearPackVisualEntry[] _packVisuals = Array.Empty<GearPackVisualEntry>();
    [SerializeField] private GearRarityChance[] _rarityChances = Array.Empty<GearRarityChance>();
    [SerializeField] private GearPackShopEntry[] _shopEntries = Array.Empty<GearPackShopEntry>();
    [SerializeField] private int _minItemCards = 4;
    [SerializeField] private int _maxItemCards = 10;
    [SerializeField] private int _softPerRarity = 20;
    [SerializeField] private int _softRandomRange = 20;

    public int MinItemCards => _minItemCards;
    public int MaxItemCards => _maxItemCards;
    public int SoftPerRarity => _softPerRarity;
    public int SoftRandomRange => _softRandomRange;

    public bool TryGetPackVisual(GearPackRarity rarity, out GearPackVisualEntry visualEntry)
    {
        for (int i = 0; i < _packVisuals.Length; i++)
        {
            if (_packVisuals[i].Rarity == rarity)
            {
                visualEntry = _packVisuals[i];
                return true;
            }
        }

        visualEntry = default;
        return false;
    }

    public bool TryGetRandomItem(GearPackRarity packRarity, out GearPackItemEntry itemEntry)
    {
        return TryGetRandomItem(packRarity, null, null, out itemEntry);
    }

    public bool TryGetRandomItem(GearPackRarity packRarity, InventorySlotType? requiredSlotType, InventorySlotType? excludedSlotType, out GearPackItemEntry itemEntry)
    {
        if (TryGetRandomItemConfig(requiredSlotType, excludedSlotType, out GearVisualsConfig itemConfig, out GearRarity rarity) == false)
        {
            itemEntry = default;
            return false;
        }

        itemEntry = new GearPackItemEntry
        {
            ItemId = itemConfig.ItemId,
            SlotType = itemConfig.SlotType,
            Rarity = rarity
        };

        return true;
    }

    public int GetShopPrice(GearPackRarity rarity)
    {
        for (int i = 0; i < _shopEntries.Length; i++)
        {
            if (_shopEntries[i].Rarity == rarity)
                return Mathf.Max(0, _shopEntries[i].SoftPrice);
        }

        return 0;
    }

    private GearRarity GetRandomRarity()
    {
        int totalWeight = 0;

        for (int i = 0; i < _rarityChances.Length; i++)
            totalWeight += Mathf.Max(0, _rarityChances[i].Chance);

        if (totalWeight <= 0)
            return GearRarity.Common;

        int randomWeight = UnityEngine.Random.Range(0, totalWeight);

        for (int i = 0; i < _rarityChances.Length; i++)
        {
            int chance = Mathf.Max(0, _rarityChances[i].Chance);

            if (randomWeight < chance)
                return _rarityChances[i].Rarity;

            randomWeight -= chance;
        }

        return GearRarity.Common;
    }

    private bool TryGetRandomItemConfig(InventorySlotType? requiredSlotType, InventorySlotType? excludedSlotType, out GearVisualsConfig itemConfig, out GearRarity rarity)
    {
        int totalWeight = 0;

        for (int i = 0; i < _rarityChances.Length; i++)
        {
            int chance = Mathf.Max(0, _rarityChances[i].Chance);

            if (chance <= 0 || HasAvailableItemConfig(_rarityChances[i].Rarity, requiredSlotType, excludedSlotType) == false)
                continue;

            totalWeight += chance;
        }

        if (totalWeight <= 0)
        {
            return TryGetRandomAvailableItemConfig(requiredSlotType, excludedSlotType, out itemConfig, out rarity);
        }

        int randomWeight = UnityEngine.Random.Range(0, totalWeight);

        for (int i = 0; i < _rarityChances.Length; i++)
        {
            int chance = Mathf.Max(0, _rarityChances[i].Chance);

            if (chance <= 0 || HasAvailableItemConfig(_rarityChances[i].Rarity, requiredSlotType, excludedSlotType) == false)
                continue;

            if (randomWeight < chance)
            {
                rarity = _rarityChances[i].Rarity;
                return TryGetRandomItemConfig(rarity, requiredSlotType, excludedSlotType, out itemConfig);
            }

            randomWeight -= chance;
        }

        rarity = GearRarity.Common;
        return TryGetRandomItemConfig(rarity, requiredSlotType, excludedSlotType, out itemConfig);
    }

    private bool TryGetRandomAvailableItemConfig(InventorySlotType? requiredSlotType, InventorySlotType? excludedSlotType, out GearVisualsConfig itemConfig, out GearRarity rarity)
    {
        GearRarity[] rarities = (GearRarity[])Enum.GetValues(typeof(GearRarity));
        int availableRarityCount = 0;

        for (int i = 0; i < rarities.Length; i++)
        {
            if (HasAvailableItemConfig(rarities[i], requiredSlotType, excludedSlotType))
                availableRarityCount++;
        }

        if (availableRarityCount <= 0)
        {
            itemConfig = null;
            rarity = GearRarity.Common;
            return false;
        }

        int selectedIndex = UnityEngine.Random.Range(0, availableRarityCount);

        for (int i = 0; i < rarities.Length; i++)
        {
            if (HasAvailableItemConfig(rarities[i], requiredSlotType, excludedSlotType) == false)
                continue;

            if (selectedIndex == 0)
            {
                rarity = rarities[i];
                return TryGetRandomItemConfig(rarity, requiredSlotType, excludedSlotType, out itemConfig);
            }

            selectedIndex--;
        }

        itemConfig = null;
        rarity = GearRarity.Common;
        return false;
    }

    private bool HasAvailableItemConfig(GearRarity rarity, InventorySlotType? requiredSlotType, InventorySlotType? excludedSlotType)
    {
        for (int i = 0; i < _items.Length; i++)
        {
            if (IsItemConfigAvailable(_items[i], rarity, requiredSlotType, excludedSlotType))
                return true;
        }

        return false;
    }

    private bool TryGetRandomItemConfig(GearRarity rarity, InventorySlotType? requiredSlotType, InventorySlotType? excludedSlotType, out GearVisualsConfig itemConfig)
    {
        int availableCount = 0;

        for (int i = 0; i < _items.Length; i++)
        {
            if (IsItemConfigAvailable(_items[i], rarity, requiredSlotType, excludedSlotType))
                availableCount++;
        }

        if (availableCount <= 0)
        {
            itemConfig = null;
            return false;
        }

        int selectedIndex = UnityEngine.Random.Range(0, availableCount);

        for (int i = 0; i < _items.Length; i++)
        {
            if (IsItemConfigAvailable(_items[i], rarity, requiredSlotType, excludedSlotType) == false)
                continue;

            if (selectedIndex == 0)
            {
                itemConfig = _items[i];
                return true;
            }

            selectedIndex--;
        }

        itemConfig = null;
        return false;
    }

    private bool IsItemConfigAvailable(GearVisualsConfig itemConfig, GearRarity rarity, InventorySlotType? requiredSlotType, InventorySlotType? excludedSlotType)
    {
        if (itemConfig == null || itemConfig.TryGetVisual(rarity, out _) == false)
            return false;

        if (requiredSlotType.HasValue && itemConfig.SlotType != requiredSlotType.Value)
            return false;

        return excludedSlotType.HasValue == false || itemConfig.SlotType != excludedSlotType.Value;
    }
}

[Serializable]
public struct GearPackVisualEntry
{
    [SerializeField] private GearPackRarity _rarity;
    [SerializeField] private Sprite _icon;
    [SerializeField] private string _displayName;

    public GearPackRarity Rarity => _rarity;
    public Sprite Icon => _icon;
    public string DisplayName => string.IsNullOrEmpty(_displayName) ? $"{_rarity} Pack" : _displayName;
}

[Serializable]
public struct GearPackItemEntry
{
    public string ItemId;
    public InventorySlotType SlotType;
    public GearRarity Rarity;
}

[Serializable]
public struct GearRarityChance
{
    [SerializeField] private GearRarity _rarity;
    [SerializeField] private int _chance;

    public GearRarity Rarity => _rarity;
    public int Chance => _chance;
}

[Serializable]
public struct GearPackShopEntry
{
    [SerializeField] private GearPackRarity _rarity;
    [SerializeField] private int _softPrice;

    public GearPackRarity Rarity => _rarity;
    public int SoftPrice => _softPrice;
}
