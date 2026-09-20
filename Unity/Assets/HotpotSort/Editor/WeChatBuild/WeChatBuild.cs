using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using WeChatWASM;
namespace HotpotSort.Build
{
    public static class WeChatBuild
    {
        public static void CompileDiagnostic()
        {
            Debug.Log("HOTPOT_SCRIPT_COMPILE_OK Unity=" + Application.unityVersion);
        }
        public static void CompilePlayerDiagnostic()
        {
            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/wechat/player-script-diagnostic"));
            Directory.CreateDirectory(output);
            var settings = new UnityEditor.Build.Player.ScriptCompilationSettings
            {
                target = BuildTarget.WebGL,
                group = BuildTargetGroup.WebGL,
                options = UnityEditor.Build.Player.ScriptCompilationOptions.None
            };
            var result = UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(settings, output);
            if (result.assemblies == null || result.assemblies.Count == 0)
                throw new BuildFailedException("Player script compilation produced no assemblies");
            Debug.Log("HOTPOT_WEBGL_PLAYER_SCRIPTS_OK assemblies=" + result.assemblies.Count);
        }
        // Explicit invocation only; exporting does not upload, submit or activate a release.
        public static void Export()
        {
            var appId = Environment.GetEnvironmentVariable("WECHAT_APP_ID");
            if (string.IsNullOrWhiteSpace(appId)) throw new BuildFailedException("WECHAT_APP_ID is required");
            var defines = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.WebGL);
            foreach (var define in defines.Split(';'))
                if (define == "HOTPOT_DEVELOPMENT" || define.StartsWith("TUANJIE") || define == "UNITY_INSTANTGAME")
                    throw new BuildFailedException("Unsupported production scripting define");
            if (WXConvertCore.IsInstantGameAutoStreaming()) throw new BuildFailedException("AutoStreaming is not enabled for this route");
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/wechat"));
            var paths = new HotpotSort.Contracts.RuntimePaths(Path.Combine(root, "data"), Path.Combine(root, "cache"), root);
            var output = Path.Combine(paths.BuildRoot, "export-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmss") + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(output);
            var config = WXConvertCore.config;
            var oldApp = config.ProjectConf.Appid;
            var oldDestination = config.ProjectConf.DST;
            try
            {
                config.ProjectConf.Appid = appId;
                config.ProjectConf.DST = output;
                var result = WXConvertCore.DoExport();
                if (result != WXConvertCore.WXExportError.SUCCEED) throw new BuildFailedException("WX export failed: " + result);
            }
            finally
            {
                config.ProjectConf.Appid = oldApp;
                config.ProjectConf.DST = oldDestination;
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
