using System;
using System.Globalization;
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
        [SerializeField] private float supplyInterval = .2f;
        private const string ContentDigest="e6612cb54b548b81aabef9bc376441682ff0157b556b5a6258cba4c7057e5d08";
        private DailySessionFactory factory;
        private DailySession current;
        private SessionController controller;
        private GameplayView view;
        private ISessionActions actions;
        private IDiagnosticSink diagnostics;
        private bool showingError;
        private ulong supplySequence;
        private double nextSupply;
        private Viewport appliedViewport;
        private ViewSnapshot shown=new ViewSnapshot { phase=ViewPhase.Entry };
        public event Action<ViewUpdate> Updated;
        public override string ContentVersion { get { EnsureFactory(); return factory.Content.ContentVersion; } }
        public override string ConfigurationDigest { get { EnsureFactory(); return factory.ConfigurationDigest; } }
        public override string BuildIdentity => "v0.1.0";
        public override string RuntimeNamespace => "hotpot-v0-1-0";
        public override IGameSessionFactory CoreFactory { get { EnsureFactory(); return this; } }
        public override IGameViewFactory ViewFactory => this;
        public override bool UsesDevelopmentDoubles => false;
        public GameplayView PlayerView => view;
        public DailySession ActiveCore => current;
        private void EnsureFactory()
        {
            if(factory!=null)return;
            if(!dailyContent)throw new InvalidOperationException("Daily content scene binding is missing");
            var content=DailyContent.Load(dailyContent.text,ContentDigest);
            var catalog=Enumerable.Range(0,16).Select(id=>new IngredientEntry("food_"+id.ToString("00"),"Hotpot/food_"+id.ToString("00"),"food_"+id.ToString("00"),"circle-radius-18-board-units","standard",content.ContentVersion));
            factory=new DailySessionFactory(content,catalog);
        }
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
            EnsureFactory(); showingError=false; supplySequence=0; nextSupply=0;
            current=factory.CreateDailySession(context); return current;
        }
        public IGameView CreateView() { return this; }
        void IGameView.Bind(ISessionActions value) { actions=value; }
        public void SetViewport(Viewport value) { appliedViewport=value; view.SetViewport(new Rect(value.SafeX,value.SafeY,value.SafeWidth,value.SafeHeight)); }
        public void Show(GameSnapshot snapshot,GameEventBatch events)
        {
            showingError=false;
            var update=DailyViewMapper.Map(snapshot,events,factory.Content,controller.ActiveSeconds);
            shown=update.snapshot; Updated?.Invoke(update);
        }
        public void ShowLoading() { showingError=false; }
        public void ShowError(string code,string message) { showingError=true; view.ShowError(message); }
        public void ResetSession()
        {
            current=null; supplySequence=0; nextSupply=0;
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
            current.Supply(new SupplyObservation(++supplySequence,Boundary,observation.spaceAvailable,controller.ActiveSeconds>=nextSupply));
        }
        private void Update()
        {
            if(controller==null || current==null || !controller.CanAcceptInput || controller.ActiveSeconds<nextSupply)return;
            int id=DailyViewMapper.PendingHead(current.Snapshot); if(id==0)return;
            float radius=DailyViewMapper.PlateRadius(factory.Content.Plates[id-1].Kinds.Count);
            view.ObserveSupply(new Vector2(DailyViewMapper.SpawnX(id),304+radius),radius);
            nextSupply=controller.ActiveSeconds+Math.Max(.01f,supplyInterval);
        }
    }
}
