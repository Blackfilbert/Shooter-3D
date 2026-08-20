using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class EquipPopup : UIPopup
{
    [SerializeField] private InventoryItemView _itemView;
    [SerializeField] private TMP_Text _itemNameText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _rarityText;
    [SerializeField] private TMP_Text _slotText;
    [SerializeField] private Image _slotIcon;
    [SerializeField] private SlotIconEntry[] _slotIcons = System.Array.Empty<SlotIconEntry>();
    [SerializeField] private Color _commonRarityColor = Color.green;
    [SerializeField] private Color _uncommonRarityColor = Color.blue;
    [SerializeField] private TMP_Text _statTitle;
    [SerializeField] private Image _statIcon;
    [SerializeField] private TMP_Text _statValue;
    [SerializeField] private TMP_Text _statPlusValue;
    [SerializeField] private Sprite _damageStatSprite;
    [SerializeField] private Sprite _healthStatSprite;
    [SerializeField] private TMP_Text _upgradeText;
    [SerializeField] private Button _equipButton;
    [SerializeField] private Button _unequipButton;
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private Button _closeButton;

    private int _itemIndex;
    private InventorySlotType _slotType;
    private GearVisualEntry? _visualEntry;
    private bool _isEquippedItem;

    protected override void Awake()
    {
        base.Awake();

        if (_equipButton != null)
            _equipButton.onClick.AddListener(Equip);

        if (_unequipButton != null)
            _unequipButton.onClick.AddListener(Unequip);

        if (_upgradeButton != null)
            _upgradeButton.onClick.AddListener(Upgrade);

        if (_closeButton != null)
            _closeButton.onClick.AddListener(Hide);
    }

    protected override void OnDestroy()
    {
        if (_equipButton != null)
            _equipButton.onClick.RemoveListener(Equip);

        if (_unequipButton != null)
            _unequipButton.onClick.RemoveListener(Unequip);

        if (_upgradeButton != null)
            _upgradeButton.onClick.RemoveListener(Upgrade);

        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(Hide);

        base.OnDestroy();
    }

    public void OpenForInventoryItem(int itemIndex, SaveManager.InventoryItemSaveData itemData, GearVisualEntry? visualEntry)
    {
        _itemIndex = itemIndex;
        _slotType = itemData.SlotType;
        _visualEntry = visualEntry;
        _isEquippedItem = false;
        RefreshInventoryItemView(itemData);
        Show();
    }

    public void OpenForEquippedItem(SaveManager.EquippedItemSaveData itemData, GearVisualEntry? visualEntry)
    {
        _itemIndex = -1;
        _slotType = itemData.SlotType;
        _visualEntry = visualEntry;
        _isEquippedItem = true;
        RefreshEquippedItemView(itemData);
        Show();
    }

    private void RefreshInventoryItemView(SaveManager.InventoryItemSaveData itemData)
    {
        int availableCopies = InventoryManager.GetAvailableUpgradeCopiesForItemAt(_itemIndex);

        if (_itemView != null)
            _itemView.Initialize(_itemIndex, itemData, _visualEntry, null, availableCopies + 1, false);

        SetInfo(itemData, _visualEntry);
        SetStats(itemData);
        SetActions(true, false, InventoryManager.CanUpgradeItemAt(_itemIndex), itemData.Level);
    }

    private void RefreshEquippedItemView(SaveManager.EquippedItemSaveData itemData)
    {
        SaveManager.InventoryItemSaveData inventoryItemData = CreateInventoryItemData(itemData);
        int availableCopies = InventoryManager.GetAvailableUpgradeCopiesForEquippedItem(itemData.SlotType);

        if (_itemView != null)
            _itemView.Initialize(-1, inventoryItemData, _visualEntry, null, availableCopies + 1, false);

        SetInfo(inventoryItemData, _visualEntry);
        SetStats(inventoryItemData);
        SetActions(false, true, InventoryManager.CanUpgradeEquippedItem(itemData.SlotType), itemData.Level);
    }

    private void SetStats(SaveManager.InventoryItemSaveData itemData)
    {
        InventoryItemStats currentStats = InventoryManager.GetItemStats(itemData);
        InventoryItemStats upgradeStats = InventoryManager.GetItemStats(CreateUpgradePreviewData(itemData));
        bool isWeapon = itemData.SlotType == InventorySlotType.Weapon;
        string title = isWeapon ? "Damage" : "Health";
        Sprite icon = isWeapon ? _damageStatSprite : _healthStatSprite;
        int currentValue = isWeapon ? currentStats.Damage : currentStats.Health;
        int upgradeValue = isWeapon ? upgradeStats.Damage : upgradeStats.Health;
        int upgradeDelta = Mathf.Max(0, upgradeValue - currentValue);

        if (_statTitle != null)
            _statTitle.text = title;

        if (_statIcon != null)
        {
            _statIcon.sprite = icon;
            _statIcon.enabled = icon != null;
        }

        if (_statValue != null)
            _statValue.text = currentValue.ToString();

        if (_statPlusValue != null)
            _statPlusValue.text = $"+{upgradeDelta}";
    }

    private void SetInfo(SaveManager.InventoryItemSaveData itemData, GearVisualEntry? visualEntry)
    {
        string itemName = visualEntry.HasValue && string.IsNullOrEmpty(visualEntry.Value.DisplayName) == false
            ? visualEntry.Value.DisplayName
            : itemData.ItemId;
        string description = visualEntry.HasValue ? visualEntry.Value.Description : string.Empty;

        if (_itemNameText != null)
            _itemNameText.text = itemName;

        if (_descriptionText != null)
            _descriptionText.text = description;

        if (_rarityText != null)
        {
            _rarityText.text = itemData.Rarity.ToString();
            _rarityText.color = GetRarityColor(itemData.Rarity);
        }

        if (_slotText != null)
            _slotText.text = itemData.SlotType.ToString();

        if (_slotIcon != null)
        {
            Sprite icon = GetSlotIcon(itemData.SlotType);
            _slotIcon.sprite = icon;
            _slotIcon.enabled = icon != null;
        }
    }

    private Color GetRarityColor(GearRarity rarity)
    {
        switch (rarity)
        {
            case GearRarity.Uncommon:
                return _uncommonRarityColor;
            case GearRarity.Common:
            default:
                return _commonRarityColor;
        }
    }

    private Sprite GetSlotIcon(InventorySlotType slotType)
    {
        for (int i = 0; i < _slotIcons.Length; i++)
        {
            if (_slotIcons[i].SlotType == slotType)
                return _slotIcons[i].Icon;
        }

        return null;
    }

    private SaveManager.InventoryItemSaveData CreateUpgradePreviewData(SaveManager.InventoryItemSaveData itemData)
    {
        return new SaveManager.InventoryItemSaveData
        {
            ItemId = itemData.ItemId,
            SlotType = itemData.SlotType,
            Rarity = itemData.Rarity,
            Level = itemData.Level + 1
        };
    }

    private void SetActions(bool canEquip, bool canUnequip, bool canUpgrade, int level)
    {
        int softCost = InventoryManager.GetUpgradeSoftCost(level);

        if (_equipButton != null)
            _equipButton.gameObject.SetActive(canEquip);

        if (_unequipButton != null)
            _unequipButton.gameObject.SetActive(canUnequip);

        if (_upgradeButton != null)
            _upgradeButton.interactable = canUpgrade;

        if (_upgradeText != null)
        {
            _upgradeText.richText = true;
            _upgradeText.text = $"Upgrade\n<sprite=1>{softCost}";
        }
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

    private void Equip()
    {
        if (InventoryManager.EquipAt(_itemIndex))
            Hide();
    }

    private void Unequip()
    {
        if (InventoryManager.Unequip(_slotType))
            Hide();
    }

    private void Upgrade()
    {
        SaveManager.InventoryItemSaveData inventoryItemData = null;

        if (_isEquippedItem == false)
            InventoryManager.TryGetInventoryItem(_itemIndex, out inventoryItemData);

        bool upgraded = _isEquippedItem
            ? InventoryManager.UpgradeEquippedItem(_slotType)
            : InventoryManager.UpgradeItemAt(_itemIndex);

        if (upgraded == false)
            return;

        if (_isEquippedItem == false && inventoryItemData != null)
        {
            int itemIndex = FindInventoryItemIndex(inventoryItemData);

            if (itemIndex >= 0)
                _itemIndex = itemIndex;

            RefreshInventoryItemView(inventoryItemData);
            return;
        }

        RefreshCurrentItemView();
    }

    private void RefreshCurrentItemView()
    {
        if (_isEquippedItem)
        {
            if (InventoryManager.TryGetEquippedItemData(_slotType, out SaveManager.EquippedItemSaveData equippedItemData))
                RefreshEquippedItemView(equippedItemData);

            return;
        }

        if (InventoryManager.TryGetInventoryItem(_itemIndex, out SaveManager.InventoryItemSaveData itemData))
            RefreshInventoryItemView(itemData);
    }

    private int FindInventoryItemIndex(SaveManager.InventoryItemSaveData itemData)
    {
        SaveManager.InventoryItemSaveData[] items = InventoryManager.Items;

        for (int i = 0; i < items.Length; i++)
        {
            if (ReferenceEquals(items[i], itemData))
                return i;
        }

        return -1;
    }

    [System.Serializable]
    private struct SlotIconEntry
    {
        [SerializeField] private InventorySlotType _slotType;
        [SerializeField] private Sprite _icon;

        public InventorySlotType SlotType => _slotType;
        public Sprite Icon => _icon;
    }
}
