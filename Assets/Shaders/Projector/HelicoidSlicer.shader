Shader "Custom/HelicoidSlicer"
{
    Properties
    {
        _SliceHeight ("Slice Height", Float) = 0
        _SliceThickness ("Slice Thickness", Float) = 0.01
        _BaseColor ("Base Color", Color) = (1,1,1,1)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="HDRenderPipeline" }
        
        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "Forward" }
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                float _SliceHeight;
                float _SliceThickness;
                float4 _BaseColor;
            CBUFFER_END
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                
                return output;
            }
            
            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                // Only render pixels within slice thickness of target height
                float dist = abs(input.positionWS.y - _SliceHeight);
                if (dist > _SliceThickness)
                    discard;
                
                return _BaseColor;
            }
            ENDHLSL
        }
    }
    
    Fallback "Hidden/HDRP/Fallback"
}