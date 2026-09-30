using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using HotpotSort.Contracts.RemoteAssets;

namespace HotpotSort.Platform.RemoteAssets
{
    public static class UnityRemoteAssetManifest
    {
        public static RemoteAssetManifest Parse(string json)
        {try{return JsonUtility.FromJson<RemoteAssetManifest>(json)??throw new AssetFailure(AssetError.InvalidManifest);}catch{throw new AssetFailure(AssetError.InvalidManifest);}}
    }
    // Explicit local allow-list supplied by the packaged 34-asset catalog; never a generic Resources fallback.
    public sealed class UnityLocalAssetSource:ILocalAssetSource
    {
        readonly Dictionary<string,Type> allowed;
        public UnityLocalAssetSource(IDictionary<string,Type> allowed){this.allowed=new Dictionary<string,Type>(allowed,StringComparer.Ordinal);}
        public object Load(string path,Type type)
        {if(!allowed.TryGetValue(path,out var expected)||(expected!=type&&!(expected==typeof(Texture2D)&&type==typeof(Sprite))))throw new AssetFailure(AssetError.MissingAsset);return Resources.Load(path,type);}
    }
    public sealed class UnityRemoteAssetBundleLoader:IRemoteAssetBundleLoader
    {
        public async Task<ILoadedAssetBundle> LoadAsync(byte[] bytes,uint unityCrc,CancellationToken cancel)
        {
            cancel.ThrowIfCancellationRequested();
            if(unityCrc==0)throw new AssetFailure(AssetError.InvalidManifest); // Unity treats 0 as CRC validation disabled.
            var operation=AssetBundle.LoadFromMemoryAsync(bytes,unityCrc);
            var done=new TaskCompletionSource<bool>();operation.completed+=_=>done.TrySetResult(true);if(operation.isDone)done.TrySetResult(true);
            await done.Task;
            if(cancel.IsCancellationRequested){operation.assetBundle?.Unload(true);cancel.ThrowIfCancellationRequested();}
            if(!operation.assetBundle)throw new AssetFailure(AssetError.BundleLoad);
            return new Loaded(operation.assetBundle);
        }
        sealed class Loaded:ILoadedAssetBundle
        {
            AssetBundle bundle;
            public Loaded(AssetBundle bundle){this.bundle=bundle;}
            public object Load(string name,string type)
            {
                Type actual=type=="Sprite"?typeof(Sprite):type=="Texture2D"?typeof(Texture2D):type=="TextAsset"?typeof(TextAsset):type=="Font"?typeof(Font):null;
                if(actual==null||!bundle)throw new AssetFailure(AssetError.MissingAsset);
                var value=bundle.LoadAsset(name,actual);return value?value:null;
            }
            public void Dispose(){if(bundle){bundle.Unload(true);bundle=null;}}
        }
    }
    // Editor/native optional transport; formal WebGL uses the WX adapter. No UnityWebRequest
    // module dependency is added to the intentionally reduced bootstrap package.
    public sealed class UnityRemoteAssetTransport:IRemoteAssetTransport
    {
        public async Task<AssetDownloadResult> DownloadAsync(Uri uri,TimeSpan timeout,Action<long> progress,CancellationToken cancel)
        {
#if UNITY_EDITOR || !UNITY_WEBGL
            if(uri==null||uri.Scheme!="https"||uri.UserInfo.Length!=0||uri.Query.Length!=0||uri.Fragment.Length!=0)return new AssetDownloadResult(null,0,AssetError.NotConfigured);
            using(var handler=new System.Net.Http.HttpClientHandler{AllowAutoRedirect=false})
            using(var client=new System.Net.Http.HttpClient(handler))
            {
                client.Timeout=timeout;
                using(var response=await client.GetAsync(uri,cancel))
                {
                    int status=(int)response.StatusCode;
                    if(status>=300&&status<400)return new AssetDownloadResult(null,status,AssetError.HttpStatus,originVerified:false);
                    byte[] bytes=status==200?await response.Content.ReadAsByteArrayAsync():null;
                    cancel.ThrowIfCancellationRequested();if(bytes!=null)progress?.Invoke(bytes.Length);
                    double retry=response.Headers.RetryAfter?.Delta?.TotalSeconds??0;
                    return new AssetDownloadResult(bytes,status,retryAfterSeconds:retry);
                }
            }
#else
            await Task.CompletedTask;
            return new AssetDownloadResult(null,0,AssetError.NotConfigured);
#endif
        }
    }
}
