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
using HotpotSort.Bootstrap;

static class Task001RuleQa
{
    static readonly List<object> results=new List<object>();
    const string ContentRoot="Unity/Assets/HotpotSort/Content/Daily";
    static readonly DailySessionFactory factory=DailySessionFactory.FromProductionJson(File.ReadAllText(Path.Combine(ContentRoot,DailyContent.RuntimeFileName)));
    static readonly DailyContent content=factory.Content;
    static ChallengeContext Context(string day="20260921",int retry=0)=>new ChallengeContext(day,content.ContentVersion,factory.ConfigurationDigest,"qa",retry);
    static Dictionary<string,object> State(DailySession s)=>CanonicalJson.Map(CanonicalJson.Parse(s.Snapshot.CanonicalStateJson));
    static Dictionary<string,object>[] Items(DailySession s)=>CanonicalJson.Array(State(s)["items"]).Select(CanonicalJson.Map).ToArray();
    static Dictionary<string,object>[] Orders(DailySession s)=>CanonicalJson.Array(State(s)["orders"]).Select(CanonicalJson.Map).ToArray();
    static int Num(object o)=>CanonicalJson.Int(o);
    static void Assert(bool value,string message){if(!value)throw new Exception(message);}
    static void Conservation(DailySession s){Assert(s.Snapshot.Status!=GameStatus.Aborted,"core aborted: "+s.Snapshot.CanonicalStateJson);var items=Items(s);Assert(items.Length==183&&items.Select(i=>Num(i["itemId"])).Distinct().Count()==183,"inventory conservation");}
    static async Task Check(string id,string ac,string input,string expected,Func<Task> run)
    {
        string status="PASS",actual="All assertions passed";
        try{await run();}catch(Exception e){status="FAIL";actual=e.ToString();}
        results.Add(CanonicalJson.Object("case",id,"ac",ac,"input",input,"expected",expected,"actual",actual,"status",status));Console.WriteLine(id+" "+status+(status=="FAIL"?" "+actual:""));
    }
    static Task Sync(Action action){action();return Task.CompletedTask;}
    static DailySession Fixture(int completed,out int[] target,int targetSlot=0,int openSlots=2)
    {
        var all=content.Plates.SelectMany(p=>p.Kinds).Select((kind,index)=>new{kind,id=index+1}).ToArray();
        var triples=all.GroupBy(i=>i.kind).SelectMany(g=>g.Select((x,i)=>new{x,i}).GroupBy(x=>x.i/3).Select(t=>t.Select(x=>x.x.id).ToArray())).ToArray();
        target=triples[completed];string targetKind=all[target[0]-1].kind;
        var selected=target;var orders=Enumerable.Range(0,openSlots).Select(i=>new FixtureOrder(i==targetSlot?targetKind:null,i==targetSlot?selected.Take(2):new int[0])).ToArray();
        return factory.CreateFixtureSession(Context(),new DailyFixture(50,new int?[5],orders,triples.Take(completed).SelectMany(x=>x)));
    }
    static DailySession BufferFixture(int size)
    {
        var all=content.Plates.SelectMany(p=>p.Kinds).Select((kind,index)=>new{kind,id=index+1}).ToArray();
        string kind=all[0].kind;var ids=all.Where(i=>i.kind!=kind).Take(size).Select(i=>(int?)i.id).Concat(Enumerable.Repeat<int?>(null,5-size)).ToArray();
        return factory.CreateFixtureSession(Context(),new DailyFixture(50,ids,new[]{new FixtureOrder(kind,new int[0]),new FixtureOrder(null,new int[0])},new int[0]));
    }
    static async Task<int> Main(string[] args)
    {
        string output=args.Length>0?args[0]:Path.Combine(Path.GetTempPath(),"hotpot-task001-v5-rule-qa.json");
        await Check("C01-C02-P01","C01,C02,C07","project source; real production factory; 365 dates; independent 50-plate golden; weights/stages","byte identity; every item source tuple; fixed C; daily bijection and retry; unchanged weights",()=>Sync(()=>
        {
            Assert(CanonicalJson.Hash(File.ReadAllBytes(Path.Combine(ContentRoot,DailyContent.SkeletonSourceFileName)))=="2d0cc7dc3061937efd09832b8635558d6b861bbdcbbc793a92d70f25c554ef54","C01.source-hash");
            for(int d=0;d<365;d++)
            {
                string day=new DateTime(2026,1,1).AddDays(d).ToString("yyyyMMdd",CultureInfo.InvariantCulture);var plates=content.Plates;var kinds=plates.SelectMany(p=>p.Kinds).ToArray();
                Assert(plates.Count==50&&kinds.Length==183&&plates.All(p=>p.Kinds.Count>0),"shape "+day);Assert(kinds.Distinct().Count()==16&&kinds.GroupBy(x=>x).All(g=>g.Count()%3==0),"multiplicities "+day);
                using(var a=factory.CreateDailySession(Context(day)))using(var b=factory.CreateDailySession(Context(day,3)))
                {
                    FixedCGolden.Verify(content,a);Assert(a.StateHash==b.StateHash,"C02.retry "+day);Conservation(a);
                    var state=State(a);var mapping=CanonicalJson.Map(state["mapping"]);
                    Assert(mapping.Count==16&&mapping.Values.Distinct().Count()==16,"C02.bijection "+day);
                    var seed=Pcg32.DailySeed(day,content.ContentVersion);Assert((string)state["dailySeed"]==CanonicalJson.U64(seed),"C02.seed "+day);
                    var rng=new Pcg32(Pcg32.StreamSeed(seed,"MappingRng"));var mappedIds=Enumerable.Range(0,16).Select(i=>"food_"+i.ToString("00")).ToArray();
                    for(int i=15;i>0;i--){int j=(int)rng.NextBounded((uint)(i+1));var t=mappedIds[i];mappedIds[i]=mappedIds[j];mappedIds[j]=t;}
                    for(int i=0;i<16;i++)Assert((string)mapping[((char)('A'+i)).ToString()]==mappedIds[i],"C02.mapping "+day);
                    Assert(CanonicalJson.Write(state["mappingRng"])==CanonicalJson.Write(rng.Snapshot()),"C03.extra mapping draws");
                    Assert(CanonicalJson.Array(state["pendingPlateIds"]).Select(Num).SequenceEqual(Enumerable.Range(1,50)),"C02.initial queue");
                }
            }
            string expected="0,70,30,0,0;40,50,10,0,0;60,30,10,0,0;80,20,0,0,0;90,10,0,0,0;0,30,70,0,0;20,30,50,0,0;30,40,20,10,0;30,30,0,35,5;55,35,0,0,10;0,20,80,0,0;10,20,70,0,0;20,30,50,0,0;20,30,0,35,15;30,40,0,0,30;0,20,80,0,0;10,20,70,0,0;20,20,60,0,0;25,30,0,35,10;35,50,0,0,15";
            Assert(string.Join(";",content.Rows.Select(r=>string.Join(",",r.Weights)))==expected,"weight drift");int[] bands={0,1,1,2,2,3};int[] values={73,74,118,119,155,156};for(int i=0;i<6;i++)Assert(DailyDirector.ProgressBand(values[i])==bands[i],"progress boundary "+values[i]);
        }));
        await Check("C03","C03","consume presentation stream before and during identical supply/tap commands","mapping, director state, events and core hash remain identical",()=>Sync(()=>
        {
            using(var a=factory.CreateDailySession(Context()))using(var b=factory.CreateDailySession(Context()))
            {
                string presentation=b.PresentationRngJson;for(int i=0;i<200;i++)b.NextPresentation(17);
                Assert(b.PresentationRngJson!=presentation&&a.StateHash==b.StateHash,"C03.initial isolation");
                for(ulong i=1;i<=50;i++){a.Supply(new SupplyObservation(i,i,true,true));b.NextPresentation(7);b.Supply(new SupplyObservation(i,i,true,true));}
                ulong input=0;while(a.Snapshot.Status==GameStatus.Running&&input<183){int id=a.FindHintItem();Assert(id>0&&b.FindHintItem()==id,"C03.director target");input++;a.Tap(new TapCommand(id,input,input+50,true));b.NextPresentation(97);b.Tap(new TapCommand(id,input,input+50,true));Assert(a.StateHash==b.StateHash&&a.CoreEventsJson==b.CoreEventsJson,"C03.core isolation");}
                Assert(a.Snapshot.Status==GameStatus.Won,"C03.full trajectory");
            }
        }));
        await Check("P02","F-02","fresh daily session","two open, two locked, empty buffer",()=>Sync(()=>{using(var s=factory.CreateDailySession(Context())){var o=Orders(s);Assert((string)o[0]["state"]=="Active"&&(string)o[1]["state"]=="Active"&&(string)o[2]["state"]=="Locked"&&(string)o[3]["state"]=="Locked","initial pots");Assert(CanonicalJson.Array(State(s)["buffer"]).All(x=>x==null),"buffer not empty");var diag=CanonicalJson.Map(CanonicalJson.Parse(s.InspectDirector(0)));Assert(CanonicalJson.Array(diag["reservations"]).Count==1,"closed pot reserves");}}));
        await Check("P03","F-02","slots 2/3 targets; full buffer matching and mismatch; duplicate/rejected tap","correct route, conservation, no double consume",()=>Sync(()=>
        {
            for(int slot=2;slot<=3;slot++){int[] target;using(var s=Fixture(0,out target,slot,4)){var r=s.Tap(new TapCommand(target[2],1,1,true));Assert(r.Accepted&&s.CoreEventsJson.Contains("OrderCompleted"),"fourth/third routing");Conservation(s);string h=s.StateHash;Assert(!s.Tap(new TapCommand(target[2],1,2,true)).Accepted&&s.StateHash==h,"repeat tap");Assert(!s.Tap(new TapCommand(target[2],2,2,false)).Accepted&&s.StateHash==h,"hit rejection");}}
            using(var s=BufferFixture(5)){string kind=(string)Orders(s)[0]["kind"];int matching=Items(s).Where(i=>(string)i["location"]=="ActiveAvailable"&&(string)i["kind"]==kind).Select(i=>Num(i["itemId"])).First();Assert(s.Tap(new TapCommand(matching,1,1,true)).Accepted&&s.Snapshot.Status==GameStatus.Running,"full buffer matching");int bad=Items(s).Where(i=>(string)i["location"]=="ActiveAvailable"&&(string)i["kind"]!=kind).Select(i=>Num(i["itemId"])).First();s.Tap(new TapCommand(bad,2,2,true));Assert(s.Snapshot.Status==GameStatus.Failed,"full buffer mismatch");Assert((string)Items(s).Single(i=>Num(i["itemId"])==bad)["location"]=="ActiveAvailable","overflow consumed item");Conservation(s);}
        }));
        await Check("P04","F-02","30->31,48->49; fourth unlocked early","single unlock, completion event retains 3/3",()=>Sync(()=>
        {
            foreach(int completed in new[]{30,48}){int[] target;using(var s=Fixture(completed,out target)){var before=Orders(s);int slot=completed==30?2:3;Assert((string)before[slot]["state"]=="Locked","premature unlock");var result=s.Tap(new TapCommand(target[2],1,1,true));Assert((string)Orders(s)[slot]["state"]!="Locked","missing unlock");var events=result.Events.CanonicalEvents.Select(x=>CanonicalJson.Map(CanonicalJson.Parse(x))).ToArray();Assert(events.Any(e=>(string)e["type"]=="OrderCompleted"&&Num(CanonicalJson.Map(e["data"])["filledAfter"])==3),"3/3 absent");Assert(events.Count(e=>(string)e["type"]=="PotUnlocked"&&Num(CanonicalJson.Map(e["data"])["slotId"])==slot)==1,"unlock count");Conservation(s);}}
            int[] early;using(var s=Fixture(48,out early)){Assert(s.UnlockFourth(1).Accepted,"early unlock");s.Tap(new TapCommand(early[2],1,2,true));Assert(!s.UnlockFourth(3).Accepted,"repeat fourth");Conservation(s);}
        }));
        await Check("P05","F-01,F-02","1/5 buffer items returned twice, plates 51/52","same IDs/kinds, new tail IDs, mapper valid",()=>Sync(()=>
        {
            foreach(int count in new[]{1,5})using(var s=BufferFixture(count))
            {
                var ids=CanonicalJson.Array(State(s)["buffer"]).Where(x=>x!=null).Select(Num).ToArray();var kinds=Items(s).ToDictionary(i=>Num(i["itemId"]),i=>(string)i["kind"]);
                Assert(s.ClearBuffer(1).Accepted&&s.PlateSize(51)==count,"clear/newplate");Assert(!s.ClearBuffer(2).Accepted,"empty clear");Assert(Num(CanonicalJson.Array(State(s)["pendingPlateIds"]).Last())==51,"tail");
                Assert(s.Supply(new SupplyObservation(1,2,true,true)).Accepted,"plate51 supply");DailyViewMapper.Map(s.Snapshot,null,content,0);
                ulong input=0;foreach(int id in ids)Assert(s.Tap(new TapCommand(id,++input,3+input,true)).Accepted,"returned tap");Assert(s.ClearBuffer(20).Accepted&&s.PlateSize(52)==count,"second clear");s.Supply(new SupplyObservation(2,21,true,true));DailyViewMapper.Map(s.Snapshot,null,content,0);
                foreach(int id in ids)Assert((string)Items(s).Single(i=>Num(i["itemId"])==id)["kind"]==kinds[id],"kind changed");Conservation(s);Assert(DailyReplay.Run(factory,s.ExportReplay()).Success,"clear replay");
            }
        }));
        await Check("P03b-P04b-P05b","F-01,F-02","duplicate-order reservations, last two orders with buffer chain, pending tail preservation","no over-reservation; two complete events/one win; queue prefix unchanged",()=>Sync(()=>
        {
            var all=content.Plates.SelectMany(p=>p.Kinds).Select((kind,index)=>new{kind,id=index+1}).ToArray();
            var abundant=all.GroupBy(i=>i.kind).OrderByDescending(g=>g.Count()).First().ToArray();
            using(var s=factory.CreateFixtureSession(Context(),new DailyFixture(50,new int?[5],new[]{new FixtureOrder(abundant[0].kind,new[]{abundant[0].id}),new FixtureOrder(null,new int[0]),new FixtureOrder(abundant[0].kind,new[]{abundant[1].id}),new FixtureOrder(null,new int[0])},new int[0])))
            {var diag=CanonicalJson.Map(CanonicalJson.Parse(s.InspectDirector(3)));var reservations=CanonicalJson.Array(diag["reservations"]).Select(CanonicalJson.Map).SelectMany(r=>CanonicalJson.Array(r["itemIds"]).Select(Num)).ToArray();Assert(reservations.Length==4&&reservations.Distinct().Count()==4,"duplicate over-reservation");Conservation(s);}
            var groups=all.GroupBy(i=>i.kind).SelectMany(g=>g.Select((x,i)=>new{x,i}).GroupBy(x=>x.i/3).Select(t=>t.Select(x=>x.x.id).ToArray())).ToList();var first=groups[0];var last=groups.Last();var done=groups.Skip(1).Take(59).SelectMany(x=>x).ToArray();
            using(var s=factory.CreateFixtureSession(Context(),new DailyFixture(50,last.Select(i=>(int?)i).Concat(new int?[2]),new[]{new FixtureOrder(all[first[0]-1].kind,first.Take(2)),new FixtureOrder(null,new int[0]),new FixtureOrder(null,new int[0]),new FixtureOrder(null,new int[0])},done)))
            {var result=s.Tap(new TapCommand(first[2],1,1,true));var events=result.Events.CanonicalEvents.Select(x=>CanonicalJson.Map(CanonicalJson.Parse(x))).ToArray();Assert(s.Snapshot.Status==GameStatus.Won,"buffer tail not won");Assert(events.Count(e=>(string)e["type"]=="OrderCompleted")==2&&events.Count(e=>(string)e["type"]=="ChallengeWon")==1,"chain event count");Assert(events.Count(e=>(string)e["type"]=="BufferAutoAbsorbed")==3,"missing backfill");Conservation(s);}
            int bufferId=all.Take(20).First(i=>i.kind!=all[0].kind).id;
            using(var s=factory.CreateFixtureSession(Context(),new DailyFixture(10,new int?[]{bufferId,null,null,null,null},new[]{new FixtureOrder(all[0].kind,new int[0]),new FixtureOrder(null,new int[0])},new int[0])))
            {string prefix=CanonicalJson.Write(State(s)["pendingPlateIds"]);s.ClearBuffer(1);var pending=CanonicalJson.Array(State(s)["pendingPlateIds"]);Assert(CanonicalJson.Write(pending.Take(pending.Count-1).ToArray())==prefix&&Num(pending.Last())==51,"C06.tail prefix");
                for(int id=11;id<=51;id++){Assert(DailyViewMapper.PendingHead(s.Snapshot)==id,"C06.original remaining plates first");Assert(s.Supply(new SupplyObservation((ulong)id,(ulong)id,true,true)).Accepted,"C06.tail supply");}
                var returned=Items(s).Single(i=>Num(i["itemId"])==bufferId);Assert(Num(returned["plateId"])==51&&(string)returned["kind"]==all[bufferId-1].kind,"C06.returned identity");Conservation(s);}
        }));
        await Check("C04-P06","C04","space/cooldown/duplicate/paused/empty observations; 50 commits","rejects preserve hash; recovery commits exactly current head 1..50",()=>Sync(()=>
        {
            using(var s=factory.CreateDailySession(Context()))
            {
                Action<SupplyObservation,string> reject=(observation,id)=>{string hash=s.StateHash;Assert(!s.Supply(observation).Accepted&&s.StateHash==hash,id);};
                reject(new SupplyObservation(1,1,false,true),"C04.space");reject(new SupplyObservation(1,1,true,false),"C04.cooldown");
                for(ulong i=1;i<=50;i++)
                {
                    Assert(DailyViewMapper.PendingHead(s.Snapshot)==(int)i,"C04.head order");int pending=CanonicalJson.Array(State(s)["pendingPlateIds"]).Count;
                    var result=s.Supply(new SupplyObservation(i,i*4,true,true));Assert(result.Accepted,"C04.recovery");
                    Assert(CanonicalJson.Array(State(s)["pendingPlateIds"]).Count==pending-1,"C04.single commit");
                    var active=Items(s).Where(x=>Num(x["plateId"])==(int)i).ToArray();Assert(active.All(x=>(string)x["location"]=="ActiveAvailable")&&active.Length==content.Plates[(int)i-1].Kinds.Count,"C04.inventory commit");
                    reject(new SupplyObservation(i,i*4+1,true,true),"C04.duplicate");s.Pause(i*4+1);reject(new SupplyObservation(i+1,i*4+2,true,true),"C04.pause");s.Resume(i*4+3);
                }
                reject(new SupplyObservation(51,204,true,true),"C04.empty");Conservation(s);
            }
        }));
        await Check("P07","F-04","599999/600000; tap/clear/unlock at deadline","one timeout, no late reward",()=>Sync(()=>
        {
            using(var s=factory.CreateDailySession(Context())){Assert(!s.Timeout(599999).Accepted,"early timeout");Assert(s.Timeout(600000).Accepted&&s.Snapshot.Status==GameStatus.Failed,"deadline");Assert(!s.Timeout(600001).Accepted,"repeat timeout");}
            using(var s=BufferFixture(1)){Assert(s.ClearBuffer(600000).Reason=="Timeout"&&!CanonicalJson.Array(State(s)["buffer"]).All(x=>x==null),"late clear awarded");}
            using(var s=factory.CreateDailySession(Context())){Assert(s.UnlockFourth(600000).Reason=="Timeout"&&(string)Orders(s)[3]["state"]=="Locked","late unlock");}
            using(var s=BufferFixture(1)){int id=Items(s).Where(i=>(string)i["location"]=="ActiveAvailable").Select(i=>Num(i["itemId"])).First();s.Tap(new TapCommand(id,1,600000,true));Assert((string)Items(s).Single(i=>Num(i["itemId"])==id)["location"]=="ActiveAvailable","late tap consumed");}
        }));
        await Check("P08","F-04","real controller, controlled wall/monotonic/lifecycle clocks","06:00/retry and additive pause, stale resolve invalidated",async()=>
        {
            Assert(TimeResolver.ChallengeDay(new DateTimeOffset(2026,9,21,5,59,59,TimeSpan.FromHours(8)))=="20260920","before6");Assert(TimeResolver.ChallengeDay(new DateTimeOffset(2026,9,21,6,0,0,TimeSpan.FromHours(8)))=="20260921","after6");
            var time=new FakeTime();var clock=new Clock();var platform=new Platform();using(var controller=new SessionController(factory,new Views(),new TimeResolver(null,time),clock,platform,content.ContentVersion,factory.ConfigurationDigest))
            {
                await controller.StartTodayAsync();string day=controller.Snapshot.Challenge.ChallengeId;clock.Value=20;controller.Request(SessionAction.Pause);controller.SetRewardPaused(true);platform.Emit(PlatformLifecycle.Background);clock.Value=200;Assert(controller.ActiveSeconds==20,"paused clock");controller.Request(SessionAction.Resume);controller.SetRewardPaused(false);Assert(controller.Snapshot.Status==GameStatus.Paused,"background lost");platform.Emit(PlatformLifecycle.Foreground);clock.Value=205;Assert(controller.ActiveSeconds==25,"resume clock");
                time.Utc=time.Utc.AddHours(1);Assert(controller.Snapshot.Challenge.ChallengeId==day,"crossday in-progress changed");await controller.RetryAsync();Assert(controller.Snapshot.Challenge.ChallengeId=="20260921","retry stale day");string hash=controller.Snapshot.CanonicalStateJson;await controller.RetryAsync();Assert(controller.Snapshot.CanonicalStateJson==hash,"same day retry state");
            }
            var delayed=new FakeTime{Pending=new TaskCompletionSource<DateTimeOffset>()};using(var c=new SessionController(factory,new Views(),new TimeResolver(null,delayed),new Clock(),new Platform(),content.ContentVersion,factory.ConfigurationDigest)){var start=c.StartTodayAsync();c.Exit();delayed.Pending.SetResult(DateTimeOffset.UtcNow);await start;Assert(c.Snapshot==null,"late resolve resurrected");}
            var disposedTime=new FakeTime{Pending=new TaskCompletionSource<DateTimeOffset>()};var disposed=new SessionController(factory,new Views(),new TimeResolver(null,disposedTime),new Clock(),new Platform(),content.ContentVersion,factory.ConfigurationDigest);var pendingStart=disposed.StartTodayAsync();disposed.Dispose();disposedTime.Pending.SetResult(DateTimeOffset.UtcNow);await pendingStart;Assert(disposed.Snapshot==null,"disposed resolve resurrected");
        });
        await Check("P09","F-03,F-04","controlled rewards, quota, no-target, cancellation/failure/concurrency/old generation","award and quota exactly once only on successful effect",async()=>
        {
            var service=new Rewards();var profile=new Profile();var coordinator=new RewardCoordinator(service,profile);long generation=1;int applied=0;bool paused=false;
            Func<RewardKind,RewardRoute,string,RewardRequest> request=(kind,route,day)=>new RewardRequest(generation,kind,route,day);
            Func<RewardRequest,Func<bool>,Task<bool>> send=(r,apply)=>coordinator.RequestAsync(r,()=>generation,()=>true,apply,p=>paused=p);
            for(int i=0;i<3;i++)Assert(await send(request((RewardKind)i,RewardRoute.SimulatedShare,"20260920"),()=>{applied++;return true;}),"share allowance");
            Assert(profile.SharesUsed("20260920")==3&&!await send(request(RewardKind.Hint,RewardRoute.SimulatedShare,"20260920"),()=>true),"quota overflow");
            Assert(!await send(request(RewardKind.FourthPot,RewardRoute.SimulatedShare,"20260921"),()=>true),"fourth share");Assert(await send(request(RewardKind.FourthPot,RewardRoute.SimulatedAd,"20260921"),()=>true)&&profile.SharesUsed("20260921")==0,"ad quota");
            var once=request(RewardKind.Hint,RewardRoute.SimulatedShare,"20260921");Assert(await send(once,()=>true)&&!await send(once,()=>true)&&profile.SharesUsed("20260921")==1,"duplicate request");
            foreach(var outcome in new[]{RewardOutcome.Cancelled,RewardOutcome.Failed}){service.Outcome=outcome;Assert(!await send(request(RewardKind.Hint,RewardRoute.SimulatedShare,"20260921"),()=>throw new Exception("unexpected apply")),"failed outcome");}service.Outcome=RewardOutcome.Success;
            Assert(!await send(request(RewardKind.Hint,RewardRoute.SimulatedShare,"20260921"),()=>false)&&profile.SharesUsed("20260921")==1,"ineffective apply quota");
            int calls=service.Calls;Assert(!await coordinator.RequestAsync(request(RewardKind.Hint,RewardRoute.SimulatedAd,"x"),()=>generation,()=>false,()=>true,p=>paused=p)&&service.Calls==calls,"no-target called service");
            service.Pending=new TaskCompletionSource<RewardOutcome>();var old=request(RewardKind.Hint,RewardRoute.SimulatedShare,"20260919");var work=send(old,()=>{applied++;return true;});Assert(paused,"reward pause absent");Assert(!await send(request(RewardKind.Hint,RewardRoute.SimulatedAd,"20260919"),()=>true),"concurrent accepted");generation++;paused=false;service.Pending.SetResult(RewardOutcome.Success);Assert(!await work&&!paused&&profile.SharesUsed("20260919")==0,"old-generation granted");service.Pending=null;
            var crossing=request(RewardKind.Hint,RewardRoute.SimulatedShare,"20260918");Assert(await send(crossing,()=>true)&&profile.SharesUsed("20260918")==1,"quota request-day attribution");Assert(applied==3,"extra reward applied");
            service.Pending=new TaskCompletionSource<RewardOutcome>();bool valid=true;var disappearing=coordinator.RequestAsync(request(RewardKind.Hint,RewardRoute.SimulatedShare,"20260917"),()=>generation,()=>valid,()=>throw new Exception("target invalid applied"),p=>paused=p);valid=false;service.Pending.SetResult(RewardOutcome.Success);Assert(!await disappearing&&!paused&&profile.SharesUsed("20260917")==0,"invalid target charged");service.Pending=null;
            service.Throw=true;bool threw=false;try{await send(request(RewardKind.Hint,RewardRoute.SimulatedShare,"20260917"),()=>true);}catch(InvalidOperationException){threw=true;}Assert(threw&&!paused&&profile.SharesUsed("20260917")==0,"service exception cleanup");
        });
        await Check("P10","F-01,F-03","v2 accepted commands and tampered replay","matching state/events hashes, corruption rejected",()=>Sync(()=>
        {
            using(var s=factory.CreateDailySession(Context())){s.Supply(new SupplyObservation(1,1,true,true));s.Tap(new TapCommand(1,1,2,true));s.Pause(3);s.Resume(4);s.UnlockFourth(5);s.Timeout(600000);var replay=s.ExportReplay();var result=DailyReplay.Run(factory,replay);Assert(result.Success,result.Error);string altered=replay.CanonicalJson.Replace(content.ContentVersion,"hotpot_daily_task001_v3_1");Assert(altered!=replay.CanonicalJson,"C08.tamper must alter replay");var tampered=new ReplayPackage(replay.SessionId,replay.SchemaVersion,altered);Assert(!DailyReplay.Run(factory,tampered).Success,"tampered accepted");}
            string baseline=Path.Combine(Path.GetDirectoryName(output),"baseline-rules.replay.json");Assert(File.Exists(baseline),"C08.old candidate fixture absent");Assert(!DailyReplay.Run(factory,new ReplayPackage("baseline",DailySession.ReplaySchema,File.ReadAllText(baseline))).Success,"C08.old candidate replay accepted");
        }));
        await Check("P11","F-01,F-02","all plates supplied for rule isolation; tap matching orders until 61","complete inventory and only one win; no solvability claim",()=>Sync(()=>
        {
            using(var s=factory.CreateDailySession(Context()))
            {
                for(ulong i=1;i<=50;i++)s.Supply(new SupplyObservation(i,i,true,true));ulong input=0;
                while(s.Snapshot.Status==GameStatus.Running&&input<183){int id=s.FindHintItem();Assert(id>0,"no matched full-inventory item");s.Tap(new TapCommand(id,++input,input+50,true));Conservation(s);}
                Assert(s.Snapshot.Status==GameStatus.Won&&input==183,"not won after183");Assert(Items(s).All(i=>(string)i["location"]=="Completed"),"remaining inventory");Assert(Num(CanonicalJson.Map(State(s)["statistics"])["completedOrderCount"])==61,"orders !=61");Assert(!s.Timeout(600000).Accepted,"won timed out");var replay=s.ExportReplay();var result=DailyReplay.Run(factory,replay);Assert(result.Success,result.Error);File.WriteAllText(Path.ChangeExtension(output,"replay.json"),replay.CanonicalJson);
            }
        }));
        await Check("C08-import","C08","new bytes, altered bytes, previous source identity, canonical runtime and changed runtime","import equals production; corrupted/old identities rejected",()=>Sync(()=>
        {
            var source=File.ReadAllBytes(Path.Combine(ContentRoot,DailyContent.SkeletonSourceFileName));var weights=File.ReadAllBytes(Path.Combine(ContentRoot,"LevelDifficultyConfig.source.json"));
            var imported=HotpotSort.ContentImport.DailyContentImporter.Import(source,weights);Assert(imported.Digest==content.Digest,"C08.import binding");
            Action<Action,string> rejects=(action,id)=>{bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}Assert(rejected,id);};
            source[0]^=1;rejects(()=>HotpotSort.ContentImport.DailyContentImporter.Import(source,weights),"C08.source corrupt");
            rejects(()=>HotpotSort.ContentImport.DailyContentImporter.Import(File.ReadAllBytes(Path.Combine(ContentRoot,"skeleton_C.source.json")),weights),"C08.old source");
            rejects(()=>DailyContent.LoadProduction(File.ReadAllText(Path.Combine(ContentRoot,"daily_core_1.0.0.json"))),"C08.old runtime");
            rejects(()=>new DailyContent(content.ContentVersion,content.Plates,content.Rows,"hotpot_original_inventory_v1"),"C08.old generator");
            rejects(()=>DailyContent.LoadProduction(content.CanonicalJsonText.Replace("skeleton_c_normalized_v1.1","skeleton_c_v1")),"C08.runtime corruption");
        }));
        string report=CanonicalJson.Write(CanonicalJson.Object("task","TASK-001","taskVersion",6,"checkpoint","HC-02-v6","scope","Existing 14 rule groups; C04 validates core CanSpawn only, not the physical height gate","contentVersion",content.ContentVersion,"contentDigest",content.Digest,"configurationDigest",factory.ConfigurationDigest,"utc",DateTimeOffset.UtcNow.ToString("o"),"results",results));File.WriteAllText(output,report);
        int failed=results.Select(CanonicalJson.Map).Count(r=>(string)r["status"]=="FAIL");Console.WriteLine("REPORT "+output+"; failed="+failed);return failed==0?0:1;
    }
    sealed class FakeTime:ITimeProvider{public DateTimeOffset Utc=new DateTimeOffset(2026,9,21,5,59,59,TimeSpan.FromHours(8));public TaskCompletionSource<DateTimeOffset> Pending;public Task<DateTimeOffset> GetUtcAsync()=>Pending?.Task??Task.FromResult(Utc);}
    sealed class Clock:IMonotonicClock{public double Value;public double Seconds=>Value;}
    sealed class Platform:IPlatformLifecycleAdapter{public event Action<PlatformLifecycle> Changed;public event Action<Viewport> ViewportChanged;public void Start(){ViewportChanged?.Invoke(new Viewport(720,1280,0,0,720,1280));}public void Emit(PlatformLifecycle state)=>Changed?.Invoke(state);public void Dispose(){}}
    sealed class Views:IGameViewFactory,IGameView{public IGameView CreateView()=>this;public void Bind(ISessionActions a){}public void SetViewport(Viewport v){}public void Show(GameSnapshot s,GameEventBatch e){}public void ShowLoading(){}public void ShowError(string a,string b){}public void ResetSession(){}public void Dispose(){}}
    sealed class Rewards:IRewardService{public bool IsDevelopmentSimulation=>true;public int Calls;public bool Throw;public RewardOutcome Outcome=RewardOutcome.Success;public TaskCompletionSource<RewardOutcome> Pending;public Task<RewardOutcome> RequestAsync(RewardRequest r){Calls++;if(Throw)throw new InvalidOperationException("simulated service failure");return Pending?.Task??Task.FromResult(Outcome);}}
    sealed class Profile:IProfileStore
    {
        readonly Dictionary<string,int> quota=new Dictionary<string,int>();readonly HashSet<string> claims=new HashSet<string>();public bool IsDevelopmentSimulation=>true;public int TotalFirstWins=>0;public bool RecordFirstWin(string d)=>true;public int SharesUsed(string d)=>quota.ContainsKey(d)?quota[d]:0;
        public bool TryConsumeShare(string d,string id){if(claims.Contains(id)||SharesUsed(d)>=3)return false;claims.Add(id);quota[d]=SharesUsed(d)+1;return true;}public PlayerSettings LoadSettings()=>new PlayerSettings();public void SaveSettings(PlayerSettings s){}
    }
}
