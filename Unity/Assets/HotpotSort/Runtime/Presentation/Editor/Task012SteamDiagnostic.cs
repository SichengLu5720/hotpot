#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Capture=HotpotSort.Task001V7.VisualCapture;
namespace HotpotSort.Presentation
{
    [InitializeOnLoad] public static class Task012SteamDiagnostic
    {
        const string Key="Task012.SteamDiagnostic";
        static Task012SteamDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run(){SessionState.SetBool(Key,true);UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);EditorApplication.EnterPlaymode();}
        static void Execute()
        {
            var output=Environment.GetEnvironmentVariable("HOTPOT_TASK012_OUTPUT");Directory.CreateDirectory(output);
            GameplayView view=null;int exit=0;
            var checks=new System.Collections.Generic.List<string>();
            Action<bool,string> check=(ok,label)=>{checks.Add((ok?"PASS ":"FAIL ")+label);if(!ok)throw new Exception(label);};
            try
            {
                view=new GameObject("SteamDiagnostic").AddComponent<GameplayView>();view.ConfigureAssets(PresentationAssets.CandidateRoot);
                var s=Capture.Fixture();s.sessionId="steam-test";s.sessionGeneration=1;foreach(var o in s.orders)o.enabled=true;
                view.Bind(new Capture.FullPort(s,false));view.SetForeground(true);view.World.SetSimulating(false);
                var f=view.GetComponent<GameplayFeedback>();f.enabled=false;
                float min=1,max=0,maxStep=0;var previous=new float[4];
                for(int frame=0;frame<3600;frame++)
                {
                    f.Tick(1f/60);
                    var images=view.GetComponentsInChildren<RawImage>().Where(x=>x.name.StartsWith("Steam_")||x.name=="bubble"||x.name=="oil_glint").ToArray();
                    if(images.Any(x=>x.color.a>.16001f))throw new Exception("opacity spike");
                    for(int slot=0;slot<4;slot++)
                    {
                        float sum=images.Where(x=>x.name=="Steam_"+slot).Sum(x=>x.color.a);
                        if(frame>240){min=Mathf.Min(min,sum);max=Mathf.Max(max,sum);maxStep=Mathf.Max(maxStep,Mathf.Abs(sum-previous[slot]));}
                        previous[slot]=sum;
                    }
                }
                check(min>.15f&&max<.17f&&maxStep<.006f,"60s four-pot continuity min="+min+" max="+max+" maxStep="+maxStep);
                var nodes=view.GetComponentsInChildren<RawImage>().Where(x=>x.name.StartsWith("Steam_")).ToArray();var positions=nodes.Select(x=>x.transform.localPosition).ToArray();var alphas=nodes.Select(x=>x.color.a).ToArray();
                s.pauseReasons=ViewPauseReasons.User;f.Apply(new ViewUpdate{snapshot=s},s,null);for(int i=0;i<600;i++)f.Tick(.1f);
                check(nodes.Select((x,i)=>x.transform.localPosition==positions[i]&&x.color.a==alphas[i]).All(x=>x),"pause freezes for 60s");
                s.pauseReasons=ViewPauseReasons.None;f.Apply(new ViewUpdate{snapshot=s},s,null);f.Tick(1f/60);check(f.ActiveEffectCount<20,"resume without catch-up burst");
                foreach(var phase in new[]{ViewPhase.Entry,ViewPhase.Aborted,ViewPhase.Won,ViewPhase.Overflow}){s.phase=phase;f.Apply(new ViewUpdate{snapshot=s},s,null);check(!view.GetComponentsInChildren<RawImage>().Any(x=>x.name.StartsWith("Steam_")),"clears "+phase);}
                s.phase=ViewPhase.Running;s.sessionGeneration++;foreach(var o in s.orders)o.enabled=false;f.Apply(new ViewUpdate{snapshot=s},s,null);for(int i=0;i<600;i++)f.Tick(1f/60);check(f.ActiveEffectCount==0,"new generation and locked pots clear");
                view.Bind(null);check(f.ActiveEffectCount==0,"unbind clears");
                Debug.Log("TASK012_STEAM_OK");
            }
            catch(Exception e){exit=1;Debug.LogException(e);checks.Add(e.ToString());}
            finally{SessionState.SetBool(Key,false);if(view){view.Bind(null);UnityEngine.Object.DestroyImmediate(view.gameObject);}File.WriteAllLines(output+"/steam-checks.txt",checks);EditorApplication.Exit(exit);}
        }
    }
}
#endif
