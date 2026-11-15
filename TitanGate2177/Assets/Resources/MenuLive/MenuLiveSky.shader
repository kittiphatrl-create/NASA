// LAST DOCK menu, live 3D: the sky. The galaxy band and the nebula come from an equirectangular panorama rendered
// in Blender (Blender +X at u = 0.5, Blender +Y = Unity +Z at u = 0.25, up at the top). The stars are drawn here
// so they stay pin-sharp at any zoom, denser along the galaxy band, with a gentle twinkle.
Shader "Hidden/TitanGate/MenuLiveSky"
{
    Properties
    {
        _MainTex ("Panorama", 2D) = "black" {}
        _Gain ("Panorama gain", Float) = 1.15
        _StarGain ("Star gain", Float) = 1
        _BandDir ("Galaxy band normal", Vector) = (0, 1, 0, 0)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" }
        Pass
        {
            ZWrite Off Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex; float _Gain, _StarGain; float4 _BandDir;
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            v2f vert (appdata_base v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.dir = v.vertex.xyz; return o; }
            float3 hash3(float3 p)
            {
                p = float3(dot(p, float3(127.1, 311.7, 74.7)), dot(p, float3(269.5, 183.3, 246.1)), dot(p, float3(113.5, 271.9, 124.6)));
                return frac(sin(p) * 43758.5453);
            }
            float3 stars(float3 d, float scale, float density, float size)
            {
                float3 p = d * scale, c = floor(p), f = p - c;
                float3 h = hash3(c);
                if (h.x > density) return 0;
                float3 sp = 0.2 + 0.6 * hash3(c + 17.0);
                float px = max(length(fwidth(p)), 1e-4);
                float r = length(f - sp) / px;
                float b = saturate(1.0 - r / size); b *= b;
                float tw = 0.75 + 0.25 * sin(_Time.y * (1.0 + h.z * 3.0) + h.y * 40.0);
                float3 col = lerp(float3(0.7, 0.8, 1.0), float3(1.0, 0.85, 0.65), h.y);
                return col * b * (0.35 + 1.4 * h.z * h.z) * tw;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float2 uv = float2(0.5 - atan2(d.z, d.x) / (2 * UNITY_PI), 0.5 + asin(clamp(d.y, -1, 1)) / UNITY_PI);
                float3 col = tex2Dlod(_MainTex, float4(uv, 0, 0)).rgb * _Gain;
                float band = 1 - smoothstep(0.0, 0.25, abs(dot(d, normalize(_BandDir.xyz))));
                col += stars(d, 260, 0.10 + 0.25 * band, 1.6) * _StarGain;
                col += stars(d, 90, 0.05 + 0.08 * band, 2.3) * _StarGain * 1.4;
                col += stars(d, 34, 0.06, 3.0) * _StarGain * 2.2;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
