Shader "Underwater/Hair Baked URP"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Color", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _AlphaMap ("Alpha Mask", 2D) = "white" {}
        _UseAlphaMap ("Use Alpha Mask", Range(0, 1)) = 0
        _AlphaStrength ("Alpha Strength", Range(0, 4)) = 2
        _Cutoff ("Shadow / Hard Cutoff", Range(0, 1)) = 0.02

        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0, 2)) = 0.6

        _Smoothness ("Smoothness", Range(0, 1)) = 0.65
        _SpecColor ("Specular Color", Color) = (0.35, 0.28, 0.22, 1)
        _SpecularStrength ("Specular Strength", Range(0, 2)) = 0.45
        _ShadowStrength ("Receive Shadow Strength", Range(0, 1)) = 0

        _FresnelColor ("Rim / Translucent Color", Color) = (0.65, 0.32, 0.18, 1)
        _FresnelPower ("Rim Power", Range(0.5, 8)) = 3.5
        _TranslucentStrength ("Translucent Strength", Range(0, 1)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Cull Off
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_AlphaMap);
            SAMPLER(sampler_AlphaMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _AlphaMap_ST;
                float4 _BumpMap_ST;
                float4 _BaseColor;
                float4 _SpecColor;
                float4 _FresnelColor;
                float _UseAlphaMap;
                float _AlphaStrength;
                float _Cutoff;
                float _BumpScale;
                float _Smoothness;
                float _SpecularStrength;
                float _ShadowStrength;
                float _FresnelPower;
                float _TranslucentStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 tangentWS : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float2 uv : TEXCOORD4;
                float fogFactor : TEXCOORD5;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = normalInputs.tangentWS;
                output.bitangentWS = normalInputs.bitangentWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half alphaMask = SAMPLE_TEXTURE2D(_AlphaMap, sampler_AlphaMap, TRANSFORM_TEX(input.uv, _AlphaMap)).r;
                half alpha = lerp(1.0h, saturate(alphaMask * _AlphaStrength), saturate(_UseAlphaMap)) * baseSample.a;

                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, TRANSFORM_TEX(input.uv, _BumpMap)), _BumpScale);
                half3x3 tangentToWorld = half3x3(normalize(input.tangentWS), normalize(input.bitangentWS), normalize(input.normalWS));
                half3 normalWS = normalize(mul(normalTS, tangentToWorld));

                half3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half shadow = lerp(1.0h, mainLight.shadowAttenuation, _ShadowStrength);

                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half3 ambient = SampleSH(normalWS);
                // 머리카락 카드는 얇고 겹쳐서 자기 그림자가 쉽게 깨집니다.
                // ShadowStrength로 그림자 영향만 별도 조절합니다.
                half3 diffuse = baseSample.rgb * (ambient + mainLight.color * ndotl * shadow);

                half3 halfDir = normalize(mainLight.direction + viewDirWS);
                half specPower = lerp(16.0h, 160.0h, _Smoothness);
                half spec = pow(saturate(dot(normalWS, halfDir)), specPower) * _SpecularStrength * shadow;
                half3 specular = spec * _SpecColor.rgb * mainLight.color;

                // Blender의 Translucent/Fresnel 느낌을 약한 림 라이트로 흉내냅니다.
                half fresnel = pow(1.0h - saturate(dot(normalWS, viewDirWS)), _FresnelPower);
                half3 translucent = fresnel * _FresnelColor.rgb * _TranslucentStrength;

                half3 color = diffuse + specular + translucent;
                color = MixFog(color, input.fogFactor);

                return half4(color, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_AlphaMap);
            SAMPLER(sampler_AlphaMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _AlphaMap_ST;
                float4 _BaseColor;
                float _UseAlphaMap;
                float _AlphaStrength;
                float _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half baseAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                half alphaMask = SAMPLE_TEXTURE2D(_AlphaMap, sampler_AlphaMap, TRANSFORM_TEX(input.uv, _AlphaMap)).r;
                half alpha = lerp(1.0h, saturate(alphaMask * _AlphaStrength), saturate(_UseAlphaMap)) * baseAlpha;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}
