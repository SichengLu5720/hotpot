using System;
using System.IO;
using HotpotSort.Contracts;
using HotpotSort.Session;
using HotpotSort.Platform;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotpotSort.Contracts.RemoteAssets;
using HotpotSort.Platform.RemoteAssets;
using HotpotSort.Presentation;
using HotpotSort.Profile;
#if UNITY_WEBGL || UNITY_EDITOR
using WeChatWASM;
#endif

namespace HotpotSort.Bootstrap
{
    // Sole production scene entry. Fixed real factories are supplied at integration.
    public sealed class Bootstrap : MonoBehaviour
    {
        [SerializeField] private ProductionComposition composition;
        private static Bootstrap owner;
        private WeChatPlatform platform;
        private WeChatServiceScope weChatServices;
        private WeChatSilentLogin silentLogin;
        private WeChatProfileTransport profileTransport;
        private LocalDevelopmentServices localProfile;
        private EntryProfileTransport entryProfileTransport;
        private bool wasProfileEntry;
        public WeChatLoginStatus LoginStatus=>silentLogin?.Status??WeChatLoginStatus.Local;
        private bool destroyed;
        private IPresentationAssetProvider assets;
        private bool preparingAssets,startIntent,starting,foreground=true,activationFailed;
        private long entryIntent;
        public IPresentationAssetProvider Assets=>assets;
        public Task PendingStart {get;private set;}=Task.CompletedTask;
#if UNITY_EDITOR
        // Isolated diagnostics only. Production never reads a URL or manifest from the command line.
        public static Func<IPresentationAssetProvider> DiagnosticAssets;
#endif
        public SessionController Controller { get; private set; }
        public LocalDiagnostics Diagnostics { get; private set; }
        public bool IsConfigured => Controller != null;
        public string Status { get; private set; } = "not-started";
        private async void Start()
        {
            if (owner != null && owner != this) { Status = "duplicate-bootstrap"; enabled = false; return; }
            owner = this;
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                // WeChat/iOS otherwise inherits Unity's conservative mobile cadence.
                // The export caps DPR separately, so 60 Hz does not multiply the
                // native 3x fill-rate cost on high-density iPhones.
                QualitySettings.vSyncCount=0;
                Application.targetFrameRate=60;
#endif
                if (composition == null) throw new InvalidOperationException("production-core-view-factories-missing");
                if (composition.UsesDevelopmentDoubles) throw new InvalidOperationException("production-rejects-development-doubles");
                if (string.IsNullOrWhiteSpace(composition.RuntimeNamespace) ||
                    System.Text.RegularExpressions.Regex.IsMatch(composition.RuntimeNamespace, "[^a-zA-Z0-9_-]"))
                    throw new InvalidOperationException("invalid-runtime-namespace");
                Status = "initializing-platform";
                await WeChatPlatform.InitializeAsync();
                if (destroyed) return;
                assets=CreateAssets();PresentationAssets.Install(assets);
                if(composition is DailyProductionComposition assetComposition)assetComposition.ConfigureAssetGate(()=>PendingStart=StartWithAssetsAsync(),CancelEntryStart);
#if UNITY_WEBGL && !UNITY_EDITOR
                if(composition is DailyProductionComposition daily)
                {
                    var config=WeChatRuntimeConfig.LoadPackaged();
                    weChatServices=new WeChatServiceScope(config);
                    var cloudFunction=new WeChatCloudProfileFunction(config);
                    silentLogin=new WeChatSilentLogin(config,exchange:cloudFunction);
                    profileTransport=new WeChatProfileTransport(config,silentLogin,cloudFunction);
                    entryProfileTransport=new EntryProfileTransport(profileTransport,()=>AtProfileEntry,()=>Controller?.Generation??-1);
                    localProfile=new LocalDevelopmentServices(environment:config.environment,isDevelopmentSimulation:false);
                    silentLogin.Changed+=OnIdentityChanged;
                    string markerKey="HotpotSort.OwnerMarker."+config.environment;
                    string marker=PlayerPrefs.GetString(markerKey,"");
                    if(string.IsNullOrEmpty(marker)){marker=Guid.NewGuid().ToString("N");PlayerPrefs.SetString(markerKey,marker);PlayerPrefs.Save();}
                    daily.ConfigureServices(localProfile,weChatServices.Rewards,null,weChatServices.Share);
                    daily.ConfigureBrothActivity(null,weChatServices.ActivityLinks);
                    daily.ConfigureFriendSurface(weChatServices.Friends,marker);
                }
                var root = WX.env.USER_DATA_PATH + "/" + composition.RuntimeNamespace;
#else
                var configured = Environment.GetEnvironmentVariable("HOTPOT_RUNTIME_ROOT");
                var root = string.IsNullOrWhiteSpace(configured)
                    ? Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/wechat/runtime", composition.RuntimeNamespace))
                    : Path.GetFullPath(Path.Combine(configured, composition.RuntimeNamespace));
#endif
                Diagnostics = new LocalDiagnostics(new RuntimePaths(root + "/data", root + "/cache", root + "/build"));
                composition.BindDiagnostics(Diagnostics);
                platform = new WeChatPlatform();
                platform.Changed+=OnAssetLifecycle;
                Controller = new SessionController(composition.CoreFactory, composition.ViewFactory,
                    new TimeResolver(composition.TrustedTime, new DeviceTimeProvider()), new UnityClock(),
                    platform, composition.ContentVersion, composition.ConfigurationDigest);
                if(composition is DailyProductionComposition viewportComposition)
                {
                    viewportComposition.SetPlatformPixelRatio(platform.DevicePixelRatio);
                    platform.ViewportChanged+=_=>viewportComposition.SetPlatformPixelRatio(platform.DevicePixelRatio);
                }
                composition.BindSessionObservation(Controller);
                Controller.ObservationChanged+=OnProfileBoundary;
                OnProfileBoundary();
                if(silentLogin!=null)_=silentLogin.StartAsync(); // Never await network before entry/play.
                assets.Changed+=OnAssetState;OnAssetState(assets.Snapshot);
                // The approved entry button owns StartToday. Initialization does not create a daily session.
                Status = Controller.Error ?? "ready";
            }
            catch (Exception ex) { Status = ex is AssetFailure failure?failure.Code.ToString():ex.GetType().Name + ": " + ex.Message; Debug.LogError(Status); Controller?.Dispose(); Controller = null; platform?.Dispose();profileTransport?.Dispose();silentLogin?.Dispose();weChatServices?.Dispose();weChatServices=null; }
        }
        [Serializable] sealed class AssetPackageConfig {public int schemaVersion;public string releaseId,manifestSha256,baseUrl,sourceMode,cloudEnvironment,cloudFileId,packagedPath;public bool sizeProbe;}
        IPresentationAssetProvider CreateAssets()
        {
#if UNITY_EDITOR
            if(DiagnosticAssets!=null)return DiagnosticAssets();
#endif
#if UNITY_WEBGL && !UNITY_EDITOR
            // The approved theme is shipped as native Unity Resources. Do not gate a
            // phone session on an additional AssetBundle read/decompression pass: all
            // visible assets are already part of the converted WeChat data package.
            return new LocalPresentationAssetProvider();
#else
            return new LocalPresentationAssetProvider();
#endif
        }
        public static IPresentationAssetProvider CreateRemoteAssets(string manifestJson,string configurationJson,string environment,IRemoteAssetTransport transport,IRemoteAssetFileSystem fileSystem,string cloudEnvironment=null,string themeRoot=V7Art.Root)
        {
            var themeFile=Resources.Load<TextAsset>(themeRoot+"/presentation-theme");
            if(!themeFile)throw new AssetFailure(AssetError.MissingAsset);
            var theme=JsonUtility.FromJson<PresentationTheme>(themeFile.text);
            if(PresentationThemeValidation.Validate(theme,themeRoot).Length!=0)throw new AssetFailure(AssetError.MissingAsset);
            var local=theme.assets.Where(a=>!PresentationAssets.IsRemote(a.resourceAddress)).Select(a=>a.resourceAddress).Distinct(StringComparer.Ordinal).ToDictionary(a=>a,a=>typeof(Texture2D),StringComparer.Ordinal);
            local.Add(themeRoot+"/presentation-theme",typeof(TextAsset));local.Add(TaskAssetValidation.ModernFontResource,typeof(Font));local.Add("Hotpot/TASK002/v9/r001/fonts/glyphs",typeof(TextAsset));
            var expectedPaths=theme.assets.Where(a=>PresentationAssets.IsRemote(a.resourceAddress)).Select(a=>a.resourceAddress).Distinct(StringComparer.Ordinal).ToDictionary(a=>a,a=>"Texture2D",StringComparer.Ordinal);
            RemoteAssetManifest manifest=null;AssetPackageConfig config=null;
            try{config=JsonUtility.FromJson<AssetPackageConfig>(configurationJson);manifest=UnityRemoteAssetManifest.Parse(manifestJson);}catch{}
            bool valid=config!=null&&(config.schemaVersion==1||config.schemaVersion==2)&&!config.sizeProbe&&manifest!=null&&config.releaseId==manifest.releaseId&&config.manifestSha256==RemoteAssetValidation.Sha(Encoding.UTF8.GetBytes(manifestJson));
            var source=config==null?null:new RemoteAssetSource(config.schemaVersion==1?"Https":config.sourceMode,config.baseUrl,config.cloudEnvironment,config.cloudFileId,config.packagedPath);
            if(source!=null&&source.Mode=="CloudFile"&&(config.schemaVersion!=2||source.CloudEnvironment!=cloudEnvironment))valid=false;
            string fingerprint=RemoteAssetValidation.Sha(Encoding.UTF8.GetBytes("1|"+Application.unityVersion+"|WebGL|"+QualitySettings.activeColorSpace+"|OpenGLES3"));
            var identity=new AssetReleaseIdentity(environment,config?.releaseId,Application.unityVersion,"WebGL",fingerprint,manifest?.localAssetSetHash,RemoteAssetValidation.Sha(themeFile.bytes),manifest?.fullAssetSetHash,expectedPaths);
            return new PresentationAssetProvider(valid?manifest:null,identity,config?.baseUrl,new UnityLocalAssetSource(local),transport,new TransactionalAssetCache(fileSystem),new UnityRemoteAssetBundleLoader(),new SystemAssetDelay(),source);
        }
        void OnAssetState(AssetPreparationSnapshot state){if(composition is DailyProductionComposition daily)daily.PlayerView?.SetAssetPreparation(state);}
        bool AtProfileEntry=>!destroyed&&foreground&&Controller!=null&&!Controller.IsBusy&&Controller.Snapshot==null&&!starting;
        void OnIdentityChanged(){if(silentLogin?.Status==WeChatLoginStatus.Authenticated)AdoptProfileAtEntry();}
        void OnProfileBoundary()
        {
            bool entry=AtProfileEntry;
            if(entry&&!wasProfileEntry)AdoptProfileAtEntry();
            wasProfileEntry=entry;
        }
        void AdoptProfileAtEntry()
        {
            if(!AtProfileEntry||localProfile==null||silentLogin?.Identity==null)return;
            try{if(localProfile.TryAdoptAccountAtEntry(silentLogin.Identity.AccountId,entryProfileTransport,true))
            {
                _=localProfile.SyncAsync();
                if(composition is DailyProductionComposition daily)
                {
                    var config=WeChatRuntimeConfig.LoadPackaged();var account=silentLogin.Identity.AccountId;
                    daily.ConfigureCollectionAuthority(new WeChatIngredientTradeService(config,account,daily.ApplyCollectionAuthority,new WeChatIngredientTradeBridge(),read:()=>daily.BrothCollection));
                    daily.ConfigureBrothActivity(new WeChatBrothActivityService(config,account,()=>daily.BrothCollection,daily.ApplyCollectionAuthority,new WeChatIngredientTradeBridge()),weChatServices?.ActivityLinks);
                }
            }}
            catch{Debug.LogWarning("Profile adoption unavailable; local facts preserved");}
        }
        public void CancelEntryStart(){startIntent=false;entryIntent++;assets?.Cancel();}
        public void SetAssetForeground(bool value){foreground=value;OnProfileBoundary();if(value)_=StartIfReadyAsync(entryIntent);}
        void OnAssetLifecycle(PlatformLifecycle state)
        {
            bool active=state==PlatformLifecycle.Foreground;
            SetAssetForeground(active);OnProfileBoundary();
            if(active&&silentLogin!=null)
            {
                if(silentLogin.Status!=WeChatLoginStatus.Authenticated&&silentLogin.Status!=WeChatLoginStatus.Authenticating)_=silentLogin.StartAsync();
                else if(localProfile!=null)_=localProfile.SyncAsync();
            }
        }
        async Task StartWithAssetsAsync()
        {
            if(destroyed||Controller==null||Controller.Snapshot!=null||preparingAssets||starting)return;
            startIntent=true;long intent=++entryIntent;preparingAssets=true;
            try
            {
                var state=assets.Snapshot;
                if(foreground&&composition is DailyProductionComposition entry)
                    entry.PlayerView?.NotifyEntryStartAccepted();
                await (activationFailed||state.State==AssetReadiness.Failed||state.State==AssetReadiness.Cancelled?assets.RetryAsync():assets.PrepareAsync());
                await StartIfReadyAsync(intent);
            }
            finally{preparingAssets=false;}
        }
        async Task StartIfReadyAsync(long intent)
        {
            if(destroyed||starting||!foreground||!startIntent||intent!=entryIntent||assets?.Snapshot.State!=AssetReadiness.Ready||Controller.Snapshot!=null)return;
            starting=true;startIntent=false;
            try
            {
                if(composition is DailyProductionComposition daily){daily.ActivateCompleteAssets();daily.PrepareCollectionSession();}
                activationFailed=false;
                // The existing time resolver runs only here, after the full release is active.
                await Controller.StartTodayAsync();
            }
            catch(Exception e){activationFailed=true;OnAssetState(new AssetPreparationSnapshot(assets.Snapshot.Generation,assets.Snapshot.ReleaseId,AssetReadiness.Failed,e is AssetFailure failure?failure.Code:AssetError.MissingAsset,0,0,0));}
            finally{starting=false;}
        }
        private void OnApplicationPause(bool paused) { platform?.NotifyEditorPause(paused); }
        private void OnDestroy()
        {
            destroyed = true;
            if(Controller!=null)Controller.ObservationChanged-=OnProfileBoundary;
            if(silentLogin!=null)silentLogin.Changed-=OnIdentityChanged;
            localProfile?.InvalidateSyncCallbacks();
            CancelEntryStart();
            try{Controller?.Dispose();}finally{try{if(composition is DailyProductionComposition daily)daily.ReleaseAssetConsumers();if(assets!=null){assets.Changed-=OnAssetState;PresentationAssets.Clear(assets);assets.Dispose();}if(platform!=null)platform.Changed-=OnAssetLifecycle;platform?.Dispose();}finally{profileTransport?.Dispose();silentLogin?.Dispose();weChatServices?.Dispose();weChatServices=null;}}
            if (owner == this) owner = null;
        }
    }
}
