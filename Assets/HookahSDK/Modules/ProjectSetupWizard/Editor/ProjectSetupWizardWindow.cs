using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Facebook.Unity.Settings;
using Google;
using UnityEditor.Android;
using UnityEditor.Build;

namespace Hookah.Editor.ProjectSetupWizard
{
    public class ProjectSetupWizardWindow : EditorWindow
    {
        #region Constants
        private const string LOGO_ICON_CODE = "iVBORw0KGgoAAAANSUhEUgAAAlgAAAFgCAYAAABnpweBAAAACXBIWXMAAAsTAAALEwEAmpwYAAAE9GlUWHRYTUw6Y29tLmFkb2JlLnhtcAAAAAAAPD94cGFja2V0IGJlZ2luPSLvu78iIGlkPSJXNU0wTXBDZWhpSHpyZVN6TlRjemtjOWQiPz4gPHg6eG1wbWV0YSB4bWxuczp4PSJhZG9iZTpuczptZXRhLyIgeDp4bXB0az0iQWRvYmUgWE1QIENvcmUgOS4xLWMwMDIgNzkuYjdjNjRjY2Y5LCAyMDI0LzA3LzE2LTEyOjM5OjA0ICAgICAgICAiPiA8cmRmOlJERiB4bWxuczpyZGY9Imh0dHA6Ly93d3cudzMub3JnLzE5OTkvMDIvMjItcmRmLXN5bnRheC1ucyMiPiA8cmRmOkRlc2NyaXB0aW9uIHJkZjphYm91dD0iIiB4bWxuczp4bXA9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC8iIHhtbG5zOmRjPSJodHRwOi8vcHVybC5vcmcvZGMvZWxlbWVudHMvMS4xLyIgeG1sbnM6cGhvdG9zaG9wPSJodHRwOi8vbnMuYWRvYmUuY29tL3Bob3Rvc2hvcC8xLjAvIiB4bWxuczp4bXBNTT0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wL21tLyIgeG1sbnM6c3RFdnQ9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9zVHlwZS9SZXNvdXJjZUV2ZW50IyIgeG1wOkNyZWF0b3JUb29sPSJBZG9iZSBQaG90b3Nob3AgMjYuMSAoTWFjaW50b3NoKSIgeG1wOkNyZWF0ZURhdGU9IjIwMjQtMTItMzFUMDI6MjA6NDcrMDQ6MDAiIHhtcDpNb2RpZnlEYXRlPSIyMDI1LTAyLTAzVDEzOjAxOjQ2KzA0OjAwIiB4bXA6TWV0YWRhdGFEYXRlPSIyMDI1LTAyLTAzVDEzOjAxOjQ2KzA0OjAwIiBkYzpmb3JtYXQ9ImltYWdlL3BuZyIgcGhvdG9zaG9wOkNvbG9yTW9kZT0iMyIgeG1wTU06SW5zdGFuY2VJRD0ieG1wLmlpZDpiZTA4YmNjMS1kOGVhLTQ2ZmItYjEwMC1iNjRmNzcyMjkyNzEiIHhtcE1NOkRvY3VtZW50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";
        private const int TOTAL_WIZARD_STEPS = 3;
        private string[] THIRD_PARTY_ASSETS = new string[]
        {
            "Tools - UltimateScreenshotCreator - AlmostEngine",
            "Tools - DOTween - Demigiant", 
            "Tools - SRDebugger - StompyRobot", 
            "Code - TotalJSON - Leguar",
            "UI - ParticleEffectForUGUI - mob-sakai", 
            "UI - UIEffect - mob-sakai"
        };
        #endregion
        
        #region Variables
        private Texture2D _logoTexture;
        private int _currentWizardStep = 0;
        private ProjectData _projectData = new ProjectData();
        private AnalyticsSettings _analyticsData = new AnalyticsSettings();
        private Vector2 _thirdPartyScrollPos = Vector2.zero;
        #endregion
 
