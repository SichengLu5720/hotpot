using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using UnityEngine;
using UnityEngine.UI;
using HotpotSort.Contracts.RemoteAssets;

namespace HotpotSort.Presentation
{
    public sealed partial class GameplayView
    {
        public bool CollectionSettlementPending { get; set; }
        public int CollectionToolCount(RewardKind kind){int i=(int)kind;var d=collectionStore?.ReadCollection();return i>=0&&i<3&&d!=null?d.tools[i]:0;}
        public void CompleteCollectionSettlement(){CollectionSettlementPending=false;}
        public void RefreshCollectionToolStock()
        {
            if(!board)return;
            foreach(var kind in new[]{RewardKind.SwapOrder,RewardKind.ClearBuffer,RewardKind.Shuffle})
            {var tool=board.Find("Tool_"+kind);if(!tool)continue;int stock=CollectionToolCount(kind);var label=tool.Find("ToolStock")?.GetComponent<Text>();if(label){label.text="×"+stock;label.gameObject.SetActive(stock>0);}var plus=tool.Find("ToolPlus");if(plus)plus.gameObject.SetActive(stock==0);}
        }
        V7Art art;
        RawImage edgeImage;
        RectTransform dialogCard;
        string flowSignature;
        double remainingSeconds=600;
        AssetPreparationSnapshot assetPreparation;
        Text entryActionLabel,entryStatus,entrySecondaryLabel;
        Button entryActionButton;
        bool AssetBusy=>assetPreparation!=null&&(assetPreparation.State==AssetReadiness.CheckingCache||assetPreparation.State==AssetReadiness.Downloading||assetPreparation.State==AssetReadiness.Verifying||assetPreparation.State==AssetReadiness.Loading);
        public void SetAssetPreparation(AssetPreparationSnapshot state){assetPreparation=state;RefreshAssetControls();}
        void RefreshAssetControls()
        {
            if(entryActionButton)entryActionButton.interactable=!AssetBusy;
            if(entryActionLabel)entryActionLabel.text=AssetBusy?"正在准备食材…":assetPreparation?.State==AssetReadiness.Failed?"重试":"开始下火锅";
            if(entrySecondaryLabel)entrySecondaryLabel.text=AssetBusy||assetPreparation?.State==AssetReadiness.Failed?"取消":"设置";
            if(entryStatus)entryStatus.text=assetPreparation==null||assetPreparation.State==AssetReadiness.Ready||assetPreparation.State==AssetReadiness.LocalReady||assetPreparation.State==AssetReadiness.Cancelled?"":assetPreparation.State==AssetReadiness.Failed?"食材准备未完成，请重试":assetPreparation.State==AssetReadiness.Downloading?"准备食材 "+(assetPreparation.TotalBytes>0?(100*assetPreparation.DownloadedBytes/assetPreparation.TotalBytes).ToString():"0")+"%":"马上就好…";
        }
        public V7Art VisualArt=>art;
        public IRevivalPresentationPort RevivalPort=>port as IRevivalPresentationPort;
        public bool PresentationForeground=>foreground && canvasRoot && canvasRoot.gameObject.activeInHierarchy && !FriendBoardVisible;
        public Texture2D FoodTexture(int id)=>id>=0&&id<foods.Length?foods[id]:null;

        public void ConfigureEntryAssets(string root)
        {
            if(LastSnapshot==null||LastSnapshot.phase!=ViewPhase.Entry)throw new InvalidOperationException("Entry assets may only be configured at entry");
            ConfigureV7(root,false);
        }
        void ConfigureV7(string root,bool requireComplete=true)
        {
            var errors=TaskAssetValidation.Validate(root,requireComplete);
            if(errors.Length>0)throw new InvalidOperationException(string.Join("; ",errors));
            var nextArt=new V7Art(root);art?.Dispose();art=nextArt;assetRoot=root;
            if(PresentationAssets.Provider.Snapshot.State==AssetReadiness.Ready)
            {
                for(int i=0;i<(root==PresentationAssets.CandidateRoot?foods.Length:16);i++)foods[i]=art.Texture(AssetKey.Food(i));
                plate=art.Texture(AssetKey.Plate);dish=art.Texture(AssetKey.BufferDish);
                potBody=art.Texture(AssetKey.PotBody);potUnlit=art.Texture(AssetKey.PotUnlit);potBroth=art.Texture(AssetKey.PotBroth);potRim=art.Texture(AssetKey.PotRim);
            }
            table=art.Texture(AssetKey.Table);
            playerFont=displayFont=TaskAssetValidation.LoadModernFont();
            backgroundImage.texture=table;backgroundImage.color=UnifiedTheme?Color.white:new Color(.63f,.64f,.63f,1);
            if(!edgeImage){var n=Node(canvasRoot,"EdgeFestivity",new Rect());n.SetSiblingIndex(1);edgeImage=n.gameObject.AddComponent<RawImage>();edgeImage.raycastTarget=false;n.anchorMin=Vector2.zero;n.anchorMax=Vector2.one;n.offsetMin=n.offsetMax=Vector2.zero;}
            edgeImage.texture=art.Texture(AssetKey.EdgeCloth);
            edgeImage.color=Color.clear;
            edgeImage.enabled=false;
            feedback.ResetFeedback();feedback.ConfigureAssets(root,foods);
            ClearChildren(content);board=null;flowSignature=null;plateVisuals.Clear();plateSignatures.Clear();ghostVisuals.Clear();
            Render();
        }

