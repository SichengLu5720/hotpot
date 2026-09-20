using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HotpotSort.Contracts
{
    // Transport identities are strings: no lossy JavaScript 64-bit conversion.
    public sealed class ChallengeContext
    {
        public string ChallengeId { get; }
        public string ContentVersion { get; }
        public string ConfigurationDigest { get; }
        public string TimeSource { get; }
        public int RetryIndex { get; }
        public ChallengeContext(string challengeId, string contentVersion, string configurationDigest,
            string timeSource, int retryIndex)
        {
            ChallengeId = Require(challengeId, nameof(challengeId));
            ContentVersion = Require(contentVersion, nameof(contentVersion));
            ConfigurationDigest = Require(configurationDigest, nameof(configurationDigest));
            TimeSource = Require(timeSource, nameof(timeSource));
            if (retryIndex < 0) throw new ArgumentOutOfRangeException(nameof(retryIndex));
            RetryIndex = retryIndex;
        }
        internal static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Required identity", name);
            return value;
        }
    }

    public enum GameStatus { Ready, Running, Paused, Won, Failed, Aborted }
    public enum SessionAction { StartToday, Pause, Resume, Retry, Exit }
    public enum PlatformLifecycle { Foreground, Background, Interrupted }

    public sealed class GameSnapshot
    {
        public string SessionId { get; }
        public ChallengeContext Challenge { get; }
        public GameStatus Status { get; }
        public string EventSequence { get; }
        public string TransactionId { get; }
        // A owns the versioned inventory/statistics schema. C passes it through unchanged.
        public string SchemaVersion { get; }
        public string CanonicalStateJson { get; }
        public GameSnapshot(string sessionId, ChallengeContext challenge, GameStatus status,
            string eventSequence, string transactionId, string schemaVersion, string canonicalStateJson)
        {
            SessionId = ChallengeContext.Require(sessionId, nameof(sessionId));
            Challenge = challenge ?? throw new ArgumentNullException(nameof(challenge));
            Status = status;
            EventSequence = ChallengeContext.Require(eventSequence, nameof(eventSequence));
            TransactionId = ChallengeContext.Require(transactionId, nameof(transactionId));
            SchemaVersion = ChallengeContext.Require(schemaVersion, nameof(schemaVersion));
            CanonicalStateJson = ChallengeContext.Require(canonicalStateJson, nameof(canonicalStateJson));
        }
    }

    public sealed class GameEventBatch
    {
        public string SessionId { get; }
        public string TransactionId { get; }
        public IReadOnlyList<string> CanonicalEvents { get; }
        public GameEventBatch(string sessionId, string transactionId, IEnumerable<string> canonicalEvents)
        {
            SessionId = ChallengeContext.Require(sessionId, nameof(sessionId));
            TransactionId = ChallengeContext.Require(transactionId, nameof(transactionId));
            if (canonicalEvents == null) throw new ArgumentNullException(nameof(canonicalEvents));
            CanonicalEvents = new ReadOnlyCollection<string>(new List<string>(canonicalEvents));
        }
    }

    public sealed class Viewport
    {
        public int Width { get; }
        public int Height { get; }
        // Pixel coordinates with bottom-left origin, matching Unity safeArea.
        public float SafeX { get; }
        public float SafeY { get; }
        public float SafeWidth { get; }
        public float SafeHeight { get; }
        public Viewport(int width, int height, float safeX, float safeY, float safeWidth, float safeHeight)
        {
            Width = width; Height = height; SafeX = safeX; SafeY = safeY;
            SafeWidth = safeWidth; SafeHeight = safeHeight;
        }
    }

    public interface IGameSession : IDisposable
    {
        GameSnapshot Snapshot { get; }
        event Action<GameSnapshot, GameEventBatch> Changed;
        void Pause();
        void Resume();
    }
    public interface IGameSessionFactory
    {
        // Each call creates a fresh authoritative instance; retry uses the supplied context.
        IGameSession CreateSession(ChallengeContext context);
    }
    public interface ISessionActions { void Request(SessionAction action); }
    public interface IGameView : IDisposable
    {
        void Bind(ISessionActions actions);
        void SetViewport(Viewport viewport);
        void Show(GameSnapshot snapshot, GameEventBatch events);
        void ShowLoading();
        void ShowError(string code, string message);
        // Destroy all previous session animation/physics instances before rebinding.
        void ResetSession();
    }
    public interface IGameViewFactory { IGameView CreateView(); }
}
