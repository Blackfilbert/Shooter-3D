using System;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotView : MonoBehaviour
{
    [SerializeField] private InventorySlotType _slotType;
    [SerializeField] private Button _button;
    [SerializeField] private Transform _itemViewParent;
    [SerializeField] private InventoryItemView _itemViewPrefab;

    private Action<InventorySlotType> _clicked;
    private InventoryItemView _itemView;
    private bool _hasItem;

    public InventorySlotType SlotType => _slotType;

    private void Awake()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        if (_button != null)
            _button.onClick.AddListener(OnClicked);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnClicked);
    }

    public void Initialize(SaveManager.EquippedItemSaveData itemData, GearVisualEntry? visualEntry, Action<InventorySlotType> clicked)
    {
        _clicked = clicked;
        ClearItemView();

        _hasItem = itemData != null && string.IsNullOrEmpty(itemData.ItemId) == false;

        if (_hasItem == false)
            return;

        SpawnItemView(itemData, visualEntry);
    }

    private void SpawnItemView(SaveManager.EquippedItemSaveData itemData, GearVisualEntry? visualEntry)
    {
        if (_itemViewPrefab == null)
            return;

        Transform parent = _itemViewParent != null ? _itemViewParent : transform;
        _itemView = Instantiate(_itemViewPrefab, parent);
        _itemView.Initialize(-1, CreateInventoryItemData(itemData), visualEntry, _ => OnClicked(), InventoryManager.GetAvailableUpgradeCopiesForEquippedItem(_slotType) + 1);
    }

    private void ClearItemView()
    {
        Transform parent = _itemViewParent != null ? _itemViewParent : transform;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);

        _itemView = null;
    }

    private SaveManager.InventoryItemSaveData CreateInventoryItemData(SaveManager.EquippedItemSaveData itemData)
    {
        return new SaveManager.InventoryItemSaveData
        {
            ItemId = itemData.ItemId,
            SlotType = itemData.SlotType,
            Rarity = itemData.Rarity,
            Level = itemData.Level
        };
    }

    private void OnClicked()
    {
        if (_hasItem == false)
            return;

        _clicked?.Invoke(_slotType);
    }
}
