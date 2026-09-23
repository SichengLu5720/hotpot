using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotpotSort.Contracts.RemoteAssets;
using HotpotSort.Platform;
// No Unity calls used in this pure configuration/lifecycle harness.
namespace UnityEngine {public static class JsonUtility {public static void FromJsonOverwrite(string json,object value)=>throw new NotSupportedException();}}
static class CloudAssetQa
{
    const string Env="fixture-env",FileId="cloud://fixture-env.fixture-bucket/release/hotpot-remote.bundle";
    static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
    sealed class Bridge:ICloudAssetNativeBridge
    {
        public int Starts,Aborts,Releases,Reads,Id;public bool ThrowStart,ThrowRead;public Action<int,double,string> Callback;
        public void Start(int id,string env,string file,double timeout,Action<int,double,string> cb){Check(env==Env&&file==FileId&&timeout==60000,"exact native args");Starts++;Id=id;Callback=cb;if(ThrowStart)throw new Exception();}
        public void Abort(int id){Aborts++;}
        public void Release(int id){Releases++;}
        public byte[] ReadTemporary(string path){Reads++;Check(path=="temporary-fixture","read only returned path");if(ThrowRead)throw new Exception();return new byte[]{1,2,3};}
    }
    static Task<AssetDownloadResult> Start(Bridge bridge,CancellationToken token=default,Action<long> progress=null)=>new CloudAssetDownloadClient(bridge).DownloadAsync(Env,FileId,TimeSpan.FromSeconds(60),progress,token);
    static async Task Main()
    {
        int groups=0;async Task Run(string name,Func<Task> test){await test();groups++;Console.WriteLine(name+" PASS");}
        await Run("C01-config-and-exact-environment",async()=>{
            Check(RemoteAssetSource.IsCloudFile(FileId,Env),"valid cloud shape");
            foreach(string bad in new[]{"cloud://fixture-env/path","cloud://fixture-env./x","cloud://other.fixture-bucket/x",FileId+"?x",FileId+"#x",FileId+"%20",FileId+"/../x",FileId+"/",FileId+"\\x",FileId+"\nx",FileId.Replace("fixture-bucket","fixture-bucket:443"),FileId.Replace("cloud:","Cloud:")})Check(!RemoteAssetSource.IsCloudFile(bad,Env),"bad shape accepted");
            var b=new Bridge();Check((await new CloudAssetDownloadClient(b).DownloadAsync("other",FileId,TimeSpan.FromSeconds(60),null,default)).Error==AssetError.NotConfigured&&b.Starts==0,"invalid config reached native");
        });
        await Run("C02-mode-selection-and-explicit-HTTPS",()=>{
            var values=new Dictionary<string,string>{{"WECHAT_CLOUD_ENV_ID",Env},{"HOTPOT_REMOTE_CLOUD_FILE_ID",FileId},{"WECHAT_ASSET_BASE_URL","https://assets.example.invalid/remote/"}};
            string Read(string key)=>values.TryGetValue(key,out var value)?value:null;
            var c=WeChatRuntimeConfig.FromEnvironment(Read);Check(c.AssetState==WeChatCapabilityState.Ready&&c.AssetSource.Mode=="CloudFile"&&c.AssetSource.BaseUrl=="","prefer cloud exactly one source");
            values["HOTPOT_REMOTE_SOURCE"]="Https";c=WeChatRuntimeConfig.FromEnvironment(Read);Check(c.AssetSource.Mode=="Https"&&c.AssetSource.CloudFileId==""&&c.AssetSource.CloudEnvironment=="","explicit HTTPS excludes cloud");
            values["HOTPOT_REMOTE_SOURCE"]="Unknown";bool failed=false;try{WeChatRuntimeConfig.FromEnvironment(Read);}catch(FormatException){failed=true;}Check(failed,"unknown mode");
            values.Remove("HOTPOT_REMOTE_SOURCE");values.Remove("HOTPOT_REMOTE_CLOUD_FILE_ID");Check(WeChatRuntimeConfig.FromEnvironment(Read).AssetSource.Mode=="Https","HTTPS no cloud default");
            values["HOTPOT_REMOTE_SOURCE"]="CloudFile";Check(WeChatRuntimeConfig.FromEnvironment(Read).AssetState==WeChatCapabilityState.NotConfigured,"missing file id must not fall back");return Task.CompletedTask;
        });
        await Run("C03-cold-progress-success-duplicate",async()=>{
            var b=new Bridge();long progress=0;var task=Start(b,progress:n=>progress=n);b.Callback(0,2,"");Check(progress==2&&!task.IsCompleted,"progress");b.Callback(1,0,"temporary-fixture");Check((await task).Bytes.SequenceEqual(new byte[]{1,2,3})&&b.Reads==1&&b.Releases==1,"cold success");b.Callback(1,0,"temporary-fixture");b.Callback(0,10,"");Check(b.Reads==1&&progress==2,"duplicate retired");
        });
        await Run("C04-abort-and-stale-generation",async()=>{
            var b=new Bridge();var cancellation=new CancellationTokenSource();long progress=0;var old=Start(b,cancellation.Token,n=>progress=n);var callback=b.Callback;int firstId=b.Id;cancellation.Cancel();try{await old;throw new Exception("cancel missing");}catch(OperationCanceledException){}
            Check(b.Aborts>0,"real abort missing");var next=Start(b);Check(b.Id>firstId,"id reused");callback(1,0,"temporary-fixture");callback(0,99,"");Check(!next.IsCompleted&&b.Reads==0&&progress==0,"stale operation affected new generation");b.Callback(1,0,"temporary-fixture");Check((await next).StatusCode==200,"next request");
        });
        await Run("C05-permission-init-download-timeout",async()=>{
            foreach(int kind in new[]{2,3,4,5}){var b=new Bridge();var task=Start(b);b.Callback(kind,0,"");var result=await task;Check(b.Starts==1&&b.Reads==0&&b.Releases==1,"failure cleanup");Check(kind==3?result.StatusCode==403:result.Error==(kind==2?AssetError.NotConfigured:kind==5?AssetError.Timeout:AssetError.Network),"error mapping");}
        });
        await Run("C06-read-and-bridge-exceptions",async()=>{
            var b=new Bridge{ThrowRead=true};var task=Start(b);b.Callback(1,0,"temporary-fixture");Check((await task).Error==AssetError.CacheIO&&b.Releases==1,"read error cleanup");
            b=new Bridge{ThrowStart=true};Check((await Start(b)).Error==AssetError.NotConfigured&&b.Aborts==1,"start exception cleanup");
        });
        await Run("C07-pre-cancel-and-progress-subscriber",async()=>{
            var b=new Bridge();var c=new CancellationTokenSource();c.Cancel();try{await Start(b,c.Token);throw new Exception("cancel missing");}catch(OperationCanceledException){}Check(b.Starts==0,"pre-cancel called SDK");
            var task=Start(b,progress:_=>throw new Exception());b.Callback(0,2,"");b.Callback(1,0,"temporary-fixture");Check((await task).StatusCode==200,"progress subscriber broke success");
        });
        await Run("C08-chunked-binary-exact-content-and-short-read",()=>{
            foreach(int length in new[]{0,1,262144,262145,14718885}){
                var expected=new byte[length];for(int i=0;i<length;i++)expected[i]=(byte)(i*31);
                int next=0,calls=0;
                var actual=ChunkedAssetRead.Read(length,(offset,count)=>{
                    Check(offset==next&&count>0&&count<=262144,"bounded sequential read");
                    next+=count;calls++;var part=new byte[count];Buffer.BlockCopy(expected,offset,part,0,count);return part;
                });
                Check(actual.SequenceEqual(expected)&&next==length&&calls==(length+262143)/262144,"exact assembly");
            }
            foreach(int mode in new[]{0,1,2}){bool failed=false;try{ChunkedAssetRead.Read(mode==0?-1:4,(o,n)=>mode==1?null:new byte[n-1]);}catch(AssetFailure e){failed=e.Code==AssetError.CacheIO;}Check(failed,"invalid/short read accepted");}
            return Task.CompletedTask;
        });
        Console.WriteLine("CLOUD_ASSET_MANAGED_PASS groups="+groups+" externalRequests=0");
    }
}
