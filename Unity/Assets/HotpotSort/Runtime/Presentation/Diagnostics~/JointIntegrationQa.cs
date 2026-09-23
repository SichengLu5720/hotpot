using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Platform;
using HotpotSort.Profile;
using HotpotSort.Session;

static class JointIntegrationQa
{
    const string Day="20260922";
    static DateTimeOffset utc=new DateTimeOffset(2026,9,22,0,0,0,TimeSpan.Zero);
    static readonly List<object> results=new List<object>();static int failures;
    static void Assert(bool value,string label){if(!value)throw new Exception(label);}
    static async Task Check(string name,Func<Task> body){try{await body();results.Add(new{name,status="PASS"});Console.WriteLine(name+" PASS");}catch(Exception ex){failures++;results.Add(new{name,status="FAIL",error=ex.ToString()});Console.WriteLine(name+" FAIL "+ex);}}
    static RewardRequest Request(RewardRoute route,RewardKind kind=RewardKind.Hint,long generation=1)=>new RewardRequest(generation,kind,route,Day,"session");
    static async Task<int> Main(string[] args)
    {
        await Check("J01-four-route-shared-pool",async()=>
        {
            var p=new Profile();var coordinator=new RewardCoordinator(new Immediate(),p,()=>utc);int effects=0;
            foreach(var route in new[]{RewardRoute.SimulatedShare,RewardRoute.WeChatShare,RewardRoute.SimulatedAd,RewardRoute.WeChatRewardedVideo})
            {
                var result=await coordinator.RequestDetailedAsync(Request(route),()=>1,()=>true,()=>{effects++;Assert(coordinator.IsApplying,"effect boundary not guarded");return true;},_=>{});
                Assert(result==RewardApplicationResult.Applied,"route denied "+route);utc=utc.AddMinutes(5);
            }
            Assert(effects==4&&p.SharesUsed(Day)==2&&p.Store.ReadSnapshot().rewards.Count==4,"route quota classification");
            Assert(!p.Store.ReadShareAvailability(Day,utc.AddMinutes(-10)).Available,"shared 15min cooldown");
            Assert(RewardRoutes.ForService(RewardRoute.SimulatedShare,false)==RewardRoute.WeChatShare&&RewardRoutes.ForService(RewardRoute.SimulatedAd,false)==RewardRoute.WeChatRewardedVideo,"platform mapping");
            Assert(RewardRoutes.ForService(RewardRoute.WeChatShare,true)==RewardRoute.SimulatedShare,"editor mapping");
            Assert(await coordinator.RequestDetailedAsync(Request(RewardRoute.WeChatShare,RewardKind.FourthPot),()=>1,()=>true,()=>true,_=>{})==RewardApplicationResult.Unavailable,"fourth pot share admitted");
        });
        await Check("J02-native-share-durable-effect",async()=>
        {
            var runtime=new Runtime();var share=new WeChatShareService(new WeChatRuntimeConfig(),runtime);var service=new WeChatRewardService(new WeChatRuntimeConfig(),runtime,share);var p=new Profile();var c=new RewardCoordinator(service,p,()=>utc);int effects=0;bool paused=false;
            var request=Request(RewardRoute.WeChatShare);var task=c.RequestDetailedAsync(request,()=>1,()=>true,()=>{effects++;return true;},v=>paused=v);
            runtime.Hide();runtime.Show();Assert(!task.IsCompleted&&paused,"platform callback not deferred");runtime.Flush();
            Assert(await task==RewardApplicationResult.Applied&&effects==1&&!paused&&p.Memory.Value.rewards.Single().route==(int)RewardRoute.WeChatShare&&p.SharesUsed(Day)==1,"native success was not durably charged");
            Assert(await c.RequestDetailedAsync(request,()=>1,()=>true,()=>{effects++;return true;},_=>{})==RewardApplicationResult.Duplicate&&effects==1,"native duplicate");service.Dispose();share.Dispose();
        });
        await Check("J03-exit-retry-target-cancellation",async()=>
        {
            foreach(var route in new[]{RewardRoute.WeChatShare,RewardRoute.WeChatRewardedVideo})foreach(bool sessionChange in new[]{false,true})
            {
                var runtime=new Runtime();var config=new WeChatRuntimeConfig{rewardedAdUnitId="test"};var share=new WeChatShareService(config,runtime);var service=new WeChatRewardService(config,runtime,share);var p=new Profile();var c=new RewardCoordinator(service,p,()=>utc);long generation=1;int effects=0;bool paused=false;
                var task=c.RequestDetailedAsync(Request(route),()=>generation,()=>true,()=>{effects++;return true;},v=>paused=v);
                if(route==RewardRoute.WeChatShare){runtime.Hide();runtime.Show();}else{runtime.Video.LoadedNow();runtime.Video.End(true);}
                if(sessionChange){generation++;c.InvalidateSession();}else c.InvalidateTarget();
                runtime.Flush();Assert(await task==RewardApplicationResult.Stale&&effects==0&&p.SharesUsed(Day)==0&&c.PendingRequest==null,"stale platform awarded");
                if(!sessionChange)Assert(!paused,"target cancellation retained Reward pause");
                Assert(runtime.Listeners==0,"cancel listeners leaked");service.Dispose();share.Dispose();
            }
        });
        await Check("J04-config-fails-closed",async()=>
        {
            var runtime=new Runtime();var config=WeChatRuntimeConfig.Unavailable();var share=new WeChatShareService(config,runtime);var service=new WeChatRewardService(config,runtime,share);var p=new Profile();var c=new RewardCoordinator(service,p,()=>utc);int effects=0;
            foreach(var route in new[]{RewardRoute.WeChatShare,RewardRoute.WeChatRewardedVideo})
                Assert(await c.RequestDetailedAsync(Request(route),()=>1,()=>true,()=>{effects++;return true;},_=>{})==RewardApplicationResult.Unavailable,"missing config awarded");
            Assert(effects==0&&p.SharesUsed(Day)==0&&runtime.Shares==0&&runtime.Videos==0&&!share.RegisterMenu(),"fallback/free reward or external side effect");service.Dispose();share.Dispose();
        });
        await Check("J05-revival-failure-retry-same-route",async()=>
        {
            var runtime=new Runtime();var config=new WeChatRuntimeConfig();var share=new WeChatShareService(config,runtime);var service=new WeChatRewardService(config,runtime,share);var p=new Profile();var c=new RewardCoordinator(service,p,()=>utc);bool revival=true,reward=false;int effects=0;
            var first=c.RequestDetailedAsync(Request(RewardRoute.WeChatShare,RewardKind.Revival),()=>1,()=>revival,()=>{effects++;revival=false;return true;},v=>reward=v);
            runtime.Seconds=10;runtime.Advance();Assert(await first==RewardApplicationResult.Failed&&revival&&!reward&&effects==0&&p.SharesUsed(Day)==0,"failed revival mutated core/ledger");
            var second=c.RequestDetailedAsync(Request(RewardRoute.WeChatShare,RewardKind.Revival),()=>1,()=>revival,()=>{effects++;revival=false;return true;},v=>reward=v);
            runtime.Hide();runtime.Show();runtime.Flush();Assert(await second==RewardApplicationResult.Applied&&effects==1&&p.SharesUsed(Day)==1,"same-route retry");service.Dispose();share.Dispose();
        });
        await Check("J06-cancel-during-pause-before-platform",async()=>
        {
            var service=new Immediate();var c=new RewardCoordinator(service,new Profile(),()=>utc);int effects=0;
            var result=await c.RequestDetailedAsync(Request(RewardRoute.WeChatShare),()=>1,()=>true,()=>{effects++;return true;},paused=>{if(paused)c.InvalidateTarget();});
            Assert(result==RewardApplicationResult.Stale&&service.Calls==0&&effects==0,"invalidated target launched platform call");
        });
        File.WriteAllText(args[0],JsonSerializer.Serialize(new{tasks=new[]{"TASK-001-v9","TASK-006-v1","TASK-007-v1"},failures,results},new JsonSerializerOptions{WriteIndented=true}));return failures==0?0:1;
    }
    sealed class Memory:IProfilePersistence{public ProfileDocument Value;public ProfileDocument Load()=>Value==null?null:ProfileStore.Copy(Value);public void Save(ProfileDocument d)=>Value=ProfileStore.Copy(d);}
    sealed class Profile:IProfileStore,IShareQuotaStore,IAppliedRewardStore
    {
        public readonly Memory Memory=new Memory();public readonly ProfileStore Store;
        public Profile(){Store=new ProfileStore("test","account",Memory,new ProfileClock(()=>utc,()=>0));}
        public bool IsDevelopmentSimulation=>false;public int TotalFirstWins=>Store.ReadSnapshot().firstWinDays.Count;
        public bool RecordFirstWin(string d)=>Store.RecordFirstWin(d);public int SharesUsed(string d)=>Store.ReadShareAvailability(d,utc).Used;
        public bool TryConsumeShare(string d,string id)=>Store.TryReserveShare(d,id,utc)&&Store.TryCommitShare(d,id,utc);
        public PlayerSettings LoadSettings()=>new PlayerSettings();public void SaveSettings(PlayerSettings s){}
        public ShareAvailability ReadShareAvailability(string d,DateTimeOffset t)=>Store.ReadShareAvailability(d,t);
        public bool TryReserveShare(string d,string id,DateTimeOffset t)=>Store.TryReserveShare(d,id,t);public bool HasShareReservation(string d,string id)=>Store.HasShareReservation(d,id);
        public bool TryCommitShare(string d,string id,DateTimeOffset t)=>Store.TryCommitShare(d,id,t);public void ReleaseShare(string d,string id)=>Store.ReleaseShare(d,id);
        public bool HasAppliedReward(string id)=>Store.HasAppliedReward(id);public bool TryCommitAppliedReward(RewardRequest r,DateTimeOffset t)=>Store.TryCommitAppliedReward(r,t);
    }
    sealed class Immediate:IRewardService{public int Calls;public bool IsDevelopmentSimulation=>false;public Task<RewardOutcome> RequestAsync(RewardRequest r){Calls++;return Task.FromResult(RewardOutcome.Success);}}
    sealed class Runtime:IWeChatRewardRuntime
    {
        public bool Available=>true;public double Seconds;public double MonotonicSeconds=>Seconds;public int Shares,Videos;public Video Video;
        public event Action Hidden,Shown,Tick;public event Action Stopping{add{}remove{}}
        public int Listeners=>(Hidden?.GetInvocationList().Length??0)+(Shown?.GetInvocationList().Length??0)+(Tick?.GetInvocationList().Length??0);
        readonly Queue<Action> queue=new Queue<Action>();public void Post(Action a)=>queue.Enqueue(a);public void Flush(){while(queue.Count>0)queue.Dequeue()();}
        public void Hide()=>Hidden?.Invoke();public void Show()=>Shown?.Invoke();public void Advance()=>Tick?.Invoke();
        public void Share(string a,string b){Shares++;}public void RegisterMenu(string a,string b){}public IWeChatRewardVideo CreateVideo(string id){Videos++;return Video=new Video();}
    }
    sealed class Video:IWeChatRewardVideo
    {public event Action Loaded;public event Action Failed{add{}remove{}}public event Action<bool?> Closed;public void Load(){}public void Show(){}public void LoadedNow()=>Loaded?.Invoke();public void End(bool? value)=>Closed?.Invoke(value);public void Dispose(){Loaded=null;Closed=null;}}
}
