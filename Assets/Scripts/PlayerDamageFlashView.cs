using DG.Tweening;
using UnityEngine;

public class PlayerDamageFlashView : MonoBehaviour
{
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _duration = 1f;

    private Tween _flashTween;
    private bool _isSubscribed;

    private void Awake()
    {
        EnsureCanvasGroup();
        SetAlpha(0f);
    }

    private void OnEnable()
    {
        Subscribe();
        SetAlpha(0f);
    }

    private void Start()
    {
        Subscribe();
    }

    private void Update()
    {
        if (_isSubscribed == false)
            Subscribe();
    }

    private void OnDisable()
    {
        if (_playerHealth != null && _isSubscribed)
            _playerHealth.Damaged -= OnDamaged;

        _isSubscribed = false;
        KillFlash();
        SetAlpha(0f);
    }

    private void OnDamaged(int damage)
    {
        if (damage <= 0)
            return;

        EnsureCanvasGroup();
        KillFlash();
        SetAlpha(0f);

        float halfDuration = Mathf.Max(0f, _duration) * 0.5f;
        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Append(_canvasGroup.DOFade(1f, halfDuration).SetEase(Ease.OutQuad));
        sequence.Append(_canvasGroup.DOFade(0f, halfDuration).SetEase(Ease.InQuad));
        _flashTween = sequence;
    }

    private void Subscribe()
    {
        if (_isSubscribed)
            return;

        if (_playerHealth == null)
            _playerHealth = Global.PlayerHealth;

        if (_playerHealth == null)
            return;

        _playerHealth.Damaged += OnDamaged;
        _isSubscribed = true;
    }

    private void EnsureCanvasGroup()
    {
        if (_canvasGroup != null)
            return;

        _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void KillFlash()
    {
        if (_flashTween == null)
            return;

        _flashTween.Kill();
        _flashTween = null;
    }

    private void SetAlpha(float alpha)
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = alpha;
    }
}
