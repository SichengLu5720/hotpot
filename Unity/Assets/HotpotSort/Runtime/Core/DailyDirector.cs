using System;
using System.Collections.Generic;
using System.Linq;
using HotpotSort.Determinism;

namespace HotpotSort.Core
{
    internal sealed class DailyDirector
    {
        readonly DailyContent content;
        internal DailyDirector(DailyContent content) { this.content = content; }
        internal sealed class Decision { internal string Kind; internal object Diagnostic; internal string Fallback; }
        internal static int ProgressBand(int numerator) { return numerator * 100 <= 183 * 40 ? 0 : numerator * 100 <= 183 * 65 ? 1 : numerator * 100 <= 183 * 85 ? 2 : 3; }
        sealed class Candidate
        {
            internal string Kind, Reason;
            internal int Remaining, Reserved, B, V, Need, Interference, D, Category = -1;
            internal bool Legal, Duplicate;
            internal readonly List<int> Scanned = new List<int>();
            internal object Json() => CanonicalJson.Object("kind", Kind, "remaining", Remaining, "reserved", Reserved,
                "allocatable", Remaining - Reserved, "b", B, "v", V, "need", Need, "interferenceItems", Interference,
                "scannedPlateIds", Scanned, "d", Legal ? (object)D : null, "category", Category < 0 ? null : (object)(Category + 1),
                "legal", Legal, "duplicate", Duplicate, "reason", Reason);
        }
        internal Decision Choose(int slot, IReadOnlyList<CoreItem> items, CoreOrder[] orders, int?[] buffer, List<int> pending, Pcg32 rng, bool select = true)
        {
            var reserved = new HashSet<int>(); var reservations = new List<object>();
            var external = items.Where(x => x.Location == "Pending" || x.Location == "ActiveAvailable" || x.Location == "Buffer").ToList();
            // Stable reservation order is independent of list/dictionary enumeration.
            var reservationPool = external.OrderBy(x => x.Location == "Buffer" ? 0 : x.Location == "ActiveAvailable" ? 1 : 2)
                .ThenBy(x => x.Location == "Buffer" ? x.Slot : x.PlateId).ThenBy(x => x.SourceIndex).ToList();
            for (int other = 0; other < 2; other++)
            {
                if (other == slot || orders[other].Kind == null) continue;
                var ids = reservationPool.Where(x => x.Kind == orders[other].Kind && !reserved.Contains(x.Id)).Take(3 - orders[other].Items.Count).Select(x => x.Id).ToArray();
                foreach (int id in ids) reserved.Add(id);
                reservations.Add(CanonicalJson.Object("slotId", other, "kind", orders[other].Kind, "unmet", 3 - orders[other].Items.Count, "itemIds", ids));
            }
            int bufferCount = buffer.Count(x => x.HasValue), n = 5 - bufferCount;
            int progress = items.Count(x => x.Location == "Completed" || x.Location == "Order");
            int band = ProgressBand(progress), bb = Math.Min(bufferCount, 4);
            var candidates = new List<Candidate>();
            for (char k = 'A'; k <= 'P'; k++)
            {
                var c = new Candidate { Kind = k.ToString() }; candidates.Add(c);
                c.Remaining = external.Count(x => x.Kind == c.Kind); c.Reserved = external.Count(x => x.Kind == c.Kind && reserved.Contains(x.Id));
                c.Duplicate = orders.Where((o, i) => i != slot && i < 2).Any(o => o.Kind == c.Kind);
                c.B = external.Count(x => x.Kind == c.Kind && x.Location == "Buffer" && !reserved.Contains(x.Id));
                c.V = external.Count(x => x.Kind == c.Kind && x.Location == "ActiveAvailable" && !reserved.Contains(x.Id));
                c.Need = Math.Max(0, 3 - c.V);
                if (c.Remaining - c.Reserved < 3) { c.Reason = "InsufficientAllocatable"; continue; }
                c.Legal = true;
                if (c.B > 0) c.D = -c.B;
                else if (c.V >= 3) c.D = 0;
                else
                {
                    int found = 0;
                    foreach (int plate in pending)
                    {
                        c.Scanned.Add(plate);
                        foreach (var item in items.Where(x => x.PlateId == plate))
                        {
                            if (item.Kind != c.Kind) c.Interference++;
                            else if (!reserved.Contains(item.Id)) found++;
                        }
                        if (found >= c.Need) break; // complete final plate, never break inside its items
                    }
                    if (found < c.Need) { c.Legal = false; c.Reason = "PendingScanInsufficient"; continue; }
                    c.D = Math.Max(1, (c.Interference + 2) / 3);
                }
                c.Category = c.D < 0 ? 0 : c.D == 0 ? 1 : c.D >= 1 && c.D < n - 1 ? 2 : c.D >= 1 && c.D == n - 1 ? 3 : c.D == n || c.D == n + 1 ? 4 : -1;
                c.Reason = c.Duplicate ? "ExcludedFromStrictDuplicate" : c.Category < 0 ? "OutsideBands" : "StrictLegal";
            }
            var strict = candidates.Where(c => c.Legal && !c.Duplicate).ToList();
            bool relaxed = strict.Count == 0;
            var pool = relaxed ? candidates.Where(c => c.Legal).ToList() : strict;
            var raw = content.Rows[band * 5 + bb].Weights.ToArray();
            var filtered = raw.Select((w, i) => pool.Any(c => c.Category == i) ? w : 0).ToArray();
            var draws = new List<object>(); int category = -1; string fallback; Candidate chosen = null;
            long total = filtered.Sum(w => (long)w);
            if (pool.Count == 0) fallback = "EmptyTailSlot";
            else if (total > 0)
            {
                fallback = relaxed ? "AllowDuplicateWeighted" : "None";
                if (select)
                {
                    uint ticket = rng.NextBounded((uint)total, draws);
                    for (int i = 0; i < 5; i++) { if (ticket < filtered[i]) { category = i; break; } ticket -= (uint)filtered[i]; }
                }
            }
            else if (pool.Any(c => c.Category >= 0))
            {
                fallback = relaxed ? "AllowDuplicateZeroWeight" : "ZeroWeightPressureOrder";
                var priority = bb >= 3 ? new[] { 0, 1, 2, 3, 4 } : new[] { 1, 2, 0, 3, 4 };
                category = priority.First(i => pool.Any(c => c.Category == i));
            }
            else fallback = "MinCostOutsideBands";
            if (select && pool.Count > 0)
            {
                var tiePool = category >= 0 ? pool.Where(c => c.Category == category).ToList() : pool.Where(c => c.D == pool.Min(x => x.D)).ToList();
                tiePool.Sort((a, b) => StringComparer.Ordinal.Compare(a.Kind, b.Kind));
                chosen = tiePool[(int)rng.NextBounded((uint)tiePool.Count, draws)];
            }
            return new Decision { Kind = chosen?.Kind, Fallback = fallback, Diagnostic = CanonicalJson.Object(
                "slotId", slot, "progressNumerator", progress, "progressDenominator", 183, "progressBand", new[] { "Early", "Mid", "Late", "End" }[band],
                "buffer", buffer, "bufferBand", bb, "freeBuffer", n, "reservations", reservations, "candidates", candidates.Select(c => c.Json()).ToArray(),
                "rawWeights", raw, "filteredWeights", filtered, "strictPoolCount", strict.Count, "relaxed", relaxed, "draws", draws,
                "selectedCategory", chosen == null || chosen.Category < 0 ? null : (object)(chosen.Category + 1), "selectedKind", chosen?.Kind,
                "fallback", fallback, "algorithm", DailyContent.DirectorVersion) };
        }
    }
}
