using UnityEngine;

public class LevelCompletionRewardViewController : MonoBehaviour
{
    [SerializeField] private RewardView _rewardViewPrefab;
    [SerializeField] private Transform _rewardsParent;
    [SerializeField] private GearPacksConfig _gearPacksConfig;
    [SerializeField] private GeneralParameters _generalParameters;

    private void OnDisable()
    {
        Clear();
    }

    public void Show(LevelCompletionReward reward)
    {
        Debug.Log($"[RewardDebug] LevelCompletionRewardView.Show start. prefab={(_rewardViewPrefab != null)}, parent={(GetParent() != null ? GetParent().name : "null")}, pack={reward.PackRarity}, packCount={reward.PackCount}, soft={reward.Soft}, hard={reward.Hard}");
        Clear();

        if (_rewardViewPrefab == null)
        {
            Debug.Log("[RewardDebug] LevelCompletionRewardView.Show stopped. reward prefab null.");
            return;
        }

        if (reward.PackCount > 0)
            SpawnPackReward(reward.PackRarity, reward.PackCount);

        if (reward.Soft > 0)
            SpawnCurrencyReward(CurrencyType.Soft, reward.Soft);

        if (reward.Hard > 0)
            SpawnCurrencyReward(CurrencyType.Hard, reward.Hard);

        Debug.Log($"[RewardDebug] LevelCompletionRewardView.Show done. childCount={(GetParent() != null ? GetParent().childCount : -1)}");
    }

    public void Clear()
    {
        Transform parent = GetParent();

        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private void SpawnPackReward(GearPackRarity rarity, int count)
    {
        RewardView view = SpawnRewardView();

        if (view == null)
        {
            Debug.Log("[RewardDebug] SpawnPackReward failed. view=null.");
            return;
        }

        Sprite icon = null;
        string displayName = $"{rarity} Pack";

        if (_gearPacksConfig != null && _gearPacksConfig.TryGetPackVisual(rarity, out GearPackVisualEntry visualEntry))
        {
            icon = visualEntry.Icon;
            displayName = visualEntry.DisplayName;
        }
        else
        {
            Debug.Log($"[RewardDebug] SpawnPackReward no pack visual. config={(_gearPacksConfig != null)}, rarity={rarity}");
        }

        Debug.Log($"[RewardDebug] SpawnPackReward initialize. rarity={rarity}, count={count}, title={displayName}, icon={icon != null}");
        view.Initialize(new RewardViewData
        {
            Type = RewardType.Pack,
            Icon = icon,
            Title = displayName,
            Amount = count
        });
    }

    private void SpawnCurrencyReward(CurrencyType currencyType, int amount)
    {
        RewardView view = SpawnRewardView();

        if (view == null)
            return;

        Sprite icon = null;

        if (_generalParameters != null)
            _generalParameters.TryGetCurrencyIcon(currencyType, out icon);

        view.Initialize(new RewardViewData
        {
            Type = currencyType == CurrencyType.Hard ? RewardType.Hard : RewardType.Soft,
            Icon = icon,
            Title = currencyType.ToString(),
            Amount = amount
        });
    }

    private RewardView SpawnRewardView()
    {
        Transform parent = GetParent();

        if (parent == null)
        {
            Debug.Log("[RewardDebug] SpawnRewardView failed. parent=null.");
            return null;
        }

        RewardView view = Instantiate(_rewardViewPrefab, parent);
        Debug.Log($"[RewardDebug] SpawnRewardView done. parent={parent.name}, childCount={parent.childCount}, view={view.name}");
        return view;
    }

    private Transform GetParent()
    {
        return _rewardsParent != null ? _rewardsParent : transform;
    }
}
