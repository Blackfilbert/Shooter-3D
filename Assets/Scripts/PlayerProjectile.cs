using System;
using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
    private int _damage;
    private DamageType _damageType;
    private Vector3 _direction;
    private float _speed;
    private float _range;
    private float _hitImpulse;
    private LayerMask _hitMask;
    private Vector3 _startPosition;
    private Collider _lockedHitCollider;
    private Vector3 _lockedHitPoint;
    private bool _hasLockedHit;
    private bool _useUnscaledTime;
    private bool _isCompleted;

    public bool UsesLockedHit => _hasLockedHit;

    public event Action<ShotResult> Completed;

    public void Initialize(int damage, DamageType damageType, Vector3 direction, float speed, float range, float hitImpulse, LayerMask hitMask)
    {
        _damage = Mathf.Max(1, damage);
        _damageType = damageType;
        _direction = direction.normalized;
        _speed = Mathf.Max(0f, speed);
        _range = Mathf.Max(0f, range);
        _hitImpulse = hitImpulse;
        _hitMask = hitMask;
        _startPosition = transform.position;
    }

    public void InitializeLockedHit(int damage, DamageType damageType, Vector3 direction, float speed, float range, float hitImpulse, LayerMask hitMask, Collider hitCollider, Vector3 hitPoint)
    {
        Initialize(damage, damageType, direction, speed, range, hitImpulse, hitMask);
        _lockedHitCollider = hitCollider;
        _lockedHitPoint = hitPoint;
        _hasLockedHit = true;
        _useUnscaledTime = true;
    }

    private void Update()
    {
        if (_hasLockedHit)
        {
            UpdateLockedHitMovement();
            return;
        }

        float moveDistance = _speed * Time.deltaTime;

        if (moveDistance > 0f && Physics.Raycast(transform.position, _direction, out RaycastHit hit, moveDistance, _hitMask, QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point;
            Hit(hit.collider);
            return;
        }

        transform.position += _direction * moveDistance;

        if (Vector3.Distance(_startPosition, transform.position) >= _range)
            Complete(ShotResult.Miss);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasLockedHit)
            return;

        Hit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_hasLockedHit)
            return;

        Hit(collision.collider);
    }

    private void UpdateLockedHitMovement()
    {
        float moveDistance = _speed * GetDeltaTime();

        if (moveDistance <= 0f)
            return;

        transform.position = Vector3.MoveTowards(transform.position, _lockedHitPoint, moveDistance);

        if ((transform.position - _lockedHitPoint).sqrMagnitude > 0.0001f)
            return;

        if (_lockedHitCollider != null)
            Hit(_lockedHitCollider);
        else
            Complete(ShotResult.Miss);
    }

    private void Hit(Collider hitCollider)
    {
        if (_isCompleted || hitCollider == null)
            return;

        if ((_hitMask.value & (1 << hitCollider.gameObject.layer)) == 0)
            return;

        Rigidbody hitRigidbody = hitCollider.attachedRigidbody;

        if (hitRigidbody != null)
            hitRigidbody.AddForceAtPosition(_direction * _hitImpulse, transform.position, ForceMode.Impulse);

        IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();

        if (damageable == null)
        {
            Complete(ShotResult.Miss);
            return;
        }

        EnemyHealth enemyHealth = hitCollider.GetComponentInParent<EnemyHealth>();
        DestructibleObject destructibleObject = hitCollider.GetComponentInParent<DestructibleObject>();

        if (CanDamageTarget(enemyHealth, destructibleObject) == false)
        {
            Complete(ShotResult.TutorialBlocked);
            return;
        }

        bool wasFullHealth = enemyHealth != null && enemyHealth.Health == enemyHealth.MaxHealth;

        if (enemyHealth != null)
            enemyHealth.RegisterProjectileHit(hitCollider);

        damageable.TakeDamage(_damage, _damageType, transform.position, _direction);

        ShotResult result = ShotResult.Hit;

        if (enemyHealth != null && enemyHealth.IsDead)
            result = wasFullHealth ? ShotResult.OneShotKill : ShotResult.Kill;

        Complete(result);
    }

    private bool CanDamageTarget(EnemyHealth enemyHealth, DestructibleObject destructibleObject)
    {
        if (Global.GameplayLevelController == null)
            return true;

        if (enemyHealth != null)
            return Global.GameplayLevelController.CanDamageEnemy(enemyHealth);

        if (destructibleObject != null)
            return Global.GameplayLevelController.CanDamageDestructibleObject(destructibleObject);

        return true;
    }

    private void Complete(ShotResult result)
    {
        if (_isCompleted)
            return;

        _isCompleted = true;
        Completed?.Invoke(result);
        Destroy(gameObject);
    }

    private float GetDeltaTime()
    {
        return _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}
