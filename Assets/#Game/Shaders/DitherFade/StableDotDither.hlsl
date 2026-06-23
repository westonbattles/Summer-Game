// StableDotDither.hlsl
// Custom Function node setup:
//   Name:   "StableDotDither"
//   Source: this file
//
// Inputs:
//   Fade     float   0 = hidden, 1 = fully visible
//   Position float3  connect a Position node, preferably Object space
//   CellSize float   size of each dot cell in Position units
//
// Outputs:
//   Alpha
//   AlphaClipThreshold

#ifndef STABLE_DOT_DITHER_INCLUDED
#define STABLE_DOT_DITHER_INCLUDED

void StableDotDither_float(float Fade, float3 Position, float CellSize,
                           out float Alpha, out float AlphaClipThreshold)
{
    Fade = saturate(Fade);
    CellSize = max(CellSize, 0.0001);

    // Object/world-space cells make the pattern stick to the model or world
    // instead of crawling across the screen as the camera moves.
    float3 cell = frac(Position / CellSize) - 0.5;

    // 0 at the cell center, 1 at the farthest cube corner.
    float radialThreshold = saturate(length(cell) * 1.154700538);

    Alpha = Fade - radialThreshold;
    AlphaClipThreshold = 0.0;
}

void StableDotDither_half(half Fade, half3 Position, half CellSize,
                          out half Alpha, out half AlphaClipThreshold)
{
    Fade = saturate(Fade);
    CellSize = max(CellSize, 0.0001h);

    half3 cell = frac(Position / CellSize) - 0.5h;
    half radialThreshold = saturate(length(cell) * 1.154700538h);

    Alpha = Fade - radialThreshold;
    AlphaClipThreshold = 0.0h;
}

#endif // STABLE_DOT_DITHER_INCLUDED
