using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using HotpotSort.Contracts.RemoteAssets;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using WeChatWASM;
#endif

namespace HotpotSort.Platform
{
    public sealed class WeChatAssetTransport : IRemoteAssetTransport
    {
        readonly string cloudEnvironment;
        public WeChatAssetTransport(string cloudEnvironment=null){this.cloudEnvironment=cloudEnvironment;}
        public async Task<AssetDownloadResult> DownloadAsync(Uri uri,TimeSpan timeout,Action<long> progress,CancellationToken cancel)
        {
            if(uri!=null&&uri.Scheme=="hotpot-local")return ReadPackaged(uri,progress,cancel);
            if(uri!=null&&uri.Scheme=="cloud")return await WeChatCloudAssetDownload.DownloadAsync(cloudEnvironment,uri.OriginalString,timeout,progress,cancel);
#if UNITY_WEBGL && !UNITY_EDITOR
            if(uri==null||uri.Scheme!="https"||uri.UserInfo.Length!=0||uri.Query.Length!=0||uri.Fragment.Length!=0)return new AssetDownloadResult(null,0,AssetError.NotConfigured);
            cancel.ThrowIfCancellationRequested();
            var done=new TaskCompletionSource<AssetDownloadResult>();WXDownloadTask task=null;bool redirected=false;double retryAfter=0;
            using(var registration=cancel.Register(()=>{try{task?.Abort();}catch{}done.TrySetCanceled();}))
            {
                try
                {
                    task=WX.DownloadFile(new DownloadFileOption{
                        url=uri.AbsoluteUri,timeout=Math.Min(60000,Math.Max(1,timeout.TotalMilliseconds)),enableProfile=true,
                        success=result=>{
                            if(cancel.IsCancellationRequested||done.Task.IsCompleted)return;
                            // SDK has no final-URL property. Require profile evidence and reject ANY redirect;
                            // never assume an auto-followed redirect retained the original origin.
                            bool verified=!redirected&&result.profile!=null&&result.profile.redirectStart==0&&result.profile.redirectEnd==0;
                            if(!verified){done.TrySetResult(new AssetDownloadResult(null,0,AssetError.Network,originVerified:false));return;}
                            if(result.statusCode!=200){done.TrySetResult(new AssetDownloadResult(null,(int)result.statusCode,retryAfterSeconds:retryAfter));return;}
                            try{var bytes=WeChatCloudAssetDownload.ReadBinaryFile(result.tempFilePath);done.TrySetResult(new AssetDownloadResult(bytes,200));}
                            catch{done.TrySetResult(new AssetDownloadResult(null,0,AssetError.CacheIO));}
                        },
                        fail=result=>{if(!cancel.IsCancellationRequested)done.TrySetResult(new AssetDownloadResult(null,0,AssetError.Network));}
                    });
                    task.OnProgressUpdate(result=>{if(!cancel.IsCancellationRequested&&!done.Task.IsCompleted)progress?.Invoke((long)result.totalBytesWritten);});
                    task.OnHeadersReceived(result=>{
                        if(cancel.IsCancellationRequested||done.Task.IsCompleted||result.header==null)return;
                        foreach(var pair in result.header)
                        {
                            if(string.Equals(pair.Key,"Location",StringComparison.OrdinalIgnoreCase)){redirected=true;try{task.Abort();}catch{}done.TrySetResult(new AssetDownloadResult(null,0,AssetError.Network,originVerified:false));}
                            if(string.Equals(pair.Key,"Retry-After",StringComparison.OrdinalIgnoreCase))
                            {
                                if(double.TryParse(pair.Value,NumberStyles.Float,CultureInfo.InvariantCulture,out var seconds))retryAfter=Math.Min(30,Math.Max(0,seconds));
                                else if(DateTimeOffset.TryParse(pair.Value,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var until))retryAfter=Math.Min(30,Math.Max(0,(until-DateTimeOffset.UtcNow).TotalSeconds));
                            }
                        }
                    });
                    if(cancel.IsCancellationRequested){try{task.Abort();}catch{}done.TrySetCanceled();}
                    return await done.Task;
                }
                catch(OperationCanceledException){throw;}
                catch{return new AssetDownloadResult(null,0,AssetError.Network);}
                finally{try{task?.OffProgressUpdate();task?.OffHeadersReceived();}catch{}}
            }
#else
            await Task.CompletedTask;
            return new AssetDownloadResult(null,0,AssetError.NotConfigured);
#endif
        }
        static AssetDownloadResult ReadPackaged(Uri uri,Action<long> progress,CancellationToken cancel)
        {
            if(uri.Host!="package"||uri.UserInfo.Length!=0||uri.Query.Length!=0||uri.Fragment.Length!=0)return new AssetDownloadResult(null,0,AssetError.NotConfigured);
            string relative=uri.AbsolutePath.TrimStart('/');
            if(!RemoteAssetSource.IsPackagedPath(relative))return new AssetDownloadResult(null,0,AssetError.NotConfigured);
            cancel.ThrowIfCancellationRequested();
            try
            {
                var resource=Resources.Load<TextAsset>(relative);
                if(resource==null)return new AssetDownloadResult(null,0,AssetError.CacheIO);
                byte[] packaged=resource.bytes;
                cancel.ThrowIfCancellationRequested();progress?.Invoke(packaged.LongLength);
                return new AssetDownloadResult(packaged,200);
            }
            catch(OperationCanceledException){throw;}
            catch{return new AssetDownloadResult(null,0,AssetError.CacheIO);}
        }
    }
    public sealed class WeChatRemoteAssetFileSystem:IRemoteAssetFileSystem
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        readonly WXFileSystemManager fs=WX.GetFileSystemManager();
        readonly string root=WX.env.USER_DATA_PATH+"/hotpot-remote-assets-v1";
        string Resolve(string relative)
        {
            if(!WeChatRuntimeConfig.IsPackagePath(relative)||relative.Contains("%")||relative.Contains("?")||relative.Contains("#"))throw new AssetFailure(AssetError.CacheIO);
            return root+"/"+relative;
        }
        public byte[] Read(string relative)
        {try{string path=Resolve(relative);if(fs.AccessSync(path)!="access:ok")return null;return WeChatCloudAssetDownload.ReadBinaryFile(path);}catch{throw new AssetFailure(AssetError.CacheIO);}}
        public void Write(string relative,byte[] bytes)
        {
            try{string path=Resolve(relative),directory=path.Substring(0,path.LastIndexOf('/'));
                if(fs.AccessSync(directory)!="access:ok"&&fs.MkdirSync(directory,true)!="mkdir:ok")throw new AssetFailure(AssetError.CacheIO);
                if(fs.WriteFileSync(path,bytes)!="ok")throw new AssetFailure(AssetError.CacheIO);
            }catch{throw new AssetFailure(AssetError.CacheIO);}
        }
        public void Move(string source,string destination)
        {try{fs.RenameSync(Resolve(source),Resolve(destination));if(fs.AccessSync(Resolve(destination))!="access:ok"||fs.AccessSync(Resolve(source))=="access:ok")throw new AssetFailure(AssetError.CacheIO);}catch{throw new AssetFailure(AssetError.CacheIO);}}
        public void Delete(string relative)
        {try{var path=Resolve(relative);if(fs.AccessSync(path)=="access:ok")fs.UnlinkSync(path);}catch{throw new AssetFailure(AssetError.CacheIO);}}
#else
        public byte[] Read(string relative)=>throw new AssetFailure(AssetError.CacheIO);
        public void Write(string relative,byte[] bytes)=>throw new AssetFailure(AssetError.CacheIO);
        public void Move(string source,string destination)=>throw new AssetFailure(AssetError.CacheIO);
        public void Delete(string relative)=>throw new AssetFailure(AssetError.CacheIO);
#endif
    }
}
