#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HotpotSort.Core;
using HotpotSort.Determinism;
using HotpotSort.Presentation;
using HotpotSort.UnityPhysics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HotpotSort.Bootstrap
{
    [InitializeOnLoad]
    public static class Task001V9Diagnostic
    {
        const string Key="Task001V9Qa";
        const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        static Task001V9Diagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Verify();};}
        public static void Run()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-task001Report");
            SessionState.SetString(Key+"Report",args[index+1]);SessionState.SetBool(Key,true);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();
        }
        static void Assert(bool value,string message){if(!value)throw new Exception(message);}
        static string F(float value)=>value.ToString("R",CultureInfo.InvariantCulture);
        static object Invoke(object obj,string name)=>obj.GetType().GetMethod(name,Private).Invoke(obj,null);
        static ViewPlate Plate(int id,float y,float radius=39)=>new ViewPlate{plateId=id.ToString(),x=id%2==0?130:290,y=y,radius=radius,
            motion=new ViewPlateMotion{animateEntry=false},items=new[]{new ViewItem{itemId="i"+id,radius=12}}};
        static void Verify()
        {
            var results=new List<object>();int exit=0;var node=new GameObject("IsolatedV9PhysicsQA");
            try
            {
                var world=node.AddComponent<PlatePresentationWorld>();world.enabled=false;world.SetSimulating(true);
                foreach(int count in new[]{0,1,2,3})foreach(float y in new[]{139.999f,140f,140.001f})
                {
                    world.Reconcile(new ViewSnapshot{sessionId="gate"+count+"/"+y,sessionGeneration=7,plates=Enumerable.Range(1,count).Select(i=>Plate(i,y)).ToArray()});
                    var observation=world.ObserveHeightGate(11);int expected=y<140?count:0;
                    Assert(observation.entryCount==expected&&observation.canSupply==(expected<3),"strict entry count");
                    Assert(observation.version==1&&observation.entryLimit==3&&observation.gateY==140&&observation.sessionGeneration==7&&observation.snapshotRevision==11,"versioned metadata");
                    Assert(observation.spaceAvailable==observation.canSupply&&observation.heightGateClear==observation.canSupply,"alias disagreement");
                    results.Add(CanonicalJson.Object("case","gate","count",count,"y",F(y),"entryCount",expected,"allowed",observation.canSupply));
                }
                world.Reconcile(new ViewSnapshot{sessionId="gate3/139.999",plates=new ViewPlate[0]});
                Assert(world.OccupiedPlateCount==0&&world.ObserveHeightGate(12).canSupply,"ghosts block gate");
                var supplied=new List<ViewPlate>();var random=new System.Random(601377);var schedule=new ActiveSupplySchedule();int next=0;float minGap=420,minBoundary=828;var firstVisible=new Dictionary<string,object>();
                world.Reconcile(new ViewSnapshot{sessionId="queue",sessionGeneration=8});
                for(int step=0;step<2200;step++)
                {
                    if(schedule.TryTake(step*.02,true)&&next<12&&world.ObserveHeightGate(step).canSupply)
                    {
                        float radius=DailyViewMapper.PlateRadius(next%5+1);var plate=Plate(next+1,DailyViewMapper.SpawnY(radius),radius);
                        plate.x=DailyViewMapper.SpawnX(next)+(float)(random.NextDouble()*100-50);
                        plate.motion=new ViewPlateMotion{hasSpawnPosition=true,spawnX=plate.x,spawnY=plate.y};supplied.Add(plate);next++;
                        world.Reconcile(new ViewSnapshot{sessionId="queue",sessionGeneration=8,plates=supplied.ToArray()});
                        var spawned=world.Bodies.Last();Assert(world.Position(spawned).y+radius==-8,"spawn not wholly hidden");
                        Assert(spawned.rigidbody.linearDamping==.08f&&spawned.rim.sharedMaterial.friction==.08f&&spawned.rim.sharedMaterial.bounciness==.08f,"physics tuning");
                    }
                    Invoke(world,"FixedUpdate");var geometry=(Vector2)Invoke(world,"MeasureGeometry");minGap=Math.Min(minGap,geometry.x);minBoundary=Math.Min(minBoundary,geometry.y);
                    Assert(geometry.x>=-.001f&&geometry.y>=-.001f,"overlap/boundary at step "+step+" gap="+geometry);
                    foreach(var body in world.Bodies)if(!firstVisible.ContainsKey(body.data.plateId)&&world.Position(body).y+body.data.radius>=292)
                        firstVisible.Add(body.data.plateId,CanonicalJson.Object("step",step,"y",F(world.Position(body).y),"radius",F(body.data.radius)));
                }
                Assert(next==12&&firstVisible.Count==12,"queue stalled");
                var previousRandom=UnityEngine.Random.state;
                try{UnityEngine.Random.InitState(137);Assert(world.TryShuffle(true),"shuffle failed");}finally{UnityEngine.Random.state=previousRandom;}
                Assert(((Vector2)Invoke(world,"MeasureGeometry")).x>=-.001f,"shuffle overlap");
                Assert(world.Bodies.All(b=>world.Position(b).y-b.data.radius>=303.999f&&world.Position(b).y+b.data.radius<=828.001f),"shuffle bounds");
                Assert(PlateSupplyGeometry.Top==-134&&PlateSupplyGeometry.Gate==140&&PlateSupplyGeometry.ClipTop==292&&PlateSupplyGeometry.Floor==828,"protected geometry");
                results.Add(CanonicalJson.Object("case","physics","status","PASS","spawned",next,"steps",2200,"minimumGap",F(minGap),"minimumBoundary",F(minBoundary),"firstVisible",firstVisible));
                Debug.Log("V9_PHYSICS_PASS");
            }
            catch(Exception ex){exit=1;results.Add(CanonicalJson.Object("status","FAIL","error",ex.ToString()));Debug.LogException(ex);}
            finally
            {
                try{File.WriteAllText(SessionState.GetString(Key+"Report",""),CanonicalJson.Write(CanonicalJson.Object("task","TASK-001","version",9,"checkpoint","HC-02-v9-Code","exitCode",exit,"scope","Isolated physics + stable observations, no player data or visual acceptance","results",results)));}
                catch(Exception ex){exit=2;Debug.LogException(ex);}
                finally{SessionState.SetBool(Key,false);UnityEngine.Object.DestroyImmediate(node);EditorApplication.Exit(exit);}
            }
        }
    }
}
#endif
