using System;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyLevelStatsConfig", menuName = "Configs/Enemy Level Stats Config")]
public class EnemyLevelStatsConfig : ScriptableObject
{
    [SerializeField] private string _googleSheetUrl;
    [SerializeField] private EnemyLevelStatsEntry[] _levels = Array.Empty<EnemyLevelStatsEntry>();

    public string GoogleSheetUrl => _googleSheetUrl;
    public EnemyLevelStatsEntry[] Levels => _levels;

    public bool TryGetLevel(int levelIndex, out EnemyLevelStatsEntry levelStats)
    {
        if (_levels == null || _levels.Length == 0)
        {
            levelStats = default;
            return false;
        }

        int patternIndex = Mathf.Max(0, levelIndex) % _levels.Length;
        levelStats = _levels[patternIndex];
        return true;
    }
}

[Serializable]
public struct EnemyLevelStatsEntry
{
    [SerializeField] private int _levelIndex;
    [SerializeField] private float[] _healthMultipliers;
    [SerializeField] private string _scheme;
    [SerializeField] private float _damageReward;

    public EnemyLevelStatsEntry(int levelIndex, float[] healthMultipliers, string scheme, float damageReward)
    {
        _levelIndex = levelIndex;
        _healthMultipliers = healthMultipliers;
        _scheme = scheme;
        _damageReward = damageReward;
    }

    public int LevelIndex => _levelIndex;
    public float[] HealthMultipliers => _healthMultipliers;
    public string Scheme => _scheme;
    public float DamageReward => _damageReward;
    public int Count => GetSchemeCount();

    public float GetHealthMultiplier(int stepIndex)
    {
        if (_healthMultipliers == null || _healthMultipliers.Length == 0)
            return 1f;

        int multiplierIndex = Mathf.Clamp(stepIndex, 0, _healthMultipliers.Length - 1);
        return Mathf.Max(0.01f, _healthMultipliers[multiplierIndex]);
    }

    public float GetDamageReward(int stepIndex)
    {
        if (TryGetSchemeValue(stepIndex, out int reward))
            return reward;

        return _damageReward;
    }

    private int GetSchemeCount()
    {
        if (string.IsNullOrWhiteSpace(_scheme))
            return _healthMultipliers != null ? _healthMultipliers.Length : 0;

        return _scheme.Split('-').Length;
    }

    private bool TryGetSchemeValue(int stepIndex, out int value)
    {
        value = 0;

        if (string.IsNullOrWhiteSpace(_scheme))
            return false;

        string[] parts = _scheme.Split('-');

        if (stepIndex < 0 || stepIndex >= parts.Length)
            return false;

        return int.TryParse(parts[stepIndex], out value);
    }
}
