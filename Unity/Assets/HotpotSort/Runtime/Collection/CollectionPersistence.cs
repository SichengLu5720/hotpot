using System;
using HotpotSort.Contracts;

namespace HotpotSort.Collection
{
    // Host supplies serialization/storage; the core remains usable without Unity.
    public sealed class CollectionPersistence:ICollectionPersistence
    {
        readonly string key,environment,account;
        readonly Func<string,string> read;
        readonly Action<string,string> write;
        readonly Func<CollectionDocument,string> encode;
        readonly Func<string,CollectionDocument> decode;
        public CollectionPersistence(string key,string environment,string account,Func<string,string> read,Action<string,string> write,Func<CollectionDocument,string> encode,Func<string,CollectionDocument> decode)
        {this.key=key;this.environment=environment;this.account=account;this.read=read;this.write=write;this.encode=encode;this.decode=decode;}
        public CollectionDocument Load()
        {
            string main=read(key),backup=read(key+".backup");
            if(string.IsNullOrEmpty(main)&&string.IsNullOrEmpty(backup))return null;
            foreach(var text in new[]{main,backup})try{if(string.IsNullOrEmpty(text))continue;var d=decode(text);CollectionStore.Validate(d,environment,account);return d;}catch{}
            throw new InvalidOperationException("Collection cache corrupt; original retained");
        }
        public void Save(CollectionDocument document)
        {
            CollectionStore.Validate(document,environment,account);string encoded=encode(document);
            var previous=Load();if(previous!=null)write(key+".backup",encode(previous));
            write(key,encoded);
        }
    }
}
