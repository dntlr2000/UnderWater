Shader "Underwater/Background Effects/Soft Light Circle Decal"
{
    Properties
    {
        [HDR]_TintColor ("Tint Color", Color) = (0.55, 0.95, 1.25, 1)
        _Intensity ("Intensity", Range(0, 10)) = 1.5
        _Opacity ("Opacity", Range(0, 1)) = 0.35
        _Radius ("Radius", Range(0.01, 0.75)) = 0.48
        _Softness ("Softness", Range(0.001, 0.5)) = 0.22
        _CenterStrength ("Center Strength", Range(0, 2)) = 0.7
        _EdgeNoise ("Edge Noise", Range(0, 1)) = 0.08
        _NoiseScale ("Noise Scale", Range(0.1, 40)) = 8
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry+1"
        }

        Pass
        {
            Name "DBufferProjector"
            Tags { "LightMode" = "DBufferProjector" }

            Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            ColorMask RGBA 0
            ColorMask 0 1
            ColorMask 0 2
            Cull Front
            ZTest Greater
            ZWrite Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma exclude_renderers gles3 glcore
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TintColor;
                float _Intensity;
                float _Opacity;
                float _Radius;
                float _Softness;
                float _CenterStrength;
                float _EdgeNoise;
                float _NoiseScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            half CircleMask(float2 uv)
            {
                float2 centeredUV = uv - 0.5;
                float noise = (ValueNoise(uv * _NoiseScale) * 2.0 - 1.0) * _EdgeNoise;
                float radius = max(_Radius + noise, 0.001);
                float distanceFromCenter = length(centeredUV);
                float outerFade = 1.0 - smoothstep(max(radius - _Softness, 0.0), radius, distanceFromCenter);
                float centerGlow = pow(saturate(1.0 - distanceFromCenter / radius), max(_CenterStrength, 0.001));
                return saturate(outerFade * lerp(1.0, centerGlow, 0.55));
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            void Frag(Varyings input, OUTPUT_DBUFFER(outDBuffer))
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 screenUV = input.positionCS.xy * _ScreenSize.zw;
                #if UNITY_REVERSED_Z
                    float depth = LoadSceneDepth(input.positionCS.xy);
                #else
                    float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, LoadSceneDepth(input.positionCS.xy));
                #endif

                float3 positionWS = ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);
                float3 positionDS = TransformWorldToObject(positionWS);
                positionDS *= float3(1.0, -1.0, 1.0);

                float clipValue = 0.5 - Max3(abs(positionDS).x, abs(positionDS).y, abs(positionDS).z);
                clip(clipValue);

                float2 decalUV = positionDS.xz + 0.5;
                half mask = CircleMask(decalUV) * _Opacity;
                clip(mask - 0.001);

                DecalSurfaceData surfaceData = (DecalSurfaceData)0;
                surfaceData.baseColor = half4(_TintColor.rgb * _Intensity, mask);
                surfaceData.normalWS = half4(0, 0, 1, 1);
                surfaceData.emissive = 0;
                surfaceData.metallic = 0;
                surfaceData.occlusion = 0;
                surfaceData.smoothness = 0;
                surfaceData.MAOSAlpha = 1;

                ENCODE_INTO_DBUFFER(surfaceData, outDBuffer);
            }
            ENDHLSL
        }
    }
}
