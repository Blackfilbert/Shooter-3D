using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthView : MonoBehaviour
{
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private TMP_Text _healthText;
    [SerializeField] private Slider _healthSlider;

    private bool _isSubscribed;

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        Subscribe();
    }

    private void Update()
    {
        if (_isSubscribed == false)
            Subscribe();
    }

    private void OnDisable()
    {
        if (_playerHealth != null && _isSubscribed)
            _playerHealth.HealthChanged -= UpdateView;

        _isSubscribed = false;
    }

    private void UpdateView(int health, int maxHealth)
    {
        if (_healthText != null)
            _healthText.text = $"{health}/{maxHealth}";

        if (_healthSlider != null)
        {
            _healthSlider.minValue = 0f;
            _healthSlider.maxValue = maxHealth;
            _healthSlider.value = health;
        }
    }

    private void Subscribe()
    {
        if (_isSubscribed)
            return;

        if (_playerHealth == null)
            _playerHealth = Global.PlayerHealth;

        if (_playerHealth == null)
            return;

        _playerHealth.HealthChanged += UpdateView;
        _isSubscribed = true;
        UpdateView(_playerHealth.Health, _playerHealth.MaxHealth);
    }
}
