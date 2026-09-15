// simple hardcoded lighting
Shader "Hidden/Cozy/Gizmos Billboard Unlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Scale ("Billboard Scale", Float) = 1.0
        _ZOffset ("_ZOffset", Float) = 0.001
        _OccludedAlpha ("_OccludedAlpha", Float) = 0.05
    }
    SubShader
    {
        Tags { "ForceSupported" = "True" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Fog { Mode Off }

        CGINCLUDE
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"

        struct appdata
        {
            float3 pos : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float4 pos : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        sampler2D _MainTex;
        float4 _GizmoBatchColor;
        float _ZOffset;
        float _Scale;
        float _OccludedAlpha;

        v2f vert (appdata i)
        {
            v2f o;
            float4 camPos = float4(UnityObjectToViewPos(float3(0, 0, 0)).xyz, 1.0);

            float4 viewDir = float4(i.pos.x, i.pos.y, i.pos.z, 0.0) * _Scale;
            float4 outPos = mul(UNITY_MATRIX_P, camPos + viewDir);

            o.pos = outPos;
            o.pos.z += _ZOffset;

            o.uv = i.uv;

            return o;
        }
        ENDCG

        Pass // regular pass
        {
            ZTest LEqual
            Offset 1, 1
            CGPROGRAM
            half4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * _GizmoBatchColor;
            }
            ENDCG
        }
        Pass // occluded pass
        {
            ZTest Greater
            CGPROGRAM
            half4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * _GizmoBatchColor * half4(1,1,1,_OccludedAlpha);
            }
            ENDCG
        }
    }
}
