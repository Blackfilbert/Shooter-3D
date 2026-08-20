using TMPro;
using UnityEngine;

public class GameplayLevelTextView : MonoBehaviour
{
    [SerializeField] private TMP_Text _levelText;

    private GameplayLevelController _levelController;

    private void OnEnable()
    {
        Subscribe();
        UpdateView(GetLevelIndex());
    }

    private void Update()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (_levelController != null || Global.GameplayLevelController == null)
            return;

        _levelController = Global.GameplayLevelController;
        _levelController.LevelLoaded += UpdateView;

        if (_levelController.CurrentLevelIndex >= 0)
            UpdateView(_levelController.CurrentLevelIndex);
    }

    private void Unsubscribe()
    {
        if (_levelController == null)
            return;

        _levelController.LevelLoaded -= UpdateView;
        _levelController = null;
    }

    private int GetLevelIndex()
    {
        if (_levelController != null && _levelController.CurrentLevelIndex >= 0)
            return _levelController.CurrentLevelIndex;

        if (Global.HasSelectedGameplayLevel)
            return Global.SelectedGameplayLevelIndex;

        if (SaveManager.HasSelectedLevel)
            return SaveManager.SelectedLevelIndex;

        return 0;
    }

    private void UpdateView(int levelIndex)
    {
        if (_levelText != null)
            _levelText.text = $"Level {Mathf.Max(0, levelIndex) + 1}";
    }
}
