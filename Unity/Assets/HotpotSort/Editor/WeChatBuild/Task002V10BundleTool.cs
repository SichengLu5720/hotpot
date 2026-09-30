using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace HotpotSort.Build
{
    public static class Task002V10BundleTool
    {
        public static string LogicalRoot
        {
            get
            {
                var scene=File.ReadAllText("Assets/HotpotSort/Scenes/Boot.unity");
                var match=System.Text.RegularExpressions.Regex.Match(scene,@"(?m)^\s*approvedAssetRoot:\s*(\S+)\s*$");
                Require(match.Success&&System.Text.RegularExpressions.Regex.IsMatch(match.Groups[1].Value,@"\AHotpot/TASK001/v[0-9]+/r[0-9]+\z"),"Invalid selected theme root");
                return match.Groups[1].Value+"/";
            }
        }
        public static string Root=>"Assets/HotpotSort/Resources/"+LogicalRoot;
        public const string MovedRoot="Assets/HotpotSort/RemoteAssetBundles/v10/";
        public const string Marker="Assets/HotpotSort/RemoteAssetBundles/v10/staging-plan.json";
        [Serializable] public sealed class AssetEntry {public string logicalPath,bundleAssetName,type;}
        [Serializable] public sealed class BundleEntry {public string relativePath,sha256;public long byteLength;public uint crc32;}
        [Serializable] public sealed class Release {public int schemaVersion=1;public string releaseId,unityVersion,buildTarget,compatibilityFingerprint;public BundleEntry bundle;public AssetEntry[] assets;public string localAssetSetHash,themeSha256,fullAssetSetHash;}
        [Serializable] public sealed class FileEntry {public string path,guid,sha256,metaSha256,logicalPath,destination;public long bytes;public bool remote;}
        [Serializable] public sealed class Plan {public int schemaVersion=1;public string sourceProject,unityVersion,themeSha256;public FileEntry[] assets;public string[] dependencies;}
        [Serializable] sealed class Theme {public string resourceRoot;public ThemeAsset[] assets;}
        [Serializable] sealed class ThemeAsset {public string key,resourceAddress;}
        [Serializable] public sealed class Metadata {public string logicalPath,format,filter,wrap,alphaSha256,importerSha256;public int width,height;public bool readable;public float[] rect,pivot,border;public float ppu;}
        [Serializable] public sealed class Equivalence {public string status,firstHash,secondHash;public bool deterministic;public int remoteCount,localCount,alphaProbes;public Metadata[] source,bundle;public string[] cases;}
        public static string Hash(byte[] bytes){using(var s=SHA256.Create())return BitConverter.ToString(s.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        public static string Digest(string p)=>Hash(File.ReadAllBytes(p));
        static void Require(bool ok,string why){if(!ok)throw new InvalidOperationException(why);}
        static string Slash(string s)=>s.Replace('\\','/');
        public static bool IsRemote(string relative)=>relative.StartsWith("food/",StringComparison.Ordinal)||relative.StartsWith("containers/",StringComparison.Ordinal)||relative.StartsWith("pots/",StringComparison.Ordinal)||relative.StartsWith("fx/",StringComparison.Ordinal)||relative=="hero/win";
        static int FoodCount=>LogicalRoot=="Hotpot/TASK001/v10/r001/"?32:16;
        public static readonly string[] BrothResources={"Hotpot/TASK031/Broths/clear","Hotpot/TASK031/Broths/tomato","Hotpot/TASK031/Broths/mushroom","Hotpot/TASK031/UI/broth_activity_entry"};
        // Current delivery uses complete native Resources, never the historical split.
        public static FileEntry[] ValidateNativeResources()
        {
            Require(LogicalRoot=="Hotpot/TASK001/v10/r001/","Unexpected native theme");
            Require(!File.Exists(Marker),"Historical split marker cannot enter native delivery");
            var theme=JsonUtility.FromJson<Theme>(File.ReadAllText(Root+"presentation-theme.json"));
            Require(theme.resourceRoot==LogicalRoot.TrimEnd('/'),"Native theme root mismatch");
            var addresses=theme.assets.Select(a=>a.resourceAddress).Distinct().ToArray();
            var pngs=Directory.GetFiles(Root,"*.png",SearchOption.AllDirectories).Select(p=>Slash(p).Substring("Assets/HotpotSort/Resources/".Length)).Select(p=>p.Substring(0,p.Length-4)).ToArray();
            Require(addresses.OrderBy(p=>p).SequenceEqual(pngs.OrderBy(p=>p)),"Native theme texture inventory mismatch");
            var extras=Directory.GetFiles("Assets/HotpotSort/Resources/Hotpot/TASK031","*.png",SearchOption.AllDirectories).Select(p=>Slash(p).Substring("Assets/HotpotSort/Resources/".Length)).Select(p=>p.Substring(0,p.Length-4));
            Require(extras.OrderBy(p=>p).SequenceEqual(BrothResources.OrderBy(p=>p)),"Activity whitelist must contain exactly four formal assets");
            var rows=new List<FileEntry>();
            foreach(string address in addresses.Concat(BrothResources)){
                string path="Assets/HotpotSort/Resources/"+address+".png";var texture=Resources.Load<Texture2D>(address);
                Require(texture&&AssetDatabase.GetAssetPath(texture)==path&&File.Exists(path+".meta"),"Native resource missing: "+address);
                Require(AssetDatabase.GetDependencies(path,true).All(p=>p==path),"Unexpected native texture dependency: "+address);
                rows.Add(new FileEntry{path=path,destination=path,logicalPath=address,guid=AssetDatabase.AssetPathToGUID(path),sha256=Digest(path),metaSha256=Digest(path+".meta"),bytes=new FileInfo(path).Length,remote=false});
            }
            for(int i=0;i<32;i++){var texture=Resources.Load<Texture2D>(LogicalRoot+"food/food_"+i.ToString("00"));Require(texture&&texture.isReadable,"Native food alpha missing");}
            return rows.ToArray();
        }
        public sealed class NativeResourceBuildGuard:UnityEditor.Build.IPreprocessBuildWithReport
        {
            public int callbackOrder=>0;
            public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report){if(LogicalRoot=="Hotpot/TASK001/v10/r001/")ValidateNativeResources();}
        }
        static string[] ExpectedRemote()=>Enumerable.Range(0,FoodCount).Select(i=>"food/food_"+i.ToString("00")).Concat(new[]{"containers/plate_main","containers/dish_buffer","pots/body","pots/broth","pots/rim","pots/unlit","hero/win"}).ToArray();
        static string Output(){var p=Environment.GetEnvironmentVariable("HOTPOT_V10_OUTPUT");Require(!string.IsNullOrWhiteSpace(p),"HOTPOT_V10_OUTPUT required");Directory.CreateDirectory(p);return Path.GetFullPath(p);}
        static void Write(string p,object value)=>File.WriteAllText(p,JsonUtility.ToJson(value,true),new UTF8Encoding(false));
        static void Execute(Action action){int exit=0;try{action();}catch(Exception e){exit=1;Debug.LogError("TASK002_V10_FAILED "+e);}finally{if(Application.isBatchMode)EditorApplication.Exit(exit);}}
        public static void PlanSource()=>Execute(()=>{
            string output=Output();var assets=Directory.GetFiles(Root,"*.png",SearchOption.AllDirectories).Select(Slash).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
            Require(assets.Length>0,"No theme PNGs");var theme=JsonUtility.FromJson<Theme>(File.ReadAllText(Root+"presentation-theme.json"));Require(theme.resourceRoot==LogicalRoot.TrimEnd('/'),"Theme root mismatch");
            var rows=assets.Select(p=>{string relative=p.Substring(Root.Length,p.Length-Root.Length-4);return new FileEntry{path=p,guid=AssetDatabase.AssetPathToGUID(p),sha256=Digest(p),metaSha256=Digest(p+".meta"),bytes=new FileInfo(p).Length,logicalPath=LogicalRoot+relative,remote=IsRemote(relative),destination=IsRemote(relative)?MovedRoot+relative+".png":p};}).ToArray();
            Require(rows.Any(x=>x.remote)&&rows.Any(x=>!x.remote),"Theme must have local and remote assets");
            foreach(string key in Enumerable.Range(0,FoodCount).Select(i=>"food."+i.ToString("00")).Concat(new[]{"plate.main","dish.buffer","pot.body","pot.broth","pot.rim","pot.unlit","hero.win"}))
                Require(theme.assets.Any(a=>a.key==key&&rows.Any(x=>x.remote&&x.logicalPath==a.resourceAddress)),"Required remote key absent: "+key);
            Require(theme.assets.Select(x=>x.resourceAddress).Distinct().OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(rows.Select(x=>x.logicalPath).OrderBy(x=>x,StringComparer.Ordinal)),"Theme paths/aliases do not cover exact inventory");
            var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();Require(scenes.SequenceEqual(new[]{"Assets/HotpotSort/Scenes/Boot.unity"}),"Build scene changed");
            var dependencies=AssetDatabase.GetDependencies(scenes,true);Require(!dependencies.Any(d=>rows.Any(x=>x.remote&&x.path==d)),"Remote PNG directly referenced by bootstrap dependency");
            foreach(var row in rows)Require(AssetDatabase.GetDependencies(row.path,true).All(d=>d==row.path||d.StartsWith("Resources/",StringComparison.Ordinal)),"Unexpected PNG dependency");
            Write(Path.Combine(output,"source-plan.json"),new Plan{sourceProject=Path.GetFullPath("."),unityVersion=Application.unityVersion,themeSha256=Digest(Root+"presentation-theme.json"),assets=rows,dependencies=dependencies});
            Write(Path.Combine(output,"source-metadata.json"),new Equivalence{status="BASELINE",remoteCount=rows.Count(x=>x.remote),localCount=rows.Count(x=>!x.remote),source=rows.Select(x=>Capture(x.path,x.logicalPath,null)).ToArray()});
            Debug.Log("TASK002_V10_PLAN_PASS remote="+rows.Count(x=>x.remote)+" local="+rows.Count(x=>!x.remote));
        });
        static Metadata Capture(string path,string logical,AssetBundle bundle)
        {
            var texture=bundle?bundle.LoadAsset<Texture2D>(logical.ToLowerInvariant()):AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var sprite=bundle?bundle.LoadAsset<Sprite>(logical.ToLowerInvariant()):AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Require(texture&&sprite,"PNG must resolve as Texture2D and Sprite: "+logical);
            string alpha="";if(logical.Contains("/food/")){Require(texture.isReadable,"Food is not readable");alpha=Hash(texture.GetPixels32().Select(p=>p.a).ToArray());}
            var r=sprite.rect;var p=sprite.pivot;var b=sprite.border;
            return new Metadata{logicalPath=logical,width=texture.width,height=texture.height,format=texture.format.ToString(),filter=texture.filterMode.ToString(),wrap=texture.wrapMode.ToString(),readable=texture.isReadable,rect=new[]{r.x,r.y,r.width,r.height},pivot=new[]{p.x,p.y},border=new[]{b.x,b.y,b.z,b.w},ppu=sprite.pixelsPerUnit,alphaSha256=alpha,importerSha256=File.Exists(path+".meta")?Digest(path+".meta"):""};
        }
        public static void BuildRemote()=>Execute(()=>{
            string output=Output();Require(File.Exists(Marker),"Only a prepared isolated v10 project can build remote assets");var plan=JsonUtility.FromJson<Plan>(File.ReadAllText(Marker));
            Require(plan.unityVersion==Application.unityVersion&&EditorUserBuildSettings.activeBuildTarget==BuildTarget.WebGL,"Toolchain/target mismatch");
            foreach(var x in plan.assets){Require(File.Exists(x.destination)&&Digest(x.destination)==x.sha256&&Digest(x.destination+".meta")==x.metaSha256,"Source/importer bytes changed");if(x.remote)Require(!File.Exists(x.path)&&!Resources.Load<Texture2D>(x.logicalPath),"Remote resource still duplicated");else Require(Resources.Load<Texture2D>(x.logicalPath),"Bootstrap PNG unavailable");}
            var remote=plan.assets.Where(x=>x.remote).OrderBy(x=>x.logicalPath,StringComparer.Ordinal).ToArray();
            var baseline=remote.Select(x=>Capture(x.destination,x.logicalPath,null)).ToArray();
            const string bundleName="hotpot-remote.bundle";var build=new AssetBundleBuild{assetBundleName=bundleName,assetNames=remote.Select(x=>x.destination).ToArray(),addressableNames=remote.Select(x=>x.logicalPath.ToLowerInvariant()).ToArray()};
            var options=BuildAssetBundleOptions.DeterministicAssetBundle|BuildAssetBundleOptions.ForceRebuildAssetBundle|BuildAssetBundleOptions.StrictMode;
            var hashes=new List<string>();
            foreach(string round in new[]{"round1","round2"}){string dir=Path.Combine(output,round);Require(!Directory.Exists(dir),"Never overwrite bundle evidence");Directory.CreateDirectory(dir);var result=BuildPipeline.BuildAssetBundles(dir,new[]{build},options,BuildTarget.WebGL);Require(result,"AssetBundle build failed");hashes.Add(Digest(Path.Combine(dir,bundleName)));}
            Require(hashes[0]==hashes[1],"Nondeterministic bundle bytes");string bundleFile=Path.Combine(output,"round1",bundleName);Require(BuildPipeline.GetCRCForAssetBundle(bundleFile,out uint crc),"Bundle CRC unavailable");
            var loaded=AssetBundle.LoadFromFile(bundleFile,crc);Require(loaded,"WebGL bundle cannot load for metadata equivalence in current Editor");Metadata[] actual;
            try{Require(loaded.GetAllAssetNames().OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(remote.Select(x=>x.logicalPath.ToLowerInvariant()).OrderBy(x=>x,StringComparer.Ordinal)),"Unexpected assets/dependencies in bundle");actual=remote.Select(x=>Capture(x.destination,x.logicalPath,loaded)).ToArray();for(int i=0;i<actual.Length;i++)Require(JsonUtility.ToJson(actual[i])==JsonUtility.ToJson(baseline[i]),"Bundle metadata/Alpha mismatch: "+actual[i].logicalPath);}finally{loaded.Unload(true);}
            string SetHash(IEnumerable<FileEntry> entries)=>Hash(Encoding.UTF8.GetBytes(string.Join("\n",entries.OrderBy(x=>x.logicalPath,StringComparer.Ordinal).Select(x=>x.logicalPath+"|"+x.sha256+"|"+x.metaSha256))));
            string local=SetHash(plan.assets.Where(x=>!x.remote)),full=SetHash(plan.assets),fingerprint=Hash(Encoding.UTF8.GetBytes("1|"+Application.unityVersion+"|WebGL|"+PlayerSettings.colorSpace+"|"+string.Join(",",PlayerSettings.GetGraphicsAPIs(BuildTarget.WebGL).Select(x=>x.ToString()))));
            string release=Hash(Encoding.UTF8.GetBytes("1|"+fingerprint+"|"+full+"|"+local+"|"+plan.themeSha256));
            var manifest=new Release{releaseId=release,unityVersion=Application.unityVersion,buildTarget="WebGL",compatibilityFingerprint=fingerprint,bundle=new BundleEntry{relativePath=release+"/"+bundleName,byteLength=new FileInfo(bundleFile).Length,sha256=hashes[0],crc32=crc},assets=remote.Select(x=>new AssetEntry{logicalPath=x.logicalPath,bundleAssetName=x.logicalPath.ToLowerInvariant(),type="Texture2D"}).ToArray(),localAssetSetHash=local,themeSha256=plan.themeSha256,fullAssetSetHash=full};
            string deployment=Path.Combine(output,"deployment",release);Directory.CreateDirectory(deployment);File.Copy(bundleFile,Path.Combine(deployment,bundleName),false);Write(Path.Combine(deployment,"manifest.json"),manifest);
            Write(Path.Combine(output,"equivalence.json"),new Equivalence{status="PASS",firstHash=hashes[0],secondHash=hashes[1],deterministic=true,remoteCount=remote.Length,localCount=plan.assets.Length-remote.Length,alphaProbes=FoodCount,source=baseline,bundle=actual,cases=new[]{"Exact theme inventory split","Remote absent from Resources","PNG/meta/GUID preserved","Two deterministic WebGL bundles","Texture and Sprite metadata equivalence",FoodCount+" complete Alpha planes match","No unexpected bundle assets"}});
            Debug.Log("TASK002_V10_BUNDLE_PASS remote="+remote.Length+" local="+(plan.assets.Length-remote.Length)+" deterministic=true bytes="+manifest.bundle.byteLength);
        });
        [Serializable] sealed class SizeResult {public string status,output,dataSha256,wasmSha256;public long dataBytes,wasmBytes,combinedBytes,limitBytes=30408704;public bool belowLimit,realSdkConversion=false,playerFlowIntegrated=false;}
        public static void BuildBootstrapSize()=>Execute(()=>{
            string output=Output();Require(File.Exists(Marker),"Size build requires isolated remote-resource staging");
            var plan=JsonUtility.FromJson<Plan>(File.ReadAllText(Marker));foreach(var a in plan.assets)Require(a.remote?!File.Exists(a.path):File.Exists(a.path),"Bootstrap split changed");
            // Match DoExport's supported SDK setup before building the isolated bootstrap.
            // Fresh package importer defaults are not the authoritative panel configuration.
            bool configuredGlx=WeChatWASM.WXConvertCore.config.CompileOptions.enableEmscriptenGLX;
            WeChatWASM.WXConvertCore.PreInit();
            Require(WeChatWASM.WXConvertCore.config.CompileOptions.enableEmscriptenGLX==configuredGlx,"SDK initialization changed the selected rendering mode");
            Debug.Log("TASK002_V10_SDK_PREINIT glx="+configuredGlx+" compression="+PlayerSettings.WebGL.compressionFormat);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=Path.Combine(output,"webgl"),target=BuildTarget.WebGL,options=BuildOptions.None});
            Require(report.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded,"Bootstrap player build failed");
            string data=Directory.GetFiles(output,"*.data",SearchOption.AllDirectories).Single(),wasm=Directory.GetFiles(output,"*.wasm",SearchOption.AllDirectories).Single();
            string dataBr=Path.Combine(output,"bootstrap.data.br"),wasmBr=Path.Combine(output,"bootstrap.wasm.br");
            Require(WeChatWASM.WXConvertCore.MultiThreadBrotliCompress(data,dataBr)&&WeChatWASM.WXConvertCore.MultiThreadBrotliCompress(wasm,wasmBr),"SDK Brotli compression failed");
            long db=new FileInfo(dataBr).Length,wb=new FileInfo(wasmBr).Length;
            Write(Path.Combine(output,"size-result.json"),new SizeResult{status="BUILT_SIZE_PROBE",output=output,dataSha256=Digest(data),wasmSha256=Digest(wasm),dataBytes=db,wasmBytes=wb,combinedBytes=db+wb,belowLimit=db+wb<=30408704});
            Require(db+wb<=30408704,"Bootstrap exceeds SDK Data/Wasm package limit");
            Debug.Log("TASK002_V10_SIZE_PASS data="+db+" wasm="+wb+" combined="+(db+wb)+" limit=30408704");
        });
    }
}
