using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Determinism;
using HotpotSort.Replay;
using HotpotSort.Session;
using HotpotSort.Profile;
using HotpotSort.Bootstrap;

static class LockedPotQa
{
    static int checks;
    static readonly DailySessionFactory Factory=DailySessionFactory.FromProductionJson(File.ReadAllText("Unity/Assets/HotpotSort/Content/Daily/"+DailyContent.RuntimeFileName));
    static ChallengeContext Context()=>new ChallengeContext("20260925",Factory.Content.ContentVersion,Factory.ConfigurationDigest,"locked-pot-qa",0);
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    static Dictionary<string,object>[] Orders(DailySession s)=>CanonicalJson.Array(CanonicalJson.Map(CanonicalJson.Parse(s.Snapshot.CanonicalStateJson))["orders"]).Select(CanonicalJson.Map).ToArray();
    static bool Locked(DailySession s,int slot)=>(string)Orders(s)[slot]["state"]=="Locked";
    static DailySession Fixture(int completed,out int[] target)
    {
        var all=Factory.Content.Plates.SelectMany(p=>p.Kinds).Select((kind,index)=>new{kind,id=index+1}).ToArray();
        var triples=all.GroupBy(i=>i.kind).SelectMany(g=>g.Select((x,i)=>new{x,i}).GroupBy(x=>x.i/3).Select(t=>t.Select(x=>x.x.id).ToArray())).ToArray();
        target=triples[completed];var orders=new[]{new FixtureOrder(all[target[0]-1].kind,target.Take(2)),new FixtureOrder(null,new int[0])};
        return Factory.CreateFixtureSession(Context(),new DailyFixture(50,new int?[5],orders,triples.Take(completed).SelectMany(x=>x)));
    }
    static void Core()
    {
        foreach(int slot in new[]{2,3})using(var s=Factory.CreateDailySession(Context()))
        {
            Check(s.CanUnlockPot(slot),"initial target");Check(!s.CanUnlockPot(-1)&&!s.CanUnlockPot(4)&&!s.CanUnlockPot(1),"invalid targets");
            var result=s.UnlockPot(slot,1);Check(result.Accepted&&!Locked(s,slot)&&Locked(s,5-slot),"independent unlock");
            Check(result.Events.CanonicalEvents.Select(x=>CanonicalJson.Map(CanonicalJson.Parse(x))).Count(e=>(string)e["type"]=="PotUnlocked"&&CanonicalJson.Int(CanonicalJson.Map(e["data"])["slotId"])==slot)==1,"one targeted event");
            Check(!s.UnlockPot(slot,2).Accepted,"repeat unlock rejected");
            var replay=DailyReplay.Run(Factory,s.ExportReplay());Check(replay.Success,"replay: "+replay.Error);
        }
        foreach(int completed in new[]{30,48})
        {
            int[] target;using(var s=Fixture(completed,out target))
            {int slot=completed==30?2:3;Check(Locked(s,slot),"threshold not early");Check(DailyViewMapper.Map(s.Snapshot,null,Factory.Content,0).snapshot.completedOrders==completed,"typed completed count before threshold");Check(s.Tap(new TapCommand(target[2],1,1,true)).Accepted&&!Locked(s,slot),"threshold 31/49");Check(DailyViewMapper.Map(s.Snapshot,null,Factory.Content,0).snapshot.completedOrders==completed+1,"typed completed count after threshold");}
        }
        using(var s=Factory.CreateDailySession(Context())){s.Pause(1);Check(!s.UnlockThird(2).Accepted&&Locked(s,2),"paused cannot unlock");}
        using(var s=Factory.CreateDailySession(Context())){Check(s.UnlockThird(600000).Reason=="Timeout"&&Locked(s,2),"timeout cannot unlock");}
    }
    static async Task Rewards()
    {
        foreach(var kind in new[]{RewardKind.ThirdPot,RewardKind.FourthPot})
        {
            var service=new Service();var profile=new Profile();var coordinator=new RewardCoordinator(service,profile);long generation=1;int applied=0;bool target=true,paused=false;
            Func<RewardRoute,RewardRequest> request=route=>new RewardRequest(generation,kind,route,"20260925");
            Func<RewardRequest,Task<RewardApplicationResult>> run=r=>coordinator.RequestDetailedAsync(r,()=>generation,()=>target,()=>{applied++;return true;},p=>paused=p);
            foreach(var route in new[]{RewardRoute.SimulatedShare,RewardRoute.WeChatShare})Check(await run(request(route))==RewardApplicationResult.Unavailable&&service.Calls==0,"shares rejected before service");
            foreach(var outcome in new[]{RewardOutcome.Cancelled,RewardOutcome.Failed,RewardOutcome.Unavailable}){service.Outcome=outcome;await run(request(RewardRoute.SimulatedAd));Check(applied==0&&!paused,"unsuccessful reward does not unlock");}
            service.Outcome=RewardOutcome.Success;var success=request(RewardRoute.SimulatedAd);Check(await run(success)==RewardApplicationResult.Applied&&applied==1&&!paused,"success applies");
            Check(await run(success)==RewardApplicationResult.Duplicate&&applied==1,"duplicate ignored");
            service.Pending=new TaskCompletionSource<RewardOutcome>();var pending=run(request(RewardRoute.SimulatedAd));Check(paused,"pending pauses");
            Check(await run(request(RewardRoute.SimulatedAd))==RewardApplicationResult.Unavailable,"parallel request rejected");
            target=false;service.Pending.SetResult(RewardOutcome.Success);Check(await pending==RewardApplicationResult.Unavailable&&applied==1&&!paused,"lost target ignores success");
            target=true;service.Pending=new TaskCompletionSource<RewardOutcome>();pending=run(request(RewardRoute.SimulatedAd));coordinator.InvalidateSession();generation++;service.Pending.SetResult(RewardOutcome.Success);
            Check(await pending==RewardApplicationResult.Stale&&applied==1&&!paused,"late old-session callback ignored");Check(profile.SharesUsed("20260925")==0,"pot never consumes quota");
        }
        var persistence=new Persistence();var clock=new ProfileClock(()=>DateTimeOffset.UtcNow,()=>0);var store=new ProfileStore("qa","local",persistence,clock);
        var third=new RewardRequest(1,RewardKind.ThirdPot,RewardRoute.SimulatedAd,"20260925");
        Check(store.TryCommitAppliedReward(third,DateTimeOffset.UtcNow),"third pot profile commit");
        store=new ProfileStore("qa","local",persistence,clock);Check(store.HasAppliedReward(third.RequestId)&&!store.TryCommitAppliedReward(third,DateTimeOffset.UtcNow),"third pot persisted idempotency");
        Check((int)RewardKind.FourthPot==3&&(int)RewardKind.Revival==4,"historical enum values");
    }
    static async Task<int> Main(){try{Core();await Rewards();Console.WriteLine("LOCKED_POT_QA_PASS checks="+checks);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
    sealed class Service:IRewardService {public bool IsDevelopmentSimulation=>true;public int Calls;public RewardOutcome Outcome;public TaskCompletionSource<RewardOutcome> Pending;public Task<RewardOutcome> RequestAsync(RewardRequest r){Calls++;return Pending?.Task??Task.FromResult(Outcome);}}
    sealed class Persistence:IProfilePersistence {ProfileDocument saved;public ProfileDocument Load()=>saved==null?null:ProfileStore.Copy(saved);public void Save(ProfileDocument value)=>saved=ProfileStore.Copy(value);}
    sealed class Profile:IProfileStore {public bool IsDevelopmentSimulation=>true;public int TotalFirstWins=>0;public bool RecordFirstWin(string day)=>true;public int SharesUsed(string day)=>0;public bool TryConsumeShare(string day,string id)=>throw new Exception("unexpected share");public PlayerSettings LoadSettings()=>new PlayerSettings();public void SaveSettings(PlayerSettings value){}}
}
