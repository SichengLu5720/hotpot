#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HotpotSort.Bootstrap;
using HotpotSort.Contracts;
using HotpotSort.Task001V7;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace HotpotSort.Presentation
{
    [InitializeOnLoad] public static class Task030IntegrationDiagnostic
    {
        const string Key="Hotpot.Task030Integration";
        static Task030IntegrationDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");EditorApplication.EnterPlaymode();}
        static void Check(bool value,string message){if(!value)throw new Exception(message);Debug.Log("TASK030_INTEGRATION_PASS "+message);}
        static async void Execute()
        {
            int code=0;
            try
            {
                string output=Path.GetFullPath("../.harness/artifacts/TASK-030/integration");Directory.CreateDirectory(output);
                DailyProductionComposition composition=null;
                for(int i=0;i<200;i++){composition=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();if(composition&&composition.PlayerView)break;await Task.Delay(50);}
                var view=composition.PlayerView;Check(view&&view.LastSnapshot.phase==ViewPhase.Entry,"real Boot entry");
                var local=new LocalDevelopmentServices(storageKey:"Task030Diagnostic."+Guid.NewGuid().ToString("N"));
                composition.ConfigureServices(local,local,local,local);
                await Task.Delay(80);
                VisualCapture.Capture(view,720,1280,output+"/home-locked.png");
                Check(!view.GetComponentsInChildren<Button>().First(b=>b.name=="CollectionEntry").interactable,"locked entry");
                var reward=local.Collection.RecordWin("diagnostic-win",DateTimeOffset.UtcNow,true);
                await Task.Delay(80);VisualCapture.Capture(view,720,1280,output+"/home-unlocked.png");
                view.OpenCollection();await Task.Delay(80);VisualCapture.Capture(view,720,1280,output+"/collection.png");
                var grid=view.GetComponentsInChildren<RectTransform>().First(t=>t.name=="CollectionGrid");Check(grid.childCount==32,"32 foods");
                var draft=(HashSet<string>)typeof(GameplayView).GetField("collectionDraft",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view);
                draft.Remove("food_00");Check(view.HandleCollectionBack()&&view.CollectionVisible,"back blocked under minimum");
                VisualCapture.Capture(view,720,1280,output+"/under-minimum.png");
                draft.Add("food_00");Check(view.TryCloseCollection(),"valid selection exits");
                view.ShowCollectionReward(reward,()=>{});await Task.Delay(80);VisualCapture.Capture(view,720,1280,output+"/reward.png");view.ResetCollectionTransientPresentation();
                Task030CollectionVisualDiagnostic.Run();
                Check(TaskAssetValidation.Validate(PresentationAssets.CandidateRoot,true).Length==0,"formal asset gate");
                var all=local.Collection.ReadCollection();all.serverRevision++;
                all.unlocked=Enumerable.Range(0,32).Select(CollectionCatalog.Id).ToList();all.selected=Enumerable.Range(16,16).Select(CollectionCatalog.Id).ToList();
                local.Collection.ApplyAuthoritativeSnapshot(all);
                composition.SessionAction(ViewAction.StartToday);
                var boot=UnityEngine.Object.FindFirstObjectByType<Bootstrap.Bootstrap>();await boot.PendingStart;
                for(int i=0;i<100&&view.LastSnapshot.plates.Length==0;i++)await Task.Delay(100);
                await Task.Delay(1500);
                Check(boot.Controller.Error==null&&view.LastSnapshot.phase==ViewPhase.Running,"selected session starts without identity mismatch");
                Check(view.LastSnapshot.plates.Length>0&&view.LastSnapshot.plates.SelectMany(p=>p.items).Any()&&view.LastSnapshot.plates.SelectMany(p=>p.items).All(i=>i.foodId>=16),"new ingredients in actual warmup");
                VisualCapture.Capture(view,720,1280,output+"/new-foods-gameplay.png");
                Debug.Log("TASK030_INTEGRATION_COMPLETE");
            }
            catch(Exception e){code=1;Debug.LogException(e);}
            finally{SessionState.SetBool(Key,false);EditorApplication.Exit(code);}
        }
    }
}
#endif
