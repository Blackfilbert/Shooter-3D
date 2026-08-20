using UnityEngine;
using UnityEngine.InputSystem;

public class Cheats : MonoBehaviour
{
    private static Cheats _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.dKey.wasPressedThisFrame)
            CurrencyManager.Add(CurrencyType.Energy, 1);

        if (keyboard.sKey.wasPressedThisFrame)
            CurrencyManager.Add(CurrencyType.Soft, 100);

        if (keyboard.spaceKey.wasPressedThisFrame && Global.GameplayLevelController != null)
            Global.GameplayLevelController.CheatWinLevel();
    }
}
