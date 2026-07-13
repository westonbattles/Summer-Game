#ifndef STYLIZED_TERRAIN_LIGHTING_INCLUDED
#define STYLIZED_TERRAIN_LIGHTING_INCLUDED

// Shared ramp calculation for main and additional lights
float TerrainStylizedRamp(float NdotL, float shadowAtten, float rampMin, float rampMax)
{
    float lightRamp = smoothstep(rampMin, rampMax, NdotL);
    float shadowRamp = smoothstep(0.25, 0.9, shadowAtten);
    return min(lightRamp, shadowRamp);
}

void StylizedTerrainLighting_float(
    float3 Albedo,
    float3 NormalWS,
    float3 PositionWS,
    float3 ViewDirWS,
    float4 ScreenPos,
    float3 ShadowColor,
    float RampMin,
    float RampMax,
    float AmbientStrength,
    out float3 FinalColor)
{
#if defined(SHADERGRAPH_PREVIEW)
    float3 fakeLightDir = normalize(float3(0.5, 0.5, 0.25));
    float previewNdotL = saturate(dot(NormalWS, fakeLightDir));
    float3 previewBaked = lerp(ShadowColor, float3(1, 1, 1), smoothstep(RampMin, RampMax, previewNdotL));
    FinalColor = Albedo * previewBaked;
#else
    // ---- Main Directional Light ----
    float4 shadowCoord = TransformWorldToShadowCoord(PositionWS);
    Light mainLight = GetMainLight(shadowCoord);

    float NdotL = saturate(dot(NormalWS, mainLight.direction));
    float lit = TerrainStylizedRamp(NdotL, mainLight.shadowAttenuation * mainLight.distanceAttenuation, RampMin, RampMax);
    
    // Core lighting color floor
    float3 directLightColor = lerp(ShadowColor, mainLight.color.rgb, lit);

    // ---- Additional Lights (Point / Spot) ----
    float3 additionalLightColor = float3(0, 0, 0);

    // Safety fix: Rename struct instance to avoid Unity 6/URP Lit generation collisions
    InputData terrainInputData = (InputData) 0;
    terrainInputData.positionWS = PositionWS;
    terrainInputData.normalizedScreenSpaceUV = ScreenPos.xy;

    uint pixelLightCount = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(pixelLightCount)

    Light light = GetAdditionalLight(lightIndex, PositionWS, half4(1, 1, 1, 1));
    float aNdotL = saturate(dot(NormalWS, light.direction));
    float aLit = TerrainStylizedRamp(aNdotL, light.shadowAttenuation, RampMin, RampMax);
    additionalLightColor += light.color.rgb * aLit * light.distanceAttenuation;
    LIGHT_LOOP_END

    // ---- Ambient ----
    float3 ambient = SampleSH(NormalWS) * AmbientStrength;

    // ---- Combine Shading and Tint Albedo ----
    // We multiply by Albedo at the very end so your painted terrain layers texture maps get lit correctly
    float3 totalLighting = directLightColor + additionalLightColor + ambient;
    FinalColor = Albedo * totalLighting;
#endif
}

#endif // STYLIZED_TERRAIN_LIGHTING_INCLUDED