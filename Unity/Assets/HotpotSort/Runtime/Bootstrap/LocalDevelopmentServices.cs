using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using UnityEngine;

namespace HotpotSort.Bootstrap
{
    public sealed class LocalDevelopmentServices : IProfileStore, IRewardService, IFriendBoard, IThemeShare
    {
        [Serializable] sealed class Quota { public string day;public int used; }
        [Serializable] sealed class Saved
        {
            public List<string> wins=new List<string>(),claims=new List<string>();
            public List<Quota> quotas=new List<Quota>();
            public bool music=true,effects=true;public float musicVolume=.6f,effectsVolume=.8f;
        }
        readonly string key;
        readonly Saved state;
        public Func<RewardRequest,Task<RewardOutcome>> RewardPrompt;
        public Func<string,Task<RewardOutcome>> SharePrompt;
        public LocalDevelopmentServices(string storageKey="HotpotSort.Task001.v3.DevelopmentProfile")
        {
            key=storageKey;
            try{state=JsonUtility.FromJson<Saved>(PlayerPrefs.GetString(key,""))??new Saved();}catch{state=new Saved();}
        }
        public bool IsDevelopmentSimulation=>true;
        public int TotalFirstWins=>state.wins.Count;
        void Save(){PlayerPrefs.SetString(key,JsonUtility.ToJson(state));PlayerPrefs.Save();}
        public bool RecordFirstWin(string day){if(state.wins.Contains(day))return false;state.wins.Add(day);Save();return true;}
        public int SharesUsed(string day)=>state.quotas.Find(q=>q.day==day)?.used??0;
        public bool TryConsumeShare(string day,string requestId)
        {
            if(state.claims.Contains(requestId) || SharesUsed(day)>=3)return false;
            var quota=state.quotas.Find(q=>q.day==day);if(quota==null){quota=new Quota{day=day};state.quotas.Add(quota);}
            quota.used++;state.claims.Add(requestId);Save();return true;
        }
        public PlayerSettings LoadSettings()=>new PlayerSettings{MusicEnabled=state.music,EffectsEnabled=state.effects,MusicVolume=state.musicVolume,EffectsVolume=state.effectsVolume};
        public void SaveSettings(PlayerSettings value){state.music=value.MusicEnabled;state.effects=value.EffectsEnabled;state.musicVolume=Mathf.Clamp01(value.MusicVolume);state.effectsVolume=Mathf.Clamp01(value.EffectsVolume);Save();}
        public Task<RewardOutcome> RequestAsync(RewardRequest request)=>RewardPrompt?.Invoke(request)??Task.FromResult(RewardOutcome.Failed);
        public Task<IReadOnlyList<FriendRecord>> LoadAsync()=>Task.FromResult<IReadOnlyList<FriendRecord>>(new[]{new FriendRecord{DisplayName="我（本地开发模拟；未连接好友数据）",FirstWins=TotalFirstWins}});
        public Task<RewardOutcome> ShareThemeAsync(string resourceAddress)=>SharePrompt?.Invoke(resourceAddress)??Task.FromResult(RewardOutcome.Failed);
    }
}
