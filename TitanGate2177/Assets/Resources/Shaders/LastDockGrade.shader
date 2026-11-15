// LAST DOCK full-screen grade (built-in pipeline, no post-processing package): soft glow on bright pixels,
// a warm/cool tint, a vignette, desaturation when the base is dark, an orange storm cast and a red danger pulse.
Shader "Hidden/TitanGate/LastDockGrade"
{
    Properties { _MainTex ("", 2D) = "white" {} }
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
            float4 _MainTex_TexelSize;
            float _Vignette, _Danger, _Dark, _Storm, _Glow;
            float4 _Tint;

            fixed3 Bright(float2 uv) { return max(tex2D(_MainTex, uv).rgb - 0.78, 0); }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                fixed4 c = tex2D(_MainTex, uv);
                float2 o = _MainTex_TexelSize.xy * 4;
                fixed3 g = Bright(uv + float2(o.x, 0)) + Bright(uv - float2(o.x, 0)) + Bright(uv + float2(0, o.y)) + Bright(uv - float2(0, o.y))
                         + Bright(uv + o) + Bright(uv - o) + Bright(uv + float2(o.x, -o.y)) + Bright(uv + float2(-o.x, o.y));
                c.rgb += g * _Glow * 0.18;
                float l = dot(c.rgb, float3(0.299, 0.587, 0.114));
                // gentle grade: cool shadows, warm highlights
                float3 graded = lerp(c.rgb * float3(0.92, 0.97, 1.08), c.rgb * float3(1.06, 1.0, 0.92), saturate(l * 1.4));
                c.rgb = lerp(c.rgb, graded * _Tint.rgb, 0.6);
                c.rgb = lerp(c.rgb, l.xxx * float3(0.8, 0.85, 1.0), _Dark * 0.55);
                c.rgb *= 1 - _Dark * 0.3;
                c.rgb = lerp(c.rgb, c.rgb * float3(1.18, 0.82, 0.62), _Storm * 0.4);
                float2 d = uv - 0.5;
                float v = saturate(1 - dot(d, d) * _Vignette);
                c.rgb *= lerp(1, v, 0.85);
                c.rgb = lerp(c.rgb, float3(0.75, 0.04, 0.06), _Danger * (1 - v) * 0.85);
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
