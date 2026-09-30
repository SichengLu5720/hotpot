using System;
using System.IO;
using System.Text;
using System.Threading;
using HotpotSort.Contracts.RemoteAssets;

namespace HotpotSort.Platform.RemoteAssets
{
    public sealed class TransactionalAssetCache:IRemoteAssetCache
    {
        readonly IRemoteAssetFileSystem fs;
        public TransactionalAssetCache(IRemoteAssetFileSystem fs){this.fs=fs??throw new ArgumentNullException(nameof(fs));}
        static void Partition(string key)
        {var parts=(key??"").Split('/');if(parts.Length!=4)throw new AssetFailure(AssetError.CacheIO);foreach(var p in parts)if(!RemoteAssetValidation.Hash(p))throw new AssetFailure(AssetError.CacheIO);}
        static string DirectoryKey(string partition)=>RemoteAssetValidation.Sha(Encoding.UTF8.GetBytes(partition));
        public byte[] ReadCommitted(string partition)
        {
            Partition(partition);string directory=DirectoryKey(partition);byte[] record=fs.Read(directory+"/commit");if(record==null)return null;
            string[] fields=Encoding.UTF8.GetString(record).Split('\n');
            if(fields.Length!=4||fields[0]!="1"||fields[1]!=partition||!Guid.TryParseExact(fields[2],"N",out _)||!RemoteAssetValidation.Hash(fields[3]))return null;
            var bytes=fs.Read(directory+"/"+fields[2]+".bundle");
            return bytes!=null&&RemoteAssetValidation.Sha(bytes)==fields[3]?bytes:null;
        }
        public void Commit(string partition,byte[] verifiedBytes,CancellationToken cancel)
        {
            Partition(partition);string directory=DirectoryKey(partition),id=Guid.NewGuid().ToString("N");string temp=directory+"/"+id+".part",payload=directory+"/"+id+".bundle",record=directory+"/"+id+".record";bool committed=false;
            try
            {
                cancel.ThrowIfCancellationRequested();fs.Write(temp,verifiedBytes);
                var reread=fs.Read(temp);if(reread==null||reread.Length!=verifiedBytes.Length||RemoteAssetValidation.Sha(reread)!=RemoteAssetValidation.Sha(verifiedBytes))throw new AssetFailure(AssetError.CacheIO);
                cancel.ThrowIfCancellationRequested();fs.Move(temp,payload);
                fs.Write(record,Encoding.UTF8.GetBytes("1\n"+partition+"\n"+id+"\n"+RemoteAssetValidation.Sha(verifiedBytes)));
                cancel.ThrowIfCancellationRequested();fs.Move(record,directory+"/commit");committed=true;
            }
            finally
            {
                // No recursive cleanup, no SDK/global cache clearing, no prior valid payload removal.
                try{fs.Delete(temp);fs.Delete(record);if(!committed)fs.Delete(payload);}catch{}
            }
        }
    }
    public sealed class LocalRemoteAssetFileSystem:IRemoteAssetFileSystem
    {
        readonly string root;
        public LocalRemoteAssetFileSystem(string applicationCacheRoot){root=Path.GetFullPath(Path.Combine(applicationCacheRoot,"hotpot-remote-assets-v1"));}
        string Resolve(string relative)
        {
            if(!RemoteAssetValidation.SafePath(relative))throw new AssetFailure(AssetError.CacheIO);
            string path=Path.GetFullPath(Path.Combine(root,relative));
            if(!path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new AssetFailure(AssetError.CacheIO);return path;
        }
        public byte[] Read(string path){string resolved=Resolve(path);return File.Exists(resolved)?File.ReadAllBytes(resolved):null;}
        public void Write(string path,byte[] bytes){string resolved=Resolve(path);Directory.CreateDirectory(Path.GetDirectoryName(resolved));using(var file=new FileStream(resolved,FileMode.CreateNew,FileAccess.Write,FileShare.None)){file.Write(bytes,0,bytes.Length);file.Flush(true);}}
        public void Move(string source,string destination)
        {
            string from=Resolve(source),to=Resolve(destination);
            if(File.Exists(to))File.Replace(from,to,null);else File.Move(from,to);
        }
        public void Delete(string path){File.Delete(Resolve(path));}
    }
}
