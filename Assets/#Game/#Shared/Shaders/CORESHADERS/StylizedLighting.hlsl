#ifndef STYLIZED_LIGHTING_INCLUDED
#define STYLIZED_LIGHTING_INCLUDED

// Core lighting model for all Stylized* shader graphs.
// Custom Function node (File mode), function name: StylizedLighting
//
// Required Boolean Keywords on the graph (Global, Multi Compile, exact refs):
//   _MAIN_LIGHT_SHADOWS
//   _MAIN_LIGHT_SHADOWS_CASCADE
//   _SHADOWS_SOFT
//   _ADDITIONAL_LIGHTS            (Forward path light data)
//   _CLUSTER_LIGHT_LOOP           (Forward+ path, Unity 6.1+ name;
//                                  was _FORWARD_PLUS in Unity 6.0)
//   _ADDITIONAL_LIGHT_SHADOWS     (shadows cast BY point/spot lights)
//
// Design notes:
// - Each additional light's NdotL goes through the same smoothstep ramp as
//   the main light, so all lights speak the same visual language. Distance
//   attenuation stays smooth (outside the ramp) to avoid banding rings
//   around point lights.
// - Additional lights do NOT use ShadowColor; they only add. ShadowColor is
//   the floor established by the main light pass.

// Shared ramp so main and additional lights can't drift apart.
float StylizedRamp(float NdotL, float shadowAtten, float rampMin, float rampMax)
{
    return smoothstep(rampMin, rampMax, NdotL * shadowAtten);
}

void StylizedLighting_float(
    float3 PositionWS,
    float3 NormalWS,
    float3 ViewDirWS,
    float4 ScreenPos, // Screen Position node, Default mode
    float3 ShadowColor,
    float RampMin,
    float RampMax,
    float AmbientStrength,
    float3 RimColor,
    float RimPower,
    float RimStrength,
    float RimShadowMask, // 0 = rim everywhere (props), 1 = rim only on
                             // shadow side (characters, reads as bounce light)
    out float3 Shading)
{
#if defined(SHADERGRAPH_PREVIEW)
    float3 fakeLightDir = normalize(float3(0.5, 0.5, 0.25));
    float previewNdotL = saturate(dot(NormalWS, fakeLightDir));
    Shading = lerp(ShadowColor, float3(1, 1, 1), smoothstep(RampMin, RampMax, previewNdotL));
#else
    // ---- Main directional light ----
    float4 shadowCoord = TransformWorldToShadowCoord(PositionWS);
    Light mainLight = GetMainLight(shadowCoord);

    float NdotL = saturate(dot(NormalWS, mainLight.direction));
    float lit = StylizedRamp(NdotL, mainLight.shadowAttenuation * mainLight.distanceAttenuation,
                             RampMin, RampMax);
    float3 direct = lerp(ShadowColor, mainLight.color.rgb, lit);

    // ---- Additional lights (point / spot) ----
    // LIGHT_LOOP_BEGIN expands differently per rendering path. Under
    // Forward+ it iterates the screen-space cluster and expects a local
    // InputData with positionWS and normalizedScreenSpaceUV populated.
    float3 additional = float3(0, 0, 0);

    InputData inputData = (InputData) 0;
    inputData.positionWS = PositionWS;
    inputData.normalizedScreenSpaceUV = ScreenPos.xy;

    uint pixelLightCount = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(pixelLightCount)

    Light light = GetAdditionalLight(lightIndex, PositionWS, half4(1, 1, 1, 1));
    float aNdotL = saturate(dot(NormalWS, light.direction));
    float aLit = StylizedRamp(aNdotL, light.shadowAttenuation, RampMin, RampMax);
    additional += light.color.rgb * aLit * light.distanceAttenuation;
    LIGHT_LOOP_END

    // NOTE: under Forward+, extra *directional* lights beyond the main sun
    // are not in the cluster loop and are ignored here. We only use one sun,
    // so this is intentional scope, not an oversight.

    // ---- Ambient & rim ----
    float3 ambient = SampleSH(NormalWS) * AmbientStrength;

    float3 viewDir = normalize(ViewDirWS);
    float fresnel = pow(1.0 - saturate(dot(NormalWS, viewDir)), RimPower);
    // Blend between uniform rim and shadow-side-only rim. Always gated by
    // shadowAttenuation so nothing glows inside a cast shadow.
    float rimMask = lerp(1.0, 1.0 - lit, RimShadowMask) * mainLight.shadowAttenuation;
    float3 rim = RimColor * fresnel * RimStrength * rimMask;

    Shading = direct + additional + ambient + rim;
#endif
}

// Apply scene fog. Must run on the FINAL color (after albedo multiply),
// otherwise fog gets tinted per-object by albedo. Wire this as the last
// node before Base Color. Respects the Lighting window's fog settings
// (mode, color, density/range); does nothing when fog is disabled.
void ApplyFog_float(
    float3 Color,
    float3 PositionWS,
    out float3 Out)
{
#if defined(SHADERGRAPH_PREVIEW)
    Out = Color;
#else
    float4 positionCS = TransformWorldToHClip(PositionWS);
    float fogFactor = ComputeFogFactor(positionCS.z);
    Out = MixFog(Color, fogFactor);
#endif
}

