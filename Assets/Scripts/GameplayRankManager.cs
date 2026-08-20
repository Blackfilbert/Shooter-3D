using DG.Tweening;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayRankManager : MonoBehaviour
{
    private const int RankInitialSizePercent = 130;
    private const int SRankIndex = 4;

    private static readonly string[] RankNames =
    {
        "D",
        "C",
        "B",
        "A",
        "S",
        "SS",
        "SSS"
    };

    private static readonly string[] RankStyleNames =
    {
        "Disgraced",
        "Chaotic",
        "Brutal",
        "Aggressive",
        "Savage",
        "Savage Slaughter",
        "Sniper Shadow Symphony"
    };

    private static readonly int[] RankStylePointRequirements =
    {
        50,
        100,
        150,
        250,
        400,
        500,
        600
    };

    private static readonly Color[] RankColors =
    {
        new Color32(54, 149, 232, 255),
        new Color32(91, 179, 132, 255),
        new Color32(204, 145, 34, 255),
        new Color32(226, 132, 34, 255),
        new Color32(226, 72, 68, 255),
        new Color32(216, 72, 139, 255),
        new Color32(174, 102, 204, 255)
    };

    [SerializeField] private TMP_Text _currentRankText;
    [SerializeField] private TMP_Text _additionalCurrentRankText;
    [SerializeField] private CanvasGroup _currentRankCanvasGroup;
    [SerializeField] private TMP_Text _finalRankText;
    [SerializeField] private Slider _stylePointLossDelaySlider;
    [SerializeField] private GameObject _headshotTextObject;
    [SerializeField] private GameObject _desperateTextObject;
    [SerializeField] private GameObject _quickTextObject;
    [SerializeField] private GameObject _enemyAttackModeObject;
    [SerializeField] private float _animationDuration = 0.2f;
    [SerializeField] private float _failedShotDamagePercent = 1f;
    [SerializeField] private int _stylePointsPerKill = 100;
    [SerializeField] private int _stylePointMissPenalty = 100;
    [SerializeField] private float _stylePointLossDelay = 10f;
    [SerializeField] private float _stylePointLossPerSecond = 10f;
    [SerializeField] private float _quickKillDuration = 1f;
    [SerializeField] private float _trickTextDuration = 5f;
    [SerializeField] private float _enemyAttackMinInterval = 4f;
    [SerializeField] private float _enemyAttackMaxInterval = 6f;

    private GameplayLevelController _levelController;
    private PlayerWeapon _playerWeapon;
    private float _stylePoints;
    private float _stylePointLossDelayRemaining;
    private int _currentRankIndex = -1;
    private int _bestRankIndex = -1;
    private Image _stylePointLossDelaySliderFill;
    private Color _stylePointLossDelaySliderDefaultColor = Color.white;
    private Tween _currentRankTween;
    private string _pendingRankName;
    private bool _hasPendingRank;
    private bool _isLevelFinished;
    private bool _wasGameplayStarted;
    private float _gameplayStartedTime;
    private bool _hasPendingHeadshot;
    private bool _hasPendingDesperate;
    private bool _hasPendingQuick;
    private Coroutine _headshotTextCoroutine;
    private Coroutine _desperateTextCoroutine;
    private Coroutine _quickTextCoroutine;
    private readonly List<EnemyAttackTimer> _enemyAttackTimers = new List<EnemyAttackTimer>();
    private bool _isEnemyAttackModeActive;
    private bool _isShotInProgress;
    private int _enemiesKilledDuringShot;

    public string BestRankName => GetRankName(_bestRankIndex);
    public int CurrentRankIndex => _currentRankIndex;
    public bool HasCurrentRankAtLeastS => _currentRankIndex >= SRankIndex;

    private void Awake()
    {
        Global.RegisterGameplayRankManager(this);
    }

    private void OnEnable()
    {
        KillFeedbackView.ComboFeedbackArrived += OnComboFeedbackArrived;
        EnsureCanvasGroup();
        SubscribeLevelController();
        ResetRank();
    }

    private void OnDisable()
    {
        KillFeedbackView.ComboFeedbackArrived -= OnComboFeedbackArrived;
        UnsubscribeLevelController();
        UnsubscribeWeapon();
        KillTween();
        HideTrickTexts();
        _isEnemyAttackModeActive = false;
        _enemyAttackTimers.Clear();
        SetObjectActive(_enemyAttackModeObject, false);
    }

    private void OnDestroy()
    {
        Global.UnregisterGameplayRankManager(this);
    }

    private void Start()
    {
        SubscribeLevelController();
        SubscribeWeapon();
    }

    private void Update()
    {
        UpdateGameplayStartedTime();
        UpdateEnemyAttacks();

        if (_isLevelFinished || _stylePoints <= 0f)
            return;

        if (_stylePointLossDelayRemaining > 0f)
        {
            _stylePointLossDelayRemaining = Mathf.Max(0f, _stylePointLossDelayRemaining - Time.deltaTime);
            UpdateStylePointLossDelaySlider();
            return;
        }

        _stylePoints = Mathf.Max(0f, _stylePoints - Mathf.Max(0f, _stylePointLossPerSecond) * Time.deltaTime);
        UpdateCurrentRank(false, false);
    }

    private void SubscribeLevelController()
    {
        if (_levelController != null || Global.GameplayLevelController == null)
            return;

        _levelController = Global.GameplayLevelController;
        _levelController.LevelLoaded += OnLevelLoaded;
        _levelController.EnemyKilled += OnEnemyKilled;
        _levelController.LevelWon += ShowFinalRank;
        _levelController.LevelLost += ShowFinalRank;
    }

    private void UnsubscribeLevelController()
    {
        if (_levelController == null)
            return;

        _levelController.LevelLoaded -= OnLevelLoaded;
        _levelController.EnemyKilled -= OnEnemyKilled;
        _levelController.LevelWon -= ShowFinalRank;
        _levelController.LevelLost -= ShowFinalRank;
        _levelController = null;
    }

    private void SubscribeWeapon()
    {
        if (_playerWeapon != null || Global.PlayerWeapon == null)
            return;

        _playerWeapon = Global.PlayerWeapon;
        _playerWeapon.ProjectileFired += OnProjectileFired;
        _playerWeapon.ShotCompleted += OnShotCompleted;
    }

    private void UnsubscribeWeapon()
    {
        if (_playerWeapon == null)
            return;

        _playerWeapon.ProjectileFired -= OnProjectileFired;
        _playerWeapon.ShotCompleted -= OnShotCompleted;
        _playerWeapon = null;
    }

    private void OnLevelLoaded(int levelIndex)
    {
        ResetRank();
    }

    private void OnEnemyKilled(EnemyHealth enemyHealth)
    {
        if (_isShotInProgress)
            _enemiesKilledDuringShot++;

        QueueTricks(enemyHealth);
        AddStylePoints();
    }

    private void OnProjectileFired(PlayerProjectile projectile)
    {
        _isShotInProgress = true;
        _enemiesKilledDuringShot = 0;
    }

    private void OnShotCompleted(ShotResult shotResult)
    {
        bool killedByIndirectShot = _enemiesKilledDuringShot > 0
            && shotResult != ShotResult.Kill
            && shotResult != ShotResult.OneShotKill;

        _isShotInProgress = false;
        _enemiesKilledDuringShot = 0;

        if (shotResult == ShotResult.TutorialBlocked)
            return;

        if (shotResult == ShotResult.Miss && IsTutorialFlowActive())
            return;

        if (shotResult == ShotResult.OneShotKill)
            return;

        if (killedByIndirectShot)
            return;

        if (shotResult == ShotResult.Miss)
            ApplyMissPenalty();

        ActivateEnemyAttackMode();
    }

    private void ApplyMissPenalty()
    {
        _stylePoints = Mathf.Max(0f, _stylePoints - Mathf.Max(0, _stylePointMissPenalty));
        _stylePointLossDelayRemaining = Mathf.Max(0f, _stylePointLossDelay);
        UpdateStylePointLossDelaySlider();
        UpdateCurrentRank(false, false);
    }

    private bool IsTutorialFlowActive()
    {
        return Global.GameplayTutorialManager != null && Global.GameplayTutorialManager.IsTutorialFlowActive;
    }

    private void AddStylePoints()
    {
        _stylePoints += Mathf.Max(0, _stylePointsPerKill);
        _stylePointLossDelayRemaining = Mathf.Max(0f, _stylePointLossDelay);
        UpdateStylePointLossDelaySlider();
        UpdateCurrentRank(true, true);
    }

    private void ResetRank()
    {
        _stylePoints = 0f;
        _stylePointLossDelayRemaining = 0f;
        _currentRankIndex = -1;
        _bestRankIndex = -1;
        _isLevelFinished = false;
        _pendingRankName = string.Empty;
        _hasPendingRank = false;
        _wasGameplayStarted = false;
        _gameplayStartedTime = 0f;
        _hasPendingHeadshot = false;
        _hasPendingDesperate = false;
        _hasPendingQuick = false;
        _isEnemyAttackModeActive = false;
        _isShotInProgress = false;
        _enemiesKilledDuringShot = 0;
        _enemyAttackTimers.Clear();
        HideCurrentRank();
        HideTrickTexts();
        SetObjectActive(_enemyAttackModeObject, false);
        ResetRankColors();
        UpdateStylePointLossDelaySlider();

        if (_finalRankText != null)
            _finalRankText.text = string.Empty;
    }

    private void UpdateCurrentRank(bool updateBestRank, bool waitForFeedback)
    {
        int rankIndex = GetRankIndex(_stylePoints);

        if (updateBestRank)
            _bestRankIndex = Mathf.Max(_bestRankIndex, rankIndex);

        if (_currentRankIndex == rankIndex)
            return;

        _currentRankIndex = rankIndex;
        UpdateRankColors(_currentRankIndex);

        if (_currentRankIndex < 0)
        {
            _pendingRankName = string.Empty;
            _hasPendingRank = false;
            HideCurrentRank();
            return;
        }

        ShowCurrentRankDelayed(GetRankName(_currentRankIndex), waitForFeedback);
    }

    private void ShowCurrentRankDelayed(string rankName, bool waitForFeedback)
    {
        if (waitForFeedback || KillFeedbackView.IsComboFeedbackPending)
        {
            _pendingRankName = rankName;
            _hasPendingRank = true;
            return;
        }

        ShowCurrentRank(rankName);
    }

    private void OnComboFeedbackArrived()
    {
        ShowPendingTricks();

        if (_hasPendingRank)
        {
            _hasPendingRank = false;
            ShowCurrentRank(_pendingRankName);
        }
    }

    private void ShowCurrentRank(string rankName)
    {
        if (_currentRankText == null)
            return;

        KillTween();
        EnsureCanvasGroup();

        bool wasVisible = _currentRankText.gameObject.activeSelf && (_currentRankCanvasGroup == null || _currentRankCanvasGroup.alpha > 0f);

        _currentRankText.text = rankName;
        _currentRankText.color = GetRankColor(_currentRankIndex);
        _currentRankText.gameObject.SetActive(true);
        SetAdditionalCurrentRankText(rankName, true);

        if (_currentRankCanvasGroup != null)
            _currentRankCanvasGroup.alpha = wasVisible ? 1f : 0f;

        _currentRankText.transform.localScale = Vector3.one * 0.85f;

        Sequence sequence = DOTween.Sequence().SetUpdate(true);

        if (_currentRankCanvasGroup != null && wasVisible == false)
        {
            sequence.Append(_currentRankCanvasGroup.DOFade(1f, _animationDuration));
            sequence.Join(_currentRankText.transform.DOScale(1f, _animationDuration).SetEase(Ease.OutBack));
        }
        else
        {
            sequence.Append(_currentRankText.transform.DOScale(1f, _animationDuration).SetEase(Ease.OutBack));
        }

        _currentRankTween = sequence;
    }

    private void ShowFinalRank()
    {
        _isLevelFinished = true;
        _stylePointLossDelayRemaining = 0f;
        UpdateStylePointLossDelaySlider();
        HideCurrentRank();
        HideTrickTexts();
        SetObjectActive(_enemyAttackModeObject, false);

        if (_finalRankText != null)
        {
            _finalRankText.text = GetRankStyleText(_currentRankIndex);
            _finalRankText.color = Color.white;
        }
    }

    private int GetRankIndex(float stylePoints)
    {
        for (int i = RankStylePointRequirements.Length - 1; i >= 0; i--)
        {
            if (stylePoints >= RankStylePointRequirements[i])
                return i;
        }

        return -1;
    }

    private string GetRankName(int rankIndex)
    {
        if (rankIndex < 0)
            return string.Empty;

        return RankNames[Mathf.Clamp(rankIndex, 0, RankNames.Length - 1)];
    }

    private string GetRankStyleText(int rankIndex)
    {
        if (rankIndex < 0)
            return string.Empty;

        int clampedRankIndex = Mathf.Clamp(rankIndex, 0, RankStyleNames.Length - 1);
        return FormatRankStyleName(RankStyleNames[clampedRankIndex], GetRankColor(clampedRankIndex));
    }

    private void KillTween()
    {
        if (_currentRankTween == null)
            return;

        _currentRankTween.Kill();
        _currentRankTween = null;
    }

    private void HideCurrentRank()
    {
        KillTween();

        if (_currentRankText != null)
            _currentRankText.gameObject.SetActive(false);

        SetAdditionalCurrentRankText(string.Empty, false);

        if (_currentRankCanvasGroup != null)
            _currentRankCanvasGroup.alpha = 0f;
    }

    private void SetAdditionalCurrentRankText(string rankName, bool isActive)
    {
        if (_additionalCurrentRankText == null)
            return;

        _additionalCurrentRankText.text = rankName;
        _additionalCurrentRankText.color = GetRankColor(_currentRankIndex);
        _additionalCurrentRankText.gameObject.SetActive(isActive);
    }

    private void EnsureCanvasGroup()
    {
        if (_currentRankCanvasGroup != null || _currentRankText == null)
            return;

        _currentRankCanvasGroup = _currentRankText.GetComponent<CanvasGroup>();

        if (_currentRankCanvasGroup == null)
            _currentRankCanvasGroup = _currentRankText.gameObject.AddComponent<CanvasGroup>();
    }

    private void UpdateStylePointLossDelaySlider()
    {
        if (_stylePointLossDelaySlider == null)
            return;

        _stylePointLossDelaySlider.minValue = 0f;
        _stylePointLossDelaySlider.maxValue = 1f;

        float delay = Mathf.Max(0f, _stylePointLossDelay);
        _stylePointLossDelaySlider.value = delay > 0f ? Mathf.Clamp01(_stylePointLossDelayRemaining / delay) : 0f;
    }

    private void UpdateRankColors(int rankIndex)
    {
        Image sliderFill = GetStylePointLossDelaySliderFill();

        if (sliderFill != null)
            sliderFill.color = GetRankColor(rankIndex);
    }

    private void ResetRankColors()
    {
        Image sliderFill = GetStylePointLossDelaySliderFill();

        if (sliderFill != null)
            sliderFill.color = _stylePointLossDelaySliderDefaultColor;
    }

    private Image GetStylePointLossDelaySliderFill()
    {
        if (_stylePointLossDelaySliderFill != null || _stylePointLossDelaySlider == null || _stylePointLossDelaySlider.fillRect == null)
            return _stylePointLossDelaySliderFill;

        _stylePointLossDelaySliderFill = _stylePointLossDelaySlider.fillRect.GetComponent<Image>();

        if (_stylePointLossDelaySliderFill != null)
            _stylePointLossDelaySliderDefaultColor = _stylePointLossDelaySliderFill.color;

        return _stylePointLossDelaySliderFill;
    }

    private Color GetRankColor(int rankIndex)
    {
        if (rankIndex < 0)
            return _stylePointLossDelaySliderDefaultColor;

        return RankColors[Mathf.Clamp(rankIndex, 0, RankColors.Length - 1)];
    }

    private string FormatRankStyleName(string styleName, Color color)
    {
        if (string.IsNullOrEmpty(styleName))
            return string.Empty;

        string colorHex = ColorUtility.ToHtmlStringRGB(color);
        StringBuilder builder = new StringBuilder(styleName.Length + 64);
        bool isWordStart = true;

        for (int i = 0; i < styleName.Length; i++)
        {
            char character = styleName[i];

            if (isWordStart && char.IsWhiteSpace(character) == false)
            {
                builder.Append("<size=");
                builder.Append(RankInitialSizePercent);
                builder.Append("%><color=#");
                builder.Append(colorHex);
                builder.Append('>');
                builder.Append(character);
                builder.Append("</color></size>");
                isWordStart = false;
                continue;
            }

            builder.Append(character);

            if (char.IsWhiteSpace(character))
                isWordStart = true;
        }

        return builder.ToString();
    }

    private void UpdateGameplayStartedTime()
    {
        bool isGameplayStarted = _levelController != null && _levelController.IsGameplayStarted;

        if (isGameplayStarted && _wasGameplayStarted == false)
            _gameplayStartedTime = Time.time;

        _wasGameplayStarted = isGameplayStarted;
    }

    private void QueueTricks(EnemyHealth enemyHealth)
    {
        if (enemyHealth == null)
            return;

        UpdateGameplayStartedTime();
        _hasPendingHeadshot |= enemyHealth.WasKilledByHeadshot;
        _hasPendingDesperate |= enemyHealth.WasDamagedBeforeKill;
        _hasPendingQuick |= _wasGameplayStarted && Time.time - _gameplayStartedTime <= Mathf.Max(0f, _quickKillDuration);
    }

    private void ShowPendingTricks()
    {
        if (_hasPendingHeadshot)
            _headshotTextCoroutine = ShowTrickText(_headshotTextObject, _headshotTextCoroutine);

        if (_hasPendingDesperate)
            _desperateTextCoroutine = ShowTrickText(_desperateTextObject, _desperateTextCoroutine);

        if (_hasPendingQuick)
            _quickTextCoroutine = ShowTrickText(_quickTextObject, _quickTextCoroutine);

        _hasPendingHeadshot = false;
        _hasPendingDesperate = false;
        _hasPendingQuick = false;
    }

    private Coroutine ShowTrickText(GameObject target, Coroutine coroutine)
    {
        if (target == null)
            return null;

        if (coroutine != null)
            StopCoroutine(coroutine);

        target.SetActive(true);
        return StartCoroutine(HideTrickTextAfterDelay(target));
    }

    private System.Collections.IEnumerator HideTrickTextAfterDelay(GameObject target)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, _trickTextDuration));

        if (target != null)
            target.SetActive(false);
    }

    private void HideTrickTexts()
    {
        StopTrickTextCoroutine(ref _headshotTextCoroutine);
        StopTrickTextCoroutine(ref _desperateTextCoroutine);
        StopTrickTextCoroutine(ref _quickTextCoroutine);
        SetObjectActive(_headshotTextObject, false);
        SetObjectActive(_desperateTextObject, false);
        SetObjectActive(_quickTextObject, false);
    }

    private void StopTrickTextCoroutine(ref Coroutine coroutine)
    {
        if (coroutine == null)
            return;

        StopCoroutine(coroutine);
        coroutine = null;
    }

    private void SetObjectActive(GameObject target, bool isActive)
    {
        if (target != null)
            target.SetActive(isActive);
    }

    private void ActivateEnemyAttackMode()
    {
        if (_isEnemyAttackModeActive || _isLevelFinished)
            return;

        _isEnemyAttackModeActive = true;
        SetObjectActive(_enemyAttackModeObject, true);
        SyncEnemyAttackTimers();
    }

    private void UpdateEnemyAttacks()
    {
        if (_isEnemyAttackModeActive == false || _isLevelFinished || Global.PlayerHealth == null || Global.PlayerHealth.IsDead)
            return;

        SyncEnemyAttackTimers();

        for (int i = 0; i < _enemyAttackTimers.Count; i++)
        {
            EnemyAttackTimer attackTimer = _enemyAttackTimers[i];
            attackTimer.RemainingTime -= Time.deltaTime;

            if (attackTimer.RemainingTime > 0f)
                continue;

            AttackPlayer(attackTimer.Enemy);
            attackTimer.RemainingTime = GetEnemyAttackInterval();
        }
    }

    private void SyncEnemyAttackTimers()
    {
        if (_levelController == null)
            return;

        for (int i = _enemyAttackTimers.Count - 1; i >= 0; i--)
        {
            EnemyHealth enemy = _enemyAttackTimers[i].Enemy;

            if (enemy == null || enemy.IsDead || IsAliveEnemy(enemy) == false)
                _enemyAttackTimers.RemoveAt(i);
        }

        IReadOnlyList<EnemyHealth> aliveEnemies = _levelController.AliveEnemies;

        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            EnemyHealth enemy = aliveEnemies[i];

            if (enemy == null || enemy.IsDead || HasEnemyAttackTimer(enemy))
                continue;

            _enemyAttackTimers.Add(new EnemyAttackTimer(enemy, GetEnemyAttackInterval()));
        }
    }

    private bool HasEnemyAttackTimer(EnemyHealth enemy)
    {
        for (int i = 0; i < _enemyAttackTimers.Count; i++)
        {
            if (_enemyAttackTimers[i].Enemy == enemy)
                return true;
        }

        return false;
    }

    private bool IsAliveEnemy(EnemyHealth enemy)
    {
        if (_levelController == null || enemy == null)
            return false;

        IReadOnlyList<EnemyHealth> aliveEnemies = _levelController.AliveEnemies;

        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            if (aliveEnemies[i] == enemy)
                return true;
        }

        return false;
    }

    private void AttackPlayer(EnemyHealth enemy)
    {
        if (enemy == null || enemy.IsDead || Global.PlayerHealth == null || Global.PlayerHealth.IsDead)
            return;

        enemy.PlayAttackAnimation();

        if (Global.AudioManager != null)
            Global.AudioManager.PlayEnemyShoot();

        int damage = Mathf.Max(1, Mathf.CeilToInt(Global.PlayerHealth.Health * (_failedShotDamagePercent / 100f)));
        Global.PlayerHealth.TakeDamage(damage);
    }

    private float GetEnemyAttackInterval()
    {
        float minInterval = Mathf.Max(0f, _enemyAttackMinInterval);
        float maxInterval = Mathf.Max(minInterval, _enemyAttackMaxInterval);
        return Random.Range(minInterval, maxInterval);
    }

    private sealed class EnemyAttackTimer
    {
        public readonly EnemyHealth Enemy;
        public float RemainingTime;

        public EnemyAttackTimer(EnemyHealth enemy, float remainingTime)
        {
            Enemy = enemy;
            RemainingTime = remainingTime;
        }
    }
}
