using UnityEditor;

public static class CreateStatusEffectBehaviorScript
{
    private const string TemplatePath =
        "Assets/Scripts/RepurposeableComponents/StatusEffects/Editor/StatusEffectBehaviorTemplate.cs.txt";

    [MenuItem("Assets/Create/Scripting/Status Effect Behavior Script", false, 81)]
    public static void CreateScript()
    {
        ProjectWindowUtil.CreateScriptAssetFromTemplateFile(
            TemplatePath,
            "NewStatusEffectBehavior.cs"
        );
    }
}