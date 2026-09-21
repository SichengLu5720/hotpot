using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Presentation;
using HotpotSort.Session;
using UnityEngine;

namespace HotpotSort.Bootstrap
{
    public sealed class DailyProductionComposition : ProductionComposition, IGameSessionFactory, IGameViewFactory, IGameView, IPresentationPort
    {
        [SerializeField] private TextAsset dailyContent;
        [SerializeField] private Font playerFont;
        [SerializeField] private string approvedAssetRoot = "";
        private DailySessionFactory factory;
        private DailySession current;
        private SessionController controller;
        private GameplayView view;
        private ISessionActions actions;
        private IDiagnosticSink diagnostics;
        private bool showingError;
        private ulong supplySequence;
        private double nextSupply;
        private readonly Dictionary<string,ViewPlateMotion> spawnMotions=new Dictionary<string,ViewPlateMotion>();
        private System.Random spawnRandom=new System.Random(601377);
        private int successfulSpawns;
        private Viewport appliedViewport;
        private ViewSnapshot shown=new ViewSnapshot { phase=ViewPhase.Entry };
        private IProfileStore profile;
        private IRewardService rewardService;
        private IFriendBoard friends;
        private IThemeShare sharing;
        private RewardCoordinator rewards;
        private string winRecordedSession;
        public event Action<ViewUpdate> Updated;
        public override string ContentVersion { get { EnsureFactory(); return factory.Content.ContentVersion; } }
        public override string ConfigurationDigest { get { EnsureFactory(); return factory.ConfigurationDigest; } }
        public override string BuildIdentity => "task001-v5-fixed-c-development";
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
        public void ConfigureServices(IProfileStore store,IRewardService reward,IFriendBoard board,IThemeShare share)
        {profile=store;rewardService=reward;friends=board;sharing=share;rewards=new RewardCoordinator(reward,store);}
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
            view.ConfigureAssets(approvedAssetRoot);
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
            view.ShareRequested+=ShareTheme;
            view.SetAudioSettings(profile.LoadSettings());
            view.Bind(this);
            controller.ObservationChanged+=OnSessionObservation;
            OnSessionObservation();
        }
        private void OnSessionObservation()
        {
            if(controller.CurrentViewport!=null && !ReferenceEquals(appliedViewport,controller.CurrentViewport))SetViewport(controller.CurrentViewport);
            if(!string.IsNullOrEmpty(controller.Error))ShowError("session-error",controller.Error);
        }
        private void OnDestroy() { if(controller!=null)controller.ObservationChanged-=OnSessionObservation; }
        public IGameSession CreateSession(ChallengeContext context)
        {
            EnsureFactory(); showingError=false; supplySequence=0; nextSupply=0; ResetSpawnPresentation();
            current=factory.CreateDailySession(context); return current;
        }
        public IGameView CreateView() { return this; }
        void IGameView.Bind(ISessionActions value) { actions=value; }
        public void SetViewport(Viewport value) { appliedViewport=value; view.SetViewport(new Rect(value.SafeX,value.SafeY,value.SafeWidth,value.SafeHeight)); }
        public void Show(GameSnapshot snapshot,GameEventBatch events)
        {
            showingError=false;
            var update=DailyViewMapper.Map(snapshot,events,factory.Content,controller.ActiveSeconds);
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
            var state=HotpotSort.Determinism.CanonicalJson.Map(HotpotSort.Determinism.CanonicalJson.Parse(snapshot.CanonicalStateJson));
            var statistics=HotpotSort.Determinism.CanonicalJson.Map(state["statistics"]);
            view.SetProgress(HotpotSort.Determinism.CanonicalJson.Int(statistics["completedOrderCount"])/61f);
        }
        public void ShowLoading() { showingError=false; }
        public void ShowError(string code,string message) { showingError=true; view.ShowError(message); }
        public void ResetSession()
        {
            current=null; supplySequence=0; nextSupply=0;winRecordedSession=null;ResetSpawnPresentation();
            if(!showingError) { shown=new ViewSnapshot { phase=ViewPhase.Entry }; view.ResetView(); }
        }
        void IDisposable.Dispose() { /* View persists as the approved entry after session Exit. Bootstrap owns its lifetime. */ }
        public ViewSnapshot Read() { return shown; }
        public void SessionAction(ViewAction action)
        {
            switch(action)
            {
                case ViewAction.StartToday: actions?.Request(Contracts.SessionAction.StartToday);break;
                case ViewAction.Pause: actions?.Request(Contracts.SessionAction.Pause);break;
                case ViewAction.Resume: actions?.Request(Contracts.SessionAction.Resume);break;
                case ViewAction.RetrySameDay: actions?.Request(controller.Resolved==null?Contracts.SessionAction.StartToday:Contracts.SessionAction.Retry);break;
                case ViewAction.Exit: showingError=false; actions?.Request(Contracts.SessionAction.Exit); shown=new ViewSnapshot { phase=ViewPhase.Entry }; view.ResetView();break;
            }
        }
        private ulong Boundary => (ulong)Math.Max(0,Math.Floor(controller.ActiveSeconds*1000));
        private void ResetSpawnPresentation(){spawnMotions.Clear();successfulSpawns=0;spawnRandom=new System.Random(601377);}
        public void Tap(ViewTap command)
        {
            if(current==null || !controller.CanAcceptInput) { view.AcknowledgeTap(command.itemId);return; }
            int id;
            if(!int.TryParse(command.itemId,NumberStyles.None,CultureInfo.InvariantCulture,out id)) { view.AcknowledgeTap(command.itemId);return; }
            var result=current.Tap(new TapCommand(id,(ulong)command.inputSeq,Boundary,true));
            if(!result.Accepted)view.AcknowledgeTap(command.itemId);
        }
        public void ObserveSupply(ViewSupplyObservation observation)
        {
            if(current==null || !controller.CanAcceptInput)return;
            if(observation.snapshotRevision!=shown.revision)return;
            current.Supply(new SupplyObservation(++supplySequence,Boundary,observation.heightGateClear,controller.ActiveSeconds>=nextSupply));
        }
        private void Update()
        {
            if(controller==null)return;
            view?.SetRemainingTime(Math.Max(0,600-controller.ActiveSeconds));
            if(current==null || !controller.CanAcceptInput)return;
            if(controller.ActiveSeconds>=600){current.Timeout(Boundary);return;}
            if(controller.ActiveSeconds<nextSupply)return;
            int id=DailyViewMapper.PendingHead(current.Snapshot); if(id==0)return;
            float radius=DailyViewMapper.PlateRadius(current.PlateSize(id));
            view.ObserveSupply(new Vector2(DailyViewMapper.SpawnX(successfulSpawns),304+radius),radius);
            nextSupply=controller.ActiveSeconds+.2;
        }
        bool HasTarget(RewardKind kind)
        {
            if(current==null || !controller.CanAcceptInput)return false;
            switch(kind){case RewardKind.Hint:return view.FindClickableHint()!=null;case RewardKind.ClearBuffer:return current.CanClearBuffer;case RewardKind.FourthPot:return current.CanUnlockFourth;default:return view.World.TryShuffle(false);}
        }
        bool ApplyReward(RewardKind kind)
        {
            if(!HasTarget(kind))return false;
            switch(kind)
            {
                case RewardKind.Hint:var hint=view.FindClickableHint();if(hint==null)return false;view.HighlightItem(hint);return true;
                case RewardKind.ClearBuffer:return current.ClearBuffer(Boundary).Reason==null;
                case RewardKind.FourthPot:return current.UnlockFourth(Boundary).Reason==null;
                default:return view.World.TryShuffle(true);
            }
        }
        async void RequestReward(RewardKind kind,RewardRoute route)
        {
            if(!HasTarget(kind)){view.ShowNotice("暂无有效目标","当前没有可使用该道具的目标，不会领取奖励或扣除次数。");return;}
            var request=new RewardRequest(controller.Generation,kind,route,TimeResolver.ChallengeDay(DateTimeOffset.UtcNow));
            try
            {
                bool applied=await rewards.RequestAsync(request,()=>controller.Generation,()=>HasTarget(kind),()=>ApplyReward(kind),controller.SetRewardPaused);
                if(!applied && request.SessionGeneration==controller.Generation)view.ShowNotice("未领取奖励","取消、模拟失败、额度已用完或目标已失效；没有扣除分享次数。");
            }
            catch(Exception ex){Debug.LogException(ex);view.ShowNotice("模拟服务错误",ex.Message);}
        }
        void ShowSettings(){if(current!=null)actions.Request(Contracts.SessionAction.Pause);view.ShowSettings(profile.LoadSettings(),value=>{profile.SaveSettings(value);view.SetAudioSettings(value);});}
        async void ShowFriends()
        {
            try{var entries=await friends.LoadAsync();view.ShowNotice("好友榜 · 开发模拟",string.Join("\n",entries.Select(e=>e.DisplayName+"  "+e.FirstWins+" 次")));}
            catch(Exception ex){view.ShowNotice("好友榜暂不可用",ex.Message);}
        }
        async void ShareTheme()
        {
            try{await sharing.ShareThemeAsync(string.IsNullOrEmpty(approvedAssetRoot)?null:approvedAssetRoot+"/share/share_theme");}
            catch(Exception ex){view.ShowNotice("分享未完成",ex.Message);}
        }
    }
}
