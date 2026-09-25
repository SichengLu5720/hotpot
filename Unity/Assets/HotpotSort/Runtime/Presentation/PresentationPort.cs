using System;
using System.Threading.Tasks;
using HotpotSort.Contracts;

namespace HotpotSort.Presentation
{
    // View-local transport only. K0 adapter must map these values; this is not a shared/core contract.
    public enum ViewPhase { Entry, Running, Paused, Won, Overflow, Aborted }
    public enum ViewAction { StartToday, Pause, Resume, RetrySameDay, Exit }
    [Flags] public enum ViewPauseReasons { None=0, User=1, Background=2, Reward=4, Revival=8, Tutorial=16 }
    public enum ViewTutorialStep { None, SelectFood, FoodInFlight, OrderExplanation }
    [Serializable] public sealed class ViewItem
    {
        public string itemId,layoutVersion;
        public int foodId,sourceIndex,drawOrder;
        public float x,y,radius=18,rotationDegrees;
        // Bottom-left texture UV rectangle, identical for rendering and alpha sampling.
        public float uvX,uvY,uvWidth=1,uvHeight=1;
    }
    [Serializable] public sealed class ViewPlateMotion
    {
        public bool animateEntry = true, hasSpawnPosition;
        public float spawnX, spawnY;
        public long correctionRevision;
    }
    [Serializable] public sealed class ViewPlate { public string plateId; public float x, y, radius = 48; public ViewItem[] items = new ViewItem[0]; public ViewPlateMotion motion; public int initialItemCount; public string layoutVersion; public float envelopeRatio; public bool layoutUsesProxyContour; }
    [Serializable] public sealed class ViewOrder { public int slot, foodId, count, required; public bool enabled; }
    [Serializable] public sealed class ViewFact { public string label, value; }
    [Serializable] public sealed class ViewSnapshot
    {
        public string sessionId;
        public long sessionGeneration;
        public ViewPauseReasons pauseReasons;
        public bool revivalPending,revivalUsed;
        public string revivalOfferId;
        public RevivalTransferBatch revivalTransfer;
        public long revision, eventSeq;
        public int completedOrders,totalOrders;
        public bool isWarmup,warmupComplete,bufferWarning;
        public ViewTutorialStep tutorialStep;
        public string tutorialItemId;
        public int tutorialOrderSlot=-1,thirdPotThreshold=31,fourthPotThreshold=49;
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
        public long sessionGeneration;
        public RevivalTransferBatch revivalTransfer;
        public string sessionId,transactionId,itemId,ingredientId,plateId,kind,sourceContainer,targetContainer;
        public int sourceSlot=-1,targetSlot=-1,slot=-1,filledBefore=-1,filledAfter=-1,orderIdentity=-1;
    }
    public sealed class ViewUpdate { public ViewSnapshot snapshot; public ViewEvent[] events = new ViewEvent[0]; }
    public struct ViewTap { public string itemId; public long inputSeq, snapshotRevision; public float boardX, boardY; }
    public struct ViewSupplyObservation
    {
        public const int CurrentVersion=1, EntryLimit=3;
        public int version,entryCount;
        public int entryLimit => EntryLimit;
        public float gateY => 140;
        public bool canSupply => entryCount>=0 && entryCount<EntryLimit;
        public long sessionGeneration,observationSequence;
        // Compatibility aliases share the authoritative count; no second eligibility flag.
        public bool heightGateClear { get=>canSupply; set=>entryCount=value?0:EntryLimit; }
        public bool spaceAvailable { get=>canSupply; set=>heightGateClear=value; }
        public float fixedGate => gateY;
        public float x, y, radius, minimumCenterY;
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
    // Optional capability; existing IPresentationPort implementations remain compatible.
    public interface IRevivalPresentationPort
    {
        RevivalOfferView ReadRevivalOffer();
        Task<RewardApplicationResult> RequestRevivalAsync();
        void DeclineRevival();
        bool CompleteRevivalTransfer(RevivalCompletionToken token);
    }
    // All callbacks are scoped to the rendered session; duplicate/stale callbacks are ignored.
    public interface IWarmupPresentationPort
    {
        bool SelectTutorialFood(string sessionId,long generation,string itemId);
        bool TutorialFoodArrived(string sessionId,long generation,string itemId);
        bool CompleteOpeningTutorial(string sessionId,long generation);
        bool CompleteBufferWarning(string sessionId,long generation);
        // Invoke only after final order animations and the 0.8s message have ended,
        // and after presentation has cleared old objects/animations/input state.
        bool CompleteWarmup(string sessionId,long generation);
    }
}
