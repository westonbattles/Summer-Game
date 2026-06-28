Shader "Hidden/Summer Game/Player Screen Space Outline"
{
    Properties
    {
        [HDR] _OutlineColor("Outline Color", Color) = (0.02, 0.03, 0.04, 1)
        _OutlineWidth("Outline Width", Range(0, 0.1)) = 0.035
        _DitherCellSize("Dither Cell Size (Pixels)", Range(1, 32)) = 7
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "PlayerScreenSpaceOutline"

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex FullscreenVertex
            #pragma fragment OutlineFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_PlayerOutlineMask);
            SAMPLER(sampler_PlayerOutlineMask);

            half4 _OutlineColor;
            float _OutlineWidth;
            float _DitherCellSize;
            float4 _PlayerOutlineMask_TexelSize;
            float _PlayerOutlineFade;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings FullscreenVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            float SampleMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(
                    _PlayerOutlineMask,
                    sampler_PlayerOutlineMask,
                    UnityStereoTransformScreenSpaceTex(uv)).r;
            }

            float ScreenDotDitherThreshold(float4 positionCS)
            {
                float cellSize = max(_DitherCellSize, 1.0);
                float2 cell = frac(positionCS.xy / cellSize) - 0.5;
                return saturate(length(cell) * 1.41421356237);
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float center = SampleMask(input.uv);
                float expanded = center;

                // Preserve the old 0.035 setting while interpreting it as
                // approximately 3.5 screen pixels for this screen-space effect.
                int radius = clamp((int)ceil(_OutlineWidth * 100.0), 1, 8);
                float2 texel = _PlayerOutlineMask_TexelSize.xy;

                [unroll]
                for (int distance = 1; distance <= 8; distance++)
                {
                    if (distance > radius)
                    {
                        break;
                    }

                    float2 offset = texel * distance;

                    expanded = max(expanded, SampleMask(input.uv + float2( offset.x, 0)));
                    expanded = max(expanded, SampleMask(input.uv + float2(-offset.x, 0)));
                    expanded = max(expanded, SampleMask(input.uv + float2(0,  offset.y)));
                    expanded = max(expanded, SampleMask(input.uv + float2(0, -offset.y)));
                    expanded = max(expanded, SampleMask(input.uv + float2( offset.x,  offset.y)));
                    expanded = max(expanded, SampleMask(input.uv + float2(-offset.x,  offset.y)));
                    expanded = max(expanded, SampleMask(input.uv + float2( offset.x, -offset.y)));
                    expanded = max(expanded, SampleMask(input.uv + float2(-offset.x, -offset.y)));
                }

                float outline = saturate(expanded - center);
                float fade = saturate(_PlayerOutlineFade);
                float ditherThreshold = ScreenDotDitherThreshold(input.positionCS);
                float ditherVisible = step(ditherThreshold, fade);

                half4 color = _OutlineColor;
                color.a *= outline * ditherVisible;
                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
