using System;
using System.Linq;
using HotpotSort.Determinism;
namespace HotpotSort.Core
{
    // Exact two approved profiles. Reconstructed historical identity must match its
    // archived canonical digest before it can execute any replay command.
    internal static class KnownDailyProfiles
    {
        internal static readonly int[][] Difficulty1={new[]{0,70,30,0,0},new[]{40,50,10,0,0},new[]{60,30,10,0,0},new[]{80,20,0,0,0},new[]{90,10,0,0,0},new[]{0,30,70,0,0},new[]{20,30,50,0,0},new[]{30,40,20,10,0},new[]{35,30,0,35,0},new[]{65,35,0,0,0},new[]{0,20,80,0,0},new[]{10,20,70,0,0},new[]{20,30,50,0,0},new[]{30,30,0,35,5},new[]{40,50,0,0,10},new[]{0,20,80,0,0},new[]{10,20,70,0,0},new[]{20,20,60,0,0},new[]{25,30,0,35,10},new[]{35,50,0,0,15}};
        internal static readonly int[][] Difficulty3={new[]{0,70,30,0,0},new[]{40,50,10,0,0},new[]{60,30,10,0,0},new[]{80,20,0,0,0},new[]{90,10,0,0,0},new[]{0,30,70,0,0},new[]{20,30,50,0,0},new[]{30,40,20,10,0},new[]{30,30,0,35,5},new[]{55,35,0,0,10},new[]{0,20,80,0,0},new[]{10,20,70,0,0},new[]{20,30,50,0,0},new[]{20,30,0,35,15},new[]{30,40,0,0,30},new[]{0,20,80,0,0},new[]{10,20,70,0,0},new[]{20,20,60,0,0},new[]{25,30,0,35,10},new[]{35,50,0,0,15}};
        internal static DailyContent Resolve(DailyContent source,string digest)
        {
            if(source.Digest==digest)return source;
            if(source.Digest!=DailyContent.ProductionDigest&&source.Digest!=DailyContent.LegacyProductionDigest)throw new ArgumentException("Unrecognized source profile");
            bool current=digest==DailyContent.ProductionDigest;
            if(!current&&digest!=DailyContent.LegacyProductionDigest)throw new ArgumentException("Unknown replay content digest");
            var rows=current?Difficulty1:Difficulty3;
            var content=new DailyContent(current?DailyContent.CurrentVersion:DailyContent.LegacyVersion,source.Plates,rows.Select((w,i)=>new WeightRow(i/5,i%5,w)));
            if(content.Digest!=digest)throw new ArgumentException("Historical content reconstruction mismatch");
            return content;
        }
    }
}
