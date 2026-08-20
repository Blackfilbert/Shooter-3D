using UnityEngine;

public class LevelCompletionRandomObjectsView : MonoBehaviour
{
    private const int RequiredCompletedLevelIndex = 9;

    [SerializeField] private GameObject[] _objects;
    [SerializeField] private int _minActiveObjects = 1;
    [SerializeField] private int _maxActiveObjects = 11;

    private void OnEnable()
    {
        ApplySavedState();
    }

    private void ApplySavedState()
    {
        int completedLevelIndex = SaveManager.CompletedLevelIndex;

        if (completedLevelIndex < RequiredCompletedLevelIndex)
        {
            gameObject.SetActive(false);
            return;
        }

        if (SaveManager.HasLevelCompletionActiveObjectsCount && SaveManager.LevelCompletionObjectLevelIndex == completedLevelIndex)
        {
            ApplyActiveCount(SaveManager.LevelCompletionActiveObjectsCount);
            return;
        }

        int activeCount = GenerateRandomActiveCount();
        SaveManager.SetLevelCompletionActiveObjectsCount(activeCount);
        ApplyActiveCount(activeCount);
    }

    private int GenerateRandomActiveCount()
    {
        int objectsCount = GetValidObjectsCount();
        int minActive = Mathf.Clamp(_minActiveObjects, 0, objectsCount);
        int maxActive = Mathf.Clamp(_maxActiveObjects, minActive, objectsCount);
        return Random.Range(minActive, maxActive + 1);
    }

    private int GetValidObjectsCount()
    {
        return Mathf.Min(_objects != null ? _objects.Length : 0, 31);
    }

    private void ApplyActiveCount(int activeCount)
    {
        int objectsCount = GetValidObjectsCount();
        activeCount = Mathf.Clamp(activeCount, 0, objectsCount);

        for (int i = 0; i < objectsCount; i++)
        {
            if (_objects[i] != null)
                _objects[i].SetActive(i < activeCount);
        }

        for (int i = objectsCount; _objects != null && i < _objects.Length; i++)
        {
            if (_objects[i] != null)
                _objects[i].SetActive(false);
        }
    }

    private void SetAllObjectsActive(bool isActive)
    {
        if (_objects == null)
            return;

        for (int i = 0; i < _objects.Length; i++)
        {
            if (_objects[i] != null)
                _objects[i].SetActive(isActive);
        }
    }
}
