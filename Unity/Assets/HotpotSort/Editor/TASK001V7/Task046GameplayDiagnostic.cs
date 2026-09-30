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
    [InitializeOnLoad] public static class Task046GameplayDiagnostic
    {
        const string Key="Task046.Gameplay";
        static readonly List<string> checks=new List<string>();
        static Task046GameplayDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run()
        {
            SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");
            UnityEngine.Object.FindFirstObjectByType<HotpotSort.Bootstrap.Bootstrap>().enabled=false;EditorApplication.EnterPlaymode();
        }
        static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add(message);Debug.Log("TASK046 PASS "+message);}
        static async Task Wait(Func<bool> condition,string reason,int ms=15000)
        {double end=EditorApplication.timeSinceStartup+ms/1000d;while(!condition()){if(EditorApplication.timeSinceStartup>end)throw new TimeoutException(reason);await Task.Delay(50);}}
        static Button Button(GameplayView v,string prefix)=>v.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name.StartsWith("Action_"+prefix,StringComparison.Ordinal));
        static void Click(GameplayView v,string prefix){var b=Button(v,prefix);Check(b&&b.interactable,"button available: "+prefix);b.onClick.Invoke();}
        static async void Execute()
        {
            int exit=0;string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../.harness/qa/TASK-048"));Directory.CreateDirectory(root);
            var c=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();
            try
            {
                var local=new LocalDevelopmentServices("HotpotSort.Task046.QA."+Guid.NewGuid().ToString("N"),"test");local.CompleteTutorial(false);local.CompleteTutorial(true);
                c.ConfigureServices(local,local,local,local);
                var boot=UnityEngine.Object.FindFirstObjectByType<HotpotSort.Bootstrap.Bootstrap>();boot.enabled=true;
                await Wait(()=>boot.IsConfigured,"boot");var controller=boot.Controller;var v=c.PlayerView;
                local.RewardPrompt=request=>{Debug.Log("TASK046 PROMPT "+request.Kind);return v.ShowRewardSimulationAsync(request);};
                await controller.StartTodayAsync();
                await Wait(()=>v.LastSnapshot.plates.Length>=4,"warmup supply");await Task.Delay(1500);
                Check(v.LastSnapshot.completedOrders==0&&v.LastSnapshot.thirdPotThreshold==20&&v.LastSnapshot.fourthPotThreshold==38,"warmup excluded from pot progress");
                var context=c.ActiveCore.Snapshot.Challenge;Call(controller,"Release");Call(controller,"Create",context,ChallengeStage.Formal,0);
                await Wait(()=>v.LastSnapshot.plates.Length>=10,"formal supply",20000);await Task.Delay(1500);
                Check(v.LastSnapshot.completedOrders==0&&v.LastSnapshot.totalOrders==61,"formal starts at zero of 61");
                Check(local is IAccountRewardProfileStore,"runtime profile exposes reward inventory");
                // Exercise the actual intro, platform simulation, reservation and completion paths.
                await Wait(()=>c.ReadSwapOrderOffer().legalSlots.Length>0,"legal swap",10000);
                Call(v,"BeginSwapPresentation");
                Check(c.ReadSwapOrderOffer().legalSlots.Length>0,"intro overlay does not hide legal targets");
                VisualCapture.Capture(v,1080,1920,Path.Combine(root,"swap-intro.png"));
                c.SetViewport(controller.CurrentViewport);Canvas.ForceUpdateCanvases();await Task.Delay(100);
                int before=local.SharesUsed(HotpotSort.Session.TimeResolver.ChallengeDay(DateTimeOffset.UtcNow));
                Click(v,"分享领取");Debug.Log("TASK046 AFTER CLAIM phase="+v.LastSnapshot.phase+" creditPending="+v.SwapCreditAcquisitionPending+" input="+controller.CanAcceptInput+" labels="+string.Join("|",v.GetComponentsInChildren<Text>().Select(t=>t.text)));await Wait(()=>Button(v,"模拟成功")!=null,"swap platform prompt");Click(v,"取消");
                await Wait(()=>Button(v,"分享领取")!=null,"swap returns after cancel");
                Check(local.SwapOrderRewardInventory==0&&local.SharesUsed(HotpotSort.Session.TimeResolver.ChallengeDay(DateTimeOffset.UtcNow))==before,"cancel leaves inventory and quota unchanged");
                Click(v,"分享领取");await Wait(()=>Button(v,"模拟成功")!=null,"swap retry platform prompt");Click(v,"模拟成功");
                await Wait(()=>local.SwapOrderRewardInventory==1&&c.ReadSwapOrderOffer().selecting,"reward granted and selection entered");
                Check(local.SharesUsed(HotpotSort.Session.TimeResolver.ChallengeDay(DateTimeOffset.UtcNow))==before+1,"swap reward consumes one share");
                Call(v,"CancelSwapPresentation");Check(local.SwapOrderRewardInventory==1,"selection cancel retains credit");
                Call(v,"BeginSwapPresentation");Click(v,"使用库存");
                var target=v.GetComponentsInChildren<Button>().First(b=>b.name.StartsWith("SwapTarget_"));target.onClick.Invoke();
                await Wait(()=>local.SwapOrderRewardInventory==0,"successful swap consumes inventory");
                Check(!c.ActiveCore.SwapOrderPending,"swap transaction completed");
                controller.StartChallengeTimer();
                var born=new HashSet<string>();float minimumSpawnGap=float.PositiveInfinity;
                c.Updated+=update=>
                {
                    if(!v.RedealActive)return;
                    foreach(var body in v.World.Bodies)
                    {
                        if(!born.Add(body.data.plateId))continue;
                        var at=v.World.Position(body);
                        foreach(var other in v.World.Bodies)
                            if(other!=body)minimumSpawnGap=Mathf.Min(minimumSpawnGap,Vector2.Distance(at,v.World.Position(other))-body.data.radius-other.data.radius);
                    }
                };
                Call(v,"ChooseReward",RewardKind.Shuffle);
                var demo=v.GetComponentInChildren<ToolDemoLoop>();demo.enabled=false;
                foreach(var frame in new[]{new{seconds=.95f,name="hold"},new{seconds=.77f,name="clear"},new{seconds=.8f,name="drop"},new{seconds=.8f,name="settled"}})
                {
                    for(float time=0;time<frame.seconds-.001f;time+=.01f)demo.Advance(.01f);
                    VisualCapture.Capture(v,1080,1920,Path.Combine(root,"shuffle-demo-"+frame.name+".png"));
                }
                c.SetViewport(controller.CurrentViewport);Canvas.ForceUpdateCanvases();await Task.Delay(100);
                Click(v,"分享领取");await Wait(()=>Button(v,"模拟成功")!=null,"shuffle platform prompt");
                Click(v,"模拟成功");await Wait(()=>v.RedealActive,"redeal started");double seconds=controller.ChallengeSeconds;int max=0;
                double deadline=EditorApplication.timeSinceStartup+30;
                while(v.RedealActive){max=Math.Max(max,v.LastSnapshot.plates.Length);Check(Math.Abs(controller.ChallengeSeconds-seconds)<.02,"redeal freezes timer");if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("redeal finish");await Task.Delay(100);}
                Check((int)c.GetType().GetField("redealSupplied",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(c)==24,"redeal supplies twenty-four plates");
                Check(max<=24,"redeal batch does not exceed twenty-four");
                Check(born.Count==24&&minimumSpawnGap>=2f,"all twenty-four spawn without overlapping colliders");
                Check(v.LastSnapshot.plates.Select(p=>Mathf.RoundToInt(p.motion.spawnX)).Distinct().Count()>=8,"irregular horizontal spawn positions");
                var observation=v.CaptureClickability();var needs=v.LastSnapshot.orders.Where(o=>o.enabled&&o.count<o.required).Select(o=>o.foodId).ToArray();
                Check(observation!=null&&v.LastSnapshot.plates.SelectMany(p=>p.items).Any(i=>needs.Contains(i.foodId)&&observation.clickable.Contains(i.itemId)),"redeal ends with clickable order food");
                Check(controller.CanAcceptInput,"redeal restores input");
                await Wait(()=>v.LastSnapshot.plates.Length>24,"normal supply continues above twenty-four",10000);
                Check(v.LastSnapshot.plates.Length>24,"normal supply no longer has a plate-count cap");
                VisualCapture.Capture(v,1080,1920,Path.Combine(root,"redeal-result.png"));
            }
            catch(Exception ex){exit=1;Debug.LogException(ex);checks.Add("FAIL "+ex);}
            finally{SessionState.SetBool(Key,false);File.WriteAllText(Path.Combine(root,"gameplay-result.json"),CanonicalJson.Write(CanonicalJson.Object("exitCode",exit,"checks",checks)));EditorApplication.Exit(exit);}
        }
    }
}
#endif
