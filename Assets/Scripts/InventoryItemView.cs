using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class InventoryItemView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _icon;
    [SerializeField] private Image _background;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _statsText;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private Slider _countSlider;
    [SerializeField] private Image _countSliderFill;
    [SerializeField] private Sprite _countSliderFillSprite;
    [SerializeField] private Sprite _countSliderReadyFillSprite;
    [SerializeField] private GameObject _alertObject;
    [SerializeField] private GameObject _upgradeAvailableObject;
    [SerializeField] private UnityEvent _clickedEvent;

    private int _itemIndex;
    private Action<int> _clicked;
    private bool _isClickEnabled;

    public int ItemIndex => _itemIndex;

    private void Awake()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        if (_button == null)
            _button = GetComponentInChildren<Button>();

        if (_button != null)
            _button.onClick.AddListener(OnButtonClicked);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnButtonClicked);
    }

    public void Initialize(int itemIndex, SaveManager.InventoryItemSaveData itemData, GearVisualEntry? visualEntry, Action<int> clicked, int count = 1, bool dimWhenNotClickable = true, bool showAlert = false)
    {
        _itemIndex = itemIndex;
        _clicked = clicked;
        _isClickEnabled = clicked != null;

        int availableCopies = Mathf.Max(0, count - 1);
        int requiredCopies = InventoryManager.GetRequiredCopiesForUpgrade(itemData.Level);

        SetInfo(itemData.Level, InventoryManager.GetItemStats(itemData).GearScore, count, visualEntry);
        SetButtonState(_isClickEnabled || dimWhenNotClickable == false);
        SetAlert(showAlert);
        SetUpgradeAvailable(CanUpgrade(itemData.Level, availableCopies, requiredCopies));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_button != null)
            return;

        OnClicked();
    }

    public void Click()
    {
        OnClicked();
    }

    public void SetInteractable(bool isInteractable)
    {
        SetButtonState(isInteractable);
    }

    private void SetInfo(int level, int gearScore, int count, GearVisualEntry? visualEntry)
    {
        if (_titleText != null)
            _titleText.text = $"level {level}";

        if (_statsText != null)
        {
            _statsText.richText = true;
            _statsText.text = $"<sprite=2> {gearScore}";
        }

        int availableCopies = Mathf.Max(0, count - 1);
        int requiredCopies = InventoryManager.GetRequiredCopiesForUpgrade(level);

        if (_countText != null)
            _countText.text = $"{availableCopies}/{requiredCopies}";

        if (_countSlider != null)
        {
            _countSlider.minValue = 0f;
            _countSlider.maxValue = requiredCopies;
            _countSlider.value = Mathf.Clamp(availableCopies, 0, requiredCopies);
        }

        SetCountSliderFill(availableCopies >= requiredCopies);

        if (visualEntry.HasValue)
        {
            GearVisualEntry visual = visualEntry.Value;

            if (_icon != null)
                _icon.sprite = visual.Icon;

            if (_background != null && visual.BackgroundSprite != null)
                _background.sprite = visual.BackgroundSprite;

            return;
        }

        if (_icon != null)
            _icon.sprite = null;
    }

    private void SetCountSliderFill(bool isReadyToUpgrade)
    {
        if (_countSliderFill == null)
            return;

        Sprite sprite = isReadyToUpgrade ? _countSliderReadyFillSprite : _countSliderFillSprite;

        if (sprite != null)
            _countSliderFill.sprite = sprite;
    }

    private void SetAlert(bool isActive)
    {
        if (_alertObject != null)
            _alertObject.SetActive(isActive);
    }

    private void SetUpgradeAvailable(bool isActive)
    {
        if (_upgradeAvailableObject != null)
            _upgradeAvailableObject.SetActive(isActive);
    }

    private bool CanUpgrade(int level, int availableCopies, int requiredCopies)
    {
        return availableCopies >= requiredCopies
            && CurrencyManager.GetCount(CurrencyType.Soft) >= InventoryManager.GetUpgradeSoftCost(level);
    }

    private void SetButtonState(bool isInteractable)
    {
        if (_button != null)
            _button.interactable = isInteractable;
    }

    private void OnClicked()
    {
        if (_isClickEnabled == false)
            return;

        if (_clicked == null)
        {
            _clickedEvent?.Invoke();
            return;
        }

        _clicked?.Invoke(_itemIndex);
        _clickedEvent?.Invoke();
    }

    private void OnButtonClicked()
    {
        OnClicked();
    }
}
