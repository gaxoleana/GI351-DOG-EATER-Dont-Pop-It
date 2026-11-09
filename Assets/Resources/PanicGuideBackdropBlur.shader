Shader "Hidden/PanicGuideBackdropBlur"
{
    Properties { _MainTex ("Background", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Direction;
            fixed4 frag(v2f_img input) : SV_Target
            {
                float2 d = _Direction.xy;
                fixed4 color = tex2D(_MainTex, input.uv) * 0.227027;
                color += tex2D(_MainTex, input.uv + d * 1.384615) * 0.316216;
                color += tex2D(_MainTex, input.uv - d * 1.384615) * 0.316216;
                color += tex2D(_MainTex, input.uv + d * 3.230769) * 0.070270;
                color += tex2D(_MainTex, input.uv - d * 3.230769) * 0.070270;
                return color;
            }
            ENDHLSL
        }
    }
}
