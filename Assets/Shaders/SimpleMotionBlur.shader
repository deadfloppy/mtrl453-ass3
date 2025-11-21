Shader "Hidden/SimpleMotionBlur"
{
    Properties
    {
        _MainTex ("Current Frame", 2D) = "white" {}
        _PrevTex ("Previous Frame", 2D) = "black" {}
        _BlurAmount ("Blur Amount", Float) = 0.5
    }
    
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        
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
            sampler2D _PrevTex;
            float _BlurAmount;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                float4 current = tex2D(_MainTex, i.uv);
                float4 previous = tex2D(_PrevTex, i.uv);
                
                // Blend current frame with accumulated previous frames
                return lerp(current, previous, _BlurAmount);
            }
            ENDCG
        }
    }
}
