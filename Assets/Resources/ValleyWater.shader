Shader "LookUpOn/Valley Water"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "../Shaders/ValleyNoise.cginc"
            struct v2f { float4 vertex : SV_POSITION; float3 world : TEXCOORD0; UNITY_FOG_COORDS(1) };
            v2f vert(appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld,v.vertex).xyz;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.world.xz;
                float t = _Time.y;
                float drift = ValleyNoise(p * .13 + float2(.03,-.21) * t);
                float eddies = ValleyNoise(p * .37 + float2(.11,-.27) * t);
                float detail = 1 - smoothstep(38, 250, distance(_WorldSpaceCameraPos, i.world));
                float a = sin(p.x * .82 + p.y * .54 - t * 1.12 + drift * 6.8);
                float b = sin(p.x * 2.18 - p.y * 1.19 + t * 1.74 + eddies * 7.3);
                float3 n = normalize(float3(.035 * a + .037 * b * detail,1,.027 * a - .039 * b * detail));
                float3 v = normalize(_WorldSpaceCameraPos - i.world);
                float reflectance = pow(1-saturate(dot(n,v)), 4);
                float colorNoise = ValleyNoise(p * .16 + float2(.05,-.17)*t);
                float3 deep = lerp(float3(.075,.189,.170),float3(.19,.30,.23),colorNoise);
                float3 reflectedSky = float3(.42,.53,.56) + .13 * ValleyNoise(p*.038 + t*.025);
                float glint = pow(saturate(dot(n, normalize(normalize(_WorldSpaceLightPos0.xyz) + v))),120);
                fixed4 c = fixed4(lerp(deep,reflectedSky,.16 + .77 * reflectance) + glint * float3(.70,.56,.34),1);
                UNITY_APPLY_FOG(i.fogCoord,c);
                return c;
            }
            ENDCG
        }
    }
}
