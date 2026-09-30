using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using HotpotSort.Contracts;

namespace HotpotSort.Profile
{
    public interface IProfilePersistence
    {
        // Save must atomically replace one complete document, retaining a recoverable backup.
        ProfileDocument Load();
        void Save(ProfileDocument document);
    }
    public sealed class NotConfiguredProfileTransport:IProfileSyncTransport
    {public Task<ProfileSyncResponse> SyncAsync(ProfileDocument upload)=>Task.FromResult(new ProfileSyncResponse{Status=ProfileSyncStatus.NotConfigured});}

    // Local-first additive facts. Core/replay never reference this module or await sync.
    public sealed partial class ProfileStore:IShareQuotaStore,IAsyncProfileStore,IAppliedRewardStore,ITutorialProfileStore,IAccountRewardProfileStore,IOrderProtectionLifecycleStore
    {
        readonly object serial=new object();
        readonly IProfilePersistence persistence;
        readonly IProfileSyncTransport transport;
        readonly ProfileClock clock;
        ProfileDocument state;
        string reservedDay,reservedRequest;
        long generation;
        bool syncing,dirty;
        public event Action ProfileChanged;
        public ProfileTimeSnapshot TimeSnapshot { get{lock(serial)return clock.Read();} }
        public ProfileStore(string environment,string account,IProfilePersistence persistence,ProfileClock clock,IProfileSyncTransport transport=null,ProfileDocument legacy=null)
        {
            if(string.IsNullOrWhiteSpace(environment)||string.IsNullOrWhiteSpace(account))throw new ArgumentException("Profile partition required");
            this.persistence=persistence??throw new ArgumentNullException(nameof(persistence));this.clock=clock??throw new ArgumentNullException(nameof(clock));
            this.transport=transport??new NotConfiguredProfileTransport();
            var loaded=persistence.Load();
            state=loaded??legacy??new ProfileDocument{environment=environment,account=account};
            Validate(state,environment,account);state=Copy(state);
            bool initialize=!state.orderProtectionInitialized;
            if(initialize){state.orderProtectionInitialized=true;state.orderProtectionInitialPercent=loaded==null&&legacy==null?20:0;}
            bool lifecycleInitialize=state.protectionLifecycleVersion==0;
            if(lifecycleInitialize){state.protectionLifecycleVersion=1;state.protectionLegacyPlayer=loaded!=null||legacy!=null;}
            if(loaded==null||initialize||lifecycleInitialize){Queue(state,"migration","baseline");persistence.Save(Copy(state));}
        }
        public static ProfileDocument Migrate(string environment,string account,IEnumerable<string> wins,IEnumerable<ShareQuotaRecord> quotas,IEnumerable<string> claims)
        {
            var value=new ProfileDocument{environment=environment,account=account};
            value.firstWinDays.AddRange((wins??new string[0]).Distinct());value.legacyRequestIds.AddRange((claims??new string[0]).Distinct());
            foreach(var q in quotas??new ShareQuotaRecord[0])
            {
                var prior=value.legacyQuotas.Find(x=>x.day==q.day);
                if(prior==null)value.legacyQuotas.Add(Clone(q));
                else{prior.used=Math.Max(prior.used,q.used);prior.lastEffectiveUtc=Latest(prior.lastEffectiveUtc,q.lastEffectiveUtc);}
                if(!string.IsNullOrEmpty(q.committedRequestId)&&!value.legacyRequestIds.Contains(q.committedRequestId))value.legacyRequestIds.Add(q.committedRequestId);
            }
            Validate(value,environment,account);return value;
        }
        public ProfileDocument ReadSnapshot(){lock(serial)return Copy(state);}
        public int OrderProtectionPercent { get{lock(serial)return ProtectionAt(state,TimeSnapshot.Utc);} }
        public int ConsecutiveFormalFailures { get{lock(serial)return CountConsecutiveFormalFailures(state);} }
        public int SwapOrderRewardInventory { get{lock(serial)return Inventory(state);} }
        public bool WarmupTutorialCompleted { get{lock(serial)return state.warmupTutorialCompleted;} }
        public bool BufferWarningCompleted { get{lock(serial)return state.bufferWarningCompleted;} }
        public void CompleteTutorial(bool bufferWarning)
        {
            lock(serial)
            {
                Flush();if(bufferWarning?state.bufferWarningCompleted:state.warmupTutorialCompleted)return;
                var next=Copy(state);if(bufferWarning)next.bufferWarningCompleted=true;else next.warmupTutorialCompleted=true;
                Queue(next,"tutorial",bufferWarning?"buffer":"warmup");Commit(next);
            }
            Notify();
        }
        // Entry-only composition imports immutable local facts into an authenticated
        // partition. The caller retargets a COPY, never the source persisted document.
        // Queue every imported fact so a crash/retry cannot lose its server obligation.
        public void ImportFacts(ProfileDocument facts)
        {
            lock(serial)
            {
                Flush();var next=Merge(state,facts);
                foreach(var day in facts.firstWinDays)Queue(next,"win",day);
                foreach(var reward in facts.rewards)Queue(next,"reward",reward.requestId);
                foreach(var result in facts.formalResults)Queue(next,"formal",result.resultId);
                foreach(var grant in facts.swapOrderRewardGrants)Queue(next,"swapGrant",grant.grantId);
                foreach(var spend in facts.swapOrderRewardSpends)Queue(next,"swapSpend",spend.spendId);
                Queue(next,"protection","lifecycle");Queue(next,"migration","baseline");Commit(next);
            }
            Notify();
        }
        public bool RecordFirstWin(string day)
        {
            Day(day);lock(serial){Flush();if(state.firstWinDays.Contains(day))return false;var next=Copy(state);next.firstWinDays.Add(day);Queue(next,"win",day);Commit(next);}
            Notify();return true;
        }
        public bool HasAppliedReward(string requestId){lock(serial)return Applied(state,requestId);}
        public ShareAvailability ReadShareAvailability(string day,DateTimeOffset utc)
        {lock(serial)return Availability(state,day,utc,reservedRequest!=null);}
        public bool TryReserveShare(string day,string requestId,DateTimeOffset utc)
        {
            Day(day);lock(serial){Flush();if(string.IsNullOrEmpty(requestId)||Applied(state,requestId)||!Availability(state,day,utc,reservedRequest!=null).Available)return false;
                reservedDay=day;reservedRequest=requestId;return true;}
        }
        public bool HasShareReservation(string day,string requestId)
        {lock(serial)return ReservationMatches(day,requestId)&&Availability(state,day,TimeSnapshot.Utc).Used<ShareAvailability.DailyLimit;}
        bool ReservationMatches(string day,string requestId)=>reservedRequest!=null&&reservedDay==day&&reservedRequest==requestId&&!Applied(state,requestId);
        public void ReleaseShare(string day,string requestId)
        {lock(serial){if(reservedDay==day&&reservedRequest==requestId){reservedDay=null;reservedRequest=null;}}}
        public bool TryCommitShare(string day,string requestId,DateTimeOffset utc)
        {return CommitReward(new AppliedRewardRecord{requestId=requestId,quotaDay=day,rewardKind=-1,route=(int)RewardRoute.SimulatedShare,effectiveUtc=Utc(utc),timeSource=TimeSnapshot.Source});}
        public bool TryCommitAppliedReward(RewardRequest request,DateTimeOffset utc)
        {return CommitReward(new AppliedRewardRecord{requestId=request.RequestId,quotaDay=request.QuotaChallengeDate,rewardKind=(int)request.Kind,route=(int)request.Route,effectiveUtc=Utc(utc),timeSource=TimeSnapshot.Source});}
        public AccountMutationResult RecordFormalResult(string resultId,string sessionId,string challengeDay,FormalChallengeResultKind result,DateTimeOffset completedUtc)
        {
            var fact=new FormalChallengeResultRecord{resultId=resultId,sessionId=sessionId,challengeDay=challengeDay,result=result,protectionVersion=1,completedUtc=Utc(completedUtc),timeSource=TimeSnapshot.Source};Validate(fact);
            lock(serial)
            {
                Flush();var prior=state.formalResults.Find(x=>x.resultId==resultId);
                if(prior!=null){fact.protectionVersion=prior.protectionVersion;fact.timeSource=prior.timeSource;if(!Same(prior,fact))throw new InvalidOperationException("Conflicting immutable formal result");return AccountMutationResult.Duplicate;}
                var next=Copy(state);next.formalResults.Add(fact);Queue(next,"formal",fact.resultId);Commit(next);
            }
            Notify();return AccountMutationResult.Applied;
        }
        public AccountMutationResult GrantSwapOrderRewards(string grantId,int amount,DateTimeOffset effectiveUtc)
        {
            var fact=new SwapOrderRewardGrantRecord{grantId=grantId,amount=amount,effectiveUtc=Utc(effectiveUtc),timeSource=TimeSnapshot.Source};Validate(fact);
            lock(serial)
            {
                Flush();var prior=state.swapOrderRewardGrants.Find(x=>x.grantId==grantId);
                if(prior!=null){if(!Same(prior,fact))throw new InvalidOperationException("Conflicting immutable swap-order grant");return AccountMutationResult.Duplicate;}
                var next=Copy(state);next.swapOrderRewardGrants.Add(fact);Queue(next,"swapGrant",fact.grantId);Commit(next);
            }
            Notify();return AccountMutationResult.Applied;
        }
        public AccountMutationResult ConsumeSwapOrderReward(string spendId,DateTimeOffset effectiveUtc)
        {
            Identity(spendId,nameof(spendId));
            lock(serial)
            {
                Flush();if(state.swapOrderRewardSpends.Any(x=>x.spendId==spendId))return AccountMutationResult.Duplicate;
                string credit=AvailableCredits(state).FirstOrDefault();if(credit==null)return AccountMutationResult.InsufficientInventory;
                var fact=new SwapOrderRewardSpendRecord{spendId=spendId,creditId=credit,effectiveUtc=Utc(effectiveUtc),timeSource=TimeSnapshot.Source};Validate(fact);
                var next=Copy(state);next.swapOrderRewardSpends.Add(fact);Queue(next,"swapSpend",fact.spendId);Commit(next);
            }
            Notify();return AccountMutationResult.Applied;
        }
        bool CommitReward(AppliedRewardRecord fact)
        {
            lock(serial)
            {
                Validate(fact);if(Applied(state,fact.requestId))return false;
                bool share=IsShare(fact.route);
                if(share&&!ReservationMatches(fact.quotaDay,fact.requestId))return false;
                var next=Copy(state);next.rewards.Add(fact);Queue(next,"reward",fact.requestId);
                // Retain a failed write in memory so retrying persistence never reapplies
                // the effect or changes its original effectiveUtc. No false completion.
                state=next;state.localRevision++;dirty=true;
                if(share){reservedDay=null;reservedRequest=null;}
                Flush();
            }
            Notify();return true;
        }
        public void Flush(){lock(serial){if(!dirty)return;persistence.Save(Copy(state));dirty=false;}}
        public void InvalidateSyncCallbacks(){lock(serial){generation++;syncing=false;reservedDay=null;reservedRequest=null;}}
        public async Task<ProfileSyncStatus> SyncAsync()
        {
            ProfileDocument upload;long lease;
            lock(serial){if(syncing)return ProfileSyncStatus.Busy;Flush();syncing=true;lease=generation;upload=Copy(state);}
            try
            {
                var response=await transport.SyncAsync(upload);
                lock(serial)
                {
                    if(lease!=generation)return ProfileSyncStatus.Stale;
                    if(response==null){clock.Offline();return ProfileSyncStatus.Failed;}
                    if(response.Status!=ProfileSyncStatus.Synced){clock.Offline();return response.Status;}
                    if(response.Snapshot==null)throw new InvalidOperationException("Successful sync omitted snapshot");
                    var merged=Merge(state,response.Snapshot);
                    var sent=new HashSet<string>(upload.pending.Select(p=>p.operationId),StringComparer.Ordinal);
                    var acknowledged=new HashSet<string>(response.AcknowledgedOperations??new string[0],StringComparer.Ordinal);
                    // Acknowledgement alone cannot erase an upload the server lost.
                    merged.pending.RemoveAll(p=>sent.Contains(p.operationId)&&acknowledged.Contains(p.operationId)&&ContainsOperation(response.Snapshot,p,upload));
                    merged.confirmationCursor=response.ConfirmationCursor;Commit(merged);
                    if(response.ServerUtc.HasValue)clock.Trust(response.ServerUtc.Value);else clock.Offline();
                }
                Notify();return ProfileSyncStatus.Synced;
            }
            catch{lock(serial){if(lease!=generation)return ProfileSyncStatus.Stale;clock.Offline();}return ProfileSyncStatus.Failed;}
            finally{lock(serial){if(lease==generation)syncing=false;}}
        }
        void Commit(ProfileDocument next){next.localRevision=state.localRevision+1;persistence.Save(Copy(next));state=next;}
        void Notify(){var handlers=ProfileChanged;if(handlers==null)return;foreach(Action handler in handlers.GetInvocationList())try{handler();}catch{/* Observers cannot undo a durable transaction. */}}
        static bool ContainsOperation(ProfileDocument remote,ProfileSyncOperation op,ProfileDocument sent)
        {
            if(op.kind=="protection")return LifecycleContains(remote,sent);
            if(op.kind=="win")return remote.firstWinDays.Contains(op.entityId);
            if(op.kind=="tutorial")return op.entityId=="warmup"?remote.warmupTutorialCompleted:op.entityId=="buffer"&&remote.bufferWarningCompleted;
            if(op.kind=="reward")return remote.rewards.Any(e=>e.requestId==op.entityId&&Same(e,sent.rewards.Single(x=>x.requestId==op.entityId)));
            if(op.kind=="formal")return remote.formalResults.Any(e=>e.resultId==op.entityId&&Same(e,sent.formalResults.Single(x=>x.resultId==op.entityId)));
            if(op.kind=="swapGrant")return remote.swapOrderRewardGrants.Any(e=>e.grantId==op.entityId&&Same(e,sent.swapOrderRewardGrants.Single(x=>x.grantId==op.entityId)));
            if(op.kind=="swapSpend")return remote.swapOrderRewardSpends.Any(e=>e.spendId==op.entityId&&Same(e,sent.swapOrderRewardSpends.Single(x=>x.spendId==op.entityId)));
            return op.kind=="migration"&&(!sent.orderProtectionInitialized||(remote.orderProtectionInitialized&&remote.orderProtectionInitialPercent<=sent.orderProtectionInitialPercent))&&(!sent.warmupTutorialCompleted||remote.warmupTutorialCompleted)&&(!sent.bufferWarningCompleted||remote.bufferWarningCompleted)&&sent.firstWinDays.All(remote.firstWinDays.Contains)&&sent.legacyRequestIds.All(remote.legacyRequestIds.Contains)&&
                sent.legacyQuotas.All(q=>remote.legacyQuotas.Any(r=>r.day==q.day&&r.used>=q.used&&(string.IsNullOrEmpty(q.lastEffectiveUtc)||Date(r.lastEffectiveUtc)>=Date(q.lastEffectiveUtc))));
        }
        public static ProfileDocument Merge(ProfileDocument local,ProfileDocument remote)
        {
            Validate(local,local.environment,local.account);Validate(remote,local.environment,local.account);var merged=Copy(local);
            MergeLifecycle(merged,remote);
            if(remote.orderProtectionInitialized){merged.orderProtectionInitialPercent=merged.orderProtectionInitialized?Math.Min(merged.orderProtectionInitialPercent,remote.orderProtectionInitialPercent):remote.orderProtectionInitialPercent;merged.orderProtectionInitialized=true;}
            merged.warmupTutorialCompleted|=remote.warmupTutorialCompleted;merged.bufferWarningCompleted|=remote.bufferWarningCompleted;
            foreach(var day in remote.firstWinDays)if(!merged.firstWinDays.Contains(day))merged.firstWinDays.Add(day);
            foreach(var id in remote.legacyRequestIds)if(!merged.legacyRequestIds.Contains(id))merged.legacyRequestIds.Add(id);
            foreach(var reward in remote.rewards)
            {
                var prior=merged.rewards.Find(e=>e.requestId==reward.requestId);
                if(prior==null)merged.rewards.Add(Clone(reward));
                else if(!Same(prior,reward))throw new InvalidOperationException("Conflicting immutable reward fact");
            }
            foreach(var q in remote.legacyQuotas)
            {var prior=merged.legacyQuotas.Find(e=>e.day==q.day);if(prior==null)merged.legacyQuotas.Add(Clone(q));else{prior.used=Math.Max(prior.used,q.used);prior.lastEffectiveUtc=Latest(prior.lastEffectiveUtc,q.lastEffectiveUtc);}}
            MergeImmutable(merged.formalResults,remote.formalResults,x=>x.resultId,Same,Clone,"formal result");
            MergeImmutable(merged.swapOrderRewardGrants,remote.swapOrderRewardGrants,x=>x.grantId,Same,Clone,"swap-order grant");
            MergeImmutable(merged.swapOrderRewardSpends,remote.swapOrderRewardSpends,x=>x.spendId,Same,Clone,"swap-order spend");
            merged.firstWinDays.Sort(StringComparer.Ordinal);merged.legacyRequestIds.Sort(StringComparer.Ordinal);merged.rewards.Sort((a,b)=>StringComparer.Ordinal.Compare(a.requestId,b.requestId));merged.legacyQuotas.Sort((a,b)=>StringComparer.Ordinal.Compare(a.day,b.day));
            merged.formalResults.Sort((a,b)=>StringComparer.Ordinal.Compare(a.resultId,b.resultId));merged.swapOrderRewardGrants.Sort((a,b)=>StringComparer.Ordinal.Compare(a.grantId,b.grantId));merged.swapOrderRewardSpends.Sort((a,b)=>StringComparer.Ordinal.Compare(a.spendId,b.spendId));Validate(merged,merged.environment,merged.account);
            return merged;
        }
        public static ShareAvailability Availability(ProfileDocument document,string day,DateTimeOffset utc,bool reserved=false)
        {
            // Preserve richer facts even when a migrated baseline already counted the ID.
            var legacy=document.legacyQuotas.Find(q=>q.day==day);var facts=document.rewards.Where(e=>e.quotaDay==day&&IsShare(e.route)&&!document.legacyRequestIds.Contains(e.requestId)).ToArray();
            int used=(legacy?.used??0)+facts.Length;
            return new ShareAvailability(day,used,null,reserved,utc);
        }
        public static bool IsShare(int route)=>RewardRoutes.IsShare((RewardRoute)route);
        static bool Applied(ProfileDocument value,string id)=>value.legacyRequestIds.Contains(id)||value.rewards.Any(e=>e.requestId==id);
        static IEnumerable<string> AvailableCredits(ProfileDocument value)
        {
            var spent=new HashSet<string>(value.swapOrderRewardSpends.Select(x=>x.creditId),StringComparer.Ordinal);
            return value.swapOrderRewardGrants.OrderBy(x=>x.effectiveUtc,StringComparer.Ordinal).ThenBy(x=>x.grantId,StringComparer.Ordinal)
                .SelectMany(x=>Enumerable.Range(0,x.amount).Select(i=>x.grantId+":"+i.ToString(CultureInfo.InvariantCulture))).Where(x=>!spent.Contains(x));
        }
        static int Inventory(ProfileDocument value)=>AvailableCredits(value).Count();
        public static int ProtectionPercent(ProfileDocument value)
        {
            int percent=value.orderProtectionInitialized?value.orderProtectionInitialPercent:0;
            foreach(var item in value.formalResults.Where(x=>x.protectionVersion==1).OrderBy(x=>x.completedUtc,StringComparer.Ordinal).ThenBy(x=>x.resultId,StringComparer.Ordinal))
                percent=Math.Max(0,Math.Min(20,percent+(item.result==FormalChallengeResultKind.Won?-5:3)));
            return percent;
        }
        static int CountConsecutiveFormalFailures(ProfileDocument value)
        {
            int count=0;foreach(var item in value.formalResults.OrderBy(x=>x.completedUtc,StringComparer.Ordinal).ThenBy(x=>x.resultId,StringComparer.Ordinal))count=item.result==FormalChallengeResultKind.Won?0:count+1;return count;
        }
        static void Queue(ProfileDocument value,string kind,string id)
        {string key=kind+":"+id;if(!value.pending.Any(p=>p.operationId==key))value.pending.Add(new ProfileSyncOperation{operationId=key,kind=kind,entityId=id});}
        static void Day(string value){if(!DateTime.TryParseExact(value,"yyyyMMdd",CultureInfo.InvariantCulture,DateTimeStyles.None,out _))throw new ArgumentException("Invalid challenge day");}
        static string Utc(DateTimeOffset utc)=>utc.ToUniversalTime().ToString("o",CultureInfo.InvariantCulture);
        static DateTimeOffset Date(string value)=>DateTimeOffset.ParseExact(value,"o",CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind).ToUniversalTime();
        static string Latest(string a,string b)=>string.IsNullOrEmpty(a)?b:string.IsNullOrEmpty(b)?a:Date(a)>=Date(b)?a:b;
        static void Validate(AppliedRewardRecord e)
        {if(e==null||string.IsNullOrEmpty(e.requestId)||(e.rewardKind!=-1&&!Enum.IsDefined(typeof(RewardKind),e.rewardKind))||!Enum.IsDefined(typeof(RewardRoute),e.route)||!Enum.IsDefined(typeof(ProfileTimeSource),e.timeSource))throw new ArgumentException("Invalid reward fact");Day(e.quotaDay);Date(e.effectiveUtc);}
        static void Validate(FormalChallengeResultRecord e)
        {if(e==null||e.protectionVersion<0||e.protectionVersion>2||!Enum.IsDefined(typeof(FormalChallengeResultKind),e.result)||!Enum.IsDefined(typeof(ProfileTimeSource),e.timeSource))throw new ArgumentException("Invalid formal result fact");Identity(e.resultId,"resultId");Identity(e.sessionId,"sessionId");Day(e.challengeDay);Date(e.completedUtc);}
        static void Validate(SwapOrderRewardGrantRecord e)
        {if(e==null||e.amount<1||e.amount>100||!Enum.IsDefined(typeof(ProfileTimeSource),e.timeSource))throw new ArgumentException("Invalid swap-order grant fact");Identity(e.grantId,"grantId",150);Date(e.effectiveUtc);}
        static void Validate(SwapOrderRewardSpendRecord e)
        {if(e==null||!Enum.IsDefined(typeof(ProfileTimeSource),e.timeSource))throw new ArgumentException("Invalid swap-order spend fact");Identity(e.spendId,"spendId");Identity(e.creditId,"creditId");Date(e.effectiveUtc);}
        static void Identity(string value,string name,int max=160)
        {if(string.IsNullOrWhiteSpace(value)||value.Length>max||value.Any(c=>c<32||c==127))throw new ArgumentException("Invalid identity",name);}
        public static void Validate(ProfileDocument value,string environment,string account)
        {
            if(value==null||value.schemaVersion!=1||value.environment!=environment||value.account!=account||value.firstWinDays==null||value.rewards==null||value.legacyQuotas==null||value.legacyRequestIds==null||value.formalResults==null||value.swapOrderRewardGrants==null||value.swapOrderRewardSpends==null||value.pending==null)throw new ArgumentException("Unsupported/corrupt profile or wrong partition");
            if((value.orderProtectionInitialPercent!=0&&value.orderProtectionInitialPercent!=20)||(!value.orderProtectionInitialized&&value.orderProtectionInitialPercent!=0))throw new ArgumentException("Invalid protection baseline");
            ValidateLifecycle(value);
            foreach(var day in value.firstWinDays)Day(day);foreach(var e in value.rewards)Validate(e);foreach(var e in value.formalResults)Validate(e);foreach(var e in value.swapOrderRewardGrants)Validate(e);foreach(var e in value.swapOrderRewardSpends)Validate(e);
            if(value.localRevision<0||value.firstWinDays.Distinct().Count()!=value.firstWinDays.Count||value.legacyRequestIds.Distinct().Count()!=value.legacyRequestIds.Count||value.rewards.Select(e=>e.requestId).Distinct().Count()!=value.rewards.Count||value.legacyQuotas.Select(q=>q.day).Distinct().Count()!=value.legacyQuotas.Count||value.formalResults.Select(e=>e.resultId).Distinct().Count()!=value.formalResults.Count||value.swapOrderRewardGrants.Select(e=>e.grantId).Distinct().Count()!=value.swapOrderRewardGrants.Count||value.swapOrderRewardSpends.Select(e=>e.spendId).Distinct().Count()!=value.swapOrderRewardSpends.Count||value.swapOrderRewardSpends.Select(e=>e.creditId).Distinct().Count()!=value.swapOrderRewardSpends.Count||value.legacyRequestIds.Any(string.IsNullOrEmpty))throw new ArgumentException("Duplicate/corrupt profile facts");
            var credits=new HashSet<string>(value.swapOrderRewardGrants.SelectMany(x=>Enumerable.Range(0,x.amount).Select(i=>x.grantId+":"+i.ToString(CultureInfo.InvariantCulture))),StringComparer.Ordinal);if(value.swapOrderRewardSpends.Any(x=>!credits.Contains(x.creditId)))throw new ArgumentException("Swap-order spend references an unknown credit");
            foreach(var q in value.legacyQuotas){Day(q.day);if(q.used<0)throw new ArgumentException("Negative quota");if(!string.IsNullOrEmpty(q.lastEffectiveUtc))Date(q.lastEffectiveUtc);}
            foreach(var p in value.pending)if(p==null||p.operationId!=p.kind+":"+p.entityId||(p.kind!="protection"&&p.kind!="win"&&p.kind!="reward"&&p.kind!="migration"&&p.kind!="tutorial"&&p.kind!="formal"&&p.kind!="swapGrant"&&p.kind!="swapSpend")||(p.kind=="tutorial"&&p.entityId!="warmup"&&p.entityId!="buffer"))throw new ArgumentException("Corrupt outbox");
        }
        public static ProfileDocument Copy(ProfileDocument value)=>new ProfileDocument{schemaVersion=value.schemaVersion,localRevision=value.localRevision,environment=value.environment,account=value.account,confirmationCursor=value.confirmationCursor,
            protectionLifecycleVersion=value.protectionLifecycleVersion,protectionLegacyPlayer=value.protectionLegacyPlayer,protectionEntries=new List<string>(value.protectionEntries??new List<string>()),protectionAttempts=(value.protectionAttempts??new List<ProtectionAttemptRecord>()).Select(Clone).ToList(),
            orderProtectionInitialized=value.orderProtectionInitialized,orderProtectionInitialPercent=value.orderProtectionInitialPercent,
            warmupTutorialCompleted=value.warmupTutorialCompleted,bufferWarningCompleted=value.bufferWarningCompleted,
            firstWinDays=new List<string>(value.firstWinDays),legacyRequestIds=new List<string>(value.legacyRequestIds),rewards=value.rewards.Select(Clone).ToList(),legacyQuotas=value.legacyQuotas.Select(Clone).ToList(),formalResults=value.formalResults.Select(Clone).ToList(),swapOrderRewardGrants=value.swapOrderRewardGrants.Select(Clone).ToList(),swapOrderRewardSpends=value.swapOrderRewardSpends.Select(Clone).ToList(),pending=value.pending.Select(p=>new ProfileSyncOperation{operationId=p.operationId,kind=p.kind,entityId=p.entityId}).ToList()};
        static ShareQuotaRecord Clone(ShareQuotaRecord q)=>new ShareQuotaRecord{day=q.day,used=q.used,lastEffectiveUtc=q.lastEffectiveUtc,committedRequestId=q.committedRequestId,dataVersion=q.dataVersion};
        static AppliedRewardRecord Clone(AppliedRewardRecord e)=>new AppliedRewardRecord{requestId=e.requestId,quotaDay=e.quotaDay,effectiveUtc=e.effectiveUtc,rewardKind=e.rewardKind,route=e.route,timeSource=e.timeSource};
        static FormalChallengeResultRecord Clone(FormalChallengeResultRecord e)=>new FormalChallengeResultRecord{resultId=e.resultId,sessionId=e.sessionId,challengeDay=e.challengeDay,completedUtc=e.completedUtc,result=e.result,protectionAttemptId=e.protectionAttemptId,protectionVersion=e.protectionVersion,timeSource=e.timeSource};
        static SwapOrderRewardGrantRecord Clone(SwapOrderRewardGrantRecord e)=>new SwapOrderRewardGrantRecord{grantId=e.grantId,effectiveUtc=e.effectiveUtc,amount=e.amount,timeSource=e.timeSource};
        static SwapOrderRewardSpendRecord Clone(SwapOrderRewardSpendRecord e)=>new SwapOrderRewardSpendRecord{spendId=e.spendId,creditId=e.creditId,effectiveUtc=e.effectiveUtc,timeSource=e.timeSource};
        static bool Same(AppliedRewardRecord a,AppliedRewardRecord b)=>a.requestId==b.requestId&&a.quotaDay==b.quotaDay&&a.effectiveUtc==b.effectiveUtc&&a.rewardKind==b.rewardKind&&a.route==b.route&&a.timeSource==b.timeSource;
        static bool Same(FormalChallengeResultRecord a,FormalChallengeResultRecord b)=>a.resultId==b.resultId&&a.sessionId==b.sessionId&&a.challengeDay==b.challengeDay&&a.completedUtc==b.completedUtc&&a.result==b.result&&(a.protectionAttemptId??"")==(b.protectionAttemptId??"")&&a.protectionVersion==b.protectionVersion&&a.timeSource==b.timeSource;
        static bool Same(SwapOrderRewardGrantRecord a,SwapOrderRewardGrantRecord b)=>a.grantId==b.grantId&&a.effectiveUtc==b.effectiveUtc&&a.amount==b.amount&&a.timeSource==b.timeSource;
        static bool Same(SwapOrderRewardSpendRecord a,SwapOrderRewardSpendRecord b)=>a.spendId==b.spendId&&a.creditId==b.creditId&&a.effectiveUtc==b.effectiveUtc&&a.timeSource==b.timeSource;
        static void MergeImmutable<T>(List<T> target,IEnumerable<T> source,Func<T,string> id,Func<T,T,bool> same,Func<T,T> clone,string label)
            where T:class
        {foreach(var item in source){var prior=target.Find(x=>id(x)==id(item));if(prior==null)target.Add(clone(item));else if(!same(prior,item))throw new InvalidOperationException("Conflicting immutable "+label);}}
    }
}
