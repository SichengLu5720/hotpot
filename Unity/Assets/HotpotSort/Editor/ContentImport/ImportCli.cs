#if !UNITY_5_3_OR_NEWER
using System;
using System.IO;
using System.Text;
using HotpotSort.Determinism;

namespace HotpotSort.ContentImport
{
    internal static class ImportCli
    {
        static int Main(string[] args)
        {
            if (args.Length != 3) { Console.Error.WriteLine("Usage: import <original skeleton.json> <original weights.json> <output directory>"); return 2; }
            try
            {
                var content = DailyContentImporter.Import(File.ReadAllBytes(args[0]), File.ReadAllBytes(args[1]));
                Directory.CreateDirectory(args[2]);
                File.WriteAllText(Path.Combine(args[2], Core.DailyContent.RuntimeFileName), content.CanonicalJsonText, new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(args[2], "import-manifest-v5.json"), CanonicalJson.Write(CanonicalJson.Object(
                    "importerVersion", Core.DailyContent.ImporterVersion, "sourceProfile", "FixedCAcceptedDifficulty3", "weightScale", 100,
                    "skeletonSourceSha256", Core.DailyContent.SkeletonSourceHash, "weightsSourceSha256", Core.DailyContent.WeightsSourceHash,
                    "output", Core.DailyContent.RuntimeFileName, "outputSha256", content.Digest, "plateCount", 50, "itemCount", 183, "kindCount", 16, "weightRows", 20)), new UTF8Encoding(false));
                Console.WriteLine("IMPORTED " + content.Digest); return 0;
            }
            catch (Exception e) { Console.Error.WriteLine(e.Message); return 1; }
        }
    }
}
#endif
