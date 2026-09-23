using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotpotSort.Platform
{
    public enum WeChatLoginStatus { Local, NotConfigured, Unavailable, NeedsServer, Authenticating, Authenticated, Cancelled, Failed }
    public sealed class WeChatIdentity
    {
        public readonly string AccountId;
        public WeChatIdentity(string accountId)
        { if(string.IsNullOrWhiteSpace(accountId))throw new ArgumentException("Missing server account identity");AccountId=accountId; }
    }
    // A trusted backend must exchange the one-use SDK code. No client-derived identity.
    public interface IWeChatIdentityExchange
    { Task<WeChatIdentity> ExchangeAsync(string oneUseCode,CancellationToken cancellation); }
    public interface IWeChatLoginSdk
    { bool Available {get;} void Login(Action<string> success,Action failure); }
    public sealed class WeChatSilentLogin:IDisposable
    {
        readonly WeChatRuntimeConfig config;readonly IWeChatLoginSdk sdk;readonly IWeChatIdentityExchange exchange;
        CancellationTokenSource pending;long generation;bool disposed;
        public WeChatLoginStatus Status {get;private set;}=WeChatLoginStatus.Local;
        public WeChatIdentity Identity {get;private set;}
        public long Generation=>generation;
        public event Action Changed;
        public WeChatSilentLogin(WeChatRuntimeConfig config,IWeChatLoginSdk sdk=null,IWeChatIdentityExchange exchange=null)
        {this.config=config??throw new ArgumentNullException(nameof(config));this.sdk=sdk??new NativeLogin();this.exchange=exchange;}
        public async Task<WeChatLoginStatus> StartAsync(CancellationToken cancellation=default)
        {
            Cancel();if(disposed)return WeChatLoginStatus.Cancelled;
            long lease=generation;Identity=null;
            if(config.LoginState!=WeChatCapabilityState.Ready)return Set(WeChatLoginStatus.NotConfigured);
            if(!sdk.Available)return Set(WeChatLoginStatus.Unavailable);
            var source=CancellationTokenSource.CreateLinkedTokenSource(cancellation);pending=source;source.CancelAfter(TimeSpan.FromSeconds(10));
            Set(WeChatLoginStatus.Authenticating);
            try
            {
                var completion=new TaskCompletionSource<string>();
                using(source.Token.Register(()=>completion.TrySetCanceled()))
                {
                    sdk.Login(code=>{if(lease==generation&&!source.IsCancellationRequested)completion.TrySetResult(code);},
                        ()=>{if(lease==generation&&!source.IsCancellationRequested)completion.TrySetException(new InvalidOperationException("Silent login failed"));});
                    string code=await completion.Task;
                    if(string.IsNullOrEmpty(code))return Set(WeChatLoginStatus.Failed);
                    if(exchange==null)return Set(WeChatLoginStatus.NeedsServer);
                    // Do not retain, persist or log the credential; only the backend may resolve it.
                    var call=exchange.ExchangeAsync(code,source.Token);code=null;
                    var cancelled=new TaskCompletionSource<bool>();
                    WeChatIdentity identity;
                    using(source.Token.Register(()=>cancelled.TrySetResult(true)))
                    {if(await Task.WhenAny(call,cancelled.Task)!=call)return lease==generation?Set(WeChatLoginStatus.Cancelled):WeChatLoginStatus.Cancelled;identity=await call;}
                    if(lease!=generation||source.IsCancellationRequested)return WeChatLoginStatus.Cancelled;
                    if(identity==null)return Set(WeChatLoginStatus.NeedsServer);
                    Identity=identity;return Set(WeChatLoginStatus.Authenticated);
                }
            }
            catch(OperationCanceledException){return lease==generation?Set(WeChatLoginStatus.Cancelled):WeChatLoginStatus.Cancelled;}
            catch{return lease==generation?Set(WeChatLoginStatus.Failed):WeChatLoginStatus.Cancelled;}
            finally{if(ReferenceEquals(pending,source))pending=null;source.Dispose();}
        }
        WeChatLoginStatus Set(WeChatLoginStatus status){Status=status;var handlers=Changed;if(handlers!=null)foreach(Action h in handlers.GetInvocationList())try{h();}catch{}return status;}
        public void Cancel(){generation++;var prior=pending;pending=null;Identity=null;prior?.Cancel();if(!disposed)Set(WeChatLoginStatus.Cancelled);}
        public void Dispose(){if(disposed)return;Cancel();disposed=true;Changed=null;}
        sealed class NativeLogin:IWeChatLoginSdk
        {
            public bool Available {
                get {
#if UNITY_WEBGL && !UNITY_EDITOR
                    return true;
#else
                    return false;
#endif
                }
            }
            public void Login(Action<string> success,Action failure)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                WeChatWASM.WX.Login(new WeChatWASM.LoginOption{success=r=>success(r.code),fail=_=>failure()});
#else
                failure();
#endif
            }
        }
    }
}
