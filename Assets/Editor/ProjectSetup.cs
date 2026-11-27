using System;
using System.IO;
using System.Linq;
using System.Threading;
using LookUpOn.Core;
using LookUpOn.Networking;
using LookUpOn.World;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LookUpOn.Editor
{
    public static class ProjectSetup
    {
        [InitializeOnLoadMethod]
        static void FirstOpen()
        {
            if (Application.isBatchMode || File.Exists("Assets/Scenes/Frontier.unity")) return;
            EditorApplication.delayCall += () => { if (!EditorApplication.isPlayingOrWillChangePlaymode) CreateScene(); };
        }
        [MenuItem("LookUpOn/Create or reset prototype scene")]
        public static void CreateScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var game = new GameObject("LookUpOn").AddComponent<LookUpOnGame>();
            game.TerrainShader = Shader.Find("LookUpOn/Faceted Terrain"); game.StandardShader = Shader.Find("Standard");
            if (!game.TerrainShader || !game.StandardShader) throw new Exception("Required shaders missing");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Frontier.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Frontier.unity", true) };
            PlayerSettings.companyName = "LookUpOn"; PlayerSettings.productName = "LookUpOn";
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 800;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            AssetDatabase.SaveAssets();
        }
        public static void PlayFrontier()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Frontier.unity");
            EditorApplication.EnterPlaymode();
        }
        [MenuItem("LookUpOn/Validate prototype")]
        public static void Validate()
        {
            int checks = 0;
            void Check(bool pass, string message) { if (!pass) throw new Exception("CHECK FAILED: " + message); checks++; }
            Check(WorldMath.RegionColumns * WorldMath.RegionRows == 42 && WorldMath.TileColumns * WorldMath.TileRows == 42 * 64, "42 regions and 2688 tiles");
            Check(WorldMath.TileX(-5000)==0 && WorldMath.TileX(5000)==55 && WorldMath.TileZ(5000)==47, "exact battlefield bounds");
            Check(WorldMath.TileOriginX(0)==-5000 && WorldMath.TileOriginX(56)==5000 && WorldMath.TileOriginZ(48)==5000, "10 km square coverage");
            Check(Enumerable.Range(0,42).Select(r=>WorldMath.RegionSeed(2026,r%7,r/7)).Distinct().Count()==42, "independent regional seeds");
            Check(WorldMath.Height(123, -14, 2026) == WorldMath.Height(123, -14, 2026), "deterministic terrain");
            Check(WorldMath.Height(123, -14, 2026) != WorldMath.Height(123, -14, 2027), "seed diversity");
            foreach (int z in new[] { -4500, -1667, -90, 0, 1700, 4800 })
            {
                float phase = WorldMath.ValleyPhase(2026);
                Check(WorldMath.Height(WorldMath.RiverX(z, phase), z, 2026) < WorldMath.WaterHeight, "river has a submerged bed");
                Check(WorldMath.Height(WorldMath.RoadX(z, phase), z, 2026) > WorldMath.WaterHeight, "dirt road stays above water");
            }
            foreach (string name in new[] { "ValleySky", "ValleyVegetation", "ValleyWater" })
                Check(Resources.Load<Shader>(name) != null, "valley shader included: " + name);
            var a = ChunkWorld.MakeMesh(7, 8, 2026); var b = ChunkWorld.MakeMesh(8, 8, 2026);
            float width = WorldMath.TileOriginX(8)-WorldMath.TileOriginX(7);
            foreach (var v in a.vertices.Where(v => Mathf.Abs(v.x-width)<.001f))
                Check(b.vertices.Any(w => w.x == 0 && Mathf.Abs(w.z-v.z)<.001f && Mathf.Abs(w.y-v.y)<.001f), "regional seed seam");
            var low = ChunkWorld.MakeMesh(8,8,2026,8);
            foreach(var v in b.vertices.Where(v=>v.x==0))
            {
                var edge=low.vertices.Where(w=>w.x==0).Distinct().OrderBy(w=>w.z).ToArray();
                int i=Mathf.Min(edge.Length-2,Mathf.FloorToInt(v.z/WorldMath.TileSizeZ*8));
                float h=Mathf.Lerp(edge[i].y,edge[i+1].y,Mathf.InverseLerp(edge[i].z,edge[i+1].z,v.z));
                Check(Mathf.Abs(h-v.y)<.002f,"mixed LOD borders stitch");
            }
            Check(a.normals.All(n => n.y > 0), "upward triangle winding");
            UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b); UnityEngine.Object.DestroyImmediate(low);
            var ring = new HashRing(); var reverse = new HashRing();
            var two = new HashRing();
            two.SetPeers(new[] { "78bda5db1a6f4505bca6ac420d44b1a5", "faaeac5acb984151a72b1ec0d24b8aeb" });
            Check(Enumerable.Range(0, 42).Select(i => two.Owners(i % 7, i / 7, 1)[0]).Distinct().Count() == 2,
                "virtual nodes spread ownership across both tested peers");
            ring.SetPeers(new[] { "alpha", "beta", "gamma", "delta" }); reverse.SetPeers(new[] { "delta", "gamma", "beta", "alpha" });
            for (int i = -50; i < 50; i++)
            {
                var owners = ring.Owners(i, -i);
                Check(owners.Length == 3 && owners.Distinct().Count() == 3, "distinct replica placement");
                Check(owners.SequenceEqual(reverse.Owners(i, -i)), "membership order independence");
            }
            var before = Enumerable.Range(0, 200).Select(i => ring.Owners(i, i, 1)[0]).ToArray();
            ring.SetPeers(new[] { "alpha", "beta", "gamma", "delta", "epsilon" });
            for (int i = 0; i < 200; i++) Check(ring.Owners(i, i, 1)[0] == before[i] || ring.Owners(i, i, 1)[0] == "epsilon", "minimal remap on join");
            string id = Guid.NewGuid().ToString("N"), author = Guid.NewGuid().ToString("N");
            var s1 = new BeaconStore(); var s2 = new BeaconStore();
            var beacon = new Beacon { id = id, author = author, x = 15, z = -22 };
            Check(s1.Merge(beacon) && !s1.Merge(beacon), "idempotent beacon merge");
            Check(!s1.Merge(new Beacon { id = Guid.NewGuid().ToString("N"), author = author, x = float.NaN }), "invalid coordinate rejected");
            var conflict = new Beacon { id = id, author = author, x = 10, z = -22 };
            s1.Merge(conflict); s2.Merge(conflict); s2.Merge(beacon);
            Check(s1.Items.First().x == s2.Items.First().x, "conflict convergence");
            Check(s1.Digest == s2.Digest && s1.Digest.Length == 64, "canonical fingerprint convergence");
            string priorDigest = s1.Digest;
            int priorRevision = s1.Revision;
            s1.Merge(conflict);
            Check(s1.Digest == priorDigest && s1.Revision == priorRevision, "duplicate does not change revision or fingerprint");
            var different = new BeaconStore(); different.Merge(new Beacon { id = id, author = author, x = 11, z = -22 });
            Check(different.Digest != s1.Digest, "equal counts with different data have different fingerprints");
            // Real loopback sockets exercise discovery, late join, anti-entropy and timeout.
            using (var p1 = new PeerNetwork(Guid.NewGuid().ToString("N"), 2026, s1, false))
            using (var p2 = new PeerNetwork(Guid.NewGuid().ToString("N"), 2026, new BeaconStore(), false))
            using (var mismatch = new PeerNetwork(Guid.NewGuid().ToString("N"), 99, new BeaconStore(), false))
            {
                Check(p1.Start(0) && p2.Start(0) && mismatch.Start(0), "bind loopback peers");
                p2.Connect("127.0.0.1", p1.Port); mismatch.Connect("127.0.0.1", p1.Port);
                for (int i = 0; i < 24; i++) { p1.Tick(i * .2, Vector3.zero, 0); p2.Tick(i * .2, Vector3.one, 0); mismatch.Tick(i * .2, Vector3.zero, 0); Thread.Sleep(5); }
                Check(p1.Peers.Count == 1 && p2.Peers.Count == 1, "bidirectional peer discovery and world mismatch rejection");
                p2.Dispose(); mismatch.Dispose(); p1.Tick(5, Vector3.zero, 0); p1.Tick(14, Vector3.zero, 0);
                Check(p1.Peers.Count == 0 && p1.Ring.Owners(0, 0).Length == 1, "failure timeout and ring reassignment");
            }
            var sync = new BeaconStore();
            for (int i = 0; i < 70; i++) s1.Merge(new Beacon { id = Guid.NewGuid().ToString("N"), author = author, x = i, z = i });
            using (var p1 = new PeerNetwork(Guid.NewGuid().ToString("N"), 2026, s1, false))
            using (var p2 = new PeerNetwork(Guid.NewGuid().ToString("N"), 2026, sync, false))
            {
                Check(p1.Start(0) && p2.Start(0), "bind late join peers"); p2.Connect("127.0.0.1", p1.Port);
                p1.SavedDigest = s1.Digest;
                for (int i = 0; i < 30; i++) { p1.Tick(i * .2, Vector3.zero, 0); p2.Tick(i * .2, Vector3.one, 0); Thread.Sleep(5); }
                Check(sync.Count == s1.Count, "multi-page late-join beacon recovery");
                Check(p2.Peers.Values.Single().StateDigest == s1.Digest, "remote memory telemetry matches content");
                Check(p2.Peers.Values.Single().SavedDigest == s1.Digest, "remote saved fingerprint transported");
                Check(p2.Peers.Values.Single().BeaconCount == s1.Count && p2.BytesReceived > 0 && p1.BytesSent > 0, "counts and traffic are observed");
                Check(p1.Peers.Values.Single().Position == Vector3.one, "peer coordinates retained for monitor");
            }
            foreach(bool enabled in new[]{true,false})
            {
                string aid=Guid.NewGuid().ToString("N"),bid=Guid.NewGuid().ToString("N"),authority=Guid.NewGuid().ToString("N");
                var sa=new BeaconStore();var sb=new BeaconStore();sb.Merge(new Beacon{id=Guid.NewGuid().ToString("N"),author=bid,x=50,z=50});
                using(var pa=new PeerNetwork(aid,2026,sa,false))
                using(var pb=new PeerNetwork(bid,2026,sb,false))
                {
                    if(!enabled)Check(pa.ApplyBackupPolicy(false,10,authority),"admin can disable backups");
                    Check(pa.Start(0)&&pb.Start(0),"backup test sockets");pb.Connect("127.0.0.1",pa.Port);
                    for(int i=0;i<25;i++){pa.Tick(i*.2,Vector3.zero,0);pb.Tick(i*.2,Vector3.one,0);Thread.Sleep(5);}
                    Check(pb.BackupsEnabled==enabled,"backup policy propagates to peer");
                    Check(sa.Count==1,"live peer state synchronizes in either mode");
                    var ownedByB=Enumerable.Range(0,42).Where(r=>pa.Catalog.Primary(r%7,r/7)==bid).ToArray();
                    Check(ownedByB.Length>0,"departing peer owns regions");
                    pb.Dispose();pa.Tick(5,Vector3.zero,0);pa.Tick(14,Vector3.zero,0);
                    Check(sa.Count==(enabled?1:0),"ON retains replica; OFF removes departed state");
                    Check(ownedByB.All(r=>pa.Catalog.Available(r%7,r/7)==enabled),"ON recovers terrain; OFF makes departed owner regions unavailable");
                    if(!enabled)
                    {
                        var savedRecords=pa.Catalog.Records;var savedOwners=pa.Catalog.OwnerRecords;
                        var restored=new RegionCatalog(aid,2026);var restoreRing=new HashRing();restoreRing.SetPeers(new[]{aid});
                        restored.SetBackups(false,restoreRing,new[]{aid});restored.Restore(savedRecords,savedOwners,restoreRing,new[]{aid});
                        Check(ownedByB.All(r=>!restored.Available(r%7,r/7)),"OFF owner loss survives save/catalog restore");
                        Check(pa.ApplyBackupPolicy(true,11,authority),"admin can enable backups");
                        Check(ownedByB.All(r=>!pa.Catalog.Available(r%7,r/7)),"enabling does not invent lost replicas retroactively");
                        Check(!pa.ApplyBackupPolicy(false,10,authority),"old policy cannot override new revision");
                    }
                }
            }
            string report = "PASS: " + checks + " assertions. 10 km battlefield, 42 seeds / 2688 tiles, regional seams, mixed LOD stitching, hash ring, UDP synchronization, live admin policy propagation, backups ON recovery and backups OFF region/state loss.";
            Directory.CreateDirectory("Builds"); File.WriteAllText("Builds/validation.txt", report); Debug.Log(report);
        }
        [MenuItem("LookUpOn/Build Windows prototype")]
        public static void Build()
        {
            CreateScene(); Validate();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Frontier.unity" }, locationPathName = "Builds/Windows/LookUpOn.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build failed: " + report.summary.result);
            Debug.Log("LOOKUPON_BUILD_SUCCESS");
        }
    }
}
