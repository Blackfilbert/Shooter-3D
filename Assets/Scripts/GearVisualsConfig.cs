using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GearVisualsConfig", menuName = "Configs/Gear Visuals Config")]
public class GearVisualsConfig : ScriptableObject
{
    [SerializeField] private string _itemId;
    [SerializeField] private InventorySlotType _slotType;
    [SerializeField] private GearVisualEntry[] _entries = Array.Empty<GearVisualEntry>();

    public string ItemId => string.IsNullOrEmpty(_itemId) ? name : _itemId;
    public InventorySlotType SlotType => _slotType;

    public bool TryGetVisual(GearRarity rarity, out GearVisualEntry visualEntry)
    {
        for (int i = 0; i < _entries.Length; i++)
        {
            if (_entries[i].Rarity == rarity)
            {
                visualEntry = _entries[i];
                return true;
            }
        }

        visualEntry = default;
        return false;
    }
}

[Serializable]
public struct GearVisualEntry
{
    [SerializeField] private GearRarity _rarity;
    [SerializeField] private string _displayName;
    [SerializeField] private string _description;
    [SerializeField] private Sprite _icon;
    [SerializeField] private GameObject _prefab;
    [SerializeField] private Sprite _backgroundSprite;

    public GearRarity Rarity => _rarity;
    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public GameObject Prefab => _prefab;
    public Sprite BackgroundSprite => _backgroundSprite;
}
