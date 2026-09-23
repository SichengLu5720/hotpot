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
using PlayerSettings=HotpotSort.Contracts.PlayerSettings;

namespace HotpotSort.Task001V7
{
    public static partial class VisualCapture
    {
        static readonly Vector2Int[] sizes={new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1440,3200)};
        static readonly List<string> checks=new List<string>();
        static string captureRoot;static int fixtureId;static Func<int,PlateFoodShape> finalShapes;
        public static void RunFull(){SessionState.SetBool("Hotpot.VisualV7.Full",true);Run();}
        static void Check(bool result,string message){checks.Add((result?"PASS ":"FAIL ")+message);if(!result)throw new Exception(message);}
        static void AllSizes(GameplayView view,string name)
        {
            foreach(var size in sizes)Capture(view,size.x,size.y,captureRoot+"/"+name+"-"+size.x+"x"+size.y+".png");
        }
        static void Click(GameplayView view,string label)
        {
            var button=view.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.gameObject.activeInHierarchy&&b.name=="Action_"+label);
            if(!button)throw new Exception("Missing button "+label);button.onClick.Invoke();
        }
        static FullPort BindState(GameplayView view,ViewSnapshot state,bool share=true)
        {
            foreach(var p in state.plates)if(p.items.Length>0&&string.IsNullOrEmpty(p.layoutVersion)){for(int i=0;i<p.items.Length;i++)p.items[i].sourceIndex=i;var layout=PlateItemLayout.Create(12345,p.plateId,p.radius,p.items,finalShapes);p.items=layout.Items;p.layoutVersion=PlateItemLayout.Version;p.initialItemCount=p.items.Length;p.envelopeRatio=layout.EnvelopeRatio;}
            state.sessionId="v8-fixture-"+(++fixtureId);state.sessionGeneration=fixtureId;
            var port=new FullPort(state,share);view.Bind(port);view.SetForeground(false);return port;
        }
        static ViewSnapshot Phase(ViewPhase phase,string message=null)
        {
            var state=Fixture();state.phase=phase;state.message=message;
            state.pauseReasons=phase==ViewPhase.Paused?ViewPauseReasons.User:ViewPauseReasons.None;return state;
        }
        static async Task FullCapture(string root,HotpotSort.Bootstrap.Bootstrap boot,DailyProductionComposition composition,GameplayView view)
        {
            finalShapes=id=>(PlateFoodShape)typeof(DailyProductionComposition).GetMethod("LoadPlateShape",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(composition,new object[]{id});
            var isolated=new LocalDevelopmentServices("HotpotSort.Task001.v8.FullVisual.QA");composition.ConfigureServices(isolated,isolated,isolated,isolated);
            captureRoot=root;checks.Clear();Directory.CreateDirectory(root+"/motion");
            Check(view.AssetRoot==V7Art.Root,"Boot consumes the complete v7 root");
            Check(TaskAssetValidation.Validate(V7Art.Root).Length==0,"All theme addresses and 16 readable food textures load");
            var font=Resources.Load<Font>(V7Art.Root+"/fonts/readable");
            foreach(char c in "火锅消消每日挑战暂存已满复活重新开始分享激励视频设置音乐音效时间到好友榜")Check(font.HasCharacter(c),"Body glyph "+c);
            AllSizes(view,"entry");
            view.ShowSettings(new PlayerSettings(),_=>{});AllSizes(view,"settings-entry");Click(view,"完成");
            view.ShowNotice("好友榜 · 开发模拟","");AllSizes(view,"friends-empty");Click(view,"知道了");
            await boot.Controller.StartTodayAsync();await Task.Delay(2200);
            view.SetViewport(new Rect(0,0,Screen.width,Screen.height),Screen.height);Canvas.ForceUpdateCanvases();
            string hint=view.FindClickableHint();bool tapped=false;
            if(hint!=null)
            {
                var board=(RectTransform)typeof(GameplayView).GetField("board",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(view);
                var at=view.World.ItemPosition(hint);var point=RectTransformUtility.WorldToScreenPoint(null,board.TransformPoint(new Vector3(at.x,-at.y,0)));
                tapped=view.SubmitScreenTap(point);
            }
            await Task.Delay(450);AllSizes(view,"live-core-gameplay");
            File.WriteAllText(root+"/live-core-snapshot.json",JsonUtility.ToJson(view.LastSnapshot,true));
            Check(tapped,"Actual Boot start and input adapter accepted a visible food tap");
            boot.Controller.Request(SessionAction.Pause);AllSizes(view,"live-core-pause");
            boot.Controller.Request(SessionAction.Resume);await Task.Delay(120);
            Check(view.LastSnapshot.phase==ViewPhase.Running,"Actual controller pause/resume returned to Running");
            boot.Controller.Request(SessionAction.Pause);
            var state=Fixture();BindState(view,state);view.SetRemainingTime(462);AllSizes(view,"gameplay-same-state");
            File.WriteAllText(root+"/same-state.json",JsonUtility.ToJson(state,true));
            state=Fixture();foreach(var order in state.orders)order.enabled=true;BindState(view,state);AllSizes(view,"four-pots");
            state=Crowded();BindState(view,state);AllSizes(view,"crowded");
            state=Fixture();state.plates[0].y=285;BindState(view,state);AllSizes(view,"partial-occlusion");
            Check(view.PlateCrop.yMin==292&&view.PlateCrop.yMax==828,"Visual/input crop remains 292..828");
            state=Phase(ViewPhase.Paused);BindState(view,state);AllSizes(view,"pause");
            view.ShowSettings(new PlayerSettings(),_=>{});AllSizes(view,"settings");Click(view,"完成");
            state=Phase(ViewPhase.Won);state.facts=new[]{new ViewFact{label="完成订单",value="61 / 61"},new ViewFact{label="用时",value="07:42"}};BindState(view,state);AllSizes(view,"win");
            Check(!view.GetComponentsInChildren<Button>().Any(b=>b.name.Contains("分享")),"Ordinary victory has no share entry");
            state=Phase(ViewPhase.Overflow,"BufferOverflow");BindState(view,state);AllSizes(view,"buffer-final");
            Check(!view.GetComponentsInChildren<Button>().Any(b=>b.name.Contains("分享")),"Ordinary failure has no share entry");
            state=Phase(ViewPhase.Overflow,"Timeout");BindState(view,state);view.SetRemainingTime(0);AllSizes(view,"timeout");
            foreach(bool share in new[]{true,false})
            {
                state=Fixture();state.revivalPending=true;state.revivalOfferId="offer";state.pauseReasons=ViewPauseReasons.Revival;
                for(int i=0;i<5;i++)state.buffer[i]=new ViewItem{itemId="buffer-"+i,foodId=i*3,radius=16};
                BindState(view,state,share);view.SetRemainingTime(462);AllSizes(view,share?"revival-share-offer":"revival-ad-offer");
                int routes=view.GetComponentsInChildren<Button>().Count(b=>b.name=="Action_分享复活"||b.name=="Action_看广告复活");
                Check(routes==1,"Exactly one revival reward route "+share);
                var request=new RewardRequest(state.sessionGeneration,RewardKind.Revival,share?RewardRoute.SimulatedShare:RewardRoute.SimulatedAd,"fixture",state.sessionId,"offer");
                var result=view.ShowRewardSimulationAsync(request);AllSizes(view,share?"reward-share-simulation":"reward-ad-simulation");Click(view,"取消");Check(await result==RewardOutcome.Cancelled,"Development simulation cancellation delivered");
            }
            CaptureMotion(view);
            CaptureRevival(view);
            // The final standalone three-resolution 120-second probe is authoritative; old 15s proxy is not run.
            File.WriteAllLines(root+"/checks.txt",checks);
            File.WriteAllText(root+"/capture-info.json","{\"scope\":\"Actual Unity PlayMode rendering. live-core-* uses real Boot/session/input; other states are deterministic presentation fixtures. Motion uses the same presentation tick with controlled delta. Not final scripted QA or target-device approval.\"}");
            PlayerPrefs.DeleteKey("HotpotSort.Task001.v8.FullVisual.QA");PlayerPrefs.Save();SessionState.SetBool("Hotpot.VisualV7.Full",false);
        }
        static ViewSnapshot Crowded()
        {
            var state=Fixture();foreach(var order in state.orders)order.enabled=true;
            var plates=new List<ViewPlate>();int id=100;
            for(int row=0;row<5;row++)for(int col=0;col<4;col++)
            {
                var p=new ViewPlate{plateId="crowd-"+row+"-"+col,x=55+col*103,y=342+row*99,radius=45,motion=new ViewPlateMotion{animateEntry=false}};
                p.items=new[]{new ViewItem{itemId=(id++).ToString(),foodId=(row*4+col)%16,x=-18,radius=16},new ViewItem{itemId=(id++).ToString(),foodId=(row*4+col+5)%16,x=18,radius=16}};plates.Add(p);
            }
            state.plates=plates.ToArray();return state;
        }
        static void CaptureMotion(GameplayView view)
        {
            var state=Fixture();var port=BindState(view,state);var feedback=view.GetComponent<GameplayFeedback>();feedback.enabled=false;view.SetForeground(true);view.World.SetSimulating(false);
            var item=state.plates[0].items[0];
            state=JsonUtility.FromJson<ViewSnapshot>(JsonUtility.ToJson(state));state.plates=state.plates.Skip(1).ToArray();port.state=state;
            port.Emit(new[]{Event(state,1,"ItemRoutedToOrder",item.itemId,"food_00",0)});
            for(int i=0;i<=8;i++){if(i>0){feedback.Tick(.05f);}Capture(view,1080,1920,captureRoot+"/motion/entry-"+i+".png");}
            Check(feedback.ActiveEffectCount==0,"Arrival food and ripple gone after 0.40s; broth is never modified");
            state.orders[0].foodId=1;state.orders[0].count=0;state.orders[2].enabled=true;
            port.Emit(new[]{Event(state,2,"OrderCompleted",null,"food_00",0),Event(state,3,"PotUnlocked",null,null,2)});
            for(int i=0;i<=6;i++){if(i>0)feedback.Tick(.1f);Capture(view,1080,1920,captureRoot+"/motion/serve-unlock-"+i+".png");}
            feedback.enabled=true;view.SetForeground(false);
        }
        static ViewEvent Event(ViewSnapshot s,long seq,string kind,string id,string food,int slot)=>new ViewEvent{sessionId=s.sessionId,sessionGeneration=s.sessionGeneration,sequence=seq,transactionId="visual-"+seq,kind=kind,itemId=id,ingredientId=food,slot=slot,targetSlot=slot,targetContainer="Order",sourceContainer="Plate"};
        static void CaptureRevival(GameplayView view)
        {
            var state=Fixture();state.revivalPending=state.revivalUsed=true;state.pauseReasons=ViewPauseReasons.Revival;state.revivalOfferId="visual-offer";
            var port=BindState(view,state);
            var batch=new RevivalTransferBatch{token=new RevivalCompletionToken{sessionId=state.sessionId,sessionGeneration=state.sessionGeneration,transactionId="revive",revivalOfferId="visual-offer",requestId="visual-request",newPlateId="queue-only",completionToken="visual-completion",eventSeq=1},items=Enumerable.Range(0,5).Select(i=>new RevivalTransferItem{itemId="buffer-"+i,ingredientId="food_"+(i*3).ToString("00"),sourceSlot=i,targetIndex=i}).ToArray()};
            state.revivalTransfer=batch;
            port.Emit(new[]{new ViewEvent{sessionId=state.sessionId,sessionGeneration=state.sessionGeneration,sequence=1,transactionId="revive",kind="RevivalTransferStarted",revivalTransfer=batch}});
            var feedback=view.GetComponent<GameplayFeedback>();feedback.enabled=false;view.SetForeground(true);view.World.SetSimulating(false);
            var original=state.plates.SelectMany(p=>p.items).Select(i=>i.itemId).ToArray();
            float now=0;float[] stamps={0,.12f,.33f,.50f,.74f,.81f};
            foreach(float stamp in stamps)
            {
                while(now<stamp-.0001f){float dt=Mathf.Min(.01f,stamp-now);feedback.Tick(dt);now+=dt;}
                Capture(view,1080,1920,captureRoot+"/motion/revival-"+Mathf.RoundToInt(stamp*1000)+".png");
                Check(state.plates.SelectMany(p=>p.items).Select(i=>i.itemId).SequenceEqual(original),"Revival retains overflow/board inventory at "+stamp);
            }
            Check(port.completions==1,"Revival completion callback exactly once");
            Check(!view.GetComponentsInChildren<RectTransform>().Any(n=>n.gameObject.activeInHierarchy&&n.name=="RevivalTransport_VisualOnly"),"Revival transport removed; no current-board insertion");
            port.Emit(new[]{new ViewEvent{sessionId=state.sessionId,sessionGeneration=state.sessionGeneration,sequence=1,transactionId="revive",kind="RevivalTransferStarted",revivalTransfer=batch}});
            feedback.Tick(.1f);Check(port.completions==1&&!feedback.RevivalActive,"Duplicate revival event does not replay");
            // New pending batch checks nested user pause and generation cancellation.
            state.revivalPending=state.revivalUsed=true;state.pauseReasons=ViewPauseReasons.Revival;state.revivalTransfer=batch;batch.token.completionToken="cancel-me";batch.token.eventSeq=2;
            port.Emit(new[]{new ViewEvent{sessionId=state.sessionId,sessionGeneration=state.sessionGeneration,sequence=2,kind="RevivalTransferStarted",revivalTransfer=batch}});
            feedback.Tick(.1f);float elapsed=feedback.RevivalElapsed;state.pauseReasons|=ViewPauseReasons.User;port.Emit(Array.Empty<ViewEvent>());feedback.Tick(.1f);
            Check(Mathf.Abs(feedback.RevivalElapsed-elapsed)<.0001f,"User pause freezes revival clock");
            Check(view.GetComponentsInChildren<Button>().Any(b=>b.name=="Action_继续下锅"),"Revival user pause exposes the resume action");
            Capture(view,1080,1920,captureRoot+"/motion/revival-user-paused.png");
            BindState(view,Fixture());feedback.Tick(.1f);Check(!feedback.RevivalActive,"Session replacement cancels stale revival transport");
            feedback.enabled=true;view.SetForeground(false);
        }
        static async Task CaptureLoadProxy(GameplayView view)
        {
            var state=Crowded();var port=BindState(view,state);view.SetForeground(true);view.World.SetSimulating(false);
            var feedback=view.GetComponent<GameplayFeedback>();long sequence=1;var watch=System.Diagnostics.Stopwatch.StartNew();int frames=0;var samples=new List<double>();
            for(int i=0;i<80;i++)
            {
                var item=state.plates[i%state.plates.Length].items[0];
                port.Emit(new[]{Event(state,sequence++,"ItemRoutedToOrder",item.itemId,"food_"+item.foodId.ToString("00"),i%4)});
                var timer=System.Diagnostics.Stopwatch.StartNew();Capture(view,720,1280,captureRoot+"/load-last.png");timer.Stop();samples.Add(timer.Elapsed.TotalMilliseconds);frames++;await Task.Delay(1);
            }
            samples.Sort();
            File.WriteAllText(captureRoot+"/render-load-proxy.json","{\"scope\":\"Editor offscreen render plus PNG readback/encoding; not standalone FPS certification\",\"frames\":"+frames+",\"medianMs\":"+samples[samples.Count/2].ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"p95Ms\":"+samples[(int)(samples.Count*.95)].ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"poolCount\":"+feedback.PoolSize+"}");
            Check(feedback.PoolSize<=64,"Effects remain within bounded reusable pool under four-pot/crowded repeated flights");
            view.SetForeground(false);
        }
        public sealed class FullPort:IPresentationPort,IRevivalPresentationPort
        {
            public ViewSnapshot state;public int completions;bool share;
            public FullPort(ViewSnapshot s,bool share){state=s;this.share=share;}
            public event Action<ViewUpdate> Updated;public ViewSnapshot Read()=>state;
            public void Emit(ViewEvent[] events){state.revision++;state.eventSeq=events.Length==0?state.eventSeq:events.Max(e=>e.sequence);Updated?.Invoke(new ViewUpdate{snapshot=state,events=events});}
            public void SessionAction(ViewAction action){}public void Tap(ViewTap tap){}public void ObserveSupply(ViewSupplyObservation observation){}
            public RevivalOfferView ReadRevivalOffer()=>new RevivalOfferView{available=state.revivalPending&&!state.revivalUsed,sessionId=state.sessionId,generation=state.sessionGeneration,offerId=state.revivalOfferId,route=share?RewardRoute.SimulatedShare:RewardRoute.SimulatedAd,shareAvailability=new ShareAvailability("fixture",share?0:3,null,false,DateTimeOffset.UtcNow)};
            public Task<RewardApplicationResult> RequestRevivalAsync()=>Task.FromResult(RewardApplicationResult.Unavailable);
            public void DeclineRevival(){}
            public bool CompleteRevivalTransfer(RevivalCompletionToken token){if(!state.revivalPending||state.revivalTransfer==null||!state.revivalTransfer.token.Matches(token))return false;completions++;state.revivalPending=false;state.revivalTransfer=null;state.pauseReasons=ViewPauseReasons.None;Emit(Array.Empty<ViewEvent>());return true;}
        }
    }
}
#endif
