using System;
using System.Collections.Generic;
using System.Linq;

namespace LookUpOn.Core
{
    public sealed class HashRing
    {
        readonly List<KeyValuePair<uint, string>> nodes = new List<KeyValuePair<uint, string>>();
        public void SetPeers(IEnumerable<string> peers)
        {
            nodes.Clear();
            foreach (string peer in peers.Distinct().OrderBy(p => p, StringComparer.Ordinal))
                for (int i = 0; i < 64; i++) nodes.Add(new KeyValuePair<uint, string>(NodeKey(peer, i), peer));
            nodes.Sort((a, b) => { int c = a.Key.CompareTo(b.Key); return c != 0 ? c : StringComparer.Ordinal.Compare(a.Value, b.Value); });
        }
        // Mix the full hash so adjacent virtual-node suffixes do not bunch together.
        static uint NodeKey(string peer, int index)
        {
            uint key = WorldMath.Hash(peer + ":" + index);
            unchecked
            {
                key = (key ^ (key >> 16)) * 0x7feb352du;
                key = (key ^ (key >> 15)) * 0x846ca68bu;
            }
            return key ^ (key >> 16);
        }
        public string[] Owners(int x, int z, int replicas = 3)
        {
            if (nodes.Count == 0 || replicas <= 0) return Array.Empty<string>();
            uint key = WorldMath.Hash(x + ":" + z);
            int start = nodes.FindIndex(n => n.Key >= key);
            if (start < 0) start = 0;
            var result = new List<string>();
            for (int i = 0; i < nodes.Count && result.Count < replicas; i++)
            {
                string id = nodes[(start + i) % nodes.Count].Value;
                if (!result.Contains(id)) result.Add(id);
            }
            return result.ToArray();
        }
    }
}
