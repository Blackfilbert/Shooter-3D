using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrencyView : MonoBehaviour
{
    [SerializeField] private GeneralParameters _generalParameters;
    [SerializeField] private CurrencyType _currencyType;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private TMP_Text _energyTimerText;
    [SerializeField] private Image _icon;

    private float _nextEnergyTimerRefreshTime;
    private float _nextSpecialKeyRefreshTime;

    private void OnEnable()
    {
        CurrencyManager.CurrencyChanged += OnCurrencyChanged;
        UpdateView();
    }

    private void OnDisable()
    {
        CurrencyManager.CurrencyChanged -= OnCurrencyChanged;
    }

    private void Update()
    {
        if (SpecialKeyManager.IsSpecialKey(_currencyType) && Time.unscaledTime >= _nextSpecialKeyRefreshTime)
        {
            _nextSpecialKeyRefreshTime = Time.unscaledTime + 1f;
            UpdateView();
            return;
        }

        if (_currencyType != CurrencyType.Energy || Time.unscaledTime < _nextEnergyTimerRefreshTime)
            return;

        _nextEnergyTimerRefreshTime = Time.unscaledTime + 1f;
        UpdateView();
    }

    private void UpdateView()
    {
        if (_currencyType == CurrencyType.Energy && EnergyManager.IsUnlocked == false)
        {
            gameObject.SetActive(false);
            return;
        }

        UpdateCount(CurrencyManager.GetCount(_currencyType));

        if (_icon != null && _generalParameters != null && _generalParameters.TryGetCurrencyIcon(_currencyType, out Sprite icon))
            _icon.sprite = icon;
    }

    private void OnCurrencyChanged(CurrencyType currencyType, int count)
    {
        if (currencyType != _currencyType)
            return;

        UpdateCount(count);
    }

    private void UpdateCount(int count)
    {
        if (_countText != null)
        {
            if (_currencyType == CurrencyType.Energy)
                _countText.text = $"{count}/{EnergyManager.MaxEnergy}";
            else if (SpecialKeyManager.IsSpecialKey(_currencyType))
                _countText.text = $"{count}/{SpecialKeyManager.MaxKeys}";
            else
                _countText.text = count.ToString();
        }

        if (_currencyType == CurrencyType.Energy)
            UpdateEnergyTimer(count);
    }

    private void UpdateEnergyTimer(int count)
    {
        if (_energyTimerText == null)
            return;

        bool isVisible = count < EnergyManager.MaxEnergy;
        _energyTimerText.gameObject.SetActive(isVisible);

        if (isVisible == false)
            return;

        int secondsUntilNextRefill = EnergyManager.GetSecondsUntilNextRefill();
        int minutes = secondsUntilNextRefill / 60;
        int seconds = secondsUntilNextRefill % 60;
        _energyTimerText.text = $"{minutes}:{seconds:00}";
    }
}
