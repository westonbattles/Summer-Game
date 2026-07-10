#ifndef KILl_TRACKER_INCLUDED
#define KILl_TRACKER_INCLUDED

// killtracker.hlsl
void SegmentedBar_float(
    float2 UV,
    float Fill, // 0-1, driven from C#
    float SegmentCount, // e.g. 10
    float GapWidth, // 0-0.5, in per-segment space (0.1 ~ nice)
    float RimWidth, // 0-0.5, in UV space
    float AspectRatio, // bar width / height, e.g. 8
    out float SegmentMask, // 1 inside a box, 0 in the gaps
    out float FillMask, // 1 where the bar is filled
    out float RimMask)       // glow band around the outer border
{
    // --- segments ---
    float sx = UV.x * SegmentCount;
    float cell = floor(sx);
    float f = frac(sx);
    float half = GapWidth * 0.5;
    SegmentMask = step(half, f) * step(f, 1.0 - half);

    // --- fill: each box fills individually as Fill sweeps across ---
    FillMask = saturate(Fill * SegmentCount - cell);

    // --- outer rim: distance to the quad border, aspect-corrected ---
    float dx = min(UV.x, 1.0 - UV.x) * AspectRatio;
    float dy = min(UV.y, 1.0 - UV.y);
    float d = min(dx, dy);
    RimMask = 1.0 - smoothstep(0.0, RimWidth, d);
}

#endif