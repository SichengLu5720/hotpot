#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using HotpotSort.Determinism;
using HotpotSort.Presentation;
using HotpotSort.UnityPhysics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HotpotSort.Bootstrap
{
    [InitializeOnLoad]
    public static class Task038PlateSettlingDiagnostic
    {
        const string Key="Task038.PlateSettling";
        const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        static Task038PlateSettlingDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Verify();};}
        public static void Run()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-task038Report");
            SessionState.SetString(Key+".Report",index>=0&&index+1<args.Length?args[index+1]:"");SessionState.SetBool(Key,true);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();
        }
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        static string F(float value)=>value.ToString("R",CultureInfo.InvariantCulture);
        static void Step(PlatePresentationWorld world,int count=1){var method=typeof(PlatePresentationWorld).GetMethod("FixedUpdate",Private);for(int i=0;i<count;i++)method.Invoke(world,null);}
        static Vector2 Geometry(PlatePresentationWorld world)=>(Vector2)typeof(PlatePresentationWorld).GetMethod("MeasureGeometry",Private,null,Type.EmptyTypes,null).Invoke(world,null);
        static ViewPlate Plate(int id,float x,float y,float radius=39)=>new ViewPlate{plateId=id.ToString(),x=x,y=y,radius=radius,
            motion=new ViewPlateMotion{animateEntry=false},items=new[]{new ViewItem{itemId="item-"+id,radius=12}}};
        static PlatePresentationWorld World(out GameObject node)
        {node=new GameObject("Task038PlateSettlingWorld");var world=node.AddComponent<PlatePresentationWorld>();world.enabled=false;world.SetSimulating(true);return world;}
        static void Settle(PlatePresentationWorld world,int expectedCount,int limit=1200)
        {
            for(int i=0;i<limit&&!world.Bodies.All(b=>b.rigidbody.IsSleeping());i++)Step(world);
            Check(world.Bodies.Count()==expectedCount&&world.Bodies.All(b=>b.rigidbody.IsSleeping()),"board did not sleep");
            Check(world.Bodies.All(b=>b.rigidbody.linearVelocity==Vector2.zero&&b.rigidbody.angularVelocity==0),"sleep retained residual velocity");
            Check(world.HasSettledInitialBoard(expectedCount),"settled contract not published");
        }
        static void Verify()
        {
            int exit=0;var results=new List<object>();
            try
            {
                VerifyStableRestAndWake(results);
                VerifyDenseRest(results);
                VerifySupportRemoval(results);
                VerifyFloorStopAndPush(results);
                Debug.Log("TASK038_PLATE_SETTLING_PASS");
            }
            catch(Exception ex){exit=1;results.Add(CanonicalJson.Object("status","FAIL","error",ex.ToString()));Debug.LogException(ex);}
            finally
            {
                try
                {
                    string report=SessionState.GetString(Key+".Report","");
                    if(!string.IsNullOrEmpty(report)){string directory=Path.GetDirectoryName(report);if(!string.IsNullOrEmpty(directory))Directory.CreateDirectory(directory);File.WriteAllText(report,CanonicalJson.Write(CanonicalJson.Object("task","TASK-038","exitCode",exit,"scope","Direct isolated PlatePresentationWorld settling and wake behavior","results",results)));}
                }
                catch(Exception ex){exit=2;Debug.LogException(ex);}
                finally{SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
            }
        }
        static void VerifyStableRestAndWake(List<object> results)
        {
            GameObject node;var world=World(out node);
            try
            {
                var first=Plate(1,210,789);var snapshot=new ViewSnapshot{sessionId="rest-wake",plates=new[]{first}};
                world.Reconcile(snapshot);var body=world.Bodies.Single();
                Check(body.rim.sharedMaterial.friction==PlatePresentationWorld.PlateContactFriction&&body.rim.sharedMaterial.bounciness==PlatePresentationWorld.ContactBounciness,"plate contact tuning changed");
                Check(Mathf.Approximately(body.rim.radius,first.radius*.01f),"collision radius changed");
                Step(world,10);Check(!body.rigidbody.IsSleeping()&&!world.HasSettledInitialBoard(1),"board slept before about 0.3 seconds");
                Settle(world,1);var hitPoint=world.ItemPosition("item-1");Check(world.Hit(hitPoint)=="item-1","sleep changed click mapping");

                var incoming=Plate(2,100,500);snapshot.plates=new[]{first,incoming};world.Reconcile(snapshot);
                Check(world.Bodies.All(b=>!b.rigidbody.IsSleeping()),"new plate did not wake settled board");
                Step(world);Check(world.Bodies.Single(b=>b.data.plateId=="2").rigidbody.linearVelocity.y>0,"new plate did not enter native motion");

                first=Plate(1,200,789);first.motion.correctionRevision=1;snapshot.plates=new[]{first,incoming};world.Reconcile(snapshot);
                Check(world.Bodies.All(b=>!b.rigidbody.IsSleeping()),"snapshot correction did not wake board");
                var previousRandom=UnityEngine.Random.state;
                try{UnityEngine.Random.InitState(38038);Check(world.TryShuffle(true),"shuffle fixture failed");}
                finally{UnityEngine.Random.state=previousRandom;}
                Check(world.Bodies.All(b=>!b.rigidbody.IsSleeping()),"shuffle did not wake board");
                var geometry=Geometry(world);Check(geometry.x>=-.001f&&geometry.y>=-.001f,"shuffle introduced overlap or boundary escape");
                results.Add(CanonicalJson.Object("case","stable-rest-and-wake","status","PASS","settleSeconds",F(.3f),"plateFriction",F(PlatePresentationWorld.PlateContactFriction),"boundaryFriction",F(PlatePresentationWorld.BoundaryContactFriction),"bounciness",F(PlatePresentationWorld.ContactBounciness),"hitMappingPreserved",true,"newPlateWake",true,"correctionWake",true,"shuffleWake",true));
            }
            finally{UnityEngine.Object.DestroyImmediate(node);}
        }
        static void VerifyDenseRest(List<object> results)
        {
            GameObject node;var world=World(out node);
            try
            {
                var plates=new List<ViewPlate>();int id=0;
                foreach(float y in new[]{789f,711f,633f,555f})foreach(float x in new[]{80f,160f,240f,320f})plates.Add(Plate(++id,x,y));
                world.Reconcile(new ViewSnapshot{sessionId="dense-rest",plates=plates.ToArray()});Settle(world,plates.Count);
                var geometry=Geometry(world);Check(geometry.x>=-1f&&geometry.y>=-1f,"dense rest exceeded native contact tolerance");
                var positions=world.Bodies.Select(world.Position).ToArray();Step(world,100);
                Check(world.Bodies.Select(world.Position).SequenceEqual(positions),"sleeping dense board drifted");
                results.Add(CanonicalJson.Object("case","dense-rest","status","PASS","plates",plates.Count,"minimumPairGap",F(geometry.x),"minimumBoundaryGap",F(geometry.y),"postSleepSteps",100));
            }
            finally{UnityEngine.Object.DestroyImmediate(node);}
        }
        static void VerifySupportRemoval(List<object> results)
        {
            GameObject node;var world=World(out node);
            try
            {
                var lower=Plate(1,210,789);var upper=Plate(2,210,711);
                world.Reconcile(new ViewSnapshot{sessionId="support-removal",plates=new[]{lower,upper}});Settle(world,2);
                var upperBody=world.Bodies.Single(b=>b.data.plateId=="2");float before=world.Position(upperBody).y;
                world.Reconcile(new ViewSnapshot{sessionId="support-removal",plates=new[]{upper}});
                Check(!upperBody.rigidbody.IsSleeping(),"removed support did not wake remaining plate");Step(world,25);
                float drop=world.Position(upperBody).y-before;Check(drop>20,"remaining plate did not fall after support removal");
                results.Add(CanonicalJson.Object("case","support-removal","status","PASS","dropBoardUnits",F(drop)));
            }
            finally{UnityEngine.Object.DestroyImmediate(node);}
        }
        static void VerifyFloorStopAndPush(List<object> results)
        {
            GameObject node;var world=World(out node);
            try
            {
                var first=Plate(1,210,789);
                world.Reconcile(new ViewSnapshot{sessionId="floor-stop",plates=new[]{first}});Settle(world,1);
                var body=world.Bodies.Single();body.rigidbody.WakeUp();body.rigidbody.linearVelocity=new Vector2(.3f,0);
                Step(world);
                Check(Mathf.Abs(body.rigidbody.linearVelocity.x)<.0001f,"floor retained low-speed horizontal inertia");
                float stopped=world.Position(body).x;Step(world,10);
                Check(Mathf.Abs(world.Position(body).x-stopped)<.1f,"floor plate coasted after stopping");
                var incoming=Plate(2,stopped-78f,789);
                world.Reconcile(new ViewSnapshot{sessionId="floor-stop",plates=new[]{first,incoming}});
                var pusher=world.Bodies.Single(b=>b.data.plateId=="2");
                pusher.rigidbody.linearVelocity=new Vector2(1f,0);Step(world,10);
                float pushed=world.Position(body).x-stopped;
                Check(pushed>.05f,"floor stop prevented real collision displacement");
                var geometry=Geometry(world);Check(geometry.x>=-1f&&geometry.y>=-1f,"floor push exceeded contact tolerance");
                results.Add(CanonicalJson.Object("case","floor-stop-and-push","status","PASS","pushedBoardUnits",F(pushed)));
            }
            finally{UnityEngine.Object.DestroyImmediate(node);}
        }
    }
}
#endif
