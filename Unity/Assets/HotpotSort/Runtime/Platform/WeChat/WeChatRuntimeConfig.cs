using System;
using UnityEngine;
using HotpotSort.Contracts.RemoteAssets;

namespace HotpotSort.Platform
{
    [Serializable]
    public sealed class WeChatRuntimeConfig
    {
        public const string PackageFile = "hotpot-runtime-config.json";
        public const string FontResource = "Hotpot/TASK002/v9/r001/fonts/modern-sans";
        public int schemaVersion = 1;
        public int protocolVersion = 1;
        public string environment = "development";
        public string assetBaseUrl = "";
        public string assetSourceMode = "";
        public string assetCloudFileId = "";
        public string cloudEnvironmentId = "";
        public string cloudFunctionName = "hotpotProfileSync";
        public string rewardedAdUnitId = "";
        public string shareImagePath = "hotpot/share-theme.png";
        public string shareTitle = "来火锅消消，一起开锅！";
        public bool enableLogin = true;
        public bool enableCloud = true;
        public bool enableRewardedVideo = true;
        public bool enableShare = true;
        public bool enableFriendBoard = true;
        [NonSerialized] public bool packageConfigured = true;

        public RemoteAssetSource AssetSource => RemoteAssetSource.Select(assetSourceMode,assetBaseUrl,cloudEnvironmentId,assetCloudFileId);
        public WeChatCapabilityState AssetState {
            get { var source=AssetSource;return packageConfigured&&(source.Mode=="CloudFile"?RemoteAssetSource.IsCloudFile(source.CloudFileId,source.CloudEnvironment):source.Mode=="Https"&&IsAssetBaseUrl(source.BaseUrl))?WeChatCapabilityState.Ready:WeChatCapabilityState.NotConfigured; }
        }

        public WeChatCapabilityState LoginState => !packageConfigured ? WeChatCapabilityState.NotConfigured : enableLogin ? WeChatCapabilityState.Ready : WeChatCapabilityState.Disabled;
        public WeChatCapabilityState FriendBoardState => !packageConfigured ? WeChatCapabilityState.NotConfigured : enableFriendBoard ? WeChatCapabilityState.Ready : WeChatCapabilityState.Disabled;
        public WeChatCapabilityState CloudState => !packageConfigured ? WeChatCapabilityState.NotConfigured : !enableCloud ? WeChatCapabilityState.Disabled :
            string.IsNullOrWhiteSpace(cloudEnvironmentId) || string.IsNullOrWhiteSpace(cloudFunctionName) ? WeChatCapabilityState.NotConfigured : WeChatCapabilityState.Ready;
        public WeChatCapabilityState RewardedVideoState => !packageConfigured ? WeChatCapabilityState.NotConfigured : !enableRewardedVideo ? WeChatCapabilityState.Disabled :
            string.IsNullOrWhiteSpace(rewardedAdUnitId) ? WeChatCapabilityState.NotConfigured : WeChatCapabilityState.Ready;
        public WeChatCapabilityState ShareState => !packageConfigured ? WeChatCapabilityState.NotConfigured : !enableShare ? WeChatCapabilityState.Disabled :
            IsPackagePath(shareImagePath) && !string.IsNullOrWhiteSpace(shareTitle) ? WeChatCapabilityState.Ready : WeChatCapabilityState.NotConfigured;

        // Only the completed export contains environment identifiers. Never persist this object to source assets.
        public static WeChatRuntimeConfig FromEnvironment(Func<string, string> read)
        {
            if (read == null) throw new ArgumentNullException(nameof(read));
            var config = new WeChatRuntimeConfig {
                environment = Value(read, "WECHAT_ENVIRONMENT", "development"),
                assetBaseUrl = Value(read, "WECHAT_ASSET_BASE_URL", ""),
                assetSourceMode = Value(read, "HOTPOT_REMOTE_SOURCE", ""),
                assetCloudFileId = Value(read, "HOTPOT_REMOTE_CLOUD_FILE_ID", ""),
                cloudEnvironmentId = Value(read, "WECHAT_CLOUD_ENV_ID", ""),
                cloudFunctionName = Value(read, "WECHAT_CLOUD_FUNCTION", "hotpotProfileSync"),
                rewardedAdUnitId = Value(read, "WECHAT_REWARDED_AD_UNIT_ID", ""),
                enableLogin = Flag(read, "WECHAT_ENABLE_LOGIN"),
                enableCloud = Flag(read, "WECHAT_ENABLE_CLOUD"),
                enableRewardedVideo = Flag(read, "WECHAT_ENABLE_REWARDED_VIDEO"),
                enableShare = Flag(read, "WECHAT_ENABLE_SHARE"),
                enableFriendBoard = Flag(read, "WECHAT_ENABLE_FRIEND_BOARD")
            };
            config.Validate();
            return config;
        }

