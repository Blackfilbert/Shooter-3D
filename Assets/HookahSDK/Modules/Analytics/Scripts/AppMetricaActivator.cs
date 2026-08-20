using Io.AppMetrica;
using UnityEngine;

namespace Hookah.Analytics.Helpers
{
    public static class AppMetricaActivator
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Activate()
        {
            AppMetrica.Activate(new AppMetricaConfig("81ee3a17-f020-4c15-9ba8-2fef61070d20")
            {
                FirstActivationAsUpdate = !IsFirstLaunch(),
            });
        }

        private static bool IsFirstLaunch()
        {
            // Implement logic to detect whether the app is opening for the first time.
            // For example, you can check for files (settings, databases, and so on),
            // which the app creates on its first launch.
            return true;
        }
    }
}