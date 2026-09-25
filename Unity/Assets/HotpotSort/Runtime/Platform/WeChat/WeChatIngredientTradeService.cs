using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using UnityEngine;

namespace HotpotSort.Platform
{
    // No client inventory is uploaded. Successful snapshots are handed to the
    // collection authority adapter, whose revision gate rejects older callbacks.
    public sealed class WeChatIngredientTradeService : IIngredientTradeService
    {
        [Serializable] public sealed class Command
        {
            public int protocolVersion=1;
            public string requestId,environment,account,action,operationId,tradeId,targetAccount,offeredId,receivedId;
            public long expectedRevision;
            public int tool;
            public List<CollectionWin> pendingWins=new List<CollectionWin>();
            public List<string> selected=new List<string>();
        }
        [Serializable] public sealed class WireTrade
        { public string requestId,initiator,recipient,offeredId,receivedId,status,createdUtc,expiresUtc; }
        [Serializable] public sealed class Response
        {
            public int protocolVersion; public string requestId,status,error,accountId;
            public bool friendVerificationAvailable; public CollectionDocument snapshot;
            public WireTrade trade; public List<WireTrade> trades;
        }
        readonly WeChatRuntimeConfig config;
        readonly IWeChatCloudFunctionBridge bridge;
        readonly string account,targetAccount;
        readonly Action<CollectionDocument> apply;
        static int sequence=1000000;
        public WeChatIngredientTradeService(WeChatRuntimeConfig config,string account,Action<CollectionDocument> apply,IWeChatCloudFunctionBridge bridge,string targetAccount=null)
        {this.config=config;this.account=account;this.apply=apply;this.bridge=bridge;this.targetAccount=targetAccount;}
        public Task<IngredientTradeResult> CreateAsync(string offeredId,string receivedId,string operationId)=>Call(new Command{action="create",offeredId=offeredId,receivedId=receivedId,operationId=operationId,tradeId=operationId,targetAccount=targetAccount});
        public Task<IngredientTradeResult> AcceptAsync(string requestId,string operationId)=>Trade("accept",requestId,operationId);
        public Task<IngredientTradeResult> RejectAsync(string requestId,string operationId)=>Trade("reject",requestId,operationId);
        public Task<IngredientTradeResult> CancelAsync(string requestId,string operationId)=>Trade("withdraw",requestId,operationId);
        public Task<IngredientTradeResult> RefreshAsync(string requestId)=>Trade("getTrade",requestId,Guid.NewGuid().ToString("N"));
        public Task<IngredientTradeResult> ListAsync()=>Call(new Command{action="list"});
        public Task<IngredientTradeResult> SyncAsync(List<CollectionWin> wins,string operationId)=>Call(new Command{action="sync",pendingWins=wins,operationId=operationId});
        public Task<IngredientTradeResult> SaveSelectionAsync(List<string> selected,long expectedRevision,string operationId)=>Call(new Command{action="selection",selected=selected,expectedRevision=expectedRevision,operationId=operationId});
        public Task<IngredientTradeResult> ConsumeAsync(RewardKind tool,string operationId)=>Call(new Command{action="consume",tool=(int)tool,operationId=operationId});
        Task<IngredientTradeResult> Trade(string action,string id,string op)=>Call(new Command{action=action,tradeId=id,operationId=op});
        async Task<IngredientTradeResult> Call(Command command)
        {
            if(config==null||bridge==null||config.CloudState!=WeChatCapabilityState.Ready)return Failure(IngredientTradeFailure.NotConfigured);
            if(command.action=="create"&&string.IsNullOrEmpty(targetAccount))return Failure(IngredientTradeFailure.Unavailable);
            command.requestId=Guid.NewGuid().ToString("N");command.environment=config.environment;command.account=account;
            var done=new TaskCompletionSource<IngredientTradeResult>();int id=Interlocked.Increment(ref sequence),retired=0;
            using(var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            using(timeout.Token.Register(()=>{if(Interlocked.Exchange(ref retired,1)==0)done.TrySetResult(Failure(IngredientTradeFailure.Offline));}))
            {
                try{
                    bridge.Start(id,config.cloudEnvironmentId,"hotpotIngredientTrade",JsonUtility.ToJson(command),(kind,json)=>{
                        if(Interlocked.Exchange(ref retired,1)!=0)return;
                        try{
                            if(kind!=0){done.TrySetResult(Failure(IngredientTradeFailure.Offline));return;}
                            if(json==null||json.Length>4194304)throw new FormatException();
                            var response=JsonUtility.FromJson<Response>(json);
                            if(response==null||response.protocolVersion!=1||response.requestId!=command.requestId||response.accountId!=account)throw new FormatException();
                            if(response.status!="Synced"){done.TrySetResult(Failure(Map(response.error)));return;}
                            if(response.snapshot==null||response.snapshot.account!=account||response.snapshot.environment!=config.environment)throw new FormatException();
                            var result=new IngredientTradeResult{collection=response.snapshot,request=Convert(response.trade)};
                            if(response.trades!=null)foreach(var trade in response.trades)result.requests.Add(Convert(trade));
                            apply?.Invoke(response.snapshot);done.TrySetResult(result);
                        }catch{done.TrySetResult(Failure(IngredientTradeFailure.Failed));}
                    });return await done.Task;
                }catch{return Failure(IngredientTradeFailure.Failed);}
                finally{Interlocked.Exchange(ref retired,1);try{bridge.Cancel(id);}catch{}}
            }
        }
        IngredientTradeRequest Convert(WireTrade t)
        {
            if(t==null)return null;IngredientTradeStatus status;
            if(t.status=="Withdrawn")status=IngredientTradeStatus.Cancelled;
            else if(!Enum.TryParse(t.status,out status))throw new FormatException();
            bool owner=t.initiator==account,pending=status==IngredientTradeStatus.Pending;
            return new IngredientTradeRequest{requestId=t.requestId,offeredId=t.offeredId,receivedId=t.receivedId,createdUtc=t.createdUtc,expiresUtc=t.expiresUtc,status=status,isInitiator=owner,canAccept=pending&&!owner,canReject=pending&&!owner,canCancel=pending&&owner};
        }
        static IngredientTradeResult Failure(IngredientTradeFailure failure)=>new IngredientTradeResult{failure=failure};
        static IngredientTradeFailure Map(string error)
        {
            switch(error){case "NotConfigured":return IngredientTradeFailure.NotConfigured;case "Unauthenticated":case "WrongPartition":return IngredientTradeFailure.Unauthenticated;
            case "FriendVerificationUnavailable":return IngredientTradeFailure.Unavailable;case "NotFriends":case "NotParticipant":case "NotAuthorized":return IngredientTradeFailure.Forbidden;
            case "InsufficientInventory":return IngredientTradeFailure.InsufficientDuplicates;case "TradeExpired":return IngredientTradeFailure.Expired;case "TradeClosed":return IngredientTradeFailure.AlreadyResolved;
            case "TradeNotFound":return IngredientTradeFailure.NotFound;case "OperationConflict":case "TradeIdConflict":return IngredientTradeFailure.OperationConflict;case "StaleRevision":return IngredientTradeFailure.Stale;
            case "ServerFailure":return IngredientTradeFailure.Failed;default:return IngredientTradeFailure.InvalidRequest;}
        }
    }
}
