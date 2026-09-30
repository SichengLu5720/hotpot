#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Capture=HotpotSort.Task001V7.VisualCapture;
namespace HotpotSort.Presentation
{
    [InitializeOnLoad] public static class Task012RepairCapture
    {
        const string Key="Task012.Repair";
        static readonly List<string> checks=new List<string>();
        static void Check(bool ok,string label){checks.Add((ok?"PASS ":"FAIL ")+label);if(!ok)throw new Exception(label);}
        static Task012RepairCapture(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run(){SessionState.SetBool(Key,true);UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);EditorApplication.EnterPlaymode();}
        static void Shot(GameplayView view,int width,int height,string path,bool desktop)
        {
            var canvas=view.GetComponentInChildren<Canvas>();var owner=new GameObject("RepairCamera");var camera=owner.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=height*.5f;camera.transform.position=new Vector3(0,0,-1000);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var rt=new RenderTexture(width,height,24);rt.Create();camera.targetTexture=rt;var prior=RenderTexture.active;RenderTexture.active=rt;GL.Clear(true,true,Color.black);
            int gameWidth=desktop?1008:width;if(desktop)camera.pixelRect=new Rect((width-gameWidth)/2,0,gameWidth,height);
            var old=canvas.renderMode;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=100;Canvas.ForceUpdateCanvases();view.SetViewport(new Rect(0,0,gameWidth,height),height);Canvas.ForceUpdateCanvases();camera.Render();
            var edges=view.GetComponentsInChildren<RawImage>().Where(i=>i.name.StartsWith("ThemeEdge")&&i.enabled).ToArray();Check(edges.Length>=2,"visible edge strips "+Path.GetFileName(path));Check(edges.All(i=>!i.raycastTarget),"edge raycast disabled "+width+"x"+height);
            var content=canvas.transform.Find("SafeContent");Check(edges.All(i=>i.transform.GetSiblingIndex()<content.GetSiblingIndex()),"edges behind entire content "+width+"x"+height);
            view.SetViewport(new Rect(0,0,gameWidth,height),height,new Rect(gameWidth-180,height-100,160,60));
            var capsuleTop=new Rect(gameWidth-180,40,160,60);
            foreach(var edge in edges.Where(i=>i.enabled)){var r=edge.rectTransform;Check(!new Rect(r.anchoredPosition.x,-r.anchoredPosition.y,r.rect.width,r.rect.height).Overlaps(capsuleTop),"capsule exclusion "+width+"x"+height);}
            var png=new Texture2D(width,height,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,width,height),0,0);png.Apply();File.WriteAllBytes(path,png.EncodeToPNG());RenderTexture.active=prior;canvas.renderMode=old;canvas.worldCamera=null;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(png);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(owner);
        }
        static void Execute()
        {
            var args=Environment.GetCommandLineArgs();var path=args[Array.IndexOf(args,"-visualEvidence")+1];Directory.CreateDirectory(path);int exit=0;
            try{
                checks.Clear();var view=new GameObject("RepairFixture").AddComponent<GameplayView>();view.ConfigureAssets(PresentationAssets.CandidateRoot);view.Bind(null);
                foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1440,3200),new Vector2Int(3840,2160)})Shot(view,size.x,size.y,path+"/entry-"+size.x+"x"+size.y+".png",size.x==3840);
                foreach(var pair in new[]{("Action_开始下火锅",new Vector2(70,-700)),("Action_设置",new Vector2(70,-772)),("Action_好友榜",new Vector2(217,-772))})Check(view.GetComponentsInChildren<Button>().Single(b=>b.name==pair.Item1).GetComponent<RectTransform>().anchoredPosition==pair.Item2,"entry hit position preserved "+pair.Item1);
                Check(view.GetComponentsInChildren<RawImage>().Single(i=>i.name=="EntryHero").rectTransform.rect.size==new Vector2(444,444),"entry hero aspect contained");
                var s=Capture.Fixture();view.Bind(new Capture.FullPort(s,false));view.SetForeground(false);
                foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1440,3200),new Vector2Int(3840,2160)})Shot(view,size.x,size.y,path+"/play-"+size.x+"x"+size.y+".png",size.x==3840);
                var edges=view.GetComponentsInChildren<RawImage>().Where(i=>i.name.StartsWith("ThemeEdge")&&i.enabled).ToArray();if(edges.Any(i=>i.raycastTarget))throw new Exception("edge raycast");
                Check(view.PlateCrop==new Rect(0,292,420,536),"physical crop preserved");view.Bind(null);
                File.WriteAllLines(path+"/checks.txt",checks);File.WriteAllText(path+"/scope.txt","Actual Unity PlayMode rendering. Desktop uses centered 1008x2160 camera pixelRect within 3840x2160 target matching user aspect. Presentation fixtures, not live core gameplay; no final executable rebuilt.");Debug.Log("TASK012_REPAIR_CAPTURE_OK");
            }catch(Exception e){exit=1;Debug.LogException(e);File.WriteAllText(path+"/error.txt",e.ToString());}finally{SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
        }
    }
}
#endif
