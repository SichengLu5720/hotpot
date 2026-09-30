#if UNITY_EDITOR
using System;
using System.Linq;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug=UnityEngine.Debug;
namespace HotpotSort.Presentation
{
    [InitializeOnLoad] public static class ClickLatencyDiagnostic
    {
        const string Key="ClickLatencyDiagnostic";
        sealed class Port:IPresentationPort
        {public ViewSnapshot state;public event Action<ViewUpdate> Updated;public ViewSnapshot Read()=>state;public void Tap(ViewTap c){}public void SessionAction(ViewAction a){}public void ObserveSupply(ViewSupplyObservation o){} }
        static ClickLatencyDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();}
        static void Execute()
        {
            int exit=0;GameObject node=null;Texture2D texture=null;
            try
            {
                node=new GameObject("ClickLatencyFixture");var view=node.AddComponent<GameplayView>();view.ConfigureAssets(V7Art.Root);
                texture=new Texture2D(64,64);texture.SetPixels(Enumerable.Repeat(Color.white,4096).ToArray());texture.Apply();
                var foods=(Texture2D[])typeof(GameplayView).GetField("foods",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(view);foods[0]=texture;
                var state=new ViewSnapshot{sessionId="latency",sessionGeneration=1,phase=ViewPhase.Running,orders=new ViewOrder[0],plates=Enumerable.Range(0,25).Select(i=>new ViewPlate{plateId=i.ToString(),x=50+(i%5)*80,y=340+(i/5)*100,radius=39,motion=new ViewPlateMotion{animateEntry=false},items=new[]{new ViewItem{itemId=(i*2+1).ToString(),foodId=0,radius=20},new ViewItem{itemId=(i*2+2).ToString(),foodId=0,radius=20,drawOrder=1}}}).ToArray()};
                view.Bind(new Port{state=state});view.World.SetSimulating(false);view.SetViewport(new Rect(0,0,Screen.width,Screen.height),Screen.height);Canvas.ForceUpdateCanvases();
                var watch=Stopwatch.StartNew();var ids=view.CaptureClickableItemIds();watch.Stop();
                Debug.Log("CLICK_LATENCY_CACHE coldCaptureMs="+watch.Elapsed.TotalMilliseconds+" known="+ids.Length+" total=50 texture=64 densePlates=25");
                watch.Restart();bool hidden=view.IsItemClickable("1");watch.Stop();Debug.Log("CLICK_LATENCY_BASELINE singleHiddenMs="+watch.Elapsed.TotalMilliseconds+" clickable="+hidden);
                var tick=typeof(GameplayView).GetMethod("TickClickabilityCache",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                double maxFrame=0,maxCapture=0,totalCapture=0;int maxSamples=0;
                for(int frame=0;frame<120;frame++)
                {
                    foreach(var body in view.World.Bodies)body.rigidbody.position+=new Vector2(0,.0001f);
                    UnityEngine.Physics2D.SyncTransforms();tick.Invoke(view,null);maxFrame=Math.Max(maxFrame,view.ClickCacheLastMilliseconds);maxSamples=Math.Max(maxSamples,view.ClickCacheLastSamples);
                    watch.Restart();var observed=view.CaptureClickability();watch.Stop();maxCapture=Math.Max(maxCapture,watch.Elapsed.TotalMilliseconds);totalCapture+=watch.Elapsed.TotalMilliseconds;
                    if(observed.clickable.Length!=25||observed.unknown.Length!=0)throw new Exception("Dense moving coverage not exact");
                    if(observed.clickable.Any(id=>!view.IsItemClickable(id)))throw new Exception("False visible witness");
                }
                if(maxSamples>128)throw new Exception("Sample budget exceeded");
                Debug.Log("CLICK_LATENCY_CACHE warmCaptureMeanMs="+totalCapture/120+" maxCaptureMs="+maxCapture+" backgroundMaxMs="+maxFrame+" maxConservativeSamples="+maxSamples+" movingCoverage=50/50 frames=120");
                var first=view.World.Bodies.First();var saved=first.rigidbody.position;first.rigidbody.position=new Vector2(saved.x,-10);Physics2D.SyncTransforms();
                if(view.CaptureClickability().clickable.Any(id=>first.data.items.Any(i=>i.itemId==id)))throw new Exception("Stale crop witness");
                first.rigidbody.position=saved;Physics2D.SyncTransforms();
                first.data.items=first.data.items.Take(1).ToArray();view.World.Reconcile(state);Physics2D.SyncTransforms();
                if(!view.CaptureClickability().clickable.Contains(first.data.items[0].itemId))throw new Exception("Removed occluder negative not invalidated");
                state.pauseReasons=ViewPauseReasons.Background;
                if(view.CaptureClickability()!=null)throw new Exception("Paused observation available");state.pauseReasons=ViewPauseReasons.None;
                state.sessionId="new-session";view.CaptureClickability();
                var cache=typeof(GameplayView).GetField("clickCache",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(view) as System.Collections.IDictionary;
                if(cache.Count!=49)throw new Exception("Session cache not cleared");
                state.sessionGeneration++;view.CaptureClickability();
                // A stale original witness must not reject a still-clickable crop sliver.
                var top=first.data.items[0];first.rigidbody.position=new Vector2((view.PlateCrop.xMin-top.radius+1)*.01f,saved.y);Physics2D.SyncTransforms();
                state.sessionGeneration++;view.CaptureClickability();cache.Clear();
                var pressType=typeof(GameplayView).GetNestedType("PressedItem",System.Reflection.BindingFlags.NonPublic);
                var press=Activator.CreateInstance(pressType,true);pressType.GetField("itemId").SetValue(press,top.itemId);pressType.GetField("hitOffset").SetValue(press,new Vector2(-999,0));
                bool rescued=(bool)typeof(GameplayView).GetMethod("ValidateReleasedFood",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(view,new[]{press});
                if(!rescued||view.ClickReleaseFallbacks!=1)throw new Exception("Sliver release fallback not preserved");
                Debug.Log("CLICK_LATENCY_CACHE sliverRescue=PASS fallbackCount="+view.ClickReleaseFallbacks+" fallbackMs="+view.ClickReleaseFallbackMilliseconds);
                Debug.Log("CLICK_LATENCY_CACHE invalidation=crop,removed-occluder,pause,session PASS");
            }
            catch(Exception ex){exit=1;Debug.LogException(ex);}
            finally{if(node)UnityEngine.Object.DestroyImmediate(node);if(texture)UnityEngine.Object.DestroyImmediate(texture);SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
        }
    }
}
#endif
