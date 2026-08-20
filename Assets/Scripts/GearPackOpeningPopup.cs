using System;
using UnityEngine;
using UnityEngine.UI;

public class GearPackOpeningPopup : UIPopup
{
    [SerializeField] private Button _clickArea;
    [SerializeField] private GameObject[] _initialStateObjects;
    [SerializeField] private GearPackRewardViewController _rewardViewController;

    private GearPackReward _reward;
    private int _rewardIndex;
    private bool _hasReward;

    public event Action Completed;

    protected override void Awake()
    {
        base.Awake();

        if (_clickArea != null)
            _clickArea.onClick.AddListener(ShowNextReward);
    }

    protected override void OnDestroy()
    {
        if (_clickArea != null)
            _clickArea.onClick.RemoveListener(ShowNextReward);

        base.OnDestroy();
    }

    public void Open(GearPackReward reward)
    {
        Debug.Log($"[RewardDebug] GearPackOpeningPopup.Open. rewardView={(_rewardViewController != null)}, pack={reward.PackRarity}, items={(reward.Items != null ? reward.Items.Length : 0)}, soft={reward.Soft}");
        _reward = reward;
        _rewardIndex = -1;
        _hasReward = true;

        SetInitialStateObjects(true);

        if (_rewardViewController != null)
            _rewardViewController.Clear();

        Show();
    }

    private void ShowNextReward()
    {
        if (_hasReward == false)
        {
            Debug.Log("[RewardDebug] GearPackOpeningPopup.ShowNextReward skipped. hasReward=false.");
            return;
        }

        _rewardIndex++;

        if (_rewardIndex == 0)
            SetInitialStateObjects(false);

        int rewardsCount = _rewardViewController != null ? _rewardViewController.GetRewardsCount(_reward) : GetFallbackRewardsCount();
        Debug.Log($"[RewardDebug] GearPackOpeningPopup.ShowNextReward. rewardIndex={_rewardIndex}, rewardsCount={rewardsCount}");

        if (_rewardIndex < rewardsCount)
        {
            ShowReward(_rewardIndex);

            return;
        }

        _hasReward = false;
        Hide();
        Completed?.Invoke();
    }

    private void ShowReward(int rewardIndex)
    {
        if (_rewardViewController != null)
        {
            _rewardViewController.ShowSingle(_reward, rewardIndex);
            return;
        }
    }

    private int GetFallbackRewardsCount()
    {
        int count = GetFallbackItemRewardsCount();

        if (_reward.Soft > 0)
            count++;

        return count;
    }

    private int GetFallbackItemRewardsCount()
    {
        int count = 0;

        if (_reward.Items == null)
            return count;

        for (int i = 0; i < _reward.Items.Length; i++)
        {
            if (IsValidFallbackItemReward(_reward.Items[i]))
                count++;
        }

        return count;
    }

    private bool IsValidFallbackItemReward(GearPackItemReward itemReward)
    {
        return string.IsNullOrEmpty(itemReward.ItemId) == false && itemReward.Cards > 0;
    }

    private void SetInitialStateObjects(bool isActive)
    {
        if (_initialStateObjects == null)
            return;

        for (int i = 0; i < _initialStateObjects.Length; i++)
        {
            if (_initialStateObjects[i] != null)
                _initialStateObjects[i].SetActive(isActive);
        }
    }
}
