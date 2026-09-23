#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HotpotSort.Bootstrap;
using HotpotSort.Presentation;
using HotpotSort.Contracts;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Prior=HotpotSort.Task001V7.VisualCapture;

namespace HotpotSort.Task001V8
{
 [InitializeOnLoad] public static class VisualIntegrationCapture
 {
  const string Active="VisualV8.Active",PathKey="VisualV8.Path";
  const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
  static string root;static int sequence;static readonly List<string> checks=new List<string>();
  static Func<int,PlateFoodShape> shape;
  static VisualIntegrationCapture(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Active,false))Execute();};}
  public static void Run(){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"-visualEvidence");if(i<0)throw new Exception("Evidence path required");SessionState.SetString(PathKey,a[i+1]);SessionState.SetBool(Active,true);Directory.CreateDirectory(a[i+1]);EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");EditorApplication.EnterPlaymode();}
  static void Check(bool ok,string label){checks.Add((ok?"PASS ":"FAIL ")+label);if(!ok)throw new Exception(label);}
  static object Invoke(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,Private).Invoke(obj,args);
  static void Capture(GameplayView view,string label){Invoke(view,"LateUpdate");foreach(var s in new[]{new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1440,3200)})Prior.Capture(view,s.x,s.y,root+"/"+label+"-"+s.x+"x"+s.y+".png");}
  static ViewSnapshot Layout(ViewSnapshot s){foreach(var p in s.plates){for(int i=0;i<p.items.Length;i++)p.items[i].sourceIndex=i;var l=PlateItemLayout.Create(12345,p.plateId,p.radius,p.items,shape);p.items=l.Items;p.layoutVersion=PlateItemLayout.Version;p.initialItemCount=p.items.Length;p.envelopeRatio=l.EnvelopeRatio;Check(l.MinimumVisibleFraction>=.75f,"visible75 "+p.plateId);Check(Mathf.Abs(PlateItemLayout.MeasureEnvelope(p.items,shape,p.radius)-.8f)<.001f,"envelope80 "+p.plateId);}return s;}
  static Prior.FullPort Bind(GameplayView view,ViewSnapshot s){s.sessionId="v8-visual-"+(++sequence);s.sessionGeneration=sequence;var p=new Prior.FullPort(s,true);view.Bind(p);view.SetForeground(true);view.World.enabled=false;view.World.SetSimulating(true);Physics2D.SyncTransforms();view.GetComponent<GameplayFeedback>().enabled=false;return p;}
  static void Viewport(GameplayView view){view.SetViewport(new Rect(0,0,Screen.width,Screen.height),Screen.height);Canvas.ForceUpdateCanvases();}
  static void CheckRendering(GameplayView view,ViewSnapshot s)
  {
   foreach(var p in s.plates)foreach(var item in p.items){var node=view.GetComponentsInChildren<RectTransform>().Single(n=>n.name=="PlateFood_"+item.itemId);var image=node.GetComponent<RawImage>();Check(Vector2.Distance(node.anchoredPosition,new Vector2(item.x,-item.y))<.001f,"center "+item.itemId);Check(Mathf.Abs(Mathf.DeltaAngle(node.localEulerAngles.z,-item.rotationDegrees))<.001f,"rotation "+item.itemId);Check(Mathf.Abs(node.sizeDelta.x-item.radius*2)<.001f,"size "+item.itemId);Check(image.uvRect==new Rect(item.uvX,item.uvY,item.uvWidth,item.uvHeight),"uv "+item.itemId);}
   foreach(var p in s.plates){var ids=view.GetComponentsInChildren<RectTransform>().Where(n=>n.parent&&n.parent.name=="PlateVisual_"+p.plateId&&n.name.StartsWith("PlateFood_")).Select(n=>n.name).ToArray();Check(ids.SequenceEqual(p.items.OrderBy(i=>i.drawOrder).Select(i=>"PlateFood_"+i.itemId)),"draw order "+p.plateId);}
  }
  static Vector2 OracleLocal(ViewItem item,float dx,float dy){double a=item.rotationDegrees*Math.PI/180;return new Vector2((float)(dx*Math.Cos(a)+dy*Math.Sin(a)),(float)(-dx*Math.Sin(a)+dy*Math.Cos(a)));}
  static void HitCheck(GameplayView view,ViewSnapshot s)
  {
   Viewport(view);int samples=0,transparentPass=0;Func<ViewItem,Vector2,bool> alpha=(item,local)=>(bool)Invoke(view,"OpaqueHit",item,local);
   foreach(var p in s.plates)
   {
    var ordered=p.items.OrderByDescending(i=>i.drawOrder).ToArray();
    for(float y=p.y-p.radius;y<p.y+p.radius;y+=3)for(float x=p.x-p.radius;x<p.x+p.radius;x+=3)
    {
     if(!view.PlateCrop.Contains(new Vector2(x,y))||(new Vector2(x-p.x,y-p.y)).sqrMagnitude>p.radius*p.radius)continue;
     string expected=null;bool skipped=false;
     foreach(var item in ordered){var q=OracleLocal(item,x-p.x-item.x,y-p.y-item.y);if(Mathf.Abs(q.x)>item.radius||Mathf.Abs(q.y)>item.radius)continue;var uv=new Vector2(item.uvX+item.uvWidth*(.5f+q.x/(2*item.radius)),item.uvY+item.uvHeight*(.5f-q.y/(2*item.radius)));if(view.FoodTexture(item.foodId).GetPixelBilinear(uv.x,uv.y).a>.1f){expected=item.itemId;if(skipped)transparentPass++;break;}skipped=true;}
     string actual=view.World.Hit(new Vector2(x,y),alpha);if(expected!=actual)throw new Exception("alpha mismatch "+x+","+y+" expected "+expected+" actual "+actual);samples++;
    }
   }
   Check(samples>1000,"rotated alpha actual colliders "+samples+" points");Check(transparentPass>0,"transparent top texture permits lower hit "+transparentPass);
   foreach(var item in s.plates.SelectMany(p=>p.items))Check(view.IsItemClickable(item.itemId),"clickable scan "+item.itemId);
   var hint=s.plates[1].items[0];view.HighlightItem(hint.itemId);for(int tick=0;tick<5;tick++)view.GetComponent<GameplayFeedback>().Tick(.1f);
   var hintNode=view.GetComponentsInChildren<RectTransform>().First(n=>n.name=="Hint"&&n.gameObject.activeInHierarchy);Check(Mathf.Abs(Mathf.DeltaAngle(hintNode.localEulerAngles.z,-hint.rotationDegrees))<.001f,"hint shares item rotation");Check(Vector2.Distance(hintNode.anchoredPosition,new Vector2(view.World.ItemPosition(hint.itemId).x,-view.World.ItemPosition(hint.itemId).y))<.001f,"hint shares center");Capture(view,"rotated-alpha-hint");
  }
  static async void Execute()
  {
   root=SessionState.GetString(PathKey,"");int exit=0;string qaKey="HotpotSort.Task001.v8.VisualFinal."+Guid.NewGuid().ToString("N");
   try
   {
    HotpotSort.Bootstrap.Bootstrap boot=null;for(int i=0;i<100;i++){boot=UnityEngine.Object.FindFirstObjectByType<HotpotSort.Bootstrap.Bootstrap>();if(boot&&boot.IsConfigured)break;await Task.Delay(50);}Check(boot&&boot.IsConfigured,"Boot compiled and configured");
    var composition=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();var view=composition.PlayerView;shape=id=>(PlateFoodShape)Invoke(composition,"LoadPlateShape",id);var local=new LocalDevelopmentServices(qaKey);composition.ConfigureServices(local,local,local,local);
    Capture(view,"entry");Check(view.VisualArt.Theme.anchors.supplyGate==140,"runtime theme gate140");
    await boot.Controller.StartTodayAsync();await Task.Delay(3200);Viewport(view);string liveHint=view.FindClickableHint();Check(liveHint!=null,"live Boot has clickable hint");
    var board=(RectTransform)typeof(GameplayView).GetField("board",Private).GetValue(view);bool accepted=false;
    foreach(var body in view.World.Bodies){foreach(var item in body.data.items){var at=view.World.ItemPosition(item.itemId);var screen=RectTransformUtility.WorldToScreenPoint(null,board.TransformPoint(new Vector3(at.x,-at.y,0)));if(view.SubmitScreenTap(screen)){accepted=true;break;}}if(accepted)break;}
    Check(accepted,"live Boot actual input accepted");await Task.Delay(300);boot.Controller.Request(SessionAction.Pause);Capture(view,"live-core-paused");boot.Controller.Request(SessionAction.Resume);Check(view.LastSnapshot.phase==ViewPhase.Running,"live resume");boot.Controller.Request(SessionAction.Pause);
    var s=Layout(Prior.Fixture());Bind(view,s);view.SetRemainingTime(462);Capture(view,"gameplay-same-state");CheckRendering(view,s);HitCheck(view,s);File.WriteAllText(root+"/same-state.json",JsonUtility.ToJson(s,true));
    s=Layout(Prior.Fixture());s.plates=s.plates.GroupBy(p=>p.items.Length).OrderBy(g=>g.Key).Select(g=>g.First()).ToArray();for(int i=0;i<5;i++){s.plates[i].x=i%2==0?115:305;s.plates[i].y=365+i/2*170;}Bind(view,s);Capture(view,"count-1-5");
    s=Layout(Prior.Fixture());foreach(var o in s.orders)o.enabled=true;Bind(view,s);Capture(view,"four-pots");
    var list=new List<ViewPlate>();int id=500;for(int row=0;row<5;row++)for(int col=0;col<4;col++)list.Add(new ViewPlate{plateId="dense-"+row+"-"+col,x=55+col*103,y=342+row*99,radius=45,motion=new ViewPlateMotion{animateEntry=false},items=new[]{new ViewItem{itemId=(id++).ToString(),foodId=(row*4+col)%16},new ViewItem{itemId=(id++).ToString(),foodId=(row*4+col+5)%16}}});s.plates=list.ToArray();Layout(s);Bind(view,s);Capture(view,"crowded");
    s=Layout(Prior.Fixture());s.plates=new[]{s.plates[1]};s.plates[0].x=150;
    foreach(float y in new[]{-59f,139,140,250,292,355}){s.plates[0].y=y;Bind(view,s);Capture(view,"entry-y"+y);Viewport(view);if(y<=140)foreach(var item in s.plates[0].items)Check(!view.IsItemClickable(item.itemId),"hidden not clickable "+y+" "+item.itemId);}
    // Real per-body gravity and entry impulse, manually stepped; no synthetic teleports.
    s.plates[0].y=-59;s.plates[0].motion=new ViewPlateMotion{animateEntry=true};Bind(view,s);var bodyEntry=view.World.Bodies.First();float last=-59;var trajectory=new List<string>();int captured=0;
    for(int step=0;step<180;step++){Invoke(view.World,"FixedUpdate");float y=view.World.Position(bodyEntry).y;Check(y>=last-.01f,"entry downward step "+step);last=y;trajectory.Add(step+","+y.ToString("R",System.Globalization.CultureInfo.InvariantCulture));if(captured<3&&y>new[]{140f,242f,355f}[captured]){Capture(view,"physical-entry-"+captured);captured++;}if(captured==3)break;}
    Check(captured==3,"physical entry crossed gate rim and full visibility");File.WriteAllLines(root+"/physical-entry.csv",trajectory);
    foreach(var phase in new[]{ViewPhase.Paused,ViewPhase.Won,ViewPhase.Overflow}){s=Layout(Prior.Fixture());s.phase=phase;s.pauseReasons=phase==ViewPhase.Paused?ViewPauseReasons.User:ViewPauseReasons.None;s.message=phase==ViewPhase.Overflow?"Timeout":null;Bind(view,s);Capture(view,"regression-"+phase);if(phase!=ViewPhase.Paused)Check(!view.GetComponentsInChildren<Button>().Any(b=>b.name.Contains("分享")),"no result share "+phase);}
    s=Layout(Prior.Fixture());for(int i=0;i<5;i++)s.buffer[i]=new ViewItem{itemId="buffer-"+i,foodId=i*3};Bind(view,s);Capture(view,"buffer-upright");foreach(var n in view.GetComponentsInChildren<RectTransform>().Where(n=>n.name.StartsWith("Food_")))Check(Mathf.Abs(Mathf.DeltaAngle(n.localEulerAngles.z,0))<.001f,"order buffer upright "+n.name);
    s=Layout(Prior.Fixture());s.revivalPending=s.revivalUsed=true;s.pauseReasons=ViewPauseReasons.Revival;var revival=Bind(view,s);
    var batch=new RevivalTransferBatch{token=new RevivalCompletionToken{sessionId=s.sessionId,sessionGeneration=s.sessionGeneration,transactionId="v8-revive",revivalOfferId="offer",requestId="request",newPlateId="queue-only",completionToken="v8-completion",eventSeq=1},items=Enumerable.Range(0,5).Select(i=>new RevivalTransferItem{itemId="buffer-"+i,ingredientId="food_"+(i*3).ToString("00"),sourceSlot=i,targetIndex=i}).ToArray()};s.revivalTransfer=batch;revival.Emit(new[]{new ViewEvent{sessionId=s.sessionId,sessionGeneration=s.sessionGeneration,sequence=1,kind="RevivalTransferStarted",revivalTransfer=batch}});var feedback=view.GetComponent<GameplayFeedback>();for(int i=0;i<5;i++)feedback.Tick(.1f);Capture(view,"revival-gather");for(int i=0;i<4;i++)feedback.Tick(.1f);Check(revival.completions==1&&!feedback.RevivalActive,"revival completion once without active-board insertion");Capture(view,"revival-complete");
    view.ShowSettings(new HotpotSort.Contracts.PlayerSettings(),_=>{});Capture(view,"settings");view.GetComponentsInChildren<Button>().First(b=>b.name=="Action_完成").onClick.Invoke();view.ShowNotice("好友榜 · 开发模拟","");Capture(view,"friends");view.GetComponentsInChildren<Button>().First(b=>b.name=="Action_知道了").onClick.Invoke();
    var reward=view.ShowRewardSimulationAsync(new RewardRequest(s.sessionGeneration,RewardKind.Hint,RewardRoute.SimulatedShare,"fixture",s.sessionId));Capture(view,"reward-share");view.GetComponentsInChildren<Button>().First(b=>b.name=="Action_取消").onClick.Invoke();Check(await reward==RewardOutcome.Cancelled,"reward simulation cancel preserved");
    File.WriteAllText(root+"/result.json","{\"status\":\"Visual Integration Ready for Pre-delivery QA\",\"scope\":\"Unity PlayMode renderer and targeted visual self checks; live Boot input and controlled physics entry; fixtures for composed states. Not final full QA or target-device acceptance.\",\"checks\":"+checks.Count+"}");Debug.Log("V8_VISUAL_INTEGRATION_OK");
   }
   catch(Exception e){exit=1;File.WriteAllText(root+"/error.txt",e.ToString());Debug.LogException(e);}
   finally{PlayerPrefs.DeleteKey(qaKey);PlayerPrefs.Save();File.WriteAllLines(root+"/checks.txt",checks);SessionState.SetBool(Active,false);EditorApplication.Exit(exit);}
  }
 }
}
#endif
