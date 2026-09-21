using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotpotSort.Contracts
{
    public enum RewardKind { Hint, ClearBuffer, Shuffle, FourthPot }
    public enum RewardRoute { SimulatedAd, SimulatedShare }
    public enum RewardOutcome { Success, Cancelled, Failed }
    public sealed class RewardRequest
    {
        public string RequestId { get; } = Guid.NewGuid().ToString("N");
        public long SessionGeneration { get; }
        public RewardKind Kind { get; }
        public RewardRoute Route { get; }
        public string QuotaChallengeDate { get; }
        public RewardRequest(long generation,RewardKind kind,RewardRoute route,string day)
        {SessionGeneration=generation;Kind=kind;Route=route;QuotaChallengeDate=day;}
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
    public sealed class FriendRecord { public string DisplayName;public int FirstWins; }
    public interface IFriendBoard { bool IsDevelopmentSimulation { get; } Task<IReadOnlyList<FriendRecord>> LoadAsync(); }
    public interface IThemeShare { bool IsDevelopmentSimulation { get; } Task<RewardOutcome> ShareThemeAsync(string resourceAddress); }
}
