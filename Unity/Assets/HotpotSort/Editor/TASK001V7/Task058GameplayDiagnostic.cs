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
 [InitializeOnLoad] public static class Task058GameplayDiagnostic
 {
  const string Key="Task058.Run";
  static readonly List<string> checks=new List<string>();
  static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  static Task058GameplayDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
  public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");UnityEngine.Object.FindFirstObjectByType<HotpotSort.Bootstrap.Bootstrap>().enabled=false;EditorApplication.EnterPlaymode();}
  public static void BuildPlayer()
  {
   string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../.harness/qa/TASK-058/integrated"));Directory.CreateDirectory(Path.Combine(root,"Player"));
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/HotpotSort/Scenes/Boot.unity"},locationPathName=Path.Combine(root,"Player","HotpotSortTask058.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
   File.WriteAllText(Path.Combine(root,"player-build-result.json"),CanonicalJson.Write(CanonicalJson.Object("result",report.summary.result.ToString(),"errors",report.summary.totalErrors,"warnings",report.summary.totalWarnings,"bytes",report.summary.totalSize.ToString())));
   EditorApplication.Exit(report.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded?0:1);
  }
  static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Private).Invoke(o,args);
  static object Field(object o,string n)=>o.GetType().GetField(n,Private).GetValue(o);
  static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks.Add(label);Debug.Log("TASK058_PASS "+label);}
  static async Task Wait(Func<bool> predicate,string label,int ms=30000){double end=EditorApplication.timeSinceStartup+ms/1000d;while(!predicate()){if(EditorApplication.timeSinceStartup>end)throw new TimeoutException(label);await Task.Delay(20);}}
  static void Place(GameplayView v,HotpotSort.UnityPhysics.PlatePresentationWorld.PlateBody b,float x,float y)
  {b.rigidbody.position+=(new Vector2(x,y)-v.World.Position(b))*.01f;}
  static void ClearEntry(GameplayView v){int i=0;foreach(var b in v.World.Bodies)Place(v,b,80+(i++%3)*130,700);}
  static async void Execute()
  {
   int exit=0;string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../.harness/qa/TASK-058/visual"));Directory.CreateDirectory(root);
   var c=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();GameplayView v=null;
   try
   {
    var local=new LocalDevelopmentServices("Hotpot.Task058."+Guid.NewGuid().ToString("N"),"test");local.CompleteTutorial(false);local.CompleteTutorial(true);c.ConfigureServices(local,local,local,local);
    var boot=UnityEngine.Object.FindFirstObjectByType<HotpotSort.Bootstrap.Bootstrap>();boot.enabled=true;await Wait(()=>boot.IsConfigured,"boot");var controller=boot.Controller;v=c.PlayerView;
    await controller.StartTodayAsync();await Wait(()=>DailyViewMapper.PendingHead(c.ActiveCore.Snapshot)==0,"warmup exhausts");
    var context=c.ActiveCore.Snapshot.Challenge;Call(controller,"Release");Call(controller,"Create",context,ChallengeStage.Formal,0);
    await Wait(()=>v.World.OccupiedPlateCount>5,"ordinary live supply");
    await Wait(()=>v.World.HasStableSupplyBoard(v.LastSnapshot.plates.Length),"ordinary board stabilizes");
    Check(v.World.OccupiedPlateCount>5,"real startup supplies formal board");
    Check(Mathf.Abs(v.World.SupplyGateY-(v.PlateCrop.yMin-72))<.01f,"stop line 72 above actual visible edge");
    Check(v.LastSnapshot.thirdPotThreshold==31&&v.LastSnapshot.fourthPotThreshold==0,"mapped third31 fourthrewardonly");
    int stableCount=v.World.OccupiedPlateCount;await Task.Delay(2000);Check(stableCount==v.World.OccupiedPlateCount,"full stable board stops supply for two seconds");
    var initial=v.CaptureClickability();
    Check(c.ActiveCore.OrderProtectionPercentAtStart==20,"new user starts P20");
    Check(initial.nativeAbove!=null,"native food-center observation exists");
    Check(initial.offscreen!=null&&initial.activePlateOrder.Length==v.LastSnapshot.plates.Length,"cost observation contains active order and offscreen certificate");
    Check(!initial.offscreen.Intersect(initial.clickable.Concat(initial.unknown).Concat(initial.nextLayer)).Any(),"offscreen disjoint from clickable and unknown");
    ulong seq=1000;
    for(int step=0;step<24&&!c.ActiveCore.CoreEventsJson.Contains("native_d1_1.5111_ordinary_v1");step++)
    {
     await Task.Delay(350);
     var observation=(ClickableObservation)Call(c,"CaptureOrderClickability");if(observation==null)continue;
     var state=CanonicalJson.Map(CanonicalJson.Parse(c.ActiveCore.Snapshot.CanonicalStateJson));
     var kinds=CanonicalJson.Array(state["orders"]).Select(CanonicalJson.Map).Where(o=>o["kind"]!=null).Select(o=>(string)o["kind"]).ToArray();
     var target=CanonicalJson.Array(state["items"]).Select(CanonicalJson.Map).FirstOrDefault(i=>(string)i["location"]=="ActiveAvailable"&&kinds.Contains((string)i["kind"])&&observation.ItemIds.Contains(CanonicalJson.Int(i["itemId"])));
     if(target==null)continue;
     Check(c.ActiveCore.Tap(new TapCommand(CanonicalJson.Int(target["itemId"]),seq++,(ulong)c.GetType().GetProperty("Boundary",Private).GetValue(c),true,observation)).Accepted,"observed target tap accepted");
    }
    Check(c.ActiveCore.CoreEventsJson.Contains("native_d1_1.5111_ordinary_v1"),"live composition observation selects new cost on refill");
    await Task.Delay(1200);
    VisualCapture.Capture(v,1080,1920,Path.Combine(root,"locked-1080x1920.png"));VisualCapture.Capture(v,1080,1440,Path.Combine(root,"locked-1080x1440.png"));
    // Fresh production callbacks, no fabricated reward completion in the core.
    foreach(var outcome in new[]{RewardOutcome.Cancelled,RewardOutcome.Failed})
    {
     local.RewardPrompt=r=>Task.FromResult(outcome);Call(c,"RequestReward",RewardKind.FourthPot,RewardRoute.SimulatedShare);
     await Task.Delay(100);Check((c.ActiveCore.UnlockedExtraPotMask&2)==0,"fourth remains locked after "+outcome);Call(v,"CloseModal");
    }
    local.RewardPrompt=r=>Task.FromResult(RewardOutcome.Success);Call(c,"RequestReward",RewardKind.FourthPot,RewardRoute.SimulatedShare);
    await Wait(()=>(c.ActiveCore.UnlockedExtraPotMask&2)!=0,"fourth share completion");Check(local.ReadSnapshot().rewards.Any(r=>r.rewardKind==(int)RewardKind.FourthPot&&r.route==(int)RewardRoute.SimulatedShare),"fourth share applied and persisted");
    Call(c,"RequestReward",RewardKind.ThirdPot,RewardRoute.SimulatedAd);await Wait(()=>c.ActiveCore.UnlockedExtraPotMask==3,"third ad completion");Check(local.ReadSnapshot().rewards.Any(r=>r.rewardKind==(int)RewardKind.ThirdPot&&r.route==(int)RewardRoute.SimulatedAd),"third advertisement unlock retained");
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