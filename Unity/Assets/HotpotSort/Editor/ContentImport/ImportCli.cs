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
            if(args.Length==6&&args[0]=="validate-layouts")return LayoutContentDiagnostic.Run(args[1],args[2],args[3],args[4],args[5]);
            if(args.Length==4&&args[0]=="layouts")return ImportLayouts(args[1],args[2],args[3]);
            if (args.Length != 3) { Console.Error.WriteLine("Usage: import <original skeleton.json> <original weights.json> <output directory> | layouts <recipe.json> <skeleton.json> <output directory> | validate-layouts <recipe> <skeleton> <catalog> <manifest> <production>"); return 2; }
            try
            {
                var content = DailyContentImporter.Import(File.ReadAllBytes(args[0]), File.ReadAllBytes(args[1]));
                Directory.CreateDirectory(args[2]);
                File.WriteAllText(Path.Combine(args[2], Core.DailyContent.RuntimeFileName), content.CanonicalJsonText, new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(args[2], "import-manifest-v5.json"), CanonicalJson.Write(CanonicalJson.Object(
                    "importerVersion", Core.DailyContent.ImporterVersion, "sourceProfile", "FixedCAcceptedDifficulty1", "weightScale", 100,
                    "skeletonSourceSha256", Core.DailyContent.SkeletonSourceHash, "weightsSourceSha256", Core.DailyContent.WeightsSourceHash,
                    "output", Core.DailyContent.RuntimeFileName, "outputSha256", content.Digest, "plateCount", 50, "itemCount", 183, "kindCount", 16, "weightRows", 20)), new UTF8Encoding(false));
                Console.WriteLine("IMPORTED " + content.Digest); return 0;
            }
            catch (Exception e) { Console.Error.WriteLine(e.Message); return 1; }
        }

        static int ImportLayouts(string recipePath,string skeletonPath,string outputDirectory)
        {
            try
            {
                var catalog=DailyContentImporter.ImportLayouts(File.ReadAllBytes(recipePath),File.ReadAllBytes(skeletonPath));
                Directory.CreateDirectory(outputDirectory);
                File.WriteAllText(Path.Combine(outputDirectory,Core.DailyLayoutCatalog.RuntimeFileName),catalog.CanonicalJsonText,new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(outputDirectory,Core.DailyLayoutCatalog.ManifestFileName),CanonicalJson.Write(CanonicalJson.Object(
                    "catalogVersion",Core.DailyLayoutCatalog.Version,"importerVersion","daily_layout_importer_v1","recipeSourceSha256",catalog.RecipeSourceHash,
                    "skeletonSourceSha256",catalog.SkeletonSourceHash,"output",Core.DailyLayoutCatalog.RuntimeFileName,"outputSha256",catalog.Digest,
                    "layoutCount",catalog.Layouts.Count,"formalPlateCount",50,"formalItemCount",183,"formalOrderCount",61,
                    "warmupItemCount",18,"warmupOrderCount",6)),new UTF8Encoding(false));
                Console.WriteLine("LAYOUTS_IMPORTED "+catalog.Digest);return 0;
            }
            catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}
        }
    }
}
#endif
