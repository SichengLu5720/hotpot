using System;
using System.IO;
using System.Text.Json;
using HotpotSort.Contracts;
using HotpotSort.Profile;
class Task057ProfileQa
{
 sealed class Memory:IProfilePersistence {public ProfileDocument Value; public ProfileDocument Load()=>Value==null?null:ProfileStore.Copy(Value); public void Save(ProfileDocument d){Value=ProfileStore.Copy(d);}}
 static int checks,serial; static DateTimeOffset now=new DateTimeOffset(2026,9,29,0,0,0,TimeSpan.Zero);
 static void Eq<T>(T expected,T actual,string label){checks++;if(!Equals(expected,actual))throw new Exception(label+": expected "+expected+", actual "+actual);}
 static ProfileStore Open(Memory m,string account="local",ProfileDocument legacy=null)=>new ProfileStore("development",account,m,new ProfileClock(()=>now,()=>0),null,legacy);
 static void Result(ProfileStore p,bool won){serial++;now=now.AddMinutes(1);p.RecordFormalResult("r"+serial,"s"+serial,"20260929",won?FormalChallengeResultKind.Won:FormalChallengeResultKind.FailedTimeout,now);}
 static void Main(){
 var m=new Memory();var p=Open(m);Eq(20,p.OrderProtectionPercent,"new");
 foreach(var expected in new[]{15,10,5,0,0}){Result(p,true);Eq(expected,p.OrderProtectionPercent,"wins");}
 foreach(var expected in new[]{3,6,9,12,15,18,20,20}){Result(p,false);Eq(expected,p.OrderProtectionPercent,"failures");}
 Result(p,true);Eq(15,p.OrderProtectionPercent,"cap then win");Result(p,false);Eq(18,p.OrderProtectionPercent,"mixed");
 var snapshot=p.ReadSnapshot();var fact=snapshot.formalResults[snapshot.formalResults.Count-1];Eq(AccountMutationResult.Duplicate,p.RecordFormalResult(fact.resultId,fact.sessionId,fact.challengeDay,fact.result,DateTimeOffset.Parse(fact.completedUtc)),"duplicate");Eq(18,p.OrderProtectionPercent,"duplicate unchanged");
 now=now.AddDays(20);p=Open(m);Eq(18,p.OrderProtectionPercent,"cross-day reload");
 var fresh=Open(new Memory());var merged=ProfileStore.Merge(fresh.ReadSnapshot(),p.ReadSnapshot());Eq(18,ProfileStore.ProtectionPercent(merged),"new device preserves");Eq(18,ProfileStore.ProtectionPercent(ProfileStore.Merge(p.ReadSnapshot(),fresh.ReadSnapshot())),"merge commutative");Eq(18,ProfileStore.ProtectionPercent(ProfileStore.Merge(merged,merged)),"merge idempotent");
 var old=new ProfileDocument{environment="development",account="local"};old.formalResults.Add(new FormalChallengeResultRecord{resultId="historical",sessionId="old",challengeDay="20260929",completedUtc=now.ToString("o"),result=FormalChallengeResultKind.FailedTimeout});var oldMem=new Memory{Value=old};var migrated=Open(oldMem);Eq(0,migrated.OrderProtectionPercent,"historical not replayed");Result(migrated,false);Eq(3,migrated.OrderProtectionPercent,"old failure");Result(migrated,true);Eq(0,migrated.OrderProtectionPercent,"old floor");
 Eq(0,ProfileStore.ProtectionPercent(ProfileStore.Merge(migrated.ReadSnapshot(),fresh.ReadSnapshot())),"old baseline beats new device");
 var history=migrated.ReadSnapshot().formalResults.Find(x=>x.resultId=="historical");Eq(AccountMutationResult.Duplicate,migrated.RecordFormalResult(history.resultId,history.sessionId,history.challengeDay,history.result,DateTimeOffset.Parse(history.completedUtc)),"historical duplicate preserves version");
 Eq(0,Open(new Memory(),"local",ProfileStore.Migrate("development","local",null,null,null)).OrderProtectionPercent,"empty legacy is old");
 var another=Open(new Memory(),"another");Eq(20,another.OrderProtectionPercent,"account independent");bool rejected=false;try{ProfileStore.Merge(p.ReadSnapshot(),another.ReadSnapshot());}catch(ArgumentException){rejected=true;}Eq(true,rejected,"partition mismatch");
 var invalid=p.ReadSnapshot();invalid.orderProtectionInitialPercent=21;rejected=false;try{ProfileStore.Validate(invalid,"development","local");}catch(ArgumentException){rejected=true;}Eq(true,rejected,"invalid baseline");
 var imported=ProfileStore.Copy(p.ReadSnapshot());imported.account="another";another.ImportFacts(imported);Eq(18,another.OrderProtectionPercent,"entry account import");
 var copy=p.ReadSnapshot();copy.formalResults.Clear();Eq(18,p.OrderProtectionPercent,"deep copy isolation");
 var dir=Path.GetFullPath(".harness/qa/TASK-057/profile");Directory.CreateDirectory(dir);var options=new JsonSerializerOptions{IncludeFields=true};
 var file=Path.Combine(dir,"profile-envelope.txt");
 var disk=new RecoverableProfileStorage(file,"development","local",k=>File.Exists(k)?File.ReadAllText(k):"",(k,v)=>File.WriteAllText(k,v),d=>JsonSerializer.Serialize(d,options),j=>JsonSerializer.Deserialize<ProfileDocument>(j,options));
 disk.Save(p.ReadSnapshot());var restored=new ProfileStore("development","local",disk,new ProfileClock(()=>now,()=>0));Eq(18,restored.OrderProtectionPercent,"serialized disk reload");
 var left=Open(new Memory{Value=ProfileStore.Copy(snapshot)});var right=Open(new Memory{Value=ProfileStore.Copy(snapshot)});Result(left,true);Result(right,false);
 var combined=ProfileStore.Merge(left.ReadSnapshot(),right.ReadSnapshot());Eq(16,ProfileStore.ProtectionPercent(combined),"concurrent results merged chronologically");Eq(16,ProfileStore.ProtectionPercent(ProfileStore.Merge(right.ReadSnapshot(),left.ReadSnapshot())),"concurrent reverse merge");
 Console.WriteLine("TASK057_PROFILE_PASS checks="+checks);
 }
}
