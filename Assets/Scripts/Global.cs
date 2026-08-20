using UnityEngine;

public static class Global
{
    private static int _selectedGameplayLevelIndex;
    private static GameplayLevelsConfig _selectedGameplayLevelsConfig;
    private static GameObject _selectedGameplayLevelPrefab;

    public static PlayerMovement PlayerMovement { get; private set; }
    public static PlayerTouchInput PlayerTouchInput { get; private set; }
    public static PlayerWeapon PlayerWeapon { get; private set; }
    public static PlayerHealth PlayerHealth { get; private set; }
    public static HUDManager HUDManager { get; private set; }
    public static LobbyMenuController LobbyMenuController { get; private set; }
    public static GameplayLevelController GameplayLevelController { get; private set; }
    public static GameplayRankManager GameplayRankManager { get; private set; }
    public static GameplayTutorialManager GameplayTutorialManager { get; private set; }
    public static EnemyDeathCameraController EnemyDeathCameraController { get; private set; }
    public static PopupManager PopupManager { get; private set; }
    public static UIController UIController { get; private set; }
    public static AudioManager AudioManager { get; private set; }
    public static bool HasSelectedGameplayLevel { get; private set; }
    public static bool IsSelectedGameplayLevelSpecial { get; private set; }
    public static int SelectedGameplayLevelIndex => _selectedGameplayLevelIndex;
    public static GameplayLevelsConfig SelectedGameplayLevelsConfig => _selectedGameplayLevelsConfig;
    public static GameObject SelectedGameplayLevelPrefab => _selectedGameplayLevelPrefab;

    public static void SetSelectedGameplayLevel(int levelIndex, GameplayLevelsConfig levelsConfig, bool isSpecialLevel = false)
    {
        _selectedGameplayLevelIndex = levelIndex;
        _selectedGameplayLevelsConfig = levelsConfig;
        _selectedGameplayLevelPrefab = null;
        HasSelectedGameplayLevel = true;
        IsSelectedGameplayLevelSpecial = isSpecialLevel;
    }

    public static void SetSelectedSpecialGameplayLevel(GameObject levelPrefab)
    {
        _selectedGameplayLevelIndex = 0;
        _selectedGameplayLevelsConfig = null;
        _selectedGameplayLevelPrefab = levelPrefab;
        HasSelectedGameplayLevel = levelPrefab != null;
        IsSelectedGameplayLevelSpecial = levelPrefab != null;
    }

    public static void ClearSelectedGameplayLevel()
    {
        _selectedGameplayLevelIndex = 0;
        _selectedGameplayLevelsConfig = null;
        _selectedGameplayLevelPrefab = null;
        HasSelectedGameplayLevel = false;
        IsSelectedGameplayLevelSpecial = false;
    }

    public static void RegisterPlayerMovement(PlayerMovement playerMovement)
    {
        PlayerMovement = playerMovement;
    }

    public static void UnregisterPlayerMovement(PlayerMovement playerMovement)
    {
        if (PlayerMovement == playerMovement)
            PlayerMovement = null;
    }

    public static void RegisterPlayerTouchInput(PlayerTouchInput playerTouchInput)
    {
        PlayerTouchInput = playerTouchInput;
    }

    public static void UnregisterPlayerTouchInput(PlayerTouchInput playerTouchInput)
    {
        if (PlayerTouchInput == playerTouchInput)
            PlayerTouchInput = null;
    }

    public static void RegisterPlayerWeapon(PlayerWeapon playerWeapon)
    {
        PlayerWeapon = playerWeapon;
    }

    public static void UnregisterPlayerWeapon(PlayerWeapon playerWeapon)
    {
        if (PlayerWeapon == playerWeapon)
            PlayerWeapon = null;
    }

    public static void RegisterPlayerHealth(PlayerHealth playerHealth)
    {
        PlayerHealth = playerHealth;
    }

    public static void UnregisterPlayerHealth(PlayerHealth playerHealth)
    {
        if (PlayerHealth == playerHealth)
            PlayerHealth = null;
    }

    public static void RegisterHUDManager(HUDManager hudManager)
    {
        HUDManager = hudManager;
    }

    public static void UnregisterHUDManager(HUDManager hudManager)
    {
        if (HUDManager == hudManager)
            HUDManager = null;
    }

    public static void RegisterLobbyMenuController(LobbyMenuController lobbyMenuController)
    {
        LobbyMenuController = lobbyMenuController;
    }

    public static void UnregisterLobbyMenuController(LobbyMenuController lobbyMenuController)
    {
        if (LobbyMenuController == lobbyMenuController)
            LobbyMenuController = null;
    }

    public static void RegisterGameplayLevelController(GameplayLevelController gameplayLevelController)
    {
        GameplayLevelController = gameplayLevelController;
    }

    public static void UnregisterGameplayLevelController(GameplayLevelController gameplayLevelController)
    {
        if (GameplayLevelController == gameplayLevelController)
            GameplayLevelController = null;
    }

    public static void RegisterGameplayRankManager(GameplayRankManager gameplayRankManager)
    {
        GameplayRankManager = gameplayRankManager;
    }

    public static void UnregisterGameplayRankManager(GameplayRankManager gameplayRankManager)
    {
        if (GameplayRankManager == gameplayRankManager)
            GameplayRankManager = null;
    }

    public static void RegisterGameplayTutorialManager(GameplayTutorialManager gameplayTutorialManager)
    {
        GameplayTutorialManager = gameplayTutorialManager;
    }

    public static void UnregisterGameplayTutorialManager(GameplayTutorialManager gameplayTutorialManager)
    {
        if (GameplayTutorialManager == gameplayTutorialManager)
            GameplayTutorialManager = null;
    }

    public static void RegisterEnemyDeathCameraController(EnemyDeathCameraController enemyDeathCameraController)
    {
        EnemyDeathCameraController = enemyDeathCameraController;
    }

    public static void UnregisterEnemyDeathCameraController(EnemyDeathCameraController enemyDeathCameraController)
    {
        if (EnemyDeathCameraController == enemyDeathCameraController)
            EnemyDeathCameraController = null;
    }

    public static void RegisterPopupManager(PopupManager popupManager)
    {
        PopupManager = popupManager;
    }

    public static void UnregisterPopupManager(PopupManager popupManager)
    {
        if (PopupManager == popupManager)
            PopupManager = null;
    }

    public static void RegisterUIController(UIController uiController)
    {
        UIController = uiController;
    }

    public static void UnregisterUIController(UIController uiController)
    {
        if (UIController == uiController)
            UIController = null;
    }

    public static void RegisterAudioManager(AudioManager audioManager)
    {
        AudioManager = audioManager;
    }

    public static void UnregisterAudioManager(AudioManager audioManager)
    {
        if (AudioManager == audioManager)
            AudioManager = null;
    }
}
