using UnityEngine;

public class GearPackRewardViewController : MonoBehaviour
{
    [SerializeField] private RewardView _rewardViewPrefab;
    [SerializeField] private Transform _rewardsParent;
    [SerializeField] private GearVisualsConfig[] _gearVisualsConfigs;
    [SerializeField] private GeneralParameters _generalParameters;

    private void OnDisable()
    {
        Clear();
    }

    public void ShowAll(GearPackReward reward)
    {
        Debug.Log($"[RewardDebug] GearPackRewardView.ShowAll start. prefab={_rewardViewPrefab != null}, parent={(GetParent() != null ? GetParent().name : "null")}, items={(reward.Items != null ? reward.Items.Length : 0)}, soft={reward.Soft}");
        Clear();

        if (_rewardViewPrefab == null)
        {
            Debug.Log("[RewardDebug] GearPackRewardView.ShowAll stopped. reward prefab null.");
            return;
        }

        if (reward.Items != null)
        {
            for (int i = 0; i < reward.Items.Length; i++)
            {
                if (IsValidItemReward(reward.Items[i]))
                    SpawnItemReward(reward.Items[i]);
            }
        }

        if (reward.Soft > 0)
            SpawnCurrencyReward(CurrencyType.Soft, reward.Soft);

        Debug.Log($"[RewardDebug] GearPackRewardView.ShowAll done. childCount={(GetParent() != null ? GetParent().childCount : -1)}");
    }

    public void ShowSingle(GearPackReward reward, int rewardIndex)
    {
        Debug.Log($"[RewardDebug] GearPackRewardView.ShowSingle start. rewardIndex={rewardIndex}, prefab={_rewardViewPrefab != null}, parent={(GetParent() != null ? GetParent().name : "null")}, itemRewards={GetItemRewardsCount(reward)}, soft={reward.Soft}");
        Clear();

        if (_rewardViewPrefab == null)
        {
            Debug.Log("[RewardDebug] GearPackRewardView.ShowSingle stopped. reward prefab null.");
            return;
        }

        int itemCount = GetItemRewardsCount(reward);

        if (rewardIndex < itemCount)
        {
            if (TryGetItemReward(reward, rewardIndex, out GearPackItemReward itemReward))
            {
                Debug.Log($"[RewardDebug] GearPackRewardView.ShowSingle item. itemId={itemReward.ItemId}, slot={itemReward.SlotType}, rarity={itemReward.Rarity}, cards={itemReward.Cards}");
                SpawnItemReward(itemReward);
            }

            return;
        }

        if (rewardIndex == itemCount && reward.Soft > 0)
        {
            Debug.Log($"[RewardDebug] GearPackRewardView.ShowSingle soft. amount={reward.Soft}");
            SpawnCurrencyReward(CurrencyType.Soft, reward.Soft);
        }
    }

    public int GetRewardsCount(GearPackReward reward)
    {
        int count = GetItemRewardsCount(reward);

        if (reward.Soft > 0)
            count++;

        return count;
    }

    public void Clear()
    {
        Transform parent = GetParent();

        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private void SpawnItemReward(GearPackItemReward reward)
    {
        RewardView view = SpawnRewardView();

        if (view == null)
        {
            Debug.Log("[RewardDebug] GearPackRewardView.SpawnItemReward failed. view=null.");
            return;
        }

        Sprite icon = GetItemIcon(reward);
        Debug.Log($"[RewardDebug] GearPackRewardView.SpawnItemReward. itemId={reward.ItemId}, slot={reward.SlotType}, rarity={reward.Rarity}, cards={reward.Cards}, icon={icon != null}");
        view.Initialize(new RewardViewData
        {
            Type = RewardType.Gear,
            Icon = icon,
            Title = $"{reward.Rarity} {reward.SlotType}",
            Amount = reward.Cards
        });
    }

    private int GetItemRewardsCount(GearPackReward reward)
    {
        int count = 0;

        if (reward.Items == null)
            return count;

        for (int i = 0; i < reward.Items.Length; i++)
        {
            if (IsValidItemReward(reward.Items[i]))
                count++;
        }

        return count;
    }

    private bool TryGetItemReward(GearPackReward reward, int rewardIndex, out GearPackItemReward itemReward)
    {
        int currentIndex = 0;

        if (reward.Items != null)
        {
            for (int i = 0; i < reward.Items.Length; i++)
            {
                if (IsValidItemReward(reward.Items[i]) == false)
                    continue;

                if (currentIndex == rewardIndex)
                {
                    itemReward = reward.Items[i];
                    return true;
                }

                currentIndex++;
            }
        }

        itemReward = default;
        return false;
    }

    private bool IsValidItemReward(GearPackItemReward reward)
    {
        return string.IsNullOrEmpty(reward.ItemId) == false && reward.Cards > 0;
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
            Debug.Log("[RewardDebug] GearPackRewardView.SpawnRewardView failed. parent=null.");
            return null;
        }

        RewardView view = Instantiate(_rewardViewPrefab, parent);
        Debug.Log($"[RewardDebug] GearPackRewardView.SpawnRewardView done. parent={parent.name}, childCount={parent.childCount}, view={view.name}");
        return view;
    }

    private Transform GetParent()
    {
        return _rewardsParent != null ? _rewardsParent : transform;
    }

    private Sprite GetItemIcon(GearPackItemReward reward)
    {
        GearVisualsConfig config = GetVisualsConfig(reward.ItemId, reward.SlotType);

        if (config != null && config.TryGetVisual(reward.Rarity, out GearVisualEntry visualEntry))
            return visualEntry.Icon;

        return null;
    }

    private GearVisualsConfig GetVisualsConfig(string itemId, InventorySlotType slotType)
    {
        if (_gearVisualsConfigs == null)
            return null;

        for (int i = 0; i < _gearVisualsConfigs.Length; i++)
        {
            if (_gearVisualsConfigs[i] == null)
                continue;

            if (_gearVisualsConfigs[i].ItemId == itemId && _gearVisualsConfigs[i].SlotType == slotType)
                return _gearVisualsConfigs[i];
        }

        return null;
    }
}
