using UnityEngine;

public class DungeonAlerts : MonoBehaviour
{
    [SerializeField] private GameObject mainAlert;
    [SerializeField] private GameObject firstDungeonAlert;
    [SerializeField] private GameObject secondDungeonAlert;

    private void Start()
    {
        RefreshAlerts();
    }

    public void RefreshAlerts()
    {
        bool showFirst =
            SaveManager.CompletedLevelIndex > 9 &&
            SpecialKeyManager.HasKey(CurrencyType.SpecialKey);

        bool showSecond =
            SaveManager.CompletedLevelIndex > 14 &&
            SpecialKeyManager.HasKey(CurrencyType.SpecialKey2);

        firstDungeonAlert.SetActive(showFirst);
        secondDungeonAlert.SetActive(showSecond);

        mainAlert.SetActive(showFirst || showSecond);
    }
}