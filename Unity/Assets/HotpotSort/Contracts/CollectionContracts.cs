using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotpotSort.Contracts
{
    public static class CollectionCatalog
    {
        public const int Count=32, SessionCount=16;
        public const string TooFewMessage="食材不够吃啦～请再多选点";
        static readonly string[] names={"肥牛卷","羊肉卷","午餐肉","毛肚","虾","鱼片","鱿鱼","蟹柳","鱼丸","豆腐","鸭血","鹌鹑蛋","玉米段","土豆片","金针菇","青菜","虾滑","牛肉丸","牛肉滑","鸭肠","黄喉","响铃卷","小郡肝","藕片","娃娃菜","豆皮","贡菜","川粉","冻豆腐","海带结","香菇","鲜竹笋"};
        public static string Id(int index){if(index<0||index>=Count)throw new ArgumentOutOfRangeException(nameof(index));return "food_"+index.ToString("00");}
        public static int Index(string id){for(int i=0;i<Count;i++)if(Id(i)==id)return i;return -1;}
        public static string Name(string id){int i=Index(id);return i<0?"":names[i];}
    }
    [Serializable] public sealed class CollectionWin { public string day,sessionId,utc; }
    [Serializable] public sealed class CollectionReward
    {
        public string day,ingredientId,sessionId;
        public bool firstUnlock;
        public List<int> tools=new List<int>();
    }
    [Serializable] public sealed class CollectionDocument
    {
        public int schemaVersion=1;
        public string environment,account;
        public long revision,serverRevision;
        public bool entryUnlocked;
        public List<string> ownedBroths=new List<string>{BrothCatalog.Red};
        public string currentBroth=BrothCatalog.Red,brothActivityChoice;
        public bool brothActivityQualified;
        public List<string> unlocked=new List<string>(),selected=new List<string>(),appliedOperations=new List<string>();
        public int[] duplicates=new int[32],tools=new int[3];
        public List<CollectionWin> pendingWins=new List<CollectionWin>();
        public List<CollectionReward> rewards=new List<CollectionReward>();
    }
    public interface ICollectionPersistence { CollectionDocument Load(); void Save(CollectionDocument document); }
    public interface ICollectionRandom { int Next(int exclusiveMaximum); }
    public interface ICollectionStore
    {
        event Action CollectionChanged;
        CollectionDocument ReadCollection();
        List<string> BeginSelectionDraft();
        bool TrySaveSelection(IEnumerable<string> ingredientIds);
        string[] CreateSessionSelection();
        CollectionReward RecordWin(string sessionId,DateTimeOffset utc,bool grantLocally);
        bool TryConsumeTool(RewardKind kind,string operationId);
        bool TryExchangeDuplicate(string operationId,string offeredId,string receivedId);
        void ApplyAuthoritativeSnapshot(CollectionDocument snapshot);
    }
    public enum IngredientTradeStatus { Pending, Accepted, Rejected, Cancelled, Expired }
    public enum IngredientTradeFailure
    {
        None, NotConfigured, Offline, Unauthenticated, InvalidRequest, NotFound,
        Forbidden, InsufficientDuplicates, AlreadyResolved, Expired,
        OperationConflict, Stale, Unavailable, Failed
    }
    [Serializable] public sealed class IngredientTradeRequest
    {
        // Opaque request identity; displayName is display-only, never an account/open ID.
        public string requestId,initiatorDisplayName,offeredId,receivedId;
        // ISO-8601 UTC (round-trip "o") timestamps supplied by the authority.
        public string createdUtc,expiresUtc;
        public IngredientTradeStatus status;
        // Capabilities computed by the authenticated service, not inferred from a name.
        public bool isInitiator,canAccept,canReject,canCancel;
    }
    public sealed class IngredientTradeResult
    {
        // Success always includes the authenticated caller's authoritative collection.
        // Failure is explicit; collection/request may additionally contain fresh state.
        public IngredientTradeFailure failure;
        public CollectionDocument collection;
        public IngredientTradeRequest request;
        public List<IngredientTradeRequest> requests=new List<IngredientTradeRequest>();
        public bool Succeeded=>failure==IngredientTradeFailure.None&&collection!=null;
    }
    public interface IIngredientTradeService
    {
        // Reuse operationId on retries. Same identity with different arguments must fail
        // with OperationConflict. Actors are resolved by authentication, never by the UI.
        Task<IngredientTradeResult> CreateAsync(string offeredId,string receivedId,string operationId);
        Task<IngredientTradeResult> AcceptAsync(string requestId,string operationId);
        Task<IngredientTradeResult> RejectAsync(string requestId,string operationId);
        Task<IngredientTradeResult> CancelAsync(string requestId,string operationId);
        Task<IngredientTradeResult> ListAsync();
        Task<IngredientTradeResult> RefreshAsync(string requestId);
    }
}
