using System;
using System.IO;
using LookUpOn.Networking;
using UnityEngine;

namespace LookUpOn.Monitoring
{
    [Serializable] public class BackupControl { public int seed; public bool backupsEnabled = true; public long revision; public string authority; }
    [Serializable] public class PeerControl { public string peerId, runId, connection; public int seed; public bool retentionDisabled; public long revision; }
    [Serializable] public class ControlDocument { public BackupControl[] worlds; public PeerControl[] peers; }
    public sealed class AdminControl
    {
        readonly string path;
        double next;
        public readonly string RunId = Guid.NewGuid().ToString("N");
        public long PeerRevision { get; private set; }
        public string PeerStatus { get; private set; } = "";
        public string Status { get; private set; } = "Admin control: default ON";
        public AdminControl(string controlFile = "")
        {
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (!Directory.Exists(Path.Combine(project, "Assets"))) project = Path.GetFullPath(Path.Combine(project, "..", ".."));
            path = string.IsNullOrEmpty(controlFile) ? Path.Combine(project, "AdminPanel", "controls.json") : Path.GetFullPath(controlFile);
        }
        public void Poll(int seed, string peerId, PeerNetwork network)
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (now < next) return; next = now + .5;
            try
            {
                if (!File.Exists(path)) return;
                if (new FileInfo(path).Length > 65536) throw new IOException("Control file too large");
                var document = JsonUtility.FromJson<ControlDocument>(File.ReadAllText(path));
                if (document == null) return;
                foreach (var control in document.worlds ?? Array.Empty<BackupControl>())
                    if (control?.seed == seed) network.ApplyBackupPolicy(control.backupsEnabled, control.revision, control.authority);
                foreach (var control in document.peers ?? Array.Empty<PeerControl>())
                {
                    if (control == null || control.seed != seed || control.peerId != peerId || control.revision <= PeerRevision) continue;
                    network.SetLocalRetention(!control.retentionDisabled);
                    PeerStatus = (control.runId == RunId ? control.connection : "") switch
                    {
                        "disconnect" => network.Pause() ? "Networking disconnected" : "Peer is already offline",
                        "reconnect" => network.Resume() ? "Networking resumed" : network.Status,
                        _ => network.RetainsBackups ? "Retaining backup copies" : "Primary copies only"
                    };
                    PeerRevision = control.revision;
                }
                Status = "Local admin file connected";
            }
            catch (Exception e) { Status = "Admin read failed: " + e.Message; }
        }
    }
}
