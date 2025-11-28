Shader "Custom/AuraGlow"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0, 0.7, 1, 1)
        _OutlineWidth ("Outline Width", Range(0,0.05)) = 0.02
        _WobbleStrength ("Wobble Strength", Range(0,0.05)) = 0.01
        _WobbleSpeed ("Wobble Speed", Range(0,5)) = 1
        _Glow ("Glow Intensity", Range(0,5)) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        // ---------- Outline Pass ----------
        Pass
        {
            Name "OUTLINE"
            Cull Front    // 裏面を描く＝アウトライン

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _OutlineColor;
            float _OutlineWidth;
            float _WobbleStrength;
            float _WobbleSpeed;
            float _Glow;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // ノイズ揺れ
                float wobble = (sin(_Time.y * _WobbleSpeed + IN.positionOS.x * 20)
                              + cos(_Time.y * _WobbleSpeed + IN.positionOS.y * 20)) * 0.5;

                // 法線方向に膨張（アウトライン）
                float3 offset = IN.normalOS * (_OutlineWidth + wobble * _WobbleStrength);

                float3 posOS = IN.positionOS.xyz + offset;
                float4 posWS = TransformObjectToHClip(float4(posOS, 1));

                OUT.positionHCS = posWS;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                return _OutlineColor * _Glow;
            }
            ENDHLSL
        }

        // ---------- Base Mesh Pass ----------
        Pass
        {
            Name "BASE"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _MainColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                return _MainColor;
            }

            ENDHLSL
        }
    }
}
