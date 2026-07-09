using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GroundRenderingLayerUtility
{
    const string GameObjectLayerName = "Ground";
    const string RenderingLayerName = "Ground";

    [MenuItem("Tools/VFX/Sync Ground Rendering Layer From GameObject Layer")]
    static void SyncGroundRenderingLayer()
    {
        int groundGameObjectLayer = LayerMask.NameToLayer(GameObjectLayerName);
        if (groundGameObjectLayer < 0)
        {
            EditorUtility.DisplayDialog(
                "Missing GameObject Layer",
                $"Create a GameObject layer named '{GameObjectLayerName}' first.",
                "OK");
            return;
        }

        if (!TryGetRenderingLayerValue(RenderingLayerName, out uint groundRenderingLayer))
        {
            EditorUtility.DisplayDialog(
                "Missing Rendering Layer",
                $"Create a Rendering Layer named '{RenderingLayerName}' in Project Settings > Tags and Layers first.",
                "OK");
            return;
        }

        int matchedRenderers = 0;
        int updatedRenderers = 0;
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Renderer renderer in renderers)
        {
            if (renderer.gameObject.layer != groundGameObjectLayer) continue;

            matchedRenderers++;
            uint newMask = renderer.renderingLayerMask | groundRenderingLayer;
            if (renderer.renderingLayerMask == newMask) continue;

            Undo.RecordObject(renderer, "Sync Ground Rendering Layer");
            renderer.renderingLayerMask = newMask;
            EditorUtility.SetDirty(renderer);
            updatedRenderers++;
        }

        int matchedTerrains = 0;
        int updatedTerrains = 0;
        Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Terrain terrain in terrains)
        {
            if (terrain.gameObject.layer != groundGameObjectLayer) continue;

            matchedTerrains++;
            uint newMask = terrain.renderingLayerMask | groundRenderingLayer;
            if (terrain.renderingLayerMask == newMask) continue;

            Undo.RecordObject(terrain, "Sync Ground Rendering Layer");
            terrain.renderingLayerMask = newMask;
            EditorUtility.SetDirty(terrain);
            updatedTerrains++;
        }

        MarkLoadedScenesDirty();

        Debug.Log(
            $"Ground rendering layer sync complete. " +
            $"Renderers matched: {matchedRenderers}, updated: {updatedRenderers}. " +
            $"Terrains matched: {matchedTerrains}, updated: {updatedTerrains}.");
    }

    static bool TryGetRenderingLayerValue(string renderingLayerName, out uint value)
    {
        string[] names = RenderingLayerMask.GetDefinedRenderingLayerNames();
        int[] values = RenderingLayerMask.GetDefinedRenderingLayerValues();

        for (int i = 0; i < names.Length && i < values.Length; i++)
        {
            if (names[i] != renderingLayerName) continue;

            value = unchecked((uint)values[i]);
            return true;
        }

        value = 0;
        return false;
    }

    static void MarkLoadedScenesDirty()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded) EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
