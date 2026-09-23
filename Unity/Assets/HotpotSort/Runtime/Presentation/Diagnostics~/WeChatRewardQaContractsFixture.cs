// Isolated compile fixture ONLY: opt in with -p:IsolatedContracts=true until core lands enum additions.
// Default QA project always consumes the real shared contracts.
using System;
using System.Threading.Tasks;
namespace HotpotSort.Contracts
{
    public enum RewardKind { Hint, ClearBuffer, Shuffle, FourthPot, Revival }
    public enum RewardRoute { SimulatedAd, SimulatedShare, WeChatShare, WeChatRewardedVideo }
    public enum RewardOutcome { Success, Cancelled, Failed, Unavailable }
    public sealed class RewardRequest
    {
        public string RequestId { get; }=Guid.NewGuid().ToString("N");
        public RewardKind Kind { get; }
        public RewardRoute Route { get; }
        public RewardRequest(long generation,RewardKind kind,RewardRoute route,string day,string sessionId=null,string revivalOfferId=null){Kind=kind;Route=route;}
    }
    public interface IRewardService { bool IsDevelopmentSimulation { get; } Task<RewardOutcome> RequestAsync(RewardRequest request); }
    public interface IThemeShare { bool IsDevelopmentSimulation { get; } Task<RewardOutcome> ShareThemeAsync(string resourceAddress); }
}
