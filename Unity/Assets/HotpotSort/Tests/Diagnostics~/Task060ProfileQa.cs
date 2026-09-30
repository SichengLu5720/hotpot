using System;
using System.Linq;
using System.Text.Json;
using System.IO;
using HotpotSort.Contracts;
using HotpotSort.Profile;
class Task060ProfileQa
{
 sealed class Memory:IProfilePersistence{public ProfileDocument Value;public ProfileDocument Load()=>Value==null?null:ProfileStore.Copy(Value);public void Save(ProfileDocument d){Value=ProfileStore.Copy(d);}}
 static DateTimeOffset now=new DateTimeOffset(2026,9,30,0,0,0,TimeSpan.Zero);static readonly DateTimeOffset epoch=now;static int checks;
 static ProfileStore Open(Memory m,string account="a")=>new ProfileStore("development",account,m,new ProfileClock(()=>now,()=>0));
 static void Eq<T>(T e,T a,string label){checks++;if(!Equals(e,a))throw new Exception(label+" expected "+e+" got "+a);}
 static void Result(ProfileStore p,string a,bool won)=>p.RecordProtectedFormalResult(a,"r-"+a,"s-"+a,"20260930",won?FormalChallengeResultKind.Won:FormalChallengeResultKind.FailedTimeout,now);
 static void Main(){
 var m=new Memory();var p=Open(m);p.RecordGameEntry(now);Eq(60,p.BeginFormalChallenge("first",now),"new first");now=epoch.AddDays(8);Eq(60,p.BeginFormalChallenge("exit-retry",now),"exit keeps first past day8");Eq(60,p.BeginFormalChallenge("first",now),"idempotent frozen");Result(p,"exit-retry",true);Eq(20,p.OrderProtectionPercent,"first result clips after delta");Result(p,"exit-retry",true);Eq(20,p.OrderProtectionPercent,"duplicate result");
 now=epoch.AddDays(9);p.RecordGameEntry(now);Eq(20,p.BeginFormalChallenge("regular",now),"normal cap");Result(p,"regular",true);Eq(15,p.OrderProtectionPercent,"normal win");p=Open(m);Eq(15,p.OrderProtectionPercent,"reload");
 now=epoch.AddDays(23);p.RecordGameEntry(now);p.RecordGameEntry(now.AddSeconds(1));Eq(60,p.BeginFormalChallenge("return",now.AddSeconds(2)),"14 day return");now=now.AddSeconds(3);Result(p,"return",true);Eq(55,p.OrderProtectionPercent,"return win");
 var oldTime=now;now=now.AddSeconds(1);Result(p,"first",false);Eq(55,p.OrderProtectionPercent,"old cycle late result ignored");
 now=epoch.AddDays(24).AddMinutes(1);Eq(50,p.BeginFormalChallenge("day2",now),"day2 cap");Result(p,"day2",true);Eq(45,p.OrderProtectionPercent,"cap before delta");
 var before=p.ReadSnapshot();var clone=ProfileStore.Copy(before);clone.protectionEntries.Reverse();clone.protectionAttempts.Reverse();clone.formalResults.Reverse();Eq(ProfileStore.ProtectionAt(before,now),ProfileStore.ProtectionAt(ProfileStore.Merge(clone,before),now),"reordered merge");Eq(ProfileStore.ProtectionAt(before,now),ProfileStore.ProtectionAt(ProfileStore.Merge(before,clone),now),"reverse merge");
 var legacy=new Memory{Value=new ProfileDocument{environment="development",account="a",orderProtectionInitialized=true,orderProtectionInitialPercent=20}};var lp=Open(legacy);Eq(20,lp.BeginFormalChallenge("old",now),"legacy preserved");lp.RecordGameEntry(now);Eq(20,lp.BeginFormalChallenge("old2",now),"unknown absence is not return");
 var anon=Open(new Memory());anon.BeginFormalChallenge("anon",now);lp.ImportFacts(anon.ReadSnapshot());Eq(20,lp.BeginFormalChallenge("old-import",now),"old account ignores imported new cycle");
 var other=Open(new Memory(),"b");Eq(60,other.BeginFormalChallenge("fresh",now),"account isolated");bool reject=false;try{ProfileStore.Merge(before,other.ReadSnapshot());}catch(ArgumentException){reject=true;}Eq(true,reject,"partition rejection");
 now=epoch;var balance=Open(new Memory());balance.RecordGameEntry(now);Eq(60,balance.BeginFormalChallenge("b1",now),"balance initial");Result(balance,"b1",true);Eq(55,balance.OrderProtectionPercent,"initial win minus5");balance.BeginFormalChallenge("b2",now);Result(balance,"b2",false);Eq(58,balance.OrderProtectionPercent,"failure plus3");balance.BeginFormalChallenge("b3",now);Result(balance,"b3",false);Eq(60,balance.OrderProtectionPercent,"firstday cap60");
 now=epoch.AddDays(14).AddTicks(-1);balance.RecordGameEntry(now);Eq(20,balance.BeginFormalChallenge("not-return",now),"less than14 days no return");
 now=epoch;var daily=Open(new Memory());daily.RecordGameEntry(now);daily.BeginFormalChallenge("d0",now);Result(daily,"d0",false);foreach(var pair in new[]{(1,50),(2,40),(3,35),(4,30),(5,25),(6,20),(7,20)}){now=epoch.AddDays(pair.Item1);Eq(pair.Item2,daily.BeginFormalChallenge("d"+pair.Item1,now),"daily cap "+pair.Item1);}
 var options=new JsonSerializerOptions{IncludeFields=true,WriteIndented=true};Directory.CreateDirectory(".harness/qa/TASK-060/profile");File.WriteAllText(".harness/qa/TASK-060/profile/profile-fixture.json",JsonSerializer.Serialize(before,options));Console.WriteLine("TASK060 profile PASS checks="+checks);
 }
}
