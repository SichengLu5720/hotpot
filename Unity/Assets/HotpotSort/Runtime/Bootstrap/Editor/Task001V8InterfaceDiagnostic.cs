#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HotpotSort.Contracts;
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
    public static class Task001V8InterfaceDiagnostic
    {
        const string ActiveKey="Task001V8InterfaceActive",ReportKey="Task001V8InterfaceReport";
        static Task001V8InterfaceDiagnostic(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(ActiveKey,false))Verify();};}
        public static void Run()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-task001Report");if(index<0)throw new ArgumentException("Report path required");
            IntegrationDiagnostic.VerifyBoot();SessionState.SetString(ReportKey,args[index+1]);SessionState.SetBool(ActiveKey,true);EditorApplication.EnterPlaymode();
        }
        const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        static string F(float value)=>value.ToString("R",CultureInfo.InvariantCulture);
        static void Assert(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        static object Invoke(object target,string method,params object[] arguments)=>target.GetType().GetMethod(method,Private).Invoke(target,arguments);
        static Dictionary<string,object> State(DailySession session)=>CanonicalJson.Map(CanonicalJson.Parse(session.Snapshot.CanonicalStateJson));
        static string Signature(ViewItem item)=>CanonicalJson.Write(CanonicalJson.Object("item",item.itemId,"x",F(item.x),"y",F(item.y),"radius",F(item.radius),"angle",F(item.rotationDegrees),"order",item.drawOrder));
        public static void Verify()
        {
            string report=SessionState.GetString(ReportKey,"");
            var results=new List<object>();int exit=0;GameObject node=null;
            try
            {
                Assert(EditorApplication.isPlaying,"Physics diagnostic requires Play Mode");var composition=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();
                var factory=DailySessionFactory.FromProductionJson(File.ReadAllText("Assets/HotpotSort/Content/Daily/"+DailyContent.RuntimeFileName));
                Func<int,PlateFoodShape> shape=id=>(PlateFoodShape)Invoke(composition,"LoadPlateShape",id);
                var shapes=Enumerable.Range(0,16).Select(shape).ToArray();Assert(shapes.All(s=>!s.IsProxy),"production shapes are proxy");
                var samples=new List<object>();
                foreach(string day in new[]{"20260921","20260922","20260923"})
                {
                    var context=new ChallengeContext(day,factory.Content.ContentVersion,factory.ConfigurationDigest,"v8-interface-qa",0);
                    using(var session=factory.CreateDailySession(context))
                    {
                        for(ulong i=1;i<=50;i++)session.Supply(new SupplyObservation(i,i,true,true));
                        string coreHash=session.StateHash,coreRng=session.PresentationRngJson;
                        var cache=new PlateLayoutCache(shape);var view=DailyViewMapper.Map(session.Snapshot,null,factory.Content,0,1,ViewPauseReasons.None,cache).snapshot;
                        foreach(var plate in view.plates)
                        {
                            float ratio=PlateItemLayout.MeasureEnvelope(plate.items,shape,plate.radius);
                            float visible=PlateItemLayout.Visibility(plate.items,plate.items.Select(i=>shape(i.foodId)).ToArray());
                            Assert(Math.Abs(ratio-.8f)<.0001&&visible>=.75f&&!plate.layoutUsesProxyContour,"actual alpha occupancy/visibility "+day+"/"+plate.plateId);
                            foreach(var item in plate.items)
                            {
                                Assert(item.rotationDegrees>=-15&&item.rotationDegrees<=15,"angle range");
                                foreach(var p in shape(item.foodId).Contour){var at=PlateItemTransform.ToBoard(item,0,0,p.x*item.radius*2,p.y*item.radius*2);Assert(at.x*at.x+at.y*at.y<=plate.radius*plate.radius,"food beyond rim");}
                            }
                            samples.Add(CanonicalJson.Object("day",day,"plate",plate.plateId,"count",plate.items.Length,"occupancy",F(ratio),"minVisible",F(visible)));
                        }
                        var before=view.plates.SelectMany(p=>p.items).ToDictionary(i=>i.itemId,Signature);
                        int tapped=session.FindHintItem();Assert(session.Tap(new TapCommand(tapped,1,51,true)).Accepted,"test tap");
                        var remaining=DailyViewMapper.Map(session.Snapshot,null,factory.Content,0,1,ViewPauseReasons.None,cache).snapshot;
                        Assert(remaining.plates.SelectMany(p=>p.items).All(i=>before[i.itemId]==Signature(i)),"remaining items moved after removal");
                        var retry=factory.CreateDailySession(context);for(ulong i=1;i<=50;i++)retry.Supply(new SupplyObservation(i,i,true,true));
                        var retryView=DailyViewMapper.Map(retry.Snapshot,null,factory.Content,0,99,ViewPauseReasons.None,new PlateLayoutCache(shape)).snapshot;
                        Assert(retryView.plates.SelectMany(p=>p.items).All(i=>before[i.itemId]==Signature(i)),"new session layout changed");
                        Assert(retry.StateHash==coreHash&&retry.PresentationRngJson==coreRng,"layout consumed core or presentation RNG");retry.Dispose();
                    }
                }
                results.Add(CanonicalJson.Object("case","V8-L01","status","PASS","samples",samples));
                // Return/mixed plates also use original source slots and the same contour contract.
                for(int count=1;count<=5;count++)
                {
                    var initial=Enumerable.Range(0,count).Select(i=>new ViewItem{itemId=(180-i).ToString(),sourceIndex=i,foodId=(count*3+i)%16}).ToArray();
                    var layout=PlateItemLayout.Create(12345,"51",DailyViewMapper.PlateRadius(count),initial,shape);
                    Assert(layout.MinimumVisibleFraction>=.75f&&Math.Abs(PlateItemLayout.MeasureEnvelope(layout.Items,shape,DailyViewMapper.PlateRadius(count))-.8f)<.0001,"returned mixed layout");
                }
                results.Add(CanonicalJson.Object("case","V8-L02","status","PASS","scope","Returned plate 1-5, independent keyed angle and alpha envelope."));
                node=new GameObject("V8InterfacePhysicsQA");var world=node.AddComponent<PlatePresentationWorld>();world.enabled=false;
                if(typeof(PlatePresentationWorld).GetField("physicsRoot",Private).GetValue(world)==null)Invoke(world,"Awake");
                world.SetSimulating(true);var physicsSamples=new List<object>();
                foreach(float radius in new[]{39f,45f,51f,57f,63f})
                {
                    var plate=new ViewPlate{plateId="p",x=210,y=DailyViewMapper.SpawnY(radius),radius=radius,items=new[]{new ViewItem{itemId="i",radius=10}}};
                    world.Reconcile(new ViewSnapshot{sessionId="r"+radius,phase=ViewPhase.Running,plates=new[]{plate}});var body=world.Bodies.Single();
                    Assert(Math.Abs(world.Position(body).y+radius+8)<.0001&&world.Position(body).y+radius<0,"not wholly offscreen");
                    float start=world.Position(body).y;for(int step=0;step<10;step++)Invoke(world,"FixedUpdate");
                    Assert(world.Position(body).y>start&&world.Position(body).y<140,"ceiling teleported/stalled new plate");
                    var geometry=(Vector2)Invoke(world,"MeasureGeometry");Assert(geometry.x>=-.001&&geometry.y>=-.001,"spawn physics bounds");
                    foreach(float y in new[]{139.999f,140f,140.001f})
                    {
                        body.rigidbody.position=new Vector2(210,y)*.01f;body.node.transform.position=body.rigidbody.position;Physics2D.SyncTransforms();
                        var observation=world.ObserveHeightGate(7);Assert(observation.heightGateClear==(y>=140)&&observation.fixedGate==140,"strict height gate "+y);
                        physicsSamples.Add(CanonicalJson.Object("radius",F(radius),"requestedY",F(y),"actualY",F(world.Position(body).y),"allowed",observation.heightGateClear));
                    }
                }
                Assert(PlatePresentationWorld.HiddenTop==-134&&PlateSupplyGeometry.ClipTop==292,"top/crop contract");
                results.Add(CanonicalJson.Object("case","V8-P01","status","PASS","samples",physicsSamples));
                var supplied=new List<ViewPlate>();var horizontal=new System.Random(601377);int nextPlate=0;float minGap=420,minBoundary=828;
                world.Reconcile(new ViewSnapshot{sessionId="queue",phase=ViewPhase.Running});
                for(int step=0;step<2200;step++)
                {
                    if(step%10==0&&nextPlate<12&&world.ObserveHeightGate(step).heightGateClear)
                    {
                        float radius=DailyViewMapper.PlateRadius(nextPlate%5+1);float x=DailyViewMapper.SpawnX(nextPlate)+(float)(horizontal.NextDouble()*100-50);
                        supplied.Add(new ViewPlate{plateId="q"+nextPlate,x=x,y=DailyViewMapper.SpawnY(radius),radius=radius,items=new[]{new ViewItem{itemId="qitem"+nextPlate,radius=10}},
                            motion=new ViewPlateMotion{hasSpawnPosition=true,spawnX=x,spawnY=DailyViewMapper.SpawnY(radius)}});nextPlate++;
                        world.Reconcile(new ViewSnapshot{sessionId="queue",phase=ViewPhase.Running,plates=supplied.ToArray()});
                    }
                    Invoke(world,"FixedUpdate");var geometry=(Vector2)Invoke(world,"MeasureGeometry");minGap=Math.Min(minGap,geometry.x);minBoundary=Math.Min(minBoundary,geometry.y);
                    Assert(geometry.x>=-.001f&&geometry.y>=-.001f,"multi-plate overlap or wall penetration");
                }
                Assert(nextPlate==12,"targeted queue did not enter all twelve plates");
                var randomState=UnityEngine.Random.state;
                try
                {
                    UnityEngine.Random.InitState(137);Assert(world.TryShuffle(true),"targeted shuffle failed");
                    foreach(var body in world.Bodies){var at=world.Position(body);Assert(at.y-body.data.radius>=303.999f&&at.y+body.data.radius<=828.001f,"shuffle range changed");}
                    Assert(((Vector2)Invoke(world,"MeasureGeometry")).x>=-.001f,"shuffle overlap");
                }
                finally{UnityEngine.Random.state=randomState;}
                results.Add(CanonicalJson.Object("case","V8-P02","status","PASS","spawned",nextPlate,"steps",2200,"minimumGap",F(minGap),"minimumBoundary",F(minBoundary),"scope","12 mixed-radius physical supplies at 0.2-second gate checks and visible-region shuffle; not rendered gameplay QA."));
                var lower=new ViewItem{itemId="lower",radius=20,rotationDegrees=-15};var upper=new ViewItem{itemId="upper",radius=20,rotationDegrees=15,drawOrder=1};
                world.Reconcile(new ViewSnapshot{sessionId="hit",phase=ViewPhase.Running,plates=new[]{new ViewPlate{plateId="hit",x=210,y=500,radius=63,motion=new ViewPlateMotion{animateEntry=false},items=new[]{lower,upper}}}});
                var point=PlateItemTransform.ToBoard(upper,210,500,10,3);
                Assert(world.Hit(new Vector2(point.x,point.y),(item,local)=>item.itemId=="upper"&&Math.Abs(local.x-10)<.001&&Math.Abs(local.y-3)<.001)=="upper","inverse rotated hit");
                Assert(world.Hit(new Vector2(point.x,point.y),(item,local)=>item.itemId=="lower")=="lower","transparent upper blocks lower");
                var uv=PlateItemTransform.ToTextureUv(upper,210,500,point.x,point.y);Assert(Math.Abs(uv.x-.75)<.0001&&Math.Abs(uv.y-.425)<.0001,"shared alpha UV");
                results.Add(CanonicalJson.Object("case","V8-H01","status","PASS","scope","Shared rotate/inverse/UV, rotated collider, transparent top fall-through. GameplayView integration is pending Visual."));
                Debug.Log("TASK001_V8_INTERFACE_OK: 150 real-alpha initial plates, mixed returns, stable removal/retry/RNG and targeted physics/hit checks.");
            }
            catch(Exception ex){exit=1;results.Add(CanonicalJson.Object("status","FAIL","error",ex.ToString()));Debug.LogException(ex);}
            finally
            {
                File.WriteAllText(report,CanonicalJson.Write(CanonicalJson.Object("task","TASK-001","taskVersion",8,"checkpoint","HC-02-v8","scope","Core interface checks, not final visual/full QA","exitCode",exit,"results",results)));
                SessionState.SetBool(ActiveKey,false);if(node)UnityEngine.Object.DestroyImmediate(node);EditorApplication.Exit(exit);
            }
        }
    }
}
#endif
