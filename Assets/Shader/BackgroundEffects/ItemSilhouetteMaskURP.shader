Shader "Hidden/Underwater/Item Silhouette Mask"
{
    Properties
    {
        _MaskBaseMap("Source alpha", 2D) = "white" {}
        _MaskST("Source UV scale/offset", Vector) = (1,1,0,0)
        _MaskRotation("glTF UV rotation", Vector) = (0,0,0,0)
        _MaskUVSet("Source UV set", Float) = 0
        _MaskAlpha("Source alpha factor", Float) = 1
        _MaskCutoff("Alpha cutoff (-1 = opaque)", Float) = -1
        _MaskCull("Source culling", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "ItemCoverage"
            Cull [_MaskCull]
            ZTest LEqual
            ZWrite On
            Blend Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            TEXTURE2D(_MaskBaseMap); SAMPLER(sampler_MaskBaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _MaskST, _MaskRotation;
                float _MaskAlpha, _MaskCutoff, _MaskUVSet;
            CBUFFER_END
            float4 _ItemOutlineColor;
            float _ItemOutlineWidth;
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float eyeDepth : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            struct Output
            {
                half4 color : SV_Target0;
                float2 depthWidth : SV_Target1;
            };
            // Project the original geometry without normal expansion or Fresnel shading.
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.eyeDepth = -pos.positionVS.z;
                float2 uv = _MaskUVSet > 0.5 ? input.uv1 : input.uv;
                output.uv = uv * _MaskST.xy + _MaskST.zw + uv.yx * _MaskRotation.xy;
                return output;
            }
            // Retain only visible coverage, respecting cutouts and opaque scene occlusion.
            Output Frag(Varyings input)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                if (_MaskCutoff >= 0)
                    clip(SAMPLE_TEXTURE2D(_MaskBaseMap, sampler_MaskBaseMap, input.uv).a * _MaskAlpha - _MaskCutoff);
                float rawDepth = LoadSceneDepth(uint2(input.positionCS.xy));
                float sceneDepth = unity_OrthoParams.w > 0.5
                    ? LinearDepthToEyeDepth(rawDepth) : LinearEyeDepth(rawDepth, _ZBufferParams);
                clip(sceneDepth + max(0.002, sceneDepth * 0.00005) - input.eyeDepth);
                Output output;
                output.color = _ItemOutlineColor;
                output.depthWidth = float2(input.eyeDepth, _ItemOutlineWidth);
                return output;
            }
            ENDHLSL
        }
    }
}
