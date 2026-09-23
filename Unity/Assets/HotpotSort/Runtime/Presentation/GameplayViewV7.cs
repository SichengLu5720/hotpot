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
                for(int i=0;i<16;i++)foods[i]=art.Texture(AssetKey.Food(i));
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
                ResizeModal();return;
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
                ToolButton("提示","hint",18,RewardKind.Hint);
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
                var order=Array.Find(LastSnapshot.orders,o=>o.slot==slot);
                bool enabled=order!=null&&order.enabled;
                string signature=enabled+":"+order?.foodId+":"+order?.count;
                if(orderSignatures[slot]==signature)continue;
                orderSignatures[slot]=signature;var box=orderNodes[slot];ClearChildren(box);
                Picture(box,enabled?potBody:potUnlit,new Rect(-14,30,128,128),"PotBody",true);
                if(enabled)
                {
                    Picture(box,potBroth,new Rect(-14,30,128,128),"Broth");
                    Picture(box,potRim,new Rect(-14,30,128,128),"PotRim");
                    if(order.foodId>=0){Skin(box,"OrderTag",new Rect(3,0,94,38),"ui.order_card");Food(box,order.foodId,new Rect(4,0,38,38));Label(box,order.count+"/"+order.required,new Rect(40,0,51,38),20);}
                    else Label(box,"已结清",new Rect(0,0,100,38),16).color=V7Art.Ivory;
                }
                else
                {
                    ModernIcon(box,"Locked","lock",new Rect(31,69,38,38),ModernPalette.Paper);
                    if(slot==3)VButton(box,"提前开锅",new Rect(4,113,92,32),()=>RewardRequested?.Invoke(RewardKind.FourthPot,RewardRoute.SimulatedAd),false,18);
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

            feedback.SetBoard(board,plateLayer);hud.SetAsLastSibling();
            string flow=LastSnapshot.sessionId+":"+LastSnapshot.sessionGeneration+":"+LastSnapshot.phase+":"+LastSnapshot.revivalPending+":"+LastSnapshot.revivalUsed+":"+LastSnapshot.pauseReasons;
            if(flow!=flowSignature)
            {
                CloseModal();flowSignature=flow;
                if((LastSnapshot.pauseReasons&ViewPauseReasons.User)!=0)OverlayV7();
                else if(LastSnapshot.revivalPending&&!LastSnapshot.revivalUsed)RevivalOfferV7();
                else if(!LastSnapshot.revivalPending&&LastSnapshot.phase!=ViewPhase.Running)OverlayV7();
            }
        }
        void ToolButton(string title,string icon,float x,RewardKind kind)
        {
            var n=Node(board,"Tool_"+kind,new Rect(x,831,120,67));
            var hit=n.gameObject.AddComponent<Image>();hit.color=Color.clear;
            var b=n.gameObject.AddComponent<Button>();b.targetGraphic=hit;b.onClick.AddListener(()=>ChooseReward(kind));StyleButton(b);
            ModernIcon(n,"ToolIcon",icon,UnifiedTheme?new Rect(44,3,32,32):new Rect(42,5,36,36),ModernPalette.Tea);var caption=Label(n,title,new Rect(0,UnifiedTheme?36:42,120,24),18);if(UnifiedTheme)caption.color=ThemeIvory;
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
                var title=Label(frame,"火锅消消",new Rect(48,108,324,65),46);title.font=displayFont;title.color=ThemeIvory;
                PictureContain(frame,art.Texture(AssetKey.Entry),new Rect(-12,224,444,444),"EntryHero");
                Skin(frame,"RestaurantCounter",new Rect(52,676,316,153),"ui.bottom_bar");
            }
            else
            {
                var title=Label(frame,"火锅消消",new Rect(20,107,380,78),52);title.font=displayFont;title.color=ModernPalette.Paper;
                Label(frame,"每 日 挑 战",new Rect(40,193,340,35),20).color=ModernPalette.Paper;
                Picture(frame,art.Texture(AssetKey.Entry),new Rect(0,208,420,560),"EntryHero");
            }
            VButton(frame,"开始下火锅",new Rect(70,700,280,58),()=>Action(ViewAction.StartToday),true,24);
            entryActionButton=frame.Find("Action_开始下火锅").GetComponent<Button>();entryActionLabel=entryActionButton.GetComponentInChildren<Text>();
            VButton(frame,"设置",new Rect(70,772,133,43),()=>{if(AssetBusy||assetPreparation?.State==AssetReadiness.Failed)Action(ViewAction.Exit);else SettingsRequested?.Invoke();},false,18);
            entrySecondaryLabel=frame.Find("Action_设置").GetComponentInChildren<Text>();
            VButton(frame,"好友榜",new Rect(217,772,133,43),()=>FriendsRequested?.Invoke(),false,18);
            if(!UnifiedTheme)Label(frame,"北京时间 06:00 更新",new Rect(30,837,360,26),18).color=V7Art.Ivory;
            entryStatus=Label(frame,"",new Rect(20,866,380,26),17);entryStatus.color=ModernPalette.Paper;RefreshAssetControls();
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
            bool paused=LastSnapshot.phase==ViewPhase.Paused||(LastSnapshot.pauseReasons&ViewPauseReasons.User)!=0,won=LastSnapshot.phase==ViewPhase.Won;
            if(LastSnapshot.revivalPending&&!paused){if(!LastSnapshot.revivalUsed)RevivalOfferV7();return;}
            string title=paused?"歇一会儿":won?"热锅开席！":LastSnapshot.message=="Timeout"?"时间到":"暂存已满";
            if(LastSnapshot.phase==ViewPhase.Aborted)title="挑战已中止";
            var card=ModalV7("Flow_"+LastSnapshot.phase,won?620:460);
            var head=Label(card,title,new Rect(25,28,310,55),34);head.font=displayFont;
            if(won)
            {
                if(UnifiedTheme)PictureContain(card,art.Texture(AssetKey.Win),new Rect(45,78,270,267),"WinHero");
                else Picture(card,art.Texture(AssetKey.Win),new Rect(80,78,200,267),"WinHero");
                int i=0;foreach(var fact in LastSnapshot.facts){Label(card,fact.label+"："+fact.value,new Rect(25,350+i*30,310,29),17);i++;}
                VButton(card,"重新挑战",new Rect(40,477,280,52),()=>Action(ViewAction.RetrySameDay),true);
                VButton(card,"返回首页",new Rect(40,543,280,46),()=>Action(ViewAction.Exit),false);
            }
            else
            {
                Label(card,paused?"食材等你回来，再开一锅。":LastSnapshot.message=="Timeout"?"这一桌先收好，再来一次吧。":"这一桌满了，下一锅再接再厉。",new Rect(30,99,300,58),18);
                if(paused)VButton(card,"设置",new Rect(40,196,280,46),()=>SettingsRequested?.Invoke(),false);
                VButton(card,paused?"继续下锅":"重新挑战",new Rect(40,271,280,52),()=>Action(paused?ViewAction.Resume:ViewAction.RetrySameDay),true);
                VButton(card,"返回首页",new Rect(40,337,280,46),()=>Action(ViewAction.Exit),false);
            }
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
            var offer=RevivalPort?.ReadRevivalOffer();
            var availability=offer?.shareAvailability;
            var route=availability!=null&&availability.Available?RewardRoute.SimulatedShare:RewardRoute.SimulatedAd;
            if(route==RewardRoute.SimulatedAd&&offer!=null&&!offer.rewardedVideoAvailable)
            {DialogV7("广告暂不可用","分享暂不可用，激励视频未配置或未启用。\n没有发放奖励，也不会扣除分享次数。",new[]{"知道了"},_=>CloseModal());return;}
            string item=kind==RewardKind.Hint?"提示":kind==RewardKind.ClearBuffer?"清空暂存":"打乱";
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
            Label(card,"设置",new Rect(25,28,310,50),32);
            SettingRow(card,settings.MusicEnabled?"music_on":"music_off","音乐",settings.MusicEnabled,112,()=>{settings.MusicEnabled=!settings.MusicEnabled;save(settings);SettingsV7(settings,save);});
            Volume(card,new Rect(55,199,250,12),settings.MusicVolume,v=>{settings.MusicVolume=v;save(settings);});
            SettingRow(card,settings.EffectsEnabled?"effects_on":"effects_off","音效",settings.EffectsEnabled,261,()=>{settings.EffectsEnabled=!settings.EffectsEnabled;save(settings);SettingsV7(settings,save);});
            Volume(card,new Rect(55,348,250,12),settings.EffectsVolume,v=>{settings.EffectsVolume=v;save(settings);});
            Label(card,"声音暂未开放\n设置已为你保留",new Rect(25,393,310,59),18).color=ModernPalette.Muted;
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
            var b=n.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(()=>{feedback.Button();action();});StyleButton(b);
            Label(n,text,new Rect(8,0,rect.width-16,rect.height),size).color=UnifiedTheme?ThemeIvory:primary?ModernPalette.Paper:ModernPalette.Ink;
        }
        void IconButton(Transform parent,string title,string icon,Rect rect,UnityEngine.Events.UnityAction action)
        {
            var n=Skin(parent,title,rect,"ui.icon_disc");var image=n.GetComponent<Image>();image.raycastTarget=true;
            var button=n.gameObject.AddComponent<Button>();button.onClick.AddListener(action);StyleButton(button);ModernIcon(n,icon,icon,new Rect(6,6,rect.width-12,rect.height-12),ModernPalette.Coral);
        }
    }
}
