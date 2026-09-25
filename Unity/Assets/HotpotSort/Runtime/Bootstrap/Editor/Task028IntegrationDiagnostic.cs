#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Determinism;
using HotpotSort.Presentation;
using HotpotSort.Profile;
using HotpotSort.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HotpotSort.Bootstrap
{
    [InitializeOnLoad] public static class Task028IntegrationDiagnostic
    {
        const string Key="Task028.Integration";
        static Task028IntegrationDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");UnityEngine.Object.FindFirstObjectByType<Bootstrap>().enabled=false;EditorApplication.EnterPlaymode();}
        static void Check(bool v,string m){if(!v)throw new Exception(m);Debug.Log("TASK028 INTEGRATION PASS "+m);}
        static async Task Until(Func<bool> f,string m,int ms=15000){for(int n=0;n<ms/25;n++){if(f())return;await Task.Delay(25);}throw new Exception("Timeout: "+m);}
        static bool Text(GameplayView v,string t)=>v.GetComponentsInChildren<Text>().Any(x=>x.text.Replace("\n","")==t);
        static void Confirm(GameplayView v)=>v.GetComponentsInChildren<Button>().Single(b=>b.name=="TutorialAnyTap").onClick.Invoke();
        static System.Collections.Generic.Dictionary<string,object>[] Items(DailySession s)=>CanonicalJson.Array(CanonicalJson.Map(CanonicalJson.Parse(s.Snapshot.CanonicalStateJson))["items"]).Select(CanonicalJson.Map).ToArray();
        static void SupplyAll(DailySession s){ulong seq=1000;while(DailyViewMapper.PendingHead(s.Snapshot)!=0)Check(s.Supply(new SupplyObservation(++seq,0,true,true)).Accepted,"rule-isolation supply");}
        static void Invoke(object target,string name,params object[] args)=>target.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,args);
        static async void Execute()
        {
            string key="HotpotSort.Task028.Integration."+Guid.NewGuid().ToString("N");int exit=0;Bootstrap boot=null;
            try
            {
                boot=UnityEngine.Object.FindFirstObjectByType<Bootstrap>();var composition=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();
                var local=new LocalDevelopmentServices(key,"test");composition.ConfigureServices(local,local,local,local);boot.enabled=true;
                await Until(()=>boot.IsConfigured,"boot");var c=boot.Controller;var view=composition.PlayerView;
                composition.ConfigureServices(local,local,local,local);view.SetForeground(true);
                composition.SessionAction(ViewAction.StartToday);await Until(()=>composition.ActiveCore!=null,"asset-gated start");
                Check(c.Stage==ChallengeStage.Warmup&&!c.ChallengeTimerStarted,"real start enters untimed warmup");
                await Until(()=>!string.IsNullOrEmpty(view.LastSnapshot.tutorialItemId),"real clickable tutorial target");
                var initial=view.LastSnapshot;string first=initial.tutorialItemId;string oldId=initial.sessionId;long oldGeneration=initial.sessionGeneration;
                string hash=composition.ActiveCore.StateHash;
                composition.Tap(new ViewTap{itemId="999",inputSeq=1,snapshotRevision=initial.revision});Check(hash==composition.ActiveCore.StateHash,"first step rejects unrelated input");
                composition.Tap(new ViewTap{itemId=first,inputSeq=2,snapshotRevision=view.LastSnapshot.revision});
                Check(view.LastSnapshot.tutorialStep==ViewTutorialStep.FoodInFlight&&!Text(view,"集满 3 个相同食材，即可完成订单。"),"explanation waits for real arrival");
                await Until(()=>view.LastSnapshot.tutorialStep==ViewTutorialStep.OrderExplanation&&Text(view,"集满 3 个相同食材，即可完成订单。"),"real arrival explanation");
                Check(c.Pauses==PauseReasons.Tutorial&&!c.ChallengeTimerStarted,"arrival pauses tutorial without timer");Confirm(view);
                Check(local.WarmupTutorialCompleted&&!local.BufferWarningCompleted,"any-tap persists independent opening flag");
                Check(!composition.CompleteOpeningTutorial(oldId,oldGeneration),"opening confirmation is idempotent");
                SupplyAll(composition.ActiveCore);ulong taps=2000;
                var third=Items(composition.ActiveCore).Where(x=>(string)x["kind"]=="C").Select(x=>CanonicalJson.Int(x["itemId"])).ToArray();
                for(int i=0;i<4;i++)composition.ActiveCore.Tap(new TapCommand(third[i],++taps,0,true));
                await Until(()=>view.LastSnapshot.bufferWarning&&Text(view,"暂存区放满会导致挑战失败。"),"four-slot warning");
                Check(!c.CanAcceptInput,"warning input lock");Confirm(view);Check(local.BufferWarningCompleted,"any-tap persists buffer flag");
                for(int i=4;i<6;i++)composition.ActiveCore.Tap(new TapCommand(third[i],++taps,0,true));
                Check(composition.ActiveCore.RevivalPending,"warmup retains revival");composition.DeclineRevival();Check(c.Snapshot.Status==GameStatus.Failed,"warmup decline fails");
                await c.RetryAsync();Check(c.Stage==ChallengeStage.Warmup&&view.LastSnapshot.tutorialStep==ViewTutorialStep.None&&!view.LastSnapshot.bufferWarning,"failed retry resets warmup and keeps permanent flags");
                local.RewardPrompt=r=>Task.FromResult(RewardOutcome.Success);Invoke(composition,"RequestReward",RewardKind.ThirdPot,RewardRoute.SimulatedAd);
                await Until(()=>composition.ActiveCore.UnlockedExtraPotMask==1,"ad unlock");Check(local.ReadSnapshot().rewards.Any(r=>r.rewardKind==(int)RewardKind.ThirdPot),"simulated completed ad unlock persisted");
                await c.RetryAsync();Check(composition.ActiveCore.UnlockedExtraPotMask==1,"same warmup retry inherits advertised pot");
                Check(!composition.TutorialFoodArrived(oldId,oldGeneration,first)&&!composition.CompleteWarmup(oldId,oldGeneration),"prior session callbacks rejected");
                SupplyAll(composition.ActiveCore);taps=3000;
                while(composition.ActiveCore.Snapshot.Status==GameStatus.Running){int id=composition.ActiveCore.FindHintItem();Check(id>0,"warmup solvable");composition.ActiveCore.Tap(new TapCommand(id,++taps,0,true));}
                string warmId=c.Snapshot.SessionId;long warmGeneration=c.Generation;
                Check(view.LastSnapshot.warmupComplete&&c.Stage==ChallengeStage.Warmup&&!c.CanAcceptInput,"six orders await feedback and lock input");
                bool formalInitial=false,messageAbsent=false;composition.Updated+=u=>{if(!u.snapshot.isWarmup&&u.snapshot.phase==ViewPhase.Running&&!formalInitial){formalInitial=true;messageAbsent=!Text(view,"最后一关！");Check(u.snapshot.plates.Length==0,"formal initial snapshot has no supplied plates");}};
                await Until(()=>Text(view,"最后一关！"),"last-round message after final serve");
                Check(!view.GetComponent<GameplayFeedback>().WarmupTransitionBusy&&!view.World.Bodies.Any(),"last-round message waits for cleared feedback and board");
                double start=Time.realtimeSinceStartupAsDouble;await Task.Delay(350);Check(c.Stage==ChallengeStage.Warmup&&Text(view,"最后一关！"),"formal supply blocked while message visible");
                await Until(()=>c.Stage==ChallengeStage.Formal,"formal transition");
                Check(Time.realtimeSinceStartupAsDouble-start>=.70&&formalInitial&&messageAbsent,"message disappears before formal session starts");
                Check(composition.ActiveCore.UnlockedExtraPotMask==1&&view.LastSnapshot.completedOrders==6&&!c.ChallengeTimerStarted,"formal inherits pot and six orders without countdown");
                Check(!composition.CompleteWarmup(warmId,warmGeneration),"old transition callback rejected");
                await Until(()=>view.FindClickableHint()!=null,"formal actual supply");var hint=view.FindClickableHint();composition.Tap(new ViewTap{itemId=hint,inputSeq=1,snapshotRevision=view.LastSnapshot.revision});
                Check(c.ChallengeTimerStarted,"formal first effective click starts countdown");await c.RetryAsync();
                Check(c.Stage==ChallengeStage.Warmup&&composition.ActiveCore.UnlockedExtraPotMask==0,"formal retry begins fresh warmup workflow");
                var reloaded=new LocalDevelopmentServices(key,"test");Check(reloaded.WarmupTutorialCompleted&&reloaded.BufferWarningCompleted,"permanent flags reload from actual local storage");
                c.Exit();Check(composition.ActiveCore==null&&!composition.CompleteWarmup(warmId,warmGeneration),"exit rejects old transition");
                Debug.Log("TASK028_INTEGRATION_PASS");
            }
            catch(Exception e){exit=1;Debug.LogException(e);}
            finally
            {
                boot?.Controller?.Dispose();string partition=key+".profile-v1."+RecoverableProfileStorage.Hash("test\nlocal");
                PlayerPrefs.DeleteKey(key);PlayerPrefs.DeleteKey(partition);PlayerPrefs.DeleteKey(partition+".backup");PlayerPrefs.Save();
                SessionState.SetBool(Key,false);EditorApplication.Exit(exit);
            }
        }
    }
}
#endif
