using System;
using HotpotSort.Contracts;
using HotpotSort.Platform;

static class Task027ShareQa
{
    static int checks;
    static void Check(bool pass,string text){if(!pass)throw new Exception(text);checks++;}
    static void Main()
    {
        Check(SettlementShareText.Title(true,0,0).Contains("胜利，完成进度 100%"),"win always 100");
        foreach(var row in new[]{new[]{23,61,38},new[]{0,61,0},new[]{1,8,13},new[]{80,61,100},new[]{-1,61,0},new[]{5,0,0}})
            Check(SettlementShareText.Title(false,row[0],row[1]).Contains("失败，完成进度 "+row[2]+"%"),"clamped rounded failure");
        var runtime=new Runtime();var config=new WeChatRuntimeConfig();
        using(var share=new WeChatShareService(config,runtime))
        {
            string title=SettlementShareText.Title(false,23,61);
            Check(share.ShareThemeAsync("unused",title).Result==RewardOutcome.Success,"ordinary launch accepted");
            Check(runtime.Title==title&&runtime.Image==config.shareImagePath,"result title and configured image");
            Check(runtime.Listeners==0&&runtime.VideoCalls==0,"no reward lifecycle or video");
            runtime.RoundTrip();Check(runtime.Listeners==0,"return does not attach reward listeners");
            runtime.Throw=true;Check(share.ShareThemeAsync("unused",title).Result==RewardOutcome.Failed,"launch failure returned");
            runtime.Throw=false;Check(share.ShareThemeAsync("unused",title).Result==RewardOutcome.Success,"retry after failure");
            runtime.Available=false;Check(share.ShareThemeAsync("unused",title).Result==RewardOutcome.Unavailable,"unavailable returned");runtime.Available=true;
            Check(share.ShareThemeAsync("unused").Result==RewardOutcome.Success&&runtime.Title==config.shareTitle,"old API retains default title");
            var reward=share.RequestRewardShareAsync("existing-reward");
            Check(!reward.IsCompleted&&runtime.Listeners==3,"reward flow unchanged");
            Check(share.ShareThemeAsync("unused",title).Result==RewardOutcome.Unavailable,"ordinary share cannot interfere with active reward");
            share.CancelAll();Check(reward.Result==RewardOutcome.Cancelled&&runtime.Listeners==0,"reward clean cancellation");
        }
        Console.WriteLine("TASK027_SHARE_QA_PASS checks="+checks);
    }
    sealed class Runtime:IWeChatRewardRuntime
    {
        public bool Available{get;set;}=true;public double MonotonicSeconds=>0;
        public event Action Hidden,Shown,Tick,Stopping;
        public int Listeners=>(Hidden?.GetInvocationList().Length??0)+(Shown?.GetInvocationList().Length??0)+(Tick?.GetInvocationList().Length??0);
        public string Title,Image;public bool Throw;public int VideoCalls;
        public void Share(string title,string image){if(Throw)throw new Exception("launch failed");Title=title;Image=image;}
        public void RoundTrip(){Hidden?.Invoke();Shown?.Invoke();Tick?.Invoke();}
        public void Post(Action action)=>action();
        public void RegisterMenu(string title,string image){}
        public IWeChatRewardVideo CreateVideo(string id){VideoCalls++;throw new Exception("no video expected");}
    }
}
