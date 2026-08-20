using UnityEngine;

public class SpecialEnemyPeriodicAttack : MonoBehaviour
{
    [SerializeField] private EnemyHealth _enemyHealth;
    [SerializeField] private float _attackInterval = 5f;
    [SerializeField] private float _currentHealthDamagePercent = 3f;

    private float _nextAttackTime;
    private bool _hasGameplayStarted;

    private void Awake()
    {
        if (_enemyHealth == null)
            _enemyHealth = GetComponentInParent<EnemyHealth>();
    }

    private void OnEnable()
    {
        _nextAttackTime = Time.time + Mathf.Max(0f, _attackInterval);
    }

    private void Update()
    {
        if (Global.GameplayLevelController != null && Global.GameplayLevelController.IsGameplayStarted)
            _hasGameplayStarted = true;

        if (CanAttack() == false || Time.time < _nextAttackTime)
            return;

        _nextAttackTime = Time.time + Mathf.Max(0f, _attackInterval);
        AttackPlayer();
    }

    private bool CanAttack()
    {
        return _enemyHealth != null
            && _enemyHealth.IsDead == false
            && _hasGameplayStarted
            && Global.GameplayLevelController != null
            && Global.GameplayLevelController.IsLevelFinished == false
            && Global.PlayerHealth != null
            && Global.PlayerHealth.IsDead == false;
    }

    private void AttackPlayer()
    {
        _enemyHealth.PlayAttackAnimation();

        int damage = Mathf.Max(1, Mathf.CeilToInt(Global.PlayerHealth.Health * (Mathf.Max(0f, _currentHealthDamagePercent) / 100f)));
        Global.PlayerHealth.TakeDamage(damage);
    }
}
