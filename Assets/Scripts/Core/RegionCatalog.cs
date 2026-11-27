using System;
using System.Collections.Generic;
using System.Linq;

namespace LookUpOn.Core
{
    [Serializable] public class RegionSeedRecord { public int region, seed; }
    public sealed class RegionCatalog
    {
        readonly string localId;
        readonly int worldSeed;
        readonly Dictionary<int, int> held = new Dictionary<int, int>();
        readonly string[] primary = new string[WorldMath.RegionCount];
        readonly string[] missingOwner = new string[WorldMath.RegionCount];
        HashSet<string> members = new HashSet<string>();
        Func<int, bool> reportedCopy;
        public bool BackupsEnabled { get; private set; } = true;
        public bool RetainsBackups { get; private set; } = true;
        public RegionCatalog(string localId, int seed) { this.localId = localId; worldSeed = seed; }
        public void Refresh(HashRing ring, IEnumerable<string> active, Func<int, bool> remoteCopy = null)
        {
            var next = new HashSet<string>(active);
            reportedCopy = remoteCopy;
            for (int r = 0; r < WorldMath.RegionCount; r++)
            {
                int x = r % WorldMath.RegionColumns, z = r / WorldMath.RegionColumns;
                string owner = ring.Owners(x, z, 1).FirstOrDefault();
                bool hasCopy = BackupsEnabled && (held.ContainsKey(r) || remoteCopy?.Invoke(r) == true);
                if (primary[r] != null && !next.Contains(primary[r]) && !hasCopy) missingOwner[r] ??= primary[r];
                if (missingOwner[r] != null && (next.Contains(missingOwner[r]) || hasCopy)) missingOwner[r] = null;
                primary[r] = owner;
                if (missingOwner[r] == null && owner == localId) held[r] = WorldMath.RegionSeed(worldSeed, x, z);
                if (!RetainsBackups && (owner != localId || missingOwner[r] != null)) held.Remove(r);
            }
            members = next;
        }
        public void SetBackups(bool enabled, HashRing ring, IEnumerable<string> active)
        {
            SetRetention(enabled, enabled, ring, active);
        }
        public void SetRetention(bool enabled, bool retains, HashRing ring, IEnumerable<string> active)
        {
            if (enabled == BackupsEnabled && retains == RetainsBackups) return;
            BackupsEnabled = enabled;
            RetainsBackups = retains;
            Refresh(ring, active, reportedCopy);
        }
        public bool Merge(RegionSeedRecord record)
        {
            if (record == null || record.region < 0 || record.region >= WorldMath.RegionCount || !BackupsEnabled) return false;
            int r = record.region;
            if (record.seed != WorldMath.RegionSeed(worldSeed, r % WorldMath.RegionColumns, r / WorldMath.RegionColumns)) return false;
            if (RetainsBackups || primary[r] == localId) held[r] = record.seed;
            missingOwner[r] = null;
            return true;
        }
        public bool Available(int x, int z)
        {
            if (!WorldMath.ValidRegion(x, z)) return false;
            int r = WorldMath.RegionId(x, z);
            return missingOwner[r] == null && (held.ContainsKey(r) || members.Contains(primary[r] ?? ""));
        }
        public string MissingOwner(int x, int z) => missingOwner[WorldMath.RegionId(x, z)];
        public string Primary(int x, int z) => MissingOwner(x, z) ?? primary[WorldMath.RegionId(x, z)];
        public RegionSeedRecord[] Records => held.OrderBy(p => p.Key).Select(p => new RegionSeedRecord { region = p.Key, seed = p.Value }).ToArray();
        public int HeldCount => held.Count;
        public string[] OwnerRecords => Enumerable.Range(0,42).Select(r => missingOwner[r] ?? primary[r]).ToArray();
        public void Restore(RegionSeedRecord[] records, string[] owners, HashRing ring, IEnumerable<string> active)
        {
            if (records == null || owners == null || owners.Length != 42) return;
            held.Clear();
            foreach(var record in records)
                if(record!=null && record.region>=0 && record.region<42 && record.seed==WorldMath.RegionSeed(worldSeed,record.region%7,record.region/7))held[record.region]=record.seed;
            for(int r=0;r<42;r++)
                if(Guid.TryParseExact(owners[r],"N",out _))primary[r]=owners[r];
            Refresh(ring,active);
        }
    }
}
