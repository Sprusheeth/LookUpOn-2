using System.Collections.Generic;
using System.Linq;
using LookUpOn.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace LookUpOn.World
{
    public sealed class ChunkWorld : MonoBehaviour
    {
        public const int TerrainLayer = 8;
        const int BorderCells = 8;
        public int Seed = 2026;
        public Transform Observer;
        public int Radius = 5;
        public System.Func<int, int, bool> RegionAvailable;
        public Material TerrainMaterial, SceneryMaterial, WaterMaterial;
        readonly Dictionary<Vector2Int, GameObject> chunks = new Dictionary<Vector2Int, GameObject>();
        readonly Queue<Vector2Int> pending = new Queue<Vector2Int>();
        readonly Dictionary<Vector2Int, int> lods = new Dictionary<Vector2Int, int>();
        float nextRefresh;
        ValleyScenery scenery;
        Vector2Int center = new Vector2Int(int.MaxValue, int.MaxValue);
        public int Loaded => chunks.Count;
        public IEnumerable<Vector2Int> LoadedCoordinates => chunks.Keys;
        public int Pending => pending.Count;
        public bool CanStand(float x, float z)
        {
            var tile = new Vector2Int(WorldMath.TileX(x), WorldMath.TileZ(z));
            return chunks.ContainsKey(tile) && Available(tile);
        }
        bool Available(Vector2Int tile) => WorldMath.ValidTile(tile.x, tile.y)
            && (RegionAvailable == null || RegionAvailable(tile.x / WorldMath.Subdivisions, tile.y / WorldMath.Subdivisions));
        int Detail(Vector2Int key)
        {
            int d = Mathf.Max(Mathf.Abs(key.x - center.x), Mathf.Abs(key.y - center.y));
            return d <= 1 ? 32 : d <= 3 ? 16 : 8;
        }
        void Remove(Vector2Int key)
        {
            ReleaseTile(chunks[key]);
            chunks.Remove(key);
            lods.Remove(key);
        }
        static void ReleaseTile(GameObject tile)
        {
            foreach (var filter in tile.GetComponentsInChildren<MeshFilter>()) Destroy(filter.sharedMesh);
            Destroy(tile);
        }
        public void WarmStart(Vector3 position)
        {
            center = new Vector2Int(WorldMath.TileX(position.x), WorldMath.TileZ(position.z));
            for (int z = -1; z <= 1; z++)
                for (int x = -1; x <= 1; x++)
                    Build(center + new Vector2Int(x, z));
        }
        void Update()
        {
            if (!Observer) return;
            var next = new Vector2Int(WorldMath.TileX(Observer.position.x), WorldMath.TileZ(Observer.position.z));
            if (next != center || Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + .5f;
                center = next;
                pending.Clear();
                foreach (var key in chunks.Keys.Where(k => !Available(k) || Mathf.Abs(k.x - center.x) > Radius + 1 || Mathf.Abs(k.y - center.y) > Radius + 1).ToArray())
                    Remove(key);
                var desired = new List<Vector2Int>();
                for (int z = -Radius; z <= Radius; z++)
                    for (int x = -Radius; x <= Radius; x++)
                    {
                        var key = center + new Vector2Int(x, z);
                        if (Available(key) && (!chunks.ContainsKey(key) || lods[key] != Detail(key))) desired.Add(key);
                    }
                foreach (var key in desired.OrderBy(k => (k - center).sqrMagnitude)) pending.Enqueue(key);
            }
            if (pending.Count > 0) Build(pending.Dequeue());
        }
        public static Mesh MakeMesh(int cx, int cz, int seed, int n = WorldMath.Cells)
        {
            float originX = WorldMath.TileOriginX(cx), originZ = WorldMath.TileOriginZ(cz);
            int stride = n + 1;
            var vertices = new Vector3[stride * stride];
            var normals = new Vector3[vertices.Length];
            var indices = new int[n * n * 6];
            for (int z = 0; z <= n; z++)
                for (int x = 0; x <= n; x++)
                {
                    float wx = Coordinate(cx, x, n, WorldMath.TileColumns);
                    float wz = Coordinate(cz, z, n, WorldMath.TileRows);
                    float h = WorldMath.Height(wx, wz, seed);
                    if (x == 0 || x == n) h = BorderHeight(wx, cz, z, n, WorldMath.TileRows, false, seed);
                    else if (z == 0 || z == n) h = BorderHeight(wz, cx, x, n, WorldMath.TileColumns, true, seed);
                    int index = z * stride + x;
                    vertices[index] = new Vector3(wx - originX, h, wz - originZ);
                    // Sample the same normal in both tiles, including where their resolutions differ.
                    const float step = 2f;
                    normals[index] = new Vector3(
                        WorldMath.Height(wx - step, wz, seed) - WorldMath.Height(wx + step, wz, seed),
                        2 * step,
                        WorldMath.Height(wx, wz - step, seed) - WorldMath.Height(wx, wz + step, seed)).normalized;
                }
            int t = 0;
            for (int z = 0; z < n; z++)
                for (int x = 0; x < n; x++)
                {
                    int a = z * stride + x, b = a + stride;
                    indices[t++] = a; indices[t++] = b; indices[t++] = a + 1;
                    indices[t++] = a + 1; indices[t++] = b; indices[t++] = b + 1;
                }
            var mesh = new Mesh { name = "Valley terrain " + cx + "," + cz };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = indices;
            return mesh;
        }
        static float Coordinate(int tile, int vertex, int cells, int tileCount) =>
            (float)((double)(tile * cells + vertex) * WorldMath.MapSize / (tileCount * cells) - WorldMath.HalfMap);
        static float BorderHeight(float fixedCoordinate, int tile, int vertex, int cells, int tileCount, bool alongX, int seed)
        {
            float t = vertex * (float)BorderCells / cells;
            int lo = Mathf.Min(BorderCells - 1, Mathf.FloorToInt(t));
            float a = Coordinate(tile, lo, BorderCells, tileCount);
            float b = Coordinate(tile, lo + 1, BorderCells, tileCount);
            return alongX
                ? Mathf.Lerp(WorldMath.Height(a, fixedCoordinate, seed), WorldMath.Height(b, fixedCoordinate, seed), t - lo)
                : Mathf.Lerp(WorldMath.Height(fixedCoordinate, a, seed), WorldMath.Height(fixedCoordinate, b, seed), t - lo);
        }
        void Build(Vector2Int key)
        {
            if (!Available(key)) return;
            if (chunks.ContainsKey(key)) Remove(key);
            int cells = Detail(key);
            var root = new GameObject("Valley tile " + key.x + "," + key.y + " / LOD " + cells);
            root.layer = TerrainLayer;
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(WorldMath.TileOriginX(key.x), 0, WorldMath.TileOriginZ(key.y));
            var mesh = MakeMesh(key.x, key.y, Seed, cells);
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = TerrainMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            if (cells >= 16) root.AddComponent<MeshCollider>().sharedMesh = mesh;
            chunks.Add(key, root);
            lods[key] = cells;
            if (scenery == null) scenery = new ValleyScenery(Seed, SceneryMaterial, WaterMaterial);
            scenery.Build(root.transform, key, cells, mesh.vertices);
        }
        void OnDestroy()
        {
            foreach (var chunk in chunks.Values) if (chunk) ReleaseTile(chunk);
            scenery?.Dispose();
        }
    }
}
