using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotpotSort.Contracts;

namespace HotpotSort.Session
{
    public sealed class RewardCoordinator
    {
        readonly IRewardService service;
        readonly IShareQuotaStore quota;
        readonly IAppliedRewardStore appliedStore;
        readonly Func<DateTimeOffset> utcNow;
        readonly HashSet<string> completed=new HashSet<string>(StringComparer.Ordinal);
        RewardRequest active;
        Action<bool> activePause;
        Func<long> activeGeneration;
        public RewardRequest PendingRequest=>active;
        public bool IsApplying { get; private set; }
        public DateTimeOffset UtcNow=>utcNow().ToUniversalTime();
        public RewardCoordinator(IRewardService service,IProfileStore profile,Func<DateTimeOffset> utcNow=null)
        {
            this.service=service??throw new ArgumentNullException(nameof(service));
            if(profile==null)throw new ArgumentNullException(nameof(profile));
            quota=profile as IShareQuotaStore??new LegacyQuotaAdapter(profile);
            appliedStore=profile as IAppliedRewardStore;
            this.utcNow=utcNow??(()=>(profile as IAsyncProfileStore)?.TimeSnapshot.Utc??DateTimeOffset.UtcNow);
        }
        public ShareAvailability ReadShareAvailability()
        {var now=UtcNow;return quota.ReadShareAvailability(TimeResolver.ChallengeDay(now),now);}
        public ShareAvailability ReadShareAvailability(string day)=>quota.ReadShareAvailability(day,UtcNow);
        public void InvalidateSession()
        {
            if(active==null)return;
            var request=active;var releasePause=activePause;var generation=activeGeneration;
            active=null;activePause=null;activeGeneration=null;
            quota.ReleaseShare(request.QuotaChallengeDate,request.RequestId);
            try{(service as IRewardRequestCancellation)?.CancelRequest(request.RequestId);}
            finally{if(generation!=null&&request.SessionGeneration==generation())releasePause?.Invoke(false);}
        }
        public void InvalidateTarget()=>InvalidateSession();
        public async Task<bool> RequestAsync(RewardRequest request,Func<long> generation,Func<bool> hasTarget,Func<bool> apply,Action<bool> pause)
            =>await ExecuteAsync(request,generation,hasTarget,apply,pause,false)==RewardApplicationResult.Applied;
        public Task<RewardApplicationResult> RequestDetailedAsync(RewardRequest request,Func<long> generation,Func<bool> hasTarget,Func<bool> apply,Action<bool> pause)
            =>ExecuteAsync(request,generation,hasTarget,apply,pause,true);
        async Task<RewardApplicationResult> ExecuteAsync(RewardRequest request,Func<long> generation,Func<bool> hasTarget,Func<bool> apply,Action<bool> pause,bool convertServiceErrors)
        {
            if(request==null)throw new ArgumentNullException(nameof(request));
            if(request.SessionGeneration!=generation())return RewardApplicationResult.Stale;
            if(completed.Contains(request.RequestId)||active?.RequestId==request.RequestId||appliedStore?.HasAppliedReward(request.RequestId)==true)return RewardApplicationResult.Duplicate;
            if(active!=null && active.SessionGeneration!=generation())InvalidateSession();
            if(active!=null)return RewardApplicationResult.Unavailable;
            if(!Enum.IsDefined(typeof(RewardKind),request.Kind)||!Enum.IsDefined(typeof(RewardRoute),request.Route)||!hasTarget())return RewardApplicationResult.Unavailable;
            bool share=RewardRoutes.IsShare(request.Route);
            if(share && (request.Kind==RewardKind.FourthPot||!quota.TryReserveShare(request.QuotaChallengeDate,request.RequestId,UtcNow)))return RewardApplicationResult.Unavailable;
            active=request;activePause=pause;activeGeneration=generation;
            try
            {
                pause(true);
                if(!ReferenceEquals(active,request))return RewardApplicationResult.Stale;
                RewardOutcome outcome;
                try {outcome=await service.RequestAsync(request);}
                catch {if(!convertServiceErrors)throw;outcome=RewardOutcome.Failed;}
                if(request.SessionGeneration!=generation()||!ReferenceEquals(active,request))
                {(service as IRewardRequestCancellation)?.CancelRequest(request.RequestId);return RewardApplicationResult.Stale;}
                completed.Add(request.RequestId);
                if(outcome!=RewardOutcome.Success)
                    return outcome==RewardOutcome.Cancelled?RewardApplicationResult.Cancelled:outcome==RewardOutcome.Unavailable?RewardApplicationResult.Unavailable:RewardApplicationResult.Failed;
                if(share&&!quota.HasShareReservation(request.QuotaChallengeDate,request.RequestId))return RewardApplicationResult.Unavailable;
                // Ordinary props retain their Running precondition. Revival commits
                // while both Reward and Revival reasons are still held.
                if(request.Kind!=RewardKind.Revival)pause(false);
                if(!hasTarget())
                {(service as IRewardRequestCancellation)?.CancelRequest(request.RequestId);return RewardApplicationResult.Unavailable;}
                bool applied;IsApplying=true;
                try{applied=apply();}finally{IsApplying=false;}
                if(!applied){(service as IRewardRequestCancellation)?.CancelRequest(request.RequestId);return RewardApplicationResult.Unavailable;}
                if(appliedStore!=null?!appliedStore.TryCommitAppliedReward(request,UtcNow):share&&!quota.TryCommitShare(request.QuotaChallengeDate,request.RequestId,UtcNow))
                    throw new InvalidOperationException("Share reservation changed during synchronous reward commit");
                return RewardApplicationResult.Applied;
            }
            finally
            {
                quota.ReleaseShare(request.QuotaChallengeDate,request.RequestId);
                if(ReferenceEquals(active,request))
                {
                    active=null;activePause=null;activeGeneration=null;
                    if(request.SessionGeneration==generation())pause(false);
                }
            }
        }
        // Used-only injected profiles remain supported as migrated legacy records.
        // Durable timestamp storage is supplied by IShareQuotaStore (Local implements it).
        sealed class LegacyQuotaAdapter:IShareQuotaStore
        {
            readonly IProfileStore profile;
            readonly List<ShareQuotaRecord> records=new List<ShareQuotaRecord>();
            readonly ShareQuotaLedger ledger;
            public LegacyQuotaAdapter(IProfileStore profile){this.profile=profile;ledger=new ShareQuotaLedger(records,new List<string>());}
            void Load(string day){if(!records.Exists(r=>r.day==day))records.Add(new ShareQuotaRecord{day=day,used=profile.SharesUsed(day)});}
            public ShareAvailability ReadShareAvailability(string day,DateTimeOffset utc){Load(day);return ledger.ReadShareAvailability(day,utc);}
            public bool TryReserveShare(string day,string id,DateTimeOffset utc){Load(day);return ledger.TryReserveShare(day,id,utc);}
            public bool HasShareReservation(string day,string id)=>ledger.HasShareReservation(day,id)&&profile.SharesUsed(day)<3;
            public bool TryCommitShare(string day,string id,DateTimeOffset utc)=>HasShareReservation(day,id)&&profile.TryConsumeShare(day,id)&&ledger.TryCommitShare(day,id,utc);
            public void ReleaseShare(string day,string id)=>ledger.ReleaseShare(day,id);
        }
    }
}
