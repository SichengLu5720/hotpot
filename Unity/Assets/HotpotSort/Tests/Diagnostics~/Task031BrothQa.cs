using System;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Collection;
class Task031BrothQa
{
 sealed class Memory:ICollectionPersistence { public CollectionDocument Value; public CollectionDocument Load()=>Value;public void Save(CollectionDocument d){Value=CollectionStore.Copy(d);} }
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);Console.WriteLine("PASS "+message);}
 static CollectionStore Store(string id,bool unlocked=true){var d=CollectionStore.NewDocument("development",id);d.entryUnlocked=unlocked;return new CollectionStore("development",id,new Memory{Value=d});}
 static async Task Main()
 {
  var old=CollectionStore.NewDocument("development","old");old.ownedBroths=null;old.currentBroth=null;
  var persisted=new Memory{Value=old};var restored=new CollectionStore("development","old",persisted);
  Check(restored.ReadCollection().currentBroth=="red"&&restored.ReadCollection().ownedBroths.Count==1,"legacy missing fields restore red");
  var clock=DateTimeOffset.Parse("2026-09-25T15:59:00Z");var authority=new DevelopmentBrothAuthority(()=>clock);
  var a=Store("a");var b=Store("b",false);var owner=new BrothActivityStore(authority,a);var helper=new BrothActivityStore(authority,b);
  Check(owner.IsDevelopmentSimulation,"explicit development simulation");
  Check((await owner.ClaimAsync("r1","clear","c0")).failure==BrothFailure.NotQualified,"unqualified claim rejected");
  Check((await owner.SelectAsync("r2","clear","s0")).failure==BrothFailure.NotOwned,"unowned selection rejected");
  var invitation=(await owner.CreateInvitationAsync("r3","i1")).invitation.invitationId;
  Check(!a.ReadCollection().brothActivityQualified,"creating invitation grants nothing");
  Check((await owner.ConfirmAssistAsync("r4",invitation,"self")).failure==BrothFailure.SelfAssist,"self assist rejected");
  var result=await helper.ConfirmAssistAsync("r5",invitation,"h1");
  Check(result.Succeeded&&result.requestId=="r5"&&result.isDevelopmentSimulation&&result.collection.tools[0]==1&&result.collection.tools[1]==1&&result.collection.tools[2]==1,"new account assist atomic reward");
  Check((await helper.ConfirmAssistAsync("r6",invitation,"h1")).collection.tools[0]==1,"same operation retry no duplicate reward");
  Check((await helper.ConfirmAssistAsync("r7",invitation,"h2")).failure==BrothFailure.AlreadyAssisted,"new operation duplicate rejected");
  await owner.ReadAsync("r8");var qualified=a.ReadCollection();
  Check(BrothCatalog.HasClaimReminder(qualified)&&qualified.brothActivityQualified,"qualification refresh and reminder");
  clock=clock.AddYears(1);await owner.ReadAsync("r9");Check(a.ReadCollection().brothActivityQualified,"qualification does not expire");
  Check((await owner.ClaimAsync("r10","clear","c1")).Succeeded,"claim succeeds once");
  Check(!BrothCatalog.HasClaimReminder(a.ReadCollection())&&a.ReadCollection().currentBroth=="clear","claim selects and clears reminder");
  Check((await owner.ClaimAsync("r11","tomato","c1")).failure==BrothFailure.OperationConflict,"operation argument conflict rejected");
  Check((await owner.ClaimAsync("r12","tomato","c2")).failure==BrothFailure.AlreadyChosen,"cannot rechoose");
  var future=a.ReadCollection();future.ownedBroths.Add("tomato");future.serverRevision++;authority.Seed(future);await owner.ReadAsync("r13");
  Check((await owner.SelectAsync("r14","tomato","s1")).Succeeded&&a.ReadCollection().brothActivityChoice=="clear","future ownership preserves activity choice");
  a.ApplyAuthoritativeSnapshot(qualified);Check(a.ReadCollection().currentBroth=="tomato","old snapshot cannot roll back");
  var saved=new Memory{Value=a.ReadCollection()};Check(new CollectionStore("development","a",saved).ReadCollection().brothActivityChoice=="clear","restart restores choice");
  var c=Store("c");var other=new BrothActivityStore(authority,c);Check((await other.ConfirmAssistAsync("r15",invitation,"h3")).failure==BrothFailure.ActivityComplete&&c.ReadCollection().tools[0]==0,"completed host rejects further helpers");
  for(int i=0;i<4;i++){
   var host=new BrothActivityStore(authority,Store("host"+i));var token=(await host.CreateInvitationAsync("i"+i,"invite")).invitation.invitationId;
   var help=await helper.ConfirmAssistAsync("h"+i,token,"daily"+i);
   Check(i<3?help.Succeeded:help.failure==BrothFailure.DailyLimit,"daily help "+i);
   if(i==3){clock=clock.AddMinutes(2);Check((await helper.ConfirmAssistAsync("next",token,"dailyNext")).Succeeded,"Beijing midnight resets limit");}
  }
  bool refused=false;try{new BrothActivityStore(authority,Store("wx_fake"));}catch(ArgumentException){refused=true;}Check(refused,"native identity cannot use development authority");
  Console.WriteLine("TASK031 BROTH CHECKS PASSED");
 }
}
