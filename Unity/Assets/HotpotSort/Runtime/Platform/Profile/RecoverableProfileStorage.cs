using System;
using System.Security.Cryptography;
using System.Text;
using HotpotSort.Contracts;

namespace HotpotSort.Profile
{
    // Complete checked documents are written to one local key. A prior valid document
    // is retained in another key; both damaged copies fail explicitly (never reset wins).
    public sealed class RecoverableProfileStorage:IProfilePersistence
    {
        readonly Func<string,string> read;
        readonly Action<string,string> write;
        readonly Func<ProfileDocument,string> encode;
        readonly Func<string,ProfileDocument> decode;
        readonly string key,environment,account;
        public bool RecoveredBackup { get; private set; }
        public RecoverableProfileStorage(string key,string environment,string account,Func<string,string> read,Action<string,string> write,
            Func<ProfileDocument,string> encode,Func<string,ProfileDocument> decode)
        {this.key=key;this.environment=environment;this.account=account;this.read=read;this.write=write;this.encode=encode;this.decode=decode;}
        public static string Hash(string data)
        {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(data))).Replace("-","").ToLowerInvariant();}
        ProfileDocument Parse(string envelope)
        {
            if(string.IsNullOrEmpty(envelope))return null;
            int newline=envelope.IndexOf('\n');if(newline!=64)throw new InvalidOperationException("Corrupt profile envelope");
            string body=envelope.Substring(newline+1);if(Hash(body)!=envelope.Substring(0,64))throw new InvalidOperationException("Profile checksum mismatch");
            var value=decode(body);ProfileStore.Validate(value,environment,account);return value;
        }
        public ProfileDocument Load()
        {
            string current=read(key),backup=read(key+".backup");
            if(string.IsNullOrEmpty(current)&&string.IsNullOrEmpty(backup))return null;
            try{var value=Parse(current);if(value!=null)return value;}catch{/* Try the last complete valid document. */}
            var recovered=Parse(backup);if(recovered==null)throw new InvalidOperationException("Profile has no valid recoverable backup");
            RecoveredBackup=true;return recovered;
        }
        public void Save(ProfileDocument document)
        {
            ProfileStore.Validate(document,environment,account);string body=encode(document),current=read(key);
            bool valid=false;try{valid=Parse(current)!=null;}catch{/* Never replace a valid backup with corrupt bytes. */}
            if(valid)write(key+".backup",current);
            write(key,Hash(body)+"\n"+body);
        }
    }
}
