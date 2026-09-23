#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HotpotSort.Bootstrap;
using HotpotSort.Contracts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Capture=HotpotSort.Task001V7.VisualCapture;

namespace HotpotSort.Presentation
{
    [InitializeOnLoad]
    public static class Task012IntegrationDiagnostic
    {
        const string Key="Hotpot.Task012.Integration";
        static Task012IntegrationDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);Execute();}};}
        static string Output=>Environment.GetEnvironmentVariable("HOTPOT_TASK012_OUTPUT")??throw new Exception("HOTPOT_TASK012_OUTPUT required");
        static void Check(bool ok,string label){if(!ok)throw new Exception(label);Debug.Log("TASK012_INTEGRATION PASS "+label);}
        public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");EditorApplication.EnterPlaymode();}
        static async void Execute()
        {
            int exit=0;
            try
            {
                Directory.CreateDirectory(Output);HotpotSort.Bootstrap.Bootstrap boot=null;
                for(int i=0;i<200;i++){boot=UnityEngine.Object.FindFirstObjectByType<HotpotSort.Bootstrap.Bootstrap>();if(boot&&boot.IsConfigured)break;await Task.Delay(50);}
                Check(boot&&boot.IsConfigured,"real Bootstrap initializes");
                var composition=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();var view=composition.PlayerView;
                Check(view.AssetRoot==PresentationAssets.CandidateRoot&&boot.Controller.Snapshot==null,"v10 real entry without session");
                foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1440,3200)})Capture.Capture(view,size.x,size.y,Output+"/entry-"+size.x+"x"+size.y+".png");
                composition.SessionAction(ViewAction.StartToday);await boot.PendingStart;await Task.Delay(3500);
                Check(boot.Controller.Snapshot!=null&&boot.Controller.CanAcceptInput,"real core starts");
                Check(view.World.Bodies.Any(),"real supply creates plates");
                foreach(var item in view.World.Bodies.SelectMany(b=>b.data.items))
                {var uv=view.VisualArt.FoodUv(item.foodId);Check(Mathf.Abs(item.uvX-uv.x)<.00001f&&Mathf.Abs(item.uvY-uv.y)<.00001f&&Mathf.Abs(item.uvWidth-uv.width)<.00001f&&Mathf.Abs(item.uvHeight-uv.height)<.00001f,"mapped alpha and display UV "+item.foodId);}
                Check(boot.Controller.ChallengeSeconds==0,"idle timer remains frozen");
                view.SetViewport(new Rect(0,0,Screen.width,Screen.height),Screen.height);Canvas.ForceUpdateCanvases();
                var board=view.GetComponentsInChildren<RectTransform>().Single(t=>t.name=="GameplayBoard");bool tapped=false;
                for(int y=320;y<815&&!tapped;y+=8)for(int x=12;x<410&&!tapped;x+=8)
                    tapped=view.SubmitScreenTap(RectTransformUtility.WorldToScreenPoint(null,board.TransformPoint(new Vector3(x,-y,0))));
                await Task.Delay(150);Check(tapped&&boot.Controller.ChallengeSeconds>0,"real alpha tap accepted and starts timer");
                foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1440,3200)})Capture.Capture(view,size.x,size.y,Output+"/live-"+size.x+"x"+size.y+".png");
                composition.SessionAction(ViewAction.Pause);Check(!boot.Controller.CanAcceptInput,"pause");composition.SessionAction(ViewAction.Resume);Check(boot.Controller.CanAcceptInput,"resume");
                long generation=boot.Controller.Generation;composition.SessionAction(ViewAction.RetrySameDay);
                for(int i=0;i<100&&boot.Controller.Generation==generation;i++)await Task.Delay(30);
                Check(boot.Controller.Generation>generation&&boot.Controller.CanAcceptInput,"retry");composition.SessionAction(ViewAction.Exit);Check(boot.Controller.Snapshot==null,"exit");
                Debug.Log("TASK012_INTEGRATION_OK");
            }
            catch(Exception e){exit=1;Debug.LogException(e);}
            finally{EditorApplication.Exit(exit);}
        }
        public static void BuildDesktop()
        {
            Directory.CreateDirectory(Output);
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/HotpotSort/Scenes/Boot.unity"},locationPathName=Output+"/HotpotTask012.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            Debug.Log("TASK012_DESKTOP_BUILD "+result.summary.result);EditorApplication.Exit(result.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded?0:1);
        }
    }
}
#endif
