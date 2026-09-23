#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using HotpotSort.Contracts;
using HotpotSort.Contracts.RemoteAssets;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Capture=HotpotSort.Task001V7.VisualCapture;
namespace HotpotSort.Presentation
{
    [InitializeOnLoad]
    public static class Task012VisualCapture
    {
        const string Key="Task012.VisualCapture";
        static string root;static int generation;static GameplayView view;
        static readonly List<string> checks=new List<string>();
        static Task012VisualCapture(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run(){SessionState.SetBool(Key,true);UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);EditorApplication.EnterPlaymode();}
        static void Check(bool value,string label){checks.Add((value?"PASS ":"FAIL ")+label);if(!value)throw new Exception(label);}
        static void Shot(string name,bool all=false){foreach(var size in all?new[]{new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1440,3200)}:new[]{new Vector2Int(1080,1920)})Capture.Capture(view,size.x,size.y,root+"/"+name+"-"+size.x+"x"+size.y+".png");}
        static PlateFoodShape Shape(int id)
        {
            var t=view.VisualArt.Texture("food."+id.ToString("00"));var uv=view.VisualArt.FoodUv(id);const int n=64;var alpha=new byte[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)alpha[y*n+x]=(byte)Mathf.RoundToInt(t.GetPixelBilinear(uv.x+(x+.5f)/n*uv.width,uv.y+(1-(y+.5f)/n)*uv.height).a*255);
            return new PlateFoodShape(alpha,n,uv.x,uv.y,uv.width,uv.height);
        }
        static Capture.FullPort Bind(ViewSnapshot state)
        {
            foreach(var p in state.plates)if(p.items.Length>0){for(int i=0;i<p.items.Length;i++)p.items[i].sourceIndex=i;var layout=PlateItemLayout.Create(12345,p.plateId,p.radius,p.items,Shape);p.items=layout.Items;p.layoutVersion=PlateItemLayout.Version;p.initialItemCount=p.items.Length;p.envelopeRatio=layout.EnvelopeRatio;}
            state.sessionId="task012-"+(++generation);state.sessionGeneration=generation;var port=new Capture.FullPort(state,false);view.Bind(port);view.SetForeground(false);view.SetRemainingTime(582);return port;
        }
        static ViewSnapshot Crowded(){var s=Capture.Fixture();foreach(var o in s.orders)o.enabled=true;var ps=new List<ViewPlate>();for(int row=0;row<5;row++)for(int col=0;col<4;col++)ps.Add(new ViewPlate{plateId="crowd-"+row+"-"+col,x=55+103*col,y=342+99*row,radius=45,motion=new ViewPlateMotion{animateEntry=false},items=new[]{new ViewItem{itemId="a-"+row+col,foodId=(row*4+col)%16},new ViewItem{itemId="b-"+row+col,foodId=(row*4+col+5)%16}}});s.plates=ps.ToArray();return s;}
        static void Close(string label){view.GetComponentsInChildren<Button>().First(b=>b.name=="Action_"+label).onClick.Invoke();}
        static void LayoutChecks()
        {
            view.Bind(null);Shot("entry-contain-check");var hero=view.GetComponentsInChildren<RawImage>().Single(i=>i.name=="EntryHero");Check(Mathf.Abs(hero.rectTransform.rect.width/hero.rectTransform.rect.height-1)<.001f,"square hero is contained without stretch");
            var capsule=new Rect(850,1825,180,64);view.SetViewport(new Rect(0,0,1080,1920),1920,capsule);
            var capsuleTop=new Rect(capsule.x,1920-capsule.yMax,capsule.width,capsule.height);
            foreach(var image in view.GetComponentsInChildren<RawImage>().Where(i=>i.enabled&&i.name.StartsWith("ThemeEdgeGutter_"))){var rt=image.rectTransform;var r=new Rect(rt.anchoredPosition.x,-rt.anchoredPosition.y,rt.rect.width,rt.rect.height);Check(!r.Overlaps(capsuleTop)&&r.width<=1080*.14f,"background edge stays narrow and excludes capsule");}
            view.ShowFriendBoard(()=>null,()=>{},_=>{},_=>{});Shot("friends-open-data-shell");Check(view.FriendBoardVisible,"real friend shell opens");view.CloseFriendBoardView();Check(!view.FriendBoardVisible,"real friend shell closes");
        }
        static void Tick(GameplayFeedback f,float duration){while(duration>.00001f){float dt=Mathf.Min(.01f,duration);f.Tick(dt);duration-=dt;}}
        static void Motion()
        {
            var f=view.GetComponent<GameplayFeedback>();f.enabled=false;
            var point=typeof(GameplayFeedback).GetMethod("FlightPoint",BindingFlags.NonPublic|BindingFlags.Static);
            var from=new Vector2(210,580);
            foreach(string route in new[]{"PlateOrder","PlateBuffer","BufferOrder"})
            {
                var s=Capture.Fixture();Bind(s);view.SetForeground(true);view.World.SetSimulating(false);
                bool toOrder=route!="PlateBuffer",sourceBuffer=route=="BufferOrder";var target=toOrder?view.VisualArt.Pot(1):view.VisualArt.Buffer(2);float duration=sourceBuffer?.46f:.34f;
                var evt=new ViewEvent{sessionId=s.sessionId,sessionGeneration=s.sessionGeneration,sequence=1,transactionId="flight",kind=toOrder?"ItemRoutedToOrder":"ItemRoutedToBuffer",sourceContainer=sourceBuffer?"Buffer":"Plate",sourceSlot=0,targetContainer=toOrder?"Order":"Buffer",targetSlot=toOrder?1:2,itemId="flight",ingredientId="food_00"};
                var actualFrom=sourceBuffer?view.VisualArt.Buffer(0):from;
                f.Apply(new ViewUpdate{snapshot=s,events=new[]{evt}},s,_=>from);Tick(f,duration*.5f);
                var node=view.GetComponentsInChildren<RectTransform>().Single(n=>n.name=="FlyingItem");
                var expected=(Vector2)point.Invoke(null,new object[]{actualFrom,target,.5f,0f});
                Check(Vector2.Distance(node.anchoredPosition,new Vector2(expected.x,-expected.y))<.05f,"straight live midpoint "+route);Shot("motion-"+route);
                int active=f.ActiveEffectCount;f.Apply(new ViewUpdate{snapshot=s,events=new[]{evt}},s,_=>from);Check(active==f.ActiveEffectCount,"duplicate flight ignored "+route);
                view.SetForeground(false);var at=node.anchoredPosition;Tick(f,.3f);Check(node.anchoredPosition==at,"background freezes flight "+route);view.SetForeground(true);
                Tick(f,duration*.5f-.01f);Check(node.gameObject.activeSelf&&node.name=="FlyingItem","flight stays until duration "+route);Tick(f,.02f);Check(!node.gameObject.activeSelf||node.name!="FlyingItem","flight completes at "+duration+" "+route);
            }
            var state=Capture.Fixture();for(int i=0;i<5;i++)state.buffer[i]=new ViewItem{itemId="clear-"+i,foodId=i*3};var port=Bind(state);view.SetForeground(true);view.World.SetSimulating(false);
            port.state=JsonUtility.FromJson<ViewSnapshot>(JsonUtility.ToJson(state));port.state.buffer=new ViewItem[5];var clear=new ViewEvent{sessionId=state.sessionId,sessionGeneration=state.sessionGeneration,sequence=1,transactionId="clear",kind="BufferReturnedToQueue"};port.Emit(new[]{clear});Check(f.ClearTransportCount==1,"clear transport exists");Tick(f,.3f);Shot("motion-clear-gathering");port.Emit(new[]{clear});Check(f.ClearTransportCount==1,"clear duplicate ignored");
            var nodes=view.GetComponentsInChildren<RectTransform>().Where(n=>n.name.StartsWith("ClearBufferItem_")).ToArray();var positions=nodes.Select(n=>n.anchoredPosition).ToArray();port.state.pauseReasons=ViewPauseReasons.User;port.Emit(Array.Empty<ViewEvent>());Tick(f,.3f);Check(nodes.Select(n=>n.anchoredPosition).SequenceEqual(positions),"user pause freezes clear");Bind(Capture.Fixture());Check(f.ClearTransportCount==0&&f.ActiveEffectCount==0,"session replacement clears feedback");Check(f.PoolSize<=64,"bounded effect pool");f.enabled=true;
        }
        static void Share()
        {
            view.Bind(null);var canvas=view.GetComponentInChildren<Canvas>().transform;
            var go=new GameObject("ShareCandidateOnly",typeof(RectTransform),typeof(RawImage));go.transform.SetParent(canvas,false);var rect=(RectTransform)go.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;go.GetComponent<RawImage>().texture=view.VisualArt.Texture("background.table");
            var hero=new GameObject("ShareHeroContain",typeof(RectTransform),typeof(RawImage));hero.transform.SetParent(rect,false);var hr=(RectTransform)hero.transform;hr.anchorMin=hr.anchorMax=new Vector2(.5f,.5f);hr.sizeDelta=new Vector2(640,640);hr.anchoredPosition=new Vector2(150,-20);hero.GetComponent<RawImage>().texture=view.VisualArt.Texture("hero.share");
            var text=new GameObject("ShareTitle",typeof(RectTransform),typeof(Text));text.transform.SetParent(rect,false);var tr=(RectTransform)text.transform;tr.anchorMin=tr.anchorMax=new Vector2(0,.5f);tr.pivot=new Vector2(0,.5f);tr.sizeDelta=new Vector2(400,220);tr.anchoredPosition=new Vector2(40,0);var label=text.GetComponent<Text>();label.font=view.playerFont;label.text="火锅消消\n每日挑战";label.fontSize=58;label.color=new Color32(255,245,229,255);label.alignment=TextAnchor.MiddleCenter;
            Capture.Capture(view,1000,800,root+"/share-candidate-1000x800.png");UnityEngine.Object.DestroyImmediate(go);
        }
        static void Execute()
        {
            var args=Environment.GetCommandLineArgs();root=args[Array.IndexOf(args,"-visualEvidence")+1];Directory.CreateDirectory(root);int exit=0;
            try{
                Check(TaskAssetValidation.Validate(PresentationAssets.CandidateRoot,true).Length==0,"complete v10 asset validation");
                view=new GameObject("Task012PresentationFixture").AddComponent<GameplayView>();view.ConfigureAssets(PresentationAssets.CandidateRoot);view.Bind(null);Shot("entry",true);
                view.SetAssetPreparation(new AssetPreparationSnapshot(1,"test",AssetReadiness.Downloading,AssetError.None,57,100,1));Shot("loading");
                Check(!view.GetComponentsInChildren<Button>().First(b=>b.name=="Action_开始下火锅").interactable,"loading gates start");
                view.SetAssetPreparation(new AssetPreparationSnapshot(1,"test",AssetReadiness.Failed,AssetError.NotConfigured,0,100,1));Shot("loading-failed");view.SetAssetPreparation(null);
                Bind(Capture.Fixture());Shot("normal",true);var s=Crowded();Bind(s);Shot("crowded-four-pots",true);
                for(int i=0;i<5;i++)s.buffer[i]=new ViewItem{itemId="buffer-"+i,foodId=i*3};Bind(s);Shot("five-buffer");
                s=Capture.Fixture();s.plates[0].y=285;Bind(s);Shot("partial-occlusion");Check(view.PlateCrop==new Rect(0,292,420,536),"physical crop preserved");
                s=Capture.Fixture();s.phase=ViewPhase.Paused;s.pauseReasons=ViewPauseReasons.User;Bind(s);Shot("pause");
                view.ShowSettings(new HotpotSort.Contracts.PlayerSettings(),_=>{});Shot("settings");Close("完成");
                view.ShowNotice("好友榜","暂无好友记录");Shot("friends-empty-shell");Close("知道了");view.ShowNotice("好友榜","加载失败，请稍后重试");Shot("friends-failed-shell");Close("知道了");
                s=Capture.Fixture();s.revivalPending=true;s.revivalOfferId="fixture";s.pauseReasons=ViewPauseReasons.Revival;Bind(s);Shot("revival");
                view.ShowNotice("暂时无法观看","奖励暂不可用，请稍后重试");Shot("reward-unavailable");Close("知道了");
                s=Capture.Fixture();s.phase=ViewPhase.Won;s.facts=new[]{new ViewFact{label="完成订单",value="61 / 61"},new ViewFact{label="用时",value="07:42"}};Bind(s);Shot("win",true);
                s=Capture.Fixture();s.phase=ViewPhase.Overflow;s.message="Timeout";Bind(s);view.SetRemainingTime(0);Shot("timeout");
                view.ShowError("食材加载失败，请重试");Shot("error");
                Check(view.GetComponentsInChildren<RawImage>(true).Where(i=>i.name.StartsWith("ThemeEdgeGutter_")).All(i=>!i.raycastTarget),"edges cannot intercept input");
                Check(TaskAssetValidation.Validate(V7Art.Root,true).Length==0,"v7 fallback remains complete");
                LayoutChecks();Motion();Share();
                File.WriteAllText(root+"/scope.txt","Actual Unity PlayMode offscreen presentation fixtures; no live Bootstrap/core/device/performance claim. Friends captures are shell fixtures, not WeChat open-data rendering.");Debug.Log("TASK012_VISUAL_CAPTURE_OK");
            }catch(Exception e){exit=1;Debug.LogException(e);File.WriteAllText(root+"/error.txt",e.ToString());}
            finally{File.WriteAllLines(root+"/checks.txt",checks);SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
        }
    }
}
#endif
