#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using HotpotSort.Core;
namespace HotpotSort.EditorTools
{
    [Serializable] public sealed class GoldenCase {public string name,level;public uint seed;public bool failOnFull;public Command[] commands;public GameSnapshot expected;}
    [Serializable] public sealed class GoldenSuite {public GoldenCase[] cases;}
    [Serializable] public sealed class ValidationResult {public bool passed;public int goldenCases;public string unityVersion,utc;}
    public static class ProjectTools
    {
        private const string Scene="Assets/HotpotSort/Scenes/Boot.unity";
        [MenuItem("Hotpot/Open Prototype Scene")]
        public static void OpenScene(){EditorSceneManager.OpenScene(Scene);}
        [MenuItem("Hotpot/Apply Portrait Settings")]
        public static void ApplySettings()
        {
            PlayerSettings.companyName="PrototypeLab";PlayerSettings.productName="Hotpot Sort Prototype";
            PlayerSettings.defaultScreenWidth=420;PlayerSettings.defaultScreenHeight=900;PlayerSettings.defaultIsNativeResolution=false;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;PlayerSettings.runInBackground=false;
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};AssetDatabase.SaveAssets();
        }
        [MenuItem("Hotpot/Run Core Validation")]
        public static void RunCoreValidation()
        {
            var source=Resources.Load<TextAsset>("Hotpot/game-data");var fixtures=Resources.Load<TextAsset>("Hotpot/golden-cases");
            if(source==null||fixtures==null)throw new InvalidOperationException("Missing data or golden fixtures");
            var data=JsonUtility.FromJson<GameData>(source.text);var suite=JsonUtility.FromJson<GoldenSuite>(fixtures.text);
            foreach(var test in suite.cases)
            {
                var game=new GameSession(data,test.level,test.seed,test.failOnFull);
                foreach(var cmd in test.commands){if(cmd.type=="spawn")game.Spawn();else if(cmd.type=="tap")game.Tap(cmd.id);else throw new InvalidOperationException("Bad fixture command");}
                game.Audit();string actual=JsonUtility.ToJson(game.Snapshot()),expected=JsonUtility.ToJson(test.expected);
                if(actual!=expected)throw new InvalidOperationException("Golden mismatch: "+test.name+"\nExpected: "+expected+"\nActual: "+actual);
            }
            foreach(string id in new[]{"A","B","C","P"})
            {
                var game=new GameSession(data,id,7);var world=new PlateWorld(game.Rules,7);for(int i=0;i<3600;i++)world.Step(game,1.0/120);world.Audit(game);game.Audit();
                if(game.Active.Count==0)throw new InvalidOperationException("No plates spawned");
            }
            var result=new ValidationResult{passed=true,goldenCases=suite.cases.Length,unityVersion=Application.unityVersion,utc=DateTime.UtcNow.ToString("o")};
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../qa/unity-validation.json"));Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(result,true));
            Debug.Log("Hotpot core validation passed. "+suite.cases.Length+" golden fixtures; 4 physics smoke checks. Report: "+output);
        }
        [MenuItem("Hotpot/Build Current Target")]
        public static void BuildCurrentTarget()
        {
            ApplySettings();RunCoreValidation();var target=EditorUserBuildSettings.activeBuildTarget;
            string name=target==BuildTarget.StandaloneWindows64||target==BuildTarget.StandaloneWindows?"HotpotSort.exe":target==BuildTarget.StandaloneOSX?"HotpotSort.app":target==BuildTarget.Android?"HotpotSort.apk":target==BuildTarget.WebGL?"WebGL":"HotpotSort";
            string path=Path.Combine("Builds",target.ToString(),name);Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName=path,target=target,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Build failed: "+report.summary.result);
            Debug.Log("Build completed: "+Path.GetFullPath(path));
        }
    }
    public sealed class PrototypeTextureImport : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if(!assetPath.Contains("HotpotSort/Resources/Hotpot/"))return;
            var importer=(TextureImporter)assetImporter;importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=256;
        }
    }
}
#endif
