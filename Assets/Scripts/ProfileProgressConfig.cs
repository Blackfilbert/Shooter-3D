using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ProfileProgressConfig", menuName = "Configs/Profile Progress Config")]
public class ProfileProgressConfig : ScriptableObject
{
    [SerializeField] private int _maxLevel = 10;
    [SerializeField] private ProfileLevelReward[] _rewards = Array.Empty<ProfileLevelReward>();

    public int MaxLevel => Mathf.Max(1, _maxLevel);
    public ProfileLevelReward[] Rewards => _rewards;

    public bool TryGetReward(int level, out ProfileLevelReward reward)
    {
        for (int i = 0; i < _rewards.Length; i++)
        {
            if (_rewards[i].Level == level)
            {
                reward = _rewards[i];
                return true;
            }
        }

        reward = default;
        return false;
    }
}

[Serializable]
public struct ProfileLevelReward
{
    [SerializeField] private int _level;
    [SerializeField] private string _title;
    [SerializeField] private Sprite _icon;

    public int Level => _level;
    public string Title => _title;
    public Sprite Icon => _icon;
}
