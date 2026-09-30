using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Determinism;
using HotpotSort.Replay;
static class Task057IntegrationQa
{
 static int checks;static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
 static Dictionary<string,object> State(DailySession s)=>CanonicalJson.Map(CanonicalJson.Parse(s.Snapshot.CanonicalStateJson));
 static ClickableObservation Obs(DailySession s,int[] above=null){var state=State(s);return new ClickableObservation(s.Snapshot.SessionId,s.Snapshot.TransactionId,CanonicalJson.Array(state["items"]).Select(CanonicalJson.Map).Where(i=>(string)i["location"]=="ActiveAvailable").Select(i=>CanonicalJson.Int(i["itemId"])),offscreenItemIds:Array.Empty<int>(),activePlateOrder:CanonicalJson.Array(state["activePlateIds"]).Select(CanonicalJson.Int),costVersion:4,nativeAboveItemIds:above??Array.Empty<int>());}
 static void Main(string[] args)
 {
  var baseFactory=DailySessionFactory.FromProductionJson(File.ReadAllText(args[0]),File.ReadAllText(args[1]));
  for(int day=22;day<29;day++)foreach(int p in new[]{0,20})
  {
   string date="2026-09-"+day;var f=baseFactory.WithLayoutForChallenge(date);var c=new ChallengeContext(date,f.Content.ContentVersion,f.ConfigurationDigest,"test",0,"42");
   using(var s=f.CreateStageWithProtection(c,ChallengeStage.Formal,0,p))
   {
    Check(s.Snapshot.Status==GameStatus.Running,"native starts "+date);Check(s.OrderProtectionPercentAtStart==p,"frozen P");
    ulong tick=1,seq=1;for(int j=0;j<50;j++)s.Supply(new SupplyObservation((ulong)j+1,tick++,true,true));
    for(int n=0;n<190&&s.Snapshot.Status==GameStatus.Running;n++)
    {
     var state=State(s);var all=CanonicalJson.Array(state["items"]).Select(CanonicalJson.Map).ToArray();
     var kinds=CanonicalJson.Array(state["orders"]).Select(CanonicalJson.Map).Where(o=>o["kind"]!=null).Select(o=>(string)o["kind"]).ToArray();
     var item=all.FirstOrDefault(i=>(string)i["location"]=="ActiveAvailable"&&kinds.Contains((string)i["kind"]));Check(item!=null,"allocated target remains available");
     var hash=s.StateHash;var events=s.CoreEventsJson;var observation=Obs(s);
     s.GetSwapOrderTargets(observation);s.InspectDirector(0);Check(s.StateHash==hash&&s.CoreEventsJson==events,"queries do not change RNG/state/events");
     Check(s.Tap(new TapCommand(CanonicalJson.Int(item["itemId"]),seq++,tick++,true,observation)).Accepted,"native tap");
     Check(s.Snapshot.Status!=GameStatus.Aborted,"native invariants");
    }
    Check(s.Snapshot.Status==GameStatus.Won,"all seven layouts complete");Check(s.CoreEventsJson.Contains("native_d1_1.5111_ordinary_v1"),"native evaluator reached");
    var replay=DailyReplay.Run(baseFactory,s.ExportReplay());Check(replay.Success&&replay.Session.StateHash==s.StateHash,"native replay "+replay.Error);
   }
  }
  var cf=baseFactory.WithLayoutForChallenge("2026-09-29");var cx=new ChallengeContext("2026-09-29",cf.Content.ContentVersion,cf.ConfigurationDigest,"test",0,"41");
  using(var old=cf.CreateStage(cx,ChallengeStage.Formal,0,3))
  {old.Supply(new SupplyObservation(1,1,true,true));var result=DailyReplay.Run(baseFactory,old.ExportReplay());Check(result.Success,"historical support version retained "+result.Error);}
  using(var s=cf.CreateStageWithProtection(cx,ChallengeStage.Formal,0,20))
  {
   ulong tick=1;for(int j=0;j<50;j++)s.Supply(new SupplyObservation((ulong)j+1,tick++,true,true));
   var observation=Obs(s);var slots=s.GetSwapOrderTargets(observation);Check(slots.Length>0,"swap eligible");int slot=slots[0];
   Check(s.BeginSwapOrder(slot,s.OrderIdentity(slot),tick++,observation).Accepted,"swap begins");var token=s.ReadSwapOrderTransfer().token;
   Check(s.CompleteSwapOrder(token,tick++,Obs(s)).Accepted,"swap ends");Check(s.Snapshot.Status==GameStatus.Running,"swap state valid");
   var replay=DailyReplay.Run(baseFactory,s.ExportReplay());Check(replay.Success,"swap exact replay "+replay.Error);
  }
  foreach(int p in new[]{0,20})using(var s=cf.CreateStageWithProtection(cx,ChallengeStage.Formal,0,p))
  {
   var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
   var choose=typeof(DailySession).GetMethod("ChooseNative",flags);
   var completed=typeof(DailySession).GetField("completedOrders",flags);
   foreach(int n in new[]{0,9,10})
   {
    completed.SetValue(s,n);
    for(int j=0;j<100;j++)
    {
     var d=(DailyDirector.Decision)choose.Invoke(s,new object[]{0,true,null});var protect=CanonicalJson.Map(CanonicalJson.Map(d.Diagnostic)["protection"]);
     int roll=CanonicalJson.Int(protect["roll"]);
     Check((bool)protect["firstTen"]==(n<10),"first ten boundary");
     Check(n<10?roll==-1:roll>=0&&roll<100,"first ten bypass probability draw");
     Check((string)protect["branch"]==((n<10||roll<p)?"AllZeroBase":"Base"),"P decision exact comparison");
    }
   }
  }
  using(var s=cf.CreateStageWithProtection(cx,ChallengeStage.Formal,0,0))
  {
   ulong tick=1,seq=1,supply=1;
   for(int j=0;j<12;j++)s.Supply(new SupplyObservation(supply++,tick++,true,true));
   for(int n=0;n<45&&s.Snapshot.Status==GameStatus.Running&&!s.CoreEventsJson.Contains("NativeTargetExchanged");n++)
   {
    var state=State(s);var all=CanonicalJson.Array(state["items"]).Select(CanonicalJson.Map).Where(i=>(string)i["location"]=="ActiveAvailable").ToArray();
    var kinds=CanonicalJson.Array(state["orders"]).Select(CanonicalJson.Map).Where(o=>o["kind"]!=null).Select(o=>(string)o["kind"]).ToArray();
    var item=all.FirstOrDefault(i=>kinds.Contains((string)i["kind"]));
    if(item==null){s.Supply(new SupplyObservation(supply++,tick++,true,true));continue;}
    int id=CanonicalJson.Int(item["itemId"]);
    var observation=new ClickableObservation(s.Snapshot.SessionId,s.Snapshot.TransactionId,new[]{id},offscreenItemIds:Array.Empty<int>(),activePlateOrder:CanonicalJson.Array(state["activePlateIds"]).Select(CanonicalJson.Int),costVersion:4,nativeAboveItemIds:all.Select(i=>CanonicalJson.Int(i["itemId"])).Where(i=>i!=id));
    Check(s.Tap(new TapCommand(id,seq++,tick++,true,observation)).Accepted,"above exchange path tap");
   }
   Check(s.CoreEventsJson.Contains("NativeTargetExchanged"),"native exchange executed through real session");
   var replay=DailyReplay.Run(baseFactory,s.ExportReplay());Check(replay.Success,"exchange exact replay "+replay.Error);
  }
  if(args.Length>2)
  {
   var runtime=DailyReplay.Run(baseFactory,new ReplayPackage("runtime",DailySession.ReplaySchema,File.ReadAllText(args[2])));
   if(!runtime.Success){File.WriteAllText(".harness/qa/TASK-057/core/runtime-replayed-events.json",runtime.Session.CoreEventsJson);Console.WriteLine("runtime failure record "+runtime.RecordIndex);}
   Check(runtime.Success,"actual Unity observation replay "+runtime.Error);
  }
  Directory.CreateDirectory(".harness/qa/TASK-057/core");File.WriteAllText(".harness/qa/TASK-057/core/result.json",CanonicalJson.Write(CanonicalJson.Object("checks",checks,"passed",true)));
  Console.WriteLine("TASK057_INTEGRATION_PASS checks="+checks);
 }
}

