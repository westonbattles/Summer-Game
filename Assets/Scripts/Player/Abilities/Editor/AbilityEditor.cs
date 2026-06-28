using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Ability))]
public sealed class AbilityEditor : Editor
{
    SerializedProperty _behaviourProperty;

    Type[] _behaviourTypes;
    string[] _behaviourNames;

    void OnEnable()
    {
        _behaviourProperty = serializedObject.FindProperty("behaviour");
        
        // save all inherited types from AbilityBehaviour into _behaviourTypes
        _behaviourTypes = TypeCache
            .GetTypesDerivedFrom<AbilityBehaviour>()
            .Where(type => !type.IsAbstract && !type.IsGenericType)
            .OrderBy(type => type.Name)
            .ToArray();
        
        // build our dropdown list (0 being none selected)
        _behaviourNames = new string[_behaviourTypes.Length + 1];
        _behaviourNames[0] = "None";

        for (int i = 0; i < _behaviourTypes.Length; i++)
        {
            _behaviourNames[i + 1] = ObjectNames.NicifyVariableName(_behaviourTypes[i].Name);
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "behaviour");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Behaviour", EditorStyles.boldLabel);

        int currentIndex = GetCurrentBehaviourIndex();
        int selectedIndex = EditorGUILayout.Popup(
            "Type",
            currentIndex,
            _behaviourNames);

        if (selectedIndex != currentIndex)
        {
            SetBehaviour(selectedIndex);
            return;
        }
        
        // add a settings panel with our behaviour settings
        if (_behaviourProperty.managedReferenceValue != null)
        {
            EditorGUILayout.Space();
            _behaviourProperty.isExpanded = true;

            EditorGUILayout.PropertyField(
                _behaviourProperty,
                new GUIContent("Settings"),
                true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    int GetCurrentBehaviourIndex()
    {
        object currentBehaviour = _behaviourProperty.managedReferenceValue;

        if (currentBehaviour == null)
        {
            return 0;
        }

        Type currentType = currentBehaviour.GetType();

        for (int i = 0; i < _behaviourTypes.Length; i++)
        {
            if (_behaviourTypes[i] == currentType)
            {
                return i + 1;
            }
        }

        return 0;
    }

    void SetBehaviour(int selectedIndex)
    {
        Undo.RecordObject(target, "Change Ability Behaviour");

        if (selectedIndex == 0)
        {
            _behaviourProperty.managedReferenceValue = null;
        }
        else
        {
            Type selectedType = _behaviourTypes[selectedIndex - 1];
            _behaviourProperty.managedReferenceValue = Activator.CreateInstance(selectedType);
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }
}
