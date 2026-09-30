using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace HotpotSort.Build
{
    // Frozen player contract, deliberately fail-closed: new dynamic loaders require a fresh audit.
    public static class PackageDependencyGuard
    {
        const string V7="Hotpot/TASK001/v7/r001";
        const string V3="Assets/HotpotSort/Resources/Hotpot/TASK001/v3/r001";
        const string PlayerContract="335223c53c9dfd729d8af660c6dd98fc6adc3b7ff68b66742443cda2d8e0b7f9";
        const string ThemeHash="a36382e366d3d80de8b83c2bc6dc9afa1df62f6c7679085a0231881a11ff5242";
        [Serializable] public sealed class Entry { public string path,guid,sha256,reason;public long bytes; }
        [Serializable] public sealed class Proof { public string status,sourceProject,playerContract;public bool staged;public Entry[] inputs,mustKeep;public string[] scenes,dependencies,cases;public int textureProbes,alphaProbes; }
        [Serializable] sealed class Theme {public string resourceRoot;public Asset[] assets;}
        [Serializable] sealed class Asset {public string key,resourceAddress;}
        static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        static string Normalize(string p)=>p.Replace('\\','/');
        static string Hash(byte[] b){using(var s=SHA256.Create())return BitConverter.ToString(s.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
        static string Digest(string p)=>Hash(File.ReadAllBytes(p));
        static bool IsPlayer(string p)=>p.EndsWith(".cs",StringComparison.Ordinal)&&!p.Contains("/Editor/")&&!p.Contains("Diagnostics~");
        public static bool Excludable(string p)=>p==V3+".meta"||p.StartsWith(V3+"/",StringComparison.Ordinal);
        static void ValidateRoot(string root){Require(root==V7,"Unknown/changed player visual root");}
        static void Reject(Action f){bool rejected=false;try{f();}catch(InvalidOperationException){rejected=true;}Require(rejected,"Negative guard accepted invalid input");}
        public static void Run()
        {
            int exit=0;string output=Environment.GetEnvironmentVariable("HOTPOT_PACKAGE_PROOF");
            try
            {
                Require(!string.IsNullOrEmpty(output),"HOTPOT_PACKAGE_PROOF required");
                bool staged=Environment.GetEnvironmentVariable("HOTPOT_PACKAGE_STAGED")=="1";
                var proof=Validate(staged);
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
                File.WriteAllText(output,JsonUtility.ToJson(proof,true),new UTF8Encoding(false));
                Debug.Log("TASK002_PACKAGE_DEPENDENCIES_PASS keep="+proof.mustKeep.Length+" textures="+proof.textureProbes+" alpha="+proof.alphaProbes);
            }
            catch(Exception ex){exit=1;Debug.LogError("TASK002_PACKAGE_DEPENDENCIES_FAILED "+ex.Message);}
            finally{if(Application.isBatchMode)EditorApplication.Exit(exit);}
        }
        public static void RunNative()
        {
            int exit=0;
            try{
                var rows=Task002V10BundleTool.ValidateNativeResources();
                var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
                Require(scenes.SequenceEqual(new[]{"Assets/HotpotSort/Scenes/Boot.unity"}),"Native scene contract changed");
                var dependencies=AssetDatabase.GetDependencies(scenes,true);Require(!dependencies.Any(Excludable),"Native scene reaches historical excluded resources");
                string output=Environment.GetEnvironmentVariable("HOTPOT_PACKAGE_PROOF");Require(!string.IsNullOrEmpty(output),"HOTPOT_PACKAGE_PROOF required");
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
                File.WriteAllText(output,JsonUtility.ToJson(new Proof{status="PASS",sourceProject=Path.GetFullPath("."),staged=false,mustKeep=rows.Select(r=>MakeEntry(r.path,"Complete native Resources whitelist")).ToArray(),scenes=scenes,dependencies=dependencies,textureProbes=rows.Length,alphaProbes=32,cases=new[]{"Complete native theme inventory","Exactly four TASK031 assets","32 readable foods","No historical bundle marker","Enabled scene dependency closure"}},true));
                Debug.Log("TASK031_NATIVE_PACKAGE_PASS textures="+rows.Length+" activity=4");
            }catch(Exception ex){exit=1;Debug.LogError("TASK031_NATIVE_PACKAGE_FAILED "+ex);}
            finally{if(Application.isBatchMode)EditorApplication.Exit(exit);}
        }
        public static Proof Validate(bool staged)
        {
            var cases=new List<string>();
            var player=Directory.GetFiles("Assets/HotpotSort","*.cs",SearchOption.AllDirectories).Select(Normalize).Where(IsPlayer).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
            string contract=Hash(Encoding.UTF8.GetBytes(string.Join("\n",player.Select(p=>p+"|"+Digest(p)))));
            Require(contract==PlayerContract,"Player source/dynamic-load contract changed; audit required");cases.Add("Frozen complete player contract incl. icon skin feedback load paths");
            foreach(var p in player)Require(!Regex.IsMatch(File.ReadAllText(p),@"Resources\.LoadAll|fonts/(readable|display)|CreateDynamicFontFromOSFont|GetBuiltinResource<Font>"),"Unapproved load-all or legacy/system font dependency");
            var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
            Require(scenes.Length==1&&scenes[0]=="Assets/HotpotSort/Scenes/Boot.unity","Enabled scene contract changed");
            var sceneText=File.ReadAllText(scenes[0]);var match=Regex.Match(sceneText,@"(?m)^\s*approvedAssetRoot:\s*(\S+)\s*$");Require(match.Success,"Boot visual root missing");ValidateRoot(match.Groups[1].Value);
            var dependencies=AssetDatabase.GetDependencies(scenes,true);Require(!dependencies.Any(Excludable),"Enabled scene dependency reaches v3");cases.Add("AssetDatabase recursive enabled-scene dependency closure");
            var keep=new Dictionary<string,string>(StringComparer.Ordinal);
            Action<string,string> add=(p,reason)=>{Require(File.Exists(p),"Required file missing: "+p);Require(!Excludable(p),"Must-keep path intersects v3");keep[p]=reason;};
            foreach(var p in dependencies)if(File.Exists(p))add(p,"Enabled-scene recursive AssetDatabase dependency");
            foreach(var p in Directory.GetFiles("Assets","*",SearchOption.AllDirectories).Select(Normalize).Where(p=>p.EndsWith(".unity")||p.EndsWith(".prefab")))
            {
                foreach(Match g in Regex.Matches(File.ReadAllText(p),@"guid: ([a-f0-9]{32})"))
                {if(g.Groups[1].Value.StartsWith("0000000000000000",StringComparison.Ordinal))continue;string target=AssetDatabase.GUIDToAssetPath(g.Groups[1].Value);Require(!string.IsNullOrEmpty(target)&&File.Exists(target),"Unresolved scene/prefab reference");Require(!Excludable(target),"Scene/prefab reference reaches v3");}
            }
            cases.Add("All scene/prefab references resolved and outside excluded family");
            string themePath="Assets/HotpotSort/Resources/"+V7+"/presentation-theme.json";
            Require(Digest(themePath)==ThemeHash,"Theme paths/aliases changed; audit required");var theme=JsonUtility.FromJson<Theme>(File.ReadAllText(themePath));ValidateRoot(theme.resourceRoot);add(themePath,"Frozen full theme and aliases");
            int textures=0,alpha=0;
            foreach(var asset in theme.assets)
            {
                Require(asset.resourceAddress.StartsWith(V7+"/",StringComparison.Ordinal),"Unknown dynamic theme root");
                var t=Resources.Load<Texture2D>(asset.resourceAddress);Require(t,"Missing theme texture: "+asset.key);textures++;
                add(AssetDatabase.GetAssetPath(t),"Theme texture / icon / skin / feedback alias: "+asset.key);
                foreach(var d in AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(t),true))if(File.Exists(d))add(d,"Recursive dynamic texture dependency");
                if(asset.key.StartsWith("food.",StringComparison.Ordinal))
                {Require(t.isReadable,"Food Alpha must be readable");var pixels=t.GetPixels32();Require(pixels.Length==t.width*t.height&&pixels.Any(p=>p.a>0),"Food Alpha probe failed");alpha++;}
            }
            foreach(string address in Enumerable.Range(0,16).Select(i=>"Hotpot/food_"+i.ToString("00")).Concat(new[]{"Hotpot/plate"}))
            {var t=Resources.Load<Texture2D>(address);Require(t,"Root Awake texture missing");add(AssetDatabase.GetAssetPath(t),"Unconditional GameplayView.Awake root load");textures++;}
            string fonts="Assets/HotpotSort/Resources/Hotpot/TASK002/v9/r001/fonts/";
            foreach(string name in new[]{"modern-sans.otf","glyphs.txt","font-manifest.json","OFL.txt"})add(fonts+name,"Runtime font / glyph coverage / export manifest and license");
            add("Assets/HotpotSort/Content/Daily/hotpot_daily_task001_v5_fixed_c_1.json","Boot bound production content");
            add("Assets/HotpotSort/Resources/"+V7+"/hero/share.png","Static WeChat share source");
            add("Assets/HotpotSort/WeChatOpenData/index.js","Formal exported open-data source");
            FontSubsetBuildGuard.Validate();WeChatExportV9.ValidateOpenDataSource();
            Require(alpha==16&&textures==89,"Expected full theme 72 + root 17 texture/16 Alpha probes");
            cases.Add("89 dynamic texture probes incl all aliases and 16 readable food Alpha");cases.Add("Font glyph/license/hash and formal open-data source guards");
            if(staged)Require(!Directory.Exists(V3)&&!File.Exists(V3+".meta"),"Historical family survived staging");
            Reject(()=>ValidateRoot("Hotpot/TASK001/v3/r001"));Reject(()=>ValidateRoot("Hotpot/unknown"));Reject(()=>Require(!Excludable(V3+"/food/food_00.png"),"excluded dependency"));
            Require(!Excludable("Assets/HotpotSort/Resources/Hotpot/TASK001/v3/r001-other/food.png"),"Prefix escapes family");cases.Add("Changed roots/excluded dependency reject; adjacent paths protected");
            var entries=new List<Entry>();
            foreach(string folder in new[]{"Assets","Packages","ProjectSettings"})foreach(string file in Directory.GetFiles(folder,"*",SearchOption.AllDirectories).Select(Normalize).OrderBy(p=>p,StringComparer.Ordinal))
            {if(Regex.IsMatch(file,@"/__pycache__/|\.pyc$|/Diagnostics~/bin/|/Diagnostics~/obj/"))continue;entries.Add(MakeEntry(file,keep.TryGetValue(file,out var reason)?reason:"Source integrity inventory"));}
            return new Proof{status="PASS",sourceProject=Path.GetFullPath("."),playerContract=contract,staged=staged,inputs=entries.ToArray(),mustKeep=keep.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>MakeEntry(x.Key,x.Value)).ToArray(),scenes=scenes,dependencies=dependencies,cases=cases.ToArray(),textureProbes=textures,alphaProbes=alpha};
        }
        static Entry MakeEntry(string p)
        {return MakeEntry(p,"");}
        static Entry MakeEntry(string p,string reason)
        {return new Entry{path=p,guid=p.StartsWith("Assets/",StringComparison.Ordinal)?AssetDatabase.AssetPathToGUID(p):"",sha256=Digest(p),bytes=new FileInfo(p).Length,reason=reason};}
    }
}