        #region Window
        [MenuItem("Hookah/Project/Setup Wizard")]
        public static void ShowWindow()
        {
            ProjectSetupWizardWindow window = CreateInstance<ProjectSetupWizardWindow>();
            window.titleContent = new GUIContent("Hookah SDK - Project Setup Wizard");

            window.minSize = new Vector2(600, 300);
            window.maxSize = new Vector2(600, 300);

            window.ShowUtility();
        }
#endregion

#region UnityMethods
        private void OnEnable()
        {
            if (_logoTexture == null)
            {
                _logoTexture = new Texture2D(2, 2);
                
                byte[] imageData = Convert.FromBase64String(LOGO_ICON_CODE);
                _logoTexture.LoadImage(imageData);
            }
        }

        private void OnGUI()
        {
            switch (_currentWizardStep)
            {
                case 0:
                    DrawWelcomeStep();
                    break;
                case 1:
                    DrawProjectInfoStep();
                    break;
                case 2:
                    DrawAnalyticsSettingsStep();
                    break;
                case 3:
                    DrawThirdPartyAssetsStep();
                    break;
                default:
                    DrawWelcomeStep();
                    break;
            }
        }
#endregion

#region  Steps
        private void DrawWelcomeStep()
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();
            {
                GUILayout.Space(30);

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (_logoTexture != null)
                {
                    GUILayout.Label(_logoTexture, GUILayout.Width(175), GUILayout.Height(100));
                }
                else
                {
                    GUILayout.Label("Logo not found.", EditorStyles.centeredGreyMiniLabel);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUIStyle centeredStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    alignment = TextAnchor.MiddleCenter
                };
                GUILayout.Label("This is a project prototype pre-configurator to simplify development.", centeredStyle, GUILayout.Width(300));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                centeredStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.gray },
                    fontSize = 10
                };
                GUILayout.Label("v0.1a", centeredStyle, GUILayout.Width(300));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Start", GUILayout.Width(100), GUILayout.Height(30)))
            {
                _currentWizardStep = 1;

                this.minSize = new Vector2(600, 200);
                this.maxSize = new Vector2(600, 200);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(20);
            GUILayout.EndVertical();
        }
        
