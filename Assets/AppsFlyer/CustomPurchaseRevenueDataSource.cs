using System.Collections.Generic;
using AppsFlyerSDK;
using UnityEngine;

public class CustomPurchaseRevenueDataSource : MonoBehaviour, IAppsFlyerPurchaseRevenueDataSource
{
    public Dictionary<string, object> PurchaseRevenueAdditionalParametersForProducts(HashSet<object> products, HashSet<object> transactions)
    {
        int currentStage = PlayerPrefs.GetInt("selectedLevelNumber");
        int isElite = PlayerPrefs.GetInt("isEliteMode");
        string stageType = isElite == 1 ? "elite" : "normal";

        return new Dictionary<string, object>
    {
        { "custom_data", $"{{\"{stageType}stage\":\"{currentStage}\"}}" }
    };
    }
}
