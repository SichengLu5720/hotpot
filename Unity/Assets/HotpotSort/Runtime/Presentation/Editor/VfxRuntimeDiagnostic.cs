#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Capture=HotpotSort.Task001V7.VisualCapture;

namespace HotpotSort.Presentation
{
    [InitializeOnLoad]
    public static class VfxRuntimeDiagnostic
    {
        const string Key="Hotpot.VfxRuntime";
        static int checks;
        static VfxRuntimeDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();}
        static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;Debug.Log("VFX_PASS "+label);}
        static void Tick(GameplayFeedback feedback,float seconds){while(seconds>.00001f){float dt=Mathf.Min(.01f,seconds);feedback.Tick(dt);seconds-=dt;}}
        static ViewEvent Complete(ViewSnapshot state,long sequence)=>new ViewEvent{sessionId=state.sessionId,sessionGeneration=state.sessionGeneration,sequence=sequence,kind="OrderCompleted",slot=0,ingredientId="food_00"};
        static ViewEvent AutoAbsorb(ViewSnapshot state,long sequence)=>new ViewEvent{sessionId=state.sessionId,sessionGeneration=state.sessionGeneration,sequence=sequence,kind="BufferAutoAbsorbed",slot=0,itemId="item-buffer-0",ingredientId="food_00",sourceContainer="Buffer",sourceSlot=0,targetContainer="Order",targetSlot=0};
        static void Execute()
        {
            int exit=0;GameObject owner=null;
            try
            {
                var schedule=new PotAmbientSchedule();var settings=new PresentationEffects{steamKeys=new[]{"a","b","c"}};
                var first=new double[4];int variant;float drift;
                for(int step=1;step<250;step++)for(int slot=0;slot<4;slot++)if(schedule.TryEmit(slot,step*.01,true,false,settings,out variant,out drift)&&first[slot]==0)first[slot]=step*.01;
                Check(first.All(t=>t>0)&&first.Distinct().Count()==4,"four enabled pots are independently staggered");
                Check(!schedule.TryEmit(0,100,false,false,settings,out variant,out drift),"locked pot emits none");
                Check(!schedule.TryEmit(1,100,true,true,settings,out variant,out drift),"serving pot emits none");
                owner=new GameObject("VfxRuntimeFixture");var view=owner.AddComponent<GameplayView>();view.ConfigureAssets(V7Art.Root);
                var feedback=view.GetComponent<GameplayFeedback>();feedback.enabled=false;view.SetForeground(true);
                var state=new ViewSnapshot{sessionId="vfx",sessionGeneration=1,phase=ViewPhase.Running,orders=Enumerable.Range(0,4).Select(i=>new ViewOrder{slot=i,enabled=i<2,foodId=0,required=3}).ToArray(),buffer=new ViewItem[5]};
                var port=new Capture.FullPort(state,true);view.Bind(port);view.World.SetSimulating(false);
                int settles=0;bool oldHidden=true;feedback.ReplacementSettling=(slot,t)=>{settles++;oldHidden&=!view.GetComponentsInChildren<RectTransform>().Any(n=>n.name=="ServingWholePot_"+slot);};
                var events=Enumerable.Range(1,5).Select(i=>Complete(state,i)).ToArray();string before=JsonUtility.ToJson(state);port.Emit(events);
                Check(feedback.IsServing(0)&&feedback.PendingServeCount(0)==4,"five completions retained in order");
                port.Emit(events);Check(feedback.PendingServeCount(0)==4,"duplicate events do not queue twice");
                state.pauseReasons=ViewPauseReasons.User;port.Emit(Array.Empty<ViewEvent>());Tick(feedback,1);Check(feedback.PendingServeCount(0)==4&&settles==0,"pause freezes serve phases");
                state.pauseReasons=ViewPauseReasons.Background;port.Emit(Array.Empty<ViewEvent>());Tick(feedback,1);Check(feedback.PendingServeCount(0)==4&&settles==0,"background freezes serve phases");
                state.pauseReasons=ViewPauseReasons.Reward;port.Emit(Array.Empty<ViewEvent>());Tick(feedback,1);Check(feedback.PendingServeCount(0)==4&&settles==0,"reward modal freezes serve phases");
                state.pauseReasons=ViewPauseReasons.None;port.Emit(Array.Empty<ViewEvent>());before=JsonUtility.ToJson(state);Tick(feedback,.5f);Check(settles==0,"replacement not exposed during outgoing phase");
                Tick(feedback,5.2f);Check(!feedback.IsServing(0)&&feedback.PendingServeCount(0)==0&&settles>0,"queued serves drain and every replacement settles");
                Check(oldHidden,"old pot hidden before every replacement frame");
                Check(JsonUtility.ToJson(state)==before,"feedback leaves core snapshot unchanged");
                Check(view.GetComponentsInChildren<RawImage>().Where(n=>n.transform.IsChildOf(view.GetComponentsInChildren<RectTransform>().First(r=>r.name=="FeedbackLayer"))).All(n=>!n.raycastTarget),"effect images do not intercept input");
                port.Emit(new[]{Complete(state,6),AutoAbsorb(state,7)});
                Check(feedback.PendingDeferredRouteCount(0)==1,"buffer auto-absorb waits for replacement pot");
                Tick(feedback,.90f);Check(feedback.PendingDeferredRouteCount(0)==1,"buffer route remains deferred during push-in");
                Tick(feedback,.06f);Check(feedback.PendingDeferredRouteCount(0)==0&&feedback.IsServing(0),"buffer route starts only after replacement settles");
                Tick(feedback,.48f);Check(!feedback.IsServing(0),"slower buffer route finishes before the next serve can start");
                port.Emit(new[]{Complete(state,8)});state.sessionGeneration=2;port.Emit(Array.Empty<ViewEvent>());Check(!feedback.IsServing(0)&&feedback.PendingServeCount(0)==0,"session change cancels queued and active serves");
                Check(feedback.ActiveEffectCount==0,"session change cancels active effects");
                port.Emit(new[]{Complete(state,1)});state.phase=ViewPhase.Aborted;port.Emit(Array.Empty<ViewEvent>());Check(!feedback.IsServing(0)&&feedback.ActiveEffectCount==0,"terminal exit cancels feedback");
                Check(feedback.PoolSize<=64,"reusable effects remain bounded");
                Debug.Log("VFX_RUNTIME_PASS checks="+checks);
            }
            catch(Exception ex){exit=1;Debug.LogException(ex);}
            finally{if(owner)UnityEngine.Object.Destroy(owner);SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
        }
    }
}
#endif
