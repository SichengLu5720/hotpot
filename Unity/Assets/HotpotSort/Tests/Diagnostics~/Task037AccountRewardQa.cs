using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Profile;
using HotpotSort.Session;

static class Task037AccountRewardQa
{
    static int checks;
    static readonly DateTimeOffset Epoch=DateTimeOffset.Parse("2026-09-21T22:00:00Z");
    static void Check(bool pass,string text){if(!pass)throw new Exception(text);checks++;}
    static void Throws(Action action,string text){try{action();throw new Exception(text);}catch(InvalidOperationException){checks++;}}
    static ProfileStore Store(Memory memory=null)=>new ProfileStore("development","account",memory??new Memory(),new ProfileClock(()=>Epoch,()=>0));

    static async Task Main()
    {
        Check(TimeResolver.ChallengeDay(DateTimeOffset.Parse("2026-09-22T21:59:59.999Z"))=="20260922","before Beijing 06:00");
        Check(TimeResolver.ChallengeDay(DateTimeOffset.Parse("2026-09-22T22:00:00.000Z"))=="20260923","at Beijing 06:00");

        var memory=new Memory();var store=Store(memory);const string day="20260922";
        for(int i=0;i<ShareAvailability.DailyLimit;i++)
        {
            string id="share-"+i;var request=new RewardRequest(1,RewardKind.SwapOrder,RewardRoute.WeChatShare,day);
            Check(store.ReadShareAvailability(day,Epoch).Available,"share available "+i);
            Check(store.TryReserveShare(day,id,Epoch),"reserve share "+i);
            Check(store.TryCommitShare(day,id,Epoch),"commit share "+i);
            Check(store.ReadShareAvailability(day,Epoch).NextAvailableUtc==null,"no cooldown "+i);
        }
        var exhausted=store.ReadShareAvailability(day,Epoch);
        Check(exhausted.Used==7&&exhausted.Remaining==0&&!exhausted.Available,"seven share cap");
        Check(!store.TryReserveShare(day,"share-8",Epoch),"eighth rejected");
        Check(store.ReadShareAvailability("20260923",Epoch).Available,"next challenge day reset");

        var service=new Service();var adapter=new Adapter(Store());var coordinator=new RewardCoordinator(service,adapter,()=>Epoch);
        var kinds=new[]{RewardKind.ThirdPot,RewardKind.FourthPot,RewardKind.SwapOrder,RewardKind.ClearBuffer,RewardKind.Shuffle,RewardKind.Revival,RewardKind.SwapOrder};
        foreach(var kind in kinds)
        {
            var request=new RewardRequest(1,kind,RewardRoute.WeChatShare,"20260924");
            Check(await coordinator.RequestDetailedAsync(request,()=>1,()=>true,()=>true,_=>{})==RewardApplicationResult.Applied,kind+" share route");
        }
        Check(await coordinator.RequestDetailedAsync(new RewardRequest(1,RewardKind.SwapOrder,RewardRoute.WeChatShare,"20260924"),()=>1,()=>true,()=>true,_=>{})==RewardApplicationResult.Unavailable,"coordinator rejects eighth share");

        var accountMemory=new Memory();var account=Store(accountMemory);
        Check(account.ConsecutiveFormalFailures==0,"old profile defaults to zero failures");
        Check(account.RecordFormalResult("result-1","session-1",day,FormalChallengeResultKind.FailedBufferFull,Epoch)==AccountMutationResult.Applied,"buffer failure");
        Check(account.RecordFormalResult("result-2","session-2",day,FormalChallengeResultKind.FailedTimeout,Epoch.AddMinutes(1))==AccountMutationResult.Applied,"timeout failure");
        Check(account.ConsecutiveFormalFailures==2,"failure streak two");
        Check(account.RecordFormalResult("result-2","session-2",day,FormalChallengeResultKind.FailedTimeout,Epoch.AddMinutes(1))==AccountMutationResult.Duplicate,"result idempotent");
        Throws(()=>account.RecordFormalResult("result-2","session-other",day,FormalChallengeResultKind.Won,Epoch.AddMinutes(2)),"conflicting result accepted");
        account=Store(accountMemory);Check(account.ConsecutiveFormalFailures==2,"failure streak persisted");
        Check(account.RecordFormalResult("result-3","session-3",day,FormalChallengeResultKind.Won,Epoch.AddMinutes(3))==AccountMutationResult.Applied&&account.ConsecutiveFormalFailures==0,"formal win resets");
        Check(account.RecordFormalResult("result-4","session-4",day,FormalChallengeResultKind.FailedTimeout,Epoch.AddMinutes(4))==AccountMutationResult.Applied&&account.ConsecutiveFormalFailures==1,"post-win failure starts one");

        Check(account.GrantSwapOrderRewards("grant-1",2,Epoch)==AccountMutationResult.Applied,"grant two swap rewards");
        Check(account.GrantSwapOrderRewards("grant-1",2,Epoch)==AccountMutationResult.Duplicate&&account.SwapOrderRewardInventory==2,"grant idempotent");
        Check(account.ConsumeSwapOrderReward("spend-1",Epoch.AddMinutes(5))==AccountMutationResult.Applied&&account.SwapOrderRewardInventory==1,"consume one");
        Check(account.ConsumeSwapOrderReward("spend-1",Epoch.AddMinutes(5))==AccountMutationResult.Duplicate&&account.SwapOrderRewardInventory==1,"spend idempotent");
        Check(account.ConsumeSwapOrderReward("spend-2",Epoch.AddMinutes(6))==AccountMutationResult.Applied&&account.SwapOrderRewardInventory==0,"consume final");
        Check(account.ConsumeSwapOrderReward("spend-3",Epoch.AddMinutes(7))==AccountMutationResult.InsufficientInventory,"empty inventory rejected");
        account=Store(accountMemory);Check(account.SwapOrderRewardInventory==0&&account.ConsecutiveFormalFailures==1,"account facts persisted");
        var snapshot=account.ReadSnapshot();Check(snapshot.pending.Exists(x=>x.kind=="formal")&&snapshot.pending.Exists(x=>x.kind=="swapGrant")&&snapshot.pending.Exists(x=>x.kind=="swapSpend"),"account facts queued for cloud sync");

        Console.WriteLine("TASK037_ACCOUNT_REWARD_QA_PASS checks="+checks);
    }

