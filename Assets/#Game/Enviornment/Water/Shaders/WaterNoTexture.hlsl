// WaterNoTexture.hlsl
// Port of water shader by Ajarus (viktor@ajarus.com) to Unity 6 URP HLSL
// Attribution-ShareAlike CC License
//
// Custom Function node setup:
//   Name:   "WaterNoTexture"
//   Source: this file
//
// Inputs:  float2 UV, float Time, float4 ShallowColor, float4 DeepColor
// Outputs: float4 Color, float Shimmer

#ifndef WATER_NO_TEXTURE_INCLUDED
#define WATER_NO_TEXTURE_INCLUDED

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

void WaterNoTexture_float(float2 UV, float Time, float4 ShallowColor, float4 DeepColor,
                          out float4 Color, out float Shimmer)
{
    float time = Time * 1.3;

    float2 p  = UV;
    float2 c1 = p;
    float2 c2 = p;

    // Raw wave field value — ranges roughly -1 to 1, remap to 0-1 for lerp
    float cc1 = WaveCol(c1, time);
    float t   = cc1 * 0.5 + 0.5;

    // Lerp between deep and shallow based on wave height
    Color = lerp(DeepColor, ShallowColor, t);

    // Finite difference gradient X
    c2.x += 1.0 / _delta;
    float dx = _emboss * (cc1 - WaveCol(c2, time)) / _delta;

    // Finite difference gradient Y
    c2.x = p.x;
    c2.y += 1.0 / _delta;
    float dy = _emboss * (cc1 - WaveCol(c2, time)) / _delta;

    // Shimmer
    float alpha = 1.0 + dot(dx, dy) * _gain;

    float ddx = dx - _reflectionCutOff;
    float ddy = dy - _reflectionCutOff;
    if (ddx > 0.0 && ddy > 0.0)
        alpha = pow(alpha, ddx * ddy * _reflectionIntensity);

    Shimmer = max(0.0, alpha - 1.0);
}

#endif // WATER_NO_TEXTURE_INCLUDED
