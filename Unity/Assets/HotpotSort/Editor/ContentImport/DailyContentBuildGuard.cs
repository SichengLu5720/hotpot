#if UNITY_EDITOR
using System;
using System.IO;
using HotpotSort.Core;
using HotpotSort.Determinism;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HotpotSort.ContentImport
{
    public sealed class DailyContentBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) => Validate();
        [MenuItem("Hotpot Sort/Validate Daily Content")]
        public static void Validate()
        {
            try
            {
                string root = Path.Combine(Application.dataPath, "HotpotSort/Content/Daily");
                var imported=DailyContentImporter.Import(File.ReadAllBytes(Path.Combine(root,DailyContent.SkeletonSourceFileName)),
                    File.ReadAllBytes(Path.Combine(root,"LevelDifficultyConfig.source.json")));
                var runtime=DailyContent.LoadProduction(File.ReadAllText(Path.Combine(root,DailyContent.RuntimeFileName)));
                if(runtime.Digest!=imported.Digest)throw new ArgumentException("Runtime content does not match confirmed C import");
                var manifest=CanonicalJson.Map(CanonicalJson.Parse(File.ReadAllText(Path.Combine(root,"import-manifest-v5.json"))));
                if((string)manifest["outputSha256"]!=runtime.Digest || (string)manifest["skeletonSourceSha256"]!=DailyContent.SkeletonSourceHash
                    || (string)manifest["output"]!=DailyContent.RuntimeFileName)throw new ArgumentException("Fixed C import manifest mismatch");
                var importedLayouts=DailyContentImporter.ImportLayouts(File.ReadAllBytes(Path.Combine(root,DailyLayoutCatalog.RecipeFileName)),
                    File.ReadAllBytes(Path.Combine(root,DailyContent.SkeletonSourceFileName)));
                var layoutManifest=CanonicalJson.Map(CanonicalJson.Parse(File.ReadAllText(Path.Combine(root,DailyLayoutCatalog.ManifestFileName))));
                var layoutCatalog=DailyLayoutCatalog.Load(File.ReadAllText(Path.Combine(root,DailyLayoutCatalog.RuntimeFileName)),(string)layoutManifest["outputSha256"]);
                if(layoutCatalog.Digest!=importedLayouts.Digest||(string)layoutManifest["recipeSourceSha256"]!=importedLayouts.RecipeSourceHash
                    ||(string)layoutManifest["skeletonSourceSha256"]!=DailyContent.SkeletonSourceHash||(string)layoutManifest["output"]!=DailyLayoutCatalog.RuntimeFileName
                    ||CanonicalJson.Int(layoutManifest["layoutCount"])!=7)throw new ArgumentException("Seven-day layout import manifest mismatch");
                Debug.Log("TASK040_CONTENT_VALID: "+runtime.ContentVersion+" "+layoutCatalog.Digest+" seven paired layouts");
            }
            catch (Exception e) { throw new BuildFailedException("Daily content validation failed: " + e.Message); }
        }
    }
}
#endif
