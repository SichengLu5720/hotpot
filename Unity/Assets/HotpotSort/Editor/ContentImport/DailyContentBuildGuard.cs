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
                var expected = DailyContentImporter.Import(File.ReadAllBytes(Path.Combine(root, "skeleton_C.source.json")), File.ReadAllBytes(Path.Combine(root, "LevelDifficultyConfig.source.json")));
                var manifest = CanonicalJson.Map(CanonicalJson.Parse(File.ReadAllText(Path.Combine(root, "import-manifest.json"))));
                string digest = (string)manifest["outputSha256"];
                var loaded = DailyContent.Load(File.ReadAllText(Path.Combine(root, "daily_core_1.0.0.json")), digest);
                if (loaded.Digest != expected.Digest || (string)manifest["importerVersion"] != DailyContent.ImporterVersion ||
                    (string)manifest["skeletonSourceSha256"] != DailyContent.SkeletonSourceHash || (string)manifest["weightsSourceSha256"] != DailyContent.WeightsSourceHash)
                    throw new ArgumentException("Daily content does not match the frozen source import");
            }
            catch (Exception e) { throw new BuildFailedException("Daily content validation failed: " + e.Message); }
        }
    }
}
#endif
