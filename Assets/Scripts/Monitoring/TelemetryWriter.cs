using System;
using System.IO;
using System.Linq;
using LookUpOn.Core;
using LookUpOn.Networking;
using LookUpOn.World;
using UnityEngine;

namespace LookUpOn.Monitoring
{
    [Serializable] public class PeerView
    {
        public string id, endpoint, stateDigest, savedDigest;
        public bool local, backupsEnabled, retentionDisabled;
        public float x, y, z, yaw;
        public int chunkX, chunkZ, beacons, packets;
        public double ageSeconds;
        public int seedCopies;
    }
    [Serializable] public class RegionView { public int x, z, localBeacons, seed, loadedTiles; public float minX, maxX, minZ, maxZ; public bool available; public string missingOwner; public string[] owners, holders; }
    [Serializable] public class MonitorSnapshot
    {
        public string updatedUtc, peerId, status, stateDigest, savedDigest, savePath, saveStatus, lastSavedUtc;
        public int schema = 2, protocol = PeerNetwork.ProtocolVersion, worldVersion = WorldMath.WorldVersion, seed, udpPort, loadedChunks, pendingChunks, beaconCount, beaconLimit = BeaconStore.Capacity;
        public int travelLimitMetres = 5000, mapSizeMetres = 10000, regionColumns = 7, regionRows = 6, tilesPerRegion = 64, totalTiles = 2688;
        public float regionSizeX = WorldMath.RegionSizeX, regionSizeZ = WorldMath.RegionSizeZ, tileSizeX = WorldMath.TileSizeX, tileSizeZ = WorldMath.TileSizeZ;
        public bool networking, backupFileExists, backupsEnabled;
        public bool networkPaused, peerControlSupported;
        public long backupRevision, peerControlRevision;
        public string backupAuthority, adminControlStatus, peerControlStatus, runId;
        public long bytesIn, bytesOut;
        public PeerView[] peers;
        public RegionView[] regions;
        public string[] events;
    }
    public sealed class TelemetryWriter
    {
        readonly string path;
        double next;
        public string Error { get; private set; } = "";
        public TelemetryWriter(string peerId, string directory = "")
        {
            // Editor and standalone place this beside Assets / the executable, respectively.
            string root = string.IsNullOrEmpty(directory) ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Telemetry")) : Path.GetFullPath(directory);
            path = Path.Combine(root, peerId + ".json");
        }
        public void Publish(string id, int seed, Vector3 position, float yaw, PeerNetwork network, BeaconStore store, ChunkWorld world,
            string savePath, string saveStatus, string lastSavedUtc, AdminControl control)
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (now < next) return;
            next = now + 1;
            try
            {
                var local = new PeerView { id = id, local = true, endpoint = network.Running ? "local:" + network.Port : "offline",
                    x = position.x, y = position.y, z = position.z, yaw = yaw, chunkX = WorldMath.RegionX(position.x), chunkZ = WorldMath.RegionZ(position.z),
                    beacons = store.Count, stateDigest = store.Digest, savedDigest = network.SavedDigest, packets = network.Received,
                    seedCopies = network.Catalog.HeldCount, backupsEnabled = network.BackupsEnabled, retentionDisabled = !network.WantsBackups };
                var counts = store.Items.GroupBy(b => new Vector2Int(WorldMath.RegionX(b.x), WorldMath.RegionZ(b.z))).ToDictionary(g => g.Key, g => g.Count());
                var loaded = world.LoadedCoordinates.GroupBy(k => new Vector2Int(k.x / 8, k.y / 8)).ToDictionary(g => g.Key, g => g.Count());
                var snapshot = new MonitorSnapshot {
                    updatedUtc = DateTime.UtcNow.ToString("o"), peerId = id, seed = seed, status = network.Status, networking = network.Running,
                    udpPort = network.Port, loadedChunks = world.Loaded, pendingChunks = world.Pending, beaconCount = store.Count,
                    stateDigest = store.Digest, savedDigest = network.SavedDigest, savePath = savePath, saveStatus = saveStatus,
                    lastSavedUtc = lastSavedUtc, backupFileExists = network.RetainsBackups && !string.IsNullOrEmpty(savePath) && File.Exists(savePath + ".bak"), bytesIn = network.BytesReceived, bytesOut = network.BytesSent,
                    backupsEnabled = network.BackupsEnabled, backupRevision = network.BackupRevision, backupAuthority = network.BackupAuthority, adminControlStatus = control.Status,
                    networkPaused = network.Paused, peerControlSupported = true, peerControlRevision = control.PeerRevision,
                    peerControlStatus = control.PeerStatus, runId = control.RunId,
                    peers = new[] { local }.Concat(network.Peers.Values.OrderBy(p => p.Id).Select(p => new PeerView {
                        id = p.Id, endpoint = p.Endpoint.ToString(), x = p.Position.x, y = p.Position.y, z = p.Position.z, yaw = p.Yaw,
                        chunkX = WorldMath.RegionX(p.Position.x), chunkZ = WorldMath.RegionZ(p.Position.z), ageSeconds = Math.Max(0, now - p.LastSeen),
                        beacons = p.BeaconCount, packets = p.Packets, stateDigest = p.StateDigest, savedDigest = p.SavedDigest,
                        seedCopies = p.SeedCopies, backupsEnabled = p.BackupsEnabled, retentionDisabled = p.RetentionDisabled
                    })).ToArray(),
                    regions = Enumerable.Range(0,42).Select(r => new Vector2Int(r % 7, r / 7)).Select(k => new RegionView {
                        x = k.x, z = k.y, localBeacons = counts.TryGetValue(k, out int count) ? count : 0,
                        loadedTiles = loaded.TryGetValue(k, out int tiles) ? tiles : 0, seed = WorldMath.RegionSeed(seed,k.x,k.y),
                        minX = WorldMath.RegionOriginX(k.x), maxX = WorldMath.RegionOriginX(k.x+1), minZ = WorldMath.RegionOriginZ(k.y), maxZ = WorldMath.RegionOriginZ(k.y+1),
                        available = network.Catalog.Available(k.x,k.y), missingOwner = network.Catalog.MissingOwner(k.x,k.y),
                        owners = new[] { network.Catalog.Primary(k.x,k.y) }.Concat(network.BackupsEnabled ? network.Ring.Owners(k.x,k.y).Where(o => o != network.Catalog.Primary(k.x,k.y)) : Array.Empty<string>()).ToArray(),
                        holders = (network.Catalog.Records.Any(s => s.region == WorldMath.RegionId(k.x,k.y)) ? new[] { id } : Array.Empty<string>())
                            .Concat(network.Peers.Values.Where(p => now-p.LastSeen<=2 && p.SeedRegions.Contains(WorldMath.RegionId(k.x,k.y))).Select(p => p.Id)).ToArray()
                    }).ToArray(), events = network.Events.Reverse().ToArray()
                };
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path + ".tmp", JsonUtility.ToJson(snapshot));
                if (File.Exists(path)) File.Replace(path + ".tmp", path, null); else File.Move(path + ".tmp", path);
                Error = "";
            }
            catch (Exception e) { Error = e.Message; }
        }
    }
}
