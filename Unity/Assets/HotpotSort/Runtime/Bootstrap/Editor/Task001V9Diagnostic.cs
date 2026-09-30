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
        static object Invoke(object obj,string name)=>obj.GetType().GetMethod(name,Private,null,Type.EmptyTypes,null).Invoke(obj,null);
        static ViewPlate Plate(int id,float y,float radius=39)=>new ViewPlate{plateId=id.ToString(),x=id%2==0?130:290,y=y,radius=radius,
            motion=new ViewPlateMotion{animateEntry=false},items=new[]{new ViewItem{itemId="i"+id,radius=12}}};
        static void Verify()
        {
            var results=new List<object>();int exit=0;var node=new GameObject("IsolatedV9PhysicsQA");
            try
            {
                var world=node.AddComponent<PlatePresentationWorld>();world.enabled=false;world.SetSimulating(true);
                VerifyDiagnostics(world,results);
                VerifyDenseMotion(world,results);
                VerifyContinuousMotion(world,results);
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
                        Assert(spawned.rigidbody.linearDamping==.08f&&spawned.rim.sharedMaterial.friction==0f&&spawned.rim.sharedMaterial.bounciness==.08f,"physics tuning");
                    }
                    Invoke(world,"FixedUpdate");var geometry=(Vector2)Invoke(world,"MeasureGeometry");minGap=Math.Min(minGap,geometry.x);minBoundary=Math.Min(minBoundary,geometry.y);
                    // Native contact slop is reported separately, not erased by projection.
                    Assert(geometry.x>=-10f&&geometry.y>=-5f,"native deep penetration at step "+step+" gap="+geometry);
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
        static void VerifyContinuousMotion(PlatePresentationWorld world,List<object> results)
        {
            var snapshot=new ViewSnapshot{sessionId="continuous-motion",plates=new[]{Plate(1,200)}};
            world.Reconcile(snapshot);var body=world.Bodies.Single();
            Assert(body.rigidbody.linearVelocity==Vector2.zero,"spawn must use force rather than assigned velocity");
            Invoke(world,"FixedUpdate");float firstSpeed=body.rigidbody.linearVelocity.y;
            Assert(firstSpeed>0&&firstSpeed<.5f,"spawn force");
            for(int step=0;step<20;step++)Invoke(world,"FixedUpdate");
            var moving=body.rigidbody.linearVelocity;
            Assert(moving.y>firstSpeed+3f&&Mathf.Abs(moving.x)<.001f,"isolated body must accelerate downward");
            world.Reconcile(snapshot);Assert(body.rigidbody.linearVelocity==moving,"snapshot changed velocity");
            var at=world.Position(body);world.SetSimulating(false);
            for(int step=0;step<10;step++)Invoke(world,"FixedUpdate");
            world.Reconcile(snapshot);Assert(world.Position(body)==at,"paused body moved");
            world.SetSimulating(true);Assert(body.rigidbody.linearVelocity==moving,"resume changed velocity");
            Invoke(world,"FixedUpdate");Assert(body.rigidbody.linearVelocity.y>moving.y,"resume did not accelerate");
            snapshot.plates[0]=Plate(1,400);world.Reconcile(snapshot);
            Assert(body.rigidbody.linearVelocity==Vector2.zero,"correction velocity reset changed");
            for(int step=0;step<20;step++)Invoke(world,"FixedUpdate");
            Assert(body.rigidbody.linearVelocity.y>3f&&world.Position(body).y>400,"corrected body did not resume falling");
            results.Add(CanonicalJson.Object("case","continuous-motion","status","PASS","firstSpeedY",F(firstSpeed),"acceleratedSpeedY",F(moving.y),"snapshotAndPausePreserveVelocity",true,"correctionResumesFalling",true));
        }
        static void VerifyDiagnostics(PlatePresentationWorld world,List<object> results)
        {
            Assert(!world.PhysicsDiagnosticsEnabled,"diagnostics production default must be off");
            var controlNode=new GameObject("DiagnosticControl");var control=controlNode.AddComponent<PlatePresentationWorld>();control.enabled=false;control.SetSimulating(true);
            try
            {
                var snapshot=new ViewSnapshot{sessionId="diag",plates=new[]{Plate(1,600)}};
                world.PhysicsDiagnosticsEnabled=true;world.Reconcile(snapshot);control.Reconcile(snapshot);
                for(int step=0;step<6500;step++)
                {
                    Invoke(world,"FixedUpdate");Invoke(control,"FixedUpdate");
                    Assert(world.Position(world.Bodies.Single())==control.Position(control.Bodies.Single())&&world.Bodies.Single().rigidbody.linearVelocity==control.Bodies.Single().rigidbody.linearVelocity,"diagnostics mutated physics");
                }
                var logs=world.GetPhysicsDiagnosticSnapshot();
                Assert(logs.Length==32&&logs.Any(s=>s.Contains("\"kind\":\"stationary\"")&&s.Contains("\"floorContact\":true")),"stationary contact log or bounded ring missing");
                float clock=(float)typeof(PlatePresentationWorld).GetField("diagnosticTime",Private).GetValue(world);
                int before=logs.Length;world.SetSimulating(false);
                for(int step=0;step<200;step++)Invoke(world,"FixedUpdate");
                Assert((float)typeof(PlatePresentationWorld).GetField("diagnosticTime",Private).GetValue(world)==clock&&world.GetPhysicsDiagnosticSnapshot().Length==before,"pause accumulated diagnostics");
                world.SetSimulating(true);
                world.Reconcile(new ViewSnapshot{sessionId="diag-synthetic",plates=new[]{Plate(1,400),Plate(2,500)}});
                Assert(world.GetPhysicsDiagnosticSnapshot().Length==0,"session did not reset ring");
                var beforePositions=world.Bodies.Select(b=>world.Position(b)).ToArray();
                // Exercise pre-restoration evidence with deliberately failing input only;
                // this is not an observed gameplay rollback and never applies these positions.
                typeof(PlatePresentationWorld).GetMethod("RecordGeometryFailure",Private).Invoke(world,new object[]{new[]{new Vector2(10,400),new Vector2(20,400)},2,true});
                string failure=world.GetPhysicsDiagnosticSnapshot().Single();
                Assert(failure.Contains("\"pairA\":\"1\"")&&failure.Contains("\"pairB\":\"2\"")&&failure.Contains("\"boundary\":\"Left\"")&&failure.Contains("\"pairGap\":-68"),"pre-restoration failure evidence missing");
                Assert(world.Bodies.Select(b=>world.Position(b)).SequenceEqual(beforePositions),"failure recording mutated positions");
                results.Add(CanonicalJson.Object("case","diagnostics","status","PASS","nonMutationSteps",6500,"ringCapacity",logs.Length,"stationaryFloorContact",true,"pauseClockFrozen",true,"syntheticFailureCapture",true,"actualGameplayRollbackReproduced",false));
            }
            finally{world.PhysicsDiagnosticsEnabled=false;world.Clear();UnityEngine.Object.DestroyImmediate(controlNode);}
        }
        static void VerifyDenseMotion(PlatePresentationWorld world,List<object> results)
        {
            var controlNode=new GameObject("DenseDiagnosticsControl");var control=controlNode.AddComponent<PlatePresentationWorld>();control.enabled=false;control.SetSimulating(true);
            try
            {
            foreach(int target in new[]{22,25})
            {
                var supplied=new List<ViewPlate>();var random=new System.Random(601377);var schedule=new ActiveSupplySchedule();
                var empty=new ViewSnapshot{sessionId="dense"+target};world.Reconcile(empty);control.Reconcile(empty);world.SetSimulating(true);world.PhysicsDiagnosticsEnabled=true;
                float maxLateSpeed=0,maxLateMove=0,minGap=420,minBoundary=828;var watch=new System.Diagnostics.Stopwatch();
                var lateStart=new Vector2[target];var lateMin=new Vector2[target];var lateMax=new Vector2[target];var latePreviousDelta=new Vector2[target];var latePath=new float[target];int reversals=0;
                int lateRollbackStart=0;
                for(int step=0;step<4000;step++)
                {
                    if(schedule.TryTake(step*.02,true)&&supplied.Count<target&&world.ObserveHeightGate(step).canSupply)
                    {
                        int next=supplied.Count;float radius=DailyViewMapper.PlateRadius(next%5+1);var plate=Plate(next+1,DailyViewMapper.SpawnY(radius),radius);
                        plate.x=DailyViewMapper.SpawnX(next)+(float)(random.NextDouble()*100-50);
                        plate.motion=new ViewPlateMotion{hasSpawnPosition=true,spawnX=plate.x,spawnY=plate.y};supplied.Add(plate);
                        var snapshot=new ViewSnapshot{sessionId="dense"+target,plates=supplied.ToArray()};world.Reconcile(snapshot);control.Reconcile(snapshot);
                    }
                    var before=world.Bodies.Select(b=>world.Position(b)).ToArray();watch.Start();Invoke(world,"FixedUpdate");watch.Stop();Invoke(control,"FixedUpdate");
                    var actual=world.Bodies.ToArray();var expected=control.Bodies.ToArray();
                    for(int i=0;i<actual.Length;i++)Assert(world.Position(actual[i])==control.Position(expected[i])&&actual[i].rigidbody.linearVelocity==expected[i].rigidbody.linearVelocity,"dense diagnostics changed motion");
                    if(step==3000){lateRollbackStart=world.ConstraintRollbacks;for(int i=0;i<actual.Length;i++)lateStart[i]=lateMin[i]=lateMax[i]=world.Position(actual[i]);}
                    if(step>=3000)
                    {
                        int i=0;foreach(var body in world.Bodies){maxLateSpeed=Mathf.Max(maxLateSpeed,body.rigidbody.linearVelocity.magnitude);maxLateMove=Mathf.Max(maxLateMove,Vector2.Distance(before[i++],world.Position(body)));}
                        var geometry=(Vector2)Invoke(world,"MeasureGeometry");minGap=Mathf.Min(minGap,geometry.x);minBoundary=Mathf.Min(minBoundary,geometry.y);
                        for(int j=0;j<actual.Length;j++)
                        {
                            var at=world.Position(actual[j]);var delta=at-before[j];lateMin[j]=Vector2.Min(lateMin[j],at);lateMax[j]=Vector2.Max(lateMax[j],at);latePath[j]+=delta.magnitude;
                            if(delta.sqrMagnitude>.000001f&&latePreviousDelta[j].sqrMagnitude>.000001f&&Vector2.Dot(delta,latePreviousDelta[j])<0)reversals++;
                            latePreviousDelta[j]=delta;
                        }
                    }
                }
                watch.Stop();
                Assert(supplied.Count==target&&world.ConstraintRollbacks==0,"dense supply or global rewind");
                // One board unit is an observation bound, not a zero-overlap claim;
                // report actual native slop and let the real preview establish visibility.
                Assert(minGap>=-1f&&minBoundary>=-1f,"dense native penetration exceeds observation bound");
                Assert(maxLateMove<.001f&&maxLateSpeed<.001f,"dense native contact failed to settle");
                float range=0,net=0,path=0;int bi=0;foreach(var b in world.Bodies){range=Mathf.Max(range,(lateMax[bi]-lateMin[bi]).magnitude);net=Mathf.Max(net,Vector2.Distance(lateStart[bi],world.Position(b)));path=Mathf.Max(path,latePath[bi]);bi++;}
                results.Add(CanonicalJson.Object("case","dense","target",target,"spawned",supplied.Count,"rollbacks",world.ConstraintRollbacks,"lateRollbacks",world.ConstraintRollbacks-lateRollbackStart,"transientSteps",world.TransientGeometrySteps,"lateMaxSpeedWorld",F(maxLateSpeed),"lateMaxMoveBoard",F(maxLateMove),"late20sMaxPositionRangeBoard",F(range),"late20sMaxNetDriftBoard",F(net),"late20sMaxPathBoard",F(path),"lateDirectionReversals",reversals,"minimumGap",F(minGap),"minimumBoundary",F(minBoundary),"milliseconds",watch.ElapsedMilliseconds));
                var airborne=Plate(999,-80);airborne.x=210;supplied.Add(airborne);
                world.Reconcile(new ViewSnapshot{sessionId="dense"+target,plates=supplied.ToArray()});
                var falling=world.Bodies.Last();float initialY=world.Position(falling).y;
                for(int step=0;step<10;step++)Invoke(world,"FixedUpdate");
                Assert(world.Position(falling).y>initialY+10&&falling.rigidbody.linearVelocity.y>1,"dense contacts froze unrelated airborne plate");
            }
            world.PhysicsDiagnosticsEnabled=false;
            var stack=new[]{Plate(1,789),Plate(2,710.98f),Plate(3,632.96f)};foreach(var p in stack)p.x=210;
            world.Reconcile(new ViewSnapshot{sessionId="support-removal",plates=stack});
            for(int step=0;step<500;step++)Invoke(world,"FixedUpdate");
            var upper=world.Bodies.Last();float upperY=world.Position(upper).y;
            world.Reconcile(new ViewSnapshot{sessionId="support-removal",plates=stack.Skip(1).ToArray()});
            for(int step=0;step<25;step++)Invoke(world,"FixedUpdate");
            float drop=world.Position(upper).y-upperY;Assert(drop>20,"support removal did not produce natural fall");
            results.Add(CanonicalJson.Object("case","dense-support","status","PASS","supportRemovalDropBoard",F(drop),"unrelatedAirborneFalls",true,"denseDiagnosticsNonMutation",true));
            var sliding=Plate(1,789);sliding.x=100;world.Reconcile(new ViewSnapshot{sessionId="frictionless-slide",plates=new[]{sliding}});
            var slideBody=world.Bodies.Single();slideBody.rigidbody.linearVelocity=new Vector2(2,0);
            for(int step=0;step<50;step++)Invoke(world,"FixedUpdate");
            Assert(slideBody.rigidbody.linearVelocity.x>1.8f&&world.Position(slideBody).x>280,"contact solver added tangential friction");
            results.Add(CanonicalJson.Object("case","frictionless-slide","status","PASS","speedX",F(slideBody.rigidbody.linearVelocity.x),"positionX",F(world.Position(slideBody).x)));
            }
            finally{world.PhysicsDiagnosticsEnabled=false;UnityEngine.Object.DestroyImmediate(controlNode);}
        }
    }
}
#endif
