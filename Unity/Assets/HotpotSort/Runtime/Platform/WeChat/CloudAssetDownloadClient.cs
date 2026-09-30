using System;
using System.Threading;
using System.Threading.Tasks;
using HotpotSort.Contracts.RemoteAssets;

namespace HotpotSort.Platform
{
    // Bound each native transfer; DevTools can fail to decode a whole large binary response.
    internal static class ChunkedAssetRead
    {
        internal const int ChunkBytes=256*1024;
        internal static byte[] Read(int size,Func<int,int,byte[]> read)
        {
            if(size<0||read==null)throw new AssetFailure(AssetError.CacheIO);
            var result=new byte[size];
            for(int offset=0;offset<size;)
            {
                int count=Math.Min(ChunkBytes,size-offset);
                var chunk=read(offset,count);
                if(chunk==null||chunk.Length!=count)throw new AssetFailure(AssetError.CacheIO);
                Buffer.BlockCopy(chunk,0,result,offset,count);
                offset+=count;
            }
            return result;
        }
    }
    public interface ICloudAssetNativeBridge
    {
        void Start(int id,string environment,string fileId,double timeout,Action<int,double,string> callback);
        void Abort(int id);
        void Release(int id);
        byte[] ReadTemporary(string path);
    }
    // Platform-neutral ownership/lifecycle used by the native adapter and deterministic fixtures.
    public sealed class CloudAssetDownloadClient
    {
        readonly ICloudAssetNativeBridge bridge;
        static int sequence;
        public CloudAssetDownloadClient(ICloudAssetNativeBridge bridge){this.bridge=bridge??throw new ArgumentNullException(nameof(bridge));}
        public async Task<AssetDownloadResult> DownloadAsync(string environment,string fileId,TimeSpan timeout,Action<long> progress,CancellationToken cancel)
        {
            if(!RemoteAssetSource.IsCloudFile(fileId,environment))return new AssetDownloadResult(null,0,AssetError.NotConfigured);
            cancel.ThrowIfCancellationRequested();
            int id=Interlocked.Increment(ref sequence);if(id<=0)return new AssetDownloadResult(null,0,AssetError.NotConfigured);
            var done=new TaskCompletionSource<AssetDownloadResult>();bool retired=false;
            using(var registration=cancel.Register(()=>{
                if(retired)return;retired=true;
                try{bridge.Abort(id);}catch{}
                done.TrySetCanceled();
            }))
            {
                try{
                    if(!cancel.IsCancellationRequested)bridge.Start(id,environment,fileId,timeout.TotalMilliseconds,(kind,amount,path)=>{
                        if(retired)return;
                        if(kind==0){if(!double.IsNaN(amount)&&amount>=0&&amount<long.MaxValue)try{progress?.Invoke((long)amount);}catch{}return;}
                        retired=true;AssetDownloadResult result;
                        if(kind==1){try{result=new AssetDownloadResult(bridge.ReadTemporary(path),200);}catch{result=new AssetDownloadResult(null,0,AssetError.CacheIO);}}
                        else result=kind==2?new AssetDownloadResult(null,0,AssetError.NotConfigured):kind==3?new AssetDownloadResult(null,403):new AssetDownloadResult(null,0,kind==5?AssetError.Timeout:AssetError.Network);
                        try{bridge.Release(id);}catch{}
                        done.TrySetResult(result);
                    });
                    return await done.Task;
                }
                catch(OperationCanceledException){throw;}
                catch{return new AssetDownloadResult(null,0,AssetError.NotConfigured);}
                finally{retired=true;try{bridge.Abort(id);}catch{}}
            }
        }
    }
}