        private void DrawProjectInfoStep()
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.Space(98);
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16
            };
            GUILayout.Label("Base Settings", titleStyle, GUILayout.Width(300));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(GUILayout.Width(400));
            {
                _projectData.ProjectName = EditorGUILayout.TextField("Project Name", _projectData.ProjectName);
                _projectData.BundleId = EditorGUILayout.TextField("Bundle ID", _projectData.BundleId);
            }
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            
            GUILayout.BeginHorizontal();
            GUILayout.Space(7);

            if (GUILayout.Button("Back", GUILayout.Width(100), GUILayout.Height(30)))
            {
                _currentWizardStep = 0;

                this.minSize = new Vector2(600, 300);
                this.maxSize = new Vector2(600, 300);
            }
            GUILayout.Space(70);

            Rect progressRect = GUILayoutUtility.GetRect(200, 12);
            progressRect.y += 11;
            EditorGUI.DrawRect(progressRect, new Color(1,1,1,0.1f));
            float progress = _currentWizardStep / (float)TOTAL_WIZARD_STEPS;
            Rect fillRect = new Rect(progressRect.x, progressRect.y, progressRect.width * progress, progressRect.height);
            EditorGUI.DrawRect(fillRect, new Color(1,1,1,0.3f));
            GUIStyle progressStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                fontSize = 10
            };
            EditorGUI.LabelField(progressRect, $"{_currentWizardStep}/{TOTAL_WIZARD_STEPS}", progressStyle);

            GUILayout.Space(70);
            if (GUILayout.Button("Next", GUILayout.Width(100), GUILayout.Height(30)))
            {
                _currentWizardStep = 2;
            }
            GUILayout.Space(5);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.EndVertical();
        }
        
        private void DrawAnalyticsSettingsStep()
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16
            };
            GUILayout.Label("Analytics Settings", titleStyle, GUILayout.Width(300));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("AppMetrica", EditorStyles.boldLabel);
            _analyticsData.AppMetricaSDKKey = EditorGUILayout.TextField("SDK Key", _analyticsData.AppMetricaSDKKey);
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Facebook", EditorStyles.boldLabel);
            _analyticsData.FacebookAppId = EditorGUILayout.TextField("App Id", _analyticsData.FacebookAppId);
            _analyticsData.FacebookClientToken = EditorGUILayout.TextField("Client Token", _analyticsData.FacebookClientToken);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.Space(7);
            if (GUILayout.Button("Back", GUILayout.Width(100), GUILayout.Height(30)))
            {
                _currentWizardStep = 1;
            }
            GUILayout.Space(70);
            Rect progressRect = GUILayoutUtility.GetRect(200, 12);
            progressRect.y += 11;
            EditorGUI.DrawRect(progressRect, new Color(1,1,1,0.1f));
            float progress2 = _currentWizardStep / (float)TOTAL_WIZARD_STEPS;
            Rect fillRect2 = new Rect(progressRect.x, progressRect.y, progressRect.width * progress2, progressRect.height);
            EditorGUI.DrawRect(fillRect2, new Color(1,1,1,0.3f));
            GUIStyle progressStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                fontSize = 10
            };
            EditorGUI.LabelField(progressRect, $"{_currentWizardStep}/{TOTAL_WIZARD_STEPS}", progressStyle);
            GUILayout.Space(70);
            if (GUILayout.Button("Next", GUILayout.Width(100), GUILayout.Height(30)))
            {
                _currentWizardStep = 3;
            }
            GUILayout.Space(5);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.EndVertical();
        }

        private void DrawThirdPartyAssetsStep()
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Space(2);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(5);
                GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16
                };
                GUILayout.Label("Third-party Assets", titleStyle, GUILayout.Width(150));
                
                titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = Color.gray },
                    fontSize = 12
                };
                
                GUILayout.Label(" (will be installed automatically)", titleStyle, GUILayout.Width(435));

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                _thirdPartyScrollPos = EditorGUILayout.BeginScrollView(_thirdPartyScrollPos, false, true, GUILayout.Width(595), GUILayout.Height(120));
                
                string[] assets = new List<string>(THIRD_PARTY_ASSETS).OrderBy(x=>x).ToArray();
                foreach (string asset in assets)
                {
                    GUILayout.Label(asset);
                }
                EditorGUILayout.EndScrollView();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.Space(100);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.Space(7);
            if (GUILayout.Button("Back", GUILayout.Width(100), GUILayout.Height(30)))
            {
                _currentWizardStep = 2;
            }
            GUILayout.Space(70);
            Rect progRect = GUILayoutUtility.GetRect(200, 12);
            progRect.y += 11;
            EditorGUI.DrawRect(progRect, new Color(1,1,1,0.1f));
            float prog = _currentWizardStep / (float)TOTAL_WIZARD_STEPS;
            Rect fillProg = new Rect(progRect.x, progRect.y, progRect.width * prog, progRect.height);
            EditorGUI.DrawRect(fillProg, new Color(1,1,1,0.3f));
            GUIStyle progStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                fontSize = 10
            };
            EditorGUI.LabelField(progRect, $"{_currentWizardStep}/{TOTAL_WIZARD_STEPS}", progStyle);
            GUILayout.Space(70);
            if (GUILayout.Button("Run", GUILayout.Width(100), GUILayout.Height(30)))
            {
                Run();
                
                this.Close();
            }
            GUILayout.Space(5);
            GUILayout.EndHorizontal();

            GUILayout.Space(20);
            GUILayout.EndVertical();
        }
#endregion

