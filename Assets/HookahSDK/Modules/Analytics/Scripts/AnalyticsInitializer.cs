using Unity.VisualScripting;
using UnityEngine;
using Hookah.Analytics;

namespace Hookah.Analytics.Helpers
{
        public static class AnalyticsInitializer
        {
                [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
                private static void Init()
                {
                        GameObject analyticsObject = new GameObject("[Analytics]");
                        analyticsObject.transform.SetPositionAndRotation(Vector3.zero, new Quaternion());

                        analyticsObject.AddComponent<FB_Analytics>();
                        analyticsObject.AddComponent<AnalyticsPlaytimeReporter>();
                        
                        GameObject.DontDestroyOnLoad(analyticsObject);
                }
        }

        public class AnalyticsPlaytimeReporter : MonoBehaviour
        {
                private const string TotalPlaytimeEventKey = "total_playtime";
                private const string TotalMinutesParameterKey = "total_minutes";
                private const string SessionMinutesParameterKey = "session_minutes";

                private float _lastRecordedRealtime;

                private void Awake()
                {
                        _lastRecordedRealtime = Time.realtimeSinceStartup;
                }

                private void OnApplicationPause(bool pauseStatus)
                {
                        if (pauseStatus)
                                SendPlaytime();
                        else
                                _lastRecordedRealtime = Time.realtimeSinceStartup;
                }

                private void OnApplicationQuit()
                {
                        SendPlaytime();
                }

                private void SendPlaytime()
                {
                        float sessionSeconds = Time.realtimeSinceStartup - _lastRecordedRealtime;
                        int roundedSessionSeconds = Mathf.FloorToInt(sessionSeconds);

                        if (roundedSessionSeconds <= 0)
                                return;

                        long totalSeconds = SaveManager.AddPlaytimeSeconds(roundedSessionSeconds);

                        Analytics.Key(TotalPlaytimeEventKey)
                                .Param(TotalMinutesParameterKey, GetMinutes(totalSeconds))
                                .Param(SessionMinutesParameterKey, GetMinutes(roundedSessionSeconds))
                                .Send();

                        _lastRecordedRealtime = Time.realtimeSinceStartup;
                }

                private static string GetMinutes(float seconds)
                {
                        return Mathf.FloorToInt(seconds / 60f).ToString();
                }
        }
}
