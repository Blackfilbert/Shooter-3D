using UnityEngine;

public class PopupManager : MonoBehaviour
{
    [SerializeField] private RectTransform _popupParent;
    [SerializeField] private GearPackOpeningPopup _gearPackOpeningPopupPrefab;
    [SerializeField] private EquipPopup _equipPopupPrefab;

    private GearPackOpeningPopup _gearPackOpeningPopup;
    private EquipPopup _equipPopup;

    private void Awake()
    {
        if (_popupParent == null)
            _popupParent = transform as RectTransform;

        Global.RegisterPopupManager(this);
    }

    private void OnDestroy()
    {
        Global.UnregisterPopupManager(this);
    }

    public bool OpenGearPack(GearPackReward reward)
    {
        GearPackOpeningPopup popup = GetGearPackOpeningPopup();

        if (popup == null)
            return false;

        popup.Open(reward);
        return true;
    }

    public bool OpenEquipPopup(int itemIndex, SaveManager.InventoryItemSaveData itemData, GearVisualEntry? visualEntry)
    {
        EquipPopup popup = GetEquipPopup();

        if (popup == null)
            return false;

        popup.OpenForInventoryItem(itemIndex, itemData, visualEntry);
        return true;
    }

    public bool OpenEquipPopup(SaveManager.EquippedItemSaveData itemData, GearVisualEntry? visualEntry)
    {
        EquipPopup popup = GetEquipPopup();

        if (popup == null)
            return false;

        popup.OpenForEquippedItem(itemData, visualEntry);
        return true;
    }

    private GearPackOpeningPopup GetGearPackOpeningPopup()
    {
        if (_gearPackOpeningPopup != null)
            return _gearPackOpeningPopup;

        if (_gearPackOpeningPopupPrefab == null || _popupParent == null)
            return null;

        _gearPackOpeningPopup = Instantiate(_gearPackOpeningPopupPrefab, _popupParent);
        return _gearPackOpeningPopup;
    }

    private EquipPopup GetEquipPopup()
    {
        if (_equipPopup != null)
            return _equipPopup;

        if (_equipPopupPrefab == null || _popupParent == null)
            return null;

        _equipPopup = Instantiate(_equipPopupPrefab, _popupParent);
        return _equipPopup;
    }
}
