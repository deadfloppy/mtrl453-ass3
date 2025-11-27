Shader "Custom/POVPersistenceHDRP"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _HistoryTex ("History", 2D) = "black" {}
        _Persistence ("Persistence", Range(0, 1)) = 0.9
    }

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            Name "POV Persistence"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            #pragma vertex Vert
            #pragma fragment Frag

            // Explicitly exclude stereo keywords
            #pragma skip_variants STEREO_INSTANCING_ON UNITY_SINGLE_PASS_STEREO STEREO_MULTIVIEW_ON STEREO_CUBEMAP_RENDER_ON

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord   : TEXCOORD0;
            };

            // Main source texture (camera output or previous pass)
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // History texture for persistence effect
            TEXTURE2D(_HistoryTex);
            SAMPLER(sampler_HistoryTex);

            float _Persistence;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                // Sample current frame
                float4 current = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // Sample history
                float4 history = SAMPLE_TEXTURE2D(_HistoryTex, sampler_HistoryTex, uv);

                // Blend current with history based on persistence value
                // Higher persistence = longer trails
                // result = max(current, history * persistence)
                float4 result = max(current, history * _Persistence);

                return result;
            }

            ENDHLSL
        }
    }
    Fallback Off
}
