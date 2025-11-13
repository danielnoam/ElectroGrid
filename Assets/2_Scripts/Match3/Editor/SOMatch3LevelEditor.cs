#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SOMatch3Level))]
public class SOMatch3LevelEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SOMatch3Level level = (SOMatch3Level)target;
        
        // Draw all fields - objectives will handle their own custom drawing
        SerializedProperty levelNameProp = serializedObject.FindProperty("levelName");
        SerializedProperty gridShapeProp = serializedObject.FindProperty("gridShape");
        SerializedProperty matchObjectsProp = serializedObject.FindProperty("matchObjects");
        SerializedProperty objectivesProp = serializedObject.FindProperty("objectives");
        SerializedProperty loseConditionsProp = serializedObject.FindProperty("loseConditions");

        EditorGUILayout.PropertyField(levelNameProp);
        EditorGUILayout.PropertyField(gridShapeProp);
        EditorGUILayout.PropertyField(matchObjectsProp);
        EditorGUILayout.PropertyField(objectivesProp);
        EditorGUILayout.PropertyField(loseConditionsProp);

        serializedObject.ApplyModifiedProperties();
    }
}

#endif