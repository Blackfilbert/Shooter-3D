using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ProfileRewardView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private GameObject _checkmark;
    [SerializeField] private Slider _slider;

    private Tween _tween;

    private void OnDestroy()
    {
        KillTween();
    }

    public void Initialize(ProfileLevelReward reward)
    {
        if (_icon != null)
            _icon.sprite = reward.Icon;
    }

    public void SetCompleted(bool completed)
    {
        if (_checkmark != null)
            _checkmark.SetActive(completed);
    }

    public void SetProgress(float progress)
    {
        if (_slider == null)
            return;

        _slider.minValue = 0f;
        _slider.maxValue = 1f;
        _slider.value = Mathf.Clamp01(progress);
    }

    public Tween AnimateProgress(float targetProgress, float duration)
    {
        KillTween();

        if (_slider == null)
            return null;

        _tween = _slider.DOValue(Mathf.Clamp01(targetProgress), duration).SetEase(Ease.OutQuad).SetUpdate(true);
        return _tween;
    }

    private void KillTween()
    {
        if (_tween == null)
            return;

        _tween.Kill();
        _tween = null;
    }
}
