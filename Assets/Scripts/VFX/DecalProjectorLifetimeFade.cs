using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class DecalProjectorLifetimeFade : MonoBehaviour
{
    [SerializeField] DecalProjector projector;

    [Header("Timing")]
    [SerializeField, Min(0f)] float delayBeforeFade = 1f;
    [SerializeField, Min(0.001f)] float fadeDuration = 1f;
    [SerializeField] AnimationCurve fadeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Shader Graph")]
    [SerializeField] bool driveShaderFadeAmount = true;
    [SerializeField] string fadePropertyName = "_FadeAmount";

    [Header("Decal Projector")]
    [SerializeField] bool driveProjectorFadeFactor;
    [SerializeField] bool invertProjectorFadeFactor = true;

    Material sourceMaterial;
    Material runtimeMaterial;
    int fadePropertyId;
    float elapsed;

    void Awake()
    {
        CacheReferences();
        CreateRuntimeMaterial();
    }

    void OnEnable()
    {
        elapsed = 0f;
        CacheReferences();
        CreateRuntimeMaterial();
        ApplyFade(0f);
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        float normalizedFade = Mathf.InverseLerp(delayBeforeFade, delayBeforeFade + fadeDuration, elapsed);
        ApplyFade(fadeCurve.Evaluate(normalizedFade));
    }

    void OnDisable()
    {
        elapsed = 0f;
        ApplyFade(0f);
    }

    void OnDestroy()
    {
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
    }

    void OnValidate()
    {
        if (projector == null) projector = GetComponent<DecalProjector>();
        fadePropertyId = Shader.PropertyToID(fadePropertyName);
    }

    void CacheReferences()
    {
        if (projector == null) projector = GetComponent<DecalProjector>();
        ResolveFadePropertyName(projector != null ? projector.material : null);
    }

    void CreateRuntimeMaterial()
    {
        if (projector == null || !Application.isPlaying) return;

        if (sourceMaterial == null) sourceMaterial = projector.material;
        if (sourceMaterial == null || runtimeMaterial != null) return;

        runtimeMaterial = new Material(sourceMaterial);
        projector.material = runtimeMaterial;
        ResolveFadePropertyName(runtimeMaterial);
    }

    void ApplyFade(float fade)
    {
        if (projector == null) return;

        if (driveShaderFadeAmount)
        {
            Material targetMaterial = runtimeMaterial != null ? runtimeMaterial : projector.material;
            if (targetMaterial != null) targetMaterial.SetFloat(fadePropertyId, fade);
        }

        if (driveProjectorFadeFactor)
        {
            projector.fadeFactor = invertProjectorFadeFactor ? 1f - fade : fade;
        }
    }

    void ResolveFadePropertyName(Material targetMaterial)
    {
        fadePropertyId = Shader.PropertyToID(fadePropertyName);

        if (targetMaterial == null || targetMaterial.HasProperty(fadePropertyId)) return;

        if (!fadePropertyName.StartsWith("_"))
        {
            string underscoredName = "_" + fadePropertyName;
            int underscoredId = Shader.PropertyToID(underscoredName);
            if (targetMaterial.HasProperty(underscoredId))
            {
                fadePropertyId = underscoredId;
            }
        }
    }
}
