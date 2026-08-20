using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SpecialLevelStartButtonView : MonoBehaviour
{
    [SerializeField] private GameObject _levelPrefab;
    [SerializeField] private Button _button;
    [SerializeField] private string _gameplaySceneName;
    [SerializeField] private bool _spendSpecialKey = true;
    [SerializeField] private CurrencyType _specialKeyType = CurrencyType.SpecialKey;
    [SerializeField] private GameObject _lockState;
    [SerializeField] private int _unlockCompletedLevelIndex = 9;
    [SerializeField] private GameObject _notEnoughKeysView;
    [SerializeField] private float _feedbackAnimationDuration = 0.2f;
    [SerializeField] private float _feedbackHoldDuration = 1.2f;

    private float _nextKeyRefreshTime;
    private Sequence _feedbackSequence;
    private CanvasGroup _notEnoughKeysCanvasGroup;

    private void Awake()
    {
        if (_button == null)
            _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        CurrencyManager.CurrencyChanged += OnCurrencyChanged;

        if (_button != null)
            _button.onClick.AddListener(StartSpecialLevel);

        UpdateView();
    }

    private void OnDisable()
    {
        CurrencyManager.CurrencyChanged -= OnCurrencyChanged;

        if (_button != null)
            _button.onClick.RemoveListener(StartSpecialLevel);

        StopFeedbackAnimation();
    }

    private void Update()
    {
        if (_spendSpecialKey == false || Time.unscaledTime < _nextKeyRefreshTime)
            return;

        _nextKeyRefreshTime = Time.unscaledTime + 1f;
        UpdateView();
    }

    public void StartSpecialLevel()
    {
        if (_levelPrefab == null || string.IsNullOrEmpty(_gameplaySceneName) || IsLocked())
            return;

        if (_spendSpecialKey && SpecialKeyManager.TrySpend(GetSpecialKeyType()) == false)
        {
            UpdateView();
            ShowNotEnoughKeys();
            return;
        }

        Global.SetSelectedSpecialGameplayLevel(_levelPrefab);
        SceneManager.LoadScene(_gameplaySceneName);
    }

    private void UpdateView()
    {
        if (_button == null)
            return;

        bool hasLevel = _levelPrefab != null && string.IsNullOrEmpty(_gameplaySceneName) == false;
        bool isLocked = IsLocked();

        _button.gameObject.SetActive(hasLevel);
        _button.interactable = hasLevel && isLocked == false;

        if (_lockState != null)
            _lockState.SetActive(hasLevel && isLocked);
    }

    private void OnCurrencyChanged(CurrencyType currencyType, int count)
    {
        if (currencyType == GetSpecialKeyType())
            UpdateView();
    }

    private bool IsLocked()
    {
        return SaveManager.CompletedLevelIndex < _unlockCompletedLevelIndex;
    }

    private CurrencyType GetSpecialKeyType()
    {
        return SpecialKeyManager.IsSpecialKey(_specialKeyType) ? _specialKeyType : CurrencyType.SpecialKey;
    }

    private void ShowNotEnoughKeys()
    {
        if (_notEnoughKeysView == null)
            return;

        StopFeedbackAnimation();
        EnsureFeedbackCanvasGroup();

        _notEnoughKeysView.SetActive(true);
        _notEnoughKeysView.transform.localScale = Vector3.zero;

        if (_notEnoughKeysCanvasGroup != null)
            _notEnoughKeysCanvasGroup.alpha = 0f;

        _feedbackSequence = DOTween.Sequence().SetUpdate(true);
        _feedbackSequence.Append(_notEnoughKeysView.transform.DOScale(Vector3.one, _feedbackAnimationDuration).SetEase(Ease.OutBack));

        if (_notEnoughKeysCanvasGroup != null)
            _feedbackSequence.Join(_notEnoughKeysCanvasGroup.DOFade(1f, _feedbackAnimationDuration));

        _feedbackSequence.AppendInterval(Mathf.Max(0f, _feedbackHoldDuration));
        _feedbackSequence.Append(_notEnoughKeysView.transform.DOScale(Vector3.zero, _feedbackAnimationDuration).SetEase(Ease.InBack));

        if (_notEnoughKeysCanvasGroup != null)
            _feedbackSequence.Join(_notEnoughKeysCanvasGroup.DOFade(0f, _feedbackAnimationDuration));

        _feedbackSequence.OnComplete(() =>
        {
            _notEnoughKeysView.SetActive(false);
            _feedbackSequence = null;
        });
    }

    private void EnsureFeedbackCanvasGroup()
    {
        if (_notEnoughKeysView == null || _notEnoughKeysCanvasGroup != null)
            return;

        _notEnoughKeysCanvasGroup = _notEnoughKeysView.GetComponent<CanvasGroup>();

        if (_notEnoughKeysCanvasGroup == null)
            _notEnoughKeysCanvasGroup = _notEnoughKeysView.AddComponent<CanvasGroup>();
    }

    private void StopFeedbackAnimation()
    {
        if (_feedbackSequence == null)
            return;

        _feedbackSequence.Kill();
        _feedbackSequence = null;
    }
}
