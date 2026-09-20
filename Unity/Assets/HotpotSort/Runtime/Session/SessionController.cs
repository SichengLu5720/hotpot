using System;
using System.Threading.Tasks;
using HotpotSort.Contracts;

namespace HotpotSort.Session
{
    [Flags] public enum PauseReasons { None = 0, User = 1, Background = 2 }
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
        private bool counting;
        public long Generation { get; private set; }
        public PauseReasons Pauses { get; private set; }
        public ResolvedChallenge Resolved { get; private set; }
        public GameSnapshot Snapshot { get; private set; }
        public Viewport CurrentViewport => viewport;
        public string Error { get; private set; }
        public bool IsBusy => busy;
        public double ActiveSeconds => accumulated + (counting ? Math.Max(0, clock.Seconds - runningSince) : 0);
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
        private void Create(ChallengeContext context)
        {
            accumulated = 0; counting = false;
            view = views.CreateView() ?? throw new InvalidOperationException("view-factory-returned-null");
            view.Bind(this); if (viewport != null) view.SetViewport(viewport); view.ShowLoading();
            session = core.CreateSession(context) ?? throw new InvalidOperationException("core-factory-returned-null");
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
            StopClock(); Snapshot = snapshot;
            if (snapshot.Status == GameStatus.Running && Pauses == PauseReasons.None)
            { runningSince = clock.Seconds; counting = true; }
            view.Show(snapshot, events); Notify();
        }
        private void StopClock()
        {
            if (counting) accumulated += Math.Max(0, clock.Seconds - runningSince);
            counting = false;
        }
        private void SetPause(PauseReasons reason, bool enabled)
        {
            var next = enabled ? Pauses | reason : Pauses & ~reason;
            if (next == Pauses) return;
            StopClock(); Pauses = next;
            try { ReconcilePause(); }
            catch (Exception ex) { Fail(ex); }
            Notify();
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
        public void Retry()
        {
            if (disposed || busy || Resolved == null) return;
            busy = true; Error = null;
            var previous = Resolved;
            var c = previous.Context;
            try
            {
                Release();
                Pauses &= PauseReasons.Background;
                var retry = new ChallengeContext(c.ChallengeId, c.ContentVersion, c.ConfigurationDigest, c.TimeSource, checked(c.RetryIndex + 1));
                Resolved = new ResolvedChallenge(retry, previous.ResolvedUtc, previous.FallbackReason);
                Create(retry);
            }
            catch (Exception ex) { Fail(ex); }
            finally { busy = false; Notify(); }
        }
        public void Exit()
        {
            if (disposed) return;
            try { Release(); } catch (Exception ex) { Error = ex.GetType().Name + ": " + ex.Message; }
            Resolved = null; Snapshot = null; busy = false; accumulated = 0;
            Pauses &= PauseReasons.Background; Notify();
        }
        private void Release()
        {
            ++Generation; StopClock();
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
