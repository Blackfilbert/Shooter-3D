using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameplayTutorialManager : MonoBehaviour
{
    private const int FirstTutorialLevelIndex = 0;
    private const int LastMarkerTutorialLevelIndex = 2;
    private const int BarrelTutorialLevelIndex = 3;
    private const int LevelsPerTutorialCycle = 10;
    private const int BarrelTutorialBossTargetsCount = 2;

    [SerializeField] private GameObject _introTextObject;
    [SerializeField] private GameObject _startReadyObject;
    [SerializeField] private GameObject _aimTextObject;
    [SerializeField] private GameObject _targetAimedTextObject;

    private readonly List<EnemyHealth> _targets = new List<EnemyHealth>();
    private readonly List<EnemyHealth> _currentTargets = new List<EnemyHealth>();
    private readonly List<DestructibleObject> _destructibleTargets = new List<DestructibleObject>();
    private readonly List<DestructibleObject> _currentDestructibleTargets = new List<DestructibleObject>();
    private GameplayLevelController _levelController;
    private PlayerWeapon _subscribedWeapon;
    private int _levelIndex = -1;
    private bool _isActive;
    private bool _isMarkerOnlyActive;
    private bool _isBarrelTutorialActive;
    private bool _isGameplayStarted;
    private bool _isStartButtonUnlocked;
    private bool _isWaitingForScreenRelease;
    private bool _wasPointerPressed;
    private EnemyHealth _currentTarget;

    public bool IsActive => _isActive;
    public bool IsTutorialFlowActive => _isActive || _isMarkerOnlyActive || _isBarrelTutorialActive;
    public bool CanShowStartButton => _isActive == false || _isStartButtonUnlocked;
    public EnemyHealth CurrentTarget => _currentTarget;
    public IReadOnlyList<EnemyHealth> CurrentTargets => _currentTargets;
    public IReadOnlyList<DestructibleObject> CurrentDestructibleTargets => _currentDestructibleTargets;

    public event Action<EnemyHealth> TargetChanged;
    public event Action<IReadOnlyList<EnemyHealth>> TargetsChanged;
    public event Action<IReadOnlyList<DestructibleObject>> DestructibleTargetsChanged;
    public event Action WrongTarget;

    private void Awake()
    {
        Global.RegisterGameplayTutorialManager(this);
        SetObjectActive(_introTextObject, false);
        SetObjectActive(_startReadyObject, false);
        SetObjectActive(_aimTextObject, false);
        SetObjectActive(_targetAimedTextObject, false);
    }

    private void OnDestroy()
    {
        UnsubscribeWeapon();
        Global.UnregisterGameplayTutorialManager(this);
    }

    private void Update()
    {
        SubscribeWeapon();

        if (_isActive == false || _isWaitingForScreenRelease == false)
            return;

        bool isPointerPressed = IsPointerPressed();

        if (_wasPointerPressed && isPointerPressed == false)
            CompleteIntro();

        _wasPointerPressed = isPointerPressed;
    }

    public void HandleLevelLoaded(GameplayLevelController levelController, int levelIndex)
    {
        _levelController = levelController;
        _levelIndex = levelIndex;
        ClearTargets(false);
        _currentTargets.Clear();
        _currentDestructibleTargets.Clear();
        _currentTarget = null;
        _isActive = IsFirstPassTutorialLevel(levelIndex, FirstTutorialLevelIndex);
        _isMarkerOnlyActive = IsFirstPassMarkerTutorialLevel(levelIndex);
        _isBarrelTutorialActive = IsFirstPassBarrelTutorialLevel(levelIndex);
        _isGameplayStarted = false;
        _isStartButtonUnlocked = _isActive == false;
        _isWaitingForScreenRelease = _isActive;
        _wasPointerPressed = false;

        SetObjectActive(_introTextObject, _isActive);
        SetObjectActive(_startReadyObject, false);
        SetObjectActive(_aimTextObject, false);
        SetObjectActive(_targetAimedTextObject, false);
        SubscribeWeapon();

        if ((_isActive || _isMarkerOnlyActive || _isBarrelTutorialActive) && _levelController != null)
        {
            IReadOnlyList<EnemyHealth> aliveEnemies = _levelController.AliveEnemies;

            for (int i = 0; i < aliveEnemies.Count; i++)
            {
                if (aliveEnemies[i] != null && _targets.Contains(aliveEnemies[i]) == false)
                {
                    _targets.Add(aliveEnemies[i]);
                    aliveEnemies[i].HealthChanged += OnTargetHealthChanged;
                }
            }
        }

        TargetChanged?.Invoke(_currentTarget);

        if (CanRefreshTargets())
            RefreshTargets();
        else
            TargetsChanged?.Invoke(_currentTargets);

        DestructibleTargetsChanged?.Invoke(_currentDestructibleTargets);
        _levelController?.RefreshStartButton();
    }

    public void RegisterEnemy(EnemyHealth enemyHealth)
    {
        if (enemyHealth == null || _targets.Contains(enemyHealth))
            return;

        _targets.Add(enemyHealth);
        enemyHealth.HealthChanged += OnTargetHealthChanged;

        if (CanRefreshTargets())
            RefreshTargets();
        else
            RefreshTargetsForRegisteredContent();
    }

    public void RegisterDestructibleObject(DestructibleObject destructibleObject)
    {
        if (destructibleObject == null || _destructibleTargets.Contains(destructibleObject))
            return;

        _destructibleTargets.Add(destructibleObject);
        destructibleObject.Destroyed += OnDestructibleDestroyed;

        if (CanRefreshTargets())
            RefreshTargets();
    }

    public void RefreshTargetsForRegisteredContent()
    {
        if (_isActive == false && _isMarkerOnlyActive == false && _isBarrelTutorialActive == false && ShouldActivateBarrelTutorialFromContent())
            _isBarrelTutorialActive = true;

        if (CanRefreshTargets())
            RefreshTargets();
    }

    public void UnregisterDestructibleObject(DestructibleObject destructibleObject)
    {
        if (destructibleObject == null)
            return;

        if (_destructibleTargets.Remove(destructibleObject))
            destructibleObject.Destroyed -= OnDestructibleDestroyed;

        if (_currentDestructibleTargets.Remove(destructibleObject))
            DestructibleTargetsChanged?.Invoke(_currentDestructibleTargets);
    }

    public void HandleEnemyKilled(EnemyHealth enemyHealth)
    {
        if (CanRefreshTargets())
            RefreshTargets();
    }

    public void HandleShotCompleted(ShotResult shotResult)
    {
        if ((_isActive || _isMarkerOnlyActive || _isBarrelTutorialActive) && IsKillShot(shotResult))
            RefreshTargets();
    }

    public void HandleGameplayStarted()
    {
        if (_isActive == false)
            return;

        _isGameplayStarted = true;
        SetObjectActive(_introTextObject, false);
        SetObjectActive(_startReadyObject, false);
        SetObjectActive(_aimTextObject, true);
        SetObjectActive(_targetAimedTextObject, false);
        SubscribeWeapon();
        UpdateAimText();
    }

    public void HandleReturnedToPreview()
    {
        _isGameplayStarted = false;
        SetObjectActive(_aimTextObject, false);
        SetObjectActive(_targetAimedTextObject, false);
    }

    public bool CanDamageEnemy(EnemyHealth enemyHealth)
    {
        if (enemyHealth == null)
            return true;

        if (_isActive)
        {
            if (enemyHealth == _currentTarget)
                return true;

            enemyHealth.RestoreFullHealth();
            WrongTarget?.Invoke();
            return false;
        }

        if (HasCurrentDestructibleTargets())
        {
            WrongTarget?.Invoke();
            return false;
        }

        if (HasCurrentEnemyTargets() == false || ContainsTarget(_currentTargets, enemyHealth))
            return true;

        enemyHealth.RestoreFullHealth();
        WrongTarget?.Invoke();
        return false;
    }

    public bool CanDamageDestructibleObject(DestructibleObject destructibleObject)
    {
        if (destructibleObject == null)
            return true;

        if (_isActive == false && _isBarrelTutorialActive == false)
            return true;

        if (_isBarrelTutorialActive && GetAliveTargetsCount() > BarrelTutorialBossTargetsCount)
        {
            WrongTarget?.Invoke();
            return false;
        }

        if (HasCurrentEnemyTargets())
        {
            WrongTarget?.Invoke();
            return false;
        }

        if (HasCurrentDestructibleTargets() == false || ContainsTarget(_currentDestructibleTargets, destructibleObject))
            return true;

        WrongTarget?.Invoke();
        return false;
    }

    public void HandleLevelFinished()
    {
        _isActive = false;
        _isMarkerOnlyActive = false;
        _isBarrelTutorialActive = false;
        _isGameplayStarted = false;
        _isStartButtonUnlocked = true;
        _isWaitingForScreenRelease = false;
        ClearTargets();
        _currentTargets.Clear();
        _currentDestructibleTargets.Clear();
        _currentTarget = null;
        SetObjectActive(_introTextObject, false);
        SetObjectActive(_startReadyObject, false);
        SetObjectActive(_aimTextObject, false);
        SetObjectActive(_targetAimedTextObject, false);
        TargetChanged?.Invoke(_currentTarget);
        TargetsChanged?.Invoke(_currentTargets);
        DestructibleTargetsChanged?.Invoke(_currentDestructibleTargets);
    }

    private void CompleteIntro()
    {
        _isWaitingForScreenRelease = false;
        _isStartButtonUnlocked = true;
        SetObjectActive(_introTextObject, false);
        SetObjectActive(_startReadyObject, true);
        _levelController?.RefreshStartButton();
        RefreshTargets();
    }

    private void RefreshTargets()
    {
        if (_isMarkerOnlyActive)
        {
            RefreshMarkerTargets();
            return;
        }

        if (_isBarrelTutorialActive)
        {
            RefreshBarrelTutorialTargets();
            return;
        }

        RefreshSingleTarget();
    }

    private void RefreshSingleTarget()
    {
        EnemyHealth target = null;
        int damage = Global.PlayerWeapon != null ? Global.PlayerWeapon.Damage : 1;

        for (int i = 0; i < _targets.Count; i++)
        {
            EnemyHealth enemy = _targets[i];

            if (enemy == null || enemy.IsDead)
                continue;

            if (enemy.Health <= damage)
            {
                target = enemy;
                break;
            }

            if (target == null)
                target = enemy;
        }

        if (target == null && _targets.Count > 0 && HasAliveTargets() == false)
            _isActive = false;

        bool targetChanged = _currentTarget != target;
        _currentTarget = target;
        SetSingleCurrentTarget(_currentTarget);

        if (targetChanged)
            TargetChanged?.Invoke(_currentTarget);

        UpdateAimText();
    }

    private void RefreshMarkerTargets()
    {
        _currentTarget = null;
        TargetChanged?.Invoke(_currentTarget);
        _currentTargets.Clear();

        int damage = Global.PlayerWeapon != null ? Global.PlayerWeapon.Damage : 1;
        EnemyHealth fallbackTarget = null;

        for (int i = 0; i < _targets.Count; i++)
        {
            EnemyHealth enemy = _targets[i];

            if (enemy == null || enemy.IsDead)
                continue;

            if (enemy.Health <= damage)
                _currentTargets.Add(enemy);
            else if (fallbackTarget == null)
                fallbackTarget = enemy;
        }

        if (_currentTargets.Count <= 0 && fallbackTarget != null)
            _currentTargets.Add(fallbackTarget);

        if (_currentTargets.Count <= 0 && _targets.Count > 0 && HasAliveTargets() == false)
            _isMarkerOnlyActive = false;

        TargetsChanged?.Invoke(_currentTargets);
    }

    private void RefreshBarrelTutorialTargets()
    {
        _currentTarget = null;
        TargetChanged?.Invoke(_currentTarget);
        _currentTargets.Clear();
        _currentDestructibleTargets.Clear();

        int damage = Global.PlayerWeapon != null ? Global.PlayerWeapon.Damage : 1;
        int aliveBossesCount = 0;

        for (int i = 0; i < _targets.Count; i++)
        {
            EnemyHealth enemy = _targets[i];

            if (enemy == null || enemy.IsDead)
                continue;

            if (enemy.IsBoss)
            {
                aliveBossesCount++;
                continue;
            }

            if (enemy.Health <= damage)
                _currentTargets.Add(enemy);
        }

        if (_currentTargets.Count <= 0 && aliveBossesCount == BarrelTutorialBossTargetsCount)
            AddAliveDamageDestructibles();

        if (_currentTargets.Count <= 0 && _currentDestructibleTargets.Count <= 0 && HasAliveTargets() == false)
            _isBarrelTutorialActive = false;

        TargetsChanged?.Invoke(_currentTargets);
        DestructibleTargetsChanged?.Invoke(_currentDestructibleTargets);
    }

    private void AddAliveDamageDestructibles()
    {
        for (int i = 0; i < _destructibleTargets.Count; i++)
        {
            DestructibleObject destructibleObject = _destructibleTargets[i];

            if (destructibleObject == null || destructibleObject.IsDestroyed || destructibleObject.DealsDamage == false)
                continue;

            _currentDestructibleTargets.Add(destructibleObject);
        }
    }

    private void SetSingleCurrentTarget(EnemyHealth target)
    {
        _currentTargets.Clear();

        if (target != null)
            _currentTargets.Add(target);

        TargetsChanged?.Invoke(_currentTargets);
    }

    private bool HasAliveTargets()
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            EnemyHealth enemy = _targets[i];

            if (enemy != null && enemy.IsDead == false)
                return true;
        }

        return false;
    }

    private int GetAliveTargetsCount()
    {
        int count = 0;

        for (int i = 0; i < _targets.Count; i++)
        {
            EnemyHealth enemy = _targets[i];

            if (enemy != null && enemy.IsDead == false)
                count++;
        }

        return count;
    }

    private bool HasCurrentEnemyTargets()
    {
        for (int i = 0; i < _currentTargets.Count; i++)
        {
            EnemyHealth enemy = _currentTargets[i];

            if (enemy != null && enemy.IsDead == false)
                return true;
        }

        return false;
    }

    private bool HasCurrentDestructibleTargets()
    {
        for (int i = 0; i < _currentDestructibleTargets.Count; i++)
        {
            DestructibleObject destructibleObject = _currentDestructibleTargets[i];

            if (destructibleObject != null && destructibleObject.IsDestroyed == false)
                return true;
        }

        return false;
    }

    private bool ContainsTarget(IReadOnlyList<EnemyHealth> targets, EnemyHealth enemyHealth)
    {
        if (targets == null || enemyHealth == null)
            return false;

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == enemyHealth)
                return true;
        }

        return false;
    }

    private bool ContainsTarget(IReadOnlyList<DestructibleObject> targets, DestructibleObject destructibleObject)
    {
        if (targets == null || destructibleObject == null)
            return false;

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == destructibleObject)
                return true;
        }

        return false;
    }

    private void SubscribeWeapon()
    {
        if (_subscribedWeapon == Global.PlayerWeapon)
            return;

        UnsubscribeWeapon();
        _subscribedWeapon = Global.PlayerWeapon;

        if (_subscribedWeapon != null)
        {
            _subscribedWeapon.AimedEnemyChanged += OnAimedEnemyChanged;
            _subscribedWeapon.DamageChanged += OnWeaponDamageChanged;

            if (CanRefreshTargets())
                RefreshTargets();

            UpdateAimText();
        }
    }

    private void UnsubscribeWeapon()
    {
        if (_subscribedWeapon == null)
            return;

        _subscribedWeapon.AimedEnemyChanged -= OnAimedEnemyChanged;
        _subscribedWeapon.DamageChanged -= OnWeaponDamageChanged;
        _subscribedWeapon = null;
    }

    private void OnTargetHealthChanged(int health, int maxHealth)
    {
        if (CanRefreshTargets())
            RefreshTargets();
    }

    private void OnDestructibleDestroyed(DestructibleObject destructibleObject)
    {
        UnregisterDestructibleObject(destructibleObject);

        if (CanRefreshTargets())
            RefreshTargets();
    }

    private void OnAimedEnemyChanged(EnemyHealth enemyHealth)
    {
        UpdateAimText();
    }

    private void OnWeaponDamageChanged(int damage)
    {
        if (CanRefreshTargets())
            RefreshTargets();
    }

    private void UpdateAimText()
    {
        if (_isActive == false || _isGameplayStarted == false)
            return;

        bool isTargetAimed = _subscribedWeapon != null && _subscribedWeapon.AimedEnemy == _currentTarget && _currentTarget != null;
        SetObjectActive(_aimTextObject, isTargetAimed == false);
        SetObjectActive(_targetAimedTextObject, isTargetAimed);
    }

    private bool IsPointerPressed()
    {
        return (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            || (Mouse.current != null && Mouse.current.leftButton.isPressed);
    }

    private bool IsKillShot(ShotResult shotResult)
    {
        return shotResult == ShotResult.Kill || shotResult == ShotResult.OneShotKill;
    }

    private bool CanRefreshTargets()
    {
        return _isMarkerOnlyActive || _isBarrelTutorialActive || (_isActive && _isStartButtonUnlocked);
    }

    private bool ShouldActivateBarrelTutorialFromContent()
    {
        if (_levelController == null)
            return false;

        return IsFirstPassBarrelTutorialLevel(_levelIndex)
            && HasAliveBoss()
            && HasAliveRegularEnemy()
            && HasAliveDamageDestructible();
    }

    private bool IsFirstPassTutorialLevel(int levelIndex, int tutorialLevelIndex)
    {
        return levelIndex == tutorialLevelIndex && SaveManager.CompletedLevelIndex < levelIndex;
    }

    private bool IsFirstPassMarkerTutorialLevel(int levelIndex)
    {
        return levelIndex > FirstTutorialLevelIndex
            && levelIndex <= LastMarkerTutorialLevelIndex
            && SaveManager.CompletedLevelIndex < levelIndex;
    }

    private bool IsFirstPassBarrelTutorialLevel(int levelIndex)
    {
        int cycleLevelIndex = Mathf.Max(0, levelIndex) % LevelsPerTutorialCycle;
        bool isBarrelTutorialLevel = cycleLevelIndex == BarrelTutorialLevelIndex || cycleLevelIndex == BarrelTutorialLevelIndex + 1;
        return isBarrelTutorialLevel && SaveManager.CompletedLevelIndex < levelIndex;
    }

    private bool HasAliveBoss()
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            EnemyHealth enemy = _targets[i];

            if (enemy != null && enemy.IsDead == false && enemy.IsBoss)
                return true;
        }

        return false;
    }

    private bool HasAliveRegularEnemy()
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            EnemyHealth enemy = _targets[i];

            if (enemy != null && enemy.IsDead == false && enemy.IsBoss == false)
                return true;
        }

        return false;
    }

    private bool HasAliveDamageDestructible()
    {
        for (int i = 0; i < _destructibleTargets.Count; i++)
        {
            DestructibleObject destructibleObject = _destructibleTargets[i];

            if (destructibleObject != null && destructibleObject.IsDestroyed == false && destructibleObject.DealsDamage)
                return true;
        }

        return false;
    }

    private void ClearTargets(bool clearDestructibleTargets = true)
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            if (_targets[i] != null)
                _targets[i].HealthChanged -= OnTargetHealthChanged;
        }

        _targets.Clear();

        if (clearDestructibleTargets == false)
            return;

        for (int i = 0; i < _destructibleTargets.Count; i++)
        {
            if (_destructibleTargets[i] != null)
                _destructibleTargets[i].Destroyed -= OnDestructibleDestroyed;
        }

        _destructibleTargets.Clear();
    }

    private void SetObjectActive(GameObject target, bool isActive)
    {
        if (target != null)
            target.SetActive(isActive);
    }
}
