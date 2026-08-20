using Facebook.Unity;
using UnityEngine;

public class FB_Analytics : MonoBehaviour
{
    #region Variables

    #endregion

    #region UnityMethods
    private void OnApplicationFocus(bool focus)
    {
        if (focus)
            Start();
    }

    private void Start()
    {
        if (!FB.IsInitialized)
            FB.Init(InitCallback);
        else
            FB.ActivateApp();
    }
    #endregion

    #region Callbacks
    private void InitCallback()
    {
        if (FB.IsInitialized)
            FB.ActivateApp();
        else
            Debug.Log("Failed to Initialize the Facebook SDK");
    }
    #endregion
}
