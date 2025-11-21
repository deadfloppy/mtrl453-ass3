Shader "Hidden/SimpleBloom"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BloomTex ("Bloom", 2D) = "black" {}
        _Threshold ("Threshold", Float) = 0.5
        _Intensity ("Intensity", Float) = 1.0
    }
    
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        
        // Pass 0: Extract bright pixels
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
            float _Threshold;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                
                // Extract bright pixels above threshold
                float brightness = max(col.r, max(col.g, col.b));
                float contribution = max(0, brightness - _Threshold);
                contribution /= max(brightness, 0.00001);
                
                return col * contribution;
            }
            ENDCG
        }
        
        // Pass 1: Gaussian blur
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
            float2 _BlurOffset;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                // 5-tap Gaussian blur
                float4 col = float4(0, 0, 0, 0);
                col += tex2D(_MainTex, i.uv - _BlurOffset * 2.0) * 0.06;
                col += tex2D(_MainTex, i.uv - _BlurOffset) * 0.24;
                col += tex2D(_MainTex, i.uv) * 0.40;
                col += tex2D(_MainTex, i.uv + _BlurOffset) * 0.24;
                col += tex2D(_MainTex, i.uv + _BlurOffset * 2.0) * 0.06;
                return col;
            }
            ENDCG
        }
        
        // Pass 2: Combine original + bloom
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
            sampler2D _BloomTex;
            float _Intensity;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                float4 original = tex2D(_MainTex, i.uv);
                float4 bloom = tex2D(_BloomTex, i.uv);
                
                // Additive blend with intensity
                return original + bloom * _Intensity;
            }
            ENDCG
        }
        
        // Pass 3: Show bloom only (debug)
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
            
            sampler2D _BloomTex;
            float _Intensity;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                float4 bloom = tex2D(_BloomTex, i.uv);
                return bloom * _Intensity;
            }
            ENDCG
        }
    }
}
