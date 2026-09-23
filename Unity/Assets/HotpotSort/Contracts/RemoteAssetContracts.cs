using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotpotSort.Contracts.RemoteAssets
{
    public enum AssetReadiness { LocalReady, CheckingCache, Downloading, Verifying, Loading, Ready, Failed, Cancelled }
    public enum AssetError { None, NotConfigured, InvalidManifest, IncompatibleRelease, Network, Timeout, HttpStatus, Integrity, CacheIO, BundleLoad, MissingAsset }
    public sealed class AssetFailure : Exception
    {
        public AssetError Code { get; }
        public AssetFailure(AssetError code) : base("Presentation asset operation: " + code) { Code = code; }
    }
    public sealed class AssetPreparationSnapshot
    {
        public long Generation { get; }
        public string ReleaseId { get; }
        public AssetReadiness State { get; }
        public AssetError Error { get; }
        public long DownloadedBytes { get; }
        public long TotalBytes { get; }
        public int Attempt { get; }
        public AssetPreparationSnapshot(long generation, string release, AssetReadiness state, AssetError error, long downloaded, long total, int attempt)
        { Generation=generation; ReleaseId=release ?? ""; State=state; Error=error; DownloadedBytes=downloaded; TotalBytes=total; Attempt=attempt; }
    }
    public interface IPresentationAssetProvider : IDisposable
    {
        AssetPreparationSnapshot Snapshot { get; }
        event Action<AssetPreparationSnapshot> Changed;
        // Single-flight including Failed/Cancelled. Only explicit Retry starts another generation.
        Task<AssetPreparationSnapshot> PrepareAsync();
        Task<AssetPreparationSnapshot> RetryAsync();
        void Cancel();
        T GetLocal<T>(string logicalPath) where T : class;
        T GetComplete<T>(string logicalPath) where T : class;
    }
    [Serializable] public sealed class RemoteAssetManifest
    {
        public int schemaVersion;
        public string releaseId, unityVersion, buildTarget, compatibilityFingerprint;
        public RemoteAssetBundle bundle;
        public RemoteAssetEntry[] assets;
        public string localAssetSetHash, themeSha256, fullAssetSetHash;
    }
    [Serializable] public sealed class RemoteAssetBundle
    {
        public string relativePath, sha256;
        public long byteLength;
        // Unity BuildPipeline.GetCRCForAssetBundle CRC of uncompressed bundle data, not compressed-file CRC.
        public uint crc32;
    }
    [Serializable] public sealed class RemoteAssetEntry { public string logicalPath, bundleAssetName, type; }
    public sealed class AssetDownloadResult
    {
        public byte[] Bytes { get; }
        public int StatusCode { get; }
        public AssetError Error { get; }
        public double RetryAfterSeconds { get; }
        public bool OriginVerified { get; }
        public AssetDownloadResult(byte[] bytes, int status, AssetError error=AssetError.None, double retryAfterSeconds=0, bool originVerified=true)
        { Bytes=bytes; StatusCode=status; Error=error; RetryAfterSeconds=retryAfterSeconds; OriginVerified=originVerified; }
    }
    public interface IRemoteAssetTransport
    {
        // Exactly one request; no SDK/application retries. HTTPS rejects redirects; cloud:// attests
        // the exact configured file ID and environment. Cancellation must abort the native task.
        Task<AssetDownloadResult> DownloadAsync(Uri uri, TimeSpan timeout, Action<long> progress, CancellationToken cancel);
    }
    // Immutable, package-selected source. No runtime fallback on a failed request.
    public sealed class RemoteAssetSource
    {
        public readonly string Mode, BaseUrl, CloudEnvironment, CloudFileId;
        public RemoteAssetSource(string mode,string baseUrl,string cloudEnvironment,string cloudFileId)
        { Mode=mode;BaseUrl=baseUrl??"";CloudEnvironment=cloudEnvironment??"";CloudFileId=cloudFileId??""; }
        public static RemoteAssetSource Select(string mode,string baseUrl,string environment,string fileId)
        {
            if(string.IsNullOrEmpty(mode))mode=string.IsNullOrEmpty(fileId)?"Https":"CloudFile";
            return new RemoteAssetSource(mode,mode=="Https"?baseUrl:"",mode=="CloudFile"?environment:"",mode=="CloudFile"?fileId:"");
        }
        public static bool IsCloudFile(string fileId,string environment)
        {
            if(string.IsNullOrEmpty(environment)||environment.Length>128||string.IsNullOrEmpty(fileId)||fileId.Length>1024)return false;
            foreach(char c in environment)if(!AsciiIdentifier(c))return false;
            string prefix="cloud://"+environment+".";
            if(!fileId.StartsWith(prefix,StringComparison.Ordinal))return false;
            int slash=fileId.IndexOf('/',prefix.Length);
            if(slash<=prefix.Length||slash==fileId.Length-1)return false;
            for(int i=prefix.Length;i<slash;i++)if(!AsciiIdentifier(fileId[i]))return false;
            string path=fileId.Substring(slash+1);
            foreach(char c in path)if(char.IsWhiteSpace(c)||char.IsControl(c)||"\\:%?#@".IndexOf(c)>=0)return false;
            foreach(string segment in path.Split('/'))if(segment.Length==0||segment=="."||segment=="..")return false;
            return Uri.TryCreate(fileId,UriKind.Absolute,out var uri)&&uri.Scheme=="cloud"&&uri.Port==-1;
        }
        static bool AsciiIdentifier(char c)=>(c>='a'&&c<='z')||(c>='A'&&c<='Z')||(c>='0'&&c<='9')||c=='-'||c=='_';
        public Uri CloudUri()
        {
            if(Mode!="CloudFile"||BaseUrl.Length!=0||!IsCloudFile(CloudFileId,CloudEnvironment))throw new AssetFailure(AssetError.NotConfigured);
            return new Uri(CloudFileId,UriKind.Absolute);
        }
    }
    public interface IAssetDelay { Task WaitAsync(TimeSpan duration, CancellationToken cancel); }
    public interface ILocalAssetSource { object Load(string logicalPath, Type type); }
    public interface ILoadedAssetBundle : IDisposable { object Load(string bundleAssetName, string declaredType); }
    public interface IRemoteAssetBundleLoader
    {
        // Must validate the manifest's Unity CRC, including on warm cache loads.
        Task<ILoadedAssetBundle> LoadAsync(byte[] bytes, uint unityCrc, CancellationToken cancel);
    }
    public interface IRemoteAssetFileSystem
    {
        // Paths are relative to one app-owned remote-asset namespace, never arbitrary SDK cache paths.
        byte[] Read(string relativePath);
        void Write(string relativePath, byte[] bytes);
        void Move(string sourceRelativePath, string destinationRelativePath);
        void Delete(string relativePath);
    }
    public interface IRemoteAssetCache
    {
        byte[] ReadCommitted(string partition);
        void Commit(string partition, byte[] verifiedBytes, CancellationToken cancel);
    }
}
