using DG.Tweening;
using UnityEngine;

public class ScalePulse : MonoBehaviour
{
    [SerializeField] private float _scaleMultiplier = 1.05f;
    [SerializeField] private float _duration = 0.5f;
    [SerializeField] private Ease _ease = Ease.InOutSine;

    private Vector3 _initialScale;
    private Tween _tween;

    private void Awake()
    {
        _initialScale = transform.localScale;
    }

    private void OnEnable()
    {
        Play();
    }

    private void OnDisable()
    {
        Stop();
    }

    private void OnDestroy()
    {
        Stop();
    }

    public void Configure(float scaleMultiplier)
    {
        _scaleMultiplier = scaleMultiplier;

        if (isActiveAndEnabled)
            Play();
    }

    private void Play()
    {
        Stop();
        transform.localScale = _initialScale;
        _tween = transform.DOScale(_initialScale * Mathf.Max(0f, _scaleMultiplier), Mathf.Max(0f, _duration))
            .SetEase(_ease)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void Stop()
    {
        if (_tween != null)
        {
            _tween.Kill();
            _tween = null;
        }

        transform.localScale = _initialScale;
    }
}
