// LAST DOCK menu, live 3D: lit surfaces (the Kestrel and the asteroids). Lighting is computed here from the
// menu sun, fill and Mars bounce (global _ML* values), so no scene light or shadow setting can change it.
Shader "Hidden/TitanGate/MenuLiveLit"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _EmitTex ("Emission", 2D) = "black" {}
        _NormTex ("Normal (tangent space)", 2D) = "bump" {}
        _UseNorm ("Use normal map", Float) = 1
        _EmitGain ("Emission gain", Float) = 1.6
        _Spec ("Specular", Float) = 0.45
        _Gloss ("Gloss", Float) = 48
        _Tint ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Pass
        {
            ZWrite On Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex, _EmitTex, _NormTex;
            float _UseNorm, _EmitGain, _Spec, _Gloss; float4 _Tint;
            float4 _MLSunDir, _MLSunCol, _MLFillDir, _MLFillCol, _MLBounceDir, _MLBounceCol, _MLAmb, _MLRim;
            struct a2v { float4 vertex : POSITION; float3 normal : NORMAL; float4 tangent : TANGENT; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 wpos : TEXCOORD1; float3 n : TEXCOORD2; float4 t : TEXCOORD3; };
            v2f vert (a2v v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv;
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.n = UnityObjectToWorldNormal(v.normal);
                o.t = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w * unity_WorldTransformParams.w);
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.n);
                if (_UseNorm > 0.5)
                {
                    float3 tn = tex2D(_NormTex, i.uv).rgb * 2 - 1;
                    float3 t = normalize(i.t.xyz - n * dot(i.t.xyz, n));
                    float3 b = cross(n, t) * i.t.w;
                    n = normalize(tn.x * t + tn.y * b + tn.z * n);
                }
                float3 albedo = tex2D(_MainTex, i.uv).rgb * _Tint.rgb;
                float3 V = normalize(_WorldSpaceCameraPos - i.wpos);
                float3 L = normalize(_MLSunDir.xyz);
                float ndl = dot(n, L);
                float sun = saturate(ndl * 0.85 + 0.15) * saturate(ndl * 4 + 0.6);
                float3 light = _MLSunCol.rgb * sun
                             + _MLFillCol.rgb * saturate(dot(n, normalize(_MLFillDir.xyz)) * 0.5 + 0.5)
                             + _MLBounceCol.rgb * saturate(dot(n, normalize(_MLBounceDir.xyz)))
                             + _MLAmb.rgb;
                float3 H = normalize(L + V);
                float spec = pow(saturate(dot(n, H)), _Gloss) * _Spec * saturate(ndl * 3);
                float rim = pow(1 - saturate(dot(n, V)), 3.5);
                float3 col = albedo * light + _MLSunCol.rgb * spec + _MLRim.rgb * rim * (0.35 + 0.65 * saturate(ndl + 0.4));
                col += tex2D(_EmitTex, i.uv).rgb * _EmitGain;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
