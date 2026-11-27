Shader "LookUpOn/Faceted Terrain"
{
    Properties
    {
        _ValleyPhase ("River phase", Float) = 0
        _SurfaceSeed ("Surface seed", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Lambert
        #pragma target 3.0
        #include "ValleyNoise.cginc"

        float _ValleyPhase, _SurfaceSeed;
        struct Input { float3 worldPos; float3 worldNormal; };
        void surf(Input IN, inout SurfaceOutput o)
        {
            float2 p = IN.worldPos.xz;
            float fade = 1 - smoothstep(23, 155, distance(IN.worldPos, _WorldSpaceCameraPos));
            float seed = _SurfaceSeed;
            float patch = ValleyNoise(p * .009 + seed) * .64 + ValleyNoise(p * .037 - seed) * .36;
            float mottling = ValleyNoise(p * .28) - .5;
            float grains = (ValleyNoise(p * 2.7) - .5) * fade;
            float river = 135 + 155 * sin(p.y * .00145 + _ValleyPhase) + 48 * sin(p.y * .0038 + 1.1);
            float width = 22 + 5 * sin(p.y * .003 + .9);
            float road = river - 99 - 23 * sin(p.y * .0026 + .7);
            float distanceToRoad = abs(p.x - road);
            float woodland = ValleyNoise(p * .005 + float2(16,42));
            float3 green = lerp(float3(.112,.167,.062), float3(.320,.354,.159), smoothstep(.17,.80,patch));
            green = lerp(green, float3(.170,.216,.064), smoothstep(.48,.8,woodland) * .42);
            float stone = smoothstep(.08, .43, 1 - normalize(IN.worldNormal).y + .08 * patch);
            float strata = sin(IN.worldPos.y * .39 + p.y * .042 + mottling * 2.5) * .035;
            float3 rock = float3(.375,.350,.29) + strata + mottling * .14;
            float3 color = lerp(green, rock, stone);
            float bank = 1 - smoothstep(width - 4, width + 8, abs(p.x-river) + mottling * 5);
            float3 gravel = lerp(float3(.24,.22,.16), float3(.48,.425,.302), smoothstep(46.3, 49.2, IN.worldPos.y));
            color = lerp(color, gravel, bank);
            float verge = (ValleyNoise(p * .075) - .5) * 2.2;
            float dirt = 1 - smoothstep(3.1, 5.8, distanceToRoad + mottling * 2.1 + verge);
            float wheelRuts = 1 - smoothstep(.25, .64, abs(distanceToRoad - 1.45));
            float3 track = lerp(float3(.56,.457,.315), float3(.405,.337,.235), wheelRuts * .55);
            color = lerp(color, track, dirt);
            o.Albedo = max(.01, color * (1 + .24 * mottling + .32 * grains));
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
