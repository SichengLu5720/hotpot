using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Session;
using HotpotSort.Profile;
using HotpotSort.Collection;
using UnityEngine;

namespace HotpotSort.Bootstrap
{
    public sealed class LocalDevelopmentServices : IProfileStore, IShareQuotaStore, IRewardService, IFriendBoard, IResultThemeShare, IAsyncProfileStore, IAppliedRewardStore, ITutorialProfileStore
    {
        [Serializable] sealed class Saved
        {
            public List<string> wins=new List<string>(),claims=new List<string>();
            public List<ShareQuotaRecord> quotas=new List<ShareQuotaRecord>();
            public bool music=true,effects=true;public float musicVolume=.6f,effectsVolume=.8f;
        }
        readonly string key;
        readonly Saved state;
        ProfileStore syncedProfile;
        public ICollectionStore Collection { get; private set; }
        public IBrothActivityService BrothActivity {get;private set;}
        public DevelopmentBrothAuthority DevelopmentBroths {get;private set;}
        void ConfigureDevelopmentBroths()
        {
            BrothActivity=null;
            if(!developmentSimulation||environment!="development"||Collection.ReadCollection().account.StartsWith("wx_",StringComparison.Ordinal))return;
            DevelopmentBroths=new DevelopmentBrothAuthority();
            BrothActivity=new BrothActivityStore(DevelopmentBroths,Collection);
            var store=Collection;var authority=DevelopmentBroths;
            store.CollectionChanged+=()=>authority.Seed(store.ReadCollection());
        }
        readonly string environment;
        bool syncing,syncAgain,accountTransportBound;
        readonly bool developmentSimulation;
        public ProfileSyncStatus LastSyncStatus { get; private set; }=ProfileSyncStatus.NotConfigured;
        public event Action ProfileChanged;
        void OnProfileChanged()=>ProfileChanged?.Invoke();
        public ProfileTimeSnapshot TimeSnapshot=>syncedProfile.TimeSnapshot;
        public ProfileDocument ReadSnapshot()=>syncedProfile.ReadSnapshot();
        public bool WarmupTutorialCompleted=>syncedProfile.WarmupTutorialCompleted;
        public bool BufferWarningCompleted=>syncedProfile.BufferWarningCompleted;
        public void CompleteTutorial(bool bufferWarning){syncedProfile.CompleteTutorial(bufferWarning);_=SyncAsync();}
        public async Task<ProfileSyncStatus> SyncAsync()
        {
            if(syncing){syncAgain=true;return ProfileSyncStatus.Busy;}
            syncing=true;
            try
            {
                // Coalesce simultaneous foreground/commit/login requests, with a finite
                // follow-up rather than an unbounded retry loop.
                for(int attempt=0;attempt<2;attempt++)
                {
                    syncAgain=false;var expected=syncedProfile;var result=await expected.SyncAsync();
                    if(ReferenceEquals(expected,syncedProfile))LastSyncStatus=result;
                    if(!syncAgain)break;
                }
                return LastSyncStatus;
            }
            catch{return LastSyncStatus=ProfileSyncStatus.Failed;}
            finally{syncing=false;}
        }
        public void InvalidateSyncCallbacks()=>syncedProfile.InvalidateSyncCallbacks();
        public bool HasAppliedReward(string requestId)=>syncedProfile.HasAppliedReward(requestId);
        public bool TryCommitAppliedReward(RewardRequest request,DateTimeOffset utc)
        {bool applied=syncedProfile.TryCommitAppliedReward(request,utc);if(applied)_=SyncAsync();return applied;}
        public Func<RewardRequest,Task<RewardOutcome>> RewardPrompt;
        public Func<string,Task<RewardOutcome>> SharePrompt;
        public string LastShareTitle {get;private set;}
        public LocalDevelopmentServices(string storageKey="HotpotSort.Task001.v3.DevelopmentProfile",string environment="development",string account="local",IProfileSyncTransport transport=null,bool isDevelopmentSimulation=true)
        {
            key=storageKey;
            this.environment=environment;
            developmentSimulation=isDevelopmentSimulation;
            if(!isDevelopmentSimulation&&account=="local")
            {
                string cached=PlayerPrefs.GetString(key+".active-account."+environment,"");
                if(System.Text.RegularExpressions.Regex.IsMatch(cached,"\\Awx_[a-f0-9]{64}\\z"))account=cached;
            }
            accountTransportBound=account!="local"&&transport!=null;
            var legacyJson=PlayerPrefs.GetString(key,"");
            state=string.IsNullOrEmpty(legacyJson)?new Saved():JsonUtility.FromJson<Saved>(legacyJson)??throw new InvalidOperationException("Unreadable legacy profile; original data preserved");
            if(state.quotas==null)state.quotas=new List<ShareQuotaRecord>();
            if(state.claims==null)state.claims=new List<string>();
            if(state.wins==null)state.wins=new List<string>();
            syncedProfile=CreateProfile(account,transport,ProfileStore.Migrate(environment,account,state.wins,state.quotas,state.claims));
            syncedProfile.ProfileChanged+=OnProfileChanged;
            Collection=CreateCollection(account);
            ((CollectionStore)Collection).ImportCompletedWinHistory(TotalFirstWins>0);
            ConfigureDevelopmentBroths();
        }
        ICollectionStore CreateCollection(string account)
        {
            string collectionKey=key+".collection-v1."+RecoverableProfileStorage.Hash(environment+"\n"+account);
            return new CollectionStore(environment,account,new CollectionPersistence(collectionKey,environment,account,k=>PlayerPrefs.GetString(k,""),
                (k,json)=>{PlayerPrefs.SetString(k,json);PlayerPrefs.Save();},d=>JsonUtility.ToJson(d),json=>JsonUtility.FromJson<CollectionDocument>(json)));
        }
        ProfileStore CreateProfile(string account,IProfileSyncTransport transport,ProfileDocument legacy=null)
        {
            string partitionKey=key+".profile-v1."+RecoverableProfileStorage.Hash(environment+"\n"+account);
            var persistence=new RecoverableProfileStorage(partitionKey,environment,account,k=>PlayerPrefs.GetString(k,""),
                (k,json)=>{PlayerPrefs.SetString(k,json);PlayerPrefs.Save();},document=>JsonUtility.ToJson(document),json=>JsonUtility.FromJson<ProfileDocument>(json));
            return new ProfileStore(environment,account,persistence,new ProfileClock(()=>DateTimeOffset.UtcNow,()=>Time.realtimeSinceStartupAsDouble),transport,legacy);
        }
        public bool TryAdoptAccountAtEntry(string account,IProfileSyncTransport transport,bool atEntry)
        {
            if(!atEntry||string.IsNullOrEmpty(account)||account=="local")return false;
            var local=syncedProfile.ReadSnapshot();
            if(local.account==account&&accountTransportBound)return true;
            if(local.account!="local"&&local.account!=account)return false; // Never relabel another account's facts.
            string claimKey=key+".local-owner."+environment;
            string owner=PlayerPrefs.GetString(claimKey,"");
            if(owner.Length!=0&&owner!=account)return false;
            // Persist ownership before import. On write failure keep the active local
            // partition unchanged; retry/restart reuses the same facts and timestamps.
            PlayerPrefs.SetString(claimKey,account);PlayerPrefs.Save();
            syncedProfile.Flush();
            var next=CreateProfile(account,transport);
            var nextCollection=CreateCollection(account);
            if(local.account=="local")((CollectionStore)nextCollection).ImportPendingWins(Collection.ReadCollection());
            var imported=ProfileStore.Copy(local);imported.account=account;
            next.ImportFacts(imported);
            PlayerPrefs.SetString(key+".active-account."+environment,account);PlayerPrefs.Save();
            syncedProfile.InvalidateSyncCallbacks();syncedProfile.ProfileChanged-=OnProfileChanged;
            syncedProfile=next;syncedProfile.ProfileChanged+=OnProfileChanged;
            Collection=nextCollection;
            ((CollectionStore)Collection).ImportCompletedWinHistory(TotalFirstWins>0);
            ConfigureDevelopmentBroths();
            accountTransportBound=true;
            LastSyncStatus=ProfileSyncStatus.NotConfigured;OnProfileChanged();
            return true;
        }
        public bool IsDevelopmentSimulation=>developmentSimulation;
        public int TotalFirstWins=>syncedProfile.ReadSnapshot().firstWinDays.Count;
        void Save(){PlayerPrefs.SetString(key,JsonUtility.ToJson(state));PlayerPrefs.Save();}
        public bool RecordFirstWin(string day){bool added=syncedProfile.RecordFirstWin(day);if(added)_=SyncAsync();return added;}
        public int SharesUsed(string day)=>syncedProfile.ReadShareAvailability(day,TimeSnapshot.Utc).Used;
        public bool TryConsumeShare(string day,string requestId)
        {
            var utc=TimeSnapshot.Utc;
            return syncedProfile.TryReserveShare(day,requestId,utc)&&TryCommitShare(day,requestId,utc);
        }
        public ShareAvailability ReadShareAvailability(string day,DateTimeOffset utc)=>syncedProfile.ReadShareAvailability(day,utc);
        public bool TryReserveShare(string day,string requestId,DateTimeOffset utc)=>syncedProfile.TryReserveShare(day,requestId,utc);
        public bool HasShareReservation(string day,string requestId)=>syncedProfile.HasShareReservation(day,requestId);
        public bool TryCommitShare(string day,string requestId,DateTimeOffset utc)
        {bool applied=syncedProfile.TryCommitShare(day,requestId,utc);if(applied)_=SyncAsync();return applied;}
        public void ReleaseShare(string day,string requestId)=>syncedProfile.ReleaseShare(day,requestId);
        public PlayerSettings LoadSettings()=>new PlayerSettings{MusicEnabled=state.music,EffectsEnabled=state.effects,MusicVolume=state.musicVolume,EffectsVolume=state.effectsVolume};
        public void SaveSettings(PlayerSettings value){state.music=value.MusicEnabled;state.effects=value.EffectsEnabled;state.musicVolume=Mathf.Clamp01(value.MusicVolume);state.effectsVolume=Mathf.Clamp01(value.EffectsVolume);Save();}
        public Task<RewardOutcome> RequestAsync(RewardRequest request)=>RewardPrompt?.Invoke(request)??Task.FromResult(RewardOutcome.Failed);
        public Task<IReadOnlyList<FriendRecord>> LoadAsync()=>Task.FromResult<IReadOnlyList<FriendRecord>>(new[]{new FriendRecord{DisplayName="我（本地开发模拟；未连接好友数据）",FirstWins=TotalFirstWins}});
        public Task<RewardOutcome> ShareThemeAsync(string resourceAddress)=>SharePrompt?.Invoke(resourceAddress)??Task.FromResult(RewardOutcome.Failed);
        public Task<RewardOutcome> ShareThemeAsync(string resourceAddress,string title)
        {LastShareTitle=title;return ShareThemeAsync(resourceAddress);}
    }
}
