using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LookUpOn.Core;
using LookUpOn.Networking;
using LookUpOn.Player;
using LookUpOn.World;
using LookUpOn.Monitoring;
using UnityEngine;

namespace LookUpOn
{
    [Serializable] public class SaveData { public int version = 2, worldVersion = WorldMath.WorldVersion; public int seed; public string peerId; public bool backupsEnabled; public Beacon[] beacons; public RegionSeedRecord[] regionSeeds; public string[] regionOwners; }
    [Serializable] public class SmokeReport { public bool passed, aboveGround; public int chunks, peers, beacons, packets; public float playerY, groundY; }
    public sealed class LookUpOnGame : MonoBehaviour
    {
        public int Seed = 2026;
        public Shader TerrainShader;
        public Shader StandardShader;
        readonly BeaconStore beacons = new BeaconStore();
        readonly Dictionary<string, GameObject> markers = new Dictionary<string, GameObject>();
        readonly Dictionary<string, GameObject> avatars = new Dictionary<string, GameObject>();
        readonly List<Material> materials = new List<Material>();
        ChunkWorld world;
        Explorer player;
        PeerNetwork network;
        TelemetryWriter telemetry;
        AdminControl adminControl;
        SaveData loadedSave;
        FileStream saveLease;
        string saveStatus = "Not saved yet", lastSavedUtc = "";
        string peerId, savePath, notice = "10 km battlefield · 42 regions · 64 tiles per region";
        string address = "127.0.0.1", remotePort = "47770", localPort = "47770";
        Material beaconMaterial, avatarMaterial;
        double nextSave;
        string lastSaved = "";
        GUIStyle label, small, title, heading, button, field, outdoorSmall, outdoorTitle, outdoorLabel;
        Texture2D white;
        bool noSave;
        void Start()
        {
            Application.runInBackground = true; Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            string[] args = Environment.GetCommandLineArgs();
            string Arg(string key, string fallback) { int i = Array.IndexOf(args, key); return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback; }
            if (int.TryParse(Arg("--seed", Seed.ToString()), out int seed)) Seed = seed;
            bool smoke = args.Contains("--smoke");
            noSave = smoke || args.Contains("--no-save");
            peerId = Guid.NewGuid().ToString("N");
            savePath = Path.Combine(Application.persistentDataPath, "battlefield-" + Seed + "-v" + WorldMath.WorldVersion + ".json");
            if (!noSave) AcquireSaveSlot();
            if (!noSave) Load();
            if (noSave) { savePath = ""; saveStatus = "Persistence disabled for this run"; }
            network = new PeerNetwork(peerId, Seed, beacons, !smoke);
            adminControl = new AdminControl(Arg("--control-file", "")); adminControl.Poll(Seed, peerId, network);
            if(loadedSave!=null)network.Catalog.Restore(loadedSave.regionSeeds,loadedSave.regionOwners,network.Ring,new[]{peerId});
            telemetry = new TelemetryWriter(peerId, Arg("--telemetry-dir", ""));
            ConfigureLighting();
            world = new GameObject("Procedural world · seed " + Seed).AddComponent<ChunkWorld>(); world.Seed = Seed;
            world.RegionAvailable = network.Catalog.Available;
            world.TerrainMaterial = new Material(TerrainShader); materials.Add(world.TerrainMaterial);
            world.TerrainMaterial.SetFloat("_ValleyPhase", WorldMath.ValleyPhase(Seed));
            world.TerrainMaterial.SetFloat("_SurfaceSeed", WorldMath.Grid(7, 8, Seed) * 50);
            world.SceneryMaterial = ValleyMaterial("ValleyVegetation");
            world.WaterMaterial = ValleyMaterial("ValleyWater");
            beaconMaterial = Material(new Color(.99f, .66f, .28f)); avatarMaterial = Material(new Color(.35f, .86f, .91f));
            var explorer = new GameObject("Local explorer");
            float spawnZ = -320;
            if (int.TryParse(Arg("--spawn-offset", "0"), out int offset)) spawnZ += Mathf.Clamp(offset, -1500, 1500);
            float valleyPhase = WorldMath.ValleyPhase(Seed);
            float spawnX = WorldMath.RoadX(spawnZ, valleyPhase);
            var spawn = new Vector3(spawnX, WorldMath.Height(spawnX, spawnZ, Seed) + 3, spawnZ);
            explorer.transform.position = spawn;
            float roadHeading = Mathf.Atan2(WorldMath.RoadX(spawnZ + 80, valleyPhase) - spawnX, 80) * Mathf.Rad2Deg;
            explorer.transform.rotation = Quaternion.Euler(0, roadHeading + 16, 0);
            var controller = explorer.AddComponent<CharacterController>(); controller.height = 1.8f; controller.center = Vector3.up * .9f; controller.radius = .35f; controller.stepOffset = .4f;
            player = explorer.AddComponent<Explorer>(); player.Seed = Seed; player.Spawn = spawn;
            player.CanStand = world.CanStand;
            var cameraObject = new GameObject("Explorer camera"); cameraObject.tag = "MainCamera"; cameraObject.transform.SetParent(explorer.transform, false);
            cameraObject.transform.localPosition = Vector3.up * 1.65f; cameraObject.transform.localRotation = Quaternion.Euler(3, 0, 0);
            player.View = cameraObject.AddComponent<Camera>(); player.View.fieldOfView = 72; player.View.farClipPlane = 2000;
            player.View.clearFlags = CameraClearFlags.Skybox;
            cameraObject.AddComponent<AudioListener>(); world.Observer = explorer.transform; world.WarmStart(explorer.transform.position);
            Physics.SyncTransforms();
            if(Physics.Raycast(explorer.transform.position+Vector3.up*500,Vector3.down,out var ground,1000,1<<ChunkWorld.TerrainLayer))
            { controller.enabled=false;explorer.transform.position=ground.point+Vector3.up*.3f;controller.enabled=true; }
            if (args.Contains("--port"))
            {
                localPort = Arg("--port", "47770"); StartNetwork();
                if (args.Contains("--connect")) { address = Arg("--connect", "127.0.0.1"); remotePort = Arg("--remote-port", "47770"); Connect(); }
            }
            if (smoke) StartCoroutine(Smoke(Arg("--report", "smoke.json"), Arg("--capture", ""), args.Contains("--place-beacon")));
            else if (args.Contains("--place-beacon")) PlaceBeacon();
            Debug.Log("LOOKUPON_READY seed=" + Seed);
        }
        Material ValleyMaterial(string name)
        {
            var shader = Resources.Load<Shader>(name);
            if (!shader || !shader.isSupported) throw new InvalidOperationException("Required valley shader unavailable: " + name);
            var material = new Material(shader);
            materials.Add(material);
            return material;
        }
        void ConfigureLighting()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .00175f;
            RenderSettings.fogColor = new Color(.68f, .73f, .74f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.49f, .56f, .67f);
            RenderSettings.ambientEquatorColor = new Color(.35f, .39f, .38f);
            RenderSettings.ambientGroundColor = new Color(.24f, .24f, .19f);
            var sun = new GameObject("Valley sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, .91f, .75f);
            sun.intensity = .95f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .72f;
            sun.transform.rotation = Quaternion.Euler(38, -48, 0);
            QualitySettings.shadowDistance = 185;
            RenderSettings.sun = sun;
            var sky = ValleyMaterial("ValleySky");
            sky.SetVector("_SunDirection", -sun.transform.forward);
            RenderSettings.skybox = sky;
        }
        Material Material(Color color)
        {
            var m = new Material(StandardShader); m.color = color; m.SetFloat("_Glossiness", .05f); materials.Add(m); return m;
        }
        void Update()
        {
            if (network == null) return;
            adminControl.Poll(Seed, peerId, network);
            network.Tick(Time.realtimeSinceStartupAsDouble, player.transform.position, player.transform.eulerAngles.y);
            if (player.Captured && Input.GetKeyDown(KeyCode.E)) PlaceBeacon();
            foreach (var b in beacons.Items)
            {
                var pos = new Vector3(b.x, WorldMath.Height(b.x, b.z, Seed), b.z);
                if (!markers.TryGetValue(b.id, out var marker))
                {
                    marker = new GameObject("Survey beacon " + b.id.Substring(0, 6)); markers.Add(b.id, marker);
                    Primitive(marker.transform, PrimitiveType.Cylinder, Vector3.up * 1.8f, new Vector3(.15f, 1.8f, .15f), beaconMaterial);
                    Primitive(marker.transform, PrimitiveType.Cube, Vector3.up * 3.8f, Vector3.one * .65f, beaconMaterial).transform.localRotation = Quaternion.Euler(0, 45, 45);
                }
                marker.transform.position = pos;
                marker.SetActive((pos - player.transform.position).sqrMagnitude < 240 * 240);
            }
            var activeMarkers = new HashSet<string>(beacons.Items.Select(b => b.id));
            foreach (string key in markers.Keys.Where(k => !activeMarkers.Contains(k)).ToArray()) { Destroy(markers[key]); markers.Remove(key); }
            foreach (var peer in network.Peers.Values)
            {
                if (!avatars.TryGetValue(peer.Id, out var avatar))
                {
                    avatar = new GameObject("Peer " + peer.Id.Substring(0, 6)); avatars.Add(peer.Id, avatar);
                    Primitive(avatar.transform, PrimitiveType.Capsule, Vector3.up, new Vector3(.7f, 1, .7f), avatarMaterial);
                    Primitive(avatar.transform, PrimitiveType.Cube, new Vector3(0, 1.7f, .35f), new Vector3(.5f, .2f, .2f), beaconMaterial);
                    avatar.transform.position = peer.Position;
                }
                avatar.transform.position = Vector3.Lerp(avatar.transform.position, peer.Position, 12 * Time.deltaTime);
                avatar.transform.rotation = Quaternion.Slerp(avatar.transform.rotation, Quaternion.Euler(0, peer.Yaw, 0), 12 * Time.deltaTime);
            }
            foreach (string id in avatars.Keys.Where(id => !network.Peers.ContainsKey(id)).ToArray()) { Destroy(avatars[id]); avatars.Remove(id); }
            if (!noSave && Time.realtimeSinceStartupAsDouble > nextSave) { nextSave = Time.realtimeSinceStartupAsDouble + 3; Save(); }
            telemetry.Publish(peerId, Seed, player.transform.position, player.transform.eulerAngles.y, network, beacons, world, savePath, saveStatus, lastSavedUtc, adminControl);
        }
        static GameObject Primitive(Transform root, PrimitiveType type, Vector3 p, Vector3 s, Material m)
        {
            var obj = GameObject.CreatePrimitive(type); obj.transform.SetParent(root, false); obj.transform.localPosition = p;
            obj.transform.localScale = s; obj.GetComponent<Renderer>().sharedMaterial = m; Destroy(obj.GetComponent<Collider>()); return obj;
        }
        void PlaceBeacon()
        {
            if (beacons.Count >= BeaconStore.Capacity) { notice = "Prototype beacon limit reached (4096)."; return; }
            Vector3 p = player.transform.position + player.transform.forward * 4;
            if (!WorldMath.Inside(p.x, p.z) || !network.Catalog.Available(WorldMath.RegionX(p.x), WorldMath.RegionZ(p.z))) return;
            beacons.Merge(new Beacon { id = Guid.NewGuid().ToString("N"), author = peerId, x = p.x, z = p.z });
            notice = "Survey beacon placed · replicated while peers are connected.";
        }
        void Load()
        {
            try
            {
                if (!File.Exists(savePath)) return;
                if (new FileInfo(savePath).Length > 2000000) { notice = "Saved world is too large; started without loading it."; return; }
                var save = JsonUtility.FromJson<SaveData>(File.ReadAllText(savePath));
                if (save != null && save.version == 2 && save.worldVersion == WorldMath.WorldVersion && save.seed == Seed)
                {
                    loadedSave=save;
                    if (Guid.TryParseExact(save.peerId, "N", out _)) peerId = save.peerId;
                    if (save.beacons != null) foreach (var b in save.beacons) beacons.Merge(b);
                }
            }
            catch (Exception e) { notice = "Could not load beacons: " + e.Message; }
        }
        void AcquireSaveSlot()
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                for (int slot = 0; slot < 32; slot++)
                {
                    string candidate = Path.Combine(Application.persistentDataPath, "battlefield-" + Seed + "-slot" + slot + "-v" + WorldMath.WorldVersion + ".json");
                    try { saveLease = new FileStream(candidate + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
                    catch (IOException) { continue; }
                    savePath = candidate; localPort = (47770 + slot).ToString(); return;
                }
                throw new IOException("All 32 local save slots are in use.");
            }
            catch (Exception e) { saveLease?.Dispose(); saveLease = null; saveStatus = "Persistence disabled: " + e.Message; notice = saveStatus; }
        }
        void Save()
        {
            if (saveLease == null) return;
            try
            {
                string json = JsonUtility.ToJson(new SaveData { seed = Seed, peerId = peerId, backupsEnabled = network.BackupsEnabled,
                    beacons = beacons.Items.Where(b => network.RetainsBackups || b.author == peerId).ToArray(), regionSeeds = network.Catalog.Records, regionOwners = network.Catalog.OwnerRecords }, true);
                if (json == lastSaved) return;
                Directory.CreateDirectory(Path.GetDirectoryName(savePath)); File.WriteAllText(savePath + ".tmp", json);
                if (File.Exists(savePath)) File.Replace(savePath + ".tmp", savePath, network.RetainsBackups ? savePath + ".bak" : null); else File.Move(savePath + ".tmp", savePath);
                lastSaved = json;
                network.SavedDigest = network.RetainsBackups ? beacons.Digest : ""; lastSavedUtc = DateTime.UtcNow.ToString("o"); saveStatus = network.RetainsBackups ? "Saved with replicas" : "Own data saved · backups OFF";
            }
            catch (Exception e) { saveStatus = "Save failed: " + e.Message; notice = saveStatus; network.SavedDigest = ""; }
        }
        void StartNetwork()
        {
            if (int.TryParse(localPort, out int port) && port >= 1 && port <= 65535) network.Start(port);
            else notice = "Local port must be between 1 and 65535.";
        }
        void Connect()
        {
            if (int.TryParse(remotePort, out int port)) network.Connect(address.Trim(), port); else notice = "Enter a numeric peer port.";
        }
        void OnApplicationQuit() { if (!noSave && network != null) Save(); network?.Dispose(); saveLease?.Dispose(); }
        void OnDestroy() { network?.Dispose(); saveLease?.Dispose(); foreach (var m in materials) if (m) Destroy(m); }
        void Styles()
        {
            if (label != null) return;
            white = Texture2D.whiteTexture;
            label = new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = new Color(.86f, .91f, .9f) } };
            small = new GUIStyle(label) { fontSize = 11, normal = { textColor = new Color(.61f, .73f, .73f) } };
            title = new GUIStyle(label) { fontSize = 38, fontStyle = FontStyle.Bold };
            heading = new GUIStyle(label) { fontSize = 21, fontStyle = FontStyle.Bold };
            outdoorSmall = new GUIStyle(small) { normal = { textColor = new Color(.10f, .24f, .25f) } };
            outdoorTitle = new GUIStyle(title) { normal = { textColor = new Color(.07f, .18f, .20f) } };
            outdoorLabel = new GUIStyle(label) { normal = { textColor = new Color(.10f, .24f, .25f) } };
            button = new GUIStyle(GUI.skin.button) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            field = new GUIStyle(GUI.skin.textField) { fontSize = 14, padding = new RectOffset(10, 10, 7, 7) };
        }
        void Box(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, white); GUI.color = Color.white; }
        void Text(float x, float y, float w, string value, GUIStyle style = null) => GUI.Label(new Rect(x, y, w, 48), value, style ?? label);
        void OnGUI()
        {
            if (player == null) return; Styles();
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
            float w = Screen.width / scale, h = Screen.height / scale;
            Color panel = new Color(.035f, .09f, .11f, .91f), accent = new Color(.61f, .89f, .71f);
            Box(new Rect(26, 24, 285, 103), panel); Box(new Rect(26, 24, 3, 103), accent);
            Text(44, 32, 260, "LOOK / UP / ON", heading); Text(45, 65, 260, "BATTLEFIELD     /     10 KM × 10 KM", small);
            Text(45, 91, 260, "SEED " + Seed + "   ·   RECIPE " + WorldMath.WorldVersion.ToString("00"), small);
            Box(new Rect(w - 285, 24, 259, 103), panel);
            Text(w - 267, 35, 230, (network.Running ? "●  PEER MESH" : "○  SOLO MODE"), heading);
            Text(w - 267, 69, 230, network.Peers.Count + " PEERS   /   " + beacons.Count + " BEACONS", small);
            Text(w - 267, 91, 230, world.Loaded + " TILES   ·   " + (network.RetainsBackups ? "BACKUPS ON" : "PRIMARY COPIES ONLY"), small);
            int cx = WorldMath.RegionX(player.transform.position.x), cz = WorldMath.RegionZ(player.transform.position.z);
            Box(new Rect(26, h - 207, 236, 141), panel);
            Text(42, h - 197, 210, "REGION  " + cx.ToString("+0;-0;0") + " / " + cz.ToString("+0;-0;0"), small);
            for (int z = -2; z <= 2; z++) for (int x = -2; x <= 2; x++)
            {
                if (!WorldMath.ValidRegion(cx+x,cz+z)) continue;
                string owner = network.Catalog.Primary(cx+x,cz+z);
                Color c = owner == peerId ? new Color(.25f, .49f, .42f) : new Color(.24f, .52f, .64f);
                if (!network.Catalog.Available(cx+x,cz+z)) c = new Color(.7f,.24f,.2f);
                if (x == 0 && z == 0) c = accent;
                Box(new Rect(43 + (x + 2) * 19, h - 164 + (z + 2) * 17, 16, 14), c);
            }
            Text(151, h - 161, 106, "LOCAL", small); Text(151, h - 139, 106, "GREEN", small); Text(151, h - 112, 106, "PEER / BLUE", small);
            Box(new Rect(26, h - 52, w - 52, 30), panel);
            Text(40, h - 49, 650, "WASD  Move     SHIFT  Sprint     SPACE  Jump     E  Place beacon     TAB / ESC  Panel", small);
            Text(w - 420, h - 49, 380, "X " + player.transform.position.x.ToString("F0") + "   Z " + player.transform.position.z.ToString("F0") + "   /   " + (network.Running ? "UDP " + network.Port : "OFFLINE"), small);
            if (!network.Catalog.Available(cx, cz))
            {
                Box(new Rect(w/2-260, h/2-70, 520, 90), panel);
                Text(w/2-240,h/2-55,480,"REGION UNAVAILABLE · OWNER DISCONNECTED",label);
                Text(w/2-240,h/2-25,480,"No active seed copy. Wait for its owner or a backup holder.",small);
            }
            if (player.Captured)
            {
                Box(new Rect(w / 2 - 5, h / 2, 10, 1), new Color(1, 1, 1, .7f)); Box(new Rect(w / 2, h / 2 - 5, 1, 10), new Color(1, 1, 1, .7f));
                return;
            }
            float px = w - 461, py = 150;
            Box(new Rect(px, py, 435, 498), panel); Box(new Rect(px, py, 435, 3), accent);
            Text(px + 25, py + 20, 390, "The battlefield awaits.", heading);
            Text(px + 25, py + 57, 390, "42 regions · 64 independent tiles per region", label);
            Text(px + 25, py + 82, 390, "River valley, woodland and dirt tracks.", small);
            GUI.backgroundColor = accent;
            if (GUI.Button(new Rect(px + 25, py + 122, 385, 45), "ENTER THE FRONTIER  →", button)) player.Capture(true);
            GUI.backgroundColor = Color.white;
            Text(px + 25, py + 186, 385, "CONNECT A PEER", small);
            Text(px + 25, py + 211, 140, "Your UDP port", small);
            localPort = GUI.TextField(new Rect(px + 25, py + 235, 120, 32), localPort, 5, field);
            if (GUI.Button(new Rect(px + 156, py + 235, 254, 32), network.Running ? "Stop peer networking" : network.Paused ? "Resume networking" : "Start LAN discovery", button))
            { if (network.Running) network.Dispose(); else if (network.Paused) network.Resume(); else StartNetwork(); }
            Text(px + 25, py + 281, 250, "Peer IPv4 address", small); Text(px + 290, py + 281, 120, "Peer port", small);
            address = GUI.TextField(new Rect(px + 25, py + 305, 254, 32), address, 45, field);
            remotePort = GUI.TextField(new Rect(px + 290, py + 305, 120, 32), remotePort, 5, field);
            GUI.enabled = network.Running;
            if (GUI.Button(new Rect(px + 25, py + 348, 385, 32), "Connect directly", button)) Connect();
            GUI.enabled = true;
            Text(px + 25, py + 393, 385, network.Status, small);
            Text(px + 25, py + 419, 385, "Same seed required · trusted LAN prototype", small);
            Text(px + 25, py + 445, 385, "Admin backups: " + (network.RetainsBackups ? "ON · replicas retained" : network.BackupsEnabled ? "OFF for this peer" : "OFF for world"), small);
            Text(28, 148, 650, "01 / EXPLORE THE UNBUILT", outdoorSmall);
            Text(27, 176, 700, "Hold your ground.", outdoorTitle);
            Text(28, 229, 650, "100 km². Generated region by region.", outdoorLabel);
            Text(28, 269, 650, notice, outdoorSmall);
        }
        IEnumerator Smoke(string report, string capture, bool place)
        {
            yield return new WaitForSecondsRealtime(2);
            if (place) PlaceBeacon();
            yield return new WaitForSecondsRealtime(8);
            if (!string.IsNullOrEmpty(capture))
            {
                yield return new WaitForEndOfFrame();
                var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); tex.Apply(); File.WriteAllBytes(capture, tex.EncodeToPNG()); Destroy(tex);
            }
            bool expectedPeer = Environment.GetCommandLineArgs().Contains("--expect-peer");
            bool aboveGround=Physics.Raycast(player.transform.position+Vector3.up*500,Vector3.down,out var ground,1000,1<<ChunkWorld.TerrainLayer)&&player.transform.position.y>=ground.point.y-.25f;
            bool ok = world.Loaded >= 81 && aboveGround && WorldMath.Inside(player.transform.position.x, player.transform.position.z) && (!expectedPeer || (network.Peers.Count > 0 && beacons.Count > 0));
            File.WriteAllText(report, JsonUtility.ToJson(new SmokeReport { passed=ok,aboveGround=aboveGround,chunks=world.Loaded,peers=network.Peers.Count,beacons=beacons.Count,packets=network.Received,playerY=player.transform.position.y,groundY=ground.point.y }));
            Debug.Log("LOOKUPON_SMOKE " + File.ReadAllText(report)); Application.Quit(ok ? 0 : 1);
        }
    }
}
