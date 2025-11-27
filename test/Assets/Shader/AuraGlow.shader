Shader "Custom/AuraGlow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (0, 0.5, 1, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 10)) = 2
        _GlowSize ("Glow Size (pixels)", Range(1, 50)) = 10
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.3
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
        // グローエフェクトをレンダリング
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
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _GlowColor;
            float _GlowIntensity;
            float _GlowSize;
            float _PulseSpeed;
            float _PulseAmount;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            // グロー計算
            float GetGlowAlpha(float2 uv)
            {
                float alpha = 0;
                float totalWeight = 0;
                
                // サンプリング回数を5x5=25回に固定
                const int sampleRadius = 2; // (5-1)/2 = 2

                // _GlowSizeに応じてサンプリングする間隔を計算
                float2 sampleStep = _GlowSize / sampleRadius * _MainTex_TexelSize.xy;

                [unroll]
                for (int x = -sampleRadius; x <= sampleRadius; x++)
                {
                    [unroll]
                    for (int y = -sampleRadius; y <= sampleRadius; y++)
                    {
                        float2 offset = float2(x, y) * sampleStep;
                        
                        // 重み計算
                        float dist = length(float2(x, y));
                        float weight = exp(-(dist * dist) / (sampleRadius * sampleRadius));
                        
                        // テクスチャをサンプリング
                        float4 sampleColor = tex2D(_MainTex, uv + offset);
                        alpha += sampleColor.a * weight;
                        totalWeight += weight;
                    }
                }
                
                if (totalWeight > 0)
                {
                    return alpha / totalWeight;
                }
                return 0;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // 元のテクスチャサンプリング
                float4 originalColor = tex2D(_MainTex, i.uv);
                
                // グローのアルファ値を計算
                float glowAlpha = GetGlowAlpha(i.uv);
                
                // 元のアルファを引いて、アウトライン部分のみを取得
                float outlineAlpha = max(0, glowAlpha - originalColor.a * 0.9);
                
                // パルスアニメーション
                float pulse = 1 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                
                // グローカラーを設定
                float4 glowColor = _GlowColor;
                glowColor.rgb *= _GlowIntensity * pulse;
                glowColor.a = outlineAlpha;
                
                // 元の画像とグローを合成
                float4 finalColor = originalColor;
                finalColor.rgb = lerp(glowColor.rgb, originalColor.rgb, originalColor.a);
                finalColor.a = max(originalColor.a, outlineAlpha * glowColor.a);
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    FallBack "Sprites/Default"
}