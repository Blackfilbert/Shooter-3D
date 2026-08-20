using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class SpecialBarrelOnlyEnemy : MonoBehaviour
{
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");

    [SerializeField] private EnemyHealth _enemyHealth;
    [SerializeField] private Animator _animator;
    [SerializeField] private string _walkAnimationBool = "IsWalking";
    [SerializeField] private Transform[] _points;
    [SerializeField] private float _moveSpeed = 1f;
    [SerializeField] private float _turnSpeed = 360f;
    [SerializeField] private float _pointReachDistance = 0.05f;
    [SerializeField] private bool _setHealthFromPlayerDamage = true;
    [SerializeField] private int _barrelsToKill = 5;
    [SerializeField] private float _barrelDamageMultiplier = 2f;
    [SerializeField] private float _healthMultiplier = 0.8f;
    [SerializeField] private GameObject _deathExplosionVfx;
    [SerializeField] private float _meshExplosionForce = 6f;
    [SerializeField] private float _meshExplosionRadius = 3f;
    [SerializeField] private float _meshExplosionUpwardsModifier = 0.5f;

    private int _currentPointIndex;
    private int _pointDirection = 1;
    private int _walkAnimationHash;
    private bool _hasWalkAnimationParameter;
    private bool _hasExploded;

    public bool CanTakeCurrentDamage()
    {
        return DamageSourceContext.IsExplosionDamage;
    }

    public void ShowBlockedDamage(Vector3 hitPoint)
    {
        if (Global.HUDManager != null)
            Global.HUDManager.ShowWorldDamage(hitPoint, 0);
    }

    private void Awake()
    {
        if (_enemyHealth == null)
            _enemyHealth = GetComponent<EnemyHealth>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        _walkAnimationHash = string.IsNullOrEmpty(_walkAnimationBool) ? IsWalkingHash : Animator.StringToHash(_walkAnimationBool);
        _hasWalkAnimationParameter = HasAnimatorBoolParameter(_walkAnimationHash);
    }

    private void OnEnable()
    {
        if (_enemyHealth != null)
            _enemyHealth.Died += OnDied;
    }

    private void OnDisable()
    {
        if (_enemyHealth != null)
            _enemyHealth.Died -= OnDied;
    }

    private void Start()
    {
        ApplySpecialHealth();
    }

    private void Update()
    {
        if (_enemyHealth == null || _enemyHealth.IsDead)
        {
            SetWalkAnimation(false);
            return;
        }

        if (Global.EnemyDeathCameraController != null && Global.EnemyDeathCameraController.IsFocusing)
        {
            SetWalkAnimation(false);
            return;
        }

        if (ShouldLoseBecauseBarrelsEnded())
        {
            SetWalkAnimation(false);
            Global.GameplayLevelController.LoseLevelBySpecialBarrels();
            return;
        }

        UpdateMovement();
    }

    private void UpdateMovement()
    {
        if (_points == null || _points.Length == 0 || _moveSpeed <= 0f)
        {
            SetWalkAnimation(false);
            return;
        }

        Transform target = _points[_currentPointIndex];

        if (target == null)
        {
            AdvancePoint();
            return;
        }

        Vector3 targetPosition = target.position;
        targetPosition.y = transform.position.y;
        Vector3 direction = targetPosition - transform.position;

        if (direction.sqrMagnitude <= _pointReachDistance * _pointReachDistance)
        {
            AdvancePoint();
            SetWalkAnimation(false);
            return;
        }

        Vector3 moveDirection = direction.normalized;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, _moveSpeed * Time.deltaTime);
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, Mathf.Max(0f, _turnSpeed) * Time.deltaTime);
        SetWalkAnimation(true);
    }

    private void AdvancePoint()
    {
        if (_points == null || _points.Length <= 1)
            return;

        int nextPointIndex = _currentPointIndex + _pointDirection;

        if (nextPointIndex >= _points.Length)
        {
            _pointDirection = -1;
            nextPointIndex = _points.Length - 2;
        }
        else if (nextPointIndex < 0)
        {
            _pointDirection = 1;
            nextPointIndex = 1;
        }

        _currentPointIndex = nextPointIndex;
    }

    private void ApplySpecialHealth()
    {
        if (_setHealthFromPlayerDamage == false || _enemyHealth == null)
            return;

        int playerDamage = Global.PlayerWeapon != null ? Global.PlayerWeapon.Damage : 1;
        float healthMultiplier = Mathf.Max(0f, _healthMultiplier);
        int health = Mathf.Max(1, Mathf.CeilToInt(playerDamage * Mathf.Max(1, _barrelsToKill) * Mathf.Max(0f, _barrelDamageMultiplier) * healthMultiplier));
        _enemyHealth.SetMaxHealth(health, true);
    }

    private bool ShouldLoseBecauseBarrelsEnded()
    {
        GameplayLevelController levelController = Global.GameplayLevelController;
        return levelController != null
            && levelController.IsLevelFinished == false
            && levelController.HasAliveDamageDestructibleObjects() == false;
    }

    private void OnDied(EnemyHealth enemyHealth)
    {
        if (_hasExploded)
            return;

        _hasExploded = true;
        ActivateDeathExplosionVfx();
        ExplodeMeshes();
    }

    private void ActivateDeathExplosionVfx()
    {
        if (_deathExplosionVfx == null)
            return;

        GameObject vfx = _deathExplosionVfx.scene.IsValid() ? _deathExplosionVfx : Instantiate(_deathExplosionVfx);
        vfx.transform.SetParent(null, true);
        vfx.transform.position = transform.position;
        vfx.SetActive(true);
    }

    private void ExplodeMeshes()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(false);
        Vector3 explosionPosition = transform.position;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer meshRenderer = renderers[i];

            if (meshRenderer == null || meshRenderer.transform == transform)
                continue;

            Transform meshTransform = meshRenderer.transform;
            meshTransform.SetParent(null, true);

            Collider meshCollider = meshTransform.GetComponent<Collider>();

            if (meshCollider == null)
                meshCollider = meshTransform.gameObject.AddComponent<BoxCollider>();

            meshCollider.enabled = true;

            Rigidbody meshRigidbody = meshTransform.GetComponent<Rigidbody>();

            if (meshRigidbody == null)
                meshRigidbody = meshTransform.gameObject.AddComponent<Rigidbody>();

            meshRigidbody.isKinematic = false;
            meshRigidbody.detectCollisions = true;
            meshRigidbody.AddExplosionForce(_meshExplosionForce, explosionPosition, Mathf.Max(0.01f, _meshExplosionRadius), _meshExplosionUpwardsModifier, ForceMode.Impulse);
        }
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
}
