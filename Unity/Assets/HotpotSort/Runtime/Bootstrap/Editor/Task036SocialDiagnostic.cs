#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HotpotSort.Contracts;
namespace HotpotSort.Bootstrap {
 [InitializeOnLoad] public static class Task036SocialDiagnostic {
  const string Key="Task036.Smoke";
  static Task036SocialDiagnostic(){EditorApplication.playModeStateChanged+=async s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);await Smoke();}};}
  public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");UnityEngine.Object.FindFirstObjectByType<Bootstrap>().enabled=false;EditorApplication.EnterPlaymode();}
  static async Task Smoke(){try{
   var boot=UnityEngine.Object.FindFirstObjectByType<Bootstrap>();var c=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();var local=new LocalDevelopmentServices("Hotpot.Task036.Smoke."+Guid.NewGuid().ToString("N"),"test");local.CompleteTutorial(false);local.CompleteTutorial(true);c.ConfigureServices(local,local,local,local);boot.enabled=true;
   for(int i=0;i<600&&!boot.IsConfigured;i++)await Task.Delay(25);if(!boot.IsConfigured)throw new Exception("Boot timeout");
   c.ConfigureServices(local,local,local,local);var view=c.PlayerView;view.SetForeground(true);
   c.enabled=false;var doc=c.BrothCollection;doc.brothAssistantAccount="simulated-helper";doc.brothAssistStartedAt=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();doc.brothAssistExpiresAt=doc.brothAssistStartedAt+72L*3600000;doc.entryUnlocked=true;doc.serverRevision+=100;local.Collection.ApplyAuthoritativeSnapshot(doc);view.UpdateBrothPresentation(doc,true);view.OpenBrothActivity();view.RefreshBrothCountdown();await Task.Delay(300);if(!view.BrothVisible)throw new Exception("Activity missing");
   view.OpenBrothAssistConfirmation(new BrothInvitation{invitationId=new string('a',64),isAssistant=true,startedAt=doc.brothAssistStartedAt,expiresAt=doc.brothAssistExpiresAt},true);view.RefreshBrothCountdown();await Task.Delay(300);if(!view.BrothVisible)throw new Exception("Helper waiting missing");
   Debug.Log("TASK036 UNITY PASS Boot, activity waiting, helper waiting, countdown");EditorApplication.Exit(0);
  }catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}}
 }
}
#endif
