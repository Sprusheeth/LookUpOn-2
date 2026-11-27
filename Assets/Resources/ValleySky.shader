Shader "LookUpOn/Valley Sky"
{
    Properties { _SunDirection ("Sun direction", Vector) = (-.4,.45,.3,0) }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "../Shaders/ValleyNoise.cginc"
            float3 _SunDirection;
            struct v2f { float4 vertex : SV_POSITION; float3 direction : TEXCOORD0; };
            v2f vert(appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.direction = v.vertex.xyz;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.direction);
                float h = max(0,d.y);
                float3 color = lerp(float3(.69,.76,.78),float3(.28,.49,.70),pow(h,.55));
                float2 q = d.xz / max(.055,d.y) * 1.18 + float2(4.2,-2.3);
                float cloud = ValleyNoise(q)*.62 + ValleyNoise(q*2.07)*.25 + ValleyNoise(q*4.3)*.13;
                cloud = smoothstep(.47,.77,cloud) * smoothstep(.02,.17,h);
                color = lerp(color,lerp(float3(.64,.70,.74),float3(.99,.97,.88),smoothstep(.4,.85,cloud)),cloud*.83);
                float sun = saturate(dot(d,normalize(_SunDirection)));
                color += float3(1,.75,.45) * (pow(sun,20)*.105 + smoothstep(.99962,.99983,sun)*.58);
                return fixed4(color,1);
            }
            ENDCG
        }
    }
}
