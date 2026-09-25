using System;
using System.Threading.Tasks;
using HotpotSort.Contracts;

namespace HotpotSort.Platform
{
    public sealed class WeChatRewardService : IRewardService, IRewardRequestCancellation, IRewardChannelAvailability, IDisposable
    {
        readonly WeChatRuntimeConfig config;
        readonly IWeChatRewardRuntime runtime;
        readonly WeChatShareService share;
        Pending active;
        bool disposed;
        sealed class Pending
        {
            public string Id;
            public double Deadline;
            public bool Showing, Queued;
            public IWeChatRewardVideo Video;
            public Action Loaded, Failed, Tick;
            public Action<bool?> Closed;
            public readonly TaskCompletionSource<RewardOutcome> Completion=new TaskCompletionSource<RewardOutcome>();
        }
        public bool IsDevelopmentSimulation=>false;
        public bool IsRewardedVideoAvailable=>!disposed&&runtime.Available&&config.RewardedVideoState==WeChatCapabilityState.Ready;
        public WeChatRewardService(WeChatRuntimeConfig config,IWeChatRewardRuntime runtime,WeChatShareService share)
        { this.config=config??throw new ArgumentNullException(nameof(config)); this.runtime=runtime??throw new ArgumentNullException(nameof(runtime)); this.share=share??throw new ArgumentNullException(nameof(share)); runtime.Stopping+=Dispose; }
        public async Task<RewardOutcome> RequestAsync(RewardRequest request)
        {
            if(request==null)throw new ArgumentNullException(nameof(request));
            if(disposed||!runtime.Available||active!=null)return RewardOutcome.Unavailable;
            if(request.Route==RewardRoute.WeChatShare)
            {
                if(request.Kind==RewardKind.ThirdPot||request.Kind==RewardKind.FourthPot)return RewardOutcome.Unavailable;
                var p=new Pending{Id=request.RequestId}; active=p;
                try{return await share.RequestRewardShareAsync(request.RequestId);}
                finally{if(active==p)active=null;}
            }
            if(request.Route!=RewardRoute.WeChatRewardedVideo||!IsRewardedVideoAvailable)return RewardOutcome.Unavailable;
            var video=new Pending{Id=request.RequestId,Deadline=runtime.MonotonicSeconds+15}; active=video;
            try
            {
                video.Video=runtime.CreateVideo(config.rewardedAdUnitId);
                if(video.Video==null){Complete(video,RewardOutcome.Unavailable);return await video.Completion.Task;}
                video.Loaded=()=>OnLoaded(video);
                video.Failed=()=>QueueOutcome(video,RewardOutcome.Failed);
                video.Closed=ended=> { if(video.Showing)QueueOutcome(video,ended==true?RewardOutcome.Success:RewardOutcome.Failed); };
                video.Tick=()=> { if(active==video&&!video.Showing&&runtime.MonotonicSeconds>=video.Deadline)Complete(video,RewardOutcome.Failed); };
                video.Video.Loaded+=video.Loaded; video.Video.Failed+=video.Failed; video.Video.Closed+=video.Closed; runtime.Tick+=video.Tick;
                video.Video.Load();
            }
            catch { Complete(video,RewardOutcome.Failed); }
            return await video.Completion.Task;
        }
        void OnLoaded(Pending p)
        {
            if(active!=p||p.Showing||p.Queued)return;
            if(runtime.MonotonicSeconds>=p.Deadline){Complete(p,RewardOutcome.Failed);return;}
            p.Showing=true;
            try {p.Video.Show();} catch {Complete(p,RewardOutcome.Failed);}
        }
        void QueueOutcome(Pending p,RewardOutcome outcome)
        {
            if(active!=p||p.Queued)return;
            p.Queued=true;
            runtime.Post(()=> { if(active==p)Complete(p,outcome); });
        }
        void Complete(Pending p,RewardOutcome outcome)
        {
            if(active!=p)return;
            active=null;
            if(p.Tick!=null)runtime.Tick-=p.Tick;
            if(p.Video!=null)
            {
                p.Video.Loaded-=p.Loaded;p.Video.Failed-=p.Failed;p.Video.Closed-=p.Closed;
                try {p.Video.Dispose();} catch { /* Still complete the waiter if native destruction fails. */ }
            }
            p.Completion.TrySetResult(outcome);
        }
        public void CancelRequest(string requestId)
        {
            if(active==null||active.Id!=requestId)return;
            var p=active;
            share.CancelRequest(requestId);
            if(active==p)Complete(p,RewardOutcome.Cancelled);
        }
        public void CancelAll(){if(active!=null)CancelRequest(active.Id);}
        public void Dispose(){if(disposed)return;disposed=true;runtime.Stopping-=Dispose;CancelAll();}
    }
}
