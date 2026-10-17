Shader "SchoolRuleZero/Scenery"
{
    Properties { _MainTex ("Base Color", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        // Reduced scan meshes contain reversed-winding facets. Render both
        // opaque sides so the wall/background cannot show through those facets.
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float3 normal : TEXCOORD0; float2 uv : TEXCOORD1; };
            sampler2D _MainTex;
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex);
                o.normal=UnityObjectToWorldNormal(v.normal);o.uv=v.uv;return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                // Continuous lighting prevents small normal variations on flat
                // imported walls from becoming large contrasting triangle patches.
                float light=abs(dot(normalize(i.normal),normalize(float3(-.3,.8,-.5))));
                float tone=.94+.06*saturate(light);
                // Keep the original UV/texture detail and a narrow lighting range;
                // the reduced wall and desk meshes must not regain triangle stains.
                return fixed4(tex2D(_MainTex,i.uv).rgb*tone*float3(1.015,1,.98),1);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