        RectTransform LogicalFrame(Transform parent,string name)
        {
            var frame=Node(parent,name,new Rect(0,0,420,900));
            float scale=Mathf.Min(viewport.width/420,viewport.height/900);
            frame.localScale=Vector3.one*scale;
            frame.anchoredPosition=new Vector2((viewport.width-420*scale)*.5f,-(viewport.height-900*scale)*.5f);
            return frame;
        }
        public void RefreshOrderPresentation(){Render();}
        void RenderV7()
        {
            if(!content)return;
            if(edgeImage)edgeImage.enabled=false;
            backgroundImage.color=UnifiedTheme?Color.white:new Color(.63f,.64f,.63f,1);
            if(viewport.width<=0||viewport.height<=0)viewport=new Rect(0,0,Screen.width,Screen.height);
            Place(content,new Rect(viewport.x,(viewportScreenHeight>0?viewportScreenHeight:Screen.height)-viewport.yMax,viewport.width,viewport.height));
            LayoutThemeEdges();
            if(table){float aspect=canvasRoot.rect.width/canvasRoot.rect.height,source=table.width/(float)table.height;float u=aspect<source?aspect/source:1,v=aspect>source?source/aspect:1;backgroundImage.uvRect=new Rect((1-u)/2,(1-v)/2,u,v);}
            if(LastSnapshot.phase==ViewPhase.Entry)
            {
                var entryFrame=content.Find("Entry") as RectTransform;
                if(flowSignature!="entry"||!entryFrame){ClearChildren(content);board=null;plateVisuals.Clear();plateSignatures.Clear();ghostVisuals.Clear();EntryV7();flowSignature="entry";}
                else{float factor=Mathf.Min(viewport.width/420,viewport.height/900);entryFrame.localScale=Vector3.one*factor;entryFrame.anchoredPosition=new Vector2((viewport.width-420*factor)/2,-(viewport.height-900*factor)/2);}
                LayoutCollectionSidebar();ResizeModal();return;
            }
            if(!board)
            {
                ClearChildren(content);plateVisuals.Clear();plateSignatures.Clear();ghostVisuals.Clear();
                Array.Clear(orderSignatures,0,4);Array.Clear(bufferSignatures,0,5);
                board=Node(content,"GameplayBoard",new Rect(0,0,420,900));
                plateClip=Node(board,"PlateClip",PlateCrop);plateClip.gameObject.AddComponent<RectMask2D>();
                plateLayer=Node(plateClip,"PlateLayer",new Rect(0,-PlateCrop.yMin,420,900));
                hud=Node(board,"FixedHUD",new Rect(0,0,420,64));
                IconButton(hud,"暂停","pause",new Rect(10,6,46,46),()=>Action(ViewAction.Pause));
                timerPlate=Skin(hud,"Timer",new Rect(153,8,114,44),"ui.timer");
                timerLabel=Label(hud,"10:00",new Rect(162,9,96,40),23);
                if(UnifiedTheme)timerLabel.color=ThemeIvory;
                for(int i=0;i<4;i++)orderNodes[i]=Node(board,"Order_"+i,new Rect(6+i*103,72,100,140));
                for(int i=0;i<5;i++)bufferNodes[i]=Node(board,"Buffer_"+i,new Rect(43+i*67,227,64,64));
                PlateCrop=new Rect(0,292,420,536);Place(plateClip,PlateCrop);
                var blocker=Node(board,"BufferInputBlock",new Rect(0,227,420,65)).gameObject.AddComponent<Image>();blocker.color=Color.clear;blocker.raycastTarget=true;
                Skin(board,"BottomBar",new Rect(12,829,396,70),"ui.bottom_bar");
                ToolButton("换单","retry",18,RewardKind.SwapOrder);
                ToolButton("清空暂存","clear_buffer",150,RewardKind.ClearBuffer);
                ToolButton("打乱","shuffle",282,RewardKind.Shuffle);
                feedback.SetBoard(board,plateLayer);
            }
            float scale=Mathf.Min(viewport.width/420,viewport.height/900);
            board.anchoredPosition=new Vector2((viewport.width-420*scale)/2,-(viewport.height-900*scale)/2);board.localScale=Vector3.one*scale;
            LayoutTopTimer();SetRemainingTime(remainingSeconds);
            ResizeModal();
            for(int slot=0;slot<4;slot++)
            {
                orderNodes[slot].gameObject.SetActive(!serving[slot]);
                var order=feedback.DisplayOrder(slot,Array.Find(LastSnapshot.orders,o=>o.slot==slot));
                bool enabled=order!=null&&order.enabled;
                RefreshFireProgress(slot);
                string signature=enabled+":"+order?.foodId+":"+order?.count;
                if(orderSignatures[slot]==signature)continue;
                orderSignatures[slot]=signature;var box=orderNodes[slot];ClearChildren(box);
                Picture(box,enabled?potBody:potUnlit,new Rect(-14,30,128,128),"PotBody",true);
                if(enabled)
                {
                    Picture(box,CurrentBrothSurface(),new Rect(-14,30,128,128),"Broth");
                    Picture(box,potRim,new Rect(-14,30,128,128),"PotRim");
                    if(order.foodId>=0){Skin(box,"OrderTag",new Rect(3,0,94,38),"ui.order_card");Food(box,order.foodId,new Rect(4,0,38,38));Label(box,order.count+"/"+order.required,new Rect(40,0,51,38),20);}
                    else Label(box,"已结清",new Rect(0,0,100,38),16).color=V7Art.Ivory;
                }
                else
                {
                    RewardPlus(box,"PotPlus",new Rect(37,75,26,26));
                    if(slot>=2)LockedPotControls(box,slot);
                }
            }
            for(int slot=0;slot<5;slot++)
            {
                var item=slot<LastSnapshot.buffer.Length?LastSnapshot.buffer[slot]:null;
                string signature=item==null?"empty":item.itemId+":"+item.foodId;
                if(bufferSignatures[slot]==signature)continue;
                bufferSignatures[slot]=signature;var cell=bufferNodes[slot];ClearChildren(cell);bufferFoodNodes[slot]=null;bufferLandingActive[slot]=false;
                Picture(cell,dish,new Rect(0,0,64,64),"Dish");
                if(item!=null)bufferFoodNodes[slot]=Food(cell,item.foodId,new Rect(6,6,52,52));
            }
            var alive=new HashSet<string>();var aliveItems=new HashSet<string>();
            foreach(var body in World.Bodies)
            {
                var p=body.data; var at=World.Position(body);
                alive.Add(p.plateId);RectTransform node;
                if(!plateVisuals.TryGetValue(p.plateId,out node)){node=Node(plateLayer,"PlateVisual_"+p.plateId,new Rect(at.x,at.y,0,0));plateVisuals.Add(p.plateId,node);}
                string signature=p.layoutVersion+":"+string.Join("|",p.items.Select(i=>JsonUtility.ToJson(i)));
                if(!plateSignatures.ContainsKey(p.plateId) || plateSignatures[p.plateId]!=signature)
                {
                    ClearChildren(node);plateSignatures[p.plateId]=signature;
                    float extent=string.IsNullOrEmpty(assetRoot)?p.radius:p.radius*512f/448f;
                    Picture(node,plate,new Rect(-extent,-extent,extent*2,extent*2),"Plate_"+p.plateId);
                    foreach(var item in p.items.OrderBy(i=>i.drawOrder)) PlateFood(node,item);
                }
                foreach(var item in p.items)aliveItems.Add(item.itemId);
            }
            foreach(string id in itemVisuals.Keys.ToArray())if(!aliveItems.Contains(id))itemVisuals.Remove(id);
            foreach(string id in plateVisuals.Keys.ToArray())if(!alive.Contains(id)){plateVisuals[id].gameObject.SetActive(false);Destroy(plateVisuals[id].gameObject);plateVisuals.Remove(id);plateSignatures.Remove(id);}
            foreach(var pair in ghostVisuals.ToArray())if(pair.Key.remaining<=0 || !World.Remnants.Contains(pair.Key)){if(pair.Value)Destroy(pair.Value.gameObject);ghostVisuals.Remove(pair.Key);}
            foreach(var ghost in World.Remnants)
            {
                if(ghostVisuals.ContainsKey(ghost))continue;
                var node=Node(plateLayer,"Clearing_"+ghost.plateId,new Rect(ghost.position.x-ghost.radius,ghost.position.y-ghost.radius,ghost.radius*2,ghost.radius*2));
                var image=node.gameObject.AddComponent<RawImage>(); image.texture=plate; image.raycastTarget=false;
                ghostVisuals.Add(ghost,node);
            }

            RefreshLockedPotDialog();
            feedback.SetBoard(board,plateLayer);hud.SetAsLastSibling();
            string flow=LastSnapshot.sessionId+":"+LastSnapshot.sessionGeneration+":"+LastSnapshot.phase+":"+LastSnapshot.revivalPending+":"+LastSnapshot.revivalUsed+":"+LastSnapshot.pauseReasons;
            if(flow!=flowSignature)
            {
                CloseModal();flowSignature=flow;
                if((LastSnapshot.pauseReasons&ViewPauseReasons.User)!=0)OverlayV7();
                else if(LastSnapshot.revivalPending&&!LastSnapshot.revivalUsed)RevivalOfferV7();
                else if(!LastSnapshot.revivalPending&&LastSnapshot.phase!=ViewPhase.Running&&LastSnapshot.pauseReasons!=ViewPauseReasons.Tutorial)OverlayV7();
            }
        }
        int lockedPotDialogSlot=-1;
        Text lockedPotRemainingLabel;
        int LockedPotCompletedOrders=>Mathf.Max(0,LastSnapshot?.completedOrders??0);
        int LockedPotThreshold(int slot)=>Mathf.Max(1,LastSnapshot==null?1:slot==2?LastSnapshot.thirdPotThreshold:LastSnapshot.fourthPotThreshold);
        bool IsLockedPot(int slot)=>slot>=2&&slot<=3&&LastSnapshot!=null&&!LastSnapshot.orders.Any(o=>o.slot==slot&&o.enabled);
        RectTransform PotCapsule(Transform parent,string name,Rect rect,Color color)
        {
            var node=Node(parent,name,rect);var image=node.gameObject.AddComponent<Image>();
            image.sprite=rounded;image.type=Image.Type.Sliced;image.color=color;image.raycastTarget=false;
            if(rounded)image.pixelsPerUnitMultiplier=Mathf.Max(.01f,rounded.border.x/(rect.height*.5f));
            return node;
        }
        void LockedPotControls(RectTransform box,int slot)
        {
            var hit=Node(box,"LockedPotHit",new Rect(-14,30,128,128));
            var image=hit.gameObject.AddComponent<Image>();image.color=Color.clear;
            var button=hit.gameObject.AddComponent<Button>();button.targetGraphic=image;
            BindButtonClick(button,()=>ShowLockedPotDialog(slot));
            var track=PotCapsule(box,"UnlockProgress",new Rect(10,119,80,12),ThemeInk);
            PotCapsule(track,"TrackInset",new Rect(2,2,76,8),ModernPalette.Muted);
            EnsureFireGradient();
            var fill=Node(track,"Fill",new Rect(2,2,76,8)).gameObject.AddComponent<Image>();
            fill.sprite=fireGradient;fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.fillOrigin=0;fill.raycastTarget=false;
            fireFills[slot]=fill;RefreshFireProgress(slot);
        }
        readonly Image[] fireFills=new Image[4];
        Sprite fireGradient;
        Texture2D fireGradientTexture;
        public static Color FireProgressColor(float progress)
        {
            Color a=new Color32(0x78,0x1C,0x14,255),b=new Color32(0xB5,0x26,0x18,255),c=new Color32(0xE4,0x3B,0x1F,255),d=new Color32(0xFF,0x76,0x26,255);
            float t=Mathf.Clamp01(progress)*3;
            return t<=1?Color.Lerp(a,b,t):t<=2?Color.Lerp(b,c,t-1):Color.Lerp(c,d,t-2);
        }
        void EnsureFireGradient()
        {
            if(fireGradient)return;
            const int width=304,height=32;
            fireGradientTexture=new Texture2D(width,height,TextureFormat.RGBA32,false);fireGradientTexture.wrapMode=TextureWrapMode.Clamp;
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                Color color=FireProgressColor(x/(float)(width-1));
                float dx=Mathf.Max(0,Mathf.Abs(x-(width-1)*.5f)-(width*.5f-8)),dy=Mathf.Max(0,Mathf.Abs(y-(height-1)*.5f)-(height*.5f-8));
                color.a=Mathf.Clamp01(8-Mathf.Sqrt(dx*dx+dy*dy));fireGradientTexture.SetPixel(x,y,color);
            }
            fireGradientTexture.Apply();fireGradient=Sprite.Create(fireGradientTexture,new Rect(0,0,width,height),new Vector2(.5f,.5f));
        }
        void RefreshFireProgress(int slot)
        {
            if(slot>=2&&fireFills[slot])fireFills[slot].fillAmount=Mathf.Clamp01(LockedPotCompletedOrders/(float)LockedPotThreshold(slot));
        }
        Sprite rewardCircle;
        Texture2D rewardCircleTexture;
        void RewardPlus(Transform parent,string name,Rect rect)
        {
            if(!rewardCircle)
            {
                rewardCircleTexture=new Texture2D(64,64,TextureFormat.RGBA32,false);
                for(int y=0;y<64;y++)for(int x=0;x<64;x++)rewardCircleTexture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(31.5f-Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f)))));
                rewardCircleTexture.Apply();rewardCircle=Sprite.Create(rewardCircleTexture,new Rect(0,0,64,64),new Vector2(.5f,.5f));
            }
            var badge=PotCapsule(parent,name,rect,ThemeInk);
            badge.GetComponent<Image>().sprite=rewardCircle;badge.GetComponent<Image>().type=Image.Type.Simple;
            float side=rect.width,stroke=side*.11f,length=side*.48f;
            var horizontal=Node(badge,"PlusHorizontal",new Rect((side-length)/2,(side-stroke)/2,length,stroke)).gameObject.AddComponent<Image>();
            horizontal.color=ThemeIvory;horizontal.raycastTarget=false;
            var vertical=Node(badge,"PlusVertical",new Rect((side-stroke)/2,(side-length)/2,stroke,length)).gameObject.AddComponent<Image>();
            vertical.color=ThemeIvory;vertical.raycastTarget=false;
        }
        void ShowLockedPotDialog(int slot)
        {
            if(!IsLockedPot(slot)||LastSnapshot.phase!=ViewPhase.Running||LastSnapshot.pauseReasons!=ViewPauseReasons.None||modal)return;
            if(art==null)
            {
                ShowDialog("提前开锅","再完成 "+Mathf.Max(0,LockedPotThreshold(slot)-LockedPotCompletedOrders)+" 锅订单即可解锁",new[]{"看视频解锁","关闭"},i=>{CloseModal();if(i==0)RequestLockedPotReward(slot);});
                lockedPotDialogSlot=slot;
                lockedPotRemainingLabel=modal.GetComponentsInChildren<Text>().FirstOrDefault(t=>t.text.StartsWith("再完成 "));
                return;
            }
            var card=ModalV7("LockedPotUnlock",390);
            lockedPotDialogSlot=slot;
            Label(card,"提前开锅",new Rect(35,31,290,48),30);
            IconButton(card,"关闭","close",new Rect(301,14,42,42),CloseModal);
            PictureContain(card,potUnlit,new Rect(124,86,112,112),"LockedPotPreview");
            lockedPotRemainingLabel=Label(card,"",new Rect(22,208,316,38),21);
            VButton(card,"看视频解锁",new Rect(40,292,280,54),()=>{CloseModal();RequestLockedPotReward(slot);},true,22);
            RefreshLockedPotDialog();
        }
        void RequestLockedPotReward(int slot)
        {
            if(IsLockedPot(slot)&&LastSnapshot.phase==ViewPhase.Running&&LastSnapshot.pauseReasons==ViewPauseReasons.None)
                RewardRequested?.Invoke(slot==2?RewardKind.ThirdPot:RewardKind.FourthPot,RewardRoute.SimulatedAd);
        }
        void RefreshLockedPotDialog()
        {
            if(lockedPotDialogSlot<0||!modal)return;
            if(!IsLockedPot(lockedPotDialogSlot)){CloseModal();return;}
            if(lockedPotRemainingLabel)lockedPotRemainingLabel.text="再完成 "+Mathf.Max(0,LockedPotThreshold(lockedPotDialogSlot)-LockedPotCompletedOrders)+" 锅订单即可解锁";
        }
        void ToolButton(string title,string icon,float x,RewardKind kind)
        {
            var n=Node(board,"Tool_"+kind,new Rect(x,831,120,67));
            var hit=n.gameObject.AddComponent<Image>();hit.color=Color.clear;
            var b=n.gameObject.AddComponent<Button>();b.targetGraphic=hit;BindButtonClick(b,()=>ChooseReward(kind));StyleButton(b);
            ModernIcon(n,"ToolIcon",icon,UnifiedTheme?new Rect(44,3,32,32):new Rect(42,5,36,36),ModernPalette.Tea);var caption=Label(n,title,new Rect(0,UnifiedTheme?36:42,120,24),18);if(UnifiedTheme)caption.color=ThemeIvory;
            int stock=CollectionToolCount(kind);
            var stockLabel=Label(n,"×"+stock,new Rect(83,10,35,18),12);stockLabel.name="ToolStock";stockLabel.gameObject.SetActive(stock>0);
            RewardPlus(n,"ToolPlus",new Rect(83,10,18,18));n.Find("ToolPlus").gameObject.SetActive(stock==0);
        }
        void EntryV7()
        {
            var frame=LogicalFrame(content,"Entry");
            if(UnifiedTheme)
            {
                // A composed restaurant sign: raised copper plaque and seal,
                // generous hero and matching counter plinth. Existing action hit boxes stay fixed.
                Skin(frame,"RestaurantSign",new Rect(36,78,348,155),"ui.bottom_bar");
                PictureContain(frame,art.Texture("brand.hotpot_seal"),new Rect(174,38,72,72),"RestaurantSeal");
                var title=Label(frame,"一锅又一锅",new Rect(48,108,324,65),46);title.font=displayFont;title.color=ThemeIvory;
                PictureContain(frame,art.Texture(AssetKey.Entry),new Rect(-12,224,444,444),"EntryHero");
                Skin(frame,"RestaurantCounter",new Rect(52,676,316,169),"ui.bottom_bar");
            }
            else
            {
                var title=Label(frame,"一锅又一锅",new Rect(20,107,380,78),52);title.font=displayFont;title.color=ModernPalette.Paper;
                Label(frame,"每 日 挑 战",new Rect(40,193,340,35),20).color=ModernPalette.Paper;
                Picture(frame,art.Texture(AssetKey.Entry),new Rect(0,208,420,560),"EntryHero");
            }
            VButton(frame,"开始下火锅",new Rect(70,700,280,58),()=>Action(ViewAction.StartToday),true,24);
            entryActionButton=frame.Find("Action_开始下火锅").GetComponent<Button>();entryActionLabel=entryActionButton.GetComponentInChildren<Text>();
            EntryIconButton(frame,"设置","settings",new Rect(70,759,133,70),()=>{if(AssetBusy||assetPreparation?.State==AssetReadiness.Failed)Action(ViewAction.Exit);else SettingsRequested?.Invoke();});
            entrySecondaryLabel=frame.Find("Action_设置").GetComponentInChildren<Text>();
            EntryIconButton(frame,"排行榜","friends",new Rect(217,759,133,70),()=>FriendsRequested?.Invoke());
            if(!UnifiedTheme)Label(frame,"北京时间 06:00 更新",new Rect(30,837,360,26),18).color=V7Art.Ivory;
            entryStatus=Label(frame,"",new Rect(20,866,380,26),17);entryStatus.color=ModernPalette.Paper;RefreshAssetControls();
            if(assetRoot==PresentationAssets.CandidateRoot){AttachCollectionEntry(frame);AttachBrothEntry(frame);}
        }
        RectTransform ModalV7(string name,float height=490)
        {
            CloseModal();
            modal=Node(content,name,new Rect(0,0,viewport.width,viewport.height));
            var shade=modal.gameObject.AddComponent<Image>();shade.color=new Color(.09f,.08f,.065f,.72f);shade.raycastTarget=true;
            var frame=LogicalFrame(modal,"ModalFrame");
            dialogCard=Skin(frame,"IvoryCard",new Rect(30,(900-height)/2,360,height),"ui.panel");dialogCard.GetComponent<Image>().raycastTarget=true;
            return dialogCard;
        }
        void ResizeModal()
        {
            if(!modal)return;float scale=Mathf.Min(viewport.width/420,viewport.height/900);
            Place(modal,new Rect(0,0,viewport.width,viewport.height));var frame=modal.Find("ModalFrame") as RectTransform;
            if(frame){frame.localScale=Vector3.one*scale;frame.anchoredPosition=new Vector2((viewport.width-420*scale)/2,-(viewport.height-900*scale)/2);}
        }
        void OverlayV7()
        {
            if(IsSettlement){SettlementOverlay();return;}
            bool paused=LastSnapshot.phase==ViewPhase.Paused||(LastSnapshot.pauseReasons&ViewPauseReasons.User)!=0,won=LastSnapshot.phase==ViewPhase.Won;
            if(LastSnapshot.revivalPending&&!paused){if(!LastSnapshot.revivalUsed)RevivalOfferV7();return;}
            string title=paused?"歇一会儿":won?"热锅开席！":"失败";
            if(LastSnapshot.phase==ViewPhase.Aborted)title="挑战已中止";
            var card=ModalV7("Flow_"+LastSnapshot.phase,won?744:460);
            var head=Label(card,title,new Rect(25,28,310,55),34);head.font=displayFont;
            if(IsSettlement)BuildSettlementProgress(card,new Rect(40,94,280,58));
            if(won)
            {
                if(UnifiedTheme)PictureContain(card,art.Texture(AssetKey.Win),new Rect(45,158,270,267),"WinHero");
                else Picture(card,art.Texture(AssetKey.Win),new Rect(80,158,200,267),"WinHero");
                int i=0;foreach(var fact in LastSnapshot.facts){Label(card,fact.label+"："+fact.value,new Rect(25,430+i*30,310,29),17);i++;}
                SettlementButtons(()=>{
                    VButton(card,"重新挑战",new Rect(40,601,280,52),()=>Action(ViewAction.RetrySameDay),true);
                    VButton(card,"返回首页",new Rect(40,667,280,46),()=>Action(ViewAction.Exit),false);
                });
            }
            else
            {
                Label(card,paused?"食材等你回来，再开一锅。":LastSnapshot.message=="Timeout"?"这一桌先收好，再来一次吧。":"这一桌满了，下一锅再接再厉。",new Rect(30,IsSettlement?173:99,300,58),18);
                if(paused)VButton(card,"设置",new Rect(40,196,280,46),()=>SettingsRequested?.Invoke(),false);
                SettlementButtons(()=>{
                VButton(card,paused?"继续下锅":"重新挑战",new Rect(40,271,280,52),()=>Action(paused?ViewAction.Resume:ViewAction.RetrySameDay),true);
                VButton(card,"返回首页",new Rect(40,337,280,46),()=>Action(ViewAction.Exit),false);
                });
            }
        }
        void SettlementOverlay()
        {
            CloseModal();
            modal=Node(content,"Flow_"+LastSnapshot.phase,new Rect(0,0,viewport.width,viewport.height));
            var shade=modal.gameObject.AddComponent<Image>();shade.color=new Color(.055f,.035f,.02f,.78f);shade.raycastTarget=true;
            var frame=LogicalFrame(modal,"ModalFrame");
            bool won=LastSnapshot.phase==ViewPhase.Won;
            var title=Label(frame,won?"胜利":"失败",new Rect(42,145,336,80),58);title.font=displayFont;title.color=Cream;
            Label(frame,won?"热锅开席，今日挑战完成！":LastSnapshot.message=="Timeout"?"这一桌先收好，再来一次吧。":"这一桌满了，下一锅再接再厉。",new Rect(40,236,340,54),19).color=Cream;
            BuildSettlementProgress(frame,new Rect(52,330,316,108));
            var stats=Node(frame,"SettlementStatistics",new Rect(64,471,292,155));
            int index=0;
            foreach(var fact in LastSnapshot.facts)
            {
                var label=Label(stats,fact.label,new Rect(0,index*31,180,28),17);label.alignment=TextAnchor.MiddleLeft;label.color=Cream;
                var value=Label(stats,fact.value,new Rect(176,index*31,116,28),18);value.alignment=TextAnchor.MiddleRight;value.color=Cream;
                index++;
            }
            SettlementButtons(()=>{
                if(art!=null)
                {
                    VButton(frame,"重新挑战",new Rect(70,669,280,48),()=>Action(ViewAction.RetrySameDay),true);
                    VButton(frame,"分享",new Rect(70,729,280,46),()=>ShareRequested?.Invoke(),false);
                    VButton(frame,"返回首页",new Rect(70,787,280,46),()=>Action(ViewAction.Exit),false);
                }
                else
                {
                    Button(frame,"重新挑战",new Rect(70,669,280,48),()=>Action(ViewAction.RetrySameDay));
                    Button(frame,"分享",new Rect(70,729,280,46),()=>ShareRequested?.Invoke());
                    Button(frame,"返回首页",new Rect(70,787,280,46),()=>Action(ViewAction.Exit));
                }
            });
        }
        void RevivalOfferV7(RewardApplicationResult? lastResult=null)
        {
            var offer=RevivalPort?.ReadRevivalOffer();
            var card=ModalV7("RevivalOffer",490);
            var title=Label(card,"暂存已满",new Rect(25,29,310,53),34);title.font=displayFont;
            Label(card,"把暂存食材重新装盘\n回到供给队尾，继续这一局",new Rect(25,100,310,70),19);
            Label(card,"每局仅可复活一次",new Rect(25,185,310,30),16).color=V7Art.Muted;
            bool share=offer!=null&&offer.route==RewardRoute.SimulatedShare;
            VButton(card,share?"分享复活":"看广告复活",new Rect(40,265,280,54),async()=>
            {
                if(RevivalPort==null)return;
                var result=await RevivalPort.RequestRevivalAsync();
                if(this&&LastSnapshot.revivalPending&&!LastSnapshot.revivalUsed)RevivalOfferV7(result);
            },true,22);
            string quota=share&&offer.shareAvailability!=null?"今日分享剩余 "+offer.shareAvailability.Remaining+" 次":"";
            if(!share&&offer!=null&&!offer.rewardedVideoAvailable)quota="广告暂不可用：未配置或未启用\n没有发放奖励";
            else if(lastResult.HasValue&&lastResult.Value!=RewardApplicationResult.Applied)quota="未领取奖励，可重试\n没有扣除分享次数";
            Label(card,quota,new Rect(20,337,320,45),18).color=ModernPalette.Muted;
            VButton(card,"结束本局",new Rect(40,400,280,44),()=>RevivalPort?.DeclineRevival(),false,18);
        }
        void ChooseRewardV7(RewardKind kind)
        {
            if(CollectionToolCount(kind)>0){RewardRequested?.Invoke(kind,RewardRoute.SimulatedAd);return;}
            var offer=RevivalPort?.ReadRevivalOffer();
            var availability=offer?.shareAvailability;
            var route=availability!=null&&availability.Available?RewardRoute.SimulatedShare:RewardRoute.SimulatedAd;
            if(route==RewardRoute.SimulatedAd&&offer!=null&&!offer.rewardedVideoAvailable)
            {DialogV7("广告暂不可用","分享暂不可用，激励视频未配置或未启用。\n没有发放奖励，也不会扣除分享次数。",new[]{"知道了"},_=>CloseModal());return;}
            string item=kind==RewardKind.SwapOrder?"换单":kind==RewardKind.ClearBuffer?"清空暂存":"打乱";
            string note=route==RewardRoute.SimulatedShare?"今日分享剩余 "+availability.Remaining+" 次":"分享暂不可用，可通过广告领取。";
            ToolRewardOfferV7(kind,"领取"+item,note,route==RewardRoute.SimulatedShare?"分享领取":"看广告领取",route);
        }
        Task<RewardOutcome> RewardSimulationV7(RewardRequest request)
        {
            simulation?.TrySetResult(RewardOutcome.Cancelled);
            simulation=new TaskCompletionSource<RewardOutcome>();var completion=simulation;
            bool share=request.Route==RewardRoute.SimulatedShare;
            var card=ModalV7(share?"RewardShareSimulation":"RewardAdSimulation",520);
            Label(card,share?"奖励分享":"激励视频",new Rect(25,26,310,49),29);
            Label(card,"Unity 开发模拟\n不会发送分享，也不会播放真实广告",new Rect(20,79,320,59),18).color=V7Art.Muted;
            Label(card,"选择本次模拟结果",new Rect(30,163,300,35),18).color=ModernPalette.Muted;
            string[] names={"模拟成功","取消","模拟失败"};
            for(int i=0;i<3;i++){int index=i;VButton(card,names[i],new Rect(40,251+i*68,280,50),()=>{CloseModal();if(simulation==completion)simulation=null;completion.TrySetResult(index==0?RewardOutcome.Success:index==1?RewardOutcome.Cancelled:RewardOutcome.Failed);},i==0,20);}
            return completion.Task;
        }
        void DialogV7(string title,string message,string[] buttons,Action<int> select)
        {
            bool friend=title.Contains("好友");
            var card=ModalV7(friend?"Friends":"Dialog",500);
            Label(card,friend?"好友榜":title,new Rect(25,27,310,53),29);
            if(friend)
            {
                ModernIcon(card,"FriendsEmpty","friends",new Rect(142,102,76,76),ModernPalette.Tea);
                Label(card,string.IsNullOrWhiteSpace(message)?"还没有本地成绩":message,new Rect(25,194,310,125),18);
                if(title.Contains("开发模拟"))Label(card,"Unity 开发模拟\n未连接微信好友数据",new Rect(15,326,330,49),18).color=ModernPalette.Muted;
            }
            else Label(card,message,new Rect(28,104,304,130),18);
            for(int i=0;i<buttons.Length;i++){int index=i;VButton(card,buttons[i],new Rect(40,500-31-(buttons.Length-i)*60,280,48),()=>select(index),i==0,20);}
        }
        void SettingsV7(PlayerSettings settings,Action<PlayerSettings> save)
        {
            var card=ModalV7("Settings",570);
            Audio?.SetSettingsOpen(true);
            Label(card,"设置",new Rect(25,28,310,50),32);
            SettingRow(card,settings.MusicEnabled?"music_on":"music_off","音乐",settings.MusicEnabled,112,()=>{settings.MusicEnabled=!settings.MusicEnabled;save(settings);SettingsV7(settings,save);});
            Volume(card,new Rect(55,199,250,12),settings.MusicVolume,v=>{settings.MusicVolume=v;save(settings);});
            SettingRow(card,settings.EffectsEnabled?"effects_on":"effects_off","音效",settings.EffectsEnabled,261,()=>{settings.EffectsEnabled=!settings.EffectsEnabled;save(settings);SettingsV7(settings,save);});
            Volume(card,new Rect(55,348,250,12),settings.EffectsVolume,v=>{settings.EffectsVolume=v;save(settings);});
            Label(card,"音乐与音效独立调节\n设置自动保存",new Rect(25,393,310,59),18).color=ModernPalette.Muted;
            VButton(card,"完成",new Rect(40,478,280,50),()=>{save(settings);CloseModal();if(LastSnapshot.phase==ViewPhase.Paused)OverlayV7();},true);
        }
        void SettingRow(Transform parent,string icon,string text,bool on,float y,UnityEngine.Events.UnityAction action)
        {
            ModernIcon(parent,icon,icon,new Rect(43,y+4,44,44),ModernPalette.Tea);
            Label(parent,text,new Rect(101,y,100,52),21);
            VButton(parent,on?"开":"关",new Rect(223,y+4,93,44),action,on,20);
        }
        void VolumeV7(Transform parent,Rect rect,float value,Action<float> changed)
        {
            var node=Node(parent,"Volume",new Rect(rect.x,rect.y-13,rect.width,38));
            var hit=node.gameObject.AddComponent<Image>();hit.color=Color.clear;
            Skin(node,"VolumeTrack",new Rect(0,14,rect.width,10),"ui.progress_track");
            var area=Node(node,"HandleArea",new Rect(12,6,rect.width-24,26));
            var handle=Skin(area,"Handle",new Rect(0,0,26,26),"ui.icon_disc");handle.pivot=new Vector2(.5f,.5f);
            handle.GetComponent<Image>().color=UnifiedTheme?Color.white:ModernPalette.Tea;
            handle.anchorMin=new Vector2(0,0);handle.anchorMax=new Vector2(0,1);handle.sizeDelta=new Vector2(26,0);handle.anchoredPosition=Vector2.zero;
            var slider=node.gameObject.AddComponent<Slider>();slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();slider.direction=Slider.Direction.LeftToRight;slider.minValue=0;slider.maxValue=1;slider.value=value;slider.onValueChanged.AddListener(v=>changed(v));
        }
        RectTransform Skin(Transform parent,string name,Rect rect,string key)
        {
            return ModernSkin(parent,name,rect,key);
        }
        void VButton(Transform parent,string text,Rect rect,UnityEngine.Events.UnityAction action,bool primary=true,int size=21)
        {
            var n=Skin(parent,"Action_"+text,rect,primary?"ui.button_primary":"ui.button_secondary");var image=n.GetComponent<Image>();image.raycastTarget=true;
            var b=n.gameObject.AddComponent<Button>();b.targetGraphic=image;BindButtonClick(b,action,text=="开始下火锅");StyleButton(b);
            Label(n,text,new Rect(8,0,rect.width-16,rect.height),size).color=UnifiedTheme?ThemeIvory:primary?ModernPalette.Paper:ModernPalette.Ink;
        }
        void EntryIconButton(Transform parent,string title,string icon,Rect rect,UnityEngine.Events.UnityAction action)
        {
            var n=Node(parent,"Action_"+title,rect);
            var hit=n.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            var disc=Skin(n,"IconDisc",new Rect((rect.width-52)/2,0,52,52),"ui.icon_disc");
            var button=n.gameObject.AddComponent<Button>();button.targetGraphic=disc.GetComponent<Image>();
            BindButtonClick(button,action);StyleButton(button);
            ModernIcon(disc,"Icon",icon,new Rect(10,10,32,32),ModernPalette.Coral);
            Label(n,title,new Rect(0,48,rect.width,22),18).color=UnifiedTheme?ThemeIvory:ModernPalette.Paper;
        }
        void IconButton(Transform parent,string title,string icon,Rect rect,UnityEngine.Events.UnityAction action)
        {
            var n=Skin(parent,title,rect,"ui.icon_disc");var image=n.GetComponent<Image>();image.raycastTarget=true;
            var button=n.gameObject.AddComponent<Button>();BindButtonClick(button,action);StyleButton(button);ModernIcon(n,icon,icon,new Rect(6,6,rect.width-12,rect.height-12),ModernPalette.Coral);
        }
    }
}
