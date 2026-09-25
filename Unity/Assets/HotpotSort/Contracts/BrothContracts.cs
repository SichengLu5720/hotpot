using System;
using System.Threading.Tasks;

namespace HotpotSort.Contracts
{
    public static class BrothCatalog
    {
        public const string Red="red",Clear="clear",Tomato="tomato",Mushroom="mushroom";
        public static bool IsCandidate(string id)=>id==Clear||id==Tomato||id==Mushroom;
        public static bool IsValid(string id)=>id==Red||IsCandidate(id);
        public static bool HasClaimReminder(CollectionDocument document)=>document!=null&&document.brothActivityQualified&&string.IsNullOrEmpty(document.brothActivityChoice);
    }
    public enum BrothFailure { None,Unavailable,Offline,Unauthenticated,InvalidRequest,NotFound,SelfAssist,AlreadyAssisted,ActivityComplete,DailyLimit,NotQualified,AlreadyChosen,NotOwned,OperationConflict,Stale,Failed }
    [Serializable] public sealed class BrothInvitation
    {
        // Opaque server-generated link token; never a caller-supplied account identity.
        public string invitationId;
        public bool isInitiator,canAssist;
    }
    public sealed class BrothResult
    {
        public string requestId;
        public bool isDevelopmentSimulation;
        public BrothFailure failure;
        public CollectionDocument collection;
        public BrothInvitation invitation;
        public bool Succeeded=>failure==BrothFailure.None&&collection!=null;
    }
    public interface IBrothActivityService
    {
        bool IsDevelopmentSimulation { get; }
        // requestId identifies this callback; operationId identifies a mutation and
        // MUST be reused on retry. Different arguments with the same operation fail.
        // Native adapters authenticate actors, validate request/account/environment,
        // and apply only newer serverRevision snapshots. Sharing itself never assists.
        Task<BrothResult> ReadAsync(string requestId);
        Task<BrothResult> CreateInvitationAsync(string requestId,string operationId);
        Task<BrothResult> InspectInvitationAsync(string requestId,string invitationId);
        Task<BrothResult> ConfirmAssistAsync(string requestId,string invitationId,string operationId);
        Task<BrothResult> ClaimAsync(string requestId,string brothId,string operationId);
        Task<BrothResult> SelectAsync(string requestId,string brothId,string operationId);
    }
}
