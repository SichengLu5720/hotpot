using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Profile;
using UnityEngine;

namespace HotpotSort.Platform
{
    public interface IWeChatCloudFunctionBridge
    {
        void Start(int id,string environment,string functionName,string json,Action<int,string> completion);
        void Cancel(int id);
    }
    // Ready for composition after the separate account-partition adoption decision.
    // Identity is provided by CloudBase's authenticated native context, not the SDK
    // login code or any client account ID. The one-use code is never stored/sent.
    public sealed class WeChatCloudProfileFunction:IWeChatProfileFunction,IWeChatIdentityExchange
    {
        public const int MaxBytes=4194304;
        readonly WeChatRuntimeConfig config;readonly IWeChatCloudFunctionBridge bridge;
        static int sequence;
        [Serializable] public sealed class Request
        {public int protocolVersion=1;public string requestId,action,environment;public ProfileDocument document;}
        // Unity serializes null inline classes as default objects. Identity requests
        // must have no document field, rather than a null ProfileDocument reference.
        [Serializable] sealed class IdentityRequest
        {public int protocolVersion=1;public string requestId,action="identity",environment;}
        [Serializable] public sealed class Response
        {public int protocolVersion;public string requestId,status,error,accountId,confirmationCursor,serverUtc;public ProfileDocument snapshot;public string[] acknowledgedOperations;}
        public WeChatCloudProfileFunction(WeChatRuntimeConfig config,IWeChatCloudFunctionBridge bridge=null)
        {this.config=config??throw new ArgumentNullException(nameof(config));this.bridge=bridge??new NativeBridge();}
        public async Task<WeChatIdentity> ExchangeAsync(string oneUseCode,CancellationToken cancellation)
        {
            oneUseCode=null;
            var response=await Call(config.cloudEnvironmentId,config.cloudFunctionName,"identity",null,cancellation);
            return response!=null&&response.status=="Synced"&&ValidAccount(response.accountId)?new WeChatIdentity(response.accountId):null;
        }
        public async Task<ProfileSyncResponse> SyncAsync(string environmentId,string functionName,WeChatIdentity identity,ProfileDocument document,CancellationToken cancellation)
        {
            if(identity==null||document==null||document.environment!=config.environment||document.account!=identity.AccountId)return Result(ProfileSyncStatus.Unavailable);
            try{
                ProfileStore.Validate(document,config.environment,identity.AccountId);
                var response=await Call(environmentId,functionName,"sync",ProfileStore.Copy(document),cancellation);
                if(response==null)return Result(ProfileSyncStatus.Offline);
                if(response.status=="Rejected")return Result(ProfileSyncStatus.Unavailable);
                if(response.status!="Synced"||response.accountId!=identity.AccountId||response.snapshot==null)return Result(ProfileSyncStatus.Failed);
                ProfileStore.Validate(response.snapshot,config.environment,identity.AccountId);
                if(!DateTimeOffset.TryParseExact(response.serverUtc,"o",CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out var serverUtc)||serverUtc.Offset!=TimeSpan.Zero||response.acknowledgedOperations==null)return Result(ProfileSyncStatus.Failed);
                return new ProfileSyncResponse{Status=ProfileSyncStatus.Synced,Snapshot=response.snapshot,ServerUtc=serverUtc,ConfirmationCursor=response.confirmationCursor,AcknowledgedOperations=response.acknowledgedOperations};
            }catch(OperationCanceledException){throw;}catch{return Result(ProfileSyncStatus.Failed);}
        }
        async Task<Response> Call(string environmentId,string functionName,string action,ProfileDocument document,CancellationToken token)
        {
            if(config.CloudState!=WeChatCapabilityState.Ready||environmentId!=config.cloudEnvironmentId||functionName!=config.cloudFunctionName)return null;
            token.ThrowIfCancellationRequested();
            string requestId=Guid.NewGuid().ToString("N");
            string json=action=="identity"
                ?JsonUtility.ToJson(new IdentityRequest{requestId=requestId,environment=config.environment})
                :JsonUtility.ToJson(new Request{requestId=requestId,action=action,environment=config.environment,document=document});
            if(Encoding.UTF8.GetByteCount(json)>MaxBytes)return null;
            int id=Interlocked.Increment(ref sequence);if(id<=0)return null;
            var done=new TaskCompletionSource<Response>();bool retired=false;
            using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(10));
                using(var registration=timeout.Token.Register(()=>{if(retired)return;retired=true;try{bridge.Cancel(id);}catch{}done.TrySetCanceled();}))
                {
                    try{
                        if(!timeout.IsCancellationRequested)bridge.Start(id,environmentId,functionName,json,(kind,body)=>{
                            if(retired)return;retired=true;Response response=null;
                            if(kind==0&&body!=null&&Encoding.UTF8.GetByteCount(body)<=MaxBytes)try{response=JsonUtility.FromJson<Response>(body);if(response.protocolVersion!=1||response.requestId!=requestId)response=null;}catch{}
                            done.TrySetResult(response);
                        });
                        return await done.Task;
                    }finally{retired=true;try{bridge.Cancel(id);}catch{}}
                }
            }
        }
        static bool ValidAccount(string value)=>value!=null&&System.Text.RegularExpressions.Regex.IsMatch(value,"\\Awx_[a-f0-9]{64}\\z");
        static ProfileSyncResponse Result(ProfileSyncStatus status)=>new ProfileSyncResponse{Status=status};
        sealed class NativeBridge:IWeChatCloudFunctionBridge
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Callback(int id,int kind,IntPtr json);
            [DllImport("__Internal")] static extern void HotpotProfileFunction_Start(int id,string environment,string functionName,string json,Callback callback);
            [DllImport("__Internal")] static extern void HotpotProfileFunction_Cancel(int id);
            static readonly Callback callback=Receive;
            static readonly Dictionary<int,Action<int,string>> receivers=new Dictionary<int,Action<int,string>>();
            [AOT.MonoPInvokeCallback(typeof(Callback))] static void Receive(int id,int kind,IntPtr json)
            {if(!receivers.TryGetValue(id,out var receive))return;receivers.Remove(id);receive(kind,Marshal.PtrToStringAnsi(json));}
#endif
            public void Start(int id,string environment,string functionName,string json,Action<int,string> completion)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                receivers.Add(id,completion);HotpotProfileFunction_Start(id,environment,functionName,json,callback);
#else
                completion(1,"");
#endif
            }
            public void Cancel(int id)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                receivers.Remove(id);HotpotProfileFunction_Cancel(id);
#endif
            }
        }
    }
}
