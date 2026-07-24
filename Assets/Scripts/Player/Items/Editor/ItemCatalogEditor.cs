using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ItemCatalog))]
public class ItemCatalogEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.wordWrap = true;
        labelStyle.fontStyle = FontStyle.Italic;
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Auto-upddates the catalog asset to include all ItemDefinitions assets in Asset/#Game/Items", labelStyle);
        
        ItemCatalog catalogAsset = (ItemCatalog)target; // cast the inspected itemcatalog asset and store ref to it
        if (GUILayout.Button("Refresh Item Catalog From  Assets"))
        {
            catalogAsset.UpdateItemCatalogFromAssets();
            Debug.Log("Refreshed item assets, catalog is good to go now yeaayeeesssss bangg");
            
            // bullshit asset saving (just in case)
            AssetDatabase.SaveAssetIfDirty(catalogAsset);
            serializedObject.Update();
        }

        EditorGUILayout.Space(30);
        EditorGUILayout.LabelField("(Private definitions, non-modifiable)", labelStyle);
        SerializedProperty definitionsProperty = serializedObject.FindProperty("definitions");
        EditorGUILayout.PropertyField(definitionsProperty, new GUIContent("Item Definitions"), true);


    }
}