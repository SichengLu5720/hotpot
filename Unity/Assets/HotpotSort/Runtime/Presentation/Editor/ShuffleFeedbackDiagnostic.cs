#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HotpotSort.Presentation
{
    [InitializeOnLoad]
    public static class ShuffleFeedbackDiagnostic
    {
        const string Key="Hotpot.ShuffleFeedbackDiagnostic";
        static ShuffleFeedbackDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Check();};}
        public static void Run()
        {
            SessionState.SetBool(Key,true);
            var args=Environment.GetCommandLineArgs();SessionState.SetString(Key+"Path",args[Array.IndexOf(args,"-task001Report")+1]);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();
        }
        sealed class Port : IPresentationPort
        {
            public ViewSnapshot snapshot;
            public event Action<ViewUpdate> Updated {add{} remove{}}
            public ViewSnapshot Read()=>snapshot;
            public void SessionAction(ViewAction action){}
            public void Tap(ViewTap command){throw new Exception("decorative shuffle position accepted a tap");}
            public void ObserveSupply(ViewSupplyObservation observation){}
        }
        static void Require(bool value,string why){if(!value)throw new Exception(why);}
        static void Tick(GameplayView view,float dt)=>typeof(GameplayView).GetMethod("TickShuffleFeedback",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,new object[]{dt});
        static void Check()
        {
            int code=0;string result;
            try
            {
                var view=new GameObject("ShuffleDiagnostic").AddComponent<GameplayView>();
                var snapshot=new ViewSnapshot {sessionId="shuffle-check",sessionGeneration=1,phase=ViewPhase.Running,
                    plates=Enumerable.Range(0,6).Select(i=>new ViewPlate {plateId=(i+1).ToString(),x=80+i%3*125,y=430+i/3*180,radius=39,
                    motion=new ViewPlateMotion{animateEntry=false},items=new[]{new ViewItem{itemId=(i+1).ToString(),foodId=i,radius=16}}}).ToArray()};
                view.Bind(new Port{snapshot=snapshot});view.World.SetSimulating(false);
                var before=view.World.Bodies.ToDictionary(b=>b.data.plateId,b=>view.World.Position(b));
                Require(view.TryShuffleWithFeedback()&&view.ShuffleFeedbackActive,"successful shuffle lacks feedback");
                Require(!view.TryShuffleWithFeedback(),"repeat shuffle replaced active feedback");
                Require(view.World.Bodies.Any(b=>Vector2.Distance(before[b.data.plateId],view.World.Position(b))>.01f),"no actual shuffle");
                Require(view.World.Bodies.All(b=>b.data.radius==39&&Mathf.Abs(b.rim.radius-.39f)<.0001f&&b.data.items.Length==1),"physical radius or inventory changed");
                Require(!view.IsItemClickable("1")&&!view.SubmitScreenTap(Vector2.zero),"in-motion proxy accepts click");
                Tick(view,.1f);Tick(view,.1f);
                var age=typeof(GameplayView).GetField("shuffleAge",BindingFlags.NonPublic|BindingFlags.Instance);
                float elapsed=(float)age.GetValue(view);snapshot.pauseReasons=ViewPauseReasons.Background;
                Tick(view,.1f);Require((float)age.GetValue(view)==elapsed,"background advances animation");
                snapshot.pauseReasons=ViewPauseReasons.None;for(int i=0;i<4;i++)Tick(view,.1f);
                Require(!view.ShuffleFeedbackActive,"feedback never completes");
                Require(view.TryShuffleWithFeedback(),"subsequent shuffle failed");view.Bind(null);
                Require(!view.ShuffleFeedbackActive,"unbind retained feedback");
                Require(!view.TryShuffleWithFeedback(),"empty world played shuffle");
                result="SHUFFLE_FEEDBACK_PASS checks=9 actualUnityPhysics=true externalRequests=0";Debug.Log(result);
            }
            catch(Exception ex){code=1;result=ex.ToString();Debug.LogException(ex);}
            SessionState.SetBool(Key,false);File.WriteAllText(SessionState.GetString(Key+"Path",""),result);EditorApplication.Exit(code);
        }
    }
}
#endif
