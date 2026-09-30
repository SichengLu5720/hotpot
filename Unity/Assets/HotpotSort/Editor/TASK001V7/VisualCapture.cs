#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HotpotSort.Bootstrap;
using HotpotSort.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HotpotSort.Task001V7
{
    [InitializeOnLoad]
    public static partial class VisualCapture
    {
        const string Active="Hotpot.VisualV7.Active", PathKey="Hotpot.VisualV7.Path";
        static VisualCapture(){EditorApplication.playModeStateChanged+=Changed;}
        public static void RunBaseline(){Run();}
        public static void Run()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-visualEvidence");
            if(i<0)throw new ArgumentException("-visualEvidence required");
            SessionState.SetString(PathKey,args[i+1]);SessionState.SetBool(Active,true);
            Directory.CreateDirectory(args[i+1]);EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Active,false))Execute();}
        public static void Capture(GameplayView view,int width,int height,string path)
        {
            var canvas=view.GetComponentInChildren<Canvas>();var go=new GameObject("VisualQACamera");var camera=go.AddComponent<Camera>();
            var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.orthographic=true;camera.orthographicSize=height*.5f;camera.transform.position=new Vector3(0,0,-1000);
            var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=100;
            Canvas.ForceUpdateCanvases();view.SetViewport(new Rect(0,0,width,height),height);Canvas.ForceUpdateCanvases();camera.Render();
            var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=prior;
            canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;camera.targetTexture=null;UnityEngine.Object.Destroy(image);rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(go);
        }
        static async void Execute()
        {
            string root=SessionState.GetString(PathKey,"");int exit=0;
            try
            {
                HotpotSort.Bootstrap.Bootstrap boot=null;
                for(int i=0;i<100;i++){boot=UnityEngine.Object.FindFirstObjectByType<HotpotSort.Bootstrap.Bootstrap>();if(boot&&boot.IsConfigured)break;await Task.Delay(50);}
                if(!boot||!boot.IsConfigured)throw new Exception("Boot not ready");
                var composition=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();var view=composition.PlayerView;
                if(SessionState.GetBool("Hotpot.VisualV7.Full",false)){await FullCapture(root,boot,composition,view);Debug.Log("V7_FULL_VISUAL_CAPTURE_OK "+root);return;}
                Capture(view,1080,1920,root+"/entry.png");
                await boot.Controller.StartTodayAsync();await Task.Delay(5000);boot.Controller.Request(HotpotSort.Contracts.SessionAction.Pause);
                // Same authored transport fixture before/after: actual Unity renderer, not core-gameplay evidence.
                var state=Fixture();view.Bind(new CapturePort(state));view.SetForeground(false);
                foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1440,3200)})Capture(view,size.x,size.y,root+"/gameplay-"+size.x+"x"+size.y+".png");
                File.WriteAllText(root+"/state.json",JsonUtility.ToJson(state,true));
                File.WriteAllText(root+"/capture-info.json","{\"scope\":\"Actual Unity PlayMode UI via offscreen camera; deterministic presentation fixture, not physical gameplay proof\",\"exitCode\":0}");
                Debug.Log("V7_VISUAL_CAPTURE_OK "+root);
            }
            catch(Exception e){exit=1;Debug.LogException(e);File.WriteAllText(root+"/error.txt",e.ToString());}
            finally{SessionState.SetBool(Active,false);EditorApplication.Exit(exit);}
        }
        public static ViewSnapshot Fixture()
        {
            var s=new ViewSnapshot{sessionId="visual-same-state",sessionGeneration=7,revision=1,phase=ViewPhase.Running,orders=new[]{new ViewOrder{slot=0,foodId=0,required=3,count=1,enabled=true},new ViewOrder{slot=1,foodId=15,required=3,count=2,enabled=true},new ViewOrder{slot=2,foodId=2,required=3},new ViewOrder{slot=3,foodId=4,required=3}}};
            int item=1;var plates=new System.Collections.Generic.List<ViewPlate>();
            float[] xs={65,193,330,85,252,360,60,198,325};float[] ys={368,378,373,498,510,620,657,674,766};int[] counts={1,3,2,5,4,1,2,3,2};
            for(int i=0;i<xs.Length;i++)
            {
                int n=counts[i];var p=new ViewPlate{plateId="visual-"+i,x=xs[i],y=ys[i],radius=Mathf.Min(63,33+6*n),motion=new ViewPlateMotion{animateEntry=false},items=new ViewItem[n]};
                for(int j=0;j<n;j++){float angle=j*Mathf.PI*2/n;float r=n==1?0:n>3?29:21;p.items[j]=new ViewItem{itemId=(item++).ToString(),foodId=(i*3+j)%16,x=Mathf.Cos(angle)*r,y=Mathf.Sin(angle)*r,radius=16};}plates.Add(p);
            }
            s.plates=plates.ToArray();return s;
        }
        public sealed class CapturePort:IPresentationPort
        {
            public ViewSnapshot state;public CapturePort(ViewSnapshot s){state=s;}public event Action<ViewUpdate> Updated;
            public ViewSnapshot Read()=>state;public void SessionAction(ViewAction action){}public void Tap(ViewTap command){}public void ObserveSupply(ViewSupplyObservation value){}
        }
    }
}
#endif
