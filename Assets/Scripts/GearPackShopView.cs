using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GearPackShopView : MonoBehaviour
{
    [SerializeField] private GearPacksConfig _config;
    [SerializeField] private GearPackRarity _packRarity = GearPackRarity.Common;
    [SerializeField] private TMP_Text _priceText;
    [SerializeField] private Button _buyButton;

    private void Awake()
    {
        if (_buyButton != null)
            _buyButton.onClick.AddListener(BuyPack);
    }

    private void OnEnable()
    {
        CurrencyManager.CurrencyChanged += OnCurrencyChanged;
        UpdateView();
    }

    private void OnDisable()
    {
        CurrencyManager.CurrencyChanged -= OnCurrencyChanged;
    }

    private void OnDestroy()
    {
        if (_buyButton != null)
            _buyButton.onClick.RemoveListener(BuyPack);
    }

    public void BuyPack()
    {
        if (GearPackManager.BuyPack(_config, _packRarity, out GearPackReward reward) == false)
            return;

        UpdateView();
    }

    private void UpdateView()
    {
        int price = _config != null ? _config.GetShopPrice(_packRarity) : 0;

        if (_priceText != null)
            _priceText.text = price.ToString();

        if (_buyButton != null)
            _buyButton.interactable = _config != null && CurrencyManager.GetCount(CurrencyType.Soft) >= price;
    }

    private void OnCurrencyChanged(CurrencyType currencyType, int count)
    {
        if (currencyType == CurrencyType.Soft)
            UpdateView();
    }
}
