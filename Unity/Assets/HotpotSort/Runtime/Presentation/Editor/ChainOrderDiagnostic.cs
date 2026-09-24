#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Capture=HotpotSort.Task001V7.VisualCapture;
namespace HotpotSort.Presentation
{
    [InitializeOnLoad] public static class ChainOrderDiagnostic
    {
        const string Key="Hotpot.ChainOrder";
        static ChainOrderDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();}
        static void Check(bool pass,string label){if(!pass)throw new Exception(label);Debug.Log("CHAIN_ORDER_PASS "+label);}
        static void Tick(GameplayFeedback f,float seconds){while(seconds>.00001f){var dt=Mathf.Min(.01f,seconds);f.Tick(dt);seconds-=dt;}}
        static void Execute()
        {
            int exit=0;GameObject owner=null;
            try
            {
                owner=new GameObject("ChainOrderFixture");var view=owner.AddComponent<GameplayView>();view.ConfigureAssets(V7Art.Root);view.SetForeground(true);
                var f=view.GetComponent<GameplayFeedback>();f.enabled=false;
                var s=new ViewSnapshot{sessionId="chain",sessionGeneration=1,phase=ViewPhase.Running,orders=Enumerable.Range(0,4).Select(i=>new ViewOrder{slot=i,enabled=i<2,foodId=0,count=i==0?2:0,required=3}).ToArray(),buffer=new ViewItem[5]};
                var port=new Capture.FullPort(s,true);view.Bind(port);view.World.SetSimulating(false);
                long seq=0;
                Func<string,int,ViewEvent> evt=(kind,food)=>new ViewEvent{sessionId=s.sessionId,sessionGeneration=s.sessionGeneration,sequence=++seq,kind=kind,slot=0,ingredientId="food_"+food.ToString("00"),targetSlot=0,targetContainer="Order",sourceContainer=kind=="BufferAutoAbsorbed"?"Buffer":"Plate",sourceSlot=0,itemId="route-"+seq,filledAfter=3};
                var route=evt("ItemRoutedToOrder",0);var a=evt("OrderCompleted",0);var b=evt("OrderCreated",1);
                var r1=evt("BufferAutoAbsorbed",1);r1.filledAfter=1;var r2=evt("BufferAutoAbsorbed",1);r2.filledAfter=2;var r3=evt("BufferAutoAbsorbed",1);
                var doneB=evt("OrderCompleted",1);var c=evt("OrderCreated",2);
                // New snapshot instance is required: previous must retain order A.
                var final=JsonUtility.FromJson<ViewSnapshot>(JsonUtility.ToJson(s));final.orders[0].foodId=2;final.orders[0].count=0;port.state=final;
                var events=new[]{route,a,b,r1,r2,r3,doneB,c};port.Emit(events);
                Check(f.DisplayOrder(0,final.orders[0]).foodId==0&&!f.IsServing(0),"A held while last food flies");
                port.Emit(events);Check(f.PendingServeCount(0)==2,"duplicate batch ignored");
                Tick(f,.30f);Check(!f.IsServing(0),"no serve before direct arrival");Tick(f,.06f);Check(f.IsServing(0),"A serves after arrival");
                Tick(f,.51f);Check(f.DisplayOrder(0,final.orders[0]).foodId==1,"replacement shows B not final C");
                Capture.Capture(view,1080,1920,System.IO.Path.GetFullPath("../.harness/qa/TASK-020/chain-B-entering.png"));
                Tick(f,.44f);Check(f.PendingDeferredRouteCount(0)==0&&f.DisplayOrder(0,final.orders[0]).count==0,"B settled before buffer flights");
                final.pauseReasons=ViewPauseReasons.User;port.Emit(Array.Empty<ViewEvent>());Tick(f,1);Check(f.DisplayOrder(0,final.orders[0]).count==0,"pause freezes arrivals");
                final.pauseReasons=ViewPauseReasons.None;port.Emit(Array.Empty<ViewEvent>());Tick(f,.48f);
                Check(f.IsServing(0)&&f.DisplayOrder(0,final.orders[0]).foodId==1&&f.DisplayOrder(0,final.orders[0]).count==3,"B fills then serves");
                Capture.Capture(view,1080,1920,System.IO.Path.GetFullPath("../.harness/qa/TASK-020/chain-B-serving.png"));
                Tick(f,.51f);Check(f.DisplayOrder(0,final.orders[0]).foodId==2,"C only after B leaves");Tick(f,.5f);Check(!f.IsServing(0)&&f.PendingServeCount(0)==0,"chain drains");
                Capture.Capture(view,1080,1920,System.IO.Path.GetFullPath("../.harness/qa/TASK-020/chain-C-settled.png"));
                port.Emit(new[]{evt("OrderCompleted",2)});final.sessionGeneration=2;port.Emit(Array.Empty<ViewEvent>());Check(!f.IsServing(0)&&f.PendingServeCount(0)==0,"new session cancels chain");
                Debug.Log("CHAIN_ORDER_ALL_PASS");
            }
            catch(Exception e){exit=1;Debug.LogException(e);}
            finally{if(owner)UnityEngine.Object.Destroy(owner);SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
        }
    }
}
#endif
