using System;

namespace HotpotSort.Presentation
{
    // View-local transport only. K0 adapter must map these values; this is not a shared/core contract.
    public enum ViewPhase { Entry, Running, Paused, Won, Overflow, Aborted }
    public enum ViewAction { StartToday, Pause, Resume, RetrySameDay, Exit }
    [Serializable] public sealed class ViewItem { public string itemId; public int foodId; public float x, y, radius = 18; }
    [Serializable] public sealed class ViewPlateMotion
    {
        public bool animateEntry = true, hasSpawnPosition;
        public float spawnX, spawnY;
        public long correctionRevision;
    }
    [Serializable] public sealed class ViewPlate { public string plateId; public float x, y, radius = 48; public ViewItem[] items = new ViewItem[0]; public ViewPlateMotion motion; }
    [Serializable] public sealed class ViewOrder { public int slot, foodId, count, required; public bool enabled; }
    [Serializable] public sealed class ViewFact { public string label, value; }
    [Serializable] public sealed class ViewSnapshot
    {
        public string sessionId;
        public long revision, eventSeq;
        public ViewPhase phase;
        public ViewOrder[] orders = new ViewOrder[0];
        public ViewItem[] buffer = new ViewItem[5];
        public ViewPlate[] plates = new ViewPlate[0];
        public ViewFact[] facts = new ViewFact[0];
        public string message;
    }
    public sealed class ViewEvent
    {
        public long sequence;
        public string sessionId,transactionId,itemId,ingredientId,plateId,kind,sourceContainer,targetContainer;
        public int sourceSlot=-1,targetSlot=-1,slot=-1,filledBefore=-1,filledAfter=-1,orderIdentity=-1;
    }
    public sealed class ViewUpdate { public ViewSnapshot snapshot; public ViewEvent[] events = new ViewEvent[0]; }
    public struct ViewTap { public string itemId; public long inputSeq, snapshotRevision; public float boardX, boardY; }
    public struct ViewSupplyObservation
    {
        public bool spaceAvailable, heightGateClear;
        public float x, y, radius, fixedGate, minimumCenterY;
        public string blockingPlateId;
        public long snapshotRevision;
    }
    public interface IPresentationPort
    {
        event Action<ViewUpdate> Updated;
        ViewSnapshot Read();
        void SessionAction(ViewAction action);
        void Tap(ViewTap command);
        // Core owns cooldown, pending queue, IDs and commit. Observation never allocates a plate.
        void ObserveSupply(ViewSupplyObservation observation);
    }
}
