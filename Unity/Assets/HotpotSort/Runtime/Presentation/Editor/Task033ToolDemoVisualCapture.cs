#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Capture=HotpotSort.Task001V7.VisualCapture;
namespace HotpotSort.Presentation
{
    [InitializeOnLoad] public static class Task033ToolDemoVisualCapture
    {
        const string Key="Task033.VisualCapture";
        static int checks;
        static Task033ToolDemoVisualCapture(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run(){SessionState.SetBool(Key,true);UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);EditorApplication.EnterPlaymode();}
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Debug.Log("TASK033 VISUAL PASS "+message);}
        static object Call(GameplayView view,string name,params object[] args)=>typeof(GameplayView).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,args);
        sealed class Port:IPresentationPort,ISwapOrderActions
        {
            public ViewSnapshot state;public bool selecting;public int[] legal={0};public int completed,cancelled;
            public SwapOrderTransfer transfer;
            public event Action<ViewUpdate> Updated;
            public ViewSnapshot Read()=>state;
            public void SessionAction(ViewAction a){}public void Tap(ViewTap tap){}public void ObserveSupply(ViewSupplyObservation o){}
            public SwapOrderOffer ReadSwapOrderOffer()=>new SwapOrderOffer{sessionId=state.sessionId,selecting=selecting,legalSlots=legal,transfer=transfer};
            public bool BeginSwapOrderSelection(){selecting=legal.Length>0;return selecting;}
            public void CancelSwapOrderSelection(){selecting=false;cancelled++;}
            public Task<RewardApplicationResult> SelectSwapOrderAsync(int slot,RewardRoute route)
            {
                if(!legal.Contains(slot))return Task.FromResult(RewardApplicationResult.Unavailable);
                selecting=false;transfer=new SwapOrderTransfer{sessionId=state.sessionId,token="swap-test",slot=slot,items=new[]{new SwapOrderItem{itemId="return-0",ingredientId="food_00",sourceIndex=0,bufferIndex=0},new SwapOrderItem{itemId="return-1",ingredientId="food_00",sourceIndex=1,bufferIndex=1}}};
                return Task.FromResult(RewardApplicationResult.Applied);
            }
            public bool CompleteSwapOrderTransfer(string session,string token){completed++;transfer=null;state.orders[0].foodId=4;state.orders[0].count=0;state.buffer[0]=new ViewItem{itemId="return-0",foodId=0};state.buffer[1]=new ViewItem{itemId="return-1",foodId=0};Updated?.Invoke(new ViewUpdate{snapshot=state});return true;}
        }
        static void Execute()
        {
            int exit=0;
            try
            {
                var args=Environment.GetCommandLineArgs();string path=args[Array.IndexOf(args,"-visualEvidence")+1];Directory.CreateDirectory(path);
                var view=new GameObject("Task033VisualFixture").AddComponent<GameplayView>();view.ConfigureAssets(PresentationAssets.CandidateRoot);
                var state=Capture.Fixture();state.sessionId="task033-visual";state.orders[0].count=2;state.tutorialStep=ViewTutorialStep.None;state.buffer=new ViewItem[5];
                var port=new Port{state=state};view.Bind(port);view.SetViewport(new Rect(0,0,1080,1920),1920);view.SetForeground(true);view.World.SetSimulating(false);Canvas.ForceUpdateCanvases();
                for(int i=0;i<20;i++)Call(view,"TickClickabilityCache");
                var before=view.CaptureClickability();Call(view,"BeginSwapPresentation");
                Check(port.selecting,"selection begins");Check(view.GetComponentsInChildren<Button>().Any(b=>b.name=="SwapTarget_0"),"legal target accepts selection");
                Check(!view.BeginScreenPress(Vector2.zero),"food input blocked during selection");
                var after=view.CaptureClickability();Check(before.clickable.All(id=>after.clickable.Contains(id)),"selection dim does not remove clickability observations");
                Check(state.pauseReasons==ViewPauseReasons.None,"selection does not request a pause");
                Capture.Capture(view,1080,1920,path+"/swap-selection.png");
                Check(view.HandleSwapBack()&&port.cancelled==1&&!port.selecting,"system back cancels without transfer");
                Call(view,"BeginSwapPresentation");view.GetComponentsInChildren<Button>().Single(b=>b.name=="SwapOrderSelection").onClick.Invoke();Check(port.cancelled==2&&!port.selecting,"non-order background click cancels");
                port.legal=Array.Empty<int>();Call(view,"BeginSwapPresentation");Check(!port.selecting&&view.GetComponentsInChildren<Text>().Any(t=>t.text=="当前没有可换的订单"),"no target gives text without entering selection");
                port.legal=new[]{0};Call(view,"BeginSwapPresentation");Call(view,"ChooseSwapTarget",0);
                Capture.Capture(view,1080,1920,path+"/swap-reward-demo.png");
                var demo=view.GetComponentInChildren<ToolDemoLoop>();for(int i=0;i<23;i++)demo.Advance(.1f);Capture.Capture(view,1080,1920,path+"/swap-demo-return.png");
                for(int i=0;i<10;i++)demo.Advance(.1f);Capture.Capture(view,1080,1920,path+"/swap-demo-changed.png");
                Call(view,"CloseModal");port.legal=Array.Empty<int>();Call(view,"RequestSwap",RewardRoute.SimulatedAd);Check(port.selecting&&port.completed==0&&port.transfer==null,"stale selection refreshes without consuming or transferring");
                port.legal=new[]{0};Call(view,"RefreshSwapOffer");Call(view,"RequestSwap",RewardRoute.SimulatedAd);
                Call(view,"TickSwapPresentation",.17f);Check(port.completed==0&&view.GetComponentsInChildren<RectTransform>().Count(n=>n.name.StartsWith("SwapReturn_"))==2,"two foods in return flight before commit");Capture.Capture(view,1080,1920,path+"/swap-return.png");
                Call(view,"TickSwapPresentation",.18f);Check(port.completed==1&&state.orders[0].foodId==4,"transfer completes once after 0.34 seconds");Call(view,"TickSwapPresentation",.5f);Check(port.completed==1,"return completion not repeated");Capture.Capture(view,1080,1920,path+"/swap-complete.png");
                // Three identical overlapping cutouts must expose exactly one next layer.
                state.plates=new[]{new ViewPlate{plateId="stack",x=210,y=510,radius=60,motion=new ViewPlateMotion{animateEntry=false},items=Enumerable.Range(0,3).Select(i=>new ViewItem{itemId="stack-"+i,foodId=0,radius=24,x=0,y=0,drawOrder=i}).ToArray()}};
                view.Bind(port);view.SetViewport(new Rect(0,0,Screen.width,Screen.height),Screen.height);Canvas.ForceUpdateCanvases();for(int i=0;i<25;i++)Call(view,"TickClickabilityCache");
                Debug.Log("TASK033 STACK current="+string.Join(",",view.CaptureClickability().clickable)+" next="+string.Join(",",view.CaptureClickability().nextLayer)+" world="+view.World.Hit(new Vector2(210,510))+" position="+view.World.ItemPosition("stack-2")+" clickable="+view.IsItemClickable("stack-2"));
                var observation=view.CaptureClickability();Check(observation.clickable.Contains("stack-2")&&!observation.clickable.Contains("stack-1"),"real alpha identifies current top cutout");
                Check(observation.nextLayer.Contains("stack-1")&&!observation.nextLayer.Contains("stack-0"),"one layer forecast does not recurse");
                Check(!observation.clickable.Intersect(observation.nextLayer).Any()&&!observation.unknown.Intersect(observation.nextLayer).Any(),"observation sets are disjoint");
                File.WriteAllText(path+"/visual-result.txt","PASS checks="+checks+"; Unity PlayMode presentation fixture; no phone or real rewards verification.");Debug.Log("TASK033_VISUAL_CAPTURE_PASS checks="+checks);
            }
            catch(Exception e){exit=1;Debug.LogException(e);}
            finally{SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
        }
    }
}
#endif
