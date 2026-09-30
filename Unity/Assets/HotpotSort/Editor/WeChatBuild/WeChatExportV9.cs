using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using HotpotSort.Platform;
using HotpotSort.Platform.Editor;
using UnityEditor.Build;
using UnityEngine;

namespace HotpotSort.Build
{
    // No SDK/project settings mutations. Called only after the SDK's real completion signal.
    public static class WeChatExportV9
    {
        public static WeChatRuntimeConfig ReadConfiguration() => WeChatRuntimeConfig.FromEnvironment(Environment.GetEnvironmentVariable);
        public static void Preflight(WeChatRuntimeConfig config)
        {
            config.Validate();
            FontSubsetBuildGuard.Validate();
            if (config.enableFriendBoard) ValidateOpenDataSource();
            if (config.enableShare && !File.Exists(ShareSource())) throw new BuildFailedException("Static share theme missing");
            ValidateIdentity(Environment.GetEnvironmentVariable("WECHAT_APP_ID"));
            RemoteAssetExportGuard.Preflight();
        }
        public static void Complete(string package, WeChatRuntimeConfig config, bool sdkCompletionObserved)
        {
            if(!sdkCompletionObserved) throw new BuildFailedException("SDK completion must be observed before finalizing an export");
            StageRuntimeFiles(package,config);
            RemoteAssetExportGuard.Complete(package);
            File.WriteAllText(Path.Combine(package,"hotpot","export-validation.json"),JsonUtility.ToJson(new PackageEvidence {
                schemaVersion=1, taskVersion=9, sdkCompletionObserved=true,
                runtimeConfigPresent=true, openDataEnabled=config.enableFriendBoard,
                cloudConfigured=config.CloudState==WeChatCapabilityState.Ready,
                adConfigured=config.RewardedVideoState==WeChatCapabilityState.Ready,
                fontManifestSha256=Hash(Path.Combine(package,"hotpot","font-manifest.json")),
                shareImageSha256=config.enableShare?Hash(Path.Combine(package,config.shareImagePath)):""
            },true),new UTF8Encoding(false));
        }
        // Also exercised by controlled packaging tests. This method alone never records SDK completion.
        internal static void StageRuntimeFiles(string package, WeChatRuntimeConfig config)
        {
            config.Validate();
            if(config.enableFriendBoard) ValidateOpenDataSource();
            var appId=Environment.GetEnvironmentVariable("WECHAT_APP_ID");
            ValidateIdentity(appId);
            var projectPath=Path.Combine(package,"project.config.json");
            var project=File.ReadAllText(projectPath);
            var identity=new Regex("\"appid\"\\s*:\\s*\"[^\"]*\"");
            if(identity.Matches(project).Count!=1) throw new BuildFailedException("Invalid generated project identity field");
            // Identity enters only this completed export, never SDK configuration or log text.
            File.WriteAllText(projectPath,identity.Replace(project,_=>"\"appid\": \""+appId+"\""),new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(package,WeChatRuntimeConfig.PackageFile),JsonUtility.ToJson(config,true),new UTF8Encoding(false));
            var hotpot=Path.Combine(package,"hotpot");
            Directory.CreateDirectory(hotpot);
            if(config.enableShare) File.Copy(ShareSource(),Path.Combine(package,config.shareImagePath),true);
            foreach(var name in new[]{"font-manifest.json","OFL.txt"}) File.Copy(FontSubsetBuildGuard.Root+name,Path.Combine(hotpot,name),true);
            if(config.enableFriendBoard)
            {
                // SDK sample files must never survive beside the production domain. Replace only a
                // proven child directory of this fresh export, retaining a recoverable history copy.
                var destination=Path.GetFullPath(Path.Combine(package,"open-data"));
                var expected=Path.GetFullPath(package).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
                if(!destination.StartsWith(expected,StringComparison.OrdinalIgnoreCase)) throw new BuildFailedException("Invalid open-data destination");
                if(Directory.Exists(destination)) Directory.Move(destination,Path.Combine(Path.GetDirectoryName(package),"sdk-open-data-backup-"+Guid.NewGuid().ToString("N")));
                CopyDirectory(OpenDataSource(),destination);
            }
            ValidatePackage(package,config);
            var packaged=WeChatRuntimeConfig.FromJson(File.ReadAllText(Path.Combine(package,WeChatRuntimeConfig.PackageFile)));
            if(JsonUtility.ToJson(packaged)!=JsonUtility.ToJson(config)) throw new BuildFailedException("Packaged runtime configuration differs from export configuration");
            if(config.enableShare && Hash(ShareSource())!=Hash(Path.Combine(package,config.shareImagePath))) throw new BuildFailedException("Packaged share image differs from source");
            if(config.enableFriendBoard)
            {
                ValidateFormalDomain(Path.Combine(package,"open-data"));
                WeChatFriendBoardExportGuard.ValidateExport(package,OpenDataSource());
            }
        }
        public static void ValidatePackage(string package,WeChatRuntimeConfig config)
        {
            if(!File.Exists(Path.Combine(package,WeChatRuntimeConfig.PackageFile))) throw new BuildFailedException("Missing runtime config");
            if(config.enableShare && !File.Exists(Path.Combine(package,config.shareImagePath))) throw new BuildFailedException("Missing packaged share theme");
            if(config.enableFriendBoard)
            {
                var game=File.ReadAllText(Path.Combine(package,"game.json"));
                if(!Regex.IsMatch(game,"\"openDataContext\"\\s*:\\s*\"open-data/?\"")) throw new BuildFailedException("Friend relation / open-data export binding missing");
                var index=Path.Combine(package,"open-data","index.js");
                if(!File.Exists(index)) throw new BuildFailedException("Missing formal open-data entry");
            }
            var imports=new Regex("(?:require\\s*\\(\\s*|from\\s*|import\\s*)['\"](\\.[^'\"]+)['\"]");
            foreach(var js in Directory.GetFiles(package,"*.js",SearchOption.AllDirectories))
                foreach(Match match in imports.Matches(File.ReadAllText(js)))
                {
                    string path=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(js),match.Groups[1].Value));
                    if(!path.StartsWith(Path.GetFullPath(package)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)) throw new BuildFailedException("Packaged module escapes root");
                    if(!File.Exists(path) && !File.Exists(path+".js") && !File.Exists(path+".json") && !File.Exists(Path.Combine(path,"index.js")))
                        throw new BuildFailedException("Missing packaged JavaScript dependency");
                }
        }
        internal static string OpenDataSource()
        {
            string configured=Environment.GetEnvironmentVariable("WECHAT_OPEN_DATA_SOURCE");
            return string.IsNullOrWhiteSpace(configured) ? "Assets/HotpotSort/WeChatOpenData" : configured;
        }
        internal static void ValidateOpenDataSource()
        {
            WeChatFriendBoardExportGuard.ValidateSource(OpenDataSource());
        }
        internal static void ValidateFormalDomain(string destination)
        {
            var source=Path.GetFullPath(OpenDataSource());
            var files=Directory.GetFiles(source,"*",SearchOption.AllDirectories).Where(p=>!p.EndsWith(".meta",StringComparison.OrdinalIgnoreCase)).ToArray();
            var actual=Directory.GetFiles(destination,"*",SearchOption.AllDirectories);
            if(files.Length!=actual.Length) throw new BuildFailedException("Open-data file inventory differs from formal source");
            foreach(var file in files)
            {
                var relative=file.Substring(source.TrimEnd(Path.DirectorySeparatorChar).Length+1);
                var target=Path.Combine(destination,relative);
                if(!File.Exists(target) || Hash(file)!=Hash(target)) throw new BuildFailedException("Open-data bytes differ from formal source");
            }
        }
        private static string ShareSource()
        {
            if(Task002V10BundleTool.LogicalRoot=="Hotpot/TASK001/v10/r001/")
            {
                const string source="Assets/HotpotSort/Resources/Hotpot/TASK001/v10/r001/hero/share.png";
                const string candidate="Assets/HotpotSort/Editor/WeChatBuild/ShareExport/v10-share-theme.png";
                if(!File.Exists(source)||Hash(source)!="f01b2c423282b3c2f5f8d5aad9b52201f742fa1b408fe6cca17ab1c260899d1a"||!File.Exists(candidate)||Hash(candidate)!="d5ffc486e646f288bccc0f2adbd4e9567449b7dfa78d5699323d866d89206e7a")
                    throw new BuildFailedException("v10 share composition or source changed");
                return candidate;
            }
            // The export derivative is PNG-lossless: identical 1200x960 RGBA pixels.
            // Bind both hashes so changing the approved artwork cannot silently ship an old derivative.
            const string original = "Assets/HotpotSort/Resources/Hotpot/TASK001/v7/r001/hero/share.png";
            const string optimized = "Assets/HotpotSort/Editor/WeChatBuild/ShareExport/share-theme.png";
            if (!File.Exists(original) || Hash(original) != "99dfcf7418715d68959de447496330a2b746e6c954f205206b9037c8ad2d323b")
                throw new BuildFailedException("Share artwork changed: regenerate the lossless export derivative with tools/optimize-share-png.cjs and update its verified hashes.");
            if (!File.Exists(optimized) || Hash(optimized) != "8440f7931ca7f41e459884ee171a5ea00e9935d478207a95d0618d1c1ce16908")
                throw new BuildFailedException("Missing or changed lossless share export derivative");
            return optimized;
        }
        private static void ValidateIdentity(string value)
        {
            if(string.IsNullOrWhiteSpace(value) || !Regex.IsMatch(value,"\\Awx[0-9a-fA-F]{16}\\z")) throw new BuildFailedException("WECHAT_APP_ID is required and must be a valid mini-game identity");
        }
        private static void CopyDirectory(string source,string destination)
        {
            Directory.CreateDirectory(destination);
            foreach(var file in Directory.GetFiles(source)) if(!file.EndsWith(".meta",StringComparison.OrdinalIgnoreCase)) File.Copy(file,Path.Combine(destination,Path.GetFileName(file)),false);
            foreach(var directory in Directory.GetDirectories(source)) CopyDirectory(directory,Path.Combine(destination,Path.GetFileName(directory)));
        }
        private static string Hash(string path)
        {
            using(var sha=SHA256.Create()) using(var stream=File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
        }
        [Serializable] private sealed class PackageEvidence
        {
            public int schemaVersion,taskVersion;
            public bool sdkCompletionObserved,runtimeConfigPresent,openDataEnabled,cloudConfigured,adConfigured;
            public string fontManifestSha256,shareImageSha256;
        }
    }
}
