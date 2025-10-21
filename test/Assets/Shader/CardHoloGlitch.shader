Shader "Custom/CardHoloGlitch"
{
    Properties
    {
        _Color("Base Color", Color) = (0,0.8,1,0.4)
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _ScrollSpeed("Scroll Speed", Vector) = (0.5,0.2,0,0)
        _GlitchIntensity("Glitch Intensity", Range(0,1)) = 0.3
        _LineWidth("Line Width", Range(0.01,0.5)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" } // 透明用
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha // 透過ブレンド
        ZWrite Off

        Pass
        {
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

            float4 _Color;
            sampler2D _NoiseTex;
            float2 _ScrollSpeed;
            float _GlitchIntensity;
            float _LineWidth;

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz); // 位置変換
                o.uv = v.uv; // UVを渡す
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv + _ScrollSpeed * _Time;
                float n = tex2D(_NoiseTex, uv).r;
            
                float xMask = 0.0;
                float yMask = 0.0;
                int maxLines = 10;
            
                for (int l = 0; l < maxLines; l++)
                {
                    // 線ごとのランダムシード
                    float seed = float(l) * 12.345 + dot(i.uv, float2(78.9, 45.6));
            
                    // ランダムに出現位置を決定（0〜1）
                    float xOffset = frac(sin(seed*7.1) * 43758.5453);
                    float yOffset = frac(sin(seed*13.7) * 12345.6789);
            
                    // ランダムに周波数・速度・幅・強度
                    float freq = lerp(5.0, 20.0, frac(sin(seed*1.3)*43758.5453));
                    float speedX = lerp(1.0, 8.0, frac(cos(seed*2.1)*24680.1357));
                    float speedY = lerp(1.0, 8.0, frac(sin(seed*2.7)*13579.2468));
                    float width = lerp(0.1, 0.5, frac(sin(seed*3.1)*98765.4321));
                    float intensity = lerp(0.1, 0.5, frac(cos(seed*4.2)*86420.8642));
            
                    // 線生成（UVにランダムオフセットを追加）
                    float xLine = frac(i.uv.x * freq + _Time * speedX + xOffset);
                    float yLine = frac(i.uv.y * freq + _Time * speedY + yOffset);
            
                    xMask += step(1.0 - width, xLine) * intensity;
                    yMask += step(1.0 - width, yLine) * intensity;
                }
            
                // ランダムフリッカー
                float flicker = step(0.6, frac(sin(dot(i.uv*1234.0,float2(12.34,56.78))) * 43758.5453 + _Time*5.0));
            
                float alpha = n + xMask + yMask;
                alpha *= flicker;
                alpha = saturate(alpha);
            
                return half4(_Color.rgb, _Color.a * alpha);
            }

            
            ENDHLSL
        }
    }
}
