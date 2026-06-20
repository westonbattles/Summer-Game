// DomainWarpFBM.hlsl
// Port of "Base warp fBM" (ShaderToy) to Unity 6 URP HLSL
// https://www.shadertoy.com/view/tdG3Rd
// Custom Function node: Name = "DomainWarpFBM", Source = this file
//
// Inputs:  float2 UV, float Time
// Outputs: float4 Color, float Shade

#ifndef DOMAIN_WARP_FBM_INCLUDED
#define DOMAIN_WARP_FBM_INCLUDED

// ---------------------------------------------------------------------------
// Colormap (direct port — piecewise linear RGB ramps)
// ---------------------------------------------------------------------------

float colormap_red(float x)
{
    if (x < 0.0)
        return 54.0 / 255.0;
    else if (x < 20049.0 / 82979.0)
        return (829.79 * x + 54.51) / 255.0;
    else
        return 1.0;
}

float colormap_green(float x)
{
    if (x < 20049.0 / 82979.0)
        return 0.0;
    else if (x < 327013.0 / 810990.0)
        return (8546482679670.0 / 10875673217.0 * x - 2064961390770.0 / 10875673217.0) / 255.0;
    else if (x <= 1.0)
        return (103806720.0 / 483977.0 * x + 19607415.0 / 483977.0) / 255.0;
    else
        return 1.0;
}

float colormap_blue(float x)
{
    if (x < 0.0)
        return 54.0 / 255.0;
    else if (x < 7249.0 / 82979.0)
        return (829.79 * x + 54.51) / 255.0;
    else if (x < 20049.0 / 82979.0)
        return 127.0 / 255.0;
    else if (x < 327013.0 / 810990.0)
        return (792.02249341361393720147485376583 * x - 64.364790735602331034989206222672) / 255.0;
    else
        return 1.0;
}

float4 Colormap(float x)
{
    return float4(colormap_red(x), colormap_green(x), colormap_blue(x), 1.0);
}

// ---------------------------------------------------------------------------
// Noise
// ---------------------------------------------------------------------------

float rand(float2 n)
{
    return frac(sin(dot(n, float2(12.9898, 4.1414))) * 43758.5453);
}

float noise(float2 p)
{
    float2 ip = floor(p);
    float2 u  = frac(p);
    u = u * u * (3.0 - 2.0 * u);

    float res = lerp(
        lerp(rand(ip),                  rand(ip + float2(1.0, 0.0)), u.x),
        lerp(rand(ip + float2(0.0, 1.0)), rand(ip + float2(1.0, 1.0)), u.x),
        u.y);
    return res * res;
}

// ---------------------------------------------------------------------------
// fBM
// GLSL mat2 is column-major: mat2(0.80, 0.60, -0.60, 0.80)
// means col0=(0.80, 0.60), col1=(-0.60, 0.80).
// HLSL float2x2 is row-major, so we transpose:
//   row0=(0.80, -0.60), row1=(0.60, 0.80)
// Use mul(mtx, p) to match the original's mtx*p.
// ---------------------------------------------------------------------------

static const float2x2 mtx = float2x2(0.80, -0.60,
                                      0.60,  0.80);

float fbm(float2 p, float time)
{
    float f = 0.0;
    f += 0.500000 * noise(p + time);      p = mul(mtx, p) * 2.02;
    f += 0.031250 * noise(p);             p = mul(mtx, p) * 2.01;
    f += 0.250000 * noise(p);             p = mul(mtx, p) * 2.03;
    f += 0.125000 * noise(p);             p = mul(mtx, p) * 2.01;
    f += 0.062500 * noise(p);             p = mul(mtx, p) * 2.04;
    f += 0.015625 * noise(p + sin(time));
    return f / 0.96875;
}

float pattern(float2 p, float time)
{
    return fbm(p + fbm(p + fbm(p, time), time), time);
}

// ---------------------------------------------------------------------------
// Entry point — called by the Custom Function node
// Inputs:  UV   (float2, connect Shader Graph UV0 node)
//          Time (float,  connect Shader Graph Time node's "Time" port)
// Outputs: Color (float4)
//          Shade (float)
// ---------------------------------------------------------------------------

void DomainWarpFBM_float(float2 UV, float Time, out float4 Color, out float Shade)
{
    float shade = pattern(UV, Time);
    Color = float4(Colormap(shade).rgb, shade);
    Shade = shade;
}

#endif // DOMAIN_WARP_FBM_INCLUDED
