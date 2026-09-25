#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Capture=HotpotSort.Task001V7.VisualCapture;
namespace HotpotSort.Presentation
{
    [InitializeOnLoad] public static class Task035BrandDiagnostic
    {
        const string Key="Task035.BrandDiagnostic";
        static Task035BrandDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run(){SessionState.SetBool(Key,true);UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);EditorApplication.EnterPlaymode();}
        static void Execute()
        {
            var root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../.harness/qa/TASK-035"));Directory.CreateDirectory(root);int exit=0;
            try
            {
                var configType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("HotpotSort.Platform.WeChatRuntimeConfig")).First(t=>t!=null);
                var share=(string)configType.GetField("shareTitle").GetValue(Activator.CreateInstance(configType));
                if(share!="来《一锅又一锅》，一起开锅！")throw new Exception("Share title mismatch");
                foreach(var assetRoot in new[]{PresentationAssets.CandidateRoot,V7Art.Root,""})
                {
                    var view=new GameObject("Task035BrandFixture").AddComponent<GameplayView>();
                    view.ConfigureAssets(assetRoot);view.Bind(null);
                    foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1440,3200)})
                    {
                        string mode=assetRoot==PresentationAssets.CandidateRoot?"current":assetRoot==V7Art.Root?"v7":"legacy";
                        Capture.Capture(view,size.x,size.y,root+"/entry-"+mode+"-"+size.x+"x"+size.y+".png");
                        var title=view.GetComponentsInChildren<Text>().Single(t=>t.text.StartsWith("一锅又一锅"));
                        title.font.RequestCharactersInTexture(share,title.fontSize,title.fontStyle);
                        if(share.Any(c=>!title.font.HasCharacter(c)))throw new Exception(mode+" missing glyph");
                        if(title.cachedTextGenerator.lines.Count!=1||title.cachedTextGenerator.characterCountVisible!=title.text.Length)throw new Exception(mode+" title clipping");
                        if(view.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("火锅消消")))throw new Exception("Old visible name");
                        Debug.Log("TASK035 PASS "+mode+" "+size+" title="+title.text+" width="+title.preferredWidth+"/"+title.rectTransform.rect.width);
                    }
                    UnityEngine.Object.DestroyImmediate(view.gameObject);
                }
                File.WriteAllText(root+"/result.txt","PASS: Unity compile, current/v7/legacy title and glyph checks at 3 sizes; exact default share title. Offscreen PlayMode screenshots, not device evidence.");
            }
            catch(Exception e){exit=1;Debug.LogException(e);File.WriteAllText(root+"/result.txt",e.ToString());}
            finally{SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
        }
    }
}
#endif
