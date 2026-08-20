using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using UnityEditor;
using UnityEngine;

public class EnemyLevelStatsConfigImporterWindow : EditorWindow
{
    private const int MaxPatternRows = 10;
    private const string DefaultConfigPath = "Assets/Configs/Levels/EnemyLevelStatsConfig.asset";

    private EnemyLevelStatsConfig _targetConfig;
    private string _googleSheetUrl = string.Empty;

    [MenuItem("Tools/Enemy Level Stats Importer")]
    [MenuItem("Tools/Parse Enemy Level Stats")]
    private static void Open()
    {
        GetWindow<EnemyLevelStatsConfigImporterWindow>("Enemy Stats Importer");
    }

    private void OnEnable()
    {
        if (_targetConfig == null)
            _targetConfig = AssetDatabase.LoadAssetAtPath<EnemyLevelStatsConfig>(DefaultConfigPath);
    }

    private void OnGUI()
    {
        _targetConfig = (EnemyLevelStatsConfig)EditorGUILayout.ObjectField("Target Config", _targetConfig, typeof(EnemyLevelStatsConfig), false);
        _googleSheetUrl = EditorGUILayout.TextField("Google Sheet CSV URL", _googleSheetUrl);

        string importUrl = GetImportUrl();
        EditorGUI.BeginDisabledGroup(_targetConfig == null || string.IsNullOrWhiteSpace(importUrl));

        if (GUILayout.Button("Import"))
            Import();

        EditorGUI.EndDisabledGroup();
    }

    private void Import()
    {
        string importUrl = GetImportUrl();
        string csv;

        using (WebClient client = new WebClient())
            csv = client.DownloadString(importUrl);

        EnemyLevelStatsEntry[] levels = Parse(csv);
        SerializedObject serializedConfig = new SerializedObject(_targetConfig);
        SerializedProperty urlProperty = serializedConfig.FindProperty("_googleSheetUrl");
        SerializedProperty levelsProperty = serializedConfig.FindProperty("_levels");

        if (urlProperty != null && string.IsNullOrWhiteSpace(_googleSheetUrl) == false)
            urlProperty.stringValue = _googleSheetUrl;

        levelsProperty.arraySize = levels.Length;

        for (int i = 0; i < levels.Length; i++)
            WriteLevel(levelsProperty.GetArrayElementAtIndex(i), levels[i]);

        serializedConfig.ApplyModifiedProperties();
        EditorUtility.SetDirty(_targetConfig);
        AssetDatabase.SaveAssets();
    }

    private string GetImportUrl()
    {
        if (string.IsNullOrWhiteSpace(_googleSheetUrl) == false)
            return _googleSheetUrl;

        return _targetConfig != null ? _targetConfig.GoogleSheetUrl : string.Empty;
    }

    private static EnemyLevelStatsEntry[] Parse(string csv)
    {
        string[] lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        List<EnemyLevelStatsEntry> levels = new List<EnemyLevelStatsEntry>();
        char delimiter = DetectDelimiter(lines);
        int startLineIndex = HasHeader(lines, delimiter) ? 1 : 0;

        for (int i = startLineIndex; i < lines.Length && levels.Count < MaxPatternRows; i++)
        {
            string[] cells = SplitCsvLine(lines[i], delimiter);

            if (cells.Length < 3)
                continue;

            int levelIndex = ParseInt(cells[0]);
            List<float> multipliers = new List<float>();
            string scheme = string.Empty;
            float damageReward = 0f;

            for (int cellIndex = 1; cellIndex < cells.Length; cellIndex++)
            {
                string cell = cells[cellIndex].Trim();

                if (string.IsNullOrWhiteSpace(cell))
                    continue;

                if (IsScheme(cell))
                {
                    scheme = cell;

                    if (cellIndex + 1 < cells.Length)
                        damageReward = ParseFloat(cells[cellIndex + 1]);

                    break;
                }

                if (TryParseFloat(cell, out float multiplier))
                    multipliers.Add(multiplier);
            }

            levels.Add(new EnemyLevelStatsEntry(levelIndex, multipliers.ToArray(), scheme, damageReward));
        }

        return levels.ToArray();
    }

    private static char DetectDelimiter(string[] lines)
    {
        if (lines == null || lines.Length == 0)
            return ',';

        string line = lines[0];
        int tabCount = Count(line, '\t');
        int semicolonCount = Count(line, ';');
        int commaCount = Count(line, ',');

        if (tabCount >= semicolonCount && tabCount >= commaCount && tabCount > 0)
            return '\t';

        if (semicolonCount >= commaCount && semicolonCount > 0)
            return ';';

        return ',';
    }

    private static int Count(string value, char symbol)
    {
        int count = 0;

        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == symbol)
                count++;
        }

        return count;
    }

    private static bool HasHeader(string[] lines, char delimiter)
    {
        if (lines == null || lines.Length == 0)
            return false;

        string[] cells = SplitCsvLine(lines[0], delimiter);

        if (cells.Length == 0)
            return false;

        return int.TryParse(cells[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out _) == false;
    }

    private static string[] SplitCsvLine(string line, char delimiter)
    {
        List<string> cells = new List<string>();
        bool insideQuotes = false;
        int startIndex = 0;

        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '"')
                insideQuotes = insideQuotes == false;

            if (line[i] != delimiter || insideQuotes)
                continue;

            cells.Add(Unquote(line.Substring(startIndex, i - startIndex)));
            startIndex = i + 1;
        }

        cells.Add(Unquote(line.Substring(startIndex)));
        return cells.ToArray();
    }

    private static string Unquote(string value)
    {
        value = value.Trim();

        if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
            value = value.Substring(1, value.Length - 2);

        return value.Replace("\"\"", "\"");
    }

    private static bool IsScheme(string value)
    {
        if (value.Contains("-") == false)
            return false;

        string[] parts = value.Split('-');

        if (parts.Length <= 1)
            return false;

        for (int i = 0; i < parts.Length; i++)
        {
            if (int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out _) == false)
                return false;
        }

        return true;
    }

    private static int ParseInt(string value)
    {
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result);
        return result;
    }

    private static float ParseFloat(string value)
    {
        TryParseFloat(value, out float result);
        return result;
    }

    private static bool TryParseFloat(string value, out float result)
    {
        value = value.Trim().Replace(",", ".");
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }

    private static void WriteLevel(SerializedProperty property, EnemyLevelStatsEntry level)
    {
        property.FindPropertyRelative("_levelIndex").intValue = level.LevelIndex;
        property.FindPropertyRelative("_scheme").stringValue = level.Scheme;
        property.FindPropertyRelative("_damageReward").floatValue = level.DamageReward;

        SerializedProperty multipliersProperty = property.FindPropertyRelative("_healthMultipliers");
        float[] multipliers = level.HealthMultipliers;
        int multiplierCount = multipliers != null ? multipliers.Length : 0;
        multipliersProperty.arraySize = multiplierCount;

        for (int i = 0; i < multiplierCount; i++)
            multipliersProperty.GetArrayElementAtIndex(i).floatValue = multipliers[i];
    }
}
