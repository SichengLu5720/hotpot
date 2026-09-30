using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Determinism;
using HotpotSort.Replay;
using HotpotSort.Bootstrap;
static class Task060ToolRegressionQa
{
 static int checks;
 static void Check(bool b,string s){checks++;if(!b)throw new Exception(s);}
 static Dictionary<string,object> State(DailySession s)=>CanonicalJson.Map(CanonicalJson.Parse(s.Snapshot.CanonicalStateJson));
 static ClickableObservation Obs(DailySession s)=>new ClickableObservation(s.Snapshot.SessionId,s.Snapshot.TransactionId,CanonicalJson.Array(State(s)["items"]).Select(CanonicalJson.Map).Where(i=>(string)i["location"]=="ActiveAvailable").Select(i=>CanonicalJson.Int(i["itemId"])),offscreenItemIds:Array.Empty<int>(),activePlateOrder:CanonicalJson.Array(State(s)["activePlateIds"]).Select(CanonicalJson.Int),costVersion:4,nativeAboveItemIds:Array.Empty<int>());
 static void Supply(DailySession s,ref ulong tick){for(int i=0;i<50;i++)s.Supply(new SupplyObservation((ulong)i+1,tick++,true,true));}
 static void OffOrder(DailySession s,ref ulong tick,ref ulong seq){var st=State(s);var kinds=CanonicalJson.Array(st["orders"]).Select(CanonicalJson.Map).Select(o=>(string)o["kind"]).ToArray();var item=CanonicalJson.Array(st["items"]).Select(CanonicalJson.Map).First(i=>(string)i["location"]=="ActiveAvailable"&&!kinds.Contains((string)i["kind"]));Check(s.Tap(new TapCommand(CanonicalJson.Int(item["itemId"]),seq++,tick++,true,Obs(s))).Accepted,"off-order tap");}
 static void Replay(DailySessionFactory f,DailySession s){var result=DailyReplay.Run(f,s.ExportReplay());Check(result.Success&&result.Session.StateHash==s.StateHash,"exact replay "+result.Error);result.Session?.Dispose();}
 static void Main(string[] args)
 {
  var f=DailySessionFactory.FromProductionJson(File.ReadAllText(args[0]),File.ReadAllText(args[1])).WithLayoutForChallenge("2026-09-29");var c=new ChallengeContext("2026-09-29",f.Content.ContentVersion,f.ConfigurationDigest,"test",0,"41");
  using(var s=f.CreateStageWithProtection(c,ChallengeStage.Formal,0,20))
  {
   ulong tick=1,seq=1;Check(s.UsedToolMask==0,"fresh");Check(!s.ClearBuffer(tick++).Accepted&&s.UsedToolMask==0,"empty clear free");Check(s.GetSwapOrderTargets(null).Length==0&&s.UsedToolMask==0,"no observation free");Supply(s,ref tick);
   var hash=s.StateHash;for(int i=0;i<10;i++){s.CanUseTool(RewardKind.SwapOrder);s.GetSwapOrderTargets(Obs(s));}Check(s.StateHash==hash,"query pure");
   int slot=s.GetSwapOrderTargets(Obs(s))[0];Check(!s.BeginSwapOrder(slot,s.OrderIdentity(slot),tick++,Obs(s),()=>false).Accepted&&s.UsedToolMask==0,"failed consumption free");
   Check(s.BeginSwapOrder(slot,s.OrderIdentity(slot),tick++,Obs(s)).Accepted&&s.UsedToolMask==0,"begin not counted");string token=s.ReadSwapOrderTransfer().token;
   Check(!s.CompleteSwapOrder(token,tick++,Obs(s),()=>false).Accepted&&s.UsedToolMask==0,"completion consume failure free");Check(s.CompleteSwapOrder(token,tick++,Obs(s)).Accepted&&s.UsedToolMask==1,"swap committed");Check(!s.CompleteSwapOrder(token,tick++,Obs(s)).Accepted&&s.UsedToolMask==1,"duplicate completion");Check(s.GetSwapOrderTargets(Obs(s)).Length==0,"used swap targets empty");Check(!s.BeginSwapOrder(slot,s.OrderIdentity(slot),tick++,Obs(s)).Accepted,"repeat swap blocked");
   OffOrder(s,ref tick,ref seq);Check(s.CanClearBuffer&&s.ClearBuffer(tick++).Accepted&&s.UsedToolMask==3,"clear independent");OffOrder(s,ref tick,ref seq);Check(!s.CanClearBuffer&&!s.ClearBuffer(tick++).Accepted&&s.UsedToolMask==3,"repeat clear blocked with target");Check(s.CanRedeal&&s.Redeal(tick++).Accepted&&s.UsedToolMask==7,"shuffle independent");Check(!s.CanRedeal&&!s.Redeal(tick++).Accepted,"repeat shuffle blocked");Check(s.CanUseTool(RewardKind.Revival)&&s.CanUseTool(RewardKind.FourthPot),"non tools unaffected");Check(DailyViewMapper.Map(s.Snapshot,null,f.Content,0).snapshot.usedToolMask==7,"view carries mask");Replay(f,s);
  }
  using(var w=f.CreateStageWithProtection(c,ChallengeStage.Warmup,0,20))
  {Check(w.Redeal(1).Accepted&&w.UsedToolMask==4,"warmup usage");using(var s=f.CreateStageWithProtection(c,ChallengeStage.Formal,0,20,w.UsedToolMask)){Check(!s.CanRedeal&&s.CanUseTool(RewardKind.ClearBuffer)&&s.UsedToolMask==4,"warmup carry");Replay(f,s);}Replay(f,w);}
  using(var s=f.CreateStageWithProtection(c,ChallengeStage.Warmup,0,20)){Check(s.UsedToolMask==0&&s.CanUseTool(RewardKind.Shuffle),"restart reset");}
  using(var old=f.CreateReplaySession(c,null,ChallengeStage.Formal,0,0,3,20,2)){Check(old.Redeal(1).Accepted&&old.Redeal(2).Accepted&&old.UsedToolMask==0&&!State(old).ContainsKey("toolLimitVersion"),"old58 unlimited");Replay(f,old);}
  using(var s=f.CreateStageWithProtection(c,ChallengeStage.Formal,0,20))
  {ulong tick=1,seq=1;Supply(s,ref tick);OffOrder(s,ref tick,ref seq);Check(s.ClearBuffer(tick++).Accepted,"pre-revival clear");for(int i=0;i<6;i++)OffOrder(s,ref tick,ref seq);Check(s.RevivalPending,"revival offered");string offer=s.RevivalOfferId;Check(s.ResolveRevival(new ResolveRevivalCommand(offer,"test",true,tick++)).Accepted,"revival success");Check(s.UsedToolMask==2,"revival neither resets nor counts clear");Check(s.CompleteRevivalTransfer(offer,s.RevivalTransferToken,tick++).Accepted,"revival done");Check(!s.CanUseTool(RewardKind.ClearBuffer),"clear still spent");Replay(f,s);}
  foreach(int mask in new[]{-1,8}){bool threw=false;try{f.CreateStageWithProtection(c,ChallengeStage.Formal,0,0,mask);}catch(ArgumentException){threw=true;}Check(threw,"invalid mask rejected");}
  for(int mask=0;mask<=7;mask++)using(var s=f.CreateStageWithProtection(c,ChallengeStage.Formal,0,20,mask)){Check(s.CanUseTool(RewardKind.SwapOrder)==((mask&1)==0)&&s.CanUseTool(RewardKind.ClearBuffer)==((mask&2)==0)&&s.CanUseTool(RewardKind.Shuffle)==((mask&4)==0),"all mask independence");Replay(f,s);}
  using(var s=f.CreateStageWithProtection(c,ChallengeStage.Formal,0,20)){Check(!s.Redeal(1,25).Accepted&&s.UsedToolMask==0,"invalid shuffle free");}
  if(args.Length>2){var result=DailyReplay.Run(f,new ReplayPackage("old-runtime",DailySession.ReplaySchema,File.ReadAllText(args[2])));Check(result.Success,"actual TASK058 replay "+result.Error);result.Session?.Dispose();}
  Directory.CreateDirectory(".harness/qa/TASK-060/tool-regression");File.WriteAllText(".harness/qa/TASK-060/tool-regression/result.json",CanonicalJson.Write(CanonicalJson.Object("passed",true,"checks",checks)));
  Console.WriteLine("PASS "+checks+" core checks");
 }
}

namespace HotpotSort.Presentation { public static class PresentationAssets { public const string CandidateRoot="Hotpot/TASK001/v10/r001"; } }
