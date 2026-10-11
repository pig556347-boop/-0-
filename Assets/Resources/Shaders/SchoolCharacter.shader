Shader "SchoolRuleZero/Character"
{
    Properties { _MainTex ("Base Color", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float3 normal : TEXCOORD0; float2 uv : TEXCOORD1; };
            sampler2D _MainTex;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.normal=UnityObjectToWorldNormal(v.normal);o.uv=v.uv;return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float light=dot(normalize(i.normal),normalize(float3(-.3,.8,-.5)));
                float tone=light>.5?1:light>-.2?.85:.68;
                return fixed4(tex2D(_MainTex,i.uv).rgb*tone,1);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
