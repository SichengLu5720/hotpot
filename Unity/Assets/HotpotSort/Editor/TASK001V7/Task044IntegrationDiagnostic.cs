#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HotpotSort.Bootstrap;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Determinism;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HotpotSort.Task001V7
{
    [InitializeOnLoad]
    public static class Task044IntegrationDiagnostic
    {
        const string Active="Task044.Integration.Active",PathKey="Task044.Integration.Path";
        static Task044IntegrationDiagnostic(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-task044Evidence");
            if(i<0||i+1>=args.Length)throw new ArgumentException("-task044Evidence required");
            Directory.CreateDirectory(args[i+1]);SessionState.SetString(PathKey,args[i+1]);SessionState.SetBool(Active,true);
            EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Active,false))Execute();}
        static async void Execute()
        {
            string root=SessionState.GetString(PathKey,"");int exit=0;var checks=new List<string>();
            try
            {
                foreach(var stale in Directory.GetFiles(root,"day-*.png",SearchOption.TopDirectoryOnly))File.Delete(stale);
                DailyProductionComposition composition=null;HotpotSort.Bootstrap.Bootstrap boot=null;
                double deadline=EditorApplication.timeSinceStartup+60;
                while(composition==null||!composition.PlayerView||boot==null||!boot.IsConfigured)
                {composition=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();boot=UnityEngine.Object.FindFirstObjectByType<HotpotSort.Bootstrap.Bootstrap>();if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Boot not ready");await Task.Yield();}
                var captureView=new GameObject("Task044LayoutCapture").AddComponent<HotpotSort.Presentation.GameplayView>();captureView.ConfigureAssets(HotpotSort.Presentation.PresentationAssets.CandidateRoot);captureView.SetViewport(new Rect(0,0,1080,1920),1920);captureView.SetForeground(true);await Task.Yield();
                var production=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/HotpotSort/Content/Daily/"+DailyContent.RuntimeFileName);
                var layouts=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/HotpotSort/Content/Daily/"+DailyLayoutCatalog.RuntimeFileName);
                var reference=DailySessionFactory.FromProductionJson(production.text,layouts.text);
                DateTime first=new DateTime(2026,9,28);
                for(int offset=0;offset<7;offset++)
                {
                    string day=first.AddDays(offset).ToString("yyyyMMdd",System.Globalization.CultureInfo.InvariantCulture);
                    var expected=reference.WithLayoutForChallenge(day);string id=expected.Layout.LayoutId;
                    var context=new ChallengeContext(day,composition.ContentVersion,composition.ConfigurationDigest,"task044",offset);
                    var warm=(DailySession)composition.CreateStage(context,ChallengeStage.Warmup,0);
                    var warmFixture=LayoutFixture(expected.Layout,true,offset);captureView.Bind(new VisualCapture.CapturePort(warmFixture));captureView.World.SetSimulating(false);await Task.Yield();
                    string warmPath=Path.Combine(root,$"day-{offset}-{id}-warmup.png");VisualCapture.Capture(captureView,1080,1920,warmPath);
                    Check(composition.ProductionContent.Digest==expected.Content.Digest,$"day {offset} selected {id}",checks);
                    Check(VisibleCapture(warmPath),$"day {offset} warmup screenshot visible 1080x1920",checks);
                    warm.Supply(new SupplyObservation(1,0,true,true));
                    var warmState=CanonicalJson.Map(CanonicalJson.Parse(warm.Snapshot.CanonicalStateJson));
                    var warmOrders=CanonicalJson.Array(warmState["orders"]).Select(CanonicalJson.Map).Where(o=>(bool)(o["state"].ToString()!="Locked")).Select(o=>o["kind"] as string).ToArray();
                    var tutorialActive=CanonicalJson.Array(warmState["items"]).Select(CanonicalJson.Map).Any(item=>CanonicalJson.Int(item["plateId"])==1&&(string)item["kind"]==expected.Layout.TutorialKind&&(string)item["location"]=="ActiveAvailable");
                    Check(warmOrders.Contains(expected.Layout.TutorialKind)&&tutorialActive,$"day {offset} tutorial target is an active opening-order item",checks);
                    string warmMapping=CanonicalJson.Write(CanonicalJson.Map(CanonicalJson.Parse(warm.Snapshot.CanonicalStateJson))["mapping"]);
                    var formal=(DailySession)composition.CreateStage(context,ChallengeStage.Formal,warm.UnlockedExtraPotMask);
                    var formalFixture=LayoutFixture(expected.Layout,false,offset);captureView.Bind(new VisualCapture.CapturePort(formalFixture));captureView.World.SetSimulating(false);await Task.Yield();
                    string formalPath=Path.Combine(root,$"day-{offset}-{id}-formal.png");VisualCapture.Capture(captureView,1080,1920,formalPath);
                    string formalMapping=CanonicalJson.Write(CanonicalJson.Map(CanonicalJson.Parse(formal.Snapshot.CanonicalStateJson))["mapping"]);
                    Check(warmMapping==formalMapping,$"day {offset} warmup/formal mapping shared",checks);
                    Check(formal.ConsecutiveFormalFailuresAtStart==composition.ConsecutiveFormalFailures,$"day {offset} account failure count frozen",checks);
                    Check(VisibleCapture(formalPath),$"day {offset} formal screenshot visible 1080x1920",checks);
                    formal.Dispose();warm.Dispose();await Task.Yield();
                }
                UnityEngine.Object.Destroy(captureView.gameObject);
                Check(Directory.GetFiles(root,"day-*.png").Length==14,"fourteen representative screenshots",checks);
                File.WriteAllText(Path.Combine(root,"integration-result.json"),CanonicalJson.Write(CanonicalJson.Object("status","Passed","checks",checks,"screenshots",14,"width",1080,"height",1920)));
                Debug.Log("TASK044_INTEGRATION_PASS checks="+checks.Count+" screenshots=14");
            }
            catch(Exception e){exit=1;Debug.LogException(e);File.WriteAllText(Path.Combine(root,"integration-error.txt"),e.ToString());}
            finally{SessionState.SetBool(Active,false);EditorApplication.Exit(exit);}
        }
        static void Check(bool pass,string message,List<string> checks){if(!pass)throw new Exception(message);checks.Add(message);}
        static bool VisibleCapture(string path)
        {
            if(!File.Exists(path)||new FileInfo(path).Length<100000)return false;
            var texture=new Texture2D(2,2);try{return texture.LoadImage(File.ReadAllBytes(path))&&texture.width==1080&&texture.height==1920;}finally{UnityEngine.Object.Destroy(texture);}
        }
        static HotpotSort.Presentation.ViewSnapshot LayoutFixture(DailyLayoutPair layout,bool warmup,int day)
        {
            var source=(warmup?layout.WarmupPlates:layout.FormalPlates).Take(warmup?layout.WarmupPlates.Count:12).ToArray();
            var snapshot=new HotpotSort.Presentation.ViewSnapshot{sessionId=$"task044-{day}-{(warmup?"warmup":"formal")}",sessionGeneration=day+1,revision=1,phase=HotpotSort.Presentation.ViewPhase.Running,
                orders=new[]{new HotpotSort.Presentation.ViewOrder{slot=0,foodId=0,required=3,enabled=true},new HotpotSort.Presentation.ViewOrder{slot=1,foodId=1,required=3,enabled=true},new HotpotSort.Presentation.ViewOrder{slot=2,foodId=2,required=3},new HotpotSort.Presentation.ViewOrder{slot=3,foodId=3,required=3}},
                buffer=new HotpotSort.Presentation.ViewItem[5],completedOrders=warmup?0:6,totalOrders=67,thirdPotThreshold=37,fourthPotThreshold=55};
            int item=1;snapshot.plates=source.Select((plate,index)=>
            {
                int column=index%4,row=index/4,n=plate.Kinds.Count;float radius=39+6*(n-1);
                var viewPlate=new HotpotSort.Presentation.ViewPlate{plateId=$"{layout.LayoutId}-{(warmup?"w":"f")}-{plate.PlateId}",x=55+column*103,y=390+row*145,radius=radius,motion=new HotpotSort.Presentation.ViewPlateMotion{animateEntry=false},items=new HotpotSort.Presentation.ViewItem[n]};
                for(int i=0;i<n;i++){float angle=(float)(i*Math.PI*2/Math.Max(1,n));float spread=n==1?0:Math.Min(29,14+4*n);viewPlate.items[i]=new HotpotSort.Presentation.ViewItem{itemId=(item++).ToString(),foodId=plate.Kinds[i][0]-'A',x=Mathf.Cos(angle)*spread,y=Mathf.Sin(angle)*spread,radius=16};}
                return viewPlate;
            }).ToArray();return snapshot;
        }

        public static void BuildPlayer()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-task044Evidence");
            if(i<0||i+1>=args.Length)throw new ArgumentException("-task044Evidence required");
            string root=args[i+1],player=Path.Combine(root,"Player","HotpotSortTask044.exe");Directory.CreateDirectory(Path.GetDirectoryName(player));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/HotpotSort/Scenes/Boot.unity"},locationPathName=player,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText(Path.Combine(root,"player-build-result.json"),"{\"result\":\""+report.summary.result+"\",\"errors\":"+report.summary.totalErrors+",\"warnings\":"+report.summary.totalWarnings+",\"bytes\":"+report.summary.totalSize+"}");
            Debug.Log("TASK044_PLAYER_BUILD "+report.summary.result);EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
        }
    }
}
#endif
