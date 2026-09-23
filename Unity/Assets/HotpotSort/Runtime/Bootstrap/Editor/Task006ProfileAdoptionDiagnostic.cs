#if UNITY_EDITOR
using System;
using System.IO;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Profile;
using UnityEditor;
using UnityEngine;

namespace HotpotSort.Bootstrap
{
    public static class Task006ProfileAdoptionDiagnostic
    {
        const string Account="wx_aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        sealed class Wire:IProfileSyncTransport
        {
            public TaskCompletionSource<ProfileSyncResponse> Pending;public ProfileDocument Last;
            public Task<ProfileSyncResponse> SyncAsync(ProfileDocument value){Last=ProfileStore.Copy(value);Pending=new TaskCompletionSource<ProfileSyncResponse>();return Pending.Task;}
            public void Complete(string day=null){var value=ProfileStore.Copy(Last);if(day!=null&&!value.firstWinDays.Contains(day))value.firstWinDays.Add(day);Pending.SetResult(new ProfileSyncResponse{Status=ProfileSyncStatus.Synced,Snapshot=value,AcknowledgedOperations=value.pending.ConvertAll(p=>p.operationId).ToArray(),ServerUtc=DateTimeOffset.UtcNow});}
        }
        sealed class Disk:IProfilePersistence
        {public ProfileDocument Value;public bool Fail;public ProfileDocument Load()=>Value==null?null:ProfileStore.Copy(Value);public void Save(ProfileDocument value){if(Fail)throw new IOException("injected persistence fault");Value=ProfileStore.Copy(value);}}
        public static async void Run()
        {
            int code=0,groups=0;string key="HotpotSort.Task006.AdoptionQA."+Guid.NewGuid().ToString("N");
            try
            {
                var local=new LocalDevelopmentServices(key);local.RecordFirstWin("20260922");
                DateTimeOffset utc=DateTimeOffset.UtcNow;
                Require(local.TryReserveShare("20260923","qa-original",utc)&&local.TryCommitShare("20260923","qa-original",utc),"local fact");
                bool entry=false;long generation=1;var wire=new Wire();var gate=new EntryProfileTransport(wire,()=>entry,()=>generation);
                Require(!local.TryAdoptAccountAtEntry(Account,gate,false)&&local.ReadSnapshot().account=="local","adopted mid-game");groups++;
                entry=true;Require(local.TryAdoptAccountAtEntry(Account,gate,true),"entry adoption");
                Require(local.ReadSnapshot().rewards.Count==1&&local.ReadSnapshot().rewards[0].effectiveUtc==utc.ToString("o"),"original time");groups++;
                var pending=local.SyncAsync();entry=false;generation++;wire.Complete("20260921");await pending;
                Require(local.TotalFirstWins==1&&local.ReadSnapshot().pending.Count>0,"mid-game merge");
                entry=true;generation++;pending=local.SyncAsync();wire.Complete("20260921");await pending;
                Require(local.TotalFirstWins==2&&local.ReadSnapshot().pending.Count==0,"entry merge");groups++;
                var offline=new LocalDevelopmentServices(key,isDevelopmentSimulation:false);
                Require(offline.ReadSnapshot().account==Account&&offline.TotalFirstWins==2,"offline cached account");
                Require(offline.TryAdoptAccountAtEntry(Account,gate,true),"cached transport rebind");pending=offline.SyncAsync();wire.Complete();await pending;
                Require(offline.LastSyncStatus==ProfileSyncStatus.Synced&&offline.ReadSnapshot().rewards.Count==1,"restart duplicate or unavailable");groups++;
                Require(!offline.TryAdoptAccountAtEntry("wx_other",gate,true),"cross-account leakage");
                pending=offline.SyncAsync();Require(await offline.SyncAsync()==ProfileSyncStatus.Busy,"single flight");var first=wire.Pending;wire.Complete();
                for(int i=0;i<100&&ReferenceEquals(first,wire.Pending);i++)await Task.Yield();
                Require(!ReferenceEquals(first,wire.Pending),"missing coalesced follow-up");wire.Complete();await pending;groups++;
                var disk=new Disk();var store=new ProfileStore("development",Account,disk,new ProfileClock(()=>DateTimeOffset.UtcNow,()=>0));
                var facts=local.ReadSnapshot();disk.Fail=true;bool failed=false;try{store.ImportFacts(facts);}catch(IOException){failed=true;}
                Require(failed&&store.ReadSnapshot().firstWinDays.Count==0,"failed import activated");disk.Fail=false;store.ImportFacts(facts);store.ImportFacts(facts);
                Require(store.ReadSnapshot().rewards.Count==1&&store.ReadSnapshot().pending.Exists(p=>p.operationId=="reward:qa-original"),"import retry lost outbox");groups++;
                Debug.Log("TASK006_PROFILE_ADOPTION_PASS groups="+groups);
            }
            catch(Exception e){code=1;Debug.LogError("TASK006_PROFILE_ADOPTION_FAILED "+e.GetType().Name+": "+e.Message);}
            finally
            {
                foreach(string account in new[]{"local",Account})
                {string p=key+".profile-v1."+RecoverableProfileStorage.Hash("development\n"+account);PlayerPrefs.DeleteKey(p);PlayerPrefs.DeleteKey(p+".backup");}
                PlayerPrefs.DeleteKey(key);PlayerPrefs.DeleteKey(key+".local-owner.development");PlayerPrefs.DeleteKey(key+".active-account.development");PlayerPrefs.Save();
                File.WriteAllText(Environment.GetEnvironmentVariable("HOTPOT_V10_REPORT"),"{\"exitCode\":"+code+",\"groups\":"+groups+",\"isolatedPrefs\":true,\"externalRequests\":0}");
                EditorApplication.Exit(code);
            }
        }
    }
}
#endif
