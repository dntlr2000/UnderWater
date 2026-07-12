Shader "Underwater/Effects/Inverted Hull Rim Glow"
{
    Properties
    {
        [HDR]_RimColor ("Rim Color", Color) = (0.2, 1.2, 1.4, 1)
        _ShellOffset ("Shell Offset", Range(0, 0.1)) = 0.015
        _RimPower ("Rim Power", Range(0.25, 8)) = 3
        _RimThreshold ("Rim Threshold", Range(0, 1)) = 0.2
        _RimSoftness ("Rim Softness", Range(0.001, 1)) = 0.2
        _Intensity ("Intensity", Range(0, 10)) = 2
        _Alpha ("Alpha", Range(0, 1)) = 0.7
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
            Name "InvertedHullRimGlow"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _RimColor;
                float _ShellOffset;
                float _RimPower;
                float _RimThreshold;
                float _RimSoftness;
                float _Intensity;
                float _Alpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS += normalWS * _ShellOffset;

                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirWS = normalize(input.viewDirWS);
                half facing = saturate(abs(dot(normalWS, viewDirWS)));
                half rim = pow(1.0 - facing, _RimPower);
                half rimEnd = min(_RimThreshold + max(_RimSoftness, 0.001), 1.001);
                half edgeMask = smoothstep(_RimThreshold, rimEnd, rim);
                clip(edgeMask - 0.001);

                half alpha = saturate(edgeMask * _Alpha);
                half3 color = _RimColor.rgb * edgeMask * _Intensity;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
