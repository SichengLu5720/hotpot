using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace HotpotSort.Presentation
{
    public sealed partial class GameplayView
    {
        ICollectionStore collectionStore;
        IIngredientTradeService collectionTrade;
        RectTransform collectionTradePanel,collectionTradeBackdrop;
        Text collectionTradeNotice,collectionOfferLabel,collectionReceiveLabel;
        string collectionOffer,collectionReceive,collectionTradeOperation,collectionTradeOperationKey;
        bool collectionTradeBusy;
        public event Action<IngredientTradeRequest> CollectionTradeShareRequested;
        RectTransform collectionEntry,collectionOverlay,collectionCard,collectionGrid,collectionRewardOverlay;
        Text collectionNotice;
        readonly HashSet<string> collectionDraft=new HashSet<string>();
        readonly Dictionary<string,Texture2D> collectionMuted=new Dictionary<string,Texture2D>();
        readonly HashSet<string> collectionPresentedRewards=new HashSet<string>();
        Action collectionRewardContinue;
        static readonly Color CollectionCream=new Color32(247,228,190,255),CollectionGold=new Color32(198,140,79,255);
        public bool CollectionVisible=>collectionOverlay;
        public bool CollectionRewardVisible=>collectionRewardOverlay;

        public void ConfigureCollection(ICollectionStore store,IIngredientTradeService trade=null)
        {
            if(collectionStore!=null)collectionStore.CollectionChanged-=RefreshCollection;
            collectionStore=store;collectionTrade=trade;
            if(store!=null)store.CollectionChanged+=RefreshCollection;
            RefreshCollection();
        }
        // Call after the Entry logical frame has been constructed.
        public void AttachCollectionEntry(Transform entryFrame)
        {
            if(collectionEntry)Destroy(collectionEntry.gameObject);
            collectionEntry=Node(entryFrame,"CollectionEntry",new Rect(CollectionSidebarAxis()-30,246,60,70));
            RefreshCollectionEntry();
        }
        public void RefreshCollectionEntry()
        {
            if(!collectionEntry)return;
            ClearChildren(collectionEntry);
            bool unlocked=collectionStore!=null&&collectionStore.ReadCollection().entryUnlocked;
            var image=CollectionPicture(collectionEntry,"Badge",new Rect(0,0,60,70),"icons/ingredient_entry",!unlocked);
            image.raycastTarget=true;
            var button=collectionEntry.GetComponent<Button>()??collectionEntry.gameObject.AddComponent<Button>();
            button.onClick.RemoveAllListeners();button.targetGraphic=image;button.interactable=unlocked;
            BindButtonClick(button,OpenCollection);
            if(!unlocked)CollectionLock(collectionEntry,new Rect(47,44,12,17));
        }
        void RefreshCollection()
        {
            RefreshCollectionEntry();
            if(collectionGrid)RenderCollectionGrid();
            UpdateBrothPresentation(collectionStore?.ReadCollection(),brothDevelopment);
        }
        public void OpenCollection()
        {
            if(collectionStore==null||!collectionStore.ReadCollection().entryUnlocked||collectionOverlay)return;
            brothMessage="";
            collectionDraft.Clear();foreach(var id in collectionStore.BeginSelectionDraft())collectionDraft.Add(id);
            collectionOverlay=CollectionLayer("Collection");modal=collectionOverlay;
            var frame=LogicalFrame(collectionOverlay,"ModalFrame");
            collectionCard=BrothPanel(frame,"CollectionCard",new Rect(25,83,370,758));
            CollectionText(collectionCard,"收藏",new Rect(45,12,280,40),25);
            CollectionButton(collectionCard,"‹",new Rect(9,12,34,36),()=>TryCloseCollection(),25);
            var clip=Node(collectionCard,"CollectionViewport",new Rect(44,113,282,540));
            clip.gameObject.AddComponent<RectMask2D>();
            var hit=clip.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            collectionGrid=Node(clip,"CollectionGrid",new Rect(0,0,282,952));
            var scroll=clip.gameObject.AddComponent<ScrollRect>();scroll.viewport=clip;scroll.content=collectionGrid;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=28;
            CollectionButton(collectionCard,"好友交换",new Rect(126,699,118,30),OpenCollectionTrade,15);
            collectionNotice=CollectionText(collectionCard,"",new Rect(42,661,286,33),13);
            RenderCollectionGrid();
            BrothTabs();
        }
        void RenderCollectionGrid()
        {
            if(!collectionGrid||collectionStore==null)return;
            ClearChildren(collectionGrid);var doc=collectionStore.ReadCollection();
            for(int i=0;i<CollectionCatalog.Count;i++)
            {
                string id=CollectionCatalog.Id(i);bool unlocked=doc.unlocked.Contains(id),selected=collectionDraft.Contains(id);
                var card=CollectionBox(collectionGrid,id,new Rect((i%4)*71,(i/4)*119,64,110),unlocked?new Color32(66,48,35,255):new Color32(48,39,30,255));
                var edge=card.GetComponent<Outline>();edge.effectColor=selected?CollectionGold:new Color32(121,90,58,255);
                CollectionPicture(card,"Food",new Rect(9,25,46,48),"food/"+id,!unlocked);
                CollectionText(card,CollectionCatalog.Name(id),new Rect(1,83,62,20),11).color=unlocked?CollectionCream:new Color32(155,147,134,255);
                if(unlocked)
                {
                    CollectionText(card,selected?"✓":"○",new Rect(45,3,17,18),13).color=CollectionGold;
                    if(doc.duplicates!=null&&doc.duplicates.Length>i&&doc.duplicates[i]>0)CollectionText(card,"×"+doc.duplicates[i],new Rect(3,3,32,17),10);
                    var button=card.gameObject.AddComponent<Button>();button.targetGraphic=card.GetComponent<Image>();
                    BindButtonClick(button,()=>{if(!collectionDraft.Remove(id))collectionDraft.Add(id);collectionNotice.text="";RenderCollectionGrid();});
                }
                else CollectionLock(card,new Rect(27,44,12,17));
            }
        }
        // Route page close, platform back and Escape here; true means the back event was consumed.
        public bool HandleCollectionBack()
        {
            if(HandleBrothBack())return true;
            if(collectionRewardOverlay)return true;
            if(!collectionOverlay)return false;
            if(collectionTradePanel){CloseCollectionTrade();return true;}
            TryCloseCollection();return true;
        }
        public bool TryCloseCollection()
        {
            if(!collectionOverlay)return true;
            if(collectionTradeBusy||brothBusy)return false;
            if(collectionDraft.Count<CollectionCatalog.SessionCount){SetCollectionTab(false);collectionNotice.text=CollectionCatalog.TooFewMessage;return false;}
            if(collectionStore==null||!collectionStore.TrySaveSelection(collectionDraft)){collectionNotice.text="暂时无法保存，请稍后重试";return false;}
            if(modal==collectionOverlay)modal=null;
            collectionOverlay.gameObject.SetActive(false);Destroy(collectionOverlay.gameObject);
            collectionOverlay=collectionCard=collectionGrid=collectionBrothPage=null;collectionNotice=null;return true;
        }
        void OpenCollectionTrade()
        {
            if(collectionTrade==null){collectionNotice.text="好友交换暂不可用，请稍后再试";return;}
            if(collectionTradePanel)return;
            collectionTradeBackdrop=CollectionBox(collectionCard,"TradeBackdrop",new Rect(0,0,370,758),new Color32(48,29,17,255));
            collectionTradePanel=CollectionBox(collectionTradeBackdrop,"FriendTrade",new Rect(32,0,306,758),new Color32(48,29,17,255));
            CollectionText(collectionTradePanel,"好友交换",new Rect(44,14,218,40),24);
            CollectionButton(collectionTradePanel,"‹",new Rect(9,14,34,36),CloseCollectionTrade,25);
            CollectionText(collectionTradePanel,"一份换一份",new Rect(20,60,266,28),15);
            CollectionButton(collectionTradePanel,"选择提供的食材",new Rect(22,103,262,42),()=>OpenTradePicker(true),15);
            collectionOfferLabel=CollectionText(collectionTradePanel,"",new Rect(22,149,262,25),14);
            CollectionButton(collectionTradePanel,"选择想要的食材",new Rect(22,182,262,42),()=>OpenTradePicker(false),15);
            collectionReceiveLabel=CollectionText(collectionTradePanel,"",new Rect(22,228,262,25),14);
            CollectionButton(collectionTradePanel,"发起交换",new Rect(83,268,140,38),CreateCollectionTrade,17);
            collectionTradeNotice=CollectionText(collectionTradePanel,"",new Rect(10,315,286,42),13);
            RefreshTradeLabels();RefreshCollectionTrades();
        }
        public async void OpenCollectionTradeRequest(string requestId)
        {
            if(collectionTrade==null||collectionStore==null||string.IsNullOrEmpty(requestId))return;
            OpenCollection();if(!collectionOverlay)return;
            try
            {
                var result=await collectionTrade.RefreshAsync(requestId);if(!this||!collectionOverlay)return;
                if(result==null||!result.Succeeded){collectionNotice.text=TradeFailureText(result);return;}
                collectionStore.ApplyAuthoritativeSnapshot(result.collection);OpenCollectionTrade();
            }
            catch{if(this&&collectionNotice)collectionNotice.text="网络暂不可用，请重试";}
        }
        void CloseCollectionTrade(){if(collectionTradeBusy)return;if(collectionTradeBackdrop){collectionTradeBackdrop.gameObject.SetActive(false);Destroy(collectionTradeBackdrop.gameObject);}collectionTradePanel=collectionTradeBackdrop=null;}
        void RefreshTradeLabels(){if(collectionOfferLabel)collectionOfferLabel.text=CollectionCatalog.Name(collectionOffer);if(collectionReceiveLabel)collectionReceiveLabel.text=CollectionCatalog.Name(collectionReceive);}
        void OpenTradePicker(bool offer)
        {
            if(collectionTradeBusy)return;
            var picker=CollectionBox(collectionTradePanel,"TradePicker",new Rect(0,0,306,758),new Color32(48,29,17,255));
            CollectionText(picker,offer?"提供一份副本":"想要的食材",new Rect(40,14,226,40),22);
            CollectionButton(picker,"‹",new Rect(8,14,32,36),()=>{picker.gameObject.SetActive(false);Destroy(picker.gameObject);},24);
            var clip=Node(picker,"PickerViewport",new Rect(12,70,282,660));clip.gameObject.AddComponent<RectMask2D>();
            var background=clip.gameObject.AddComponent<Image>();background.color=Color.clear;
            var grid=Node(clip,"PickerGrid",new Rect(0,0,282,768));
            var scroll=clip.gameObject.AddComponent<ScrollRect>();scroll.content=grid;scroll.viewport=clip;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var doc=collectionStore.ReadCollection();int count=0;
            for(int i=0;i<32;i++)
            {
                if(offer&&(doc.duplicates==null||doc.duplicates.Length<=i||doc.duplicates[i]<1))continue;
                string id=CollectionCatalog.Id(i);var tile=CollectionBox(grid,id,new Rect(count%4*71,count/4*96,64,88),new Color32(66,48,35,255));count++;
                CollectionPicture(tile,"Food",new Rect(12,9,40,43),"food/"+id,false);
                CollectionText(tile,CollectionCatalog.Name(id),new Rect(1,60,62,20),10);
                var button=tile.gameObject.AddComponent<Button>();button.targetGraphic=tile.GetComponent<Image>();BindButtonClick(button,()=>{if(offer)collectionOffer=id;else collectionReceive=id;RefreshTradeLabels();picker.gameObject.SetActive(false);Destroy(picker.gameObject);});
            }
            grid.sizeDelta=new Vector2(282,Mathf.Max(660,Mathf.Ceil(count/4f)*96));
            if(count==0)CollectionText(picker,"还没有可交换的副本",new Rect(20,260,266,40),17);
        }
        async void CreateCollectionTrade()
        {
            if(collectionTradeBusy)return;
            if(string.IsNullOrEmpty(collectionOffer)||string.IsNullOrEmpty(collectionReceive)||collectionOffer==collectionReceive){collectionTradeNotice.text="请选择两种不同的食材";return;}
            string offered=collectionOffer,received=collectionReceive;
            await RunCollectionTrade("create:"+offered+":"+received,op=>collectionTrade.CreateAsync(offered,received,op),true);
        }
        async Task RunCollectionTrade(string key,Func<string,Task<IngredientTradeResult>> action,bool share=false)
        {
            if(collectionTradeBusy)return;collectionTradeBusy=true;
            if(collectionTradeOperationKey!=key){collectionTradeOperationKey=key;collectionTradeOperation=Guid.NewGuid().ToString("N");}
            if(collectionTradeNotice)collectionTradeNotice.text="正在处理…";
            try
            {
                var result=await action(collectionTradeOperation);if(!this)return;
                if(result!=null&&result.Succeeded)
                {
                    collectionStore.ApplyAuthoritativeSnapshot(result.collection);collectionTradeOperationKey=null;
                    if(collectionTradeNotice)collectionTradeNotice.text=share?"交换已创建，可分享给好友":"已更新";
                    if(share&&result.request!=null)CollectionTradeShareRequested?.Invoke(result.request);
                }
                else if(collectionTradeNotice)collectionTradeNotice.text=TradeFailureText(result);
            }
            catch{if(collectionTradeNotice)collectionTradeNotice.text="网络暂不可用，请重试";}
            finally{collectionTradeBusy=false;}
            if(this&&collectionTradePanel)RefreshCollectionTrades();
        }
        static string TradeFailureText(IngredientTradeResult result)
        {
            if(result==null)return "暂时无法交换，请重试";
            switch(result.failure){case IngredientTradeFailure.InsufficientDuplicates:return "可交换副本不足";case IngredientTradeFailure.Expired:return "交换已过期";case IngredientTradeFailure.AlreadyResolved:return "交换已处理";case IngredientTradeFailure.NotConfigured:return "好友交换暂不可用";case IngredientTradeFailure.Offline:return "网络暂不可用，请重试";default:return "暂时无法交换，请稍后重试";}
        }
        async void RefreshCollectionTrades()
        {
            if(!collectionTradePanel||collectionTrade==null)return;var owner=collectionTradePanel;
            try
            {
                var result=await collectionTrade.ListAsync();if(!this||!owner||owner!=collectionTradePanel)return;
                if(result==null||!result.Succeeded){collectionTradeNotice.text=TradeFailureText(result);return;}
                collectionStore.ApplyAuthoritativeSnapshot(result.collection);
                var old=owner.Find("TradeListViewport");if(old){old.gameObject.SetActive(false);Destroy(old.gameObject);}
                var clip=Node(owner,"TradeListViewport",new Rect(12,366,282,374));clip.gameObject.AddComponent<RectMask2D>();var hit=clip.gameObject.AddComponent<Image>();hit.color=Color.clear;
                var list=Node(clip,"Requests",new Rect(0,0,282,Mathf.Max(374,result.requests.Count*106)));
                var scroll=clip.gameObject.AddComponent<ScrollRect>();scroll.content=list;scroll.viewport=clip;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
                for(int i=0;i<result.requests.Count;i++)
                {
                    var request=result.requests[i];var row=CollectionBox(list,"Request",new Rect(0,i*106,282,98),new Color32(66,48,35,255));
                    CollectionText(row,CollectionCatalog.Name(request.offeredId)+" ×1  ⇄  "+CollectionCatalog.Name(request.receivedId)+" ×1",new Rect(4,5,274,28),13);
                    string[] states={"待交换","已交换","已拒绝","已撤回","已过期"};CollectionText(row,states[(int)request.status],new Rect(4,32,274,22),12);
                    if(request.canAccept)CollectionButton(row,"接受",new Rect(25,60,105,28),async()=>await RunCollectionTrade("accept:"+request.requestId,op=>collectionTrade.AcceptAsync(request.requestId,op)),13);
                    if(request.canReject)CollectionButton(row,"拒绝",new Rect(152,60,105,28),async()=>await RunCollectionTrade("reject:"+request.requestId,op=>collectionTrade.RejectAsync(request.requestId,op)),13);
                    if(request.canCancel){CollectionButton(row,"撤回",new Rect(152,60,105,28),async()=>await RunCollectionTrade("cancel:"+request.requestId,op=>collectionTrade.CancelAsync(request.requestId,op)),13);CollectionButton(row,"分享好友",new Rect(25,60,105,28),()=>CollectionTradeShareRequested?.Invoke(request),13);}
                }
                if(result.requests.Count==0)CollectionText(list,"暂无交换请求",new Rect(10,65,262,36),15);
            }
            catch{if(this&&collectionTradeNotice)collectionTradeNotice.text="网络暂不可用，请重试";}
        }

        // Invoke before revealing settlement actions. The callback continues the existing flow.
        public bool ShowCollectionReward(CollectionReward reward,Action continuation)
        {
            if(reward==null)return false;
            string key=reward.day+":"+reward.sessionId;
            if(collectionPresentedRewards.Contains(key)||collectionRewardOverlay)return false;
            collectionPresentedRewards.Add(key);collectionRewardContinue=continuation;
            collectionRewardOverlay=CollectionLayer("CollectionReward");
            var frame=LogicalFrame(collectionRewardOverlay,"RewardFrame");
            var card=CollectionBox(frame,"RewardCard",new Rect(65,245,290,355),new Color32(48,29,17,255));
            CollectionText(card,"今日收获",new Rect(20,20,250,44),26);
            if(!string.IsNullOrEmpty(reward.ingredientId))
            {
                CollectionPicture(card,"RewardFood",new Rect(107,95,76,76),"food/"+reward.ingredientId,false);
                CollectionText(card,CollectionCatalog.Name(reward.ingredientId)+" ×1",new Rect(20,185,250,30),20);
                CollectionText(card,reward.firstUnlock?"新食材已解锁":"可交换副本 +1",new Rect(20,223,250,26),15);
            }
            else
            {
                string[] names={"提示","清空暂存","打乱"};
                for(int i=0;i<reward.tools.Count;i++){int tool=reward.tools[i];CollectionText(card,(tool>=0&&tool<3?names[tool]:"道具")+" ×1",new Rect(20,108+i*54,250,40),22);}
            }
            CollectionButton(card,"收下",new Rect(79,284,132,42),()=>{var next=collectionRewardContinue;collectionRewardContinue=null;collectionRewardOverlay.gameObject.SetActive(false);Destroy(collectionRewardOverlay.gameObject);collectionRewardOverlay=null;next?.Invoke();},20);
            return true;
        }
        public void DisposeCollectionPresentation()
        {
            if(collectionStore!=null)collectionStore.CollectionChanged-=RefreshCollection;
            collectionStore=null;collectionRewardContinue=null;
            ResetCollectionTransientPresentation();
            foreach(var texture in collectionMuted.Values)if(texture)Destroy(texture);collectionMuted.Clear();
        }
        // For actual session teardown only; ordinary Back must use HandleCollectionBack.
        public void ResetCollectionTransientPresentation()
        {
            ResetBrothOverlay();collectionBrothPage=null;
            collectionRewardContinue=null;
            if(modal==collectionOverlay)modal=null;
            if(collectionOverlay){collectionOverlay.gameObject.SetActive(false);Destroy(collectionOverlay.gameObject);}
            if(collectionRewardOverlay){collectionRewardOverlay.gameObject.SetActive(false);Destroy(collectionRewardOverlay.gameObject);}
            collectionOverlay=collectionGrid=collectionCard=collectionTradePanel=collectionTradeBackdrop=collectionRewardOverlay=null;
            collectionNotice=collectionTradeNotice=collectionOfferLabel=collectionReceiveLabel=null;
        }
        public void ResizeCollectionPresentation()
        {
            foreach(var layer in new[]{collectionOverlay,collectionRewardOverlay})
            {
                if(!layer)continue;Place(layer,new Rect(0,0,viewport.width,viewport.height));
                var frame=layer.GetChild(0) as RectTransform;if(!frame)continue;
                float scale=Mathf.Min(viewport.width/420,viewport.height/900);frame.localScale=Vector3.one*scale;
                frame.anchoredPosition=new Vector2((viewport.width-420*scale)*.5f,-(viewport.height-900*scale)*.5f);
            }
        }
        RectTransform CollectionLayer(string name)
        {
            var layer=Node(content,name,new Rect(0,0,viewport.width,viewport.height));
            var shade=layer.gameObject.AddComponent<Image>();shade.color=new Color(.07f,.04f,.02f,.80f);shade.raycastTarget=true;return layer;
        }
        RectTransform CollectionBox(Transform parent,string name,Rect rect,Color color)
        {
            var node=Node(parent,name,rect);var image=node.gameObject.AddComponent<Image>();image.sprite=rounded;image.type=Image.Type.Sliced;image.color=color;
            var outline=node.gameObject.AddComponent<Outline>();outline.effectColor=CollectionGold;outline.effectDistance=new Vector2(.6f,-.6f);return node;
        }
        Text CollectionText(Transform parent,string text,Rect rect,int size){var label=Label(parent,text,rect,size);label.color=CollectionCream;return label;}
        void CollectionButton(Transform parent,string text,Rect rect,UnityEngine.Events.UnityAction action,int size)
        {
            var node=CollectionBox(parent,"CollectionAction_"+text,rect,new Color32(80,50,28,255));
            CollectionText(node,text,new Rect(0,0,rect.width,rect.height),size);
            var button=node.gameObject.AddComponent<Button>();button.targetGraphic=node.GetComponent<Image>();BindButtonClick(button,action);
        }
        RawImage CollectionPicture(Transform parent,string name,Rect rect,string relative,bool muted)
        {
            string root=string.IsNullOrEmpty(assetRoot)?"Hotpot/TASK001/v10/r001":assetRoot;
            var texture=PresentationAssets.Load<Texture2D>(root+"/"+relative);
            if(muted&&texture)
            {
                string key=root+"/"+relative;
                if(!collectionMuted.TryGetValue(key,out var grey))
                {
                    var source=texture.GetPixels32();int width=Mathf.Min(160,texture.width),height=Mathf.Max(1,Mathf.RoundToInt(width*(float)texture.height/texture.width));var pixels=new Color32[width*height];
                    for(int y=0;y<height;y++)for(int x=0;x<width;x++){var p=source[Mathf.Min(texture.height-1,y*texture.height/height)*texture.width+Mathf.Min(texture.width-1,x*texture.width/width)];byte g=(byte)((p.r*.299f+p.g*.587f+p.b*.114f)*.67f);pixels[y*width+x]=new Color32(g,g,g,p.a);}
                    grey=new Texture2D(width,height,TextureFormat.RGBA32,false);grey.SetPixels32(pixels);grey.Apply(false,true);collectionMuted[key]=grey;
                }
                texture=grey;
            }
            if(texture){float scale=Mathf.Min(rect.width/texture.width,rect.height/texture.height);var size=new Vector2(texture.width*scale,texture.height*scale);rect=new Rect(rect.center-size*.5f,size);}
            var image=Node(parent,name,rect).gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;
            return image;
        }
        void CollectionLock(Transform parent,Rect rect)
        {
            var node=Node(parent,"Lock",rect);
            var shackle=CollectionBox(node,"Shackle",new Rect(rect.width*.2f,0,rect.width*.6f,rect.height*.65f),CollectionCream);
            var hole=CollectionBox(shackle,"Hole",new Rect(2,2,rect.width*.6f-4,rect.height*.4f),new Color32(57,40,27,255));
            CollectionBox(node,"Body",new Rect(0,rect.height*.4f,rect.width,rect.height*.6f),CollectionCream);
        }
    }
}
