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
 [InitializeOnLoad] public static class Task051SupplyDiagnostic
 {
  const string Key="Task051.Run";
  static readonly List<string> checks=new List<string>();
  static Task051SupplyDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
  public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");UnityEngine.Object.FindFirstObjectByType<HotpotSort.Bootstrap.Bootstrap>().enabled=false;EditorApplication.EnterPlaymode();}
  static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
  static void Check(bool b,string s){if(!b)throw new Exception(s);checks.Add(s);Debug.Log("TASK051_PASS "+s);}
  static async Task Wait(Func<bool> f,string label,int ms=20000){double end=EditorApplication.timeSinceStartup+ms/1000d;while(!f()){if(EditorApplication.timeSinceStartup>end)throw new TimeoutException(label);await Task.Delay(50);}}
  static bool NormalEntryBlocked(DailyProductionComposition c,GameplayView v)
  {
   if(!v.World.ObserveHeightGate(v.LastSnapshot.revision).canSupply)return true;
   int next=DailyViewMapper.PendingHead(c.ActiveCore.Snapshot);if(next==0)return true;
   float r=DailyViewMapper.PlateRadius(c.ActiveCore.PlateSize(next));float y=PlateSupplyGeometry.SpawnY(r,v.PlateCrop.yMin);
   var spawn=(Vector2)c.GetType().GetField("normalSupplySpawn",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(c);
   return !v.World.HasSpawnSpace(spawn,r);
  }
  static void Dump(DailyProductionComposition c,GameplayView v,string root,string label)
  {
   int next=DailyViewMapper.PendingHead(c.ActiveCore.Snapshot);float r=next==0?0:DailyViewMapper.PlateRadius(c.ActiveCore.PlateSize(next));float y=PlateSupplyGeometry.SpawnY(r,v.PlateCrop.yMin);
   var lines=new List<string>{"top="+v.PlateCrop.yMin+" gate="+v.World.SupplyGateY+" count="+v.World.OccupiedPlateCount+" next="+next+" radius="+r+" spawnY="+y+" gateClear="+v.World.ObserveHeightGate(v.LastSnapshot.revision).canSupply+" settled="+v.World.HasStableSupplyBoard(v.LastSnapshot.plates.Length)};
   for(float x=r+2;x<=418-r;x+=4)if(v.World.HasSpawnSpace(new Vector2(x,y),r))lines.Add("safeX="+x);
   foreach(var b in v.World.Bodies)lines.Add(b.data.plateId+" pos="+v.World.Position(b)+" radius="+b.data.radius+" velocity="+b.rigidbody.linearVelocity+" angular="+b.rigidbody.angularVelocity);
   File.WriteAllLines(Path.Combine(root,label+".txt"),lines);Debug.Log("TASK051_SNAPSHOT "+label+" "+lines[0]);
  }
  static async void Execute()
  {
   int exit=0;string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../.harness/qa/TASK-051/visual"));Directory.CreateDirectory(root);
   var c=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();GameplayView v=null;
   try
   {
    var local=new LocalDevelopmentServices("Hotpot.Task051."+Guid.NewGuid().ToString("N"),"test");local.CompleteTutorial(false);local.CompleteTutorial(true);c.ConfigureServices(local,local,local,local);
    var boot=UnityEngine.Object.FindFirstObjectByType<HotpotSort.Bootstrap.Bootstrap>();boot.enabled=true;await Wait(()=>boot.IsConfigured,"boot");var controller=boot.Controller;v=c.PlayerView;
    await controller.StartTodayAsync();await Wait(()=>DailyViewMapper.PendingHead(c.ActiveCore.Snapshot)==0,"warmup exhausted");await Task.Delay(800);
    Check((bool)Call(c,"BeginRedeal"),"warmup shuffle accepted");await Wait(()=>!v.RedealActive,"warmup shuffle ends");
    Check(DailyViewMapper.PendingHead(c.ActiveCore.Snapshot)==0,"small queue exhausted without waiting for full board");
    var context=c.ActiveCore.Snapshot.Challenge;Call(controller,"Release");Call(controller,"Create",context,ChallengeStage.Formal,0);

    await Wait(()=>v.World.OccupiedPlateCount>5&&NormalEntryBlocked(c,v)&&v.World.HasStableSupplyBoard(v.LastSnapshot.plates.Length),"normal board fills",30000);
    Check(Math.Abs(v.World.SupplyGateY-(v.PlateCrop.yMin-24))<.01,"gate follows actual visible top");
    int count=v.World.OccupiedPlateCount;await Task.Delay(700);Check(v.World.OccupiedPlateCount==count,"full board blocks ordinary supply");
    Check(v.World.Bodies.All(b=>Math.Abs(b.data.motion.spawnY-PlateSupplyGeometry.SpawnY(b.data.radius,v.PlateCrop.yMin))<.01),"spawn artwork begins above visible top");
    var body=v.World.Bodies.First();v.World.SetSimulating(false);var positions=v.World.Bodies.ToDictionary(b=>b,b=>b.rigidbody.position);
    foreach(var b in v.World.Bodies)b.rigidbody.position+=new Vector2(0,(v.World.SupplyGateY+100-v.World.Position(b).y)*.01f);
    body.rigidbody.position+=new Vector2(0,(v.World.SupplyGateY-v.World.Position(body).y)*.01f);
    Check(v.World.ObserveHeightGate(1).canSupply,"center equal to gate allowed");
    body.rigidbody.position+=new Vector2(0,-.01f);Check(!v.World.ObserveHeightGate(1).canSupply,"one center above gate blocks");
    foreach(var p in positions)p.Key.rigidbody.position=p.Value;v.World.SetSimulating(true);
    controller.StartChallengeTimer();
    var born=new HashSet<string>();var births=new Dictionary<int,int>();float minGap=float.PositiveInfinity;int max=0;
    c.Updated+=u=>{if(!v.RedealActive)return;foreach(var b in v.World.Bodies){if(!born.Add(b.data.plateId))continue;int frame=Time.frameCount;births[frame]=births.ContainsKey(frame)?births[frame]+1:1;foreach(var other in v.World.Bodies)if(other!=b)minGap=Mathf.Min(minGap,Vector2.Distance(v.World.Position(b),v.World.Position(other))-b.data.radius-other.data.radius);}max=Math.Max(max,v.World.OccupiedPlateCount);};
    Check((bool)Call(c,"BeginRedeal"),"formal shuffle accepted");double frozen=controller.ChallengeSeconds;
    await Wait(()=>v.World.OccupiedPlateCount>=2,"parallel first batch");Check(v.RedealActive,"falling entry does not end refill");
    controller.SetRewardPaused(true);int held=v.World.OccupiedPlateCount;await Task.Delay(400);Check(v.RedealActive&&v.World.OccupiedPlateCount==held,"pause holds refill");controller.SetRewardPaused(false);
    await Wait(()=>!v.RedealActive,"formal refill finish",30000);
    Check(Math.Abs(controller.ChallengeSeconds-frozen)<.25,"refill freezes gameplay timer");
    Check(minGap>=2.8f,"every new spawn avoids existing and same-batch colliders, gap="+minGap);
    Check(births.Values.Any(n=>n>=2),"multiple plates supplied in same frame");
    Check(DailyViewMapper.PendingHead(c.ActiveCore.Snapshot)>0,"remaining plates kept in queue");
    Check(controller.CanAcceptInput,"input restored");
    var obs=v.CaptureClickability();var needs=v.LastSnapshot.orders.Where(o=>o.enabled&&o.count<3).Select(o=>o.foodId).ToArray();
    Check(v.LastSnapshot.plates.SelectMany(p=>p.items).Any(i=>needs.Contains(i.foodId)&&obs.clickable.Contains(i.itemId)),"clickable matching target after refill");
    Check(born.Count<50&&born.Count>3,"fill amount determined by geometry: born="+born.Count+" max="+max);
    VisualCapture.Capture(v,1080,1920,Path.Combine(root,"refill-1080x1920.png"));VisualCapture.Capture(v,1080,1440,Path.Combine(root,"refill-1080x1440.png"));
    // Simulate freed board space while preserving inventory, then verify the actual normal supply path resumes.
    foreach(var b in v.World.Bodies)b.rigidbody.position+=new Vector2(0,2f);
    int prior=v.World.OccupiedPlateCount;await Wait(()=>v.World.OccupiedPlateCount>prior,"normal supply resumes after space opens",5000);
    Check(true,"ordinary supply resumes after refill");
    Debug.Log("TASK051_ALL_PASS checks="+checks.Count);
   }
   catch(Exception e){if(v){Dump(c,v,root,"failure");VisualCapture.Capture(v,1080,1920,Path.Combine(root,"failure-1080x1920.png"));}exit=1;Debug.LogException(e);checks.Add("FAIL "+e);}
   finally{if(v)v.World.Clear();SessionState.SetBool(Key,false);File.WriteAllText(Path.Combine(root,"runtime-result.json"),CanonicalJson.Write(CanonicalJson.Object("exitCode",exit,"checks",checks)));EditorApplication.Exit(exit);}
  }
 }
}
#endif



