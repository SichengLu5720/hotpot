#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Capture=HotpotSort.Task001V7.VisualCapture;
namespace HotpotSort.Presentation
{
    [InitializeOnLoad] public static class Task013PerfDiagnostic
    {
        const string Key="Task013.Perf";
        static Task013PerfDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run(){SessionState.SetBool(Key,true);UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);EditorApplication.EnterPlaymode();}
        static void Execute()
        {
            var output=Environment.GetEnvironmentVariable("HOTPOT_TASK013_OUTPUT");Directory.CreateDirectory(output);GameplayView view=null;int exit=0;
            try
            {
                view=new GameObject("PerfDiagnostic").AddComponent<GameplayView>();view.ConfigureAssets(PresentationAssets.CandidateRoot);
                var s=Capture.Fixture();s.sessionId="perf";s.sessionGeneration=1;foreach(var o in s.orders)o.enabled=true;
                view.Bind(new Capture.FullPort(s,false));view.SetForeground(true);view.World.SetSimulating(false);var f=view.GetComponent<GameplayFeedback>();f.enabled=false;
                for(int i=0;i<600;i++)f.Tick(1f/60);
                long allocationProbe=GC.GetAllocatedBytesForCurrentThread();var sentinel=new byte[4096];bool allocationSupported=GC.GetAllocatedBytesForCurrentThread()>allocationProbe;GC.KeepAlive(sentinel);
                File.WriteAllText(output+"/allocation-metric.txt",allocationSupported?"Supported":"Unavailable on this Unity backend: zero byte counters must not be interpreted as allocation-free.");
                var timer=new Stopwatch();long before=GC.GetAllocatedBytesForCurrentThread();timer.Start();for(int i=0;i<10000;i++)f.IsServing(i%4);timer.Stop();long servingBytes=GC.GetAllocatedBytesForCurrentThread()-before;
                double servingMs=timer.Elapsed.TotalMilliseconds;timer.Reset();before=GC.GetAllocatedBytesForCurrentThread();timer.Start();for(int i=0;i<10000;i++)f.Tick(1f/60);timer.Stop();long tickBytes=GC.GetAllocatedBytesForCurrentThread()-before;
                var images=view.GetComponentsInChildren<RawImage>();
                File.WriteAllText(output+"/micro.json","{\"servingCalls\":10000,\"servingBytes\":"+servingBytes+",\"servingMs\":"+servingMs.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"tickCalls\":10000,\"tickBytes\":"+tickBytes+",\"tickMs\":"+timer.Elapsed.TotalMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"pool\":"+f.PoolSize+",\"active\":"+f.ActiveEffectCount+",\"rawImages\":"+images.Length+",\"activeEdgeImages\":"+images.Count(x=>x.enabled&&x.name.Contains("Edge"))+"}");
                File.WriteAllLines(output+"/textures.csv",new[]{"name,width,height,readable,format,runtimeBytes"}.Concat(Resources.FindObjectsOfTypeAll<Texture2D>().Where(t=>AssetDatabase.GetAssetPath(t).Contains("/v10/r001/")).Select(t=>t.name+","+t.width+","+t.height+","+t.isReadable+","+t.format+","+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t))));
                UnityEngine.Debug.Log("TASK013_MICRO_OK");
            }
            catch(Exception e){exit=1;UnityEngine.Debug.LogException(e);}
            finally{SessionState.SetBool(Key,false);if(view){view.Bind(null);UnityEngine.Object.DestroyImmediate(view.gameObject);}EditorApplication.Exit(exit);}
        }
    }
}
#endif
