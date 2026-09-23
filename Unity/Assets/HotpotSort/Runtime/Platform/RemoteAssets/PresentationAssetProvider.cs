using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotpotSort.Contracts.RemoteAssets;

namespace HotpotSort.Platform.RemoteAssets
{
    // Own on the Unity/UI synchronization context; no session, gameplay, or background-resume side effects.
    public sealed class PresentationAssetProvider : IPresentationAssetProvider
    {
        readonly ILocalAssetSource local;
        readonly IRemoteAssetTransport transport;
        readonly IRemoteAssetCache cache;
        readonly IRemoteAssetBundleLoader loader;
        readonly IAssetDelay delay;
        readonly RemoteAssetManifest manifest;
        readonly string partition;
        readonly Uri uri;
        readonly AssetError configurationError;
        readonly HashSet<string> remotePaths;
        readonly SemaphoreSlim serial=new SemaphoreSlim(1,1);
        readonly Dictionary<string,object> complete=new Dictionary<string,object>(StringComparer.Ordinal);
        CancellationTokenSource cancellation;
        Task<AssetPreparationSnapshot> flight;
        ILoadedAssetBundle loaded;
        bool disposed;
        long generation;
        public AssetPreparationSnapshot Snapshot { get; private set; }
        public event Action<AssetPreparationSnapshot> Changed;
        public PresentationAssetProvider(RemoteAssetManifest input, AssetReleaseIdentity expected, string baseUrl, ILocalAssetSource local,
            IRemoteAssetTransport transport, IRemoteAssetCache cache, IRemoteAssetBundleLoader loader, IAssetDelay delay, RemoteAssetSource source=null)
        {
            this.local=local??throw new ArgumentNullException(nameof(local));this.transport=transport??throw new ArgumentNullException(nameof(transport));
            this.cache=cache??throw new ArgumentNullException(nameof(cache));this.loader=loader??throw new ArgumentNullException(nameof(loader));this.delay=delay??throw new ArgumentNullException(nameof(delay));
            remotePaths=new HashSet<string>(expected?.ExpectedAssets.Keys??new string[0],StringComparer.Ordinal);
            Snapshot=new AssetPreparationSnapshot(0,expected?.ReleaseId,AssetReadiness.LocalReady,AssetError.None,0,0,0);
            try{
                source=source??new RemoteAssetSource("Https",baseUrl,"","");
                if(source.Mode!="CloudFile"&&(source.Mode!="Https"||string.IsNullOrWhiteSpace(source.BaseUrl)||source.CloudFileId.Length!=0||source.CloudEnvironment.Length!=0))throw new AssetFailure(AssetError.NotConfigured);
                manifest=RemoteAssetValidation.FreezeAndValidate(input,expected);
                uri=source.Mode=="CloudFile"?source.CloudUri():new Uri(RemoteAssetValidation.BaseUri(source.BaseUrl),manifest.bundle.relativePath);
                partition=RemoteAssetValidation.Partition(expected,manifest);
            }
            catch(AssetFailure e){configurationError=e.Code;}
        }
        public Task<AssetPreparationSnapshot> PrepareAsync()
        {
            if(disposed)return Task.FromResult(Snapshot);
            if(flight!=null)return flight;
            var done=new TaskCompletionSource<AssetPreparationSnapshot>();flight=done.Task;
            cancellation=new CancellationTokenSource();long current=++generation;
            _=RunAndComplete(current,cancellation.Token,done);return done.Task;
        }
        public Task<AssetPreparationSnapshot> RetryAsync()
        {
            if(disposed)return Task.FromResult(Snapshot);
            Cancel();flight=null;complete.Clear();loaded?.Dispose();loaded=null;
            return PrepareAsync();
        }
        bool Current(long current,CancellationToken token)=>!disposed&&current==generation&&!token.IsCancellationRequested;
        void Check(long current,CancellationToken token){if(!Current(current,token))throw new OperationCanceledException();}
        void Publish(long current,AssetReadiness state,AssetError error=AssetError.None,long bytes=0,int attempt=0)
        {
            if(disposed||current!=generation)return;
            Snapshot=new AssetPreparationSnapshot(current,Snapshot.ReleaseId,state,error,bytes,manifest?.bundle.byteLength??0,attempt);
            // Presentation subscribers cannot turn a committed load into failure or leak platform exceptions.
            if(Changed!=null)foreach(Action<AssetPreparationSnapshot> subscriber in Changed.GetInvocationList())try{subscriber(Snapshot);}catch{}
        }
        async Task RunAndComplete(long current,CancellationToken token,TaskCompletionSource<AssetPreparationSnapshot> done)
        {
            ILoadedAssetBundle candidate=null;bool entered=false;
            try
            {
                await serial.WaitAsync(token);entered=true;Check(current,token);
                if(configurationError!=AssetError.None)throw new AssetFailure(configurationError);
                Publish(current,AssetReadiness.CheckingCache);
                byte[] bytes;
                try{bytes=cache.ReadCommitted(partition);}catch{throw new AssetFailure(AssetError.CacheIO);}
                bool cached=bytes!=null;
                if(cached)
                {
                    try{RemoteAssetValidation.VerifyBytes(bytes,manifest);}catch(AssetFailure){bytes=null;cached=false;}
                }
                if(!cached)bytes=await Download(current,token);
                Check(current,token);Publish(current,AssetReadiness.Verifying);RemoteAssetValidation.VerifyBytes(bytes,manifest);
                Check(current,token);Publish(current,AssetReadiness.Loading);
                try{candidate=await loader.LoadAsync(bytes,manifest.bundle.crc32,token);}
                catch(OperationCanceledException){throw;}catch(AssetFailure){throw;}catch{throw new AssetFailure(AssetError.BundleLoad);}
                Check(current,token);if(candidate==null)throw new AssetFailure(AssetError.BundleLoad);
                var verified=new Dictionary<string,object>(StringComparer.Ordinal);
                foreach(var asset in manifest.assets)
                {
                    object value;
                    try{value=candidate.Load(asset.bundleAssetName,asset.type);}catch{throw new AssetFailure(AssetError.MissingAsset);}
                    if(value==null||value.GetType().Name!=asset.type)throw new AssetFailure(AssetError.MissingAsset);
                    verified.Add(asset.logicalPath,value);
                }
                Check(current,token);
                if(!cached)
                {
                    // The release has already passed byte integrity, bundle loading, and complete asset validation.
                    // A device-specific persistent-cache write failure must not discard that verified in-memory release.
                    try{cache.Commit(partition,bytes,token);}catch(OperationCanceledException){throw;}catch{}
                }
                Check(current,token);loaded=candidate;candidate=null;
                foreach(var pair in verified)complete.Add(pair.Key,pair.Value);
                Publish(current,AssetReadiness.Ready,bytes:bytes.Length);
            }
            catch(OperationCanceledException){if(Current(current,token))Publish(current,AssetReadiness.Cancelled);}
            catch(AssetFailure e){if(Current(current,token))Publish(current,AssetReadiness.Failed,e.Code);}
            catch{if(Current(current,token))Publish(current,AssetReadiness.Failed,AssetError.BundleLoad);}
            finally
            {
                candidate?.Dispose();if(entered)serial.Release();
                // An old generation resolves as Cancelled, never with the newer generation's Ready snapshot.
                done.TrySetResult(current==generation&&!token.IsCancellationRequested?Snapshot:new AssetPreparationSnapshot(current,Snapshot.ReleaseId,AssetReadiness.Cancelled,AssetError.None,0,manifest?.bundle.byteLength??0,0));
            }
        }
        async Task<byte[]> Download(long current,CancellationToken token)
        {
            for(int attempt=1;attempt<=3;attempt++)
            {
                Check(current,token);Publish(current,AssetReadiness.Downloading,attempt:attempt);
                AssetDownloadResult result;
                using(var request=CancellationTokenSource.CreateLinkedTokenSource(token))
                using(var timer=CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    int captured=attempt;
                    CancellationToken attemptToken=request.Token;
                    Task<AssetDownloadResult> task;
                    try{task=transport.DownloadAsync(uri,TimeSpan.FromSeconds(60),bytes=>{if(Current(current,attemptToken))Publish(current,AssetReadiness.Downloading,bytes:Math.Max(0,Math.Min(bytes,manifest.bundle.byteLength)),attempt:captured);},attemptToken);}
                    catch{task=Task.FromResult(new AssetDownloadResult(null,0,AssetError.Network));}
                    Task deadline=delay.WaitAsync(TimeSpan.FromSeconds(60),timer.Token);
                    if(await Task.WhenAny(task,deadline)!=task)
                    {
                        request.Cancel();Check(current,token);
                        // Transport contract requires Abort completion on cancellation before another network request.
                        try{await task;}catch{}
                        result=new AssetDownloadResult(null,0,AssetError.Timeout);
                    }
                    else
                    {
                        timer.Cancel();
                        try{result=await task;}catch(OperationCanceledException){Check(current,token);result=new AssetDownloadResult(null,0,AssetError.Timeout);}catch{result=new AssetDownloadResult(null,0,AssetError.Network);}
                    }
                    request.Cancel(); // Retire this attempt's progress callbacks before later states/generations.
                }
                Check(current,token);
                if(!result.OriginVerified)throw new AssetFailure(AssetError.Network);
                if(result.Error==AssetError.None&&result.StatusCode==200)return result.Bytes;
                var error=result.Error!=AssetError.None?result.Error:AssetError.HttpStatus;
                bool retry=error==AssetError.Network||error==AssetError.Timeout||(error==AssetError.HttpStatus&&(result.StatusCode==408||result.StatusCode==429||result.StatusCode>=500&&result.StatusCode<=599));
                if(!retry||attempt==3)throw new AssetFailure(error);
                double wait=attempt==1?1:3;
                if(!double.IsNaN(result.RetryAfterSeconds)&&result.RetryAfterSeconds>0)wait=Math.Max(wait,Math.Min(30,result.RetryAfterSeconds));
                await delay.WaitAsync(TimeSpan.FromSeconds(wait),token);
            }
            throw new AssetFailure(AssetError.Network);
        }
        public void Cancel()
        {
            if(disposed||Snapshot.State==AssetReadiness.Ready)return;
            cancellation?.Cancel();Publish(generation,AssetReadiness.Cancelled);
        }
        public T GetLocal<T>(string logicalPath) where T:class
        {
            if(disposed)throw new AssetFailure(AssetError.MissingAsset);
            // A remote logical path can never accidentally fall back to a historical Resources copy.
            if(remotePaths.Contains(logicalPath))throw new AssetFailure(AssetError.MissingAsset);
            object value;try{value=local.Load(logicalPath,typeof(T));}catch{throw new AssetFailure(AssetError.MissingAsset);}
            if(!(value is T typed))throw new AssetFailure(AssetError.MissingAsset);return typed;
        }
        public T GetComplete<T>(string logicalPath) where T:class
        {
            if(disposed||Snapshot.State!=AssetReadiness.Ready)throw new AssetFailure(AssetError.MissingAsset);
            if(complete.TryGetValue(logicalPath,out var value))return value is T typed?typed:throw new AssetFailure(AssetError.MissingAsset);
            return GetLocal<T>(logicalPath);
        }
        public void Dispose()
        {
            if(disposed)return;Cancel();Publish(generation,AssetReadiness.Cancelled);disposed=true;cancellation?.Dispose();complete.Clear();loaded?.Dispose();loaded=null;Changed=null;
        }
    }
    public sealed class SystemAssetDelay:IAssetDelay
    { public Task WaitAsync(TimeSpan duration,CancellationToken cancel)=>Task.Delay(duration,cancel); }
}
