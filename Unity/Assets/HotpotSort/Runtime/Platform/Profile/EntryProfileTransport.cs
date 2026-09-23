using System;
using System.Threading.Tasks;
using HotpotSort.Contracts;

namespace HotpotSort.Profile
{
    // Uploading durable facts is safe during play; replacing the visible profile is
    // not. A server commit remains durable even when its response is retired here.
    // The unchanged local outbox is re-sent idempotently at the next entry boundary.
    public sealed class EntryProfileTransport:IProfileSyncTransport
    {
        readonly IProfileSyncTransport inner;
        readonly Func<bool> atEntry;
        readonly Func<long> sessionGeneration;
        public EntryProfileTransport(IProfileSyncTransport inner,Func<bool> atEntry,Func<long> sessionGeneration)
        {this.inner=inner??throw new ArgumentNullException(nameof(inner));this.atEntry=atEntry??throw new ArgumentNullException(nameof(atEntry));this.sessionGeneration=sessionGeneration??throw new ArgumentNullException(nameof(sessionGeneration));}
        public async Task<ProfileSyncResponse> SyncAsync(ProfileDocument document)
        {
            long lease=sessionGeneration();var response=await inner.SyncAsync(document);
            if(!atEntry()||sessionGeneration()!=lease)return new ProfileSyncResponse{Status=ProfileSyncStatus.Stale};
            return response;
        }
    }
}
