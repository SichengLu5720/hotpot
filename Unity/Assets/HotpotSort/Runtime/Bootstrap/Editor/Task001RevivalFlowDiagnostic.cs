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
using HotpotSort.Presentation;
using HotpotSort.Platform;
using HotpotSort.Profile;
using HotpotSort.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HotpotSort.Bootstrap
{
    // Core/interface integration evidence through the real Boot -> Mapper -> GameplayView.
    // Does not assert the new artwork, animation, visual placement or performance.
    [InitializeOnLoad]
    public static class Task001RevivalFlowDiagnostic
    {
        const string Active="Hotpot.Task001.V7.CoreFlow.Active",Report="Hotpot.Task001.V7.CoreFlow.Report";
        static Task001RevivalFlowDiagnostic(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-task001Report");
            if(index<0||index+1>=args.Length)throw new ArgumentException("-task001Report required");
            SessionState.SetString(Report,args[index+1]);SessionState.SetBool(Active,true);
            EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");UnityEngine.Object.FindFirstObjectByType<Bootstrap>().enabled=false;EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange value){if(value==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Active,false))Execute();}
        static void Assert(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        static Dictionary<string,object> State(DailySession core)=>CanonicalJson.Map(CanonicalJson.Parse(core.Snapshot.CanonicalStateJson));
        static int Num(object value)=>CanonicalJson.Int(value);
        static ulong Boundary(SessionController controller)=>(ulong)Math.Floor(controller.ActiveSeconds*1000);
        static void TriggerOverflow(DailyProductionComposition composition,SessionController controller)
        {
            var core=composition.ActiveCore;
            // Direct committed supplies deliberately isolate rules from the physical gate.
            // The existing v6 geometry/physics suite remains a separate integration check.
            ulong seq=10000;
            while(DailyViewMapper.PendingHead(core.Snapshot)!=0)
                Assert(core.Supply(new SupplyObservation(++seq,Boundary(controller),true,true)).Accepted,"direct rule-isolation supply");
            for(int i=0;i<6;i++)
            {
                var state=State(core);var kinds=CanonicalJson.Array(state["orders"]).Select(CanonicalJson.Map).Where(o=>(string)o["state"]!="Locked").Select(o=>(string)o["kind"]).ToArray();
                int id=CanonicalJson.Array(state["items"]).Select(CanonicalJson.Map).Where(item=>(string)item["location"]=="ActiveAvailable"&&!kinds.Contains((string)item["kind"])).Select(item=>Num(item["itemId"])).First();
                Assert(core.Tap(new TapCommand(id,(ulong)i+10000,Boundary(controller),true)).Accepted,"real overflow tap");
            }
            Assert(core.RevivalPending&&controller.Pauses==PauseReasons.Revival,"real offer pause");
        }
        static void Background(SessionController controller,bool enabled)=>typeof(SessionController).GetMethod("OnLifecycle",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(controller,new object[]{enabled?PlatformLifecycle.Background:PlatformLifecycle.Foreground});
        static async void Execute()
        {
            var results=new List<object>();string report=SessionState.GetString(Report,"");
            string key="HotpotSort.Task001.v9.FlowQA."+Guid.NewGuid().ToString("N");int exit=0;Bootstrap boot=null;
            var testKeys=new List<string>{key};
            try
            {
                var composition=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();
                var local=new LocalDevelopmentServices(key,"test");var utc=DateTimeOffset.Parse("2026-09-22T00:00:00Z");
                composition.ConfigureServices(local,local,local,local,()=>utc);
                boot=UnityEngine.Object.FindFirstObjectByType<Bootstrap>();boot.enabled=true;
                for(int i=0;i<100;i++){boot=UnityEngine.Object.FindFirstObjectByType<Bootstrap>();if(boot&&boot.IsConfigured)break;await Task.Delay(50);}
                Assert(boot&&boot.IsConfigured,"Boot initialization");var controller=boot.Controller;
                composition.enabled=false;composition.PlayerView.World.enabled=false;
                typeof(SessionController).GetField("clock",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(controller,new FrozenClock());
                typeof(SessionController).GetField("time",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(controller,new TimeResolver(null,new FixedUtc(utc)));
                var pending=new TaskCompletionSource<RewardOutcome>();RewardRequest requested=null;
                local.RewardPrompt=request=>{requested=request;return pending.Task;};
                composition.ConfigureServices(local,local,local,local,()=>utc);
                await controller.StartTodayAsync();TriggerOverflow(composition,controller);
                double elapsed=controller.ActiveSeconds;await Task.Delay(120);Assert(controller.ActiveSeconds==elapsed,"offer clock moves");
                var core=composition.ActiveCore;var source=CanonicalJson.Array(State(core)["buffer"]).Select(Num).ToArray();string session=core.Snapshot.SessionId;
                var offer=composition.ReadRevivalOffer();Assert(offer.available&&offer.route==RewardRoute.SimulatedShare,"initial preferred route");
                RevivalTransferBatch delivered=null;composition.Updated+=update=>{var e=update.events.FirstOrDefault(value=>value.revivalTransfer!=null);if(e!=null)delivered=e.revivalTransfer;};
                var reward=composition.RequestRevivalAsync();Assert(requested.Kind==RewardKind.Revival&&requested.Route==RewardRoute.SimulatedShare,"revival route contract");
                Assert(await composition.RequestRevivalAsync()==RewardApplicationResult.Duplicate,"second reward click");
                pending.SetResult(RewardOutcome.Success);Assert(await reward==RewardApplicationResult.Applied,"reward result");
                Assert(core.Snapshot.SessionId==session&&core.RevivalUsed&&core.RevivalPending&&local.SharesUsed("20260922")==1,"revival identity/quota");
                Assert(controller.Pauses==PauseReasons.Revival&&controller.ActiveSeconds==elapsed,"revival pause released on reward");
                Assert(delivered!=null&&delivered.items.Select(item=>int.Parse(item.itemId)).SequenceEqual(source),"real mapper event source facts");
                var actual=composition.PlayerView.LastSnapshot;
                Assert(actual.buffer.All(item=>item==null)&&actual.revivalTransfer!=null&&actual.revivalTransfer.token.Matches(delivered.token),"real GameplayView did not receive batch");
                File.WriteAllText(Path.ChangeExtension(report,"view-batch.json"),JsonUtility.ToJson(actual.revivalTransfer,true));
                results.Add(CanonicalJson.Object("case","UF01","status","PASS","scope","Real core -> Mapper -> GameplayView batch transport; initial share priority; double click; same session; paused until completion. No rendered transfer animation assertion."));
                controller.Request(SessionAction.Pause);Background(controller,true);
                Assert(!composition.CompleteRevivalTransfer(new RevivalCompletionToken{sessionId="stale",sessionGeneration=controller.Generation}),"wrong completion accepted");
                Assert(composition.CompleteRevivalTransfer(delivered.token)&&!composition.CompleteRevivalTransfer(delivered.token),"completion not idempotent");
                Assert(controller.Pauses==(PauseReasons.User|PauseReasons.Background)&&controller.ActiveSeconds==elapsed&&!core.RevivalPending,"completion lost external pause");
                controller.Request(SessionAction.Resume);Background(controller,false);Assert(controller.CanAcceptInput,"resume after completion");
                results.Add(CanonicalJson.Object("case","UF02","status","PASS","scope","Real completion token validation, one completion, User/Background pause composition."));
                var oldToken=delivered.token;await controller.RetryAsync();Assert(!composition.CompleteRevivalTransfer(oldToken),"old completion after retry");TriggerOverflow(composition,controller);
                Assert(composition.ReadRevivalOffer().route==RewardRoute.SimulatedAd,"cooldown did not select ad");
                string retainedOffer=composition.ActiveCore.RevivalOfferId;
                pending=new TaskCompletionSource<RewardOutcome>();var cancelled=composition.RequestRevivalAsync();string cancelledId=requested.RequestId;pending.SetResult(RewardOutcome.Cancelled);
                Assert(await cancelled==RewardApplicationResult.Cancelled&&composition.ActiveCore.Snapshot.Status==GameStatus.Paused&&composition.ActiveCore.RevivalPending&&!composition.ActiveCore.RevivalUsed&&controller.Pauses==PauseReasons.Revival&&local.SharesUsed("20260922")==1,"cancel changed offer/effect/quota");
                Assert(composition.ReadRevivalOffer().offerId==retainedOffer&&composition.ReadRevivalOffer().route==RewardRoute.SimulatedAd,"cancel changed route/offer");
                pending=new TaskCompletionSource<RewardOutcome>();var retried=composition.RequestRevivalAsync();Assert(requested.RequestId!=cancelledId&&requested.RevivalOfferId==retainedOffer&&requested.Route==RewardRoute.SimulatedAd,"retry did not create fresh same-offer request");pending.SetResult(RewardOutcome.Success);
                Assert(await retried==RewardApplicationResult.Applied&&composition.ActiveCore.RevivalUsed&&composition.ActiveCore.RevivalPending&&controller.Pauses==PauseReasons.Revival&&local.SharesUsed("20260922")==1,"ad retry effect/quota/pause");
                Assert(composition.CompleteRevivalTransfer(delivered.token)&&controller.CanAcceptInput,"retry transfer completion");
                await controller.RetryAsync();TriggerOverflow(composition,controller);composition.DeclineRevival();Assert(composition.ActiveCore.Snapshot.Status==GameStatus.Failed&&!composition.ActiveCore.RevivalPending,"explicit decline did not end offer");
                results.Add(CanonicalJson.Object("case","UF03","status","PASS","scope","Cooldown selects ad; cancellation retains same paused offer/route; fresh-ID retry succeeds; explicit decline ends run."));
                await controller.RetryAsync();TriggerOverflow(composition,controller);pending=new TaskCompletionSource<RewardOutcome>();var obsolete=composition.RequestRevivalAsync();var oldPromise=pending;
                await controller.RetryAsync();string fresh=composition.ActiveCore.Snapshot.SessionId;oldPromise.SetResult(RewardOutcome.Success);
                Assert(await obsolete==RewardApplicationResult.Stale&&composition.ActiveCore.Snapshot.SessionId==fresh&&controller.CanAcceptInput&&!composition.ActiveCore.RevivalUsed&&local.SharesUsed("20260922")==1,"old session reward altered retry");
                results.Add(CanonicalJson.Object("case","UF04","status","PASS","scope","Retry invalidates pending request; late success cannot grant or pause new game."));
                composition.ActiveCore.Timeout(600000);Assert(composition.ActiveCore.Snapshot.Status==GameStatus.Failed&&!composition.ActiveCore.RevivalPending&&!composition.ReadRevivalOffer().available,"timeout revival");
                controller.Exit();Assert(!composition.CompleteRevivalTransfer(oldToken)&&composition.ActiveCore==null,"exit completion resurrected");
                results.Add(CanonicalJson.Object("case","UF05","status","PASS","scope","Timeout bypasses revival; exit invalidates completion."));
                await NativeMatrix(composition,controller,utc,testKeys,results);
                Debug.Log("TASK001_V9_REVIVAL_FLOW_OK: real Boot/composition and native adapter callback matrix; no platform calls or visual/device claim.");
            }
            catch(Exception e){exit=1;results.Add(CanonicalJson.Object("case","exception","status","FAIL","actual",e.ToString()));Debug.LogException(e);}
            finally
            {
                boot?.Controller?.Dispose();
                foreach(var testKey in testKeys){string partition=testKey+".profile-v1."+RecoverableProfileStorage.Hash("test\nlocal");PlayerPrefs.DeleteKey(testKey);PlayerPrefs.DeleteKey(partition);PlayerPrefs.DeleteKey(partition+".backup");}PlayerPrefs.Save();
                File.WriteAllText(report,CanonicalJson.Write(CanonicalJson.Object("task","TASK-001","taskVersion",9,"checkpoint","HC-02-v9-Code / HC-02-v1-Code","scope","Real Boot/Core/Coordinator/Mapper/native adapter fakes; no platform, rendering or device QA","results",results,"exitCode",exit)));
                SessionState.SetBool(Active,false);EditorApplication.Exit(exit);
            }
        }
        static async Task NativeMatrix(DailyProductionComposition composition,SessionController controller,DateTimeOffset utc,List<string> keys,List<object> results)
        {
            foreach(string scenario in new[]{"share-failed","share-cancelled","share-unavailable","share-no-hide","ad-load-failure","ad-no-inventory","ad-false","ad-null"})
            foreach(bool retry in new[]{true,false})
            {
                bool isShare=scenario.StartsWith("share-");
                string key="HotpotSort.RevivalMatrix."+Guid.NewGuid().ToString("N");keys.Add(key);
                var profile=new LocalDevelopmentServices(key,"test");
                if(!isShare)Assert(profile.TryReserveShare("20260922","seed",utc)&&profile.TryCommitShare("20260922","seed",utc),"ad fixture cooldown");
                int beforeQuota=profile.SharesUsed("20260922"),beforeClaims=profile.ReadSnapshot().rewards.Count;
                var runtime=new FakeRuntime();var config=new WeChatRuntimeConfig{rewardedAdUnitId="test-only"};
                using(var share=new WeChatShareService(config,runtime))
                using(var service=new WeChatRewardService(config,runtime,share))
                {
                    var tracked=new TrackedRewards(service);
                    composition.ConfigureServices(profile,tracked,null,share,()=>utc);
                    if(composition.ActiveCore==null)await controller.StartTodayAsync();else await controller.RetryAsync();TriggerOverflow(composition,controller);
                    var core=composition.ActiveCore;string offer=core.RevivalOfferId,hash=core.StateHash;
                    int[] source=CanonicalJson.Array(State(core)["buffer"]).Select(Num).ToArray();
                    var route=isShare?RewardRoute.WeChatShare:RewardRoute.WeChatRewardedVideo;
                    Assert(composition.ReadRevivalOffer().effectiveRoute==route,"matrix initial route "+scenario);
                    int events=0;RevivalTransferBatch batch=null;
                    Action<ViewUpdate> observed=update=>{foreach(var e in update.events)if(e.revivalTransfer!=null){events++;batch=e.revivalTransfer;}};
                    composition.Updated+=observed;
                    try
                    {
                        if(scenario=="share-failed")runtime.ThrowShare=true;
                        if(scenario=="share-unavailable")config.enableShare=false;
                        var failed=composition.RequestRevivalAsync();var first=tracked.Last;
                        Action oldHide=runtime.HideCopy,oldShow=runtime.ShowCopy;
                        Action oldLoad=runtime.Video?.LoadCopy;Action<bool?> oldClose=runtime.Video?.CloseCopy;
                        if(scenario=="share-cancelled")tracked.CancelRequest(first.RequestId);
                        if(scenario=="share-no-hide"){runtime.Now=10;runtime.Advance();}
                        if(scenario=="ad-load-failure")runtime.Video.Error();
                        if(scenario=="ad-no-inventory"){runtime.Video.Error();runtime.Video.Error();}
                        if(scenario=="ad-false"||scenario=="ad-null"){runtime.Video.Ready();runtime.Video.Close(scenario=="ad-false"?(bool?)false:null);runtime.Video.Close(true);}
                        runtime.Flush();
                        var expected=scenario=="share-cancelled"?RewardApplicationResult.Cancelled:scenario=="share-unavailable"?RewardApplicationResult.Unavailable:RewardApplicationResult.Failed;
                        Assert(await failed==expected,"matrix failure outcome "+scenario);
                        Assert(core.StateHash==hash&&core.RevivalPending&&!core.RevivalUsed&&controller.Pauses==PauseReasons.Revival&&events==0,"failed attempt mutated offer "+scenario);
                        Assert(profile.SharesUsed("20260922")==beforeQuota&&profile.ReadSnapshot().rewards.Count==beforeClaims&&!profile.ReadShareAvailability("20260922",utc).Reserved,"failed attempt spent/reserved quota "+scenario);
                        Assert(composition.ReadRevivalOffer().available&&composition.ReadRevivalOffer().offerId==offer&&composition.ReadRevivalOffer().effectiveRoute==route,"failed attempt changed route/offer "+scenario);
                        Assert(runtime.Listeners==0,"failure leaked lifecycle listeners "+scenario);
                        if(!retry)
                        {
                            composition.DeclineRevival();oldHide?.Invoke();oldShow?.Invoke();oldLoad?.Invoke();oldClose?.Invoke(true);runtime.Flush();
                            Assert(core.Snapshot.Status==GameStatus.Failed&&!core.RevivalPending&&!core.RevivalUsed&&events==0&&profile.SharesUsed("20260922")==beforeQuota,"decline or late callback revived "+scenario);
                        }
                        else
                        {
                            runtime.ThrowShare=false;config.enableShare=true;
                            var applied=composition.RequestRevivalAsync();var second=tracked.Last;
                            Assert(second.RequestId!=first.RequestId&&second.RevivalOfferId==offer&&second.Route==route,"retry identity "+scenario);
                            Assert(await composition.RequestRevivalAsync()==RewardApplicationResult.Duplicate,"double click spawned request "+scenario);
                            oldHide?.Invoke();oldShow?.Invoke();oldLoad?.Invoke();oldClose?.Invoke(true);runtime.Flush();
                            Assert(!applied.IsCompleted&&!core.RevivalUsed&&events==0,"old callback granted new request "+scenario);
                            if(isShare){runtime.Hide();runtime.Show();runtime.Show();}
                            else{runtime.Video.Ready();runtime.Video.Ready();Assert(runtime.Video.Shows==1,"duplicate load shows twice");runtime.Video.Close(true);runtime.Video.Close(false);}
                            Assert(!applied.IsCompleted&&!core.RevivalUsed,"platform result was not deferred");runtime.Flush();
                            Assert(await applied==RewardApplicationResult.Applied&&core.RevivalUsed&&core.RevivalPending&&controller.Pauses==PauseReasons.Revival,"successful retry did not stay transfer-paused "+scenario);
                            Assert(events==1&&batch!=null&&batch.items.Select(x=>int.Parse(x.itemId)).SequenceEqual(source),"revival transfer lost/duplicated tray items "+scenario);
                            var items=CanonicalJson.Array(State(core)["items"]).Select(CanonicalJson.Map).ToArray();
                            Assert(items.Select(x=>Num(x["itemId"])).Distinct().Count()==items.Length&&source.All(id=>items.Count(x=>Num(x["itemId"])==id&&Num(x["plateId"])==int.Parse(batch.token.newPlateId))==1),"inventory duplicate "+scenario);
                            Assert(CanonicalJson.Array(State(core)["buffer"]).All(x=>x==null),"small trays not cleared "+scenario);
                            Assert(profile.SharesUsed("20260922")==beforeQuota+(isShare?1:0)&&profile.ReadSnapshot().rewards.Count==beforeClaims+1,"success commit count "+scenario);
                            string transferred=core.StateHash;runtime.Show();runtime.Video?.Close(true);runtime.Flush();Assert(core.StateHash==transferred&&events==1,"duplicate success reapplied "+scenario);
                            Assert(!controller.CanAcceptInput&&composition.CompleteRevivalTransfer(batch.token)&&!composition.CompleteRevivalTransfer(batch.token)&&controller.CanAcceptInput,"completion resume/token idempotency "+scenario);
                        }
                        results.Add(CanonicalJson.Object("case","NR_"+scenario+"_"+(retry?"retry":"decline"),"status","PASS","scope","Real Boot/Composition/Core/Coordinator/native adapters; same offer and route, quota/effect atomicity, duplicate and late callback handling."));
                    }
                    finally{composition.Updated-=observed;}
                }
            }
            foreach(bool isShare in new[]{true,false})foreach(bool replaceSession in new[]{true,false})
            {
                string key="HotpotSort.RevivalInvalidation."+Guid.NewGuid().ToString("N");keys.Add(key);
                var profile=new LocalDevelopmentServices(key,"test");if(!isShare)Assert(profile.TryReserveShare("20260922","seed",utc)&&profile.TryCommitShare("20260922","seed",utc),"invalidation seed");
                int before=profile.SharesUsed("20260922"),claims=profile.ReadSnapshot().rewards.Count;
                var runtime=new FakeRuntime();var config=new WeChatRuntimeConfig{rewardedAdUnitId="test-only"};
                using(var share=new WeChatShareService(config,runtime))using(var service=new WeChatRewardService(config,runtime,share))
                {
                    var tracked=new TrackedRewards(service);composition.ConfigureServices(profile,tracked,null,share,()=>utc);
                    await controller.RetryAsync();TriggerOverflow(composition,controller);
                    var oldCore=composition.ActiveCore;var request=composition.RequestRevivalAsync();
                    Action hide=runtime.HideCopy,show=runtime.ShowCopy,load=runtime.Video?.LoadCopy;Action<bool?> close=runtime.Video?.CloseCopy;
                    if(isShare){runtime.Hide();runtime.Show();}else{runtime.Video.Ready();runtime.Video.Close(true);}
                    Assert(!request.IsCompleted,"invalidation fixture not deferred");
                    if(replaceSession)await controller.RetryAsync();
                    else Assert(oldCore.ResolveRevival(new ResolveRevivalCommand(oldCore.RevivalOfferId,null,false,Boundary(controller))).Accepted,"invalidate revival target");
                    var current=composition.ActiveCore;string hash=current.StateHash;
                    hide?.Invoke();show?.Invoke();load?.Invoke();close?.Invoke(true);runtime.Flush();
                    Assert(await request==RewardApplicationResult.Stale&&tracked.Cancellations>0&&runtime.Listeners==0,"invalidation did not cancel platform");
                    Assert(current.StateHash==hash&&!current.RevivalUsed&&profile.SharesUsed("20260922")==before&&profile.ReadSnapshot().rewards.Count==claims,"invalidated success changed core/quota");
                    Assert(replaceSession?controller.CanAcceptInput:current.Snapshot.Status==GameStatus.Failed,"invalidation destination");
                    results.Add(CanonicalJson.Object("case","NR_"+(isShare?"share":"ad")+"_invalidate_"+(replaceSession?"session":"target"),"status","PASS","scope","Queued and captured stale callbacks after actual Composition cancellation cannot revive, spend quota or pause replacement session."));
                }
            }
        }
        sealed class TrackedRewards:IRewardService,IRewardRequestCancellation
        {
            readonly WeChatRewardService service;public RewardRequest Last;public int Cancellations;
            public TrackedRewards(WeChatRewardService service){this.service=service;}
            public bool IsDevelopmentSimulation=>false;
            public Task<RewardOutcome> RequestAsync(RewardRequest request){Last=request;return service.RequestAsync(request);}
            public void CancelRequest(string id){Cancellations++;service.CancelRequest(id);}public void CancelAll(){Cancellations++;service.CancelAll();}
        }
        sealed class FakeRuntime:IWeChatRewardRuntime
        {
            public bool Available=>true;public double Now;public double MonotonicSeconds=>Now;public bool ThrowShare;public FakeVideo Video;
            public event Action Hidden,Shown,Tick;public event Action Stopping{add{}remove{}}
            public Action HideCopy=>Hidden;public Action ShowCopy=>Shown;
            public int Listeners=>(Hidden?.GetInvocationList().Length??0)+(Shown?.GetInvocationList().Length??0)+(Tick?.GetInvocationList().Length??0);
            readonly Queue<Action> queue=new Queue<Action>();public void Post(Action a)=>queue.Enqueue(a);public void Flush(){while(queue.Count>0)queue.Dequeue()();}
            public void Share(string title,string image){if(ThrowShare)throw new InvalidOperationException("injected-share-failure");}
            public void RegisterMenu(string title,string image){}public IWeChatRewardVideo CreateVideo(string id)=>Video=new FakeVideo();
            public void Hide()=>Hidden?.Invoke();public void Show()=>Shown?.Invoke();public void Advance()=>Tick?.Invoke();
        }
        sealed class FakeVideo:IWeChatRewardVideo
        {
            public event Action Loaded,Failed;public event Action<bool?> Closed;public int Shows;
            public Action LoadCopy=>Loaded;public Action<bool?> CloseCopy=>Closed;
            public void Load(){}public void Show(){Shows++;}public void Ready()=>Loaded?.Invoke();public void Error()=>Failed?.Invoke();public void Close(bool? ended)=>Closed?.Invoke(ended);
            public void Dispose(){Loaded=null;Failed=null;Closed=null;}
        }
        sealed class FrozenClock:IMonotonicClock{public double Seconds=>0;}
        sealed class FixedUtc:ITimeProvider{readonly DateTimeOffset utc;public FixedUtc(DateTimeOffset utc){this.utc=utc;}public Task<DateTimeOffset> GetUtcAsync()=>Task.FromResult(utc);}
    }
}
#endif
