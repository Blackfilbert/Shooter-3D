using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

public class ProfileLevelPopup : UIPopup
{
    private const float PopupOpenAnimationDelay = 0.25f;

    [SerializeField] private ScrollRect _trackScrollRect;
    [SerializeField] private ProfileLevelTrackItemView _trackItemPrefab;
    [SerializeField] private ProfileRewardView _rewardViewPrefab;
    [SerializeField] private Button _closeButton;
    [SerializeField] private float _sliderAnimationDuration = 0.45f;

    private ProfileProgressConfig _config;
    private ProfileLevelTrackItemView[] _trackItems = new ProfileLevelTrackItemView[0];
    private Tween _sliderTween;
    private Coroutine _scrollCoroutine;
    private Coroutine _levelUpAnimationCoroutine;

    public event Action LevelUpAnimationCompleted;

    protected override void Awake()
    {
        base.Awake();

        if (_closeButton != null)
            _closeButton.onClick.AddListener(Hide);
    }

    protected override void OnDestroy()
    {
        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(Hide);

        KillSliderTween();
        StopScrollCoroutine();
        StopLevelUpAnimationCoroutine();
        base.OnDestroy();
    }

    public void Initialize(ProfileProgressConfig config, bool animate)
    {
        _config = config;
        RefreshTrackItems(animate);
        ScheduleScrollToCurrentLevel();

        if (animate)
            ScheduleAnimateProgress();
        else
            SetProgressImmediate();
    }

    public override void Hide()
    {
        KillSliderTween();
        StopScrollCoroutine();
        StopLevelUpAnimationCoroutine();
        base.Hide();
    }

    private void SetProgressImmediate()
    {
        ScheduleScrollToCurrentLevel();
    }

    private void AnimateProgress()
    {
        KillSliderTween();

        int targetLevel = GetClampedLevel();
        Sequence sequence = DOTween.Sequence().SetUpdate(true);

        ProfileLevelTrackItemView targetLevelItem = GetTrackItem(targetLevel);

        if (targetLevelItem != null)
        {
            targetLevelItem.SetProgress(0f);
            Tween progressTween = targetLevelItem.AnimateProgress(1f, _sliderAnimationDuration);

            if (progressTween != null)
                sequence.Join(progressTween);
            else
                sequence.AppendInterval(_sliderAnimationDuration);

            sequence.AppendCallback(() => targetLevelItem.SetState(ProfileLevelTrackItemState.Completed));
        }
        else
        {
            sequence.AppendInterval(_sliderAnimationDuration);
        }

        sequence.AppendCallback(ScheduleScrollToCurrentLevel);
        sequence.OnComplete(() => LevelUpAnimationCompleted?.Invoke());
        _sliderTween = sequence;
    }

    private int GetClampedLevel()
    {
        int maxLevel = _config != null ? _config.MaxLevel : ProfileManager.Level;
        return Mathf.Clamp(ProfileManager.Level, 1, maxLevel);
    }

    private void RefreshTrackItems(bool animate)
    {
        _trackItems = new ProfileLevelTrackItemView[0];

        Transform parent = _trackScrollRect != null && _trackScrollRect.content != null ? _trackScrollRect.content : null;

        if (parent == null || _trackItemPrefab == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);

        if (_config == null)
            return;

        int maxLevel = _config.MaxLevel;
        System.Array.Resize(ref _trackItems, maxLevel);

        for (int level = maxLevel; level >= 1; level--)
        {
            ProfileLevelTrackItemView itemView = Instantiate(_trackItemPrefab, parent);
            ProfileLevelReward? reward = _config.TryGetReward(level, out ProfileLevelReward rewardValue) ? rewardValue : null;
            ProfileLevelTrackItemState state = GetItemState(level, animate);

            itemView.Initialize(level, reward, _rewardViewPrefab, state, GetItemProgress(level, animate));
            _trackItems[level - 1] = itemView;
        }

        Canvas.ForceUpdateCanvases();
    }

    private ProfileLevelTrackItemState GetItemState(int level, bool animate)
    {
        int currentLevel = GetClampedLevel();

        if (animate && level == currentLevel)
            return ProfileLevelTrackItemState.JustUnlocked;

        if (level <= currentLevel)
            return ProfileLevelTrackItemState.Completed;

        return ProfileLevelTrackItemState.Locked;
    }

    private float GetItemProgress(int level, bool animate)
    {
        int currentLevel = GetClampedLevel();

        if (animate && level == currentLevel)
            return 0f;

        if (level <= currentLevel)
            return 1f;

        return 0f;
    }

    private ProfileLevelTrackItemView GetTrackItem(int level)
    {
        if (level < 1 || level > _trackItems.Length)
            return null;

        return _trackItems[level - 1];
    }

    private void ScrollToCurrentLevel()
    {
        if (_trackScrollRect == null || _trackScrollRect.content == null)
            return;

        ProfileLevelTrackItemView currentItem = GetTrackItem(GetClampedLevel());
        RectTransform itemRect = currentItem != null ? currentItem.RectTransform : null;
        RectTransform content = _trackScrollRect.content;
        RectTransform viewport = _trackScrollRect.viewport != null ? _trackScrollRect.viewport : _trackScrollRect.transform as RectTransform;

        if (itemRect == null || viewport == null)
            return;

        Canvas.ForceUpdateCanvases();

        Bounds itemBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, itemRect);
        float viewportCenterY = viewport.rect.center.y;
        float itemCenterY = itemBounds.center.y;
        Vector2 anchoredPosition = content.anchoredPosition;
        anchoredPosition.y += viewportCenterY - itemCenterY;
        content.anchoredPosition = anchoredPosition;

        _trackScrollRect.velocity = Vector2.zero;
        _trackScrollRect.StopMovement();
    }

    private void KillSliderTween()
    {
        if (_sliderTween == null)
            return;

        _sliderTween.Kill();
        _sliderTween = null;
    }

    private void ScheduleScrollToCurrentLevel()
    {
        StopScrollCoroutine();
        _scrollCoroutine = StartCoroutine(ScrollToCurrentLevelNextFrame());
    }

    private void ScheduleAnimateProgress()
    {
        StopLevelUpAnimationCoroutine();
        _levelUpAnimationCoroutine = StartCoroutine(AnimateProgressAfterPopupShown());
    }

    private System.Collections.IEnumerator AnimateProgressAfterPopupShown()
    {
        yield return null;
        yield return new WaitForSecondsRealtime(PopupOpenAnimationDelay);
        AnimateProgress();
        _levelUpAnimationCoroutine = null;
    }

    private System.Collections.IEnumerator ScrollToCurrentLevelNextFrame()
    {
        yield return null;
        ScrollToCurrentLevel();
        _scrollCoroutine = null;
    }

    private void StopScrollCoroutine()
    {
        if (_scrollCoroutine == null)
            return;

        StopCoroutine(_scrollCoroutine);
        _scrollCoroutine = null;
    }

    private void StopLevelUpAnimationCoroutine()
    {
        if (_levelUpAnimationCoroutine == null)
            return;

        StopCoroutine(_levelUpAnimationCoroutine);
        _levelUpAnimationCoroutine = null;
    }
}
