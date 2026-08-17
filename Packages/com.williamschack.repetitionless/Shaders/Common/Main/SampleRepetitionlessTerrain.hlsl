#ifndef SAMPLEREPETITIONLESSTERRAIN_INCLUDED
#define SAMPLEREPETITIONLESSTERRAIN_INCLUDED

#include "SampleRepetitionlessLayer.hlsl"

// Assume control and terrain holes properties are:
// _TerrainHolesTexture
// _Control{Index}

void SampleRepetitionlessTerrain(
    // General Settings
    SamplerState SS, float2 UV, float3 WorldNormalVector,
    float3 WorldPosition, float3 CameraPosition,
    int SurfaceType, int UVSpace, int VertexColourBlendModeIndex, int DebuggingIndex,
    float4 VertexColour,

    // Properties
    int LayersCount,
    Texture2D PropertiesTexture,
    Texture2D AssignedTexturesTexture,

    // Textures
    Texture2DArray AVTextures,
    Texture2DArray NSOTextures,
    Texture2DArray EMTextures,
    Texture2DArray BMTextures,

    Texture2D NoiseTexture,

    // Outputs
    out float4 AlbedoColorOut,
    out float3 NormalVectorOut,
    out float MetallicOut,
    out float SmoothnessOut,
    out float OcclussionOut,
    out float3 EmissionColorOut
) {
    float4 albedoColor  = 1;
    float3 normalVector = WorldNormalVector;
    float  metallic     = 0;
    float  smoothness   = 0;
    float  occlussion   = 0;
    float3 emission     = 0;

    // Terrain Holes
#ifdef _ALPHATEST_ON
    float holeColour = SAMPLE_TEXTURE2D(_TerrainHolesTexture, sampler_TerrainHolesTexture, UV).r;
#else
    float holeColour = 1;
#endif
    clip(albedoColor.a - (1 - (holeColour - 0.01)));

    if (holeColour == 0) {
        AlbedoColorOut   = albedoColor;
        NormalVectorOut  = normalVector;
        MetallicOut      = metallic;
        SmoothnessOut    = smoothness;
        OcclussionOut    = occlussion;
        EmissionColorOut = emission;
        return;
    }

    // Sample control texture
    half4 controlColour = SAMPLE_TEXTURE2D(_Control0, sampler_Control0, UV);
    
    // Get individual weights and sum
    half controlWeights[4] = {
        controlColour.x,
        controlColour.y,
        controlColour.z,
        controlColour.w
    };

    half controlSum = dot(controlColour, 1.0);

    half backgroundControl = saturate(1 - controlSum);

    // Normalize weights for additive blending
    if (controlSum > 1) {
        [unroll]
        for (int controlWeightIndex = 0; controlWeightIndex < 4; controlWeightIndex++) {
            controlWeights[controlWeightIndex] /= controlSum;
        }
    }

    // Read array assigned texture
    int assignedAVTextures[3];
    int assignedNSOTextures[3];
    int assignedEVTextures[3];
    int assignedBMTextures;

    GetArrayAssignedTextures(AssignedTexturesTexture, assignedAVTextures, assignedNSOTextures, assignedEVTextures, assignedBMTextures);

    // Variables
    albedoColor  = backgroundControl;
    normalVector = WorldNormalVector * backgroundControl;
    metallic     = 0;
    smoothness   = 0;
    occlussion   = backgroundControl;
    emission     = 0;

    // Sample Layers
    [loop]
    for (int i = 0; i < 4; i++) {
        half layerControl = controlWeights[i];

        [branch]
        if (layerControl == 0)
            continue;

        float4 layerAlbedo    = albedoColor;
        float3 layerNormal    = normalVector;
        float layerMetallic   = metallic;
        float layerSmoothness = smoothness;
        float layerOcclussion = occlussion;
        float3 layerEmission  = emission;

        SampleRepetitionlessLayer(
            SS, UV, WorldNormalVector,
            WorldPosition, CameraPosition,
            SurfaceType, UVSpace, VertexColourBlendModeIndex, DebuggingIndex,
            VertexColour,
            i,
            PropertiesTexture,
            assignedAVTextures[0], assignedAVTextures[1], assignedAVTextures[2],
            assignedNSOTextures[0], assignedNSOTextures[1], assignedNSOTextures[2],
            assignedEVTextures[0], assignedEVTextures[1], assignedEVTextures[2],
            assignedBMTextures,
            AVTextures,
            NSOTextures,
            EMTextures,
            BMTextures,
            NoiseTexture,
            layerAlbedo, layerNormal, layerMetallic, layerSmoothness, layerOcclussion, layerEmission
        );

        albedoColor  += layerAlbedo     * layerControl;
        normalVector += layerNormal     * layerControl;
        metallic     += layerMetallic   * layerControl;
        smoothness   += layerSmoothness * layerControl;
        occlussion   += layerOcclussion * layerControl;
        emission     += layerEmission   * layerControl;
    }

    AlbedoColorOut   = albedoColor;
    NormalVectorOut  = normalize(normalVector);
    MetallicOut      = metallic;
    SmoothnessOut    = smoothness;
    OcclussionOut    = occlussion;
    EmissionColorOut = emission;
}

#endif