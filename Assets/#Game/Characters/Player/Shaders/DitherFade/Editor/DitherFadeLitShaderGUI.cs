using System;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;

namespace SummerGame.Editor
{
    /// <summary>
    /// URP Lit's normal material UI and keyword management, with the two player
    /// fade controls added to Surface Inputs.
    /// </summary>
    internal sealed class DitherFadeLitShaderGUI : BaseShaderGUI
    {
        private static readonly string[] WorkflowModeNames =
            Enum.GetNames(typeof(LitGUI.WorkflowMode));

        private LitGUI.LitProperties litProperties;
        private MaterialProperty fadeProperty;
        private MaterialProperty ditherCellSizeProperty;

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            litProperties = new LitGUI.LitProperties(properties);
            fadeProperty = FindProperty("_Fade", properties);
            ditherCellSizeProperty = FindProperty("_DitherCellSize", properties);
        }

        public override void ValidateMaterial(Material material)
        {
            SetMaterialKeywords(material, LitGUI.SetMaterialKeywords);
        }

        public override void DrawSurfaceOptions(Material material)
        {
            EditorGUIUtility.labelWidth = 0f;
            if (litProperties.workflowMode != null)
                DoPopup(LitGUI.Styles.workflowModeText, litProperties.workflowMode, WorkflowModeNames);

            base.DrawSurfaceOptions(material);
        }

        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);

            EditorGUILayout.Space();
            materialEditor.ShaderProperty(fadeProperty, fadeProperty.displayName);
            materialEditor.ShaderProperty(ditherCellSizeProperty, ditherCellSizeProperty.displayName);
            EditorGUILayout.Space();

            LitGUI.Inputs(litProperties, materialEditor, material);
            DrawEmissionProperties(material, true);
            DrawTileOffset(materialEditor, baseMapProp);
        }

        public override void DrawAdvancedOptions(Material material)
        {
            if (litProperties.reflections != null && litProperties.highlights != null)
            {
                materialEditor.ShaderProperty(litProperties.highlights, LitGUI.Styles.highlightsText);
                materialEditor.ShaderProperty(litProperties.reflections, LitGUI.Styles.reflectionsText);
            }

            base.DrawAdvancedOptions(material);
        }
    }
}
