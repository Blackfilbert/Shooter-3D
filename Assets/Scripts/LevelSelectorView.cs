using TMPro;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectorView : MonoBehaviour
{
    [SerializeField] private GameplayLevelsConfig _levelsConfig;
    [SerializeField] private Button _startButton;
    [SerializeField] private string _gameplaySceneName;

    private int _selectedLevelIndex;

    public GameplayLevelsConfig LevelsConfig => _levelsConfig;
    public int SelectedLevelIndex => _selectedLevelIndex;

    public event Action<int> SelectedLevelChanged;

    private void Awake()
    {
        RefreshSelectedLevelIndex();
    }

    private void OnEnable()
    {
        CurrencyManager.CurrencyChanged += OnCurrencyChanged;

        if (_startButton != null)
            _startButton.onClick.AddListener(StartSelectedLevel);

        RefreshSelectedLevelIndex();
        UpdateView();
        SelectedLevelChanged?.Invoke(_selectedLevelIndex);
    }

    private void OnDisable()
    {
        CurrencyManager.CurrencyChanged -= OnCurrencyChanged;

        if (_startButton != null)
            _startButton.onClick.RemoveListener(StartSelectedLevel);
    }

    public void SelectPrevious()
    {
        SetSelectedLevel(_selectedLevelIndex - 1);
    }

    public void SelectNext()
    {
        SetSelectedLevel(_selectedLevelIndex + 1);
    }

    public void SetSelectedLevel(int levelIndex)
    {
        int selectedLevelIndex = Mathf.Clamp(levelIndex, 0, GetLastUnlockedLevelIndex());

        if (_selectedLevelIndex == selectedLevelIndex)
        {
            UpdateView();
            return;
        }

        _selectedLevelIndex = selectedLevelIndex;
        UpdateView();
        SelectedLevelChanged?.Invoke(_selectedLevelIndex);
    }

    public void StartSelectedLevel()
    {
        if (GetLevelsCount() <= 0 || string.IsNullOrEmpty(_gameplaySceneName))
            return;

        if (EnergyManager.TrySpendForLevel(_selectedLevelIndex) == false)
        {
            UpdateView();
            return;
        }

        SaveManager.SetSelectedLevel(_selectedLevelIndex);
        Global.SetSelectedGameplayLevel(_selectedLevelIndex, _levelsConfig);
        SceneManager.LoadScene(_gameplaySceneName);
    }

    private void UpdateView()
    {
        int levelsCount = GetLevelsCount();

        if (_startButton != null)
        {
            _startButton.gameObject.SetActive(levelsCount > 0 && string.IsNullOrEmpty(_gameplaySceneName) == false);
            _startButton.interactable = levelsCount > 0 && EnergyManager.HasEnergyForLevel(_selectedLevelIndex);
        }
    }

    private void OnCurrencyChanged(CurrencyType currencyType, int count)
    {
        if (currencyType == CurrencyType.Energy)
            UpdateView();
    }

    private int GetLevelsCount()
    {
        return _levelsConfig != null ? _levelsConfig.Count : 0;
    }

    private int GetLastUnlockedLevelIndex()
    {
        int levelsCount = GetLevelsCount();

        if (levelsCount <= 0)
            return 0;

        return Mathf.Max(0, SaveManager.CompletedLevelIndex + 1);
    }

    private void RefreshSelectedLevelIndex()
    {
        _selectedLevelIndex = GetLastUnlockedLevelIndex();
    }
}
