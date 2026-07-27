using System;
using UnityEditor;

public class ItemModelImportProcessor : AssetPostprocessor
{
    const string ItemAssetRoot = "Assets/#Game/Items/";

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(ItemAssetRoot, StringComparison.Ordinal)) return;

        // World-item outline meshes weld hard-edge normals at runtime. Keeping
        // item model vertex data readable makes that possible in player builds.
        ((ModelImporter)assetImporter).isReadable = true;
    }
}
