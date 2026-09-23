#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using HotpotSort.Contracts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Capture=HotpotSort.Task001V7.VisualCapture;

namespace HotpotSort.Presentation
{
    // Focused presentation fixtures: no reward, inventory or queue mutation.
    [InitializeOnLoad]
    public static class Task011FlightClearDiagnostic
    {
        const string Key="Hotpot.Task011.FlightClear";
        static readonly List<string> checks=new List<string>();
        static Task011FlightClearDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-visualEvidence");
            if(index<0||index+1>=args.Length)throw new ArgumentException("-visualEvidence required");
            Directory.CreateDirectory(args[index+1]);SessionState.SetString(Key+"Path",args[index+1]);SessionState.SetBool(Key,true);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();
        }
        static void Check(bool ok,string label){checks.Add((ok?"PASS ":"FAIL ")+label);if(!ok)throw new Exception(label);}
        static void Tick(GameplayFeedback feedback,float duration){while(duration>.00001f){float dt=Mathf.Min(.01f,duration);feedback.Tick(dt);duration-=dt;}}
        static ViewSnapshot State(long generation)
        {
            return new ViewSnapshot{sessionId="task011",sessionGeneration=generation,revision=1,phase=ViewPhase.Running,
                orders=Enumerable.Range(0,4).Select(i=>new ViewOrder{slot=i,foodId=15,required=3,enabled=i<3}).ToArray(),
                buffer=Enumerable.Range(0,5).Select(i=>new ViewItem{itemId="buffer-"+i,foodId=i*3}).ToArray()};
        }
        static ViewSnapshot Clone(ViewSnapshot state)=>JsonUtility.FromJson<ViewSnapshot>(JsonUtility.ToJson(state));
        static ViewEvent Event(ViewSnapshot state,long sequence,string kind)=>new ViewEvent{sessionId=state.sessionId,sessionGeneration=state.sessionGeneration,sequence=sequence,kind=kind,transactionId="test-"+sequence};
        static RectTransform[] ClearItems(GameplayView view)=>view.GetComponentsInChildren<RectTransform>().Where(n=>n.name.StartsWith("ClearBufferItem_")).ToArray();
        static Capture.FullPort StartClear(GameplayView view,long generation,out ViewEvent clearEvent)
        {
            var before=State(generation);var port=new Capture.FullPort(before,true);view.Bind(port);
            port.state=Clone(before);port.state.buffer=new ViewItem[5];clearEvent=Event(port.state,1,"BufferReturnedToQueue");port.Emit(new[]{clearEvent});return port;
        }
        static void Shot(GameplayView view,string path,string name)=>Capture.Capture(view,1080,1920,Path.Combine(path,name+".png"));
        static void Execute()
        {
            int exit=0;string path=SessionState.GetString(Key+"Path","");GameObject owner=null;
            try
            {
                checks.Clear();owner=new GameObject("Task011FlightClearFixture");var view=owner.AddComponent<GameplayView>();view.ConfigureAssets(V7Art.Root);
                var feedback=view.GetComponent<GameplayFeedback>();feedback.enabled=false;view.SetForeground(true);
                var state=State(1);state.buffer=new ViewItem[5];view.Bind(new Capture.FullPort(state,true));view.World.SetSimulating(false);
                var point=typeof(GameplayFeedback).GetMethod("FlightPoint",BindingFlags.NonPublic|BindingFlags.Static);
                Func<Vector2,Vector2,float,float,Vector2> evaluate=(a,b,t,arc)=>(Vector2)point.Invoke(null,new object[]{a,b,t,arc});
                var from=new Vector2(210,580);var pot=view.VisualArt.Pot(1);var buffer=view.VisualArt.Buffer(2);
                foreach(var destination in new[]{pot,buffer})
                {
                    float arc=destination==pot?78:0;
                    Check(Vector2.Distance(evaluate(from,destination,0,arc),from)<.001f&&Vector2.Distance(evaluate(from,destination,1,arc),destination)<.001f,"flight exact endpoints "+arc);
                }
                for(int i=0;i<=20;i++)
                {
                    var p=evaluate(from,buffer,i/20f,0);var d=buffer-from;
                    Check(Mathf.Abs((p.x-from.x)*d.y-(p.y-from.y)*d.x)<.02f,"buffer collinear sample "+i);
                }
                Check(Vector2.Distance(evaluate(from,new Vector2(210,160),.5f,78),new Vector2(210,370))>77,"vertical order flight still visibly curved");
                foreach(string destination in new[]{"Order","Buffer"})
                {
                    state=State(destination=="Order"?2:3);state.buffer=new ViewItem[5];view.Bind(new Capture.FullPort(state,true));
                    var evt=Event(state,1,destination=="Order"?"ItemRoutedToOrder":"ItemRoutedToBuffer");evt.sourceContainer="Plate";evt.targetContainer=destination;evt.targetSlot=destination=="Order"?1:2;evt.itemId="flight";evt.ingredientId="food_00";
                    feedback.Apply(new ViewUpdate{snapshot=state,events=new[]{evt}},state,_=>from);Tick(feedback,.17f);
                    var node=view.GetComponentsInChildren<RectTransform>().Single(n=>n.name=="FlyingItem");
                    var expected=evaluate(from,destination=="Order"?pot:buffer,.5f,destination=="Order"?78:0);
                    Check(Vector2.Distance(node.anchoredPosition,new Vector2(expected.x,-expected.y))<.02f,"live flight midpoint "+destination);Shot(view,path,"flight-"+destination.ToLowerInvariant());
                    Tick(feedback,.16f);Check(node.gameObject.activeSelf,"flight still present at .33 seconds "+destination);
                    Tick(feedback,.02f);Check(!node.gameObject.activeSelf||node.name!="FlyingItem","flight landed after .34 seconds "+destination);
                }
                ViewEvent clearEvent;var clearPort=StartClear(view,4,out clearEvent);
                Check(feedback.ClearTransportCount==1&&ClearItems(view).Length==5,"clear creates one plate and all five foods");
                Check(ClearItems(view).All(n=>!n.GetComponent<RawImage>().raycastTarget),"clear replicas never intercept taps");Shot(view,path,"clear-start");
                clearPort.Emit(new[]{clearEvent});Check(feedback.ClearTransportCount==1,"duplicate queue-return event ignored");
                Tick(feedback,.3f);Shot(view,path,"clear-gathering");var positions=ClearItems(view).Select(n=>n.anchoredPosition).ToArray();
                clearPort.state.pauseReasons=ViewPauseReasons.User;clearPort.Emit(Array.Empty<ViewEvent>());Tick(feedback,.4f);
                Check(ClearItems(view).Select(n=>n.anchoredPosition).SequenceEqual(positions),"user pause freezes clear");
                clearPort.state.pauseReasons=ViewPauseReasons.Background;clearPort.Emit(Array.Empty<ViewEvent>());Tick(feedback,.4f);
                Check(ClearItems(view).Select(n=>n.anchoredPosition).SequenceEqual(positions),"background reason freezes clear");
                clearPort.state.pauseReasons=ViewPauseReasons.None;clearPort.Emit(Array.Empty<ViewEvent>());view.SetForeground(false);Tick(feedback,.4f);
                Check(ClearItems(view).Select(n=>n.anchoredPosition).SequenceEqual(positions),"foreground gate freezes clear");view.SetForeground(true);
                Tick(feedback,.26f);Shot(view,path,"clear-gathered");
                var anchors=view.VisualArt.Theme.anchors;var nodes=ClearItems(view);
                for(int i=0;i<nodes.Length;i++){var p=anchors.gatherItemSlots[i];Check(Vector2.Distance(nodes[i].anchoredPosition,new Vector2(anchors.revivalGatherPlate.x+p.x,-anchors.revivalGatherPlate.y-p.y))<.05f,"gather slot reached "+i);}
                Tick(feedback,.22f);Shot(view,path,"clear-departing");Tick(feedback,.2f);
                Check(feedback.ClearTransportCount==0&&clearPort.completions==0,"ordinary clear completes without revival callback");
                Check(clearPort.state.buffer.All(i=>i==null)&&clearPort.state.plates.Length==0,"presentation does not insert active plates or refill buffer");
                StartClear(view,5,out clearEvent);view.Bind(new Capture.FullPort(State(6),true));Check(feedback.ClearTransportCount==0,"session replacement cancels clear");
                clearPort=StartClear(view,7,out clearEvent);clearPort.state.phase=ViewPhase.Aborted;clearPort.Emit(Array.Empty<ViewEvent>());Check(feedback.ClearTransportCount==0,"abort cancels clear");
                StartClear(view,8,out clearEvent);view.ResetView();Check(feedback.ClearTransportCount==0,"retry/reset cancels clear");
                state=State(9);var revivalPort=new Capture.FullPort(state,true);view.Bind(revivalPort);revivalPort.state=Clone(state);revivalPort.state.buffer=new ViewItem[5];
                state=revivalPort.state;state.revivalPending=state.revivalUsed=true;state.pauseReasons=ViewPauseReasons.Revival;
                var batch=new RevivalTransferBatch{token=new RevivalCompletionToken{sessionId=state.sessionId,sessionGeneration=9,completionToken="exact-token",transactionId="revive",eventSeq=2},items=Enumerable.Range(0,5).Select(i=>new RevivalTransferItem{itemId="buffer-"+i,ingredientId="food_"+(i*3).ToString("00"),sourceSlot=i,targetIndex=i}).ToArray()};
                state.revivalTransfer=batch;var returned=Event(state,1,"BufferReturnedToQueue");var revival=Event(state,2,"RevivalTransferStarted");revival.revivalTransfer=batch;
                revivalPort.Emit(new[]{returned,revival});Check(feedback.ClearTransportCount==0&&feedback.RevivalActive,"revival queue-return does not start ordinary clear");
                Tick(feedback,.82f);Check(revivalPort.completions==1&&!feedback.RevivalActive,"revival exact-token completion remains once");
                revivalPort.Emit(new[]{returned,revival});Check(feedback.ClearTransportCount==0&&!feedback.RevivalActive&&revivalPort.completions==1,"duplicate revival batch never replays");
                state=State(10);state.buffer=new ViewItem[5];view.Bind(new Capture.FullPort(state,true));
                for(int i=1;i<=100;i++){var evt=Event(state,i,"ItemRoutedToBuffer");evt.ingredientId="food_00";evt.sourceContainer="Plate";evt.targetContainer="Buffer";evt.targetSlot=0;feedback.Apply(new ViewUpdate{snapshot=state,events=new[]{evt}},state,_=>from);}
                Check(feedback.PoolSize<=64&&feedback.ActiveEffectCount<=64,"existing effect pool cap retained");feedback.ResetFeedback();Check(feedback.ClearTransportCount==0&&feedback.PoolSize==0,"reset removes all owned effects");
                Debug.Log("TASK011_FLIGHT_CLEAR_PASS checks="+checks.Count);
            }
            catch(Exception ex){exit=1;Debug.LogException(ex);File.WriteAllText(Path.Combine(path,"error.txt"),ex.ToString());}
            finally{File.WriteAllLines(Path.Combine(path,"checks.txt"),checks);if(owner)UnityEngine.Object.Destroy(owner);SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
        }
    }
}
#endif
