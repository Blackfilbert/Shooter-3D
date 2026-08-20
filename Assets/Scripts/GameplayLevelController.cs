using System;
using System.Collections.Generic;
using Hookah.Analytics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameplayLevelController : MonoBehaviour
{
    private const int SRankRequiredStartLevelIndex = 3;
    private const float WinAfterKillCameraMaxWait = 5f;
    private const int SpecialLevelSoftReward = 200;

    [SerializeField] private GameplayLevelsConfig _levelsConfig;
    [SerializeField] private GearPacksConfig _gearPacksConfig;
    [SerializeField] private Transform _levelParent;
    [SerializeField] private bool _loadLevelOnStart;
    [SerializeField] private int _startLevelIndex;
    [SerializeField] private GameObject _winScreen;
    [SerializeField] private GameObject _loseScreen;
    [SerializeField] private GameObject _playerUI;
    [SerializeField] private GameObject _previewCameraObject;
    [SerializeField] private LevelCompletionRewardViewController _levelRewardsView;
    [SerializeField] private GameplayExitView _exitView;
    [SerializeField] private Button _startButton;
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private float _previewFov = 30f;
    [SerializeField] private float _gameplayFov = 2f;
    [SerializeField] private string _menuSceneName;

    private GameObject _currentLevel;
    private int _currentLevelIndex = -1;
    private readonly List<EnemyHealth> _aliveEnemies = new List<EnemyHealth>();
    private readonly List<DestructibleObject> _destructibleObjects = new List<DestructibleObject>();
    private Coroutine _winCoroutine;
    private Coroutine _resultScreenCoroutine;
    private Coroutine _startButtonPressCoroutine;
    private PlayerHealth _subscribedPlayerHealth;
    private PlayerWeapon _subscribedPlayerWeapon;
    private EnemyDeathCameraController _subscribedDeathCameraController;
    private EventTrigger _startButtonTrigger;
    private bool _isStartButtonTriggerSetup;
    private bool _isLevelFinished;
    private bool _isGameplayStarted;
    private bool _isWaitingForKillCamera;
    private bool _isWinScheduled;
    private bool _isSpecialLevel;

    public int CurrentLevelIndex => _currentLevelIndex;
    public GameObject CurrentLevel => _currentLevel;
    public bool IsLevelFinished => _isLevelFinished;
    public bool IsGameplayStarted => _isGameplayStarted;
    public bool IsWinScheduled => _isWinScheduled;
    public bool IsSpecialLevel => _isSpecialLevel;
    public float PreviewFov => _previewFov;
    public bool ShouldUseProjectileKillCamera => _isSpecialLevel == false && _currentLevel != null && _isLevelFinished == false && _isGameplayStarted && _aliveEnemies.Count == 1;

    public event Action<int> LevelLoaded;
    public event Action<EnemyHealth> EnemyRegistered;
    public event Action<EnemyHealth> EnemyKilled;
    public event Action LevelWon;
    public event Action LevelLost;

    public IReadOnlyList<EnemyHealth> AliveEnemies => _aliveEnemies;

    private void Awake()
    {
        if (_levelParent == null)
            _levelParent = transform;

        Global.RegisterGameplayLevelController(this);
    }

    private void OnEnable()
    {
        SetupStartButtonTrigger();
    }

    private void Start()
    {
        SubscribePlayerHealth();
        SubscribePlayerWeapon();
        SubscribeDeathCameraController();
        SetResultScreens(false, false);
        SetStartButton(false);
        SetPlayerUI(false);

        if (Global.HasSelectedGameplayLevel)
        {
            _isSpecialLevel = Global.IsSelectedGameplayLevelSpecial;

            if (_isSpecialLevel && Global.SelectedGameplayLevelPrefab != null)
            {
                LoadLevelPrefab(Global.SelectedGameplayLevelPrefab, Global.SelectedGameplayLevelIndex);
                return;
            }

            if (_levelsConfig == null)
                _levelsConfig = Global.SelectedGameplayLevelsConfig;

            LoadLevel(Global.SelectedGameplayLevelIndex);
            return;
        }

        if (_loadLevelOnStart)
            LoadLevel(_startLevelIndex);
    }

    private void OnDisable()
    {
    }

    private void OnDestroy()
    {
        UnsubscribePlayerHealth();
        UnsubscribePlayerWeapon();
        UnsubscribeDeathCameraController();
        StopWinCheck();
        StopResultScreenRoutine();
        StopStartButtonPressRoutine();
        ClearEnemies();
        Global.UnregisterGameplayLevelController(this);
    }

    public bool LoadLevel(int levelIndex)
    {
        if (_levelsConfig == null)
            return false;

        if (_levelsConfig.TryGetLevelPrefab(levelIndex, out GameObject levelPrefab) == false)
            return false;

        return LoadLevelPrefab(levelPrefab, levelIndex);
    }

    private bool LoadLevelPrefab(GameObject levelPrefab, int levelIndex)
    {
        if (levelPrefab == null)
            return false;

        ClearCurrentLevel();
        ClearEnemies();
        _isLevelFinished = false;
        _isGameplayStarted = false;
        _isWaitingForKillCamera = false;
        _isWinScheduled = false;
        StopWinCheck();
        StopResultScreenRoutine();
        SetResultScreens(false, false);
        SetPlayerUI(false);

        _currentLevel = Instantiate(levelPrefab, _levelParent);
        _currentLevel.transform.localPosition = Vector3.zero;
        _currentLevel.transform.localRotation = Quaternion.identity;
        _currentLevelIndex = levelIndex;

        if (_isSpecialLevel == false && Global.GameplayTutorialManager != null)
            Global.GameplayTutorialManager.HandleLevelLoaded(this, _currentLevelIndex);

        ApplyPreviewState();
        LevelLoaded?.Invoke(_currentLevelIndex);
        return true;
    }

    public void StartGameplay()
    {
        if (_currentLevel == null || _isLevelFinished || _isGameplayStarted)
            return;

        _isGameplayStarted = true;
        _isWaitingForKillCamera = false;
        SubscribePlayerWeapon();
        SetPlayerUI(true);
        SetStartButton(false);
        SetPreviewCameraObject(false);
        SetCameraFov(_gameplayFov);

        if (Global.GameplayTutorialManager != null)
            Global.GameplayTutorialManager.HandleGameplayStarted();
    }

    private void StartGameplay(PointerEventData eventData)
    {
        StartGameplay();

        if (_isGameplayStarted && Global.PlayerTouchInput != null)
        {
            StopStartButtonPressRoutine();
            _startButtonPressCoroutine = StartCoroutine(BeginStartButtonPressRoutine(eventData));
        }
    }

    public void ReturnToPreview()
    {
        if (_currentLevel == null || _isLevelFinished)
            return;

        _isGameplayStarted = false;
        SetPlayerUI(false);
        SetStartButton(IsKillCameraFocusing() == false);
        SetPreviewCameraObject(Global.EnemyDeathCameraController == null || Global.EnemyDeathCameraController.IsFocusing == false);
        SetCameraFov(_previewFov);

        if (Global.GameplayTutorialManager != null)
            Global.GameplayTutorialManager.HandleReturnedToPreview();
    }

    public void SetPreviewCameraObjectVisible(bool isVisible)
    {
        SetPreviewCameraObject(isVisible);
    }

    public void RegisterEnemy(EnemyHealth enemyHealth)
    {
        SubscribePlayerHealth();

        if (enemyHealth == null || enemyHealth.IsDead || _aliveEnemies.Contains(enemyHealth))
            return;

        _aliveEnemies.Add(enemyHealth);
        enemyHealth.Died += OnEnemyDied;
        EnemyRegistered?.Invoke(enemyHealth);

        if (Global.GameplayTutorialManager != null)
            Global.GameplayTutorialManager.RegisterEnemy(enemyHealth);
    }

    public void UnregisterEnemy(EnemyHealth enemyHealth)
    {
        if (enemyHealth == null)
            return;

        if (_aliveEnemies.Remove(enemyHealth))
            enemyHealth.Died -= OnEnemyDied;
    }

    public bool CanDamageEnemy(EnemyHealth enemyHealth)
    {
        return Global.GameplayTutorialManager == null || Global.GameplayTutorialManager.CanDamageEnemy(enemyHealth);
    }

    public bool CanDamageDestructibleObject(DestructibleObject destructibleObject)
    {
        return Global.GameplayTutorialManager == null || Global.GameplayTutorialManager.CanDamageDestructibleObject(destructibleObject);
    }

    public void RegisterDestructibleObject(DestructibleObject destructibleObject)
    {
        if (destructibleObject == null || destructibleObject.IsDestroyed || _destructibleObjects.Contains(destructibleObject))
            return;

        _destructibleObjects.Add(destructibleObject);
        destructibleObject.Destroyed += OnDestructibleObjectDestroyed;
    }

    public void UnregisterDestructibleObject(DestructibleObject destructibleObject)
    {
        if (destructibleObject == null)
            return;

        if (_destructibleObjects.Remove(destructibleObject))
            destructibleObject.Destroyed -= OnDestructibleObjectDestroyed;
    }

    public bool HasAliveDamageDestructibleObjects()
    {
        for (int i = _destructibleObjects.Count - 1; i >= 0; i--)
        {
            DestructibleObject destructibleObject = _destructibleObjects[i];

            if (destructibleObject == null || destructibleObject.IsDestroyed)
            {
                _destructibleObjects.RemoveAt(i);
                continue;
            }

            if (destructibleObject.DealsDamage)
                return true;
        }

        return false;
    }

    public void LoseLevelBySpecialBarrels()
    {
        LoseLevel();
    }

    public void ClearCurrentLevel()
    {
        if (_currentLevel == null)
            return;

        Destroy(_currentLevel);
        _currentLevel = null;
        _currentLevelIndex = -1;
    }

    private void OnEnemyDied(EnemyHealth enemyHealth)
    {
        SubscribeDeathCameraController();

        if (Global.EnemyDeathCameraController != null && Global.EnemyDeathCameraController.Focus(enemyHealth.transform))
            _isWaitingForKillCamera = true;

        UnregisterEnemy(enemyHealth);
        EnemyKilled?.Invoke(enemyHealth);

        if (Global.GameplayTutorialManager != null)
            Global.GameplayTutorialManager.HandleEnemyKilled(enemyHealth);

        if (_aliveEnemies.Count <= 0)
            ScheduleWin();
    }

    private void OnPlayerDied()
    {
        LoseLevel();
    }

    private void OnDestructibleObjectDestroyed(DestructibleObject destructibleObject)
    {
        UnregisterDestructibleObject(destructibleObject);
    }

    private void WinLevel()
    {
        Debug.Log($"[WinScreenDebug] WinLevel called. isLevelFinished={_isLevelFinished}, currentLevelIndex={_currentLevelIndex}, winScreen={GetObjectPath(_winScreen)}, winScreenActive={GetActiveState(_winScreen)}, playerUI={GetObjectPath(_playerUI)}, playerUIActive={GetActiveState(_playerUI)}");

        if (_isLevelFinished)
            return;

        if (ShouldLoseByRank())
        {
            Debug.Log("[WinScreenDebug] WinLevel redirected to LoseLevel by rank check.");
            LoseLevel();
            return;
        }

        _isLevelFinished = true;
        _isGameplayStarted = false;
        _isWinScheduled = false;
        if (Global.GameplayTutorialManager != null)
            Global.GameplayTutorialManager.HandleLevelFinished();

        StopWinCheck();
        StopResultScreenRoutine();
        SetPlayerUI(false);
        SetStartButton(false);
        SetResultScreens(true, false);
        Debug.Log($"[WinScreenDebug] WinLevel after early SetResultScreens. winScreenActive={GetActiveState(_winScreen)}, winScreenHierarchyActive={GetHierarchyActiveState(_winScreen)}, loseScreenActive={GetActiveState(_loseScreen)}");

        if (_isSpecialLevel == false)
        {
            Debug.Log($"[RewardDebug] WinLevel reward flow start. level={_currentLevelIndex}, gearPacksConfig={(_gearPacksConfig != null)}, rewardsView={(_levelRewardsView != null)}");
            SaveManager.CompleteLevel(_currentLevelIndex);
            Debug.Log($"[RewardDebug] CompleteLevel done. completedLevel={SaveManager.CompletedLevelIndex}");
            LevelCompletionReward reward = GrantVictoryPackSafe();
            ShowLevelRewardsSafe(reward);
            RegisterEraTransitionSafe();
            AddVictoryExperienceSafe();
            SendLevelCompletedAnalyticsSafe();
        }
        else
        {
            LevelCompletionReward reward = GrantSpecialLevelReward();
            ShowLevelRewardsSafe(reward);
        }

        Debug.Log($"[WinScreenDebug] WinLevel after rewards. winScreenActive={GetActiveState(_winScreen)}, winScreenHierarchyActive={GetHierarchyActiveState(_winScreen)}, loseScreenActive={GetActiveState(_loseScreen)}");
        if (Global.AudioManager != null)
            Global.AudioManager.PlaySound(AudioSfxType.Victory);
        LevelWon?.Invoke();
    }

    private void LoseLevel()
    {
        if (_isLevelFinished)
            return;

        _isLevelFinished = true;
        _isGameplayStarted = false;
        _isWinScheduled = false;
        if (Global.GameplayTutorialManager != null)
            Global.GameplayTutorialManager.HandleLevelFinished();

        StopWinCheck();
        StopResultScreenRoutine();
        SetPlayerUI(false);
        SetStartButton(false);
        SetResultScreens(false, true);
        if (Global.AudioManager != null)
            Global.AudioManager.PlaySound(AudioSfxType.Defeat);
        LevelLost?.Invoke();
    }

    public void RestartLevel()
    {
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    public void LoadMenu()
    {
        if (string.IsNullOrEmpty(_menuSceneName))
            return;

        Global.ClearSelectedGameplayLevel();
        SceneManager.LoadScene(_menuSceneName);
    }

    public void ShowExitView()
    {
        if (_exitView != null)
            _exitView.Show();
    }

    public void LoadNextLevel()
    {
        if (_isSpecialLevel && Global.SelectedGameplayLevelPrefab != null)
        {
            Global.SetSelectedSpecialGameplayLevel(Global.SelectedGameplayLevelPrefab);
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.buildIndex);
            return;
        }

        if (_levelsConfig == null || _levelsConfig.Count <= 0)
            return;

        int nextLevelIndex = _isSpecialLevel ? SaveManager.SpecialLevelIndex % _levelsConfig.Count : _currentLevelIndex + 1;

        if (_isSpecialLevel)
            SaveManager.AdvanceSpecialLevelIndex(_levelsConfig.Count);
        else
            SaveManager.SetSelectedLevel(nextLevelIndex);

        Global.SetSelectedGameplayLevel(nextLevelIndex, _levelsConfig, _isSpecialLevel);
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    public void CheatWinLevel()
    {
        if (_currentLevel == null)
            return;

        WinLevel();
    }

    private void ScheduleWin()
    {
        _isWinScheduled = true;
        SetPlayerUI(false);
        SetStartButton(false);
        StopWinCheck();
        _winCoroutine = StartCoroutine(WinAfterCameraRoutine());
    }

    private System.Collections.IEnumerator WinAfterCameraRoutine()
    {
        yield return null;

        float waitStartTime = Time.realtimeSinceStartup;

        while (Global.EnemyDeathCameraController != null
            && Global.EnemyDeathCameraController.IsFocusing
            && Time.realtimeSinceStartup - waitStartTime < WinAfterKillCameraMaxWait)
        {
            yield return null;
        }

        if (Global.EnemyDeathCameraController != null && Global.EnemyDeathCameraController.IsFocusing)
            Global.EnemyDeathCameraController.CancelFocus();

        _winCoroutine = null;

        if (_isLevelFinished)
            yield break;

        if (_isWinScheduled || _aliveEnemies.Count <= 0)
            WinLevel();
    }

    private void ApplyPreviewState()
    {
        ResolvePlayerCamera();
        SetCameraFov(_previewFov);
        SetPreviewCameraObject(true);
        SetStartButton(true);
    }

    private void StopWinCheck()
    {
        if (_winCoroutine == null)
            return;

        StopCoroutine(_winCoroutine);
        _winCoroutine = null;
    }

    private void OnShotCompleted(ShotResult shotResult)
    {
        if (_currentLevel == null || _isLevelFinished)
            return;

        if (Global.GameplayTutorialManager != null)
            Global.GameplayTutorialManager.HandleShotCompleted(shotResult);

        if (_isWaitingForKillCamera || IsKillCameraFocusing())
            return;

        ReturnToPreview();
    }

    private System.Collections.IEnumerator BeginStartButtonPressRoutine(PointerEventData eventData)
    {
        while (_playerUI != null && _playerUI.activeInHierarchy == false)
            yield return null;

        yield return null;

        _startButtonPressCoroutine = null;

        if (_isGameplayStarted && Global.PlayerTouchInput != null)
            Global.PlayerTouchInput.BeginExternalPress(eventData);
    }

    private void SubscribePlayerHealth()
    {
        if (_subscribedPlayerHealth == Global.PlayerHealth)
            return;

        UnsubscribePlayerHealth();
        _subscribedPlayerHealth = Global.PlayerHealth;

        if (_subscribedPlayerHealth != null)
            _subscribedPlayerHealth.Died += OnPlayerDied;
    }

    private void UnsubscribePlayerHealth()
    {
        if (_subscribedPlayerHealth == null)
            return;

        _subscribedPlayerHealth.Died -= OnPlayerDied;
        _subscribedPlayerHealth = null;
    }

    private void StopStartButtonPressRoutine()
    {
        if (_startButtonPressCoroutine == null)
            return;

        StopCoroutine(_startButtonPressCoroutine);
        _startButtonPressCoroutine = null;
    }

    private void SubscribePlayerWeapon()
    {
        if (_subscribedPlayerWeapon == Global.PlayerWeapon)
            return;

        UnsubscribePlayerWeapon();
        _subscribedPlayerWeapon = Global.PlayerWeapon;

        if (_subscribedPlayerWeapon != null)
        {
            _subscribedPlayerWeapon.ProjectileFired += OnProjectileFired;
            _subscribedPlayerWeapon.ShotCompleted += OnShotCompleted;
        }
    }

    private void UnsubscribePlayerWeapon()
    {
        if (_subscribedPlayerWeapon == null)
            return;

        _subscribedPlayerWeapon.ProjectileFired -= OnProjectileFired;
        _subscribedPlayerWeapon.ShotCompleted -= OnShotCompleted;
        _subscribedPlayerWeapon = null;
    }

    private void SubscribeDeathCameraController()
    {
        if (_subscribedDeathCameraController == Global.EnemyDeathCameraController)
            return;

        UnsubscribeDeathCameraController();
        _subscribedDeathCameraController = Global.EnemyDeathCameraController;

        if (_subscribedDeathCameraController != null)
            _subscribedDeathCameraController.FocusCompleted += OnDeathCameraFocusCompleted;
    }

    private void UnsubscribeDeathCameraController()
    {
        if (_subscribedDeathCameraController == null)
            return;

        _subscribedDeathCameraController.FocusCompleted -= OnDeathCameraFocusCompleted;
        _subscribedDeathCameraController = null;
    }

    private void OnDeathCameraFocusCompleted()
    {
        _isWaitingForKillCamera = false;

        if (_currentLevel == null || _isLevelFinished)
            return;

        if (_isWinScheduled || _aliveEnemies.Count <= 0)
        {
            WinLevel();
            return;
        }

        if (_isGameplayStarted)
            ReturnToPreview();
        else
            SetStartButton(true);
    }

    private void OnProjectileFired(PlayerProjectile projectile)
    {
        if (_currentLevel == null || _isLevelFinished || _isGameplayStarted == false)
            return;

        if (_aliveEnemies.Count != 1 || projectile == null || projectile.UsesLockedHit == false)
            return;

        SubscribeDeathCameraController();

        if (Global.EnemyDeathCameraController != null && Global.EnemyDeathCameraController.FocusProjectile(projectile.transform))
            _isWaitingForKillCamera = true;
    }

    private bool IsKillCameraFocusing()
    {
        return Global.EnemyDeathCameraController != null && Global.EnemyDeathCameraController.IsFocusing;
    }

    private bool IsKillShot(ShotResult shotResult)
    {
        return shotResult == ShotResult.Kill || shotResult == ShotResult.OneShotKill;
    }

    private void SetResultScreens(bool isWinScreenActive, bool isLoseScreenActive)
    {
        Debug.Log($"[WinScreenDebug] SetResultScreens win={isWinScreenActive}, lose={isLoseScreenActive}, winScreen={GetObjectPath(_winScreen)}, loseScreen={GetObjectPath(_loseScreen)}");
        SetResultScreen(_winScreen, isWinScreenActive);
        SetResultScreen(_loseScreen, isLoseScreenActive);
    }

    private void SetResultScreen(GameObject screen, bool isActive)
    {
        if (screen == null)
        {
            Debug.Log($"[WinScreenDebug] SetResultScreen skipped. screen=null, targetActive={isActive}");
            return;
        }

        Debug.Log($"[WinScreenDebug] SetResultScreen before. screen={GetObjectPath(screen)}, targetActive={isActive}, activeSelf={screen.activeSelf}, activeInHierarchy={screen.activeInHierarchy}");

        if (isActive)
        {
            SetParentsActive(screen.transform);
            ShowResultScreen(screen);
            StartResultScreenRoutine(screen);
            Debug.Log($"[WinScreenDebug] SetResultScreen after show. screen={GetObjectPath(screen)}, activeSelf={screen.activeSelf}, activeInHierarchy={screen.activeInHierarchy}");
            return;
        }

        screen.SetActive(isActive);
        Debug.Log($"[WinScreenDebug] SetResultScreen after hide. screen={GetObjectPath(screen)}, activeSelf={screen.activeSelf}, activeInHierarchy={screen.activeInHierarchy}");
    }

    private void ShowResultScreen(GameObject screen)
    {
        UIPopup popup = screen.GetComponent<UIPopup>();

        if (popup != null)
        {
            Debug.Log($"[WinScreenDebug] ShowResultScreen via UIPopup.Show. screen={GetObjectPath(screen)}");
            popup.Show();
            return;
        }

        Debug.Log($"[WinScreenDebug] ShowResultScreen via SetActive. screen={GetObjectPath(screen)}");
        screen.SetActive(true);
        ForceResultScreenVisible(screen);
    }

    private void StartResultScreenRoutine(GameObject screen)
    {
        StopResultScreenRoutine();
        _resultScreenCoroutine = StartCoroutine(ResultScreenVisibleRoutine(screen));
    }

    private System.Collections.IEnumerator ResultScreenVisibleRoutine(GameObject screen)
    {
        for (int i = 0; i < 5; i++)
        {
            Debug.Log($"[WinScreenDebug] ResultScreenVisibleRoutine frame={i}, screen={GetObjectPath(screen)}, active={GetActiveState(screen)}, hierarchy={GetHierarchyActiveState(screen)}");
            ForceResultScreenVisible(screen);
            yield return null;
        }

        _resultScreenCoroutine = null;
    }

    private void StopResultScreenRoutine()
    {
        if (_resultScreenCoroutine == null)
            return;

        StopCoroutine(_resultScreenCoroutine);
        _resultScreenCoroutine = null;
    }

    private void ForceResultScreenVisible(GameObject screen)
    {
        if (screen == null)
        {
            Debug.Log("[WinScreenDebug] ForceResultScreenVisible skipped. screen=null");
            return;
        }

        SetParentsActive(screen.transform);
        screen.SetActive(true);
        BringHierarchyToFront(screen.transform);
        SetCanvasGroupsVisible(screen);
        SetCanvasesEnabled(screen);
        Debug.Log($"[WinScreenDebug] ForceResultScreenVisible done. screen={GetObjectPath(screen)}, activeSelf={screen.activeSelf}, activeInHierarchy={screen.activeInHierarchy}, parent={GetObjectPath(screen.transform.parent != null ? screen.transform.parent.gameObject : null)}");
    }

    private void BringHierarchyToFront(Transform target)
    {
        Transform current = target;

        while (current != null)
        {
            current.SetAsLastSibling();
            current = current.parent;
        }
    }

    private void SetCanvasGroupsVisible(GameObject screen)
    {
        CanvasGroup[] canvasGroups = screen.GetComponentsInParent<CanvasGroup>(true);

        for (int i = 0; i < canvasGroups.Length; i++)
            SetCanvasGroupVisible(canvasGroups[i]);

        canvasGroups = screen.GetComponentsInChildren<CanvasGroup>(true);

        for (int i = 0; i < canvasGroups.Length; i++)
            SetCanvasGroupVisible(canvasGroups[i]);
    }

    private void SetCanvasGroupVisible(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void SetCanvasesEnabled(GameObject screen)
    {
        Canvas[] canvases = screen.GetComponentsInParent<Canvas>(true);

        for (int i = 0; i < canvases.Length; i++)
            canvases[i].enabled = true;

        canvases = screen.GetComponentsInChildren<Canvas>(true);

        for (int i = 0; i < canvases.Length; i++)
            canvases[i].enabled = true;
    }

    private void SetParentsActive(Transform target)
    {
        Transform parent = target != null ? target.parent : null;

        while (parent != null)
        {
            if (parent.gameObject.activeSelf == false)
            {
                Debug.Log($"[WinScreenDebug] Activating parent {GetObjectPath(parent.gameObject)} for target={GetObjectPath(target != null ? target.gameObject : null)}");
                parent.gameObject.SetActive(true);
            }

            parent = parent.parent;
        }
    }

    private string GetActiveState(GameObject target)
    {
        return target != null ? target.activeSelf.ToString() : "null";
    }

    private string GetHierarchyActiveState(GameObject target)
    {
        return target != null ? target.activeInHierarchy.ToString() : "null";
    }

    private string GetObjectPath(GameObject target)
    {
        if (target == null)
            return "null";

        string path = target.name;
        Transform parent = target.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    private void ShowLevelRewards(LevelCompletionReward reward)
    {
        if (_levelRewardsView != null)
            _levelRewardsView.Show(reward);
    }

    private LevelCompletionReward GrantSpecialLevelReward()
    {
        CurrencyManager.Add(CurrencyType.Soft, SpecialLevelSoftReward);

        return new LevelCompletionReward
        {
            Soft = SpecialLevelSoftReward
        };
    }

    private LevelCompletionReward GrantVictoryPackSafe()
    {
        try
        {
            GearPackRarity rarity = GearPackManager.AddVictoryPack(_gearPacksConfig, out LevelCompletionReward reward);
            Debug.Log($"[RewardDebug] AddVictoryPack done. rarity={rarity}, rewardPack={reward.PackRarity}, rewardCount={reward.PackCount}, savedCommon={GearPackManager.GetPackCount(GearPackRarity.Common)}, savedUncommon={GearPackManager.GetPackCount(GearPackRarity.Uncommon)}");
            return reward;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[RewardDebug] AddVictoryPack failed. fallback common pack. exception={exception}");
            GearPackManager.AddPack(GearPackRarity.Common, 1);
            return new LevelCompletionReward
            {
                PackRarity = GearPackRarity.Common,
                PackCount = 1
            };
        }
    }

    private void ShowLevelRewardsSafe(LevelCompletionReward reward)
    {
        try
        {
            Debug.Log($"[RewardDebug] ShowLevelRewards start. view={(_levelRewardsView != null)}, pack={reward.PackRarity}, packCount={reward.PackCount}, soft={reward.Soft}, hard={reward.Hard}");
            ShowLevelRewards(reward);
            Debug.Log("[RewardDebug] ShowLevelRewards done.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[RewardDebug] ShowLevelRewards failed. exception={exception}");
        }
    }

    private void RegisterEraTransitionSafe()
    {
        try
        {
            EraTransitionManager.RegisterLevelCompleted(_currentLevelIndex);
            Debug.Log("[RewardDebug] Era transition registration done.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[RewardDebug] Era transition registration failed. exception={exception}");
        }
    }

    private void AddVictoryExperienceSafe()
    {
        try
        {
            ProfileManager.AddVictoryExperience();
            Debug.Log("[RewardDebug] Victory experience added.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[RewardDebug] Victory experience failed. exception={exception}");
        }
    }

    private void SendLevelCompletedAnalytics()
    {
        Analytics.Key("level_completed")
            .Param("level", (_currentLevelIndex + 1).ToString())
            .Send();
    }

    private void SendLevelCompletedAnalyticsSafe()
    {
        try
        {
            SendLevelCompletedAnalytics();
            Debug.Log("[RewardDebug] Level completed analytics sent.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[RewardDebug] Level completed analytics failed. exception={exception}");
        }
    }

    private bool ShouldLoseByRank()
    {
        return _currentLevelIndex >= SRankRequiredStartLevelIndex
            && Global.GameplayRankManager != null
            && Global.GameplayRankManager.HasCurrentRankAtLeastS == false;
    }

    private void SetPlayerUI(bool isActive)
    {
        if (_playerUI != null)
            _playerUI.SetActive(isActive);
    }

    private void SetPreviewCameraObject(bool isActive)
    {
        if (_previewCameraObject != null)
            _previewCameraObject.SetActive(isActive);
    }

    private void SetStartButton(bool isActive)
    {
        if (_startButton != null)
            _startButton.gameObject.SetActive(isActive && CanShowStartButton());
    }

    public void RefreshStartButton()
    {
        if (_currentLevel == null || _isLevelFinished || _isGameplayStarted || IsKillCameraFocusing())
            return;

        SetStartButton(true);
    }

    private bool CanShowStartButton()
    {
        return Global.GameplayTutorialManager == null || Global.GameplayTutorialManager.CanShowStartButton;
    }

    private void SetupStartButtonTrigger()
    {
        if (_startButton == null || _isStartButtonTriggerSetup)
            return;

        _startButton.onClick.RemoveListener(StartGameplay);
        _startButtonTrigger = _startButton.GetComponent<EventTrigger>();

        if (_startButtonTrigger == null)
            _startButtonTrigger = _startButton.gameObject.AddComponent<EventTrigger>();

        AddStartButtonTrigger(EventTriggerType.PointerDown, StartGameplay);
        _isStartButtonTriggerSetup = true;
    }

    private void AddStartButtonTrigger(EventTriggerType triggerType, Action<PointerEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = triggerType
        };

        entry.callback.AddListener(eventData => callback((PointerEventData)eventData));
        _startButtonTrigger.triggers.Add(entry);
    }

    private void ResolvePlayerCamera()
    {
        if (_playerCamera == null && Global.PlayerWeapon != null)
            _playerCamera = Global.PlayerWeapon.Camera;
    }

    private void SetCameraFov(float fov)
    {
        ResolvePlayerCamera();

        if (_playerCamera != null)
            _playerCamera.fieldOfView = Mathf.Max(1f, fov);
    }

    private void ClearEnemies()
    {
        for (int i = 0; i < _aliveEnemies.Count; i++)
        {
            if (_aliveEnemies[i] != null)
                _aliveEnemies[i].Died -= OnEnemyDied;
        }

        _aliveEnemies.Clear();

        for (int i = 0; i < _destructibleObjects.Count; i++)
        {
            if (_destructibleObjects[i] != null)
                _destructibleObjects[i].Destroyed -= OnDestructibleObjectDestroyed;
        }

        _destructibleObjects.Clear();
    }
}
