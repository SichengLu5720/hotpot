#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotpotSort.Bootstrap;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Determinism;
using HotpotSort.Presentation;
using HotpotSort.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace HotpotSort.Task001V7
{
 [InitializeOnLoad] public static class Task054OrderDiagnostic
 {
  const string Key="Task054.Run";
  static readonly List<string> checks=new List<string>();
  static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  static Task054OrderDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
  public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");UnityEngine.Object.FindFirstObjectByType<HotpotSort.Bootstrap.Bootstrap>().enabled=false;EditorApplication.EnterPlaymode();}
  static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Private).Invoke(o,args);
  static object Field(object o,string n)=>o.GetType().GetField(n,Private).GetValue(o);
  static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks.Add(label);Debug.Log("TASK054_PASS "+label);}
  static async Task Wait(Func<bool> predicate,string label,int ms=30000){double end=EditorApplication.timeSinceStartup+ms/1000d;while(!predicate()){if(EditorApplication.timeSinceStartup>end)throw new TimeoutException(label);await Task.Delay(20);}}
  static void Place(GameplayView v,HotpotSort.UnityPhysics.PlatePresentationWorld.PlateBody b,float x,float y)
  {b.rigidbody.position+=(new Vector2(x,y)-v.World.Position(b))*.01f;}
  static void ClearEntry(GameplayView v){int i=0;foreach(var b in v.World.Bodies)Place(v,b,80+(i++%3)*130,700);}
  static async void Execute()
  {
   int exit=0;string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../.harness/qa/TASK-054/visual"));Directory.CreateDirectory(root);
   var c=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();GameplayView v=null;
   try
   {
    var local=new LocalDevelopmentServices("Hotpot.Task054."+Guid.NewGuid().ToString("N"),"test");local.CompleteTutorial(false);local.CompleteTutorial(true);c.ConfigureServices(local,local,local,local);
    var boot=UnityEngine.Object.FindFirstObjectByType<HotpotSort.Bootstrap.Bootstrap>();boot.enabled=true;await Wait(()=>boot.IsConfigured,"boot");var controller=boot.Controller;v=c.PlayerView;
    await controller.StartTodayAsync();await Wait(()=>DailyViewMapper.PendingHead(c.ActiveCore.Snapshot)==0,"warmup exhausts");
    var context=c.ActiveCore.Snapshot.Challenge;Call(controller,"Release");Call(controller,"Create",context,ChallengeStage.Formal,0);
    await Wait(()=>v.World.OccupiedPlateCount>5,"ordinary live supply");
    await Wait(()=>v.World.HasStableSupplyBoard(v.LastSnapshot.plates.Length),"ordinary board stabilizes");
    Check(v.World.OccupiedPlateCount>5,"real startup supplies formal board");
    var initial=v.CaptureClickability();
    Check(initial.offscreen!=null&&initial.activePlateOrder.Length==v.LastSnapshot.plates.Length,"cost observation contains active order and offscreen certificate");
    Check(!initial.offscreen.Intersect(initial.clickable.Concat(initial.unknown).Concat(initial.nextLayer)).Any(),"offscreen disjoint from clickable and unknown");
    ulong seq=1000;
    for(int step=0;step<24&&!c.ActiveCore.CoreEventsJson.Contains("director_supply_cost_v1");step++)
    {
     await Task.Delay(350);
     var observation=(ClickableObservation)Call(c,"CaptureOrderClickability");if(observation==null)continue;
     var state=CanonicalJson.Map(CanonicalJson.Parse(c.ActiveCore.Snapshot.CanonicalStateJson));
     var kinds=CanonicalJson.Array(state["orders"]).Select(CanonicalJson.Map).Where(o=>o["kind"]!=null).Select(o=>(string)o["kind"]).ToArray();
     var target=CanonicalJson.Array(state["items"]).Select(CanonicalJson.Map).FirstOrDefault(i=>(string)i["location"]=="ActiveAvailable"&&kinds.Contains((string)i["kind"])&&observation.ItemIds.Contains(CanonicalJson.Int(i["itemId"])));
     if(target==null)continue;
     Check(c.ActiveCore.Tap(new TapCommand(CanonicalJson.Int(target["itemId"]),seq++,100000+(ulong)step,true,observation)).Accepted,"observed target tap accepted");
    }
    Check(c.ActiveCore.CoreEventsJson.Contains("director_supply_cost_v1"),"live composition observation selects new cost on refill");
    await Task.Delay(1200);
    File.WriteAllText(Path.Combine(root,"events.json"),c.ActiveCore.CoreEventsJson);
    File.WriteAllText(Path.Combine(root,"replay.json"),c.ActiveCore.ExportReplay().CanonicalJson);
    VisualCapture.Capture(v,1080,1920,Path.Combine(root,"normal-1080x1920.png"));VisualCapture.Capture(v,1080,1440,Path.Combine(root,"normal-1080x1440.png"));
   }
   catch(Exception e){exit=1;Debug.LogException(e);checks.Add("FAIL "+e);}
   finally{if(v)v.World.Clear();SessionState.SetBool(Key,false);File.WriteAllText(Path.Combine(root,"runtime-result.json"),CanonicalJson.Write(CanonicalJson.Object("exitCode",exit,"checks",checks)));EditorApplication.Exit(exit);}
  }
 }
}
#endif