using System;
using System.Linq;
using HotpotSort.Contracts;
using HotpotSort.Determinism;
namespace HotpotSort.Core
{
    public sealed partial class DailySession
    {
        string swapToken,swapKind,swapOldKind;
        int swapSlot;
        SwapOrderItem[] swapItems;
        public bool SwapOrderPending=>!string.IsNullOrEmpty(swapToken);
        void CancelSwapForTerminal()
        {
            if(!SwapOrderPending)return;
            Emit("SwapOrderCancelled",CanonicalJson.Object("token",swapToken,"slotId",swapSlot,"reason","Terminal"));
            swapToken=null;swapKind=null;swapOldKind=null;swapItems=null;
        }
        public SwapOrderTransfer ReadSwapOrderTransfer()
        {
            lock(gate)return !SwapOrderPending?null:new SwapOrderTransfer{sessionId=sessionId,token=swapToken,slot=swapSlot,oldIngredientId=(string)mapping[swapOldKind],newIngredientId=(string)mapping[swapKind],items=swapItems.Select(i=>new SwapOrderItem{itemId=i.itemId,ingredientId=i.ingredientId,sourceIndex=i.sourceIndex,bufferIndex=i.bufferIndex}).ToArray()};
        }
        object SwapTransferJson()=>CanonicalJson.Object("token",swapToken,"slotId",swapSlot,"oldKind",swapOldKind,"newKind",swapKind,"items",swapItems.Select(i=>CanonicalJson.Object("itemId",int.Parse(i.itemId),"ingredientId",i.ingredientId,"sourceIndex",i.sourceIndex,"bufferIndex",i.bufferIndex)).ToArray());
        bool SwapSlotValid(int slot)=>!SwapOrderPending&&status==GameStatus.Running&&slot>=0&&slot<orders.Length&&orders[slot].Enabled&&orders[slot].Kind!=null&&orders[slot].Items.Count<3&&buffer.Count(i=>!i.HasValue)>=orders[slot].Items.Count;
        DailyDirector.Decision SwapChoice(int slot,bool select)=>director.Choose(slot,items,orders,buffer,pending,directorRng,select,commandClickability,commandUnknownClickability,CumulativeCompletedOrders,6,true,commandNextLayer,NormalPotCount,orders[slot].Kind);
        public int[] GetSwapOrderTargets(ClickableObservation observation)
        {
            lock(gate)
            {
                if(disposed||resolving||SwapOrderPending||status!=GameStatus.Running)return Array.Empty<int>();
                commandClickability=ValidateClickability(observation,CanonicalJson.Object());
                // Fresh, certified geometry is required; stale/missing observations never authorize a swap.
                if(commandClickability==null||observation.PolicyVersion!=6)return Array.Empty<int>();
                return Enumerable.Range(0,orders.Length).Where(slot=>SwapSlotValid(slot)&&SwapChoice(slot,false).HasCandidates).ToArray();
            }
        }
        public int OrderIdentity(int slot){lock(gate)return slot>=0&&slot<orders.Length?orders[slot].Identity:-1;}
        public CommandResult BeginSwapOrder(int slot,int expectedOrderIdentity,ulong logicalBoundary,ClickableObservation observation,Func<bool> consume=null)
        {
            lock(gate)
            {
                if(Guard(logicalBoundary)!=null||!SwapSlotValid(slot)||orders[slot].Identity!=expectedOrderIdentity)return Reject("SwapOrderRejected","StaleTarget",null);
                if(Stage!=ChallengeStage.Warmup&&logicalBoundary>=600000)return Timeout(logicalBoundary);
                var detail=CanonicalJson.Object("slotId",slot,"orderIdentity",expectedOrderIdentity,"logicalBoundary",CanonicalJson.U64(logicalBoundary));
                commandClickability=ValidateClickability(observation,detail);
                if(commandClickability==null||observation.PolicyVersion!=6||!SwapChoice(slot,false).HasCandidates)return Reject("SwapOrderRejected","NoTarget",null);
                // No state/RNG change before the synchronous charge succeeds.
                if(consume!=null&&!consume())return Reject("SwapOrderRejected","NotConsumed",null);
                Begin(logicalBoundary);swapSlot=slot;swapOldKind=orders[slot].Kind;
                var returned=orders[slot].Items.ToArray();
                swapItems=new SwapOrderItem[returned.Length];
                for(int index=0;index<returned.Length;index++)
                {
                    int id=returned[index],free=Array.FindIndex(buffer,x=>!x.HasValue);var item=items[id-1];
                    item.Location="Buffer";item.Slot=free;buffer[free]=id;
                    swapItems[index]=new SwapOrderItem{itemId=id.ToString(),ingredientId=(string)mapping[item.Kind],sourceIndex=index,bufferIndex=free};
                }
                orders[slot].Items.Clear();maxBuffer=Math.Max(maxBuffer,buffer.Count(i=>i.HasValue));
                var choice=SwapChoice(slot,true);swapKind=choice.Kind;
                Emit("DirectorEvaluated",choice.Diagnostic);
                orders[slot].Kind=null;
                swapToken="swap-"+CanonicalJson.U64(transactionId);
                Emit("SwapOrderTransferStarted",SwapTransferJson());
                Close();Record("BeginSwapOrder",detail);return Publish(true,null);
            }
        }
        public CommandResult CompleteSwapOrder(string token,ulong logicalBoundary,ClickableObservation observation=null)
        {
            lock(gate)
            {
                if(Guard(logicalBoundary)!=null||status!=GameStatus.Running||!SwapOrderPending||token!=swapToken)return Reject("SwapOrderCompletionRejected","StaleCompletion",null);
                if(Stage!=ChallengeStage.Warmup&&logicalBoundary>=600000)return Timeout(logicalBoundary);
                var detail=CanonicalJson.Object("token",token,"logicalBoundary",CanonicalJson.U64(logicalBoundary));
                commandClickability=ValidateClickability(observation,detail);
                Begin(logicalBoundary);int slot=swapSlot;var order=orders[slot];order.Kind=swapKind;order.Identity=++nextOrderIdentity;
                swapToken=null;swapKind=null;swapOldKind=null;swapItems=null;
                Emit("SwapOrderCompleted",CanonicalJson.Object("token",token,"slotId",slot,"kind",order.Kind));
                Emit("OrderCreated",CanonicalJson.Object("slotId",slot,"kind",order.Kind,"filledBefore",0,"filledAfter",0,"selectionReason","SwapOrder"));
                AbsorbBuffer(slot);Settle();Close();Record("CompleteSwapOrder",detail);return Publish(true,null);
            }
        }
    }
}
