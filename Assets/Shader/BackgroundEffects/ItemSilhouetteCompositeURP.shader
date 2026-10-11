Shader "Hidden/Underwater/Item Silhouette Composite"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "OuterSilhouetteOnly"
            Cull Off
            ZWrite Off
            ZTest Always
            Blend One One
            ColorMask RGB
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            TEXTURE2D_X(_ItemSilhouetteMask);
            TEXTURE2D_X_FLOAT(_ItemSilhouetteData);
            int _ItemOutlineRadius;

            // Dilate visible coverage and subtract its original area, never drawing over an item.
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                int2 pixel = int2(input.positionCS.xy);
                if (LOAD_TEXTURE2D_X(_ItemSilhouetteMask, pixel).a > 0) return 0;
                float rawDepth = LoadSceneDepth(pixel);
                float sceneDepth = unity_OrthoParams.w > 0.5
                    ? LinearDepthToEyeDepth(rawDepth) : LinearEyeDepth(rawDepth, _ZBufferParams);
                half4 result = 0;
                float nearest = 100;
                int radius = clamp(_ItemOutlineRadius, 1, 4);
                [loop] for (int y = -radius; y <= radius; y++)
                {
                    [loop] for (int x = -radius; x <= radius; x++)
                    {
                        float distanceSquared = x * x + y * y;
                        if (distanceSquared >= nearest || distanceSquared == 0) continue;
                        int2 samplePixel = pixel + int2(x, y);
                        if (any(samplePixel < 0) || any(samplePixel >= int2(_ScaledScreenParams.xy))) continue;
                        half4 color = LOAD_TEXTURE2D_X(_ItemSilhouetteMask, samplePixel);
                        if (color.a <= 0) continue;
                        float2 depthWidth = LOAD_TEXTURE2D_X(_ItemSilhouetteData, samplePixel).rg;
                        float coverage = saturate(depthWidth.y + 0.5 - sqrt(distanceSquared));
                        // Also test the destination so an edge cannot spill across a foreground wall.
                        if (coverage <= 0 || depthWidth.x > sceneDepth + max(0.002, sceneDepth * 0.00005)) continue;
                        nearest = distanceSquared;
                        // Premultiply before blending so an LDR target does not clamp HDR tint first.
                        result = half4(color.rgb * (color.a * coverage), 0);
                    }
                }
                return result;
            }
            ENDHLSL
        }
    }
}
