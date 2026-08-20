using DG.Tweening;
using System.Text;
using TMPro;
using UnityEngine;

public class KillFeedbackView : MonoBehaviour
{
    private const int RankInitialSizePercent = 130;

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

    [SerializeField] private TMP_Text _damageText;
    [SerializeField] private TMP_Text _comboText;
    [SerializeField] private RectTransform _damageTarget;
    [SerializeField] private RectTransform _comboTarget;
    [SerializeField] private float _showDuration = 0.25f;
    [SerializeField] private float _flyDuration = 0.35f;
    [SerializeField] private float _fadeDuration = 0.12f;

    private GameplayLevelController _levelController;
    private PlayerWeapon _playerWeapon;
    private EnemyHealth _pendingKilledEnemy;
    private Vector2 _damageStartPosition;
    private Vector2 _comboStartPosition;
    private int _killStreak;
    private Sequence _damageSequence;
    private Sequence _comboSequence;
    private static bool _isDamageFeedbackPending;
    private static bool _isComboFeedbackPending;

    public static bool IsDamageFeedbackPending => _isDamageFeedbackPending;
    public static bool IsComboFeedbackPending => _isComboFeedbackPending;
    public static event System.Action DamageFeedbackArrived;
    public static event System.Action ComboFeedbackArrived;

    private void Awake()
    {
        _damageStartPosition = GetAnchoredPosition(_damageText);
        _comboStartPosition = GetAnchoredPosition(_comboText);
        SetTextActive(_damageText, false);
        SetTextActive(_comboText, false);
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        Subscribe();
    }

