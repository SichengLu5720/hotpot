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
 [InitializeOnLoad] public static class Task052SupplyDiagnostic
 {
  const string Key="Task052.Run";
  static readonly List<string> checks=new List<string>();
  static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  static Task052SupplyDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
  public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");UnityEngine.Object.FindFirstObjectByType<HotpotSort.Bootstrap.Bootstrap>().enabled=false;EditorApplication.EnterPlaymode();}
  static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Private).Invoke(o,args);
  static object Field(object o,string n)=>o.GetType().GetField(n,Private).GetValue(o);
  static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks.Add(label);Debug.Log("TASK052_PASS "+label);}
  static async Task Wait(Func<bool> predicate,string label,int ms=30000){double end=EditorApplication.timeSinceStartup+ms/1000d;while(!predicate()){if(EditorApplication.timeSinceStartup>end)throw new TimeoutException(label);await Task.Delay(20);}}
  static void Place(GameplayView v,HotpotSort.UnityPhysics.PlatePresentationWorld.PlateBody b,float x,float y)
  {b.rigidbody.position+=(new Vector2(x,y)-v.World.Position(b))*.01f;}
  static void ClearEntry(GameplayView v){int i=0;foreach(var b in v.World.Bodies)Place(v,b,80+(i++%3)*130,700);}
  static async void Execute()
  {
   int exit=0;long blockedBytes=-1,spaceBytes=-1;double blockedMs=-1,spaceMs=-1;string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../.harness/qa/TASK-052/visual"));Directory.CreateDirectory(root);
   var c=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();GameplayView v=null;
   try
   {
    var local=new LocalDevelopmentServices("Hotpot.Task052."+Guid.NewGuid().ToString("N"),"test");local.CompleteTutorial(false);local.CompleteTutorial(true);c.ConfigureServices(local,local,local,local);
    var boot=UnityEngine.Object.FindFirstObjectByType<HotpotSort.Bootstrap.Bootstrap>();boot.enabled=true;await Wait(()=>boot.IsConfigured,"boot");var controller=boot.Controller;v=c.PlayerView;
    await controller.StartTodayAsync();await Wait(()=>DailyViewMapper.PendingHead(c.ActiveCore.Snapshot)==0,"warmup exhausts");
    var context=c.ActiveCore.Snapshot.Challenge;Call(controller,"Release");Call(controller,"Create",context,ChallengeStage.Formal,0);
    await Wait(()=>v.World.OccupiedPlateCount>5,"ordinary live supply");
    await Wait(()=>v.World.HasStableSupplyBoard(v.LastSnapshot.plates.Length),"ordinary board stabilizes");
    Check(v.World.OccupiedPlateCount>5,"real startup supplies formal board");
    VisualCapture.Capture(v,1080,1920,Path.Combine(root,"normal-1080x1920.png"));VisualCapture.Capture(v,1080,1440,Path.Combine(root,"normal-1080x1440.png"));
    // Controlled geometry from this point: proves supply reactions, not a player removal path.
    c.enabled=false;v.World.SetSimulating(false);
    var tick=(Action)Delegate.CreateDelegate(typeof(Action),c,c.GetType().GetMethod("TickNormalSupply",Private));
    int frame=Time.frameCount;await Wait(()=>Time.frameCount!=frame,"next test frame");
    ClearEntry(v);var blocker=v.World.Bodies.First();Place(v,blocker,210,v.World.SupplyGateY-1);
    int before=v.World.OccupiedPlateCount;
    for(int i=0;i<256;i++)tick();
    long start=GC.GetAllocatedBytesForCurrentThread();long ticks=System.Diagnostics.Stopwatch.GetTimestamp();for(int i=0;i<10000;i++)tick();long stop=System.Diagnostics.Stopwatch.GetTimestamp();blockedBytes=GC.GetAllocatedBytesForCurrentThread()-start;blockedMs=(stop-ticks)*1000d/System.Diagnostics.Stopwatch.Frequency;
    Check(v.World.OccupiedPlateCount==before,"height-blocked calls never supply");
    Check(blockedBytes==0,"10000 warmed height-blocked direct calls allocate zero bytes: "+blockedBytes);
    controller.SetRewardPaused(true);ClearEntry(v);tick();Check(v.World.OccupiedPlateCount==before,"paused supply stays blocked despite free entrance");controller.SetRewardPaused(false);
    ClearEntry(v);int callFrame=Time.frameCount;tick();
    Check(v.World.OccupiedPlateCount==before+1&&Time.frameCount==callFrame,"first unblocked call supplies immediately without 300ms delay");
    ClearEntry(v);for(int i=0;i<100;i++)tick();
    Check(v.World.OccupiedPlateCount==before+1,"repeated calls in same frame supply at most one plate even with free entrance");
    frame=Time.frameCount;await Wait(()=>Time.frameCount!=frame,"next safety test frame");
    int index=0;foreach(var b in v.World.Bodies)Place(v,b,55+(index++%5)*77,v.World.SupplyGateY+1);
    before=v.World.OccupiedPlateCount;tick();var spawn=(Vector2)Field(c,"normalSupplySpawn");int head=(int)Field(c,"normalSpawnHead");
    Check(v.World.HasSupplyCapacity&&v.World.OccupiedPlateCount==before&&head!=0,"clear height gate still waits when safe spawn space is occupied");
    for(int i=0;i<256;i++)tick();start=GC.GetAllocatedBytesForCurrentThread();ticks=System.Diagnostics.Stopwatch.GetTimestamp();for(int i=0;i<10000;i++)tick();stop=System.Diagnostics.Stopwatch.GetTimestamp();spaceBytes=GC.GetAllocatedBytesForCurrentThread()-start;spaceMs=(stop-ticks)*1000d/System.Diagnostics.Stopwatch.Frequency;
    Check(spaceBytes==0,"10000 warmed space-blocked direct calls allocate zero bytes: "+spaceBytes);
    Check((Vector2)Field(c,"normalSupplySpawn")==spawn&&(int)Field(c,"normalSpawnHead")==head&&v.World.OccupiedPlateCount==before,"safe-space retries preserve cached spawn and queue head");
    ClearEntry(v);tick();Check(v.World.OccupiedPlateCount==before+1,"safe-space release supplies on the next call");
    Check(v.World.Bodies.Last().data.motion.spawnX==spawn.x,"accepted spawn uses the cached horizontal position");
    Debug.Log("TASK052_TIMING heightMs="+blockedMs+" spaceMs="+spaceMs+" calls=10000");
    Debug.Log("TASK052_ALL_PASS checks="+checks.Count);
   }
   catch(Exception e){exit=1;Debug.LogException(e);checks.Add("FAIL "+e);}
   finally{if(v)v.World.Clear();SessionState.SetBool(Key,false);File.WriteAllText(Path.Combine(root,"runtime-result.json"),CanonicalJson.Write(CanonicalJson.Object("exitCode",exit,"blockedCalls",10000,"blockedAllocatedBytes",blockedBytes,"heightBlockedTotalMs",blockedMs.ToString("F6",System.Globalization.CultureInfo.InvariantCulture),"spaceBlockedAllocatedBytes",spaceBytes,"spaceBlockedTotalMs",spaceMs.ToString("F6",System.Globalization.CultureInfo.InvariantCulture),"controlledGeometry",true,"checks",checks)));EditorApplication.Exit(exit);}
  }
 }
}
#endif


