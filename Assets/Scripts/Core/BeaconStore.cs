using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Linq;

namespace LookUpOn.Core
{
    [Serializable]
    public class Beacon
    {
        public string id;
        public string author;
        public float x, z;
    }
    // An immutable, grow-only set. Identical IDs resolve deterministically even on conflicting input.
    public sealed class BeaconStore
    {
        public const int Capacity = 4096;
        readonly SortedDictionary<string, Beacon> items = new SortedDictionary<string, Beacon>(StringComparer.Ordinal);
        public IEnumerable<Beacon> Items => items.Values;
        public int Count => items.Count;
        public int RemoveWhere(Func<Beacon, bool> predicate)
        {
            string[] remove = items.Values.Where(predicate).Select(b => b.id).ToArray();
            foreach (string id in remove) items.Remove(id);
            if (remove.Length > 0) { Revision++; digest = null; }
            return remove.Length;
        }
        public int Revision { get; private set; }
        string digest;
        public string Digest
        {
            get
            {
                if (digest != null) return digest;
                using (var stream = new MemoryStream())
                using (var writer = new BinaryWriter(stream))
                using (var sha = SHA256.Create())
                {
                    foreach (var b in items.Values)
                    { writer.Write(b.id); writer.Write(b.author); writer.Write(b.x == 0 ? 0f : b.x); writer.Write(b.z == 0 ? 0f : b.z); }
                    writer.Flush(); digest = BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
                    return digest;
                }
            }
        }
        public bool Merge(Beacon b)
        {
            if (b == null || !Guid.TryParseExact(b.id, "N", out _) || !Guid.TryParseExact(b.author, "N", out _)
                || float.IsNaN(b.x) || float.IsInfinity(b.x) || float.IsNaN(b.z) || float.IsInfinity(b.z)
                || !WorldMath.Inside(b.x, b.z)) return false;
            if (items.TryGetValue(b.id, out var old))
            {
                int order = StringComparer.Ordinal.Compare(b.author, old.author);
                if (order == 0) order = b.x.CompareTo(old.x);
                if (order == 0) order = b.z.CompareTo(old.z);
                if (order >= 0) return false;
            }
            else if (items.Count >= Capacity) return false;
            items[b.id] = new Beacon { id = b.id, author = b.author, x = b.x, z = b.z };
            Revision++; digest = null;
            return true;
        }
    }
}
