#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotpotSort.Determinism;
using HotpotSort.Platform;
using HotpotSort.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HotpotSort.Bootstrap
{
    [InitializeOnLoad]
    public static class Task002V9IntegrationDiagnostic
    {
        const string Key="Task002V9IntegrationQa";
        const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        static Task002V9IntegrationDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run()
        {
            var args=Environment.GetCommandLineArgs();SessionState.SetString(Key+"Report",args[Array.IndexOf(args,"-task001Report")+1]);SessionState.SetBool(Key,true);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();
        }
        static void Assert(bool value,string error){if(!value)throw new Exception(error);}
        static void Execute()
        {
            var results=new List<object>();int failures=0;GameObject node=null;
            Action<string,Action> check=(name,action)=>{try{action();results.Add(CanonicalJson.Object("case",name,"status","PASS"));}catch(Exception e){failures++;results.Add(CanonicalJson.Object("case",name,"status","FAIL","error",e.ToString()));}};
            try
            {
                check("sdk-success-and-error-codes",()=>
                {
                    var complete=typeof(WeChatPlatform).GetMethod("CompleteInitialization",BindingFlags.NonPublic|BindingFlags.Static);
                    Assert(complete!=null,"SDK completion callback handler absent");
                    foreach(int code in new[]{0,200,-1,1,199,201,400,500,int.MinValue,int.MaxValue})
                    {
                        var tcs=new TaskCompletionSource<bool>();complete.Invoke(null,new object[]{tcs,code});bool expected=code==0||code==200;
                        Assert(tcs.Task.Status==(expected?TaskStatus.RanToCompletion:TaskStatus.Faulted),"Unexpected code disposition: "+code);
                        if(!expected)Assert(tcs.Task.Exception.InnerException.Message=="wx-init-failed:"+code,"Error identity lost");
                        complete.Invoke(null,new object[]{tcs,expected?500:200});
                        Assert(tcs.Task.Status==(expected?TaskStatus.RanToCompletion:TaskStatus.Faulted),"Late callback changed terminal result");
                    }
                });
                check("approved-font-asset-and-glyphs",()=>
                {
                    var errors=TaskAssetValidation.ValidateModernFont();Assert(errors.Length==0,string.Join(";",errors));
                    Assert(TaskAssetValidation.ModernFontResource==WeChatRuntimeConfig.FontResource,"Platform/presentation resource mismatch");
                    var font=Resources.Load<Font>(TaskAssetValidation.ModernFontResource);
                    var importer=(TrueTypeFontImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(font));
                    Assert(importer.includeFontData,"Font data not embedded");
                });
                check("missing-font-explicit-error",()=>
                {
                    var require=typeof(TaskAssetValidation).GetMethod("RequireModernFont",BindingFlags.Public|BindingFlags.Static);
                    Assert(require!=null,"Explicit missing-font guard absent");
                    try{require.Invoke(null,new object[]{null});throw new Exception("Missing font accepted");}
                    catch(TargetInvocationException e){Assert(e.InnerException is InvalidOperationException&&e.InnerException.Message.Contains(TaskAssetValidation.ModernFontResource),"Missing font did not preserve explicit resource error");}
                });
                check("runtime-awake-and-v7-title-body-bindings",()=>
                {
                    node=new GameObject("Task002V9IsolatedFontQA");var view=node.AddComponent<GameplayView>();
                    var font=Resources.Load<Font>(TaskAssetValidation.ModernFontResource);
                    Action assertFonts=()=>
                    {
                        Assert(view.playerFont==font,"Body does not use approved subset");
                        Assert((Font)typeof(GameplayView).GetField("displayFont",Private).GetValue(view)==font,"Title does not use approved subset");
                        var texts=node.GetComponentsInChildren<Text>(true);Assert(texts.Length>0,"No runtime labels");
                        Assert(texts.All(t=>t.font==font),"Runtime label uses historical/system font");
                    };
                    assertFonts();view.ConfigureAssets(V7Art.Root);assertFonts();
                    Assert(TaskAssetValidation.Validate(V7Art.Root).Length==0,"Formal theme validation failed");
                });
                check("runtime-no-legacy-or-system-font-dependency",()=>
                {
                    var files=Directory.GetFiles(Path.Combine(Application.dataPath,"HotpotSort/Runtime"),"*.cs",SearchOption.AllDirectories)
                        .Where(p=>!p.Contains("Diagnostics~")&&!p.Contains(Path.DirectorySeparatorChar+"Editor"+Path.DirectorySeparatorChar));
                    foreach(var file in files)
                    {
                        var source=File.ReadAllText(file);
                        Assert(!source.Contains("fonts/readable")&&!source.Contains("fonts/display")&&!source.Contains("CreateDynamicFontFromOSFont"),"Historical/system font runtime dependency: "+file);
                    }
                    string validation=File.ReadAllText(Path.Combine(Application.dataPath,"HotpotSort/Runtime/Presentation/TaskAssetValidation.cs"));
                    Assert(!validation.Contains("root+\"/fonts/\""),"Theme validation still requires historical fonts");
                });
            }
            catch(Exception e){failures++;results.Add(CanonicalJson.Object("case","harness","status","FAIL","error",e.ToString()));}
            finally
            {
                SessionState.SetBool(Key,false);if(node)UnityEngine.Object.Destroy(node);
                try{File.WriteAllText(SessionState.GetString(Key+"Report",""),CanonicalJson.Write(CanonicalJson.Object("exitCode",failures==0?0:1,"scope","Isolated empty-scene runtime font/init checks; no player profile; no export/device validation","results",results)));Debug.Log(failures==0?"TASK002_INTEGRATION_PASS":"TASK002_INTEGRATION_FAIL");}
                finally{EditorApplication.Exit(failures==0?0:1);}
            }
        }
    }
}
#endif
