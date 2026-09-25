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
        public const string TradeQueryKey="ingredientTrade";
        public string PendingTradeId {get;private set;}
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
            if(disposed||query==null)return;
            bool changed=false;
            if(query.TryGetValue(TradeQueryKey,out var trade)&&ValidInvitation(trade)){PendingTradeId=trade;changed=true;}
            if(query.TryGetValue(QueryKey,out var id)&&ValidInvitation(id)){PendingInvitationId=id;changed=true;}
            if(changed)PendingInvitationChanged?.Invoke();
        }
        public void DismissTrade(string id){if(PendingTradeId!=id)return;PendingTradeId=null;PendingInvitationChanged?.Invoke();}
        public void Dismiss(string invitationId){if(PendingInvitationId!=invitationId)return;PendingInvitationId=null;PendingInvitationChanged?.Invoke();}
        // True means share UI requested, never assistance or reward success.
        public bool RequestShare(string invitationId)
            =>RequestShareQuery(invitationId,QueryKey);
        public bool RequestTradeShare(string tradeId)=>RequestShareQuery(tradeId,TradeQueryKey);
        bool RequestShareQuery(string invitationId,string key)
        {
            if(disposed||config==null||config.ShareState!=WeChatCapabilityState.Ready||!ValidInvitation(invitationId))return false;
            try{
                string query=key+"="+invitationId;
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
