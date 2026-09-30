using System;
using System.IO;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Profile;
using HotpotSort.Replay;
static class Task060IntegrationQa
{
 static int checks;
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
 sealed class Memory:IProfilePersistence { public ProfileDocument Value;public ProfileDocument Load()=>Value;public void Save(ProfileDocument d){Value=ProfileStore.Copy(d);} }
 static void Main(string[] args)
 {
  var f=DailySessionFactory.FromProductionJson(File.ReadAllText(args[0]),File.ReadAllText(args[1])).WithLayoutForChallenge("2026-09-30");
  var context=new ChallengeContext("2026-09-30",f.Content.ContentVersion,f.ConfigurationDigest,"test",0,"60");
  foreach(int p in new[]{0,20,25,30,35,40,50,60})using(var session=f.CreateStageWithLifecycleProtection(context,ChallengeStage.Formal,0,p))
  {
   Check(session.OrderProtectionPercentAtStart==p,"new frozen P "+p);
   session.Supply(new SupplyObservation(1,1,true,true));
   Check(session.Redeal(2).Accepted&&!session.Redeal(3).Accepted,"tool once retained");
   var r=DailyReplay.Run(f,session.ExportReplay());Check(r.Success&&r.Session.StateHash==session.StateHash,"v4 replay "+r.Error);r.Session?.Dispose();
  }
  using(var old=f.CreateStageWithProtection(context,ChallengeStage.Formal,0,20))using(var current=f.CreateStageWithLifecycleProtection(context,ChallengeStage.Formal,0,20))
  {
   for(ulong t=1;t<30;t++){old.Supply(new SupplyObservation(t,t,true,true));current.Supply(new SupplyObservation(t,t,true,true));}
   Check(current.Snapshot.CanonicalStateJson.Replace("\"orderSupportVersion\":4","\"orderSupportVersion\":3")==old.Snapshot.CanonicalStateJson,"same P preserves exact D1 and supply state except version");
  }
  bool rejected=false;try{f.CreateStageWithProtection(context,ChallengeStage.Formal,0,60);}catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected,"historical v3 cap preserved");
  foreach(int p in new[]{-1,61}){rejected=false;try{f.CreateStageWithLifecycleProtection(context,ChallengeStage.Formal,0,p);}catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected,"invalid lifecycle P rejected");}
  var now=DateTimeOffset.Parse("2026-09-30T00:00:00Z");var memory=new Memory();var profile=new ProfileStore("test","test",memory,new ProfileClock(()=>now,()=>0));profile.RecordGameEntry(now);
  var adaptive=new AdaptiveOrderPityFactory(f,profile,()=>now);
  using(var warm=(DailySession)adaptive.CreateStage(context,ChallengeStage.Warmup,0))
  {
   Check(warm.Redeal(1).Accepted,"warm tool use");now=now.AddDays(3);
   using(var formal=(DailySession)adaptive.CreateStage(context,ChallengeStage.Formal,0))
   {
    Check(formal.OrderProtectionPercentAtStart==60,"warmup does not age first formal protection");Check(formal.UsedToolMask==4,"tools carry across stages");
    now=now.AddDays(8);Check(formal.OrderProtectionPercentAtStart==60,"running P frozen across day boundary");
   }
  }
  using(var formal=(DailySession)adaptive.CreateStage(context,ChallengeStage.Formal,0))
  {Check(formal.OrderProtectionPercentAtStart==60,"exit preserves unresolved first challenge");Check(formal.Timeout(600000).Accepted,"real terminal result");Check(adaptive.LastRecordError==null,"terminal recorded through factory");}
  using(var formal=(DailySession)adaptive.CreateStage(context,ChallengeStage.Formal,0))Check(formal.OrderProtectionPercentAtStart==20,"post-first-result day9 freezes regular cap");
  for(int i=2;i<args.Length;i++){var r=DailyReplay.Run(f,new ReplayPackage("historical",DailySession.ReplaySchema,File.ReadAllText(args[i])));Check(r.Success,"historical/runtime replay "+args[i]+" "+r.Error);r.Session?.Dispose();}
  Directory.CreateDirectory(".harness/qa/TASK-060/integration");File.WriteAllText(".harness/qa/TASK-060/integration/core-result.txt","PASS "+checks);Console.WriteLine("PASS "+checks);
 }
}
namespace HotpotSort.Presentation {public static class PresentationAssets {public const string CandidateRoot="Hotpot/TASK001/v10/r001";}}
