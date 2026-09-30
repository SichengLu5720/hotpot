using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using UnityEngine;

namespace HotpotSort.Bootstrap
{
    public sealed partial class DailyProductionComposition
    {
        [Serializable] sealed class PendingBrothWin {public string sessionId,completedUtc,operation;}
        [Serializable] sealed class BrothWinQueue {public List<PendingBrothWin> wins=new List<PendingBrothWin>();}
        bool socialFlushing,tradeLanding;
        string tradeShown,helpRestoredAccount;
        double nextSocialRefresh,nextBrothWinFlush,nextTradeLanding,nextCountdown;
        string BrothWinKey=>"Hotpot.BrothWins."+BrothCollection.environment+"."+BrothCollection.account;
        void BindSocialPresentation()
        {
            view.CollectionTradeShareRequested-=ShareCollectionTrade;view.CollectionTradeShareRequested+=ShareCollectionTrade;
            view.CollectionTradeClosed-=DismissCollectionTrade;view.CollectionTradeClosed+=DismissCollectionTrade;
        }
        void ShareCollectionTrade(IngredientTradeRequest request)
        {
            bool requested=brothLinks?.RequestTradeShare(request.requestId)==true;
            view.SetCollectionTradeMessage(requested?"已打开分享，等待对方接受":"分享暂不可用，请重试");
        }
        void DismissCollectionTrade(){brothLinks?.DismissTrade(brothLinks.PendingTradeId);tradeShown=null;}
        void RestoreBrothHelping()
        {
            var doc=BrothCollection;if(doc==null||brothLinks==null||helpRestoredAccount==doc.account||doc.brothHelping==null||doc.brothHelping.Count==0)return;
            helpRestoredAccount=doc.account;
            if(PendingBrothInvitationId==null)brothLinks.ReceiveQuery(new Dictionary<string,string>{{HotpotSort.Platform.WeChatActivityLinkService.QueryKey,doc.brothHelping[0].invitationId}});
        }
        void RecordBrothChallengeWin(string sessionId,DateTimeOffset completed)
        {
            if(BrothCollection==null)return;
            try{
                string key=BrothWinKey;var queue=JsonUtility.FromJson<BrothWinQueue>(PlayerPrefs.GetString(key,"{}"))??new BrothWinQueue();queue.wins=queue.wins??new List<PendingBrothWin>();
                if(queue.wins.Exists(w=>w.sessionId==sessionId))return;
                queue.wins.Add(new PendingBrothWin{sessionId=sessionId,completedUtc=completed.ToUniversalTime().ToString("o"),operation=Guid.NewGuid().ToString("N")});
                PlayerPrefs.SetString(key,JsonUtility.ToJson(queue));PlayerPrefs.Save();nextBrothWinFlush=0;FlushBrothChallengeWins();
            }catch(Exception ex){Debug.LogWarning("Broth win queue unavailable: "+ex.GetType().Name);}
        }
        async void FlushBrothChallengeWins()
        {
            if(socialFlushing||!(BrothActivity is IBrothChallengeCompletion completion)||BrothCollection==null)return;
            var service=BrothActivity;string account=BrothCollection.account,key=BrothWinKey;socialFlushing=true;nextBrothWinFlush=Time.realtimeSinceStartupAsDouble+15;
            try{
                var queue=JsonUtility.FromJson<BrothWinQueue>(PlayerPrefs.GetString(key,"{}"));
                foreach(var win in queue?.wins??new List<PendingBrothWin>()){
                    var result=await completion.CompleteChallengeAsync(Guid.NewGuid().ToString("N"),win.sessionId,win.completedUtc,win.operation);
                    if(!this||!ReferenceEquals(service,BrothActivity)||BrothCollection?.account!=account)return;
                    if(!result.Succeeded)return;
                    var latest=JsonUtility.FromJson<BrothWinQueue>(PlayerPrefs.GetString(key,"{}"));latest?.wins?.RemoveAll(w=>w.operation==win.operation);PlayerPrefs.SetString(key,JsonUtility.ToJson(latest??new BrothWinQueue()));PlayerPrefs.Save();OnBrothActivityChanged();
                }
            }catch(Exception ex){Debug.LogWarning("Broth win sync pending: "+ex.GetType().Name);}
            finally{socialFlushing=false;}
        }
        async void TryPresentCollectionTrade()
        {
            string id=brothLinks?.PendingTradeId;if(id==null||id==tradeShown||tradeLanding||collectionAuthority==null||!view||view.BrothVisible||shown.phase!=HotpotSort.Presentation.ViewPhase.Entry||Time.realtimeSinceStartupAsDouble<nextTradeLanding)return;
            var authority=collectionAuthority;string account=BrothCollection?.account;tradeLanding=true;nextTradeLanding=Time.realtimeSinceStartupAsDouble+4;
            try{bool opened=await view.OpenCollectionTradeRequestAsync(id);if(this&&opened&&ReferenceEquals(authority,collectionAuthority)&&account==BrothCollection?.account&&id==brothLinks?.PendingTradeId)tradeShown=id;}
            catch(Exception ex){Debug.LogWarning("Trade landing retry pending: "+ex.GetType().Name);}
            finally{tradeLanding=false;}
        }
        void UpdateSocialActivity()
        {
            TryPresentCollectionTrade();double now=Time.realtimeSinceStartupAsDouble;
            if(now>=nextBrothWinFlush)FlushBrothChallengeWins();
            if(now>=nextCountdown){nextCountdown=now+1;if(view)view.RefreshBrothCountdown();}
            if(now>=nextSocialRefresh&&BrothActivity!=null&&!brothBusy&&!brothInspecting){nextSocialRefresh=now+15;_=RefreshBrothAsync();if(PendingBrothInvitationId!=null&&view&&!view.CollectionTradeVisible)brothShownInvitation=null;}
        }
    }
}
