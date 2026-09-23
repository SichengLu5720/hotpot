using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotpotSort.Contracts
{
    public enum RewardKind { Hint, ClearBuffer, Shuffle, FourthPot, Revival }
    public enum RewardRoute { SimulatedAd, SimulatedShare, WeChatShare, WeChatRewardedVideo }
    public static class RewardRoutes
    {
        public static bool IsShare(RewardRoute route)=>route==RewardRoute.SimulatedShare||route==RewardRoute.WeChatShare;
        public static bool IsVideo(RewardRoute route)=>route==RewardRoute.SimulatedAd||route==RewardRoute.WeChatRewardedVideo;
        public static RewardRoute ForService(RewardRoute selected,bool simulation)
        {
            if(IsShare(selected))return simulation?RewardRoute.SimulatedShare:RewardRoute.WeChatShare;
            if(IsVideo(selected))return simulation?RewardRoute.SimulatedAd:RewardRoute.WeChatRewardedVideo;
            throw new ArgumentOutOfRangeException(nameof(selected));
        }
    }
    public enum RewardOutcome { Success, Cancelled, Failed, Unavailable }
    public enum RewardApplicationResult { Applied, Cancelled, Failed, Unavailable, Stale, Duplicate }
    public sealed class RewardRequest
    {
        public string RequestId { get; } = Guid.NewGuid().ToString("N");
        public long SessionGeneration { get; }
        public RewardKind Kind { get; }
        public RewardRoute Route { get; }
        public string QuotaChallengeDate { get; }
        public string SessionId { get; }
        public string RevivalOfferId { get; }
        public RewardRequest(long generation,RewardKind kind,RewardRoute route,string day,string sessionId=null,string revivalOfferId=null)
        {SessionGeneration=generation;Kind=kind;Route=route;QuotaChallengeDate=day;SessionId=sessionId;RevivalOfferId=revivalOfferId;}
    }
    public interface IRewardService { bool IsDevelopmentSimulation { get; } Task<RewardOutcome> RequestAsync(RewardRequest request); }
    public sealed class PlayerSettings
    {
        public bool MusicEnabled=true,EffectsEnabled=true;
        public float MusicVolume=.6f,EffectsVolume=.8f;
    }
    public interface IProfileStore
    {
        bool IsDevelopmentSimulation { get; }
        int TotalFirstWins { get; }
        bool RecordFirstWin(string challengeDay);
        int SharesUsed(string challengeDay);
        bool TryConsumeShare(string challengeDay,string requestId);
        PlayerSettings LoadSettings();
        void SaveSettings(PlayerSettings value);
    }
    // Optional extension leaves existing profile implementations source compatible.
    // Production development storage implements this extension to persist UTC cooldowns.
    [Serializable] public sealed class ShareQuotaRecord
    {
        public string day, lastEffectiveUtc, committedRequestId;
        public int used, dataVersion;
    }
    public sealed class ShareAvailability
    {
        public string Day { get; }
        public int Used { get; }
        public int Remaining => Math.Max(0,3-Used);
        public DateTimeOffset? NextAvailableUtc { get; }
        public bool Reserved { get; }
        public bool Available { get; }
        public ShareAvailability(string day,int used,DateTimeOffset? next,bool reserved,DateTimeOffset now)
        {Day=day;Used=used;NextAvailableUtc=next;Reserved=reserved;Available=used<3&&!reserved&&(!next.HasValue||now>=next.Value);}
    }
    public interface IShareQuotaStore
    {
        ShareAvailability ReadShareAvailability(string day,DateTimeOffset utc);
        bool TryReserveShare(string day,string requestId,DateTimeOffset utc);
        bool HasShareReservation(string day,string requestId);
        bool TryCommitShare(string day,string requestId,DateTimeOffset effectiveUtc);
        void ReleaseShare(string day,string requestId);
    }
    public enum ProfileTimeSource { DeviceTime, TrustedServer }
    public enum ProfileSyncStatus { Synced, NotConfigured, Offline, Failed, Stale, Busy, Unavailable }
    public sealed class ProfileTimeSnapshot
    {
        public DateTimeOffset Utc { get; }
        public ProfileTimeSource Source { get; }
        public ProfileTimeSnapshot(DateTimeOffset utc,ProfileTimeSource source){Utc=utc.ToUniversalTime();Source=source;}
    }
    [Serializable] public sealed class AppliedRewardRecord
    {
        public string requestId,quotaDay,effectiveUtc;
        // -1 is an explicitly unknown kind migrated through the old share-only API.
        public int rewardKind,route;
        public ProfileTimeSource timeSource;
    }
    [Serializable] public sealed class ProfileSyncOperation { public string operationId,kind,entityId; }
    [Serializable] public sealed class ProfileDocument
    {
        public int schemaVersion=1;
        public long localRevision;
        public string environment,account,confirmationCursor;
        public List<string> firstWinDays=new List<string>(),legacyRequestIds=new List<string>();
        public List<AppliedRewardRecord> rewards=new List<AppliedRewardRecord>();
        public List<ShareQuotaRecord> legacyQuotas=new List<ShareQuotaRecord>();
        public List<ProfileSyncOperation> pending=new List<ProfileSyncOperation>();
    }
    public sealed class ProfileSyncResponse
    {
        public ProfileSyncStatus Status;
        public ProfileDocument Snapshot;
        public string ConfirmationCursor;
        public string[] AcknowledgedOperations=new string[0];
        public DateTimeOffset? ServerUtc;
    }
    public interface IProfileSyncTransport
    { Task<ProfileSyncResponse> SyncAsync(ProfileDocument upload); }
    public interface IAsyncProfileStore
    {
        ProfileDocument ReadSnapshot();
        Task<ProfileSyncStatus> SyncAsync();
        event Action ProfileChanged;
        ProfileTimeSnapshot TimeSnapshot { get; }
        void InvalidateSyncCallbacks();
    }
    // The coordinator invokes this only after the synchronous effect has applied.
    // Implementations durably commit the fact and outbox together before returning true.
    public interface IAppliedRewardStore
    {
        bool HasAppliedReward(string requestId);
        bool TryCommitAppliedReward(RewardRequest request,DateTimeOffset effectiveUtc);
    }
    // Session observes this contract without depending on Core or presentation assemblies.
    public interface IRevivalSessionState
    {
        bool RevivalPending { get; }
        bool RevivalUsed { get; }
        string RevivalOfferId { get; }
        string RevivalTransferToken { get; }
    }
    public sealed class FriendRecord { public string DisplayName;public int FirstWins; }
    public interface IFriendBoard { bool IsDevelopmentSimulation { get; } Task<IReadOnlyList<FriendRecord>> LoadAsync(); }
    public interface IThemeShare { bool IsDevelopmentSimulation { get; } Task<RewardOutcome> ShareThemeAsync(string resourceAddress); }
}
