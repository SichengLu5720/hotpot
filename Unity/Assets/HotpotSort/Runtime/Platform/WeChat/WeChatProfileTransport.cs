using System;
using System.Threading;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Profile;

namespace HotpotSort.Platform
{
    // No endpoint/response format is invented here. The approved server implementation
    // must authenticate identity and implement the existing ProfileDocument/outbox contract.
    public interface IWeChatProfileFunction
    {
        Task<ProfileSyncResponse> SyncAsync(string environmentId,string functionName,WeChatIdentity identity,
            ProfileDocument document,CancellationToken cancellation);
    }
    public sealed class WeChatProfileTransport:IProfileSyncTransport,IDisposable
    {
        readonly WeChatRuntimeConfig config;readonly WeChatSilentLogin login;readonly IWeChatProfileFunction function;
        readonly Func<TimeSpan,CancellationToken,Task> delay;
        CancellationTokenSource lifetime=new CancellationTokenSource();bool disposed;long generation;
        public WeChatProfileTransport(WeChatRuntimeConfig config,WeChatSilentLogin login,IWeChatProfileFunction function=null,
            Func<TimeSpan,CancellationToken,Task> delay=null)
        {this.config=config??throw new ArgumentNullException(nameof(config));this.login=login??throw new ArgumentNullException(nameof(login));this.function=function;this.delay=delay??((time,token)=>Task.Delay(time,token));}
        public Task<ProfileSyncResponse> SyncAsync(ProfileDocument upload)=>SyncAsync(upload,CancellationToken.None);
        public async Task<ProfileSyncResponse> SyncAsync(ProfileDocument upload,CancellationToken cancellation)
        {
            if(disposed||cancellation.IsCancellationRequested)return Result(ProfileSyncStatus.Stale);
            if(config.CloudState!=WeChatCapabilityState.Ready)return Result(ProfileSyncStatus.NotConfigured);
            if(function==null||login.Status!=WeChatLoginStatus.Authenticated||login.Identity==null)return Result(ProfileSyncStatus.Unavailable);
            var identity=login.Identity;long lease=generation,loginLease=login.Generation;
            if(upload==null||upload.environment!=config.environment||upload.account!=identity.AccountId)return Result(ProfileSyncStatus.Unavailable);
            var frozen=ProfileStore.Copy(upload);ProfileStore.Validate(frozen,config.environment,identity.AccountId);
            using(var source=CancellationTokenSource.CreateLinkedTokenSource(cancellation,lifetime.Token))
            {
                source.CancelAfter(TimeSpan.FromSeconds(15));
                try
                {
                    for(int attempt=0;attempt<3;attempt++)
                    {
                        if(Stale())return Result(ProfileSyncStatus.Stale);
                        ProfileSyncResponse response;
                        try
                        {
                            var call=function.SyncAsync(config.cloudEnvironmentId,config.cloudFunctionName,identity,ProfileStore.Copy(frozen),source.Token);
                            var cancelled=new TaskCompletionSource<bool>();
                            using(source.Token.Register(()=>cancelled.TrySetResult(true)))
                            {if(await Task.WhenAny(call,cancelled.Task)!=call)return Result(ProfileSyncStatus.Stale);response=await call;}
                        }
                        catch(OperationCanceledException){return Result(ProfileSyncStatus.Stale);}
                        catch{response=Result(ProfileSyncStatus.Failed);}
                        if(Stale())return Result(ProfileSyncStatus.Stale);
                        if(response!=null&&response.Status!=ProfileSyncStatus.Failed&&response.Status!=ProfileSyncStatus.Offline)return response;
                        if(attempt<2)await delay(TimeSpan.FromMilliseconds(250*(1<<attempt)),source.Token);
                    }
                    return Result(ProfileSyncStatus.Offline);
                }
                catch(OperationCanceledException){return Result(ProfileSyncStatus.Stale);}
                bool Stale()=>disposed||lease!=generation||loginLease!=login.Generation||source.IsCancellationRequested;
            }
        }
        static ProfileSyncResponse Result(ProfileSyncStatus status)=>new ProfileSyncResponse{Status=status};
        public void CancelPending(){generation++;var old=lifetime;lifetime=new CancellationTokenSource();old.Cancel();old.Dispose();}
        public void Dispose(){if(disposed)return;disposed=true;CancelPending();lifetime.Dispose();}
    }
}
