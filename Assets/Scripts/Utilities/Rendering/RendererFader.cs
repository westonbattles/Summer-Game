using UnityEngine;

public class RendererFader
{
    static readonly int FadeId = Shader.PropertyToID("_Fade");
    static readonly int OutlineFadeId = Shader.PropertyToID("_PlayerOutlineFade");
    readonly Renderer[] renderers;
    readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

    public RendererFader(GameObject root)
    {
        renderers = root.GetComponentsInChildren<Renderer>(true);
        Shader.SetGlobalFloat(OutlineFadeId, 1f);
    }

    public void SetFade(float fade)
    {
        // might be implicitly done
        fade = Mathf.Clamp01(fade);
        Shader.SetGlobalFloat(OutlineFadeId, fade);
        
        foreach (Renderer renderer in renderers)
        {
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(FadeId, fade);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

}
