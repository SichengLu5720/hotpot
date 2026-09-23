using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Profile;
using HotpotSort.Session;

static class ProfileSyncQa
{
    static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true,WriteIndented=true};
    static readonly List<object> Results=new List<object>();
    static readonly DateTimeOffset Epoch=new DateTimeOffset(2026,9,21,22,0,0,TimeSpan.Zero);
    const string Day="20260922";
    static string Encode(ProfileDocument d)=>JsonSerializer.Serialize(d,Json);
    static ProfileDocument Decode(string s)=>JsonSerializer.Deserialize<ProfileDocument>(s,Json);
    static void Assert(bool value,string detail){if(!value)throw new Exception(detail);}
    static void Throws(Action action){bool threw=false;try{action();}catch{threw=true;}Assert(threw,"expected explicit failure");}
    static async Task Check(string id,Func<Task> body){try{await body();Results.Add(new{id,status="PASS"});Console.WriteLine(id+" PASS");}catch(Exception ex){Results.Add(new{id,status="FAIL",error=ex.ToString()});Console.WriteLine(id+" FAIL "+ex);}}
    static Task Sync(Action a){a();return Task.CompletedTask;}
    sealed class Memory:IProfilePersistence
    {
        public ProfileDocument Value;public bool Fail;public int Writes;
        public ProfileDocument Load()=>Value==null?null:ProfileStore.Copy(Value);
        public void Save(ProfileDocument value){Writes++;if(Fail)throw new IOException("injected durable write failure");Value=ProfileStore.Copy(value);}
    }
    sealed class Time
    {public DateTimeOffset Utc=Epoch;public double Monotonic;public ProfileClock Clock()=>new ProfileClock(()=>Utc,()=>Monotonic);}
    sealed class Transport:IProfileSyncTransport
    {
        public Func<ProfileDocument,Task<ProfileSyncResponse>> Exchange;
        public Task<ProfileSyncResponse> SyncAsync(ProfileDocument d)=>Exchange(d);
    }
    static ProfileStore Store(Memory m=null,Time t=null,IProfileSyncTransport transport=null,ProfileDocument legacy=null)=>new ProfileStore("test","account",m??new Memory(),(t??new Time()).Clock(),transport,legacy);
    static RewardRequest Request(string day=Day,RewardKind kind=RewardKind.Hint,RewardRoute route=RewardRoute.SimulatedShare)=>new RewardRequest(1,kind,route,day,"session");
    static void Award(ProfileStore store,RewardRequest request,DateTimeOffset utc)
    {
        if(request.Route==RewardRoute.SimulatedShare)Assert(store.TryReserveShare(request.QuotaChallengeDate,request.RequestId,utc),"reserve");
        Assert(store.TryCommitAppliedReward(request,utc),"commit");
    }
    static ProfileSyncResponse Ack(ProfileDocument upload)=>new ProfileSyncResponse{Status=ProfileSyncStatus.Synced,Snapshot=ProfileStore.Copy(upload),AcknowledgedOperations=upload.pending.Select(p=>p.operationId).ToArray(),ConfirmationCursor="cursor-1",ServerUtc=Epoch};
    static string Facts(ProfileDocument value)=>Encode(new ProfileDocument{environment=value.environment,account=value.account,firstWinDays=value.firstWinDays.OrderBy(x=>x).ToList(),legacyRequestIds=value.legacyRequestIds.OrderBy(x=>x).ToList(),rewards=value.rewards.OrderBy(x=>x.requestId).ToList(),legacyQuotas=value.legacyQuotas.OrderBy(x=>x.day).ToList()});
    static async Task<int> Main(string[] args)
    {
        await Check("PS01-local-durable-restart",()=>Sync(()=>
        {
            var memory=new Memory();var s=Store(memory);var request=Request();Assert(s.RecordFirstWin(Day)&&!s.RecordFirstWin(Day),"first win idempotency");
            Award(s,request,Epoch);Assert(memory.Value.rewards.Count==1&&memory.Value.pending.Count==3,"fact + outbox not one persisted document");
            var restarted=Store(memory);Assert(restarted.HasAppliedReward(request.RequestId)&&restarted.ReadSnapshot().firstWinDays.Single()==Day,"restart lost facts");
            Assert(!restarted.TryReserveShare(Day,request.RequestId,Epoch.AddHours(1)),"global ID duplicated");
            var detached=restarted.ReadSnapshot();detached.rewards.Clear();Assert(restarted.ReadSnapshot().rewards.Count==1,"mutable read alias");
        }));
        await Check("PS02-cooldown-and-day",()=>Sync(()=>
        {
            var s=Store();Award(s,Request(),Epoch);Assert(!s.ReadShareAvailability(Day,Epoch.AddSeconds(299.999)).Available&&s.ReadShareAvailability(Day,Epoch.AddSeconds(300)).Available,"5min edge");
            Award(s,Request(kind:RewardKind.Revival),Epoch.AddMinutes(5));Assert(!s.ReadShareAvailability(Day,Epoch.AddSeconds(1199.999)).Available&&s.ReadShareAvailability(Day,Epoch.AddMinutes(20)).Available,"15min edge");
            Award(s,Request(kind:RewardKind.Shuffle),Epoch.AddMinutes(20));Assert(!s.ReadShareAvailability(Day,Epoch.AddYears(1)).Available,"cap");
            Assert(s.ReadShareAvailability("20260923",Epoch).Available,"next day");
            Assert(TimeResolver.ChallengeDay(Epoch.AddMilliseconds(-1))=="20260921"&&TimeResolver.ChallengeDay(Epoch)==Day,"Beijing06");
            var across=Store();var frozen=Request("20260921");Award(across,frozen,Epoch.AddSeconds(1));Assert(across.ReadShareAvailability("20260921",Epoch).Used==1&&across.ReadShareAvailability(Day,Epoch).Used==0,"late reward rebucketed");
        }));
        await Check("PS03-union-idempotent-overcap",()=>Sync(()=>
        {
            var a=Store();var b=Store();a.RecordFirstWin(Day);b.RecordFirstWin("20260921");
            Award(a,Request(),Epoch);Award(a,Request(),Epoch.AddMinutes(5));Award(b,Request(),Epoch.AddMinutes(1));Award(b,Request(),Epoch.AddMinutes(6));
            var aa=a.ReadSnapshot();var bb=b.ReadSnapshot();var union=ProfileStore.Merge(aa,bb);
            Assert(union.firstWinDays.Count==2&&union.rewards.Count==4&&ProfileStore.Availability(union,Day,Epoch.AddHours(5)).Used==4&&!ProfileStore.Availability(union,Day,Epoch.AddHours(5)).Available,"offline overcap clawback");
            Assert(Facts(ProfileStore.Merge(bb,aa))==Facts(union)&&Facts(ProfileStore.Merge(union,union))==Facts(union),"commutative/idempotent facts");
            var third=Store();Award(third,Request(),Epoch.AddMinutes(2));Assert(Facts(ProfileStore.Merge(ProfileStore.Merge(aa,bb),third.ReadSnapshot()))==Facts(ProfileStore.Merge(aa,ProfileStore.Merge(bb,third.ReadSnapshot()))),"associativity");
            var random=new Random(73);for(int i=0;i<80;i++){var facts=union.rewards.OrderBy(_=>random.Next()).ToList();var permuted=ProfileStore.Copy(union);permuted.rewards=facts;Assert(Facts(ProfileStore.Merge(aa,permuted))==Facts(union),"permutation");}
        }));
        await Check("PS04-late-original-time",()=>Sync(()=>
        {
            var a=Store();var b=Store();Award(a,Request(),Epoch);Award(b,Request(),Epoch.AddMinutes(2));var merged=ProfileStore.Merge(a.ReadSnapshot(),b.ReadSnapshot());
            var available=ProfileStore.Availability(merged,Day,Epoch.AddHours(2));Assert(available.Used==2&&available.NextAvailableUtc==Epoch.AddMinutes(17)&&available.Available,"sync restarted cooldown");
            var corrupt=ProfileStore.Copy(merged);corrupt.rewards[0].effectiveUtc=Epoch.AddDays(1).ToString("o");Throws(()=>ProfileStore.Merge(merged,corrupt));
        }));
        await Check("PS05-async-outbox-ack",async()=>
        {
            var pending=new TaskCompletionSource<ProfileSyncResponse>();ProfileDocument sent=null;
            var transport=new Transport{Exchange=d=>{sent=d;return pending.Task;}};var memory=new Memory();var s=Store(memory,transport:transport);s.RecordFirstWin(Day);
            var sync=s.SyncAsync();Assert(!sync.IsCompleted,"sync unexpectedly blocking");Assert(await s.SyncAsync()==ProfileSyncStatus.Busy,"parallel sync");
            s.RecordFirstWin("20260923");pending.SetResult(Ack(sent));Assert(await sync==ProfileSyncStatus.Synced,"sync");
            Assert(s.ReadSnapshot().pending.Single().entityId=="20260923"&&memory.Value.confirmationCursor=="cursor-1","late ack erased new write");
            Assert(s.TimeSnapshot.Source==ProfileTimeSource.TrustedServer,"trusted sync anchor");
        });
        await Check("PS06-stale-session-account-environment",async()=>
        {
            var pending=new TaskCompletionSource<ProfileSyncResponse>();ProfileDocument sent=null;var time=new Time();
            var s=Store(t:time,transport:new Transport{Exchange=d=>{sent=d;return pending.Task;}});var sync=s.SyncAsync();s.InvalidateSyncCallbacks();var ack=Ack(sent);ack.Snapshot.firstWinDays.Add(Day);pending.SetResult(ack);
            Assert(await sync==ProfileSyncStatus.Stale&&s.ReadSnapshot().firstWinDays.Count==0&&s.TimeSnapshot.Source==ProfileTimeSource.DeviceTime,"stale callback applied");
            var other=ProfileStore.Copy(s.ReadSnapshot());other.environment="production";Throws(()=>ProfileStore.Merge(s.ReadSnapshot(),other));other.environment="test";other.account="other";Throws(()=>ProfileStore.Merge(s.ReadSnapshot(),other));
        });
        await Check("PS07-offline-and-notconfigured",async()=>
        {
            var s=Store();s.RecordFirstWin(Day);var count=s.ReadSnapshot().pending.Count;Assert(await s.SyncAsync()==ProfileSyncStatus.NotConfigured&&s.ReadSnapshot().pending.Count==count,"NotConfigured dropped queue");
            var failed=Store(transport:new Transport{Exchange=_=>throw new IOException("offline")});failed.RecordFirstWin(Day);Assert(await failed.SyncAsync()==ProfileSyncStatus.Failed&&failed.ReadSnapshot().pending.Count==2,"offline data lost");
            var lossy=Store(transport:new Transport{Exchange=d=>{var response=Ack(d);response.Snapshot.firstWinDays.Clear();return Task.FromResult(response);}});lossy.RecordFirstWin(Day);Assert(await lossy.SyncAsync()==ProfileSyncStatus.Synced&&lossy.ReadSnapshot().pending.Any(p=>p.kind=="win"),"lying ack dequeued win");
        });
        await Check("PS08-migration-no-fake-time",()=>Sync(()=>
        {
            var legacy=ProfileStore.Migrate("test","account",new[]{Day,Day},new[]{new ShareQuotaRecord{day=Day,used=2}},new[]{"legacy-id"});var s=Store(legacy:legacy);
            var before=s.ReadShareAvailability(Day,Epoch);Assert(before.Used==2&&before.NextAvailableUtc==null&&before.Available,"missing timestamp forged");Assert(!s.TryReserveShare(Day,"legacy-id",Epoch),"legacy duplicate");
            Award(s,Request(),Epoch);Assert(s.ReadShareAvailability(Day,Epoch).Used==3,"legacy used lost");
            Assert(ProfileStore.Availability(ProfileStore.Merge(s.ReadSnapshot(),legacy),Day,Epoch).Used==3,"repeated migration doubled baseline");
            var dated=ProfileStore.Migrate("test","account",null,new[]{new ShareQuotaRecord{day=Day,used=1,lastEffectiveUtc=Epoch.ToString("o"),committedRequestId="known"}},null);
            Assert(ProfileStore.Availability(dated,Day,Epoch).NextAvailableUtc==Epoch.AddMinutes(5),"legacy timestamp lost");
        }));
        await Check("PS09-trusted-device-clock",()=>Sync(()=>
        {
            var time=new Time();var clock=time.Clock();Assert(clock.Read().Source==ProfileTimeSource.DeviceTime,"initial trust forged");clock.Trust(Epoch);time.Utc=Epoch.AddYears(1);time.Monotonic=120;
            Assert(clock.Read().Utc==Epoch.AddSeconds(120)&&clock.Read().Source==ProfileTimeSource.TrustedServer,"device changed trusted time");clock.Offline();Assert(clock.Read().Utc==time.Utc&&clock.Read().Source==ProfileTimeSource.DeviceTime,"offline flag");
            clock.Trust(Epoch);time.Monotonic=-1;Assert(clock.Read().Source==ProfileTimeSource.DeviceTime,"monotonic reset trusted");
        }));
        await Check("PS10-backup-corruption",()=>Sync(()=>
        {
            var values=new Dictionary<string,string>();Func<string,string> read=k=>values.TryGetValue(k,out var v)?v:null;
            var storage=new RecoverableProfileStorage("partition","test","account",read,(k,v)=>values[k]=v,Encode,Decode);
            var s=new ProfileStore("test","account",storage,new Time().Clock());s.RecordFirstWin(Day);s.RecordFirstWin("20260923");values["partition"]="corrupt";
            var recovered=storage.Load();Assert(storage.RecoveredBackup&&recovered.firstWinDays.Contains(Day),"backup recovery");values["partition.backup"]="also corrupt";Throws(()=>storage.Load());
            var foreign=new ProfileDocument{environment="production",account="account"};Throws(()=>storage.Save(foreign));
        }));
        await Check("PS11-write-failure-retained-fact",()=>Sync(()=>
        {
            var memory=new Memory();var s=Store(memory);var request=Request();Assert(s.TryReserveShare(Day,request.RequestId,Epoch),"reserve");memory.Fail=true;
            Throws(()=>s.TryCommitAppliedReward(request,Epoch));Assert(s.HasAppliedReward(request.RequestId)&&s.ReadSnapshot().pending.Any(p=>p.entityId==request.RequestId),"failed write forgot applied fact");
            memory.Fail=false;s.Flush();var restarted=Store(memory);Assert(restarted.ReadSnapshot().rewards.Single().effectiveUtc==Epoch.ToString("o")&&!restarted.TryCommitAppliedReward(request,Epoch.AddMinutes(3)),"retry changed time or duplicated");
        }));
        await Check("PS12-coordinator-effects-and-failures",async()=>
        {
            var memory=new Memory();var store=Store(memory);var profile=new Adapter(store);var service=new Rewards();var coordinator=new RewardCoordinator(service,profile,()=>Epoch);int effects=0;
            var request=Request();Assert(await coordinator.RequestDetailedAsync(request,()=>1,()=>true,()=>{effects++;return true;},_=>{})==RewardApplicationResult.Applied,"apply");
            Assert(memory.Value.rewards.Single().requestId==request.RequestId&&effects==1,"completion before durability");
            var retry=new RewardCoordinator(service,profile,()=>Epoch);Assert(await retry.RequestDetailedAsync(request,()=>1,()=>true,()=>{effects++;return true;},_=>{})==RewardApplicationResult.Duplicate&&effects==1,"duplicate effect after restart coordinator");
            foreach(var outcome in new[]{RewardOutcome.Cancelled,RewardOutcome.Failed,RewardOutcome.Unavailable})
            {service.Outcome=outcome;var ad=Request(route:RewardRoute.SimulatedAd);Assert(await retry.RequestDetailedAsync(ad,()=>1,()=>true,()=>{effects++;return true;},_=>{})!=RewardApplicationResult.Applied,"failed awarded");}
            Assert(effects==1&&store.ReadSnapshot().rewards.Count==1,"failed attempt charged");
            service.Outcome=RewardOutcome.Success;var fourth=Request(kind:RewardKind.FourthPot,route:RewardRoute.SimulatedAd);Assert(await retry.RequestDetailedAsync(fourth,()=>1,()=>true,()=>true,_=>{})==RewardApplicationResult.Applied&&store.ReadSnapshot().rewards.Count==2&&store.ReadShareAvailability(Day,Epoch).Used==1,"ad charged shared quota or unrecorded");
        });
        await Check("PS13-coordinator-durable-failure-retry-restart",async()=>
        {
            foreach(var route in new[]{RewardRoute.WeChatShare,RewardRoute.WeChatRewardedVideo})
            {
                var memory=new Memory();var store=Store(memory);var profile=new Adapter(store);var service=new Rewards();var now=Epoch;
                var coordinator=new RewardCoordinator(service,profile,()=>now);var request=Request(route:route);int effects=0;bool paused=false,failed=false;
                try{await coordinator.RequestDetailedAsync(request,()=>1,()=>true,()=>{effects++;memory.Fail=true;return true;},value=>paused=value);}
                catch(IOException){failed=true;}
                Assert(failed&&effects==1&&!paused&&store.HasAppliedReward(request.RequestId),"effect/write failure boundary");
                now=Epoch.AddMinutes(42);
                Assert(await coordinator.RequestDetailedAsync(request,()=>1,()=>true,()=>{effects++;return true;},_=>{})==RewardApplicationResult.Duplicate&&effects==1,"same coordinator reapplied failed durable effect");
                var second=new RewardCoordinator(service,profile,()=>now);
                Assert(await second.RequestDetailedAsync(request,()=>1,()=>true,()=>{effects++;return true;},_=>{})==RewardApplicationResult.Duplicate,"new coordinator reapplied pending durable fact");
                memory.Fail=false;Assert(await store.SyncAsync()==ProfileSyncStatus.NotConfigured,"durable retry should flush even offline");
                var restarted=Store(memory);var third=new RewardCoordinator(service,new Adapter(restarted),()=>now);
                Assert(await third.RequestDetailedAsync(request,()=>1,()=>true,()=>{effects++;return true;},_=>{})==RewardApplicationResult.Duplicate&&effects==1,"restart after durable retry duplicated effect");
                var fact=restarted.ReadSnapshot().rewards.Single();Assert(fact.requestId==request.RequestId&&fact.effectiveUtc==Epoch.ToString("o")&&restarted.ReadSnapshot().pending.Any(p=>p.entityId==request.RequestId),"retry/restart lost original timestamp/outbox");
            }
        });
        int failed=Results.Count(r=>JsonSerializer.Serialize(r).Contains("\"FAIL\""));
        File.WriteAllText(args[0],JsonSerializer.Serialize(new{task="TASK-006",version=1,checkpoint="HC-02-v1-Code",failed,results=Results,scope="Local/pure .NET contracts and fault injection only; no cloud deployment or real cross-device acceptance"},Json));return failed==0?0:1;
    }
    sealed class Rewards:IRewardService{public RewardOutcome Outcome=RewardOutcome.Success;public bool IsDevelopmentSimulation=>true;public Task<RewardOutcome> RequestAsync(RewardRequest request)=>Task.FromResult(Outcome);}
    sealed class Adapter:IProfileStore,IShareQuotaStore,IAppliedRewardStore
    {
        readonly ProfileStore store;public Adapter(ProfileStore s){store=s;}public bool IsDevelopmentSimulation=>true;public int TotalFirstWins=>store.ReadSnapshot().firstWinDays.Count;
        public bool RecordFirstWin(string d)=>store.RecordFirstWin(d);public int SharesUsed(string d)=>store.ReadShareAvailability(d,Epoch).Used;
        public bool TryConsumeShare(string d,string id)=>store.TryReserveShare(d,id,Epoch)&&store.TryCommitShare(d,id,Epoch);
        public PlayerSettings LoadSettings()=>new PlayerSettings();public void SaveSettings(PlayerSettings s){}
        public ShareAvailability ReadShareAvailability(string d,DateTimeOffset t)=>store.ReadShareAvailability(d,t);
        public bool TryReserveShare(string d,string id,DateTimeOffset t)=>store.TryReserveShare(d,id,t);public bool HasShareReservation(string d,string id)=>store.HasShareReservation(d,id);
        public bool TryCommitShare(string d,string id,DateTimeOffset t)=>store.TryCommitShare(d,id,t);public void ReleaseShare(string d,string id)=>store.ReleaseShare(d,id);
        public bool HasAppliedReward(string id)=>store.HasAppliedReward(id);public bool TryCommitAppliedReward(RewardRequest r,DateTimeOffset t)=>store.TryCommitAppliedReward(r,t);
    }
}
