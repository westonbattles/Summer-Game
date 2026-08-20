using System.IO;
using UnityEditor;
using UnityEngine;

public static class MazeRoomPrefabCreator
{
    [MenuItem("Assets/Create/Maze Generation/Room Prefab")]
    static void CreateRoomPrefab()
    {
        string folder = GetSelectedFolder();
        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/New Maze Room.prefab");

        GameObject room = new GameObject("New Maze Room");

        try
        {
            room.AddComponent<MazeRoom>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(room, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }
        finally
        {
            Object.DestroyImmediate(room);
        }
    }

    static string GetSelectedFolder()
    {
        Object selected = Selection.activeObject;
        if (selected == null) return "Assets";

        string path = AssetDatabase.GetAssetPath(selected);
        if (!AssetDatabase.IsValidFolder(path)) path = Path.GetDirectoryName(path)?.Replace('\\', '/');

        return AssetDatabase.IsValidFolder(path) ? path : "Assets";
    }
}
