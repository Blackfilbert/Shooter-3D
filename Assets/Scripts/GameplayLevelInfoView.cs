using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayLevelInfoView : MonoBehaviour
{
    private const int LevelsPerEra = 10;

    [SerializeField] private GameplayLevelInfoConfig _infoConfig;
    [SerializeField] private LevelSelectorView _levelSelectorView;
    [SerializeField] private int _levelIndex;
    [SerializeField] private Image _image;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _arenaText;
    [SerializeField] private TMP_Text _recommendedGearScoreText;

    private void OnEnable()
    {
        if (_levelSelectorView != null)
            _levelSelectorView.SelectedLevelChanged += OnSelectedLevelChanged;

        UpdateView(GetLevelIndex());
    }

    private void OnDisable()
    {
        if (_levelSelectorView != null)
            _levelSelectorView.SelectedLevelChanged -= OnSelectedLevelChanged;
    }

    public void SetLevelIndex(int levelIndex)
    {
        _levelIndex = Mathf.Max(0, levelIndex);
        UpdateView(_levelIndex);
    }

    private void OnSelectedLevelChanged(int levelIndex)
    {
        UpdateView(levelIndex);
    }

    private int GetLevelIndex()
    {
        if (_levelSelectorView != null)
            return _levelSelectorView.SelectedLevelIndex;

        if (Global.HasSelectedGameplayLevel)
            return Global.SelectedGameplayLevelIndex;

        if (SaveManager.CompletedLevelIndex >= 0)
            return SaveManager.CompletedLevelIndex + 1;

        if (SaveManager.HasSelectedLevel)
            return SaveManager.SelectedLevelIndex;

        return Mathf.Max(0, _levelIndex);
    }

    private void UpdateView(int levelIndex)
    {
        int eraLevelIndex = GetEraLevelIndex(levelIndex);
        GameplayLevelInfo levelInfo = default;
        bool hasInfo = _infoConfig != null;

        if (hasInfo)
            hasInfo = _infoConfig.TryGetLevelInfo(levelIndex, out levelInfo);

        if (_image != null && hasInfo && levelInfo.Image != null)
            _image.sprite = levelInfo.Image;

        if (_titleText != null)
            _titleText.text = hasInfo && string.IsNullOrEmpty(levelInfo.Title) == false ? levelInfo.Title : string.Empty;

        if (_arenaText != null)
            _arenaText.text = $"ARENA {eraLevelIndex + 1}";

        if (_recommendedGearScoreText != null)
        {
            int recommendedGearScore = hasInfo ? levelInfo.RecommendedGearScore : 0;
            _recommendedGearScoreText.text = $"rec. combat power {recommendedGearScore}";
        }
    }

    private int GetEraLevelIndex(int levelIndex)
    {
        return Mathf.Max(0, levelIndex) % LevelsPerEra;
    }
}
