using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotpotSort.Bootstrap;
using HotpotSort.Contracts;
using HotpotSort.Profile;

namespace UnityEngine
{
 public static class PlayerPrefs
 {
  public static Dictionary<string,string> Data=new Dictionary<string,string>();
  public static bool Fail;
  public static string GetString(string key,string fallback)=>Data.TryGetValue(key,out var v)?v:fallback;
  public static void SetString(string key,string value){if(Fail)throw new InvalidOperationException("injected write failure");Data[key]=value;}
  public static void Save(){if(Fail)throw new InvalidOperationException("injected save failure");}
 }
 public static class Mathf {public static float Clamp01(float x)=>Math.Max(0,Math.Min(1,x));}
 public static class Time {public static double realtimeSinceStartupAsDouble=>0;}
}
class ProfileAdoptionQa
{
 const string Account="wx_aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 sealed class Transport:IProfileSyncTransport
 {
  public TaskCompletionSource<ProfileSyncResponse> Pending;
  public ProfileDocument Last;
  public int Calls;
  public Task<ProfileSyncResponse> SyncAsync(ProfileDocument value){Calls++;Last=ProfileStore.Copy(value);Pending=new TaskCompletionSource<ProfileSyncResponse>();return Pending.Task;}
  public void Complete(string remoteWin=null)
  {var snapshot=ProfileStore.Copy(Last);if(remoteWin!=null&&!snapshot.firstWinDays.Contains(remoteWin))snapshot.firstWinDays.Add(remoteWin);Pending.SetResult(new ProfileSyncResponse{Status=ProfileSyncStatus.Synced,Snapshot=snapshot,ServerUtc=DateTimeOffset.UtcNow,AcknowledgedOperations=snapshot.pending.ConvertAll(x=>x.operationId).ToArray()});}
 }
 static async Task Main()
 {
  var local=new LocalDevelopmentServices("qa");local.RecordFirstWin("20260922");
  DateTimeOffset utc=DateTimeOffset.UtcNow;
  Check(local.TryReserveShare("20260923","original-reward",utc),"reserve");Check(local.TryCommitShare("20260923","original-reward",utc),"commit");
  var wire=new Transport();bool entry=false;long generation=1;
  var gated=new EntryProfileTransport(wire,()=>entry,()=>generation);
  Check(!local.TryAdoptAccountAtEntry(Account,gated,false)&&local.ReadSnapshot().account=="local","mid-game adoption");
  Console.WriteLine("A01-local-immediate-midgame-identity-deferred PASS");
  entry=true;Check(local.TryAdoptAccountAtEntry(Account,gated,true),"entry adoption");
  var adopted=local.ReadSnapshot();Check(adopted.account==Account&&adopted.firstWinDays.Count==1&&adopted.rewards.Count==1&&adopted.rewards[0].effectiveUtc==utc.ToString("o"),"facts changed");
  Check(adopted.pending.Exists(x=>x.operationId=="reward:original-reward"),"import outbox missing");
  Console.WriteLine("A02-entry-durable-union-original-time-outbox PASS");
  int events=0;local.ProfileChanged+=()=>events++;
  var pending=local.SyncAsync();entry=false;generation++;wire.Complete("20260921");await pending;
  Check(!local.ReadSnapshot().firstWinDays.Contains("20260921")&&local.ReadSnapshot().pending.Count>0,"midgame remote merge");
  entry=true;generation++;pending=local.SyncAsync();wire.Complete("20260921");await pending;
  Check(local.ReadSnapshot().firstWinDays.Count==2&&local.ReadSnapshot().pending.Count==0&&events>0,"entry retry union");
  Console.WriteLine("A03-late-response-deferred-entry-resync-and-observers PASS");
  var restart=new LocalDevelopmentServices("qa");Check(restart.TryAdoptAccountAtEntry(Account,gated,true),"restart adoption");
  Check(restart.ReadSnapshot().firstWinDays.Count==2&&restart.ReadSnapshot().rewards.Count==1,"restart duplicate/loss");
  var another=new LocalDevelopmentServices("qa");Check(!another.TryAdoptAccountAtEntry("wx_other",gated,true)&&another.ReadSnapshot().account=="local","cross-account local leakage");
  Console.WriteLine("A04-restart-idempotency-account-isolation PASS");
  var fail=new LocalDevelopmentServices("failure");fail.RecordFirstWin("20260920");UnityEngine.PlayerPrefs.Fail=true;
  try{fail.TryAdoptAccountAtEntry(Account,gated,true);throw new Exception("failure not surfaced");}catch(InvalidOperationException){}
  finally{UnityEngine.PlayerPrefs.Fail=false;}
  Check(fail.ReadSnapshot().account=="local"&&fail.TotalFirstWins==1,"failed adoption changed active store");
  Check(fail.TryAdoptAccountAtEntry(Account,gated,true)&&fail.TotalFirstWins==1,"adoption retry");
  Console.WriteLine("A05-persistence-failure-keeps-local-retry PASS");
  var offline=new LocalDevelopmentServices("qa",isDevelopmentSimulation:false);
  Check(offline.ReadSnapshot().account==Account&&offline.TotalFirstWins==2&&offline.ReadSnapshot().rewards.Count==1,"offline account cache lost");
  Check(offline.TryAdoptAccountAtEntry(Account,gated,true),"offline transport rebind");
  pending=offline.SyncAsync();wire.Complete();await pending;
  Check(offline.LastSyncStatus==ProfileSyncStatus.Synced,"cached account transport not bound");
  Console.WriteLine("A06-offline-cached-account-and-entry-transport-rebind PASS");
  var coalesce=new LocalDevelopmentServices("coalesce",account:Account,transport:gated);
  pending=coalesce.SyncAsync();Check(await coalesce.SyncAsync()==ProfileSyncStatus.Busy,"single flight");var first=wire.Pending;wire.Complete();
  // Continuations can run inline or on the scheduler; await the bounded second request.
  for(int i=0;i<100&&ReferenceEquals(first,wire.Pending);i++)await Task.Yield();
  Check(!ReferenceEquals(first,wire.Pending),"missing coalesced follow-up");
  wire.Complete();await pending;
  Console.WriteLine("A07-sync-single-flight-bounded-followup PASS");
  Console.WriteLine("PROFILE_ADOPTION_PASS groups=7 externalRequests=0");
 }
}