#region Methods
        private  void Run()
        {
            Log("Running...");
            Log("Changing platform to Android...");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.Android);

            Log("Importing third-party assets...");
            //AssetDatabase.importPackageCompleted += OnAssetsImported;
            //AssetDatabase.ImportPackage("Assets/HookahSDK/Modules/ProjectSetupWizard/Editor/Packages/HookahSDK-InitPackage.unitypackage", false);
            Log("Setting up base project information...");
            PlayerSettings.companyName = "Hookah Games";
            PlayerSettings.productName = _projectData.ProjectName;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, _projectData.BundleId);
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
            
            Log("Setting up analytics keys...");
            ReplaceAppMetricaKey(_analyticsData.AppMetricaSDKKey);
            FacebookSettings.AppLabels[0] = _projectData.ProjectName;
            FacebookSettings.AppIds[0] = _analyticsData.FacebookAppId;
            FacebookSettings.ClientTokens[0] = _analyticsData.FacebookClientToken;
            FacebookSettings.SelectedAppIndex = 0;
            
            Log("Setting up adaptive game icons...");
            SetAdaptiveIcons();
            
            Log("Project setup successfully completed!");
        }

        /*private void OnAssetsImported(string packagename)
        {
            AssetDatabase.importPackageCompleted -= OnAssetsImported;
        }*/

        public static void ReplaceAppMetricaKey(string newValue)
        {
            string[] files = Directory.GetFiles(Application.dataPath, "AppMetricaActivator.cs",
                SearchOption.AllDirectories);

            if (files.Length == 0)
            {
                Log("File AppMetricaActivator.cs not found!");
                return;
            }

            string filePath = files[0];
            string fileContent = File.ReadAllText(filePath);

            if (fileContent.Contains("appmetrica_key"))
            {
                fileContent = fileContent.Replace("appmetrica_key", newValue);
                File.WriteAllText(filePath, fileContent);

                Log($"File {filePath} updated. New key is: {newValue}");

                AssetDatabase.Refresh();
            }
            else
            {
                Log("Line 'appmetrica_key' not found in file!");
            }
        }

        private void SetAdaptiveIcons()
        {
            string[] patches = new[]
            {
                "xxxhdpi",
                "xxhdpi",
                "xhdpi",
                "hdpi",
                "mdpi",
                "hdpi"
            };

            
            List<Texture2D[]> adaptivePictures = new List<Texture2D[]>();
            for (int i = 0; i < patches.Length; i++)
                adaptivePictures.Add(new Texture2D[]
                {
                    Resources.Load<Texture2D>($"Sprites/Promo/res/mipmap-{patches[i]}/ic_launcher_background"),
                    Resources.Load<Texture2D>($"Sprites/Promo/res/mipmap-{patches[i]}/ic_launcher_foreground")
                });

            List<Texture2D> legacyPictures = new List<Texture2D>();
            List<Texture2D> roundPictures = new List<Texture2D>();

            for (int i = 0; i < patches.Length; i++)
            {
                legacyPictures.Add(Resources.Load<Texture2D>($"Sprites/Promo/play_store_512"));
                roundPictures.Add(
                    Resources.Load<Texture2D>($"Sprites/Promo/res/mipmap-{patches[i]}/ic_launcher_foreground"));
            }

            bool missingAdaptive = adaptivePictures.Any(arr => arr[0] == null || arr[1] == null);
            bool missingLegacy = legacyPictures.Any(tex => tex == null);
            bool missingRound = roundPictures.Any(tex => tex == null);

            if (missingAdaptive || missingLegacy || missingRound)
                return;

            NamedBuildTarget platform = NamedBuildTarget.Android;
            PlatformIconKind kind = AndroidPlatformIconKind.Adaptive;
            PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(platform, kind);
            for (int i = 0; i < icons.Length; i++)
            {
                icons[i].SetTextures(adaptivePictures[i]);
            }

            PlayerSettings.SetPlatformIcons(platform, kind, icons);

            kind = AndroidPlatformIconKind.Legacy;
            icons = PlayerSettings.GetPlatformIcons(platform, kind);
            for (int i = 0; i < icons.Length; i++)
                icons[i].SetTextures(new Texture2D[] { legacyPictures[i] });

            PlayerSettings.SetPlatformIcons(platform, kind, icons);

            kind = AndroidPlatformIconKind.Round;
            icons = PlayerSettings.GetPlatformIcons(platform, kind);
            for (int i = 0; i < icons.Length; i++)
            {
                icons[i].SetTextures(new Texture2D[] { roundPictures[i] });
            }

            PlayerSettings.SetPlatformIcons(platform, kind, icons);

            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new Texture2D[] { legacyPictures[0] });
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, new Texture2D[] { legacyPictures[0] });
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new Texture2D[] { legacyPictures[0] });
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS, new Texture2D[] { legacyPictures[0] });
            
            AssetDatabase.SaveAssets();
        }

        #endregion

        #region  Tools
        private static void Log(string message)
        {
            Debug.Log($"<color=#cc66ff>Hookah (Setup Wizard):</color> {message}");
        }
        #endregion
        
        #region  Classes
        private class ProjectData
        {
            public string ProjectName = "";
            public string BundleId = "";
        }
        
        private class AnalyticsSettings
        {
            public string AppMetricaSDKKey = "";
            public string FacebookAppId = "";
            public string FacebookClientToken = "";
        }
        #endregion
    }
}
