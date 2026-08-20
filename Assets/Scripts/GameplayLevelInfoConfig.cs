using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GameplayLevelInfoConfig", menuName = "Shooter/Gameplay Level Info Config")]
public class GameplayLevelInfoConfig : ScriptableObject
{
    [SerializeField] private GameplayLevelInfo[] _levels = Array.Empty<GameplayLevelInfo>();

    public bool TryGetLevelInfo(int levelIndex, out GameplayLevelInfo levelInfo)
    {
        levelInfo = default;

        if (_levels == null || _levels.Length == 0)
            return false;

        int infoIndex = Mathf.Max(0, levelIndex) % _levels.Length;
        levelInfo = _levels[infoIndex];
        return true;
    }
}

[Serializable]
public struct GameplayLevelInfo
{
    [SerializeField] private Sprite _image;
    [SerializeField] private string _title;
    [SerializeField] private int _recommendedGearScore;

    public Sprite Image => _image;
    public string Title => _title;
    public int RecommendedGearScore => Mathf.Max(0, _recommendedGearScore);
}
