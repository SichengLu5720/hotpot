using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using HotpotSort.Core;
using HotpotSort.Determinism;

namespace HotpotSort.ContentImport
{
    public static class DailyContentImporter
    {
        public static DailyContent Import(byte[] skeletonSource, byte[] weightsSource, string contentVersion = DailyContent.CurrentVersion)
        {
            if (CanonicalJson.Hash(skeletonSource) != DailyContent.SkeletonSourceHash || CanonicalJson.Hash(weightsSource) != DailyContent.WeightsSourceHash)
                throw new ArgumentException("Confirmed source SHA256 mismatch");
            var skeleton = CanonicalJson.Map(CanonicalJson.Parse(new UTF8Encoding(false, true).GetString(skeletonSource)));
            var plates = new List<PlateDefinition>();
            foreach (var entry in CanonicalJson.Array(skeleton["bubbles"]))
            {
                var p = CanonicalJson.Map(entry); int id = CanonicalJson.Int(p["config_index"]);
                if (id != plates.Count + 1) throw new ArgumentException("Non-contiguous source plate index");
                plates.Add(new PlateDefinition(id, CanonicalJson.Array(p["fish_symbols"]).Cast<string>()));
            }
            var rows = new List<WeightRow>(); var thresholds = new[] { 0.40m, 0.65m, 0.85m, 1.00m };
            foreach (var entry in CanonicalJson.Array(CanonicalJson.Parse(new UTF8Encoding(false, true).GetString(weightsSource))))
            {
                var row = CanonicalJson.Map(entry); if (CanonicalJson.Int(row["Difficulty"]) != (contentVersion==DailyContent.CurrentVersion?1:3)) continue;
                int band = System.Array.IndexOf(thresholds, Convert.ToDecimal(row["LevelProgress"], CultureInfo.InvariantCulture));
                if (band < 0) throw new ArgumentException("Unknown progress threshold");
                var weights = new int[5];
                for (int i = 0; i < 5; i++)
                {
                    decimal scaled = Convert.ToDecimal(row["Order" + (i + 1)], CultureInfo.InvariantCulture) * 100m;
                    if (scaled != decimal.Truncate(scaled) || scaled < 0 || scaled > int.MaxValue) throw new ArgumentException("Non-integral/negative/oversized scaled weight");
                    weights[i] = (int)scaled;
                }
                rows.Add(new WeightRow(band, CanonicalJson.Int(row["TempCount"]), weights));
            }
            return new DailyContent(contentVersion, plates, rows);
        }

        public static DailyLayoutCatalog ImportLayouts(byte[] recipeSource, byte[] skeletonSource)
        {
            if(CanonicalJson.Hash(skeletonSource)!=DailyContent.SkeletonSourceHash)throw new ArgumentException("Confirmed Skeleton C SHA256 mismatch");
            string recipeHash=CanonicalJson.Hash(recipeSource);
            var recipe=CanonicalJson.Map(CanonicalJson.Parse(new UTF8Encoding(false,true).GetString(recipeSource)));
            if((string)recipe["schemaVersion"]!="daily_layout_recipe_v1"||(string)recipe["catalogVersion"]!=DailyLayoutCatalog.Version
                ||(string)recipe["baseSkeletonSha256"]!=DailyContent.SkeletonSourceHash)throw new ArgumentException("Unknown layout recipe/version/source");
            var basePlates=ReadSkeleton(skeletonSource);
            var entries=CanonicalJson.Array(recipe["layouts"]);if(entries.Count!=7)throw new ArgumentException("Layout recipe must contain seven entries");
            var layouts=new List<DailyLayoutPair>();
            foreach(var entry in entries)
            {
                var m=CanonicalJson.Map(entry);int index=CanonicalJson.Int(m["index"]);string mode=(string)m["formalMode"];
                if(index!=layouts.Count)throw new ArgumentException("Invalid layout index");
                List<PlateDefinition> formal;
                if(mode=="preserve")
                {
                    if(index!=0)throw new ArgumentException("Only layout zero may preserve Skeleton C");
                    formal=basePlates.Select(p=>new PlateDefinition(p.PlateId,p.Kinds)).ToList();
                }
                else if(mode=="recompose")formal=Recompose(basePlates,CanonicalJson.Array(m["formalPlateHistogram"]).Select(CanonicalJson.Int).ToArray(),(string)m["formalSeed"]);
                else throw new ArgumentException("Unknown formal layout mode");
                var warm=CanonicalJson.Map(m["warmup"]);
                var warmPlates=CanonicalJson.Array(warm["plates"]).Select((value,i)=>new PlateDefinition(i+1,CanonicalJson.Array(value).Cast<string>()));
                layouts.Add(new DailyLayoutPair(index,(string)m["layoutId"],formal,warmPlates,
                    CanonicalJson.Array(warm["openingKinds"]).Cast<string>(),(string)warm["tutorialKind"]));
            }
            var result=new DailyLayoutCatalog(recipeHash,DailyContent.SkeletonSourceHash,layouts);
            if(!result.Layouts[0].FormalPlates.Select(p=>string.Join("",p.Kinds)).SequenceEqual(basePlates.Select(p=>string.Join("",p.Kinds))))
                throw new ArgumentException("Layout zero must preserve Skeleton C queue exactly");
            return result;
        }

        static List<PlateDefinition> Recompose(IReadOnlyList<PlateDefinition> basePlates,int[] histogram,string seedText)
        {
            if(histogram.Length!=5||histogram.Any(x=>x<1)||histogram.Sum()!=50||histogram.Select((count,i)=>count*(i+1)).Sum()!=183||string.IsNullOrWhiteSpace(seedText))
                throw new ArgumentException("Invalid formal plate histogram/seed");
            var rng=new Pcg32(Pcg32.DailySeed(seedText,DailyLayoutCatalog.Version+"|recompose_v1"));
            var sizes=histogram.SelectMany((count,i)=>Enumerable.Repeat(i+1,count)).ToArray();Shuffle(sizes,rng);
            var kinds=basePlates.SelectMany(p=>p.Kinds).ToArray();Shuffle(kinds,rng);
            var result=new List<PlateDefinition>();int offset=0;
            for(int i=0;i<sizes.Length;i++){result.Add(new PlateDefinition(i+1,kinds.Skip(offset).Take(sizes[i])));offset+=sizes[i];}
            return result;
        }

        static void Shuffle<T>(T[] values,Pcg32 rng)
        {for(int i=values.Length-1;i>0;i--){int j=(int)rng.NextBounded((uint)(i+1));T value=values[i];values[i]=values[j];values[j]=value;}}

        static List<PlateDefinition> ReadSkeleton(byte[] skeletonSource)
        {
            var skeleton=CanonicalJson.Map(CanonicalJson.Parse(new UTF8Encoding(false,true).GetString(skeletonSource)));
            var plates=new List<PlateDefinition>();
            foreach(var entry in CanonicalJson.Array(skeleton["bubbles"]))
            {
                var p=CanonicalJson.Map(entry);int id=CanonicalJson.Int(p["config_index"]);
                if(id!=plates.Count+1)throw new ArgumentException("Non-contiguous source plate index");
                plates.Add(new PlateDefinition(id,CanonicalJson.Array(p["fish_symbols"]).Cast<string>()));
            }
            return plates;
        }

    }
}
