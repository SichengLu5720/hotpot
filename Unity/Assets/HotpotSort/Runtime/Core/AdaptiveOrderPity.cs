using System;
using System.Collections.Generic;
using System.Globalization;
using HotpotSort.Contracts;
using HotpotSort.Determinism;

namespace HotpotSort.Core
{
    public static class AdaptiveOrderPity
    {
        public static int TakeoverPercent(int consecutiveFormalFailures)
            => DailyDirector.SupportTakeoverPercent(consecutiveFormalFailures);

        public static AccountMutationResult? RecordFormalResult(IAccountRewardProfileStore profile,ChallengeStage stage,GameSnapshot snapshot,DateTimeOffset completedUtc,string attemptId=null)
        {
            if(profile==null)throw new ArgumentNullException(nameof(profile));
            if(snapshot==null)throw new ArgumentNullException(nameof(snapshot));
            if(stage!=ChallengeStage.Formal||snapshot.Status!=GameStatus.Won&&snapshot.Status!=GameStatus.Failed)return null;
            FormalChallengeResultKind result;
            if(snapshot.Status==GameStatus.Won)result=FormalChallengeResultKind.Won;
            else
            {
                var state=CanonicalJson.Map(CanonicalJson.Parse(snapshot.CanonicalStateJson));
                var statistics=CanonicalJson.Map(state["statistics"]);
                string failure=statistics["failureCode"] as string;
                if(failure=="BufferOverflow")result=FormalChallengeResultKind.FailedBufferFull;
                else if(failure=="Timeout")result=FormalChallengeResultKind.FailedTimeout;
                else return null;
            }
            if(attemptId!=null&&profile is IOrderProtectionLifecycleStore lifecycle)
                return lifecycle.RecordProtectedFormalResult(attemptId,"formal-result:"+snapshot.SessionId,snapshot.SessionId,ChallengeDay(snapshot.Challenge.ChallengeId),result,completedUtc);
            return profile.RecordFormalResult("formal-result:"+snapshot.SessionId,snapshot.SessionId,ChallengeDay(snapshot.Challenge.ChallengeId),result,completedUtc);
        }

        static string ChallengeDay(string value)
        {
            DateTime parsed;
            if(DateTime.TryParseExact(value,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out parsed)||
               DateTime.TryParseExact(value,"yyyyMMdd",CultureInfo.InvariantCulture,DateTimeStyles.None,out parsed))
                return parsed.ToString("yyyyMMdd",CultureInfo.InvariantCulture);
            throw new ArgumentException("Challenge identity is not a challenge day",nameof(value));
        }
    }

    // Lifecycle profiles freeze at formal start; historical adapters retain their old run boundary.
    public sealed class AdaptiveOrderPityFactory : IGameSessionFactory,IChallengeStageFactory
    {
        readonly object gate=new object();
        readonly DailySessionFactory core;
        readonly IAccountRewardProfileStore profile;
        readonly Func<DateTimeOffset> utcNow;
        readonly Dictionary<string,int> frozenByRun=new Dictionary<string,int>(StringComparer.Ordinal);
        readonly Dictionary<string,DailySession> warmupByRun=new Dictionary<string,DailySession>(StringComparer.Ordinal);
        public Exception LastRecordError { get; private set; }

        public AdaptiveOrderPityFactory(DailySessionFactory core,IAccountRewardProfileStore profile,Func<DateTimeOffset> utcNow=null)
        {
            this.core=core??throw new ArgumentNullException(nameof(core));
            this.profile=profile??throw new ArgumentNullException(nameof(profile));
            this.utcNow=utcNow??(()=>DateTimeOffset.UtcNow);
        }

        public IGameSession CreateSession(ChallengeContext context)
            => CreateStage(context,ChallengeStage.Formal,0);

        public IGameSession CreateStage(ChallengeContext context,ChallengeStage stage,int inheritedPotMask)
        {
            if(context==null)throw new ArgumentNullException(nameof(context));
            int failures,toolMask=0;string key=RunKey(context);
            lock(gate)
            {
                if(stage==ChallengeStage.Warmup)
                {
                    failures=(profile as IOrderProtectionProfileStore)?.OrderProtectionPercent??0;
                    frozenByRun[key]=failures;
                }
                else if(stage==ChallengeStage.Formal&&frozenByRun.TryGetValue(key,out failures))frozenByRun.Remove(key);
                else failures=(profile as IOrderProtectionProfileStore)?.OrderProtectionPercent??0;
            }
            lock(gate)
            {
                DailySession warmup;
                if(stage==ChallengeStage.Formal&&warmupByRun.TryGetValue(key,out warmup)){toolMask=warmup.UsedToolMask;warmupByRun.Remove(key);}
                if(stage==ChallengeStage.Warmup)warmupByRun.Remove(key);
            }
            string attemptId=null;
            var lifecycle=profile as IOrderProtectionLifecycleStore;
            if(lifecycle!=null&&stage==ChallengeStage.Formal){attemptId=Guid.NewGuid().ToString("N");failures=lifecycle.BeginFormalChallenge(attemptId,utcNow());}
            var session=lifecycle!=null?core.CreateStageWithLifecycleProtection(context,stage,inheritedPotMask,failures,toolMask):core.CreateStageWithProtection(context,stage,inheritedPotMask,failures,toolMask);
            if(stage==ChallengeStage.Warmup)lock(gate)warmupByRun[key]=session;
            if(stage==ChallengeStage.Formal)session.Changed+=(snapshot,events)=>Record(snapshot,attemptId);
            return session;
        }

        void Record(GameSnapshot snapshot,string attemptId)
        {
            try
            {
                if(AdaptiveOrderPity.RecordFormalResult(profile,ChallengeStage.Formal,snapshot,utcNow(),attemptId).HasValue)LastRecordError=null;
            }
            catch(Exception error)
            {
                LastRecordError=error;
            }
        }

        static string RunKey(ChallengeContext context)
            => context.ChallengeId+"\n"+context.ContentVersion+"\n"+context.ConfigurationDigest+"\n"+context.TimeSource+"\n"+context.RetryIndex.ToString(CultureInfo.InvariantCulture);
    }
}
