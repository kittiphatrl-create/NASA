// LAST DOCK menu, live 3D: additive camera-facing glows (engine plumes, navigation lights, the strobe).
Shader "Hidden/TitanGate/MenuLiveGlow"
{
    Properties { _Color ("Colour", Color) = (1,1,1,1) _Falloff ("Falloff", Float) = 2.2 _Core ("Hot core", Float) = 0.6 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Blend One One ZWrite Off Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color; float _Falloff, _Core;
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata_base v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord.xy; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                float r = length(i.uv * 2 - 1);
                float a = pow(saturate(1 - r), _Falloff) + _Core * pow(saturate(1 - r * 2.5), 3);
                return fixed4(_Color.rgb * a * _Color.a, 1);
            }
            ENDCG
        }
    }
}
