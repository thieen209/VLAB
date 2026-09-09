Shader "VLAB/DemoLabs/OnionSpecimen"
{
    Properties { _MainTex("Onion epidermis", 2D) = "white" {} _Blur("Defocus", Float) = 0.1 _Brightness("Light", Float) = .7 _Prepared("Prepared", Float) = 0 _Annotated("Annotated", Float) = 0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Cull Off ZWrite On
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Blur, _Brightness, _Prepared;
            Varyings vert(Attributes v) { Varyings o; o.positionCS=UnityObjectToClipPos(v.positionOS); o.uv=v.uv; return o; }
            half4 frag(Varyings v):SV_Target
            {
                float footprint=max(length(ddx(v.uv*_MainTex_TexelSize.zw)),length(ddy(v.uv*_MainTex_TexelSize.zw)));
                float lod=max(0,log2(max(footprint,1))) + _Blur*65;
                float3 col=tex2Dlod(_MainTex,float4(v.uv,0,lod)).rgb;
                float vignette=1-smoothstep(.68,1.45,length((v.uv-.5)*2));
                col*=lerp(.08,1.23,_Brightness)*lerp(.84,1,vignette);
                return half4(lerp(float3(.035,.045,.05),col,_Prepared),1);
            }
            ENDCG
        }
    }
}
