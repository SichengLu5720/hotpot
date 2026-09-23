using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HotpotSort.Contracts.RemoteAssets;

namespace HotpotSort.Platform.RemoteAssets
{
    // Trusted values come from the packaged release, never from the network manifest itself.
    public sealed class AssetReleaseIdentity
    {
        public string Environment { get; }
        public string ReleaseId { get; }
        public string UnityVersion { get; }
        public string BuildTarget { get; }
        public string CompatibilityFingerprint { get; }
        public string LocalAssetSetHash { get; }
        public string ThemeSha256 { get; }
        public string FullAssetSetHash { get; }
        public IReadOnlyDictionary<string,string> ExpectedAssets { get; }
        public AssetReleaseIdentity(string environment, string release, string unity, string target, string compatibility, string local, string theme, string full, IDictionary<string,string> expectedAssets)
        {
            Environment=environment; ReleaseId=release; UnityVersion=unity; BuildTarget=target;
            CompatibilityFingerprint=compatibility; LocalAssetSetHash=local; ThemeSha256=theme; FullAssetSetHash=full;
            ExpectedAssets=new System.Collections.ObjectModel.ReadOnlyDictionary<string,string>(new Dictionary<string,string>(expectedAssets, StringComparer.Ordinal));
        }
    }
    public static class RemoteAssetValidation
    {
        public static bool SafePath(string path)
        {
            if(string.IsNullOrEmpty(path) || path.Length>512 || path.StartsWith("/") || path.Contains("\\") || path.Contains(":") || path.Contains("%") || path.Contains("?") || path.Contains("#")) return false;
            if(path.Any(c=>char.IsControl(c) || char.IsWhiteSpace(c)))return false;
            return path.Split('/').All(s=>s.Length>0 && s!="." && s!="..");
        }
        public static Uri BaseUri(string value)
        {
            if(string.IsNullOrWhiteSpace(value))throw new AssetFailure(AssetError.NotConfigured);
            if(!Uri.TryCreate(value,UriKind.Absolute,out var uri) || uri.Scheme!="https" || string.IsNullOrEmpty(uri.Host) || uri.UserInfo.Length!=0 || uri.Query.Length!=0 || uri.Fragment.Length!=0)
                throw new AssetFailure(AssetError.NotConfigured);
            // Inspect the original string before Uri normalizes dot segments, escaped separators, or backslashes.
            int start=value.IndexOf("://",StringComparison.Ordinal)+3;int slash=value.IndexOf('/',start);
            string raw=slash<0?"":value.Substring(slash).Trim('/');
            if(value.Contains("\\") || value.Contains("%") || value.Any(char.IsControl) || (raw.Length>0&&!SafePath(raw)))throw new AssetFailure(AssetError.NotConfigured);
            return new Uri(uri.AbsoluteUri.TrimEnd('/')+"/");
        }
        public static bool Hash(string value)=>value!=null&&value.Length==64&&value.All(c=>(c>='0'&&c<='9')||(c>='a'&&c<='f'));
        public static string Sha(byte[] bytes)
        { using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(); }
        public static RemoteAssetManifest FreezeAndValidate(RemoteAssetManifest m, AssetReleaseIdentity expected)
        {
            if(m==null||expected==null||m.schemaVersion!=1||m.bundle==null||m.assets==null||m.assets.Length==0||m.assets.Length!=expected.ExpectedAssets.Count ||
                !SafePath(m.releaseId)||m.releaseId.Contains("/")||!SafePath(m.bundle.relativePath)||m.bundle.byteLength<=0||m.bundle.byteLength>int.MaxValue||m.bundle.crc32==0||
                !Hash(m.bundle.sha256)||!Hash(m.compatibilityFingerprint)||!Hash(m.localAssetSetHash)||!Hash(m.themeSha256)||!Hash(m.fullAssetSetHash))
                throw new AssetFailure(AssetError.InvalidManifest);
            if(m.releaseId!=expected.ReleaseId||m.unityVersion!=expected.UnityVersion||m.buildTarget!=expected.BuildTarget||m.compatibilityFingerprint!=expected.CompatibilityFingerprint||m.localAssetSetHash!=expected.LocalAssetSetHash||m.themeSha256!=expected.ThemeSha256||m.fullAssetSetHash!=expected.FullAssetSetHash)
                throw new AssetFailure(AssetError.IncompatibleRelease);
            var logical=new HashSet<string>(StringComparer.Ordinal);var names=new HashSet<string>(StringComparer.Ordinal);
            var entries=new List<RemoteAssetEntry>();
            foreach(var a in m.assets)
            {
                if(a==null||!SafePath(a.logicalPath)||!SafePath(a.bundleAssetName)||!logical.Add(a.logicalPath)||!names.Add(a.bundleAssetName)||
                    !expected.ExpectedAssets.TryGetValue(a.logicalPath,out var type)||type!=a.type||(a.type!="Sprite"&&a.type!="Texture2D"&&a.type!="TextAsset"&&a.type!="Font"))
                    throw new AssetFailure(AssetError.InvalidManifest);
                entries.Add(new RemoteAssetEntry{logicalPath=a.logicalPath,bundleAssetName=a.bundleAssetName,type=a.type});
            }
            return new RemoteAssetManifest{schemaVersion=1,releaseId=m.releaseId,unityVersion=m.unityVersion,buildTarget=m.buildTarget,compatibilityFingerprint=m.compatibilityFingerprint,localAssetSetHash=m.localAssetSetHash,themeSha256=m.themeSha256,fullAssetSetHash=m.fullAssetSetHash,
                bundle=new RemoteAssetBundle{relativePath=m.bundle.relativePath,byteLength=m.bundle.byteLength,sha256=m.bundle.sha256,crc32=m.bundle.crc32},assets=entries.ToArray()};
        }
        public static string Partition(AssetReleaseIdentity release, RemoteAssetManifest manifest)
            =>Sha(Encoding.UTF8.GetBytes(release.Environment??""))+"/"+manifest.compatibilityFingerprint+"/"+Sha(Encoding.UTF8.GetBytes(manifest.releaseId))+"/"+manifest.bundle.sha256;
        public static void VerifyBytes(byte[] bytes,RemoteAssetManifest manifest)
        {if(bytes==null||bytes.LongLength!=manifest.bundle.byteLength||Sha(bytes)!=manifest.bundle.sha256)throw new AssetFailure(AssetError.Integrity);}
    }
}
