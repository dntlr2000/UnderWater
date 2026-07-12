Shader "Underwater/Submerged Mud URP"
{
    Properties
    {
        [MainColor] _BaseColor ("Wet Mud Color", Color) = (0.19, 0.13, 0.09, 1)
        _DeepColor ("Dark Crevice Color", Color) = (0.055, 0.04, 0.03, 1)
        _WaterTint ("Underwater Tint", Color) = (0.06, 0.22, 0.24, 1)

        _NoiseScale ("Mud Noise Scale", Range(1, 80)) = 18
        _NoiseStrength ("Mud Noise Strength", Range(0, 1)) = 0.45
        _PatchScale ("Large Patch Scale", Range(0.1, 20)) = 3
        _PatchStrength ("Large Patch Strength", Range(0, 1)) = 0.35

        _Wetness ("Wetness", Range(0, 1)) = 0.8
        _Smoothness ("Wet Smoothness", Range(0, 1)) = 0.62
        _SpecularStrength ("Specular Strength", Range(0, 2)) = 0.35

        _RippleStrength ("Surface Ripple Normal", Range(0, 1)) = 0.08
        _RippleScale ("Surface Ripple Scale", Range(1, 80)) = 24
        _RippleSpeed ("Surface Ripple Speed", Range(0, 3)) = 0.35

        _WaterFogStrength ("Water Fog Color Strength", Range(0, 1)) = 0.25
        _RimWetHighlight ("Low Angle Wet Highlight", Range(0, 1)) = 0.12
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

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

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _DeepColor;
                float4 _WaterTint;
                float _NoiseScale;
                float _NoiseStrength;
                float _PatchScale;
                float _PatchStrength;
                float _Wetness;
                float _Smoothness;
                float _SpecularStrength;
                float _RippleStrength;
                float _RippleScale;
                float _RippleSpeed;
                float _WaterFogStrength;
                float _RimWetHighlight;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
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
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 uv)
            {
                float value = 0;
                float amplitude = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    value += ValueNoise(uv) * amplitude;
                    uv *= 2.03;
                    amplitude *= 0.5;
                }
                return value;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 tangentWS = normalize(cross(abs(normalWS.y) > 0.95 ? float3(1, 0, 0) : float3(0, 1, 0), normalWS));
                float3 bitangentWS = normalize(cross(normalWS, tangentWS));

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalWS;
                output.tangentWS = tangentWS;
                output.bitangentWS = bitangentWS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 mudUV = input.uv;

                float fineMud = Fbm(mudUV * _NoiseScale);
                float largePatch = Fbm(mudUV * _PatchScale);
                float mudMask = saturate((fineMud - 0.45) * 2.2 * _NoiseStrength + (largePatch - 0.5) * _PatchStrength + 0.5);

                half3 mudColor = lerp(_DeepColor.rgb, _BaseColor.rgb, mudMask);

                // 젖은 흙은 마른 흙보다 어둡고 물빛이 살짝 섞여 보입니다.
                mudColor = lerp(mudColor, mudColor * 0.55, _Wetness * 0.45);
                mudColor = lerp(mudColor, _WaterTint.rgb, _WaterFogStrength);

                float rippleTime = _Time.y * _RippleSpeed;
                float rippleA = Fbm(mudUV * _RippleScale + float2(rippleTime, rippleTime * 0.37));
                float rippleB = Fbm(mudUV * _RippleScale + float2(0.06, 0.02) + float2(rippleTime, rippleTime * 0.37));
                float rippleC = Fbm(mudUV * _RippleScale + float2(0.02, 0.06) + float2(rippleTime, rippleTime * 0.37));
                half3 normalTS = normalize(half3((rippleA - rippleB) * _RippleStrength, (rippleA - rippleC) * _RippleStrength, 1));
                half3x3 tangentToWorld = half3x3(normalize(input.tangentWS), normalize(input.bitangentWS), normalize(input.normalWS));
                half3 normalWS = normalize(mul(normalTS, tangentToWorld));

                half3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half3 ambient = SampleSH(normalWS);
                half3 diffuse = mudColor * (ambient + mainLight.color * ndotl * mainLight.shadowAttenuation);

                half3 halfDir = normalize(mainLight.direction + viewDirWS);
                half specPower = lerp(12.0h, 120.0h, _Smoothness);
                half wetSpec = pow(saturate(dot(normalWS, halfDir)), specPower) * _SpecularStrength * _Wetness * mainLight.shadowAttenuation;

                // 낮은 시선각에서 젖은 표면이 살짝 번지는 느낌을 추가합니다.
                half fresnel = pow(1.0h - saturate(dot(normalWS, viewDirWS)), 4.0h) * _RimWetHighlight * _Wetness;
                half3 color = diffuse + (wetSpec + fresnel) * _WaterTint.rgb;
                color = MixFog(color, input.fogFactor);

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
