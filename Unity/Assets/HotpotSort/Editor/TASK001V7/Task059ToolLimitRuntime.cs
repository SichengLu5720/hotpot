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
using UnityEngine.UI;
namespace HotpotSort.Task001V7
{
 [InitializeOnLoad] public static class Task059ToolLimitRuntime
 {
  const string Key="Task059.Run";
  static readonly List<string> checks=new List<string>();
  static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  static Task059ToolLimitRuntime(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
  public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");UnityEngine.Object.FindFirstObjectByType<HotpotSort.Bootstrap.Bootstrap>().enabled=false;EditorApplication.EnterPlaymode();}
  public static void BuildPlayer()
  {
   string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../.harness/qa/TASK-059/integrated"));Directory.CreateDirectory(Path.Combine(root,"Player"));
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/HotpotSort/Scenes/Boot.unity"},locationPathName=Path.Combine(root,"Player","HotpotSortTask059.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
   File.WriteAllText(Path.Combine(root,"player-build-result.json"),CanonicalJson.Write(CanonicalJson.Object("result",report.summary.result.ToString(),"errors",report.summary.totalErrors,"warnings",report.summary.totalWarnings,"bytes",report.summary.totalSize.ToString())));
   EditorApplication.Exit(report.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded?0:1);
  }
  static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Private).Invoke(o,args);
  static object Field(object o,string n)=>o.GetType().GetField(n,Private).GetValue(o);
  static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks.Add(label);Debug.Log("TASK059_PASS "+label);}
  static async Task Wait(Func<bool> predicate,string label,int ms=30000){double end=EditorApplication.timeSinceStartup+ms/1000d;while(!predicate()){if(EditorApplication.timeSinceStartup>end)throw new TimeoutException(label);await Task.Delay(20);}}
  static void Place(GameplayView v,HotpotSort.UnityPhysics.PlatePresentationWorld.PlateBody b,float x,float y)
  {b.rigidbody.position+=(new Vector2(x,y)-v.World.Position(b))*.01f;}
  static void ClearEntry(GameplayView v){int i=0;foreach(var b in v.World.Bodies)Place(v,b,80+(i++%3)*130,700);}
  static async void Execute()
  {
   int exit=0;string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../.harness/qa/TASK-059/visual"));Directory.CreateDirectory(root);
   var c=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();GameplayView v=null;
   try
   {
    var local=new LocalDevelopmentServices("Hotpot.Task059."+Guid.NewGuid().ToString("N"),"test");local.CompleteTutorial(false);local.CompleteTutorial(true);c.ConfigureServices(local,local,local,local);
    var boot=UnityEngine.Object.FindFirstObjectByType<HotpotSort.Bootstrap.Bootstrap>();boot.enabled=true;await Wait(()=>boot.IsConfigured,"boot");var controller=boot.Controller;v=c.PlayerView;
    int rewardRequests=0;RewardOutcome nextReward=RewardOutcome.Success;
    local.RewardPrompt=r=>{rewardRequests++;return Task.FromResult(nextReward);};
    await controller.StartTodayAsync();await Wait(()=>DailyViewMapper.PendingHead(c.ActiveCore.Snapshot)==0,"warmup exhausts");await Task.Delay(1200);
    var warmup=c.ActiveCore;ulong seq=1000;
    Func<bool,int?> pick=matching=>
    {
     var obs=(ClickableObservation)Call(c,"CaptureOrderClickability");if(obs==null)return null;
     var state=CanonicalJson.Map(CanonicalJson.Parse(c.ActiveCore.Snapshot.CanonicalStateJson));
     var kinds=CanonicalJson.Array(state["orders"]).Select(CanonicalJson.Map).Where(o=>o["kind"]!=null).Select(o=>(string)o["kind"]).ToArray();
     var item=CanonicalJson.Array(state["items"]).Select(CanonicalJson.Map).FirstOrDefault(i=>(string)i["location"]=="ActiveAvailable"&&kinds.Contains((string)i["kind"])==matching&&obs.ItemIds.Contains(CanonicalJson.Int(i["itemId"])));
     return item==null?(int?)null:CanonicalJson.Int(item["itemId"]);
    };
    Action<int> tap=id=>Check(c.ActiveCore.Tap(new TapCommand(id,seq++,(ulong)c.GetType().GetProperty("Boundary",Private).GetValue(c),true,(ClickableObservation)Call(c,"CaptureOrderClickability"))).Accepted,"observed tap accepted");
    await Wait(()=>pick(false).HasValue,"warmup non-order food");tap(pick(false).Value);await Task.Delay(500);
    Check(c.ActiveCore.CanClearBuffer,"warmup clear has target");
    nextReward=RewardOutcome.Cancelled;Call(c,"RequestReward",RewardKind.ClearBuffer,RewardRoute.SimulatedShare);await Task.Delay(100);Check(c.ActiveCore.UsedToolMask==0&&c.ActiveCore.CanClearBuffer,"cancel does not spend tool use");Call(v,"CloseModal");
    nextReward=RewardOutcome.Failed;Call(c,"RequestReward",RewardKind.ClearBuffer,RewardRoute.SimulatedShare);await Task.Delay(100);Check(c.ActiveCore.UsedToolMask==0,"failed reward does not spend tool use");Call(v,"CloseModal");
    nextReward=RewardOutcome.Success;Call(c,"RequestReward",RewardKind.ClearBuffer,RewardRoute.SimulatedAd);await Wait(()=>c.ActiveCore.UsedToolMask==2,"warmup clear commits one use");
    Check(!v.GetComponentsInChildren<Button>().Single(x=>x.name=="Tool_ClearBuffer").interactable,"used warmup clear button disabled");
    for(int n=0;n<180&&c.ActiveCore.Stage==ChallengeStage.Warmup&&c.ActiveCore.Snapshot.Status==GameStatus.Running;n++)
    {await Task.Delay(130);int? id=pick(true);if(id.HasValue)tap(id.Value);}
    await Wait(()=>c.ActiveCore.Stage==ChallengeStage.Formal,"natural warmup-to-formal transition");
    Check(c.ActiveCore.UsedToolMask==2&&v.LastSnapshot.usedToolMask==2,"warmup usage inherited through actual controller transition");
    File.WriteAllText(Path.Combine(root,"warmup-replay.json"),warmup.ExportReplay().CanonicalJson);
    await Wait(()=>v.World.OccupiedPlateCount>5&&v.World.HasStableSupplyBoard(v.LastSnapshot.plates.Length),"formal board stable");
    await Wait(()=>pick(false).HasValue,"formal non-order food");tap(pick(false).Value);await Task.Delay(500);
    int before=rewardRequests;Call(c,"RequestReward",RewardKind.ClearBuffer,RewardRoute.SimulatedShare);await Task.Delay(100);Check(rewardRequests==before&&!c.ActiveCore.CanClearBuffer,"used clear rejected before another reward despite new buffer target");Call(v,"CloseModal");
    Call(c,"RequestReward",RewardKind.Shuffle,RewardRoute.SimulatedShare);await Wait(()=>(c.ActiveCore.UsedToolMask&4)!=0,"shuffle commits one use");await Wait(()=>!(bool)Field(c,"redealing"),"shuffle presentation settles");
    before=rewardRequests;Call(c,"RequestReward",RewardKind.Shuffle,RewardRoute.SimulatedAd);await Task.Delay(100);Check(rewardRequests==before,"used shuffle cannot switch channel for second use");Call(v,"CloseModal");
    await Wait(()=>c.ReadSwapOrderOffer().legalSlots.Length>0,"legal swap slot");Call(v,"BeginSwapPresentation");Check((bool)Call(v,"EnterSwapSelection"),"swap selection begins through view");
    var slot=c.ReadSwapOrderOffer().legalSlots[0];Call(v,"ChooseSwapTarget",slot);
    await Wait(()=>c.ActiveCore.UsedToolMask==7,"swap completion records final independent use");
    Check(!c.BeginSwapOrderSelection(),"used swap selection blocked");
    foreach(var name in new[]{"Tool_SwapOrder","Tool_ClearBuffer","Tool_Shuffle"})Check(!v.GetComponentsInChildren<Button>().Single(x=>x.name==name).interactable,"used button disabled "+name);
    VisualCapture.Capture(v,1080,1920,Path.Combine(root,"all-used.png"));
    File.WriteAllText(Path.Combine(root,"formal-replay.json"),c.ActiveCore.ExportReplay().CanonicalJson);
    string old=c.ActiveCore.Snapshot.SessionId;controller.Request(SessionAction.Retry);await Wait(()=>c.ActiveCore!=null&&c.ActiveCore.Snapshot.SessionId!=old,"retry creates new run");
    Check(c.ActiveCore.UsedToolMask==0&&v.LastSnapshot.usedToolMask==0,"retry resets all uses");
    foreach(var name in new[]{"Tool_SwapOrder","Tool_ClearBuffer","Tool_Shuffle"})Check(v.GetComponentsInChildren<Button>().Single(x=>x.name==name).interactable,"fresh run button restored "+name);
    VisualCapture.Capture(v,1080,1920,Path.Combine(root,"new-run.png"));

   }
   catch(Exception e){exit=1;Debug.LogException(e);checks.Add("FAIL "+e);}
   finally{if(v)v.World.Clear();SessionState.SetBool(Key,false);File.WriteAllText(Path.Combine(root,"runtime-result.json"),CanonicalJson.Write(CanonicalJson.Object("exitCode",exit,"checks",checks)));EditorApplication.Exit(exit);}
  }
 }
}
#endif
