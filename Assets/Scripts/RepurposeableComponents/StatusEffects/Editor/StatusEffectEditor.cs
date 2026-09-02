using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StatusEffectDefinition))]
public class StatusEffectEditor : Editor
{
    Type[] _behaviorTypes;
    SerializedProperty _behaviorProperty;
    string[] _behaviorNames;

    void OnEnable()
    {
        _behaviorProperty = serializedObject.FindProperty("behavior");

        _behaviorTypes = TypeCache
            .GetTypesDerivedFrom<StatusEffectBehavior>()
            .Where(type => !type.IsAbstract && !type.IsGenericType)
            .OrderBy(type => type.Name)
            .ToArray();

        _behaviorNames = new string[_behaviorTypes.Length + 1];
        _behaviorNames[0] = "None";

        for (int i = 0; i < _behaviorTypes.Length; i++)
            _behaviorNames[i + 1] = ObjectNames.NicifyVariableName(_behaviorTypes[i].Name);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "m_Script", "behavior");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Behavior", EditorStyles.boldLabel);

        int currentIndex = GetCurrentBehaviorIndex();
        int selectedIndex = EditorGUILayout.Popup("Type", currentIndex, _behaviorNames);

        if (selectedIndex != currentIndex)
        {
            SetBehavior(selectedIndex);
            return;
        }

        if (_behaviorProperty.managedReferenceValue != null)
        {
            EditorGUILayout.Space();
            _behaviorProperty.isExpanded = true;

            EditorGUILayout.PropertyField(
                _behaviorProperty,
                new GUIContent("Settings"),
                true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    int GetCurrentBehaviorIndex()
    {
        object currentBehavior = _behaviorProperty.managedReferenceValue;
        if (currentBehavior == null) return 0;

        Type currentType = currentBehavior.GetType();
        for (int i = 0; i < _behaviorTypes.Length; i++)
        {
            if (_behaviorTypes[i] == currentType) return i + 1;
        }

        return 0;
    }

    void SetBehavior(int selectedIndex)
    {
        Undo.RecordObject(target, "Change Status Effect Behavior");

        if (selectedIndex == 0)
        {
            _behaviorProperty.managedReferenceValue = null;
        }
        else
        {
            Type selectedType = _behaviorTypes[selectedIndex - 1];
            _behaviorProperty.managedReferenceValue = Activator.CreateInstance(selectedType);
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }
}