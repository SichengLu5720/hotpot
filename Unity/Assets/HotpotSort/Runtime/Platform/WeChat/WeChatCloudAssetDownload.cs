using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HotpotSort.Contracts.RemoteAssets;

namespace HotpotSort.Platform
{
    // Constructing/configuring this adapter never initializes cloud. Only a cache miss calls it.
    public static class WeChatCloudAssetDownload
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int HotpotCloudAsset_FileSize(string path);
        internal static byte[] ReadBinaryFile(string path)
        {
            var fs=WeChatWASM.WX.GetFileSystemManager();
            return ChunkedAssetRead.Read(HotpotCloudAsset_FileSize(path),(offset,count)=>fs.ReadFileSync(path,(long?)offset,(long?)count));
        }
        sealed class NativeBridge:ICloudAssetNativeBridge
        {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void Callback(int id,int kind,double amount,IntPtr path);
        [DllImport("__Internal")] static extern void HotpotCloudAsset_Start(int id,string environment,string fileId,double timeout,Callback callback);
        [DllImport("__Internal")] static extern void HotpotCloudAsset_Abort(int id);
        [DllImport("__Internal")] static extern void HotpotCloudAsset_Release(int id);
        static readonly Dictionary<int,Action<int,double,string>> receivers=new Dictionary<int,Action<int,double,string>>();
        static readonly Callback callback=Receive;
        [AOT.MonoPInvokeCallback(typeof(Callback))]
        static void Receive(int id,int kind,double amount,IntPtr path)
        {
            if(receivers.TryGetValue(id,out var receiver))receiver(kind,amount,Marshal.PtrToStringAnsi(path));
        }
        public void Start(int id,string environment,string fileId,double timeout,Action<int,double,string> receiver)
        {receivers.Add(id,receiver);HotpotCloudAsset_Start(id,environment,fileId,timeout,callback);}
        public void Abort(int id){receivers.Remove(id);HotpotCloudAsset_Abort(id);}
        public void Release(int id){receivers.Remove(id);HotpotCloudAsset_Release(id);}
        public byte[] ReadTemporary(string path)=>ReadBinaryFile(path);
        }
        static readonly CloudAssetDownloadClient client=new CloudAssetDownloadClient(new NativeBridge());
#endif
        public static Task<AssetDownloadResult> DownloadAsync(string environment,string fileId,TimeSpan timeout,Action<long> progress,CancellationToken cancel)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return client.DownloadAsync(environment,fileId,timeout,progress,cancel);
#else
            return Task.FromResult(new AssetDownloadResult(null,0,AssetError.NotConfigured));
#endif
        }
    }
}
