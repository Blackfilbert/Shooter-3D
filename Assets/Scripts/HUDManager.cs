using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    private const float TutorialArrowPulseScaleMultiplier = 1.5f;

    [SerializeField] private Camera _camera;
    [SerializeField] private RectTransform _worldElementsParent;
    [SerializeField] private EnemyHealthBar _healthBarPrefab;
    [SerializeField] private EnemyKillBonusView _killBonusViewPrefab;
    [SerializeField] private RectTransform _enemyDirectionArrowParent;
    [SerializeField] private EnemyDirectionArrowView _enemyDirectionArrowPrefab;
    [SerializeField] private int _enemyDirectionArrowThreshold = 3;
    [SerializeField] private RectTransform _tutorialTargetPrefab;
    [SerializeField] private Vector3 _tutorialTargetWorldOffset = new Vector3(0f, 2.7f, 0f);
    [SerializeField] private Vector3 _tutorialTargetGameplayWorldOffset = new Vector3(0f, 2.7f, 0f);
    [SerializeField] private Vector2 _tutorialTargetPreviewScreenOffset = new Vector2(0f, 120f);
    [SerializeField] private Vector2 _tutorialTargetGameplayScreenOffset = new Vector2(0f, 120f);
    [SerializeField] private TMP_Text _explosionDamageTextPrefab;
    [SerializeField] private Vector3 _explosionDamageWorldOffset = Vector3.zero;
    [SerializeField] private Vector2 _explosionDamageScreenOffset = Vector2.zero;
    [SerializeField] private float _explosionDamageMoveDistance = 70f;
    [SerializeField] private float _explosionDamageDuration = 0.6f;
    [SerializeField] private TMP_Text _tryAgainText;
    [SerializeField] private float _tryAgainShowDuration = 0.8f;
    [SerializeField] private EnemyHealth[] _initialEnemies;

    private readonly Dictionary<EnemyHealth, EnemyHealthBar> _healthBars = new Dictionary<EnemyHealth, EnemyHealthBar>();
    private readonly Dictionary<EnemyHealth, EnemyKillBonusView> _killBonusViews = new Dictionary<EnemyHealth, EnemyKillBonusView>();
    private readonly Dictionary<DestructibleObject, EnemyKillBonusView> _destructibleBonusViews = new Dictionary<DestructibleObject, EnemyKillBonusView>();
    private readonly List<DestructibleObject> _subscribedDestructibleObjects = new List<DestructibleObject>();
    private readonly Dictionary<EnemyHealth, EnemyDirectionArrowView> _enemyDirectionArrows = new Dictionary<EnemyHealth, EnemyDirectionArrowView>();
    private readonly List<EnemyHealth> _sortedEnemyHUD = new List<EnemyHealth>();
    private PlayerWeapon _subscribedWeapon;
    private GameplayLevelController _levelController;
    private GameplayTutorialManager _tutorialManager;
    private EnemyHealth _previewEnemy;
    private EnemyHealth _tutorialTarget;
    private readonly Dictionary<EnemyHealth, RectTransform> _tutorialTargetViews = new Dictionary<EnemyHealth, RectTransform>();
    private readonly Dictionary<DestructibleObject, RectTransform> _tutorialDestructibleTargetViews = new Dictionary<DestructibleObject, RectTransform>();
    private EnemyDirectionArrowView _tutorialDirectionArrow;
    private Tween _tryAgainTween;
    private bool _wasGameplayStarted;

    public Camera Camera => _camera;
    public RectTransform WorldElementsParent => _worldElementsParent;

    private void Awake()
    {
        Global.RegisterHUDManager(this);
    }

    private void Start()
    {
        if (_tryAgainText != null)
            _tryAgainText.gameObject.SetActive(false);

        SubscribeWeapon();
        SubscribeLevelController();
        SubscribeTutorialManager();

        for (int i = 0; i < _initialEnemies.Length; i++)
            RegisterEnemyHealth(_initialEnemies[i]);
    }

    private void Update()
    {
        SubscribeWeapon();
        SubscribeLevelController();
        SubscribeTutorialManager();
        RefreshTutorialTargetViews();
        RefreshTutorialTargetPosition();
        SortEnemyHUDByDistance();
        RefreshDamagePreviewState();
    }

    private void OnDestroy()
    {
        UnsubscribeWeapon();
        UnsubscribeLevelController();
        UnsubscribeTutorialManager();
        UnsubscribeDestructibleObjects();
        ClearEnemyDirectionArrows();
        ClearTutorialTarget();
        KillTryAgainTween();
        Global.UnregisterHUDManager(this);
    }

    public void RegisterEnemyHealth(EnemyHealth enemyHealth)
    {
        if (enemyHealth == null || _camera == null || _worldElementsParent == null)
            return;

        if (_healthBarPrefab == null && _killBonusViewPrefab == null)
            return;

        if (_healthBars.ContainsKey(enemyHealth) || _killBonusViews.ContainsKey(enemyHealth))
            return;

        EnemyHealthBar healthBar = null;

        if (_healthBarPrefab != null)
        {
            healthBar = Spawn(_healthBarPrefab, _worldElementsParent, Vector2.zero);
            healthBar.Initialize(enemyHealth, _worldElementsParent, _camera);
            _healthBars.Add(enemyHealth, healthBar);
        }

        if (_killBonusViewPrefab != null)
        {
            EnemyKillBonusView killBonusView = Spawn(_killBonusViewPrefab, _worldElementsParent, Vector2.zero);

            if (healthBar != null)
                killBonusView.Initialize(enemyHealth, _worldElementsParent, _camera, healthBar.WorldOffset);
            else
                killBonusView.Initialize(enemyHealth, _worldElementsParent, _camera);

            _killBonusViews.Add(enemyHealth, killBonusView);
        }

        enemyHealth.Died += OnEnemyDied;
        RefreshEnemyDirectionArrows();
    }

    public void UnregisterEnemyHealth(EnemyHealth enemyHealth)
    {
        if (enemyHealth == null)
            return;

        enemyHealth.Died -= OnEnemyDied;

        if (_healthBars.TryGetValue(enemyHealth, out EnemyHealthBar healthBar))
        {
            _healthBars.Remove(enemyHealth);

            if (healthBar != null)
                DestroyHUDObject(healthBar.gameObject);
        }

        if (_previewEnemy == enemyHealth)
            _previewEnemy = null;

        if (_killBonusViews.TryGetValue(enemyHealth, out EnemyKillBonusView killBonusView))
        {
            _killBonusViews.Remove(enemyHealth);

            if (killBonusView != null)
                DestroyHUDObject(killBonusView.gameObject);
        }

        if (_enemyDirectionArrows.TryGetValue(enemyHealth, out EnemyDirectionArrowView arrowView))
        {
            _enemyDirectionArrows.Remove(enemyHealth);

            if (arrowView != null)
                DestroyHUDObject(arrowView.gameObject);
        }

        RefreshEnemyDirectionArrows();
    }

    public void RegisterDestructibleObject(DestructibleObject destructibleObject)
    {
        if (destructibleObject == null || _camera == null || _worldElementsParent == null)
            return;

        if (_subscribedDestructibleObjects.Contains(destructibleObject) == false)
        {
            _subscribedDestructibleObjects.Add(destructibleObject);
            destructibleObject.ExplosionDamageDealt += ShowExplosionDamage;
        }

        if (_killBonusViewPrefab == null || _destructibleBonusViews.ContainsKey(destructibleObject))
            return;

        EnemyKillBonusView killBonusView = Spawn(_killBonusViewPrefab, _worldElementsParent, Vector2.zero);

        if (_healthBarPrefab != null)
            killBonusView.Initialize(destructibleObject, _worldElementsParent, _camera, _healthBarPrefab.WorldOffset, Vector2.zero);
        else
            killBonusView.Initialize(destructibleObject, _worldElementsParent, _camera);

        _destructibleBonusViews.Add(destructibleObject, killBonusView);
    }

    public void UnregisterDestructibleObject(DestructibleObject destructibleObject)
    {
        if (destructibleObject == null)
            return;

        if (_subscribedDestructibleObjects.Remove(destructibleObject))
            destructibleObject.ExplosionDamageDealt -= ShowExplosionDamage;

        if (_destructibleBonusViews.TryGetValue(destructibleObject, out EnemyKillBonusView killBonusView))
        {
            _destructibleBonusViews.Remove(destructibleObject);

            if (killBonusView != null)
                DestroyHUDObject(killBonusView.gameObject);
        }
    }

    private void OnEnemyDied(EnemyHealth enemyHealth)
    {
        UnregisterEnemyHealth(enemyHealth);
    }

    private void ShowExplosionDamage(Vector3 worldPosition, int damage)
    {
        ShowWorldDamage(worldPosition, damage);
    }

    public void ShowWorldDamage(Vector3 worldPosition, int damage)
    {
        if (_explosionDamageTextPrefab == null || _worldElementsParent == null || damage < 0)
            return;

        Vector3 targetPosition = worldPosition + _explosionDamageWorldOffset;

        if (IsWorldPositionInFrontOfCamera(targetPosition) == false)
            return;

        TMP_Text damageText = SpawnAtWorldPosition(_explosionDamageTextPrefab, targetPosition, _worldElementsParent);
        RectTransform rectTransform = damageText.transform as RectTransform;

        if (rectTransform == null)
        {
            DestroyHUDObject(damageText.gameObject);
            return;
        }

        damageText.text = CompactNumberFormatter.Format(damage);
        rectTransform.anchoredPosition += _explosionDamageScreenOffset;
        rectTransform.localScale = Vector3.zero;
        CanvasGroup canvasGroup = damageText.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = damageText.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        Vector2 endPosition = rectTransform.anchoredPosition + Vector2.up * _explosionDamageMoveDistance;
        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Append(rectTransform.DOScale(Vector3.one, _explosionDamageDuration * 0.25f).SetEase(Ease.OutBack));
        sequence.Join(canvasGroup.DOFade(1f, _explosionDamageDuration * 0.2f).SetEase(Ease.OutQuad));
        sequence.Join(rectTransform.DOAnchorPos(endPosition, _explosionDamageDuration).SetEase(Ease.OutQuad));
        sequence.Insert(_explosionDamageDuration * 0.35f, canvasGroup.DOFade(0f, _explosionDamageDuration * 0.65f).SetEase(Ease.InQuad));
        sequence.OnComplete(() => DestroyHUDObject(damageText.gameObject));
    }

    private void SubscribeWeapon()
    {
        if (_subscribedWeapon != null || Global.PlayerWeapon == null)
            return;

        _subscribedWeapon = Global.PlayerWeapon;
        _subscribedWeapon.AimedEnemyChanged += OnAimedEnemyChanged;
        _subscribedWeapon.DamageChanged += OnWeaponDamageChanged;
        OnAimedEnemyChanged(_subscribedWeapon.AimedEnemy);
    }

    private void SubscribeLevelController()
    {
        if (_levelController != null || Global.GameplayLevelController == null)
            return;

        _levelController = Global.GameplayLevelController;
        _levelController.LevelLoaded += OnLevelLoaded;
        _levelController.EnemyRegistered += OnEnemyRegistered;
        _levelController.EnemyKilled += OnEnemyKilled;
        _levelController.LevelWon += ClearEnemyDirectionArrows;
        _levelController.LevelLost += ClearEnemyDirectionArrows;
        RefreshEnemyDirectionArrows();
    }

    private void SubscribeTutorialManager()
    {
        if (_tutorialManager != null || Global.GameplayTutorialManager == null)
            return;

        _tutorialManager = Global.GameplayTutorialManager;
        _tutorialManager.TargetChanged += OnTutorialTargetChanged;
        _tutorialManager.TargetsChanged += OnTutorialTargetsChanged;
        _tutorialManager.DestructibleTargetsChanged += OnTutorialDestructibleTargetsChanged;
        _tutorialManager.WrongTarget += ShowTryAgain;
        OnTutorialTargetChanged(_tutorialManager.CurrentTarget);
        OnTutorialTargetsChanged(_tutorialManager.CurrentTargets);
        OnTutorialDestructibleTargetsChanged(_tutorialManager.CurrentDestructibleTargets);
    }

    private void UnsubscribeLevelController()
    {
        if (_levelController == null)
            return;

        _levelController.LevelLoaded -= OnLevelLoaded;
        _levelController.EnemyRegistered -= OnEnemyRegistered;
        _levelController.EnemyKilled -= OnEnemyKilled;
        _levelController.LevelWon -= ClearEnemyDirectionArrows;
        _levelController.LevelLost -= ClearEnemyDirectionArrows;
        _levelController = null;
    }

    private void UnsubscribeTutorialManager()
    {
        if (_tutorialManager == null)
            return;

        _tutorialManager.TargetChanged -= OnTutorialTargetChanged;
        _tutorialManager.TargetsChanged -= OnTutorialTargetsChanged;
        _tutorialManager.DestructibleTargetsChanged -= OnTutorialDestructibleTargetsChanged;
        _tutorialManager.WrongTarget -= ShowTryAgain;
        _tutorialManager = null;
    }

    private void UnsubscribeDestructibleObjects()
    {
        for (int i = 0; i < _subscribedDestructibleObjects.Count; i++)
        {
            if (_subscribedDestructibleObjects[i] != null)
                _subscribedDestructibleObjects[i].ExplosionDamageDealt -= ShowExplosionDamage;
        }

        _subscribedDestructibleObjects.Clear();
    }

    private void OnLevelLoaded(int levelIndex)
    {
        ClearTutorialTarget();
        ClearEnemyDirectionArrows();
        RefreshEnemyDirectionArrows();

        if (_tutorialManager != null)
        {
            OnTutorialTargetChanged(_tutorialManager.CurrentTarget);
            OnTutorialTargetsChanged(_tutorialManager.CurrentTargets);
            OnTutorialDestructibleTargetsChanged(_tutorialManager.CurrentDestructibleTargets);
        }
    }

    private void OnEnemyRegistered(EnemyHealth enemyHealth)
    {
        RefreshEnemyDirectionArrows();
    }

    private void OnEnemyKilled(EnemyHealth enemyHealth)
    {
        RefreshEnemyDirectionArrows();
    }

    private void OnTutorialTargetChanged(EnemyHealth enemyHealth)
    {
        _tutorialTarget = enemyHealth;
        RefreshTutorialDirectionArrow();
    }

    private void OnTutorialTargetsChanged(IReadOnlyList<EnemyHealth> targets)
    {
        RemoveStaleTutorialTargetViews(targets);

        if (targets == null || targets.Count <= 0)
            return;

        for (int i = 0; i < targets.Count; i++)
            AddTutorialTargetView(targets[i]);

        RefreshTutorialTargetPosition();
    }

    private void OnTutorialDestructibleTargetsChanged(IReadOnlyList<DestructibleObject> targets)
    {
        RemoveStaleTutorialDestructibleTargetViews(targets);

        if (targets == null || targets.Count <= 0)
            return;

        for (int i = 0; i < targets.Count; i++)
            AddTutorialDestructibleTargetView(targets[i]);

        RefreshTutorialTargetPosition();
    }

    private void RefreshTutorialTargetViews()
    {
        if (_tutorialManager == null)
            return;

        OnTutorialTargetsChanged(_tutorialManager.CurrentTargets);
        OnTutorialDestructibleTargetsChanged(_tutorialManager.CurrentDestructibleTargets);
    }

    private void RefreshTutorialTargetPosition()
    {
        if (_camera == null || _worldElementsParent == null)
            return;

        foreach (KeyValuePair<EnemyHealth, RectTransform> pair in _tutorialTargetViews)
        {
            EnemyHealth enemy = pair.Key;
            RectTransform targetView = pair.Value;

            if (enemy == null || targetView == null)
                continue;

            Vector3 worldPosition = enemy.transform.position + GetTutorialTargetWorldOffset(enemy);
            bool isVisible = IsWorldPositionInFrontOfCamera(worldPosition);
            targetView.gameObject.SetActive(isVisible);

            if (isVisible)
                targetView.anchoredPosition = WorldToAnchoredPosition(worldPosition) + GetTutorialTargetScreenOffset();
        }

        foreach (KeyValuePair<DestructibleObject, RectTransform> pair in _tutorialDestructibleTargetViews)
        {
            DestructibleObject destructibleObject = pair.Key;
            RectTransform targetView = pair.Value;

            if (destructibleObject == null || targetView == null)
                continue;

            Vector3 worldPosition = destructibleObject.transform.position + GetTutorialTargetWorldOffset(null);
            bool isVisible = IsWorldPositionInFrontOfCamera(worldPosition);
            targetView.gameObject.SetActive(isVisible);

            if (isVisible)
                targetView.anchoredPosition = WorldToAnchoredPosition(worldPosition) + GetTutorialTargetScreenOffset();
        }
    }

    private Vector3 GetTutorialTargetWorldOffset(EnemyHealth enemyHealth)
    {
        if (enemyHealth != null && _healthBars.TryGetValue(enemyHealth, out EnemyHealthBar healthBar) && healthBar != null)
            return healthBar.WorldOffset;

        if (_levelController != null && _levelController.IsGameplayStarted)
            return _tutorialTargetGameplayWorldOffset;

        return _tutorialTargetWorldOffset;
    }

    private Vector2 GetTutorialTargetScreenOffset()
    {
        if (_levelController != null && _levelController.IsGameplayStarted)
            return _tutorialTargetGameplayScreenOffset;

        return _tutorialTargetPreviewScreenOffset;
    }

    private void AddTutorialTargetView(EnemyHealth enemyHealth)
    {
        if (enemyHealth == null || enemyHealth.IsDead || _tutorialTargetPrefab == null || _worldElementsParent == null)
            return;

        if (_tutorialTargetViews.ContainsKey(enemyHealth))
            return;

        RectTransform targetView = Spawn(_tutorialTargetPrefab, _worldElementsParent, Vector2.zero);
        _tutorialTargetViews.Add(enemyHealth, targetView);
    }

    private void AddTutorialDestructibleTargetView(DestructibleObject destructibleObject)
    {
        if (destructibleObject == null || destructibleObject.IsDestroyed || _tutorialTargetPrefab == null || _worldElementsParent == null)
            return;

        if (_tutorialDestructibleTargetViews.ContainsKey(destructibleObject))
            return;

        RectTransform targetView = Spawn(_tutorialTargetPrefab, _worldElementsParent, Vector2.zero);
        _tutorialDestructibleTargetViews.Add(destructibleObject, targetView);
    }

    private void RemoveStaleTutorialTargetViews(IReadOnlyList<EnemyHealth> targets)
    {
        List<EnemyHealth> targetsToRemove = null;

        foreach (KeyValuePair<EnemyHealth, RectTransform> pair in _tutorialTargetViews)
        {
            if (pair.Key != null && pair.Key.IsDead == false && ContainsTarget(targets, pair.Key))
                continue;

            if (targetsToRemove == null)
                targetsToRemove = new List<EnemyHealth>();

            targetsToRemove.Add(pair.Key);
        }

        if (targetsToRemove == null)
            return;

        for (int i = 0; i < targetsToRemove.Count; i++)
        {
            EnemyHealth enemyHealth = targetsToRemove[i];

            if (_tutorialTargetViews.TryGetValue(enemyHealth, out RectTransform targetView))
            {
                _tutorialTargetViews.Remove(enemyHealth);

                if (targetView != null)
                    DestroyHUDObject(targetView.gameObject);
            }
        }
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

    private void RemoveStaleTutorialDestructibleTargetViews(IReadOnlyList<DestructibleObject> targets)
    {
        List<DestructibleObject> targetsToRemove = null;

        foreach (KeyValuePair<DestructibleObject, RectTransform> pair in _tutorialDestructibleTargetViews)
        {
            if (pair.Key != null && pair.Key.IsDestroyed == false && ContainsTarget(targets, pair.Key))
                continue;

            if (targetsToRemove == null)
                targetsToRemove = new List<DestructibleObject>();

            targetsToRemove.Add(pair.Key);
        }

        if (targetsToRemove == null)
            return;

        for (int i = 0; i < targetsToRemove.Count; i++)
        {
            DestructibleObject destructibleObject = targetsToRemove[i];

            if (_tutorialDestructibleTargetViews.TryGetValue(destructibleObject, out RectTransform targetView))
            {
                _tutorialDestructibleTargetViews.Remove(destructibleObject);

                if (targetView != null)
                    DestroyHUDObject(targetView.gameObject);
            }
        }
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

    private void ClearTutorialTarget()
    {
        _tutorialTarget = null;

        foreach (KeyValuePair<EnemyHealth, RectTransform> pair in _tutorialTargetViews)
        {
            if (pair.Value != null)
                DestroyHUDObject(pair.Value.gameObject);
        }

        _tutorialTargetViews.Clear();

        foreach (KeyValuePair<DestructibleObject, RectTransform> pair in _tutorialDestructibleTargetViews)
        {
            if (pair.Value != null)
                DestroyHUDObject(pair.Value.gameObject);
        }

        _tutorialDestructibleTargetViews.Clear();

        if (_tutorialDirectionArrow != null)
            DestroyHUDObject(_tutorialDirectionArrow.gameObject);

        _tutorialDirectionArrow = null;
    }

    private void RefreshTutorialDirectionArrow()
    {
        RectTransform arrowParent = GetEnemyDirectionArrowParent();

        if (_tutorialDirectionArrow != null)
        {
            DestroyHUDObject(_tutorialDirectionArrow.gameObject);
            _tutorialDirectionArrow = null;
        }

        if (_tutorialManager == null || _tutorialManager.IsActive == false || _tutorialTarget == null || _enemyDirectionArrowPrefab == null || arrowParent == null || _camera == null)
            return;

        _tutorialDirectionArrow = Spawn(_enemyDirectionArrowPrefab, arrowParent, Vector2.zero);
        EnsureScalePulse(_tutorialDirectionArrow, TutorialArrowPulseScaleMultiplier);
        _tutorialDirectionArrow.Initialize(_tutorialTarget, arrowParent, _camera);
        RemoveEnemyDirectionArrow(_tutorialTarget);
    }

    private void ShowTryAgain()
    {
        if (_tryAgainText == null)
            return;

        KillTryAgainTween();
        _tryAgainText.gameObject.SetActive(true);
        _tryAgainText.alpha = 1f;
        _tryAgainTween = DOVirtual.DelayedCall(_tryAgainShowDuration, HideTryAgain).SetUpdate(true);
    }

    private void HideTryAgain()
    {
        if (_tryAgainText != null)
            _tryAgainText.gameObject.SetActive(false);

        _tryAgainTween = null;
    }

    private void KillTryAgainTween()
    {
        if (_tryAgainTween == null)
            return;

        _tryAgainTween.Kill();
        _tryAgainTween = null;
    }

    private void UnsubscribeWeapon()
    {
        if (_subscribedWeapon == null)
            return;

        _subscribedWeapon.AimedEnemyChanged -= OnAimedEnemyChanged;
        _subscribedWeapon.DamageChanged -= OnWeaponDamageChanged;
        ClearDamagePreview();
        _subscribedWeapon = null;
    }

    private void OnAimedEnemyChanged(EnemyHealth enemyHealth)
    {
        ClearDamagePreview();
        _previewEnemy = enemyHealth;
        ApplyDamagePreview();
    }

    private void OnWeaponDamageChanged(int damage)
    {
        ApplyDamagePreview();
    }

    private void ApplyDamagePreview()
    {
        if (_previewEnemy == null || _subscribedWeapon == null || _levelController == null || _levelController.IsGameplayStarted == false)
            return;

        if (_healthBars.TryGetValue(_previewEnemy, out EnemyHealthBar healthBar) && healthBar != null)
            healthBar.SetDamagePreview(_subscribedWeapon.Damage);
    }

    private void ClearDamagePreview()
    {
        if (_previewEnemy == null)
            return;

        if (_healthBars.TryGetValue(_previewEnemy, out EnemyHealthBar healthBar) && healthBar != null)
            healthBar.ClearDamagePreview();
    }

    private void RefreshDamagePreviewState()
    {
        bool isGameplayStarted = _levelController != null && _levelController.IsGameplayStarted;

        if (_wasGameplayStarted == isGameplayStarted)
            return;

        _wasGameplayStarted = isGameplayStarted;

        if (_wasGameplayStarted)
            ApplyDamagePreview();
        else
            ClearDamagePreview();
    }

    private void SortEnemyHUDByDistance()
    {
        if (_camera == null || _worldElementsParent == null)
            return;

        _sortedEnemyHUD.Clear();

        foreach (KeyValuePair<EnemyHealth, EnemyHealthBar> pair in _healthBars)
        {
            if (pair.Key != null)
                _sortedEnemyHUD.Add(pair.Key);
        }

        foreach (KeyValuePair<EnemyHealth, EnemyKillBonusView> pair in _killBonusViews)
        {
            if (pair.Key != null && _sortedEnemyHUD.Contains(pair.Key) == false)
                _sortedEnemyHUD.Add(pair.Key);
        }

        for (int i = 1; i < _sortedEnemyHUD.Count; i++)
        {
            EnemyHealth enemy = _sortedEnemyHUD[i];
            float distance = GetEnemyHUDDistance(enemy);
            int index = i - 1;

            while (index >= 0 && GetEnemyHUDDistance(_sortedEnemyHUD[index]) < distance)
            {
                _sortedEnemyHUD[index + 1] = _sortedEnemyHUD[index];
                index--;
            }

            _sortedEnemyHUD[index + 1] = enemy;
        }

        foreach (KeyValuePair<EnemyHealth, RectTransform> pair in _tutorialTargetViews)
        {
            if (pair.Value != null)
                pair.Value.SetAsLastSibling();
        }

        foreach (KeyValuePair<DestructibleObject, RectTransform> pair in _tutorialDestructibleTargetViews)
        {
            if (pair.Value != null)
                pair.Value.SetAsLastSibling();
        }

        for (int i = 0; i < _sortedEnemyHUD.Count; i++)
        {
            EnemyHealth enemy = _sortedEnemyHUD[i];

            if (_healthBars.TryGetValue(enemy, out EnemyHealthBar healthBar) && healthBar != null)
                healthBar.transform.SetAsLastSibling();

            if (_killBonusViews.TryGetValue(enemy, out EnemyKillBonusView killBonusView) && killBonusView != null)
                killBonusView.transform.SetAsLastSibling();
        }
    }

    private float GetEnemyHUDDistance(EnemyHealth enemyHealth)
    {
        if (enemyHealth == null || _camera == null)
            return 0f;

        return (enemyHealth.transform.position - _camera.transform.position).sqrMagnitude;
    }

    private void RefreshEnemyDirectionArrows()
    {
        RectTransform arrowParent = GetEnemyDirectionArrowParent();

        if (_levelController == null || _enemyDirectionArrowPrefab == null || arrowParent == null || _camera == null)
            return;

        if (IsTutorialFlowActive())
        {
            ClearEnemyDirectionArrows();
            return;
        }

        IReadOnlyList<EnemyHealth> aliveEnemies = _levelController.AliveEnemies;
        int threshold = Mathf.Max(0, _enemyDirectionArrowThreshold);

        if (aliveEnemies.Count > threshold)
        {
            ClearEnemyDirectionArrows();
            return;
        }

        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            EnemyHealth enemyHealth = aliveEnemies[i];

            if (enemyHealth == null || enemyHealth.IsDead || IsTutorialDirectionArrowTarget(enemyHealth) || _enemyDirectionArrows.ContainsKey(enemyHealth))
                continue;

            EnemyDirectionArrowView arrowView = Spawn(_enemyDirectionArrowPrefab, arrowParent, Vector2.zero);
            arrowView.Initialize(enemyHealth, arrowParent, _camera);
            _enemyDirectionArrows.Add(enemyHealth, arrowView);
        }

        RemoveDeadEnemyDirectionArrows();
    }

    private void RemoveDeadEnemyDirectionArrows()
    {
        List<EnemyHealth> enemiesToRemove = null;

        foreach (KeyValuePair<EnemyHealth, EnemyDirectionArrowView> pair in _enemyDirectionArrows)
        {
            if (pair.Key != null && pair.Key.IsDead == false && IsAliveEnemy(pair.Key) && IsTutorialDirectionArrowTarget(pair.Key) == false)
                continue;

            if (enemiesToRemove == null)
                enemiesToRemove = new List<EnemyHealth>();

            enemiesToRemove.Add(pair.Key);
        }

        if (enemiesToRemove == null)
            return;

        for (int i = 0; i < enemiesToRemove.Count; i++)
        {
            EnemyHealth enemyHealth = enemiesToRemove[i];

            if (_enemyDirectionArrows.TryGetValue(enemyHealth, out EnemyDirectionArrowView arrowView))
            {
                _enemyDirectionArrows.Remove(enemyHealth);

                if (arrowView != null)
                    DestroyHUDObject(arrowView.gameObject);
            }
        }
    }

    private void ClearEnemyDirectionArrows()
    {
        foreach (KeyValuePair<EnemyHealth, EnemyDirectionArrowView> pair in _enemyDirectionArrows)
        {
            if (pair.Value != null)
                DestroyHUDObject(pair.Value.gameObject);
        }

        _enemyDirectionArrows.Clear();
    }

    private void RemoveEnemyDirectionArrow(EnemyHealth enemyHealth)
    {
        if (enemyHealth == null)
            return;

        if (_enemyDirectionArrows.TryGetValue(enemyHealth, out EnemyDirectionArrowView arrowView) == false)
            return;

        _enemyDirectionArrows.Remove(enemyHealth);

        if (arrowView != null)
            DestroyHUDObject(arrowView.gameObject);
    }

    private bool IsTutorialDirectionArrowTarget(EnemyHealth enemyHealth)
    {
        return _tutorialDirectionArrow != null && _tutorialTarget == enemyHealth;
    }

    private bool IsTutorialFlowActive()
    {
        return _tutorialManager != null && _tutorialManager.IsTutorialFlowActive;
    }

    private bool IsAliveEnemy(EnemyHealth enemyHealth)
    {
        if (_levelController == null || enemyHealth == null)
            return false;

        IReadOnlyList<EnemyHealth> aliveEnemies = _levelController.AliveEnemies;

        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            if (aliveEnemies[i] == enemyHealth)
                return true;
        }

        return false;
    }

    private RectTransform GetEnemyDirectionArrowParent()
    {
        return _enemyDirectionArrowParent != null ? _enemyDirectionArrowParent : _worldElementsParent;
    }

    public T Spawn<T>(T prefab, RectTransform parent, Vector2 anchoredPosition) where T : Component
    {
        T instance = Instantiate(prefab, parent);
        RectTransform rectTransform = instance.transform as RectTransform;

        if (rectTransform != null)
            rectTransform.anchoredPosition = anchoredPosition;

        return instance;
    }

    public T SpawnAtWorldPosition<T>(T prefab, Vector3 worldPosition, RectTransform parent = null) where T : Component
    {
        RectTransform targetParent = parent != null ? parent : _worldElementsParent;
        Vector2 anchoredPosition = WorldToAnchoredPosition(worldPosition, targetParent);
        return Spawn(prefab, targetParent, anchoredPosition);
    }

    public void FadeAndDestroy(GameObject target, float duration = 0.2f)
    {
        if (target == null)
            return;

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = target.AddComponent<CanvasGroup>();

        canvasGroup.DOKill();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.DOFade(0f, duration).SetEase(Ease.OutQuad).OnComplete(() => DestroyHUDObject(target));
    }

    public void DestroyHUDObject(GameObject target)
    {
        if (target == null)
            return;

        target.transform.DOKill();

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();

        if (canvasGroup != null)
            canvasGroup.DOKill();

        Destroy(target);
    }

    public Vector2 WorldToAnchoredPosition(Vector3 worldPosition, RectTransform parent = null)
    {
        RectTransform targetParent = parent != null ? parent : _worldElementsParent;

        if (_camera == null || targetParent == null)
            return Vector2.zero;

        Vector3 viewportPosition = _camera.WorldToViewportPoint(worldPosition);
        Rect parentRect = targetParent.rect;

        return new Vector2(
            (viewportPosition.x - 0.5f) * parentRect.width,
            (viewportPosition.y - 0.5f) * parentRect.height);
    }

    public bool IsWorldPositionInFrontOfCamera(Vector3 worldPosition)
    {
        if (_camera == null)
            return false;

        return _camera.WorldToViewportPoint(worldPosition).z > 0f;
    }

    private void EnsureScalePulse(Component target, float scaleMultiplier)
    {
        if (target == null)
            return;

        ScalePulse scalePulse = target.GetComponent<ScalePulse>();

        if (scalePulse == null)
            scalePulse = target.gameObject.AddComponent<ScalePulse>();

        scalePulse.Configure(scaleMultiplier);
    }
}
