#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Determinism;
using HotpotSort.Platform;
using HotpotSort.Presentation;
using HotpotSort.Profile;
using HotpotSort.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HotpotSort.Bootstrap
{
    [InitializeOnLoad]
    public static class Task001V9JointDiagnostic
    {
        const string Key="Task001V9JointQa";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static Task001V9JointDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        static string ReportPath(){var args=Environment.GetCommandLineArgs();return args[Array.IndexOf(args,"-task001Report")+1];}
        public static void Run(){SessionState.SetString(Key+"Report",ReportPath());SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");UnityEngine.Object.FindFirstObjectByType<Bootstrap>().enabled=false;EditorApplication.EnterPlaymode();}
        public static void CompileWebGL()
        {
            int exit=0;var report=ReportPath();var results=new List<object>();
            try
            {
                var options=new UnityEditor.Build.Player.ScriptCompilationSettings{target=BuildTarget.WebGL,group=BuildTargetGroup.WebGL,options=UnityEditor.Build.Player.ScriptCompilationOptions.None};
                string destination=Path.Combine(Path.GetDirectoryName(report),"player-scripts");Directory.CreateDirectory(destination);
                var compiled=UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(options,destination);
                Assert(compiled.assemblies!=null&&compiled.assemblies.Count>0,"WebGL player script compilation empty");
                results.Add(CanonicalJson.Object("assemblies",compiled.assemblies.Count,"status","PASS"));Debug.Log("JOINT_WEBGL_PLAYER_SCRIPTS_PASS");
            }
            catch(Exception ex){exit=1;results.Add(CanonicalJson.Object("status","FAIL","error",ex.ToString()));Debug.LogException(ex);}
            finally{File.WriteAllText(report,CanonicalJson.Write(CanonicalJson.Object("exitCode",exit,"scope","Full project WebGL player scripts, not export or device validation","results",results)));EditorApplication.Exit(exit);}
        }
        static void Assert(bool condition,string why){if(!condition)throw new Exception(why);}
        static void Field(object target,string name,object value)=>target.GetType().GetField(name,Private).SetValue(target,value);
        static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Private).GetValue(target);
        static void Invoke(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Private).Invoke(target,args);
        static void Background(SessionController c,bool value)=>Invoke(c,"OnLifecycle",value?PlatformLifecycle.Background:PlatformLifecycle.Foreground);
        static void ClearGate(DailyProductionComposition c){foreach(var body in c.PlayerView.World.Bodies){body.rigidbody.position=new Vector2(body.rigidbody.position.x,4f);body.node.transform.position=body.rigidbody.position;}}
        static Dictionary<string,object> State(DailySession core)=>CanonicalJson.Map(CanonicalJson.Parse(core.Snapshot.CanonicalStateJson));
        static ulong Boundary(SessionController c)=>(ulong)Math.Floor(c.ChallengeSeconds*1000);
        static void Overflow(DailyProductionComposition c,SessionController controller)
        {
            var core=c.ActiveCore;ulong sequence=10000;
            while(DailyViewMapper.PendingHead(core.Snapshot)!=0)Assert(core.Supply(new SupplyObservation(++sequence,Boundary(controller),true,true)).Accepted,"fixture supply");
            for(int i=0;i<6;i++)
            {
                var state=State(core);var kinds=CanonicalJson.Array(state["orders"]).Select(CanonicalJson.Map).Where(o=>(string)o["state"]!="Locked").Select(o=>(string)o["kind"]).ToArray();
                var item=CanonicalJson.Array(state["items"]).Select(CanonicalJson.Map).First(it=>(string)it["location"]=="ActiveAvailable"&&!kinds.Contains((string)it["kind"]));
                Assert(core.Tap(new TapCommand(CanonicalJson.Int(item["itemId"]),(ulong)i+10000,Boundary(controller),true)).Accepted,"fixture overflow");
            }
            Assert(core.RevivalPending&&controller.Pauses.HasFlag(PauseReasons.Revival),"missing offer");
        }
        static async void Execute()
        {
            var results=new List<object>();int exit=0;string report=SessionState.GetString(Key+"Report","");
            string profileKey="HotpotSort.JointQA."+Guid.NewGuid().ToString("N");WeChatServiceScope scope=null;Bootstrap boot=null;
            try
            {
                // Inject before Start so Boot never migrates or writes the player's profile.
                var composition=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();Assert(composition,"composition missing");
                var local=new LocalDevelopmentServices(profileKey,"test");var utc=new DateTimeOffset(2026,9,22,0,0,0,TimeSpan.Zero);
                composition.ConfigureServices(local,local,local,local,()=>utc);
                boot=UnityEngine.Object.FindFirstObjectByType<Bootstrap>();boot.enabled=true;
                for(int i=0;i<100;i++){boot=UnityEngine.Object.FindFirstObjectByType<Bootstrap>();if(boot&&boot.IsConfigured)break;await Task.Delay(50);}
                Assert(boot&&boot.IsConfigured,"Boot failed "+boot?.Status);var controller=boot.Controller;
                composition.enabled=false;composition.PlayerView.World.enabled=false;
                var clock=new Clock();Field(controller,"clock",clock);Field(controller,"time",new TimeResolver(null,new FixedUtc(utc)));
                await controller.StartTodayAsync();Assert(composition.Read().phase==ViewPhase.Running,"start");
                var random=new System.Random(601377);int successes=0;
                Action<int> assertSupply=count=>
                {
                    Assert(composition.PlayerView.World.OccupiedPlateCount==count,"unexpected supply count "+count);
                    if(count>successes){var p=composition.PlayerView.World.Bodies.Last().data;float expected=DailyViewMapper.SpawnX(successes)+(float)(random.NextDouble()*100-50);Assert(Math.Abs(p.motion.spawnX-expected)<.0001&&p.motion.spawnY==-p.radius-8,"random/spawn drift");successes=count;}
                };
                Invoke(composition,"Update");assertSupply(1);clock.SecondsValue=.299999;Invoke(composition,"Update");assertSupply(1);
                clock.SecondsValue=.3;Invoke(composition,"Update");assertSupply(2);clock.SecondsValue=.6;Invoke(composition,"Update");assertSupply(3);
                string before=composition.ActiveCore.StateHash;clock.SecondsValue=.9;Invoke(composition,"Update");assertSupply(3);Assert(before==composition.ActiveCore.StateHash,"blocked tick changed core");
                var first=composition.PlayerView.World.Bodies.First();first.rigidbody.position=new Vector2(first.rigidbody.position.x,1.4f);first.node.transform.position=first.rigidbody.position;
                clock.SecondsValue=1.01;Invoke(composition,"Update");assertSupply(3);clock.SecondsValue=1.2;Invoke(composition,"Update");assertSupply(4);
                ClearGate(composition);clock.SecondsValue=5.05;Invoke(composition,"Update");assertSupply(5);Invoke(composition,"Update");assertSupply(5);
                controller.SetRewardPaused(true);controller.Request(SessionAction.Pause);Background(controller,true);clock.SecondsValue=40;Invoke(composition,"Update");assertSupply(5);
                controller.SetRewardPaused(false);controller.Request(SessionAction.Resume);Assert(controller.Pauses==PauseReasons.Background,"pause composition");Background(controller,false);ClearGate(composition);clock.SecondsValue=40.051;Invoke(composition,"Update");assertSupply(6);
                Assert(!controller.ChallengeTimerStarted&&controller.ChallengeSeconds==0,"challenge timer started before valid item route");
                var waitingState=State(composition.ActiveCore);var waitingItem=CanonicalJson.Array(waitingState["items"]).Select(CanonicalJson.Map).First(i=>(string)i["location"]=="ActiveAvailable");
                composition.Tap(new ViewTap{itemId=Convert.ToString(waitingItem["itemId"]),inputSeq=90001,snapshotRevision=composition.Read().revision});
                Assert(controller.ChallengeTimerStarted&&controller.ChallengeSeconds==0,"valid item route did not arm timer at zero");
                clock.SecondsValue=41.301;Assert(Math.Abs(controller.ChallengeSeconds-1.25)<.0001,"challenge timer did not advance after first valid route");
                controller.Request(SessionAction.Pause);clock.SecondsValue=45;Assert(Math.Abs(controller.ChallengeSeconds-1.25)<.0001,"paused challenge timer moved");controller.Request(SessionAction.Resume);
                await controller.RetryAsync();random=new System.Random(601377);successes=0;Assert(!controller.ChallengeTimerStarted&&controller.ChallengeSeconds==0,"retry retained first-action timer");Invoke(composition,"Update");assertSupply(1);
                results.Add(CanonicalJson.Object("case","UJ01","status","PASS","scope","Real Composition supply continues before first action; challenge timer starts only after routed item, pauses, resets on retry; existing supply capacity, recovery and RNG preserved"));
                if(Environment.GetCommandLineArgs().Contains("-firstActionTimerOnly")){Debug.Log("FIRST_ACTION_TIMER_PASS");return;}
                var runtime=new Runtime();var surface=new Surface();scope=new WeChatServiceScope(new WeChatRuntimeConfig(),runtime,()=>surface);
                Assert(scope.MenuRegistered&&runtime.MenuCount==1,"share menu registration");
                composition.ConfigureServices(local,scope.Rewards,null,scope.Share,()=>utc);composition.ConfigureFriendSurface(scope.Friends,"qa-owner");
                Overflow(composition,controller);var offer=composition.ReadRevivalOffer();Assert(offer.effectiveRoute==RewardRoute.WeChatShare&&!offer.isDevelopmentSimulation,"native route mapping");
                var firstRequest=composition.RequestRevivalAsync();runtime.Now=10;runtime.Advance();Assert(await firstRequest==RewardApplicationResult.Failed,"native share timeout");
                Assert(composition.ActiveCore.RevivalPending&&!composition.ActiveCore.RevivalUsed&&controller.Pauses==PauseReasons.Revival&&local.SharesUsed("20260922")==0,"failed reward ended offer/charged");
                Assert(composition.ReadRevivalOffer().offerId==offer.offerId&&composition.ReadRevivalOffer().effectiveRoute==offer.effectiveRoute,"retry changed offer or route");
                var success=composition.RequestRevivalAsync();Background(controller,true);runtime.Hide();Background(controller,false);runtime.Show();Assert(!success.IsCompleted,"callback not deferred");runtime.Flush();
                Assert(await success==RewardApplicationResult.Applied&&local.SharesUsed("20260922")==1&&controller.Pauses==PauseReasons.Revival,"revival apply invalidated its own request");
                var token=composition.Read().revivalTransfer.token;controller.Request(SessionAction.Pause);
                Assert(composition.CompleteRevivalTransfer(token)&&controller.Pauses==PauseReasons.User,"completion erased User pause");controller.Request(SessionAction.Resume);
                Assert(controller.CanAcceptInput,"completion never resumed");
                utc=utc.AddMinutes(5);await controller.RetryAsync();Overflow(composition,controller);
                var stale=composition.RequestRevivalAsync();await controller.RetryAsync();runtime.Hide();runtime.Show();runtime.Flush();
                Assert(await stale==RewardApplicationResult.Stale&&local.SharesUsed("20260922")==1&&runtime.Listeners==0,"retry leaked native request");
                results.Add(CanonicalJson.Object("case","UJ02","status","PASS","scope","Real core/view revival retry, native share callback, durable quota, transfer/User pause composition and retry cancellation"));
                composition.SetViewport(new Viewport(1080,1920,40,80,1000,1750));composition.SetPlatformPixelRatio(3);Invoke(composition,"ShowFriends");
                Assert(surface.Opens==1&&surface.Viewport.X==40&&surface.Viewport.Y==90&&surface.Viewport.DevicePixelRatio==3,"physical top-left viewport");
                int refresh=surface.Refreshes;local.RecordFirstWin("20260922");Assert(surface.Score==1&&surface.Refreshes>refresh,"score/refresh hook");
                composition.UpdateFriendViewport(new FriendBoardViewport(0,0,720,1280,2));Assert(surface.Viewport.Width==720,"viewport update");composition.CloseFriendSurface();refresh=surface.Refreshes;
                local.RecordFirstWin("20260923");composition.RefreshFriendSurface();Assert(surface.Score==2&&surface.Refreshes==refresh,"closed surface refreshed");
                var settings=local.LoadSettings();settings.MusicEnabled=false;local.SaveSettings(settings);
                var restored=new LocalDevelopmentServices(profileKey,"test");Assert(restored.TotalFirstWins==2&&restored.SharesUsed("20260922")==1&&!restored.LoadSettings().MusicEnabled,"Unity JSON/PlayerPrefs restart");
                Assert(await restored.SyncAsync()==ProfileSyncStatus.NotConfigured&&restored.ReadSnapshot().pending.Count>0,"missing cloud fake success");
                bool deviceFallback=false;try{await new ProfileTrustedTimeProvider(restored).GetUtcAsync();}catch(InvalidOperationException){deviceFallback=true;}Assert(deviceFallback,"false trusted clock");
                results.Add(CanonicalJson.Object("case","UJ03","status","PASS","scope","Surface-only open/refresh/close/viewport/publish, no friend rows, isolated Unity durable profile/settings/outbox roundtrip, NotConfigured clock fallback"));
                var pending=scope.Rewards.RequestAsync(new RewardRequest(99,RewardKind.SwapOrder,RewardRoute.WeChatShare,"20260922"));composition.CloseFriendSurface();scope.Dispose();Assert(await pending==RewardOutcome.Cancelled&&runtime.Disposed&&runtime.Listeners==0,"scope disposal leaked request");scope=null;
                results.Add(CanonicalJson.Object("case","UJ04","status","PASS","scope","Reward -> share -> runtime disposal completes pending request"));Debug.Log("JOINT_UNITY_PASS");
            }
            catch(Exception ex){exit=1;results.Add(CanonicalJson.Object("status","FAIL","error",ex.ToString()));Debug.LogException(ex);}
            finally
            {
                try{boot?.Controller?.Dispose();scope?.Dispose();}
                catch(Exception ex){exit=1;Debug.LogException(ex);}
                string partition=profileKey+".profile-v1."+RecoverableProfileStorage.Hash("test\nlocal");
                PlayerPrefs.DeleteKey(profileKey);PlayerPrefs.DeleteKey(partition);PlayerPrefs.DeleteKey(partition+".backup");PlayerPrefs.Save();
                try{File.WriteAllText(report,CanonicalJson.Write(CanonicalJson.Object("exitCode",exit,"scope","Joint targeted integration only; no visual/device acceptance","results",results)));}
                finally{SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
            }
        }
        sealed class Clock:IMonotonicClock{public double SecondsValue;public double Seconds=>SecondsValue;}
        sealed class FixedUtc:ITimeProvider{readonly DateTimeOffset value;public FixedUtc(DateTimeOffset v){value=v;}public Task<DateTimeOffset> GetUtcAsync()=>Task.FromResult(value);}
        sealed class Runtime:IWeChatRewardRuntime,IDisposable
        {
            public bool Available=>!Disposed;public bool Disposed;public double Now;public double MonotonicSeconds=>Now;public int MenuCount;
            public event Action Hidden,Shown,Tick,Stopping;
            public int Listeners=>(Hidden?.GetInvocationList().Length??0)+(Shown?.GetInvocationList().Length??0)+(Tick?.GetInvocationList().Length??0);
            readonly Queue<Action> queue=new Queue<Action>();public void Post(Action action)=>queue.Enqueue(action);public void Flush(){while(queue.Count>0)queue.Dequeue()();}
            public void Share(string a,string b){}public void RegisterMenu(string a,string b){MenuCount++;}public IWeChatRewardVideo CreateVideo(string id)=>null;
            public void Hide()=>Hidden?.Invoke();public void Show()=>Shown?.Invoke();public void Advance()=>Tick?.Invoke();
            public void Dispose(){Disposed=true;Stopping?.Invoke();queue.Clear();}
        }
        sealed class Surface:IWeChatFriendBoardSurface
        {
            public int Opens,Refreshes,Score;public FriendBoardViewport Viewport;
            public void Open(FriendBoardViewport v){Viewport=v;Opens++;}public void Refresh(){Refreshes++;}public void Close(){}public void UpdateViewport(FriendBoardViewport v){Viewport=v;}
            public void PublishOwnScore(int count,string marker,DateTimeOffset utc){Score=Math.Max(Score,count);}public void Dispose(){}
        }
    }
}
#endif
