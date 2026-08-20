using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GeneralParameters", menuName = "Configs/General Parameters")]
public class GeneralParameters : ScriptableObject
{
    [SerializeField] private CurrencyIconEntry[] _currencyIcons = Array.Empty<CurrencyIconEntry>();
    [SerializeField] private int _levelExperience = 5;
    [SerializeField] private int _gearUpgradeExperience = 10;

    public int LevelExperience => Mathf.Max(0, _levelExperience);
    public int GearUpgradeExperience => Mathf.Max(0, _gearUpgradeExperience);

    public bool TryGetCurrencyIcon(CurrencyType currencyType, out Sprite icon)
    {
        for (int i = 0; i < _currencyIcons.Length; i++)
        {
            if (_currencyIcons[i].Type == currencyType)
            {
                icon = _currencyIcons[i].Icon;
                return icon != null;
            }
        }

        icon = null;
        return false;
    }
}

[Serializable]
public struct CurrencyIconEntry
{
    [SerializeField] private CurrencyType _type;
    [SerializeField] private Sprite _icon;

    public CurrencyType Type => _type;
    public Sprite Icon => _icon;
}
