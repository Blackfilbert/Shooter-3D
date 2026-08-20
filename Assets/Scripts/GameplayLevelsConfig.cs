using UnityEngine;

[CreateAssetMenu(fileName = "GameplayLevelsConfig", menuName = "Shooter/Gameplay Levels Config")]
public class GameplayLevelsConfig : ScriptableObject
{
    [SerializeField] private GameObject[] _levelPrefabs;

    public int Count => _levelPrefabs != null ? _levelPrefabs.Length : 0;

    public bool TryGetLevelPrefab(int levelIndex, out GameObject prefab)
    {
        prefab = null;

        if (_levelPrefabs == null || _levelPrefabs.Length == 0)
            return false;

        int prefabIndex = Mathf.Max(0, levelIndex) % _levelPrefabs.Length;
        prefab = _levelPrefabs[prefabIndex];
        return prefab != null;
    }
}
