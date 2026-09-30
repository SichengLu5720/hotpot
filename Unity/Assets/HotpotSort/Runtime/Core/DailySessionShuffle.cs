using System;
using System.Linq;
using HotpotSort.Contracts;
using HotpotSort.Determinism;

namespace HotpotSort.Core
{
    public sealed partial class DailySession
    {
        int redealCount, redealGuaranteedPlateId, redealGuaranteedItemId;
        const ulong RedealStreamDomain = 0x52454445414CUL;
        Pcg32 redealRng;
        public int RedealGuaranteedPlateId { get { lock(gate) return redealGuaranteedPlateId; } }
        public int RedealGuaranteedItemId { get { lock(gate) return redealGuaranteedItemId; } }
        bool RedealItemMatches(CoreItem item) => (item.Location=="Pending" || item.Location=="ActiveAvailable") &&
            orders.Any(o=>o.Enabled && o.Kind==item.Kind && o.Items.Count<3);
        public bool CanRedeal { get { lock(gate) return CanUseTool(RewardKind.Shuffle) && items.Any(RedealItemMatches); } }
        object RedealStateJson() => CanonicalJson.Object("count",redealCount,"guaranteedPlateId",redealGuaranteedPlateId,
            "guaranteedItemId",redealGuaranteedItemId,"redealRng",redealRng.Snapshot());

        // Zero means presentation-driven fill-to-gate. Positive sizes preserve archived replay metadata.
        public CommandResult Redeal(ulong logicalBoundary,int firstBatchSize=0)
        {
            lock(gate)
            {
                var data=CanonicalJson.Object("logicalBoundary",CanonicalJson.U64(logicalBoundary));
                if(firstBatchSize!=12)data.Add("firstBatchSize",firstBatchSize);
                string reason=Guard(logicalBoundary);
                if(firstBatchSize<0||firstBatchSize>24)reason="InvalidBatchSize";
                if(reason==null && status!=GameStatus.Running)reason="NotRunning";
                if(reason==null && !CanUseTool(RewardKind.Shuffle))reason="ToolAlreadyUsed";
                if(reason==null && SwapOrderPending)reason="SwapOrderPending";
                var candidate=items.Where(RedealItemMatches).OrderByDescending(x=>x.Location=="ActiveAvailable")
                    .ThenByDescending(x=>x.SourceIndex).ThenBy(x=>x.Id).FirstOrDefault();
                if(reason==null && candidate==null)reason="NoMatchingIngredient";
                if(reason!=null)return Reject("RedealRejected",reason,data);
                if(Stage!=ChallengeStage.Warmup && logicalBoundary>=600000)return Timeout(logicalBoundary);
                Begin(logicalBoundary);
                if(redealRng==null)redealRng=new Pcg32(Pcg32.StreamSeed(seed ^ RedealStreamDomain,"PresentationRng"));
                var queue=items.Where(x=>x.Location=="Pending" || x.Location=="ActiveAvailable")
                    .Select(x=>x.PlateId).Distinct().OrderBy(x=>x).ToList();
                for(int i=queue.Count-1;i>0;i--)
                {
                    int j=(int)redealRng.NextBounded((uint)(i+1));
                    int tmp=queue[i];queue[i]=queue[j];queue[j]=tmp;
                }
                queue.Remove(candidate.PlateId);
                queue.Insert(0,candidate.PlateId);
                foreach(var item in items.Where(x=>x.Location=="ActiveAvailable"))item.Location="Pending";
                pending.Clear();pending.AddRange(queue);active.Clear();
                redealCount++;redealGuaranteedPlateId=candidate.PlateId;redealGuaranteedItemId=candidate.Id;
                Emit("PlatesRedealt",CanonicalJson.Object("pendingPlateIds",pending.ToArray(),"guaranteedPlateId",candidate.PlateId,
                    "guaranteedItemId",candidate.Id,"firstBatchCount",Math.Min(firstBatchSize,pending.Count)));
                MarkToolUsed(RewardKind.Shuffle);Close();Record("Redeal",data);return Publish(true,null);
            }
        }
        public CommandResult ReturnRedealPlate(int plateId,ulong logicalBoundary)
        {
            lock(gate)
            {
                var data=CanonicalJson.Object("plateId",plateId,"logicalBoundary",CanonicalJson.U64(logicalBoundary));
                string reason=Guard(logicalBoundary);
                if(reason==null && status!=GameStatus.Running)reason="NotRunning";
                if(reason==null && (redealCount==0 || plateId==redealGuaranteedPlateId || !active.Contains(plateId)))reason="InvalidRedealPlate";
                if(reason!=null)return Reject("ReturnRedealPlateRejected",reason,data);
                Begin(logicalBoundary);
                foreach(var item in items.Where(x=>x.PlateId==plateId && x.Location=="ActiveAvailable"))item.Location="Pending";
                active.Remove(plateId);pending.Add(plateId);
                Emit("RedealPlateReturned",data);Close();Record("ReturnRedealPlate",data);return Publish(true,null);
            }
        }
    }
}
