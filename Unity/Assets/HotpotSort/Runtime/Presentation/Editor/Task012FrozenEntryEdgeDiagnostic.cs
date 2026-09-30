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
    [InitializeOnLoad] public static class Task012FrozenEntryEdgeDiagnostic
    {
        const string Key="Task012.FrozenEntryEdge";
        static readonly List<string> checks=new List<string>();
        static Task012FrozenEntryEdgeDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run(){SessionState.SetBool(Key,true);UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);EditorApplication.EnterPlaymode();}
        static void Check(bool value,string label){checks.Add((value?"PASS ":"FAIL ")+label);if(!value)throw new Exception(label);}
        static void RectCheck(Transform parent,string name,Rect expected){var node=parent.Find(name) as RectTransform;Check(node&&node.anchoredPosition==new Vector2(expected.x,-expected.y)&&node.sizeDelta==expected.size,"unchanged entry rect "+name);}
        static void Execute()
        {
            var args=Environment.GetCommandLineArgs();string root=args[Array.IndexOf(args,"-visualEvidence")+1];Directory.CreateDirectory(root);int exit=0;GameplayView view=null;
            try{
                foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1440,3200)})
                {
                    if(view){view.Bind(null);UnityEngine.Object.DestroyImmediate(view.gameObject);}
                    view=new GameObject("FrozenEntryEdgeFixture").AddComponent<GameplayView>();view.ConfigureAssets(PresentationAssets.CandidateRoot);
                    view.Bind(null);Capture.Capture(view,size.x,size.y,root+"/entry-"+size.x+"x"+size.y+".png");
                    var entry=view.GetComponentsInChildren<RectTransform>().Single(t=>t.name=="Entry");
                    Check(!entry.Find("DailySign"),"daily plaque absent "+size);
                    Check(!entry.GetComponentsInChildren<Text>().Any(t=>t.text.Replace(" ","")=="每日挑战"||t.text.Contains("北京时间")),"daily and update text absent "+size);
                    RectCheck(entry,"RestaurantSign",new Rect(36,78,348,155));RectCheck(entry,"RestaurantSeal",new Rect(174,38,72,72));RectCheck(entry,"EntryHero",new Rect(-12,224,444,444));RectCheck(entry,"RestaurantCounter",new Rect(52,676,316,153));
                    RectCheck(entry,"Action_开始下火锅",new Rect(70,700,280,58));RectCheck(entry,"Action_设置",new Rect(70,772,133,43));RectCheck(entry,"Action_好友榜",new Rect(217,772,133,43));
                    var s=Capture.Fixture();view.Bind(new Capture.FullPort(s,false));view.SetForeground(false);Capture.Capture(view,size.x,size.y,root+"/play-"+size.x+"x"+size.y+".png");
                    var canvas=view.GetComponentInChildren<Canvas>().transform;var edge=canvas.Find("EdgeFestivity").GetComponent<RawImage>();var r=edge.rectTransform;
                    Check(edge.enabled&&!edge.raycastTarget&&edge.color==Color.white,"whole texture visible original alpha no raycast "+size);
                    Check(edge.uvRect==new Rect(0,0,1,1)&&r.anchorMin==Vector2.zero&&r.anchorMax==Vector2.one&&r.offsetMin==Vector2.zero&&r.offsetMax==Vector2.zero,"existing complete UV and full-stretch rect unchanged "+size);
                    Check(edge.texture==view.VisualArt.Texture("background.edge_cloth"),"original frozen edge texture "+size);
                    Check(edge.transform.parent==canvas&&edge.GetComponentsInParent<RectMask2D>().Length==0&&edge.GetComponentsInParent<Mask>().Length==0,"whole edge outside all masks "+size);
                    Check(edge.transform.GetSiblingIndex()>canvas.Find("CreamBackground").GetSiblingIndex()&&edge.transform.GetSiblingIndex()<canvas.Find("SafeContent").GetSiblingIndex(),"edge between table and every gameplay layer "+size);
                    Check(view.GetComponentsInChildren<RawImage>().Where(i=>i.name.StartsWith("ThemeEdgeGutter_")).All(i=>!i.enabled),"partial bands inactive during gameplay "+size);
                    Check(view.PlateCrop==new Rect(0,292,420,536),"gameplay crop preserved "+size);
                }
                Debug.Log("TASK012_FROZEN_ENTRY_EDGE_OK");
            }catch(Exception e){exit=1;Debug.LogException(e);File.WriteAllText(root+"/error.txt",e.ToString());}
            finally{if(view)view.Bind(null);File.WriteAllLines(root+"/checks.txt",checks);SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
        }
    }
}
#endif
