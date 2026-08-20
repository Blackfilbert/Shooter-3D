using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryView : MonoBehaviour
{
    [SerializeField] private GearVisualsConfig[] _gearVisualsConfigs;
    [SerializeField] private InventorySlotView[] _slotViews;
    [SerializeField] private Transform _itemsParent;
    [SerializeField] private InventoryItemView _itemViewPrefab;

    private readonly List<int> _spawnedItemIndexes = new List<int>();
    private readonly List<InventoryItemView> _spawnedItemViews = new List<InventoryItemView>();

    public event Action<int, InventoryItemView> InventoryItemSelected;

    private void OnEnable()
    {
        InventoryManager.InventoryChanged += Refresh;
        InventoryManager.EquipmentChanged += OnEquipmentChanged;
        CurrencyManager.CurrencyChanged += OnCurrencyChanged;
        Refresh();
    }

    private void OnDisable()
    {
        InventoryManager.InventoryChanged -= Refresh;
        InventoryManager.EquipmentChanged -= OnEquipmentChanged;
        CurrencyManager.CurrencyChanged -= OnCurrencyChanged;
    }

    public void Refresh()
    {
        RefreshSlots();
        RefreshItems();
    }

    public bool TryGetBestUnequippedWeaponView(out InventoryItemView itemView, out int itemIndex)
    {
        itemView = null;
        itemIndex = -1;

        if (InventoryManager.TryGetBestInventoryItemForSlot(InventorySlotType.Weapon, out int bestItemIndex) == false)
            return false;

        for (int i = 0; i < _spawnedItemIndexes.Count; i++)
        {
            if (_spawnedItemIndexes[i] != bestItemIndex)
                continue;

            itemView = _spawnedItemViews[i];
            itemIndex = bestItemIndex;
            return itemView != null;
        }

        return false;
    }

    public bool TryGetSpawnedItemViewIndex(InventoryItemView itemView, out int viewIndex)
    {
        viewIndex = -1;

        if (itemView == null)
            return false;

        for (int i = 0; i < _spawnedItemViews.Count; i++)
        {
            if (_spawnedItemViews[i] != itemView)
                continue;

            viewIndex = i;
            return true;
        }

        return false;
    }

    private void RefreshSlots()
    {
        if (_slotViews == null)
            return;

        for (int i = 0; i < _slotViews.Length; i++)
        {
            if (_slotViews[i] == null)
                continue;

            InventoryManager.TryGetEquippedItemData(_slotViews[i].SlotType, out SaveManager.EquippedItemSaveData itemData);
            _slotViews[i].Initialize(itemData, GetVisual(itemData), OpenEquippedItemPopup);
        }
    }

    private void RefreshItems()
    {
        _spawnedItemIndexes.Clear();
        _spawnedItemViews.Clear();

        if (_itemsParent == null || _itemViewPrefab == null)
            return;

        for (int i = _itemsParent.childCount - 1; i >= 0; i--)
            Destroy(_itemsParent.GetChild(i).gameObject);

        SaveManager.InventoryItemSaveData[] items = InventoryManager.Items;
        InventoryItemGroup[] groups = Array.Empty<InventoryItemGroup>();

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;

            if (IsEquippedItem(items[i]))
                continue;

            int groupIndex = FindGroupIndex(groups, items[i]);

            if (groupIndex >= 0)
            {
                groups[groupIndex].Count++;
                continue;
            }

            int length = groups.Length;
            Array.Resize(ref groups, length + 1);
            groups[length] = new InventoryItemGroup
            {
                ItemIndex = i,
                ItemData = items[i],
                Count = 1
            };
        }

        for (int i = 0; i < groups.Length; i++)
        {
            InventoryItemView itemView = Instantiate(_itemViewPrefab, _itemsParent);
            bool showAlert = InventoryManager.IsBestUnequippedItemForSlot(groups[i].ItemIndex);
            itemView.Initialize(groups[i].ItemIndex, groups[i].ItemData, GetVisual(groups[i].ItemData), OpenInventoryItemPopup, groups[i].Count, true, showAlert);
            _spawnedItemIndexes.Add(groups[i].ItemIndex);
            _spawnedItemViews.Add(itemView);
        }
    }

    private void OpenInventoryItemPopup(int itemIndex)
    {
        if (InventoryManager.TryGetInventoryItem(itemIndex, out SaveManager.InventoryItemSaveData itemData) == false)
            return;

        EquipPopup popup = Global.UIController != null ? Global.UIController.Show<EquipPopup>() : null;
        if (popup == null)
            return;

        popup.OpenForInventoryItem(itemIndex, itemData, GetVisual(itemData));
        InventoryItemSelected?.Invoke(itemIndex, GetSpawnedItemView(itemIndex));
    }

    private void OpenEquippedItemPopup(InventorySlotType slotType)
    {
        if (InventoryManager.TryGetEquippedItemData(slotType, out SaveManager.EquippedItemSaveData itemData) == false)
            return;

        EquipPopup popup = Global.UIController != null ? Global.UIController.Show<EquipPopup>() : null;
        if (popup == null)
            return;

        popup.OpenForEquippedItem(itemData, GetVisual(itemData));
    }

    private GearVisualEntry? GetVisual(SaveManager.InventoryItemSaveData itemData)
    {
        if (itemData == null)
            return null;

        GearVisualsConfig config = GetVisualsConfig(itemData.ItemId, itemData.SlotType);

        if (config != null && config.TryGetVisual(itemData.Rarity, out GearVisualEntry visualEntry))
            return visualEntry;

        return null;
    }

    private GearVisualEntry? GetVisual(SaveManager.EquippedItemSaveData itemData)
    {
        if (itemData == null || string.IsNullOrEmpty(itemData.ItemId))
            return null;

        GearVisualsConfig config = GetVisualsConfig(itemData.ItemId, itemData.SlotType);

        if (config != null && config.TryGetVisual(itemData.Rarity, out GearVisualEntry visualEntry))
            return visualEntry;

        return null;
    }

    private GearVisualsConfig GetVisualsConfig(string itemId, InventorySlotType slotType)
    {
        if (_gearVisualsConfigs == null)
            return null;

        for (int i = 0; i < _gearVisualsConfigs.Length; i++)
        {
            if (_gearVisualsConfigs[i] == null)
                continue;

            if (_gearVisualsConfigs[i].ItemId == itemId && _gearVisualsConfigs[i].SlotType == slotType)
                return _gearVisualsConfigs[i];
        }

        return null;
    }

    private void OnEquipmentChanged(InventorySlotType slotType, string itemId)
    {
        Refresh();
    }

    private void OnCurrencyChanged(CurrencyType currencyType, int count)
    {
        if (currencyType == CurrencyType.Soft)
            Refresh();
    }

    private int FindGroupIndex(InventoryItemGroup[] groups, SaveManager.InventoryItemSaveData itemData)
    {
        for (int i = 0; i < groups.Length; i++)
        {
            if (IsSameGroup(groups[i].ItemData, itemData))
                return i;
        }

        return -1;
    }

    private InventoryItemView GetSpawnedItemView(int itemIndex)
    {
        for (int i = 0; i < _spawnedItemIndexes.Count; i++)
        {
            if (_spawnedItemIndexes[i] == itemIndex)
                return _spawnedItemViews[i];
        }

        return null;
    }

    private bool IsSameGroup(SaveManager.InventoryItemSaveData firstItemData, SaveManager.InventoryItemSaveData secondItemData)
    {
        return firstItemData != null
            && secondItemData != null
            && firstItemData.ItemId == secondItemData.ItemId
            && firstItemData.SlotType == secondItemData.SlotType
            && firstItemData.Rarity == secondItemData.Rarity
            && firstItemData.Level == secondItemData.Level;
    }

    private bool IsEquippedItem(SaveManager.InventoryItemSaveData itemData)
    {
        SaveManager.EquippedItemSaveData[] equippedItems = InventoryManager.EquippedItems;

        for (int i = 0; i < equippedItems.Length; i++)
        {
            if (equippedItems[i] == null || string.IsNullOrEmpty(equippedItems[i].ItemId))
                continue;

            if (equippedItems[i].ItemId == itemData.ItemId
                && equippedItems[i].SlotType == itemData.SlotType
                && equippedItems[i].Rarity == itemData.Rarity)
                return true;
        }

        return false;
    }

    private struct InventoryItemGroup
    {
        public int ItemIndex;
        public int Count;
        public SaveManager.InventoryItemSaveData ItemData;
    }
}
