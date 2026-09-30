using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using HotpotSort.Platform;
using HotpotSort.Contracts.RemoteAssets;

namespace HotpotSort.Build
{
    public static class RemoteAssetExportGuard
    {
        public const string ManifestFile="hotpot/remote-assets-manifest.json";
        public const string ConfigFile="hotpot/remote-assets-config.json";
        public static bool Active=>File.Exists(Task002V10BundleTool.Marker);
        public static bool SizeProbe=>Active&&Environment.GetEnvironmentVariable("HOTPOT_ASSET_SIZE_PROBE")=="1";
        [Serializable] sealed class Config {public int schemaVersion=2;public string releaseId,manifestSha256,baseUrl,sourceMode,cloudEnvironment,cloudFileId,packagedPath;public bool sizeProbe;}
        public static void Preflight()
        {
            if(!Active)return;
            string path=Environment.GetEnvironmentVariable("HOTPOT_REMOTE_MANIFEST");
            if(string.IsNullOrWhiteSpace(path)||!File.Exists(path))throw new InvalidOperationException("Fixed remote manifest is required");
            var m=JsonUtility.FromJson<Task002V10BundleTool.Release>(File.ReadAllText(path));
            if(m.schemaVersion!=1||m.buildTarget!="WebGL"||m.unityVersion!=Application.unityVersion||m.assets==null||m.assets.Length==0||m.assets.Select(a=>a.logicalPath).Distinct().Count()!=m.assets.Length||!Regex.IsMatch(m.releaseId??"","^[a-f0-9]{64}$"))throw new InvalidOperationException("Remote manifest incompatible");
            if(m.bundle==null||m.bundle.relativePath!=m.releaseId+"/hotpot-remote.bundle")throw new InvalidOperationException("Unsafe remote bundle path");
            var plan=JsonUtility.FromJson<Task002V10BundleTool.Plan>(File.ReadAllText(Task002V10BundleTool.Marker));
            var expected=plan.assets.Where(a=>a.remote).Select(a=>a.logicalPath).OrderBy(a=>a,StringComparer.Ordinal);
            if(!m.assets.Select(a=>a.logicalPath).OrderBy(a=>a,StringComparer.Ordinal).SequenceEqual(expected)||m.assets.Any(a=>a.type!="Texture2D"||a.bundleAssetName!=a.logicalPath.ToLowerInvariant())||m.themeSha256!=plan.themeSha256)throw new InvalidOperationException("Remote asset mapping differs from frozen staging plan");
            string SetHash(bool localOnly)=>Task002V10BundleTool.Hash(Encoding.UTF8.GetBytes(string.Join("\n",plan.assets.Where(a=>!localOnly||!a.remote).OrderBy(a=>a.logicalPath,StringComparer.Ordinal).Select(a=>a.logicalPath+"|"+a.sha256+"|"+a.metaSha256))));
            string fingerprint=Task002V10BundleTool.Hash(Encoding.UTF8.GetBytes("1|"+Application.unityVersion+"|WebGL|"+PlayerSettings.colorSpace+"|"+string.Join(",",PlayerSettings.GetGraphicsAPIs(BuildTarget.WebGL).Select(a=>a.ToString()))));
            string release=Task002V10BundleTool.Hash(Encoding.UTF8.GetBytes("1|"+fingerprint+"|"+SetHash(false)+"|"+SetHash(true)+"|"+plan.themeSha256));
            if(m.compatibilityFingerprint!=fingerprint||m.localAssetSetHash!=SetHash(true)||m.fullAssetSetHash!=SetHash(false)||m.releaseId!=release)throw new InvalidOperationException("Remote manifest content identity mismatch");
            string bundle=Path.Combine(Path.GetDirectoryName(path),"hotpot-remote.bundle");
            if(!File.Exists(bundle)||new FileInfo(bundle).Length!=m.bundle.byteLength||Task002V10BundleTool.Digest(bundle)!=m.bundle.sha256)throw new InvalidOperationException("Deployment bundle integrity failed");
            // Deployment deliberately omits Unity's build-only .manifest sidecar.
            // Validate the payload CRC through the loader, not the sidecar lookup API.
            var crcProbe=AssetBundle.LoadFromFile(bundle,m.bundle.crc32);
            if(crcProbe==null)throw new InvalidOperationException("Deployment bundle CRC failed");
            crcProbe.Unload(true);
            if(!SizeProbe)
            {
                // Keep validating the private build binding, but do not stage a second
                // copy of the bundle. The formal player now consumes the complete
                // approved theme directly from Unity Resources.
                ValidateSource();
            }
        }
        public static WeChatRuntimeConfig ValidateSource()
        {
            WeChatRuntimeConfig config;
            try{config=WeChatRuntimeConfig.FromEnvironment(Environment.GetEnvironmentVariable);}
            catch{throw new InvalidOperationException("Remote asset source configuration invalid");}
            if(config.AssetState!=WeChatCapabilityState.Ready)throw new InvalidOperationException("Remote asset source configuration missing");
            if(config.AssetSource.Mode=="Https")ValidateBaseUrl(config.AssetSource.BaseUrl);
            if(config.AssetSource.Mode=="Packaged")
            {
                string expected="Hotpot/PackagedAssets/"+ReleaseId()+"/hotpot-assets";
                if(config.AssetSource.PackagedPath!=expected)throw new InvalidOperationException("Packaged asset path differs from fixed release path");
            }
            return config;
        }
        static string ReleaseId()
        {
            string path=Environment.GetEnvironmentVariable("HOTPOT_REMOTE_MANIFEST");
            if(string.IsNullOrWhiteSpace(path)||!File.Exists(path))return "";
            return JsonUtility.FromJson<Task002V10BundleTool.Release>(File.ReadAllText(path))?.releaseId??"";
        }
        static void StagePackagedResource(Task002V10BundleTool.Release manifest,string bundle,string packagedPath)
        {
            string expected="Hotpot/PackagedAssets/"+manifest.releaseId+"/hotpot-assets";
            if(packagedPath!=expected||!RemoteAssetSource.IsPackagedPath(packagedPath))throw new InvalidOperationException("Unsafe packaged bundle path");
            string resourceRoot=Path.GetFullPath(Path.Combine(Application.dataPath,"HotpotSort","Resources"));
            string target=Path.GetFullPath(Path.Combine(resourceRoot,(packagedPath+".bytes").Replace('/',Path.DirectorySeparatorChar)));
            string prefix=resourceRoot.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            if(!target.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Packaged bundle escaped Resources");
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.Copy(bundle,target,true);
            if(new FileInfo(target).Length!=manifest.bundle.byteLength||Task002V10BundleTool.Digest(target)!=manifest.bundle.sha256)throw new InvalidOperationException("Packaged bundle staging integrity failed");
            string assetPath="Assets"+target.Substring(Application.dataPath.Length).Replace('\\','/');
            AssetDatabase.ImportAsset(assetPath,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            var imported=Resources.Load<TextAsset>(packagedPath);
            if(imported==null||imported.bytes.LongLength!=manifest.bundle.byteLength||Task002V10BundleTool.Hash(imported.bytes)!=manifest.bundle.sha256)throw new InvalidOperationException("Packaged TextAsset import integrity failed");
        }
        public static void ValidateBaseUrl(string value)
        {
            if(!WeChatRuntimeConfig.IsAssetBaseUrl(value)||!Uri.TryCreate(value,UriKind.Absolute,out var uri)||uri.IsLoopback)throw new InvalidOperationException("WECHAT_ASSET_BASE_URL must be externally supplied HTTPS without credentials/query/fragment");
        }
        public static void Complete(string package)
        {
            if(!Active)return;Preflight();
            string path=Path.GetFullPath(Environment.GetEnvironmentVariable("HOTPOT_REMOTE_MANIFEST"));
            string packageRoot=Path.GetFullPath(package).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            if(path.StartsWith(packageRoot,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Deployment must remain outside minigame");
            var manifest=JsonUtility.FromJson<Task002V10BundleTool.Release>(File.ReadAllText(path));
            Directory.CreateDirectory(Path.Combine(package,"hotpot"));File.Copy(path,Path.Combine(package,ManifestFile),false);
            if(!SizeProbe)ValidateSource();
            File.WriteAllText(Path.Combine(package,ConfigFile),JsonUtility.ToJson(new Config{releaseId=manifest.releaseId,manifestSha256=Task002V10BundleTool.Digest(path),sourceMode=SizeProbe?null:"NativeResources",sizeProbe=SizeProbe},true),new UTF8Encoding(false));
            if(Directory.GetFiles(package,"*.bundle",SearchOption.AllDirectories).Length!=0||Directory.GetFiles(package,"hotpot-assets.unityweb.bin.br",SearchOption.AllDirectories).Length!=0)throw new InvalidOperationException("Raw bundle duplicated outside Unity data package");
        }
        public static void Smoke()
        {
            int exit=0;var oldUrl=Environment.GetEnvironmentVariable("WECHAT_ASSET_BASE_URL");var oldProbe=Environment.GetEnvironmentVariable("HOTPOT_ASSET_SIZE_PROBE");
            var oldSource=Environment.GetEnvironmentVariable("HOTPOT_REMOTE_SOURCE");var oldFile=Environment.GetEnvironmentVariable("HOTPOT_REMOTE_CLOUD_FILE_ID");var oldPackaged=Environment.GetEnvironmentVariable("HOTPOT_PACKAGED_ASSET_PATH");
            try
            {
                if(!Active)throw new InvalidOperationException("Smoke requires v10 staging");
                Environment.SetEnvironmentVariable("HOTPOT_ASSET_SIZE_PROBE",null);Environment.SetEnvironmentVariable("WECHAT_ASSET_BASE_URL",null);
                Environment.SetEnvironmentVariable("HOTPOT_REMOTE_SOURCE",null);Environment.SetEnvironmentVariable("HOTPOT_REMOTE_CLOUD_FILE_ID",null);Environment.SetEnvironmentVariable("HOTPOT_PACKAGED_ASSET_PATH",null);
                bool rejected=false;try{Preflight();}catch(InvalidOperationException e){if(!e.Message.StartsWith("Remote asset source configuration",StringComparison.Ordinal))throw;rejected=true;}if(!rejected)throw new InvalidOperationException("Missing source accepted for formal export");
                foreach(string invalid in new[]{"http://invalid.example/","https://localhost/","https://user:password@invalid.example/","https://invalid.example/?token=fixture"}){rejected=false;try{ValidateBaseUrl(invalid);}catch(InvalidOperationException){rejected=true;}if(!rejected)throw new InvalidOperationException("Unsafe URL accepted");}
                Environment.SetEnvironmentVariable("HOTPOT_ASSET_SIZE_PROBE","1");Preflight();
                string output=Environment.GetEnvironmentVariable("HOTPOT_V10_OUTPUT");Complete(Path.Combine(output,"controlled-package"));
                Environment.SetEnvironmentVariable("HOTPOT_ASSET_SIZE_PROBE",null);Environment.SetEnvironmentVariable("WECHAT_ASSET_BASE_URL","https://assets.example.invalid/remote/");Preflight();
                File.WriteAllText(Path.Combine(output,"guard-result.json"),"{\"status\":\"PASS\",\"missingUrlRejected\":true,\"unsafeUrlRejected\":true,\"mappingHashCrcValidated\":true,\"networkRequests\":0,\"formalCompletion\":false}");
                Debug.Log("TASK002_V10_EXPORT_GUARD_PASS");
            }
            catch(Exception e){exit=1;Debug.LogError("TASK002_V10_FAILED "+e.GetType().Name+": "+e.Message);}
            finally{Environment.SetEnvironmentVariable("WECHAT_ASSET_BASE_URL",oldUrl);Environment.SetEnvironmentVariable("HOTPOT_ASSET_SIZE_PROBE",oldProbe);Environment.SetEnvironmentVariable("HOTPOT_REMOTE_SOURCE",oldSource);Environment.SetEnvironmentVariable("HOTPOT_REMOTE_CLOUD_FILE_ID",oldFile);Environment.SetEnvironmentVariable("HOTPOT_PACKAGED_ASSET_PATH",oldPackaged);if(Application.isBatchMode)EditorApplication.Exit(exit);}
        }
    }
}
