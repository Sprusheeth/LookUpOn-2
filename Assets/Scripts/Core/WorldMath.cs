using System;

namespace LookUpOn.Core
{
    // Recipe 3 keeps the same region and tile addresses and changes terrain heights.
    public static class WorldMath
    {
        public const int WorldVersion = 3, MapSize = 10000, HalfMap = 5000;
        public const float WaterHeight = 47f;
        public const int RegionColumns = 7, RegionRows = 6, RegionCount = 42, Subdivisions = 8;
        public const int TileColumns = RegionColumns * Subdivisions, TileRows = RegionRows * Subdivisions, Cells = 32;
        public const float RegionSizeX = (float)MapSize / RegionColumns, RegionSizeZ = (float)MapSize / RegionRows;
        public const float TileSizeX = (float)MapSize / TileColumns, TileSizeZ = (float)MapSize / TileRows;
        public static bool Inside(float x, float z) => x >= -HalfMap && x <= HalfMap && z >= -HalfMap && z <= HalfMap;
        public static bool ValidTile(int x, int z) => x >= 0 && x < TileColumns && z >= 0 && z < TileRows;
        public static bool ValidRegion(int x, int z) => x >= 0 && x < RegionColumns && z >= 0 && z < RegionRows;
        static int Index(float v, int count) => Math.Max(0, Math.Min(count - 1, (int)Math.Floor(((double)v + HalfMap) * count / MapSize)));
        public static int TileX(float x) => Index(x, TileColumns);
        public static int TileZ(float z) => Index(z, TileRows);
        public static int RegionX(float x) => TileX(x) / Subdivisions;
        public static int RegionZ(float z) => TileZ(z) / Subdivisions;
        public static int RegionId(int x, int z) => z * RegionColumns + x;
        public static float TileOriginX(int x) => (float)((double)x * MapSize / TileColumns - HalfMap);
        public static float TileOriginZ(int z) => (float)((double)z * MapSize / TileRows - HalfMap);
        public static float RegionOriginX(int x) => (float)((double)x * MapSize / RegionColumns - HalfMap);
        public static float RegionOriginZ(int z) => (float)((double)z * MapSize / RegionRows - HalfMap);
        public static int RegionSeed(int seed, int x, int z) => unchecked((int)Hash("battlefield-v2:" + seed + ":" + x + ":" + z));
        public static uint Hash(string value)
        {
            uint h = 2166136261;
            foreach (char c in value) { h ^= c; h = unchecked(h * 16777619); }
            return h;
        }
        public static float Grid(int x, int z, int seed)
        {
            uint h = unchecked((uint)x * 374761393u + (uint)z * 668265263u + (uint)seed * 1442695041u);
            h = unchecked((h ^ (h >> 13)) * 1274126177u);
            return (h ^ (h >> 16)) / (float)uint.MaxValue;
        }
        public static float Noise(float x, float z, int seed)
        {
            int ix = (int)Math.Floor(x), iz = (int)Math.Floor(z);
            float a = x - ix, b = z - iz;
            a = a * a * (3 - 2 * a); b = b * b * (3 - 2 * b);
            float lo = Grid(ix, iz, seed) * (1 - a) + Grid(ix + 1, iz, seed) * a;
            float hi = Grid(ix, iz + 1, seed) * (1 - a) + Grid(ix + 1, iz + 1, seed) * a;
            return lo * (1 - b) + hi * b;
        }
        static float Smooth(float v) { v = Math.Max(0, Math.Min(1, v)); return v * v * (3 - 2 * v); }
        static float Mix(float a, float b, float t) => a + (b - a) * t;
        public static float ValleyPhase(int seed) => Grid(21, 17, seed) * 1.4f - .7f;
        public static float RiverX(float z, float phase) =>
            135 + 155 * (float)Math.Sin(z * .00145f + phase) + 48 * (float)Math.Sin(z * .0038f + 1.1f);
        public static float RiverWidth(float z) => 22 + 5 * (float)Math.Sin(z * .003f + .9f);
        public static float RoadX(float z, float phase) =>
            RiverX(z, phase) - 99 - 23 * (float)Math.Sin(z * .0026f + .7f);
        public static float Woodland(float x, float z, int seed) =>
            .73f * Noise(x * .0033f, z * .0033f, seed + 83) +
            .27f * Noise(x * .012f, z * .012f, seed + 89);
        public static float Height(float x, float z, int worldSeed)
        {
            int rx = RegionX(x), rz = RegionZ(z), seed = RegionSeed(worldSeed, rx, rz);
            float u = (x - RegionOriginX(rx)) / RegionSizeX, v = (z - RegionOriginZ(rz)) / RegionSizeZ;
            float edge = Smooth(Math.Min(Math.Min(u, 1 - u), Math.Min(v, 1 - v)) * 16);
            float phase = ValleyPhase(worldSeed);
            float river = RiverX(z, phase), distance = Math.Abs(x - river);
            float terrace = 51 + 4 * Noise(x * .004f, z * .003f, worldSeed + 15);
            float upland = 67 + 128 * Noise(x * .00115f, z * .0009f, worldSeed)
                + 46 * Noise((x + z * .43f) * .0023f, z * .0014f, worldSeed + 1);
            float ridge = 1 - Math.Abs(2 * Noise(x * .0044f, z * .0027f, worldSeed + 6) - 1);
            upland += ridge * ridge * 23;
            float foothills = Smooth((distance - 65) / 480);
            float height = Mix(terrace, upland, foothills);
            // Local detail tapers at the ownership border and toward the floodplain.
            height += (Noise(x * .016f, z * .016f, seed) - .5f) * edge * Mix(.6f, 8, foothills);
            height = Mix(43f, height, Smooth((distance - RiverWidth(z) + 10) / 21));
            float road = 1 - Smooth((Math.Abs(x - RoadX(z, phase)) - 4) / 19);
            float roadGrade = 52 + 3 * Noise(z * .003f, 8, worldSeed + 15);
            return Mix(height, roadGrade, road * .88f);
        }
    }
}
