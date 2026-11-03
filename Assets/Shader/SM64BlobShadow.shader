Shader "SM64/BlobShadow"
{
    Properties
    {
        _Color ("Shadow Color", Color) = (0.04, 0.04, 0.08, 0.65)
        _Softness ("Edge Softness", Range(0.01, 1.0)) = 0.3
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent-10" 
            "RenderPipeline"="UniversalPipeline" 
            "IgnoreProjector"="True"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Offset -1, -1

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Softness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Distance from center (0.5, 0.5) mapped to [0, 1] at edge
                float2 centeredUV = (input.uv - 0.5) * 2.0;
                float dist = length(centeredUV);

                // Smooth circular falloff
                float edge = 1.0;
                float inner = max(0.0, edge - _Softness);
                float alpha = 1.0 - smoothstep(inner, edge, dist);

                clip(alpha - 0.001);

                return half4(_Color.rgb, _Color.a * alpha);
            }
            ENDHLSL
        }
    }
    
    // Fallback for built-in render pipeline
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent-10" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Offset -1, -1

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color;
            float _Softness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 centeredUV = (i.uv - 0.5) * 2.0;
                float dist = length(centeredUV);
                float edge = 1.0;
                float inner = max(0.0, edge - _Softness);
                float alpha = 1.0 - smoothstep(inner, edge, dist);
                clip(alpha - 0.001);
                return fixed4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }
}

