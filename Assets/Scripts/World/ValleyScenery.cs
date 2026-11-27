using System;
using System.Collections.Generic;
using LookUpOn.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace LookUpOn.World
{
    // Decorations are batched per tile so streaming does not create an object per plant.
    sealed class ValleyScenery : IDisposable
    {
        readonly int seed;
        readonly float phase;
        readonly Material scenery, water;
        readonly Mesh[] crowns = new Mesh[5];
        readonly Mesh[] detailedCrowns = new Mesh[5];
        readonly Mesh[] trunks = new Mesh[5];
        readonly Mesh[] rocks = new Mesh[5];
        readonly Mesh grass;

        public ValleyScenery(int seed, Material scenery, Material water)
        {
            this.seed = seed;
            this.scenery = scenery;
            this.water = water;
            phase = WorldMath.ValleyPhase(seed);
            for (int i = 0; i < crowns.Length; i++)
            {
                var rng = new System.Random(i + 780);
                var wood = new Shape();
                var foliage = new Shape();
                var leaves = new Shape();
                var leafRandom = new System.Random(i + 660);
                wood.Branch(Vector3.zero, Vector3.up * 6f, .28f, .075f, 7, new Color(.27f, .22f, .16f));
                for (int j = 0; j < 7; j++)
                {
                    float angle = j * 2.4f + Next(rng);
                    float spread = j == 0 ? .5f : 1.4f + Next(rng) * 1.6f;
                    float top = j == 0 ? 8.3f : 4.3f + Next(rng) * 3.2f;
                    Vector3 tip = new Vector3(Mathf.Cos(angle) * spread, top, Mathf.Sin(angle) * spread);
                    if (j > 0) wood.Branch(Vector3.up * (2.3f + Next(rng) * 1.2f), tip, .13f, .024f, 5, new Color(.31f, .25f, .18f));
                    float size = 1.45f + Next(rng) * .85f;
                    var tint = Color.Lerp(new Color(.18f, .27f, .087f), new Color(.34f, .43f, .14f), Next(rng));
                    Vector3 center = tip + Vector3.up * .45f;
                    foliage.Blob(center, new Vector3(size, size * .79f, size), rng, tint, 5, 10, .25f);
                    for (int k = 0; k < 25; k++)
                    {
                        float y = Next(leafRandom) * 2 - 1;
                        float angleAround = Next(leafRandom) * 2 * Mathf.PI;
                        float horizontal = Mathf.Sqrt(1 - y * y);
                        var outward = new Vector3(Mathf.Cos(angleAround) * horizontal, y, Mathf.Sin(angleAround) * horizontal);
                        var leafCenter = center + Vector3.Scale(outward, new Vector3(size, size * .74f, size)) * (.30f + .67f * Next(leafRandom));
                        var orientation = Quaternion.FromToRotation(Vector3.forward, (outward + Vector3.up * .75f).normalized)
                            * Quaternion.AngleAxis(360 * Next(leafRandom), Vector3.forward);
                        float breadth = size * (.30f + .24f * Next(leafRandom));
                        leaves.LeafSpray(leafCenter, orientation * Vector3.right * breadth, orientation * Vector3.up * (breadth * 1.15f),
                            tint * (.72f + .56f * Next(leafRandom)));
                    }
                }
                crowns[i] = foliage.Create("Broadleaf crown");
                detailedCrowns[i] = leaves.Create("Broadleaf sprays");
                trunks[i] = wood.Create("Tree branches");
                var rock = new Shape();
                rock.Blob(Vector3.zero, new Vector3(1.25f, .82f, 1f), rng, new Color(.40f, .38f, .33f), 4, 8, .28f);
                rocks[i] = rock.Create("Weathered rock");
            }
            var blades = new Shape();
            var grassRandom = new System.Random(322);
            for (int i = 0; i < 7; i++)
            {
                float a = i * 2.4f;
                Vector3 across = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (.035f + Next(grassRandom) * .035f);
                Vector3 foot = new Vector3(Mathf.Sin(a), 0, -Mathf.Cos(a)) * Next(grassRandom) * .23f;
                float height = .25f + Next(grassRandom) * .6f;
                blades.Blade(foot, across, height, i % 3 == 0 ? new Color(.43f, .44f, .20f) : new Color(.27f, .34f, .11f));
            }
            grass = blades.Create("Meadow grass");
        }

        public void Build(Transform tile, Vector2Int key, int cells, Vector3[] ground)
        {
            float ox = WorldMath.TileOriginX(key.x), oz = WorldMath.TileOriginZ(key.y);
            float width = WorldMath.TileOriginX(key.x + 1) - ox, depth = WorldMath.TileOriginZ(key.y + 1) - oz;
            var rng = new System.Random(unchecked((int)WorldMath.Hash(key.x + ":" + key.y + ":" + WorldMath.RegionSeed(seed, key.x / 8, key.y / 8))));
            var crownsBatch = new List<CombineInstance>();
            var woodBatch = new List<CombineInstance>();
            var rockBatch = new List<CombineInstance>();
            var grassBatch = new List<CombineInstance>();
            for (int j = 0; j < 13; j++)
                for (int i = 0; i < 11; i++)
                {
                    float x = (i + .14f + Next(rng) * .72f) * width / 11;
                    float z = (j + .14f + Next(rng) * .72f) * depth / 13;
                    float roll = Next(rng);
                    int variant = rng.Next(crowns.Length);
                    float size = .64f + Next(rng) * .74f;
                    float yaw = Next(rng) * 360;
                    float wx = ox + x, wz = oz + z;
                    float treeCover = WorldMath.Woodland(wx, wz, seed);
                    float y = Ground(ground, cells, x / width, z / depth);
                    float road = Mathf.Abs(wx - WorldMath.RoadX(wz, phase));
                    float river = Mathf.Abs(wx - WorldMath.RiverX(wz, phase));
                    float rise = Mathf.Abs(WorldMath.Height(wx + 2, wz, seed) - WorldMath.Height(wx - 2, wz, seed))
                        + Mathf.Abs(WorldMath.Height(wx, wz + 2, seed) - WorldMath.Height(wx, wz - 2, seed));
                    if (y < WorldMath.WaterHeight + .5f || road < 8f) continue;
                    float density = Mathf.SmoothStep(0, .80f, Mathf.InverseLerp(.42f, .72f, treeCover));
                    if (roll < density && rise < 4.1f && river > WorldMath.RiverWidth(wz) + 1)
                    {
                        var matrix = Matrix4x4.TRS(new Vector3(x, y - .27f, z), Quaternion.Euler(0, yaw, 0), Vector3.one * size);
                        crownsBatch.Add(new CombineInstance { mesh = cells == 32 ? detailedCrowns[variant] : crowns[variant], transform = matrix });
                        woodBatch.Add(new CombineInstance { mesh = trunks[variant], transform = matrix });
                    }
                    else if (roll > .94f || rise > 2.7f && roll > .78f)
                    {
                        var scale = new Vector3(1.1f, .52f + size * .45f, 1f) * size;
                        var matrix = Matrix4x4.TRS(new Vector3(x, y - .17f, z), Quaternion.Euler(9, yaw, 12), scale);
                        rockBatch.Add(new CombineInstance { mesh = rocks[variant], transform = matrix });
                    }
                }
            if (cells >= 16)
            {
                int spacing = cells == 32 ? 5 : 11;
                int cols = Mathf.FloorToInt(width / spacing), rows = Mathf.FloorToInt(depth / spacing);
                for (int j = 0; j < rows; j++)
                    for (int i = 0; i < cols; i++)
                    {
                        float x = (i + Next(rng)) * width / cols, z = (j + Next(rng)) * depth / rows;
                        float wx = ox + x, wz = oz + z;
                        float y = Ground(ground, cells, x / width, z / depth);
                        float road = Mathf.Abs(wx - WorldMath.RoadX(wz, phase));
                        float cover = WorldMath.Woodland(wx + 37, wz - 63, seed);
                        if (road < 5.4f || y < WorldMath.WaterHeight + .22f || Next(rng) > cover + .3f) continue;
                        float scale = .45f + Next(rng) * .76f;
                        var matrix = Matrix4x4.TRS(new Vector3(x, y - .09f, z), Quaternion.Euler(0, Next(rng) * 360, 0), Vector3.one * scale);
                        grassBatch.Add(new CombineInstance { mesh = grass, transform = matrix });
                    }
            }
            Batch(tile, "Tree crowns", crownsBatch, scenery, cells >= 16);
            Batch(tile, "Tree trunks", woodBatch, scenery, cells >= 16);
            Batch(tile, "Rocks", rockBatch, scenery, cells >= 16);
            Batch(tile, "Meadow grasses", grassBatch, scenery, false);
            foreach (var v in ground)
            {
                if (v.y >= WorldMath.WaterHeight) continue;
                var surface = new Mesh { name = "River water" };
                surface.vertices = new[] { new Vector3(0, WorldMath.WaterHeight, 0), new Vector3(0, WorldMath.WaterHeight, depth),
                    new Vector3(width, WorldMath.WaterHeight, 0), new Vector3(width, WorldMath.WaterHeight, depth) };
                surface.triangles = new[] { 0, 1, 2, 2, 1, 3 };
                Attach(tile, "River", surface, water, false);
                break;
            }
        }

        static float Ground(Vector3[] vertices, int cells, float u, float v)
        {
            float x = u * cells, z = v * cells;
            int ix = Mathf.Min(cells - 1, (int)x), iz = Mathf.Min(cells - 1, (int)z);
            int a = iz * (cells + 1) + ix, b = a + cells + 1;
            float dx = x - ix, dz = z - iz;
            if (dx + dz <= 1)
                return vertices[a].y + dx * (vertices[a + 1].y - vertices[a].y) + dz * (vertices[b].y - vertices[a].y);
            return vertices[b + 1].y + (1 - dx) * (vertices[b].y - vertices[b + 1].y) + (1 - dz) * (vertices[a + 1].y - vertices[b + 1].y);
        }

        static void Batch(Transform tile, string name, List<CombineInstance> batch, Material material, bool shadows)
        {
            if (batch.Count == 0) return;
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(batch.ToArray());
            Attach(tile, name, mesh, material, shadows);
        }

        static void Attach(Transform tile, string name, Mesh mesh, Material material, bool shadows)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(tile, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }

        public void Dispose()
        {
            foreach (var mesh in crowns) UnityEngine.Object.Destroy(mesh);
            foreach (var mesh in detailedCrowns) UnityEngine.Object.Destroy(mesh);
            foreach (var mesh in trunks) UnityEngine.Object.Destroy(mesh);
            foreach (var mesh in rocks) UnityEngine.Object.Destroy(mesh);
            UnityEngine.Object.Destroy(grass);
        }

        static float Next(System.Random rng) => (float)rng.NextDouble();

        sealed class Shape
        {
            readonly List<Vector3> vertices = new List<Vector3>();
            readonly List<Color> colors = new List<Color>();
            readonly List<Vector2> leafUvs = new List<Vector2>();
            readonly List<int> triangles = new List<int>();

            public void Blob(Vector3 center, Vector3 scale, System.Random rng, Color color, int rings, int sides, float jitter)
            {
                int start = vertices.Count;
                vertices.Add(center + Vector3.up * scale.y);
                colors.Add(color * 1.14f);
                for (int row = 1; row < rings; row++)
                {
                    float latitude = row * Mathf.PI / rings;
                    for (int side = 0; side < sides; side++)
                    {
                        float a = side * 2 * Mathf.PI / sides + (row % 2) * .19f;
                        var direction = new Vector3(Mathf.Sin(latitude) * Mathf.Cos(a), Mathf.Cos(latitude), Mathf.Sin(latitude) * Mathf.Sin(a));
                        vertices.Add(center + Vector3.Scale(direction, scale) * (1 + (Next(rng) - .5f) * 2 * jitter));
                        colors.Add(color * (.94f + direction.y * .16f + Next(rng) * .15f));
                    }
                }
                int bottom = vertices.Count;
                vertices.Add(center - Vector3.up * scale.y);
                colors.Add(color * .77f);
                for (int i = 0; i < sides; i++)
                {
                    int next = (i + 1) % sides;
                    Face(start, start + 1 + next, start + 1 + i);
                    for (int row = 0; row < rings - 2; row++)
                    {
                        int a = start + 1 + row * sides;
                        Face(a + i, a + next, a + sides + i);
                        Face(a + next, a + sides + next, a + sides + i);
                    }
                    Face(bottom - sides + i, bottom - sides + next, bottom);
                }
            }

            public void Branch(Vector3 basePoint, Vector3 tip, float radius, float tipRadius, int sides, Color color)
            {
                int start = vertices.Count;
                var rotation = Quaternion.FromToRotation(Vector3.up, (tip - basePoint).normalized);
                for (int ring = 0; ring < 2; ring++)
                    for (int i = 0; i < sides; i++)
                    {
                        float angle = i * 2 * Mathf.PI / sides;
                        Vector3 radial = rotation * new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                        vertices.Add(ring == 0 ? basePoint + radial * radius : tip + radial * tipRadius);
                        colors.Add(color * (.89f + (i % 3) * .12f));
                    }
                for (int i = 0; i < sides; i++)
                {
                    int n = (i + 1) % sides;
                    Face(start + i, start + i + sides, start + n);
                    Face(start + n, start + i + sides, start + n + sides);
                }
            }

            public void Blade(Vector3 foot, Vector3 across, float height, Color color)
            {
                int i = vertices.Count;
                Vector3 bend = Vector3.Cross(Vector3.up, across);
                vertices.Add(foot - across);
                vertices.Add(foot + across);
                vertices.Add(foot + Vector3.up * (height * .55f) + bend * 2 - across * .7f);
                vertices.Add(foot + Vector3.up * (height * .55f) + bend * 2 + across * .7f);
                vertices.Add(foot + Vector3.up * height + bend * 5);
                colors.Add(color * .59f);
                colors.Add(color * .59f);
                colors.Add(color);
                colors.Add(color);
                colors.Add(color * 1.16f);
                Face(i, i + 2, i + 1);
                Face(i + 1, i + 2, i + 3);
                Face(i + 2, i + 4, i + 3);
            }

            public void LeafSpray(Vector3 center, Vector3 across, Vector3 up, Color color)
            {
                int i = vertices.Count;
                vertices.Add(center - across - up);
                vertices.Add(center + across - up);
                vertices.Add(center - across + up);
                vertices.Add(center + across + up);
                for (int v = 0; v < 4; v++) colors.Add(color);
                // U starts at one to distinguish cutout foliage from the solid scenery.
                leafUvs.Add(new Vector2(1, 0));
                leafUvs.Add(new Vector2(2, 0));
                leafUvs.Add(new Vector2(1, 1));
                leafUvs.Add(new Vector2(2, 1));
                Face(i, i + 1, i + 2);
                Face(i + 2, i + 1, i + 3);
            }

            void Face(int a, int b, int c) { triangles.Add(a); triangles.Add(b); triangles.Add(c); }
            public Mesh Create(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(vertices);
                mesh.SetColors(colors);
                mesh.uv = leafUvs.Count == 0 ? new Vector2[vertices.Count] : leafUvs.ToArray();
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                return mesh;
            }
        }
    }
}
