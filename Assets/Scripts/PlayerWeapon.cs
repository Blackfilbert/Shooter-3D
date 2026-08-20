using System;
using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private int _damage = 1;
    [SerializeField] private DamageType _damageType = DamageType.Normal;
    [SerializeField] private float _range = 100f;
    [SerializeField] private float _hitImpulse = 4f;
    [SerializeField] private LayerMask _hitMask = ~0;
    [SerializeField] private PlayerProjectile _projectilePrefab;
    [SerializeField] private float _projectileSpeed = 60f;
    [SerializeField] private float _projectileSpawnOffset = 0.5f;

    private int _damageBonus;
    private PlayerProjectile _currentProjectile;

    public Camera Camera => _camera;
    public int Damage => Mathf.Max(1, _damage + _damageBonus + InventoryManager.GetTotalEquippedStats().Damage);
    public DamageType DamageType => _damageType;
    public float Range => _range;
    public EnemyHealth AimedEnemy => _aimedEnemy;

    public event Action<int> DamageChanged;
    public event Action<DamageType> DamageTypeChanged;
    public event Action<ShotResult> ShotCompleted;
    public event Action<PlayerProjectile> ProjectileFired;
    public event Action<EnemyHealth> AimedEnemyChanged;

    private EnemyHealth _aimedEnemy;

    private void Awake()
    {
        InventoryManager.EquipmentChanged += OnEquipmentChanged;
        Global.RegisterPlayerWeapon(this);
    }

    private void Start()
    {
        DamageChanged?.Invoke(Damage);
    }

    private void Update()
    {
        UpdateAimedEnemy();
    }

    private void OnDestroy()
    {
        InventoryManager.EquipmentChanged -= OnEquipmentChanged;
        Global.UnregisterPlayerWeapon(this);
    }

    public bool Shoot()
    {
        if (_currentProjectile != null)
            return false;

        if (Global.GameplayLevelController != null && Global.GameplayLevelController.IsLevelFinished)
            return false;

        if (Global.GameplayLevelController != null && Global.GameplayLevelController.IsGameplayStarted == false)
            return false;

        if (_camera == null)
            return false;

        if (_projectilePrefab == null)
            return false;

        if (Global.AudioManager != null)
            Global.AudioManager.PlaySound(AudioSfxType.PlayerShoot);

        Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 spawnPosition = ray.origin + ray.direction * Mathf.Max(0f, _projectileSpawnOffset);
        PlayerProjectile projectile = Instantiate(_projectilePrefab, spawnPosition, Quaternion.LookRotation(ray.direction));
        _currentProjectile = projectile;
        projectile.Completed += OnProjectileCompleted;

        if (TryGetLockedProjectileHit(ray, out Collider lockedHitCollider, out Vector3 lockedHitPoint))
            projectile.InitializeLockedHit(Damage, _damageType, ray.direction, _projectileSpeed, _range, _hitImpulse, _hitMask, lockedHitCollider, lockedHitPoint);
        else
            projectile.Initialize(Damage, _damageType, ray.direction, _projectileSpeed, _range, _hitImpulse, _hitMask);

        ProjectileFired?.Invoke(projectile);

        return true;
    }

    public void AddDamage(int amount)
    {
        if (amount == 0)
            return;

        _damageBonus += amount;
        DamageChanged?.Invoke(Damage);
    }

    public void MultiplyDamage(float multiplier)
    {
        if (multiplier <= 0f)
            return;

        int currentDamage = Damage;
        _damageBonus += Mathf.Max(1, Mathf.RoundToInt(currentDamage * multiplier)) - currentDamage;
        DamageChanged?.Invoke(Damage);
    }

    public void SetDamageType(DamageType damageType)
    {
        _damageType = damageType;
        DamageTypeChanged?.Invoke(_damageType);
    }

    private void OnEquipmentChanged(InventorySlotType slotType, string itemId)
    {
        DamageChanged?.Invoke(Damage);
    }

    private bool ShouldUseLockedProjectileHit()
    {
        return Global.GameplayLevelController != null && Global.GameplayLevelController.ShouldUseProjectileKillCamera;
    }

    private bool TryGetLockedProjectileHit(Ray ray, out Collider hitCollider, out Vector3 hitPoint)
    {
        hitCollider = null;
        hitPoint = ray.origin + ray.direction * _range;

        if (ShouldUseLockedProjectileHit() == false)
            return false;

        if (Physics.Raycast(ray, out RaycastHit hit, _range, _hitMask, QueryTriggerInteraction.Ignore) == false)
            return false;

        if (hit.collider.GetComponentInParent<IDamageable>() == null)
            return false;

        hitCollider = hit.collider;
        hitPoint = hit.point;
        return true;
    }

    private void OnProjectileCompleted(ShotResult shotResult)
    {
        if (_currentProjectile != null)
        {
            _currentProjectile.Completed -= OnProjectileCompleted;
            _currentProjectile = null;
        }

        ShotCompleted?.Invoke(shotResult);
    }

    private void UpdateAimedEnemy()
    {
        EnemyHealth aimedEnemy = GetAimedEnemy();

        if (_aimedEnemy == aimedEnemy)
            return;

        _aimedEnemy = aimedEnemy;
        AimedEnemyChanged?.Invoke(_aimedEnemy);
    }

    private EnemyHealth GetAimedEnemy()
    {
        if (_camera == null)
            return null;

        Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, _range, _hitMask, QueryTriggerInteraction.Ignore) == false)
            return null;

        return hit.collider.GetComponentInParent<EnemyHealth>();
    }
}

public enum DamageType
{
    Normal,
    Fire,
    Electric
}

public enum ShotResult
{
    TutorialBlocked,
    Miss,
    Hit,
    Kill,
    OneShotKill
}
