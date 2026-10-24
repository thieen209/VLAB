Shader "VLAB/Menu Circular Frost"
{
    Properties { [PerRendererData] _MainTex("Room photograph",2D)="white"{} }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            sampler2D _MainTex;float3 _Ray0,_Ray1,_Ray2,_Ray3;
            struct A{float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;UNITY_VERTEX_OUTPUT_STEREO};
            V Vert(A v){V o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            fixed4 Frag(V i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float alpha=saturate((.5-length(i.uv-.5))*84);clip(alpha-.001);
                float3 d=normalize(lerp(lerp(_Ray0,_Ray3,i.uv.x),lerp(_Ray1,_Ray2,i.uv.x),i.uv.y));
                float2 uv=float2(.75-atan2(d.z,d.x)/6.2831853,1-acos(clamp(d.y,-1,1))/3.14159265);
                fixed3 c=tex2D(_MainTex,uv).rgb*4;
                c+=tex2D(_MainTex,uv+float2(.003,0)).rgb+tex2D(_MainTex,uv-float2(.003,0)).rgb;
                c+=tex2D(_MainTex,uv+float2(0,.005)).rgb+tex2D(_MainTex,uv-float2(0,.005)).rgb;
                c+=tex2D(_MainTex,uv+float2(.004,.006)).rgb+tex2D(_MainTex,uv+float2(-.004,.006)).rgb;
                c+=tex2D(_MainTex,uv+float2(.004,-.006)).rgb+tex2D(_MainTex,uv-float2(.004,.006)).rgb;
                return fixed4(lerp(c/12,fixed3(.045,.09,.12),.55),alpha*i.color.a);
            }
            ENDHLSL
        }
    }
}
