using System;
using System.Threading.Tasks;
using HotpotSort.Contracts;

namespace HotpotSort.Session
{
    [Flags] public enum PauseReasons { None = 0, User = 1, Background = 2, Reward = 4, Revival = 8, Tutorial = 16 }
    public interface IPlatformLifecycleAdapter : IDisposable
    {
        event Action<PlatformLifecycle> Changed;
        event Action<Viewport> ViewportChanged;
        void Start();
    }
    public sealed class SessionController : ISessionActions, IDisposable
    {
        private readonly IGameSessionFactory core;
        private readonly IGameViewFactory views;
        private readonly TimeResolver time;
        private readonly IMonotonicClock clock;
        private readonly IPlatformLifecycleAdapter platform;
        private readonly string contentVersion, digest;
        private IGameSession session;
        private IGameView view;
        private Action<GameSnapshot, GameEventBatch> listener;
        private Viewport viewport;
        private bool disposed, busy;
        private double accumulated, runningSince;
        private double challengeAccumulated, challengeRunningSince;
        private bool counting, challengeCounting, challengeTimerStarted;
        public long Generation { get; private set; }
        public PauseReasons Pauses { get; private set; }
        public ResolvedChallenge Resolved { get; private set; }
        public GameSnapshot Snapshot { get; private set; }
        public Viewport CurrentViewport => viewport;
        public string Error { get; private set; }
        public bool IsBusy => busy;
        public double ActiveSeconds => accumulated + (counting ? Math.Max(0, clock.Seconds - runningSince) : 0);
        public double ChallengeSeconds => challengeAccumulated + (challengeCounting ? Math.Max(0, clock.Seconds - challengeRunningSince) : 0);
        public bool ChallengeTimerStarted => challengeTimerStarted;
        public ChallengeStage Stage => (session as IChallengeStageState)?.Stage??ChallengeStage.Legacy;
        public bool CanAcceptInput => !disposed && !busy && Pauses == PauseReasons.None && Snapshot?.Status == GameStatus.Running;
        public event Action ObservationChanged;

