#if !UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Determinism;

namespace HotpotSort.ContentImport
{
    internal static class LayoutContentDiagnostic
    {
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        internal static int Run(string recipePath,string skeletonPath,string catalogPath,string manifestPath,string productionPath)
        {
            try
            {
                byte[] recipe=File.ReadAllBytes(recipePath),skeleton=File.ReadAllBytes(skeletonPath);
                var imported=DailyContentImporter.ImportLayouts(recipe,skeleton);
                var manifest=CanonicalJson.Map(CanonicalJson.Parse(File.ReadAllText(manifestPath)));
                var loaded=DailyLayoutCatalog.Load(File.ReadAllText(catalogPath),(string)manifest["outputSha256"]);
                Check(imported.Digest==loaded.Digest&&imported.CanonicalJsonText==loaded.CanonicalJsonText,"generated catalog differs from deterministic import");
                Check((string)manifest["recipeSourceSha256"]==CanonicalJson.Hash(recipe)&&CanonicalJson.Int(manifest["layoutCount"])==7,"manifest identity/count");

                var production=DailyContent.LoadProduction(File.ReadAllText(productionPath));
                Check(loaded.Layouts[0].FormalPlates.Select(Plate).SequenceEqual(production.Plates.Select(Plate)),"layout zero is not the existing Skeleton C queue");
                var inventory=Inventory(production.Plates);var baseHistogram=Histogram(production.Plates);var summaries=new List<string>();
                foreach(var layout in loaded.Layouts)
                {
                    Check(layout.FormalPlates.Count==50&&layout.FormalPlates.Sum(p=>p.Kinds.Count)==183,"formal size "+layout.LayoutId);
                    Check(Inventory(layout.FormalPlates).SequenceEqual(inventory),"formal inventory "+layout.LayoutId);
                    var histogram=Histogram(layout.FormalPlates);
                    Check(histogram.All(x=>x>=1)&&histogram.Where((count,i)=>Math.Abs(count-baseHistogram[i])>3).Count()==0,"formal histogram "+layout.LayoutId);
                    Check(layout.WarmupPlates.Count>=4&&layout.WarmupPlates.Count<=8&&layout.WarmupPlates.Sum(p=>p.Kinds.Count)==18,"warmup size "+layout.LayoutId);
                    Check(layout.WarmupPlates.SelectMany(p=>p.Kinds).GroupBy(x=>x).OrderBy(x=>x.Key).Select(x=>x.Count()).SequenceEqual(new[]{6,6,6}),"warmup inventory "+layout.LayoutId);
                    Check(layout.WarmupOpeningKinds.Distinct().Count()==2&&layout.WarmupOpeningKinds.Contains(layout.TutorialKind)&&layout.WarmupPlates[0].Kinds.Contains(layout.TutorialKind),"warmup opening/tutorial "+layout.LayoutId);

                    var content=new DailyContent(production.ContentVersion,layout.FormalPlates,production.Rows);
                    var ingredients=Enumerable.Range(0,16).Select(id=>new IngredientEntry("food_"+id.ToString("00"),"food/food_"+id.ToString("00"),"food_"+id.ToString("00"),"alpha-hit-radius-16-board-units","standard",content.ContentVersion));
                    var factory=new DailySessionFactory(content,ingredients);
                    using(var session=factory.CreateStage(new ChallengeContext("20260928",content.ContentVersion,factory.ConfigurationDigest,"task040-"+layout.Index,0),ChallengeStage.Formal))
                    {
                        var state=CanonicalJson.Map(CanonicalJson.Parse(session.Snapshot.CanonicalStateJson));
                        var orders=CanonicalJson.Array(state["orders"]).Select(CanonicalJson.Map).Where(x=>(string)x["state"]=="Active").Take(2).Select(x=>(string)x["kind"]).ToArray();
                        Check(orders.Length==2&&orders.All(x=>x!=null)&&orders.Distinct().Count()==2,"formal opening orders "+layout.LayoutId);
                        Check(orders.All(kind=>layout.FormalPlates.Take(10).SelectMany(p=>p.Kinds).Contains(kind)),"formal opening lookahead "+layout.LayoutId);
                    }
                    summaries.Add(layout.LayoutId+": formal=50/183/61 sizes="+string.Join(",",histogram)+" warmup="+layout.WarmupPlates.Count+"/18/6 tutorial="+layout.TutorialKind);
                }

                string altered=System.Text.Encoding.UTF8.GetString(recipe).Replace("[2, 6, 14, 13, 15]","[5, 3, 14, 13, 15]");
                bool rejected=false;try{DailyContentImporter.ImportLayouts(System.Text.Encoding.UTF8.GetBytes(altered),skeleton);}catch(ArgumentException){rejected=true;}
                Check(rejected,"invalid non-bijective formal permutation was accepted");
                foreach(string summary in summaries)Console.WriteLine(summary);
                Console.WriteLine("TASK040_LAYOUT_QA_PASS digest="+loaded.Digest+" seven deterministic pairs, fixed-seed recomposition, exact inventory, bounded +/-3 plate histograms, legal formal openings, warmup bounds/orders/tutorial, manifest/reimport/tamper guard");
                return 0;
            }
            catch(Exception e){Console.Error.WriteLine("TASK040_LAYOUT_QA_FAIL "+e.Message);return 1;}
        }
        static string Plate(PlateDefinition plate)=>plate.PlateId+":"+string.Join("",plate.Kinds);
        static int[] Inventory(IReadOnlyList<PlateDefinition> plates)=>Enumerable.Range(0,16).Select(i=>plates.Sum(p=>p.Kinds.Count(k=>k==((char)('A'+i)).ToString()))).ToArray();
        static int[] Histogram(IReadOnlyList<PlateDefinition> plates)=>Enumerable.Range(1,5).Select(n=>plates.Count(p=>p.Kinds.Count==n)).ToArray();
    }
}
#endif