// Vertex-stage wind sway for foliage. Wire into the VERTEX stage of a graph:
// output goes to the master stack's Position (Object space) slot.
// HeightMask: 0 = whole mesh sways rigidly, higher values pin the base and
// sway the top (grass/branches). Uses object-space height normalized by
// MeshHeight, so set MeshHeight roughly to the asset's height in meters.
void SwayFoliage_float(
    float3 PositionOS,
    float3 PositionWS, // world pos drives phase so instances desync
    float2 WindDirection, // XZ, doesn't need to be normalized
    float WindSpeed, // try 1.5
    float WindStrength, // object-space units of displacement, try 0.08
    float HeightMask, // exponent, try 1.5; 0 disables masking
    float MeshHeight, // approx mesh height in object space, try 1
    out float3 Out)
{
    // Two sines at different frequencies so motion doesn't read as metronome.
    float phase = dot(PositionWS.xz, float2(0.31, 0.27));
    float t = _Time.y * WindSpeed;
    float wave = sin(t + phase) * 0.7 + sin(t * 2.3 + phase * 1.7) * 0.3;

    float heightNorm = saturate(PositionOS.y / max(MeshHeight, 0.0001));
    float mask = (HeightMask <= 0.0) ? 1.0 : pow(heightNorm, HeightMask);

    float2 dir = normalize(WindDirection + float2(0.0001, 0.0));
    float3 offset = float3(dir.x, 0, dir.y) * wave * WindStrength * mask;

    Out = PositionOS + offset;
}

// Stylized water: depth-based color gradient + noise-modulated shoreline foam.
// Pure math on depth values supplied by the graph, so no includes needed and
// it previews fine. Graph must be Surface Type: Transparent, and the pipeline
// asset needs Depth Texture enabled.
//
//   SceneEyeDepth : Scene Depth node, Sampling = Eye
//   FragEyeDepth  : Screen Position node, Mode = Raw, Split -> W
//   FoamNoise     : 0..1 noise (Gradient Noise on world-XZ UVs, scrolled by Time)
void StylizedWater_float(
    float SceneEyeDepth,
    float FragEyeDepth,
    float FoamNoise,
    float3 ShallowColor,
    float3 DeepColor,
    float DepthMaxDistance, // world units at which water reads as fully deep, try 2
    float3 FoamColor,
    float FoamDistance, // foam band thickness in world units, try 0.4
    float ShallowAlpha, // try 0.4
    float DeepAlpha, // try 0.9
    out float3 Color,
    out float Alpha)
{
    // Distance from water surface to whatever opaque thing is behind it,
    // along the view ray. Negative shouldn't happen but clamp anyway.
    float waterDepth = max(SceneEyeDepth - FragEyeDepth, 0.0);

    float depthT = saturate(waterDepth / max(DepthMaxDistance, 0.0001));
    Color = lerp(ShallowColor, DeepColor, depthT);
    Alpha = lerp(ShallowAlpha, DeepAlpha, depthT);

    // Foam: hard-edged band where the water column is thin (shorelines and
    // anything opaque poking through), with noise eating into the edge so
    // it reads as churn instead of a contour line.
    float foam = step(waterDepth, FoamDistance * FoamNoise);
    Color = lerp(Color, FoamColor, foam);
    Alpha = lerp(Alpha, 1.0, foam);
}

// Gentle vertical bob for the water surface. Vertex stage, same wiring
// pattern as SwayFoliage: output -> master stack Position (Object).
// Two sine layers at different frequencies/angles so the surface undulates
// instead of pumping uniformly. Offset is computed in world space and
// transformed back, so a scaled/rotated water plane still bobs by
// WaveHeight world units.
void WaterWaves_float(
    float3 PositionOS,
    float3 PositionWS,
    float WaveHeight, // world units, try 0.05 — subtle is the point
    float WaveSpeed, // try 0.8
    float WaveScale, // spatial frequency, try 0.5
    out float3 Out)
{
#if defined(SHADERGRAPH_PREVIEW)
    Out = PositionOS;
#else
    float t = _Time.y * WaveSpeed;
    float w = sin(PositionWS.x * WaveScale + t) * 0.6
            + sin((PositionWS.x * 0.7 + PositionWS.z * 1.3) * WaveScale + t * 1.4) * 0.4;
    float3 offsetWS = float3(0, w * WaveHeight, 0);
    Out = PositionOS + TransformWorldToObjectDir(offsetWS, false);
#endif
}

// Stylized sun glints: hard-thresholded specular off a noise-wobbled up
// normal. NoiseA/NoiseB are two independent 0..1 noise samples (two Gradient
// Noise nodes at a finer scale than the foam noise, scrolling at different
// rates). Output ADDs to the water color before ApplyFog.
void WaterSparkle_float(
    float3 ViewDirWS,
    float NoiseA,
    float NoiseB,
    float Wobble, // normal perturbation, try 0.4
    float Sharpness, // specular power, try 80
    float Threshold, // glint cutoff, try 0.6; higher = rarer sparkles
    float3 SparkleColor,
    out float3 Out)
{
#if defined(SHADERGRAPH_PREVIEW)
    Out = float3(0, 0, 0);
#else
    Light mainLight = GetMainLight();
    // Mostly-up normal, wobbled per-pixel by the two noise channels.
    float3 n = normalize(float3((NoiseA - 0.5) * Wobble, 1.0, (NoiseB - 0.5) * Wobble));
    float3 v = normalize(ViewDirWS);
    float3 h = normalize(mainLight.direction + v);
    float spec = pow(saturate(dot(n, h)), Sharpness);
    float sparkle = step(Threshold, spec);
    Out = SparkleColor * mainLight.color.rgb * sparkle;
#endif
}

#endif // STYLIZED_LIGHTING_INCLUDED
