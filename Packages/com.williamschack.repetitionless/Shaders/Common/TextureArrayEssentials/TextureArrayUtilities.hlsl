// ---------------------------------------------------
// | Script created by me taken from my other asset: |
// | Texture Array Essentials (https://u3d.as/3s4d)  |
// ---------------------------------------------------

#ifndef TEXTUREARRAYUTILITIES_INCLUDED
#define TEXTUREARRAYUTILITIES_INCLUDED

#include "../Utilities/BooleanCompression.hlsl"

int GetIndexInArray(int TexturesAssignedCompressed[BOOLEAN_COMPRESSION_MAX_CHUNKS], int Index)
{
    // Dont loop with no iterations, will cause unrolling warnings
    if (Index < 0)
        return -1;

    if (!GetCompressedValue(TexturesAssignedCompressed, Index))
        return -1;

    // Get the index of the texture in the array
    int arrayIndex = 0;
    for (int i = 0; i < Index; i++) {
        if (GetCompressedValue(TexturesAssignedCompressed, i))
            arrayIndex++;
    }

    return arrayIndex;
}

float4 SampleArrayAtConstantIndex(
    Texture2DArray TextureArray,
    int TexturesAssignedCompressed[BOOLEAN_COMPRESSION_MAX_CHUNKS],
    int Index,
    float2 UV,
    float4 UnassignedColor,
    SamplerState SS
){
    // Get the index of the texture in the array
    int arrayIndex = GetIndexInArray(TexturesAssignedCompressed, Index);

    if (arrayIndex == -1)
        return UnassignedColor;

    // Sample the array at the index found previously
    return SAMPLE_TEXTURE2D_ARRAY(TextureArray, SS, UV, arrayIndex);
}

#endif