        public SessionController(IGameSessionFactory core, IGameViewFactory views, TimeResolver time,
            IMonotonicClock clock, IPlatformLifecycleAdapter platform, string contentVersion, string digest)
        {
            this.core = core ?? throw new ArgumentNullException(nameof(core));
            this.views = views ?? throw new ArgumentNullException(nameof(views));
            this.time = time ?? throw new ArgumentNullException(nameof(time));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.platform = platform ?? throw new ArgumentNullException(nameof(platform));
            this.contentVersion = contentVersion; this.digest = digest;
            platform.Changed += OnLifecycle;
            platform.ViewportChanged += OnViewport;
            try { platform.Start(); }
            catch { platform.Changed -= OnLifecycle; platform.ViewportChanged -= OnViewport; platform.Dispose(); throw; }
        }
        public async Task StartTodayAsync()
        {
            if (disposed || busy || session != null) return;
            busy = true; Error = null;
            var generation = ++Generation;
            try
            {
                var resolved = await time.ResolveAsync(contentVersion, digest);
                if (disposed || generation != Generation) return;
                Resolved = resolved;
                Create(resolved.Context);
            }
            catch (Exception ex) { if (!disposed && generation == Generation) Fail(ex); }
            finally { if (generation == Generation) { busy = false; Notify(); } }
        }
        public void Request(SessionAction action)
        {
            if (disposed) return;
            switch (action)
            {
                case SessionAction.StartToday: _ = StartTodayAsync(); break;
                case SessionAction.Pause: SetPause(PauseReasons.User, true); break;
                case SessionAction.Resume: SetPause(PauseReasons.User, false); break;
                case SessionAction.Retry: Retry(); break;
                case SessionAction.Exit: Exit(); break;
            }
        }
        private void Create(ChallengeContext context, ChallengeStage stage=ChallengeStage.Warmup,int inheritedPotMask=0)
        {
            accumulated = challengeAccumulated = 0;
            counting = challengeCounting = challengeTimerStarted = false;
            view = views.CreateView() ?? throw new InvalidOperationException("view-factory-returned-null");
            view.Bind(this); if (viewport != null) view.SetViewport(viewport); view.ShowLoading();
            session = (core is IChallengeStageFactory staged?staged.CreateStage(context,stage,inheritedPotMask):core.CreateSession(context)) ?? throw new InvalidOperationException("core-factory-returned-null");
            var expected = session; var generation = Generation;
            listener = (snapshot, events) =>
            {
                if (disposed || generation != Generation || !ReferenceEquals(expected, session)) return;
                try { Apply(snapshot, events); } catch (Exception ex) { Fail(ex); }
            };
            session.Changed += listener;
            Apply(session.Snapshot, null);
            ReconcilePause();
        }
        private void Apply(GameSnapshot snapshot, GameEventBatch events)
        {
            if (snapshot == null) throw new InvalidOperationException("core-snapshot-null");
            StopClocks(); Snapshot = snapshot;
            // Core has already paused; latch the reason without reentering its transaction.
            if(session is IRevivalSessionState revival && revival.RevivalPending)Pauses|=PauseReasons.Revival;
            if (snapshot.Status == GameStatus.Running && Pauses == PauseReasons.None)
            {
                runningSince = clock.Seconds; counting = true;
                if(challengeTimerStarted){challengeRunningSince=runningSince;challengeCounting=true;}
            }
            view.Show(snapshot, events); Notify();
        }
        private void StopClocks()
        {
            var now=clock.Seconds;
            if (counting) accumulated += Math.Max(0, now - runningSince);
            if (challengeCounting) challengeAccumulated += Math.Max(0, now - challengeRunningSince);
            counting = false;
            challengeCounting = false;
        }
        public bool StartChallengeTimer()
        {
            if(disposed||challengeTimerStarted||session==null||Stage==ChallengeStage.Warmup)return false;
            challengeTimerStarted=true;
            if(Snapshot?.Status==GameStatus.Running&&Pauses==PauseReasons.None)
            {challengeRunningSince=clock.Seconds;challengeCounting=true;}
            Notify();return true;
        }
        private void SetPause(PauseReasons reason, bool enabled)
        {
            var next = enabled ? Pauses | reason : Pauses & ~reason;
            if (next == Pauses) return;
            StopClocks(); Pauses = next;
            try { ReconcilePause(); }
            catch (Exception ex) { Fail(ex); }
            Notify();
        }
        public void SetRewardPaused(bool value) => SetPause(PauseReasons.Reward, value);
        public void SetTutorialPaused(bool value) => SetPause(PauseReasons.Tutorial,value);
        public bool ContinueToFormal(string sessionId,long generation)
        {
            if(disposed||busy||generation!=Generation||Snapshot?.SessionId!=sessionId||Snapshot.Status!=GameStatus.Won||Stage!=ChallengeStage.Warmup||Pauses!=PauseReasons.None)return false;
            int mask=((IChallengeStageState)session).UnlockedExtraPotMask;var context=Resolved.Context;
            busy=true;
            try{Release();Create(context,ChallengeStage.Formal,mask);return true;}
            catch(Exception ex){Fail(ex);return false;}
            finally{busy=false;Notify();}
        }
        public void SetRevivalPaused(bool value)
        {
            if(!value && session is IRevivalSessionState revival && revival.RevivalPending)return;
            SetPause(PauseReasons.Revival,value);
        }
        private void ReconcilePause()
        {
            if (session == null || Snapshot == null) return;
            if (Pauses != PauseReasons.None && Snapshot.Status == GameStatus.Running) session.Pause();
            else if (Pauses == PauseReasons.None && Snapshot.Status == GameStatus.Paused) session.Resume();
            Apply(session.Snapshot, null);
        }
        private void OnLifecycle(PlatformLifecycle state)
        { SetPause(PauseReasons.Background, state != PlatformLifecycle.Foreground); }
        private void OnViewport(Viewport value) { viewport = value; view?.SetViewport(value); Notify(); }
        public void Retry() { _ = RetryAsync(); }
        public async Task RetryAsync()
        {
            if (disposed || busy || Resolved == null) return;
            busy = true; Error = null;
            var previous = Resolved;
            var c = previous.Context;
            int retryMask=Stage==ChallengeStage.Warmup?((IChallengeStageState)session).UnlockedExtraPotMask:0;
            long generation=0;
            try
            {
                Release();
                generation=Generation;
                Pauses &= PauseReasons.Background;
                var fresh=await time.ResolveAsync(contentVersion,digest);
                if(disposed || Generation!=generation)return;
                var retry = new ChallengeContext(fresh.Context.ChallengeId, c.ContentVersion, c.ConfigurationDigest, fresh.Context.TimeSource, fresh.Context.ChallengeId==c.ChallengeId?checked(c.RetryIndex+1):0);
                Resolved = new ResolvedChallenge(retry, fresh.ResolvedUtc, fresh.FallbackReason);
                Create(retry,ChallengeStage.Warmup,fresh.Context.ChallengeId==c.ChallengeId?retryMask:0);
            }
            catch (Exception ex) { if(!disposed && generation==Generation)Fail(ex); }
            finally { if(generation==Generation){busy = false; Notify();} }
        }
        public void Exit()
        {
            if (disposed) return;
            try { Release(); } catch (Exception ex) { Error = ex.GetType().Name + ": " + ex.Message; }
            Resolved = null; Snapshot = null; busy = false;
            accumulated = challengeAccumulated = 0;
            counting = challengeCounting = challengeTimerStarted = false;
            Pauses &= PauseReasons.Background; Notify();
        }
        private void Release()
        {
            ++Generation; StopClocks();
            var old = session; var oldView = view; var oldListener = listener;
            session = null; view = null; listener = null; Snapshot = null;
            if (old != null) old.Changed -= oldListener;
            try { old?.Dispose(); }
            finally { try { oldView?.ResetSession(); } finally { oldView?.Dispose(); } }
        }
        private void Fail(Exception ex)
        {
            busy = false;
            Error = ex.GetType().Name + ": " + ex.Message;
            try { view?.ShowError("session-error", Error); }
            catch (Exception displayError) { Error += "; display: " + displayError.GetType().Name; }
            finally
            {
                try { Release(); }
                catch (Exception cleanupError) { Error += "; cleanup: " + cleanupError.GetType().Name; }
            }
            Notify();
        }
        private void Notify() { ObservationChanged?.Invoke(); }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            platform.Changed -= OnLifecycle; platform.ViewportChanged -= OnViewport;
            try { platform.Dispose(); } finally { Release(); }
        }
    }
}
