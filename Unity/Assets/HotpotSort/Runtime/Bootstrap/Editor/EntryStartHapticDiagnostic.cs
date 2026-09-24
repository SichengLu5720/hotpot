#if UNITY_EDITOR
using System;
using System.Linq;
using System.Threading.Tasks;
using HotpotSort.Contracts.RemoteAssets;
using HotpotSort.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HotpotSort.Bootstrap
{
    [InitializeOnLoad]
    public static class EntryStartHapticDiagnostic
    {
        const string Key="Hotpot.EntryStartHaptic";
        static Gate gate;
        sealed class Gate:IPresentationAssetProvider
        {
            readonly LocalPresentationAssetProvider local=new LocalPresentationAssetProvider();
            public AssetPreparationSnapshot Snapshot {get;private set;}=State(AssetReadiness.LocalReady);
            public event Action<AssetPreparationSnapshot> Changed;
            TaskCompletionSource<AssetPreparationSnapshot> pending;
            static AssetPreparationSnapshot State(AssetReadiness state)=>new AssetPreparationSnapshot(0,"fixture",state,AssetError.None,0,0,0);
            public void Set(AssetReadiness state){Snapshot=State(state);Changed?.Invoke(Snapshot);}
            public Task<AssetPreparationSnapshot> PrepareAsync(){pending=new TaskCompletionSource<AssetPreparationSnapshot>();Set(AssetReadiness.Downloading);return pending.Task;}
            public Task<AssetPreparationSnapshot> RetryAsync()=>PrepareAsync();
            public void Finish(AssetReadiness state){Set(state);pending?.TrySetResult(Snapshot);}
            public void Cancel()=>Finish(AssetReadiness.Cancelled);
            public T GetLocal<T>(string path) where T:class=>local.GetLocal<T>(path);
            public T GetComplete<T>(string path) where T:class=>local.GetComplete<T>(path);
            public void Dispose()=>local.Dispose();
        }
        static EntryStartHapticDiagnostic(){if(SessionState.GetBool(Key,false))Bootstrap.DiagnosticAssets=()=>gate=new Gate();EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run(){SessionState.SetBool(Key,true);Bootstrap.DiagnosticAssets=()=>gate=new Gate();EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");EditorApplication.EnterPlaymode();}
        static void Check(bool value,string name){if(!value)throw new Exception(name);Debug.Log("ENTRY_HAPTIC_PASS "+name);}
        static async void Execute()
        {
            int code=0;
            try
            {
                Bootstrap boot=null;
                for(int i=0;i<200;i++){boot=UnityEngine.Object.FindFirstObjectByType<Bootstrap>();if(boot&&boot.IsConfigured)break;await Task.Delay(50);}
                Check(boot&&boot.IsConfigured,"Boot entry initialized");
                var composition=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();var view=composition.PlayerView;
                int count=0;view.EntryStartHapticRequested+=()=>count++;
                var start=view.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="Action_开始下火锅");
                start.onClick.Invoke();Check(count==1,"accepted actual entry button emits once");
                Check(!start.interactable,"busy button disabled");
                start.onClick.Invoke();Check(count==1,"forced duplicate ignored by authoritative gate");
                gate.Finish(AssetReadiness.Failed);await boot.PendingStart;
                await Task.Delay(130);start.onClick.Invoke();Check(count==2,"accepted failed resource retry emits once");
                start.onClick.Invoke();Check(count==2,"busy retry duplicate silent");
                composition.SessionAction(ViewAction.Exit);await boot.PendingStart;Check(count==2,"cancel does not add start haptic");
                await Task.Delay(130);start.onClick.Invoke();Check(count==3,"accepted cancelled resource retry emits once");
                gate.Finish(AssetReadiness.Failed);await boot.PendingStart;
                gate.Set(AssetReadiness.LocalReady);await Task.Delay(130);
                start.onClick.Invoke();Check(count==4,"fresh accepted start emits again");
                gate.Finish(AssetReadiness.Ready);await boot.PendingStart;
                Check(view.LastSnapshot.phase==ViewPhase.Running,"real gameplay reached");
                composition.SessionAction(ViewAction.StartToday);Check(count==4,"running duplicate silent");
                view.NotifyEntryStartAccepted();Check(count==4,"entry event rejected during gameplay");
                composition.SessionAction(ViewAction.Exit);Check(count==4,"exit has no start haptic");
                Debug.Log("ENTRY_HAPTIC_COMPLETE");
            }
            catch(Exception e){code=1;Debug.LogException(e);}
            finally{SessionState.SetBool(Key,false);Bootstrap.DiagnosticAssets=null;EditorApplication.Exit(code);}
        }
    }
}
#endif
