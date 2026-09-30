#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HotpotSort.Presentation
{
    [InitializeOnLoad] public static class Task029FoodHitDiagnostic
    {
        const string Key="Task029.FoodHit";
        static int checks;static GameplayView view;static Port port;static RectTransform board;
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        sealed class Port:IPresentationPort
        {
            public ViewSnapshot state;public int taps;public event Action<ViewUpdate> Updated;
            public ViewSnapshot Read()=>state;public void Tap(ViewTap command){taps++;}
            public void SessionAction(ViewAction action){}public void ObserveSupply(ViewSupplyObservation observation){}
            public void Emit()=>Updated?.Invoke(new ViewUpdate{snapshot=state});
        }
        static Task029FoodHitDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();}
        static void Check(bool pass,string label){if(!pass)throw new Exception(label);checks++;Debug.Log("TASK029_PASS "+label);}
        static Vector2 ScreenPoint(Vector2 point)=>RectTransformUtility.WorldToScreenPoint(null,board.TransformPoint(new Vector3(point.x,-point.y,0)));
        static string Hit(Vector2 point)
        {
            object[] args={ScreenPoint(point),-1,Vector2.zero,null};
            return (bool)typeof(GameplayView).GetMethod("TryScreenHit",Flags).Invoke(view,args)?(string)args[3]:null;
        }
        static bool Opaque(ViewItem item,Vector2 offset)=>(bool)typeof(GameplayView).GetMethod("OpaqueHit",Flags).Invoke(view,new object[]{item,offset});
        static ViewItem Item(string id,int food=0,float x=0,float y=0,float radius=20)=>new ViewItem{itemId=id,foodId=food,x=x,y=y,radius=radius};
        static ViewPlate Plate(string id,ViewItem[] items,float x=210,float y=550,float radius=70)=>new ViewPlate{plateId=id,x=x,y=y,radius=radius,motion=new ViewPlateMotion{animateEntry=false},items=items};
        static void State(params ViewPlate[] plates)
        {
            view.CancelScreenPress();view.SetForeground(true);
            port=new Port{state=new ViewSnapshot{sessionId=Guid.NewGuid().ToString(),sessionGeneration=1,phase=ViewPhase.Running,orders=new ViewOrder[0],plates=plates}};
            view.Bind(port);view.World.SetSimulating(false);view.SetViewport(new Rect(0,0,Screen.width,Screen.height),Screen.height);Canvas.ForceUpdateCanvases();
            board=view.GetComponentsInChildren<RectTransform>().First(n=>n.name=="GameplayBoard");
        }
        static async void Execute()
        {
            int exit=0;Texture2D square=null;
            try
            {
                view=new GameObject("Task029Fixture").AddComponent<GameplayView>();view.ConfigureAssets(V7Art.Root);
                double coldMax=0,warmMax=0;
                for(int food=0;food<16;food++)
                {
                    var item=Item("real",food);State(Plate("p",new[]{item}));
                    Check(view.FoodTexture(food)&&view.FoodTexture(food).isReadable,"formal readable food "+food);
                    float right=-20,row=0;bool found=false;
                    for(float y=-19.5f;y<20;y+=.5f)for(float x=19.99f;x>-20;x-=.5f)
                    {
                        if(!Opaque(item,new Vector2(x,y)))continue;
                        float lo=x,hi=Mathf.Min(20,x+.5f);for(int n=0;n<18;n++){float m=(lo+hi)*.5f;if(Opaque(item,new Vector2(m,y)))lo=m;else hi=m;}
                        if(!found||lo>right){found=true;right=lo;row=y;}break;
                    }
                    Check(found,"formal visible alpha "+food);var exact=new Vector2(210+right-.02f,550+row);int searches=view.TolerantHitSearches;
                    Check(Hit(exact)=="real"&&view.TolerantHitSearches==searches,"exact alpha priority "+food);
                    var near=new Vector2(210+right+8,550+row);
                    Check(Hit(near)=="real","formal transparent edge tolerance "+food);coldMax=Math.Max(coldMax,view.TolerantHitMilliseconds);
                    Check(Hit(near)=="real","cached formal contour "+food);warmMax=Math.Max(warmMax,view.TolerantHitMilliseconds);
                    Check(Hit(new Vector2(210+right+10,550+row))=="real","formal 10 pixel edge "+food);
                    Check(Hit(new Vector2(210+right+10.1f,550+row))==null,"formal beyond tolerance "+food);
                }
                Debug.Log("TASK029_TIMING coldMaxMs="+coldMax+" warmMaxMs="+warmMax);
                var foods=(Texture2D[])typeof(GameplayView).GetField("foods",Flags).GetValue(view);
                square=new Texture2D(8,8);square.SetPixels(Enumerable.Repeat(Color.white,64).ToArray());square.Apply();foods[0]=square;
                ViewItem box(string id,float x=0,float y=0,float r=10){var item=Item(id,0,x,y,r);item.layoutVersion="test";return item;}
                State(Plate("p",new[]{box("a")}));
                Check(Hit(new Vector2(230,550))=="a","10 logical pixels accepted");
                Check(Hit(new Vector2(230.1f,550))==null,"10.1 logical pixels rejected");
                State(Plate("p",new[]{box("near",-14),box("far",16)}));Check(Hit(new Vector2(210,550))=="near","nearest contour beats upper layer");
                State(Plate("p",new[]{box("exact"),box("near",15)}));int before=view.TolerantHitSearches;Check(Hit(new Vector2(210,550))=="exact"&&view.TolerantHitSearches==before,"exact lower food wins over nearby upper contour");
                State(Plate("p",new[]{box("lower",-15),box("upper",15)}));Check(Hit(new Vector2(210,550))=="upper","equal distance prefers upper layer");
                State(Plate("p",new[]{box("lower"),box("upper")}));Check(Hit(new Vector2(230,550))=="upper","same plate hidden food not selected");
                State(Plate("lower",new[]{box("lower")}),Plate("upper",new[]{box("far",-45)}));Check(Hit(new Vector2(230,550))==null,"upper plate blocks lower food tolerance");
                State(Plate("left",new[]{box("left",40)},160,550,60),Plate("right",new[]{box("far",30)},260,550,60));Check(Hit(new Vector2(225,550))==null,"no cross plate candidate");
                State(Plate("p",new[]{box("a",45)},210,550,55));Check(Hit(new Vector2(266,550))==null,"original touch outside plate rejected");
                State(Plate("p",new[]{box("a")},210,view.PlateCrop.yMin-15));Check(Hit(new Vector2(210,view.PlateCrop.yMin+1))==null,"hidden contour beyond crop rejected");
                State(Plate("p",new[]{box("a")},210,view.PlateCrop.yMin+12));Check(Hit(new Vector2(210,view.PlateCrop.yMin-1))==null,"original touch outside crop rejected");
                State(Plate("p",new[]{box("a")}));var nearPoint=new Vector2(229,550);var exactPoint=new Vector2(210,550);
                int haptics=0;view.HapticRequested+=kind=>haptics++;
                typeof(GameplayView).GetField("lastLightHapticAt",Flags).SetValue(view,double.NegativeInfinity);
                Check(view.BeginScreenPress(ScreenPoint(nearPoint),7)&&haptics==1&&port.taps==0,"tolerance reuses press and haptic without early submit");
                Check(view.EndScreenPress(ScreenPoint(nearPoint),7)&&port.taps==1,"tolerance release commits once");
                Check(Hit(nearPoint)==null,"pending flight excluded");view.AcknowledgeTap("a");
                Check(view.BeginScreenPress(ScreenPoint(nearPoint),7),"second press accepted");
                Check(!view.UpdateScreenPress(ScreenPoint(nearPoint+Vector2.right*15),7)&&!view.EndScreenPress(ScreenPoint(nearPoint),7)&&port.taps==1,"obvious drift cancels permanently");
                port.state.pauseReasons=ViewPauseReasons.User;port.Emit();Check(Hit(nearPoint)==null,"pause gate");port.state.pauseReasons=ViewPauseReasons.None;port.Emit();
                view.SetForeground(false);Check(Hit(nearPoint)==null,"background gate");view.SetForeground(true);
                port.state.phase=ViewPhase.Won;port.Emit();Check(Hit(nearPoint)==null,"terminal gate");
                State(Plate("p",new[]{box("a")}));view.ShowNotice("test","test");Check(Hit(nearPoint)==null,"modal gate");
                State(Plate("p",new[]{box("a")}));
                var blocker=new GameObject("Task029UI",typeof(RectTransform),typeof(Image));blocker.transform.SetParent(board,false);var rect=(RectTransform)blocker.transform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(229,-550);rect.sizeDelta=new Vector2(20,20);Canvas.ForceUpdateCanvases();await System.Threading.Tasks.Task.Delay(100);
                // Batchmode has no Game view render; force an offscreen UI draw so
                // GraphicRaycaster sees actual CanvasRenderer depths, not depth=-1.
                HotpotSort.Task001V7.VisualCapture.Capture(view,Screen.width,Screen.height,"../.harness/qa/TASK-029/ui-gate.png");
                Check(Hit(nearPoint)==null,"foreground UI blocks original touch");
                rect.anchoredPosition=new Vector2(210,-550);rect.sizeDelta=new Vector2(22,22);Canvas.ForceUpdateCanvases();
                HotpotSort.Task001V7.VisualCapture.Capture(view,Screen.width,Screen.height,"../.harness/qa/TASK-029/ui-contour-gate.png");
                Check(Hit(nearPoint)==null,"UI-hidden contour is not a tolerance candidate");UnityEngine.Object.DestroyImmediate(blocker);
                Check(Hit(exactPoint)=="a","exact path remains after gates reopen");
                var viewportField=typeof(GameplayView).GetField("viewport",Flags);var viewport=viewportField.GetValue(view);viewportField.SetValue(view,new Rect(0,0,1,1));Check(Hit(nearPoint)==null,"viewport gate");viewportField.SetValue(view,viewport);
                Debug.Log("TASK029_FOOD_HIT_PASS checks="+checks);
            }
            catch(Exception error){exit=1;Debug.LogException(error);}
            finally{if(view)UnityEngine.Object.DestroyImmediate(view.gameObject);if(square)UnityEngine.Object.DestroyImmediate(square);SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
        }
    }
}
#endif
