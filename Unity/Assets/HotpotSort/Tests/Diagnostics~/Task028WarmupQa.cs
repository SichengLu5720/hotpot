using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Bootstrap;
using HotpotSort.Determinism;
using HotpotSort.Presentation;
using HotpotSort.Profile;
using HotpotSort.Replay;
using HotpotSort.Session;

static class Task028WarmupQa
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static Dictionary<string,object> State(DailySession s)=>CanonicalJson.Map(CanonicalJson.Parse(s.Snapshot.CanonicalStateJson));
    static Dictionary<string,object>[] Items(DailySession s)=>CanonicalJson.Array(State(s)["items"]).Select(CanonicalJson.Map).ToArray();
    static ChallengeContext Context(DailySessionFactory f,string day="20260925",int retry=0)=>new ChallengeContext(day,f.Content.ContentVersion,f.ConfigurationDigest,"task028",retry);
    static void Solve(DailySession s,Action<DailySession> after=null)
    {
        ulong supply=0,input=0;
        while(CanonicalJson.Array(State(s)["pendingPlateIds"]).Count>0)Check(s.Supply(new SupplyObservation(++supply,0,true,true)).Accepted,"supply");
        for(int step=0;step<205&&s.Snapshot.Status==GameStatus.Running;step++)
        {
            int id=s.FindHintItem();Check(id>0,"missing solvable order");
            Check(s.Tap(new TapCommand(id,++input,0,true)).Accepted,"tap");after?.Invoke(s);
        }
        Check(s.Snapshot.Status==GameStatus.Won,"solve status "+s.Snapshot.Status);
    }
    static void Replay(DailySessionFactory f,DailySession s)
    {var result=DailyReplay.Run(f,s.ExportReplay());Check(result.Success,"replay "+result.Error);result.Session.Dispose();}
    static async Task Main()
    {
        var factory=DailySessionFactory.FromProductionJson(File.ReadAllText("Unity/Assets/HotpotSort/Content/Daily/"+DailyContent.RuntimeFileName));
        var counts=new HashSet<int>();
        for(int i=0;i<32;i++)
        {
            string day=new DateTime(2026,9,1).AddDays(i).ToString("yyyyMMdd");
            var plates=WarmupContent.Generate(day,factory.Content.ContentVersion);counts.Add(plates.Count);
            Check(plates.Count>=4&&plates.Count<=8&&plates.All(p=>p.Kinds.Count>=1&&p.Kinds.Count<=5),"plate bounds");
            Check(plates.SelectMany(p=>p.Kinds).GroupBy(k=>k).Count()==3&&plates.SelectMany(p=>p.Kinds).GroupBy(k=>k).All(g=>g.Count()==6),"3x6 inventory");
            using var warm=factory.CreateStage(Context(factory,day),ChallengeStage.Warmup);
            using var retry=factory.CreateStage(Context(factory,day,9),ChallengeStage.Warmup);
            Check(warm.InitialHash==retry.InitialHash,"same-day retry differs");
            warm.Supply(new SupplyObservation(1,0,true,true));Check(warm.FindHintItem()>0,"first plate lacks tutorial target");
            // Fresh solver preserves strictly increasing observation sequence.
            using var solve=factory.CreateStage(Context(factory,day),ChallengeStage.Warmup,i%4);
            Solve(solve,s=>{
                var kinds=CanonicalJson.Array(State(s)["orders"]).Select(CanonicalJson.Map).Where(o=>o["kind"]!=null).Select(o=>(string)o["kind"]).ToArray();
                Check(kinds.Distinct().Count()==kinds.Length,"duplicate warmup orders");
            });
            var mapped=DailyViewMapper.Map(solve.Snapshot,null,factory.Content,0).snapshot;
            Check(mapped.warmupComplete&&mapped.phase==ViewPhase.Running&&mapped.completedOrders==6&&mapped.totalOrders==67,"intermediate settlement leak");
            Replay(factory,solve);
        }
        Check(counts.Count==5,"plate counts do not vary across full range");
        using(var untimed=factory.CreateStage(Context(factory),ChallengeStage.Warmup))
        {
            Check(untimed.Supply(new SupplyObservation(1,900000,true,true)).Accepted,"warmup supply timed out");
            Check(untimed.Tap(new TapCommand(untimed.FindHintItem(),1,900001,true)).Accepted,"warmup tap timed out");
            Check(!untimed.Timeout(900002).Accepted&&untimed.Snapshot.Status==GameStatus.Running,"warmup explicit timeout");Replay(factory,untimed);
        }
        using(var old=factory.CreateDailySession(Context(factory)))
        using(var formal=factory.CreateStage(Context(factory),ChallengeStage.Formal))
        {
            var oldState=State(old);var newState=State(formal);newState.Remove("challengeStage");newState.Remove("inheritedPotMask");
            Check(CanonicalJson.Write(oldState)==CanonicalJson.Write(newState),"formal initial inventory/random stream changed");
            Solve(formal,s=>{
                int n=s.CumulativeCompletedOrders;
                Check(((s.UnlockedExtraPotMask&1)!=0)==(n>=37),"third threshold");
                Check(((s.UnlockedExtraPotMask&2)!=0)==(n>=55),"fourth threshold");
            });
            var mapped=DailyViewMapper.Map(formal.Snapshot,null,factory.Content,0).snapshot;
            Check(mapped.completedOrders==67&&mapped.totalOrders==67&&mapped.phase==ViewPhase.Won,"formal final count");Replay(factory,formal);
            Solve(old);Replay(factory,old);
        }
        await ControllerChecks(factory);
        ProfileChecks();
        Console.WriteLine("TASK028_WARMUP_QA_PASS 32 dates, plate bounds, 3x6 inventory, same-day retry, strict kinds, first target, replay, formal identity, 37/55 unlock, stage clocks/retry/old callbacks, local/cloud merge");
    }
    sealed class Clock:IMonotonicClock,ITimeProvider
    {public double Seconds{get;set;} public Task<DateTimeOffset> GetUtcAsync()=>Task.FromResult(new DateTimeOffset(2026,9,25,3,0,0,TimeSpan.Zero));}
    sealed class Platform:IPlatformLifecycleAdapter
    {public event Action<PlatformLifecycle> Changed;public event Action<Viewport> ViewportChanged;public void Start(){}public void Dispose(){}public void Background(bool v)=>Changed?.Invoke(v?PlatformLifecycle.Background:PlatformLifecycle.Foreground);}
    sealed class Host:IGameSessionFactory,IChallengeStageFactory,IGameViewFactory,IGameView
    {
        readonly DailySessionFactory f;public DailySession Current;public Host(DailySessionFactory f){this.f=f;}
        public IGameSession CreateSession(ChallengeContext c)=>CreateStage(c,ChallengeStage.Warmup,0);
        public IGameSession CreateStage(ChallengeContext c,ChallengeStage s,int mask)=>Current=f.CreateStage(c,s,mask);
        public IGameView CreateView()=>this;public void Bind(ISessionActions a){}public void SetViewport(Viewport v){}public void Show(GameSnapshot s,GameEventBatch e){}public void ShowLoading(){}public void ShowError(string c,string m){throw new Exception(m);}public void ResetSession(){}public void Dispose(){}
    }
    static async Task ControllerChecks(DailySessionFactory f)
    {
        var host=new Host(f);var clock=new Clock();var platform=new Platform();
        using var c=new SessionController(host,host,new TimeResolver(null,clock),clock,platform,f.Content.ContentVersion,f.ConfigurationDigest);
        await c.StartTodayAsync();Check(c.Stage==ChallengeStage.Warmup,"start not warmup");
        Check(!c.StartChallengeTimer(),"warmup timer started");clock.Seconds=900;Check(c.ChallengeSeconds==0,"warmup time leaked");
        host.Current.UnlockThird(0);await c.RetryAsync();Check(c.Stage==ChallengeStage.Warmup&&host.Current.UnlockedExtraPotMask==1,"warmup retry pot inheritance");
        var id=c.Snapshot.SessionId;long generation=c.Generation;
        Check(!c.ContinueToFormal(id,generation),"premature transition");Solve(host.Current);
        Check(c.Stage==ChallengeStage.Warmup&&!c.CanAcceptInput,"transition must wait for visual ack");
        Check(!c.ContinueToFormal(id,generation-1),"stale transition accepted");
        platform.Background(true);Check(!c.ContinueToFormal(id,generation),"background transition");platform.Background(false);
        Check(c.ContinueToFormal(id,generation),"formal transition failed");
        Check(c.Stage==ChallengeStage.Formal&&host.Current.UnlockedExtraPotMask==1&&c.ChallengeSeconds==0&&!c.ChallengeTimerStarted,"formal inherited wrong state");
        Check(!c.ContinueToFormal(id,generation),"duplicate transition");
        clock.Seconds+=5;Check(c.ChallengeSeconds==0,"formal countdown started without tap");Check(c.StartChallengeTimer(),"formal timer missing");clock.Seconds+=2;Check(c.ChallengeSeconds==2,"formal timer");
        c.SetTutorialPaused(true);clock.Seconds+=5;Check(c.ChallengeSeconds==2&&!c.CanAcceptInput,"tutorial pause");c.SetTutorialPaused(false);
        await c.RetryAsync();Check(c.Stage==ChallengeStage.Warmup&&host.Current.UnlockedExtraPotMask==0,"formal retry must reset workflow");
        // Fill with the third type until the existing overflow/revival rule fires.
        ulong sequence=0;while(CanonicalJson.Array(State(host.Current)["pendingPlateIds"]).Count>0)host.Current.Supply(new SupplyObservation(++sequence,0,true,true));
        var unused=Items(host.Current).Where(i=>(string)i["kind"]=="C").Select(i=>CanonicalJson.Int(i["itemId"])).ToArray();
        for(int i=0;i<unused.Length;i++)host.Current.Tap(new TapCommand(unused[i],(ulong)i+1,0,true));
        Check(host.Current.RevivalPending,"warmup revival not available");
        host.Current.ResolveRevival(new ResolveRevivalCommand(host.Current.RevivalOfferId,null,false,0));Check(c.Snapshot.Status==GameStatus.Failed,"warmup fail");
        await c.RetryAsync();Check(c.Stage==ChallengeStage.Warmup&&host.Current.CumulativeCompletedOrders==0,"failed warmup did not restart");
        c.Exit();Check(!c.ContinueToFormal(id,generation),"exit stale callback");
    }
    sealed class Memory:IProfilePersistence
    {public ProfileDocument Value;public ProfileDocument Load()=>Value==null?null:ProfileStore.Copy(Value);public void Save(ProfileDocument d)=>Value=ProfileStore.Copy(d);}
    static void ProfileChecks()
    {
        var storage=new Memory();var clock=new ProfileClock(()=>DateTimeOffset.UtcNow,()=>0);
        var profile=new ProfileStore("development","local",storage,clock);
        Check(!profile.WarmupTutorialCompleted&&!profile.BufferWarningCompleted,"legacy defaults");profile.CompleteTutorial(false);
        Check(profile.WarmupTutorialCompleted&&!profile.BufferWarningCompleted,"independent flags");long rev=profile.ReadSnapshot().localRevision;profile.CompleteTutorial(false);Check(profile.ReadSnapshot().localRevision==rev,"duplicate tutorial write");
        var reloaded=new ProfileStore("development","local",storage,clock);Check(reloaded.WarmupTutorialCompleted,"local persistence");
        var remote=new ProfileDocument{environment="development",account="local",bufferWarningCompleted=true};
        var merged=ProfileStore.Merge(reloaded.ReadSnapshot(),remote);Check(merged.warmupTutorialCompleted&&merged.bufferWarningCompleted,"OR cloud merge");
        var stale=ProfileStore.Merge(merged,new ProfileDocument{environment="development",account="local"});Check(stale.warmupTutorialCompleted&&stale.bufferWarningCompleted,"stale cloud reset");
        var adopted=new ProfileStore("development","wx",new Memory(),clock);merged.account="wx";adopted.ImportFacts(merged);Check(adopted.WarmupTutorialCompleted&&adopted.BufferWarningCompleted,"account adoption flags");
    }
}
