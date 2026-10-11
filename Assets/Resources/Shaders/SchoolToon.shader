Shader "SchoolRuleZero/Toon"
{
    Properties { _Color ("Color", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 vertex : SV_POSITION; float3 normal : TEXCOORD0; };
            fixed4 _Color;
            v2f vert(appdata v)
            {
                v2f o; o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal); return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float light = dot(normalize(i.normal), normalize(float3(-.3,.8,-.5)));
                float tone = light > .5 ? 1 : light > -.2 ? .72 : .45;
                return fixed4(_Color.rgb * tone, _Color.a);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
