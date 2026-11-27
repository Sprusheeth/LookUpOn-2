using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using LookUpOn;
using LookUpOn.Core;
using LookUpOn.Networking;
using LookUpOn.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ReportEvidence
{
    static List<double> frames = new List<double>();
    static double start, prev;
    static long rx, tx;
    static Transform moving;
    static Vector3 origin;
    static Texture2D readback;
    static PeerNetwork remote;
    static long remoteRx;
    const string A = "2d2970167ea54c0da74e4c89a6c0cdca", B = "c88e2b8aa52b47e8bf32f5dbe2e5bc9c";
    static ReportEvidence() { EditorApplication.update += RunFrame; }
    static string Dir => Directory.GetCurrentDirectory();
    static string Arg(string key) { var a=Environment.GetCommandLineArgs(); return a[Array.IndexOf(a,key)+1]; }
    public static void Start()
    {
        string phase = Arg("-reportPhase");
        if (phase == "write")
        {
            var rows = new List<string>();
            for (int i=1; i<=5; i++)
                foreach (bool on in new[]{i%2==1,i%2!=1})
                {
                    var sa=new BeaconStore(); var sb=new BeaconStore();
                    sb.Merge(new Beacon{id="e0000000000000000000000000000000",author=B,x=50,z=50});
                    using var a=new PeerNetwork(A,2026,sa,false);
                    using var b=new PeerNetwork(B,2026,sb,false);
                    if (!on) a.ApplyBackupPolicy(false,1,A);
                    if (!a.Start(0)||!b.Start(0)||!b.Connect("127.0.0.1",a.Port)) throw new Exception("Bind");
                    for(int t=0;t<35;t++){a.Tick(t*.17, Vector3.zero,0);b.Tick(t*.17,Vector3.one,0);Thread.Sleep(6);}
                    var owned=Enumerable.Range(0,42).Where(r=>a.Catalog.Primary(r%7,r/7)==B).ToArray();
                    if(a.Peers.Count!=1 || b.BackupsEnabled!=on || sa.Count!=1 || owned.Length==0) throw new Exception("Sync");
                    b.Dispose(); a.Tick(6.2, Vector3.zero,0); a.Tick(14.3,Vector3.zero,0);
                    var missing=Enumerable.Range(0,42).Where(r=>!a.Catalog.Available(r%7,r/7)).ToArray();
                    if((on ? missing.Length!=0 : !missing.SequenceEqual(owned)) || sa.Count!=(on?1:0)) throw new Exception("Owner loss");
                    rows.Add(i+","+(on?"ON":"OFF")+","+owned.Length+","+missing.Length+","+sa.Count);
                }
            File.WriteAllLines(Path.Combine(Dir,"repetitions.csv"),new[]{"trial,policy,owned,missing,beacons"}.Concat(rows));
        }
        PlayerSettings.companyName="LookUpOnQA"; PlayerSettings.productName="ReportEvidence16c";
        AssetDatabase.SaveAssets();
        SessionState.SetString("ReportEvidence", phase);
        EditorSceneManager.OpenScene("Assets/Scenes/Frontier.unity");
        EditorApplication.EnterPlaymode();
    }
    static object Field(LookUpOnGame game, string name) => game.GetType().GetField(name, BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game);
    static void RunFrame()
    {
        string phase = SessionState.GetString("ReportEvidence","");
        if (phase=="" || !EditorApplication.isPlaying) return;
        var game=UnityEngine.Object.FindFirstObjectByType<LookUpOnGame>();
        var world=UnityEngine.Object.FindFirstObjectByType<ChunkWorld>();
        if (!game || !world || world.Loaded<115) return;
        try
        {
            var state=(BeaconStore)Field(game,"beacons");
            string path=(string)Field(game,"savePath");
            string peer=(string)Field(game,"peerId");
            if (phase=="read")
            {
                var prior=File.ReadAllLines(Path.Combine(Dir,"save-write.txt"));
                if(state.Digest!=prior[0]||peer!=prior[1]||state.Count!=1) throw new Exception("Read-back differs");
                File.WriteAllText(Path.Combine(Dir,"save-read.txt"),state.Digest+"\n"+peer+"\n"+state.Count);
                Finish(0);return;
            }
            var network=(PeerNetwork)Field(game,"network");
            double now=EditorApplication.timeSinceStartup;
            if (remote==null)
            {
                remote=new PeerNetwork(B,game.Seed,new BeaconStore(),false);
                if (!remote.Start(0) || !remote.Connect("127.0.0.1",network.Port)) throw new Exception("Second socket");
            }
            remote.Tick(now, world.Observer.position+Vector3.forward*30,0);
            if (start==0)
            {
                if (!File.Exists(path) || ((string)Field(game,"saveStatus")).StartsWith("Save failed")
                    || state.Count != 1 || network.Peers.Count != 1) return;
                var saved=JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if(saved.beacons.Length!=1 || saved.regionSeeds.Length!=42 || saved.peerId!=peer) throw new Exception("Save content");
                File.WriteAllLines(Path.Combine(Dir,"save-write.txt"),new[]{state.Digest,peer, saved.regionSeeds.Length.ToString()});
                start=prev=now;moving=world.Observer;origin=moving.position;
                rx=network.BytesReceived;tx=network.BytesSent;remoteRx=remote.BytesReceived;
                moving.GetComponent<CharacterController>().enabled = false;
                moving.GetComponent<LookUpOn.Player.Explorer>().enabled = false;
                var cam=Camera.main;
                cam.targetTexture = new RenderTexture(1280,720,24); readback=new Texture2D(1,1,TextureFormat.RGBA32,false);
            }
            else
            {
                double elapsed=now-start;
                float z=origin.z + (float)Math.Min(24,elapsed)*7;
                float x=WorldMath.RoadX(z,WorldMath.ValleyPhase(game.Seed));
                moving.position=new Vector3(x,WorldMath.Height(x,z,game.Seed)+.2f,z);
                var camera=Camera.main; camera.Render(); var prior=RenderTexture.active; RenderTexture.active=camera.targetTexture; readback.ReadPixels(new Rect(640,360,1,1),0,0,false); RenderTexture.active=prior; now=EditorApplication.timeSinceStartup; elapsed=now-start; frames.Add(now-prev); prev=now; if (elapsed<24) return;
                var slow=frames.OrderByDescending(x=>x).Take((int)Math.Ceiling(frames.Count*.01));
                File.WriteAllLines(Path.Combine(Dir,"performance.txt"),new[]{elapsed.ToString("F3"),frames.Count.ToString(),
                    (frames.Count/elapsed).ToString("F2"),(1/slow.Average()).ToString("F2"),
                    ((network.BytesReceived-rx)/elapsed).ToString("F0"),((network.BytesSent-tx)/elapsed).ToString("F0"),
                    (remote.BytesReceived-remoteRx).ToString(),SystemInfo.processorType,SystemInfo.graphicsDeviceName,
                    SystemInfo.systemMemorySize.ToString()});
                Finish(0);
            }
        }
        catch(Exception e){ Debug.LogException(e); Finish(1); }
    }
    static void Finish(int code){remote?.Dispose(); SessionState.EraseString("ReportEvidence"); EditorApplication.Exit(code);}
}


