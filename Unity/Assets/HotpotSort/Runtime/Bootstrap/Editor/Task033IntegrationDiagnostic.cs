#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace HotpotSort.Bootstrap
{
    [InitializeOnLoad] public static class Task033IntegrationDiagnostic
    {
        const string Key="Task033.Integration";
        static Task033IntegrationDiagnostic(){EditorApplication.playModeStateChanged+=async s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);EditorApplication.Exit(await Task033IntegrationProbe.Run());}};}
        public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");UnityEngine.Object.FindFirstObjectByType<Bootstrap>().enabled=false;EditorApplication.EnterPlaymode();}
        public static void Build()
        {
            string root=Environment.GetEnvironmentVariable("HOTPOT_TASK033_INTEGRATION");Directory.CreateDirectory(Path.Combine(root,"player"));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/HotpotSort/Scenes/Boot.unity"},locationPathName=Path.Combine(root,"player/HotpotSort.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText(Path.Combine(root,"build-result.json"),"{\"result\":\""+report.summary.result+"\",\"errors\":"+report.summary.totalErrors+",\"bytes\":"+report.summary.totalSize+"}");
            EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
        }
    }
}
#endif
