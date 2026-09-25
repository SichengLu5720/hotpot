using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using HotpotSort.Contracts;

namespace HotpotSort.Collection
{
    // Explicit in-memory development authority. Never instantiate for a native
    // WeChat account. Multiple simulated clients share this authority to model
    // both sides of the transaction; no client-generated success reaches a server.
    public sealed class DevelopmentBrothAuthority
    {
        internal readonly object Gate=new object();
        internal readonly Dictionary<string,CollectionDocument> Profiles=new Dictionary<string,CollectionDocument>();
        internal readonly Dictionary<string,string> Invitations=new Dictionary<string,string>();
        internal readonly Dictionary<string,string> Helpers=new Dictionary<string,string>();
        internal readonly Dictionary<string,int> Daily=new Dictionary<string,int>();
        internal readonly Dictionary<string,Receipt> Receipts=new Dictionary<string,Receipt>();
        internal sealed class Receipt { public string Fingerprint,InvitationId; }
        internal readonly Func<DateTimeOffset> Clock;
        public DevelopmentBrothAuthority(Func<DateTimeOffset> clock=null){Clock=clock??(()=>DateTimeOffset.UtcNow);}
        public static string HelpDay(DateTimeOffset utc)=>utc.ToUniversalTime().AddHours(8).ToString("yyyyMMdd",CultureInfo.InvariantCulture);
        // Seeds fixtures, including future grants, without conflating ownership and
        // this activity's choice. Normal gameplay uses only IBrothActivityService.
        public void Seed(CollectionDocument document)
        {
            CollectionStore.Validate(document,document.environment,document.account);
            if(document.environment!="development"||document.account.StartsWith("wx_",StringComparison.Ordinal))throw new ArgumentException("Development identity required");
            lock(Gate)Profiles[document.account]=CollectionStore.Copy(document);
        }
    }
    public sealed class BrothActivityStore:IBrothActivityService
    {
        readonly DevelopmentBrothAuthority authority;
        readonly ICollectionStore cache;
        readonly string account;
        public bool IsDevelopmentSimulation=>true;
        public BrothActivityStore(DevelopmentBrothAuthority authority,ICollectionStore cache)
        {
            this.authority=authority??throw new ArgumentNullException(nameof(authority));this.cache=cache??throw new ArgumentNullException(nameof(cache));
            var document=cache.ReadCollection();account=document.account;
            if(document.environment!="development"||account.StartsWith("wx_",StringComparison.Ordinal))throw new ArgumentException("Development identity required");
            lock(authority.Gate)if(!authority.Profiles.ContainsKey(account))authority.Seed(document);
        }
        public Task<BrothResult> ReadAsync(string requestId)=>Run(requestId,"read",null,null);
        public Task<BrothResult> CreateInvitationAsync(string requestId,string operationId)=>Run(requestId,"invite",null,operationId);
        public Task<BrothResult> InspectInvitationAsync(string requestId,string invitationId)=>Run(requestId,"inspect",invitationId,null);
        public Task<BrothResult> ConfirmAssistAsync(string requestId,string invitationId,string operationId)=>Run(requestId,"assist",invitationId,operationId);
        public Task<BrothResult> ClaimAsync(string requestId,string brothId,string operationId)=>Run(requestId,"claim",brothId,operationId);
        public Task<BrothResult> SelectAsync(string requestId,string brothId,string operationId)=>Run(requestId,"select",brothId,operationId);
        Task<BrothResult> Run(string requestId,string action,string argument,string operationId)
        {
            lock(authority.Gate)
            {
                var result=new BrothResult{requestId=requestId,isDevelopmentSimulation=true};
                var current=authority.Profiles[account];
                Func<BrothFailure,Task<BrothResult>> finish=failure=>{
                    result.failure=failure;result.collection=CollectionStore.Copy(authority.Profiles[account]);
                    cache.ApplyAuthoritativeSnapshot(result.collection);return Task.FromResult(result);
                };
                if(string.IsNullOrWhiteSpace(requestId)||requestId.Length>160)return finish(BrothFailure.InvalidRequest);
                if(action=="read")return finish(BrothFailure.None);
                string target=null;
                if(action=="inspect"||action=="assist"){
                    if(argument==null||!authority.Invitations.TryGetValue(argument,out target))return finish(BrothFailure.NotFound);
                    string helpDay=account+"\n"+DevelopmentBrothAuthority.HelpDay(authority.Clock());
                    bool limited=authority.Daily.TryGetValue(helpDay,out var helpedToday)&&helpedToday>=3;
                    result.invitation=new BrothInvitation{invitationId=argument,isInitiator=target==account,canAssist=target!=account&&!authority.Profiles[target].brothActivityQualified&&!limited};
                    if(action=="inspect")return finish(BrothFailure.None);
                }
                if(string.IsNullOrWhiteSpace(operationId)||operationId.Length>160)return finish(BrothFailure.InvalidRequest);
                string opKey=account+"\n"+operationId,fingerprint=action+"\n"+(argument??"");
                if(authority.Receipts.TryGetValue(opKey,out var receipt)){
                    if(receipt.Fingerprint!=fingerprint)return finish(BrothFailure.OperationConflict);
                    if(receipt.InvitationId!=null)result.invitation=new BrothInvitation{invitationId=receipt.InvitationId,isInitiator=true};
                    return finish(BrothFailure.None);
                }
                var next=CollectionStore.Copy(current);CollectionDocument other=null;string dayKey=null,invitation=null;
                if(action=="invite"){
                    if(!next.entryUnlocked)return finish(BrothFailure.NotQualified);
                    if(next.brothActivityQualified)return finish(BrothFailure.ActivityComplete);
                    invitation="dev_"+Guid.NewGuid().ToString("N");
                    result.invitation=new BrothInvitation{invitationId=invitation,isInitiator=true};
                }else if(action=="assist"){
                    if(target==account)return finish(BrothFailure.SelfAssist);
                    if(authority.Helpers.TryGetValue(target,out var helper)&&helper==account)return finish(BrothFailure.AlreadyAssisted);
                    other=CollectionStore.Copy(authority.Profiles[target]);
                    if(other.brothActivityQualified)return finish(BrothFailure.ActivityComplete);
                    dayKey=account+"\n"+DevelopmentBrothAuthority.HelpDay(authority.Clock());
                    if(authority.Daily.TryGetValue(dayKey,out var count)&&count>=3)return finish(BrothFailure.DailyLimit);
                    for(int i=0;i<3;i++)if(next.tools[i]==int.MaxValue)return finish(BrothFailure.Failed);
                    for(int i=0;i<3;i++)next.tools[i]++;
                    other.brothActivityQualified=true;
                }else if(action=="claim"){
                    if(!BrothCatalog.IsCandidate(argument))return finish(BrothFailure.InvalidRequest);
                    if(!next.brothActivityQualified)return finish(BrothFailure.NotQualified);
                    if(!string.IsNullOrEmpty(next.brothActivityChoice))return finish(BrothFailure.AlreadyChosen);
                    next.brothActivityChoice=argument;if(!next.ownedBroths.Contains(argument))next.ownedBroths.Add(argument);next.currentBroth=argument;
                }else if(action=="select"){
                    if(!BrothCatalog.IsValid(argument)||!next.ownedBroths.Contains(argument))return finish(BrothFailure.NotOwned);
                    next.currentBroth=argument;
                }else return finish(BrothFailure.InvalidRequest);
                next.serverRevision=checked(next.serverRevision+1);next.revision=next.serverRevision;
                CollectionStore.Validate(next,next.environment,next.account);
                if(other!=null){other.serverRevision=checked(other.serverRevision+1);other.revision=other.serverRevision;CollectionStore.Validate(other,other.environment,other.account);}
                // Commit only after every validation succeeds, while holding Gate.
                authority.Profiles[account]=next;
                if(other!=null){authority.Profiles[target]=other;authority.Helpers[target]=account;authority.Daily[dayKey]=authority.Daily.TryGetValue(dayKey,out var count)?count+1:1;}
                if(invitation!=null)authority.Invitations[invitation]=account;
                authority.Receipts[opKey]=new DevelopmentBrothAuthority.Receipt{Fingerprint=fingerprint,InvitationId=invitation};
                return finish(BrothFailure.None);
            }
        }
    }
}
