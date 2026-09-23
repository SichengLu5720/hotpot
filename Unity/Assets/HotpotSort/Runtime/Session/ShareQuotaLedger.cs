using System;
using System.Collections.Generic;
using System.Globalization;
using HotpotSort.Contracts;

namespace HotpotSort.Session
{
    // UTC wall-clock ledger; reservations are process-local and never spend a share.
    // Missing timestamps deliberately mean expired legacy cooldowns.
    public sealed class ShareQuotaLedger : IShareQuotaStore
    {
        readonly List<ShareQuotaRecord> records;
        readonly List<string> claims;
        readonly Action persist;
        string reservedDay,reservedRequest;
        public ShareQuotaLedger(List<ShareQuotaRecord> records,List<string> claims,Action persist=null)
        {this.records=records??throw new ArgumentNullException(nameof(records));this.claims=claims??throw new ArgumentNullException(nameof(claims));this.persist=persist;}
        ShareQuotaRecord Find(string day)=>records.Find(q=>q.day==day);
        bool Committed(string requestId)=>claims.Contains(requestId)||records.Exists(q=>q.committedRequestId==requestId);
        public ShareAvailability ReadShareAvailability(string day,DateTimeOffset utc)
        {
            var q=Find(day);int used=q?.used??0;DateTimeOffset? next=null;
            if(q!=null && !string.IsNullOrEmpty(q.lastEffectiveUtc) && (used==1||used==2) &&
                DateTimeOffset.TryParse(q.lastEffectiveUtc,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out var last))
                next=last.ToUniversalTime().AddMinutes(used==1?5:15);
            return new ShareAvailability(day,used,next,reservedRequest!=null,utc.ToUniversalTime());
        }
        public bool TryReserveShare(string day,string requestId,DateTimeOffset utc)
        {
            if(string.IsNullOrEmpty(day)||string.IsNullOrEmpty(requestId)||Committed(requestId)||!ReadShareAvailability(day,utc).Available)return false;
            reservedDay=day;reservedRequest=requestId;return true;
        }
        public bool HasShareReservation(string day,string requestId)=>reservedRequest!=null&&reservedRequest==requestId&&reservedDay==day&&!Committed(requestId)&&(Find(day)?.used??0)<3;
        public bool TryCommitShare(string day,string requestId,DateTimeOffset effectiveUtc)
        {
            if(!HasShareReservation(day,requestId))return false;
            var q=Find(day);if(q==null){q=new ShareQuotaRecord{day=day};records.Add(q);}
            q.used++;q.lastEffectiveUtc=effectiveUtc.ToUniversalTime().ToString("o",CultureInfo.InvariantCulture);q.dataVersion=2;q.committedRequestId=requestId;claims.Add(requestId);
            reservedDay=null;reservedRequest=null;persist?.Invoke();return true;
        }
        public void ReleaseShare(string day,string requestId)
        {if(reservedDay==day&&reservedRequest==requestId){reservedDay=null;reservedRequest=null;}}
    }
}
