using System;
using System.Linq;
using HotpotSort.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace HotpotSort.Presentation
{
    public sealed partial class GameplayView
    {
        ISwapOrderActions SwapActions=>port as ISwapOrderActions;
        RectTransform swapOverlay,swapFrame,swapNotice;
        SwapOrderTransfer swapTransfer;
        RectTransform[] swapFoods=Array.Empty<RectTransform>();
        int[] swapLegal=Array.Empty<int>();
        string swapSession;
        string swapAppearance;
        float swapAge,swapRefresh,swapNoticeAge;
        bool swapSelecting,swapRequest;
        int swapSelectedSlot=-1;
        bool SwapPresentationLocked=>swapSelecting||swapRequest||swapTransfer!=null;
        bool IsSwapOverlayObject(GameObject go)=>swapOverlay&&go&&go.transform.IsChildOf(swapOverlay);
        RectTransform SwapPanel(Transform parent,string name,Rect rect,Color color)
        {var node=Node(parent,name,rect);node.gameObject.AddComponent<Image>().color=color;return node;}

        void SwapNotice(string text)
        {
            if(swapNotice)Destroy(swapNotice.gameObject);
            if(!board)return;
            swapNotice=SwapPanel(swapFrame?swapFrame:board,"SwapNotice",new Rect(55,310,310,45),new Color(.10f,.06f,.035f,.94f));
            swapNotice.GetComponent<Image>().raycastTarget=false;
            Label(swapNotice,text,new Rect(5,0,300,45),18).color=ThemeIvory;
            swapNoticeAge=2f;
        }
        void BeginSwapPresentation()
        {
            if(SwapPresentationLocked)return;
            if(SwapActions==null||!SwapActions.BeginSwapOrderSelection()){SwapNotice("当前没有可换的订单");return;}
            if(swapNotice){swapNotice.gameObject.SetActive(false);Destroy(swapNotice.gameObject);swapNotice=null;}
            CancelScreenPress();swapSelecting=true;swapSession=LastSnapshot.sessionId;swapRefresh=0;
            RefreshSwapOffer();
        }
        public bool HandleSwapBack()
        {
            if(!SwapPresentationLocked)return false;
            if(!swapRequest&&swapTransfer==null){CloseModal();CancelSwapPresentation();}
            return true;
        }
        void CancelSwapPresentation()
        {
            if(swapRequest||swapTransfer!=null)return;
            SwapActions?.CancelSwapOrderSelection();ClearSwapPresentation();
        }
        void ClearSwapPresentation()
        {
            if(swapOverlay){swapOverlay.gameObject.SetActive(false);Destroy(swapOverlay.gameObject);}
            swapOverlay=swapFrame=null;swapSelecting=false;swapRequest=false;swapTransfer=null;
            swapFoods=Array.Empty<RectTransform>();swapLegal=Array.Empty<int>();swapSelectedSlot=-1;
        }
        void BuildSwapOverlay()
        {
            swapAppearance=string.Join("|",orderSignatures);
            if(swapOverlay){swapOverlay.gameObject.SetActive(false);Destroy(swapOverlay.gameObject);}
            swapOverlay=Node(content,"SwapOrderSelection",new Rect(0,0,viewport.width,viewport.height));
            var shield=swapOverlay.gameObject.AddComponent<Image>();shield.color=Color.clear;
            var cancel=swapOverlay.gameObject.AddComponent<Button>();cancel.targetGraphic=shield;cancel.transition=Selectable.Transition.None;
            cancel.onClick.AddListener(CancelSwapPresentation);
            swapFrame=LogicalFrame(swapOverlay,"SwapFrame");
            void dark(Rect r){if(r.width<=0||r.height<=0)return;var n=SwapPanel(swapFrame,"SwapDim",r,new Color(0,0,0,.65f));n.GetComponent<Image>().raycastTarget=false;}
            dark(new Rect(-2000,-2000,4420,5000));
            for(int slot=0;slot<4;slot++)
            {
                if(!swapLegal.Contains(slot))continue;
                float x=Mathf.Max(0,6+slot*103-14),right=Mathf.Min(420,6+slot*103+114);
                var bright=Instantiate(orderNodes[slot],swapFrame,false);bright.name="SwapBrightOrder_"+slot;
                foreach(var graphic in bright.GetComponentsInChildren<Graphic>())graphic.raycastTarget=false;
                foreach(var button in bright.GetComponentsInChildren<Button>())button.enabled=false;
                var hit=SwapPanel(swapFrame,"SwapTarget_"+slot,new Rect(x,72,right-x,158),Color.clear);
                var b=hit.gameObject.AddComponent<Button>();b.targetGraphic=hit.GetComponent<Image>();b.transition=Selectable.Transition.None;
                int target=slot;BindButtonClick(b,()=>ChooseSwapTarget(target));
            }
            // Illegal order regions are opaque to input too: selecting one does not cancel.
            for(int slot=0;slot<4;slot++)if(!swapLegal.Contains(slot))
            {
                var hit=SwapPanel(swapFrame,"SwapIllegal_"+slot,new Rect(6+slot*103,72,100,158),Color.clear);
                hit.gameObject.AddComponent<Button>().transition=Selectable.Transition.None;
            }
            if(modal)modal.SetAsLastSibling();
        }
        void RefreshSwapOffer()
        {
            var offer=SwapActions?.ReadSwapOrderOffer();
            if(offer==null||offer.sessionId!=swapSession){ClearSwapPresentation();return;}
            if(offer.transfer!=null){StartSwapTransfer(offer.transfer);return;}
            if(!offer.selecting&&!offer.busy&&!swapRequest){ClearSwapPresentation();return;}
            var legal=offer.legalSlots??Array.Empty<int>();
            if(!swapOverlay||!swapLegal.SequenceEqual(legal)||swapAppearance!=string.Join("|",orderSignatures)){swapLegal=legal;BuildSwapOverlay();}
        }
        void ChooseSwapTarget(int slot)
        {
            if(swapRequest||swapTransfer!=null)return;
            RefreshSwapOffer();
            if(!swapLegal.Contains(slot)){SwapNotice("当前没有可换的订单");return;}
            swapSelectedSlot=slot;
            if(CollectionToolCount(RewardKind.SwapOrder)>0){RequestSwap(RewardRoute.SimulatedAd);return;}
            var offer=RevivalPort?.ReadRevivalOffer();var availability=offer?.shareAvailability;
            var route=availability!=null&&availability.Available?RewardRoute.SimulatedShare:RewardRoute.SimulatedAd;
            if(route==RewardRoute.SimulatedAd&&offer!=null&&!offer.rewardedVideoAvailable){SwapNotice("广告暂不可用");return;}
            string note=route==RewardRoute.SimulatedShare?"今日分享剩余 "+availability.Remaining+" 次":"分享暂不可用，可通过广告领取。";
            ToolRewardOfferV7(RewardKind.SwapOrder,"领取换单",note,route==RewardRoute.SimulatedShare?"分享领取":"看广告领取",route);
        }
        async void RequestSwap(RewardRoute route)
        {
            if(swapRequest||swapSelectedSlot<0||SwapActions==null)return;
            swapRequest=true;var session=swapSession;
            try
            {
                var result=await SwapActions.SelectSwapOrderAsync(swapSelectedSlot,route);
                if(!this||LastSnapshot?.sessionId!=session)return;
                swapRequest=false;RefreshSwapOffer();
                if(result!=RewardApplicationResult.Applied&&result!=RewardApplicationResult.Cancelled)SwapNotice("当前没有可换的订单");
            }
            catch(Exception ex){Debug.LogException(ex);if(this&&LastSnapshot?.sessionId==session){swapRequest=false;RefreshSwapOffer();SwapNotice("换单失败，请重试");}}
        }
        void StartSwapTransfer(SwapOrderTransfer transfer)
        {
            if(swapTransfer!=null&&swapTransfer.token==transfer.token)return;
            swapTransfer=transfer;swapSelecting=false;swapAge=0;
            if(!swapOverlay)BuildSwapOverlay();
            swapFoods=new RectTransform[transfer.items.Length];
            for(int i=0;i<swapFoods.Length;i++)
            {
                var item=transfer.items[i];int food=0;int.TryParse(item.ingredientId?.Replace("food_",""),out food);
                swapFoods[i]=Food(swapFrame,food,new Rect(6+transfer.slot*103+26+i*8,138+i*5,48,48));
                swapFoods[i].name="SwapReturn_"+item.itemId;
            }
        }
        void TickSwapPresentation(float delta)
        {
            if(swapNotice){swapNoticeAge-=delta;swapNotice.SetAsLastSibling();if(swapNoticeAge<=0)Destroy(swapNotice.gameObject);}
            if(!SwapPresentationLocked)return;
            if(LastSnapshot==null||LastSnapshot.sessionId!=swapSession||LastSnapshot.phase==ViewPhase.Entry||LastSnapshot.phase==ViewPhase.Aborted||LastSnapshot.phase==ViewPhase.Overflow||LastSnapshot.phase==ViewPhase.Won){SwapActions?.CancelSwapOrderSelection();ClearSwapPresentation();return;}
            if(swapOverlay){Place(swapOverlay,new Rect(0,0,viewport.width,viewport.height));float s=Mathf.Min(viewport.width/420,viewport.height/900);swapFrame.localScale=Vector3.one*s;swapFrame.anchoredPosition=new Vector2((viewport.width-420*s)/2,-(viewport.height-900*s)/2);swapOverlay.SetAsLastSibling();if(modal)modal.SetAsLastSibling();}
            if(swapTransfer!=null)
            {
                if(!foreground||LastSnapshot.pauseReasons!=ViewPauseReasons.None)return;
                swapAge+=delta;float t=Mathf.Clamp01(swapAge/.34f);
                for(int i=0;i<swapFoods.Length;i++)if(swapFoods[i])
                {
                    var from=new Vector2(6+swapTransfer.slot*103+50+i*8,162+i*5);
                    var to=new Vector2(75+swapTransfer.items[i].bufferIndex*67,259);
                    var p=Vector2.Lerp(from,to,t);swapFoods[i].anchoredPosition=new Vector2(p.x,-p.y);
                }
                if(t>=1){var completed=swapTransfer;ClearSwapPresentation();SwapActions?.CompleteSwapOrderTransfer(completed.sessionId,completed.token);}
            }
            else if((swapRefresh-=delta)<=0){swapRefresh=.15f;RefreshSwapOffer();}
        }
    }
}