    private void Update()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        KillSequences();
        CompleteDamageFeedback();
        CompleteComboFeedback();
    }

    private void Subscribe()
    {
        if (_levelController == null && Global.GameplayLevelController != null)
        {
            _levelController = Global.GameplayLevelController;
            _levelController.LevelLoaded += OnLevelLoaded;
            _levelController.EnemyKilled += OnEnemyKilled;
        }

        if (_playerWeapon == null && Global.PlayerWeapon != null)
        {
            _playerWeapon = Global.PlayerWeapon;
            _playerWeapon.ShotCompleted += OnShotCompleted;
        }
    }

    private void Unsubscribe()
    {
        if (_levelController != null)
        {
            _levelController.LevelLoaded -= OnLevelLoaded;
            _levelController.EnemyKilled -= OnEnemyKilled;
            _levelController = null;
        }

        if (_playerWeapon != null)
        {
            _playerWeapon.ShotCompleted -= OnShotCompleted;
            _playerWeapon = null;
        }
    }

    private void OnLevelLoaded(int levelIndex)
    {
        _killStreak = 0;
        _pendingKilledEnemy = null;
        KillSequences();
        CompleteDamageFeedback();
        CompleteComboFeedback();
        ResetText(_damageText, _damageStartPosition);
        ResetText(_comboText, _comboStartPosition);
    }

    private void OnEnemyKilled(EnemyHealth enemyHealth)
    {
        _pendingKilledEnemy = enemyHealth;

        if (enemyHealth != null && enemyHealth.KillBonusType == EnemyKillBonusType.AddDamage && _damageText != null)
            _isDamageFeedbackPending = true;

        if (enemyHealth != null && _comboText != null)
            _isComboFeedbackPending = true;
    }

    private void OnShotCompleted(ShotResult shotResult)
    {
        EnemyHealth killedEnemy = _pendingKilledEnemy;
        _pendingKilledEnemy = null;

        if (shotResult == ShotResult.OneShotKill)
        {
            _killStreak++;
        }
        else if (shotResult == ShotResult.Kill || killedEnemy != null)
        {
            _killStreak = 1;
        }
        else
        {
            ResetCombo();
            return;
        }

        if (killedEnemy != null && killedEnemy.KillBonusType == EnemyKillBonusType.AddDamage)
            PlayDamage(Mathf.CeilToInt(killedEnemy.KillBonusAmount));

        PlayCombo(GetRankStyleText(GetCurrentRankIndex()));
    }

    private void PlayDamage(int damage)
    {
        if (_damageText == null)
            return;

        _damageText.text = $"+{CompactNumberFormatter.Format(damage)} Damage";
        _damageSequence = PlayText(_damageText, _damageStartPosition, _damageTarget, _damageSequence, CompleteDamageFeedback);
    }

    private void PlayCombo(string combo)
    {
        if (_comboText == null || string.IsNullOrEmpty(combo))
            return;

        _comboText.text = combo;
        _comboText.color = Color.white;
        _comboSequence = PlayText(_comboText, _comboStartPosition, _comboTarget, _comboSequence, CompleteComboFeedback);
    }

    private Sequence PlayText(TMP_Text text, Vector2 startPosition, RectTransform target, Sequence currentSequence, System.Action onArrived)
    {
        if (currentSequence != null)
            currentSequence.Kill();

        RectTransform rectTransform = text.transform as RectTransform;
        CanvasGroup canvasGroup = GetCanvasGroup(text);

        if (rectTransform == null)
            return null;

        text.gameObject.SetActive(true);
        rectTransform.anchoredPosition = startPosition;
        rectTransform.localScale = Vector3.one;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Append(rectTransform.DOScale(1.15f, _fadeDuration).SetEase(Ease.OutBack));
        sequence.AppendInterval(_showDuration);
        sequence.Append(rectTransform.DOAnchorPos(GetTargetPosition(rectTransform, target), _flyDuration).SetEase(Ease.InQuad));
        sequence.AppendCallback(() => onArrived?.Invoke());

        if (canvasGroup != null)
            sequence.Append(canvasGroup.DOFade(0f, _fadeDuration));

        sequence.OnComplete(() => ResetText(text, startPosition));
        return sequence;
    }

    private Vector2 GetTargetPosition(RectTransform source, RectTransform target)
    {
        if (source == null || target == null)
            return source != null ? source.anchoredPosition : Vector2.zero;

        RectTransform parent = source.parent as RectTransform;

        if (parent == null)
            return source.anchoredPosition;

        Bounds targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(parent, target);
        return targetBounds.center;
    }

    private void ResetText(TMP_Text text, Vector2 startPosition)
    {
        if (text == null)
            return;

        RectTransform rectTransform = text.transform as RectTransform;

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = startPosition;
            rectTransform.localScale = Vector3.one;
        }

        CanvasGroup canvasGroup = GetCanvasGroup(text);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        text.gameObject.SetActive(false);
    }

    private void KillSequences()
    {
        if (_damageSequence != null)
            _damageSequence.Kill();

        if (_comboSequence != null)
            _comboSequence.Kill();

        _damageSequence = null;
        _comboSequence = null;
    }

    private CanvasGroup GetCanvasGroup(TMP_Text text)
    {
        if (text == null)
            return null;

        CanvasGroup canvasGroup = text.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = text.gameObject.AddComponent<CanvasGroup>();

        return canvasGroup;
    }

    private Vector2 GetAnchoredPosition(TMP_Text text)
    {
        RectTransform rectTransform = text != null ? text.transform as RectTransform : null;
        return rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
    }

    private void SetTextActive(TMP_Text text, bool isActive)
    {
        if (text != null)
            text.gameObject.SetActive(isActive);
    }

    private string GetRankStyleText(int rankIndex)
    {
        if (rankIndex < 0)
            return string.Empty;

        int clampedRankIndex = Mathf.Clamp(rankIndex, 0, RankStyleNames.Length - 1);
        return FormatRankStyleName(RankStyleNames[clampedRankIndex], GetRankColor(clampedRankIndex));
    }

    private Color GetRankColor(int rankIndex)
    {
        return RankColors[Mathf.Clamp(rankIndex, 0, RankColors.Length - 1)];
    }

    private int GetCurrentRankIndex()
    {
        if (Global.GameplayRankManager != null)
            return Global.GameplayRankManager.CurrentRankIndex;

        return Mathf.Clamp(_killStreak - 1, 0, RankStyleNames.Length - 1);
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

    private void ResetCombo()
    {
        _killStreak = 0;
    }

    private static void CompleteDamageFeedback()
    {
        if (_isDamageFeedbackPending == false)
            return;

        _isDamageFeedbackPending = false;
        DamageFeedbackArrived?.Invoke();
    }

    private static void CompleteComboFeedback()
    {
        if (_isComboFeedbackPending == false)
            return;

        _isComboFeedbackPending = false;
        ComboFeedbackArrived?.Invoke();
    }
}
