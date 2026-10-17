Shader "SchoolRuleZero/Character"
{
    Properties
    {
        _MainTex ("Base Color", 2D) = "white" {}
        _HitFlash ("Contact flash", Range(0,1)) = 0
    }
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
            struct v2f { float4 vertex : SV_POSITION; float3 normal : TEXCOORD0; float2 uv : TEXCOORD1; float3 world : TEXCOORD2; };
            sampler2D _MainTex;float _HitFlash;
            v2f vert(appdata v)
            {
                v2f o;o.vertex=UnityObjectToClipPos(v.vertex);
                o.normal=UnityObjectToWorldNormal(v.normal);o.uv=v.uv;
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 normal=normalize(i.normal);
                float light=dot(normal,normalize(float3(-.3,.8,-.5)));
                // Broad continuous light avoids faceted bright/dark patches on scan meshes.
                float tone=lerp(.73,1,smoothstep(-.35,.65,light));
                float edge=pow(1-saturate(abs(dot(normal,normalize(_WorldSpaceCameraPos-i.world)))),3);
                float3 baseColor=tex2D(_MainTex,i.uv).rgb;
                float3 color=baseColor*tone+edge*float3(.022,.028,.033);
                return fixed4(lerp(color,float3(1,.86,.66),_HitFlash),1);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
