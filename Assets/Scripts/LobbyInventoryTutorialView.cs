using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class LobbyInventoryTutorialView : MonoBehaviour
{
    private const int MaxWeaponSearchRetries = 3;

    [SerializeField] private LobbyMenuController _menuController;
    [SerializeField] private InventoryView _inventoryView;
    [SerializeField] private GameObject _afterLevelCompletedObject;
    [SerializeField] private GameObject _weaponFoundObject;
    [SerializeField] private Transform _weaponFoundItemsParent;
    [SerializeField] private GameObject _equipPromptObject;
    [SerializeField] private int _requiredCompletedLevelIndex = 9;

    private InventoryItemView _targetItemView;
    private Transform _targetOriginalParent;
    private int _targetOriginalSiblingIndex;
    private int _targetOriginalGridIndex;
    private GameObject _targetPlaceholder;
    private readonly List<GameObject> _weaponFoundPlaceholders = new List<GameObject>();
    private int _targetItemIndex = -1;
    private bool _isTargetItemReparented;
    private bool _isEquipPromptShown;
    private bool _isMenuSubscribed;
    private bool _isInventorySubscribed;
    private bool _isInventoryManagerSubscribed;
    private int _weaponSearchRetries;
    private Coroutine _refreshNextFrameCoroutine;

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
        Refresh();
    }

    private void Start()
    {
        ResolveReferences();
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
        StopRefreshNextFrame();
        ClearTargetItem();
        SetObject(_afterLevelCompletedObject, false);
        SetObject(_weaponFoundObject, false);
        SetObject(_equipPromptObject, false);
    }

    private void ResolveReferences()
    {
        if (_menuController == null)
            _menuController = Global.LobbyMenuController;
    }

    private void Subscribe()
    {
        if (_menuController != null && _isMenuSubscribed == false)
        {
            _menuController.ActiveButtonChanged += OnActiveMenuChanged;
            _isMenuSubscribed = true;
        }

        if (_inventoryView != null && _isInventorySubscribed == false)
        {
            _inventoryView.InventoryItemSelected += OnInventoryItemSelected;
            _isInventorySubscribed = true;
        }

        if (_isInventoryManagerSubscribed)
            return;

        InventoryManager.InventoryChanged += OnInventoryChanged;
        InventoryManager.EquipmentChanged += OnEquipmentChanged;
        _isInventoryManagerSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (_menuController != null && _isMenuSubscribed)
        {
            _menuController.ActiveButtonChanged -= OnActiveMenuChanged;
            _isMenuSubscribed = false;
        }

        if (_inventoryView != null && _isInventorySubscribed)
        {
            _inventoryView.InventoryItemSelected -= OnInventoryItemSelected;
            _isInventorySubscribed = false;
        }

        if (_isInventoryManagerSubscribed)
        {
            InventoryManager.InventoryChanged -= OnInventoryChanged;
            InventoryManager.EquipmentChanged -= OnEquipmentChanged;
            _isInventoryManagerSubscribed = false;
        }
    }

    private void Refresh()
    {
        if (IsTutorialAvailable() == false)
        {
            HideAll();
            return;
        }

        if (IsInventoryOpen() == false)
        {
            _weaponSearchRetries = 0;
            ShowAfterLevelCompletedStep();
            return;
        }

        if (_isEquipPromptShown)
        {
            ShowEquipPromptStep();
            return;
        }

        ShowWeaponFoundStep();
    }

    private bool IsTutorialAvailable()
    {
        return SaveManager.CompletedLevelIndex >= _requiredCompletedLevelIndex
            && SaveManager.InventoryEquipTutorialCompleted == false;
    }

    private bool IsInventoryOpen()
    {
        return _inventoryView != null && _inventoryView.gameObject.activeInHierarchy;
    }

    private void ShowAfterLevelCompletedStep()
    {
        ClearTargetItem();
        SetObject(_afterLevelCompletedObject, true);
        SetObject(_weaponFoundObject, false);
        SetObject(_equipPromptObject, false);
    }

    private void ShowWeaponFoundStep()
    {
        SetObject(_afterLevelCompletedObject, false);
        SetObject(_equipPromptObject, false);

        if (IsInventoryOpen() == false || _inventoryView == null)
        {
            ClearTargetItem();
            SetObject(_weaponFoundObject, false);
            return;
        }

        if (_inventoryView.TryGetBestUnequippedWeaponView(out InventoryItemView itemView, out int itemIndex) == false)
        {
            ClearTargetItem();
            SetObject(_weaponFoundObject, false);
            RetryWeaponSearchOrComplete();
            return;
        }

        if (SetTargetItem(itemView, itemIndex) == false)
        {
            ClearTargetItem();
            SetObject(_weaponFoundObject, false);
            RetryWeaponSearchOrComplete();
            return;
        }

        _weaponSearchRetries = 0;
        SetObject(_weaponFoundObject, true);
    }

    private void ShowEquipPromptStep()
    {
        ClearTargetItem();
        SetObject(_afterLevelCompletedObject, false);
        SetObject(_weaponFoundObject, false);
        SetObject(_equipPromptObject, true);
    }

    private void HideAll()
    {
        _weaponSearchRetries = 0;
        ClearTargetItem();
        SetObject(_afterLevelCompletedObject, false);
        SetObject(_weaponFoundObject, false);
        SetObject(_equipPromptObject, false);
    }

    private bool SetTargetItem(InventoryItemView itemView, int itemIndex)
    {
        if (_targetItemView != itemView)
            ClearTargetItem();

        _targetItemView = itemView;
        _targetItemIndex = itemIndex;

        return _targetItemView != null && ReparentTargetItemToWeaponFoundObject();
    }

    private void ClearTargetItem()
    {
        RestoreTargetItemParent();

        _targetItemView = null;
        _targetItemIndex = -1;
    }

    private bool ReparentTargetItemToWeaponFoundObject()
    {
        if (_targetItemView == null || _weaponFoundObject == null || _isTargetItemReparented)
            return _isTargetItemReparented;

        Transform weaponFoundItemsParent = GetWeaponFoundItemsParent();

        if (weaponFoundItemsParent == null)
            return false;

        Transform targetTransform = _targetItemView.transform;
        _targetOriginalParent = targetTransform.parent;
        _targetOriginalSiblingIndex = targetTransform.GetSiblingIndex();
        _targetOriginalGridIndex = GetTargetInventoryViewIndex();
        CreateTargetPlaceholder();
        CreateWeaponFoundPlaceholders(_targetOriginalGridIndex);

        targetTransform.SetParent(weaponFoundItemsParent, false);
        targetTransform.SetSiblingIndex(_weaponFoundPlaceholders.Count);
        _isTargetItemReparented = true;
        return true;
    }

    private void RestoreTargetItemParent()
    {
        if (_targetItemView == null || _isTargetItemReparented == false)
            return;

        Transform targetTransform = _targetItemView.transform;

        if (_targetOriginalParent != null)
        {
            targetTransform.SetParent(_targetOriginalParent, false);
            targetTransform.SetSiblingIndex(Mathf.Clamp(_targetOriginalSiblingIndex, 0, _targetOriginalParent.childCount - 1));
        }

        DestroyTargetPlaceholder();
        DestroyWeaponFoundPlaceholders();
        _targetOriginalParent = null;
        _targetOriginalSiblingIndex = 0;
        _targetOriginalGridIndex = 0;
        _isTargetItemReparented = false;
    }

    private void CreateTargetPlaceholder()
    {
        if (_targetOriginalParent == null || _targetPlaceholder != null)
            return;

        _targetPlaceholder = CreatePlaceholder("InventoryTutorialItemPlaceholder", _targetOriginalParent);
        _targetPlaceholder.transform.SetSiblingIndex(_targetOriginalSiblingIndex);
    }

    private GameObject CreatePlaceholder(string objectName, Transform parent)
    {
        GameObject placeholder = new GameObject(objectName, typeof(RectTransform));
        RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
        placeholderRect.SetParent(parent, false);
        return placeholder;
    }

    private void DestroyTargetPlaceholder()
    {
        if (_targetPlaceholder == null)
            return;

        _targetPlaceholder.SetActive(false);
        Destroy(_targetPlaceholder);
        _targetPlaceholder = null;
    }

    private void CreateWeaponFoundPlaceholders(int count)
    {
        Transform parent = GetWeaponFoundItemsParent();

        if (parent == null)
            return;

        DestroyWeaponFoundPlaceholders();

        for (int i = 0; i < count; i++)
        {
            GameObject placeholder = CreatePlaceholder("InventoryTutorialWeaponFoundPlaceholder", parent);
            placeholder.transform.SetSiblingIndex(i);
            _weaponFoundPlaceholders.Add(placeholder);
        }
    }

    private Transform GetWeaponFoundItemsParent()
    {
        if (_weaponFoundItemsParent != null)
            return _weaponFoundItemsParent;

        return _weaponFoundObject != null ? _weaponFoundObject.transform : null;
    }

    private int GetTargetInventoryViewIndex()
    {
        if (_inventoryView != null && _inventoryView.TryGetSpawnedItemViewIndex(_targetItemView, out int viewIndex))
            return viewIndex;

        return 0;
    }

    private void DestroyWeaponFoundPlaceholders()
    {
        for (int i = 0; i < _weaponFoundPlaceholders.Count; i++)
        {
            if (_weaponFoundPlaceholders[i] != null)
                _weaponFoundPlaceholders[i].SetActive(false);

            Destroy(_weaponFoundPlaceholders[i]);
        }

        _weaponFoundPlaceholders.Clear();
    }

    private void CompleteTutorial()
    {
        SaveManager.CompleteInventoryEquipTutorial();
        HideAll();
    }

    private void OnActiveMenuChanged(LobbyButtonView button)
    {
        Refresh();
    }

    private void OnInventoryChanged()
    {
        Refresh();
    }

    private void OnEquipmentChanged(InventorySlotType slotType, string itemId)
    {
        if (slotType == InventorySlotType.Weapon
            && IsTutorialAvailable()
            && _isEquipPromptShown)
        {
            CompleteTutorial();
            return;
        }

        Refresh();
    }

    private void OnInventoryItemSelected(int itemIndex, InventoryItemView itemView)
    {
        if (IsTutorialAvailable() == false || IsInventoryOpen() == false)
            return;

        if (itemIndex != _targetItemIndex)
            return;

        _isEquipPromptShown = true;
        Refresh();
    }

    private void SetObject(GameObject target, bool isActive)
    {
        if (target != null)
            target.SetActive(isActive);
    }

    private void RefreshNextFrame()
    {
        if (_refreshNextFrameCoroutine != null || isActiveAndEnabled == false)
            return;

        _refreshNextFrameCoroutine = StartCoroutine(RefreshNextFrameRoutine());
    }

    private IEnumerator RefreshNextFrameRoutine()
    {
        yield return null;
        _refreshNextFrameCoroutine = null;
        Refresh();
    }

    private void StopRefreshNextFrame()
    {
        if (_refreshNextFrameCoroutine == null)
            return;

        StopCoroutine(_refreshNextFrameCoroutine);
        _refreshNextFrameCoroutine = null;
    }

    private void RetryWeaponSearchOrComplete()
    {
        _weaponSearchRetries++;

        if (_weaponSearchRetries <= MaxWeaponSearchRetries)
        {
            RefreshNextFrame();
            return;
        }

        CompleteTutorial();
    }
}
