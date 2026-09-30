#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HotpotSort.Bootstrap;
using HotpotSort.Task001V7;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HotpotSort.Presentation
{
    [InitializeOnLoad]
    public static class EntryIconsDiagnostic
    {
        const string Key="Hotpot.EntryIconsDiagnostic";
        static EntryIconsDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");EditorApplication.EnterPlaymode();}
        static void Check(bool value,string name){if(!value)throw new Exception(name);Debug.Log("ENTRY_ICONS_PASS "+name);}
        static async void Execute()
        {
            int code=0;
            try
            {
                var root=Path.GetFullPath("../.harness/qa/TASK-019");Directory.CreateDirectory(root);
                DailyProductionComposition composition=null;
                for(int i=0;i<200;i++){composition=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();if(composition&&composition.PlayerView&&composition.PlayerView.LastSnapshot!=null)break;await Task.Delay(50);}
                var view=composition.PlayerView;Check(view.LastSnapshot.phase==ViewPhase.Entry,"actual boot entry");
                foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1440,3200)})VisualCapture.Capture(view,size.x,size.y,root+"/entry-"+size.x+"x"+size.y+".png");
                var buttons=view.GetComponentsInChildren<Button>();
                var settings=buttons.First(b=>b.name=="Action_设置");var friends=buttons.First(b=>b.name=="Action_排行榜");
                var panel=(RectTransform)settings.transform.parent.Find("RestaurantCounter");
                Check(panel.rect.height==169&&panel.anchoredPosition.y==-676,"panel extends downward only");
                foreach(var b in new[]{settings,friends})
                {
                    var buttonRect=(RectTransform)b.transform;var r=buttonRect.rect;Check(r.width>=133&&r.height>=70,b.name+" hit area");
                    Check(-buttonRect.anchoredPosition.y+r.height<=-panel.anchoredPosition.y+panel.rect.height-16,b.name+" panel bottom clearance");
                    Check(b.GetComponent<Image>().raycastTarget,b.name+" hit enabled");
                    var caption=b.GetComponentInChildren<Text>();Check(caption.text==b.name.Substring(7)&&Mathf.Approximately(caption.fontSize*caption.transform.localScale.y,18),b.name+" caption");
                    var disc=(RectTransform)b.transform.Find("IconDisc");var icon=(RectTransform)disc.Find("Icon");
                    Check(disc.rect.size==new Vector2(52,52),b.name+" 52px disc");
                    Check(icon.rect.size==new Vector2(32,32),b.name+" 32px icon");
                }
                int settingsEvents=0,friendsEvents=0;view.SettingsRequested+=()=>settingsEvents++;view.FriendsRequested+=()=>friendsEvents++;
                settings.onClick.Invoke();Check(settingsEvents==1,"settings callback");VisualCapture.Capture(view,720,1280,root+"/settings.png");typeof(GameplayView).GetMethod("CloseModal",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(view,null);
                friends.onClick.Invoke();await Task.Delay(300);Check(friendsEvents==1,"friends callback");VisualCapture.Capture(view,720,1280,root+"/friends.png");
                Debug.Log("ENTRY_ICONS_COMPLETE");
            }
            catch(Exception e){code=1;Debug.LogException(e);}
            finally{SessionState.SetBool(Key,false);EditorApplication.Exit(code);}
        }
    }
}
#endif
