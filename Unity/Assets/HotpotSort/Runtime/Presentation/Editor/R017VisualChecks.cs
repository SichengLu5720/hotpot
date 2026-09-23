#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotpotSort.Presentation;
using HotpotSort.Bootstrap;
using HotpotSort.Contracts.RemoteAssets;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Capture=HotpotSort.Task001V7.VisualCapture;

namespace HotpotSort.VisualIteration
{
    [InitializeOnLoad] public static class R017VisualChecks
    {
        const string Active="R017.Active",PathKey="R017.Path";
        static readonly List<string> checks=new List<string>();
        static R017VisualChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Active,false))Execute();};}
        public static void Run(){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-visualEvidence");if(i<0)throw new Exception("Evidence path required");Directory.CreateDirectory(args[i+1]);SessionState.SetString(PathKey,args[i+1]);SessionState.SetBool(Active,true);EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");EditorApplication.delayCall+=EditorApplication.EnterPlaymode;}
        static void Check(bool ok,string label){checks.Add((ok?"PASS ":"FAIL ")+label);if(!ok)throw new Exception(label);}
        static void All(GameplayView view,string root,string name){foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1440,3200)})Capture.Capture(view,size.x,size.y,root+"/"+name+"-"+size.x+"x"+size.y+".png");}
        static async void Execute()
        {
            int exit=0;string root=SessionState.GetString(PathKey,"");
            try
            {
                DailyProductionComposition daily=null;for(int i=0;i<100;i++){daily=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();if(daily&&daily.PlayerView&&daily.PlayerView.VisualArt!=null)break;await Task.Delay(50);}
                Check(daily&&daily.PlayerView&&daily.PlayerView.VisualArt!=null,"actual Boot ready");var view=daily.PlayerView;
                Check(view.GetComponentsInChildren<RawImage>(true).Where(i=>i.name=="EdgeFestivity").All(i=>!i.enabled),"legacy fabric framing disabled");
                view.SetAssetPreparation(new AssetPreparationSnapshot(1,"test",AssetReadiness.Downloading,AssetError.None,57,100,1));All(view,root,"loading");
                Check(view.GetComponentsInChildren<Text>().Any(t=>t.text=="准备食材 57%"),"loading text localized");
                Check(!view.GetComponentsInChildren<Button>().First(b=>b.name=="Action_开始下火锅").interactable,"loading start remains gated");
                view.SetAssetPreparation(new AssetPreparationSnapshot(1,"test",AssetReadiness.Failed,AssetError.NotConfigured,0,100,1));All(view,root,"loading-failed");
                Check(view.GetComponentsInChildren<Text>().Any(t=>t.text=="重试"),"retry localized without raw error code");
                view.SetAssetPreparation(new AssetPreparationSnapshot(1,"test",AssetReadiness.Ready,AssetError.None,100,100,1));
                var boot=UnityEngine.Object.FindFirstObjectByType<HotpotSort.Bootstrap.Bootstrap>();await boot.Controller.StartTodayAsync();await Task.Delay(3200);
                view.SetViewport(new Rect(0,0,Screen.width,Screen.height),Screen.height);Canvas.ForceUpdateCanvases();
                Check(!view.GetComponentsInChildren<RectTransform>(true).Any(n=>n.name=="ProgressTrack"||n.name=="CompletedOrders"||n.name=="ProgressFrame"),"gameplay progress bar removed");
                var timer=view.GetComponentsInChildren<RectTransform>(true).Single(n=>n.name=="Timer");
                Check(Mathf.Abs(timer.anchoredPosition.x+timer.rect.width*.5f-210)<.01f&&Mathf.Abs(-timer.anchoredPosition.y-8)<.01f,"timer top-centered without platform chrome");
                var timerText=view.GetComponentsInChildren<Text>(true).Single(t=>t.text.Contains(":"));
                Check(timerText.horizontalOverflow==HorizontalWrapMode.Overflow&&timerText.rectTransform.sizeDelta==new Vector2(384,160)&&timerText.rectTransform.localScale==Vector3.one*.25f,"timer keeps one-line 4x font raster layout");
                float hudScale=Mathf.Min(Screen.width/420f,Screen.height/900f);
                float hudLeft=(Screen.width-420*hudScale)*.5f,hudTop=(Screen.height-900*hudScale)*.5f;
                var overlappingMenu=new Rect(hudLeft+180*hudScale,Screen.height-(hudTop+36*hudScale),80*hudScale,32*hudScale);
                view.SetViewport(new Rect(0,0,Screen.width,Screen.height),Screen.height,overlappingMenu);Canvas.ForceUpdateCanvases();
                Check(Mathf.Abs(-timer.anchoredPosition.y-48)<.01f,"timer moves below 12px expanded capsule guard");
                view.SetViewport(new Rect(0,0,Screen.width,Screen.height),Screen.height);Canvas.ForceUpdateCanvases();
                Check(Mathf.Abs(-timer.anchoredPosition.y-8)<.01f,"timer returns to top-center when capsule data is unavailable");
                string eligible=view.FindClickableHint();Check(eligible!=null&&view.IsItemClickable(eligible),"live gameplay has clickable target before overlay");
                var texture=new Texture2D(8,8,TextureFormat.RGBA32,false);for(int y=0;y<8;y++)for(int x=0;x<8;x++)texture.SetPixel(x,y,y<4?Color.red:Color.green);texture.Apply();
                int closed=0,opened=0,released=0,viewportCalls=0;Rect boardRect=default;
                view.ShowFriendBoard(()=>texture,()=>closed++,r=>{boardRect=r;viewportCalls++;},open=>{if(open)opened++;else released++;});
                All(view,root,"friend-surface-diagnostic");
                Check(view.FriendBoardVisible,"friend overlay visible");Check(!view.PresentationForeground,"underlying presentation clock gated");
                Check(!view.IsItemClickable(eligible),"live gameplay target blocked by friend modality");
                var image=view.GetComponentsInChildren<RawImage>().Single(x=>x.name=="SharedCanvas");
                Check(image.texture==texture&&image.uvRect==new Rect(0,1,1,-1),"real RawImage binding and upside-down SDK UV");
                Check(boardRect.width>=120&&boardRect.height>=160&&viewportCalls>=3,"physical viewport emitted at supported ratios");
                Check(!view.SubmitScreenTap(new Vector2(200,400)),"gameplay input gated while friend surface visible");
                view.SetViewport(new Rect(30,70,660,1180),1280);Check(view.FriendBoardViewportPixels.x>=30&&view.FriendBoardViewportPixels.y>=30&&view.FriendBoardViewportPixels.xMax<=690&&view.FriendBoardViewportPixels.yMax<=1210,"friend viewport excludes unsafe area");
                view.GetComponentsInChildren<Button>().Single(b=>b.name=="Action_关闭").onClick.Invoke();
                Check(!view.FriendBoardVisible&&closed==1&&opened==1&&released==1,"close callback once and modality released");view.HideFriendBoard();Check(closed==1&&released==1,"hide idempotent");
                UnityEngine.Object.Destroy(texture);
                view.SetViewport(new Rect(0,0,Screen.width,Screen.height),Screen.height);Canvas.ForceUpdateCanvases();
                Check(view.IsItemClickable(eligible),"same live target clickable after friend close");
                var icons=view.GetComponentsInChildren<ModernIconGraphic>();Check(icons.Length>=4,"modern icons exist");
                foreach(var icon in icons){Check(icon.canvasRenderer!=null,"icon canvas renderer "+icon.symbol);var mesh=icon.canvasRenderer.GetMesh();Check(mesh&&mesh.vertexCount>0,"icon draws vertices "+icon.symbol);}
                All(view,root,"live-icons");
                var legacyShare=view.ShowThemeShareAsync("Hotpot/TASK001/v7/r001/hero/share");
                Check(!view.GetComponentsInChildren<RectTransform>().Any(n=>n.name=="ThemeCard"||n.name=="RewardShareThemeOnly"),"legacy share presentation never opens a picture page");
                view.GetComponentsInChildren<Button>().Single(b=>b.name=="Action_关闭").onClick.Invoke();await legacyShare;
                File.WriteAllText(root+"/result.json","{\"status\":\"PASS\",\"scope\":\"Actual Unity presentation tests. Diagnostic color texture is not real friend data; Bootstrap/platform binding remains integrator-owned.\",\"checks\":"+checks.Count+"}");
            }
            catch(Exception e){exit=1;Debug.LogException(e);File.WriteAllText(root+"/error.txt",e.ToString());}
            finally{File.WriteAllLines(root+"/checks.txt",checks);SessionState.SetBool(Active,false);EditorApplication.Exit(exit);}
        }
    }
}
#endif
