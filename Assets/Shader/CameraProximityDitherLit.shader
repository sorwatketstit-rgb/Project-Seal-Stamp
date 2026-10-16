Shader "Custom/URP/CameraProximityDitherLit"
{
    Properties
    {
        [Header(Surface Options)]
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _Color("Legacy Color Alias", Color) = (1, 1, 1, 1)
        _Metallic("Metallic", Range(0, 1)) = 0.0
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        _BumpMap("Normal Map", 2D) = "bump" {}

        [Header(Emission)]
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionMap("Emission Map", 2D) = "white" {}

        [Header(Proximity Fade Settings)]
        _FadeNearDistance("Fade Near Distance (0 = Global)", Float) = 0.0
        _FadeFarDistance("Fade Far Distance (0 = Global)", Float) = 0.0
        _AlphaMultiplier("Alpha Multiplier", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // Global variables updated by SM64CameraController
        float _GlobalCameraFadeNear;
        float _GlobalCameraFadeFar;

        // 4x4 Bayer Matrix for high-performance screen-door dithering
        static const float Bayer4x4[16] = {
             0.0 / 16.0,  8.0 / 16.0,  2.0 / 16.0, 10.0 / 16.0,
            12.0 / 16.0,  4.0 / 16.0, 14.0 / 16.0,  6.0 / 16.0,
             3.0 / 16.0, 11.0 / 16.0,  1.0 / 16.0,  9.0 / 16.0,
            15.0 / 16.0,  7.0 / 16.0, 13.0 / 16.0,  5.0 / 16.0
        };

        float GetDitherThreshold(float2 screenPos)
        {
            uint2 pixelCoord = uint2(screenPos.xy) % 4;
            return Bayer4x4[pixelCoord.y * 4 + pixelCoord.x];
        }

        void ApplyCameraProximityDither(float3 positionWS, float4 positionCS, float localNear, float localFar, float alphaMult)
        {
            float dist = length(GetCameraPositionWS() - positionWS);

            float nearDist = localNear > 0.001 ? localNear : _GlobalCameraFadeNear;
            float farDist = localFar > 0.001 ? localFar : _GlobalCameraFadeFar;

            if (farDist > nearDist && nearDist >= 0.0)
            {
                float fade = saturate((dist - nearDist) / max(0.0001, farDist - nearDist));
                fade *= alphaMult;
                float threshold = GetDitherThreshold(positionCS.xy);
                clip(fade - threshold);
            }
        }
        ENDHLSL

        // =========================================================================
        // Pass 1: Forward Lit (Main Render Pass)
        // =========================================================================
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float2 uv : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _Color;
                float4 _BaseMap_ST;
                float4 _EmissionColor;
                float _Metallic;
                float _Smoothness;
                float _FadeNearDistance;
                float _FadeFarDistance;
                float _AlphaMultiplier;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // Dithered proximity fade clip
                ApplyCameraProximityDither(input.positionWS, input.positionCS, _FadeNearDistance, _FadeFarDistance, _AlphaMultiplier);

                // Sample base texture and evaluate color tint safely
                float4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                
                // Pick whichever color property is non-zero (supports both _BaseColor and _Color)
                float4 tint = _BaseColor;
                if (tint.r == 0 && tint.g == 0 && tint.b == 0 && tint.a == 0)
                {
                    tint = _Color;
                }
                if (tint.r == 0 && tint.g == 0 && tint.b == 0 && tint.a == 0)
                {
                    tint = float4(1, 1, 1, 1);
                }

                float4 albedo = baseTex * tint;

                // Normal vector
                float3 normalWS = normalize(input.normalWS);

                // Safe main light evaluation
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                    Light mainLight = GetMainLight(shadowCoord);
                    float shadow = mainLight.shadowAttenuation;
                #else
                    Light mainLight = GetMainLight();
                    float shadow = 1.0;
                #endif

                // Half-Lambert stylized diffuse lighting (ensures smooth falloff, never pitch black)
                float NdotL = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
                float3 mainDiffuse = mainLight.color * (NdotL * shadow);

                // Ambient light with baseline so objects are clearly visible in any scene / preview
                float3 ambient = SampleSH(normalWS);
                float3 minAmbient = float3(0.3, 0.3, 0.3);
                ambient = max(ambient, minAmbient);

                float3 totalLighting = mainDiffuse + ambient;

                // Additional lights (point / spot lights)
                #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < pixelLightCount; ++i)
                {
                    Light addLight = GetAdditionalLight(i, input.positionWS);
                    float addNdotL = saturate(dot(normalWS, addLight.direction));
                    totalLighting += addLight.color * (addNdotL * addLight.distanceAttenuation * addLight.shadowAttenuation);
                }
                #endif

                // Specular highlight
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specularTerm = pow(NdotH, max(1.0, _Smoothness * 64.0)) * _Metallic;
                float3 specular = mainLight.color * (specularTerm * shadow);

                // Emission
                float3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb;

                // Final color composition
                float3 finalColor = albedo.rgb * totalLighting + specular + emission;
                return float4(finalColor, albedo.a);
            }
            ENDHLSL
        }

        // =========================================================================
        // Pass 2: Shadow Caster
        // =========================================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _Color;
                float _FadeNearDistance;
                float _FadeFarDistance;
                float _AlphaMultiplier;
            CBUFFER_END

            float3 _LightDirection;

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return output;
            }

            float4 ShadowFrag(Varyings input) : SV_Target
            {
                ApplyCameraProximityDither(input.positionWS, input.positionCS, _FadeNearDistance, _FadeFarDistance, _AlphaMultiplier);
                return 0;
            }
            ENDHLSL
        }

        // =========================================================================
        // Pass 3: DepthOnly
        // =========================================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _Color;
                float _FadeNearDistance;
                float _FadeFarDistance;
                float _AlphaMultiplier;
            CBUFFER_END

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            float4 DepthFrag(Varyings input) : SV_Target
            {
                ApplyCameraProximityDither(input.positionWS, input.positionCS, _FadeNearDistance, _FadeFarDistance, _AlphaMultiplier);
                return 0;
            }
            ENDHLSL
        }

        // =========================================================================
        // Pass 4: DepthNormals
        // =========================================================================
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag

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
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _Color;
                float _FadeNearDistance;
                float _FadeFarDistance;
                float _AlphaMultiplier;
            CBUFFER_END

            Varyings DepthNormalsVert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            float4 DepthNormalsFrag(Varyings input) : SV_Target
            {
                ApplyCameraProximityDither(input.positionWS, input.positionCS, _FadeNearDistance, _FadeFarDistance, _AlphaMultiplier);
                return float4(normalize(input.normalWS), 0.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
