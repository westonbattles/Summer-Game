#ifndef SUMMER_GAME_DITHER_FADE_LIT_INPUT_INCLUDED
#define SUMMER_GAME_DITHER_FADE_LIT_INPUT_INCLUDED

// LitInput owns URP Lit's UnityPerMaterial constant buffer. Temporarily extend
// its single CBUFFER_END so the two custom properties remain SRP Batcher-safe
// without copying or modifying URP's LitInput implementation.
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#undef CBUFFER_END
#define CBUFFER_END float _Fade; float _DitherCellSize; };
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#undef CBUFFER_END
#define CBUFFER_END };

#include "DitherFade.hlsl"

#endif
