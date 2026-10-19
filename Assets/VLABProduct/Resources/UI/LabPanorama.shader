Shader "VLAB/Static Lab Panorama"
{
    Properties { _MainTex("Room photograph",2D)="black"{} _Opacity("Preview fade",Range(0,1))=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "StaticPanorama"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;float _Opacity;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings {float4 positionCS:SV_POSITION;float3 direction:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
            Varyings Vert(Attributes input)
            {
                Varyings output;UNITY_SETUP_INSTANCE_ID(input);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);output.direction=input.positionOS.xyz;return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 direction=normalize(input.direction);
                float2 uv=float2(.75-atan2(direction.z,direction.x)/(2*PI),1-acos(clamp(direction.y,-1,1))/PI);
                return half4(SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).rgb,_Opacity);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
