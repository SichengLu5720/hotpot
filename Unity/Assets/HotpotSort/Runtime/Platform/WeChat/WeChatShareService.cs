using System;
using System.Threading.Tasks;
using HotpotSort.Contracts;

namespace HotpotSort.Platform
{
    public sealed class WeChatShareService : IResultThemeShare, IRewardRequestCancellation, IDisposable
    {
        readonly IWeChatRewardRuntime runtime;
        readonly WeChatRuntimeConfig config;
        Pending active;
        bool disposed, menuRegistered;
        sealed class Pending
        {
            public string Id;
            public double Deadline;
            public bool Calling, CallSucceeded, Hidden, Shown, Queued;
            public Action Hide, Show, Tick;
            public readonly TaskCompletionSource<RewardOutcome> Completion = new TaskCompletionSource<RewardOutcome>();
        }
        public bool IsDevelopmentSimulation => false;
        public WeChatShareService(WeChatRuntimeConfig config, IWeChatRewardRuntime runtime)
        { this.config=config??throw new ArgumentNullException(nameof(config)); this.runtime=runtime??throw new ArgumentNullException(nameof(runtime)); runtime.Stopping+=Dispose; }
        bool Available => !disposed && runtime.Available && config.ShareState==WeChatCapabilityState.Ready;

        public bool RegisterMenu()
        {
            if (!Available) return false;
            if (menuRegistered) return true;
            try { runtime.RegisterMenu(config.shareTitle,config.shareImagePath); menuRegistered=true; return true; }
            catch { return false; }
        }

        // Ordinary sharing creates no reward request or lifecycle listener.
        public Task<RewardOutcome> ShareThemeAsync(string resourceAddress)=>ShareThemeAsync(resourceAddress,config.shareTitle);
        public Task<RewardOutcome> ShareThemeAsync(string resourceAddress,string title)
        {
            if (!Available || active!=null) return Task.FromResult(RewardOutcome.Unavailable);
            // Success means the native share sheet was requested, not proof of sending.
            try { runtime.Share(string.IsNullOrEmpty(title)?config.shareTitle:title,config.shareImagePath); return Task.FromResult(RewardOutcome.Success); }
            catch { return Task.FromResult(RewardOutcome.Failed); }
        }

        public Task<RewardOutcome> RequestRewardShareAsync(string requestId)
        {
            if (string.IsNullOrEmpty(requestId)) throw new ArgumentException("Reward request ID required",nameof(requestId));
            if (!Available || active!=null) return Task.FromResult(RewardOutcome.Unavailable);
            var p=new Pending { Id=requestId, Deadline=runtime.MonotonicSeconds+10 };
            p.Hide=()=>OnHide(p); p.Show=()=>OnShow(p); p.Tick=()=>OnTick(p);
            active=p;
            try
            {
                runtime.Hidden+=p.Hide; runtime.Shown+=p.Show; runtime.Tick+=p.Tick;
                p.Calling=true;
                runtime.Share(config.shareTitle,config.shareImagePath);
                p.Calling=false; p.CallSucceeded=true;
                if (p.Hidden && p.Shown) QueueSuccess(p);
            }
            catch { p.Calling=false; Complete(p,RewardOutcome.Failed); }
            return p.Completion.Task;
        }
        void OnHide(Pending p)
        {
            if (active!=p || p.Hidden || (!p.Calling&&!p.CallSucceeded)) return;
            if (runtime.MonotonicSeconds>=p.Deadline) { Complete(p,RewardOutcome.Failed); return; }
            p.Hidden=true;
        }
        void OnShow(Pending p)
        {
            if (active!=p || !p.Hidden) return;
            p.Shown=true;
            if(p.CallSucceeded) QueueSuccess(p);
        }
        void OnTick(Pending p)
        {
            if(active==p&&!p.Hidden&&runtime.MonotonicSeconds>=p.Deadline) Complete(p,RewardOutcome.Failed);
        }
        void QueueSuccess(Pending p)
        {
            if(active!=p||p.Queued) return;
            p.Queued=true;
            runtime.Post(()=> { if(active==p) Complete(p,RewardOutcome.Success); });
        }
        void Complete(Pending p,RewardOutcome outcome)
        {
            if(active!=p) return;
            active=null;
            runtime.Hidden-=p.Hide; runtime.Shown-=p.Show; runtime.Tick-=p.Tick;
            p.Completion.TrySetResult(outcome);
        }
        public void CancelRequest(string requestId) { if(active!=null&&active.Id==requestId) Complete(active,RewardOutcome.Cancelled); }
        public void CancelAll() { if(active!=null) Complete(active,RewardOutcome.Cancelled); }
        public void Dispose() { if(disposed)return; disposed=true; runtime.Stopping-=Dispose; CancelAll(); }
    }
}
