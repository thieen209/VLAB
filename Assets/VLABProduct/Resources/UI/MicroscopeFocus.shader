Shader "VLAB/Illustrative Microscope Focus"
{
    Properties
    {
        [PerRendererData] _MainTex("Source schematic",2D)="white"{}
        _Color("Tint",Color)=(1,1,1,1)
        _BlurPixels("Illustrative blur radius",Float)=0
        _UvBounds("Visible source region",Vector)=(0,0,1,1)
        _StencilComp("Stencil Comparison",Float)=8
        _Stencil("Stencil ID",Float)=0
        _StencilOp("Stencil Operation",Float)=0
        _StencilWriteMask("Stencil Write Mask",Float)=255
        _StencilReadMask("Stencil Read Mask",Float)=255
        _ColorMask("Color Mask",Float)=15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip",Float)=0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="False"}
        Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct v2f {float4 vertex:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;float4 local:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
            sampler2D _MainTex;float4 _MainTex_TexelSize,_UvBounds,_ClipRect;fixed4 _Color;float _BlurPixels;
            v2f vert(appdata input)
            {
                v2f output;UNITY_SETUP_INSTANCE_ID(input);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.local=input.vertex;output.vertex=UnityObjectToClipPos(input.vertex);output.uv=input.uv;output.color=input.color*_Color;return output;
            }
            fixed4 SampleSource(float2 uv){return tex2D(_MainTex,clamp(uv,_UvBounds.xy+_MainTex_TexelSize.xy*.5,_UvBounds.zw-_MainTex_TexelSize.xy*.5));}
            fixed4 frag(v2f input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 radius=_MainTex_TexelSize.xy*_BlurPixels;
                fixed4 color=SampleSource(input.uv)*.25;
                color+=(SampleSource(input.uv+float2(radius.x,0))+SampleSource(input.uv-float2(radius.x,0))+SampleSource(input.uv+float2(0,radius.y))+SampleSource(input.uv-float2(0,radius.y)))*.125;
                color+=(SampleSource(input.uv+radius)+SampleSource(input.uv-radius)+SampleSource(input.uv+float2(radius.x,-radius.y))+SampleSource(input.uv+float2(-radius.x,radius.y)))*.0625;
                color*=input.color;
                #ifdef UNITY_UI_CLIP_RECT
                color.a*=UnityGet2DClipping(input.local.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a-.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
    Fallback Off
}
