using System;
using HotpotSort.Contracts;

namespace HotpotSort.Presentation
{
    [Serializable] public sealed class RevivalTransferItem
    {
        public string itemId,ingredientId;
        public int sourceSlot,targetIndex;
    }
    [Serializable] public sealed class RevivalCompletionToken
    {
        public string sessionId,transactionId,revivalOfferId,requestId,newPlateId,completionToken;
        public long sessionGeneration,eventSeq;
        public bool Matches(RevivalCompletionToken other)=>other!=null&&sessionId==other.sessionId&&sessionGeneration==other.sessionGeneration&&
            transactionId==other.transactionId&&eventSeq==other.eventSeq&&revivalOfferId==other.revivalOfferId&&requestId==other.requestId&&
            newPlateId==other.newPlateId&&completionToken==other.completionToken;
    }
    [Serializable] public sealed class RevivalTransferBatch
    {
        public string schemaVersion="revival_transfer_v1";
        public RevivalCompletionToken token;
        // Authoritative source-slot order, captured before core clears the buffer.
        public RevivalTransferItem[] items=new RevivalTransferItem[0];
    }
    public sealed class RevivalOfferView
    {
        public string sessionId,offerId;
        public long generation;
        public bool available,requestPending;
        public RewardRoute route;
        // route retains the v1 presentation selection for existing views. Actual platform
        // execution uses effectiveRoute; new presentation owners can consume the flag.
        public RewardRoute effectiveRoute;
        public bool isDevelopmentSimulation;
        public bool rewardedVideoAvailable=true;
        public ShareAvailability shareAvailability;
    }
}
