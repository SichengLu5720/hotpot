using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Determinism;
using HotpotSort.Replay;
using HotpotSort.Session;
using HotpotSort.Presentation;
using HotpotSort.Bootstrap;

static class RevivalRuleQa
{
    static readonly DailySessionFactory factory=DailySessionFactory.FromProductionJson(File.ReadAllText("Unity/Assets/HotpotSort/Content/Daily/"+DailyContent.RuntimeFileName));
    static readonly List<object> results=new List<object>();
    static string output;
    static void Assert(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    static Dictionary<string,object> State(DailySession s)=>CanonicalJson.Map(CanonicalJson.Parse(s.Snapshot.CanonicalStateJson));
    static Dictionary<string,object>[] Items(DailySession s)=>CanonicalJson.Array(State(s)["items"]).Select(CanonicalJson.Map).ToArray();
    static int Num(object value)=>CanonicalJson.Int(value);
    static ChallengeContext Context()=>new ChallengeContext("20260922",factory.Content.ContentVersion,factory.ConfigurationDigest,"focused-v7",0);
    static DailyFixture Fixture(int slots=5,int spawned=10)
    {
        var all=factory.Content.Plates.Take(spawned).SelectMany(p=>p.Kinds).Select((kind,index)=>new{kind,id=index+1}).ToArray();
        string target=all[0].kind;
        // Descending IDs deliberately detect an incorrect item-ID sort in the batch.
        var ids=all.Where(i=>i.kind!=target).Take(slots).Reverse().Select(i=>(int?)i.id).Concat(Enumerable.Repeat<int?>(null,5-slots));
        return new DailyFixture(spawned,ids,new[]{new FixtureOrder(target,new int[0]),new FixtureOrder(null,new int[0])},new int[0]);
    }
    static DailySession New(int slots=5,DailyRulesVersion rules=DailyRulesVersion.RevivalV3)=>factory.CreateFixtureSession(Context(),Fixture(slots),rules);
    static int BadItem(DailySession s)
    {
        var kinds=CanonicalJson.Array(State(s)["orders"]).Select(CanonicalJson.Map).Where(o=>(string)o["state"]!="Locked").Select(o=>(string)o["kind"]).ToArray();
        return Items(s).Where(i=>(string)i["location"]=="ActiveAvailable"&&!kinds.Contains((string)i["kind"])).Select(i=>Num(i["itemId"])).First();
    }
    static int Offer(DailySession s,ulong boundary=12345)
    {int bad=BadItem(s);Assert(s.Tap(new TapCommand(bad,1,boundary,true)).Accepted,"overflow tap rejected");Assert(s.RevivalPending&&!s.RevivalUsed&&s.Snapshot.Status==GameStatus.Paused,"offer state");return bad;}
    static void Conserved(DailySession s)
    {Assert(s.Snapshot.Status!=GameStatus.Aborted,"invariant abort");var all=Items(s);Assert(all.Length==183&&all.Select(i=>Num(i["itemId"])).Distinct().Count()==183,"item conservation");}
    static async Task Check(string id,Func<Task> test)
    {
        string status="PASS",actual="Assertions passed";
        try{await test();}catch(Exception e){status="FAIL";actual=e.ToString();}
        results.Add(CanonicalJson.Object("case",id,"status",status,"actual",actual));Console.WriteLine(id+" "+status+(status=="FAIL"?" "+actual:""));
    }
    static Task Sync(Action test){test();return Task.CompletedTask;}
    static void Roundtrip(DailySession session)
    {var replay=session.ExportReplay();var run=DailyReplay.Run(factory,replay);Assert(run.Success,run.Error);Assert(run.Session.StateHash==session.StateHash,"replay hash");run.Session.Dispose();}
    static async Task<int> Main(string[] args)
    {
        output=args[0];
        await Check("R01_OrderedAtomicRevivalAndMapper",()=>Sync(()=>
        {
            using(var s=New())
            {
                int bad=Offer(s);var before=State(s);var originalBad=CanonicalJson.Write(Items(s).Single(i=>Num(i["itemId"])==bad));
                int[] sourceIds=CanonicalJson.Array(before["buffer"]).Select(Num).ToArray();
                Assert(!sourceIds.SequenceEqual(sourceIds.OrderBy(x=>x)),"fixture must detect incorrect sorting");
                var offer=s.RevivalOfferId;string hash=s.StateHash;
                Assert(!s.Resume(12345).Accepted&&!s.ClearBuffer(12345).Accepted&&!s.Timeout(600000).Accepted&&s.StateHash==hash,"offer escaped pause");
                Assert(!s.ResolveRevival(new ResolveRevivalCommand("wrong","r",true,12345)).Accepted&&s.StateHash==hash,"wrong offer applied");
                var result=s.ResolveRevival(new ResolveRevivalCommand(offer,"reward-ordered",true,12345));Assert(result.Accepted,"resolve rejected");
                var after=State(s);var queue=CanonicalJson.Array(after["pendingPlateIds"]).Select(Num).ToArray();
                Assert(queue.SequenceEqual(CanonicalJson.Array(before["pendingPlateIds"]).Select(Num).Concat(new[]{51})),"strict Q+[newPlateId]");
                Assert(CanonicalJson.Array(after["buffer"]).All(x=>x==null)&&s.RevivalPending&&s.RevivalUsed&&s.Snapshot.Status==GameStatus.Paused,"transfer pause");
                foreach(string key in new[]{"orders","activePlateIds","mapping","mappingRng","directorRng","logicalBoundary"})Assert(CanonicalJson.Write(before[key])==CanonicalJson.Write(after[key]),"changed "+key);
                Assert(CanonicalJson.Write(Items(s).Single(i=>Num(i["itemId"])==bad))==originalBad,"overflow item changed");
                for(int index=0;index<5;index++){var i=Items(s).Single(x=>Num(x["itemId"])==sourceIds[index]);Assert(Num(i["plateId"])==51&&Num(i["sourceIndex"])==index&&(string)i["location"]=="Pending","source mapping");}
                var mapped=DailyViewMapper.Map(result.Snapshot,result.Events,factory.Content,12.345,42,ViewPauseReasons.Revival);
                var batch=mapped.events.Single(e=>e.kind=="RevivalTransferStarted").revivalTransfer;
                Assert(mapped.snapshot.buffer.All(x=>x==null)&&batch.items.Select(i=>int.Parse(i.itemId)).SequenceEqual(sourceIds),"mapper lost pre-clear facts");
                Assert(batch.items.Select(i=>i.sourceSlot).SequenceEqual(Enumerable.Range(0,5))&&batch.items.Select(i=>i.targetIndex).SequenceEqual(Enumerable.Range(0,5)),"slot sequence");
                Assert(batch.items.All(i=>i.ingredientId.StartsWith("food_"))&&batch.token.sessionId==s.Snapshot.SessionId&&batch.token.sessionGeneration==42&&batch.token.newPlateId=="51","batch identity");
                Assert(batch.token.Matches(mapped.snapshot.revivalTransfer.token),"snapshot/event batch mismatch");
                File.WriteAllText(Path.ChangeExtension(output,"mapper.json"),CanonicalJson.Write(CanonicalJson.Object("sourceBuffer",sourceIds,"coreEventBatch",result.Events.CanonicalEvents.Select(CanonicalJson.Parse).ToArray(),"mappedItems",batch.items.Select(i=>CanonicalJson.Object("itemId",i.itemId,"ingredientId",i.ingredientId,"sourceSlot",i.sourceSlot,"targetIndex",i.targetIndex)).ToArray(),"sessionId",batch.token.sessionId,"generation",batch.token.sessionGeneration,"newPlateId",batch.token.newPlateId)));
                hash=s.StateHash;var rng=s.PresentationRngJson;
                Assert(!s.Supply(new SupplyObservation(999,12345,true,true)).Accepted&&s.StateHash==hash&&s.PresentationRngJson==rng,"transfer spawned/drew RNG");
                Assert(!s.ResolveRevival(new ResolveRevivalCommand(offer,"duplicate-failure",false,12345)).Accepted&&s.StateHash==hash,"late failure killed transfer");
                Assert(!s.CompleteRevivalTransfer(offer,"bad-token",12345).Accepted&&s.StateHash==hash,"wrong completion");
                string token=s.RevivalTransferToken;Assert(s.CompleteRevivalTransfer(offer,token,12345).Accepted&&!s.RevivalPending&&s.Snapshot.Status==GameStatus.Paused,"completion");
                hash=s.StateHash;Assert(!s.CompleteRevivalTransfer(offer,token,12345).Accepted&&hash==s.StateHash,"duplicate completion");
                Assert(s.Resume(12345).Accepted,"post-completion resume");
                for(ulong i=2;i<=7;i++)s.Tap(new TapCommand(BadItem(s),i,12345+i,true));
                Assert(s.Snapshot.Status==GameStatus.Failed&&!s.RevivalPending,"second overflow not final");Conserved(s);Roundtrip(s);
                File.WriteAllText(Path.ChangeExtension(output,"replay.json"),s.ExportReplay().CanonicalJson);
            }
        }));
        await Check("R02_DeclineTimeoutAndOrdinaryClear",()=>Sync(()=>
        {
            using(var s=New()){Offer(s);var before=Items(s).Select(CanonicalJson.Write).ToArray();Assert(s.ResolveRevival(new ResolveRevivalCommand(s.RevivalOfferId,null,false,12345)).Accepted,"decline");Assert(s.Snapshot.Status==GameStatus.Failed&&Items(s).Select(CanonicalJson.Write).SequenceEqual(before),"decline changed inventory");Roundtrip(s);}
            using(var s=New()){int id=BadItem(s);s.Tap(new TapCommand(id,1,600000,true));Assert(s.Snapshot.Status==GameStatus.Failed&&!s.RevivalPending&&!s.CoreEventsJson.Contains("RevivalOffered"),"timeout offered revival");Roundtrip(s);}
            using(var s=New()){var r=s.ClearBuffer(1);Assert(r.Accepted&&s.Snapshot.Status==GameStatus.Running&&!s.RevivalPending&&!s.RevivalUsed&&!r.Events.CanonicalEvents.Any(e=>e.Contains("RevivalTransfer")),"ordinary clear got revival semantics");Conserved(s);Roundtrip(s);}
        }));
        await Check("R03_LegacyV2AndV3ReplayIdentity",()=>Sync(()=>
        {
            var golden=File.ReadAllText(".harness/qa/TASK-001/v7/core-r001/baseline/rules.replay.json");
            var run=DailyReplay.Run(factory,new ReplayPackage("legacy-golden",DailySession.LegacyReplaySchema,golden));Assert(run.Success,run.Error);run.Session.Dispose();
            using(var old=New(5,DailyRulesVersion.LegacyV2))using(var current=New())
            {
                Assert(CanonicalJson.Write(State(old)["mapping"])==CanonicalJson.Write(State(current)["mapping"]),"rules upgrade changed seed mapping");
                old.Tap(new TapCommand(BadItem(old),1,1,true));Assert(old.Snapshot.Status==GameStatus.Failed&&!old.RevivalPending&&old.Snapshot.SchemaVersion==DailySession.LegacyStateSchema,"legacy overflow behavior");Roundtrip(old);
                Assert(!DailyReplay.Run(factory,new ReplayPackage("wrong",DailySession.ReplaySchema,old.ExportReplay().CanonicalJson)).Success,"mismatched replay identity accepted");
                Assert(current.Snapshot.SchemaVersion==DailySession.StateSchema&&current.CoreEventsJson.Contains("daily_event_v3")&&current.ExportReplay().SchemaVersion==DailySession.ReplaySchema,"v3 identity");
            }
        }));
        await Check("R04_ControllerPauseComposition",async()=>
        {
            var tracked=new TrackedFactory();var clock=new Clock();var platform=new Platform();var views=new Views();
            using(var controller=new SessionController(tracked,views,new TimeResolver(null,new Time()),clock,platform,factory.Content.ContentVersion,factory.ConfigurationDigest))
            {
                await controller.StartTodayAsync();clock.Value=12.345;var s=tracked.Current;Offer(s);Assert(controller.Pauses==PauseReasons.Revival&&!controller.CanAcceptInput,"immediate controller pause");
                double paused=controller.ActiveSeconds;clock.Value=1000;Assert(controller.ActiveSeconds==paused,"offer consumed active time");
                controller.Request(SessionAction.Pause);platform.Emit(PlatformLifecycle.Background);controller.SetRewardPaused(true);
                s.ResolveRevival(new ResolveRevivalCommand(s.RevivalOfferId,"controller-success",true,12345));controller.SetRewardPaused(false);Assert(controller.Pauses==(PauseReasons.User|PauseReasons.Background|PauseReasons.Revival),"reward cleared other pause");
                controller.SetRevivalPaused(false);Assert((controller.Pauses&PauseReasons.Revival)!=0,"released before completion");
                s.CompleteRevivalTransfer(s.RevivalOfferId,s.RevivalTransferToken,12345);controller.SetRevivalPaused(false);Assert(controller.Pauses==(PauseReasons.User|PauseReasons.Background)&&controller.Snapshot.Status==GameStatus.Paused,"completion cleared unrelated pause");
                controller.Request(SessionAction.Resume);Assert(!controller.CanAcceptInput,"background ignored");platform.Emit(PlatformLifecycle.Foreground);Assert(controller.CanAcceptInput,"all pauses cleared but not running");clock.Value+=1;Assert(Math.Abs(controller.ActiveSeconds-paused-1)<.0001,"clock resumed wrongly");
                long generation=controller.Generation;await controller.RetryAsync();Assert(controller.Generation!=generation&&controller.Pauses==PauseReasons.None&&!tracked.Current.RevivalUsed,"retry state");
                controller.Exit();Assert(controller.Snapshot==null&&controller.Pauses==PauseReasons.None,"exit state");
            }
        });
        await Check("R05_QuotaBoundariesMigrationAndCrossDay",()=>Sync(()=>
        {
            var records=new List<ShareQuotaRecord>();var claims=new List<string>();var ledger=new ShareQuotaLedger(records,claims);var now=DateTimeOffset.Parse("2026-09-21T21:59:00Z");string day=TimeResolver.ChallengeDay(now);
            Assert(day=="20260921"&&ledger.ReadShareAvailability(day,now).Available,"first share");Assert(ledger.TryReserveShare(day,"q1",now)&&!ledger.TryReserveShare(day,"concurrent",now),"reservation exclusive");
            now=now.AddMinutes(2);Assert(TimeResolver.ChallengeDay(now)=="20260922"&&ledger.TryCommitShare(day,"q1",now),"crossday old bucket commit");
            Assert(ledger.ReadShareAvailability("20260922",now).Available&&ledger.ReadShareAvailability(day,now).Used==1,"new day independent");
            Assert(!ledger.ReadShareAvailability(day,now.AddSeconds(299.999)).Available&&ledger.ReadShareAvailability(day,now.AddSeconds(300)).Available,"5 minute edge");
            var reload=new ShareQuotaLedger(records,claims);Assert(!reload.ReadShareAvailability(day,now.AddMinutes(1)).Available,"restart lost cooldown");
            now=now.AddMinutes(5);Assert(reload.TryReserveShare(day,"q2",now)&&reload.TryCommitShare(day,"q2",now),"second share");
            Assert(!reload.ReadShareAvailability(day,now.AddSeconds(899.999)).Available&&reload.ReadShareAvailability(day,now.AddSeconds(900)).Available,"15 minute edge");
            now=now.AddMinutes(15);Assert(reload.TryReserveShare(day,"q3",now)&&reload.TryCommitShare(day,"q3",now)&&!reload.ReadShareAvailability(day,now.AddDays(10)).Available,"third/cap");
            Assert(!reload.TryReserveShare("20260922","q1",now),"committed request reused on another day");
            for(int used=0;used<=3;used++)
            {
                var legacyRecords=new List<ShareQuotaRecord>{new ShareQuotaRecord{day=day,used=used}};var legacy=new ShareQuotaLedger(legacyRecords,new List<string>());
                Assert(legacy.ReadShareAvailability(day,now).Used==used&&legacy.ReadShareAvailability(day,now).Available==(used<3),"legacy migration "+used);
                if(used<3){Assert(legacy.TryReserveShare(day,"legacy",now)&&legacy.TryCommitShare(day,"legacy",now),"legacy next success");Assert(legacyRecords[0].dataVersion==2&&legacyRecords[0].lastEffectiveUtc!=null,"legacy timestamp");}
            }
        }));
        await Check("R06_RewardIdempotencyAndFailureOutcomes",async()=>
        {
            var now=DateTimeOffset.Parse("2026-09-22T00:00:00Z");var profile=new Profile();var service=new Rewards();var coordinator=new RewardCoordinator(service,profile,()=>now);long generation=1;int applied=0;bool paused=false;
            Func<RewardKind,RewardRequest> request=kind=>new RewardRequest(generation,kind,RewardRoute.SimulatedShare,"20260922","session","offer");
            Func<RewardRequest,Task<RewardApplicationResult>> send=r=>coordinator.RequestDetailedAsync(r,()=>generation,()=>true,()=>{applied++;return true;},p=>paused=p);
            Assert(await send(request((RewardKind)999))==RewardApplicationResult.Unavailable&&service.Calls==0,"unknown fell through");
            foreach(var outcome in new[]{RewardOutcome.Cancelled,RewardOutcome.Failed,RewardOutcome.Unavailable})
            {
                service.Outcome=outcome;var req=request(RewardKind.Revival);var actual=await send(req);
                Assert(actual==(outcome==RewardOutcome.Cancelled?RewardApplicationResult.Cancelled:outcome==RewardOutcome.Failed?RewardApplicationResult.Failed:RewardApplicationResult.Unavailable),"explicit outcome");
                Assert(await send(req)==RewardApplicationResult.Duplicate&&profile.SharesUsed("20260922")==0&&!paused,"failed request replayed/spent");
            }
            service.Outcome=RewardOutcome.Success;var once=request(RewardKind.Revival);Assert(await send(once)==RewardApplicationResult.Applied&&await send(once)==RewardApplicationResult.Duplicate&&applied==1&&profile.SharesUsed("20260922")==1&&!paused,"successful duplicate");
            now=now.AddMinutes(5);service.Pending=new TaskCompletionSource<RewardOutcome>();var old=request(RewardKind.Revival);var work=send(old);
            Assert(await send(old)==RewardApplicationResult.Duplicate&&paused,"inflight duplicate");
            generation++;coordinator.InvalidateSession();paused=false;var oldPending=service.Pending;service.Pending=new TaskCompletionSource<RewardOutcome>();var fresh=send(request(RewardKind.Revival));
            oldPending.SetResult(RewardOutcome.Failed);Assert(await work==RewardApplicationResult.Stale&&paused,"old result altered current pause");
            service.Pending.SetResult(RewardOutcome.Success);Assert(await fresh==RewardApplicationResult.Applied&&applied==2&&!paused,"fresh request not applied");
        });
        await Check("R07_FourRewardsShareOneQuotaAndActualEffect",async()=>
        {
            var now=DateTimeOffset.Parse("2026-09-22T00:00:00Z");var profile=new Profile();var service=new Rewards();var coordinator=new RewardCoordinator(service,profile,()=>now);
            foreach(var kind in new[]{RewardKind.Hint,RewardKind.ClearBuffer,RewardKind.Shuffle})
            {
                var r=new RewardRequest(1,kind,RewardRoute.SimulatedShare,"20260922");
                Assert(await coordinator.RequestDetailedAsync(r,()=>1,()=>true,()=>true,p=>{})==RewardApplicationResult.Applied,"cross-tool quota");
                now=now.AddMinutes(kind==RewardKind.Hint?5:15);
            }
            Assert(profile.SharesUsed("20260922")==3&&!coordinator.ReadShareAvailability("20260922").Available,"shared cap");
            Assert(await coordinator.RequestDetailedAsync(new RewardRequest(1,RewardKind.Revival,RewardRoute.SimulatedAd,"20260922"),()=>1,()=>true,()=>true,p=>{})==RewardApplicationResult.Applied&&profile.SharesUsed("20260922")==3,"ad spent quota");
            string fresh="20260923";var ineffective=new RewardRequest(1,RewardKind.Revival,RewardRoute.SimulatedShare,fresh);
            Assert(await coordinator.RequestDetailedAsync(ineffective,()=>1,()=>true,()=>false,p=>{})==RewardApplicationResult.Unavailable&&profile.SharesUsed(fresh)==0&&coordinator.ReadShareAvailability(fresh).Available,"no-effect spent quota/cooldown");
            var success=new RewardRequest(1,RewardKind.Revival,RewardRoute.SimulatedShare,fresh);bool paused=false,appliedWhilePaused=false;
            Assert(await coordinator.RequestDetailedAsync(success,()=>1,()=>true,()=>{appliedWhilePaused=paused;return true;},p=>paused=p)==RewardApplicationResult.Applied&&appliedWhilePaused&&!paused,"revival commit pause order");
        });
        await Check("R08_PresentationClockAndEventLifecycle",()=>Sync(()=>
        {
            var view=new ViewSnapshot{sessionId="s",sessionGeneration=1,phase=ViewPhase.Paused,revivalPending=true,revivalUsed=true,revivalTransfer=new RevivalTransferBatch(),pauseReasons=ViewPauseReasons.Revival};
            var clock=new VisualClock();clock.Reset("s",1);Assert(clock.Advance(.2,view,true)==.2&&clock.Advance(.2,view)==0,"clock lanes");
            foreach(var reason in new[]{ViewPauseReasons.User,ViewPauseReasons.Background,ViewPauseReasons.Reward}){view.pauseReasons|=reason;Assert(clock.Advance(5,view,true)==0,"external pause advances transfer");view.pauseReasons=ViewPauseReasons.Revival;}
            Assert(clock.Elapsed==.2&&clock.Advance(.6,view,true)==.6,"pause lost progress");view.sessionGeneration=2;Assert(clock.Advance(.2,view,true)==0,"old generation clock");clock.Cancel();view.sessionGeneration=1;Assert(clock.Advance(.2,view,true)==0,"cancelled clock revived");
            var cursor=new PresentationEventCursor();cursor.Reset("s",1);var e=new ViewEvent{sessionId="s",sessionGeneration=1,sequence=1};Assert(cursor.TryAccept(e)&&!cursor.TryAccept(e),"event duplicate");e.sequence=2;e.sessionGeneration=2;Assert(!cursor.TryAccept(e),"old event session");cursor.Cancel();e.sessionGeneration=1;Assert(!cursor.TryAccept(e),"cancelled cursor");
        }));
        int failed=results.Select(CanonicalJson.Map).Count(r=>(string)r["status"]=="FAIL");
        File.WriteAllText(output,CanonicalJson.Write(CanonicalJson.Object("task","TASK-001","taskVersion",9,"checkpoint","HC-02-v9-Code","scope","Core/Session/UTC ledger/real transaction Mapper and transport lifecycle checks; no rendered View or device claim","results",results,"failed",failed)));
        Console.WriteLine("REPORT "+output+"; failed="+failed);return failed==0?0:1;
    }
    sealed class Profile:IProfileStore,IShareQuotaStore
    {
        readonly List<ShareQuotaRecord> records=new List<ShareQuotaRecord>();readonly List<string> claims=new List<string>();readonly ShareQuotaLedger ledger;
        public Profile(){ledger=new ShareQuotaLedger(records,claims);}
        public bool IsDevelopmentSimulation=>true;public int TotalFirstWins=>0;public bool RecordFirstWin(string day)=>true;public int SharesUsed(string day)=>records.Find(r=>r.day==day)?.used??0;
        public bool TryConsumeShare(string day,string id)=>throw new Exception("legacy API unexpectedly used");public PlayerSettings LoadSettings()=>new PlayerSettings();public void SaveSettings(PlayerSettings settings){}
        public ShareAvailability ReadShareAvailability(string day,DateTimeOffset utc)=>ledger.ReadShareAvailability(day,utc);public bool TryReserveShare(string day,string id,DateTimeOffset utc)=>ledger.TryReserveShare(day,id,utc);
        public bool HasShareReservation(string day,string id)=>ledger.HasShareReservation(day,id);public bool TryCommitShare(string day,string id,DateTimeOffset utc)=>ledger.TryCommitShare(day,id,utc);public void ReleaseShare(string day,string id)=>ledger.ReleaseShare(day,id);
    }
    sealed class Rewards:IRewardService{public int Calls;public bool IsDevelopmentSimulation=>true;public RewardOutcome Outcome=RewardOutcome.Success;public TaskCompletionSource<RewardOutcome> Pending;public Task<RewardOutcome> RequestAsync(RewardRequest request){Calls++;return Pending?.Task??Task.FromResult(Outcome);}}
    sealed class TrackedFactory:IGameSessionFactory{public DailySession Current;public IGameSession CreateSession(ChallengeContext context)=>Current=factory.CreateFixtureSession(context,Fixture());}
    sealed class Clock:IMonotonicClock{public double Value;public double Seconds=>Value;}
    sealed class Time:ITimeProvider{public Task<DateTimeOffset> GetUtcAsync()=>Task.FromResult(DateTimeOffset.Parse("2026-09-22T00:00:00Z"));}
    sealed class Platform:IPlatformLifecycleAdapter{public event Action<PlatformLifecycle> Changed;public event Action<Viewport> ViewportChanged;public void Start(){ViewportChanged?.Invoke(new Viewport(720,1280,0,0,720,1280));}public void Emit(PlatformLifecycle value)=>Changed?.Invoke(value);public void Dispose(){}}
    sealed class Views:IGameViewFactory,IGameView{public IGameView CreateView()=>this;public void Bind(ISessionActions actions){}public void SetViewport(Viewport viewport){}public void Show(GameSnapshot snapshot,GameEventBatch events){}public void ShowLoading(){}public void ShowError(string code,string message){throw new Exception(message);}public void ResetSession(){}public void Dispose(){}}
}
