using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    private const int TutorialLimitedLevelsCount = 3;
    private const int FirstTutorialSpawnCount = 3;
    private const int HealthRandomBonusMin = 1;
    private const int HealthRandomBonusMax = 2;

    [SerializeField] private EnemyHealth _enemyPrefab;
    [SerializeField] private EnemyLevelStatsConfig _levelStatsConfig;
    [SerializeField] private int _levelIndexOverride = -1;
    [SerializeField] private float _defaultBossHpMultiplier = 1f;
    [SerializeField] private float _defaultBossScaleMultiplier = 1f;
    [SerializeField] private Vector2 _damageRewardMultiplierRange = new Vector2(0.9f, 1.1f);
    [SerializeField] private int[] _damageRewardMultiplierSteps = { 0, 3 };
    [SerializeField] private EnemySpawnPoint[] _spawnPoints;

    private void Start()
    {
        if (ShouldSkipSpawning())
            return;

        SpawnAll();
    }

    public void SpawnAll()
    {
        if (ShouldSkipSpawning())
            return;

        if (_enemyPrefab == null)
            return;

        if (_levelStatsConfig != null && TrySpawnFromLevelStats())
            return;

        int count = GetSpawnCount(GetLevelIndex(), _spawnPoints.Length);

        for (int i = 0; i < count; i++)
            SpawnAtPoint(_spawnPoints[i], _spawnPoints[i].Health, _spawnPoints[i].KillBonusType, _spawnPoints[i].KillBonusAmount);
    }

    private bool ShouldSkipSpawning()
    {
        return Global.GameplayLevelController != null && Global.GameplayLevelController.IsSpecialLevel;
    }

    private void SpawnAtPoint(EnemySpawnPoint spawnPoint, int health, EnemyKillBonusType killBonusType, float killBonusAmount)
    {
        if (spawnPoint.Point == null)
            return;

        EnemyHealth prefab = spawnPoint.EnemyPrefab != null ? spawnPoint.EnemyPrefab : _enemyPrefab;

        if (prefab == null)
            return;

        EnemyHealth enemy = Instantiate(prefab, spawnPoint.Point.position, spawnPoint.Point.rotation);
        ApplyBossScale(enemy.transform, spawnPoint);
        AlignFeetToPoint(enemy.transform, spawnPoint.Point.position);
        RotateToPlayer(enemy.transform);
        enemy.SetBoss(spawnPoint.IsBoss);
        enemy.Initialize(
            GetSpawnHealth(health, spawnPoint),
            spawnPoint.BehaviorType,
            spawnPoint.WalkRadius,
            spawnPoint.WalkSpeed,
            spawnPoint.MoveTarget,
            spawnPoint.MoveTriggerEnemyCount,
            spawnPoint.MoveToPointSpeed,
            killBonusType,
            killBonusAmount,
            spawnPoint.KillBonusDamageType,
            EnemyBonusStatType.None,
            DamageType.Normal,
            0f);

        if (Global.GameplayTutorialManager != null)
            Global.GameplayTutorialManager.RefreshTargetsForRegisteredContent();
    }

    private void RotateToPlayer(Transform enemyTransform)
    {
        Transform target = GetPlayerTarget();

        if (enemyTransform == null || target == null)
            return;

        Vector3 direction = target.position - enemyTransform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        enemyTransform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private int GetSpawnHealth(int health, EnemySpawnPoint spawnPoint)
    {
        if (spawnPoint.IsBoss == false)
            return health;

        return Mathf.Max(1, Mathf.CeilToInt(health * GetBossHpMultiplier(spawnPoint)));
    }

    private void ApplyBossScale(Transform enemyTransform, EnemySpawnPoint spawnPoint)
    {
        if (enemyTransform == null || spawnPoint.IsBoss == false)
            return;

        enemyTransform.localScale *= GetBossScaleMultiplier(spawnPoint);
    }

    private float GetBossHpMultiplier(EnemySpawnPoint spawnPoint)
    {
        return spawnPoint.HasHpMultiplier
            ? spawnPoint.HpMultiplier
            : Mathf.Max(1f, _defaultBossHpMultiplier);
    }

    private float GetBossScaleMultiplier(EnemySpawnPoint spawnPoint)
    {
        return spawnPoint.HasScaleMultiplier
            ? spawnPoint.ScaleMultiplier
            : Mathf.Max(1f, _defaultBossScaleMultiplier);
    }

    private Transform GetPlayerTarget()
    {
        if (Global.PlayerMovement != null)
            return Global.PlayerMovement.transform;

        if (Global.PlayerWeapon != null && Global.PlayerWeapon.Camera != null)
            return Global.PlayerWeapon.Camera.transform;

        return null;
    }

    private void AlignFeetToPoint(Transform enemyTransform, Vector3 pointPosition)
    {
        if (enemyTransform == null)
            return;

        if (TryGetRenderersBounds(enemyTransform, out Bounds bounds) == false)
            return;

        Vector3 position = enemyTransform.position;
        position.y += pointPosition.y - bounds.min.y;
        enemyTransform.position = position;
    }

    private bool TryGetRenderersBounds(Transform root, out Bounds bounds)
    {
        bounds = default;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(false);
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];

            if (targetRenderer == null || targetRenderer.enabled == false)
                continue;

            if (hasBounds == false)
            {
                bounds = targetRenderer.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(targetRenderer.bounds);
        }

        return hasBounds;
    }

    private int GetLevelIndex()
    {
        if (_levelIndexOverride >= 0)
            return _levelIndexOverride;

        return Global.GameplayLevelController != null ? Global.GameplayLevelController.CurrentLevelIndex : 0;
    }

    private bool TrySpawnFromLevelStats()
    {
        int levelIndex = GetLevelIndex();

        if (_levelStatsConfig.TryGetLevel(levelIndex, out EnemyLevelStatsEntry levelStats) == false)
            return false;

        int count = GetSpawnCount(levelIndex, Mathf.Min(_spawnPoints.Length, levelStats.Count));
        int currentPlayerDamage = Global.PlayerWeapon != null ? Global.PlayerWeapon.Damage : 1;
        int playerDamage = Mathf.Max(1, currentPlayerDamage);
        int simulatedDamage = playerDamage;

        for (int i = 0; i < count; i++)
        {
            EnemySpawnPoint spawnPoint = _spawnPoints[i];
            int health = Mathf.Max(1, Mathf.CeilToInt(playerDamage * levelStats.GetHealthMultiplier(i)));
            EnemyKillBonusType killBonusType = spawnPoint.KillBonusType;
            int baseDamageReward = killBonusType == EnemyKillBonusType.AddDamage ? GetBaseDamageReward(levelStats, i) : 0;
            float killBonusAmount = killBonusType == EnemyKillBonusType.AddDamage ? GetDamageReward(baseDamageReward, i) : spawnPoint.KillBonusAmount;

            if (spawnPoint.IsBoss == false)
                health = Mathf.Min(health, simulatedDamage);

            health = ApplyHealthRandomBonus(health, spawnPoint, simulatedDamage);

            SpawnAtPoint(spawnPoint, health, killBonusType, killBonusAmount);

            if (killBonusType == EnemyKillBonusType.AddDamage)
                simulatedDamage += baseDamageReward;
        }

        return count > 0;
    }

    private int ApplyHealthRandomBonus(int health, EnemySpawnPoint spawnPoint, int simulatedDamage)
    {
        if (spawnPoint.IsBoss)
            return health;

        int maxHealth = Mathf.Max(1, simulatedDamage);
        int randomAmount = Random.Range(HealthRandomBonusMin, HealthRandomBonusMax + 1);

        if (health >= maxHealth)
            return Mathf.Max(1, maxHealth - randomAmount);

        int direction = Random.Range(0, 2) == 0 ? -1 : 1;
        return Mathf.Clamp(health + randomAmount * direction, 1, maxHealth);
    }

    private int GetSpawnCount(int levelIndex, int defaultCount)
    {
        int count = Mathf.Max(0, defaultCount);

        if (IsFirstPassTutorialLimitedLevel(levelIndex) == false)
            return count;

        return Mathf.Min(count, FirstTutorialSpawnCount + Mathf.Max(0, levelIndex));
    }

    private bool IsFirstPassTutorialLimitedLevel(int levelIndex)
    {
        return levelIndex >= 0
            && levelIndex < TutorialLimitedLevelsCount
            && SaveManager.CompletedLevelIndex < levelIndex;
    }

    private int GetBaseDamageReward(EnemyLevelStatsEntry levelStats, int stepIndex)
    {
        return Mathf.CeilToInt(levelStats.GetDamageReward(stepIndex));
    }

    private int GetDamageReward(int reward, int stepIndex)
    {
        if (ShouldMultiplyDamageReward(stepIndex) == false)
            return reward;

        return GetMultipliedDamageReward(reward);
    }

    private int GetMultipliedDamageReward(int reward)
    {
        float minMultiplier = Mathf.Max(0f, Mathf.Min(_damageRewardMultiplierRange.x, _damageRewardMultiplierRange.y));
        float maxMultiplier = Mathf.Max(minMultiplier, Mathf.Max(_damageRewardMultiplierRange.x, _damageRewardMultiplierRange.y));
        return Mathf.CeilToInt(reward * Random.Range(minMultiplier, maxMultiplier));
    }

    private bool ShouldMultiplyDamageReward(int stepIndex)
    {
        if (_damageRewardMultiplierSteps == null || _damageRewardMultiplierSteps.Length == 0)
            return false;

        for (int i = 0; i < _damageRewardMultiplierSteps.Length; i++)
        {
            if (_damageRewardMultiplierSteps[i] == stepIndex)
                return true;
        }

        return false;
    }

    [System.Serializable]
    private struct EnemySpawnPoint
    {
        [SerializeField] private EnemyHealth _enemyPrefab;
        [SerializeField] private Transform _point;
        [SerializeField] private EnemyBehaviorType _behaviorType;
        [SerializeField] private float _walkRadius;
        [SerializeField] private float _walkSpeed;
        [SerializeField] private Transform _moveTarget;
        [SerializeField] private int _moveTriggerEnemyCount;
        [SerializeField] private float _moveToPointSpeed;
        [SerializeField] private int _health;
        [SerializeField] private EnemyKillBonusType _killBonusType;
        [SerializeField] private float _killBonusAmount;
        [SerializeField] private DamageType _killBonusDamageType;
        [SerializeField] private bool _isBoss;
        [SerializeField] private float _bossHpMultiplier;
        [SerializeField] private float _bossScaleMultiplier;

        public EnemyHealth EnemyPrefab => _enemyPrefab;
        public Transform Point => _point;
        public EnemyBehaviorType BehaviorType => _behaviorType;
        public float WalkRadius => _walkRadius;
        public float WalkSpeed => _walkSpeed;
        public Transform MoveTarget => _moveTarget;
        public int MoveTriggerEnemyCount => _moveTriggerEnemyCount;
        public float MoveToPointSpeed => _moveToPointSpeed;
        public int Health => _health;
        public EnemyKillBonusType KillBonusType => _killBonusType;
        public float KillBonusAmount => _killBonusAmount;
        public DamageType KillBonusDamageType => _killBonusDamageType;
        public bool IsBoss => _isBoss;
        public bool HasHpMultiplier => _bossHpMultiplier > 0f;
        public bool HasScaleMultiplier => _bossScaleMultiplier > 0f;
        public float HpMultiplier => _bossHpMultiplier > 0f ? _bossHpMultiplier : 1f;
        public float ScaleMultiplier => _bossScaleMultiplier > 0f ? _bossScaleMultiplier : 1f;
    }
}
