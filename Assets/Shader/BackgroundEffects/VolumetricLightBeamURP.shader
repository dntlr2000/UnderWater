Shader "Underwater/Background Effects/Volumetric Light Beam"
{
    Properties
    {
        [HDR]_BeamColor ("Beam Color", Color) = (0.35, 1.1, 1.35, 1)
        _Intensity ("Intensity", Range(0, 8)) = 1.8
        _Alpha ("Alpha", Range(0, 1)) = 0.18
        _VerticalFadePower ("Vertical Fade Power", Range(0.1, 8)) = 2.2
        _EdgeFadePower ("Edge Fade Power", Range(0.1, 8)) = 2
        _NoiseScale ("Noise Scale", Range(0.1, 30)) = 5
        _NoiseSpeed ("Noise Speed", Range(0, 2)) = 0.06
        _NoiseContrast ("Noise Contrast", Range(0, 2)) = 0.65
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
            Name "VolumetricLightBeam"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BeamColor;
                float _Intensity;
                float _Alpha;
                float _VerticalFadePower;
                float _EdgeFadePower;
                float _NoiseScale;
                float _NoiseSpeed;
                float _NoiseContrast;
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
                float3 viewDirWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.45));
                p += dot(p, p + 23.345);
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

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirWS = normalize(input.viewDirWS);

                half verticalFade = pow(saturate(1.0 - input.uv.y), _VerticalFadePower);
                half centerFade = pow(saturate(1.0 - abs(input.uv.x - 0.5) * 2.0), _EdgeFadePower);
                half viewFade = saturate(1.0 - abs(dot(normalWS, viewDirWS)) * 0.35);

                float t = _Time.y * _NoiseSpeed;
                float noise = ValueNoise(input.uv * _NoiseScale + float2(t * 0.35, -t));
                noise = lerp(1.0, noise, _NoiseContrast);

                half alpha = saturate(verticalFade * centerFade * viewFade * noise * _Alpha);
                half3 color = _BeamColor.rgb * alpha * _Intensity;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
