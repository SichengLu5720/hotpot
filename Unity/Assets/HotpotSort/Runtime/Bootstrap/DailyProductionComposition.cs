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
    public sealed partial class DailyProductionComposition : ProductionComposition, IGameSessionFactory, IGameViewFactory, IGameView, IPresentationPort, IRevivalPresentationPort, IChallengeStageFactory, IWarmupPresentationPort, ISwapOrderActions
    {
        [SerializeField] private TextAsset dailyContent;
        [SerializeField] private Font playerFont;
        [SerializeField] private string approvedAssetRoot = "";
        public string ApprovedAssetRoot=>approvedAssetRoot;
        private DailySessionFactory factory;
        private DailySession current;
        private ViewTutorialStep tutorialStep;
        private string tutorialItemId;
        private int tutorialOrderSlot=-1;
        private bool bufferWarning,pendingBufferWarning;
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
        private ICollectionStore collection;
        public IBrothActivityService BrothActivity {get;private set;}
        private HotpotSort.Platform.WeChatActivityLinkService brothLinks;
        private string developmentBrothInvitation;
        private bool brothBusy,brothInspecting,brothAddedPause;
        private long brothGeneration;
        private string brothShownInvitation,brothObservedInvitation;
        private double brothNextInspect;
        public CollectionDocument BrothCollection=>collection?.ReadCollection();
        public string PendingBrothInvitationId=>BrothActivity?.IsDevelopmentSimulation==true?developmentBrothInvitation:brothLinks?.PendingInvitationId;
        public void ReceiveDevelopmentBrothInvitation(string invitationId)
        {if(BrothActivity?.IsDevelopmentSimulation!=true)return;developmentBrothInvitation=invitationId;OnBrothActivityChanged();}
        public event Action BrothActivityChanged;
        public void ConfigureBrothActivity(IBrothActivityService service,HotpotSort.Platform.WeChatActivityLinkService links)
        {
            if(!ReferenceEquals(BrothActivity,service)){brothGeneration++;brothBusy=false;brothInspecting=false;brothShownInvitation=null;if(view)view.SetBrothOperationState(false);}
            if(brothLinks!=null)brothLinks.PendingInvitationChanged-=OnBrothActivityChanged;
            if(!ReferenceEquals(BrothActivity,service))(BrothActivity as IDisposable)?.Dispose();
            BrothActivity=service;brothLinks=links;
            if(brothLinks!=null)brothLinks.PendingInvitationChanged+=OnBrothActivityChanged;
            OnBrothActivityChanged();if(service!=null)_=RefreshBrothAsync();
        }
        void OnBrothActivityChanged()
        {
            if(view)view.UpdateBrothPresentation(BrothCollection,BrothActivity?.IsDevelopmentSimulation==true);
            if(brothObservedInvitation!=PendingBrothInvitationId){brothObservedInvitation=PendingBrothInvitationId;brothShownInvitation=null;brothNextInspect=0;}
            BrothActivityChanged?.Invoke();
        }
        void BindBrothPresentation(bool bind)
        {
            if(!view)return;
            view.BrothActivityOpened-=OnBrothOpened;view.BrothActivityClosed-=OnBrothClosed;
            view.BrothRefreshRequested-=OnBrothRefresh;view.BrothInvitationShareRequested-=OnBrothShare;
            view.BrothAssistConfirmRequested-=OnBrothConfirm;view.BrothInvitationDismissRequested-=OnBrothDismiss;
            view.BrothClaimRequested-=OnBrothClaim;view.BrothSelectRequested-=OnBrothSelect;
            if(!bind)return;
            view.BrothActivityOpened+=OnBrothOpened;view.BrothActivityClosed+=OnBrothClosed;
            view.BrothRefreshRequested+=OnBrothRefresh;view.BrothInvitationShareRequested+=OnBrothShare;
            view.BrothAssistConfirmRequested+=OnBrothConfirm;view.BrothInvitationDismissRequested+=OnBrothDismiss;
            view.BrothClaimRequested+=OnBrothClaim;view.BrothSelectRequested+=OnBrothSelect;
            OnBrothActivityChanged();
        }
        void OnBrothOpened(){BrothPause();}
        void BrothPause(){if(current!=null&&controller!=null&&(controller.Pauses&PauseReasons.User)==0){brothAddedPause=true;actions?.Request(Contracts.SessionAction.Pause);}}
        void OnBrothClosed(){brothGeneration++;brothBusy=false;brothInspecting=false;if(brothAddedPause){brothAddedPause=false;actions?.Request(Contracts.SessionAction.Resume);}}
        void OnBrothDismiss(string id){if(id==PendingBrothInvitationId)DismissBrothInvitation();}
        void OnBrothRefresh()=>RunBrothOperation("read",null);
        void OnBrothShare()=>RunBrothOperation("share",null);
        void OnBrothConfirm(string id){if(id==PendingBrothInvitationId&&id==brothShownInvitation)RunBrothOperation("assist",id);}
        void OnBrothClaim(string id)=>RunBrothOperation("claim",id);
        void OnBrothSelect(string id)=>RunBrothOperation("select",id);
        static string BrothError(BrothFailure failure)
        {
            switch(failure){
                case BrothFailure.Offline:return "网络未连接或请求超时，请重试";
                case BrothFailure.SelfAssist:return "不能为自己助力";
                case BrothFailure.AlreadyAssisted:return "你已帮助过这位玩家，不会重复发奖";
                case BrothFailure.ActivityComplete:return "对方已获得助力，无需再次帮助";
                case BrothFailure.AssistOccupied:return "已有玩家正在助力，请等待任务结束";
                case BrothFailure.InvalidChallenge:return "挑战结果暂未确认，请联网重试";
                case BrothFailure.DailyLimit:return "今日已帮助 3 人，明天再来吧";
                case BrothFailure.NotQualified:return "暂未获得选择资格，请先完成活动条件";
                case BrothFailure.AlreadyChosen:return "本活动已选择锅底，不能改选";
                case BrothFailure.NotOwned:return "尚未拥有这个锅底";
                case BrothFailure.Stale:return "状态已更新，请确认后重试";
                case BrothFailure.NotFound:return "邀请不存在，请重新获取邀请";
                case BrothFailure.Unauthenticated:return "账号尚未登录，请稍后重试";
                case BrothFailure.Unavailable:return "活动服务暂不可用，请稍后重试";
                default:return "操作未完成，请重试";
            }
        }
        async void RunBrothOperation(string action,string argument)
        {
            if(brothBusy||!view)return;
            var service=BrothActivity;var doc=BrothCollection;
            if(service==null||doc==null){view.SetBrothOperationState(false,BrothError(BrothFailure.Unavailable));return;}
            long generation=brothGeneration;string account=doc.account,invitation=PendingBrothInvitationId;
            bool Current()=>this&&view&&(action=="select"?view.CollectionVisible:view.BrothVisible)&&generation==brothGeneration&&ReferenceEquals(service,BrothActivity)&&BrothCollection?.account==account&&(action!="assist"||invitation==PendingBrothInvitationId);
            string key="Hotpot.BrothIntent."+doc.environment+"."+account+"."+action+"."+(argument??"");
            brothBusy=true;view.SetBrothOperationState(true,"正在处理…");
            try{
                string operation=PlayerPrefs.GetString(key,"");if(operation.Length==0){operation=Guid.NewGuid().ToString("N");PlayerPrefs.SetString(key,operation);PlayerPrefs.Save();}
                BrothResult result;
                if(action=="read")result=await service.ReadAsync(Guid.NewGuid().ToString("N"));
                else if(action=="share")result=await ShareBrothInvitationAsync(operation);
                else if(action=="assist")result=await service.ConfirmAssistAsync(Guid.NewGuid().ToString("N"),argument,operation);
                else if(action=="claim")result=await service.ClaimAsync(Guid.NewGuid().ToString("N"),argument,operation);
                else result=await service.SelectAsync(Guid.NewGuid().ToString("N"),argument,operation);
                if(!Current())return;
                // Service has already validated and applied the authoritative snapshot.
                OnBrothActivityChanged();
                if(result.Succeeded){
                    PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
                    view.SetBrothOperationState(false,action=="share"?(service.IsDevelopmentSimulation?"Development 模拟：邀请已创建":"已打开分享，请等待对方确认助力"):"");
                    if(action=="assist"){if(result.invitation!=null)view.OpenBrothAssistConfirmation(result.invitation,service.IsDevelopmentSimulation);view.ShowBrothAssistSuccess(argument);}
                }else{
                    if(result.failure==BrothFailure.Stale||result.failure==BrothFailure.OperationConflict){PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();await service.ReadAsync(Guid.NewGuid().ToString("N"));}
                    if(Current())view.SetBrothOperationState(false,BrothError(result.failure));
                }
            }catch(Exception ex){Debug.LogWarning("Broth operation unavailable: "+ex.GetType().Name);if(Current())view.SetBrothOperationState(false,"操作未完成，请重试");}
            finally{if(generation==brothGeneration){brothBusy=false;if(!Current()&&view)view.SetBrothOperationState(false);}}
        }
        // Defer landing until entry, preserving current gameplay/modal ownership.
        // A blocked ingredient draft retains its pending link and is retried later.
        async void TryPresentBrothInvitation()
        {
            if(!view||BrothActivity==null||PendingBrothInvitationId==null||brothBusy||brothInspecting||brothShownInvitation==PendingBrothInvitationId||Time.realtimeSinceStartupAsDouble<brothNextInspect)return;
            if(shown.phase!=ViewPhase.Entry||friendSurfaceOpen||view.CollectionRewardVisible||view.CollectionTradeVisible)return;
            var service=BrothActivity;string invitation=PendingBrothInvitationId,account=BrothCollection?.account;long generation=brothGeneration;
            brothInspecting=true;brothNextInspect=Time.realtimeSinceStartupAsDouble+3;
            try{
                var result=await service.InspectInvitationAsync(Guid.NewGuid().ToString("N"),invitation);
                if(!this||!view||generation!=brothGeneration||!ReferenceEquals(service,BrothActivity)||PendingBrothInvitationId!=invitation||BrothCollection?.account!=account||shown.phase!=ViewPhase.Entry)return;
                if(result.Succeeded&&result.invitation!=null){view.OpenBrothAssistConfirmation(result.invitation,service.IsDevelopmentSimulation);if(view.BrothVisible){brothShownInvitation=invitation;BrothPause();}}
                else{view.OpenBrothAssistConfirmation(new BrothInvitation{invitationId=invitation},service.IsDevelopmentSimulation);if(view.BrothVisible)view.SetBrothOperationState(false,BrothError(result.failure));}
            }catch(Exception ex){Debug.LogWarning("Broth invitation unavailable: "+ex.GetType().Name);}
            finally{if(generation==brothGeneration)brothInspecting=false;}
        }
        public async Task<BrothResult> RefreshBrothAsync()
        {
            var service=BrothActivity;if(service==null)return new BrothResult{failure=BrothFailure.Unavailable};
            var result=await service.ReadAsync(Guid.NewGuid().ToString("N"));
            if(ReferenceEquals(service,BrothActivity)){OnBrothActivityChanged();if(result.Succeeded)RestoreBrothHelping();}return result;
        }
        public Task<BrothResult> InspectPendingBrothInvitationAsync()
            =>BrothActivity==null||PendingBrothInvitationId==null?Task.FromResult(new BrothResult{failure=BrothFailure.Unavailable}):BrothActivity.InspectInvitationAsync(Guid.NewGuid().ToString("N"),PendingBrothInvitationId);
        public Task<BrothResult> ConfirmPendingBrothInvitationAsync(string operationId)
            =>BrothActivity==null||PendingBrothInvitationId==null?Task.FromResult(new BrothResult{failure=BrothFailure.Unavailable}):BrothActivity.ConfirmAssistAsync(Guid.NewGuid().ToString("N"),PendingBrothInvitationId,operationId);
        public void DismissBrothInvitation(){if(BrothActivity?.IsDevelopmentSimulation==true){developmentBrothInvitation=null;OnBrothActivityChanged();}else brothLinks?.Dismiss(PendingBrothInvitationId);}
        public async Task<BrothResult> ShareBrothInvitationAsync(string operationId)
        {
            var service=BrothActivity;if(service==null)return new BrothResult{failure=BrothFailure.Unavailable};
            var result=await service.CreateInvitationAsync(Guid.NewGuid().ToString("N"),operationId);
            if(!ReferenceEquals(service,BrothActivity))return new BrothResult{failure=BrothFailure.Stale};
            // Development returns its explicitly marked invite for a simulator to
            // consume. Native share availability is never treated as assistance.
            if(result.Succeeded&&!service.IsDevelopmentSimulation&&(brothLinks==null||!brothLinks.RequestShare(result.invitation?.invitationId)))result.failure=BrothFailure.Unavailable;
            return result;
        }
        private HotpotSort.Platform.WeChatIngredientTradeService collectionAuthority;
        private bool collectionToolBusy;
        private string collectionSelectionSignature;
        private bool applyingCollectionAuthority;
        string CollectionPendingKey=>"Hotpot.CollectionSelection."+collection.ReadCollection().environment+"."+collection.ReadCollection().account;
        [Serializable] sealed class PendingCollectionSelection {public List<string> selected;public string operation;public long revision;}
        public void ConfigureCollectionAuthority(HotpotSort.Platform.WeChatIngredientTradeService authority)
        {if(!ReferenceEquals(collectionAuthority,authority))collectionAuthority?.Dispose();collectionAuthority=authority;BindCollection();_=SyncCollectionAsync();_=FlushSwapCommit();}
        void BindCollection()
        {
            if(collection!=null)collection.CollectionChanged-=OnCollectionChanged;
            collection=(profile as LocalDevelopmentServices)?.Collection;
            collectionSelectionSignature=collection==null?null:string.Join(",",collection.ReadCollection().selected);
            if(collection!=null)collection.CollectionChanged+=OnCollectionChanged;
            if(view){view.ConfigureCollection(collection,collectionAuthority);BindSocialPresentation();}
            OnBrothActivityChanged();
            if((profile as LocalDevelopmentServices)?.IsDevelopmentSimulation==true)
                ConfigureBrothActivity((profile as LocalDevelopmentServices).BrothActivity,null);
        }
        async void OnCollectionChanged()
        {
            OnBrothActivityChanged();
            view?.RefreshCollectionToolStock();
            if(collection==null)return;
            var doc=collection.ReadCollection();string signature=string.Join(",",doc.selected);
            if(signature==collectionSelectionSignature)return;collectionSelectionSignature=signature;
            if(applyingCollectionAuthority||rewardService?.IsDevelopmentSimulation!=false)return;
            PlayerPrefs.SetString(CollectionPendingKey,JsonUtility.ToJson(new PendingCollectionSelection{selected=doc.selected,operation=Guid.NewGuid().ToString("N"),revision=doc.serverRevision}));PlayerPrefs.Save();
            await SavePendingCollectionSelection();
        }
        async Task SavePendingCollectionSelection()
        {
            if(collectionAuthority==null||collection==null)return;
            string key=CollectionPendingKey,json=PlayerPrefs.GetString(key,"");if(json.Length==0)return;
            var pending=JsonUtility.FromJson<PendingCollectionSelection>(json);
            var result=await collectionAuthority.SaveSelectionAsync(pending.selected,pending.revision,pending.operation);
            if(result.Succeeded&&PlayerPrefs.GetString(key,"")==json){PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
            else if(result.failure==IngredientTradeFailure.Stale&&PlayerPrefs.GetString(key,"")==json)
            {pending.revision=collection.ReadCollection().serverRevision;pending.operation=Guid.NewGuid().ToString("N");PlayerPrefs.SetString(key,JsonUtility.ToJson(pending));PlayerPrefs.Save();}
        }
        public async Task<IngredientTradeResult> SyncCollectionAsync()
        {
            if(collectionAuthority==null||collection==null)return null;
            var result=await collectionAuthority.SyncAsync(collection.ReadCollection().pendingWins,Guid.NewGuid().ToString("N"));
            await SavePendingCollectionSelection();return result;
        }
        public void ApplyCollectionAuthority(CollectionDocument snapshot)
        {
            applyingCollectionAuthority=true;
            try
            {
                collection?.ApplyAuthoritativeSnapshot(snapshot);
                string json=collection==null?"":PlayerPrefs.GetString(CollectionPendingKey,"");
                if(json.Length>0)collection.TrySaveSelection(JsonUtility.FromJson<PendingCollectionSelection>(json).selected);
            }
            finally{applyingCollectionAuthority=false;}
        }
        public void PrepareCollectionSession()
        {EnsureFactory();if(collection!=null)factory=factory.WithIngredientSelection(collection.CreateSessionSelection());}
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
            BindCollection();
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
            BindCollection();
            BindBrothPresentation(true);
            view.SettingsRequested+=ShowSettings;
            view.FriendsRequested+=ShowFriends;
            view.ShareRequested+=ShareTheme;
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
            TryPresentBrothInvitation();
            if(controller.CurrentViewport!=null && !ReferenceEquals(appliedViewport,controller.CurrentViewport))SetViewport(controller.CurrentViewport);
            if(!string.IsNullOrEmpty(controller.Error))ShowError("session-error",controller.Error);
            var pauses=(ViewPauseReasons)(int)controller.Pauses;
            if((observedPauses&ViewPauseReasons.Background)!=0&&(pauses&ViewPauseReasons.Background)==0)
            {_=SyncProfileAsync();_=SyncCollectionAsync();PublishFriendScore();RefreshFriendSurface();}
            observedPauses=pauses;
            ValidatePendingReward();
        }
        private void OnDestroy() { if(view){view.CollectionTradeShareRequested-=ShareCollectionTrade;view.CollectionTradeClosed-=DismissCollectionTrade;}collectionAuthority?.Dispose();BindBrothPresentation(false);ConfigureBrothActivity(null,null);if(collection!=null)collection.CollectionChanged-=OnCollectionChanged;rewards?.InvalidateSession();CloseFriendSurface();FriendSurfaceChanged-=OnFriendPresentationChanged;if(profile is IAsyncProfileStore sync){sync.ProfileChanged-=OnProfileChanged;sync.InvalidateSyncCallbacks();}if(controller!=null)controller.ObservationChanged-=OnSessionObservation; }
        private void OnApplicationFocus(bool focused){if(focused){_=SyncProfileAsync();_=SyncCollectionAsync();PublishFriendScore();RefreshFriendSurface();}}
        public IGameSession CreateSession(ChallengeContext context)
            =>CreateStage(context,ChallengeStage.Warmup,0);
        public IGameSession CreateStage(ChallengeContext context,ChallengeStage stage,int inheritedPotMask)
        {
            EnsureFactory(); showingError=false; supplySequence=0; supplySchedule.Reset();lastSupplyObservation=0; ResetSpawnPresentation();
            (profile as IAsyncProfileStore)?.InvalidateSyncCallbacks();revivalRouteOffer=null;
            tutorialStep=stage==ChallengeStage.Warmup&&(profile as ITutorialProfileStore)?.WarmupTutorialCompleted!=true?ViewTutorialStep.WaitingForBoard:ViewTutorialStep.None;
            tutorialItemId=null;tutorialOrderSlot=-1;bufferWarning=pendingBufferWarning=false;
            var selectedContext=new ChallengeContext(context.ChallengeId,context.ContentVersion,factory.ConfigurationDigest,context.TimeSource,context.RetryIndex);
            current=factory.CreateStage(selectedContext,stage,inheritedPotMask); return current;
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
            if(!bufferWarning&&tutorialStep==ViewTutorialStep.None&&snapshot.Status==GameStatus.Running&&update.snapshot.buffer.Count(i=>i!=null)==4&&profile is ITutorialProfileStore tutorialProfile&&!tutorialProfile.BufferWarningCompleted)pendingBufferWarning=true;
            update.snapshot.tutorialStep=tutorialStep;update.snapshot.tutorialItemId=tutorialItemId;update.snapshot.tutorialOrderSlot=tutorialOrderSlot;update.snapshot.bufferWarning=bufferWarning;
            if(snapshot.Status==GameStatus.Won && current.Stage!=ChallengeStage.Warmup && winRecordedSession!=snapshot.SessionId)
            {RecordBrothChallengeWin(snapshot.SessionId,rewards.UtcNow);profile.RecordFirstWin(snapshot.Challenge.ChallengeId);winRecordedSession=snapshot.SessionId;view.CollectionSettlementPending=true;}
            shown=update.snapshot; Updated?.Invoke(update);
            if(view.CollectionSettlementPending&&snapshot.Status==GameStatus.Won&&collectionRewardSession!=snapshot.SessionId)
            {collectionRewardSession=snapshot.SessionId;PresentCollectionWin(snapshot.SessionId,controller.Generation);}
            ValidatePendingReward();
        }
        string collectionRewardSession;
        async void PresentCollectionWin(string sessionId,long generation)
        {
            try
            {
                var reward=collection?.RecordWin(sessionId,rewards.UtcNow,rewardService.IsDevelopmentSimulation);
                if(!rewardService.IsDevelopmentSimulation)
                {var result=await SyncCollectionAsync();reward=result?.collection?.rewards.FirstOrDefault(r=>r.sessionId==sessionId);}
                if(!this||!view||current?.Snapshot.SessionId!=sessionId||controller.Generation!=generation)return;
                if(reward==null||!view.ShowCollectionReward(reward,view.CompleteCollectionSettlement))view.CompleteCollectionSettlement();
            }
            catch(Exception ex){Debug.LogWarning("Collection reward pending: "+ex.GetType().Name);if(this&&view&&current?.Snapshot.SessionId==sessionId)view.CompleteCollectionSettlement();}
        }
        public void ShowLoading() { showingError=false; }
        public void ShowError(string code,string message) { showingError=true; view.ShowError(message); }
        public void ResetSession()
        {
            CancelRemoteSwapReservation();
            swapSelecting=swapBusy=false;
            view?.ResetCollectionTransientPresentation();if(view)view.CompleteCollectionSettlement();collectionRewardSession=null;
            rewards?.InvalidateSession();pendingRevivalRequest=null;
            CloseFriendSurface();revivalRouteOffer=null;
            (profile as IAsyncProfileStore)?.InvalidateSyncCallbacks();
            current=null; supplySequence=0; supplySchedule.Reset();lastSupplyObservation=0;observingSupply=false;winRecordedSession=null;ResetSpawnPresentation();
            tutorialStep=ViewTutorialStep.None;tutorialItemId=null;tutorialOrderSlot=-1;bufferWarning=pendingBufferWarning=false;
            if(!showingError) { shown=new ViewSnapshot { phase=ViewPhase.Entry }; view.ResetView(); }
        }
        void IDisposable.Dispose() { /* View persists as the approved entry after session Exit. Bootstrap owns its lifetime. */ }
        public ViewSnapshot Read() { return shown; }
        public void SessionAction(ViewAction action)
        {
            if(SwapInputLocked){if(action==ViewAction.Exit&&!swapBusy&&current?.SwapOrderPending!=true)CancelSwapOrderSelection();return;}
            if(collectionToolBusy)return;
            if(action==ViewAction.Exit&&view.HandleCollectionBack())return;
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
            if(SwapInputLocked || friendSurfaceOpen || current==null || !controller.CanAcceptInput || bufferWarning || pendingBufferWarning ||
                (tutorialStep!=ViewTutorialStep.None&&(tutorialStep!=ViewTutorialStep.SelectFood||command.itemId!=tutorialItemId))) { view.AcknowledgeTap(command.itemId);return; }
            int id;
            if(!int.TryParse(command.itemId,NumberStyles.None,CultureInfo.InvariantCulture,out id)) { view.AcknowledgeTap(command.itemId);return; }
            if(command.snapshotRevision!=shown.revision){view.AcknowledgeTap(command.itemId);return;}
            bool tutorialTap=tutorialStep==ViewTutorialStep.SelectFood;
            if(tutorialTap)tutorialStep=ViewTutorialStep.FoodInFlight;
            var result=current.Tap(new TapCommand(id,(ulong)command.inputSeq,Boundary,true,current.MayRefillOrdersOnTap(id)?CaptureOrderClickability():null));
            if(!result.Accepted){if(tutorialTap)tutorialStep=ViewTutorialStep.SelectFood;view.AcknowledgeTap(command.itemId);}
            else if(result.Events.CanonicalEvents.Any(e=>e.Contains("\"type\":\"ItemRoutedToOrder\"")||e.Contains("\"type\":\"ItemRoutedToBuffer\"")))controller.StartChallengeTimer();
        }
        ClickableObservation CaptureOrderClickability()
        {
            if(current==null||!view||view.LastSnapshot==null||view.LastSnapshot.sessionId!=current.Snapshot.SessionId||view.LastSnapshot.revision!=shown.revision||view.LastSnapshot.revision.ToString(CultureInfo.InvariantCulture)!=current.Snapshot.TransactionId)return null;
            var observed=view.CaptureClickability();if(observed==null)return null;
            return new ClickableObservation(current.Snapshot.SessionId,current.Snapshot.TransactionId,observed.clickable.Select(id=>int.Parse(id,CultureInfo.InvariantCulture)),observed.unknown.Select(id=>int.Parse(id,CultureInfo.InvariantCulture)),nextLayerItemIds:(observed.nextLayer??Array.Empty<string>()).Select(id=>int.Parse(id,CultureInfo.InvariantCulture)));
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
            UpdateSocialActivity();
            TryPresentBrothInvitation();
            if(controller==null)return;
            if(current!=null&&tutorialStep==ViewTutorialStep.WaitingForBoard&&controller.CanAcceptInput&&DailyViewMapper.PendingHead(current.Snapshot)==0&&
                view.LastSnapshot?.sessionId==current.Snapshot.SessionId&&view.World.HasSettledInitialBoard(shown.plates.Length))
            {tutorialStep=ViewTutorialStep.SelectFood;Show(current.Snapshot,null);}
            if(current!=null&&pendingBufferWarning&&current.Snapshot.Status==GameStatus.Running)
            {pendingBufferWarning=false;bufferWarning=true;controller.SetTutorialPaused(true);}
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
        bool CurrentTutorialSession(string id,long generation)=>current!=null&&controller!=null&&current.Snapshot.SessionId==id&&controller.Generation==generation;
        public bool SelectTutorialFood(string sessionId,long generation,string itemId)
        {
            if(!CurrentTutorialSession(sessionId,generation)||tutorialStep!=ViewTutorialStep.SelectFood||!controller.CanAcceptInput)return false;
            var item=shown.plates.SelectMany(p=>p.items).FirstOrDefault(i=>i.itemId==itemId);
            var order=item==null?null:shown.orders.FirstOrDefault(o=>o.enabled&&o.foodId==item.foodId&&o.count<3);
            if(order==null||view.CaptureClickability()?.clickable.Contains(itemId)!=true)return false;
            tutorialItemId=itemId;tutorialOrderSlot=order.slot;Show(current.Snapshot,null);return true;
        }
        public bool TutorialFoodArrived(string sessionId,long generation,string itemId)
        {
            if(!CurrentTutorialSession(sessionId,generation)||tutorialStep!=ViewTutorialStep.FoodInFlight||tutorialItemId!=itemId)return false;
            tutorialStep=ViewTutorialStep.OrderExplanation;controller.SetTutorialPaused(true);return true;
        }
        public bool CompleteOpeningTutorial(string sessionId,long generation)
        {
            if(!CurrentTutorialSession(sessionId,generation)||tutorialStep!=ViewTutorialStep.OrderExplanation)return false;
            (profile as ITutorialProfileStore)?.CompleteTutorial(false);
            tutorialStep=ViewTutorialStep.None;tutorialItemId=null;tutorialOrderSlot=-1;controller.SetTutorialPaused(false);return true;
        }
        public bool CompleteBufferWarning(string sessionId,long generation)
        {
            if(!CurrentTutorialSession(sessionId,generation)||!bufferWarning)return false;
            (profile as ITutorialProfileStore)?.CompleteTutorial(true);
            bufferWarning=false;controller.SetTutorialPaused(false);return true;
        }
        public bool CompleteWarmup(string sessionId,long generation)=>controller!=null&&controller.ContinueToFormal(sessionId,generation);
        bool HasTarget(RewardKind kind)
        {
            if(kind==RewardKind.Revival)return current!=null&&current.RevivalPending&&!current.RevivalUsed;
            if(current==null || !controller.CanAcceptInput || view.ShuffleFeedbackActive || tutorialStep!=ViewTutorialStep.None || bufferWarning || pendingBufferWarning)return false;
            if(SwapInputLocked)return false;
            switch(kind){case RewardKind.SwapOrder:return current.GetSwapOrderTargets(CaptureOrderClickability()).Length>0;case RewardKind.ClearBuffer:return current.CanClearBuffer;case RewardKind.ThirdPot:return current.CanUnlockThird;case RewardKind.FourthPot:return current.CanUnlockFourth;case RewardKind.Shuffle:return view.World.TryShuffle(false);default:return false;}
        }
        bool ApplyReward(RewardKind kind)
        {
            if(!HasTarget(kind))return false;
            switch(kind)
            {
                case RewardKind.SwapOrder:return false; // Uses the selected, revalidated two-phase transaction.
                case RewardKind.ClearBuffer:return current.ClearBuffer(Boundary).Reason==null;
                case RewardKind.ThirdPot:return current.UnlockThird(Boundary,CaptureOrderClickability()).Reason==null;
                case RewardKind.FourthPot:return current.UnlockFourth(Boundary,CaptureOrderClickability()).Reason==null;
                case RewardKind.Shuffle:return view.TryShuffleWithFeedback();
                default:return false;
            }
        }
        async void RequestReward(RewardKind kind,RewardRoute route)
        {
            if(kind==RewardKind.SwapOrder){BeginSwapOrderSelection();return;}
            if(SwapInputLocked)return;
            if(collectionToolBusy)return;
            if(kind==RewardKind.Revival){await RequestRevivalAsync();return;}
            if(!HasTarget(kind)){view.ShowNotice("暂无有效目标","当前没有可使用该道具的目标，不会领取奖励或扣除次数。");return;}
            if(view.CollectionToolCount(kind)>0)
            {
                collectionToolBusy=true;var expected=current;long generation=controller.Generation;
                controller.SetRewardPaused(true);
                try
                {
                    string toolKey=CollectionPendingKey+".tool."+(int)kind;
                    string operation=PlayerPrefs.GetString(toolKey,"");if(operation.Length==0){operation=Guid.NewGuid().ToString("N");PlayerPrefs.SetString(toolKey,operation);PlayerPrefs.Save();}
                    bool consumed=rewardService.IsDevelopmentSimulation?collection.TryConsumeTool(kind,operation):collectionAuthority!=null&&(await collectionAuthority.ConsumeAsync(kind,operation)).Succeeded;
                    if(consumed){PlayerPrefs.DeleteKey(toolKey);PlayerPrefs.Save();}
                    if(ReferenceEquals(expected,current)&&generation==controller.Generation)controller.SetRewardPaused(false);
                    if(consumed&&ReferenceEquals(expected,current)&&generation==controller.Generation)ApplyReward(kind);
                    else if(!consumed)view.ShowNotice("道具暂不可用","请联网后重试，免费库存尚未确认。");
                }
                catch(Exception ex){Debug.LogWarning("Collection tool unavailable: "+ex.GetType().Name);}
                finally{if(ReferenceEquals(expected,current)&&generation==controller.Generation)controller.SetRewardPaused(false);collectionToolBusy=false;}
                return;
            }
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
                case RewardKind.ThirdPot:valid=shown.orders.Any(o=>o.slot==2&&!o.enabled);break;
                case RewardKind.FourthPot:valid=shown.orders.Any(o=>o.slot==3&&!o.enabled);break;
                case RewardKind.SwapOrder:valid=swapSelecting&&swapBusy&&!current.SwapOrderPending;break;
                case RewardKind.Shuffle:valid=shown.plates.Length>1;break;
            }
            if(!valid)rewards.InvalidateTarget();
        }
        async void ShareTheme()
        {
            var result=shown;
            if(result==null||(result.phase!=ViewPhase.Won&&result.phase!=ViewPhase.Overflow)||sharing==null)return;
            string image=string.IsNullOrEmpty(approvedAssetRoot)?null:approvedAssetRoot+"/share/share_theme";
            string title=SettlementShareText.Title(result.phase==ViewPhase.Won,result.completedOrders,result.totalOrders);
            try
            {
                if(sharing is IResultThemeShare resultShare)await resultShare.ShareThemeAsync(image,title);
                else await sharing.ShareThemeAsync(image);
            }
            catch(Exception ex){Debug.LogWarning("Settlement share unavailable: "+ex.GetType().Name);}
        }
#if UNITY_EDITOR
        const string BrothSmokeKey="Task031.IntegrationSmoke";
        [UnityEditor.InitializeOnLoadMethod] static void RegisterBrothSmoke()
        {
            UnityEditor.EditorApplication.playModeStateChanged+=state=>{if(state==UnityEditor.PlayModeStateChange.EnteredPlayMode&&UnityEditor.SessionState.GetBool(BrothSmokeKey,false))RunBrothSmokeBody();};
        }
        public static void RunBrothIntegrationDiagnostic()
        {
            UnityEditor.SessionState.SetBool(BrothSmokeKey,true);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");
            UnityEditor.EditorApplication.EnterPlaymode();
        }
        sealed class BrothSmokePersistence:ICollectionPersistence
        {
            CollectionDocument document;
            public CollectionDocument Load()=>document;
            public void Save(CollectionDocument value){document=HotpotSort.Collection.CollectionStore.Copy(value);}
        }
        static async void RunBrothSmokeBody()
        {
            string output=Environment.GetEnvironmentVariable("HOTPOT_BROTH_EVIDENCE");
            var checks=new List<string>();var errors=new List<string>();DailyProductionComposition daily=null;
            Application.LogCallback capture=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);};Application.logMessageReceived+=capture;
            try{
                if(string.IsNullOrEmpty(output))throw new InvalidOperationException("HOTPOT_BROTH_EVIDENCE required");System.IO.Directory.CreateDirectory(output);
                double deadline=UnityEditor.EditorApplication.timeSinceStartup+60;
                while(daily==null||!daily.PlayerView){daily=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();if(UnityEditor.EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Boot entry unavailable");await Task.Yield();}
                var boot=UnityEngine.Object.FindFirstObjectByType<Bootstrap>();while(!boot.IsConfigured){if(UnityEditor.EditorApplication.timeSinceStartup>deadline)throw new TimeoutException(boot.Status);await Task.Yield();}
                void Check(bool pass,string message){if(!pass)throw new Exception(message);checks.Add(message);Debug.Log("TASK031_INTEGRATION_PASS "+message);}
                async Task Frames(){for(int i=0;i<4;i++)await Task.Yield();}
                async Task Shot(string name){await Frames();var captureType=Type.GetType("HotpotSort.Task001V7.VisualCapture, HotpotSort.Task001V7.Editor",true);captureType.GetMethod("Capture",new[]{typeof(GameplayView),typeof(int),typeof(int),typeof(string)}).Invoke(null,new object[]{daily.view,1080,1920,System.IO.Path.Combine(output,name+".png")});await Frames();}
                void Event(string name,string argument){var field=typeof(GameplayView).GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);var action=field?.GetValue(daily.view) as Action<string>;Check(action!=null,name+" wired");action(argument);}
                bool TextContains(string text)=>daily.view.GetComponentsInChildren<Component>().Any(c=>c.GetType().Name=="Text"&&((string)c.GetType().GetProperty("text").GetValue(c)).Contains(text));
                var local=new LocalDevelopmentServices("Hotpot.Task031.Smoke."+Guid.NewGuid().ToString("N"));daily.ConfigureServices(local,local,local,local);
                Check(daily.BrothActivity.IsDevelopmentSimulation,"real Boot configured development broth service");
                Check(!daily.view.GetComponentsInChildren<Transform>().Any(t=>t.name=="BrothActivityEntry"),"first-time activity entrance hidden");
                await Shot("boot-first-time");
                var hostStore=new HotpotSort.Collection.CollectionStore("development","smoke-host",new BrothSmokePersistence());((HotpotSort.Collection.CollectionStore)hostStore).ImportCompletedWinHistory(true);
                var host=new HotpotSort.Collection.BrothActivityStore(local.DevelopmentBroths,hostStore);var invitation=await host.CreateInvitationAsync("smoke-create","host-create");
                daily.ReceiveDevelopmentBrothInvitation(invitation.invitation.invitationId);await Frames();
                Check(daily.view.BrothVisible&&!daily.BrothCollection.entryUnlocked,"pending invite opens before first win");
                Check(TextContains("Development 模拟"),"simulation label visible");Check(local.Collection.ReadCollection().tools.All(n=>n==0),"opening confirmation grants no reward");await Shot("friend-confirm");
                Event("BrothAssistConfirmRequested",invitation.invitation.invitationId);await Frames();
                Check(local.Collection.ReadCollection().tools.SequenceEqual(new[]{1,1,1}),"explicit confirmation grants three tools once");
                Check(TextContains("奖励已到账"),"confirmed success rendered");await Shot("friend-success");daily.view.CloseBrothActivity();
                ((HotpotSort.Collection.CollectionStore)local.Collection).ImportCompletedWinHistory(true);
                var own=await daily.BrothActivity.CreateInvitationAsync("own-create","own-invite");
                var helperStore=new HotpotSort.Collection.CollectionStore("development","smoke-helper",new BrothSmokePersistence());var helper=new HotpotSort.Collection.BrothActivityStore(local.DevelopmentBroths,helperStore);
                await helper.ConfirmAssistAsync("helper-request",own.invitation.invitationId,"helper-assist");await daily.RefreshBrothAsync();
                daily.view.OpenBrothActivity();await Frames();Check(BrothCatalog.HasClaimReminder(daily.BrothCollection),"qualified activity reminder visible");await Shot("activity-qualified");
                daily.view.ShowBrothClaimConfirmation(BrothCatalog.Tomato);await Shot("claim-confirm");Event("BrothClaimRequested",BrothCatalog.Tomato);await Frames();
                Check(daily.BrothCollection.brothActivityChoice==BrothCatalog.Tomato&&daily.view.PresentedBrothId==BrothCatalog.Tomato,"claim event updates authority and presented soup");
                Check(!BrothCatalog.HasClaimReminder(daily.BrothCollection),"claim clears reminder");daily.view.CloseBrothActivity();daily.view.OpenBrothCollection();await Shot("collection-owned");
                Event("BrothSelectRequested",BrothCatalog.Red);await Frames();Check(daily.BrothCollection.currentBroth==BrothCatalog.Red&&daily.BrothCollection.brothActivityChoice==BrothCatalog.Tomato,"collection switch preserves immutable choice");
                Check(TaskAssetValidation.Validate(PresentationAssets.CandidateRoot,true).Length==0,"complete formal asset validation");
                Check(daily.view.GetComponentsInChildren<Component>().Where(c=>c.GetType().Name=="RawImage").All(c=>c.GetType().GetProperty("texture").GetValue(c)!=null),"no missing page textures");
                Check(errors.Count==0,"no Unity error or exception during Boot and activity flow");
                System.IO.File.WriteAllLines(System.IO.Path.Combine(output,"checks.txt"),checks);
                System.IO.File.WriteAllText(System.IO.Path.Combine(output,"scope.txt"),"Real Unity Boot PlayMode, production composition event wiring with explicit Development authority. No live cloud, deployment, or device claims.");
                daily.view.Bind(null);daily.view.World.Clear();daily.view.gameObject.SetActive(false);Debug.Log("TASK031_INTEGRATION_SMOKE_OK");
                Application.logMessageReceived-=capture;UnityEditor.SessionState.SetBool(BrothSmokeKey,false);UnityEditor.EditorApplication.Exit(0);
            }catch(Exception ex){Application.logMessageReceived-=capture;Debug.LogError("TASK031_INTEGRATION_SMOKE_FAILED "+ex);if(!string.IsNullOrEmpty(output))System.IO.File.WriteAllText(System.IO.Path.Combine(output,"failure.txt"),ex.ToString()+"\n"+string.Join("\n",errors));UnityEditor.SessionState.SetBool(BrothSmokeKey,false);UnityEditor.EditorApplication.Exit(1);}
        }
#endif
    }
}
