using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Determinism;
using HotpotSort.Replay;

static class Task044IntegrationQa
{
    static int checks;
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    static Dictionary<string,object> State(DailySession session)=>CanonicalJson.Map(CanonicalJson.Parse(session.Snapshot.CanonicalStateJson));
    static int Main(string[] args)
    {
        try
        {
            if(args.Length!=2)throw new ArgumentException("production and layout catalog paths required");
            var root=DailySessionFactory.FromProductionJson(File.ReadAllText(args[0]),File.ReadAllText(args[1]));
            var layoutIds=new HashSet<string>(StringComparer.Ordinal);var digests=new HashSet<string>(StringComparer.Ordinal);
            DateTime first=new DateTime(2026,9,28);
            for(int i=0;i<7;i++)
            {
                string day=first.AddDays(i).ToString("yyyyMMdd",System.Globalization.CultureInfo.InvariantCulture);
                var selected=root.WithLayoutForChallenge(day);layoutIds.Add(selected.Layout.LayoutId);digests.Add(selected.Content.Digest);
                var context=new ChallengeContext(day,selected.Content.ContentVersion,selected.ConfigurationDigest,"qa",i);
                var warm=selected.CreateStage(context,ChallengeStage.Warmup,0,3);
                string warmMapping=CanonicalJson.Write(State(warm)["mapping"]);
                var formal=selected.CreateStage(context,ChallengeStage.Formal,0,3);
                Check(warmMapping==CanonicalJson.Write(State(formal)["mapping"]),"shared stage mapping "+day);
                Check(formal.ConsecutiveFormalFailuresAtStart==3,"frozen failure state "+day);
                var replay=DailyReplay.Run(root,formal.ExportReplay());
                Check(replay.Success&&replay.Session.StateHash==formal.StateHash,"selected-layout replay "+day+" "+replay.Error);
                Check(root.WithLayoutForChallenge(day).Layout.LayoutId==selected.Layout.LayoutId,"same-day layout stable "+day);
            }
            Check(layoutIds.Count==7&&digests.Count==7,"seven distinct runtime selections");
            var probe=root.CreateStage(new ChallengeContext("20260928",root.Content.ContentVersion,root.ConfigurationDigest,"pot-boundary",0),ChallengeStage.Formal,0,0);
            var completed=typeof(DailySession).GetField("completedOrders",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            var settle=typeof(DailySession).GetMethod("Settle",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            foreach(var boundary in new[]{new[]{19,2},new[]{20,3},new[]{37,3},new[]{38,4}})
            {
                completed.SetValue(probe,boundary[0]);settle.Invoke(probe,null);
                Check(probe.NormalPotCount==boundary[1],"normal pot stage at formal order "+boundary[0]);
                Check(probe.UnlockedExtraPotMask==(boundary[1]==2?0:boundary[1]==3?1:3),"actual unlocked pots at formal order "+boundary[0]);
            }
            var early=root.CreateStage(new ChallengeContext("20260928",root.Content.ContentVersion,root.ConfigurationDigest,"pot-early",0),ChallengeStage.Formal,3,0);
            Check(early.UnlockedExtraPotMask==3&&early.NormalPotCount==2,"reward pots preserve early unlock without advancing normal stage");
            var warmProgress=root.CreateStage(new ChallengeContext("20260928",root.Content.ContentVersion,root.ConfigurationDigest,"warm-progress",0),ChallengeStage.Warmup,0,0);
            completed.SetValue(warmProgress,6);
            Check(warmProgress.CumulativeCompletedOrders==0&&warmProgress.NormalPotCount==2,"warmup orders do not advance unlock progress");
            completed.SetValue(early,0);
            Check(early.CumulativeCompletedOrders==0&&DailySession.ThirdPotCumulativeThreshold==20&&DailySession.FourthPotCumulativeThreshold==38,"formal unlock counter starts at zero with reduced thresholds");
            Console.WriteLine("TASK044_INTEGRATION_QA_PASS checks="+checks+" layouts=7 replay=7");return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
