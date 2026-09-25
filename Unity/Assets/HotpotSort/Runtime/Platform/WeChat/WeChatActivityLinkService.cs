using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
#if UNITY_WEBGL && !UNITY_EDITOR
using WeChatWASM;
#endif

namespace HotpotSort.Platform
{
    // Receiving a link only exposes pending context. ConfirmAssist is exclusively
    // an explicit user action through IBrothActivityService, never a show callback.
    public sealed class WeChatActivityLinkService:IDisposable
    {
        public const string QueryKey="brothInvitation";
        public string PendingInvitationId {get;private set;}
        public event Action PendingInvitationChanged;
        bool disposed;
        readonly WeChatRuntimeConfig config;
        readonly Action<string> shareOverride;
#if UNITY_WEBGL && !UNITY_EDITOR
        readonly Action<OnShowListenerResult> show;
#endif
        public WeChatActivityLinkService(WeChatRuntimeConfig config,Action<string> shareOverride=null)
        {
            this.config=config;this.shareOverride=shareOverride;
#if UNITY_WEBGL && !UNITY_EDITOR
            show=result=>ReceiveQuery(result.query);WX.OnShow(show);
            try{ReceiveQuery(WX.GetLaunchOptionsSync().query);}catch{}
#endif
        }
        public static bool ValidInvitation(string id)=>id!=null&&Regex.IsMatch(id,"\\A[a-f0-9]{64}\\z");
        public static string BuildQuery(string id){if(!ValidInvitation(id))throw new ArgumentException("Invalid invitation");return QueryKey+"="+id;}
        public void ReceiveQuery(IDictionary<string,string> query)
        {
            if(disposed||query==null||!query.TryGetValue(QueryKey,out var id)||!ValidInvitation(id))return;
            PendingInvitationId=id;PendingInvitationChanged?.Invoke();
        }
        public void Dismiss(string invitationId){if(PendingInvitationId!=invitationId)return;PendingInvitationId=null;PendingInvitationChanged?.Invoke();}
        // True means share UI requested, never assistance or reward success.
        public bool RequestShare(string invitationId)
        {
            if(disposed||config==null||config.ShareState!=WeChatCapabilityState.Ready||!ValidInvitation(invitationId))return false;
            try{
                string query=BuildQuery(invitationId);
                if(shareOverride!=null){shareOverride(query);return true;}
#if UNITY_WEBGL && !UNITY_EDITOR
                WX.ShareAppMessage(new ShareAppMessageOption{title=config.shareTitle,query=query});return true;
#else
                return false;
#endif
            }catch{return false;}
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;
#if UNITY_WEBGL && !UNITY_EDITOR
            WX.OffShow(show);
#endif
            PendingInvitationChanged=null;
        }
    }
}