    sealed class Memory:IProfilePersistence
    {
        ProfileDocument value;
        public ProfileDocument Load()=>value==null?null:ProfileStore.Copy(value);
        public void Save(ProfileDocument document){value=ProfileStore.Copy(document);}
    }
    sealed class Service:IRewardService
    {public bool IsDevelopmentSimulation=>true;public Task<RewardOutcome> RequestAsync(RewardRequest request)=>Task.FromResult(RewardOutcome.Success);}
    sealed class Adapter:IProfileStore,IShareQuotaStore,IAppliedRewardStore
    {
        readonly ProfileStore store;public Adapter(ProfileStore store){this.store=store;}
        public bool IsDevelopmentSimulation=>true;public int TotalFirstWins=>store.ReadSnapshot().firstWinDays.Count;
        public bool RecordFirstWin(string day)=>store.RecordFirstWin(day);public int SharesUsed(string day)=>store.ReadShareAvailability(day,Epoch).Used;
        public bool TryConsumeShare(string day,string requestId)=>store.TryCommitShare(day,requestId,Epoch);
        public PlayerSettings LoadSettings()=>new PlayerSettings();public void SaveSettings(PlayerSettings value){}
        public ShareAvailability ReadShareAvailability(string day,DateTimeOffset utc)=>store.ReadShareAvailability(day,utc);
        public bool TryReserveShare(string day,string requestId,DateTimeOffset utc)=>store.TryReserveShare(day,requestId,utc);
        public bool HasShareReservation(string day,string requestId)=>store.HasShareReservation(day,requestId);
        public bool TryCommitShare(string day,string requestId,DateTimeOffset utc)=>store.TryCommitShare(day,requestId,utc);
        public void ReleaseShare(string day,string requestId)=>store.ReleaseShare(day,requestId);
        public bool HasAppliedReward(string requestId)=>store.HasAppliedReward(requestId);
        public bool TryCommitAppliedReward(RewardRequest request,DateTimeOffset utc)=>store.TryCommitAppliedReward(request,utc);
    }
}
