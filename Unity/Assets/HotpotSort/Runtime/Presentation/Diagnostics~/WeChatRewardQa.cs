using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Platform;

static class WeChatRewardQa
{
    static int checks;
    static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;}
    static void Outcome(Task<RewardOutcome> task,RewardOutcome expected,string name)=>Check(task.IsCompletedSuccessfully&&task.Result==expected,name);
    static RewardRequest Request(RewardRoute route,RewardKind kind=RewardKind.Hint)=>new RewardRequest(1,kind,route,"2026-09-22");
    static WeChatRuntimeConfig Config()=>new WeChatRuntimeConfig{rewardedAdUnitId="ad-test"};
    static int Main()
    {
        try {ShareMatrix();VideoMatrix();Console.WriteLine("WECHAT_REWARD_QA_PASS checks="+checks);return 0;}
        catch(Exception e){Console.Error.WriteLine("WECHAT_REWARD_QA_FAIL "+e);return 1;}
    }
    static void ShareMatrix()
    {
        var r=new Runtime();var s=new WeChatShareService(Config(),r);
        Check(s.RegisterMenu()&&s.RegisterMenu()&&r.Menus==1,"menu registered once");
        Outcome(s.ShareThemeAsync("ignored"),RewardOutcome.Success,"ordinary share launch");
        Check(r.Listeners==0,"ordinary share has no reward listeners");
        r.Hide();r.Show();
        var task=s.RequestRewardShareAsync("one");Check(r.Listeners==3,"listeners installed before call");
        r.Show();r.Flush();Check(!task.IsCompleted,"show before hide ignored");
        Outcome(s.RequestRewardShareAsync("two"),RewardOutcome.Unavailable,"share request serialized");
        r.Seconds=9.999;r.Hide();r.Seconds=900;r.Advance();Check(!task.IsCompleted,"background has no watchdog");
        r.Show();r.Show();Check(!task.IsCompleted,"show completion is deferred");r.Flush();
        Outcome(task,RewardOutcome.Success,"hide show success");Check(r.Listeners==0,"success detaches");
        task=s.RequestRewardShareAsync("timeout");r.Seconds=910;r.Hide();Outcome(task,RewardOutcome.Failed,"hide exactly at deadline fails");
        task=s.RequestRewardShareAsync("tick-timeout");r.Seconds=920;r.Advance();Outcome(task,RewardOutcome.Failed,"tick watchdog");
        r.ShareAction=()=>{r.Hide();r.Show();throw new Exception("sync");};
        task=s.RequestRewardShareAsync("sync-error");r.Flush();Outcome(task,RewardOutcome.Failed,"synchronous error wins over callbacks");
        r.ShareAction=()=>{r.Hide();r.Show();};task=s.RequestRewardShareAsync("sync-success");r.Flush();Outcome(task,RewardOutcome.Success,"synchronous lifecycle sequence");
        r.ShareAction=null;task=s.RequestRewardShareAsync("cancel");var staleHide=r.HiddenCopy;var staleShow=r.ShownCopy;
        s.CancelRequest("wrong");Check(!task.IsCompleted,"wrong cancel ID ignored");s.CancelRequest("cancel");Outcome(task,RewardOutcome.Cancelled,"cancel completes task");
        task=s.RequestRewardShareAsync("new");staleHide();staleShow();r.Flush();Check(!task.IsCompleted,"old request events ignored");
        r.Hide();r.Show();s.CancelAll();r.Flush();Outcome(task,RewardOutcome.Cancelled,"cancel beats pending success");
        task=s.RequestRewardShareAsync("dispose");s.Dispose();Outcome(task,RewardOutcome.Cancelled,"dispose completes");Check(r.Listeners==0,"dispose detaches");
        Outcome(s.RequestRewardShareAsync("late"),RewardOutcome.Unavailable,"disposed unavailable");
        var config=Config();config.enableShare=false;s=new WeChatShareService(config,r);Outcome(s.RequestRewardShareAsync("disabled"),RewardOutcome.Unavailable,"share disabled");
        config.enableShare=true;r.Available=false;Outcome(s.RequestRewardShareAsync("no-platform"),RewardOutcome.Unavailable,"share requires platform");
    }
    static void VideoMatrix()
    {
        var r=new Runtime();var config=Config();var share=new WeChatShareService(config,r);var service=new WeChatRewardService(config,r,share);
        config.rewardedAdUnitId="";Check(!service.IsRewardedVideoAvailable,"missing ID exposed to UI");Outcome(service.RequestAsync(Request(RewardRoute.WeChatRewardedVideo)),RewardOutcome.Unavailable,"missing ID");Check(r.Created==0,"no native instance for missing ID");
        config.rewardedAdUnitId="ad-test";config.enableRewardedVideo=false;Outcome(service.RequestAsync(Request(RewardRoute.WeChatRewardedVideo)),RewardOutcome.Unavailable,"ad disabled");config.enableRewardedVideo=true;
        Check(service.IsRewardedVideoAvailable,"configured video exposed to UI");r.Available=false;Check(!service.IsRewardedVideoAvailable,"platform unavailable exposed to UI");r.Available=true;
        foreach(bool? ended in new bool?[]{true,false,null})
        {
            var req=Request(RewardRoute.WeChatRewardedVideo);var task=service.RequestAsync(req);var video=r.Video;
            Check(video.Loads==1,"load started");video.Close(true);r.Flush();Check(!task.IsCompleted,"close before show ignored");
            video.LoadedNow();video.LoadedNow();Check(video.Shows==1,"duplicate load ignored");
            r.Seconds+=100;r.Advance();Check(!task.IsCompleted,"watching no watchdog");
            video.Close(ended);video.Close(true);r.Flush();Outcome(task,ended==true?RewardOutcome.Success:RewardOutcome.Failed,"explicit ended only");Check(video.Disposals==1&&video.Listeners==0&&r.Listeners==0,"native listener cleanup");
        }
        var timeout=service.RequestAsync(Request(RewardRoute.WeChatRewardedVideo));r.Seconds+=15;r.Video.LoadedNow();Outcome(timeout,RewardOutcome.Failed,"load deadline at boundary");
        timeout=service.RequestAsync(Request(RewardRoute.WeChatRewardedVideo));r.Seconds+=15;r.Advance();Outcome(timeout,RewardOutcome.Failed,"load watchdog tick");
        var error=service.RequestAsync(Request(RewardRoute.WeChatRewardedVideo));r.Video.Error();r.Flush();Outcome(error,RewardOutcome.Failed,"load or inventory error");
        error=service.RequestAsync(Request(RewardRoute.WeChatRewardedVideo));r.Video.ThrowShow=true;r.Video.LoadedNow();Outcome(error,RewardOutcome.Failed,"show throws");
        var reqCancel=Request(RewardRoute.WeChatRewardedVideo);var cancelled=service.RequestAsync(reqCancel);var oldClose=r.Video.ClosedCopy;var oldLoaded=r.Video.LoadedCopy;
        Outcome(service.RequestAsync(Request(RewardRoute.WeChatShare)),RewardOutcome.Unavailable,"share blocked by video");
        service.CancelRequest(reqCancel.RequestId);Outcome(cancelled,RewardOutcome.Cancelled,"video cancel");
        var fresh=service.RequestAsync(Request(RewardRoute.WeChatRewardedVideo));oldClose(true);oldLoaded();r.Flush();Check(!fresh.IsCompleted&&r.Video.Shows==0,"old callbacks ignored");
        r.Video.LoadedNow();r.Video.Close(true);service.CancelAll();r.Flush();Outcome(fresh,RewardOutcome.Cancelled,"cancel beats queued ad success");
        Outcome(service.RequestAsync(Request(RewardRoute.WeChatShare,RewardKind.FourthPot)),RewardOutcome.Unavailable,"fourth pot cannot share");
        Outcome(service.RequestAsync(Request(RewardRoute.WeChatShare,RewardKind.ThirdPot)),RewardOutcome.Unavailable,"third pot cannot share");
        var shareReq=Request(RewardRoute.WeChatShare);var shareTask=service.RequestAsync(shareReq);r.Hide();service.CancelAll();Outcome(shareTask,RewardOutcome.Cancelled,"outer cancel propagates to share");Check(r.Listeners==0,"outer share cancellation detaches");
        var after=service.RequestAsync(Request(RewardRoute.WeChatRewardedVideo));service.Dispose();Outcome(after,RewardOutcome.Cancelled,"service dispose");
        Outcome(service.RequestAsync(Request(RewardRoute.WeChatRewardedVideo)),RewardOutcome.Unavailable,"disposed ad unavailable");share.Dispose();
        share=new WeChatShareService(config,r);service=new WeChatRewardService(config,r,share);
        var stopped=service.RequestAsync(Request(RewardRoute.WeChatShare));r.Stop();Outcome(stopped,RewardOutcome.Cancelled,"runtime destruction cancels share");Check(r.Listeners==0,"runtime destruction detaches share callbacks");
        r=new Runtime();share=new WeChatShareService(config,r);service=new WeChatRewardService(config,r,share);
        stopped=service.RequestAsync(Request(RewardRoute.WeChatRewardedVideo));r.Stop();Outcome(stopped,RewardOutcome.Cancelled,"runtime destruction cancels video");Check(r.Video.Disposals==1,"runtime destruction disposes native instance");
    }
    sealed class Runtime:IWeChatRewardRuntime
    {
        public bool Available{get;set;}=true;public double Seconds;public double MonotonicSeconds=>Seconds;
        public event Action Hidden;public event Action Shown;public event Action Tick;public event Action Stopping;
        public Action HiddenCopy=>Hidden;public Action ShownCopy=>Shown;
        public int Listeners=>(Hidden?.GetInvocationList().Length??0)+(Shown?.GetInvocationList().Length??0)+(Tick?.GetInvocationList().Length??0);
        readonly Queue<Action> posts=new Queue<Action>();public Action ShareAction;public int Menus,Created;public Video Video;
        public void Post(Action action)=>posts.Enqueue(action);
        public void Flush(){int count=posts.Count;while(count-->0)posts.Dequeue()();}
        public void Share(string title,string image){Check(!string.IsNullOrEmpty(title)&&image=="hotpot/share-theme.png","static share content");ShareAction?.Invoke();}
        public void RegisterMenu(string title,string image)=>Menus++;
        public IWeChatRewardVideo CreateVideo(string id){Created++;return Video=new Video();}
        public void Hide()=>Hidden?.Invoke();public void Show()=>Shown?.Invoke();public void Advance()=>Tick?.Invoke();
        public void Stop()=>Stopping?.Invoke();
    }
    sealed class Video:IWeChatRewardVideo
    {
        public event Action Loaded;public event Action Failed;public event Action<bool?> Closed;
        public Action LoadedCopy=>Loaded;public Action<bool?> ClosedCopy=>Closed;
        public int Listeners=>(Loaded?.GetInvocationList().Length??0)+(Failed?.GetInvocationList().Length??0)+(Closed?.GetInvocationList().Length??0);
        public int Loads,Shows,Disposals;public bool ThrowShow;
        public void Load()=>Loads++;public void Show(){Shows++;if(ThrowShow)throw new Exception("show");}
        public void Dispose()=>Disposals++;public void LoadedNow()=>Loaded?.Invoke();public void Error()=>Failed?.Invoke();public void Close(bool? ended)=>Closed?.Invoke(ended);
    }
}
