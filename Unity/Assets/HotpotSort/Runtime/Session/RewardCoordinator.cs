using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotpotSort.Contracts;

namespace HotpotSort.Session
{
    public sealed class RewardCoordinator
    {
        readonly IRewardService service;
        readonly IProfileStore profile;
        readonly HashSet<string> completed=new HashSet<string>(StringComparer.Ordinal);
        bool busy;
        public RewardCoordinator(IRewardService service,IProfileStore profile){this.service=service;this.profile=profile;}
        public async Task<bool> RequestAsync(RewardRequest request,Func<long> generation,Func<bool> hasTarget,Func<bool> apply,Action<bool> pause)
        {
            if(busy || request.SessionGeneration!=generation() || !hasTarget() || completed.Contains(request.RequestId))return false;
            if(request.Route==RewardRoute.SimulatedShare && (request.Kind==RewardKind.FourthPot || profile.SharesUsed(request.QuotaChallengeDate)>=3))return false;
            busy=true;pause(true);
            try
            {
                var outcome=await service.RequestAsync(request);
                if(outcome!=RewardOutcome.Success || request.SessionGeneration!=generation() || !completed.Add(request.RequestId))return false;
                // Restore only the reward pause. User/background pause reasons remain intact.
                pause(false);
                if(!hasTarget() || !apply())return false;
                if(request.Route==RewardRoute.SimulatedShare && !profile.TryConsumeShare(request.QuotaChallengeDate,request.RequestId))
                    throw new InvalidOperationException("Share quota changed during an exclusive local reward request");
                return true;
            }
            finally { if(request.SessionGeneration==generation())pause(false);busy=false; }
        }
    }
}
