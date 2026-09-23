#if UNITY_EDITOR
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HotpotSort.Presentation
{
    [InitializeOnLoad]
    public static class NativeTouchUiDiagnostic
    {
        const string Key="Hotpot.NativeUiDiagnostic";
        static NativeTouchUiDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Check();};}
        public static void Run()
        {
            SessionState.SetBool(Key,true);
            var args=Environment.GetCommandLineArgs();SessionState.SetString(Key+"Path",args[Array.IndexOf(args,"-task001Report")+1]);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();
        }
        static async Task Frames(int count){int last=Time.frameCount;while(count>0){await Task.Delay(10);if(Time.frameCount!=last){last=Time.frameCount;count--;}}}
        static void Require(bool ok,string why){if(!ok)throw new Exception(why);}
        static async void Check()
        {
            int exit=0,clicks=0;string result;
            try
            {
                new GameObject("Events",typeof(EventSystem),typeof(StandaloneInputModule));
                var canvas=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                var button=new GameObject("Button",typeof(RectTransform),typeof(Image),typeof(Button));button.transform.SetParent(canvas.transform,false);
                var rect=button.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(200,100);
                var control=button.GetComponent<Button>();control.onClick.AddListener(()=>clicks++);
                var input=NativeTouchUiInput.Install();Require(input,"native input not installed");
                await Frames(2);Canvas.ForceUpdateCanvases();var center=RectTransformUtility.WorldToScreenPoint(null,rect.position);
                input.Push(1,center,TouchPhase.Began);input.Push(1,center,TouchPhase.Ended);
                await Frames(4);Require(clicks==1,"short same-frame tap lost or duplicated");
                await Frames(4);Require(clicks==1,"stationary frame duplicated click");
                input.Push(2,center,TouchPhase.Began);await Frames(2);input.Cancel();await Frames(3);Require(clicks==1,"focus cancellation clicked button");
                input.Push(3,center,TouchPhase.Began);input.Push(3,center+Vector2.one*1000,TouchPhase.Ended);await Frames(4);Require(clicks==1,"release outside clicked button");
                control.interactable=false;input.Push(4,center,TouchPhase.Began);input.Push(4,center,TouchPhase.Ended);await Frames(4);Require(clicks==1,"disabled button activated");
                control.interactable=true;input.Push(5,center,TouchPhase.Began);input.Push(5,center,TouchPhase.Ended);await Frames(4);Require(clicks==2,"input did not recover after cancellation");
                Require(!input.mousePresent&&!input.GetMouseButtonDown(0),"duplicate mouse source enabled");
                result="NATIVE_UI_PASS checks=7 actualUGUI=true externalRequests=0";Debug.Log(result);
            }
            catch(Exception ex){exit=1;result=ex.ToString();Debug.LogException(ex);}
            SessionState.SetBool(Key,false);File.WriteAllText(SessionState.GetString(Key+"Path",""),result);EditorApplication.Exit(exit);
        }
    }
}
#endif
