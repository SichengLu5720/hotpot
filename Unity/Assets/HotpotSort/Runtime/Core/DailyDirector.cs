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
        // -1 means the whole pool (both effective weights zero).
        internal static int ReliefGroup(bool buffered,bool plain,int bufferWeight,int plainWeight,Pcg32 rng,List<object> draws)
        {
            if(!buffered)return 1;if(!plain)return 0;
            long total=(long)bufferWeight+plainWeight;
            if(total==0)return -1;
            return rng.NextBounded((uint)total,draws)<bufferWeight?0:1;
        }
        internal Decision Choose(int slot, IReadOnlyList<CoreItem> items, CoreOrder[] orders, int?[] buffer, List<int> pending, Pcg32 rng, bool select = true,ISet<int> clickable=null,ISet<int> unknown=null,int completedOrders=15,int policyVersion=2,bool strictKinds=false)
        {
            var reserved = new HashSet<int>(); var reservations = new List<object>();
            var external = items.Where(x => x.Location == "Pending" || x.Location == "ActiveAvailable" || x.Location == "Buffer").ToList();
            // Stable reservation order is independent of list/dictionary enumeration.
            var reservationPool = external.OrderBy(x => x.Location == "Buffer" ? 0 : x.Location == "ActiveAvailable" ? 1 : 2)
                .ThenBy(x => x.Location == "Buffer" ? x.Slot : x.PlateId).ThenBy(x => x.SourceIndex).ToList();
            for (int other = 0; other < orders.Length; other++)
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
                c.Duplicate = orders.Where((o, i) => i != slot && o.Enabled).Any(o => o.Kind == c.Kind);
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
            var pool = relaxed && !strictKinds ? candidates.Where(c => c.Legal).ToList() : strict;
            var raw = content.Rows[band * 5 + bb].Weights.ToArray();
            var filtered = raw.Select((w, i) => pool.Any(c => c.Category == i) ? w : 0).ToArray();
            var draws = new List<object>(); int category = -1; string fallback; Candidate chosen = null;
            bool early=completedOrders<15;
            bool actionable=clickable!=null&&orders.Where((o,i)=>i!=slot&&o.Enabled&&o.Kind!=null&&o.Items.Count<3)
                .Any(o=>external.Count(x=>x.Kind==o.Kind&&(x.Location=="Buffer"||x.Location=="ActiveAvailable"&&clickable.Contains(x.Id)))>=(early?3-o.Items.Count:1));
            var protectedPool=clickable==null||actionable?new List<Candidate>():strict.Where(c=>external.Count(x=>x.Kind==c.Kind&&!reserved.Contains(x.Id)&&(x.Location=="Buffer"||x.Location=="ActiveAvailable"&&clickable.Contains(x.Id)))>=3).OrderBy(c=>c.Kind,StringComparer.Ordinal).ToList();
            // Unknown is never a negative observation. Use relief only when every
            // possible completion of the observation yields the exact same pool.
            if(clickable!=null&&unknown!=null&&unknown.Count>0&&!actionable)
            {
                bool ambiguous=orders.Where((o,i)=>i!=slot&&o.Enabled&&o.Kind!=null&&o.Items.Count<3)
                    .Any(o=>external.Count(x=>x.Kind==o.Kind&&(x.Location=="Buffer"||x.Location=="ActiveAvailable"&&(clickable.Contains(x.Id)||unknown.Contains(x.Id))))>=(early?3-o.Items.Count:1));
                foreach(var c in strict)
                {
                    int known=external.Count(x=>x.Kind==c.Kind&&!reserved.Contains(x.Id)&&(x.Location=="Buffer"||x.Location=="ActiveAvailable"&&clickable.Contains(x.Id)));
                    int unresolved=external.Count(x=>x.Kind==c.Kind&&!reserved.Contains(x.Id)&&x.Location=="ActiveAvailable"&&unknown.Contains(x.Id));
                    if(known<3&&known+unresolved>=3)ambiguous=true;
                }
                if(ambiguous)protectedPool.Clear();
            }
            int reliefRoll=-1,reliefGroup=-2;
            if(select&&protectedPool.Count>0&&(policyVersion<4||early))
            {
                reliefRoll=early?0:(int)rng.NextBounded(5,draws);
                if(reliefRoll<4)
                {
                    var reliefSelection=protectedPool;
                    if(policyVersion==3)
                    {
                        reliefGroup=ReliefGroup(protectedPool.Any(c=>c.B>0),protectedPool.Any(c=>c.B==0),raw[0],raw[1],rng,draws);
                        if(reliefGroup>=0)reliefSelection=protectedPool.Where(c=>(c.B>0?0:1)==reliefGroup).ToList();
                    }
                    chosen=reliefSelection[(int)rng.NextBounded((uint)reliefSelection.Count,draws)];
                }
            }
            long total = filtered.Sum(w => (long)w);
            if(chosen!=null){fallback=early?"VisibleReliefFirst15":"VisibleRelief80";category=chosen.Category;}
            else if (pool.Count == 0) fallback = "EmptyTailSlot";
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
            if (select && pool.Count > 0&&chosen==null)
            {
                var tiePool = category >= 0 ? pool.Where(c => c.Category == category).ToList() : pool.Where(c => c.D == pool.Min(x => x.D)).ToList();
                tiePool.Sort((a, b) => StringComparer.Ordinal.Compare(a.Kind, b.Kind));
                chosen = tiePool[(int)rng.NextBounded((uint)tiePool.Count, draws)];
            }
            var diagnostic=CanonicalJson.Object(
                "slotId", slot, "progressNumerator", progress, "progressDenominator", 183, "progressBand", new[] { "Early", "Mid", "Late", "End" }[band],
                "buffer", buffer, "bufferBand", bb, "freeBuffer", n, "reservations", reservations, "candidates", candidates.Select(c => c.Json()).ToArray(),
                "rawWeights", raw, "filteredWeights", filtered, "strictPoolCount", strict.Count, "relaxed", relaxed, "draws", draws,
                "selectedCategory", chosen == null || chosen.Category < 0 ? null : (object)(chosen.Category + 1), "selectedKind", chosen?.Kind,
                "fallback", fallback, "algorithm", DailyContent.DirectorVersion);
            if(reliefRoll>=0)diagnostic.Add("clickableRelief",CanonicalJson.Object("version",1,"roll",early?(object)null:reliefRoll,"branch",early?"First15Guaranteed":reliefRoll<4?"Protected80":"Original20","candidateKinds",protectedPool.Select(c=>c.Kind).ToArray()));
            if(reliefGroup!=-2)CanonicalJson.Map(diagnostic["clickableRelief"]).Add("groupSelection",CanonicalJson.Object("policyVersion",3,"bufferWeight",raw[0],"plainWeight",raw[1],"selectedGroup",reliefGroup==0?"Buffered":reliefGroup==1?"Plain":"WholePoolZeroWeight"));
            return new Decision { Kind=chosen?.Kind,Fallback=fallback,Diagnostic=diagnostic };
        }
    }
}
