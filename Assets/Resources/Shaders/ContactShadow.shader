Shader "SchoolRuleZero/ContactShadow"
{
    Properties { _Color ("Tint", Color) = (.05,.04,.03,.24) }
    SubShader
    {
        Tags { "Queue"="Transparent-30" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
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
                float radius=length((i.uv-.5)*2);
                float alpha=pow(saturate(1-radius),1.6);
                return fixed4(_Color.rgb,_Color.a*alpha);
            }
            ENDCG
        }
    }
}