        public static WeChatRuntimeConfig FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new FormatException("Missing WeChat runtime configuration");
            var config = new WeChatRuntimeConfig { schemaVersion = 0, protocolVersion = 0 };
            JsonUtility.FromJsonOverwrite(json,config);
            config.Validate();
            return config;
        }

        // Call after SDK initialization. Missing/bad package configuration fails closed.
        public static WeChatRuntimeConfig LoadPackaged()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { return FromJson(WeChatWASM.WX.GetFileSystemManager().ReadFileSync(PackageFile, "utf8")); }
            catch { return Unavailable(); }
#else
            return Unavailable();
#endif
        }

        public static WeChatRuntimeConfig Unavailable() => new WeChatRuntimeConfig {
            packageConfigured = false,
            enableLogin = false, enableCloud = false, enableRewardedVideo = false,
            enableShare = false, enableFriendBoard = false
        };

        public void Validate()
        {
            if (schemaVersion != 1 || protocolVersion != 1) throw new FormatException("Unsupported WeChat configuration version");
            if (environment != "development" && environment != "staging" && environment != "production") throw new FormatException("Invalid WeChat environment");
            if (!Identifier(cloudEnvironmentId) || !Identifier(cloudFunctionName) || !Identifier(rewardedAdUnitId)) throw new FormatException("Invalid WeChat configuration identifier");
            if (!IsPackagePath(shareImagePath)) throw new FormatException("Invalid packaged share image path");
            if (!string.IsNullOrEmpty(assetBaseUrl) && !IsAssetBaseUrl(assetBaseUrl)) throw new FormatException("Invalid asset base configuration");
            if(assetSourceMode!=""&&assetSourceMode!="CloudFile"&&assetSourceMode!="Https")throw new FormatException("Invalid asset source mode");
            if(!string.IsNullOrEmpty(assetCloudFileId)&&!RemoteAssetSource.IsCloudFile(assetCloudFileId,cloudEnvironmentId))throw new FormatException("Invalid cloud asset binding");
        }
        public static bool IsAssetBaseUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Contains("\\") || value.Contains("%") || value.Contains("?") || value.Contains("#") || value.Contains("@")) return false;
            foreach(char ch in value) if(char.IsControl(ch)||char.IsWhiteSpace(ch)) return false;
            if (!Uri.TryCreate(value,UriKind.Absolute,out var uri) || uri.Scheme != "https" || string.IsNullOrEmpty(uri.Host) || uri.UserInfo.Length != 0) return false;
            int start=value.IndexOf("://",StringComparison.Ordinal)+3, slash=value.IndexOf('/',start);
            if(slash<0)return true;
            string path=value.Substring(slash+1).TrimEnd('/');
            return path.Length==0 || IsPackagePath(path);
        }
        public static bool IsPackagePath(string value)
        {
            if (string.IsNullOrEmpty(value) || value.StartsWith("/") || value.Contains("\\") || value.Contains(":")) return false;
            foreach (var segment in value.Split('/')) if (segment == ".." || segment == "." || segment.Length == 0) return false;
            return true;
        }
        private static bool Identifier(string value)
        {
            if (value == null || value.Length > 128) return false;
            foreach (char ch in value) if (!(ch >= 'a' && ch <= 'z') && !(ch >= 'A' && ch <= 'Z') && !(ch >= '0' && ch <= '9') && ch != '-' && ch != '_') return false;
            return true;
        }
        private static string Value(Func<string,string> read, string key, string fallback) => (read(key) ?? fallback).Trim();
        private static bool Flag(Func<string,string> read, string key)
        {
            var value = read(key);
            if (string.IsNullOrWhiteSpace(value)) return true;
            if (value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (value == "0" || string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)) return false;
            throw new FormatException("Invalid WeChat feature flag");
        }
    }
}
