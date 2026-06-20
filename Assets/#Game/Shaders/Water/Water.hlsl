// Water.hlsl
// Port of water shader by Ajarus (viktor@ajarus.com) to Unity 6 URP HLSL
// Attribution-ShareAlike CC License
//
// Custom Function node setup:
//   Name:   "Water"
//   Source: this file
//
// Inputs:  float2 UV, float Time
// Outputs: float2 WarpedUV, float Shimmer

#ifndef WATER_INCLUDED
#define WATER_INCLUDED

#ifndef PI
    #define PI 3.1415926535897932
#endif

static const float _speed      = 0.2;
static const float _speed_x    = 0.2;
static const float _speed_y    = 0.3;

static const float _emboss     = 0.50;
static const float _intensity  = 2.4;
static const int   _steps      = 8;
static const float _frequency  = 6.0;
static const int   _angle      = 7;

static const float _delta                = 60.0;
static const float _gain                 = 700.0;
static const float _reflectionCutOff    = 0.012;
static const float _reflectionIntensity = 200000.0;

float WaveCol(float2 coord, float time)
{
    float delta_theta = 2.0 * PI / float(_angle);
    float col         = 0.0;

    for (int i = 0; i < _steps; i++)
    {
        float  theta = delta_theta * float(i);
        float2 adjc  = coord;
        adjc.x += cos(theta) * time * _speed + time * _speed_x;
        adjc.y -= sin(theta) * time * _speed - time * _speed_y;
        col += cos((adjc.x * cos(theta) - adjc.y * sin(theta)) * _frequency) * _intensity;
    }

    return cos(col);
}

void Water_float(float2 UV, float Time, out float2 WarpedUV, out float Shimmer)
{
    float time = Time * 1.3;

    float2 p  = UV;
    float2 c1 = p;
    float2 c2 = p;

    float cc1 = WaveCol(c1, time);

    // Finite difference gradient X
    c2.x += 1.0 / _delta;
    float dx = _emboss * (cc1 - WaveCol(c2, time)) / _delta;

    // Finite difference gradient Y
    c2.x = p.x;
    c2.y += 1.0 / _delta;
    float dy = _emboss * (cc1 - WaveCol(c2, time)) / _delta;

    // Warped UV for texture distortion
    c1.x += dx * 2.0;
    c1.y  = -(c1.y + dy * 2.0);
    WarpedUV = frac(c1);

    // Shimmer — only fires where the surface normal catches light
    // Output is 0 across most of the surface, spikes to >1 at highlight points
    float alpha = 1.0 + dot(dx, dy) * _gain;

    float ddx = dx - _reflectionCutOff;
    float ddy = dy - _reflectionCutOff;
    if (ddx > 0.0 && ddy > 0.0)
        alpha = pow(alpha, ddx * ddy * _reflectionIntensity);

    // Remap: subtract 1 so the "flat" baseline becomes 0
    // Only the bright specular spikes remain as positive values
    Shimmer = max(0.0, alpha - 1.0);
}

#endif // WATER_INCLUDED
