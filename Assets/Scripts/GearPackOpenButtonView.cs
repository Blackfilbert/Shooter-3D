using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GearPackOpenButtonView : MonoBehaviour
{
    [SerializeField] private GearPacksConfig _config;
    [SerializeField] private GearPackRarity _packRarity = GearPackRarity.Common;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private Button _openButton;

    private void Awake()
    {
        if (_openButton != null)
            _openButton.onClick.AddListener(OpenPack);
    }

    private void OnEnable()
    {
        GearPackManager.PackCountChanged += OnPackCountChanged;
        UpdateView();
    }

    private void OnDisable()
    {
        GearPackManager.PackCountChanged -= OnPackCountChanged;
    }

    private void OnDestroy()
    {
        if (_openButton != null)
            _openButton.onClick.RemoveListener(OpenPack);
    }

    public void OpenPack()
    {
        if (GearPackManager.OpenOwnedPack(_config, _packRarity, out GearPackReward reward) == false)
            return;

        UpdateView();
    }

    private void UpdateView()
    {
        int count = GearPackManager.GetPackCount(_packRarity);

        if (_countText != null)
            _countText.text = count.ToString();

        if (_openButton != null)
            _openButton.interactable = _config != null && count > 0;
    }

    private void OnPackCountChanged(GearPackRarity rarity, int count)
    {
        if (rarity == _packRarity)
            UpdateView();
    }
}
