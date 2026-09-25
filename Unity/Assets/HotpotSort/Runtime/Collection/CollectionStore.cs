using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotpotSort.Contracts;

namespace HotpotSort.Collection
{
    // Separate from the deterministic challenge stream and from additive legacy profile facts.
    public sealed class CollectionRandom:ICollectionRandom
    {
        readonly Random random=new Random(Guid.NewGuid().GetHashCode());
        public int Next(int exclusiveMaximum)=>random.Next(exclusiveMaximum);
    }
    public sealed class CollectionStore:ICollectionStore
    {
        readonly object serial=new object();
        readonly ICollectionPersistence persistence;
        readonly ICollectionRandom random;
        CollectionDocument state;
        public event Action CollectionChanged;
        public CollectionStore(string environment,string account,ICollectionPersistence persistence,ICollectionRandom random=null)
        {
            this.persistence=persistence??throw new ArgumentNullException(nameof(persistence));this.random=random??new CollectionRandom();
            state=persistence.Load()??NewDocument(environment,account);Validate(state,environment,account);state=Copy(state);
        }
        public static CollectionDocument NewDocument(string environment,string account)
        {var d=new CollectionDocument{environment=environment,account=account};for(int i=0;i<16;i++){d.unlocked.Add(CollectionCatalog.Id(i));d.selected.Add(CollectionCatalog.Id(i));}return d;}
        public static string RewardDay(DateTimeOffset utc)=>utc.ToUniversalTime().AddHours(2).ToString("yyyyMMdd",CultureInfo.InvariantCulture);
        public CollectionDocument ReadCollection(){lock(serial)return Copy(state);}
        public List<string> BeginSelectionDraft(){lock(serial)return new List<string>(state.selected);}
        public void ImportCompletedWinHistory(bool hasCompletedWin)
        {if(!hasCompletedWin)return;lock(serial){if(state.entryUnlocked)return;var next=Copy(state);next.entryUnlocked=true;Commit(next);}Notify();}
        public bool TrySaveSelection(IEnumerable<string> ingredientIds)
        {
            if(ingredientIds==null)return false;var ids=ingredientIds.Distinct(StringComparer.Ordinal).OrderBy(x=>x,StringComparer.Ordinal).ToList();
            lock(serial){if(ids.Count<16||ids.Any(x=>!state.unlocked.Contains(x)))return false;var next=Copy(state);next.selected=ids;Commit(next);}Notify();return true;
        }
        public string[] CreateSessionSelection()
        {
            lock(serial){var pool=state.selected.ToArray();if(pool.Length>16)for(int i=0;i<16;i++){int j=i+random.Next(pool.Length-i);var t=pool[i];pool[i]=pool[j];pool[j]=t;}return pool.Take(16).OrderBy(x=>x,StringComparer.Ordinal).ToArray();}
        }
        public CollectionReward RecordWin(string sessionId,DateTimeOffset utc,bool grantLocally)
        {
            if(string.IsNullOrEmpty(sessionId))throw new ArgumentException("Session identity required");CollectionReward result=null;
            lock(serial)
            {
                string day=RewardDay(utc);var next=Copy(state);next.entryUnlocked=true;
                if(next.rewards.Any(x=>x.day==day||x.sessionId==sessionId)||next.pendingWins.Any(x=>x.day==day||x.sessionId==sessionId)){if(!state.entryUnlocked)Commit(next);return null;}
                next.pendingWins.Add(new CollectionWin{day=day,sessionId=sessionId,utc=utc.ToUniversalTime().ToString("o",CultureInfo.InvariantCulture)});
                if(grantLocally)
                {
                    result=new CollectionReward{day=day,sessionId=sessionId};
                    if(next.unlocked.Count==32){for(int i=0;i<2;i++){int tool=random.Next(3);next.tools[tool]=checked(next.tools[tool]+1);result.tools.Add(tool);}}
                    else{result.ingredientId=CollectionCatalog.Id(random.Next(32));result.firstUnlock=!next.unlocked.Contains(result.ingredientId);if(result.firstUnlock)next.unlocked.Add(result.ingredientId);else{int i=CollectionCatalog.Index(result.ingredientId);next.duplicates[i]=checked(next.duplicates[i]+1);}}
                    next.rewards.Add(result);next.pendingWins.RemoveAll(x=>x.day==day);
                }
                Commit(next);
            }
            Notify();return result==null?null:Clone(result);
        }
        public bool TryConsumeTool(RewardKind kind,string operationId)
        {
            int i=(int)kind;if(i<0||i>2||string.IsNullOrEmpty(operationId))return false;
            lock(serial){if(state.tools[i]==0||state.appliedOperations.Contains(operationId))return false;var next=Copy(state);next.tools[i]--;next.appliedOperations.Add(operationId);Commit(next);}Notify();return true;
        }
        // Local simulation / server-confirmed transaction adapter only. Never grants a gift.
        public bool TryExchangeDuplicate(string operationId,string offeredId,string receivedId)
        {
            int a=CollectionCatalog.Index(offeredId),b=CollectionCatalog.Index(receivedId);if(a<0||b<0||a==b||string.IsNullOrEmpty(operationId))return false;
            lock(serial){if(state.appliedOperations.Contains(operationId)||state.duplicates[a]<1)return false;var next=Copy(state);next.duplicates[a]--;if(next.unlocked.Contains(receivedId))next.duplicates[b]=checked(next.duplicates[b]+1);else next.unlocked.Add(receivedId);next.appliedOperations.Add(operationId);Commit(next);}Notify();return true;
        }
        public void ApplyAuthoritativeSnapshot(CollectionDocument snapshot)
        {
            lock(serial)
            {
                Validate(snapshot,state.environment,state.account);if(snapshot.serverRevision<=state.serverRevision)return;
                var next=Copy(snapshot);next.entryUnlocked|=state.entryUnlocked;
                // Ownership and the one-time claim are permanent, unlike selection.
                if(!string.IsNullOrEmpty(state.brothActivityChoice)&&!string.IsNullOrEmpty(next.brothActivityChoice)&&state.brothActivityChoice!=next.brothActivityChoice)throw new ArgumentException("Conflicting permanent broth choice");
                foreach(var id in state.ownedBroths)if(!next.ownedBroths.Contains(id))next.ownedBroths.Add(id);
                next.brothActivityQualified|=state.brothActivityQualified;
                if(string.IsNullOrEmpty(next.brothActivityChoice))next.brothActivityChoice=state.brothActivityChoice;
                // Pending offline wins survive responses until the server includes their receipt.
                foreach(var w in state.pendingWins)if(!next.rewards.Any(x=>x.day==w.day)&&!next.pendingWins.Any(x=>x.day==w.day))next.pendingWins.Add(Clone(w));
                foreach(var id in state.unlocked)if(!next.unlocked.Contains(id))next.unlocked.Add(id);
                Commit(next);
            }Notify();
        }
        void Commit(CollectionDocument next){next.revision=state.revision+1;Validate(next,state.environment,state.account);persistence.Save(Copy(next));state=next;}
        void Notify(){var handlers=CollectionChanged;if(handlers!=null)foreach(Action h in handlers.GetInvocationList())try{h();}catch{}}
        static CollectionWin Clone(CollectionWin x)=>new CollectionWin{day=x.day,sessionId=x.sessionId,utc=x.utc};
        static CollectionReward Clone(CollectionReward x)=>new CollectionReward{day=x.day,ingredientId=x.ingredientId,sessionId=x.sessionId,firstUnlock=x.firstUnlock,tools=new List<int>(x.tools)};
        // Only offline obligations transfer into the authenticated partition. Inventory
        // itself must come from the authority, never be summed across cached profiles.
        public void ImportPendingWins(CollectionDocument source)
        {
            Validate(source,state.environment,source.account);
            lock(serial){var next=Copy(state);next.entryUnlocked|=source.entryUnlocked;foreach(var w in source.pendingWins)if(!next.rewards.Any(x=>x.day==w.day)&&!next.pendingWins.Any(x=>x.day==w.day))next.pendingWins.Add(Clone(w));Commit(next);}Notify();
        }
        public static CollectionDocument Copy(CollectionDocument d)=>new CollectionDocument{schemaVersion=d.schemaVersion,environment=d.environment,account=d.account,revision=d.revision,serverRevision=d.serverRevision,entryUnlocked=d.entryUnlocked,ownedBroths=d.ownedBroths==null?new List<string>{BrothCatalog.Red}:new List<string>(d.ownedBroths),currentBroth=string.IsNullOrEmpty(d.currentBroth)?BrothCatalog.Red:d.currentBroth,brothActivityQualified=d.brothActivityQualified,brothActivityChoice=d.brothActivityChoice,brothAssistantAccount=d.brothAssistantAccount,brothAssistStartedAt=d.brothAssistStartedAt,brothAssistExpiresAt=d.brothAssistExpiresAt,brothHelping=(d.brothHelping??new List<BrothHelping>()).Select(x=>new BrothHelping{invitationId=x.invitationId,expiresAt=x.expiresAt}).ToList(),unlocked=new List<string>(d.unlocked),selected=new List<string>(d.selected),appliedOperations=new List<string>(d.appliedOperations),duplicates=(int[])d.duplicates.Clone(),tools=(int[])d.tools.Clone(),pendingWins=d.pendingWins.Select(Clone).ToList(),rewards=d.rewards.Select(Clone).ToList()};
        public static void Validate(CollectionDocument d,string environment,string account)
        {
            if(d==null||d.schemaVersion!=1||d.environment!=environment||d.account!=account||string.IsNullOrEmpty(environment)||string.IsNullOrEmpty(account)||d.revision<0||d.serverRevision<0||d.unlocked==null||d.selected==null||d.duplicates==null||d.tools==null||d.pendingWins==null||d.rewards==null||d.appliedOperations==null)throw new ArgumentException("Invalid collection document");
            var owned=d.ownedBroths??new List<string>{BrothCatalog.Red};var current=string.IsNullOrEmpty(d.currentBroth)?BrothCatalog.Red:d.currentBroth;
            if(d.brothAssistStartedAt<0||d.brothAssistExpiresAt<0||(!string.IsNullOrEmpty(d.brothAssistantAccount)&&d.brothAssistExpiresAt<=d.brothAssistStartedAt)||(d.brothHelping!=null&&d.brothHelping.Any(x=>x==null||string.IsNullOrEmpty(x.invitationId)||x.expiresAt<=0)))throw new ArgumentException("Invalid broth help state");
            if(!owned.Contains(BrothCatalog.Red)||owned.Any(x=>!BrothCatalog.IsValid(x))||owned.Distinct().Count()!=owned.Count||!owned.Contains(current)||(!string.IsNullOrEmpty(d.brothActivityChoice)&&(!d.brothActivityQualified||!BrothCatalog.IsCandidate(d.brothActivityChoice)||!owned.Contains(d.brothActivityChoice))))throw new ArgumentException("Invalid broth collection");
            if(d.unlocked.Any(x=>CollectionCatalog.Index(x)<0)||d.unlocked.Distinct().Count()!=d.unlocked.Count||Enumerable.Range(0,16).Any(i=>!d.unlocked.Contains(CollectionCatalog.Id(i)))||d.selected.Count<16||d.selected.Distinct().Count()!=d.selected.Count||d.selected.Any(x=>!d.unlocked.Contains(x))||d.duplicates.Length!=32||d.tools.Length!=3||d.duplicates.Any(x=>x<0)||d.tools.Any(x=>x<0)||Enumerable.Range(0,32).Any(i=>d.duplicates[i]>0&&!d.unlocked.Contains(CollectionCatalog.Id(i))))throw new ArgumentException("Invalid collection inventory or selection");
            if(d.pendingWins.Any(x=>x==null||string.IsNullOrEmpty(x.sessionId)||!ValidDay(x.day)||!DateTimeOffset.TryParse(x.utc,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out var timestamp)||RewardDay(timestamp)!=x.day)||d.pendingWins.Select(x=>x.day).Distinct().Count()!=d.pendingWins.Count||d.rewards.Any(x=>x==null||!ValidDay(x.day)||x.tools==null||x.tools.Any(t=>t<0||t>2)||(string.IsNullOrEmpty(x.ingredientId)?x.tools.Count!=2:CollectionCatalog.Index(x.ingredientId)<0||x.tools.Count!=0))||d.rewards.Select(x=>x.day).Distinct().Count()!=d.rewards.Count||d.appliedOperations.Any(string.IsNullOrEmpty)||d.appliedOperations.Distinct().Count()!=d.appliedOperations.Count)throw new ArgumentException("Invalid collection receipts");
        }
        static bool ValidDay(string day)=>DateTime.TryParseExact(day,"yyyyMMdd",CultureInfo.InvariantCulture,DateTimeStyles.None,out _);
    }
}
