using System;

public static class EraTransitionManager
{
    public const int LevelsPerEraStep = 10;
    public const int StepsPerCycle = 4;
    public const int LevelsInFinalEraStep = 5;

    private const int LevelsPerCycle = LevelsPerEraStep * (StepsPerCycle - 1) + LevelsInFinalEraStep;

    private static EraTransitionPopup _activePopup;
    private static Action _completed;

    public static void RegisterLevelCompleted(int levelIndex)
    {
        int completedLevelNumber = levelIndex + 1;

        if (IsEraTransitionLevel(completedLevelNumber) == false)
            return;

        SaveManager.Data.PendingEraTransitions++;
        SaveManager.Save();
    }

    private static bool IsEraTransitionLevel(int completedLevelNumber)
    {
        if (completedLevelNumber <= 0)
            return false;

        int levelInCycle = completedLevelNumber % LevelsPerCycle;
        return levelInCycle == 0 || levelInCycle % LevelsPerEraStep == 0;
    }

    public static bool TryOpenPendingPopup(Action completed = null)
    {
        if (SaveManager.Data.PendingEraTransitions <= 0)
            return false;

        if (_activePopup != null)
            return true;

        if (Global.UIController == null)
            return false;

        EraTransitionPopup popup = Global.UIController.Show<EraTransitionPopup>();

        if (popup == null)
            return false;

        _completed = completed;
        _activePopup = popup;
        _activePopup.Completed += OnPopupCompleted;
        _activePopup.Open(SaveManager.Data.EraProgressStep, StepsPerCycle);
        return true;
    }

    private static void OnPopupCompleted()
    {
        if (_activePopup != null)
            _activePopup.Completed -= OnPopupCompleted;

        _activePopup = null;

        SaveManager.Data.PendingEraTransitions = Math.Max(0, SaveManager.Data.PendingEraTransitions - 1);
        SaveManager.Data.EraProgressStep = (SaveManager.Data.EraProgressStep + 1) % StepsPerCycle;
        SaveManager.Save();

        if (SaveManager.Data.PendingEraTransitions > 0 && TryOpenPendingPopup(_completed))
            return;

        Action completed = _completed;
        _completed = null;
        completed?.Invoke();
    }
}
