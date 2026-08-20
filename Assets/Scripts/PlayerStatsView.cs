using TMPro;
using UnityEngine;

public class PlayerStatsView : MonoBehaviour
{
    [SerializeField] private TMP_Text _damageText;
    [SerializeField] private TMP_Text _healthText;
    [SerializeField] private TMP_Text _accuracyText;
    [SerializeField] private int _baseDamage = 10;
    [SerializeField] private int _baseHealth = 100;
    [SerializeField] private int _baseAccuracy = 10;

    private PlayerWeapon _playerWeapon;
    private PlayerHealth _playerHealth;

    private void OnEnable()
    {
        InventoryManager.InventoryChanged += UpdateView;
        InventoryManager.EquipmentChanged += OnEquipmentChanged;
        ProfileManager.ProfileChanged += UpdateView;
        SubscribeRuntimeStats();
        UpdateView();
    }

    private void Update()
    {
        SubscribeRuntimeStats();
    }

    private void OnDisable()
    {
        InventoryManager.InventoryChanged -= UpdateView;
        InventoryManager.EquipmentChanged -= OnEquipmentChanged;
        ProfileManager.ProfileChanged -= UpdateView;
        UnsubscribeRuntimeStats();
    }

    private void SubscribeRuntimeStats()
    {
        if (_playerWeapon == null && Global.PlayerWeapon != null)
        {
            _playerWeapon = Global.PlayerWeapon;
            _playerWeapon.DamageChanged += OnDamageChanged;
            UpdateView();
        }

        if (_playerHealth == null && Global.PlayerHealth != null)
        {
            _playerHealth = Global.PlayerHealth;
            _playerHealth.HealthChanged += OnHealthChanged;
            UpdateView();
        }
    }

    private void UnsubscribeRuntimeStats()
    {
        if (_playerWeapon != null)
            _playerWeapon.DamageChanged -= OnDamageChanged;

        if (_playerHealth != null)
            _playerHealth.HealthChanged -= OnHealthChanged;

        _playerWeapon = null;
        _playerHealth = null;
    }

    private void UpdateView()
    {
        InventoryItemStats stats = InventoryManager.GetTotalEquippedStats();

        if (_damageText != null)
            _damageText.text = GetDamage(stats).ToString();

        if (_healthText != null)
            _healthText.text = GetHealth(stats).ToString();

        if (_accuracyText != null)
            _accuracyText.text = (_baseAccuracy + stats.AimStability).ToString();
    }

    private int GetDamage(InventoryItemStats stats)
    {
        return _playerWeapon != null ? _playerWeapon.Damage : Mathf.Max(1, _baseDamage + stats.Damage);
    }

    private int GetHealth(InventoryItemStats stats)
    {
        return _playerHealth != null ? _playerHealth.MaxHealth : Mathf.Max(1, _baseHealth + stats.Health);
    }

    private void OnEquipmentChanged(InventorySlotType slotType, string itemId)
    {
        UpdateView();
    }

    private void OnDamageChanged(int damage)
    {
        UpdateView();
    }

    private void OnHealthChanged(int health, int maxHealth)
    {
        UpdateView();
    }
}
