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
        internal sealed class Decision { internal string Kind; internal object Diagnostic; internal string Fallback; internal bool HasCandidates; }
        internal static int ProgressBand(int numerator) { return numerator * 100 <= 183 * 40 ? 0 : numerator * 100 <= 183 * 65 ? 1 : numerator * 100 <= 183 * 85 ? 2 : 3; }
        sealed class Candidate
        {
            internal string Kind, Reason;
            internal int Remaining, Reserved, B, V, Need, Interference, D, Category = -1;
            internal bool Legal, Duplicate;
            internal int SupplyItems, CostVersion;
            internal bool NewCost, Unresolved;
            internal readonly List<int> Scanned = new List<int>();
            internal object Json() { var result=CanonicalJson.Object("kind", Kind, "remaining", Remaining, "reserved", Reserved,
                "allocatable", Remaining - Reserved, "b", B, "v", V, "need", Need, "interferenceItems", Interference,
                "scannedPlateIds", Scanned, "d", Legal ? (object)D : null, "category", Category < 0 ? null : (object)(Category + 1),
                "legal", Legal, "duplicate", Duplicate, "reason", Reason);if(NewCost){result.Add("costVersion",CostVersion);result.Add("supplyItems",SupplyItems);result.Add("unresolved",Unresolved);if(Unresolved)result["d"]=null;}return result; }
        }
        // -1 means the whole pool (both effective weights zero).
        internal static int ReliefGroup(bool buffered,bool plain,int bufferWeight,int plainWeight,Pcg32 rng,List<object> draws)
        {
            if(!buffered)return 1;if(!plain)return 0;
            long total=(long)bufferWeight+plainWeight;
            if(total==0)return -1;
            return rng.NextBounded((uint)total,draws)<bufferWeight?0:1;
        }
        internal static int SupportTakeoverPercent(int consecutiveFormalFailures, int supportVersion=2)
        {
            if(consecutiveFormalFailures<0)throw new ArgumentOutOfRangeException(nameof(consecutiveFormalFailures));
            return supportVersion>=2?20+Math.Min(3*Math.Min(consecutiveFormalFailures,4),10):20+10*Math.Min(consecutiveFormalFailures,3);
        }
        internal Decision Choose(int slot, IReadOnlyList<CoreItem> items, CoreOrder[] orders, int?[] buffer, List<int> pending, Pcg32 rng, bool select = true,ISet<int> clickable=null,ISet<int> unknown=null,int completedOrders=15,int policyVersion=2,bool strictKinds=false,ISet<int> nextLayer=null,int normalPotCount=2,string swapFromKind=null,int consecutiveFormalFailures=0,int supportVersion=1,bool formalStage=false,ISet<int> offscreen=null,IReadOnlyList<int> activePlateOrder=null,int costVersion=2)
        {
            bool firstTen=supportVersion>=2&&formalStage&&completedOrders<10;
            int takeoverPercent=firstTen?100:SupportTakeoverPercent(consecutiveFormalFailures,supportVersion);
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
            bool versioned=offscreen!=null&&clickable!=null&&policyVersion==6;
            bool restoredD1=versioned&&costVersion>=3;
            bool newCost=versioned&&!restoredD1;
            var externalByPlate=newCost?external.Where(x=>x.Location!="Buffer").GroupBy(x=>x.PlateId).ToDictionary(g=>g.Key,g=>g.ToArray()):null;
            int SceneCount(string kind)
            {
                var direct=external.Where(x=>x.Location=="ActiveAvailable"&&clickable.Contains(x.Id)).ToArray();
                int count=direct.Count(x=>x.Kind==kind&&!reserved.Contains(x.Id));
                if(count>=3||nextLayer==null)return count;
                var acceptedPrerequisites=new HashSet<int>();
                foreach(int plate in (activePlateOrder??Array.Empty<int>()))
                {
                    var targets=external.Where(x=>x.PlateId==plate&&x.Location=="ActiveAvailable"&&x.Kind==kind&&!reserved.Contains(x.Id)&&nextLayer.Contains(x.Id)).ToArray();
                    if(targets.Length==0)continue;
                    // The existing next-layer certificate removes all clickable food on
                    // this plate. Require room in existing orders for that entire prefix.
                    var prefix=direct.Where(x=>x.PlateId==plate).Concat(direct.Where(x=>acceptedPrerequisites.Contains(x.Id))).GroupBy(x=>x.Id).Select(g=>g.First()).ToArray();
                    bool safe=prefix.Where(x=>x.Kind!=kind).GroupBy(x=>x.Kind).All(g=>g.Count()<=orders.Where((o,i)=>i!=slot&&o.Enabled&&o.Kind==g.Key).Sum(o=>Math.Max(0,3-o.Items.Count)));
                    if(!safe)continue;
                    foreach(var x in prefix)acceptedPrerequisites.Add(x.Id);
                    count+=targets.Length;if(count>=3)break;
                }
                return count;
            }
            var candidates = new List<Candidate>();
            for (char k = 'A'; k <= 'P'; k++)
            {
                var c = new Candidate { Kind = k.ToString(),NewCost=versioned,CostVersion=costVersion }; candidates.Add(c);
                c.Remaining = external.Count(x => x.Kind == c.Kind); c.Reserved = external.Count(x => x.Kind == c.Kind && reserved.Contains(x.Id));
                c.Duplicate = orders.Where((o, i) => i != slot && o.Enabled).Any(o => o.Kind == c.Kind);
                c.B = external.Count(x => x.Kind == c.Kind && x.Location == "Buffer" && !reserved.Contains(x.Id));
                c.V = newCost?SceneCount(c.Kind):external.Count(x => x.Kind == c.Kind && x.Location == "ActiveAvailable" && !reserved.Contains(x.Id));
                c.Need = Math.Max(0, 3 - c.V-((newCost||restoredD1)?c.B:0));
                if (c.Remaining - c.Reserved < 3) { c.Reason = "InsufficientAllocatable"; continue; }
                c.Legal = true;
                if(newCost)
                {
                    if(c.V>=3)c.D=0;
                    else if(c.V+c.B>=3)c.D=-c.B;
                    else
                    {
                        int found=0;
                        foreach(int plate in (activePlateOrder??Array.Empty<int>()).Concat(pending).Distinct())
                        {
                            if(!externalByPlate.TryGetValue(plate,out var members))continue;
                            var future=members.Where(x=>x.Location=="Pending"||x.Location=="ActiveAvailable"&&offscreen.Contains(x.Id)).ToArray();
                            if(future.Length==0)continue;
                            c.Scanned.Add(plate);c.SupplyItems+=future.Length;
                            found+=future.Count(x=>x.Kind==c.Kind&&!reserved.Contains(x.Id));
                            if(found>=c.Need)break;
                        }
                        // Unknown or blocked on-screen targets have no certified supply
                        // distance. Keep them outside weighted categories, never call them far.
                        if(found<c.Need){c.Unresolved=true;c.D=int.MaxValue;c.Reason="UnresolvedSceneAccess";continue;}
                        int divisor=costVersion>=2?4:3; c.D=Math.Max(1,(c.SupplyItems+divisor-1)/divisor);
                    }
                }
                else if (c.B > 0&&(!restoredD1||(c.V<3&&c.V+c.B>=3))) c.D = -c.B;
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
            var draws = new List<object>();
            int dangerCount=0,threshold=Math.Max(0,normalPotCount-1),supportRoll=-1;string supportBranch=null;
            Func<CoreItem,bool> supported=x=>x.Location=="Buffer"||x.Location=="ActiveAvailable"&&((clickable?.Contains(x.Id)??false)||(nextLayer?.Contains(x.Id)??false));
            Func<Candidate,int> support=c=>external.Count(x=>x.Kind==c.Kind&&!reserved.Contains(x.Id)&&supported(x));
            if(policyVersion==6)
            {
                // Allocate certified support once in stable slot order, including advertised extra pots.
                var available=external.Where(supported).GroupBy(x=>x.Kind).ToDictionary(g=>g.Key,g=>g.Count());
                for(int i=0;i<orders.Length;i++)
                {
                    var o=orders[i];if(i==slot||!o.Enabled||o.Kind==null||o.Items.Count>=3)continue;
                    int count=available.TryGetValue(o.Kind,out var value)?value:0,need=3-o.Items.Count;
                    if(count<need)dangerCount++;
                    available[o.Kind]=Math.Max(0,count-need);
                }
                if(swapFromKind!=null)
                {pool=strict.Where(c=>c.Kind!=swapFromKind&&support(c)>=3).ToList();supportBranch="SwapComplete";}
                else if(dangerCount>=threshold&&select&&(
                    supportVersion>=2
                        ?firstTen||(supportRoll=(int)rng.NextBounded(100,draws))<takeoverPercent
                        :consecutiveFormalFailures==0
                        ?(supportRoll=(int)rng.NextBounded(5,draws))==0
                        :(supportRoll=(int)rng.NextBounded(10,draws))<takeoverPercent/10))
                {
                    var complete=strict.Where(c=>support(c)>=3).ToList();
                    if(complete.Count>0){pool=complete;supportBranch="Complete";}
                    else
                    {
                        int best=strict.Count==0?0:strict.Max(support);
                        if(best>0){pool=strict.Where(c=>support(c)==best).ToList();supportBranch="MostSupported";}
                        else supportBranch="AllZeroBase";
                    }
                }
                else supportBranch=dangerCount<threshold?"BelowThresholdBase":select?"ProbabilityBase":"QueryBase";
            }
            var raw = content.Rows[band * 5 + bb].Weights.ToArray();
            if(newCost&&costVersion>=2){int relief=raw[0];raw[0]=raw[1];raw[1]=relief;}
            var filtered = raw.Select((w, i) => pool.Any(c => c.Category == i) ? w : 0).ToArray();
            int category = -1; string fallback; Candidate chosen = null;
            bool early=completedOrders<(policyVersion==5?30:15);
            bool actionable=policyVersion!=6&&clickable!=null&&orders.Where((o,i)=>i!=slot&&o.Enabled&&o.Kind!=null&&o.Items.Count<3)
                .Any(o=>external.Count(x=>x.Kind==o.Kind&&(x.Location=="Buffer"||x.Location=="ActiveAvailable"&&clickable.Contains(x.Id)))>=(early?3-o.Items.Count:1));
            var protectedPool=policyVersion==6||clickable==null||actionable?new List<Candidate>():strict.Where(c=>external.Count(x=>x.Kind==c.Kind&&!reserved.Contains(x.Id)&&(x.Location=="Buffer"||x.Location=="ActiveAvailable"&&clickable.Contains(x.Id)))>=3).OrderBy(c=>c.Kind,StringComparer.Ordinal).ToList();
            // Unknown is never a negative observation. Use relief only when every
            // possible completion of the observation yields the exact same pool.
            if(policyVersion!=6&&clickable!=null&&unknown!=null&&unknown.Count>0&&!actionable)
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
            if(policyVersion!=6&&select&&protectedPool.Count>0&&(policyVersion<4||early))
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
            if(chosen!=null){fallback=early?(policyVersion==5?"VisibleReliefFirst30":"VisibleReliefFirst15"):"VisibleRelief80";category=chosen.Category;}
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
                "fallback", fallback, "algorithm", restoredD1?"director_d1_buffer_ready_v3":newCost?(costVersion>=2?"director_supply_cost_v2":"director_supply_cost_v1"):DailyContent.DirectorVersion);
            if(reliefRoll>=0)diagnostic.Add("clickableRelief",CanonicalJson.Object("version",1,"roll",early?(object)null:reliefRoll,"branch",early?(policyVersion==5?"First30Guaranteed":"First15Guaranteed"):reliefRoll<4?"Protected80":"Original20","candidateKinds",protectedPool.Select(c=>c.Kind).ToArray()));
            if(reliefGroup!=-2)CanonicalJson.Map(diagnostic["clickableRelief"]).Add("groupSelection",CanonicalJson.Object("policyVersion",3,"bufferWeight",raw[0],"plainWeight",raw[1],"selectedGroup",reliefGroup==0?"Buffered":reliefGroup==1?"Plain":"WholePoolZeroWeight"));
            if(policyVersion==6)
            {
                var supportDiagnostic=CanonicalJson.Object("policyVersion",6,"probabilityVersion",supportVersion>=2?"FirstTen100FailureStreak20To30_v2":consecutiveFormalFailures==0?"Threshold20Percent_v1":"FailureStreak20To50_v1","takeoverPercent",takeoverPercent,"roll",supportRoll<0?null:(object)supportRoll,"normalPotCount",normalPotCount,"dangerCount",dangerCount,"threshold",threshold,"branch",supportBranch,"candidateKinds",pool.Select(c=>c.Kind).ToArray(),"support",strict.Select(c=>CanonicalJson.Object("kind",c.Kind,"count",support(c))).ToArray());
                if(consecutiveFormalFailures>0)supportDiagnostic.Add("consecutiveFormalFailures",Math.Min(consecutiveFormalFailures,supportVersion>=2?4:3));
                diagnostic.Add("recentSupport",supportDiagnostic);
            }
            return new Decision { Kind=chosen?.Kind,Fallback=fallback,Diagnostic=diagnostic,HasCandidates=pool.Count>0 };
        }
    }
}
