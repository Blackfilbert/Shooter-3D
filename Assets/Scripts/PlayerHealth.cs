using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int _baseMaxHealth = 100;

    private int _health;
    private int _maxHealth;
    private bool _isDead;

    public int Health => _health;
    public int MaxHealth => _maxHealth;
    public bool IsDead => _isDead;

    public event Action<int, int> HealthChanged;
    public event Action<int> Damaged;
    public event Action Died;

    private void Awake()
    {
        RecalculateMaxHealth(false);
        _health = _maxHealth;
        Global.RegisterPlayerHealth(this);
    }

    private void OnEnable()
    {
        InventoryManager.EquipmentChanged += OnEquipmentChanged;
    }

    private void Start()
    {
        HealthChanged?.Invoke(_health, _maxHealth);
    }

    private void OnDisable()
    {
        InventoryManager.EquipmentChanged -= OnEquipmentChanged;
    }

    private void OnDestroy()
    {
        Global.UnregisterPlayerHealth(this);
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, DamageType.Normal, transform.position, Vector3.forward);
    }

    public void TakeDamage(int damage, DamageType damageType)
    {
        TakeDamage(damage, damageType, transform.position, Vector3.forward);
    }

    public void TakeDamage(int damage, DamageType damageType, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (_isDead || damage <= 0)
            return;

        int previousHealth = _health;
        _health = Mathf.Max(0, _health - damage);
        HealthChanged?.Invoke(_health, _maxHealth);

        int appliedDamage = previousHealth - _health;

        if (appliedDamage > 0)
            Damaged?.Invoke(appliedDamage);

        if (_health <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (_isDead || amount <= 0)
            return;

        _health = Mathf.Min(_maxHealth, _health + amount);
        HealthChanged?.Invoke(_health, _maxHealth);
    }

    public void SetHealth(int health)
    {
        _health = Mathf.Clamp(health, 0, _maxHealth);
        HealthChanged?.Invoke(_health, _maxHealth);

        if (_health <= 0)
            Die();
    }

    private void OnEquipmentChanged(InventorySlotType slotType, string itemId)
    {
        RecalculateMaxHealth(true);
    }

    private void RecalculateMaxHealth(bool preserveHealthPercent)
    {
        int previousMaxHealth = _maxHealth;
        float healthPercent = previousMaxHealth > 0 ? (float)_health / previousMaxHealth : 1f;

        _maxHealth = Mathf.Max(1, _baseMaxHealth + InventoryManager.GetTotalEquippedStats().Health);

        if (preserveHealthPercent)
            _health = Mathf.Clamp(Mathf.RoundToInt(_maxHealth * healthPercent), 1, _maxHealth);
        else
            _health = Mathf.Clamp(_health, 0, _maxHealth);

        HealthChanged?.Invoke(_health, _maxHealth);
    }

    private void Die()
    {
        if (_isDead)
            return;

        _isDead = true;
        Died?.Invoke();
    }
}
