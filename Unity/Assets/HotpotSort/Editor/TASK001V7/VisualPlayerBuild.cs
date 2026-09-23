#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace HotpotSort.Task001V7
{
    public static class VisualPlayerBuild
    {
        public static void Run()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-visualEvidence");
            string root=args[i+1];string playerRoot="D:/火锅消消/Builds/TASK001/v8/Final-r003";Directory.CreateDirectory(playerRoot);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/HotpotSort/Scenes/Boot.unity"},locationPathName=playerRoot+"/HotpotSortV8.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText(root+"/build-result.json","{\"result\":\""+report.summary.result+"\",\"errors\":"+report.summary.totalErrors+",\"bytes\":"+report.summary.totalSize+"}");
            Debug.Log("V7_VISUAL_PLAYER_BUILD "+report.summary.result);EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
        }
    }
}
#endif
