
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MazeGenerationSettings))]
public class MazeGenerationSettingsEditor : Editor
{
    static readonly string[] SearchFolders = {"Assets/#Game/Maze/Rooms"};
    static readonly string[] IgnoreFolders =
    {
        //"Assets/#Game/Maze/Rooms/#Templates"
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;

        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (property.name == nameof(MazeGenerationSettings.Rooms))
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Room/Connector Catalogs",EditorStyles.boldLabel);
                
                // button styling
                GUI.contentColor = Color.darkSeaGreen;
                GUIStyle style = new GUIStyle(GUI.skin.button);
                style.fontStyle = FontStyle.BoldAndItalic;
                
                // rooms array is the property
                if (GUILayout.Button("Rebuild room prefab list from assets folder", style)) PopulateRooms(property); 
                
                GUI.contentColor = Color.white;
                
            }
            
            using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"))
            {
                EditorGUILayout.PropertyField(property, includeChildren: true);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    static void PopulateRooms(SerializedProperty roomsProperty)
    {
        MazeRoom[] rooms = AssetDatabase.FindAssets("t:Prefab", SearchFolders)
            .Select(AssetDatabase.GUIDToAssetPath)                          // turns found assets into an enumerator of assetpaths
            .Where(path => !IgnoreFolders.Any(                               // https://learn.microsoft.com/en-us/dotnet/api/system.globalization.compareinfo.isprefix?view=net-10.0
                ignorePath => CultureInfo.InvariantCulture.CompareInfo.IsPrefix(
                path,ignorePath.TrimEnd('/')+"/", CompareOptions.OrdinalIgnoreCase)))
            .Select(AssetDatabase.LoadAssetAtPath<GameObject>)              // turns that enumarator into the gameobjects found at the asset paths
            .Where(prefab => prefab != null)                   // ensures each prefab gameobject is not null
            .Select(prefab => prefab.GetComponent<MazeRoom>())    // turns the enumarator into one of MazeRoom components of each element
            .Where(room => room != null)                                  // Only use the elements that succesfully were able to get a MazeRoom element 
            .OrderBy(room => AssetDatabase.GetAssetPath(room))               // order by asset path so indiviudally foldered rooms get grouped
            .ToArray();

        roomsProperty.arraySize = rooms.Length;

        for (int i = 0; i < rooms.Length; i++) roomsProperty.GetArrayElementAtIndex(i).objectReferenceValue = rooms[i];
        roomsProperty.serializedObject.ApplyModifiedProperties();
    }
}