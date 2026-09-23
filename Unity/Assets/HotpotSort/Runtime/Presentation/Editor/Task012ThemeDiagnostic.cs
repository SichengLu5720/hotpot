#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace HotpotSort.Presentation
{
    [InitializeOnLoad]
    public static class Task012ThemeDiagnostic
    {
        const string RunKey="Hotpot.Task012.ThemeDiagnostic";
        static Task012ThemeDiagnostic(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(RunKey,false)){SessionState.SetBool(RunKey,false);Execute();}};}
        static void Check(bool result,string label){if(!result)throw new Exception(label);Debug.Log("TASK012 PASS "+label);}
        public static void Run()
        {
            SessionState.SetBool(RunKey,true);
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
        static void Execute()
        {
            int exit=0;GameObject owner=null;
            try
            {
                Check(TaskAssetValidation.Validate(V7Art.Root,true).Length==0,"v7 full asset validation");
                var old=new V7Art(V7Art.Root);
                for(int i=0;i<16;i++)Check(old.FoodUv(i)==V7Art.FoodUV(i),"legacy UV "+i);
                Check(old.Skin("ui.panel")!=null,"legacy nine-slice");
                Check(PresentationAssets.IsRemote(PresentationAssets.CandidateRoot+"/food/food_00"),"v10 food remote");
                Check(PresentationAssets.IsRemote(PresentationAssets.CandidateRoot+"/hero/win"),"v10 win remote");
                Check(!PresentationAssets.IsRemote(PresentationAssets.CandidateRoot+"/background/table"),"v10 background local");
                var theme=JsonUtility.FromJson<PresentationTheme>(JsonUtility.ToJson(old.Theme));
                theme.schemaVersion="presentation_theme_v10";theme.resourceRoot=PresentationAssets.CandidateRoot;
                foreach(var a in theme.assets)a.resourceAddress=a.resourceAddress.Replace(V7Art.Root,theme.resourceRoot);
                theme.foodUvs=new ThemeFoodUv[16];
                for(int i=0;i<16;i++)theme.foodUvs[i]=new ThemeFoodUv{id=i,width=1,height=1};
                theme.slices=new[]{new ThemeSlice{key="ui.panel",width=100,height=100,left=10,bottom=10,right=10,top=10}};
                theme.colors=new[]{new ThemeColor{key="test",r=1,g=1,b=1,a=1}};
                Check(PresentationThemeValidation.Validate(theme,theme.resourceRoot).Length==0,"v10 contract");
                theme.foodUvs[15].id=0;
                Check(PresentationThemeValidation.Validate(theme,theme.resourceRoot).Length>0,"duplicate UV rejected");
                Check(TaskAssetValidation.Validate(PresentationAssets.CandidateRoot,true).Length==0,"v10 complete assets");
                const string missingRoot="Hotpot/TASK001/v999/r999";
                Check(TaskAssetValidation.Validate(missingRoot,true).Length>0,"absent theme explicitly fails");
                owner=new GameObject("Task012ThemeFixture");var view=owner.AddComponent<GameplayView>();
                view.ConfigureAssets(V7Art.Root);
                bool rejected=false;try{view.ConfigureAssets(missingRoot);}catch(InvalidOperationException){rejected=true;}
                Check(rejected&&view.AssetRoot==V7Art.Root&&view.VisualArt.Theme.resourceRoot==V7Art.Root,"failed candidate keeps active v7");
                old.Dispose();Debug.Log("TASK012 THEME DIAGNOSTIC PASSED");
            }
            catch(Exception e){exit=1;Debug.LogException(e);}
            finally{if(owner)UnityEngine.Object.DestroyImmediate(owner);EditorApplication.Exit(exit);}
        }
    }
}
#endif
