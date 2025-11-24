Shader "Hidden/TemporalMotionBlur"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass    
        {
            Name "MotionBlurAccumulation"

            HLSLPROGRAM
            #pragma vertex VertexSimple
            #pragma fragment FragmentAccumulate
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_AccumulationTex);
            SAMPLER(sampler_AccumulationTex);

            float _BlendFactor;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            Varyings VertexSimple(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            half4 FragmentAccumulate(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                half4 currentFrame = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half4 accumulatedFrame = SAMPLE_TEXTURE2D(_AccumulationTex, sampler_AccumulationTex, uv);

                half4 result = lerp(currentFrame, accumulatedFrame, _BlendFactor);

                return result;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
