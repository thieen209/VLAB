Shader "Hidden/VLAB/PhotoBlur"
{
    Properties { _MainTex ("Room photograph", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float2 _Direction;
            float4 SampleRoom(float2 uv) { return tex2D(_MainTex, float2(frac(uv.x), saturate(uv.y))); }
            fixed4 frag(v2f_img input) : SV_Target
            {
                float2 uv=input.uv;
                return SampleRoom(uv)*.227027027
                    +(SampleRoom(uv+_Direction*1.384615385)+SampleRoom(uv-_Direction*1.384615385))*.316216216
                    +(SampleRoom(uv+_Direction*3.230769231)+SampleRoom(uv-_Direction*3.230769231))*.070270270;
            }
            ENDCG
        }
    }
    Fallback Off
}
