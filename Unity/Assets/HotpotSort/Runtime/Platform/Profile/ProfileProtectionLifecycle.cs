using System;
using System.Linq;
using System.Collections.Generic;
using HotpotSort.Contracts;
namespace HotpotSort.Profile
{
    public sealed partial class ProfileStore
    {
        public void RecordGameEntry(DateTimeOffset utc)
        {
            string stamp=Utc(utc);
            lock(serial){Flush();if(state.protectionEntries.Contains(stamp))return;var next=Copy(state);next.protectionEntries.Add(stamp);next.protectionEntries.Sort(StringComparer.Ordinal);Queue(next,"protection","lifecycle");Commit(next);}Notify();
        }
        public int BeginFormalChallenge(string attemptId,DateTimeOffset utc)
        {
            Identity(attemptId,"attemptId");int percent;
            lock(serial)
            {
                Flush();var prior=state.protectionAttempts.Find(x=>x.attemptId==attemptId);if(prior!=null)return prior.frozenPercent;
                var next=Copy(state);string cycle=ActiveCycle(next,utc);
                string pending=ReturnCycle(next,utc);
                if(pending!=null&&!next.protectionAttempts.Any(x=>x.cycleId==pending))cycle=pending;
                if(cycle==null&&!next.protectionLegacyPlayer&&next.protectionLifecycleVersion>0)cycle="new";
                bool starting=cycle!=null&&!next.protectionAttempts.Any(x=>x.cycleId==cycle);
                percent=starting?60:ProtectionAt(next,utc);
                next.protectionAttempts.Add(new ProtectionAttemptRecord{attemptId=attemptId,startedUtc=Utc(utc),cycleId=cycle??"",frozenPercent=percent});
                Queue(next,"protection","lifecycle");Commit(next);
            }
            Notify();return percent;
        }
        public AccountMutationResult RecordProtectedFormalResult(string attemptId,string resultId,string sessionId,string challengeDay,FormalChallengeResultKind result,DateTimeOffset completedUtc)
        {
            var fact=new FormalChallengeResultRecord{resultId=resultId,sessionId=sessionId,challengeDay=challengeDay,result=result,protectionVersion=2,protectionAttemptId=attemptId,completedUtc=Utc(completedUtc),timeSource=TimeSnapshot.Source};Validate(fact);
            lock(serial)
            {
                Flush();var attempt=state.protectionAttempts.Find(x=>x.attemptId==attemptId);if(attempt==null||completedUtc<Date(attempt.startedUtc))throw new ArgumentException("Missing attempt or result precedes start");
                var prior=state.formalResults.Find(x=>x.resultId==resultId||x.protectionVersion==2&&x.protectionAttemptId==attemptId);
                if(prior!=null){fact.timeSource=prior.timeSource;if(!Same(prior,fact))throw new InvalidOperationException("Conflicting immutable protected result");return AccountMutationResult.Duplicate;}
                var next=Copy(state);next.formalResults.Add(fact);Queue(next,"formal",resultId);Queue(next,"protection","lifecycle");Commit(next);
            }
            Notify();return AccountMutationResult.Applied;
        }
        static string ReturnCycle(ProfileDocument d,DateTimeOffset at)
        {
            DateTimeOffset? previous=null;string result=null;
            foreach(var stamp in d.protectionEntries.OrderBy(x=>x,StringComparer.Ordinal))
            {var time=Date(stamp);if(time>at)continue;if(previous.HasValue&&(time-previous.Value).TotalDays>=14)result="return:"+stamp;previous=time;}
            return result;
        }
        static string ActiveCycle(ProfileDocument d,DateTimeOffset at)=>d.protectionAttempts.Where(x=>!string.IsNullOrEmpty(x.cycleId)&&(!d.protectionLegacyPlayer||x.cycleId!="new")&&Date(x.startedUtc)<=at).GroupBy(x=>x.cycleId)
            .Select(g=>new{Id=g.Key,Start=g.Min(x=>Date(x.startedUtc))}).OrderBy(x=>x.Start).ThenBy(x=>x.Id,StringComparer.Ordinal).Select(x=>x.Id).LastOrDefault();
        static int Cap(DateTimeOffset start,DateTimeOffset at){int day=(int)Math.Max(0,(at-start).TotalDays);return new[]{60,50,40,35,30,25,20}[Math.Min(6,day)];}
        public static int ProtectionAt(ProfileDocument d,DateTimeOffset at)
        {
            string cycle=ActiveCycle(d,at);bool first=true;DateTimeOffset start=DateTimeOffset.MinValue;
            int p=d.orderProtectionInitialized?d.orderProtectionInitialPercent:0;
            if(cycle!=null){start=d.protectionAttempts.Where(x=>x.cycleId==cycle).Min(x=>Date(x.startedUtc));p=60;}
            var attempts=d.protectionAttempts.ToDictionary(x=>x.attemptId,StringComparer.Ordinal);
            foreach(var r in d.formalResults.Where(x=>x.protectionVersion>0&&Date(x.completedUtc)<=at).OrderBy(x=>Date(x.completedUtc)).ThenBy(x=>x.resultId,StringComparer.Ordinal))
            {
                string resultCycle=null;
                if(r.protectionVersion==2){if(!attempts.TryGetValue(r.protectionAttemptId??"",out var a))continue;resultCycle=string.IsNullOrEmpty(a.cycleId)?null:a.cycleId;}
                if(resultCycle!=cycle)continue;
                var time=Date(r.completedUtc);int cap=cycle==null?20:Cap(start,time);
                if(!first||cycle==null)p=Math.Min(p,cap);
                p=Math.Max(0,Math.Min(cap,p+(r.result==FormalChallengeResultKind.Won?-5:3)));first=false;
            }
            if(cycle!=null&&first)return 60;
            return Math.Min(p,cycle==null?20:Cap(start,at));
        }
        static ProtectionAttemptRecord Clone(ProtectionAttemptRecord a)=>new ProtectionAttemptRecord{attemptId=a.attemptId,startedUtc=a.startedUtc,cycleId=a.cycleId,frozenPercent=a.frozenPercent};
        static bool Same(ProtectionAttemptRecord a,ProtectionAttemptRecord b)=>a.attemptId==b.attemptId&&a.startedUtc==b.startedUtc&&(a.cycleId??"")==(b.cycleId??"")&&a.frozenPercent==b.frozenPercent;
        static void MergeLifecycle(ProfileDocument target,ProfileDocument source)
        {
            target.protectionLegacyPlayer|=source.protectionLegacyPlayer||(source.protectionLifecycleVersion==0&&source.orderProtectionInitialized);
            target.protectionLifecycleVersion=Math.Max(target.protectionLifecycleVersion,source.protectionLifecycleVersion);
            foreach(var e in source.protectionEntries??new List<string>())if(!target.protectionEntries.Contains(e))target.protectionEntries.Add(e);
            target.protectionEntries.Sort(StringComparer.Ordinal);
            MergeImmutable(target.protectionAttempts,source.protectionAttempts??new List<ProtectionAttemptRecord>(),x=>x.attemptId,Same,Clone,"protection attempt");
            target.protectionAttempts.Sort((a,b)=>StringComparer.Ordinal.Compare(a.attemptId,b.attemptId));
        }
        static bool LifecycleContains(ProfileDocument remote,ProfileDocument sent)=>remote.protectionLifecycleVersion>=sent.protectionLifecycleVersion&&(!sent.protectionLegacyPlayer||remote.protectionLegacyPlayer)&&sent.protectionEntries.All(remote.protectionEntries.Contains)&&sent.protectionAttempts.All(a=>remote.protectionAttempts.Any(b=>Same(a,b)));
        static void ValidateLifecycle(ProfileDocument d)
        {
            if(d.protectionLifecycleVersion<0||d.protectionLifecycleVersion>1)throw new ArgumentException("Invalid protection lifecycle version");
            if(d.protectionEntries==null)d.protectionEntries=new List<string>();if(d.protectionAttempts==null)d.protectionAttempts=new List<ProtectionAttemptRecord>();
            foreach(var e in d.protectionEntries)Date(e);
            foreach(var a in d.protectionAttempts){if(a==null)throw new ArgumentException("Null attempt");Identity(a.attemptId,"attemptId");Date(a.startedUtc);if(a.frozenPercent<0||a.frozenPercent>60)throw new ArgumentException("Invalid frozen P");if(!string.IsNullOrEmpty(a.cycleId)&&a.cycleId!="new"){if(!a.cycleId.StartsWith("return:",StringComparison.Ordinal))throw new ArgumentException("Invalid cycle");Date(a.cycleId.Substring(7));}}
            if(d.protectionEntries.Distinct().Count()!=d.protectionEntries.Count||d.protectionAttempts.Select(x=>x.attemptId).Distinct().Count()!=d.protectionAttempts.Count)throw new ArgumentException("Duplicate lifecycle fact");
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var r in d.formalResults.Where(x=>x.protectionVersion==2))
            {Identity(r.protectionAttemptId,"protectionAttemptId");var a=d.protectionAttempts.Find(x=>x.attemptId==r.protectionAttemptId);if(a==null||Date(r.completedUtc)<Date(a.startedUtc)||!seen.Add(r.protectionAttemptId))throw new ArgumentException("Invalid protected result reference");}
        }
    }
}
