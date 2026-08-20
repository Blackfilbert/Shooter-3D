using System.Collections;
using System.Collections.Generic;
using AppsFlyerSDK;
using Io.AppMetrica;
using Leguar.TotalJSON;
using UnityEngine;

namespace Hookah.Analytics
{
    public class Analytics
    {
        #region Methods
        public static AnalyticsEventBuilder Key(string key)
        {
            return new AnalyticsEventBuilder(key);
        }
        #endregion

        #region Tools
        public static void Log(string key, string value = "Empty")
        {
            Debug.Log($"<color=green>[Analytics] >> {key} : {value}</color>");
        }

        private static int RoundTime(float startTime, float endTime)
        {
            float time = endTime - startTime;

            return Mathf.Abs(Mathf.CeilToInt(time / 10.0f) * 10);
        }
        #endregion

        #region Classes
        public class AnalyticsEventBuilder
        {
            private readonly string _key;
            private readonly Dictionary<string, object> _parameters = new Dictionary<string, object>();

            public AnalyticsEventBuilder(string key)
            {
                _key = key;
            }
            
            public AnalyticsEventBuilder Param(string paramName, string paramValue = "None")
            {
                _parameters[paramName] = paramValue;
                
                return this;
            }
            
            public void Send()
            {
                string jsonParams = JSON.Serialize(_parameters).CreateString();

                AppMetrica.ReportEvent(_key, jsonParams);
                AppsFlyer.sendEvent(_key, ToAppsFlyerParameters());

                Log(_key, jsonParams);
            }

            private Dictionary<string, string> ToAppsFlyerParameters()
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();

                foreach (KeyValuePair<string, object> parameter in _parameters)
                    parameters[parameter.Key] = parameter.Value != null ? parameter.Value.ToString() : string.Empty;

                return parameters;
            }
        }
        #endregion
    }
}
