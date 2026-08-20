using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileLevelTrackItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private Slider _levelSlider;
    [SerializeField] private GameObject _filledObject;
    [SerializeField] private Transform _rewardParent;

    private ProfileRewardView _rewardView;
    private Tween _tween;

    public RectTransform RectTransform => transform as RectTransform;

    private void OnDestroy()
    {
        KillTween();
    }

    public void Initialize(int level, ProfileLevelReward? reward, ProfileRewardView rewardViewPrefab, ProfileLevelTrackItemState state, float progress)
    {
        if (_levelText != null)
            _levelText.text = level.ToString();

        SetProgress(progress);
        SetReward(reward, rewardViewPrefab, state, progress);
    }

    public void SetState(ProfileLevelTrackItemState state)
    {
        if (_rewardView != null)
            _rewardView.SetCompleted(state == ProfileLevelTrackItemState.Completed);
    }

    public void SetProgress(float progress)
    {
        float clampedProgress = Mathf.Clamp01(progress);
        SetFilledObjectActive(clampedProgress >= 1f);

        if (_levelSlider == null)
        {
            if (_rewardView != null)
                _rewardView.SetProgress(clampedProgress);

            return;
        }

        _levelSlider.minValue = 0f;
        _levelSlider.maxValue = 1f;
        _levelSlider.value = clampedProgress;

        if (_rewardView != null)
            _rewardView.SetProgress(clampedProgress);
    }

    public Tween AnimateProgress(float targetProgress, float duration)
    {
        KillTween();

        if (_levelSlider == null && _rewardView == null)
            return null;

        float clampedTargetProgress = Mathf.Clamp01(targetProgress);
        SetFilledObjectActive(GetProgress() >= 1f);

        Sequence sequence = DOTween.Sequence().SetUpdate(true);

        if (_levelSlider != null)
            sequence.Join(_levelSlider.DOValue(clampedTargetProgress, duration).SetEase(Ease.OutQuad));

        if (_rewardView != null)
        {
            Tween rewardTween = _rewardView.AnimateProgress(targetProgress, duration);

            if (rewardTween != null)
                sequence.Join(rewardTween);
        }

        sequence.OnComplete(() => SetFilledObjectActive(clampedTargetProgress >= 1f));
        _tween = sequence;
        return _tween;
    }

    private void SetReward(ProfileLevelReward? reward, ProfileRewardView rewardViewPrefab, ProfileLevelTrackItemState state, float progress)
    {
        if (_rewardView != null)
            Destroy(_rewardView.gameObject);

        if (reward.HasValue == false || rewardViewPrefab == null)
            return;

        Transform parent = _rewardParent != null ? _rewardParent : transform;
        _rewardView = Instantiate(rewardViewPrefab, parent);
        _rewardView.Initialize(reward.Value);
        _rewardView.SetCompleted(state == ProfileLevelTrackItemState.Completed);
        _rewardView.SetProgress(progress);
    }

    private void KillTween()
    {
        if (_tween == null)
            return;

        _tween.Kill();
        _tween = null;
    }

    private float GetProgress()
    {
        if (_levelSlider != null)
            return Mathf.Clamp01(_levelSlider.value);

        return 0f;
    }

    private void SetFilledObjectActive(bool isActive)
    {
        if (_filledObject != null)
            _filledObject.SetActive(isActive);
    }
}

public enum ProfileLevelTrackItemState
{
    Locked,
    Current,
    Completed,
    JustUnlocked
}
