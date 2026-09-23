using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Platform;
using HotpotSort.Profile;

static class ProfilePlatformQa
{
    sealed class Sdk:IWeChatLoginSdk
    {public bool Available{get;set;}=true;public int Calls;public Action<string> Success;public Action Failure;public void Login(Action<string> success,Action failure){Calls++;Success=success;Failure=failure;}}
    sealed class Exchange:IWeChatIdentityExchange
    {public Func<CancellationToken,Task<WeChatIdentity>> Call;public Task<WeChatIdentity> ExchangeAsync(string code,CancellationToken token){Assert(code=="fixture-code","identity exchange code mismatch");return Call(token);}}
    sealed class Function:IWeChatProfileFunction
    {public Func<ProfileDocument,CancellationToken,Task<ProfileSyncResponse>> Call;public int Calls;public Task<ProfileSyncResponse> SyncAsync(string env,string name,WeChatIdentity identity,ProfileDocument doc,CancellationToken token){Calls++;Assert(env=="qa-env"&&name=="hotpotProfileSync"&&identity.AccountId=="server-account","backend handoff");return Call(doc,token);}}
    static void Assert(bool b,string message){if(!b)throw new Exception(message);}
    static WeChatRuntimeConfig Config()=>new WeChatRuntimeConfig{cloudEnvironmentId="qa-env"};
    static ProfileDocument Document()=>new ProfileDocument{environment="development",account="server-account",firstWinDays=new List<string>{"20260922"},pending=new List<ProfileSyncOperation>{new ProfileSyncOperation{operationId="win:20260922",kind="win",entityId="20260922"}}};
    static ProfileSyncResponse Ack(ProfileDocument doc)=>new ProfileSyncResponse{Status=ProfileSyncStatus.Synced,Snapshot=ProfileStore.Copy(doc),AcknowledgedOperations=doc.pending.Select(p=>p.operationId).ToArray()};
    static async Task<WeChatSilentLogin> Authenticated()
    {var sdk=new Sdk();var login=new WeChatSilentLogin(Config(),sdk,new Exchange{Call=_=>Task.FromResult(new WeChatIdentity("server-account"))});var task=login.StartAsync();sdk.Success("fixture-code");Assert(await task==WeChatLoginStatus.Authenticated,"authenticated fixture");return login;}
    static async Task<int> Main(string[] args)
    {
        var results=new List<object>();int failed=0;
        async Task Check(string name,Func<Task> body){try{await body();results.Add(new{name,status="PASS"});Console.WriteLine(name+" PASS");}catch(Exception e){failed++;results.Add(new{name,status="FAIL",error=e.ToString()});Console.WriteLine(name+" FAIL "+e);}}
        await Check("LP01-login-needs-server-and-finite-state",async()=>
        {var sdk=new Sdk();using var login=new WeChatSilentLogin(Config(),sdk);var task=login.StartAsync();Assert(!task.IsCompleted&&login.Status==WeChatLoginStatus.Authenticating,"blocking or premature identity");sdk.Success("fixture-code");Assert(await task==WeChatLoginStatus.NeedsServer&&login.Identity==null,"fake identity");sdk.Failure();Assert(login.Status==WeChatLoginStatus.NeedsServer,"duplicate callback changed result");});
        await Check("LP02-login-missing-disabled-native-unavailable",async()=>
        {var sdk=new Sdk();using var missing=new WeChatSilentLogin(WeChatRuntimeConfig.Unavailable(),sdk);Assert(await missing.StartAsync()==WeChatLoginStatus.NotConfigured&&sdk.Calls==0,"missing config called sdk");using var disabled=new WeChatSilentLogin(new WeChatRuntimeConfig{enableLogin=false},sdk);Assert(await disabled.StartAsync()==WeChatLoginStatus.NotConfigured&&sdk.Calls==0,"disabled called sdk");using var unavailable=new WeChatSilentLogin(Config(),new Sdk{Available=false});Assert(await unavailable.StartAsync()==WeChatLoginStatus.Unavailable,"native absence");});
        await Check("LP03-login-retry-cancel-stale-backend",async()=>
        {var sdk=new Sdk();var backend=new TaskCompletionSource<WeChatIdentity>();using var login=new WeChatSilentLogin(Config(),sdk,new Exchange{Call=_=>backend.Task});var first=login.StartAsync();var old=sdk.Success;old("fixture-code");login.Cancel();Assert(await first==WeChatLoginStatus.Cancelled,"backend ignoring cancellation blocked exit");backend.SetResult(new WeChatIdentity("late-account"));Assert(login.Identity==null,"late backend identity applied");var second=login.StartAsync();old("fixture-code");Assert(!second.IsCompleted,"stale sdk callback completed new request");sdk.Failure();Assert(await second==WeChatLoginStatus.Failed,"failure state");});
        await Check("LP04-cloud-notconfigured-unavailable-keeps-document",async()=>
        {using var login=await Authenticated();var doc=Document();using var absent=new WeChatProfileTransport(new WeChatRuntimeConfig(),login);Assert((await absent.SyncAsync(doc)).Status==ProfileSyncStatus.NotConfigured,"missing cloud");using var needsServer=new WeChatProfileTransport(Config(),login);Assert((await needsServer.SyncAsync(doc)).Status==ProfileSyncStatus.Unavailable&&doc.pending.Count==1,"invented online ack");});
        await Check("LP05-retries-same-outbox-and-bounded-backoff",async()=>
        {using var login=await Authenticated();var waits=new List<double>();var doc=Document();var sent=new List<string>();var fn=new Function();fn.Call=(d,t)=>{sent.Add(d.pending.Single().operationId);d.firstWinDays.Clear();return Task.FromResult(new ProfileSyncResponse{Status=ProfileSyncStatus.Offline});};using var transport=new WeChatProfileTransport(Config(),login,fn,(time,token)=>{waits.Add(time.TotalMilliseconds);return Task.CompletedTask;});Assert((await transport.SyncAsync(doc)).Status==ProfileSyncStatus.Offline&&fn.Calls==3&&waits.SequenceEqual(new[]{250d,500d})&&sent.Distinct().Count()==1&&doc.firstWinDays.Count==1&&doc.pending.Count==1,"retry bound or mutation");});
        await Check("LP06-cloud-cancel-stale-identity-and-partition",async()=>
        {using var login=await Authenticated();var deferred=new TaskCompletionSource<ProfileSyncResponse>();var fn=new Function{Call=(d,t)=>deferred.Task};using var transport=new WeChatProfileTransport(Config(),login,fn);var other=Document();other.account="local";Assert((await transport.SyncAsync(other)).Status==ProfileSyncStatus.Unavailable&&fn.Calls==0,"foreign partition sent");var pending=transport.SyncAsync(Document());transport.CancelPending();Assert((await pending).Status==ProfileSyncStatus.Stale,"uncooperative backend blocked cancellation");deferred.SetResult(Ack(Document()));var deferred2=new TaskCompletionSource<ProfileSyncResponse>();fn.Call=(d,t)=>deferred2.Task;var second=transport.SyncAsync(Document());login.Cancel();deferred2.SetResult(Ack(Document()));Assert((await second).Status==ProfileSyncStatus.Stale,"identity change accepted late ack");});
        await Check("LP07-success-preserves-original-reward-facts",async()=>
        {using var login=await Authenticated();var doc=Document();const string utc="2026-09-22T01:02:03.0000000+00:00";doc.rewards.Add(new AppliedRewardRecord{requestId="stable-request",quotaDay="20260922",rewardKind=0,route=(int)RewardRoute.WeChatShare,effectiveUtc=utc,timeSource=ProfileTimeSource.DeviceTime});doc.pending.Add(new ProfileSyncOperation{operationId="reward:stable-request",kind="reward",entityId="stable-request"});var fn=new Function{Call=(d,t)=>Task.FromResult(Ack(d))};using var transport=new WeChatProfileTransport(Config(),login,fn);var response=await transport.SyncAsync(doc);Assert(response.Status==ProfileSyncStatus.Synced&&response.Snapshot.rewards.Single().effectiveUtc==utc&&response.AcknowledgedOperations.Contains("reward:stable-request")&&fn.Calls==1,"fact rewritten");});
        File.WriteAllText(args[0],JsonSerializer.Serialize(new{failed,results,scope="Injected SDK/backend only; native SDK surface checked by WebGL compilation; no network or real account"},new JsonSerializerOptions{WriteIndented=true}));return failed==0?0:1;
    }
}
