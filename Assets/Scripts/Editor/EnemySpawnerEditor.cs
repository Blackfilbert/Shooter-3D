using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemySpawner))]
public class EnemySpawnerEditor : Editor
{
    private SerializedProperty _enemyPrefab;
    private SerializedProperty _levelStatsConfig;
    private SerializedProperty _levelIndexOverride;
    private SerializedProperty _defaultBossHpMultiplier;
    private SerializedProperty _defaultBossScaleMultiplier;
    private SerializedProperty _spawnPoints;

    private void OnEnable()
    {
        _enemyPrefab = serializedObject.FindProperty("_enemyPrefab");
        _levelStatsConfig = serializedObject.FindProperty("_levelStatsConfig");
        _levelIndexOverride = serializedObject.FindProperty("_levelIndexOverride");
        _defaultBossHpMultiplier = serializedObject.FindProperty("_defaultBossHpMultiplier");
        _defaultBossScaleMultiplier = serializedObject.FindProperty("_defaultBossScaleMultiplier");
        _spawnPoints = serializedObject.FindProperty("_spawnPoints");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_enemyPrefab);
        EditorGUILayout.PropertyField(_levelStatsConfig);
        EditorGUILayout.PropertyField(_levelIndexOverride);
        EnsurePositiveDefault(_defaultBossHpMultiplier);
        EnsurePositiveDefault(_defaultBossScaleMultiplier);
        EditorGUILayout.PropertyField(_defaultBossHpMultiplier);
        EditorGUILayout.PropertyField(_defaultBossScaleMultiplier);
        DrawSpawnPoints();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSpawnPoints()
    {
        EditorGUILayout.PropertyField(_spawnPoints, false);

        if (_spawnPoints.isExpanded == false)
            return;

        EditorGUI.indentLevel++;
        _spawnPoints.arraySize = Mathf.Max(0, EditorGUILayout.IntField("Size", _spawnPoints.arraySize));

        for (int i = 0; i < _spawnPoints.arraySize; i++)
            DrawSpawnPoint(_spawnPoints.GetArrayElementAtIndex(i), i);

        EditorGUI.indentLevel--;
    }

    private void DrawSpawnPoint(SerializedProperty spawnPoint, int index)
    {
        SerializedProperty point = spawnPoint.FindPropertyRelative("_point");
        SerializedProperty enemyPrefab = spawnPoint.FindPropertyRelative("_enemyPrefab");
        SerializedProperty behaviorType = spawnPoint.FindPropertyRelative("_behaviorType");
        SerializedProperty walkRadius = spawnPoint.FindPropertyRelative("_walkRadius");
        SerializedProperty walkSpeed = spawnPoint.FindPropertyRelative("_walkSpeed");
        SerializedProperty moveTarget = spawnPoint.FindPropertyRelative("_moveTarget");
        SerializedProperty moveTriggerEnemyCount = spawnPoint.FindPropertyRelative("_moveTriggerEnemyCount");
        SerializedProperty moveToPointSpeed = spawnPoint.FindPropertyRelative("_moveToPointSpeed");
        SerializedProperty health = spawnPoint.FindPropertyRelative("_health");
        SerializedProperty killBonusType = spawnPoint.FindPropertyRelative("_killBonusType");
        SerializedProperty killBonusAmount = spawnPoint.FindPropertyRelative("_killBonusAmount");
        SerializedProperty killBonusDamageType = spawnPoint.FindPropertyRelative("_killBonusDamageType");
        SerializedProperty isBoss = spawnPoint.FindPropertyRelative("_isBoss");
        SerializedProperty bossHpMultiplier = spawnPoint.FindPropertyRelative("_bossHpMultiplier");
        SerializedProperty bossScaleMultiplier = spawnPoint.FindPropertyRelative("_bossScaleMultiplier");

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Element {index}", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(enemyPrefab);
        EditorGUILayout.PropertyField(point);
        EditorGUILayout.PropertyField(behaviorType);

        if ((EnemyBehaviorType)behaviorType.enumValueIndex == EnemyBehaviorType.Walk)
        {
            EditorGUILayout.PropertyField(walkRadius);
            EditorGUILayout.PropertyField(walkSpeed);
        }
        else if ((EnemyBehaviorType)behaviorType.enumValueIndex == EnemyBehaviorType.MoveToPointOnEnemyCount)
        {
            EditorGUILayout.PropertyField(moveTarget);
            EditorGUILayout.PropertyField(moveTriggerEnemyCount);
            EditorGUILayout.PropertyField(moveToPointSpeed);
        }

        EditorGUILayout.PropertyField(health);
        EditorGUILayout.PropertyField(killBonusType);

        if ((EnemyKillBonusType)killBonusType.enumValueIndex == EnemyKillBonusType.ChangeDamageType)
            EditorGUILayout.PropertyField(killBonusDamageType);
        else
            EditorGUILayout.PropertyField(killBonusAmount);

        EditorGUILayout.PropertyField(isBoss);

        if (isBoss.boolValue)
        {
            EnsurePositiveDefault(bossHpMultiplier);
            EnsurePositiveDefault(bossScaleMultiplier);
            EditorGUILayout.PropertyField(bossHpMultiplier);
            EditorGUILayout.PropertyField(bossScaleMultiplier);
        }

        EditorGUILayout.EndVertical();
    }

    private void EnsurePositiveDefault(SerializedProperty property)
    {
        if (property == null || property.floatValue > 0f)
            return;

        property.floatValue = 1f;
    }
}
