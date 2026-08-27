using UnityEditor;

public static class CreateItemBehaviorScript
{
    private const string TemplatePath = "Assets/Scripts/Player/Items/Editor/ItemBehaviorTemplate.cs.txt";

    [MenuItem("Assets/Create/Scripting/Item Behavior Script", false, 80)]
    public static void CreateScript()
    {
        ProjectWindowUtil.CreateScriptAssetFromTemplateFile(
            TemplatePath,
            "NewItemBehavior.cs"
        );
    }
}