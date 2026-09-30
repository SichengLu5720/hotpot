using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Platform;
using UnityEngine;
using WeChatWASM;

static class NativeRewardQa
{
    static readonly List<object> results=new List<object>();static int failures,assertions;
    static void Assert(bool value,string message){assertions++;if(!value)throw new Exception(message);}
    static void Check(string name,Action body){try{body();results.Add(new{name,status="PASS"});}catch(Exception error){failures++;results.Add(new{name,status="FAIL",error=error.Message});}}
    static RewardRequest Request(RewardRoute route)=>new RewardRequest(1,RewardKind.Hint,route,"20260923","test");
    static void Next(WeChatRewardLifecycle runtime){Time.frameCount++;runtime.Advance();}
    static void Outcome(Task<RewardOutcome> task,RewardOutcome outcome)=>Assert(task.IsCompletedSuccessfully&&task.Result==outcome,"wrong or hanging platform outcome");
    static int Main(string[] args)
    {
        Check("N01-native-menu-share-lifecycle",()=>
        {
            using(var runtime=new WeChatRewardLifecycle())using(var share=new WeChatShareService(new WeChatRuntimeConfig(),runtime))
            {
                Assert(share.RegisterMenu()&&share.RegisterMenu()&&WX.Menus==1,"menu repeated");
                Assert(WX.Menu.menus.Length==1&&WX.Menu.menus[0]=="shareAppMessage"&&WX.Menu.withShareTicket==false,"extra share channels");
                var task=share.RequestRewardShareAsync("native-share");
                Assert(WX.LastShare.title==WX.ShareTemplate.title&&WX.LastShare.imageUrl==WX.ShareTemplate.imageUrl,"menu/reward content differs");
                WX.ShowNow();Next(runtime);Assert(!task.IsCompleted,"show before hide succeeds");
                WX.HideNow();WX.ShowNow();runtime.Advance();Assert(!task.IsCompleted,"completion occurred in callback frame");
                Next(runtime);Outcome(task,RewardOutcome.Success);
            }
            Assert(WX.Listeners==0,"native lifecycle listeners leaked");
            using(var runtime=new WeChatRewardLifecycle())runtime.RegisterMenu("same","same.png");
            Assert(WX.Menus==1,"menu registered again after scope recreation");
        });
        Check("N02-missing-ad-id-no-sdk-create",()=>
        {
            int before=WX.Creates;
            using(var runtime=new WeChatRewardLifecycle())using(var share=new WeChatShareService(new WeChatRuntimeConfig(),runtime))using(var service=new WeChatRewardService(new WeChatRuntimeConfig(),runtime,share))
                Outcome(service.RequestAsync(Request(RewardRoute.WeChatRewardedVideo)),RewardOutcome.Unavailable);
            Assert(WX.Creates==before,"invented ad unit or native create for missing ID");
        });
        foreach(bool? ended in new bool?[]{true,false,null})Check("N03-native-close-"+(ended?.ToString()??"null"),()=>
        {
            var config=new WeChatRuntimeConfig{rewardedAdUnitId="fixture"};
            using(var runtime=new WeChatRewardLifecycle())using(var share=new WeChatShareService(config,runtime))using(var service=new WeChatRewardService(config,runtime,share))
            {
                var task=service.RequestAsync(Request(RewardRoute.WeChatRewardedVideo));var native=WX.LastVideo;
                native.Loaded(new WXTextResponse());native.Loaded(new WXTextResponse());Assert(native.Shows==1,"native repeated Show");
                var oldClose=native.Close;native.Close(ended.HasValue?new WXRewardedVideoAdOnCloseResponse{isEnded=ended.Value}:null);
                Next(runtime);Outcome(task,ended==true?RewardOutcome.Success:RewardOutcome.Failed);
                Assert(native.Destroyed==1&&native.Close==null&&native.Error==null,"native ad listeners/resources leaked");
                var newer=service.RequestAsync(Request(RewardRoute.WeChatRewardedVideo));oldClose(new WXRewardedVideoAdOnCloseResponse{isEnded=true});Next(runtime);Assert(!newer.IsCompleted,"old native callback reaches new request");service.CancelAll();Outcome(newer,RewardOutcome.Cancelled);
            }
        });
        Check("N04-native-load-show-error",()=>
        {
            var config=new WeChatRuntimeConfig{rewardedAdUnitId="fixture"};
            using(var runtime=new WeChatRewardLifecycle())using(var share=new WeChatShareService(config,runtime))using(var service=new WeChatRewardService(config,runtime,share))
            {
                var load=service.RequestAsync(Request(RewardRoute.WeChatRewardedVideo));WX.LastVideo.Error(new WXADErrorResponse());Next(runtime);Outcome(load,RewardOutcome.Failed);
                var show=service.RequestAsync(Request(RewardRoute.WeChatRewardedVideo));WX.LastVideo.Loaded(new WXTextResponse());WX.LastVideo.ShowFailed(new WXTextResponse());Next(runtime);Outcome(show,RewardOutcome.Failed);
                var timeout=service.RequestAsync(Request(RewardRoute.WeChatRewardedVideo));Time.realtimeSinceStartupAsDouble+=15;Next(runtime);Outcome(timeout,RewardOutcome.Failed);
            }
        });
        Check("N05-runtime-stopping-observer-isolation",()=>
        {
            var runtime=new WeChatRewardLifecycle();
            runtime.Stopping+=()=>throw new Exception("Unrelated stopping observer");
            using(var share=new WeChatShareService(new WeChatRuntimeConfig(),runtime))
            {
                var pending=share.RequestRewardShareAsync("shutdown");int before=UnityEngine.Object.Destroyed;
                try{runtime.Dispose();}catch{ /* Assertion below detects leaked pending service despite observer error. */ }
                Outcome(pending,RewardOutcome.Cancelled);
                Assert(UnityEngine.Object.Destroyed==before+1&&!runtime.Available&&WX.Listeners==0,"host cleanup not completed");
            }
        });
        Check("N06-sdk-off-exception-still-destroys-host",()=>
        {
            var runtime=new WeChatRewardLifecycle();int before=UnityEngine.Object.Destroyed;int hides=0;runtime.Hidden+=()=>hides++;
            WX.ThrowOffHide=true;
            try{runtime.Dispose();}catch{}
            finally{WX.ThrowOffHide=false;}
            WX.HideNow();Assert(hides==0&&UnityEngine.Object.Destroyed==before+1,"SDK removal failure retained live host/callback");
        });
        Check("N07-runtime-owns-unclaimed-native-video",()=>
        {
            var runtime=new WeChatRewardLifecycle();var video=runtime.CreateVideo("fixture");var native=WX.LastVideo;
            try
            {
                using(var other=new WeChatRewardLifecycle())Assert(other.CreateVideo("fixture")==null,"multiple active native instances");
                runtime.Dispose();Assert(native.Destroyed==1,"runtime leaked unclaimed native video");
                using(var replacement=new WeChatRewardLifecycle())using(var second=replacement.CreateVideo("fixture"))Assert(second!=null,"old owner blocked new native instance");
            }
            finally{video.Dispose();runtime.Dispose();}
        });
        File.WriteAllText(args[0],JsonSerializer.Serialize(new{task="TASK-007",version=1,assertions,failures,scope="Production WebGL reward adapters with SDK/engine boundary fakes; not device evidence",results},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("NATIVE_REWARD_QA cases="+results.Count+" assertions="+assertions+" failures="+failures);return failures==0?0:1;
    }
}
