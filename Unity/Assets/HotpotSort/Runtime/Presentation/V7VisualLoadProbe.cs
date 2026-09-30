#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using HotpotSort.Contracts;
using UnityEngine;

namespace HotpotSort.Presentation
{
    // Opt-in QA only. Production resource/layout code drives this synthetic stress scene.
    public sealed class V7VisualLoadProbe : MonoBehaviour,IPresentationPort
    {
        string output;GameplayView view;ViewSnapshot state;long sequence;string profileKey;
        public event Action<ViewUpdate> Updated;
        public ViewSnapshot Read()=>state;
        public void Tap(ViewTap tap){}
        public void ObserveSupply(ViewSupplyObservation observation){}
        public void SessionAction(ViewAction action){}
        static string F(double v)=>v.ToString("R",CultureInfo.InvariantCulture);
        [Serializable] sealed class Report
        {
            public string scope,unity,os,cpu,gpu,api,layoutVersion,resourceRoot;
            public int renderWidth,renderHeight,windowWidth,windowHeight,frames,events,maxPool;
            public double sampledSeconds,averageFps,meanMs,medianMs,p95Ms,p99Ms;
            public bool pass;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-v8FinalPerformance");
            if(i<0||i+1>=args.Length)return;
            new GameObject("V8_OptIn_FinalPerformance").AddComponent<V7VisualLoadProbe>().output=args[i+1];
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(output);QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;Application.runInBackground=true;
            for(int i=0;i<600;i++){view=FindFirstObjectByType<GameplayView>();if(view&&view.VisualArt!=null)break;yield return null;}
            if(!view){File.WriteAllText(output+"/error.txt","No approved view");Application.Quit(1);yield break;}
            var composition=FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).First(b=>b.GetType().Name=="DailyProductionComposition");
            var bootstrapAssembly=composition.GetType().Assembly;var serviceType=bootstrapAssembly.GetType("HotpotSort.Bootstrap.LocalDevelopmentServices");
            profileKey="HotpotSort.Task001.v8.PlayerQA."+Guid.NewGuid().ToString("N");
            var local=Activator.CreateInstance(serviceType,new object[]{profileKey,"development","local",null,true});
            composition.GetType().GetMethod("ConfigureServices").Invoke(composition,new object[]{local,local,local,local,null});
            composition.enabled=false;
            var load=composition.GetType().GetMethod("LoadPlateShape",BindingFlags.NonPublic|BindingFlags.Instance);
            Func<int,PlateFoodShape> shapes=id=>(PlateFoodShape)load.Invoke(composition,new object[]{id});
            state=new ViewSnapshot{sessionId="v8-final-performance",sessionGeneration=100001,phase=ViewPhase.Running,revision=1,orders=Enumerable.Range(0,4).Select(i=>new ViewOrder{slot=i,enabled=true,foodId=i,count=1,required=3}).ToArray()};
            var plates=new List<ViewPlate>();int itemId=1000;
            for(int row=0;row<5;row++)for(int col=0;col<4;col++)
            {
                string id="stress-"+row+"-"+col;
                var initial=new[]{new ViewItem{itemId=(itemId++).ToString(),foodId=(row*4+col)%16,sourceIndex=0},new ViewItem{itemId=(itemId++).ToString(),foodId=(row*4+col+5)%16,sourceIndex=1}};
                var layout=PlateItemLayout.Create(12345,id,45,initial,shapes);
                plates.Add(new ViewPlate{plateId=id,x=55+103*col,y=342+99*row,radius=45,motion=new ViewPlateMotion{animateEntry=false},items=layout.Items,initialItemCount=2,layoutVersion=PlateItemLayout.Version,envelopeRatio=layout.EnvelopeRatio});
            }
            state.plates=plates.ToArray();view.Bind(this);view.SetForeground(true);view.SetRemainingTime(462);
            var canvas=view.GetComponentInChildren<Canvas>();var cameraNode=new GameObject("FinalPerformanceRenderCamera");var camera=cameraNode.AddComponent<Camera>();
            var counter=cameraNode.AddComponent<V8RenderCadenceCounter>();camera.enabled=false;var readback=new Texture2D(1,1,TextureFormat.RGB24,false);camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.transform.position=new Vector3(0,0,-1000);
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=100;
            bool all=true;
            foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1440,3200)})
            {
                // Full requested raster is rendered every frame even on a smaller desktop display.
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(2);
                var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;camera.orthographicSize=size.y*.5f;
                view.SetViewport(new Rect(0,0,size.x,size.y),size.y);Canvas.ForceUpdateCanvases();
                string dir=output+"/"+size.x+"x"+size.y;Directory.CreateDirectory(dir);
                double start=Time.realtimeSinceStartupAsDouble,next=0,serve=0;int emitted=0,maxPool=0;
                double sampleStart=0;
                while(sampleStart==0||Time.realtimeSinceStartupAsDouble-sampleStart<120)
                {
                    double elapsed=Time.realtimeSinceStartupAsDouble-start;
                    if(elapsed>=next)
                    {
                        next=elapsed+.055;var item=state.plates[emitted%state.plates.Length].items[0];int slot=emitted++%4;
                        var events=new List<ViewEvent>{new ViewEvent{sessionId=state.sessionId,sessionGeneration=state.sessionGeneration,sequence=++sequence,transactionId="stress-"+sequence,kind="ItemRoutedToOrder",itemId=item.itemId,ingredientId="food_"+item.foodId.ToString("00"),sourceContainer="Plate",targetContainer="Order",targetSlot=slot}};
                        if(elapsed>=serve){serve=elapsed+.65;events.Add(new ViewEvent{sessionId=state.sessionId,sessionGeneration=state.sessionGeneration,sequence=++sequence,transactionId="serve-"+sequence,kind="OrderCompleted",ingredientId="food_00",slot=slot});}
                        state.eventSeq=sequence;state.revision++;Updated?.Invoke(new ViewUpdate{snapshot=state,events=events.ToArray()});
                    }
                    maxPool=Math.Max(maxPool,view.GetComponent<GameplayFeedback>().PoolSize);
                    camera.Render();var priorTarget=RenderTexture.active;RenderTexture.active=target;readback.ReadPixels(new Rect(0,0,1,1),0,0,false);RenderTexture.active=priorTarget;counter.RecordRenderedFrame();
                    yield return null;
                    if(elapsed>=15)
                    {
                        if(sampleStart==0){sampleStart=Time.realtimeSinceStartupAsDouble;counter.Begin(sampleStart);}
                    }
                }
                double duration=Time.realtimeSinceStartupAsDouble-sampleStart;counter.recording=false;var samples=counter.samples;var rows=counter.rows;
                yield return new WaitForEndOfFrame();SaveFrame(target,dir+"/complex-scene.png");
                if(samples.Count<2){File.WriteAllText(dir+"/error.txt","No actual camera render samples");Application.Quit(1);yield break;}var ordered=samples.OrderBy(v=>v).ToArray();double mean=samples.Average(),p95=ordered[(int)(ordered.Length*.95)];
                var report=new Report{scope="Windows x64 Development Player; explicit full camera render plus 1-pixel GPU readback completion cadence, exact-resolution render target; 20 physical plates/40 actual v8-layout foods/four pots/continuous flights and serving; 15s warmup then 120s sample; synthetic transport, not core-rule benchmark",unity=Application.unityVersion,os=SystemInfo.operatingSystem,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString(),layoutVersion=PlateItemLayout.Version,resourceRoot=view.AssetRoot,renderWidth=size.x,renderHeight=size.y,windowWidth=Screen.width,windowHeight=Screen.height,frames=samples.Count,events=emitted,maxPool=maxPool,sampledSeconds=duration,averageFps=1000/mean,meanMs=mean,medianMs=ordered[ordered.Length/2],p95Ms=p95,p99Ms=ordered[(int)(ordered.Length*.99)],pass=1000/mean>=30&&p95<=33.4};
                all&=report.pass;File.WriteAllLines(dir+"/frames.csv",rows);File.WriteAllText(dir+"/report.json",JsonUtility.ToJson(report,true));Debug.Log("V8_PERFORMANCE "+size+" pass="+report.pass);
                camera.targetTexture=null;target.Release();Destroy(target);
            }
            Destroy(readback);PlayerPrefs.DeleteKey(profileKey);PlayerPrefs.Save();File.WriteAllText(output+"/result.json","{\"pass\":"+all.ToString().ToLowerInvariant()+"}");Application.Quit(all?0:1);
        }
        static void SaveFrame(RenderTexture target,string path)
        {
            var prior=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=prior;Destroy(image);
        }
    }
    public sealed class V8RenderCadenceCounter : MonoBehaviour
    {
        public bool recording;
        public readonly List<double> samples=new List<double>();
        public readonly List<string> rows=new List<string>();
        double start,last;int count;
        public void Begin(double now){start=last=now;count=0;samples.Clear();rows.Clear();rows.Add("renderFrame,elapsedSeconds,renderFrameMs,unityFrame");recording=true;}
        public void RecordRenderedFrame()
        {
            if(!recording)return;double now=Time.realtimeSinceStartupAsDouble,ms=(now-last)*1000;last=now;samples.Add(ms);
            rows.Add((++count)+","+(now-start).ToString("R",CultureInfo.InvariantCulture)+","+ms.ToString("R",CultureInfo.InvariantCulture)+","+Time.frameCount);
        }
    }
}
#endif
