using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    private const float WalkTurnWaitMin = 1f;
    private const float WalkTurnWaitMax = 3f;

    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    [SerializeField] private int _maxHealth = 3;
    [SerializeField] private Animator _animator;
    [SerializeField] private Collider _mainHitbox;
    [SerializeField] private Collider _headHitbox;
    [SerializeField] private float _deathImpactForce = 5f;
    [SerializeField] private string _walkAnimationBool = "IsWalking";
    [SerializeField] private string _attackAnimationTrigger = "Attack";
    [SerializeField] private bool _isBoss;

    private Rigidbody[] _ragdollRigidbodies;
    private Collider[] _ragdollColliders;
    private EnemyBehaviorType _behaviorType;
    private Vector3 _startPosition;
    private Vector3 _walkAxis;
    private int _walkDirection = 1;
    private float _walkRadius;
    private float _walkSpeed;
    private float _walkTurnWaitRemaining;
    private Transform _moveTarget;
    private int _moveTriggerEnemyCount;
    private float _moveToPointSpeed;
    private int _walkAnimationHash;
    private int _attackAnimationTriggerHash;
    private bool _hasWalkAnimationParameter;
    private bool _hasAttackAnimationParameter;
    private bool _isWaitingBeforeWalkTurn;
    private bool _hasReachedMoveTarget;
    private int _health;
    private bool _isDead;
    private EnemyKillBonusType _killBonusType;
    private float _killBonusAmount;
    private DamageType _killBonusDamageType;
    private EnemyBonusStatType _bonusStatType;
    private DamageType _resistDamageType;
    private float _bonusStatAmount;
    private Collider _lastHitCollider;
    private bool _wasKilledByHeadshot;
    private bool _wasDamagedBeforeKill;

    public int MaxHealth => _maxHealth;
    public int Health => _health;
    public bool IsDead => _isDead;
    public EnemyKillBonusType KillBonusType => _killBonusType;
    public float KillBonusAmount => _killBonusAmount;
    public bool WasKilledByHeadshot => _wasKilledByHeadshot;
    public bool WasDamagedBeforeKill => _wasDamagedBeforeKill;
    public bool IsBoss => _isBoss;
    public string KillBonusText
    {
        get
        {
            if (_killBonusType == EnemyKillBonusType.None || _killBonusType == EnemyKillBonusType.AddAmmo)
                return string.Empty;

            if (_killBonusType == EnemyKillBonusType.ChangeDamageType)
                return $"{_killBonusType} {_killBonusDamageType}";

            return $"+{CompactNumberFormatter.Format(_killBonusAmount)}";
        }
    }

    public event Action<int, int> HealthChanged;
    public event Action BonusChanged;
    public event Action<EnemyHealth> Died;

    private void Awake()
    {
        ResolveAnimator();

        _ragdollRigidbodies = GetComponentsInChildren<Rigidbody>(true);
        _ragdollColliders = GetComponentsInChildren<Collider>(true);
        _health = _maxHealth;
        _startPosition = transform.position;
        _walkAxis = transform.right;
        _walkAnimationHash = string.IsNullOrEmpty(_walkAnimationBool) ? IsWalkingHash : Animator.StringToHash(_walkAnimationBool);
        _attackAnimationTriggerHash = string.IsNullOrEmpty(_attackAnimationTrigger) ? AttackHash : Animator.StringToHash(_attackAnimationTrigger);
        _hasWalkAnimationParameter = HasAnimatorBoolParameter(_walkAnimationHash);
        _hasAttackAnimationParameter = HasAnimatorTriggerParameter(_attackAnimationTriggerHash);
        SetRagdollActive(false);
    }

    private void ResolveAnimator()
    {
        RandomEnemyMesh randomEnemyMesh = GetComponentInChildren<RandomEnemyMesh>(true);

        if (randomEnemyMesh != null)
        {
            Animator activeAnimator = randomEnemyMesh.GetActiveAnimator();

            if (activeAnimator != null)
            {
                _animator = activeAnimator;
                return;
            }
        }

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        UpdateBehavior();
    }

    private void OnEnable()
    {
        if (Global.HUDManager != null && ShouldRegisterHealthBar())
            Global.HUDManager.RegisterEnemyHealth(this);

        if (Global.GameplayLevelController != null)
            Global.GameplayLevelController.RegisterEnemy(this);
    }

    private void OnDisable()
    {
        if (Global.HUDManager != null)
            Global.HUDManager.UnregisterEnemyHealth(this);

        if (_isDead == false && Global.GameplayLevelController != null)
            Global.GameplayLevelController.UnregisterEnemy(this);
    }

    public void Initialize(int maxHealth, EnemyBehaviorType behaviorType, float walkRadius, float walkSpeed, EnemyKillBonusType killBonusType, float killBonusAmount, DamageType killBonusDamageType, EnemyBonusStatType bonusStatType, DamageType resistDamageType, float bonusStatAmount)
    {
        Initialize(maxHealth, behaviorType, walkRadius, walkSpeed, null, 0, 0f, killBonusType, killBonusAmount, killBonusDamageType, bonusStatType, resistDamageType, bonusStatAmount);
    }

    public void Initialize(int maxHealth, EnemyBehaviorType behaviorType, float walkRadius, float walkSpeed, Transform moveTarget, int moveTriggerEnemyCount, float moveToPointSpeed, EnemyKillBonusType killBonusType, float killBonusAmount, DamageType killBonusDamageType, EnemyBonusStatType bonusStatType, DamageType resistDamageType, float bonusStatAmount)
    {
        _maxHealth = Mathf.Max(1, maxHealth);
        _health = _maxHealth;
        _isDead = false;
        _behaviorType = behaviorType;
        _walkRadius = Mathf.Max(0f, walkRadius);
        _walkSpeed = Mathf.Max(0f, walkSpeed);
        _startPosition = transform.position;
        _walkAxis = transform.right;
        _walkDirection = 1;
        _walkTurnWaitRemaining = 0f;
        _isWaitingBeforeWalkTurn = false;
        _moveTarget = moveTarget;
        _moveTriggerEnemyCount = Mathf.Max(0, moveTriggerEnemyCount);
        _moveToPointSpeed = Mathf.Max(0f, moveToPointSpeed);
        _hasReachedMoveTarget = false;
        _killBonusType = killBonusType;
        _killBonusAmount = killBonusAmount;
        _killBonusDamageType = killBonusDamageType;
        _bonusStatType = bonusStatType;
        _resistDamageType = resistDamageType;
        _bonusStatAmount = bonusStatAmount;
        _lastHitCollider = null;
        _wasKilledByHeadshot = false;
        _wasDamagedBeforeKill = false;

        if (_animator != null)
            _animator.enabled = true;

        SetWalkAnimation(_behaviorType == EnemyBehaviorType.Walk);
        SetRagdollActive(false);
        BonusChanged?.Invoke();
        HealthChanged?.Invoke(_health, _maxHealth);
    }

    public void Initialize(int maxHealth, EnemyKillBonusType killBonusType, float killBonusAmount, DamageType killBonusDamageType, EnemyBonusStatType bonusStatType, DamageType resistDamageType, float bonusStatAmount)
    {
        Initialize(maxHealth, EnemyBehaviorType.Idle, 0f, 0f, killBonusType, killBonusAmount, killBonusDamageType, bonusStatType, resistDamageType, bonusStatAmount);
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

        if (CanTakeCurrentDamage(hitPoint) == false)
            return;

        if (Global.GameplayLevelController != null && Global.GameplayLevelController.CanDamageEnemy(this) == false)
            return;

        int finalDamage = CalculateDamage(damage, damageType);

        if (finalDamage <= 0)
            return;

        bool wasDamagedBeforeHit = _health < _maxHealth;
        _health -= finalDamage;
        HealthChanged?.Invoke(_health, _maxHealth);

        if (Global.AudioManager != null)
            Global.AudioManager.PlaySound(AudioSfxType.EnemyHit);

        if (_health <= 0)
        {
            _wasKilledByHeadshot = _headHitbox != null && _lastHitCollider == _headHitbox;
            _wasDamagedBeforeKill = wasDamagedBeforeHit;
            Die(hitPoint, hitDirection);
        }

        _lastHitCollider = null;
    }

    private bool CanTakeCurrentDamage(Vector3 hitPoint)
    {
        SpecialBarrelOnlyEnemy barrelOnlyEnemy = GetComponent<SpecialBarrelOnlyEnemy>();

        if (barrelOnlyEnemy != null && barrelOnlyEnemy.CanTakeCurrentDamage() == false)
        {
            barrelOnlyEnemy.ShowBlockedDamage(hitPoint);
            return false;
        }

        SpecialWeakPointEnemy weakPointEnemy = GetComponent<SpecialWeakPointEnemy>();

        if (weakPointEnemy != null && weakPointEnemy.CanTakeCurrentDamage() == false)
            return false;

        return true;
    }

    public void RegisterProjectileHit(Collider hitCollider)
    {
        _lastHitCollider = hitCollider;
    }

    public void RestoreFullHealth()
    {
        if (_isDead)
            return;

        _health = _maxHealth;
        HealthChanged?.Invoke(_health, _maxHealth);
    }

    private bool ShouldRegisterHealthBar()
    {
        return GetComponent<SpecialWeakPointEnemy>() == null;
    }

    public void SetMaxHealth(int maxHealth, bool restoreHealth)
    {
        if (_isDead)
            return;

        _maxHealth = Mathf.Max(1, maxHealth);
        _health = restoreHealth ? _maxHealth : Mathf.Clamp(_health, 1, _maxHealth);
        HealthChanged?.Invoke(_health, _maxHealth);
    }

    public void SetBoss(bool isBoss)
    {
        _isBoss = isBoss;
    }

    public void PlayAttackAnimation()
    {
        if (_isDead || _animator == null || _animator.enabled == false)
            return;

        if (_hasAttackAnimationParameter)
        {
            _animator.ResetTrigger(_attackAnimationTriggerHash);
            _animator.SetTrigger(_attackAnimationTriggerHash);
        }
    }

    private void Die(Vector3 hitPoint, Vector3 hitDirection)
    {
        _isDead = true;
        SetWalkAnimation(false);
        Died?.Invoke(this);
        ApplyKillBonus();
        ActivateRagdoll(hitPoint, hitDirection);
    }

    private void ApplyKillBonus()
    {
        PlayerWeapon playerWeapon = Global.PlayerWeapon;

        if (playerWeapon == null)
            return;

        switch (_killBonusType)
        {
            case EnemyKillBonusType.AddAmmo:
                break;
            case EnemyKillBonusType.AddDamage:
                playerWeapon.AddDamage(Mathf.RoundToInt(_killBonusAmount));
                break;
            case EnemyKillBonusType.MultiplyDamage:
                playerWeapon.MultiplyDamage(_killBonusAmount);
                break;
            case EnemyKillBonusType.ChangeDamageType:
                playerWeapon.SetDamageType(_killBonusDamageType);
                break;
            case EnemyKillBonusType.AddSoftCurrency:
                CurrencyManager.Add(CurrencyType.Soft, Mathf.RoundToInt(_killBonusAmount));
                break;
            case EnemyKillBonusType.AddHardCurrency:
                CurrencyManager.Add(CurrencyType.Hard, Mathf.RoundToInt(_killBonusAmount));
                break;
        }
    }

    private int CalculateDamage(int damage, DamageType damageType)
    {
        if (_bonusStatType == EnemyBonusStatType.Armor)
            return ApplyPercentReduction(damage, _bonusStatAmount);

        if (_bonusStatType == EnemyBonusStatType.Resist && damageType == _resistDamageType)
            return ApplyPercentReduction(damage, _bonusStatAmount);

        return damage;
    }

    private int ApplyPercentReduction(int damage, float percent)
    {
        float reduction = Mathf.Clamp(percent, 0f, 100f) / 100f;
        return Mathf.CeilToInt(damage * (1f - reduction));
    }

    private void SetRagdollActive(bool isActive)
    {
        if (_mainHitbox != null)
            _mainHitbox.enabled = isActive == false;

        if (_headHitbox != null)
            _headHitbox.enabled = isActive == false;

        for (int i = 0; i < _ragdollRigidbodies.Length; i++)
        {
            if (_ragdollRigidbodies[i] == null)
                continue;

            _ragdollRigidbodies[i].isKinematic = isActive == false;
            _ragdollRigidbodies[i].detectCollisions = isActive;
        }

        for (int i = 0; i < _ragdollColliders.Length; i++)
        {
            Collider ragdollCollider = _ragdollColliders[i];

            if (ragdollCollider == null || ragdollCollider == _mainHitbox || ragdollCollider == _headHitbox)
                continue;

            ragdollCollider.enabled = isActive;
        }
    }

    private void ActivateRagdoll(Vector3 hitPoint, Vector3 hitDirection)
    {
        if (_animator != null)
            _animator.enabled = false;

        SetRagdollActive(true);

        Rigidbody targetRigidbody = GetClosestRagdollRigidbody(hitPoint);

        if (targetRigidbody != null)
        {
            Vector3 impactForce = hitDirection.normalized * _deathImpactForce + Vector3.up * (_deathImpactForce * 0.5f);
            targetRigidbody.AddForceAtPosition(impactForce, hitPoint, ForceMode.Impulse);
        }
    }

    private void UpdateBehavior()
    {
        if (_isDead)
            return;

        if (IsKillCameraFocusing())
        {
            SetWalkAnimation(false);
            return;
        }

        if (_behaviorType == EnemyBehaviorType.Walk)
        {
            UpdateWalkBehavior();
            return;
        }

        if (_behaviorType == EnemyBehaviorType.MoveToPointOnEnemyCount)
            UpdateMoveToPointBehavior();
    }

    private bool IsKillCameraFocusing()
    {
        return Global.EnemyDeathCameraController != null && Global.EnemyDeathCameraController.IsFocusing;
    }

    private void UpdateWalkBehavior()
    {
        if (_walkRadius <= 0f || _walkSpeed <= 0f)
            return;

        if (_isWaitingBeforeWalkTurn)
        {
            _walkTurnWaitRemaining -= Time.deltaTime;
            SetWalkAnimation(false);

            if (_walkTurnWaitRemaining > 0f)
                return;

            _isWaitingBeforeWalkTurn = false;
            _walkDirection *= -1;
        }

        Vector3 currentOffset = transform.position - _startPosition;
        float axisPosition = Vector3.Dot(currentOffset, _walkAxis);

        if (_walkDirection > 0 && axisPosition >= _walkRadius)
        {
            BeginWalkTurnWait();
            return;
        }

        if (_walkDirection < 0 && axisPosition <= -_walkRadius)
        {
            BeginWalkTurnWait();
            return;
        }

        Vector3 moveDirection = _walkAxis * _walkDirection;
        transform.position += moveDirection * (_walkSpeed * Time.deltaTime);

        if (moveDirection.sqrMagnitude > 0f)
            transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);

        SetWalkAnimation(true);
    }

    private void UpdateMoveToPointBehavior()
    {
        if (_hasReachedMoveTarget || _moveTarget == null || _moveToPointSpeed <= 0f)
            return;

        if (Global.GameplayLevelController == null || Global.GameplayLevelController.AliveEnemies.Count > _moveTriggerEnemyCount)
        {
            SetWalkAnimation(false);
            return;
        }

        Vector3 targetPosition = _moveTarget.position;
        targetPosition.y = transform.position.y;
        Vector3 direction = targetPosition - transform.position;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            _hasReachedMoveTarget = true;
            SetWalkAnimation(false);
            return;
        }

        Vector3 moveDirection = direction.normalized;
        float moveDistance = _moveToPointSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveDistance);
        transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);

        SetWalkAnimation(true);
    }

    private void BeginWalkTurnWait()
    {
        _isWaitingBeforeWalkTurn = true;
        _walkTurnWaitRemaining = UnityEngine.Random.Range(WalkTurnWaitMin, WalkTurnWaitMax);
        SetWalkAnimation(false);
    }

    private void SetWalkAnimation(bool isWalking)
    {
        if (_animator != null && _animator.enabled && _hasWalkAnimationParameter)
            _animator.SetBool(_walkAnimationHash, isWalking);
    }

    private bool HasAnimatorBoolParameter(int parameterHash)
    {
        if (_animator == null)
            return false;

        AnimatorControllerParameter[] parameters = _animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];

            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.nameHash == parameterHash)
                return true;
        }

        return false;
    }

    private bool HasAnimatorTriggerParameter(int parameterHash)
    {
        if (_animator == null)
            return false;

        AnimatorControllerParameter[] parameters = _animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];

            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.nameHash == parameterHash)
                return true;
        }

        return false;
    }

    private Rigidbody GetClosestRagdollRigidbody(Vector3 point)
    {
        Rigidbody closestRigidbody = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < _ragdollRigidbodies.Length; i++)
        {
            Rigidbody ragdollRigidbody = _ragdollRigidbodies[i];

            if (ragdollRigidbody == null)
                continue;

            float distance = (ragdollRigidbody.position - point).sqrMagnitude;

            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            closestRigidbody = ragdollRigidbody;
        }

        return closestRigidbody;
    }
}

public enum EnemyBonusStatType
{
    None,
    Resist,
    Armor
}

public enum EnemyBehaviorType
{
    Idle,
    Walk,
    MoveToPointOnEnemyCount
}

public static class CompactNumberFormatter
{
    private static readonly string[] Suffixes = { string.Empty, "k", "m", "b", "t" };

    public static string Format(float value)
    {
        float absValue = Mathf.Abs(value);
        int suffixIndex = 0;

        while (absValue >= 1000f && suffixIndex < Suffixes.Length - 1)
        {
            value /= 1000f;
            absValue /= 1000f;
            suffixIndex++;
        }

        if (suffixIndex == 0)
            return Mathf.RoundToInt(value).ToString();

        string format = Mathf.Abs(value) < 10f ? "0.#" : "0";
        return value.ToString(format, System.Globalization.CultureInfo.InvariantCulture) + Suffixes[suffixIndex];
    }
}
