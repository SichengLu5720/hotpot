using System.Collections.Generic;
using System.Linq;
using HotpotSort.Determinism;

namespace HotpotSort.Core
{
    // A separate stream and identity keep formal mapping/director draws byte-stable.
    public static class WarmupContent
    {
        public const string Version = "warmup_v1";
        public static ulong Seed(string day, string version) => Pcg32.DailySeed(day, version+"|"+Version);
        public static IReadOnlyList<PlateDefinition> Generate(string day, string version)
        {
            var rng = new Pcg32(Seed(day, version));
            int count = 4 + (int)rng.NextBounded(5);
            var sizes = Enumerable.Repeat(1, count).ToArray();
            for (int remaining = 18-count; remaining > 0; remaining--)
            {
                var legal = Enumerable.Range(0,count).Where(i=>sizes[i]<5).ToArray();
                sizes[legal[(int)rng.NextBounded((uint)legal.Length)]]++;
            }
            var kinds = Enumerable.Range(0,18).Select(i=>((char)('A'+i/6)).ToString()).ToArray();
            for(int i=kinds.Length-1;i>0;i--){int j=(int)rng.NextBounded((uint)(i+1));var tmp=kinds[i];kinds[i]=kinds[j];kinds[j]=tmp;}
            // Opening orders are A/B; the first supplied plate always has a target.
            if(kinds[0]=="C"){int j=System.Array.FindIndex(kinds,k=>k!="C");var tmp=kinds[0];kinds[0]=kinds[j];kinds[j]=tmp;}
            var plates=new List<PlateDefinition>();int offset=0;
            for(int i=0;i<count;i++){plates.Add(new PlateDefinition(i+1,kinds.Skip(offset).Take(sizes[i])));offset+=sizes[i];}
            return plates;
        }
    }
}
