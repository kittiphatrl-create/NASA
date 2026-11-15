// LAST DOCK menu, live 3D: Mars and Phobos. A soft terminator and a thin atmosphere glow on the lit limb.
Shader "Hidden/TitanGate/MenuLivePlanet"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Gain ("Light gain", Float) = 1.25
        _Atm ("Atmosphere colour", Color) = (1, 0.45, 0.2, 1)
        _AtmPower ("Atmosphere falloff", Float) = 3.5
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
            sampler2D _MainTex; float _Gain, _AtmPower; float4 _Atm;
            float4 _MLSunDir, _MLSunCol, _MLAmb;
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 wpos : TEXCOORD1; float3 n : TEXCOORD2; };
            v2f vert (appdata_base v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord.xy;
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz; o.n = UnityObjectToWorldNormal(v.normal); return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.n), V = normalize(_WorldSpaceCameraPos - i.wpos), L = normalize(_MLSunDir.xyz);
                float ndl = dot(n, L);
                float lit = smoothstep(-0.08, 0.55, ndl);
                float3 albedo = tex2D(_MainTex, i.uv).rgb;
                float fres = pow(1 - saturate(dot(n, V)), _AtmPower);
                float3 col = albedo * (_MLSunCol.rgb * lit * _Gain + _MLAmb.rgb * 0.5);
                col += _Atm.rgb * fres * saturate(ndl * 1.5 + 0.35) * _Atm.a;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
