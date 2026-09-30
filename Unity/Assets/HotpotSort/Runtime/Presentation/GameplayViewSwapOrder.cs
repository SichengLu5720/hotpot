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
        IAccountRewardProfileStore SwapRewardProfile=>port as IAccountRewardProfileStore;
        RectTransform swapOverlay,swapFrame,swapNotice;
        SwapOrderTransfer swapTransfer;
        RectTransform[] swapFoods=Array.Empty<RectTransform>();
        int[] swapLegal=Array.Empty<int>();
        string swapSession,swapAppearance,swapIntroMessage;
        float swapAge,swapRefresh,swapNoticeAge;
        bool swapIntro,swapSelecting,swapCreditRequest,swapRequest;
        int swapCreditBaseline,swapSelectedSlot=-1;
        public event Action<RewardRoute> SwapCreditRequested;
        public bool SwapCreditAcquisitionPending=>swapCreditRequest;
        public int SwapOrderCreditCount=>Math.Max(0,SwapRewardProfile?.SwapOrderRewardInventory??0)+CollectionToolCount(RewardKind.SwapOrder);
        bool SwapPresentationLocked=>swapIntro||swapSelecting||swapCreditRequest||swapRequest||swapTransfer!=null;
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
            if(IsToolUsed(RewardKind.SwapOrder)||SwapPresentationLocked||LastSnapshot==null)return;
            CancelScreenPress();swapSession=LastSnapshot.sessionId;swapRefresh=0;swapIntroMessage=null;
            BuildSwapIntro();
        }
        void BuildSwapIntro()
        {
            if(IsToolUsed(RewardKind.SwapOrder)||LastSnapshot==null||LastSnapshot.sessionId!=swapSession){ClearSwapPresentation();return;}
            swapIntro=true;swapSelecting=false;
            var card=ModalV7("SwapOrderIntro",650);
            var title=Label(card,"换一张订单",new Rect(25,20,310,49),29);title.font=displayFont;
            var stage=Skin(card,"SwapIntroDemo",new Rect(25,82,310,270),"ui.panel");
            var stageImage=stage.GetComponent<Image>();if(stageImage)stageImage.raycastTarget=false;
            var uvs=new Rect[foods.Length];for(int i=0;i<uvs.Length;i++)uvs[i]=art.FoodUv(i);
            var demo=stage.gameObject.AddComponent<ToolDemoLoop>();demo.Initialize(RewardKind.SwapOrder,plate,dish,art.Texture("ui.order_card"),foods,uvs,potBody,CurrentBrothSurface(),potRim);
            string note=string.IsNullOrWhiteSpace(swapIntroMessage)?"选择一口已开火的锅，把已收集食材退回暂存，\n再换成当前盘面能够完成的新订单。":swapIntroMessage;
            Label(card,note,new Rect(28,370,304,52),17).color=ModernPalette.Muted;
            int credit=SwapOrderCreditCount;
            var offer=RevivalPort?.ReadRevivalOffer();var share=offer?.shareAvailability;
            var actionRect=new Rect(40,484,280,52);
            if(credit>0)VButton(card,"使用库存（剩余 "+credit+" 次）",actionRect,()=>EnterSwapSelection(),true,21);
            else if(share!=null&&share.Available)VButton(card,"分享领取（今日剩余 "+share.Remaining+" 次）",actionRect,()=>BeginSwapCreditRequest(RewardRoute.SimulatedShare),true,21);
            else VButton(card,"看广告领取",actionRect,()=>BeginSwapCreditRequest(RewardRoute.SimulatedAd),true,21);
            VButton(card,"取消",new Rect(40,552,280,48),CancelSwapPresentation,false,20);
        }
        bool HasLegalSwapTarget()
        {
            var offer=SwapActions?.ReadSwapOrderOffer();
            return offer!=null&&offer.sessionId==swapSession&&(offer.legalSlots?.Length??0)>0&&!offer.busy&&offer.transfer==null;
        }
        void BeginSwapCreditRequest(RewardRoute route)
        {
            if(IsToolUsed(RewardKind.SwapOrder)||swapCreditRequest||!swapIntro)return;
            if(!HasLegalSwapTarget()){swapIntroMessage="当前没有可换的订单，不会领取或消耗次数";BuildSwapIntro();return;}
            swapCreditBaseline=SwapOrderCreditCount;swapCreditRequest=true;swapIntro=false;CloseModal();
            if(SwapCreditRequested==null){ResolveSwapCreditAcquisition(RewardApplicationResult.Unavailable);return;}
            SwapCreditRequested.Invoke(route);
        }
        // TASK-044 binds this completion to the account-level reward grant. Inventory is never spent here.
        public void ResolveSwapCreditAcquisition(RewardApplicationResult result)
        {
            if(!swapCreditRequest)return;
            swapCreditRequest=false;
            if(IsToolUsed(RewardKind.SwapOrder)||LastSnapshot==null||LastSnapshot.sessionId!=swapSession){ClearSwapPresentation();return;}
            if(result==RewardApplicationResult.Applied||SwapOrderCreditCount>swapCreditBaseline)
            {
                if(!EnterSwapSelection())
                {ClearSwapPresentation();SwapNotice("目标已失效，换单次数已保留");}
                return;
            }
            swapIntroMessage=result==RewardApplicationResult.Cancelled?"已取消领取，已有换单次数不受影响":"未领取成功，没有消耗换单次数";
            BuildSwapIntro();
        }
        bool EnterSwapSelection()
        {
            if(IsToolUsed(RewardKind.SwapOrder)){ClearSwapPresentation();return false;}
            if(SwapActions==null||!SwapActions.BeginSwapOrderSelection())
            {swapIntroMessage="当前没有可换的订单，换单次数已保留";if(swapIntro)BuildSwapIntro();return false;}
            if(modal&&modal.name=="SwapOrderIntro")CloseModal();
            swapIntro=false;swapSelecting=true;swapRefresh=0;RefreshSwapOffer();return true;
        }
        public bool HandleSwapBack()
        {
            if(!SwapPresentationLocked)return false;
            if(swapCreditRequest||swapRequest||swapTransfer!=null)return true;
            CancelSwapPresentation();return true;
        }
        void CancelSwapPresentation()
        {
            if(swapCreditRequest||swapRequest||swapTransfer!=null)return;
            SwapActions?.CancelSwapOrderSelection();
            if(swapIntro&&modal&&modal.name=="SwapOrderIntro")CloseModal();
            ClearSwapPresentation();
        }
        void ClearSwapPresentation()
        {
            if(swapIntro&&modal&&modal.name=="SwapOrderIntro")CloseModal();
            if(swapOverlay){swapOverlay.gameObject.SetActive(false);Destroy(swapOverlay.gameObject);}
            swapOverlay=swapFrame=null;swapIntro=false;swapSelecting=false;swapCreditRequest=false;swapRequest=false;swapTransfer=null;
            swapFoods=Array.Empty<RectTransform>();swapLegal=Array.Empty<int>();swapSelectedSlot=-1;swapIntroMessage=null;
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
            Label(swapFrame,"选择要替换的订单",new Rect(80,24,260,40),22).color=ThemeIvory;
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
            if(IsToolUsed(RewardKind.SwapOrder)||swapRequest||swapTransfer!=null)return;
            RefreshSwapOffer();
            if(!swapLegal.Contains(slot)){SwapNotice("当前没有可换的订单");return;}
            swapSelectedSlot=slot;RequestSwap(RewardRoute.SimulatedAd);
        }
        async void RequestSwap(RewardRoute route)
        {
            if(IsToolUsed(RewardKind.SwapOrder)||swapRequest||swapSelectedSlot<0||SwapActions==null)return;
            swapRequest=true;var session=swapSession;
            try
            {
                var result=await SwapActions.SelectSwapOrderAsync(swapSelectedSlot,route);
                if(!this||LastSnapshot?.sessionId!=session)return;
                swapRequest=false;
                if(result==RewardApplicationResult.Applied){RefreshSwapOffer();return;}
                SwapActions.CancelSwapOrderSelection();ClearSwapPresentation();
                SwapNotice(result==RewardApplicationResult.Cancelled?"已取消，换单次数已保留":"目标已失效，换单次数已保留");
            }
            catch(Exception ex){Debug.LogException(ex);if(this&&LastSnapshot?.sessionId==session){swapRequest=false;SwapActions.CancelSwapOrderSelection();ClearSwapPresentation();SwapNotice("换单失败，换单次数已保留");}}
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
            if(swapCreditRequest&&SwapOrderCreditCount>swapCreditBaseline){ResolveSwapCreditAcquisition(RewardApplicationResult.Applied);return;}
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
            else if(swapSelecting&&(swapRefresh-=delta)<=0){swapRefresh=.15f;RefreshSwapOffer();}
        }
    }
}
