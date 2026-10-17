Shader "SchoolRuleZero/ClassroomLight"
{
    Properties { _Color ("Tint", Color) = (1,.86,.62,.05) }
    SubShader
    {
        Tags { "Queue"="Transparent-40" "RenderType"="Transparent" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION;float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION;float2 uv:TEXCOORD0; };
            fixed4 _Color;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float2 feather=smoothstep(0,.14,i.uv)*smoothstep(0,.14,1-i.uv);
                return fixed4(_Color.rgb,_Color.a*feather.x*feather.y);
            }
            ENDCG
        }
    }
}
