// Full-screen cel shading for URP (Unity 6 / URP 14+).
// Use with a "Full Screen Pass Renderer Feature" (Requirements: Color + Depth + Normal).
Shader "Hidden/Fullscreen/ToonShadeFullscreen"
{
    Properties
    {
        _Color("Tint", Color) = (1, 1, 1, 1)
        _ShadeSize("Shade Size", Range(0.05, 1)) = 0.39
        _Min("Darkest Band", Range(0, 1)) = 0.3
        _Max("Brightest Band", Range(0, 2)) = 1
        _Strength("Effect Strength", Range(0, 1)) = 1
        [Toggle]_AffectSky("Affect Sky", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ToonShadeFullscreen"
            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _Color;
            float _ShadeSize;
            float _Min;
            float _Max;
            float _Strength;
            float _AffectSky;

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                float4 scene = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                // Skip the sky / empty pixels (no valid normals there)
                float depth = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                    bool isSky = depth <= 0.0001;
                #else
                    bool isSky = depth >= 0.9999;
                #endif
                if (isSky && _AffectSky < 0.5)
                    return scene;

                // World-space normal from the depth-normals texture
                float3 normalWS = normalize(SampleSceneNormals(uv));

                // Same math as the original graph
                float3 lightDir = _MainLightPosition.xyz;           // main light direction
                float ndl = dot(normalWS, lightDir);                // Dot Product
                float lit = ndl * 0.5 + 0.5;                        // Remap (-1,1) -> (0,1)

                float size = max(_ShadeSize, 0.001);
                float band = floor(lit / size);                     // Divide + Floor

                // Remap (0, 1/size) -> (Min, Max)
                float maxBand = 1.0 / size;
                float shade = _Min + (band - 0.0) * (_Max - _Min) / maxBand;

                float3 toon = scene.rgb * shade * _Color.rgb;
                return float4(lerp(scene.rgb, toon, _Strength), scene.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
