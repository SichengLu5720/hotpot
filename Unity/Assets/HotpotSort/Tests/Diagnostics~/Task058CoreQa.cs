using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Determinism;
using HotpotSort.Replay;
using HotpotSort.Bootstrap;
static class Task058CoreQa
{
 static int checks;
 static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
 static Dictionary<string,object> State(DailySession s)=>CanonicalJson.Map(CanonicalJson.Parse(s.Snapshot.CanonicalStateJson));
 static ClickableObservation Obs(DailySession s){var state=State(s);return new ClickableObservation(s.Snapshot.SessionId,s.Snapshot.TransactionId,CanonicalJson.Array(state["items"]).Select(CanonicalJson.Map).Where(i=>(string)i["location"]=="ActiveAvailable").Select(i=>CanonicalJson.Int(i["itemId"])),offscreenItemIds:Array.Empty<int>(),activePlateOrder:CanonicalJson.Array(state["activePlateIds"]).Select(CanonicalJson.Int),costVersion:4,nativeAboveItemIds:Array.Empty<int>());}
 static int Threshold(DailySession s)=>CanonicalJson.Int(CanonicalJson.Map(CanonicalJson.Map(CanonicalJson.Parse(s.InspectDirector(0)))["protection"])["threshold"]);
 static void Replay(DailySessionFactory factory,DailySession s,string name){var r=DailyReplay.Run(factory,s.ExportReplay());Check(r.Success&&r.Session.StateHash==s.StateHash,name+": "+r.Error);r.Session?.Dispose();}
 static void Main(string[] args)
 {
  var baseFactory=DailySessionFactory.FromProductionJson(File.ReadAllText(args[0]),File.ReadAllText(args[1]));
  for(int day=22;day<29;day++)
  {
   string date="2026-09-"+day;var f=baseFactory.WithLayoutForChallenge(date);var c=new ChallengeContext(date,f.Content.ContentVersion,f.ConfigurationDigest,"test",0,"42");
   using(var s=f.CreateStageWithProtection(c,ChallengeStage.Formal,0,20))
   {
    Check(s.PotUnlockVersion==2,"production rule version");
    var view=DailyViewMapper.Map(s.Snapshot,null,f.Content,0).snapshot;
    Check(view.thirdPotThreshold==31&&view.fourthPotThreshold==0,"new mapper thresholds");
    ulong tick=1,seq=1;for(int j=0;j<50;j++)Check(s.Supply(new SupplyObservation((ulong)j+1,tick++,true,true)).Accepted,"supply");
    var seen=new HashSet<int>();
    for(int n=0;n<190&&s.Snapshot.Status==GameStatus.Running;n++)
    {
     var state=State(s);var orders=CanonicalJson.Array(state["orders"]).Select(CanonicalJson.Map).ToArray();
     var kinds=orders.Where(o=>o["kind"]!=null).Select(o=>(string)o["kind"]).ToArray();
     var item=CanonicalJson.Array(state["items"]).Select(CanonicalJson.Map).FirstOrDefault(i=>(string)i["location"]=="ActiveAvailable"&&kinds.Contains((string)i["kind"]));
     Check(item!=null,"target available");Check(s.Tap(new TapCommand(CanonicalJson.Int(item["itemId"]),seq++,tick++,true,Obs(s))).Accepted,"tap accepted");
     int count=s.CumulativeCompletedOrders;seen.Add(count);
     Check(s.NormalPotCount==(count>=31?3:2),"normal pots exact every tap");
     Check(s.UnlockedExtraPotMask==(count>=31?1:0),"fourth never opens automatically");
     if(new[]{30,31,38,61}.Contains(count))
     {
      Check(Threshold(s)==(count>=31?2:1),"protection threshold at "+count);
      Replay(baseFactory,s,"boundary replay "+count);
     }
    }
    Check(s.Snapshot.Status==GameStatus.Won&&s.CumulativeCompletedOrders==61,"real 61-order completion");
    Check(new[]{30,31,38,61}.All(seen.Contains),"all exact boundaries reached");
   }
  }
  var factory=baseFactory.WithLayoutForChallenge("2026-09-29");var context=new ChallengeContext("2026-09-29",factory.Content.ContentVersion,factory.ConfigurationDigest,"test",0,"41");
  using(var s=factory.CreateStageWithProtection(context,ChallengeStage.Formal,0,0))
  {
   Check(s.CanUnlockFourth&&s.CanUnlockThird,"both reward targets initially eligible");
   Check(s.UnlockFourth(1,Obs(s)).Accepted,"fourth reward command");
   Check(s.NormalPotCount==2&&Threshold(s)==1&&s.UnlockedExtraPotMask==2,"reward fourth does not change normal threshold");
   var protection=CanonicalJson.Map(CanonicalJson.Map(CanonicalJson.Parse(s.InspectDirector(0)))["protection"]);
   Check(CanonicalJson.Int(protection["dangerCount"])==2,"reward fourth participates in danger count");
   Check(!s.UnlockFourth(2,Obs(s)).Accepted,"duplicate fourth rejected");
   Check(s.UnlockThird(3,Obs(s)).Accepted&&s.UnlockedExtraPotMask==3,"third reward command");
   Check(s.NormalPotCount==2&&Threshold(s)==1,"both reward pots preserve normal threshold");
   Replay(baseFactory,s,"reward commands replay");
  }
  using(var warmup=factory.CreateStageWithProtection(context,ChallengeStage.Warmup,0,20))
  {
   Check(warmup.CumulativeCompletedOrders==0&&warmup.NormalPotCount==2,"warmup excluded");
   Check(warmup.UnlockFourth(1).Accepted&&warmup.UnlockThird(2).Accepted,"warmup reward unlock");
   using(var formal=factory.CreateStageWithProtection(context,ChallengeStage.Formal,warmup.UnlockedExtraPotMask,20))
   {Check(formal.UnlockedExtraPotMask==3&&formal.NormalPotCount==2,"warmup inheritance");Replay(baseFactory,formal,"inherited replay");}
   Replay(baseFactory,warmup,"warmup replay");
  }
  using(var old=factory.CreateStage(context,ChallengeStage.Formal,0,0))
  {
   Check(old.PotUnlockVersion==1&&!State(old).ContainsKey("potUnlockVersion"),"legacy API version unchanged");
   var view=DailyViewMapper.Map(old.Snapshot,null,factory.Content,0).snapshot;Check(view.thirdPotThreshold==20&&view.fourthPotThreshold==38,"old mapper unchanged");
   Replay(baseFactory,old,"old API replay");
  }
  using(var previousNative=factory.CreateReplaySession(context,null,ChallengeStage.Formal,0,0,3,20))
  {
   Check(previousNative.PotUnlockVersion==1,"previous native defaults old pot rules");
   ulong tick=1,seq=1;for(int j=0;j<50;j++)previousNative.Supply(new SupplyObservation((ulong)j+1,tick++,true,true));
   var seen=new HashSet<int>();
   while(previousNative.Snapshot.Status==GameStatus.Running)
   {
    var state=State(previousNative);var kinds=CanonicalJson.Array(state["orders"]).Select(CanonicalJson.Map).Where(o=>o["kind"]!=null).Select(o=>(string)o["kind"]).ToArray();
    var item=CanonicalJson.Array(state["items"]).Select(CanonicalJson.Map).First(i=>(string)i["location"]=="ActiveAvailable"&&kinds.Contains((string)i["kind"]));
    Check(previousNative.Tap(new TapCommand(CanonicalJson.Int(item["itemId"]),seq++,tick++,true,Obs(previousNative))).Accepted,"historical native tap");
    int count=previousNative.CumulativeCompletedOrders;seen.Add(count);
    Check(previousNative.NormalPotCount==(count>=38?4:count>=20?3:2),"historical 20/38 normal pots");
    Check(previousNative.UnlockedExtraPotMask==(count>=38?3:count>=20?1:0),"historical actual 20/38 unlocks");
   }
   Check(previousNative.Snapshot.Status==GameStatus.Won&&new[]{19,20,37,38,61}.All(seen.Contains),"historical full game boundaries");
   Replay(baseFactory,previousNative,"previous native replay");
  }
  if(args.Length>2){var r=DailyReplay.Run(baseFactory,new ReplayPackage("old-runtime",DailySession.ReplaySchema,File.ReadAllText(args[2])));Check(r.Success,"actual TASK057 historical runtime: "+r.Error);r.Session?.Dispose();}
  Directory.CreateDirectory(".harness/qa/TASK-058/core");File.WriteAllText(".harness/qa/TASK-058/core/result.json",CanonicalJson.Write(CanonicalJson.Object("passed",true,"checks",checks,"layouts",7,"ordersPerLayout",61)));
  Console.WriteLine("TASK058_CORE_PASS checks="+checks);
 }
}

// Asset loading is outside this headless mapper test; only the theme root constant is needed.
namespace HotpotSort.Presentation { public static class PresentationAssets { public const string CandidateRoot="Hotpot/TASK001/v10/r001"; } }
