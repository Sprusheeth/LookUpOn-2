#ifndef LOOKUPON_VALLEY_NOISE
#define LOOKUPON_VALLEY_NOISE
float ValleyHash(float2 p)
{
    float3 q = frac(float3(p.x, p.y, p.x) * .1031);
    q += dot(q, q.yzx + 33.33);
    return frac((q.x + q.y) * q.z);
}
float ValleyNoise(float2 p)
{
    float2 i = floor(p), f = frac(p);
    f = f * f * (3 - 2 * f);
    return lerp(lerp(ValleyHash(i), ValleyHash(i + float2(1,0)), f.x),
        lerp(ValleyHash(i + float2(0,1)), ValleyHash(i + 1), f.x), f.y);
}
#endif
