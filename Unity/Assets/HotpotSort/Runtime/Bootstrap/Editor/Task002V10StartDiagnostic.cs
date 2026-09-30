#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotpotSort.Contracts.RemoteAssets;
using HotpotSort.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HotpotSort.Bootstrap
{
    [InitializeOnLoad] public static class Task002V10StartDiagnostic
    {
        const string Key="Task002V10StartDiagnostic";
        static Transport network;static byte[] bundle;static string manifest,config;
        static int groups;
        static Task002V10StartDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void BeforeScene(){if(SessionState.GetBool(Key,false))Install();}
        public static void Run()
        {
            SessionState.SetBool(Key,true);
            EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");
            EditorApplication.EnterPlaymode();
        }
        public static void RunLocal(){SessionState.SetBool(Key+"Local",true);Run();}
        static void Install()
        {
            manifest=File.ReadAllText(Environment.GetEnvironmentVariable("HOTPOT_REMOTE_MANIFEST"));
            bundle=File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("HOTPOT_REMOTE_MANIFEST")),"hotpot-remote.bundle"));
            var m=JsonUtility.FromJson<RemoteAssetManifest>(manifest);
            string sha;using(var hash=System.Security.Cryptography.SHA256.Create())sha=BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(manifest))).Replace("-","").ToLowerInvariant();
            config="{\"schemaVersion\":1,\"releaseId\":\""+m.releaseId+"\",\"manifestSha256\":\""+sha+"\",\"baseUrl\":\"https://assets.example.invalid/remote/\",\"sizeProbe\":false}";
            network=new Transport();Bootstrap.DiagnosticAssets=()=>{
                var composition=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();
                var isolated=new LocalDevelopmentServices("HotpotSort.V10.QA."+Guid.NewGuid().ToString("N"));
                composition.ConfigureServices(isolated,isolated,isolated,isolated);
                return SessionState.GetBool(Key+"Local",false)?new LocalPresentationAssetProvider():Make(network,config);
            };
        }
        static IPresentationAssetProvider Make(Transport transport,string cfg)=>Bootstrap.CreateRemoteAssets(manifest,cfg,"development",transport,new Disk(Environment.GetEnvironmentVariable("HOTPOT_V10_CACHE")),themeRoot:UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>().ApprovedAssetRoot);
        sealed class Disk:IRemoteAssetFileSystem
        {
            readonly string root;public Disk(string root){this.root=root;Directory.CreateDirectory(root);}
            string PathFor(string path){if(path.Contains("..")||Path.IsPathRooted(path))throw new Exception("unsafe QA path");return Path.Combine(root,path);}
            public byte[] Read(string path)=>File.Exists(PathFor(path))?File.ReadAllBytes(PathFor(path)):null;
            public void Write(string path,byte[] bytes){var p=PathFor(path);Directory.CreateDirectory(Path.GetDirectoryName(p));File.WriteAllBytes(p,bytes);}
            public void Move(string from,string to){var a=PathFor(from);var b=PathFor(to);if(File.Exists(b))File.Replace(a,b,null);else File.Move(a,b);}
            public void Delete(string path){File.Delete(PathFor(path));}
        }
        sealed class Transport:IRemoteAssetTransport
        {
            public int Calls;public bool Fail=true,Hold;public TaskCompletionSource<AssetDownloadResult> Pending;public Action<long> Progress;
            public Task<AssetDownloadResult> DownloadAsync(Uri uri,TimeSpan timeout,Action<long> progress,CancellationToken token)
            {
                Calls++;Progress=progress;
                if(!Hold)return Task.FromResult(Fail?new AssetDownloadResult(null,404):new AssetDownloadResult(bundle,200));
                var done=Pending=new TaskCompletionSource<AssetDownloadResult>();token.Register(()=>done.TrySetCanceled());return done.Task;
            }
        }
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        static async Task Until(Func<bool> condition){for(int i=0;i<300;i++){if(condition())return;await Task.Delay(50);}throw new Exception("Timed out");}
        static void Pass(string text){groups++;Debug.Log("V10_START_SMOKE "+text+" PASS");}
        sealed class FriendWire:HotpotSort.Platform.IWeChatFriendBoardTransport
        {
            public HotpotSort.Contracts.FriendBoardViewport Last;
            public void PostMessage(string json){}
            public void Show(Texture texture,HotpotSort.Contracts.FriendBoardViewport viewport){Last=viewport;}
            public void Hide(){}
        }
        static async void Execute()
        {
            int code=0;
            try
            {
                Bootstrap boot=null;await Until(()=>{boot=UnityEngine.Object.FindFirstObjectByType<Bootstrap>();return boot&&boot.IsConfigured;});
                var composition=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();
                if(SessionState.GetBool(Key+"Local",false))
                {
                    Check(boot.Assets.Snapshot.State==AssetReadiness.Ready&&composition.ActiveCore==null,"local complete entry");
                    composition.SessionAction(ViewAction.StartToday);await boot.PendingStart;Check(boot.Controller.CanAcceptInput&&composition.ActiveCore!=null,"local start");
                    string day=boot.Controller.Snapshot.Challenge.ChallengeId;long generation=boot.Controller.Generation;
                    composition.SessionAction(ViewAction.RetrySameDay);await Until(()=>boot.Controller.Generation>generation&&boot.Controller.CanAcceptInput);
                    Check(boot.Controller.Snapshot.Challenge.ChallengeId==day,"same-day retry changed date");
                    composition.SessionAction(ViewAction.Exit);Check(composition.ActiveCore==null&&composition.PlayerView.LastSnapshot.phase==ViewPhase.Entry,"local exit");
                    Pass("complete-local-desktop-start-same-day-retry-exit");return;
                }
                var m=JsonUtility.FromJson<RemoteAssetManifest>(manifest);
                Check(m.assets.All(a=>!Resources.Load<Texture2D>(a.logicalPath)),"remote resources still present");
                Check(composition.PlayerView.VisualArt!=null&&composition.PlayerView.LastSnapshot.phase==ViewPhase.Entry&&boot.Controller.Snapshot==null&&boot.Controller.Resolved==null,"entry failed");
                Pass("local-entry-with-remote31-absent");
                using(var missing=Make(new Transport(),config.Replace("https://assets.example.invalid/remote/","")))Check((await missing.PrepareAsync()).Error==AssetError.NotConfigured,"missing config");
                composition.SessionAction(ViewAction.StartToday);await boot.PendingStart;
                Check(boot.Assets.Snapshot.State==AssetReadiness.Failed&&boot.Assets.Snapshot.Error==AssetError.HttpStatus&&boot.Controller.Generation==0&&boot.Controller.Resolved==null&&boot.Controller.ActiveSeconds==0&&composition.ActiveCore==null,"failed request changed game");Pass("missing-config-and-failure-no-session");
                network.Fail=false;network.Hold=true;composition.SessionAction(ViewAction.StartToday);var cancelled=boot.PendingStart;var oldProgress=network.Progress;
                composition.SessionAction(ViewAction.StartToday);Check(network.Calls==2,"duplicate Start downloaded twice");
                composition.SessionAction(ViewAction.Exit);await cancelled;oldProgress(100);
                Check(boot.Assets.Snapshot.State==AssetReadiness.Cancelled&&boot.Controller.Resolved==null&&composition.ActiveCore==null,"cancel stale callback");Pass("cancel-single-flight-stale-callback");
                boot.SetAssetForeground(false);composition.SessionAction(ViewAction.StartToday);network.Pending.SetResult(new AssetDownloadResult(bundle,200));await boot.PendingStart;
                Check(boot.Assets.Snapshot.State==AssetReadiness.Ready&&boot.Controller.Snapshot==null&&boot.Controller.Resolved==null,"background readiness: "+boot.Assets.Snapshot.State+"/"+boot.Assets.Snapshot.Error+" session="+(boot.Controller.Snapshot!=null)+" resolved="+(boot.Controller.Resolved!=null));Pass("real-bundle-background-ready-no-start");
                boot.SetAssetForeground(true);await Until(()=>boot.Controller.Snapshot!=null);
                Check(composition.ActiveCore!=null&&boot.Controller.CanAcceptInput,"foreground start failed");
                await Until(()=>composition.PlayerView.World.Bodies.Count()>0);
                for(int i=0;i<16;i++)Check(composition.PlayerView.FoodTexture(i)==boot.Assets.GetComplete<Texture2D>(composition.PlayerView.VisualArt.Theme.Resolve(AssetKey.Food(i)))&&composition.PlayerView.FoodTexture(i).isReadable,"provider/alpha mismatch");
                var before=boot.Controller.Generation;composition.SessionAction(ViewAction.StartToday);await boot.PendingStart;Check(boot.Controller.Generation==before,"duplicate Ready start");
                composition.SessionAction(ViewAction.Pause);composition.SessionAction(ViewAction.Resume);Check(boot.Controller.CanAcceptInput,"pause/resume");
                var shapeField=typeof(DailyProductionComposition).GetField("plateShapes",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);Check(((System.Collections.IDictionary)shapeField.GetValue(composition)).Count>0,"alpha layout not loaded");
                Pass("start-supply-alpha-pause-core-flow");
                var friendWire=new FriendWire();using(var friend=new HotpotSort.Platform.WeChatFriendBoardSurface(friendWire))
                {
                    composition.ConfigureFriendSurface(friend,"qa_owner");
                    var pixels=composition.PlayerView.FriendBoardViewportPixels;
                    var viewport=new HotpotSort.Contracts.FriendBoardViewport((int)pixels.x,(int)pixels.y,(int)pixels.width,(int)pixels.height,1);
                    Check(composition.OpenFriendSurface(viewport),"friend open failed");
                    Check(composition.PlayerView.FriendBoardVisible&&!boot.Controller.CanAcceptInput,"friend modal/input gate missing");
                    Check(composition.PlayerView.GetComponentsInChildren<UnityEngine.UI.RawImage>().Any(i=>i.texture==friend.SharedTexture&&i.uvRect==HotpotSort.Platform.WeChatFriendBoardSurface.SharedTextureUv),"friend texture/UV bridge missing");
                    Check(friendWire.Last.Width==(int)pixels.width&&friendWire.Last.Height==(int)pixels.height,"friend physical viewport mismatch");
                    composition.PlayerView.CloseFriendBoardView();Check(!composition.FriendSurfaceOpen&&boot.Controller.CanAcceptInput,"friend close did not release own pause");
                    composition.SessionAction(ViewAction.Pause);composition.OpenFriendSurface(viewport);composition.CloseFriendSurface();
                    Check((boot.Controller.Pauses&HotpotSort.Session.PauseReasons.User)!=0,"friend close resumed preexisting user pause");
                    composition.SessionAction(ViewAction.Resume);
                    var platform=(HotpotSort.Platform.WeChatPlatform)typeof(Bootstrap).GetField("platform",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(boot);
                    composition.OpenFriendSurface(viewport);platform.NotifyEditorPause(true);composition.CloseFriendSurface();
                    Check((boot.Controller.Pauses&HotpotSort.Session.PauseReasons.Background)!=0&&!boot.Controller.CanAcceptInput,"friend close resumed background");
                    platform.NotifyEditorPause(false);Check(boot.Controller.CanAcceptInput,"background restoration");
                }
                composition.ConfigureFriendSurface(null,null);
                Pass("friend-real-presentation-texture-uv-modal-pause-composition");
                composition.SessionAction(ViewAction.Exit);
                composition.ReleaseAssetConsumers();PresentationAssets.Clear(boot.Assets);boot.Assets.Dispose();
                var warmNet=new Transport();using(var warm=Make(warmNet,config)){Check((await warm.PrepareAsync()).State==AssetReadiness.Ready&&warmNet.Calls==0,"warm cache redownloaded");}
                string cloudConfig=config.Replace("\"schemaVersion\":1","\"schemaVersion\":2").Replace("https://assets.example.invalid/remote/","").Replace("\"sizeProbe\":false","\"sizeProbe\":false,\"sourceMode\":\"CloudFile\",\"cloudEnvironment\":\"fixture-env\",\"cloudFileId\":\"cloud://fixture-env.fixture-bucket/release/hotpot-remote.bundle\"");
                var cloudNet=new Transport();using(var warm=Bootstrap.CreateRemoteAssets(manifest,cloudConfig,"development",cloudNet,new Disk(Environment.GetEnvironmentVariable("HOTPOT_V10_CACHE")),"fixture-env",composition.ApprovedAssetRoot)){Check((await warm.PrepareAsync()).State==AssetReadiness.Ready&&cloudNet.Calls==0,"cloud warm cache initialized network");}
                using(var invalid=Bootstrap.CreateRemoteAssets(manifest,cloudConfig,"development",cloudNet,new Disk(Environment.GetEnvironmentVariable("HOTPOT_V10_CACHE")),"wrong-env",composition.ApprovedAssetRoot)){Check((await invalid.PrepareAsync()).State==AssetReadiness.Failed&&cloudNet.Calls==0,"cloud environment mismatch accepted");}
                Pass("warm-cache-revalidated-cloud-binding-no-network");
            }
            catch(Exception e){code=1;Debug.LogException(e);}
            finally
            {
                bool local=SessionState.GetBool(Key+"Local",false);SessionState.SetBool(Key,false);SessionState.SetBool(Key+"Local",false);Bootstrap.DiagnosticAssets=null;
                File.WriteAllText(Environment.GetEnvironmentVariable("HOTPOT_V10_REPORT"),"{\"exitCode\":"+code+",\"groups\":"+groups+",\"networkRequests\":0,\"localMode\":"+local.ToString().ToLowerInvariant()+"}");
                EditorApplication.Exit(code);
            }
        }
    }
}
#endif
