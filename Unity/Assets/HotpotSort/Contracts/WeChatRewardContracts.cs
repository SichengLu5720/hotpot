using System;

namespace HotpotSort.Contracts
{
    // Optional read-only capability for UI; never reserves quota or starts an ad.
    public interface IRewardChannelAvailability
    {
        bool IsRewardedVideoAvailable { get; }
    }

    // Called by the coordinator before replacing/exiting a session. Does not apply rewards.
    public interface IRewardRequestCancellation
    {
        void CancelRequest(string requestId);
        void CancelAll();
    }

    public interface IWeChatRewardVideo : IDisposable
    {
        event Action Loaded;
        event Action Failed;
        event Action<bool?> Closed;
        void Load();
        void Show();
    }

    // All callbacks and methods run on the Unity main thread. Post runs in a later frame.
    public interface IWeChatRewardRuntime
    {
        bool Available { get; }
        double MonotonicSeconds { get; }
        event Action Hidden;
        event Action Shown;
        event Action Tick;
        event Action Stopping;
        void Post(Action action);
        void Share(string title, string packagedImagePath);
        void RegisterMenu(string title, string packagedImagePath);
        IWeChatRewardVideo CreateVideo(string adUnitId);
    }
}
