#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using HotpotSort.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HotpotSort.Build
{
    [InitializeOnLoad] public static class Task034FontCapture
    {
        const string Key="Task034.FontCapture";
        static Task034FontCapture(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run(){SessionState.SetBool(Key,true);UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);EditorApplication.EnterPlaymode();}
        static void Capture(Canvas canvas,string path)
        {
            const int width=1080,height=1920;
            var go=new GameObject("FontCaptureCamera");var camera=go.AddComponent<Camera>();
            var target=new RenderTexture(width,height,24);target.Create();camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.orthographic=true;camera.orthographicSize=height*.5f;camera.transform.position=new Vector3(0,0,-1000);
            var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=100;
            Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
            var png=new Texture2D(width,height,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,width,height),0,0);png.Apply();File.WriteAllBytes(path,png.EncodeToPNG());RenderTexture.active=previous;
            canvas.renderMode=mode;canvas.worldCamera=previousCamera;camera.targetTexture=null;target.Release();UnityEngine.Object.Destroy(png);UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(go);
        }
        static void Execute()
        {
            int exit=0;string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../.harness/qa/TASK-034/font"));Directory.CreateDirectory(output);
            try
            {
                FontSubsetBuildGuard.Validate();
                var errors=TaskAssetValidation.ValidateModernFont();if(errors.Length>0)throw new Exception(string.Join(";",errors));
                var view=new GameObject("Task034FontFixture").AddComponent<GameplayView>();view.ConfigureAssets(PresentationAssets.CandidateRoot);view.Bind(null);view.SetViewport(new Rect(0,0,1080,1920),1920);Canvas.ForceUpdateCanvases();
                var title=view.GetComponentsInChildren<Text>().Single(t=>t.text=="一锅又一锅");
                if(title.cachedTextGenerator.lines.Count!=1||title.cachedTextGenerator.characterCountVisible!=5)throw new Exception("Title clipping");
                string share=new HotpotSort.Platform.WeChatRuntimeConfig().shareTitle;
                if(share!="来《一锅又一锅》，一起开锅！")throw new Exception("Unexpected share text");
                title.font.RequestCharactersInTexture(share,44,FontStyle.Normal);
                foreach(char c in share)if(!title.font.HasCharacter(c)||!title.font.GetCharacterInfo(c,out var glyph,44,FontStyle.Normal)||glyph.glyphWidth<=0)throw new Exception("Missing rendered glyph U+"+((int)c).ToString("X4"));
                var canvas=view.GetComponentInChildren<Canvas>();Capture(canvas,output+"/entry-title.png");
                var sample=new GameObject("ShareTextFontProof",typeof(RectTransform),typeof(Image));sample.transform.SetParent(canvas.transform,false);var rect=(RectTransform)sample.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(950,230);sample.GetComponent<Image>().color=new Color(.10f,.06f,.03f,.98f);
                var textNode=new GameObject("ShareText",typeof(RectTransform),typeof(Text));textNode.transform.SetParent(rect,false);var textRect=(RectTransform)textNode.transform;textRect.anchorMin=Vector2.zero;textRect.anchorMax=Vector2.one;textRect.offsetMin=new Vector2(25,20);textRect.offsetMax=new Vector2(-25,-20);
                var label=textNode.GetComponent<Text>();label.font=title.font;label.fontSize=44;label.color=new Color(1,.94f,.82f);label.alignment=TextAnchor.MiddleCenter;label.text=share;label.raycastTarget=false;
                Capture(canvas,output+"/share-text.png");if(label.cachedTextGenerator.characterCountVisible!=share.Length)throw new Exception("Share text clipping");
                File.WriteAllText(output+"/unity-result.json","{\"status\":\"PASS\",\"checks\":[\"FontSubsetBuildGuard\",\"ValidateModernFont\",\"current-title-single-line\",\"share-all-glyphs-rendered\",\"share-no-clipping\"],\"deviceTested\":false}");
                Debug.Log("TASK034_FONT_RENDER_PASS");
            }
            catch(Exception e){exit=1;Debug.LogException(e);}
            finally{SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
        }
    }
}
#endif
