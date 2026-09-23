using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotpotSort.Contracts.RemoteAssets;
using HotpotSort.Platform.RemoteAssets;

sealed class Sprite {}
static class RemoteAssetQa
{
    static readonly byte[] Bytes=Encoding.UTF8.GetBytes("isolated-bundle-fixture");
    const uint Crc=1234;
    static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
    static RemoteAssetManifest Manifest()=>new RemoteAssetManifest{schemaVersion=1,releaseId="release-1",unityVersion="6000.0.26f1",buildTarget="WebGL",compatibilityFingerprint=new string('a',64),localAssetSetHash=new string('b',64),themeSha256=new string('c',64),fullAssetSetHash=new string('d',64),bundle=new RemoteAssetBundle{relativePath="release-1/presentation.bundle",byteLength=Bytes.Length,sha256=RemoteAssetValidation.Sha(Bytes),crc32=Crc},assets=Enumerable.Range(0,31).Select(i=>new RemoteAssetEntry{logicalPath="art/"+i,bundleAssetName="assets/"+i+".png",type="Sprite"}).ToArray()};
    static AssetReleaseIdentity Identity(RemoteAssetManifest m)=>new AssetReleaseIdentity("development",m.releaseId,m.unityVersion,m.buildTarget,m.compatibilityFingerprint,m.localAssetSetHash,m.themeSha256,m.fullAssetSetHash,m.assets.ToDictionary(a=>a.logicalPath,a=>a.type));
    sealed class Local:ILocalAssetSource { public int Calls;public object Load(string path,Type type){Calls++;return path=="local/title"&&type==typeof(Sprite)?new Sprite():null;} }
    sealed class Loader:IRemoteAssetBundleLoader
    {
        public int Calls,Disposed;public bool Missing;public TaskCompletionSource<ILoadedAssetBundle> Pending;
        public Task<ILoadedAssetBundle> LoadAsync(byte[] bytes,uint crc,CancellationToken cancel){Calls++;if(crc!=Crc)throw new AssetFailure(AssetError.Integrity);return Pending?.Task??Task.FromResult<ILoadedAssetBundle>(new Loaded(this));}
        public sealed class Loaded:ILoadedAssetBundle {readonly Loader owner;public Loaded(Loader owner){this.owner=owner;}public object Load(string name,string type)=>owner.Missing?null:new Sprite();public void Dispose(){owner.Disposed++;}}
    }
    sealed class Fs:IRemoteAssetFileSystem
    {
        public Dictionary<string,byte[]> Files=new Dictionary<string,byte[]>();public bool FailCommit;public string FailWrite;
        public byte[] Read(string key)=>Files.TryGetValue(key,out var bytes)?bytes.ToArray():null;
        public void Write(string key,byte[] bytes){if(FailWrite!=null&&key.EndsWith(FailWrite))throw new Exception("isolated IO failure");Files.Add(key,bytes.ToArray());}
        public void Move(string from,string to){if(FailCommit&&to.EndsWith("/commit"))throw new Exception("isolated commit failure");Files[to]=Files[from];Files.Remove(from);}
        public void Delete(string key)=>Files.Remove(key);
    }
    sealed class Delay:IAssetDelay
    {
        public readonly List<double> Backoff=new List<double>();public TaskCompletionSource<bool> Deadline;
        public Task WaitAsync(TimeSpan duration,CancellationToken cancel)
        {
            if(duration.TotalSeconds==60){var t=new TaskCompletionSource<bool>();Deadline=t;cancel.Register(()=>t.TrySetCanceled());return t.Task;}
            Backoff.Add(duration.TotalSeconds);cancel.ThrowIfCancellationRequested();return Task.CompletedTask;
        }
    }
    sealed class Transport:IRemoteAssetTransport
    {
        public int Calls,Active,MaxActive;public bool Hold;public TaskCompletionSource<AssetDownloadResult> Pending;
        public Uri LastUri;public Action<long> Progress;public Queue<AssetDownloadResult> Results=new Queue<AssetDownloadResult>();
        public Task<AssetDownloadResult> DownloadAsync(Uri uri,TimeSpan timeout,Action<long> progress,CancellationToken cancel)
        {
            LastUri=uri;Check(timeout.TotalSeconds==60,"timeout contract");Calls++;Active++;MaxActive=Math.Max(MaxActive,Active);Progress=progress;
            if(!Hold){Active--;return Task.FromResult(Results.Count>0?Results.Dequeue():new AssetDownloadResult(Bytes,200));}
            var pending=Pending=new TaskCompletionSource<AssetDownloadResult>();cancel.Register(()=>{Active--;pending.TrySetCanceled();});return pending.Task;
        }
    }
    sealed class Rig:IDisposable
    {
        public RemoteAssetManifest M=Manifest();public Local Local=new Local();public Fs Fs;public Loader Loader=new Loader();public Transport Net=new Transport();public Delay Delay=new Delay();public PresentationAssetProvider Provider;
        public Rig(Fs fs=null){Fs=fs??new Fs();}
        public PresentationAssetProvider Build(string url="https://assets.example.invalid/hotpot/",RemoteAssetSource source=null){Provider=new PresentationAssetProvider(M,Identity(Manifest()),url,Local,Net,new TransactionalAssetCache(Fs),Loader,Delay,source);return Provider;}
        public void Dispose()=>Provider?.Dispose();
    }
    static async Task Fails(Action<Rig> setup,AssetError error)
    {using(var r=new Rig()){setup(r);var state=await r.Build().PrepareAsync();Check(state.State==AssetReadiness.Failed&&state.Error==error,"expected failure "+error+" got "+state.State+"/"+state.Error);}}
    static void Missing(Action action){try{action();throw new Exception("lookup incorrectly succeeded");}catch(AssetFailure e){Check(e.Code==AssetError.MissingAsset,"typed missing error");}}
    static async Task Main(string[] args)
    {
        int groups=0;
        async Task Run(string name,Func<Task> test){await test();groups++;Console.WriteLine(name+" PASS");}
        await Run("R01-manifest-and-URL-security",async()=>{
            foreach(var mutate in new Action<RemoteAssetManifest>[] {m=>m.schemaVersion=2,m=>m.assets=m.assets.Take(30).ToArray(),m=>m.assets[1].logicalPath=m.assets[0].logicalPath,m=>m.assets[0].type="Font",m=>m.bundle.relativePath="../bad",m=>m.assets[0].bundleAssetName="https://other/b",m=>m.bundle.crc32=0})await Fails(r=>mutate(r.M),AssetError.InvalidManifest);
            await Fails(r=>r.M.releaseId="old-release",AssetError.IncompatibleRelease);
            foreach(string url in new[]{"","http://assets.invalid/","https://a:b@assets.invalid/","https://assets.invalid/?token=x","https://assets.invalid/#x","https://assets.invalid/a/../b","https://assets.invalid/%2e%2e/b","https://assets.invalid/a\\b"})using(var r=new Rig()){var p=r.Build(url);Check((await p.PrepareAsync()).Error==AssetError.NotConfigured&&r.Net.Calls==0,"unsafe configuration must not request");Check(p.GetLocal<Sprite>("local/title")!=null,"local entry preserved");}
        });
        await Run("R02-cold-warm-typed-readiness-and-immutable-input",async()=>{
            var cloud=RemoteAssetSource.Select(null,"https://assets.example.invalid/","fixture-env","cloud://fixture-env.fixture-bucket/release/bundle");
            var cloudFs=new Fs();using(var r=new Rig(cloudFs)){Check((await r.Build(source:cloud).PrepareAsync()).State==AssetReadiness.Ready&&r.Net.Calls==1&&r.Net.LastUri.OriginalString==cloud.CloudFileId,"exact cloud source");}
            using(var r=new Rig(cloudFs)){Check((await r.Build(source:cloud).PrepareAsync()).State==AssetReadiness.Ready&&r.Net.Calls==0,"cloud warm cache must not initialize or request");}
            var fs=new Fs();using(var r=new Rig(fs)){var p=r.Build();Missing(()=>p.GetComplete<Sprite>("art/0"));Missing(()=>p.GetLocal<Sprite>("art/0"));r.M.bundle.sha256=new string('0',64);var state=await p.PrepareAsync();Check(state.State==AssetReadiness.Ready&&r.Net.Calls==1&&p.GetComplete<Sprite>("art/0")!=null,"cold prepare");Missing(()=>p.GetComplete<string>("art/0"));Check(p.GetComplete<Sprite>("local/title")!=null,"local complete lookup");r.Net.Progress(1);Check(p.Snapshot.State==AssetReadiness.Ready,"late progress regressed Ready");}
            using(var r=new Rig(fs)){Check((await r.Build().PrepareAsync()).State==AssetReadiness.Ready&&r.Net.Calls==0&&r.Loader.Calls==1,"warm cache must revalidate/load");}
        });
        await Run("R03-network-retry-policy",async()=>{
            using(var r=new Rig()){r.Net.Results.Enqueue(new AssetDownloadResult(null,408));r.Net.Results.Enqueue(new AssetDownloadResult(null,429,retryAfterSeconds:100));Check((await r.Build().PrepareAsync()).State==AssetReadiness.Ready,"retry eventual success");Check(r.Net.Calls==3&&r.Delay.Backoff.SequenceEqual(new[]{1d,30d}),"retry limits/backoff");}
            using(var r=new Rig()){for(int i=0;i<3;i++)r.Net.Results.Enqueue(new AssetDownloadResult(null,500));Check((await r.Build().PrepareAsync()).Error==AssetError.HttpStatus&&r.Net.Calls==3&&r.Delay.Backoff.SequenceEqual(new[]{1d,3d}),"5xx capped retry");}
            using(var r=new Rig()){r.Net.Results.Enqueue(new AssetDownloadResult(null,404));Check((await r.Build().PrepareAsync()).Error==AssetError.HttpStatus&&r.Net.Calls==1,"404 must not retry");}
            using(var r=new Rig()){r.Net.Results.Enqueue(new AssetDownloadResult(Bytes,200,originVerified:false));Check((await r.Build().PrepareAsync()).State==AssetReadiness.Failed&&r.Net.Calls==1,"redirect origin fail closed");}
        });
        await Run("R04-single-flight-cancel-generation-and-dispose",async()=>{
            using(var r=new Rig()){r.Net.Hold=true;var p=r.Build();var first=p.PrepareAsync();Check(ReferenceEquals(first,p.PrepareAsync())&&r.Net.Calls==1,"duplicate prepare");var stale=r.Net.Progress;p.Cancel();Check((await first).State==AssetReadiness.Cancelled,"cancel pending");r.Net.Hold=false;var next=await p.RetryAsync();Check(next.Generation==2&&next.State==AssetReadiness.Ready&&r.Net.MaxActive==1,"retry generation/concurrency");stale(100);Check(p.Snapshot.State==AssetReadiness.Ready,"old progress changed generation");p.Dispose();Missing(()=>p.GetComplete<Sprite>("art/0"));Check((await p.PrepareAsync()).State==AssetReadiness.Cancelled,"dispose final state");}
            using(var r=new Rig()){r.Loader.Pending=new TaskCompletionSource<ILoadedAssetBundle>();var p=r.Build();var pending=p.PrepareAsync();p.Cancel();r.Loader.Pending.SetResult(new Loader.Loaded(r.Loader));Check((await pending).State==AssetReadiness.Cancelled&&r.Fs.Files.Count==0&&r.Loader.Disposed==1,"late loaded bundle must be disposed/no commit");}
        });
        await Run("R05-integrity-missing-asset-before-commit",async()=>{
            await Fails(r=>r.M.bundle.sha256=new string('0',64),AssetError.Integrity);
            await Fails(r=>r.M.bundle.byteLength++,AssetError.Integrity);
            await Fails(r=>r.M.bundle.crc32++,AssetError.Integrity);
            using(var r=new Rig()){r.Loader.Missing=true;Check((await r.Build().PrepareAsync()).Error==AssetError.MissingAsset&&r.Fs.Files.Count==0&&r.Net.Calls==1,"missing asset cannot commit/retry");}
        });
        await Run("R06-corrupt-partial-cache-and-commit-recovery",async()=>{
            var fs=new Fs();using(var r=new Rig(fs)){await r.Build().PrepareAsync();}
            string commit=fs.Files.Keys.Single(k=>k.EndsWith("/commit"));var committed=fs.Files[commit].ToArray();string payload=fs.Files.Keys.Single(k=>k.EndsWith(".bundle"));
            string partition=RemoteAssetValidation.Partition(Identity(Manifest()),Manifest());
            fs.FailCommit=true;var cache=new TransactionalAssetCache(fs);try{cache.Commit(partition,Bytes,CancellationToken.None);throw new Exception("commit failure missing");}catch(Exception e){Check(e.Message!="commit failure missing","expected IO failure");}
            Check(fs.Files[commit].SequenceEqual(committed)&&cache.ReadCommitted(partition).SequenceEqual(Bytes),"valid previous commit damaged");
            fs.FailCommit=false;fs.Files[payload]=new byte[]{0};using(var r=new Rig(fs)){Check((await r.Build().PrepareAsync()).State==AssetReadiness.Ready&&r.Net.Calls==1,"corrupt cache did not redownload");}
            fs.Files.Remove(commit);using(var r=new Rig(fs)){Check((await r.Build().PrepareAsync()).State==AssetReadiness.Ready&&r.Net.Calls==1,"uncommitted payload trusted");}
            using(var r=new Rig()){r.Fs.FailWrite=".record";var p=r.Build();Check((await p.PrepareAsync()).State==AssetReadiness.Ready&&p.GetComplete<Sprite>("art/0")!=null&&r.Fs.Files.Count==0,"verified release blocked by optional cache write failure");}
        });
        await Run("R07-controlled-timeout-and-retry",async()=>{
            using(var r=new Rig()){r.Net.Hold=true;var p=r.Build();var pending=p.PrepareAsync();r.Net.Hold=false;r.Delay.Deadline.SetResult(true);Check((await pending).State==AssetReadiness.Ready&&r.Net.Calls==2&&r.Net.MaxActive==1&&r.Delay.Backoff.SequenceEqual(new[]{1d}),"timeout retry/abort");}
        });
        await Run("R08-versioned-theme-cardinality-and-tampering",()=>{
            foreach(int count in new[]{31,34})
            {
                RemoteAssetManifest Create(){var m=Manifest();m.assets=Enumerable.Range(0,count).Select(i=>new RemoteAssetEntry{logicalPath="art/"+i,bundleAssetName="assets/"+i+".png",type="Sprite"}).ToArray();return m;}
                var valid=Create();var expected=Identity(valid);
                Check(RemoteAssetValidation.FreezeAndValidate(valid,expected).assets.Length==count,"valid theme cardinality "+count);
                void Reject(Action<RemoteAssetManifest> change){var bad=Create();change(bad);bool rejected=false;try{RemoteAssetValidation.FreezeAndValidate(bad,expected);}catch(AssetFailure){rejected=true;}Check(rejected,"invalid manifest accepted "+count);}
                Reject(m=>m.assets=m.assets.Skip(1).ToArray());
                Reject(m=>m.assets=m.assets.Concat(new[]{new RemoteAssetEntry{logicalPath="extra",bundleAssetName="extra",type="Sprite"}}).ToArray());
                Reject(m=>m.assets[1]=m.assets[0]);Reject(m=>m.assets[0].logicalPath="unexpected");Reject(m=>m.assets[0].type="Font");
                Reject(m=>m.themeSha256=new string('e',64));Reject(m=>m.compatibilityFingerprint=new string('e',64));
            }
            return Task.CompletedTask;
        });
        Console.WriteLine("REMOTE_ASSET_CORE_SMOKE_PASS groups="+groups+" externalRequests=0 gameplayIntegration=false");
    }
}
