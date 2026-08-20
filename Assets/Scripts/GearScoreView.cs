using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GearScoreView : MonoBehaviour
{
    [SerializeField] private TMP_Text _scoreText;

    private void OnEnable()
    {
        InventoryManager.InventoryChanged += UpdateView;
        InventoryManager.EquipmentChanged += OnEquipmentChanged;
        ProfileManager.ProfileChanged += UpdateView;
        UpdateView();
    }

    private void OnDisable()
    {
        InventoryManager.InventoryChanged -= UpdateView;
        InventoryManager.EquipmentChanged -= OnEquipmentChanged;
        ProfileManager.ProfileChanged -= UpdateView;
    }

    private void UpdateView()
    {
        if (_scoreText != null)
            _scoreText.text = InventoryManager.GetTotalEquippedStats().GearScore.ToString();
    }

    private void OnEquipmentChanged(InventorySlotType slotType, string itemId)
    {
        UpdateView();
    }
}
