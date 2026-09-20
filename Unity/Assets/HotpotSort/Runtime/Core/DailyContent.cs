using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HotpotSort.Determinism;

namespace HotpotSort.Core
{
    public sealed class PlateDefinition
    {
        public int PlateId { get; }
        public IReadOnlyList<string> Kinds { get; }
        public PlateDefinition(int plateId, IEnumerable<string> kinds)
        { PlateId = plateId; Kinds = new ReadOnlyCollection<string>(new List<string>(kinds)); }
    }
    public sealed class WeightRow
    {
        public int ProgressBand { get; }
        public int BufferBand { get; }
        public IReadOnlyList<int> Weights { get; }
        public WeightRow(int progressBand, int bufferBand, IEnumerable<int> weights)
        { ProgressBand = progressBand; BufferBand = bufferBand; Weights = new ReadOnlyCollection<int>(new List<int>(weights)); }
    }
    public sealed class IngredientEntry
    {
        public string IngredientId { get; }
        public string ResourceAddress { get; }
        public string DisplayNameKey { get; }
        public string ColliderSpec { get; }
        public string SizeClass { get; }
        public string ContentVersion { get; }
        public IngredientEntry(string ingredientId, string resourceAddress, string displayNameKey, string colliderSpec, string sizeClass, string contentVersion)
        {
            foreach (var v in new[] { ingredientId, resourceAddress, displayNameKey, colliderSpec, sizeClass, contentVersion })
                if (string.IsNullOrWhiteSpace(v)) throw new ArgumentException("Incomplete ingredient catalog entry");
            IngredientId = ingredientId; ResourceAddress = resourceAddress; DisplayNameKey = displayNameKey;
            ColliderSpec = colliderSpec; SizeClass = sizeClass; ContentVersion = contentVersion;
        }
        internal object Json() => CanonicalJson.Object("ingredientId", IngredientId, "resourceAddress", ResourceAddress,
            "displayNameKey", DisplayNameKey, "colliderSpec", ColliderSpec, "sizeClass", SizeClass, "contentVersion", ContentVersion);
    }
    public sealed class DailyContent
    {
        public const string Schema = "daily_content_v1";
        public const string DirectorVersion = "director_v1_approx_d";
        public const string ImporterVersion = "daily_importer_v1_decimal100";
        public const string SkeletonSourceHash = "7fb75bf293a460ac6bd64a436c180a760fef0f0a0f2d49c2b63bf1b4f09bf452";
        public const string WeightsSourceHash = "40031a2b589537f7c186354b77506d1662179d424b2aa9244ded1edc1a689bbe";
        public string ContentVersion { get; }
        public string Digest { get; }
        public string CanonicalJsonText { get; }
        public IReadOnlyList<PlateDefinition> Plates { get; }
        public IReadOnlyList<WeightRow> Rows { get; }
        public DailyContent(string contentVersion, IEnumerable<PlateDefinition> plates, IEnumerable<WeightRow> rows,
            string skeletonSourceHash = SkeletonSourceHash, string weightsSourceHash = WeightsSourceHash, string importerVersion = ImporterVersion)
        {
            if (string.IsNullOrWhiteSpace(contentVersion)) throw new ArgumentException("Missing content version");
            if (skeletonSourceHash != SkeletonSourceHash || weightsSourceHash != WeightsSourceHash || importerVersion != ImporterVersion)
                throw new ArgumentException("Content source/importer identity mismatch");
            ContentVersion = contentVersion;
            Plates = new ReadOnlyCollection<PlateDefinition>(new List<PlateDefinition>(plates));
            Rows = new ReadOnlyCollection<WeightRow>(new List<WeightRow>(rows).OrderBy(r => r.ProgressBand).ThenBy(r => r.BufferBand).ToList());
            Validate(); CanonicalJsonText = CanonicalJson.Write(Json()); Digest = CanonicalJson.Hash(CanonicalJsonText);
        }
        void Validate()
        {
            if (Plates.Count != 50) throw new ArgumentException("Expected 50 plates");
            var counts = new int[16]; int total = 0;
            for (int i = 0; i < Plates.Count; i++)
            {
                var plate = Plates[i]; if (plate.PlateId != i + 1 || plate.Kinds.Count == 0) throw new ArgumentException("Invalid plate ID/order/count");
                foreach (var kind in plate.Kinds)
                { if (kind == null || kind.Length != 1 || kind[0] < 'A' || kind[0] > 'P') throw new ArgumentException("Invalid kind"); counts[kind[0] - 'A']++; total++; }
            }
            if (total != 183 || counts.Any(n => n == 0 || n % 3 != 0)) throw new ArgumentException("Invalid 183-item/16-kind multiplicities");
            if (Rows.Count != 20) throw new ArgumentException("Expected 20 weight rows");
            for (int i = 0; i < Rows.Count; i++)
            {
                var row = Rows[i]; if (row.ProgressBand != i / 5 || row.BufferBand != i % 5 || row.Weights.Count != 5 || row.Weights.Any(n => n < 0) || !row.Weights.Any(n => n > 0))
                    throw new ArgumentException("Missing, duplicate or invalid weight row");
                if (row.Weights.Sum(n => (long)n) > uint.MaxValue) throw new ArgumentException("Weight total exceeds bounded RNG range");
            }
            if (!Rows[0].Weights.SequenceEqual(new[] { 0, 70, 30, 0, 0 }) || !Rows[4].Weights.SequenceEqual(new[] { 90, 10, 0, 0, 0 }))
                throw new ArgumentException("Known weight sample mismatch");
        }
        public object Json() => CanonicalJson.Object("schemaVersion", Schema, "contentVersion", ContentVersion,
            "skeletonId", "C", "skeletonVersion", "skeleton_c_v1", "sourceProfile", "OriginalDifficulty3",
            "skeletonSourceSha256", SkeletonSourceHash, "weightsSourceSha256", WeightsSourceHash, "importerVersion", ImporterVersion,
            "directorAlgorithmVersion", DirectorVersion, "rngAlgorithm", Pcg32.Version, "seedAlgorithm", Pcg32.SeedVersion,
            "totalPlates", 50, "totalItems", 183, "ingredientKinds", 16, "activeOrderSlots", 2, "maxOrderSlots", 4,
            "orderSize", 3, "bufferSize", 5, "openingLookaheadPlates", 10, "progressThresholdPercent", new[] { 40, 65, 85, 100 }, "interferenceUnitSize", 3,
            "plates", Plates.Select(p => CanonicalJson.Object("plateId", p.PlateId, "kinds", p.Kinds)).ToArray(),
            "rows", Rows.Select(r => CanonicalJson.Object("progressBand", r.ProgressBand, "bufferBand", r.BufferBand, "weights", r.Weights)).ToArray());
        public static DailyContent Load(string json, string expectedDigest)
        {
            if (CanonicalJson.Hash(json) != expectedDigest) throw new ArgumentException("Content digest mismatch");
            var m = CanonicalJson.Map(CanonicalJson.Parse(json));
            if ((string)m["schemaVersion"] != Schema) throw new ArgumentException("Unknown content schema");
            var plates = CanonicalJson.Array(m["plates"]).Select(x => { var p = CanonicalJson.Map(x); return new PlateDefinition(CanonicalJson.Int(p["plateId"]), CanonicalJson.Array(p["kinds"]).Cast<string>()); });
            var rows = CanonicalJson.Array(m["rows"]).Select(x => { var r = CanonicalJson.Map(x); return new WeightRow(CanonicalJson.Int(r["progressBand"]), CanonicalJson.Int(r["bufferBand"]), CanonicalJson.Array(r["weights"]).Select(CanonicalJson.Int)); });
            var result = new DailyContent((string)m["contentVersion"], plates, rows, (string)m["skeletonSourceSha256"], (string)m["weightsSourceSha256"], (string)m["importerVersion"]);
            if (result.CanonicalJsonText != json) throw new ArgumentException("Non-canonical content or unsupported configuration/version");
            return result;
        }
    }
}
