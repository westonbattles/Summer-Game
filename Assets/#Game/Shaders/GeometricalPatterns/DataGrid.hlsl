#ifndef DATAGRID_INCLUDED
#define DATAGRID_INCLUDED

// ----------------------------------------------------------------------------
// DataGrid - a flat "data wall" inspired by the Destiny grid concept art.
// Drop this in a Shader Graph Custom Function node (Type = File).
// Function name in the node: DataGrid   (Shader Graph appends "_float")
// ----------------------------------------------------------------------------

// 2D -> 1D hash, used for per-cell randomness
float DG_hash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

// Smooth value noise in [0,1] (drives the glowing hotspots)
float DG_vnoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);                 // smootherstep-ish interpolation
    float a = DG_hash21(i);
    float b = DG_hash21(i + float2(1.0, 0.0));
    float c = DG_hash21(i + float2(0.0, 1.0));
    float d = DG_hash21(i + float2(1.0, 1.0));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// Signed distance to an axis-aligned 2D box (half-extents b)
float DG_sdBox(float2 p, float2 b)
{
    float2 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

void DataGrid_float(
    float2 UV,
    float  Time,
    float  Scale,         // grid density (try 8-20)
    float  LineWidth,     // wireframe thickness in cell-space (try 0.02-0.06)
    float  GlowStrength,  // how hot the hotspots get (try 1-4)
    out float3 Color,
    out float  Alpha)
{
    // --- palette (edit freely) ---
    const float3 lineTint = float3(0.25, 0.55, 0.55); // teal frames
    const float3 glowTint = float3(0.30, 0.70, 1.00); // electric-blue hotspots
    const float3 nodeTint = float3(0.70, 0.90, 1.00); // bright cool node dots

    // --- grid space + slow drift so it feels alive ---
    float2 gv = UV * Scale + float2(Time * 0.05, Time * 0.02);

    float2 cellId = floor(gv);
    float2 cellUv = frac(gv) - 0.5;               // local coords [-0.5, 0.5] per cell

    float r = DG_hash21(cellId);

    // ~75% of cells draw a box; the rest stay dark (the gaps in the reference)
    float alive = step(0.25, r);

    // random box size per cell -> the varied-rectangle look
    float halfSize = lerp(0.16, 0.42, DG_hash21(cellId + 17.0));
    float bd = DG_sdBox(cellUv, halfSize.xx);

    // thin wireframe frame: bright where we're close to the box edge
    float frame = (1.0 - smoothstep(0.0, LineWidth, abs(bd))) * alive;

    // node dots on the integer lattice (cell corners).
    // multiply gv by 2.0 here if you want a denser dot field.
    float dotDist = length(frac(gv + 0.5) - 0.5);
    float dots = 1.0 - smoothstep(0.0, LineWidth * 1.6, dotDist);

    // --- glow hotspots: smooth low-freq field that drifts over time ---
    float hot = DG_vnoise(gv * 0.18 - Time * 0.12);
    hot = pow(saturate(hot), 3.5);                // keeps the bright zones sparse

    // --- compose ---
    float frameLum = frame * (0.35 + hot * GlowStrength);
    float dotLum   = dots  * (0.50 + hot * GlowStrength);

    float3 col = lerp(lineTint, glowTint, hot) * frameLum;
    col += nodeTint * dotLum;

    Color = col;
    Alpha = saturate(frameLum + dotLum);
}
#endif
