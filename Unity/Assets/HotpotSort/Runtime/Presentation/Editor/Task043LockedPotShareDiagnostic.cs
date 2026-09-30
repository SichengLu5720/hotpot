#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Capture=HotpotSort.Task001V7.VisualCapture;

namespace HotpotSort.Presentation
{
    [InitializeOnLoad] public static class Task043LockedPotShareDiagnostic
    {
        const string Key="Task043.LockedPotShareDiagnostic";

        static Task043LockedPotShareDiagnostic()
        {EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}

        public static void Run()
        {
            SessionState.SetBool(Key,true);
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        static void Check(bool condition,string message)
        {if(!condition)throw new InvalidOperationException(message);Debug.Log("TASK043 PASS "+message);}

        static Button Button(GameplayView view,string name)
            =>view.GetComponentsInChildren<Button>().Single(button=>button.name=="Action_"+name);

        static void Execute()
        {
            int exit=0;
            string root=Path.GetFullPath("../.harness/qa/TASK-043");
            try
            {
                var args=Environment.GetCommandLineArgs();int evidence=Array.IndexOf(args,"-visualEvidence");
                if(evidence>=0&&evidence+1<args.Length)root=args[evidence+1];
                Directory.CreateDirectory(root);

                var view=new GameObject("Task043LockedPotFixture").AddComponent<GameplayView>();
                view.ConfigureAssets(PresentationAssets.CandidateRoot);
                var state=Capture.Fixture();state.thirdPotThreshold=37;state.fourthPotThreshold=55;
                var port=new Port(state);view.Bind(port);view.SetForeground(true);view.World.SetSimulating(false);
                RewardKind? requested=null;RewardRoute? route=null;int requests=0;
                view.RewardRequested+=(kind,rewardRoute)=>{requested=kind;route=rewardRoute;requests++;};

                Open(view,2);
                Check(Button(view,"分享解锁").interactable,"share action available");
                Check(Button(view,"取消").interactable,"explicit cancel action available");
                Check(view.GetComponentsInChildren<Button>().Count(button=>button.transform.IsChildOf(view.GetComponentsInChildren<RectTransform>().Single(node=>node.name=="LockedPotUnlock")))==2,"one reward action and one cancel action");
                Check(!view.GetComponentsInChildren<Text>().Any(text=>text.text.Contains("今日分享")),"share quota description removed");
                Check(!view.GetComponentsInChildren<Button>().Any(button=>button.name=="Action_关闭"),"top-right close icon removed");
                Check(view.GetComponentsInChildren<Text>().Any(text=>text.text=="再完成 37 锅订单即可解锁"),"third-pot cumulative threshold preserved");
                Capture.Capture(view,1080,1920,Path.Combine(root,"share-available.png"));
                Button(view,"分享解锁").onClick.Invoke();
                Check(requests==1&&requested==RewardKind.ThirdPot&&route==RewardRoute.SimulatedShare,"third-pot share route emitted once");

                port.Used=ShareAvailability.DailyLimit;
                Open(view,3);
                Check(Button(view,"看广告解锁").interactable,"video remains available after share exhaustion");
                Check(!view.GetComponentsInChildren<Button>().Any(button=>button.name=="Action_分享解锁"),"share action replaced instead of duplicated");
                Check(!view.GetComponentsInChildren<Text>().Any(text=>text.text.Contains("今日分享")),"exhausted quota description remains hidden");
                Check(view.GetComponentsInChildren<Text>().Any(text=>text.text=="再完成 55 锅订单即可解锁"),"fourth-pot cumulative threshold preserved");
                Capture.Capture(view,1080,1920,Path.Combine(root,"share-exhausted-ad-available.png"));
                Button(view,"看广告解锁").onClick.Invoke();
                Check(requests==2&&requested==RewardKind.FourthPot&&route==RewardRoute.SimulatedAd,"fourth-pot ad fallback emitted once");

                Open(view,2);Button(view,"取消").onClick.Invoke();
                Check(requests==2&&!view.GetComponentsInChildren<RectTransform>().Any(node=>node.name=="LockedPotUnlock"),"cancel closes without unlock request");
                Open(view,2);state.orders.Single(order=>order.slot==2).enabled=true;port.Emit();
                Check(!view.GetComponentsInChildren<RectTransform>().Any(node=>node.name=="LockedPotUnlock"),"unlock update closes stale dialog");
                Check(view.GetComponentsInChildren<RectTransform>().Count(node=>node.name=="UnlockProgress")==1,"unlocked pot removes its frozen progress only");

                File.WriteAllText(Path.Combine(root,"result.txt"),"PASS: locked pots expose one share-first reward action plus card cancel; quota text and top-right close are absent; exhausted share falls back to ad; routing and stale-dialog cleanup verified in Unity PlayMode.\n");
                view.Bind(null);
            }
            catch(Exception exception){exit=1;Debug.LogException(exception);File.WriteAllText(Path.Combine(root,"error.txt"),exception.ToString());}
            finally{SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
        }

        static void Open(GameplayView view,int slot)
        {
            view.GetComponentsInChildren<Button>().Single(button=>button.name=="LockedPotHit"&&button.transform.parent.name=="Order_"+slot).onClick.Invoke();
            Check(view.GetComponentsInChildren<RectTransform>().Count(node=>node.name=="LockedPotUnlock")==1,"slot "+slot+" opens one unlock dialog");
        }

        sealed class Port:IPresentationPort,IRevivalPresentationPort
        {
            readonly ViewSnapshot state;
            public int Used;
            public bool VideoAvailable=true;
            public Port(ViewSnapshot state){this.state=state;}
            public event Action<ViewUpdate> Updated;
            public ViewSnapshot Read()=>state;
            public void Emit(){state.revision++;Updated?.Invoke(new ViewUpdate{snapshot=state,events=Array.Empty<ViewEvent>()});}
            public void SessionAction(ViewAction action){}
            public void Tap(ViewTap command){}
            public void ObserveSupply(ViewSupplyObservation observation){}
            public RevivalOfferView ReadRevivalOffer()=>new RevivalOfferView
            {
                sessionId=state.sessionId,generation=state.sessionGeneration,rewardedVideoAvailable=VideoAvailable,
                shareAvailability=new ShareAvailability("fixture",Used,null,false,DateTimeOffset.UtcNow)
            };
            public Task<RewardApplicationResult> RequestRevivalAsync()=>Task.FromResult(RewardApplicationResult.Unavailable);
            public void DeclineRevival(){}
            public bool CompleteRevivalTransfer(RevivalCompletionToken token)=>false;
        }
    }
}
#endif
