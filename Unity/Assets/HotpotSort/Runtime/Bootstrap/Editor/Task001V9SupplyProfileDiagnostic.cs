#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Determinism;
using HotpotSort.Platform;
using HotpotSort.Presentation;
using HotpotSort.Profile;
using HotpotSort.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HotpotSort.Bootstrap
{
    [InitializeOnLoad]
    public static class Task001V9SupplyProfileDiagnostic
    {
        const string Key="Task001V9SupplyProfileQa";const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static Task001V9SupplyProfileDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run()
        {var args=Environment.GetCommandLineArgs();SessionState.SetString(Key+"Report",args[Array.IndexOf(args,"-task001Report")+1]);SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");UnityEngine.Object.FindFirstObjectByType<Bootstrap>().enabled=false;EditorApplication.EnterPlaymode();}
        static void Assert(bool value,string message){if(!value)throw new Exception(message);}
        static void Set(object target,string name,object value)=>target.GetType().GetField(name,Private).SetValue(target,value);
        static T Get<T>(object target,string name)=>(T)target.GetType().GetField(name,Private).GetValue(target);
        static void Invoke(object target,string name)=>target.GetType().GetMethod(name,Private).Invoke(target,null);
        static string RandomState(System.Random random)=>string.Join("|",typeof(System.Random).GetFields(Private).OrderBy(f=>f.Name).Select(f=>f.Name+":"+(f.GetValue(random) is int[] a?string.Join(",",a):f.GetValue(random)?.ToString())));
        static async void Execute()
        {
            int exit=0;var results=new List<object>();string profileKey="HotpotSort.SupplyProfileQA."+Guid.NewGuid().ToString("N");Bootstrap boot=null;WeChatSilentLogin login=null;WeChatProfileTransport transport=null;
            try
            {
                var c=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();var sdk=new Sdk();var config=new WeChatRuntimeConfig{cloudEnvironmentId="qa-env"};
                login=new WeChatSilentLogin(config,sdk);transport=new WeChatProfileTransport(config,login);var loginTask=login.StartAsync();
                var local=new LocalDevelopmentServices(profileKey,"development",transport:transport,isDevelopmentSimulation:false);
                c.ConfigureServices(local,local,null,local);boot=UnityEngine.Object.FindFirstObjectByType<Bootstrap>();boot.enabled=true;
                for(int i=0;i<100&&!boot.IsConfigured;i++)await Task.Delay(50);
                Assert(boot.IsConfigured,"Production Bootstrap failed: "+boot.Status);Assert(!loginTask.IsCompleted,"Login fake not delayed");
                c.enabled=false;var clock=new Clock();Set(boot.Controller,"clock",clock);Set(boot.Controller,"time",new TimeResolver(null,new FixedUtc()));
                await boot.Controller.StartTodayAsync();c.PlayerView.World.enabled=false;Assert(boot.Controller.CanAcceptInput,"Game blocked by login");
                local.RecordFirstWin("20260922");int pending=local.ReadSnapshot().pending.Count;Assert(await local.SyncAsync()==ProfileSyncStatus.Unavailable,"Unauthenticated cloud reported success");
                sdk.Success("qa-one-use-code");Assert(await loginTask==WeChatLoginStatus.NeedsServer&&login.Identity==null,"Untrusted account fabricated");
                Assert(await local.SyncAsync()==ProfileSyncStatus.Unavailable&&local.ReadSnapshot().pending.Count==pending,"Server absence consumed outbox");
                var restored=new LocalDevelopmentServices(profileKey,"development",transport:transport,isDevelopmentSimulation:false);
                Assert(restored.TotalFirstWins==1&&restored.ReadSnapshot().pending.Count==pending&&restored.TimeSnapshot.Source==ProfileTimeSource.DeviceTime,"Offline restart failed");
                results.Add(CanonicalJson.Object("case","UP01","status","PASS","scope","Real Boot editor entry remains playable during injected login; NeedsServer/Unavailable preserve isolated Unity profile/outbox across reload"));
                var core=c.ActiveCore;var expectedRandom=new System.Random(601377);int validSupplies=0;
                Action supply=()=>
                {
                    Invoke(c,"Update");validSupplies++;Assert(c.PlayerView.World.OccupiedPlateCount==validSupplies,"valid supply count");
                    var plate=c.PlayerView.World.Bodies.Last().data;float x=DailyViewMapper.SpawnX(validSupplies-1)+(float)(expectedRandom.NextDouble()*100-50);
                    Assert(Math.Abs(plate.motion.spawnX-x)<.0001f,"Rejected observation consumed spawn RNG");
                };
                Func<ViewSupplyObservation> valid=()=>new ViewSupplyObservation{version=1,sessionGeneration=boot.Controller.Generation,snapshotRevision=c.Read().revision,observationSequence=Get<long>(c,"lastSupplyObservation")+1,entryCount=0};
                Action<string,ViewSupplyObservation,bool> rejected=(name,observation,inWindow)=>
                {
                    string hash=core.StateHash,random=RandomState(Get<System.Random>(c,"spawnRandom"));int head=DailyViewMapper.PendingHead(core.Snapshot),count=c.PlayerView.World.OccupiedPlateCount,spawns=Get<int>(c,"successfulSpawns");
                    var state=CanonicalJson.Map(CanonicalJson.Parse(core.Snapshot.CanonicalStateJson));string queue=CanonicalJson.Write(state["pendingPlateIds"]);
                    Set(c,"observingSupply",inWindow);c.ObserveSupply(observation);
                    Assert(core.StateHash==hash&&DailyViewMapper.PendingHead(core.Snapshot)==head&&c.PlayerView.World.OccupiedPlateCount==count&&Get<int>(c,"successfulSpawns")==spawns,"rejected observation changed core/queue/count: "+name);
                    var after=CanonicalJson.Map(CanonicalJson.Parse(core.Snapshot.CanonicalStateJson));Assert(CanonicalJson.Write(after["pendingPlateIds"])==queue&&RandomState(Get<System.Random>(c,"spawnRandom"))==random,"rejected observation changed queue/random: "+name);
                    Set(c,"observingSupply",false);results.Add(CanonicalJson.Object("case",name,"status","PASS","invariants","queue head/full queue/count/spawn RNG/core hash"));
                };
                var observation=valid();observation.version=0;rejected("US01-wrong-version",observation,true);
                observation=valid();observation.sessionGeneration--;rejected("US02-wrong-generation",observation,true);
                observation=valid();observation.snapshotRevision--;rejected("US03-stale-revision",observation,true);
                observation=valid();observation.snapshotRevision++;rejected("US04-future-revision",observation,true);
                rejected("US05-outside-window-before-tick",valid(),false);
                supply();observation=valid();observation.observationSequence=Get<long>(c,"lastSupplyObservation");rejected("US06-duplicate-sequence",observation,true);
                observation.observationSequence--;rejected("US07-decreasing-sequence",observation,true);
                rejected("US08-outside-window-after-tick",valid(),false);
                clock.Value=.3;supply();Assert(validSupplies==2,"schedule was consumed by rejected callbacks");
                await boot.Controller.RetryAsync();core=c.ActiveCore;expectedRandom=new System.Random(601377);validSupplies=0;
                observation=valid();observation.sessionGeneration--;rejected("US09-old-generation-after-retry",observation,true);supply();
                results.Add(CanonicalJson.Object("case","US10","status","PASS","scope","Immediate first check, subsequent 300ms tick, retry and unchanged old spawn RNG after all rejections"));
                Debug.Log("SUPPLY_PROFILE_UNITY_PASS");
            }
            catch(Exception e){exit=1;results.Add(CanonicalJson.Object("status","FAIL","error",e.ToString()));Debug.LogException(e);}
            finally
            {
                try{boot?.Controller?.Dispose();transport?.Dispose();login?.Dispose();}
                catch(Exception e){exit=1;Debug.LogException(e);}
                string partition=profileKey+".profile-v1."+RecoverableProfileStorage.Hash("development\nlocal");
                PlayerPrefs.DeleteKey(profileKey);PlayerPrefs.DeleteKey(partition);PlayerPrefs.DeleteKey(partition+".backup");PlayerPrefs.Save();
                try{File.WriteAllText(SessionState.GetString(Key+"Report",""),CanonicalJson.Write(CanonicalJson.Object("exitCode",exit,"scope","Production editor entry with injected SDK; no native calls/network/device acceptance; isolated QA save only","results",results)));}
                finally{SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
            }
        }
        sealed class Sdk:IWeChatLoginSdk{public bool Available=>true;public Action<string> Success;public void Login(Action<string> success,Action failure){Success=success;}}
        sealed class Clock:IMonotonicClock{public double Value;public double Seconds=>Value;}
        sealed class FixedUtc:ITimeProvider{public Task<DateTimeOffset> GetUtcAsync()=>Task.FromResult(new DateTimeOffset(2026,9,22,0,0,0,TimeSpan.Zero));}
    }
}
#endif
