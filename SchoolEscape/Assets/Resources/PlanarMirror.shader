Shader "SchoolEscape/PlanarMirror"
{
    Properties
    {
        _ReflectionTex ("Live reflection", 2D) = "black" {}
        _Tint ("Old mirror tint", Color) = (0.93, 0.98, 0.97, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Back
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _ReflectionTex;
            float4x4 _ReflectionVP;
            fixed4 _Tint;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 position : SV_POSITION; float4 reflected : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.reflected = mul(_ReflectionVP, mul(unity_ObjectToWorld, v.vertex));
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.reflected.xy / i.reflected.w * 0.5 + 0.5;
                fixed3 reflection = tex2D(_ReflectionTex, uv).rgb;
                return fixed4(reflection * _Tint.rgb, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
