using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Collection;
using HotpotSort.Platform;
class Task036SocialQa
{
 sealed class Memory:ICollectionPersistence {public CollectionDocument Value;public CollectionDocument Load()=>Value;public void Save(CollectionDocument d){Value=CollectionStore.Copy(d);}}
 static void Check(bool value,string label){if(!value)throw new Exception(label);Console.WriteLine("PASS "+label);}
 static CollectionStore Store(string id,bool unlocked=true){var d=CollectionStore.NewDocument("development",id);d.entryUnlocked=unlocked;return new CollectionStore("development",id,new Memory{Value=d});}
 static async Task Main(){
  var now=DateTimeOffset.Parse("2026-09-26T00:00:00Z");var authority=new DevelopmentBrothAuthority(()=>now);
  var hostStore=Store("host");var helperStore=Store("helper",false);var host=new BrothActivityStore(authority,hostStore);var helper=new BrothActivityStore(authority,helperStore);
  var token=(await host.CreateInvitationAsync("r1","invite")).invitation.invitationId;
  var start=await helper.ConfirmAssistAsync("r2",token,"start");await host.ReadAsync("r3");
  Check(start.Succeeded&&start.collection.tools[0]==1&&start.collection.tools[1]==1&&start.collection.tools[2]==1,"start grants three tools once before first win");
  Check(!hostStore.ReadCollection().brothActivityQualified&&hostStore.ReadCollection().brothAssistantAccount=="helper","start binds without qualification");
  Check(start.invitation.expiresAt-start.invitation.startedAt==72L*3600000,"continuous 72 hour deadline");
  Check((await helper.ConfirmAssistAsync("r4",token,"start")).collection.tools[0]==1,"start retry no duplicate tools");
  var other=new BrothActivityStore(authority,Store("other",false));Check((await other.ConfirmAssistAsync("r5",token,"other")).failure==BrothFailure.AssistOccupied,"unique helper binding");
  var copied=CollectionStore.Copy(helperStore.ReadCollection());copied.brothHelping[0].expiresAt=0;Check(helperStore.ReadCollection().brothHelping[0].expiresAt>0,"helping deep copied");
  var restored=new CollectionStore("development","helper",new Memory{Value=helperStore.ReadCollection()});Check(restored.ReadCollection().brothHelping.Count==1,"restart snapshot retains helping context");
  Check((await helper.CompleteChallengeAsync("r6","s1",now.AddHours(1).ToString("o"),"bad")).failure==BrothFailure.InvalidChallenge,"future victory rejected");
  now=now.AddMinutes(1);Check((await helper.CompleteChallengeAsync("r7","s1",now.ToString("o"),"win")).Succeeded,"formal completion accepted");await host.ReadAsync("r8");
  Check(hostStore.ReadCollection().brothActivityQualified&&helperStore.ReadCollection().brothHelping.Count==0,"victory qualifies host and clears pending binding");
  Check((await helper.CompleteChallengeAsync("r9","s1",now.ToString("o"),"win")).collection.tools[0]==1,"completion retry grants nothing extra");
  var expStore=Store("expiry");var exp=new BrothActivityStore(authority,expStore);var expToken=(await exp.CreateInvitationAsync("r10","invite")).invitation.invitationId;await helper.ConfirmAssistAsync("r11",expToken,"start2");now=now.AddHours(72);await exp.ReadAsync("r12");
  Check(!expStore.ReadCollection().brothActivityQualified&&string.IsNullOrEmpty(expStore.ReadCollection().brothAssistantAccount),"expiry unbinds without qualifying");
  Check((await helper.ConfirmAssistAsync("r13",expToken,"again")).failure==BrothFailure.AlreadyAssisted,"lifetime pair cannot restart after expiry");
  Check(helperStore.ReadCollection().tools[0]==2,"expiry does not reclaim rewards");
  var links=new WeChatActivityLinkService(new WeChatRuntimeConfig(),q=>{});string opaque=new string('a',64);links.ReceiveQuery(new Dictionary<string,string>{{WeChatActivityLinkService.TradeQueryKey,opaque}});Check(links.PendingTradeId==opaque&&links.PendingInvitationId==null,"cold trade link isolated from broth");links.ReceiveQuery(new Dictionary<string,string>());Check(links.PendingTradeId==opaque,"ordinary show preserves pending without action");Check(links.RequestTradeShare(opaque),"real trade share query accepted");links.DismissTrade(opaque);links.ReceiveQuery(new Dictionary<string,string>{{WeChatActivityLinkService.TradeQueryKey,opaque}});Check(links.PendingTradeId==opaque,"hot trade link restored");
  Console.WriteLine("TASK036 SOCIAL CHECKS PASSED");
 }
}
