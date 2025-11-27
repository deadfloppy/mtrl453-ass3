Shader "Custom/HelicoidDisplay"
{
    Properties
    {
        _PersistenceTex ("Persistence Buffer", 2D) = "white" {}
        _TimeTex ("Time Buffer", 2D) = "black" {}
        _DecayRate ("Decay Rate", Float) = 2.0
        _EmissionStrength ("Emission Strength", Float) = 3.0
        _CurrentTime ("Current Time", Float) = 0.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        
        Blend One One // Additive blending for bright glow
        ZWrite Off
        Cull Off // Render both sides
        
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
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };
            
            sampler2D _PersistenceTex;
            sampler2D _TimeTex;
            float _DecayRate;
            float _EmissionStrength;
            float _CurrentTime;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                // Sample persistence buffer (contains projected colors)
                float4 persistedColor = tex2D(_PersistenceTex, i.uv);
                
                // Sample time buffer (when was this pixel last hit)
                float lastHitTime = tex2D(_TimeTex, i.uv).r;
                
                // Use Unity's built-in time if _CurrentTime isn't set properly
                float currentTime = _CurrentTime > 0.001 ? _CurrentTime : _Time.y;
                
                // Calculate time since last projection
                float timeSinceHit = currentTime - lastHitTime;
                
                // Only apply decay if this pixel was actually hit (lastHitTime > -50)
                float persistence = 1.0;
                if (lastHitTime > -50.0)
                {
                    // Exponential decay
                    persistence = exp(-timeSinceHit * _DecayRate);
                    persistence = saturate(persistence);
                }
                else
                {
                    // Never been hit, completely transparent
                    persistence = 0.0;
                }
                
                // Apply persistence decay to color
                float4 finalColor = persistedColor * persistence;
                
                // Boost emission for visibility
                finalColor.rgb *= _EmissionStrength;
                
                // Only render bright pixels - discard dim ones to hide the surface
                float brightness = length(finalColor.rgb);
                if (brightness < 0.1)
                {
                    discard;
                }
                
                // Make it very bright and emissive
                finalColor.rgb *= 2.0;
                finalColor.a = saturate(brightness);
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    Fallback "Unlit/Transparent"
}
