Shader "Underwater/Background Effects/Underwater Surface Filter"
{
    Properties
    {
        [HDR]_TintColor ("Tint Color", Color) = (0.08, 0.65, 0.85, 1)
        _TintStrength ("Tint Strength", Range(0, 1)) = 0.45
        _Alpha ("Alpha", Range(0, 1)) = 0.55
        _DistortionStrength ("Distortion Strength", Range(0, 0.08)) = 0.018
        _NoiseScale ("Noise Scale", Range(0.1, 80)) = 18
        _NoiseSpeed ("Noise Speed", Range(0, 3)) = 0.18
        _FresnelPower ("Fresnel Power", Range(0.25, 8)) = 2.2
        _FresnelStrength ("Fresnel Strength", Range(0, 1)) = 0.22
        _Brightness ("Brightness", Range(0, 3)) = 1.05
        _CausticsTex ("Caustics Texture", 2D) = "white" {}
        [HDR]_CausticsColor ("Caustics Color", Color) = (0.45, 1.0, 0.9, 1)
        _CausticsIntensity ("Caustics Intensity", Range(0, 10)) = 1.2
        _CausticsAlphaBoost ("Caustics Alpha Boost", Range(0, 1)) = 0.12
        _CausticsTiling ("Caustics Tiling", Range(0.01, 20)) = 2.5
        _CausticsSpeedA ("Caustics Speed A", Vector) = (0.03, 0.02, 0, 0)
        _CausticsSpeedB ("Caustics Speed B", Vector) = (-0.02, 0.04, 0, 0)
        [Enum(UnityEngine.Rendering.CullMode)]_CullMode ("Cull Mode", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+20"
        }

        Pass
        {
            Name "UnderwaterSurfaceFilter"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull [_CullMode]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_CausticsTex);
            SAMPLER(sampler_CausticsTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _TintColor;
                half4 _CausticsColor;
                float _TintStrength;
                float _Alpha;
                float _DistortionStrength;
                float _NoiseScale;
                float _NoiseSpeed;
                float _FresnelPower;
                float _FresnelStrength;
                float _Brightness;
                float _CausticsIntensity;
                float _CausticsAlphaBoost;
                float _CausticsTiling;
                float4 _CausticsSpeedA;
                float4 _CausticsSpeedB;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float4 screenPosition : TEXCOORD3;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
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

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                output.screenPosition = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPosition.xy / input.screenPosition.w;
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirWS = normalize(input.viewDirWS);

                float t = _Time.y * _NoiseSpeed;
                float2 noiseUV = input.positionWS.xz * _NoiseScale + float2(t, -t * 0.63);
                float2 noiseOffset = float2(ValueNoise(noiseUV), ValueNoise(noiseUV + 31.41)) * 2.0 - 1.0;
                half fresnel = pow(saturate(1.0 - abs(dot(normalWS, viewDirWS))), _FresnelPower);

                half3 sceneColor = SampleSceneColor(screenUV + noiseOffset * _DistortionStrength);
                half3 tintedColor = lerp(sceneColor, _TintColor.rgb, _TintStrength) * _Brightness;
                tintedColor += _TintColor.rgb * fresnel * _FresnelStrength;

                float2 causticsUV = input.positionWS.xz * _CausticsTiling;
                float2 causticsUVa = causticsUV + _Time.y * _CausticsSpeedA.xy;
                float2 causticsUVb = causticsUV * 1.37 + _Time.y * _CausticsSpeedB.xy;
                half causticsA = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, causticsUVa).r;
                half causticsB = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, causticsUVb).r;
                half caustics = saturate(max(causticsA, causticsB) * (0.65 + fresnel * 0.35));
                tintedColor += _CausticsColor.rgb * caustics * _CausticsIntensity;

                half alpha = saturate(_Alpha + fresnel * _FresnelStrength + caustics * _CausticsAlphaBoost);
                return half4(tintedColor, alpha);
            }
            ENDHLSL
        }
    }
}
