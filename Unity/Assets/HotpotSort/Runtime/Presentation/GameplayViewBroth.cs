using System;
using HotpotSort.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace HotpotSort.Presentation
{
    // Presentation only. The composition owns authentication, operations, retries and authoritative updates.
    public sealed partial class GameplayView
    {
        public event Action BrothActivityOpened,BrothRefreshRequested,BrothInvitationShareRequested,BrothActivityClosed;
        public event Action<string> BrothAssistConfirmRequested,BrothInvitationDismissRequested,BrothClaimRequested,BrothSelectRequested;
        CollectionDocument brothDocument;
        RectTransform brothEntry,brothOverlay,brothCard,brothConfirm,collectionBrothPage;
        Transform brothEntryParent;
        Text brothNotice;
        bool brothDevelopment,brothBusy,brothDetails,brothFriend,brothAssistSucceeded,collectionShowingBroths;
        BrothInvitation brothInvitation;
        string brothMessage="",brothClaimCandidate,brothTextureId;
        Texture2D brothTexture;
        static readonly string[] BrothIds={BrothCatalog.Red,BrothCatalog.Clear,BrothCatalog.Tomato,BrothCatalog.Mushroom};
        CollectionDocument BrothDocument=>collectionStore?.ReadCollection()??brothDocument;
        public bool BrothVisible=>brothOverlay;
        public string PresentedBrothId=>BrothCatalog.IsValid(BrothDocument?.currentBroth)?BrothDocument.currentBroth:BrothCatalog.Red;
        public static string BrothName(string id)=>id==BrothCatalog.Clear?"清汤":id==BrothCatalog.Tomato?"番茄":id==BrothCatalog.Mushroom?"菌锅":"红汤";

        public void UpdateBrothPresentation(CollectionDocument document,bool developmentSimulation=false)
        {
            brothDocument=document;brothDevelopment=developmentSimulation;
            RefreshBrothEntry();RefreshBrothSurfaces();
            if(collectionBrothPage)RenderBrothCollection();
            if(brothOverlay)RenderBrothActivity();
        }
        public void SetBrothOperationState(bool busy,string message="")
        {
            brothBusy=busy;brothMessage=message??"";
            if(brothOverlay)RenderBrothActivity();
            if(collectionBrothPage)RenderBrothCollection();
        }
        public void ShowBrothAssistSuccess(string invitationId)
        {
            // Only an authenticated success callback may enter this state, never a page-open/share callback.
            if(!brothFriend||brothInvitation?.invitationId!=invitationId)return;
            brothAssistSucceeded=true;brothBusy=false;brothMessage="助力成功，三项奖励已到账";RenderBrothActivity();
        }
        public void AttachBrothEntry(Transform frame){brothEntryParent=frame;RefreshBrothEntry();}
        void RefreshBrothEntry()
        {
            if(!brothEntryParent)return;
            bool visible=BrothDocument?.entryUnlocked==true;
            if(!visible){if(brothEntry){brothEntry.gameObject.SetActive(false);Destroy(brothEntry.gameObject);brothEntry=null;}return;}
            if(!brothEntry)
            {
                // Same logical vertical axis as the existing TASK-030 entry; no backplate or locked variant.
                float axis=collectionEntry?collectionEntry.anchoredPosition.x+collectionEntry.rect.width*.5f:CollectionSidebarAxis();
                brothEntry=Node(brothEntryParent,"BrothActivityEntry",new Rect(axis-39.375f,327.1875f,78.75f,65.625f));
                var image=brothEntry.gameObject.AddComponent<RawImage>();image.texture=PresentationAssets.Load<Texture2D>("Hotpot/TASK031/UI/broth_activity_entry");
                var button=brothEntry.gameObject.AddComponent<Button>();button.targetGraphic=image;BindButtonClick(button,OpenBrothActivity);
                var alert=Node(brothEntry,"ClaimReminder",new Rect(59.06f,3.28f,9.84f,9.84f));
                var dot=alert.gameObject.AddComponent<Image>();dot.sprite=BrothReminderSprite();dot.color=new Color32(198,51,37,255);dot.raycastTarget=false;
                var outline=alert.gameObject.AddComponent<Outline>();outline.effectColor=new Color32(240,193,133,255);outline.effectDistance=new Vector2(.6f,-.6f);
            }
            brothEntry.Find("ClaimReminder").gameObject.SetActive(BrothCatalog.HasClaimReminder(BrothDocument));
        }
        // Screen-normalized left sidebar, anchored inside the supplied safe viewport.
        // At the approved 1080x1920 baseline this is exactly X=102; retain uniform icon scale.
        float CollectionSidebarAxis()
        {
            float scale=Mathf.Min(viewport.width/420,viewport.height/900);
            return scale>0?(viewport.width*(102f/1080)-(viewport.width-420*scale)*.5f)/scale:40;
        }
        void LayoutCollectionSidebar()
        {
            float axis=CollectionSidebarAxis();
            if(collectionEntry)Place(collectionEntry,new Rect(axis-30,246,60,70));
            if(brothEntry)Place(brothEntry,new Rect(axis-39.375f,327.1875f,78.75f,65.625f));
        }
        Texture2D BrothSurface(string id)
        {
            if(id==BrothCatalog.Red)return potBroth?potBroth:PresentationAssets.Load<Texture2D>(PresentationAssets.CandidateRoot+"/pots/pot_broth");
            return PresentationAssets.Load<Texture2D>("Hotpot/TASK031/Broths/"+id);
        }
        Sprite BrothReminderSprite()
        {
            if(!rewardCircle)
            {
                rewardCircleTexture=new Texture2D(64,64,TextureFormat.RGBA32,false);
                for(int y=0;y<64;y++)for(int x=0;x<64;x++)rewardCircleTexture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(31.5f-Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f)))));
                rewardCircleTexture.Apply();rewardCircle=Sprite.Create(rewardCircleTexture,new Rect(0,0,64,64),new Vector2(.5f,.5f));
            }
            return rewardCircle;
        }
        Texture2D CurrentBrothSurface()
        {
            string id=PresentedBrothId;
            if(id==BrothCatalog.Red)return potBroth;
            if(brothTextureId!=id||!brothTexture){brothTexture=BrothSurface(id);brothTextureId=id;}
            return brothTexture;
        }
        void RefreshBrothSurfaces()
        {
            // Mutate the existing soup image only. No order/node rebuild and no soup on unlit pots.
            foreach(var order in orderNodes){if(!order)continue;var soup=order.Find("Broth")?.GetComponent<RawImage>();if(soup)soup.texture=CurrentBrothSurface();}
        }
        void BrothPot(Transform parent,string id,Rect rect)
        {
            string root=PresentationAssets.CandidateRoot;
            Picture(parent,potBody?potBody:PresentationAssets.Load<Texture2D>(root+"/pots/pot_body"),rect,"PotBody");
            Picture(parent,BrothSurface(id),rect,"Broth_"+id);
            Picture(parent,potRim?potRim:PresentationAssets.Load<Texture2D>(root+"/pots/pot_rim"),rect,"PotRim");
        }
        RectTransform BrothPanel(Transform parent,string name,Rect rect)
        {
            // Reuse the current restaurant's copper-edged wood, not a baked preview screen.
            return art!=null?Skin(parent,name,rect,"ui.bottom_bar"):CollectionBox(parent,name,rect,new Color32(48,29,17,255));
        }
        Button BrothButton(Transform parent,string title,Rect rect,Action action,bool enabled=true,int size=16)
        {
            var node=CollectionBox(parent,"BrothAction_"+title,rect,new Color32(113,49,30,255));
            CollectionText(node,title,new Rect(2,0,rect.width-4,rect.height),size);
            var button=node.gameObject.AddComponent<Button>();button.targetGraphic=node.GetComponent<Image>();button.interactable=enabled&&!brothBusy;
            BindButtonClick(button,()=>{if(!brothBusy)action?.Invoke();});return button;
        }
        void BrothTabs()
        {
            BrothButton(collectionCard,"食材",new Rect(16,65,159,32),()=>SetCollectionTab(false),true,17);
            BrothButton(collectionCard,"锅底",new Rect(195,65,159,32),()=>SetCollectionTab(true),true,17);
            SetCollectionTab(false);
        }
        public void OpenBrothCollection(){OpenCollection();if(collectionOverlay)SetCollectionTab(true);}
        void SetCollectionTab(bool broths)
        {
            collectionShowingBroths=broths;
            if(collectionGrid)collectionGrid.parent.gameObject.SetActive(!broths);
            if(collectionNotice)collectionNotice.gameObject.SetActive(!broths);
            var trade=collectionCard?collectionCard.Find("CollectionAction_好友交换"):null;if(trade)trade.gameObject.SetActive(!broths);
            if(!collectionBrothPage&&collectionCard)collectionBrothPage=Node(collectionCard,"BrothCollection",new Rect(16,121,338,622));
            if(collectionBrothPage){collectionBrothPage.gameObject.SetActive(broths);if(broths)RenderBrothCollection();}
            foreach(string title in new[]{"食材","锅底"}){var tab=collectionCard?.Find("BrothAction_"+title);if(tab)tab.GetComponent<Image>().color=(title=="锅底")==broths?new Color32(106,71,37,255):new Color32(45,30,20,255);}
        }
        void RenderBrothCollection()
        {
            if(!collectionBrothPage)return;ClearChildren(collectionBrothPage);var doc=BrothDocument;
            for(int i=0;i<4;i++)
            {
                string id=BrothIds[i];bool owned=id==BrothCatalog.Red||doc?.ownedBroths?.Contains(id)==true,current=PresentedBrothId==id;
                var tile=CollectionBox(collectionBrothPage,"BrothTile_"+id,new Rect(i%2*174,i/2*278,164,252),new Color32(51,38,25,255));
                tile.GetComponent<Outline>().effectDistance=current?new Vector2(1,-1):new Vector2(.35f,-.35f);
                BrothPot(tile,id,new Rect(10,20,144,144));CollectionText(tile,BrothName(id),new Rect(5,155,154,32),20);
                if(!owned){CollectionLock(tile,new Rect(37,209,12,16));CollectionText(tile,"未解锁",new Rect(55,201,89,30),15).color=CollectionGold;}
                else if(current){CollectionText(tile,id==BrothCatalog.Red?"默认拥有":"已拥有",new Rect(5,188,154,24),14).color=CollectionGold;CollectionText(tile,"当前使用",new Rect(5,218,154,26),16);}
                else {CollectionText(tile,"已拥有",new Rect(5,183,154,20),12).color=CollectionGold;BrothButton(tile,"切换使用",new Rect(24,210,116,32),()=>BrothSelectRequested?.Invoke(id),true,15);}
            }
            if(!string.IsNullOrEmpty(brothMessage))CollectionText(collectionBrothPage,brothMessage,new Rect(4,565,330,42),13);
        }
        public void OpenBrothActivity()
        {
            if(BrothDocument?.entryUnlocked!=true||brothOverlay)return;
            if(collectionOverlay&&!TryCloseCollection())return;
            brothFriend=false;brothDetails=false;brothMessage="";brothClaimCandidate=null;
            CreateBrothOverlay();RenderBrothActivity();BrothActivityOpened?.Invoke();BrothRefreshRequested?.Invoke();
        }
        public void OpenBrothAssistConfirmation(BrothInvitation invitation,bool developmentSimulation=false)
        {
            // Friend landing is intentionally independent of entryUnlocked.
            if(invitation==null||string.IsNullOrEmpty(invitation.invitationId))return;
            if(collectionOverlay&&!TryCloseCollection())return;
            ResetBrothOverlay();brothInvitation=invitation;brothFriend=true;brothDevelopment=developmentSimulation;
            brothAssistSucceeded=false;brothDetails=false;brothMessage="";brothClaimCandidate=null;CreateBrothOverlay();RenderBrothActivity();
        }
        void CreateBrothOverlay()
        {
            CloseModal();brothOverlay=CollectionLayer("BrothActivity");modal=brothOverlay;
            var frame=LogicalFrame(brothOverlay,"ModalFrame");brothCard=BrothPanel(frame,"BrothCard",new Rect(25,83,370,758));
        }
        void BrothCandidates(float y,bool states)
        {
            var doc=BrothDocument;bool qualified=BrothCatalog.HasClaimReminder(doc);
            for(int i=0;i<3;i++)
            {
                string id=BrothIds[i+1];float x=20+i*110;
                BrothPot(brothCard,id,new Rect(x,y,110,110));CollectionText(brothCard,BrothName(id),new Rect(x,y+108,110,29),18);
                if(states)
                {
                    bool chosen=doc?.brothActivityChoice==id,owned=doc?.ownedBroths?.Contains(id)==true;
                    if(qualified)BrothButton(brothCard,"选"+BrothName(id),new Rect(x+10,y+143,90,30),()=>ShowBrothClaimConfirmation(id),true,14);
                    else CollectionText(brothCard,chosen?"本活动已选":owned?"已拥有":"未解锁",new Rect(x,y+138,110,26),14).color=CollectionGold;
                }
            }
        }
        void BrothRewards(float y)
        {
            string[] titles={"提示","清空暂存","打乱"},icons={"hint","clear_buffer","shuffle"};
            for(int i=0;i<3;i++)
            {
                var tile=CollectionBox(brothCard,"AssistReward_"+icons[i],new Rect(25+i*112,y,96,109),new Color32(55,39,26,255));
                CollectionPicture(tile,"RewardIcon",new Rect(30,9,36,39),"icons/"+icons[i],false);
                CollectionText(tile,titles[i],new Rect(2,60,92,23),15);CollectionText(tile,"× 1",new Rect(2,84,92,22),17).color=CollectionGold;
            }
        }
        void RenderBrothActivity()
        {
            if(!brothCard)return;ClearChildren(brothCard);brothConfirm=null;
            CollectionText(brothCard,brothFriend?"好友助力":"锅底活动",new Rect(50,17,270,43),25);
            BrothButton(brothCard,"‹",new Rect(13,21,27,32),CloseBrothActivity,true,24);
            if(brothDevelopment)CollectionText(brothCard,"Development 模拟 · 不计真实奖励",new Rect(20,64,330,22),12).color=CollectionGold;
            if(brothFriend)RenderBrothFriend();else if(brothDetails)RenderBrothDetails();else RenderBrothMain();
            if(!string.IsNullOrEmpty(brothClaimCandidate)&&!brothFriend&&BrothCatalog.HasClaimReminder(BrothDocument))BuildBrothClaimConfirmation();
        }
        void RenderBrothMain()
        {
            var doc=BrothDocument;bool qualified=doc?.brothActivityQualified==true,chosen=!string.IsNullOrEmpty(doc?.brothActivityChoice);
            CollectionText(brothCard,"邀 1 位好友，锅底三选一",new Rect(18,96,334,36),20);
            CollectionText(brothCard,chosen?"本活动已选定 "+BrothName(doc.brothActivityChoice):"成功助力后，任选一种永久拥有",new Rect(18,136,334,26),16).color=CollectionGold;
            BrothCandidates(174,true);
            CollectionText(brothCard,"好友助力  "+(qualified?"1 / 1":"0 / 1"),new Rect(30,356,310,34),21);
            var track=CollectionBox(brothCard,"AssistProgress",new Rect(62,400,246,10),new Color32(40,27,18,255));
            if(qualified)CollectionBox(track,"Fill",new Rect(1,1,244,8),CollectionGold);
            CollectionText(brothCard,"助力者可获得",new Rect(30,423,310,30),18);BrothRewards(462);
            if(!qualified)BrothButton(brothCard,"邀请好友助力",new Rect(59,612,252,46),()=>BrothInvitationShareRequested?.Invoke(),true,19);
            else CollectionText(brothCard,chosen?"已永久拥有，不可改选":"资格永久保留，请选择一种锅底",new Rect(25,613,320,44),17);
            brothNotice=CollectionText(brothCard,string.IsNullOrEmpty(brothMessage)?"仅打开分享不计入有效助力":brothMessage,new Rect(20,671,330,32),14);
            BrothButton(brothCard,"活动详情 ﹀",new Rect(111,714,148,30),()=>{brothDetails=true;RenderBrothActivity();},true,16);
        }
        void RenderBrothDetails()
        {
            CollectionText(brothCard,"好友助力  "+(BrothDocument?.brothActivityQualified==true?"1 / 1":"0 / 1"),new Rect(25,90,320,32),20);
            BrothCandidates(126,false);
            CollectionText(brothCard,"活动详情",new Rect(30,265,210,36),20).alignment=TextAnchor.MiddleLeft;
            BrothButton(brothCard,"收起 ∧",new Rect(278,270,66,30),()=>{brothDetails=false;RenderBrothActivity();},true,14);
            string[] heads={"1  一位好友，永久三选一","2  好友进入并确认才有效","3  成功助力后，好友得奖励","4  同一好友只为你助力一次","5  选择确认后不可更改"};
            string[] body={"清汤、番茄、菌锅中选择一种。\n资格永久保留，暂不设活动期限。","不同账号从专属分享进入小游戏并确认。\n不能给自己助力，仅打开页面不计数。","提示、清空暂存、打乱各 1 次。\n成功后到账，奖励可跨局、跨天保存。","北京时间每日最多帮助 3 位好友。\n你获 1 次有效助力后不再计数发奖。","选择需二次确认，本活动只能获得一种。\n未选两种可通过未来其他活动获得。"};
            for(int i=0;i<5;i++){CollectionText(brothCard,heads[i],new Rect(34,308+i*85,308,31),17).alignment=TextAnchor.MiddleLeft;CollectionText(brothCard,body[i],new Rect(34,340+i*85,308,46),14).alignment=TextAnchor.UpperLeft;}
        }
        void RenderBrothFriend()
        {
            CollectionText(brothCard,"帮好友解锁一种新锅底",new Rect(20,87,330,36),20);BrothCandidates(126,false);
            CollectionText(brothCard,"好友完成三选一，永久拥有",new Rect(20,274,330,30),16).color=CollectionGold;
            CollectionText(brothCard,brothAssistSucceeded?"助力成功，奖励已到账":"助力成功后，你将获得",new Rect(20,337,330,36),20);BrothRewards(391);
            CollectionText(brothCard,"同一好友只能为该发起者助力一次\n每日最多帮助 3 位好友",new Rect(22,521,326,58),15).color=CollectionGold;
            bool can=brothInvitation!=null&&brothInvitation.canAssist&&!brothInvitation.isInitiator&&!brothAssistSucceeded;
            BrothButton(brothCard,brothAssistSucceeded?"已助力":"确认助力",new Rect(59,602,252,46),()=>BrothAssistConfirmRequested?.Invoke(brothInvitation.invitationId),can,20);
            string fallback=brothAssistSucceeded?"提示、清空暂存、打乱各 +1":brothInvitation?.isInitiator==true?"不能给自己助力":!can?"该邀请当前无法助力":"成功助力后奖励才会到账";
            brothNotice=CollectionText(brothCard,string.IsNullOrEmpty(brothMessage)?fallback:brothMessage,new Rect(22,665,326,36),14);
            BrothButton(brothCard,brothAssistSucceeded?"完成":"稍后再说",new Rect(119,710,132,32),CloseBrothActivity,true,16);
        }
        public void ShowBrothClaimConfirmation(string id)
        {
            if(brothBusy||!brothOverlay||brothFriend||!BrothCatalog.IsCandidate(id)||!BrothCatalog.HasClaimReminder(BrothDocument))return;
            brothClaimCandidate=id;BuildBrothClaimConfirmation();
        }
        void BuildBrothClaimConfirmation()
        {
            if(brothConfirm){brothConfirm.gameObject.SetActive(false);Destroy(brothConfirm.gameObject);}
            brothConfirm=Node(brothCard,"BrothChoiceConfirmation",new Rect(0,0,370,758));var shade=brothConfirm.gameObject.AddComponent<Image>();shade.color=new Color(0,0,0,.78f);
            var card=BrothPanel(brothConfirm,"ConfirmCard",new Rect(20,165,330,382));
            CollectionText(card,"确认选择"+BrothName(brothClaimCandidate)+"？",new Rect(15,25,300,37),21);BrothPot(card,brothClaimCandidate,new Rect(105,69,120,120));
            CollectionText(card,"本活动只能选择一次\n确认后永久拥有，不可改选",new Rect(20,195,290,61),17);
            BrothButton(card,"返回再想想",new Rect(22,295,130,42),()=>{brothClaimCandidate=null;RenderBrothActivity();},true,16);
            BrothButton(card,"确认选择",new Rect(178,295,130,42),()=>{if(BrothCatalog.HasClaimReminder(BrothDocument))BrothClaimRequested?.Invoke(brothClaimCandidate);},true,16);
        }
        public void CloseBrothActivity()
        {
            if(brothBusy)return;bool friend=brothFriend;string invitation=brothInvitation?.invitationId;ResetBrothOverlay();
            if(friend&&!string.IsNullOrEmpty(invitation))BrothInvitationDismissRequested?.Invoke(invitation);BrothActivityClosed?.Invoke();
        }
        public bool HandleBrothBack()
        {
            if(!brothOverlay)return false;if(brothBusy)return true;
            if(!string.IsNullOrEmpty(brothClaimCandidate)){brothClaimCandidate=null;RenderBrothActivity();}
            else if(brothDetails){brothDetails=false;RenderBrothActivity();}else CloseBrothActivity();return true;
        }
        void ResetBrothOverlay()
        {
            if(modal==brothOverlay)modal=null;if(brothOverlay){brothOverlay.gameObject.SetActive(false);Destroy(brothOverlay.gameObject);}
            brothOverlay=brothCard=brothConfirm=null;brothNotice=null;brothClaimCandidate=null;brothInvitation=null;brothFriend=false;brothBusy=false;brothMessage="";
        }
    }
}
