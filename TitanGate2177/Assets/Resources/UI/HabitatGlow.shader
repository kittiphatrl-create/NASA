Shader "TitanGate/HabitatGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Glow", 2D) = "white" {}
        [PerRendererData] _LightBounds ("Light bounds", Vector) = (-10000,-10000,10000,10000)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; float2 world : TEXCOORD1; };
            sampler2D _MainTex;
            float4 _LightBounds;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                o.world = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 edge = min(i.world - _LightBounds.xy, _LightBounds.zw - i.world);
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                c.a *= smoothstep(0.0, 0.45, min(edge.x, edge.y));
                return c;
            }
            ENDCG
        }
    }
}
