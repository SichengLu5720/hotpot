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
    }
}
