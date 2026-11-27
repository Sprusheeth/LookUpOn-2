using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using LookUpOn.Core;
using UnityEngine;

namespace LookUpOn.Networking
{
    [Serializable]
    public class Packet
    {
        public string game = "LookUpOn";
        public int protocol = PeerNetwork.ProtocolVersion, worldVersion = WorldMath.WorldVersion, seed;
        public string id;
        public float x, y, z, yaw;
        public Beacon[] beacons;
        public int beaconCount;
        public string stateDigest, savedDigest;
        public bool backupsEnabled = true;
        public bool retentionDisabled;
        public long backupRevision;
        public string backupAuthority;
        public RegionSeedRecord[] regionSeeds;
    }
    public sealed class RemotePeer
    {
        public string Id;
        public IPEndPoint Endpoint;
        public Vector3 Position;
        public float Yaw;
        public double LastSeen;
        public int Packets, BeaconCount;
        public string StateDigest, SavedDigest;
        public bool BackupsEnabled;
        public bool RetentionDisabled;
        public int SeedCopies;
        public int[] SeedRegions = Array.Empty<int>();
    }
    // Small trusted-LAN mesh. Repeated bounded anti-entropy pages recover lost UDP updates.
    public sealed class PeerNetwork : IDisposable
    {
        public const int ProtocolVersion = 4;
        public readonly Dictionary<string, RemotePeer> Peers = new Dictionary<string, RemotePeer>();
        public readonly HashRing Ring = new HashRing();
        public readonly RegionCatalog Catalog;
        readonly BeaconStore store;
        readonly int seed;
        readonly string id;
        readonly bool discover;
        UdpClient socket;
        IPEndPoint bootstrap;
        IPEndPoint pausedBootstrap;
        int pausedPort;
        string[] pausedMembers;
        double now;
        double nextSend;
        int page;
        public int Port { get; private set; }
        public int Received { get; private set; }
        public long BytesReceived { get; private set; }
        public long BytesSent { get; private set; }
        public string SavedDigest { get; set; } = "";
        public bool BackupsEnabled { get; private set; } = true;
        public bool WantsBackups { get; private set; } = true;
        public bool RetainsBackups => BackupsEnabled && WantsBackups;
        public bool Paused => pausedPort > 0;
        public long BackupRevision { get; private set; }
        public string BackupAuthority { get; private set; } = "";
        public bool ApplyBackupPolicy(bool enabled, long revision, string authority)
        {
            if (revision < 1 || revision > 9007199254740991L || !Guid.TryParseExact(authority, "N", out _)) return false;
            if (revision < BackupRevision || (revision == BackupRevision && StringComparer.Ordinal.Compare(authority, BackupAuthority) <= 0)) return false;
            BackupsEnabled = enabled; BackupRevision = revision; BackupAuthority = authority; SavedDigest = "";
            UpdateRetention();
            PruneLiveState();
            Event("Admin backups " + (enabled ? "ON" : "OFF") + " · revision " + revision);
            return true;
        }
        public void SetLocalRetention(bool enabled)
        {
            if (enabled == WantsBackups) return;
            WantsBackups = enabled;
            SavedDigest = "";
            UpdateRetention();
            PruneLiveState();
            Event("Local backup retention " + (enabled ? "ON" : "OFF"));
        }
        void UpdateRetention() => Catalog.SetRetention(BackupsEnabled, RetainsBackups, Ring, pausedMembers ?? Peers.Keys.Concat(new[] { id }));
        void PruneLiveState()
        {
            if (!RetainsBackups) store.RemoveWhere(b => b.author != id && !Peers.ContainsKey(b.author));
        }
        public readonly Queue<string> Events = new Queue<string>();
        void Event(string message)
        {
            Events.Enqueue(DateTime.UtcNow.ToString("HH:mm:ss") + " UTC · " + message);
            while (Events.Count > 64) Events.Dequeue();
        }
        public string Status { get; private set; } = "Offline exploration";
        public bool Running => socket != null;
        public PeerNetwork(string id, int seed, BeaconStore store, bool discover = true)
        {
            this.id = id; this.seed = seed; this.store = store; this.discover = discover;
            Catalog = new RegionCatalog(id, seed);
            RefreshRing();
        }
        void RefreshRing()
        {
            var members = Peers.Keys.Concat(new[] { id }).ToArray();
            Ring.SetPeers(members);
            Catalog.Refresh(Ring, members, r => Peers.Values.Any(p => now - p.LastSeen <= 2 && p.SeedRegions.Contains(r)));
        }
        public bool Start(int port)
        {
            Dispose();
            return Open(port);
        }
        bool Open(int port)
        {
            try
            {
                socket = new UdpClient(new IPEndPoint(IPAddress.Any, port));
                socket.Client.Blocking = false;
                socket.EnableBroadcast = true;
                Port = ((IPEndPoint)socket.Client.LocalEndPoint).Port;
                Status = "Listening on UDP " + Port;
                Event(Status);
                nextSend = 0;
                return true;
            }
            catch (SocketException e) { socket?.Dispose(); socket = null; Status = "Cannot listen: " + e.SocketErrorCode; return false; }
        }
        public bool Pause()
        {
            if (!Running) return Paused;
            pausedPort = Port;
            pausedBootstrap = bootstrap;
            pausedMembers = Peers.Keys.Concat(new[] { id }).ToArray();
            socket.Dispose();
            socket = null;
            Peers.Clear();
            Status = "Peer networking disconnected by admin";
            Event(Status);
            return true;
        }
        public bool Resume()
        {
            if (!Paused) return Running;
            if (!Open(pausedPort)) return false;
            bootstrap = pausedBootstrap;
            pausedPort = 0;
            pausedBootstrap = null;
            pausedMembers = null;
            return true;
        }
        public bool Connect(string address, int port)
        {
            if (!Running || !IPAddress.TryParse(address, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork || port < 1 || port > 65535)
            { Status = "Use an IPv4 address and a port from 1 to 65535"; return false; }
            bootstrap = new IPEndPoint(ip, port); nextSend = 0;
            Status = "Looking for " + bootstrap; return true;
        }
        public void Tick(double now, Vector3 position, float yaw)
        {
            this.now = now;
            if (!Running) return;
            for (int count = 0; count < 64 && socket.Available > 0; count++)
            {
                try
                {
                    IPEndPoint from = new IPEndPoint(IPAddress.Any, 0);
                    byte[] bytes = socket.Receive(ref from);
                    if (bytes.Length > 12000) continue;
                    Packet p = JsonUtility.FromJson<Packet>(Encoding.UTF8.GetString(bytes));
                    if (p == null || p.game != "LookUpOn" || p.protocol != ProtocolVersion || p.worldVersion != WorldMath.WorldVersion || p.seed != seed || p.id == id
                        || !Guid.TryParseExact(p.id, "N", out _) || !Finite(p.x) || !Finite(p.y) || !Finite(p.z) || !Finite(p.yaw)
                        || !WorldMath.Inside(p.x, p.z) || Math.Abs(p.y) > 10000
                        || p.beaconCount < 0 || p.beaconCount > BeaconStore.Capacity
                        || !ValidDigest(p.stateDigest) || (!string.IsNullOrEmpty(p.savedDigest) && !ValidDigest(p.savedDigest))
                        || (p.beacons != null && p.beacons.Length > 24) || (p.regionSeeds != null && p.regionSeeds.Length > 42)) continue;
                    if (!Peers.TryGetValue(p.id, out var peer))
                    {
                        if (Peers.Count >= 16) continue;
                        peer = new RemotePeer { Id = p.id }; Peers.Add(p.id, peer); RefreshRing();
                        Event("Peer joined " + p.id.Substring(0, 8) + " at " + from);
                    }
                    peer.Endpoint = from; peer.Position = new Vector3(p.x, p.y, p.z); peer.Yaw = p.yaw; peer.LastSeen = now;
                    peer.Packets++; peer.BeaconCount = p.beaconCount; peer.StateDigest = p.stateDigest; peer.SavedDigest = p.savedDigest;
                    peer.BackupsEnabled = p.backupsEnabled;
                    peer.RetentionDisabled = p.retentionDisabled;
                    peer.SeedRegions = p.regionSeeds == null ? Array.Empty<int>() : p.regionSeeds.Where(r => r != null && r.region >= 0 && r.region < 42 &&
                        r.seed == WorldMath.RegionSeed(seed, r.region % 7, r.region / 7)).Select(r => r.region).Distinct().ToArray();
                    peer.SeedCopies = peer.SeedRegions.Length;
                    ApplyBackupPolicy(p.backupsEnabled, p.backupRevision, p.backupAuthority);
                    if (BackupsEnabled && p.backupsEnabled && p.regionSeeds != null) foreach (var record in p.regionSeeds) Catalog.Merge(record);
                    BytesReceived += bytes.Length;
                    if (p.beacons != null) foreach (var beacon in p.beacons)
                        if (RetainsBackups || beacon != null && beacon.author == p.id) store.Merge(beacon);
                    Received++; Status = "Mesh online · " + Peers.Count + " remote peer(s)";
                }
                catch (SocketException) { break; }
                catch (ArgumentException) { /* Ignore malformed JSON. */ }
            }
            foreach (var key in Peers.Where(p => now - p.Value.LastSeen > 8).Select(p => p.Key).ToArray())
            { Peers.Remove(key); RefreshRing(); Event("Peer expired " + key.Substring(0, 8) + "; region assignments recalculated"); Status = Peers.Count == 0 ? "Listening · no peers" : "Mesh online · " + Peers.Count + " remote peer(s)"; }
            PruneLiveState();
            if (now < nextSend) return;
            nextSend = now + .15;
            Beacon[] all = store.Items.Where(b => RetainsBackups || b.author == id).ToArray();
            if (page >= all.Length) page = 0;
            Beacon[] batch = all.Skip(page).Take(24).ToArray(); page += batch.Length;
            var packet = new Packet { id = id, seed = seed, x = position.x, y = position.y, z = position.z, yaw = yaw, beacons = batch,
                beaconCount = store.Count, stateDigest = store.Digest, savedDigest = RetainsBackups ? SavedDigest : "",
                retentionDisabled = !WantsBackups,
                backupsEnabled = BackupsEnabled, backupRevision = BackupRevision, backupAuthority = BackupAuthority, regionSeeds = Catalog.Records };
            byte[] data = Encoding.UTF8.GetBytes(JsonUtility.ToJson(packet));
            foreach (var peer in Peers.Values) Send(data, peer.Endpoint);
            if (bootstrap != null) Send(data, bootstrap);
            if (discover)
                for (int p = 47770; p < 47778; p++)
                    if (p != Port) Send(data, new IPEndPoint(IPAddress.Broadcast, p));
        }
        static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
        static bool ValidDigest(string s) => s != null && s.Length == 64 && s.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
        void Send(byte[] data, IPEndPoint endpoint)
        {
            try { BytesSent += socket.Send(data, data.Length, endpoint); }
            catch (SocketException e) { Status = "UDP send: " + e.SocketErrorCode; }
        }
        public void Dispose()
        {
            socket?.Dispose(); socket = null; bootstrap = null; pausedPort = 0; pausedBootstrap = null; pausedMembers = null;
            Peers.Clear(); RefreshRing(); PruneLiveState(); Status = "Offline exploration";
        }
    }
}
