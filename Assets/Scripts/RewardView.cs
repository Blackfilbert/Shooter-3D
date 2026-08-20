using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardView : MonoBehaviour
{
    [SerializeField] private Image _background;
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _text;
    [SerializeField] private RewardBackgroundEntry[] _backgrounds = Array.Empty<RewardBackgroundEntry>();

    public void Initialize(RewardViewData reward)
    {
        SetBackground(reward.Type);

        if (_icon != null)
        {
            _icon.sprite = reward.Icon;
            _icon.enabled = reward.Icon != null;
        }

        if (_text != null)
            _text.text = GetText(reward);
    }

    private string GetText(RewardViewData reward)
    {
        if (reward.Type == RewardType.Pack)
            return reward.Title;

        return $"X{reward.Amount}";
    }

    private void SetBackground(RewardType rewardType)
    {
        if (_background == null)
            return;

        for (int i = 0; i < _backgrounds.Length; i++)
        {
            if (_backgrounds[i].Type != rewardType)
                continue;

            _background.sprite = _backgrounds[i].Sprite;
            _background.enabled = _backgrounds[i].Sprite != null;
            return;
        }
    }
}

public enum RewardType
{
    Pack,
    Soft,
    Hard,
    Gear
}

public struct RewardViewData
{
    public RewardType Type;
    public Sprite Icon;
    public string Title;
    public int Amount;
}

[Serializable]
public struct RewardBackgroundEntry
{
    [SerializeField] private RewardType _type;
    [SerializeField] private Sprite _sprite;

    public RewardType Type => _type;
    public Sprite Sprite => _sprite;
}
