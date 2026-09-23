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
    public sealed class ProfileStore:IShareQuotaStore,IAsyncProfileStore,IAppliedRewardStore
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
            if(loaded==null){Queue(state,"migration","baseline");persistence.Save(Copy(state));}
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
                Queue(next,"migration","baseline");Commit(next);
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
        {lock(serial)return ReservationMatches(day,requestId)&&Availability(state,day,TimeSnapshot.Utc).Used<3;}
        bool ReservationMatches(string day,string requestId)=>reservedRequest!=null&&reservedDay==day&&reservedRequest==requestId&&!Applied(state,requestId);
        public void ReleaseShare(string day,string requestId)
        {lock(serial){if(reservedDay==day&&reservedRequest==requestId){reservedDay=null;reservedRequest=null;}}}
        public bool TryCommitShare(string day,string requestId,DateTimeOffset utc)
        {return CommitReward(new AppliedRewardRecord{requestId=requestId,quotaDay=day,rewardKind=-1,route=(int)RewardRoute.SimulatedShare,effectiveUtc=Utc(utc),timeSource=TimeSnapshot.Source});}
        public bool TryCommitAppliedReward(RewardRequest request,DateTimeOffset utc)
        {return CommitReward(new AppliedRewardRecord{requestId=request.RequestId,quotaDay=request.QuotaChallengeDate,rewardKind=(int)request.Kind,route=(int)request.Route,effectiveUtc=Utc(utc),timeSource=TimeSnapshot.Source});}
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
            if(op.kind=="win")return remote.firstWinDays.Contains(op.entityId);
            if(op.kind=="reward")return remote.rewards.Any(e=>e.requestId==op.entityId&&Same(e,sent.rewards.Single(x=>x.requestId==op.entityId)));
            return op.kind=="migration"&&sent.firstWinDays.All(remote.firstWinDays.Contains)&&sent.legacyRequestIds.All(remote.legacyRequestIds.Contains)&&
                sent.legacyQuotas.All(q=>remote.legacyQuotas.Any(r=>r.day==q.day&&r.used>=q.used&&(string.IsNullOrEmpty(q.lastEffectiveUtc)||Date(r.lastEffectiveUtc)>=Date(q.lastEffectiveUtc))));
        }
        public static ProfileDocument Merge(ProfileDocument local,ProfileDocument remote)
        {
            Validate(local,local.environment,local.account);Validate(remote,local.environment,local.account);var merged=Copy(local);
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
            merged.firstWinDays.Sort(StringComparer.Ordinal);merged.legacyRequestIds.Sort(StringComparer.Ordinal);merged.rewards.Sort((a,b)=>StringComparer.Ordinal.Compare(a.requestId,b.requestId));merged.legacyQuotas.Sort((a,b)=>StringComparer.Ordinal.Compare(a.day,b.day));
            return merged;
        }
        public static ShareAvailability Availability(ProfileDocument document,string day,DateTimeOffset utc,bool reserved=false)
        {
            // Preserve richer facts even when a migrated baseline already counted the ID.
            var legacy=document.legacyQuotas.Find(q=>q.day==day);var facts=document.rewards.Where(e=>e.quotaDay==day&&IsShare(e.route)&&!document.legacyRequestIds.Contains(e.requestId)).ToArray();
            int used=(legacy?.used??0)+facts.Length;string latest=legacy?.lastEffectiveUtc;
            foreach(var fact in facts)latest=Latest(latest,fact.effectiveUtc);
            DateTimeOffset? next=null;if(used>0&&used<3&&!string.IsNullOrEmpty(latest))next=Date(latest).AddMinutes(used==1?5:15);
            return new ShareAvailability(day,used,next,reserved,utc);
        }
        public static bool IsShare(int route)=>RewardRoutes.IsShare((RewardRoute)route);
        static bool Applied(ProfileDocument value,string id)=>value.legacyRequestIds.Contains(id)||value.rewards.Any(e=>e.requestId==id);
        static void Queue(ProfileDocument value,string kind,string id)
        {string key=kind+":"+id;if(!value.pending.Any(p=>p.operationId==key))value.pending.Add(new ProfileSyncOperation{operationId=key,kind=kind,entityId=id});}
        static void Day(string value){if(!DateTime.TryParseExact(value,"yyyyMMdd",CultureInfo.InvariantCulture,DateTimeStyles.None,out _))throw new ArgumentException("Invalid challenge day");}
        static string Utc(DateTimeOffset utc)=>utc.ToUniversalTime().ToString("o",CultureInfo.InvariantCulture);
        static DateTimeOffset Date(string value)=>DateTimeOffset.ParseExact(value,"o",CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind).ToUniversalTime();
        static string Latest(string a,string b)=>string.IsNullOrEmpty(a)?b:string.IsNullOrEmpty(b)?a:Date(a)>=Date(b)?a:b;
        static void Validate(AppliedRewardRecord e)
        {if(e==null||string.IsNullOrEmpty(e.requestId)||e.rewardKind< -1||e.rewardKind>(int)RewardKind.Revival||!Enum.IsDefined(typeof(RewardRoute),e.route)||!Enum.IsDefined(typeof(ProfileTimeSource),e.timeSource))throw new ArgumentException("Invalid reward fact");Day(e.quotaDay);Date(e.effectiveUtc);}
        public static void Validate(ProfileDocument value,string environment,string account)
        {
            if(value==null||value.schemaVersion!=1||value.environment!=environment||value.account!=account||value.firstWinDays==null||value.rewards==null||value.legacyQuotas==null||value.legacyRequestIds==null||value.pending==null)throw new ArgumentException("Unsupported/corrupt profile or wrong partition");
            foreach(var day in value.firstWinDays)Day(day);foreach(var e in value.rewards)Validate(e);
            if(value.localRevision<0||value.firstWinDays.Distinct().Count()!=value.firstWinDays.Count||value.legacyRequestIds.Distinct().Count()!=value.legacyRequestIds.Count||value.rewards.Select(e=>e.requestId).Distinct().Count()!=value.rewards.Count||value.legacyQuotas.Select(q=>q.day).Distinct().Count()!=value.legacyQuotas.Count||value.legacyRequestIds.Any(string.IsNullOrEmpty))throw new ArgumentException("Duplicate/corrupt profile facts");
            foreach(var q in value.legacyQuotas){Day(q.day);if(q.used<0)throw new ArgumentException("Negative quota");if(!string.IsNullOrEmpty(q.lastEffectiveUtc))Date(q.lastEffectiveUtc);}
            foreach(var p in value.pending)if(p==null||p.operationId!=p.kind+":"+p.entityId||(p.kind!="win"&&p.kind!="reward"&&p.kind!="migration"))throw new ArgumentException("Corrupt outbox");
        }
        public static ProfileDocument Copy(ProfileDocument value)=>new ProfileDocument{schemaVersion=value.schemaVersion,localRevision=value.localRevision,environment=value.environment,account=value.account,confirmationCursor=value.confirmationCursor,
            firstWinDays=new List<string>(value.firstWinDays),legacyRequestIds=new List<string>(value.legacyRequestIds),rewards=value.rewards.Select(Clone).ToList(),legacyQuotas=value.legacyQuotas.Select(Clone).ToList(),pending=value.pending.Select(p=>new ProfileSyncOperation{operationId=p.operationId,kind=p.kind,entityId=p.entityId}).ToList()};
        static ShareQuotaRecord Clone(ShareQuotaRecord q)=>new ShareQuotaRecord{day=q.day,used=q.used,lastEffectiveUtc=q.lastEffectiveUtc,committedRequestId=q.committedRequestId,dataVersion=q.dataVersion};
        static AppliedRewardRecord Clone(AppliedRewardRecord e)=>new AppliedRewardRecord{requestId=e.requestId,quotaDay=e.quotaDay,effectiveUtc=e.effectiveUtc,rewardKind=e.rewardKind,route=e.route,timeSource=e.timeSource};
        static bool Same(AppliedRewardRecord a,AppliedRewardRecord b)=>a.requestId==b.requestId&&a.quotaDay==b.quotaDay&&a.effectiveUtc==b.effectiveUtc&&a.rewardKind==b.rewardKind&&a.route==b.route&&a.timeSource==b.timeSource;
    }
}
