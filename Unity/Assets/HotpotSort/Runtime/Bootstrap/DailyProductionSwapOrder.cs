using System;
using System.Linq;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using UnityEngine;
namespace HotpotSort.Bootstrap
{
    public sealed partial class DailyProductionComposition
    {
        bool swapSelecting,swapBusy;
        [Serializable] sealed class SwapCharge { public string operation,sessionId; }
        SwapCharge swapCharge;
        HotpotSort.Platform.WeChatIngredientTradeService swapAuthority;
        bool swapCommitBusy;
        void CancelRemoteSwapReservation()
        {
            var charge=swapCharge;var authority=swapAuthority;swapCharge=null;swapAuthority=null;
            if(charge!=null&&authority!=null)_=authority.CancelSwapOrderAsync(charge.operation,charge.sessionId);
        }
        async Task FlushSwapCommit()
        {
            if(swapCommitBusy||collectionAuthority==null||collection==null)return;
            string key=CollectionPendingKey+".swapCommit",json=PlayerPrefs.GetString(key,"");if(json.Length==0)return;
            swapCommitBusy=true;
            try
            {
                var charge=JsonUtility.FromJson<SwapCharge>(json);
                var result=await collectionAuthority.CommitSwapOrderAsync(charge.operation,charge.sessionId);
                if((result.Succeeded||result.failure==IngredientTradeFailure.AlreadyResolved)&&PlayerPrefs.GetString(key,"")==json){PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
            }
            finally{swapCommitBusy=false;}
        }
        bool SwapInputLocked=>current!=null&&(current.Snapshot.Status==GameStatus.Running||current.Snapshot.Status==GameStatus.Paused)&&(swapSelecting||swapBusy||current.SwapOrderPending);
        bool CanObserveSwap=>current!=null&&controller!=null&&controller.CanAcceptInput&&!view.ShuffleFeedbackActive&&tutorialStep==HotpotSort.Presentation.ViewTutorialStep.None&&!bufferWarning&&!pendingBufferWarning;
        public SwapOrderOffer ReadSwapOrderOffer()
        {
            if(current==null)return new SwapOrderOffer();
            if(current.Snapshot.Status==GameStatus.Failed||current.Snapshot.Status==GameStatus.Won||current.Snapshot.Status==GameStatus.Aborted){swapSelecting=false;CancelRemoteSwapReservation();}
            return new SwapOrderOffer{sessionId=current.Snapshot.SessionId,snapshotRevision=current.Snapshot.TransactionId,selecting=swapSelecting,busy=swapBusy,legalSlots=CanObserveSwap&&!current.SwapOrderPending?current.GetSwapOrderTargets(CaptureOrderClickability()):Array.Empty<int>(),transfer=current.ReadSwapOrderTransfer()};
        }
        public bool BeginSwapOrderSelection()
        {
            if(SwapInputLocked||!CanObserveSwap)return false;
            if(collection!=null&&PlayerPrefs.HasKey(CollectionPendingKey+".swapCommit")){_=FlushSwapCommit();return false;}
            if(current.GetSwapOrderTargets(CaptureOrderClickability()).Length==0)return false;
            swapSelecting=true;return true;
        }
        public void CancelSwapOrderSelection(){if(!swapBusy&&current?.SwapOrderPending!=true)swapSelecting=false;}
        public async Task<RewardApplicationResult> SelectSwapOrderAsync(int slot,RewardRoute route)
        {
            if(!swapSelecting||swapBusy||!CanObserveSwap)return RewardApplicationResult.Unavailable;
            var expected=current;long generation=controller.Generation;int identity=current.OrderIdentity(slot);
            Func<bool> fresh=()=>ReferenceEquals(expected,current)&&controller.Generation==generation&&CanObserveSwap&&current.OrderIdentity(slot)==identity&&current.GetSwapOrderTargets(CaptureOrderClickability()).Contains(slot);
            if(!fresh())return RewardApplicationResult.Unavailable;
            swapBusy=true;
            try
            {
                if(view.CollectionToolCount(RewardKind.SwapOrder)>0)
                {
                    if(!rewardService.IsDevelopmentSimulation)
                    {
                        if(collectionAuthority==null)return RewardApplicationResult.Unavailable;
                        var authority=collectionAuthority;var charge=new SwapCharge{operation=Guid.NewGuid().ToString("N"),sessionId=expected.Snapshot.SessionId};
                        var reserved=await authority.ReserveSwapOrderAsync(charge.operation,charge.sessionId);
                        if(!reserved.Succeeded||reserved.toolReservationStatus!="Reserved"||!ReferenceEquals(authority,collectionAuthority)||!fresh())
                        {_=authority.CancelSwapOrderAsync(charge.operation,charge.sessionId);return RewardApplicationResult.Unavailable;}
                        var remote=current.BeginSwapOrder(slot,identity,Boundary,CaptureOrderClickability());
                        if(!remote.Accepted||remote.Reason!=null){_=authority.CancelSwapOrderAsync(charge.operation,charge.sessionId);return RewardApplicationResult.Unavailable;}
                        swapCharge=charge;swapAuthority=authority;swapSelecting=false;return RewardApplicationResult.Applied;
                    }
                    var applied=current.BeginSwapOrder(slot,identity,Boundary,CaptureOrderClickability(),()=>collection.TryConsumeTool(RewardKind.SwapOrder,Guid.NewGuid().ToString("N")));
                    if(applied.Accepted&&applied.Reason==null){swapSelecting=false;return RewardApplicationResult.Applied;}
                    return RewardApplicationResult.Unavailable;
                }
                route=RewardRoutes.ForService(route,rewardService.IsDevelopmentSimulation);
                if(route==RewardRoute.WeChatRewardedVideo&&rewardService is IRewardChannelAvailability channels&&!channels.IsRewardedVideoAvailable)return RewardApplicationResult.Unavailable;
                var request=new RewardRequest(generation,RewardKind.SwapOrder,route,HotpotSort.Session.TimeResolver.ChallengeDay(rewards.UtcNow),expected.Snapshot.SessionId);
                var result=await rewards.RequestDetailedAsync(request,()=>controller.Generation,fresh,()=>
                {
                    var applied=current.BeginSwapOrder(slot,identity,Boundary,CaptureOrderClickability());
                    return applied.Accepted&&applied.Reason==null;
                },controller.SetRewardPaused);
                if(result==RewardApplicationResult.Applied&&ReferenceEquals(expected,current))swapSelecting=false;
                return result;
            }
            finally{if(ReferenceEquals(expected,current)&&generation==controller.Generation)swapBusy=false;}
        }
        public bool CompleteSwapOrderTransfer(string expectedSessionId,string token)
        {
            if(current==null||current.Snapshot.SessionId!=expectedSessionId)return false;
            var result=current.CompleteSwapOrder(token,Boundary,CaptureOrderClickability());
            if(result.Accepted&&result.Reason==null&&swapCharge!=null)
            {
                PlayerPrefs.SetString(CollectionPendingKey+".swapCommit",JsonUtility.ToJson(swapCharge));PlayerPrefs.Save();
                swapCharge=null;swapAuthority=null;_=FlushSwapCommit();
            }
            return result.Accepted&&result.Reason==null;
        }
    }
}
