using System;
using System.Collections.Generic;
using System.Linq;
using HotpotSort.Determinism;

namespace HotpotSort.Core
{
    // Ordinary-layout port of the audited 1.5111 five-category branch. Host uses
    // PCG (not Unity random) and stable first-seen enumeration. BubbleType is
    // mapped from ORIGINAL ordinary plate capacity, never its remaining count.
    internal sealed class NativeD1Director
    {
        readonly DailyContent content;
        bool enabled, latched;
        int limit, used;
        float start, end;
        internal NativeD1Director(DailyContent content) { this.content = content; }
        internal sealed class Candidate
        {
            internal string Kind;
            internal int B, V, D, Missing;
            internal object Json() => CanonicalJson.Object("kind",Kind,"b",B,"v",V,"d",D,"missingUnitCost",Missing);
        }
        sealed class Supply
        {
            internal readonly Dictionary<string,int> Temp=new Dictionary<string,int>(), Ordinary=new Dictionary<string,int>();
            internal readonly List<CoreItem> Live=new List<CoreItem>(), Above=new List<CoreItem>();
        }
        static void Add(Dictionary<string,int> c,string k) { if(k==null)return;c[k]=c.TryGetValue(k,out int n)?n+1:1; }
        static int Count(Dictionary<string,int> c,string k) => c.TryGetValue(k,out int n)?n:0;
        static int Subtract(Dictionary<string,int> c,string k,int need)
        { int take=Math.Min(Count(c,k),need);if(take==0)return need;c[k]-=take;if(c[k]==0)c.Remove(k);return need-take; }
        static IEnumerable<CoreOrder> Targets(CoreOrder[] orders,int slot) => orders.Where((o,i)=>i!=slot&&o.Enabled&&o.Kind!=null);
        static HashSet<string> Existing(CoreOrder[] orders,int slot) => new HashSet<string>(Targets(orders,slot).Select(o=>o.Kind));
        static IEnumerable<CoreItem> OrderedActive(IReadOnlyList<CoreItem> items,IReadOnlyList<int> active)
        { return active.SelectMany(p=>items.Where(x=>x.PlateId==p&&x.Location=="ActiveAvailable").OrderBy(x=>x.SourceIndex)); }
        static Supply Gather(int slot,IReadOnlyList<CoreItem> items,CoreOrder[] orders,int?[] buffer,IReadOnlyList<int> active,ISet<int> above)
        {
            var s=new Supply();var byId=items.ToDictionary(x=>x.Id);
            foreach(var id in buffer)if(id.HasValue)Add(s.Temp,byId[id.Value].Kind);
            foreach(var x in OrderedActive(items,active)) { s.Live.Add(x);if(above!=null&&above.Contains(x.Id))s.Above.Add(x);else Add(s.Ordinary,x.Kind); }
            foreach(var o in Targets(orders,slot)){int need=Subtract(s.Temp,o.Kind,Math.Max(0,3-o.Items.Count));Subtract(s.Ordinary,o.Kind,need);}
            return s;
        }
        internal List<Candidate> Costs(int slot,IReadOnlyList<CoreItem> items,CoreOrder[] orders,int?[] buffer,IReadOnlyList<int> active,ISet<int> above,IReadOnlyDictionary<int,int> plateCapacities=null)
        {
            var s=Gather(slot,items,orders,buffer,active,above);var result=new List<Candidate>();
            foreach(string k in s.Temp.Keys.Concat(s.Ordinary.Keys).Distinct())
            {
                int missing=5;
                if(s.Above.Count>0)foreach(int p in active)
                {
                    // Type is initial capacity; a depleted large plate remains eligible.
                    if((plateCapacities!=null&&plateCapacities.TryGetValue(p,out int capacity)?capacity:items.Count(x=>x.PlateId==p))<3)continue;
                    var live=s.Live.Where(x=>x.PlateId==p).ToList();
                    if(live.Any(x=>above.Contains(x.Id)))continue;
                    var values=live.Select(x=>x.Kind).ToList();values.Remove(k);
                    foreach(var o in Targets(orders,slot))for(int n=o.Items.Count;n<3;n++)values.Remove(o.Kind);
                    missing=Math.Min(missing,values.Count);
                }
                int b=Count(s.Temp,k),v=Count(s.Ordinary,k),tb=Math.Min(3,b),tv=Math.Min(3-tb,v);
                result.Add(new Candidate{Kind=k,B=b,V=v,Missing=missing,D=-tb+(3-tb-tv)*missing});
            }
            return result;
        }
        internal static bool Matches(int category,int d,int s)
        {switch(category){case 0:return d<0;case 1:return d==0;case 2:return d>=1&&d<s-1;case 3:return d==s-1;case 4:return d==s||d==s+1;default:return false;}}
        internal static int[] Fallback(int c,float p)
        { switch(c){case 0:return new[]{0,1,2,3,4};case 1:return p<=.4f?new[]{1,0}:new[]{1,2,3,4};case 2:return new[]{2,1,0};case 3:return new[]{3,2,1,0};default:return new[]{4,3,2,1,0};} }
        internal static int Sample(IReadOnlyList<int> weights,float u)
        {float sum=0;for(int i=0;i<5;i++){sum+=(float)weights[i]/100f;if(u<sum)return i;}return 0;}
        static float Draw(Pcg32 rng) => (float)(rng.Next()/(double)uint.MaxValue);
        static string First(List<Candidate> entries,HashSet<string> existing,Func<int,bool> predicate)
        {foreach(bool avoid in new[]{true,false})foreach(var c in entries)if((!avoid||!existing.Contains(c.Kind))&&predicate(c.D))return c.Kind;return null;}
        static string FirstMax(Dictionary<string,int> counts,HashSet<string> existing,ISet<string> allowed=null)
        {foreach(bool avoid in new[]{true,false}){string best=null;int n=0;foreach(var pair in counts)if((allowed==null||allowed.Contains(pair.Key))&&(!avoid||!existing.Contains(pair.Key))&&pair.Value>n){best=pair.Key;n=pair.Value;}if(best!=null)return best;}return null;}
        internal static string Opening(IReadOnlyList<CoreItem> items,IReadOnlyList<int> pending,CoreOrder[] orders)
        {
            var c=new Dictionary<string,int>();foreach(int p in pending.Take(10))foreach(var x in items.Where(x=>x.PlateId==p).OrderBy(x=>x.SourceIndex))Add(c,x.Kind);
            foreach(var o in Targets(orders,-1))Subtract(c,o.Kind,3-o.Items.Count);
            if(c.Count==0)foreach(int p in pending)foreach(var x in items.Where(x=>x.PlateId==p).OrderBy(x=>x.SourceIndex))Add(c,x.Kind);
            return FirstMax(c,Existing(orders,-1));
        }
        static Dictionary<string,int> Global(int slot,IReadOnlyList<CoreItem> items,CoreOrder[] orders,int?[] buffer,IReadOnlyList<int> pending,IReadOnlyList<int> active,ISet<int> above)
        {
            var near=new Dictionary<string,int>();var far=new Dictionary<string,int>();var byId=items.ToDictionary(x=>x.Id);
            foreach(var x in OrderedActive(items,active))Add(above!=null&&above.Contains(x.Id)?far:near,x.Kind);
            foreach(var id in buffer)if(id.HasValue)Add(near,byId[id.Value].Kind);
            foreach(int p in pending)foreach(var x in items.Where(x=>x.PlateId==p&&x.Location=="Pending").OrderBy(x=>x.SourceIndex))Add(far,x.Kind);
            foreach(var o in Targets(orders,slot)){int need=Subtract(near,o.Kind,3-o.Items.Count);Subtract(far,o.Kind,need);}
            return near.Count==0?far:near;
        }
        internal DailyDirector.Decision Choose(int slot,IReadOnlyList<CoreItem> items,CoreOrder[] orders,int?[] buffer,IReadOnlyList<int> pending,IReadOnlyList<int> active,ISet<int> above,Pcg32 rng,float progress,ISet<string> allowedKinds=null,bool select=true,IReadOnlyDictionary<int,int> plateCapacities=null)
        {
            progress=Math.Max(0,Math.Min(1,progress));int occupied=buffer.Count(x=>x.HasValue),s=buffer.Length-occupied;
            int band=progress<=.4f?0:progress<=.65f?1:progress<=.85f?2:3;
            var row=content.Rows.Single(r=>r.ProgressBand==band&&r.BufferBand==Math.Min(4,occupied));
            // Native samples before examining empty categories. Preview deliberately skips RNG/state.
            int sampled=select?Sample(row.Weights,Draw(rng)):0;
            var all=Costs(slot,items,orders,buffer,active,above,plateCapacities);var entries=all.Where(c=>allowedKinds==null||allowedKinds.Contains(c.Kind)).ToList();
            var existing=Existing(orders,slot);var route=new List<object>();string chosen=null;
            if(select)latched=false;
            if(enabled&&progress>=start&&progress<=end&&used<limit)
            {
                Func<int,bool>[] predicates=s>=3?new Func<int,bool>[]{d=>d>=1&&d<s,d=>d==0}:s==2?new Func<int,bool>[]{d=>d==1,d=>d==0}:new Func<int,bool>[]{d=>d==-1,d=>d<0,d=>d==0};
                foreach(var pred in predicates){chosen=First(entries,existing,pred);if(chosen!=null){route.Add("priority");break;}}
            }
            if(chosen==null)foreach(int category in Fallback(sampled,progress)){route.Add(category);chosen=First(entries,existing,d=>Matches(category,d,s));if(chosen!=null)break;}
            var global=Global(slot,items,orders,buffer,pending,active,above);
            if(chosen==null){route.Add("global");chosen=FirstMax(global,existing,allowedKinds);}
            // Preview answers existence, independent of sampled category, without mutation.
            bool has=entries.Count>0||global.Keys.Any(k=>allowedKinds==null||allowedKinds.Contains(k));
            return new DailyDirector.Decision{Kind=chosen,HasCandidates=has,Fallback=route.Count==0?"none":route[route.Count-1].ToString(),Diagnostic=CanonicalJson.Object("algorithm","native_d1_1.5111_ordinary_v1","progress",Fixed(progress),"bufferFree",s,"sampledCategory",sampled,"weights",row.Weights,"route",route,"candidates",all.Select(c=>c.Json()).ToArray(),"priority",StateJson(),"selected",chosen,"preview",!select)};
        }
        // Explicit fixed precision avoids Mono/CoreCLR shortest-roundtrip formatting drift.
        static string Fixed(float value) => ((decimal)Math.Round((double)value*1000000)/1000000m).ToString("0.######",System.Globalization.CultureInfo.InvariantCulture);
        internal object StateJson() => CanonicalJson.Object("enabled",enabled,"limit",limit,"used",used,"start",Fixed(start),"end",Fixed(end),"latched",latched);
        internal void ArmPriority(int reason,float progress,Pcg32 rng,bool variantA=true)
        {
            if(!variantA)return;
            enabled=false;limit=used=0;start=end=0;latched=false;
            bool early=reason==1;
            if(!early&&progress>.6f)return;
            if(Draw(rng)>=(early?.3f:.1f))return;
            enabled=true;limit=Draw(rng)<.7f?1:2;start=early?.35f:.6f;end=.8f;
        }
        internal void ObservePriority(IReadOnlyList<CoreItem> items,CoreOrder[] orders,int?[] buffer,ISet<int> above,float progress,Pcg32 rng,int activeTargetCount)
        {
            if(!enabled||progress<start||progress>end||used>=limit||latched||buffer.Count(x=>!x.HasValue)!=2)return;
            var byId=items.ToDictionary(x=>x.Id);
            foreach(var o in orders.Where(o=>o.Enabled).Take(activeTargetCount))
            {
                if(o.Kind==null)return;
                if(buffer.Any(id=>id.HasValue&&byId[id.Value].Kind==o.Kind))return;
                if(items.Any(x=>x.Location=="ActiveAvailable"&&x.Kind==o.Kind&&(above==null||!above.Contains(x.Id))))return;
            }
            latched=true;if(used>=limit-1)ArmPriority(2,progress,rng);else used++;
        }
        internal static List<object> Exchange(string kind,int slot,IReadOnlyList<CoreItem> items,CoreOrder[] orders,int?[] buffer,IReadOnlyList<int> pending,IReadOnlyList<int> active,ISet<int> above)
        {
            var result=new List<object>();if(kind==null||above==null)return result;
            var live=OrderedActive(items,active).ToList();var byId=items.ToDictionary(x=>x.Id);
            int need=3-buffer.Count(id=>id.HasValue&&byId[id.Value].Kind==kind)-live.Count(x=>!above.Contains(x.Id)&&x.Kind==kind)-live.Count(x=>above.Contains(x.Id)&&x.Kind==kind);
            var existing=Existing(orders,slot);
            foreach(var x in live.Where(x=>above.Contains(x.Id)))
            {
                if(need<=0)break;if(x.Kind==kind||existing.Contains(x.Kind))continue;
                var target=pending.SelectMany(p=>items.Where(v=>v.PlateId==p&&v.Location=="Pending").OrderBy(v=>v.SourceIndex)).FirstOrDefault(v=>v.Kind==kind);
                if(target==null)continue;
                string old=x.Kind;x.Kind=kind;target.Kind=old;need--;
                result.Add(CanonicalJson.Object("aboveItemId",x.Id,"pendingItemId",target.Id,"oldKind",old,"newKind",kind));
            }
            return result;
        }
    }
}



