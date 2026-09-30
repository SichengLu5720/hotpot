using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using UnityEngine;

namespace HotpotSort.Platform
{
    public sealed class WeChatBrothActivityService:IBrothActivityService,IBrothChallengeCompletion,IDisposable
    {
        [Serializable] public sealed class Command
        {
            public int protocolVersion=1;
            public string requestId,environment,account,action,operationId,invitationId,brothId;
            public string sessionId,stage,outcome,completedUtc;
            public long expectedRevision;
        }
        [Serializable] public sealed class Response
        {
            public int protocolVersion;
            public string requestId,status,error,accountId;
            public CollectionDocument snapshot;
            public BrothInvitation invitation;
        }
        readonly WeChatRuntimeConfig config;
        readonly string account;
        readonly Func<CollectionDocument> read;
        readonly Action<CollectionDocument> apply;
        readonly IWeChatCloudFunctionBridge bridge;
        readonly TimeSpan timeout;
        readonly CancellationTokenSource lifetime=new CancellationTokenSource();
        readonly Dictionary<string,Command> mutations=new Dictionary<string,Command>();
        static int sequence=2000000;
        bool disposed;
        public bool IsDevelopmentSimulation=>false;
        public WeChatBrothActivityService(WeChatRuntimeConfig config,string account,Func<CollectionDocument> read,Action<CollectionDocument> apply,IWeChatCloudFunctionBridge bridge,TimeSpan? timeout=null)
        {this.config=config;this.account=account;this.read=read;this.apply=apply;this.bridge=bridge;this.timeout=timeout??TimeSpan.FromSeconds(10);}
        public Task<BrothResult> ReadAsync(string requestId)=>Call(new Command{requestId=requestId,action="brothRead"});
        public Task<BrothResult> CreateInvitationAsync(string requestId,string operationId)=>Call(new Command{requestId=requestId,operationId=operationId,action="brothCreateInvitation"});
        public Task<BrothResult> InspectInvitationAsync(string requestId,string invitationId)=>Call(new Command{requestId=requestId,invitationId=invitationId,action="brothInspectInvitation"});
        public Task<BrothResult> ConfirmAssistAsync(string requestId,string invitationId,string operationId)=>Call(new Command{requestId=requestId,invitationId=invitationId,operationId=operationId,action="brothConfirmAssist"});
        public Task<BrothResult> ClaimAsync(string requestId,string brothId,string operationId)=>Call(new Command{requestId=requestId,brothId=brothId,operationId=operationId,action="brothClaim"});
        public Task<BrothResult> SelectAsync(string requestId,string brothId,string operationId)=>Call(new Command{requestId=requestId,brothId=brothId,operationId=operationId,action="brothSelect"});
        public Task<BrothResult> CompleteChallengeAsync(string requestId,string sessionId,string completedUtc,string operationId)=>Call(new Command{requestId=requestId,sessionId=sessionId,completedUtc=completedUtc,stage="formal",outcome="won",operationId=operationId,action="brothCompleteChallenge"});
        async Task<BrothResult> Call(Command command)
        {
            Func<BrothFailure,BrothResult> fail=f=>new BrothResult{requestId=command.requestId,failure=f};
            if(disposed||config==null||bridge==null||read==null||apply==null||config.CloudState!=WeChatCapabilityState.Ready)return fail(BrothFailure.Unavailable);
            if(account==null||!Regex.IsMatch(account,"\\Awx_[a-f0-9]{64}\\z"))return fail(BrothFailure.Unauthenticated);
            if(command.requestId==null||!Regex.IsMatch(command.requestId,"\\A[a-f0-9]{32}\\z"))return fail(BrothFailure.InvalidRequest);
            bool writing=command.action!="brothRead"&&command.action!="brothInspectInvitation";
            if(writing&&(string.IsNullOrWhiteSpace(command.operationId)||command.operationId.Length>160))return fail(BrothFailure.InvalidRequest);
            if((command.action=="brothInspectInvitation"||command.action=="brothConfirmAssist")&&!WeChatActivityLinkService.ValidInvitation(command.invitationId))return fail(BrothFailure.InvalidRequest);
            var local=read();if(local==null||local.account!=account||local.environment!=config.environment)return fail(BrothFailure.Unauthenticated);
            command.environment=config.environment;command.account=account;command.expectedRevision=local.serverRevision;
            // A retry must preserve the original expectedRevision even after refresh.
            if(writing)lock(mutations){
                string pendingKey="Hotpot.BrothOperation."+config.environment+"."+account+"."+command.operationId;
                if(!mutations.ContainsKey(command.operationId)){
                    string saved=PlayerPrefs.GetString(pendingKey,"");
                    if(saved.Length>0)try{
                        var pending=JsonUtility.FromJson<Command>(saved);
                        if(pending==null||pending.account!=account||pending.environment!=config.environment||pending.operationId!=command.operationId)return fail(BrothFailure.Failed);
                        mutations.Add(command.operationId,pending);
                    }catch{return fail(BrothFailure.Failed);}
                }
                if(mutations.TryGetValue(command.operationId,out var original)){
                    if(original.action!=command.action||(original.brothId??"")!=(command.brothId??"")||(original.invitationId??"")!=(command.invitationId??"")||(original.sessionId??"")!=(command.sessionId??"")||(original.completedUtc??"")!=(command.completedUtc??""))return fail(BrothFailure.OperationConflict);
                    command.expectedRevision=original.expectedRevision;
                }else {
                    try{PlayerPrefs.SetString(pendingKey,JsonUtility.ToJson(command));PlayerPrefs.Save();}
                    catch{return fail(BrothFailure.Failed);}
                    mutations.Add(command.operationId,command);
                }
            }
            int id=Interlocked.Increment(ref sequence),retired=0;
            var done=new TaskCompletionSource<BrothResult>();
            using(var cancel=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token))
            {
                cancel.CancelAfter(timeout);
                using(cancel.Token.Register(()=>{if(Interlocked.Exchange(ref retired,1)==0)done.TrySetResult(fail(disposed?BrothFailure.Unavailable:BrothFailure.Offline));}))
                try{
                    if(cancel.IsCancellationRequested)return await done.Task;
                    bridge.Start(id,config.cloudEnvironmentId,"hotpotIngredientTrade",JsonUtility.ToJson(command),(kind,json)=>{
                        if(Interlocked.Exchange(ref retired,1)!=0)return;
                        try{
                            if(disposed){done.TrySetResult(fail(BrothFailure.Unavailable));return;}
                            if(kind!=0){done.TrySetResult(fail(BrothFailure.Offline));return;}
                            if(json==null||json.Length>4194304)throw new FormatException();
                            var response=JsonUtility.FromJson<Response>(json);
                            if(response==null||response.protocolVersion!=1||response.requestId!=command.requestId||response.accountId!=account)throw new FormatException();
                            if(response.status!="Synced"){done.TrySetResult(fail(Map(response.error)));return;}
                            var snapshot=response.snapshot;var now=read();
                            if(snapshot==null||snapshot.account!=account||snapshot.environment!=config.environment||now==null||now.account!=account||now.environment!=config.environment)throw new FormatException();
                            if(snapshot.serverRevision<now.serverRevision){done.TrySetResult(fail(BrothFailure.Stale));return;}
                            if(response.invitation!=null&&!WeChatActivityLinkService.ValidInvitation(response.invitation.invitationId))throw new FormatException();
                            if((command.action=="brothCreateInvitation"||command.action=="brothInspectInvitation")&&response.invitation==null)throw new FormatException();
                            if(command.invitationId!=null&&response.invitation!=null&&response.invitation.invitationId!=command.invitationId)throw new FormatException();
                            apply(snapshot); // CollectionStore validates inventory and immutable choice.
                            if(writing){PlayerPrefs.DeleteKey("Hotpot.BrothOperation."+config.environment+"."+account+"."+command.operationId);PlayerPrefs.Save();}
                            done.TrySetResult(new BrothResult{requestId=command.requestId,collection=read(),invitation=response.invitation});
                        }catch{done.TrySetResult(fail(BrothFailure.Failed));}
                    });
                    return await done.Task;
                }catch{return fail(BrothFailure.Failed);}
                finally{Interlocked.Exchange(ref retired,1);try{bridge.Cancel(id);}catch{}}
            }
        }
        static BrothFailure Map(string error)
        {
            switch(error){
                case "EntryLocked":return BrothFailure.NotQualified;
                case "WrongPartition":case "Unauthenticated":return BrothFailure.Unauthenticated;
                case "StaleRevision":return BrothFailure.Stale;
                case "InvalidPayload":return BrothFailure.InvalidRequest;
                case "NotConfigured":return BrothFailure.Unavailable;
                case "ServerFailure":return BrothFailure.Failed;
                default:return Enum.TryParse(error,out BrothFailure failure)&&failure!=BrothFailure.None?failure:BrothFailure.Failed;
            }
        }
        public void Dispose(){if(disposed)return;disposed=true;lifetime.Cancel();}
    }
}
