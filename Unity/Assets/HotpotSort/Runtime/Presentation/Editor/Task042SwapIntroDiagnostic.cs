#if UNITY_EDITOR
using System;
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
    public static class Task042SwapIntroDiagnostic
    {
        static int checks;
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Debug.Log("TASK042 SWAP PASS "+message);}
        static object Call(GameplayView view,string name,params object[] args)=>typeof(GameplayView).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,args);

        sealed class Port:IPresentationPort,ISwapOrderActions,IAccountRewardProfileStore
        {
            public ViewSnapshot state;
            public int[] legal={0};
            public bool selecting;
            public int inventory;
            public SwapOrderTransfer transfer;
            public event Action<ViewUpdate> Updated;
            public ViewSnapshot Read()=>state;
            public void SessionAction(ViewAction action){}
            public void Tap(ViewTap tap){}
            public void ObserveSupply(ViewSupplyObservation observation){}
            public SwapOrderOffer ReadSwapOrderOffer()=>new SwapOrderOffer{sessionId=state.sessionId,selecting=selecting,legalSlots=legal,transfer=transfer};
            public bool BeginSwapOrderSelection(){selecting=legal.Length>0;return selecting;}
            public void CancelSwapOrderSelection(){selecting=false;}
            public Task<RewardApplicationResult> SelectSwapOrderAsync(int slot,RewardRoute route)
            {
                if(!selecting||inventory<=0||!legal.Contains(slot))return Task.FromResult(RewardApplicationResult.Unavailable);
                inventory--;selecting=false;
                transfer=new SwapOrderTransfer{sessionId=state.sessionId,token="task042-transfer",slot=slot,items=new[]{new SwapOrderItem{itemId="returned",ingredientId="food_00",bufferIndex=0}}};
                return Task.FromResult(RewardApplicationResult.Applied);
            }
            public bool CompleteSwapOrderTransfer(string session,string token){transfer=null;return true;}
            public int ConsecutiveFormalFailures=>0;
            public int SwapOrderRewardInventory=>inventory;
            public AccountMutationResult RecordFormalResult(string resultId,string sessionId,string challengeDay,FormalChallengeResultKind result,DateTimeOffset completedUtc)=>AccountMutationResult.Applied;
            public AccountMutationResult GrantSwapOrderRewards(string grantId,int amount,DateTimeOffset effectiveUtc){inventory+=amount;return AccountMutationResult.Applied;}
            public AccountMutationResult ConsumeSwapOrderReward(string spendId,DateTimeOffset effectiveUtc){if(inventory<=0)return AccountMutationResult.InsufficientInventory;inventory--;return AccountMutationResult.Applied;}
            public void Push()=>Updated?.Invoke(new ViewUpdate{snapshot=state});
        }

        public static void Run()
        {
            int exit=0;GameplayView view=null;
            try
            {
                view=new GameObject("Task042SwapIntroFixture").AddComponent<GameplayView>();view.ConfigureAssets(PresentationAssets.CandidateRoot);
                var state=Capture.Fixture();state.sessionId="task042-swap";state.orders[0].count=2;state.tutorialStep=ViewTutorialStep.None;state.buffer=new ViewItem[5];state.pauseReasons=ViewPauseReasons.None;
                var port=new Port{state=state};view.Bind(port);view.SetViewport(new Rect(0,0,1080,1920),1920);view.SetForeground(true);view.World.SetSimulating(false);Canvas.ForceUpdateCanvases();
                RewardRoute requestedRoute=RewardRoute.SimulatedAd;int rewardRequests=0;
                view.SwapCreditRequested+=route=>{requestedRoute=route;rewardRequests++;};

                Call(view,"BeginSwapPresentation");
                Check(!port.selecting&&view.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="SwapOrderIntro"),"tool click opens introduction before selection");
                Check(state.pauseReasons==ViewPauseReasons.None,"introduction does not pause session state");
                var intro=view.GetComponentsInChildren<RectTransform>(true).Single(t=>t.name=="SwapOrderIntro");
                Check(Mathf.Abs(((RectTransform)intro.Find("ModalFrame/IvoryCard")).rect.height-650)<.01f&&Mathf.Abs(intro.GetComponentInChildren<ToolDemoLoop>().GetComponent<RectTransform>().rect.height-270)<.01f,"swap uses shared tool card and demonstration heights");
                Check(intro.GetComponentsInChildren<Button>().Length==2,"only one acquisition action and cancel are displayed");
                var card=intro.Find("ModalFrame/IvoryCard");
                Check(card.GetComponentsInChildren<Text>().Count(t=>t.transform.parent==card)==2,"card has only title and explanation text with no extra status row");
                var ad=view.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Action_看广告领取");ad.onClick.Invoke();
                Check(rewardRequests==1&&requestedRoute==RewardRoute.SimulatedAd&&!port.selecting,"reward route starts before target selection");
                port.inventory=1;view.ResolveSwapCreditAcquisition(RewardApplicationResult.Applied);
                Check(port.selecting&&view.GetComponentsInChildren<Button>(true).Any(b=>b.name=="SwapTarget_0"),"successful credit enters legal order selection");
                Check(view.HandleSwapBack()&&!port.selecting&&port.inventory==1,"selection cancellation retains acquired inventory");

                Call(view,"BeginSwapPresentation");
                Check(view.GetComponentsInChildren<Text>(true).Any(t=>t.text.Contains("使用库存")&&t.text.Contains("1")),"introduction displays retained inventory action");
                var inventoryIntro=view.GetComponentsInChildren<Transform>(true).Last(t=>t.name=="SwapOrderIntro"&&t.gameObject.activeInHierarchy);
                Check(inventoryIntro.GetComponentsInChildren<Button>().Length==2&&!inventoryIntro.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("看广告领取")||t.text.Contains("分享领取")),"inventory takes priority over reward actions");
                view.GetComponentsInChildren<Button>(true).Single(b=>b.name.StartsWith("Action_使用库存")).onClick.Invoke();
                Call(view,"ChooseSwapTarget",0);
                Check(port.inventory==0,"accepted swap transaction consumes one credit");
                Call(view,"TickSwapPresentation",.20f);Check(port.transfer!=null,"return transfer remains active before 0.34 seconds");
                Call(view,"TickSwapPresentation",.15f);Check(port.transfer==null,"return transfer completes after 0.34 seconds");

                port.inventory=1;port.legal=Array.Empty<int>();Call(view,"BeginSwapPresentation");
                view.GetComponentsInChildren<Button>(true).Single(b=>b.name.StartsWith("Action_使用库存")).onClick.Invoke();
                Check(!port.selecting&&port.inventory==1&&view.GetComponentsInChildren<Text>(true).Any(t=>t.text.Contains("次数已保留")),"invalid target retains inventory and stays out of selection");
                var invalidIntro=view.GetComponentsInChildren<Transform>(true).Last(t=>t.name=="SwapOrderIntro"&&t.gameObject.activeInHierarchy);
                var invalidCard=invalidIntro.Find("ModalFrame/IvoryCard");
                var notice=invalidCard.GetComponentsInChildren<Text>().Single(t=>t.text.Contains("次数已保留"));
                Check(notice.transform.parent==invalidCard&&Mathf.Abs(notice.rectTransform.anchoredPosition.y+370)<.01f,"invalid target message replaces the shared explanation region");
                Debug.Log("TASK042_SWAP_INTRO_PASS checks="+checks);
            }
            catch(Exception ex){exit=1;Debug.LogException(ex);}
            finally{if(view)UnityEngine.Object.DestroyImmediate(view.gameObject);EditorApplication.Exit(exit);}
        }
    }
}
#endif
