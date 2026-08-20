using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class GameplayExitView : MonoBehaviour
{
    [SerializeField] private Transform _content;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _menuButton;
    [SerializeField] private float _animationDuration = 0.2f;

    private Tween _scaleTween;

    private void Awake()
    {
        if (_content == null)
            _content = transform;

        if (_closeButton != null)
            _closeButton.onClick.AddListener(Hide);

        if (_menuButton != null)
            _menuButton.onClick.AddListener(HideAndLoadMenu);
    }

    private void OnDestroy()
    {
        KillTween();

        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(Hide);

        if (_menuButton != null)
            _menuButton.onClick.RemoveListener(HideAndLoadMenu);
    }

    public void Show()
    {
        KillTween();
        gameObject.SetActive(true);
        _content.localScale = Vector3.zero;
        _scaleTween = _content.DOScale(Vector3.one, _animationDuration).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void Hide()
    {
        Hide(null);
    }

    private void HideAndLoadMenu()
    {
        Hide(() =>
        {
            if (Global.GameplayLevelController != null)
                Global.GameplayLevelController.LoadMenu();
        });
    }

    private void Hide(TweenCallback completed)
    {
        KillTween();
        _scaleTween = _content.DOScale(Vector3.zero, _animationDuration).SetEase(Ease.InBack).SetUpdate(true);
        _scaleTween.OnComplete(() =>
        {
            gameObject.SetActive(false);
            completed?.Invoke();
        });
    }

    private void KillTween()
    {
        if (_scaleTween == null)
            return;

        _scaleTween.Kill();
        _scaleTween = null;
    }
}
