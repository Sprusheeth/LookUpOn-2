Shader "LookUpOn/Valley Vegetation"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        CGPROGRAM
        #pragma surface surf Lambert addshadow vertex:vert
        #pragma target 3.0
        #include "../Shaders/ValleyNoise.cginc"
        struct Input { float4 color : COLOR; float2 sprayUV; float3 worldPos; };
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.sprayUV = v.texcoord.xy;
        }
        void surf(Input IN, inout SurfaceOutput o)
        {
            float3 color = IN.color.rgb;
            if (IN.sprayUV.x >= 1)
            {
                float2 uv = IN.sprayUV - float2(1, 0);
                float2 patch = uv * float2(8, 9);
                float2 cell = floor(patch);
                float random = ValleyHash(cell);
                float2 leaf = frac(patch) - .5 + float2(random - .5, ValleyHash(cell + 21) - .5) * .18;
                float angle = random * 6.28;
                float2 shape = float2(dot(leaf, float2(cos(angle), sin(angle))), dot(leaf, float2(-sin(angle), cos(angle))));
                float edge = length((uv - .5) * 2);
                float silhouette = length(shape * float2(3.6, 2.14)) + edge * .23;
                clip(min(1.04 - edge, 1 - silhouette));
                color *= .83 + random * .44;
                o.Emission = color * .24;
            }
            else
            {
                float flecks = ValleyNoise(IN.worldPos.xz * 5 + IN.worldPos.y * 2.2);
                color *= .79 + flecks * .38;
            }
            o.Albedo = color;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
