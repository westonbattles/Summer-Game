using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemDefinition))]
public class ItemEditor : Editor
{
    
    Type[] _behaviourTypes;
    SerializedProperty _behaviourProperty;
    string[] _behaviourNames;
    
    
    void OnEnable()
    {
        _behaviourProperty = serializedObject.FindProperty("behaviour");

        // save list of all itembehaviour subclasses thru typecache and sort them by Type.name
        _behaviourTypes = TypeCache
            .GetTypesDerivedFrom<ItemBehaviour>()
            .Where(type => !type.IsAbstract && !type.IsGenericType)
            .OrderBy(type => type.Name)
            .ToArray();
        
        // build our dropdown list (0 being none selected)
        _behaviourNames = new string[_behaviourTypes.Length + 1];
        _behaviourNames[0] = "None";

        // populate name variables
        for (int i = 0; i < _behaviourTypes.Length; i++) _behaviourNames[i+1] = ObjectNames.NicifyVariableName(_behaviourTypes[i].Name);
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
        Undo.RecordObject(target, "Change Item Behaviour");

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
