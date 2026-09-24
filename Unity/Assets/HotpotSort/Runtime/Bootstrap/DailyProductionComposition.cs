using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Presentation;
using HotpotSort.Session;
using HotpotSort.Profile;
using UnityEngine;

namespace HotpotSort.Bootstrap
{
    public sealed class DailyProductionComposition : ProductionComposition, IGameSessionFactory, IGameViewFactory, IGameView, IPresentationPort, IRevivalPresentationPort
    {
        [SerializeField] private TextAsset dailyContent;
        [SerializeField] private Font playerFont;
        [SerializeField] private string approvedAssetRoot = "";
        public string ApprovedAssetRoot=>approvedAssetRoot;
        private DailySessionFactory factory;
        private DailySession current;
        private SessionController controller;
        private GameplayView view;
        private ISessionActions actions;
        private Func<Task> assetStart;
        private Action assetCancel;
        public void ConfigureAssetGate(Func<Task> start,Action cancel){assetStart=start;assetCancel=cancel;}
        public void ActivateCompleteAssets(){view.ConfigureAssets(approvedAssetRoot);plateShapes.Clear();}
        public void ReleaseAssetConsumers(){plateShapes.Clear();plateLayouts=null;view?.ReleaseAssetReferences();}
        private IDiagnosticSink diagnostics;
        private bool showingError;
        private ulong supplySequence;
        private readonly ActiveSupplySchedule supplySchedule=new ActiveSupplySchedule();
        private bool observingSupply;
        private long lastSupplyObservation;
        private readonly Dictionary<string,ViewPlateMotion> spawnMotions=new Dictionary<string,ViewPlateMotion>();
        private System.Random spawnRandom=new System.Random(601377);
        private int successfulSpawns;
        private PlateLayoutCache plateLayouts;
        private readonly Dictionary<int,PlateFoodShape> plateShapes=new Dictionary<int,PlateFoodShape>();
        private Viewport appliedViewport;
        private ViewSnapshot shown=new ViewSnapshot { phase=ViewPhase.Entry };
        private IProfileStore profile;
        private IRewardService rewardService;
        private IFriendBoard friends;
        private IThemeShare sharing;
        private RewardCoordinator rewards;
        private string winRecordedSession;
        private RewardRequest pendingRevivalRequest;
        private string revivalRouteOffer;
        private RewardRoute revivalSelection;
        private IWeChatFriendBoardSurface friendSurface;
        private string friendOwnerMarker;
        private bool friendSurfaceOpen;
        private bool friendAddedUserPause;
        private ViewPauseReasons observedPauses;
        private double platformPixelRatio=1;
        public void SetPlatformPixelRatio(double ratio){platformPixelRatio=Math.Max(.5,Math.Min(8,ratio));}
        public IWeChatFriendBoardSurface FriendSurface=>friendSurface;
        public bool FriendSurfaceOpen=>friendSurfaceOpen;
        // Presentation-neutral main-domain surface. No friend records are exposed.
        public Texture FriendSurfaceTexture=>(friendSurface as HotpotSort.Platform.WeChatFriendBoardSurface)?.SharedTexture;
        public Rect FriendSurfaceTextureUv=>HotpotSort.Platform.WeChatFriendBoardSurface.SharedTextureUv;
        public event Action<bool> FriendSurfaceChanged;
        private FriendBoardViewport FriendViewport(Rect pixels)=>new FriendBoardViewport((int)pixels.x,(int)pixels.y,Math.Max(1,(int)pixels.width),Math.Max(1,(int)pixels.height),platformPixelRatio);
        private void OnFriendPresentationChanged(bool open)
        {
            if(!view)return;
            if(open)view.ShowFriendBoard(()=>FriendSurfaceTexture,CloseFriendSurface,r=>UpdateFriendViewport(FriendViewport(r)),SetFriendModalPause);
            else view.HideFriendBoard();
        }
        private void SetFriendModalPause(bool value)
        {
            if(value)
            {
                friendAddedUserPause=current!=null&&controller!=null&&(controller.Pauses&PauseReasons.User)==0;
                if(friendAddedUserPause)actions?.Request(Contracts.SessionAction.Pause);
            }
            else if(friendAddedUserPause){friendAddedUserPause=false;actions?.Request(Contracts.SessionAction.Resume);}
        }
        public ProfileDocument ReadProfileSnapshot()=>(profile as IAsyncProfileStore)?.ReadSnapshot();
        public Task<ProfileSyncStatus> SyncProfileAsync()=>profile is IAsyncProfileStore sync?sync.SyncAsync():Task.FromResult(ProfileSyncStatus.NotConfigured);
        public override ITimeProvider TrustedTime=>profile is IAsyncProfileStore sync?new ProfileTrustedTimeProvider(sync):null;
        public event Action<ViewUpdate> Updated;
        public override string ContentVersion { get { EnsureFactory(); return factory.Content.ContentVersion; } }
        public override string ConfigurationDigest { get { EnsureFactory(); return factory.ConfigurationDigest; } }
        public override string BuildIdentity => "task001-v7-revival-development";
        public override string RuntimeNamespace => "hotpot-task001-v3-development";
        public override IGameSessionFactory CoreFactory { get { EnsureFactory(); return this; } }
        public override IGameViewFactory ViewFactory => this;
        public override bool UsesDevelopmentDoubles => false;
        public GameplayView PlayerView => view;
        public DailySession ActiveCore => current;
        public DailyContent ProductionContent { get { EnsureFactory(); return factory.Content; } }
        private void EnsureFactory()
        {
            if(factory!=null)return;
            if(!dailyContent)throw new InvalidOperationException("Fixed C daily content scene binding is missing");
            factory=DailySessionFactory.FromProductionJson(dailyContent.text);
        }
        public void ConfigureServices(IProfileStore store,IRewardService reward,IFriendBoard board,IThemeShare share,Func<DateTimeOffset> utcNow=null)
        {
            rewards?.InvalidateSession();
            if(profile is IAsyncProfileStore previous){previous.ProfileChanged-=OnProfileChanged;previous.InvalidateSyncCallbacks();}
            profile=store;rewardService=reward;friends=board;sharing=share;rewards=new RewardCoordinator(reward,store,utcNow);
            if(store is IAsyncProfileStore sync){sync.ProfileChanged+=OnProfileChanged;_=SyncProfileAsync();}
        }
        public void ConfigureFriendSurface(IWeChatFriendBoardSurface surface,string ownerMarker)
        {CloseFriendSurface();friendSurface=surface;friendOwnerMarker=ownerMarker;PublishFriendScore();}
        private void OnProfileChanged(){PublishFriendScore();if(friendSurfaceOpen)RefreshFriendSurface();}
        private void PublishFriendScore()
        {
            if(friendSurface==null||profile==null||string.IsNullOrEmpty(friendOwnerMarker))return;
            try{friendSurface.PublishOwnScore(profile.TotalFirstWins,friendOwnerMarker,(profile as IAsyncProfileStore)?.TimeSnapshot.Utc??DateTimeOffset.UtcNow);}
            catch(Exception ex){Debug.LogWarning("Friend score publish unavailable: "+ex.GetType().Name);}
        }
        // Visual owner supplies the approved top-left physical-pixel display rectangle.
        // No friend rows, aliases or avatars ever enter this composition.
        public bool OpenFriendSurface(FriendBoardViewport viewport)
        {
            if(friendSurface==null)return false;
            try{friendSurface.Open(viewport);friendSurfaceOpen=true;FriendSurfaceChanged?.Invoke(true);return true;}
            catch(Exception ex){Debug.LogWarning("Friend surface unavailable: "+ex.GetType().Name);CloseFriendSurface();return false;}
        }
        public void RefreshFriendSurface(){if(friendSurfaceOpen)try{friendSurface.Refresh();}catch(Exception ex){Debug.LogWarning("Friend refresh unavailable: "+ex.GetType().Name);}}
        public void UpdateFriendViewport(FriendBoardViewport viewport){if(friendSurfaceOpen)friendSurface.UpdateViewport(viewport);}
        public void CloseFriendSurface(){try{if(friendSurfaceOpen)friendSurface?.Close();}catch(Exception ex){Debug.LogWarning("Friend close unavailable: "+ex.GetType().Name);}finally{bool wasOpen=friendSurfaceOpen;friendSurfaceOpen=false;if(wasOpen)FriendSurfaceChanged?.Invoke(false);}}
        public override void BindDiagnostics(IDiagnosticSink sink) { diagnostics=sink; }
        public Task<DiagnosticResult> SaveReplayAsync()
        {
            if(diagnostics==null || current==null)return Task.FromResult(new DiagnosticResult(false,null,"No active replay"));
            return diagnostics.SaveAsync(current.ExportReplay());
        }
        public override void BindSessionObservation(SessionController value)
        {
            controller=value; actions=value;
            var node=new GameObject("DailyPlayerView"); node.transform.SetParent(transform,false);
            view=node.AddComponent<GameplayView>();
            if(playerFont)view.playerFont=playerFont;
            view.ConfigureEntryAssets(approvedAssetRoot);
            if(profile==null)
            {
                var local=new LocalDevelopmentServices();
                local.RewardPrompt=view.ShowRewardSimulationAsync;
                local.SharePrompt=view.ShowThemeShareAsync;
                ConfigureServices(local,local,local,local);
            }
            view.RewardRequested+=RequestReward;
            view.SettingsRequested+=ShowSettings;
            view.FriendsRequested+=ShowFriends;
            FriendSurfaceChanged+=OnFriendPresentationChanged;
            view.SetAudioSettings(profile.LoadSettings());
            view.Bind(this);
#if UNITY_WEBGL && !UNITY_EDITOR
            node.AddComponent<WeChatGameplayTapInput>();
#endif
            controller.ObservationChanged+=OnSessionObservation;
            OnSessionObservation();
        }
        private void OnSessionObservation()
        {
            if(controller.CurrentViewport!=null && !ReferenceEquals(appliedViewport,controller.CurrentViewport))SetViewport(controller.CurrentViewport);
            if(!string.IsNullOrEmpty(controller.Error))ShowError("session-error",controller.Error);
            var pauses=(ViewPauseReasons)(int)controller.Pauses;
            if((observedPauses&ViewPauseReasons.Background)!=0&&(pauses&ViewPauseReasons.Background)==0)
            {_=SyncProfileAsync();PublishFriendScore();RefreshFriendSurface();}
            observedPauses=pauses;
            ValidatePendingReward();
        }
        private void OnDestroy() { rewards?.InvalidateSession();CloseFriendSurface();FriendSurfaceChanged-=OnFriendPresentationChanged;if(profile is IAsyncProfileStore sync){sync.ProfileChanged-=OnProfileChanged;sync.InvalidateSyncCallbacks();}if(controller!=null)controller.ObservationChanged-=OnSessionObservation; }
        private void OnApplicationFocus(bool focused){if(focused){_=SyncProfileAsync();PublishFriendScore();RefreshFriendSurface();}}
        public IGameSession CreateSession(ChallengeContext context)
        {
            EnsureFactory(); showingError=false; supplySequence=0; supplySchedule.Reset();lastSupplyObservation=0; ResetSpawnPresentation();
            (profile as IAsyncProfileStore)?.InvalidateSyncCallbacks();revivalRouteOffer=null;
            current=factory.CreateDailySession(context); return current;
        }
        public IGameView CreateView() { return this; }
        void IGameView.Bind(ISessionActions value) { actions=value; }
        public void SetViewport(Viewport value)
        {
            appliedViewport=value;
            var menu=value.HasMenuButton?new Rect(value.MenuButtonX,value.MenuButtonY,value.MenuButtonWidth,value.MenuButtonHeight):new Rect();
            view.SetViewport(new Rect(value.SafeX,value.SafeY,value.SafeWidth,value.SafeHeight),value.Height,menu);
        }
        public void Show(GameSnapshot snapshot,GameEventBatch events)
        {
            showingError=false;
            var update=DailyViewMapper.Map(snapshot,events,factory.Content,controller.ChallengeSeconds,controller.Generation,(ViewPauseReasons)(int)controller.Pauses,plateLayouts);
            foreach(var plate in update.snapshot.plates)
            {
                ViewPlateMotion motion;
                if(!spawnMotions.TryGetValue(plate.plateId,out motion))
                {
                    motion=new ViewPlateMotion { hasSpawnPosition=true,spawnX=DailyViewMapper.SpawnX(successfulSpawns)+(float)(spawnRandom.NextDouble()*100-50),spawnY=plate.y };
                    spawnMotions.Add(plate.plateId,motion);successfulSpawns++;
                }
                plate.motion=motion;plate.x=motion.spawnX;plate.y=motion.spawnY;
            }
            if(snapshot.Status==GameStatus.Won && winRecordedSession!=snapshot.SessionId)
            {profile.RecordFirstWin(snapshot.Challenge.ChallengeId);winRecordedSession=snapshot.SessionId;}
            shown=update.snapshot; Updated?.Invoke(update);
            ValidatePendingReward();
        }
        public void ShowLoading() { showingError=false; }
        public void ShowError(string code,string message) { showingError=true; view.ShowError(message); }
        public void ResetSession()
        {
            rewards?.InvalidateSession();pendingRevivalRequest=null;
            CloseFriendSurface();revivalRouteOffer=null;
            (profile as IAsyncProfileStore)?.InvalidateSyncCallbacks();
            current=null; supplySequence=0; supplySchedule.Reset();lastSupplyObservation=0;observingSupply=false;winRecordedSession=null;ResetSpawnPresentation();
            if(!showingError) { shown=new ViewSnapshot { phase=ViewPhase.Entry }; view.ResetView(); }
        }
        void IDisposable.Dispose() { /* View persists as the approved entry after session Exit. Bootstrap owns its lifetime. */ }
        public ViewSnapshot Read() { return shown; }
        public void SessionAction(ViewAction action)
        {
            if(action==ViewAction.Exit||action==ViewAction.RetrySameDay||action==ViewAction.Resume)CloseFriendSurface();
            switch(action)
            {
                case ViewAction.StartToday: if(assetStart!=null)_=assetStart();break;
                case ViewAction.Pause: actions?.Request(Contracts.SessionAction.Pause);break;
                case ViewAction.Resume: actions?.Request(Contracts.SessionAction.Resume);break;
                case ViewAction.RetrySameDay: if(controller.Resolved==null){if(assetStart!=null)_=assetStart();}else actions?.Request(Contracts.SessionAction.Retry);break;
                case ViewAction.Exit: assetCancel?.Invoke();showingError=false; actions?.Request(Contracts.SessionAction.Exit); shown=new ViewSnapshot { phase=ViewPhase.Entry }; view.ResetView();break;
            }
        }
        private ulong Boundary => (ulong)Math.Max(0,Math.Floor(controller.ChallengeSeconds*1000));
        private void ResetSpawnPresentation(){spawnMotions.Clear();successfulSpawns=0;spawnRandom=new System.Random(601377);plateLayouts=new PlateLayoutCache(LoadPlateShape);}
        private PlateFoodShape LoadPlateShape(int foodId)
        {
            if(plateShapes.TryGetValue(foodId,out var shape))return shape;
            if(string.IsNullOrEmpty(approvedAssetRoot))throw new InvalidOperationException("Plate layout requires approved readable food assets");
            var texture=view.VisualArt.Texture(AssetKey.Food(foodId));
            if(!texture||!texture.isReadable)throw new InvalidOperationException("Plate alpha geometry missing or unreadable: "+foodId);
            var uv=view.VisualArt.FoodUv(foodId);
            const int resolution=128;var alpha=new byte[resolution*resolution];
            for(int y=0;y<resolution;y++)for(int x=0;x<resolution;x++)
                alpha[y*resolution+x]=(byte)Mathf.RoundToInt(texture.GetPixelBilinear(uv.x+uv.width*(x+.5f)/resolution,uv.y+uv.height*(1-(y+.5f)/resolution)).a*255);
            shape=new PlateFoodShape(alpha,resolution,uv.x,uv.y,uv.width,uv.height);plateShapes.Add(foodId,shape);return shape;
        }
        public void Tap(ViewTap command)
        {
            if(friendSurfaceOpen || current==null || !controller.CanAcceptInput) { view.AcknowledgeTap(command.itemId);return; }
            int id;
            if(!int.TryParse(command.itemId,NumberStyles.None,CultureInfo.InvariantCulture,out id)) { view.AcknowledgeTap(command.itemId);return; }
            if(command.snapshotRevision!=shown.revision){view.AcknowledgeTap(command.itemId);return;}
            var result=current.Tap(new TapCommand(id,(ulong)command.inputSeq,Boundary,true,current.MayRefillOrdersOnTap(id)?CaptureOrderClickability():null));
            if(!result.Accepted)view.AcknowledgeTap(command.itemId);
            else if(result.Events.CanonicalEvents.Any(e=>e.Contains("\"type\":\"ItemRoutedToOrder\"")||e.Contains("\"type\":\"ItemRoutedToBuffer\"")))controller.StartChallengeTimer();
        }
        ClickableObservation CaptureOrderClickability()
        {
            if(current==null||!view||view.LastSnapshot==null||view.LastSnapshot.sessionId!=current.Snapshot.SessionId||view.LastSnapshot.revision!=shown.revision||view.LastSnapshot.revision.ToString(CultureInfo.InvariantCulture)!=current.Snapshot.TransactionId)return null;
            var observed=view.CaptureClickability();if(observed==null)return null;
            return new ClickableObservation(current.Snapshot.SessionId,current.Snapshot.TransactionId,observed.clickable.Select(id=>int.Parse(id,CultureInfo.InvariantCulture)),observed.unknown.Select(id=>int.Parse(id,CultureInfo.InvariantCulture)));
        }
        public void ObserveSupply(ViewSupplyObservation observation)
        {
            if(current==null || !controller.CanAcceptInput || !observingSupply)return;
            if(observation.version!=ViewSupplyObservation.CurrentVersion || observation.sessionGeneration!=controller.Generation ||
                observation.snapshotRevision!=shown.revision || observation.observationSequence<=lastSupplyObservation)return;
            lastSupplyObservation=observation.observationSequence;observingSupply=false;
            current.Supply(new SupplyObservation(++supplySequence,Boundary,observation.canSupply,true));
        }
        private void Update()
        {
            if(controller==null)return;
            view?.SetRemainingTime(Math.Max(0,600-controller.ChallengeSeconds));
            if(current==null || !controller.CanAcceptInput)return;
            if(controller.ChallengeSeconds>=600){current.Timeout(Boundary);return;}
            if(!supplySchedule.TryTake(controller.ActiveSeconds,true))return;
            int id=DailyViewMapper.PendingHead(current.Snapshot); if(id==0)return;
            float radius=DailyViewMapper.PlateRadius(current.PlateSize(id));
            observingSupply=true;
            try{view.ObserveSupply(new Vector2(DailyViewMapper.SpawnX(successfulSpawns),DailyViewMapper.SpawnY(radius)),radius);}
            finally{observingSupply=false;}
        }
        bool HasTarget(RewardKind kind)
        {
            if(kind==RewardKind.Revival)return current!=null&&current.RevivalPending&&!current.RevivalUsed;
            if(current==null || !controller.CanAcceptInput || view.ShuffleFeedbackActive)return false;
            switch(kind){case RewardKind.Hint:return view.FindClickableHint()!=null;case RewardKind.ClearBuffer:return current.CanClearBuffer;case RewardKind.FourthPot:return current.CanUnlockFourth;case RewardKind.Shuffle:return view.World.TryShuffle(false);default:return false;}
        }
        bool ApplyReward(RewardKind kind)
        {
            if(!HasTarget(kind))return false;
            switch(kind)
            {
                case RewardKind.Hint:var hint=view.FindClickableHint();if(hint==null)return false;view.HighlightItem(hint);return true;
                case RewardKind.ClearBuffer:return current.ClearBuffer(Boundary).Reason==null;
                case RewardKind.FourthPot:return current.UnlockFourth(Boundary,CaptureOrderClickability()).Reason==null;
                case RewardKind.Shuffle:return view.TryShuffleWithFeedback();
                default:return false;
            }
        }
        async void RequestReward(RewardKind kind,RewardRoute route)
        {
            if(kind==RewardKind.Revival){await RequestRevivalAsync();return;}
            if(!HasTarget(kind)){view.ShowNotice("暂无有效目标","当前没有可使用该道具的目标，不会领取奖励或扣除次数。");return;}
            route=RewardRoutes.ForService(route,rewardService.IsDevelopmentSimulation);
            if(route==RewardRoute.WeChatRewardedVideo&&rewardService is IRewardChannelAvailability channels&&!channels.IsRewardedVideoAvailable)
            {view.ShowNotice("广告暂不可用","当前版本未配置或未启用激励视频。没有发放奖励，也不会扣除分享次数。");return;}
            var request=new RewardRequest(controller.Generation,kind,route,TimeResolver.ChallengeDay(rewards.UtcNow),current.Snapshot.SessionId);
            try
            {
                var result=await rewards.RequestDetailedAsync(request,()=>controller.Generation,()=>HasTarget(kind),()=>ApplyReward(kind),controller.SetRewardPaused);
                if(result!=RewardApplicationResult.Applied && request.SessionGeneration==controller.Generation)
                    view.ShowNotice("未领取奖励",result==RewardApplicationResult.Unavailable?"当前渠道、分享额度或道具目标不可用；没有扣除分享次数。":"分享或广告未完成，请重试；没有扣除分享次数。");
            }
            catch(Exception ex){Debug.LogException(ex);view.ShowNotice("奖励服务暂不可用","请稍后重试。");}
        }
        public RevivalOfferView ReadRevivalOffer()
        {
            var availability=rewards?.ReadShareAvailability();
            if(HasTarget(RewardKind.Revival)&&revivalRouteOffer!=current.RevivalOfferId)
            {revivalRouteOffer=current.RevivalOfferId;revivalSelection=availability!=null&&availability.Available?RewardRoute.SimulatedShare:RewardRoute.SimulatedAd;}
            return new RevivalOfferView { sessionId=current?.Snapshot.SessionId,offerId=current?.RevivalOfferId,generation=controller?.Generation??0,
                available=HasTarget(RewardKind.Revival)&&pendingRevivalRequest==null,requestPending=pendingRevivalRequest!=null,
                shareAvailability=availability,route=revivalSelection,effectiveRoute=RewardRoutes.ForService(revivalSelection,rewardService?.IsDevelopmentSimulation??true),isDevelopmentSimulation=rewardService?.IsDevelopmentSimulation??true,
                rewardedVideoAvailable=(rewardService as IRewardChannelAvailability)?.IsRewardedVideoAvailable??true };
        }
        bool CurrentRevivalRequest(RewardRequest request,DailySession expected)=>controller!=null&&ReferenceEquals(current,expected)&&
            request.SessionGeneration==controller.Generation&&current.Snapshot.SessionId==request.SessionId&&current.RevivalPending&&!current.RevivalUsed&&current.RevivalOfferId==request.RevivalOfferId;
        public async Task<RewardApplicationResult> RequestRevivalAsync()
        {
            if(pendingRevivalRequest!=null)return RewardApplicationResult.Duplicate;
            var offer=ReadRevivalOffer();if(!offer.available)return RewardApplicationResult.Unavailable;
            // Freeze day and route on the click, not when the offer first appeared.
            var utc=rewards.UtcNow;var day=TimeResolver.ChallengeDay(utc);
            var route=offer.effectiveRoute;
            var expected=current;
            var request=new RewardRequest(controller.Generation,RewardKind.Revival,route,day,current.Snapshot.SessionId,current.RevivalOfferId);
            pendingRevivalRequest=request;
            try
            {
                var result=await rewards.RequestDetailedAsync(request,()=>controller.Generation,()=>CurrentRevivalRequest(request,expected),
                    ()=>current.ResolveRevival(new ResolveRevivalCommand(request.RevivalOfferId,request.RequestId,true,Boundary)).Accepted,controller.SetRewardPaused);
                // v9: failed/cancelled/unavailable reward attempts keep this offer paused
                // and retryable. Only the explicit Decline action ends the game.
                return result;
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
                return RewardApplicationResult.Failed;
            }
            finally {if(ReferenceEquals(pendingRevivalRequest,request))pendingRevivalRequest=null;}
        }
        public void DeclineRevival()
        {
            if(pendingRevivalRequest!=null||!HasTarget(RewardKind.Revival))return;
            current.ResolveRevival(new ResolveRevivalCommand(current.RevivalOfferId,null,false,Boundary));
        }
        public bool CompleteRevivalTransfer(RevivalCompletionToken token)
        {
            if(token==null||current==null||controller==null||token.sessionId!=current.Snapshot.SessionId||token.sessionGeneration!=controller.Generation)return false;
            // Validate against fresh authoritative facts, never a mutable View DTO.
            var expected=DailyViewMapper.Map(current.Snapshot,null,factory.Content,controller.ChallengeSeconds,controller.Generation,(ViewPauseReasons)(int)controller.Pauses,plateLayouts).snapshot.revivalTransfer;
            if(expected==null||!expected.token.Matches(token))return false;
            if(!current.CompleteRevivalTransfer(token.revivalOfferId,token.completionToken,Boundary).Accepted)return false;
            controller.SetRevivalPaused(false);return true;
        }
        void ShowSettings(){if(current!=null)actions.Request(Contracts.SessionAction.Pause);view.ShowSettings(profile.LoadSettings(),value=>{profile.SaveSettings(value);view.SetAudioSettings(value);});}
        async void ShowFriends()
        {
            if(rewardService!=null&&!rewardService.IsDevelopmentSimulation)
            {
                // Technical surface is exposed for the approved visual binding; no
                // local IFriendBoard fallback is permitted in a real platform session.
                if(friendSurface==null){view.ShowNotice("好友榜暂不可用","平台尚未配置好友榜服务。");return;}
                if(friendSurfaceOpen){RefreshFriendSurface();return;}
                if(appliedViewport!=null)
                {
                    OpenFriendSurface(FriendViewport(view.FriendBoardViewportPixels));
                }
                return;
            }
            try{var entries=await friends.LoadAsync();view.ShowNotice("好友榜 · 开发模拟",string.Join("\n",entries.Select(e=>e.DisplayName+"  "+e.FirstWins+" 次")));}
            catch(Exception ex){view.ShowNotice("好友榜暂不可用",ex.Message);}
        }
        private void ValidatePendingReward()
        {
            var request=rewards?.PendingRequest;if(request==null||rewards.IsApplying)return;
            bool valid=current!=null&&controller!=null&&request.SessionGeneration==controller.Generation&&request.SessionId==current.Snapshot.SessionId&&
                (current.Snapshot.Status==GameStatus.Running||current.Snapshot.Status==GameStatus.Paused);
            if(valid)switch(request.Kind)
            {
                case RewardKind.Revival:valid=current.RevivalPending&&!current.RevivalUsed&&current.RevivalOfferId==request.RevivalOfferId;break;
                case RewardKind.ClearBuffer:valid=shown.buffer.Any(i=>i!=null);break;
                case RewardKind.FourthPot:valid=shown.orders.Any(o=>o.slot==3&&!o.enabled);break;
                case RewardKind.Hint:valid=shown.plates.Any(p=>p.items.Length>0);break;
                case RewardKind.Shuffle:valid=shown.plates.Length>1;break;
            }
            if(!valid)rewards.InvalidateTarget();
        }
        async void ShareTheme()
        {
            try{await sharing.ShareThemeAsync(string.IsNullOrEmpty(approvedAssetRoot)?null:approvedAssetRoot+"/share/share_theme");}
            catch(Exception ex){view.ShowNotice("分享未完成",ex.Message);}
        }
    }
}
