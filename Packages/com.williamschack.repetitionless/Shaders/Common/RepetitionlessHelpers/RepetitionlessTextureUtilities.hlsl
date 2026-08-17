#ifndef REPETITIONLESSTEXTUREUTILITIES_INCLUDED
#define REPETITIONLESSTEXTUREUTILITIES_INCLUDED

#include "../Utilities/TextureUtilities.hlsl"
#include "../TextureArrayEssentials/TextureArrayUtilities.hlsl"

// Samples the base and edge colour if required and lerps them together
// Uses a regular texture
float4 SampleRepetitionlessTexture(
    Texture2D Texture,
    SamplerState SS,
    
    float EdgeMask,
    float2 EdgeUV,
    float2 TransformedUV,
    bool SampleEdge,

    bool NormalMap = false,
    float NormalStrength = 1.0
){
    float4 baseTextureColor = 1;

    // Only sample base material if visible
    if (!SampleEdge || (SampleEdge && EdgeMask != 1))
        baseTextureColor = SAMPLE_TEXTURE2D(Texture, SS, TransformedUV);

    if (NormalMap) baseTextureColor.rgb = UnpackNormalMap(baseTextureColor, NormalStrength);

    if (SampleEdge) {
        float4 edgeTextureColor = SAMPLE_TEXTURE2D(Texture, SS, EdgeUV);
        if (NormalMap) edgeTextureColor.rgb = UnpackNormalMap(edgeTextureColor, NormalStrength);
        baseTextureColor = lerp(baseTextureColor, edgeTextureColor, EdgeMask);
    }

    return baseTextureColor;
}

// Samples the base and edge colour if required and lerps them together
// Uses a texture array
float4 SampleRepetitionlessArrayTexture(
    Texture2DArray TextureArray,
    int AssignedTextures[3],
    int ConstantIndex,
    SamplerState SS,

    float EdgeMask,
    float2 EdgeUV,
    float2 TransformedUV,
    bool SampleEdge
){
    float4 baseTextureColor = 1;
    
    int assignedTexturesPadded[BOOLEAN_COMPRESSION_MAX_CHUNKS] = {
        AssignedTextures[0],
        AssignedTextures[1],
        AssignedTextures[2],
        0
    };

    // Only sample base material if visible
    if (!SampleEdge || (SampleEdge && EdgeMask != 1))
        baseTextureColor = SampleArrayAtConstantIndex(TextureArray, assignedTexturesPadded, ConstantIndex, TransformedUV, 1, SS);

    if (SampleEdge) {
        float4 edgeTextureColor = SampleArrayAtConstantIndex(TextureArray, assignedTexturesPadded, ConstantIndex, EdgeUV, 1, SS);
        baseTextureColor = lerp(baseTextureColor, edgeTextureColor, EdgeMask);
    }

    return baseTextureColor;
}

#endif