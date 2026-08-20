using DG.Tweening;
using UnityEngine;

public class UIPopup : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _content;
    [SerializeField] private float _animationDuration = 0.2f;

    private Tween _tween;

    protected virtual void Awake()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_content == null)
            _content = transform as RectTransform;

        gameObject.SetActive(false);
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);
        KillTween();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        if (_content != null)
            _content.localScale = Vector3.one * 0.9f;

        Sequence sequence = DOTween.Sequence().SetUpdate(true);

        if (_canvasGroup != null)
            sequence.Join(_canvasGroup.DOFade(1f, _animationDuration));

        if (_content != null)
            sequence.Join(_content.DOScale(1f, _animationDuration).SetEase(Ease.OutBack));

        _tween = sequence;
    }

    public virtual void Hide()
    {
        KillTween();

        if (_canvasGroup != null)
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        Sequence sequence = DOTween.Sequence().SetUpdate(true);

        if (_canvasGroup != null)
            sequence.Join(_canvasGroup.DOFade(0f, _animationDuration));

        if (_content != null)
            sequence.Join(_content.DOScale(0.9f, _animationDuration).SetEase(Ease.InBack));

        sequence.OnComplete(() => gameObject.SetActive(false));
        _tween = sequence;
    }

    protected virtual void OnDestroy()
    {
        KillTween();
    }

    private void KillTween()
    {
        if (_tween == null)
            return;

        _tween.Kill();
        _tween = null;
    }
}
