using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ButtonUnlockByLevelView : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _icon;
    [SerializeField] private Sprite _lockedIcon;
    [SerializeField] private Sprite _unlockedIcon;
    [SerializeField] private int _requiredLevelNumber = 10;

    private void Awake()
    {
        if (_button == null)
            _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.sKey.wasPressedThisFrame)
            CurrencyManager.Add(CurrencyType.SpecialKey, 1);

        if (keyboard.dKey.wasPressedThisFrame)
            CurrencyManager.Add(CurrencyType.SpecialKey2, 1);
    }

    public void Refresh()
    {
        bool isUnlocked = GetCurrentLevelNumber() >= Mathf.Max(1, _requiredLevelNumber);

        if (_button != null)
            _button.interactable = isUnlocked;

        if (_icon == null)
            return;

        Sprite targetIcon = isUnlocked ? _unlockedIcon : _lockedIcon;

        if (targetIcon != null)
            _icon.sprite = targetIcon;
    }

    private int GetCurrentLevelNumber()
    {
        return Mathf.Max(1, SaveManager.CompletedLevelIndex + 1);
    }
}
