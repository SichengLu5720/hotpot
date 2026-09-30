using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HotpotSort.Determinism;

namespace HotpotSort.Core
{
    public sealed class DailyLayoutPair
    {
        public int Index { get; }
        public string LayoutId { get; }
        public IReadOnlyList<PlateDefinition> FormalPlates { get; }
        public IReadOnlyList<PlateDefinition> WarmupPlates { get; }
        public IReadOnlyList<string> WarmupOpeningKinds { get; }
        public string TutorialKind { get; }
        public string FormalDigest { get; }
        public string WarmupDigest { get; }

        public DailyLayoutPair(int index, string layoutId, IEnumerable<PlateDefinition> formalPlates,
            IEnumerable<PlateDefinition> warmupPlates, IEnumerable<string> warmupOpeningKinds, string tutorialKind)
        {
            Index=index;LayoutId=layoutId;
            FormalPlates=new ReadOnlyCollection<PlateDefinition>(new List<PlateDefinition>(formalPlates));
            WarmupPlates=new ReadOnlyCollection<PlateDefinition>(new List<PlateDefinition>(warmupPlates));
            WarmupOpeningKinds=new ReadOnlyCollection<string>(new List<string>(warmupOpeningKinds));
            TutorialKind=tutorialKind;
            FormalDigest=CanonicalJson.Hash(CanonicalJson.Write(FormalPlates.Select(PlateJson).ToArray()));
            WarmupDigest=CanonicalJson.Hash(CanonicalJson.Write(WarmupJson()));
        }

        internal object Json()=>CanonicalJson.Object("index",Index,"layoutId",LayoutId,
            "formalPlates",FormalPlates.Select(PlateJson).ToArray(),"warmup",WarmupJson(),
            "summary",CanonicalJson.Object("formalDigest",FormalDigest,"warmupDigest",WarmupDigest,
                "formalPlateSizes",Histogram(FormalPlates),"formalMixedPlates",FormalPlates.Count(p=>p.Kinds.Distinct(StringComparer.Ordinal).Count()>1),
                "warmupPlateSizes",Histogram(WarmupPlates)));
        object WarmupJson()=>CanonicalJson.Object("openingKinds",WarmupOpeningKinds,"tutorialKind",TutorialKind,
            "plates",WarmupPlates.Select(PlateJson).ToArray());
        internal static object PlateJson(PlateDefinition p)=>CanonicalJson.Object("plateId",p.PlateId,"kinds",p.Kinds);
        static object Histogram(IReadOnlyList<PlateDefinition> plates)=>Enumerable.Range(1,5).Select(n=>plates.Count(p=>p.Kinds.Count==n)).ToArray();
    }

    // Versioned data contract only. Runtime challenge-day selection is intentionally owned by integration.
    public sealed class DailyLayoutCatalog
    {
        public const string Schema="daily_layout_catalog_v1";
        public const string Version="seven_day_layouts_v1";
        public const string RuntimeFileName="seven-day-layouts.v1.json";
        public const string ManifestFileName="seven-day-layouts.v1.manifest.json";
        public const string RecipeFileName="seven-day-layouts.v1.source.json";
        public string RecipeSourceHash { get; }
        public string SkeletonSourceHash { get; }
        public IReadOnlyList<DailyLayoutPair> Layouts { get; }
        public string CanonicalJsonText { get; }
        public string Digest { get; }

        public DailyLayoutCatalog(string recipeSourceHash, string skeletonSourceHash, IEnumerable<DailyLayoutPair> layouts)
        {
            if(string.IsNullOrWhiteSpace(recipeSourceHash)||skeletonSourceHash!=DailyContent.SkeletonSourceHash)
                throw new ArgumentException("Layout source identity mismatch");
            RecipeSourceHash=recipeSourceHash;SkeletonSourceHash=skeletonSourceHash;
            Layouts=new ReadOnlyCollection<DailyLayoutPair>(new List<DailyLayoutPair>(layouts));
            Validate();CanonicalJsonText=CanonicalJson.Write(Json());Digest=CanonicalJson.Hash(CanonicalJsonText);
        }

        void Validate()
        {
            if(Layouts.Count!=7||Layouts.Select(x=>x.LayoutId).Distinct(StringComparer.Ordinal).Count()!=7)
                throw new ArgumentException("Expected seven distinct layout pairs");
            for(int i=0;i<Layouts.Count;i++)
            {
                var layout=Layouts[i];
                if(layout.Index!=i||string.IsNullOrWhiteSpace(layout.LayoutId))throw new ArgumentException("Invalid layout index/id");
                ValidateFormal(layout.FormalPlates);
                ValidateWarmup(layout);
            }
            if(Layouts.Select(x=>x.FormalDigest).Distinct(StringComparer.Ordinal).Count()!=7)
                throw new ArgumentException("Formal layout queues must be distinct");
            if(Layouts.Select(x=>x.WarmupDigest).Distinct(StringComparer.Ordinal).Count()!=7)
                throw new ArgumentException("Warmup layouts must be distinct");
            var baseHistogram=HistogramValues(Layouts[0].FormalPlates);
            if(!baseHistogram.SequenceEqual(new[]{1,7,14,14,14}))throw new ArgumentException("Layout zero must retain the Skeleton C plate histogram");
            if(Layouts.Skip(1).Any(x=>HistogramValues(x.FormalPlates).Where((count,i)=>count<1||Math.Abs(count-baseHistogram[i])>3).Any()))
                throw new ArgumentException("Formal plate histogram exceeds the confirmed +/-3 range");
            var inventory=Inventory(Layouts[0].FormalPlates);
            if(Layouts.Skip(1).Any(x=>!Inventory(x.FormalPlates).SequenceEqual(inventory)))
                throw new ArgumentException("Formal symbol inventory differs");
        }

