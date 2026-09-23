using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Platform;
using UnityEngine;
static class CloudProfileFunctionQa
{
    static string Account="wx_"+new string('a',64);
    static WeChatRuntimeConfig Config()=>new WeChatRuntimeConfig{cloudEnvironmentId="fixture-env"};
    static ProfileDocument Doc()=>new ProfileDocument{environment="development",account=Account,firstWinDays=new List<string>{"20260922"},pending=new List<ProfileSyncOperation>{new ProfileSyncOperation{operationId="win:20260922",kind="win",entityId="20260922"}}};
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    sealed class Bridge:IWeChatCloudFunctionBridge
    {
        public int Starts,Cancels,Id;public string Json;public Action<int,string> Complete;
        public void Start(int id,string environment,string functionName,string json,Action<int,string> complete){Check(environment=="fixture-env"&&functionName=="hotpotProfileSync","wrong invocation binding");Id=id;Json=json;Complete=complete;Starts++;}
        public void Cancel(int id){Cancels++;}
        public void Reply(Action<WeChatCloudProfileFunction.Response> mutate=null)
        {
            var request=JsonUtility.FromJson<WeChatCloudProfileFunction.Request>(Json);
            var response=new WeChatCloudProfileFunction.Response{protocolVersion=1,requestId=request.requestId,status="Synced",accountId=Account,snapshot=request.document,confirmationCursor=new string('b',64),acknowledgedOperations=request.document?.pending.Select(p=>p.operationId).ToArray(),serverUtc="2026-09-22T01:00:00.0000000+00:00"};
            mutate?.Invoke(response);Complete(0,JsonUtility.ToJson(response));
        }
    }
    static Task<ProfileSyncResponse> Sync(WeChatCloudProfileFunction function,ProfileDocument doc=null,CancellationToken token=default)=>function.SyncAsync("fixture-env","hotpotProfileSync",new WeChatIdentity(Account),doc??Doc(),token);
    static async Task Main()
    {
        int groups=0;async Task Run(string name,Func<Task> action){await action();groups++;Console.WriteLine(name+" PASS");}
        await Run("CP01-trusted-handshake-does-not-send-code-or-document",async()=>{var b=new Bridge();var f=new WeChatCloudProfileFunction(Config(),b);var task=f.ExchangeAsync("one-use-credential-fixture",default);Check(!b.Json.Contains("credential")&&JsonUtility.FromJson<WeChatCloudProfileFunction.Request>(b.Json).action=="identity","credential leaked");Check(!b.Json.Contains("\"document\""),"identity DTO must omit document, including Unity inline-null serialization");b.Reply();Check((await task).AccountId==Account,"identity handshake");});
        await Run("CP02-success-original-facts-server-time",async()=>{var b=new Bridge();var f=new WeChatCloudProfileFunction(Config(),b);var d=Doc();d.rewards.Add(new AppliedRewardRecord{requestId="reward",quotaDay="20260921",effectiveUtc="2026-09-21T22:00:00.0000001+00:00",rewardKind=0,route=2,timeSource=0});var task=Sync(f,d);d.firstWinDays.Clear();b.Reply();var result=await task;Check(result.Status==ProfileSyncStatus.Synced&&result.Snapshot.firstWinDays.Count==1&&result.Snapshot.rewards[0].effectiveUtc.EndsWith("0000001+00:00")&&result.ServerUtc.HasValue,"facts/copy/time changed");});
        await Run("CP03-duplicate-cancel-stale-generation",async()=>{var b=new Bridge();var f=new WeChatCloudProfileFunction(Config(),b);var c=new CancellationTokenSource();var old=Sync(f,token:c.Token);var callback=b.Complete;var oldRequest=b.Json;c.Cancel();try{await old;throw new Exception("cancel missing");}catch(OperationCanceledException){}Check(b.Cancels>0,"native callback not retired");var next=Sync(f);callback(0,"{}");Check(!next.IsCompleted,"stale completion changed next request");b.Reply();b.Reply(r=>r.status="Failed");Check((await next).Status==ProfileSyncStatus.Synced,"duplicate changed completed result");});
        await Run("CP04-correlation-partition-schema-time-validation",async()=>{foreach(var mutate in new Action<WeChatCloudProfileFunction.Response>[] {r=>r.requestId="old",r=>r.protocolVersion=99,r=>r.accountId="other",r=>r.snapshot.account="other",r=>r.snapshot.schemaVersion=0,r=>r.serverUtc="invalid",r=>r.acknowledgedOperations=null}){var b=new Bridge();var f=new WeChatCloudProfileFunction(Config(),b);var task=Sync(f);b.Reply(mutate);Check((await task).Status!=ProfileSyncStatus.Synced,"malformed response accepted");}});
        await Run("CP05-sanitized-failures-and-rejected-payload",async()=>{foreach(int kind in new[]{1,2,3,4}){var b=new Bridge();var f=new WeChatCloudProfileFunction(Config(),b);var task=Sync(f);b.Complete(kind,"private-error-not-forwarded");Check((await task).Status!=ProfileSyncStatus.Synced,"bridge error accepted");}var rejected=new Bridge();var pending=Sync(new WeChatCloudProfileFunction(Config(),rejected));rejected.Reply(r=>{r.status="Rejected";r.error="ImmutableConflict";});Check((await pending).Status==ProfileSyncStatus.Unavailable,"invalid payload must not retry as transient");});
        await Run("CP06-notconfigured-wrong-partition-oversize-pre-cancel",async()=>{var b=new Bridge();var config=Config();config.cloudEnvironmentId="";Check(await new WeChatCloudProfileFunction(config,b).ExchangeAsync("",default)==null&&b.Starts==0,"missing config called native");var f=new WeChatCloudProfileFunction(Config(),b);var d=Doc();d.account="other";Check((await Sync(f,d)).Status==ProfileSyncStatus.Unavailable&&b.Starts==0,"wrong partition sent");d=Doc();d.confirmationCursor=new string('x',WeChatCloudProfileFunction.MaxBytes);Check((await Sync(f,d)).Status!=ProfileSyncStatus.Synced&&b.Starts==0,"oversize sent");var c=new CancellationTokenSource();c.Cancel();try{await Sync(f,token:c.Token);throw new Exception("cancel missing");}catch(OperationCanceledException){}Check(b.Starts==0,"pre-cancel called native");});
        Console.WriteLine("CLOUD_PROFILE_CLIENT_PASS groups="+groups+" externalRequests=0 compositionDeferred=true");
    }
}
