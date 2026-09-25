#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Capture=HotpotSort.Task001V7.VisualCapture;
namespace HotpotSort.Presentation
{
    [InitializeOnLoad] public static class Task027SettlementProgressCapture
    {
        const string Key="Task027.Capture";
        static Task027SettlementProgressCapture(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run(){SessionState.SetBool(Key,true);UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);EditorApplication.EnterPlaymode();}
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("TASK027 PASS "+message);}
        static void Call(GameplayView view,string method,object value){typeof(GameplayView).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,new[]{value});}
        static float Elapsed(GameplayView view)=>(float)typeof(GameplayView).GetField("settlementElapsed",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view);
        static RectTransform Flow(GameplayView view)=>view.GetComponentsInChildren<RectTransform>().Single(n=>n.name.StartsWith("Flow_"));
        static string Percent(GameplayView view)=>Flow(view).Find("ModalFrame/SettlementProgress").GetComponentsInChildren<Text>().Last().text;
        static RectTransform Marker(GameplayView view)=>(RectTransform)Flow(view).Find("ModalFrame/SettlementProgress/ProgressHotpot");
        static void CheckMarkerEdge(GameplayView view,bool right)
        {
            var track=(RectTransform)Flow(view).Find("ModalFrame/SettlementProgress/ProgressTrack");
            var marker=Marker(view);var trackCorners=new Vector3[4];var markerCorners=new Vector3[4];
            track.GetWorldCorners(trackCorners);marker.GetWorldCorners(markerCorners);
            Check(markerCorners[0].x>=trackCorners[0].x-.01f&&markerCorners[2].x<=trackCorners[2].x+.01f,"marker world bounds stay within track");
            int edge=right?2:0;
            Check(Mathf.Abs(markerCorners[edge].x-trackCorners[edge].x)<.01f,right?"100 percent right edges align":"zero percent left edges align");
        }
        static void CheckGradient(GameplayView view,float progress)
        {
            var fill=Flow(view).Find("ModalFrame/SettlementProgress/ProgressTrack/ProgressFill").GetComponent<Image>();
            Check(Mathf.Abs(fill.rectTransform.rect.width-312)<.001f,"gradient remains full width");
            Check(fill.type==Image.Type.Filled&&fill.fillMethod==Image.FillMethod.Horizontal&&fill.fillOrigin==0&&Mathf.Abs(fill.fillAmount-progress)<.0001f,"gradient cropped at actual progress");
            Color32[] expected={new Color32(0x78,0x1C,0x14,255),new Color32(0xB5,0x26,0x18,255),new Color32(0xE4,0x3B,0x1F,255),new Color32(0xFF,0x76,0x26,255)};
            var texture=fill.sprite.texture;
            Check(Enumerable.Range(0,4).All(i=>{Color32 actual=texture.GetPixel(Mathf.RoundToInt(i*(texture.width-1)/3f),texture.height/2);return actual.r==expected[i].r&&actual.g==expected[i].g&&actual.b==expected[i].b;}),"four fixed full-width strong fire color stops");
        }
        static int Buttons(GameplayView view)=>Flow(view).GetComponentsInChildren<Button>().Length;
        static void Execute()
        {
            int exit=0;
            try
            {
                var args=Environment.GetCommandLineArgs();string path=args[Array.IndexOf(args,"-visualEvidence")+1];Directory.CreateDirectory(path);
                var view=new GameObject("Task027Fixture").AddComponent<GameplayView>();view.ConfigureAssets(PresentationAssets.CandidateRoot);
                foreach(string scenario in new[]{"overflow","timeout","won"})
                {
                    var state=Capture.Fixture();state.sessionId=scenario;state.totalOrders=61;state.completedOrders=23;
                    state.phase=scenario=="won"?ViewPhase.Won:ViewPhase.Overflow;state.message=scenario=="timeout"?"Timeout":"";
                    state.facts=new[]{new ViewFact{label="已完成订单",value=scenario=="won"?"61":"23"},new ViewFact{label="已处理食材",value=scenario=="won"?"183":"71"},new ViewFact{label="有效点击",value=scenario=="won"?"183":"75"},new ViewFact{label="最大暂存",value="5"},new ViewFact{label="用时",value=scenario=="timeout"?"600.0 秒":"332.0 秒"}};
                    view.Bind(new Capture.CapturePort(state));view.SetForeground(true);view.World.SetSimulating(false);
                    Check(Percent(view)=="0%"&&Buttons(view)==0,scenario+" starts at zero with no buttons");
                    CheckMarkerEdge(view,false);
                    CheckGradient(view,0);
                    Check(!Flow(view).GetComponentsInChildren<RectTransform>().Any(n=>n.name=="IvoryCard"),"no white card");
                    Check(Flow(view).Find("ModalFrame/SettlementStatistics").GetComponentsInChildren<Text>().Length==10,"five statistics shown");
                    var texts=Flow(view).GetComponentsInChildren<Text>();
                    Check(texts.First().text==(scenario=="won"?"胜利":"失败"),scenario+" settlement title");
                    if(scenario!="won")Check(texts.Any(t=>t.text==(scenario=="timeout"?"这一桌先收好，再来一次吧。":"这一桌满了，下一锅再接再厉。")),scenario+" reason preserved");
                    Call(view,"TickSettlement",.375f);float first=Elapsed(view);Check(Mathf.Abs(first-.375f)<.0001f,"quarter duration");
                    Check(Percent(view)==(scenario=="won"?"16%":"6%"),"slow start easing");
                    view.Apply(new ViewUpdate{snapshot=state});Check(Elapsed(view)==first,"duplicate snapshot does not restart");
                    view.SetForeground(false);Call(view,"TickSettlement",1f);Check(Elapsed(view)==first,"background freezes");view.SetForeground(true);
                    Call(view,"OnApplicationPause",true);Call(view,"TickSettlement",1f);Check(Elapsed(view)==first,"application pause freezes");Call(view,"OnApplicationPause",false);
                    Call(view,"TickSettlement",.375f);Check(Percent(view)==(scenario=="won"?"50%":"19%")&&Buttons(view)==0,"halfway value and buttons hidden");
                    Check(Marker(view).anchoredPosition.x>26&&Marker(view).anchoredPosition.x<290,"marker follows progress");
                    CheckGradient(view,scenario=="won"?.5f:23f/61*.5f);
                    Capture.Capture(view,1080,1920,path+"/"+scenario+"-mid.png");
                    Call(view,"TickSettlement",.375f);Check(Percent(view)==(scenario=="won"?"84%":"32%"),"slow finish easing");
                    Call(view,"TickSettlement",.375f);Check(Percent(view)==(scenario=="won"?"100%":"38%")&&Buttons(view)==3,"final percentage and three buttons at 1.5 seconds");
                    if(scenario=="won")CheckMarkerEdge(view,true);
                    CheckGradient(view,scenario=="won"?1:23f/61);
                    Capture.Capture(view,1080,1920,path+"/"+scenario+"-final.png");
                    view.Apply(new ViewUpdate{snapshot=state});Check(Buttons(view)==3&&Elapsed(view)==1.5f,"completed snapshot remains completed");
                    int shareCount=0;Action onShare=()=>shareCount++;view.ShareRequested+=onShare;
                    Flow(view).GetComponentsInChildren<Button>().Single(b=>b.name=="Action_分享").onClick.Invoke();view.ShareRequested-=onShare;
                    Check(shareCount==1,"share button uses existing share event");
                    var share=view.ShowThemeShareAsync("");
                    Check(view.GetComponentsInChildren<Text>().Any(t=>t.text=="普通分享 · 开发模拟"),"ordinary share simulation copy");
                    view.GetComponentsInChildren<Button>().Single(b=>b.name=="Action_关闭").onClick.Invoke();
                    Check(share.IsCompleted&&Buttons(view)==3&&Elapsed(view)==1.5f,"share returns to completed settlement without replay");
                    state=Capture.Fixture();state.sessionId="next-"+scenario;view.Apply(new ViewUpdate{snapshot=state});Check(Elapsed(view)==0,"new session cancels animation");
                }
                var zero=Capture.Fixture();zero.phase=ViewPhase.Overflow;zero.totalOrders=0;view.Bind(new Capture.CapturePort(zero));Call(view,"TickSettlement",1.5f);Check(Percent(view)=="0%"&&Buttons(view)==3,"zero denominator safe");
                view.ResetView();Check(Elapsed(view)==0,"reset cancels old animation");
                view.Bind(null);
                File.WriteAllText(path+"/result.txt","PASS: Unity PlayMode renderer with deterministic presentation fixture. Three terminal states, 1.5s smoothstep, hidden buttons, duplicate snapshots, background pause/resume, new session/reset and zero denominator. Screenshots are actual runtime UI; not device or human acceptance.");
                Debug.Log("TASK027_SETTLEMENT_CAPTURE_PASS");
            }
            catch(Exception e){exit=1;Debug.LogException(e);}
            finally{SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
        }
    }
}
#endif
