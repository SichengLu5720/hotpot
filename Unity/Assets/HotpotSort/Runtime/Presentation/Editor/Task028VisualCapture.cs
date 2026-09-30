#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Capture=HotpotSort.Task001V7.VisualCapture;
namespace HotpotSort.Presentation
{
    [InitializeOnLoad] public static class Task028VisualCapture
    {
        const string Key="Task028.VisualCapture";
        static Task028VisualCapture(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run(){SessionState.SetBool(Key,true);UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);EditorApplication.EnterPlaymode();}
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("TASK028 VISUAL PASS "+message);}
        static void Tick(GameplayView view,float dt=.016f)
        {
            view.SetViewport(new Rect(0,0,Screen.width,Screen.height),Screen.height);Canvas.ForceUpdateCanvases();
            typeof(GameplayView).GetMethod("TickWarmupPresentation",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,new object[]{dt});
        }
        static bool TextExists(GameplayView view,string text)=>view.GetComponentsInChildren<Text>().Any(t=>t.text.Replace("\n","")==text);
        sealed class Port:IPresentationPort,IWarmupPresentationPort
        {
            public ViewSnapshot state;public int opening,warning,completed;public bool cleanAtCompletion;public GameplayView view;
            public event Action<ViewUpdate> Updated;
            public ViewSnapshot Read()=>state;
            public void Publish(){state.revision++;Updated?.Invoke(new ViewUpdate{snapshot=state});}
            public void SessionAction(ViewAction a){}public void Tap(ViewTap tap){}public void ObserveSupply(ViewSupplyObservation o){}
            public bool SelectTutorialFood(string s,long g,string id){state.tutorialItemId=id;state.tutorialOrderSlot=0;Publish();return true;}
            public bool TutorialFoodArrived(string s,long g,string id){state.tutorialStep=ViewTutorialStep.OrderExplanation;state.pauseReasons=ViewPauseReasons.Tutorial;Publish();return true;}
            public bool CompleteOpeningTutorial(string s,long g){opening++;state.tutorialStep=ViewTutorialStep.None;state.pauseReasons=ViewPauseReasons.None;Publish();return true;}
            public bool CompleteBufferWarning(string s,long g){warning++;state.bufferWarning=false;state.pauseReasons=ViewPauseReasons.None;Publish();return true;}
            public bool CompleteWarmup(string s,long g){completed++;cleanAtCompletion=!TextExists(view,"最后一关！")&&!view.World.Bodies.Any()&&!view.GetComponentsInChildren<RectTransform>().Any(n=>n.name=="GameplayBoard");return true;}
        }
        static void Execute()
        {
            int exit=0;
            try
            {
                var args=Environment.GetCommandLineArgs();string path=args[Array.IndexOf(args,"-visualEvidence")+1];Directory.CreateDirectory(path);
                var view=new GameObject("Task028VisualFixture").AddComponent<GameplayView>();view.ConfigureAssets(PresentationAssets.CandidateRoot);
                var state=Capture.Fixture();state.sessionId="task028-visual";state.isWarmup=true;state.thirdPotThreshold=37;state.fourthPotThreshold=55;
                state.plates=state.plates.Take(7).ToArray();int foodIndex=0;int[] types={0,4,15};
                foreach(var item in state.plates.SelectMany(p=>p.items))item.foodId=types[foodIndex++%3];
                state.orders[0].count=state.orders[1].count=0;
                state.tutorialStep=ViewTutorialStep.SelectFood;state.tutorialItemId=state.plates[0].items[0].itemId;state.tutorialOrderSlot=0;
                var port=new Port{state=state,view=view};view.Bind(port);view.SetViewport(new Rect(0,0,1080,1920),1920);view.SetForeground(true);view.World.SetSimulating(false);Canvas.ForceUpdateCanvases();
                Tick(view);Check(TextExists(view,"点击食材，放入火锅。"),"first step copy");
                Check(!TextExists(view,"10:00"),"warmup timer hidden");
                Check(view.GetComponentsInChildren<RectTransform>().Any(n=>n.name=="TutorialHand"),"native pointer visible");
                Check(view.GetComponentsInChildren<RectTransform>().Single(n=>n.name=="PlateFood_"+state.tutorialItemId).localScale.x>1,"target enlarged");
                Check(view.GetComponentsInChildren<RectTransform>().Any(n=>n.name=="TutorialBrightFood"),"target has normal-brightness overlay copy");
                Check(view.GetComponentsInChildren<Image>().Count(n=>n.name=="TutorialShade")==1,"first step dims entire background");
                Check(!view.GetComponentsInChildren<RectTransform>().Any(n=>n.name=="TutorialCard"||n.name=="Action_知道了"),"no white card or confirmation button");
                Capture.Capture(view,1080,1920,path+"/first-food.png");
                state.tutorialStep=ViewTutorialStep.FoodInFlight;port.Publish();Tick(view);Check(!TextExists(view,"集满 3 个相同食材，即可完成订单。"),"order explanation waits for arrival");
                state.orders[0].count=1;view.NotifyTutorialFoodArrived(state.tutorialItemId);Tick(view);
                Check(TextExists(view,"集满 3 个相同食材，即可完成订单。"),"real arrival callback advances explanation");
                Capture.Capture(view,1080,1920,path+"/order-explanation.png");
                Check(view.GetComponentsInChildren<Image>().Count(n=>n.name=="TutorialShade")==4,"order card spotlight has four outer shades");
                Check(view.SubmitScreenTap(new Vector2(2,2)),"screen corner tap confirms order explanation");Tick(view);Check(port.opening==1,"opening confirmation uses scoped port");
                view.HighlightItem(state.plates[0].items[0].itemId);Tick(view);Check(!TextExists(view,"点击食材，放入火锅。"),"hint has no teaching text");Check(!view.GetComponentsInChildren<Image>().Any(n=>n.name=="TutorialShade"),"hint does not dim screen");Capture.Capture(view,1080,1920,path+"/hint-pointer.png");
                state.buffer=Enumerable.Range(0,5).Select(i=>i<4?new ViewItem{itemId="buffer-"+i,foodId=4}:null).ToArray();
                state.bufferWarning=true;state.pauseReasons=ViewPauseReasons.Tutorial;port.Publish();Tick(view);
                Check(TextExists(view,"暂存区放满会导致挑战失败。"),"buffer warning copy");Capture.Capture(view,1080,1920,path+"/buffer-warning.png");
                var shades=view.GetComponentsInChildren<RectTransform>().Where(n=>n.name=="TutorialShade").ToArray();
                Check(shades.Length==4&&Enumerable.Range(0,5).All(i=>shades.All(n=>!new Rect(n.anchoredPosition.x,-n.anchoredPosition.y,n.rect.width,n.rect.height).Contains(new Vector2(75+i*67,259)))),"all five buffer slots remain outside shade");
                view.GetComponentsInChildren<Button>().Single(b=>b.name=="TutorialAnyTap").onClick.Invoke();Tick(view);Check(port.warning==1,"full-screen UI tap confirms warning separately");
                state.plates=new ViewPlate[0];state.buffer=new ViewItem[5];state.warmupComplete=true;state.completedOrders=6;state.pauseReasons=ViewPauseReasons.None;
                foreach(var order in state.orders){order.foodId=-1;order.count=0;}port.Publish();Tick(view);
                Check(TextExists(view,"最后一关！")&&port.completed==0,"transition message precedes formal callback");Capture.Capture(view,1080,1920,path+"/last-stage.png");
                view.SetForeground(false);for(int i=0;i<20;i++)Tick(view,.1f);Check(port.completed==0,"transition freezes in background");view.SetForeground(true);
                for(int i=0;i<7;i++)Tick(view,.1f);Check(port.completed==0,"no formal supply before 0.8 seconds");
                for(int i=0;i<3;i++)Tick(view,.1f);Check(port.completed==1&&port.cleanAtCompletion,"text and old board removed before exactly one callback");
                view.ResetView();Tick(view);Check(!view.GetComponentsInChildren<RectTransform>().Any(n=>n.name=="TutorialHand"),"reset clears pointer");
                File.WriteAllText(path+"/visual-result.txt","PASS: actual Unity PlayMode presentation fixture. Food pointer, arrival-gated order explanation, separate warning confirmation, hint without text, hidden warmup timer, background-frozen 0.8s message and clean exactly-once callback. Not device or full core-flow verification.");
                Debug.Log("TASK028_VISUAL_CAPTURE_PASS");
            }
            catch(Exception e){exit=1;Debug.LogException(e);}
            finally{SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
        }
    }
}
#endif
