Shader "Underwater/Background Effects/Glow Distortion Shell"
{
    Properties
    {
        _TintColor ("Tint Color", Color) = (0.35, 0.9, 1.0, 1.0)
        _DistortionStrength ("Distortion Strength", Range(0, 0.08)) = 0.03
        _NoiseScale ("Noise Scale", Range(0.1, 50)) = 12
        _NoiseSpeed ("Noise Speed", Range(0, 2)) = 0.08
        _FresnelPower ("Fresnel Power", Range(0.25, 8)) = 2.5
        _FresnelInfluence ("Fresnel Influence", Range(0, 1)) = 0.45
        _TintStrength ("Tint Strength", Range(0, 1)) = 0.15
        _Alpha ("Alpha", Range(0, 1)) = 0.28
        _WobbleStrength ("Wobble Strength", Range(0, 0.08)) = 0.018
        _WobbleScale ("Wobble Scale", Range(0.1, 20)) = 4
        _WobbleSpeed ("Wobble Speed", Range(0, 3)) = 0.35
        _PulseSpeed ("Pulse Speed", Range(0, 4)) = 0.8
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.18
        _FlowScaleA ("Large Flow Scale", Range(0.1, 50)) = 5
        _FlowScaleB ("Small Flow Scale", Range(0.1, 80)) = 22
        _FlowSpeedA ("Large Flow Speed", Vector) = (0.04, 0.025, 0, 0)
        _FlowSpeedB ("Small Flow Speed", Vector) = (-0.12, 0.08, 0, 0)
        _EdgeBreakup ("Edge Breakup", Range(0, 1)) = 0.22
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
            Name "GlowDistortionShell"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TintColor;
                float _DistortionStrength;
                float _NoiseScale;
                float _NoiseSpeed;
                float _FresnelPower;
                float _FresnelInfluence;
                float _TintStrength;
                float _Alpha;
                float _WobbleStrength;
                float _WobbleScale;
                float _WobbleSpeed;
                float _PulseSpeed;
                float _PulseAmount;
                float _FlowScaleA;
                float _FlowScaleB;
                float4 _FlowSpeedA;
                float4 _FlowSpeedB;
                float _EdgeBreakup;
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

            // Produces a stable pseudo-random value from a 2D coordinate.
            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            // Samples smoothed value noise for soft underwater flow.
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

            // Offsets shell vertices slightly so the distortion volume feels like moving water.
            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float wobbleTime = _Time.y * _WobbleSpeed;
                float2 wobbleUV = positionWS.xz * _WobbleScale + float2(wobbleTime, -wobbleTime * 0.73);
                float wobbleA = ValueNoise(wobbleUV);
                float wobbleB = ValueNoise(positionWS.zy * (_WobbleScale * 0.73) + float2(-wobbleTime * 0.47, wobbleTime));
                float wobblePulse = sin(_Time.y * _PulseSpeed * 0.73) * _PulseAmount * 0.5;
                float wobble = (((wobbleA * 0.65 + wobbleB * 0.35) * 2.0 - 1.0) + wobblePulse) * _WobbleStrength;
                positionWS += normalWS * wobble;

                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                output.screenPosition = ComputeScreenPos(output.positionCS);
                return output;
            }

            // Distorts the opaque scene color with layered animated flow and a broken-up rim mask.
            half4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPosition.xy / input.screenPosition.w;
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirWS = normalize(input.viewDirWS);

                half fresnel = pow(saturate(1.0 - abs(dot(normalWS, viewDirWS))), _FresnelPower);
                half distortionMask = lerp(1.0, fresnel, _FresnelInfluence);
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;

                float2 flowUVa = input.positionWS.xz * _FlowScaleA + _Time.y * _FlowSpeedA.xy;
                float2 flowUVb = input.positionWS.xz * _FlowScaleB + _Time.y * _FlowSpeedB.xy;
                float2 flowA = float2(ValueNoise(flowUVa), ValueNoise(flowUVa + 17.37)) * 2.0 - 1.0;
                float2 flowB = float2(ValueNoise(flowUVb), ValueNoise(flowUVb + 43.11)) * 2.0 - 1.0;
                float2 legacyFlowUV = input.positionWS.xz * _NoiseScale + _Time.y * _NoiseSpeed * float2(1.0, -0.7);
                float2 legacyFlow = float2(ValueNoise(legacyFlowUV), ValueNoise(legacyFlowUV + 29.73)) * 2.0 - 1.0;
                float2 offset = (flowA * 0.55 + flowB * 0.3 + legacyFlow * 0.15) * _DistortionStrength * distortionMask * pulse;

                half edgeNoise = ValueNoise(input.positionWS.xz * (_FlowScaleB * 0.45) + _Time.y * _FlowSpeedB.xy * 0.35);
                half edgeBreakup = lerp(1.0, smoothstep(0.12, 1.0, edgeNoise), _EdgeBreakup);

                half3 sceneColor = SampleSceneColor(screenUV + offset);
                half alpha = saturate(_Alpha * distortionMask * edgeBreakup * (1.0 + (pulse - 1.0) * 0.35));
                half3 color = lerp(sceneColor, sceneColor * _TintColor.rgb, saturate(_TintStrength * _TintColor.a));
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
