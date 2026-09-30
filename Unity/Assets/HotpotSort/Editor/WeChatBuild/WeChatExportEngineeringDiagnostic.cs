using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HotpotSort.Platform;
using UnityEditor;
using UnityEngine;
using WeChatWASM;

namespace HotpotSort.Build
{
    public static class WeChatExportEngineeringDiagnostic
    {
        private static readonly List<string> cases=new List<string>();
        private static void Check(bool ok,string message) { if(!ok) throw new InvalidOperationException(message); }
        private static void Case(string name,Action action) { action();cases.Add(name); }
        private static void Reject(Action action) { bool rejected=false;try{action();}catch{rejected=true;}Check(rejected,"Invalid input was accepted"); }
        public static void Run()
        {
            var evidence=Environment.GetEnvironmentVariable("HOTPOT_TASK002_EXPORT_QA_OUTPUT");
            if(string.IsNullOrWhiteSpace(evidence))throw new ArgumentException("HOTPOT_TASK002_EXPORT_QA_OUTPUT is required");
            evidence=Path.GetFullPath(evidence);Directory.CreateDirectory(evidence);
            var fixture=Path.Combine(evidence,"controlled-package");
            var envNames=new[]{"WECHAT_APP_ID","WECHAT_OPEN_DATA_SOURCE"};
            var originals=envNames.ToDictionary(k=>k,Environment.GetEnvironmentVariable);
            int exit=0;
            try
            {
                Case("CFG01 Missing optional identifiers are NotConfigured",()=>{var c=WeChatRuntimeConfig.FromEnvironment(_=>null);Check(c.CloudState==WeChatCapabilityState.NotConfigured && c.RewardedVideoState==WeChatCapabilityState.NotConfigured,"Optional state");});
                Case("CFG02 Real routing configured and disabled flags",()=>{var data=new Dictionary<string,string>{{"WECHAT_CLOUD_ENV_ID","fixture-cloud"},{"WECHAT_REWARDED_AD_UNIT_ID","fixture-ad"},{"WECHAT_ENABLE_SHARE","false"}};var c=WeChatRuntimeConfig.FromEnvironment(k=>data.TryGetValue(k,out var v)?v:null);Check(c.CloudState==WeChatCapabilityState.Ready && c.RewardedVideoState==WeChatCapabilityState.Ready && c.ShareState==WeChatCapabilityState.Disabled,"Feature flags");});
                Case("CFG03 Malformed flags identifiers versions reject",()=>{Reject(()=>WeChatRuntimeConfig.FromEnvironment(k=>k=="WECHAT_ENABLE_SHARE"?"unknown":null));Reject(()=>WeChatRuntimeConfig.FromEnvironment(k=>k=="WECHAT_CLOUD_ENV_ID"?"bad\nvalue":null));Reject(()=>WeChatRuntimeConfig.FromJson("{}"));Reject(()=>WeChatRuntimeConfig.FromJson("{\"schemaVersion\":2,\"protocolVersion\":1}"));});
                Case("CFG04 Missing package fails closed",()=>{var c=WeChatRuntimeConfig.Unavailable();Check(!c.enableLogin && !c.enableShare && c.LoginState==WeChatCapabilityState.NotConfigured,"Missing package");});
                Case("CFG05 Share path traversal rejects",()=>{foreach(var path in new[]{"../secret","/absolute","http://remote/file","a/../b","a\\b"})Check(!WeChatRuntimeConfig.IsPackagePath(path),"Path accepted");});
                Case("CFG07 All disabled capabilities remain disabled",()=>{var c=WeChatRuntimeConfig.FromEnvironment(k=>k.StartsWith("WECHAT_ENABLE_",StringComparison.Ordinal)?"0":null);Check(c.LoginState==WeChatCapabilityState.Disabled&&c.FriendBoardState==WeChatCapabilityState.Disabled&&c.CloudState==WeChatCapabilityState.Disabled&&c.RewardedVideoState==WeChatCapabilityState.Disabled&&c.ShareState==WeChatCapabilityState.Disabled,"Disabled capability became ready");});
                Case("CFG08 Invalid environment and unsafe packaged path reject",()=>{Reject(()=>WeChatRuntimeConfig.FromEnvironment(k=>k=="WECHAT_ENVIRONMENT"?"unknown":null));Reject(()=>WeChatRuntimeConfig.FromJson("{\"schemaVersion\":1,\"protocolVersion\":1,\"shareImagePath\":\"../outside.png\"}"));var c=WeChatRuntimeConfig.Unavailable();Check(!c.enableCloud&&!c.enableRewardedVideo&&!c.enableFriendBoard,"Unavailable package enabled service");});
                Environment.SetEnvironmentVariable("WECHAT_OPEN_DATA_SOURCE",null);
                Case("OD01 Default resolves formal TASK008 source",()=>{Check(Path.GetFullPath(WeChatExportV9.OpenDataSource())==Path.GetFullPath("Assets/HotpotSort/WeChatOpenData"),"Default source");WeChatExportV9.ValidateOpenDataSource();});
                Case("OD02 External formal source override",()=>{Environment.SetEnvironmentVariable("WECHAT_OPEN_DATA_SOURCE",Path.GetFullPath("Assets/HotpotSort/WeChatOpenData"));WeChatExportV9.ValidateOpenDataSource();});
                Case("CFG06 Missing AppID blocks real export preflight",()=>{Environment.SetEnvironmentVariable("WECHAT_APP_ID",null);Reject(()=>WeChatExportV9.Preflight(new WeChatRuntimeConfig()));});
                var panel=WXConvertCore.config;
                string panelPath=AssetDatabase.GetAssetPath(panel);
                byte[] panelBefore=File.ReadAllBytes(panelPath);
                Case("SDK01 Friend relation clone does not change source panel",()=>{
                    bool original=panel.SDKOptions.UseFriendRelation;
                    var clone=UnityEngine.Object.Instantiate(panel);
                    try {clone.SDKOptions.UseFriendRelation=!original;clone.ProjectConf.Appid=string.Empty;Check(panel.SDKOptions.UseFriendRelation==original,"Clone aliased source");Check(panelBefore.SequenceEqual(File.ReadAllBytes(panelPath)),"Panel bytes changed");}
                    finally{UnityEngine.Object.DestroyImmediate(clone);}
                });
                Directory.CreateDirectory(fixture);Directory.CreateDirectory(Path.Combine(fixture,"open-data"));
                File.WriteAllText(Path.Combine(fixture,"project.config.json"),"{\"compileType\":\"game\",\"appid\":\"\",\"projectname\":\"ControlledFixture\"}");
                File.WriteAllText(Path.Combine(fixture,"game.json"),"{\"openDataContext\":\"open-data\"}");
                File.WriteAllText(Path.Combine(fixture,"game.js"),"require('./module')");
                File.WriteAllText(Path.Combine(fixture,"module.js"),"module.exports = {};");
                File.WriteAllText(Path.Combine(fixture,"open-data","sdk-sample.js"),"/* controlled obsolete SDK sample */");
                // Synthetic syntax-valid identity is confined to this fixture and cleared before return.
                Environment.SetEnvironmentVariable("WECHAT_APP_ID","wx"+new string('0',16));
                var config=new WeChatRuntimeConfig();
                Case("PKG01 Stage config share image licenses and exact formal domain",()=>WeChatExportV9.StageRuntimeFiles(fixture,config));
                Case("PKG02 SDK sample removed from runtime and formally replaced",()=>{Check(!File.Exists(Path.Combine(fixture,"open-data","sdk-sample.js")),"SDK sample survived");WeChatExportV9.ValidateFormalDomain(Path.Combine(fixture,"open-data"));});
                Case("PKG03 Runtime config JSON matches switches and no fake capability",()=>{var c=WeChatRuntimeConfig.FromJson(File.ReadAllText(Path.Combine(fixture,WeChatRuntimeConfig.PackageFile)));Check(c.CloudState==WeChatCapabilityState.NotConfigured && c.RewardedVideoState==WeChatCapabilityState.NotConfigured && c.enableFriendBoard,"Packaged capabilities");});
                Case("PKG04 Module refs and friend relation entry validate",()=>WeChatExportV9.ValidatePackage(fixture,config));
                Case("PKG05 Missing module fails",()=>{File.WriteAllText(Path.Combine(fixture,"game.js"),"require('./missing')");Reject(()=>WeChatExportV9.ValidatePackage(fixture,config));File.WriteAllText(Path.Combine(fixture,"game.js"),"require('./module')");});
                Case("PKG06 Escaping module fails",()=>{File.WriteAllText(Path.Combine(fixture,"game.js"),"require('../outside')");Reject(()=>WeChatExportV9.ValidatePackage(fixture,config));File.WriteAllText(Path.Combine(fixture,"game.js"),"require('./module')");});
                Case("PKG07 Absent openDataContext fails",()=>{File.WriteAllText(Path.Combine(fixture,"game.json"),"{}");Reject(()=>WeChatExportV9.ValidatePackage(fixture,config));File.WriteAllText(Path.Combine(fixture,"game.json"),"{\"openDataContext\":\"open-data\"}");});
                Case("SDK02 Finalizer rejects absent real completion signal",()=>{Reject(()=>WeChatExportV9.Complete(fixture,config,false));Check(!File.Exists(Path.Combine(fixture,"hotpot","export-validation.json")),"Fixture incorrectly claims SDK completion");});
                Case("SDK03 Exact completion and errors are latched",()=>{
                    var flags=BindingFlags.Static|BindingFlags.NonPublic;
                    var done=typeof(WeChatBuild).GetField("allDone",flags);var error=typeof(WeChatBuild).GetField("exportError",flags);var observe=typeof(WeChatBuild).GetMethod("ObserveExport",flags);
                    done.SetValue(null,false);error.SetValue(null,false);
                    observe.Invoke(null,new object[]{"export returned success","",LogType.Log});Check(!(bool)done.GetValue(null),"Early return treated as completion");
                    observe.Invoke(null,new object[]{"[Converter] All done!","",LogType.Log});Check((bool)done.GetValue(null),"Completion ignored");
                    observe.Invoke(null,new object[]{"controlled error","",LogType.Error});Check((bool)error.GetValue(null),"Error ignored");
                    done.SetValue(null,false);error.SetValue(null,false);
                });
                Check(panelBefore.SequenceEqual(File.ReadAllBytes(panelPath)),"Panel changed by diagnostics");
                Debug.Log("TASK002_EXPORT_ENGINEERING_PASS cases="+cases.Count);
            }
            catch(Exception error){exit=1;Debug.LogError("TASK002_EXPORT_ENGINEERING_FAILED "+error.GetType().Name+": "+error.Message);}
            finally
            {
                foreach(var item in originals)Environment.SetEnvironmentVariable(item.Key,item.Value);
                // Fixture identity carries no real account and must not survive in evidence.
                if(File.Exists(Path.Combine(fixture,"project.config.json")))File.WriteAllText(Path.Combine(fixture,"project.config.json"),"{\"compileType\":\"game\",\"appid\":\"\",\"projectname\":\"ControlledFixture\"}");
                File.WriteAllText(Path.Combine(evidence,"unity-export-engineering.json"),JsonUtility.ToJson(new Report{status=exit==0?"PASS":"FAIL",cases=cases.ToArray(),realSdkExport=false,deviceTested=false},true));
                if(Application.isBatchMode)EditorApplication.Exit(exit);
            }
        }
        [Serializable] private sealed class Report{public string status;public string[] cases;public bool realSdkExport,deviceTested;}
    }
}
