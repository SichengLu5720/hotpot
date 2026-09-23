#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HotpotSort.Presentation
{
    [InitializeOnLoad]
    public static class PressHapticDiagnostic
    {
        const string Key="Hotpot.PressHaptic";
        static int checks;
        sealed class Port:IPresentationPort
        {
            public ViewSnapshot state; public int taps;
            public event Action<ViewUpdate> Updated;
            public ViewSnapshot Read()=>state;
            public void Tap(ViewTap tap){taps++;}
            public void SessionAction(ViewAction action){}
            public void ObserveSupply(ViewSupplyObservation value){}
            public void Emit(params ViewEvent[] events){state.revision++;Updated?.Invoke(new ViewUpdate{snapshot=state,events=events});}
        }
        static PressHapticDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s!=PlayModeStateChange.EnteredPlayMode)return;if(SessionState.GetBool(Key+"Boot",false))ExecuteBoot();else if(SessionState.GetBool(Key,false))Execute();};}
        public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();}
        public static void RunBoot(){SessionState.SetBool(Key+"Boot",true);EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");EditorApplication.EnterPlaymode();}
        static async void ExecuteBoot()
        {
            int exit=0;GameplayView view=null;
            try
            {
                HotpotSort.Bootstrap.Bootstrap boot=null;
                for(int i=0;i<100;i++){boot=UnityEngine.Object.FindFirstObjectByType<HotpotSort.Bootstrap.Bootstrap>();if(boot&&boot.IsConfigured)break;await Task.Delay(50);}
                Check(boot&&boot.IsConfigured,"actual Boot initialized");view=UnityEngine.Object.FindFirstObjectByType<GameplayView>();
                var start=view.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="Action_开始下火锅");start.onClick.Invoke();
                for(int i=0;i<120&&!view.World.Bodies.SelectMany(b=>b.data.items).Any(item=>view.IsItemClickable(item.itemId));i++)await Task.Delay(50);
                Check(view.LastSnapshot.phase==ViewPhase.Running,"actual gameplay entry reached");
                var board=view.GetComponentsInChildren<RectTransform>().First(n=>n.name=="GameplayBoard");bool collected=false;
                foreach(var body in view.World.Bodies.ToArray())
                {
                    foreach(var item in body.data.items)
                    {
                        if(!view.IsItemClickable(item.itemId))continue;var center=view.World.ItemPosition(item.itemId);
                        for(float y=-item.radius;y<=item.radius&&!collected;y+=2)for(float x=-item.radius;x<=item.radius&&!collected;x+=2)
                        {
                            var screen=RectTransformUtility.WorldToScreenPoint(null,board.TransformPoint(new Vector3(center.x+x,-center.y-y,0)));
                            if(!view.BeginScreenPress(screen,77))continue;
                            int before=view.LastSnapshot.plates.Sum(p=>p.items.Length);
                            Check(view.EndScreenPress(screen,77),"actual release accepted");
                            Check(view.LastSnapshot.plates.Sum(p=>p.items.Length)==before-1,"actual core removes exactly one collected food");
                            Check(!view.EndScreenPress(screen,77),"actual duplicate release rejected");collected=true;
                        }
                        if(collected)break;
                    }
                    if(collected)break;
                }
                Check(collected,"actual input to core collection smoke");await Task.Delay(550);Debug.Log("PRESS_HAPTIC_BOOT_PASS checks="+checks);
            }
            catch(Exception ex){exit=1;Debug.LogException(ex);}
            finally{if(view)view.World.Clear();SessionState.SetBool(Key+"Boot",false);EditorApplication.Exit(exit);}
        }
        static void Check(bool condition,string label){if(!condition)throw new Exception(label);checks++;Debug.Log("PRESS_HAPTIC_PASS "+label);}
        static void Tick(GameplayFeedback feedback,float seconds){while(seconds>0){float dt=Mathf.Min(.01f,seconds);feedback.Tick(dt);seconds-=dt;}}
        static void PressTick(GameplayView view,float seconds)=>typeof(GameplayView).GetMethod("TickPressAndLanding",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,new object[]{seconds});
        static ViewEvent Route(ViewSnapshot s,long seq,bool order,int filled=1,bool automatic=false)=>new ViewEvent{sessionId=s.sessionId,sessionGeneration=s.sessionGeneration,sequence=seq,kind=automatic?"BufferAutoAbsorbed":order?"ItemRoutedToOrder":"ItemRoutedToBuffer",sourceContainer=automatic?"Buffer":"Plate",sourceSlot=0,targetContainer=order?"Order":"Buffer",targetSlot=0,itemId="b",ingredientId="food_00",filledAfter=filled};
        static void Execute()
        {
            GameObject owner=null;int exit=0;
            try
            {
                owner=new GameObject("PressHapticFixture");var view=owner.AddComponent<GameplayView>();view.ConfigureAssets(V7Art.Root);view.enabled=false;
                // enabled is required for input; deterministic ticks are invoked synchronously below.
                view.enabled=true;var feedback=owner.GetComponent<GameplayFeedback>();feedback.enabled=false;
                var state=new ViewSnapshot{sessionId="press",sessionGeneration=1,phase=ViewPhase.Running,orders=Enumerable.Range(0,4).Select(i=>new ViewOrder{slot=i,foodId=0,required=3,enabled=i<2}).ToArray(),buffer=new ViewItem[5],plates=new[]{new ViewPlate{plateId="plate",x=210,y=550,radius=48,motion=new ViewPlateMotion{animateEntry=false},items=new[]{new ViewItem{itemId="1",foodId=0,x=-12,radius=20},new ViewItem{itemId="2",foodId=1,x=20,radius=18,drawOrder=1}}}}};
                var port=new Port{state=state};view.Bind(port);view.World.SetSimulating(false);view.SetViewport(new Rect(0,0,Screen.width,Screen.height),Screen.height);Canvas.ForceUpdateCanvases();
                var board=view.GetComponentsInChildren<RectTransform>().First(n=>n.name=="GameplayBoard");
                Func<Vector2,Vector2> screen=p=>RectTransformUtility.WorldToScreenPoint(null,board.TransformPoint(new Vector3(p.x,-p.y,0)));
                Vector2 hit=Vector2.zero;bool found=false;
                for(int y=535;y<=565&&!found;y+=2)for(int x=180;x<=204&&!found;x+=2)if(view.BeginScreenPress(screen(new Vector2(x,y)),7)){hit=new Vector2(x,y);found=true;}
                Check(found,"real alpha hit starts press");Check(port.taps==0,"press does not submit");
                var item=view.GetComponentsInChildren<RectTransform>().First(n=>n.name=="PlateFood_1");
                PressTick(view,.08f);Check(Mathf.Abs(item.localScale.x-1.12f)<.001f&&item.GetSiblingIndex()==item.parent.childCount-1,"hold reaches approved scale and front layer");
                Check(!view.EndScreenPress(screen(hit),8)&&port.taps==0,"other pointer cannot release locked item");
                Check(view.EndScreenPress(screen(hit+Vector2.right*8),7)&&port.taps==1,"small drift releases exactly once");
                Check(!view.EndScreenPress(screen(hit),7)&&port.taps==1,"duplicate release ignored");
                Check(item.localScale==Vector3.one&&item.GetSiblingIndex()==1,"release restores scale and sibling");view.AcknowledgeTap("1");
                Check(view.BeginScreenPress(screen(hit),7)&&view.EndScreenPress(screen(hit),7)&&port.taps==2,"same-frame quick tap submits once");view.AcknowledgeTap("1");
                view.BeginScreenPress(screen(hit),7);Check(!view.UpdateScreenPress(screen(hit+Vector2.right*15),7)&&!view.EndScreenPress(screen(hit),7)&&port.taps==2,"beyond tolerance cancels permanently");
                view.BeginScreenPress(screen(hit),7);state.pauseReasons=ViewPauseReasons.User;port.Emit();Check(!view.EndScreenPress(screen(hit),7)&&item.localScale==Vector3.one,"pause cancels held press");state.pauseReasons=ViewPauseReasons.None;port.Emit();
                var haptics=new List<GameplayHapticKind>();view.HapticRequested+=haptics.Add;
                typeof(GameplayView).GetField("lastLightHapticAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(view,double.NegativeInfinity);
                view.BeginScreenPress(screen(hit),7);view.CancelScreenPress(7);Check(haptics.SequenceEqual(new[]{GameplayHapticKind.Light}),"valid press emits light and cancellation emits nothing extra");
                haptics.Clear();typeof(GameplayView).GetField("lastLightHapticAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(view,double.NegativeInfinity);
                state.buffer[0]=new ViewItem{itemId="b",foodId=0};var arrival=Route(state,1,false);port.Emit(arrival);Tick(feedback,.35f);
                var food=view.GetComponentsInChildren<RectTransform>().First(n=>n.parent.name=="Buffer_0"&&n.name=="Food_0");Check(Mathf.Abs(food.localScale.x-.92f)<.001f,"buffer arrival begins shrink");
                Check(haptics.SequenceEqual(new[]{GameplayHapticKind.Light}),"non-completing buffer arrival emits light");
                PressTick(view,.06f);Check(food.localScale.x>1.07f,"buffer arrival expands");PressTick(view,.08f);Check(food.localScale==Vector3.one,"buffer arrival settles");
                haptics.Clear();port.Emit(Route(state,2,true,3));Tick(feedback,.35f);Check(haptics.SequenceEqual(new[]{GameplayHapticKind.Medium}),"completing arrival replaces light with one medium");
                port.Emit(Route(state,2,true,3));Tick(feedback,.4f);Check(haptics.Count==1,"duplicate event does not vibrate twice");
                haptics.Clear();port.Emit(Route(state,3,true,3,true));Tick(feedback,.35f);Check(haptics.Count==0,"automatic flight preserves longer duration");Tick(feedback,.12f);Check(haptics.SequenceEqual(new[]{GameplayHapticKind.Medium}),"automatic completion uses medium");
                haptics.Clear();port.Emit(Route(state,4,true,3));state.pauseReasons=ViewPauseReasons.Background;port.Emit();Tick(feedback,.4f);state.pauseReasons=ViewPauseReasons.None;port.Emit();Tick(feedback,.4f);Check(haptics.Count==0,"background invalidates old arrival haptics after resume");
                port.Emit(Route(state,5,true,3));state.sessionGeneration++;port.Emit();Tick(feedback,.4f);Check(haptics.Count==0,"new generation cancels old arrival");
                port.Emit(Route(state,1,true,3));state.phase=ViewPhase.Aborted;port.Emit();Tick(feedback,.4f);Check(haptics.Count==0,"terminal state cancels arrival");
                state.phase=ViewPhase.Running;port.Emit();haptics.Clear();view.RequestHaptic(GameplayHapticKind.Medium);view.RequestHaptic(GameplayHapticKind.Light);view.RequestHaptic(GameplayHapticKind.Medium);Check(haptics.SequenceEqual(new[]{GameplayHapticKind.Medium,GameplayHapticKind.Medium}),"light throttle never drops completion medium");
                state.sessionGeneration++;port.Emit();haptics.Clear();
                typeof(GameplayView).GetField("lastLightHapticAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(view,double.NegativeInfinity);
                for(int i=0;i<64;i++)feedback.Highlight(()=>new Vector2(210,550));
                Check(feedback.ActiveEffectCount==Mathf.Clamp(view.VisualArt.Theme.effects.concurrentEffects,1,64),"VFX pool saturated at configured limit");
                int oldFlights=view.GetComponentsInChildren<RectTransform>().Count(n=>n.name=="FlyingItem");
                port.Emit(Route(state,1,false));Check(view.GetComponentsInChildren<RectTransform>().Count(n=>n.name=="FlyingItem")==oldFlights,"saturated pool allocates no flight");
                Tick(feedback,.33f);Check(haptics.Count==0,"no-flight arrival waits original duration");Tick(feedback,.02f);
                Check(haptics.SequenceEqual(new[]{GameplayHapticKind.Light})&&Mathf.Abs(food.localScale.x-.92f)<.001f,"no-flight buffer arrival still pulses and vibrates");
                haptics.Clear();port.Emit(Route(state,2,true,3,true));Tick(feedback,.45f);Check(haptics.Count==0,"no-flight auto arrival waits longer duration");Tick(feedback,.02f);
                Check(haptics.SequenceEqual(new[]{GameplayHapticKind.Medium}),"no-flight completion emits medium once");
                haptics.Clear();port.Emit(Route(state,3,true,3));state.pauseReasons=ViewPauseReasons.User;port.Emit();state.pauseReasons=ViewPauseReasons.None;port.Emit();Tick(feedback,.35f);
                Check(haptics.Count==0,"no-flight arrival cancelled by pause and resume");
                Debug.Log("PRESS_HAPTIC_RUNTIME_PASS checks="+checks);
            }
            catch(Exception ex){exit=1;Debug.LogException(ex);}
            finally{if(owner){owner.GetComponent<GameplayView>().ReleaseAssetReferences();UnityEngine.Object.Destroy(owner);}SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
        }
    }
}
#endif
