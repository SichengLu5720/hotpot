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
    [InitializeOnLoad] public static class Task058LockedPotDiagnostic
    {
        const string Key="Task058.LockedPotDiagnostic";

        static Task058LockedPotDiagnostic()
        {EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}

        public static void Run()
        {
            SessionState.SetBool(Key,true);
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        static void Check(bool condition,string message)
        {if(!condition)throw new InvalidOperationException(message);Debug.Log("TASK058 PASS "+message);}

        static Button Button(GameplayView view,string name)
            =>view.GetComponentsInChildren<Button>().Single(button=>button.name=="Action_"+name);

        static void Execute()
        {
            int exit=0;GameplayView view=null;
            string root=Path.GetFullPath("../.harness/qa/TASK-058/view");
            try
            {
                var args=Environment.GetCommandLineArgs();int evidence=Array.IndexOf(args,"-visualEvidence");
                if(evidence>=0&&evidence+1<args.Length)root=args[evidence+1];
                Directory.CreateDirectory(root);

                view=new GameObject("Task058LockedPotFixture").AddComponent<GameplayView>();
                view.ConfigureAssets(PresentationAssets.CandidateRoot);
                var state=Capture.Fixture();state.thirdPotThreshold=31;state.fourthPotThreshold=0;
                var port=new Port(state);view.Bind(port);view.SetForeground(true);view.World.SetSimulating(false);
                RewardKind? requested=null;RewardRoute? route=null;int requests=0;
                view.RewardRequested+=(kind,rewardRoute)=>{requested=kind;route=rewardRoute;requests++;};

                var thirdTrack=view.GetComponentsInChildren<RectTransform>(true).Single(node=>node.name=="UnlockProgress"&&node.parent.name=="Order_2");
                var fourthTrack=view.GetComponentsInChildren<RectTransform>(true).Single(node=>node.name=="UnlockProgress"&&node.parent.name=="Order_3");
                Check(thirdTrack.gameObject.activeSelf&&!fourthTrack.gameObject.activeSelf,"third progress visible and fourth hidden");
                state.completedOrders=30;port.Emit();
                Check(Math.Abs(thirdTrack.Find("Fill").GetComponent<Image>().fillAmount-30f/31f)<.0001f,"third progress uses 31 orders");
                Open(view,2);
                Check(Button(view,"看广告解锁").interactable,"third ad available");
                Check(!view.GetComponentsInChildren<Button>().Any(button=>button.name=="Action_分享解锁"),"third never offers share");
                Check(view.GetComponentsInChildren<Text>().Any(text=>text.text=="再完成 1 锅订单即可解锁"),"third remaining count uses 31");
                Capture.Capture(view,1080,1920,Path.Combine(root,"third-pot-ad.png"));
                Button(view,"看广告解锁").onClick.Invoke();
                Check(requests==1&&requested==RewardKind.ThirdPot&&route==RewardRoute.SimulatedAd,"third ad route emitted");

                Open(view,3);
                Check(Button(view,"分享解锁").interactable&&!view.GetComponentsInChildren<Button>().Any(button=>button.name=="Action_看广告解锁"),"fourth single reward button prefers share");
                Check(view.GetComponentsInChildren<RectTransform>().Single(node=>node.name=="LockedPotUnlock").GetComponentsInChildren<Button>().Length==2,"fourth has exactly reward and cancel buttons");
                Check(view.GetComponentsInChildren<Text>().Any(text=>text.text=="开锅"),"fourth title has no early-unlock promise");
                Check(!view.GetComponentsInChildren<Text>().Any(text=>text.text.StartsWith("再完成 ")),"fourth has no remaining-order message");
                Capture.Capture(view,1080,1920,Path.Combine(root,"fourth-pot-share.png"));
                Button(view,"分享解锁").onClick.Invoke();
                Check(requests==2&&requested==RewardKind.FourthPot&&route==RewardRoute.SimulatedShare,"fourth share route emitted");

                port.Used=ShareAvailability.DailyLimit;
                Open(view,3);
                Check(!view.GetComponentsInChildren<Button>().Any(button=>button.name=="Action_分享解锁")&&Button(view,"看广告解锁").interactable,"exhausted share replaces single action with video");
                Capture.Capture(view,1080,1920,Path.Combine(root,"fourth-pot-ad-fallback.png"));
                Button(view,"看广告解锁").onClick.Invoke();
                Check(requests==3&&requested==RewardKind.FourthPot&&route==RewardRoute.SimulatedAd,"fourth ad route emitted");

                port.VideoAvailable=false;
                Open(view,3);
                Check(!Button(view,"广告暂不可用").interactable,"unavailable reward action disabled");
                Button(view,"广告暂不可用").onClick.Invoke();
                Check(requests==3,"disabled video callback cannot request reward");
                Button(view,"取消").onClick.Invoke();
                Check(requests==3&&!view.GetComponentsInChildren<RectTransform>().Any(node=>node.name=="LockedPotUnlock"),"cancel closes without request");
                port.Used=0;
                Open(view,3);
                Check(Button(view,"分享解锁").interactable&&!view.GetComponentsInChildren<Button>().Any(button=>button.name=="Action_看广告解锁"),"share stays available without video");
                Button(view,"取消").onClick.Invoke();
                Open(view,2);
                Check(!Button(view,"广告暂不可用").interactable&&!view.GetComponentsInChildren<Button>().Any(button=>button.name=="Action_分享解锁"),"third unavailable video does not fall back to share");
                state.completedOrders=31;state.orders.Single(order=>order.slot==2).enabled=true;port.Emit();
                Check(!view.GetComponentsInChildren<RectTransform>().Any(node=>node.name=="LockedPotUnlock"),"automatic unlock closes stale third dialog");
                Check(!view.GetComponentsInChildren<RectTransform>().Any(node=>node.name=="UnlockProgress"),"no active progress remains after third unlock");
                File.WriteAllText(Path.Combine(root,"result.txt"),"PASS: third 31-order progress/ad-only; fourth no progress/countdown and single share-first/ad-fallback button; availability, cancellation, routes and stale dialog cleanup verified in Unity PlayMode.\n");

            }
            catch(Exception exception){exit=1;Debug.LogException(exception);File.WriteAllText(Path.Combine(root,"error.txt"),exception.ToString());}
            finally{if(view){view.World.Clear();view.Bind(null);}SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
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