        static void ValidateFormal(IReadOnlyList<PlateDefinition> plates)
        {
            if(plates.Count!=50)throw new ArgumentException("Formal layout must contain 50 plates");
            int total=0;var counts=new int[16];
            for(int i=0;i<plates.Count;i++)
            {
                var p=plates[i];if(p.PlateId!=i+1||p.Kinds.Count<1||p.Kinds.Count>5)throw new ArgumentException("Invalid formal plate");
                foreach(var kind in p.Kinds){if(kind==null||kind.Length!=1||kind[0]<'A'||kind[0]>'P')throw new ArgumentException("Invalid formal kind");counts[kind[0]-'A']++;total++;}
            }
            if(total!=183||counts.Any(x=>x==0||x%3!=0))throw new ArgumentException("Invalid formal 183-item/61-order inventory");
            var opening=plates.Take(10).SelectMany(p=>p.Kinds).GroupBy(x=>x).Count(g=>g.Count()>=3);
            if(opening<2)throw new ArgumentException("Formal opening cannot provide two distinct orders");
        }

        static void ValidateWarmup(DailyLayoutPair layout)
        {
            var plates=layout.WarmupPlates;
            if(plates.Count<4||plates.Count>8||plates.SelectMany(p=>p.Kinds).Count()!=18)
                throw new ArgumentException("Warmup must contain 18 items across 4-8 plates");
            for(int i=0;i<plates.Count;i++)if(plates[i].PlateId!=i+1||plates[i].Kinds.Count<1||plates[i].Kinds.Count>5)
                throw new ArgumentException("Invalid warmup plate");
            var groups=plates.SelectMany(p=>p.Kinds).GroupBy(x=>x).OrderBy(x=>x.Key,StringComparer.Ordinal).ToArray();
            if(groups.Length!=3||!groups.Select(x=>x.Key).SequenceEqual(new[]{"A","B","C"})||groups.Any(x=>x.Count()!=6))
                throw new ArgumentException("Warmup inventory must be A/B/C x6");
            if(layout.WarmupOpeningKinds.Count!=2||layout.WarmupOpeningKinds.Distinct(StringComparer.Ordinal).Count()!=2
                ||layout.WarmupOpeningKinds.Any(x=>!groups.Any(g=>g.Key==x)))throw new ArgumentException("Warmup opening orders must be distinct known kinds");
            if(!layout.WarmupOpeningKinds.Contains(layout.TutorialKind)||!plates[0].Kinds.Contains(layout.TutorialKind))
                throw new ArgumentException("Warmup tutorial target is not clickable from the first supplied plate");
        }

        static int[] Inventory(IReadOnlyList<PlateDefinition> plates)=>Enumerable.Range(0,16)
            .Select(i=>plates.Sum(p=>p.Kinds.Count(k=>k==((char)('A'+i)).ToString()))).ToArray();
        static int[] HistogramValues(IReadOnlyList<PlateDefinition> plates)=>Enumerable.Range(1,5).Select(n=>plates.Count(p=>p.Kinds.Count==n)).ToArray();

        object Json()=>CanonicalJson.Object("schemaVersion",Schema,"catalogVersion",Version,
            "recipeSourceSha256",RecipeSourceHash,"skeletonSourceSha256",SkeletonSourceHash,
            "rotationPeriod",7,"formalOrderSize",3,"warmupOrderSize",3,"layouts",Layouts.Select(x=>x.Json()).ToArray());

        public static DailyLayoutCatalog Load(string json,string expectedDigest)
        {
            json=json.TrimEnd('\r','\n');if(CanonicalJson.Hash(json)!=expectedDigest)throw new ArgumentException("Layout catalog digest mismatch");
            var root=CanonicalJson.Map(CanonicalJson.Parse(json));
            if((string)root["schemaVersion"]!=Schema||(string)root["catalogVersion"]!=Version||CanonicalJson.Int(root["rotationPeriod"])!=7)
                throw new ArgumentException("Unknown layout catalog schema/version");
            var layouts=CanonicalJson.Array(root["layouts"]).Select(entry=>
            {
                var m=CanonicalJson.Map(entry);var warm=CanonicalJson.Map(m["warmup"]);
                return new DailyLayoutPair(CanonicalJson.Int(m["index"]),(string)m["layoutId"],
                    ReadPlates(m["formalPlates"]),ReadPlates(warm["plates"]),CanonicalJson.Array(warm["openingKinds"]).Cast<string>(),(string)warm["tutorialKind"]);
            });
            var result=new DailyLayoutCatalog((string)root["recipeSourceSha256"],(string)root["skeletonSourceSha256"],layouts);
            if(result.CanonicalJsonText!=json)throw new ArgumentException("Non-canonical layout catalog");
            return result;
        }
        static IEnumerable<PlateDefinition> ReadPlates(object value)=>CanonicalJson.Array(value).Select(entry=>
        {var p=CanonicalJson.Map(entry);return new PlateDefinition(CanonicalJson.Int(p["plateId"]),CanonicalJson.Array(p["kinds"]).Cast<string>());});
    }
}
