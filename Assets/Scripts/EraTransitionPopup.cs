using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class EraTransitionPopup : UIPopup
{
    private const float PopupOpenAnimationDelay = 0.25f;

    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private Slider[] _sliders;
    [SerializeField] private GameObject[] _completeIconStates;
    [SerializeField] private GameObject[] _activeIconStates;
    [SerializeField] private GameObject[] _lockedIconStates;
    [SerializeField] private float _fillDuration = 0.45f;
    [SerializeField] private float _resetDuration = 0.25f;
    [SerializeField] private float _completedDelay = 0.35f;

    private Sequence _sequence;

    public event Action Completed;

    public void Open(int currentStep, int stepsPerCycle)
    {
        Show();
        Play(currentStep, stepsPerCycle);
    }

    protected override void OnDestroy()
    {
        KillSequence();
        base.OnDestroy();
    }

    private void Play(int currentStep, int stepsPerCycle)
    {
        KillSequence();

        int stepsCount = Mathf.Max(1, stepsPerCycle);
        int step = Mathf.Clamp(currentStep, 0, stepsCount - 1);
        SetupState(step);
        ScrollToStep(step);

        _sequence = DOTween.Sequence().SetUpdate(true);
        _sequence.AppendInterval(PopupOpenAnimationDelay);
        AppendFillStep(_sequence, step);

        if (step >= stepsCount - 1)
            AppendReset(_sequence);

        _sequence.AppendInterval(_completedDelay);
        _sequence.OnComplete(Complete);
    }

    private void SetupState(int currentStep)
    {
        int count = GetItemsCount();

        for (int i = 0; i < count; i++)
        {
            SetSliderValue(i, i < currentStep ? 1f : 0f);
            SetEraState(i, GetEraState(i, currentStep));
        }
    }

    private void AppendFillStep(Sequence sequence, int step)
    {
        Slider slider = GetSlider(step);

        if (slider != null)
        {
            slider.value = 0f;
            sequence.Append(slider.DOValue(1f, _fillDuration).SetEase(Ease.OutCubic));
        }

        sequence.AppendCallback(() =>
        {
            SetEraState(step, EraState.Complete);
            SetEraState(step + 1, EraState.Active);
        });
    }

    private void AppendReset(Sequence sequence)
    {
        int count = GetItemsCount();
        bool hasResetTween = false;

        for (int i = 0; i < count; i++)
        {
            Slider slider = GetSlider(i);

            if (slider == null)
                continue;

            Tween resetTween = slider.DOValue(0f, _resetDuration).SetEase(Ease.InCubic);

            if (hasResetTween)
                sequence.Join(resetTween);
            else
                sequence.Append(resetTween);

            hasResetTween = true;
        }

        sequence.AppendCallback(ResetState);
    }

    private void Complete()
    {
        Hide();
        Completed?.Invoke();
    }

    private void ResetState()
    {
        int count = GetItemsCount();

        for (int i = 0; i < count; i++)
        {
            SetSliderValue(i, 0f);
            SetEraState(i, i == 0 ? EraState.Active : EraState.Locked);
        }
    }

    private void ScrollToStep(int step)
    {
        if (_scrollRect == null || _scrollRect.content == null)
            return;

        Slider slider = GetSlider(step);
        RectTransform itemRect = slider != null ? slider.transform as RectTransform : null;
        RectTransform content = _scrollRect.content;
        RectTransform viewport = _scrollRect.viewport != null ? _scrollRect.viewport : _scrollRect.transform as RectTransform;

        if (itemRect == null || viewport == null)
            return;

        Canvas.ForceUpdateCanvases();

        Bounds itemBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, itemRect);
        Vector2 anchoredPosition = content.anchoredPosition;

        if (_scrollRect.vertical)
        {
            float viewportCenterY = viewport.rect.center.y;
            float itemCenterY = itemBounds.center.y;
            anchoredPosition.y += viewportCenterY - itemCenterY;
        }

        if (_scrollRect.horizontal)
        {
            float viewportCenterX = viewport.rect.center.x;
            float itemCenterX = itemBounds.center.x;
            anchoredPosition.x += viewportCenterX - itemCenterX;
        }

        content.anchoredPosition = anchoredPosition;
        _scrollRect.velocity = Vector2.zero;
        _scrollRect.StopMovement();
    }

    private int GetItemsCount()
    {
        int slidersCount = _sliders != null ? _sliders.Length : 0;
        int completeStatesCount = _completeIconStates != null ? _completeIconStates.Length : 0;
        int activeStatesCount = _activeIconStates != null ? _activeIconStates.Length : 0;
        int lockedStatesCount = _lockedIconStates != null ? _lockedIconStates.Length : 0;
        return Mathf.Max(slidersCount, Mathf.Max(completeStatesCount, Mathf.Max(activeStatesCount, lockedStatesCount)));
    }

    private Slider GetSlider(int index)
    {
        if (_sliders == null || index < 0 || index >= _sliders.Length)
            return null;

        return _sliders[index];
    }

    private void SetSliderValue(int index, float value)
    {
        Slider slider = GetSlider(index);

        if (slider != null)
            slider.value = value;
    }

    private EraState GetEraState(int index, int currentStep)
    {
        if (index < currentStep)
            return EraState.Complete;

        return index == currentStep ? EraState.Active : EraState.Locked;
    }

    private void SetEraState(int index, EraState state)
    {
        SetStateObjectActive(_completeIconStates, index, state == EraState.Complete);
        SetStateObjectActive(_activeIconStates, index, state == EraState.Active);
        SetStateObjectActive(_lockedIconStates, index, state == EraState.Locked);
    }

    private void SetStateObjectActive(GameObject[] states, int index, bool isActive)
    {
        if (states == null || index < 0 || index >= states.Length)
            return;

        if (states[index] != null)
            states[index].SetActive(isActive);
    }

    private void KillSequence()
    {
        if (_sequence == null)
            return;

        _sequence.Kill();
        _sequence = null;
    }

    private enum EraState
    {
        Locked,
        Active,
        Complete
    }
}
