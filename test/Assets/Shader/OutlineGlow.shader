Shader "Custom/OutlineGlow3D"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0, 0.5, 1, 1)
        _OutlineWidth ("Outline Width", Float) = 0.03
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        // =====================
        // 1. アウトラインパス
        // =====================
        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _OutlineColor;
            float _OutlineWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 pos = v.vertex.xyz + v.normal * _OutlineWidth;
                o.pos = UnityObjectToClipPos(float4(pos, 1.0));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }

        // =====================
        // 2. 本体 + 発光パス
        // =====================
        Pass
        {
            Name "BASE"
            Cull Back
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert2
            #pragma fragment frag2
            #include "UnityCG.cginc"

            float4 _MainColor;
            float4 _EmissionColor;

            struct appdata2
            {
                float4 vertex : POSITION;
            };

            struct v2f2
            {
                float4 pos : SV_POSITION;
            };

            v2f2 vert2 (appdata2 v)
            {
                v2f2 o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag2 (v2f2 i) : SV_Target
            {
                return _MainColor + _EmissionColor;
            }

            ENDCG
        }
    }

    FallBack Off
}
