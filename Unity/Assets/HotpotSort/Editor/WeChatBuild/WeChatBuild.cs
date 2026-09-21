using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using WeChatWASM;
namespace HotpotSort.Build
{
    public static class WeChatBuild
    {
        private static WXEditorScriptObject panelConfig;
        private static WXEditorScriptObject exportConfig;
        private static FieldInfo configCache;
        private static string panelPath;
        private static byte[] panelBytes;
        private static string exportPath;
        private static bool allDone;
        private static bool exportError;
        private static bool exportReturned;
        private static double exportStarted;
        public static void CompileDiagnostic()
        {
            Debug.Log("HOTPOT_SCRIPT_COMPILE_OK Unity=" + Application.unityVersion);
        }
        public static void CompilePlayerDiagnostic()
        {
            Debug.Log("HOTPOT_WEBGL_COMPILE_TARGET " + EditorUserBuildSettings.activeBuildTarget);
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                throw new BuildFailedException("Launch Unity with -buildTarget WebGL");
            var output = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../build/wechat/player-script-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmss") + "-" + Guid.NewGuid().ToString("N")));
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
            // Keep the identity out of Unity and SDK logs. The upload runner applies it
            // only to the completed generated project after package validation.
            ExportPackage();
        }

        public static void ExportPackage()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                throw new BuildFailedException("Launch Unity with -buildTarget WebGL");
            if (Environment.GetCommandLineArgs().Any(a => a == "-quit"))
                throw new BuildFailedException("Do not use -quit: wait for SDK completion");
            var defines = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.WebGL);
            foreach (var define in defines.Split(';'))
                if (define == "HOTPOT_DEVELOPMENT" || define.StartsWith("TUANJIE") || define == "UNITY_INSTANTGAME")
                    throw new BuildFailedException("Unsupported production scripting define");
            if (WXConvertCore.IsInstantGameAutoStreaming()) throw new BuildFailedException("AutoStreaming is not enabled for this route");
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/wechat"));
            exportPath = Path.Combine(root, "export-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmss") + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(exportPath);
            panelConfig = WXConvertCore.config;
            panelPath = AssetDatabase.GetAssetPath(panelConfig);
            panelBytes = File.ReadAllBytes(panelPath);
            // SDK PreInit saves its config. Use its existing cache with an unsaved clone,
            // so neither temporary paths nor SDK-derived defaults can alter panel values.
            configCache = typeof(UnityUtil).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Single(f => f.FieldType == typeof(WXEditorScriptObject) && ReferenceEquals(f.GetValue(null), panelConfig));
            exportConfig = UnityEngine.Object.Instantiate(panelConfig);
            exportConfig.hideFlags = HideFlags.HideAndDontSave;
            exportConfig.ProjectConf.Appid = string.Empty;
            exportConfig.ProjectConf.DST = exportPath;
            exportConfig.ProjectConf.relativeDST = exportPath;
            // TASK-002 v2: one-off compressed data subpackage; the panel is unchanged.
            exportConfig.ProjectConf.assetLoadType = 1;
            exportConfig.ProjectConf.compressDataPackage = true;
            var nodePath = Environment.GetEnvironmentVariable("WECHAT_NODE_PATH");
            if (!string.IsNullOrWhiteSpace(nodePath)) exportConfig.CompileOptions.CustomNodePath = nodePath;
            configCache.SetValue(null, exportConfig);
            allDone = exportError = exportReturned = false;
            exportStarted = EditorApplication.timeSinceStartup;
            Application.logMessageReceived += ObserveExport;
            EditorApplication.update += AwaitExport;
            try
            {
                Debug.Log("HOTPOT_WECHAT_EXPORT_STARTED " + exportPath);
                var result = WXConvertCore.DoExport();
                exportReturned = true;
                if (result != WXConvertCore.WXExportError.SUCCEED) throw new BuildFailedException("WX export failed: " + result);
            }
            catch (Exception exception)
            {
                Debug.LogError("HOTPOT_WECHAT_EXPORT_FAILED " + exception.GetType().Name);
                FinishExport(false);
            }
        }

        private static void ObserveExport(string message, string stack, LogType type)
        {
            if (message == "[Converter] All done!") allDone = true;
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) exportError = true;
        }

        private static void AwaitExport()
        {
            if (!exportReturned) return;
            if (exportError || EditorApplication.timeSinceStartup - exportStarted > 3600)
            {
                Debug.LogError("HOTPOT_WECHAT_EXPORT_FAILED SDK error or completion timeout");
                FinishExport(false);
                return;
            }
            if (!allDone) return;
            try
            {
                // This update occurs after finishExport logs All done and emits exportDone.
                var package = Path.Combine(exportPath, "minigame");
                var required = new[] { "project.config.json", "game.json", "game.js", WXConvertCore.frameworkFilename };
                var valid = required.All(name => File.Exists(Path.Combine(package, name)));
                if (valid)
                {
                    var project = JsonUtility.FromJson<ProjectIdentity>(File.ReadAllText(Path.Combine(package, "project.config.json")));
                    var game = File.ReadAllText(Path.Combine(package, "game.js"));
                    valid = project != null && project.compileType == "game" && string.IsNullOrEmpty(project.appid)
                        && game.Contains("loadDataPackageFromSubpackage: true") && game.Contains("compressDataPackage: true")
                        && File.Exists(Path.Combine(package, "data-package", WXConvertCore.dataMd5 + ".webgl.data.unityweb.bin.br"))
                        && File.Exists(Path.Combine(package, "wasmcode", WXConvertCore.codeMd5 + ".webgl.wasm.code.unityweb.wasm.br"));
                }
                Debug.Log(valid ? "HOTPOT_WECHAT_SDK_COMPLETE " + package : "HOTPOT_WECHAT_PACKAGE_INVALID");
                FinishExport(valid);
            }
            catch (Exception exception)
            {
                Debug.LogError("HOTPOT_WECHAT_PACKAGE_CHECK_FAILED " + exception.GetType().Name);
                FinishExport(false);
            }
        }

        private static void FinishExport(bool success)
        {
            Application.logMessageReceived -= ObserveExport;
            EditorApplication.update -= AwaitExport;
            if (configCache != null) configCache.SetValue(null, panelConfig);
            if (exportConfig != null) UnityEngine.Object.DestroyImmediate(exportConfig);
            var unchanged = panelBytes != null && File.ReadAllBytes(panelPath).SequenceEqual(panelBytes);
            if (!unchanged) Debug.LogError("HOTPOT_WECHAT_PANEL_CHANGED");
            else Debug.Log("HOTPOT_WECHAT_PANEL_UNCHANGED");
            if (Application.isBatchMode) EditorApplication.Exit(success && unchanged ? 0 : 1);
        }

        [Serializable]
        private sealed class ProjectIdentity
        {
            public string compileType;
            public string appid;
        }
    }
}
