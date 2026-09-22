#ifndef SUMMER_GAME_DITHER_FADE_INCLUDED
#define SUMMER_GAME_DITHER_FADE_INCLUDED

// Returns the same radial, screen-pixel dot threshold used by the original
// Summer Game/Player/Dither Fade shader.
float ScreenDotDitherThreshold(float4 positionCS, float cellSize)
{
    cellSize = max(cellSize, 1.0);
    float2 cell = frac(positionCS.xy / cellSize) - 0.5;
    return saturate(length(cell) * 1.41421356237);
}

void ClipDither(float fade, float4 positionCS, float cellSize)
{
    float fade01 = saturate(fade);
    clip(
        (fade01 > 0.0 ? fade01 : -1.0)
        - ScreenDotDitherThreshold(positionCS, cellSize)
    );
}

#endif
