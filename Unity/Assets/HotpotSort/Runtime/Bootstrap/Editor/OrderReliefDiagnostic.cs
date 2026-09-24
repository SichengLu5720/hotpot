#if UNITY_EDITOR
using System;
using System.Linq;
using System.Threading.Tasks;
using HotpotSort.Presentation;
using HotpotSort.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace HotpotSort.Bootstrap
{
    [InitializeOnLoad] public static class OrderReliefDiagnostic
    {
        const string Key="OrderReliefDiagnostic";static int checks;
        static OrderReliefDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))RunChecks();};}
        public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");EditorApplication.EnterPlaymode();}
        static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;Debug.Log("ORDER_RELIEF_PASS "+label);}
        static async void RunChecks()
        {
            int exit=0;GameplayView view=null;
            try
            {
                var guard=Type.GetType("HotpotSort.ContentImport.DailyContentBuildGuard, HotpotSort.ContentImport",true);
                guard.GetMethod("Validate").Invoke(null,null);Check(true,"production content importer and manifest exact");
                Bootstrap boot=null;
                for(int i=0;i<100;i++){boot=UnityEngine.Object.FindFirstObjectByType<Bootstrap>();if(boot&&boot.IsConfigured)break;await Task.Delay(50);}
                Check(boot&&boot.IsConfigured,"Boot ready");view=UnityEngine.Object.FindFirstObjectByType<GameplayView>();
                var composition=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();
                var start=view.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="Action_开始下火锅");start.onClick.Invoke();
                for(int i=0;i<120&&!(view.CaptureClickableItemIds()?.Length>0);i++)await Task.Delay(50);
                var captured=view.CaptureClickableItemIds();Check(captured!=null&&captured.Length>0,"operation captures real clickable food");
                Check(composition.ActiveCore.ExportReplay().CanonicalJson.Contains(DailyContent.CurrentVersion),"Boot new content identity");
                Check(captured.All(view.IsItemClickable),"captured IDs share production Alpha/crop predicate");
                int knownTotal=0,unknownTotal=0;double captureTotal=0,backgroundMax=0;
                for(int sample=0;sample<60;sample++)
                {
                    await Task.Delay(50);var timer=System.Diagnostics.Stopwatch.StartNew();var observed=view.CaptureClickability();timer.Stop();captureTotal+=timer.Elapsed.TotalMilliseconds;
                    if(observed!=null){unknownTotal+=observed.unknown.Length;knownTotal+=view.LastSnapshot.plates.Sum(p=>p.items.Length)-observed.unknown.Length;}
                    backgroundMax=Math.Max(backgroundMax,view.ClickCacheLastMilliseconds);
                }
                Debug.Log("ORDER_RELIEF_CACHE productionCaptureMeanMs="+captureTotal/60+" backgroundSampleMaxMs="+backgroundMax+" known="+knownTotal+" unknown="+unknownTotal);
                var body=view.World.Bodies.First(b=>b.data.items.Any(i=>captured.Contains(i.itemId)));var position=body.rigidbody.position;
                body.rigidbody.position=new Vector2(position.x,-.5f);body.node.transform.position=body.rigidbody.position;Physics2D.SyncTransforms();
                Check(!view.CaptureClickableItemIds().Any(id=>body.data.items.Any(i=>i.itemId==id)),"off-crop generated food excluded");
                body.rigidbody.position=position;body.node.transform.position=position;Physics2D.SyncTransforms();
                var board=view.GetComponentsInChildren<RectTransform>().First(n=>n.name=="GameplayBoard");bool collected=false;
                foreach(var plate in view.World.Bodies.ToArray())
                {
                    foreach(var item in plate.data.items)
                    {
                        if(!view.IsItemClickable(item.itemId))continue;var center=view.World.ItemPosition(item.itemId);
                        for(float y=-item.radius;y<=item.radius&&!collected;y+=2)for(float x=-item.radius;x<=item.radius&&!collected;x+=2)
                        {
                            var screen=RectTransformUtility.WorldToScreenPoint(null,board.TransformPoint(new Vector3(center.x+x,-center.y-y,0)));
                            if(!view.BeginScreenPress(screen,77))continue;
                            int before=view.LastSnapshot.plates.Sum(p=>p.items.Length);var inputTimer=System.Diagnostics.Stopwatch.StartNew();bool accepted=view.EndScreenPress(screen,77);inputTimer.Stop();Check(accepted,"real press release accepted");Debug.Log("ORDER_RELIEF_CACHE ordinaryFullReleaseMs="+inputTimer.Elapsed.TotalMilliseconds+" singleFoodFallbacks="+view.ClickReleaseFallbacks+" fallbackMs="+view.ClickReleaseFallbackMilliseconds);
                            Check(view.LastSnapshot.plates.Sum(p=>p.items.Length)==before-1,"core consumes one item");collected=true;
                        }
                        if(collected)break;
                    }
                    if(collected)break;
                }
                Check(collected,"production input chain");var replay=composition.ActiveCore.ExportReplay();
                Check(!replay.CanonicalJson.Contains("\"clickability\""),"ordinary production tap skips unnecessary observation");
                bool refilled=false;double refillMs=0;long seq=1000;
                for(int attempt=0;attempt<160&&!refilled;attempt++)
                {
                    await Task.Delay(50);
                    var observation=view.CaptureClickability();if(observation==null)continue;
                    var next=view.LastSnapshot.plates.SelectMany(p=>p.items).FirstOrDefault(i=>observation.clickable.Contains(i.itemId)&&view.LastSnapshot.orders.Any(o=>o.enabled&&o.foodId==i.foodId&&o.count<o.required));
                    if(next==null)continue;
                    bool completes=composition.ActiveCore.MayRefillOrdersOnTap(int.Parse(next.itemId));
                    var timer=System.Diagnostics.Stopwatch.StartNew();composition.Tap(new ViewTap{itemId=next.itemId,inputSeq=seq++,snapshotRevision=view.LastSnapshot.revision});timer.Stop();
                    if(completes){refilled=true;refillMs=timer.Elapsed.TotalMilliseconds;}
                }
                Check(refilled,"real production refill command reached");
                replay=composition.ActiveCore.ExportReplay();Check(replay.CanonicalJson.Contains("\"clickability\""),"refill records cached observation");
                Debug.Log("ORDER_RELIEF_CACHE fullRefillCommandMs="+refillMs+" singleFoodFallbacks="+view.ClickReleaseFallbacks);
                var asset=(TextAsset)typeof(DailyProductionComposition).GetField("dailyContent",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(composition);
                var replayType=Type.GetType("HotpotSort.Replay.DailyReplay, HotpotSort.Replay",true);
                var replayResult=replayType.GetMethod("Run").Invoke(null,new object[]{DailySessionFactory.FromProductionJson(asset.text),replay});
                Check((bool)replayResult.GetType().GetProperty("Success").GetValue(replayResult),"production command replay exact");
                Debug.Log("ORDER_RELIEF_COMPLETE checks="+checks);
            }
            catch(Exception ex){exit=1;Debug.LogException(ex);}
            finally{if(view)view.World.Clear();SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
        }
    }
}
#endif
