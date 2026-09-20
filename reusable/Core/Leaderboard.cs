// Extracted from TASK-008. No Unity, WinForms or network dependencies.
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Harness.Reusable
{
    public enum LeaderboardState { Loading, Ready, Empty, Failure }

    public sealed class ScoreEntry
    {
        public string Id { get; }
        public string Name { get; }
        public long Score { get; }
        public long AchievedAtMs { get; }
        public ScoreEntry(string id, string name, long score, long achievedAtMs)
        {
            if (string.IsNullOrWhiteSpace(id) || score < 0 || achievedAtMs <= 0)
                throw new ArgumentException("ID, nonnegative score and positive achievement timestamp required.");
            Id = id; Name = string.IsNullOrWhiteSpace(name) ? "玩家" : name;
            Score = score; AchievedAtMs = achievedAtMs;
        }
    }

    public sealed class RankedEntry
    {
        public ScoreEntry Entry { get; }
        public int Rank { get; }
        public bool IsSelf { get; }
        internal RankedEntry(ScoreEntry entry, int rank, bool self)
        { Entry = entry; Rank = rank; IsSelf = self; }
    }

    public sealed class Ranking
    {
        public IReadOnlyList<RankedEntry> Rows { get; }
        public RankedEntry Self { get; }
        public bool HasFriends { get; }
        internal Ranking(List<RankedEntry> rows)
        { Rows = rows.AsReadOnly(); Self = rows.Find(r => r.IsSelf); HasFriends = rows.Any(r => !r.IsSelf); }
    }

    public static class LeaderboardRules
    {
        // Higher score wins; a replay never changes first-achievement time.
        public static ScoreEntry Merge(ScoreEntry saved, ScoreEntry incoming)
        {
            if (incoming == null) throw new ArgumentNullException(nameof(incoming));
            if (saved == null) return incoming;
            if (saved.Id != incoming.Id) throw new ArgumentException("Cannot merge different identities.");
            if (incoming.Score <= saved.Score) return saved;
            return new ScoreEntry(saved.Id, incoming.Name, incoming.Score,
                Math.Max(incoming.AchievedAtMs, checked(saved.AchievedAtMs + 1)));
        }

        public static Ranking Rank(IEnumerable<ScoreEntry> entries, string selfId)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            var rows = entries.ToList();
            if (rows.Any(r => r == null) || rows.Select(r => r.Id).Distinct(StringComparer.Ordinal).Count() != rows.Count)
                throw new ArgumentException("Ranking requires non-null entries and unique stable IDs.");
            rows.Sort((a, b) => {
                int n = b.Score.CompareTo(a.Score);
                if (n == 0) n = a.AchievedAtMs.CompareTo(b.AchievedAtMs);
                return n == 0 ? string.CompareOrdinal(a.Id, b.Id) : n;
            });
            return new Ranking(rows.Select((r, i) => new RankedEntry(r, i + 1, r.Id == selfId)).ToList());
        }
    }

    // Implement this single method for your own backend. Never expose WeChat friend
    // records through this interface: use the included open-data-domain adapter there.
    public interface ILeaderboardProvider
    {
        Task<IReadOnlyList<ScoreEntry>> LoadAsync(CancellationToken cancellation);
    }

    public sealed class LeaderboardController : IDisposable
    {
        private readonly ILeaderboardProvider provider;
        private readonly string selfId;
        private readonly TimeSpan timeout;
        private CancellationTokenSource active;
        private int generation;
        private bool disposed;
        public LeaderboardState State { get; private set; } = LeaderboardState.Empty;
        public Ranking Current { get; private set; }
        public string Error { get; private set; } = "";
        public event Action Changed;

        public LeaderboardController(ILeaderboardProvider provider, string selfId, TimeSpan? timeout = null)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.selfId = selfId;
            this.timeout = timeout ?? TimeSpan.FromSeconds(10);
            if (this.timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        public async Task RefreshAsync()
        {
            if (disposed) throw new ObjectDisposedException(nameof(LeaderboardController));
            int request = ++generation;
            active?.Cancel();
            var cancellation = new CancellationTokenSource(); active = cancellation;
            State = LeaderboardState.Loading; Error = ""; Current = null; Changed?.Invoke();
            try
            {
                var load = provider.LoadAsync(cancellation.Token);
                // Observe even a late provider failure after the timeout.
                _ = load.ContinueWith(t => { var ignored = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                if (await Task.WhenAny(load, Task.Delay(timeout, cancellation.Token)) != load)
                    throw new TimeoutException("加载超时，请重试");
                var entries = await load;
                if (disposed || request != generation) return;
                Current = LeaderboardRules.Rank(entries, selfId);
                State = Current.HasFriends ? LeaderboardState.Ready : LeaderboardState.Empty;
            }
            catch (Exception e)
            {
                if (disposed || request != generation) return;
                Error = e.Message; State = LeaderboardState.Failure;
            }
            finally
            {
                cancellation.Cancel(); cancellation.Dispose();
                if (request == generation) active = null;
            }
            if (!disposed && request == generation) Changed?.Invoke();
        }
        public void Dispose() { disposed = true; generation++; active?.Cancel(); active = null; }
    }
}
