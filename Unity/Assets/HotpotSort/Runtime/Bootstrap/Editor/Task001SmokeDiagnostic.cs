#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Globalization;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Determinism;
using HotpotSort.Presentation;
using HotpotSort.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using PlayerSettings = HotpotSort.Contracts.PlayerSettings;

namespace HotpotSort.Bootstrap
{
    [InitializeOnLoad]
    public static class Task001SmokeDiagnostic
    {
        const string Active="Hotpot.Task001.Smoke.Active",PathKey="Hotpot.Task001.Smoke.Path",FilterKey="Hotpot.Task001.Smoke.Filter";
        static readonly List<object> results=new List<object>();
        static readonly List<string> errors=new List<string>();
        static Bootstrap boot;
        static DailyProductionComposition composition;
        static GameplayView view;
        static SessionController controller;
        static string isolatedProfile;
        static Task001SmokeDiagnostic(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run()
        {
            string[] args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-task001Report");
            string path=i>=0&&i+1<args.Length?args[i+1]:System.IO.Path.Combine(System.IO.Path.GetTempPath(),"hotpot-task001-v4-unity-qa.json");
            SessionState.SetString(PathKey,path);SessionState.SetBool(Active,true);
            int filter=Array.IndexOf(args,"-task001Cases");
            SessionState.SetString(FilterKey,filter>=0&&filter+1<args.Length?args[filter+1]:"");
            EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Active,false))Execute();}
        static void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+trace);}
        static void Assert(bool condition,string why){if(!condition)throw new InvalidOperationException(why);}
        static async Task Wait(int milliseconds=120){await Task.Delay(milliseconds);}
        static async Task Case(string id,string ac,string input,string expected,Func<Task<object>> body)
        {
            string filter=SessionState.GetString(FilterKey,"");
            if(filter.Length>0&&!filter.Split(',').Contains(id))return;
            string status="PASS";object actual;
            try{actual=await body();}catch(Exception e){status="FAIL";actual=e.ToString();}
            results.Add(CanonicalJson.Object("case",id,"ac",ac,"input",input,"expected",expected,"actual",actual,"status",status));Debug.Log(id+" "+status+(status=="FAIL"?" "+actual:""));
        }
        static Dictionary<string,object> State()=>CanonicalJson.Map(CanonicalJson.Parse(composition.ActiveCore.Snapshot.CanonicalStateJson));
        static Transform Node(string name){if(name=="hint")name="Hint";if(name=="splash")name="splash_ripple";return view.GetComponentsInChildren<Transform>(true).Where(t=>t.name==name&&(name!="Hint"||t.gameObject.activeInHierarchy)).OrderByDescending(t=>t.gameObject.activeInHierarchy).FirstOrDefault();}
        static void Click(string text)
        {if(text=="继续")text="继续下锅";if(text=="退出")text="返回首页";if(text=="音乐开关"){var toggle=view.GetComponentsInChildren<Button>().Where(b=>b.name=="Action_开"||b.name=="Action_关").OrderByDescending(b=>((RectTransform)b.transform).anchoredPosition.y).First();toggle.onClick.Invoke();return;}var node=Node("Action_"+text)??Node(text);Assert(node!=null&&node.gameObject.activeInHierarchy,"button unavailable: "+text);node.GetComponent<Button>().onClick.Invoke();}
        static async void Execute()
        {
            Application.logMessageReceived+=Log;results.Clear();errors.Clear();string report=SessionState.GetString(PathKey,"");
            try
            {
                await Case("U01","F-02,F-04,T-01","real Boot PlayMode, UI Start/Pause/Resume/Retry/Exit, timeout","real session and correct flow labels",async()=>
                {
                    for(int n=0;n<100;n++){boot=UnityEngine.Object.FindFirstObjectByType<Bootstrap>();if(boot&&boot.IsConfigured)break;await Wait(50);}
                    Assert(boot&&boot.IsConfigured,"Boot initialization: "+boot?.Status);controller=boot.Controller;composition=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();view=composition.PlayerView;
                    isolatedProfile="HotpotSort.Task001.v9.FinalQA."+Guid.NewGuid().ToString("N");var isolated=new LocalDevelopmentServices(isolatedProfile);isolated.RewardPrompt=view.ShowRewardSimulationAsync;isolated.SharePrompt=view.ShowThemeShareAsync;composition.ConfigureServices(isolated,isolated,isolated,isolated);
                    Assert(view.LastSnapshot.phase==ViewPhase.Entry&&controller.Snapshot==null,"auto-started");Click("开始下火锅");await Wait(350);Assert(composition.ActiveCore!=null&&view.LastSnapshot.phase==ViewPhase.Running,"start did not run");Assert(view.LastSnapshot.orders.Length==4&&view.LastSnapshot.buffer.Length==5,"view shape");
                    Assert(view.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("/3")),"numeric counts missing");Assert(view.GetComponentsInChildren<Text>().Any(t=>t.text=="10:00"||t.text=="09:59"),"MM:SS missing");
                    Click("暂停");await Wait();Assert(controller.Pauses==PauseReasons.User&&view.LastSnapshot.phase==ViewPhase.Paused,"pause");double paused=controller.ActiveSeconds;await Wait(150);Assert(Math.Abs(controller.ActiveSeconds-paused)<.002,"pause clock moving");Click("继续");await Wait();Assert(controller.CanAcceptInput,"resume");
                    typeof(SessionController).GetField("accumulated",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(controller,600d);await Wait();Assert(view.LastSnapshot.phase==ViewPhase.Overflow&&view.LastSnapshot.message=="Timeout","idle timeout");Assert(view.GetComponentsInChildren<Text>().Any(t=>t.text=="时间到"),"timeout Chinese label");
                    Click("重新挑战");await Wait();Assert(controller.CanAcceptInput,"retry");
                    ulong overflowSequence=10000;for(int attempt=0;attempt<150&&composition.ActiveCore.Snapshot.Status==GameStatus.Running;attempt++)
                    {
                        var state=State();var kinds=CanonicalJson.Array(state["orders"]).Select(CanonicalJson.Map).Where(o=>(string)o["state"]!="Locked").Select(o=>(string)o["kind"]).ToArray();
                        var mismatch=CanonicalJson.Array(state["items"]).Select(CanonicalJson.Map).FirstOrDefault(i=>(string)i["location"]=="ActiveAvailable"&&!kinds.Contains((string)i["kind"]));
                        if(mismatch!=null)composition.ActiveCore.Tap(new TapCommand(CanonicalJson.Int(mismatch["itemId"]),++overflowSequence,(ulong)(controller.ActiveSeconds*1000),true));await Wait(40);
                    }
                    Assert(view.LastSnapshot.revivalPending&&controller.Pauses.HasFlag(PauseReasons.Revival)&&view.GetComponentsInChildren<Text>().Any(t=>t.text=="暂存已满"),"revival offer UI missing");Click("结束本局");await Wait();Assert(view.LastSnapshot.phase==ViewPhase.Overflow,"decline must fail");Click("重新挑战");await Wait();
                    Click("暂停");Click("退出");await Wait();Assert(view.LastSnapshot.phase==ViewPhase.Entry&&composition.ActiveCore==null,"exit");Click("开始下火锅");await Wait(2200);return "Boot and all inspected flow transitions passed, including timeout and overflow offer/decline";
                });
                if(view&&composition.ActiveCore!=null)
                {
                    await Case("U02","F-02,T-02","3 portrait viewport sizes and safe insets, actual SubmitScreenTap","uniform scale, safe rect, exact item routing; out-of-board rejects",async()=>
                    {
                        var samples=new List<object>();
                        foreach(var size in new[]{new Vector2(720,1280),new Vector2(1080,1920),new Vector2(1440,3200)})
                        {
                            view.SetViewport(new Rect(12,36,size.x-24,size.y-88));await Wait(200);var board=(RectTransform)Node("GameplayBoard");Assert(Math.Abs(board.localScale.x-board.localScale.y)<.00001,"nonuniform board scale");
                            var body=view.World.Bodies.FirstOrDefault();Assert(body!=null,"no active plate");Move(view.World,body,new Vector2(210,500));var item=body.data.items.Last();Vector2 point=view.World.ItemPosition(item.itemId)+OpaqueOffset(item);Vector2 screen=RectTransformUtility.WorldToScreenPoint(null,board.TransformPoint(new Vector3(point.x,-point.y,0)));
                            Assert(view.SubmitScreenTap(screen),"screen->item hit failed "+size);await Wait();Assert(!view.World.Bodies.SelectMany(b=>b.data.items).Any(i=>i.itemId==item.itemId),"wrong clicked item");Assert(!view.SubmitScreenTap(new Vector2(-50,-50)),"outside click accepted");samples.Add(CanonicalJson.Object("width",size.x,"height",size.y,"scale",board.localScale.x,"clickedItem",item.itemId));
                        }
                        view.SetViewport(new Rect(0,0,Screen.width,Screen.height));
                        var currentItem=view.World.Bodies.First().data.items.First();var currentBoard=(RectTransform)Node("GameplayBoard");var at=view.World.ItemPosition(currentItem.itemId);var hit=RectTransformUtility.WorldToScreenPoint(null,currentBoard.TransformPoint(new Vector3(at.x,-at.y,0)));
                        view.ShowNotice("输入遮挡验证","弹窗覆盖期间不能点击盘内食材");Canvas.ForceUpdateCanvases();Assert(!view.SubmitScreenTap(hit),"modal click through");Click("知道了");return samples;
                    });
                    await Case("U03","CL-025","real Boot controlled six-plate shuffle","visible 304..828 targets, no core changes, no movement on rejection",ShuffleCheck);
                    await Case("V6-S01","CL-050","controlled local rigidbodies at exact boundary values","center-only fixed 140 gate independent of radius/crop",HeightGateCheck);
                    await Case("V6-S02","C05","real Composition controlled bodies and active clock","blocked preservation, one-head resume, no catch-up, stale rejection",SupplyCheck);
                    await Case("V6-S03","CL-023","six committed births, blocked checks, repeat snapshots and retry","cached alternating independent spawn stream",SpawnCheck);
                    await Case("C06","C06","real core buffer 1/5 and real Composition supply chain","append identity conservation and eventual single-force birth",ClearTailCheck);
                    await Case("V6-P01","CL-024","controlled local physics steps","one +5 Force plus continuous model; no repeated force",ForceCheck);
                    await Case("V6-P02","CL-024","max hidden plate and overlapping pair, 120 steps","collision retained, re-emergence, finite state and separation",HiddenPhysicsCheck);
                    await Case("V6-V01","CL-023","controlled actual Canvas paired captures at three sizes","no plate effects above crop; exposed pixels and foreground feedback",VisualCropCheck);
                    await Case("V6-I01","CL-023","real SubmitScreenTap controlled real-core food positions","alpha/crop/UI rejection; exact exposed item acceptance",InputCheck);
                    await Case("V6-I02","CL-023","controlled real view targets and alpha slivers","first truly clickable target; hidden/occluded invalid; bounded timing",HintCheck);
                    await Case("V6-I03","CL-016","real coordinator controlled delayed reward service","visibility revalidation; one quota only on effective reward",RewardCheck);
                    await Case("V6-L01","CL-023","real controller pauses and terminal core flows","stable paused world; no terminal supply; retry resets",LifecycleCheck);
                    await Case("U04","F-02,E-03","unchanged snapshots, hint on moving item, whole-pot event, retry cleanup","persistent plate nodes and event layers, no raycast effects, no ghosts",async()=>
                    {
                        await controller.RetryAsync();await Wait(600);var plate=view.World.Bodies.First();Move(view.World,plate,new Vector2(210,500));string id=plate.data.plateId;var node=Node("PlateVisual_"+id);int instance=node.GetInstanceID();view.Apply(new ViewUpdate{snapshot=view.LastSnapshot});Assert(Node("PlateVisual_"+id).GetInstanceID()==instance,"plate rebuilt");
                        var keyframes=new List<object>();Action<string,double> capture=(name,start)=>{string path=System.IO.Path.ChangeExtension(report,name+".png");double time=Time.realtimeSinceStartupAsDouble;Capture(1080,1920,path);keyframes.Add(CanonicalJson.Object("state",name,"secondsSinceTrigger",time-start,"path",path));};
                        double hintStart=Time.realtimeSinceStartupAsDouble;view.HighlightItem(plate.data.items[0].itemId);await Wait(80);var hint=Node("hint");Assert(hint!=null,"hint node absent");Vector2 hintBefore=((RectTransform)hint).anchoredPosition;capture("hint-before",hintStart);await Wait(180);Assert(Node("hint")!=null,"hint ended early");Assert(((RectTransform)hint).anchoredPosition!=hintBefore,"hint not following motion");capture("hint-after",hintStart);
                        var eventBatch=new ViewUpdate{snapshot=view.LastSnapshot,events=new[]{new ViewEvent{sessionId=view.LastSnapshot.sessionId,sessionGeneration=view.LastSnapshot.sessionGeneration,kind="OrderCompleted",slot=0,ingredientId="food_00",sequence=1000000,transactionId="qa-serving"}}};double serveStart=Time.realtimeSinceStartupAsDouble;view.Apply(eventBatch);await Wait(60);Assert(Node("ServingWholePot_0")!=null&&Node("WholePotBody")!=null,"whole pot absent");Assert(!Node("Order_0").gameObject.activeSelf,"replacement shown before serve");capture("serve-complete",serveStart);await Wait(260);capture("serve-moving",serveStart);await Wait(700);Assert(Node("Order_0").gameObject.activeSelf,"replacement never restored");capture("serve-replaced",serveStart);
                        File.WriteAllText(System.IO.Path.ChangeExtension(report,"keyframes.json"),CanonicalJson.Write(Normalize(CanonicalJson.Object("kind","Actual Canvas rendering of a synthetic OrderCompleted presentation event; not a core completion or target-device capture","keyframes",keyframes))));
                        Assert(Node("FixedHUD").GetSiblingIndex()>Node("FeedbackLayer").GetSiblingIndex(),"serving effect covers HUD");
                        Assert(view.GetComponentsInChildren<Graphic>().Where(g=>g.transform.IsChildOf(Node("FeedbackLayer"))).All(g=>!g.raycastTarget),"effect captures input");await controller.RetryAsync();await Wait(150);Assert(Node("ServingWholePot_0")==null&&Node("hint")==null,"old effects retained");return "Persistent nodes, moving hint, real pot layers and cleanup passed; synthetic view event does not establish gameplay feel";
                    });
                    await Case("U05","F-03,F-04,F-05","real simulation dialogs, isolated PlayerPrefs profile, settings","UI outcomes match; persist only profile/quota/settings; no real services",async()=>
                    {
                        string key="HotpotSort.Task001.QA."+Guid.NewGuid().ToString("N");
                        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
                        var oldProfile=(IProfileStore)typeof(DailyProductionComposition).GetField("profile",flags).GetValue(composition);
                        var oldRewards=(IRewardService)typeof(DailyProductionComposition).GetField("rewardService",flags).GetValue(composition);
                        var oldFriends=(IFriendBoard)typeof(DailyProductionComposition).GetField("friends",flags).GetValue(composition);
                        var oldShare=(IThemeShare)typeof(DailyProductionComposition).GetField("sharing",flags).GetValue(composition);
                        try
                        {
                            var local=new LocalDevelopmentServices(key);composition.ConfigureServices(local,local,local,local);Assert(local.RecordFirstWin("20260921")&&!local.RecordFirstWin("20260921"),"first-win not idempotent");var quotaUtc=DateTimeOffset.Parse("2026-09-21T00:00:00Z");for(int i=0;i<3;i++){var now=quotaUtc.AddMinutes(i*20);Assert(local.TryReserveShare("20260921","r"+i,now)&&local.TryCommitShare("20260921","r"+i,now),"quota consume");}Assert(!local.TryReserveShare("20260921","r3",quotaUtc.AddHours(2)),"quota >3");local.SaveSettings(new PlayerSettings{MusicEnabled=false,EffectsEnabled=true,MusicVolume=.25f,EffectsVolume=.7f});var reload=new LocalDevelopmentServices(key);Assert(reload.TotalFirstWins==1&&reload.SharesUsed("20260921")==3&&!reload.LoadSettings().MusicEnabled&&Math.Abs(reload.LoadSettings().EffectsVolume-.7f)<.001,"profile reload");
                            foreach(var outcome in new[]{"模拟成功","取消","模拟失败"}){var request=new RewardRequest(controller.Generation,RewardKind.SwapOrder,RewardRoute.SimulatedAd,"20260921");var pending=view.ShowRewardSimulationAsync(request);Click(outcome);Assert(await pending==(outcome=="模拟成功"?RewardOutcome.Success:outcome=="取消"?RewardOutcome.Cancelled:RewardOutcome.Failed),"simulation result");}
                            Click("暂停");Click("设置");Click("音乐开关");Click("完成");Assert(Node("Flow_Paused")!=null,"settings did not restore pause");Click("继续");return "Simulation outcomes and isolated persistent store passed";
                        }
                        finally{composition.ConfigureServices(oldProfile,oldRewards,oldFriends,oldShare);view.SetAudioSettings(oldProfile.LoadSettings());PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
                    });
                    await Case("U08","T-02","10 retries and 30-second development runtime sample","one view/world; measured editor frame times, no device claim",async()=>
                    {
                        for(int n=0;n<10;n++){await controller.RetryAsync();await Wait(70);}
                        Assert(UnityEngine.Object.FindObjectsByType<GameplayView>(FindObjectsSortMode.None).Length==1,"duplicate views");Assert(UnityEngine.Object.FindObjectsByType<HotpotSort.UnityPhysics.PlatePresentationWorld>(FindObjectsSortMode.None).Length==1,"duplicate worlds");
                        var timings=new List<double>();double start=Time.realtimeSinceStartupAsDouble,last=start;int frames=Time.frameCount,lastFrame=frames;
                        while(Time.realtimeSinceStartupAsDouble-start<30){await Task.Yield();double now=Time.realtimeSinceStartupAsDouble;if(Time.frameCount!=lastFrame){timings.Add(now-last);last=now;lastFrame=Time.frameCount;}}
                        timings.Sort();return CanonicalJson.Object("machine",SystemInfo.deviceModel,"graphics",SystemInfo.graphicsDeviceName,"width",Screen.width,"height",Screen.height,"seconds",Time.realtimeSinceStartupAsDouble-start,"frames",Time.frameCount-frames,"observedP95Seconds",timings.Count==0?0:timings[(int)(timings.Count*.95)],"gameObjects",UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length,"note","Editor batch with bound visual root "+view.AssetRoot+"; device performance unverified");
                    });
                }
                await Case("U06","E-01,T-01,T-02","formal manifest and real imported assets/UI/fonts; 3 rendered sizes","all batch hashes/import contracts, Chinese glyphs, no legacy textures; visual Human Check remains",async()=>
                {
                    Assert(view.AssetRoot==composition.ApprovedAssetRoot,"formal root not bound");Assert(TaskAssetValidation.Validate(view.AssetRoot).Length==0,"formal validation failed");
                    string root=Directory.GetParent(Application.dataPath).Parent.FullName;var manifest=CanonicalJson.Map(CanonicalJson.Parse(File.ReadAllText(System.IO.Path.Combine(root,".harness/art-production/TASK-001/v7/r001/director-final/asset-manifest.json"))));int count=0;
                    foreach(var asset in CanonicalJson.Array(manifest["assets"]).Select(CanonicalJson.Map))
                    {
                        string id=(string)asset["assetId"],relative=(string)asset["relativePath"];string path=relative.Substring("Unity/".Length);
                        Assert(CanonicalJson.Hash(File.ReadAllBytes(path))==(string)asset["sha256"],"hash "+id);count++;
                        if((string)asset["format"]!="PNG")continue;
                        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);Assert(texture.width==CanonicalJson.Int(asset["width"])&&texture.height==CanonicalJson.Int(asset["height"]),"dimensions "+id);
                        var importer=(TextureImporter)AssetImporter.GetAtPath(path);var borders=CanonicalJson.Array(asset["nineSliceBorder"]).Select(Convert.ToSingle).ToArray();
                        Assert(importer.spriteBorder==new Vector4(borders[0],borders[1],borders[2],borders[3])&&!importer.mipmapEnabled&&importer.textureCompression==TextureImporterCompression.Uncompressed,"import contract "+id);
                        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);Assert(sprite&&Vector2.Distance(sprite.pivot,new Vector2(texture.width*.5f,texture.height*.5f))<.01,"pivot "+id);
                    }
                    Assert(count==65,"manifest count");
                    const string required="来《一锅又一锅》，一起开锅！每日挑战北京时间更新本地开发模拟开始设置好友榜暂停继续重新退出提示清空暂存打乱提前单开领取激励视频分享取消成功失败音乐音效关闭完成时间到已满暂无有效目标0123456789/：";
                    view.playerFont.RequestCharactersInTexture(required,24);Assert(required.All(c=>view.playerFont.HasCharacter(c)),"Chinese/numeric glyph missing");
                    foreach(var texture in view.GetComponentsInChildren<RawImage>(true).Where(i=>i.texture).Select(i=>i.texture))Assert(AssetDatabase.GetAssetPath(texture).StartsWith("Assets/HotpotSort/Resources/"+view.AssetRoot),"legacy visual texture "+texture.name);
                    Assert(Node("Tool_Hint")&&Node("Tool_ClearBuffer")&&Node("Tool_Shuffle")&&Node("Timer"),"formal UI not consumed");
                    var captures=new List<string>();foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1440,3200)}){string path=System.IO.Path.ChangeExtension(report,size.x+"x"+size.y+".png");Capture(size.x,size.y,path);captures.Add(path);await Wait(20);}
                    return CanonicalJson.Object("assetCount",count,"font",view.playerFont.name,"screenshots",captures,"humanVisualApproval","PENDING","audio","not bound");
                });
                if(SessionState.GetString(FilterKey,"").Length==0)results.Add(CanonicalJson.Object("case","U07","ac","E-02,T-02","input","Suno noncommercial audio batch","expected","accepted licence/source and real clips","actual","Audio generation access and accepted files not available; silent","status","DEFERRED"));
            }
            catch(Exception e){errors.Add(e.ToString());}
            finally
            {
                Application.logMessageReceived-=Log;SessionState.SetBool(Active,false);int failed=results.Select(CanonicalJson.Map).Count(r=>(string)r["status"]=="FAIL");
                string[] required={"U01","U02","U03","U04","U05","V6-S01","V6-S02","V6-S03","C06","V6-P01","V6-P02","V6-V01","V6-I01","V6-I02","V6-I03","V6-L01"};
                var missing=required.Where(id=>!results.Select(CanonicalJson.Map).Any(r=>(string)r["case"]==id)).ToArray();if(missing.Length>0){errors.Add("Required cases missing: "+string.Join(",",missing));failed++;}
                try{File.WriteAllText(report,CanonicalJson.Write(Normalize(CanonicalJson.Object("task","TASK-001","taskVersion",9,"checkpoint","HC-02-v9-Code","contentVersion",composition?composition.ContentVersion:null,"configurationDigest",composition?composition.ConfigurationDigest:null,"requiredCases",required,"missingCases",missing,"filter",SessionState.GetString(FilterKey,""),"unity",Application.unityVersion,"results",results,"runtimeErrors",errors))));Debug.Log("TASK001_UNITY_QA_REPORT "+report+" failed="+failed+" errors="+errors.Count);}
                catch(Exception e){failed++;Debug.LogError("QA report failed: "+e);}
                finally{if(isolatedProfile!=null){PlayerPrefs.DeleteKey(isolatedProfile);PlayerPrefs.Save();}EditorApplication.Exit(failed==0&&errors.Count==0?0:1);}
            }
        }
        static object Normalize(object value)
        {
            if(value is float || value is double || value is decimal)return Convert.ToString(value,CultureInfo.InvariantCulture);
            if(value is IDictionary<string,object> map)return map.ToDictionary(p=>p.Key,p=>Normalize(p.Value));
            if(value is IEnumerable sequence && !(value is string))return sequence.Cast<object>().Select(Normalize).ToArray();
            return value;
        }
        static async Task Until(Func<bool> condition,string assertion)
        {
            double deadline=Time.realtimeSinceStartupAsDouble+5;
            while(!condition()&&Time.realtimeSinceStartupAsDouble<deadline)await Task.Yield();
            Assert(condition(),assertion);
        }
        const BindingFlags PrivateInstance=BindingFlags.Instance|BindingFlags.NonPublic;
        static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,PrivateInstance).GetValue(target);
        static void Field(object target,string name,object value)=>target.GetType().GetField(name,PrivateInstance).SetValue(target,value);
        static object Invoke(object target,string name,params object[] args)=>target.GetType().GetMethod(name,PrivateInstance).Invoke(target,args);
        static ulong BoundaryNow=>(ulong)(controller.ActiveSeconds*1000);
        static int SpawnCount=>Field<int>(composition,"successfulSpawns");
        static void Move(HotpotSort.UnityPhysics.PlatePresentationWorld world,HotpotSort.UnityPhysics.PlatePresentationWorld.PlateBody body,Vector2 at)
        {body.rigidbody.position=at*.01f;body.node.transform.position=at*.01f;body.rigidbody.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();}
        // Drive the real active-time schedule, never write its next-tick cursor.
        sealed class SupplyClock:IMonotonicClock
        {
            readonly IMonotonicClock source;double offset;public double Value;public bool Manual;
            public SupplyClock(IMonotonicClock source){this.source=source;}
            public double Seconds=>Manual?Value:source.Seconds+offset;
            public void Freeze(){Value=Seconds;Manual=true;}
            public void Resume(){if(Manual){offset=Value-source.Seconds;Manual=false;}}
        }
        static SupplyClock supplyClock;
        static ActiveSupplySchedule Schedule=>Field<ActiveSupplySchedule>(composition,"supplySchedule");
        static void FreezeClock(){if(supplyClock==null){supplyClock=new SupplyClock(Field<IMonotonicClock>(controller,"clock"));Field(controller,"clock",supplyClock);}supplyClock.Freeze();}
        static void AdvanceActiveTo(double seconds){FreezeClock();Assert(seconds>=controller.ActiveSeconds-.000001,"fixture must not rewind active time");supplyClock.Value+=Math.Max(0,seconds-controller.ActiveSeconds);}
        static void StopAutomatic(){FreezeClock();composition.enabled=false;view.World.enabled=false;view.World.SetSimulating(true);}
        static void RestoreAutomatic(){supplyClock?.Resume();composition.enabled=true;view.World.enabled=true;view.World.SetSimulating(controller.CanAcceptInput);}
        static void ClearGate(){foreach(var body in view.World.Bodies)Move(view.World,body,new Vector2(210,650));}
        static void SupplyOnce(){if(controller.CanAcceptInput)AdvanceActiveTo(Math.Max(controller.ActiveSeconds,Schedule.NextTick*ActiveSupplySchedule.IntervalMilliseconds/1000d+.000001));Invoke(composition,"Update");}
        static void SupplyCandidate(Vector2 position,float radius)
        {
            AdvanceActiveTo(Math.Max(controller.ActiveSeconds,Schedule.NextTick*ActiveSupplySchedule.IntervalMilliseconds/1000d+.000001));
            Assert(Schedule.TryTake(controller.ActiveSeconds,controller.CanAcceptInput),"candidate observation must own one current schedule tick");
            Field(composition,"observingSupply",true);try{view.ObserveSupply(position,radius);}finally{Field(composition,"observingSupply",false);}
        }
        static async Task Fresh(int count=1)
        {
            composition.enabled=false;FreezeClock();
            if(controller.Resolved==null)await controller.StartTodayAsync();else await controller.RetryAsync();
            Assert(composition.ActiveCore!=null&&controller.CanAcceptInput,"fresh running session");StopAutomatic();
            // A new session gets a zero-origin test clock. Rebase both sides of the
            // clock subtraction so an exact 300ms boundary is not rounded below it
            // by subtracting the preceding tests' large absolute clock values.
            double origin=supplyClock.Value;Field(controller,"runningSince",Field<double>(controller,"runningSince")-origin);supplyClock.Value=0;
            for(int i=0;i<count;i++){ClearGate();SupplyOnce();}
            Canvas.ForceUpdateCanvases();
        }
        static ViewPlate FixturePlate(string id,float x,float y,float radius=63,int food=0)
        {return new ViewPlate{plateId=id,x=x,y=y,radius=radius,motion=new ViewPlateMotion{animateEntry=false},items=new[]{new ViewItem{itemId=id,foodId=food,radius=16}}};}
        static HotpotSort.UnityPhysics.PlatePresentationWorld LocalWorld(out GameObject node)
        {node=new GameObject("V6ControlledPhysics");var world=node.AddComponent<HotpotSort.UnityPhysics.PlatePresentationWorld>();world.enabled=false;world.SetSimulating(true);return world;}
        static ViewSnapshot Fixture(params ViewPlate[] plates)=>new ViewSnapshot{sessionId="v6-controlled-physics",phase=ViewPhase.Running,plates=plates};
        static Task<object> HeightGateCheck()
        {
            GameObject node;var world=LocalWorld(out node);var evidence=new List<object>();
            try
            {
                Assert(world.ObserveHeightGate(7).heightGateClear,"empty gate");
                foreach(float radius in new[]{39f,63f})foreach(int count in new[]{0,1,2,3})
                {
                    world.Reconcile(Fixture(Enumerable.Range(1,count).Select(i=>FixturePlate(i.ToString(),70+i*75,500,radius)).ToArray()));
                    foreach(float y in new[]{139.999f,140f,140.001f})
                    {
                        foreach(var body in world.Bodies)Move(world,body,new Vector2(world.Position(body).x,y));var observation=world.ObserveHeightGate(7);int expected=y<140?count:0;
                        Assert(observation.entryCount==expected&&observation.canSupply==(expected<3),"strict entry count "+count+" at "+y);
                        Assert(observation.version==ViewSupplyObservation.CurrentVersion&&observation.entryLimit==3&&observation.fixedGate==140&&observation.snapshotRevision==7,"observation metadata");
                        evidence.Add(CanonicalJson.Object("radius",radius,"count",count,"requestedY",y,"entryCount",observation.entryCount,"allowed",observation.canSupply));
                    }
                }
                world.Reconcile(Fixture(FixturePlate("1",5,140,63),FixturePlate("2",5,139.999f,39)));
                Assert(world.ObserveHeightGate(8).entryCount==1&&world.ObserveHeightGate(8).canSupply,"one entry with equality excluded");
                Move(world,world.Bodies.Last(),new Vector2(5,140));
                Assert(world.ObserveHeightGate(8).heightGateClear,"radius edge/overlap must not block");
                world.Reconcile(Fixture());Assert(world.Remnants.Any()&&world.ObserveHeightGate(9).heightGateClear,"remnants excluded");
                return Task.FromResult<object>(CanonicalJson.Object("fixture","isolated actual Rigidbody2D roots; no production inventory","samples",evidence,"candidateGeometryIgnored","height observer has no candidate/radius parameters","fixedGate",140,"remnantCount",world.Remnants.Count()));
            }
            finally{UnityEngine.Object.Destroy(node);}
        }
        static async Task<object> SupplyCheck()
        {
            var observations=new List<object>();
            try
            {
                await Fresh(0);Assert(controller.ActiveSeconds==0&&Schedule.NextTick==0,"fresh immediate tick");Invoke(composition,"Update");Assert(SpawnCount==1,"first check must be immediate");
                AdvanceActiveTo(.299999);Invoke(composition,"Update");Assert(SpawnCount==1,"before 300ms supplied");
                AdvanceActiveTo(.3);Invoke(composition,"Update");Assert(SpawnCount==2,"300ms tick missing");
                AdvanceActiveTo(.600001);Invoke(composition,"Update");Assert(SpawnCount==3,"third plate should fit");
                var blocker=view.World.Bodies.First();foreach(var body in view.World.Bodies)Move(view.World,body,new Vector2(view.World.Position(body).x,139));
                int head=DailyViewMapper.PendingHead(composition.ActiveCore.Snapshot),count=SpawnCount;
                string hash=composition.ActiveCore.StateHash,cache=CacheJson();ulong previous=Field<ulong>(composition,"supplySequence");
                for(int i=0;i<3;i++)
                {
                    long tick=Schedule.NextTick;SupplyOnce();Assert(Schedule.NextTick==tick+1,"blocked tick not advanced");
                    ulong current=Field<ulong>(composition,"supplySequence");Assert(current==previous+1,"single observation");
                    Assert(SpawnCount==count&&CacheJson()==cache&&composition.ActiveCore.StateHash==hash&&DailyViewMapper.PendingHead(composition.ActiveCore.Snapshot)==head,"blocked state unchanged");
                    observations.Add(CanonicalJson.Object("sequence",current.ToString(),"nextTick",Schedule.NextTick,"activeSeconds",controller.ActiveSeconds,"head",head,"cache",cache));previous=current;
                }
                var stale=view.World.ObserveHeightGate(view.LastSnapshot.revision-1);stale.heightGateClear=true;
                Field(composition,"observingSupply",true);try{composition.ObserveSupply(stale);}finally{Field(composition,"observingSupply",false);}Assert(Field<ulong>(composition,"supplySequence")==previous&&SpawnCount==count,"stale observation rejected");
                Move(view.World,blocker,new Vector2(110,140));Invoke(composition,"Update");Assert(SpawnCount==count,"recovery supplied before normal tick");
                SupplyOnce();
                Assert(SpawnCount==count+1&&DailyViewMapper.PendingHead(composition.ActiveCore.Snapshot)==head+1,"same head single catch-up-free commit");
                Invoke(composition,"Update");Assert(SpawnCount==count+1,"no catch-up");
                ClearGate();AdvanceActiveTo(controller.ActiveSeconds+2);int beforeSkip=SpawnCount;Invoke(composition,"Update");Invoke(composition,"Update");Assert(SpawnCount==beforeSkip+1,"long frame caught up missed ticks");
                ClearGate();int candidateBefore=SpawnCount;
                SupplyCandidate(new Vector2(-999,-999),9999);Assert(SpawnCount==candidateBefore+1,"candidate bounds/radius used as gate");
                ClearGate();candidateBefore=SpawnCount;
                SupplyCandidate(view.World.Position(blocker),blocker.data.radius);Assert(SpawnCount==candidateBefore+1,"candidate overlap used as gate");
                ClearGate();var crop=view.PlateCrop;typeof(GameplayView).GetProperty("PlateCrop").SetValue(view,new Rect(0,400,420,428));
                Move(view.World,blocker,new Vector2(110,140));Assert(view.World.ObserveHeightGate(view.LastSnapshot.revision).heightGateClear,"gate tied to changed crop");
                typeof(GameplayView).GetProperty("PlateCrop").SetValue(view,crop);
                // Empty removal is synchronous; remnants may persist but cannot block.
                foreach(var other in view.World.Bodies.Where(b=>b!=blocker))Move(view.World,other,new Vector2(310,650));
                Move(view.World,blocker,new Vector2(110,200));ulong taps=70000;
                foreach(var item in blocker.data.items.ToArray())Assert(composition.ActiveCore.Tap(new TapCommand(int.Parse(item.itemId),++taps,BoundaryNow,true)).Accepted,"empty fixture tap");
                Assert(!view.World.Bodies.Contains(blocker)&&view.World.Remnants.Any()&&view.World.ObserveHeightGate(view.LastSnapshot.revision).heightGateClear,"empty leaves only ignored remnant");
                return CanonicalJson.Object("fixture","real Boot Composition; controlled frozen body placement","blocked",observations,"beforeHash",hash,"successCount",SpawnCount,"sameHead",head,"staleRejected",true,"noCatchUp",true);
            }finally{RestoreAutomatic();}
        }
        static string CacheJson()=>CanonicalJson.Write(Field<Dictionary<string,ViewPlateMotion>>(composition,"spawnMotions").OrderBy(p=>int.Parse(p.Key)).Select(p=>(object)new object[]{p.Key,p.Value.spawnX.ToString("R",CultureInfo.InvariantCulture),p.Value.spawnY.ToString("R",CultureInfo.InvariantCulture)}).ToArray());
        static async Task<object> SpawnCheck()
        {
            var samples=new List<object>();
            try
            {
                await Fresh(0);var expected=new System.Random(601377);
                for(int n=0;n<6;n++)
                {
                    if(n>0)
                    {
                        string cache=CacheJson();int before=SpawnCount;
                        if(n>=3){foreach(var blocker in view.World.Bodies.Take(3))Move(view.World,blocker,new Vector2(view.World.Position(blocker).x,139));SupplyOnce();SupplyOnce();}
                        else{Invoke(composition,"Update");Invoke(composition,"Update");}
                        Assert(SpawnCount==before&&CacheJson()==cache,"blocked/not-due check consumed spawn sample");
                    }
                    ClearGate();SupplyOnce();var body=view.World.Bodies.Last();var motion=body.data.motion;
                    float offset=(float)(expected.NextDouble()*100-50),basis=n%2==0?150:260;
                    Assert(Math.Abs(motion.spawnX-(basis+offset))<.0001f,"one independent random sample ordinal "+n);
                    Assert(offset>=-50&&offset<=50&&motion.spawnY==DailyViewMapper.SpawnY(body.data.radius),"spawn bounds Y");
                    Assert(body.data.radius==DailyViewMapper.PlateRadius(composition.ActiveCore.PlateSize(int.Parse(body.data.plateId))),"radius preserved");
                    string cacheBefore=CacheJson(),coreHash=composition.ActiveCore.StateHash;
                    Move(view.World,body,new Vector2(200,600));var position=view.World.Position(body);
                    composition.Show(composition.ActiveCore.Snapshot,null);
                    Assert(CacheJson()==cacheBefore&&view.World.Position(body)==position&&composition.ActiveCore.StateHash==coreHash,"snapshot no redraw sample or teleport");
                    samples.Add(CanonicalJson.Object("ordinal",n,"plateId",body.data.plateId,"baseX",basis,"offset",offset,"spawnX",motion.spawnX,"spawnY",motion.spawnY,"radius",body.data.radius,"coreHash",coreHash));
                }
                var target=view.World.Bodies.First();var first=target.data.items.First();string cached=CacheJson();var pos=view.World.Position(target);
                Assert(composition.ActiveCore.Tap(new TapCommand(int.Parse(first.itemId),91000,BoundaryNow,true)).Accepted,"reduction");
                Assert(CacheJson()==cached,"reduction consumes no random");if(view.World.Bodies.Contains(target))Assert(view.World.Position(target)==pos,"reduction teleport");
                await Fresh(0);Assert(SpawnCount==0&&CacheJson()=="[]","retry resets");SupplyOnce();
                Assert(Math.Abs(view.World.Bodies.Single().data.motion.spawnX-(150+(float)(new System.Random(601377).NextDouble()*100-50)))<.0001,"retry sequence");
                controller.Request(SessionAction.Exit);Assert(SpawnCount==0&&CacheJson()=="[]","exit resets");
                return CanonicalJson.Object("fixture","real Boot supply with controlled centers; RNG oracle isolated from core","births",samples,"retryExitReset",true,"coreIsolation","existing C03 rule group separately compares complete hash/events under presentation RNG consumption");
            }finally{RestoreAutomatic();}
        }
        static async Task<object> ShuffleCheck()
        {
            try
            {
                await Fresh(6);string hash=composition.ActiveCore.StateHash;var positions=view.World.Bodies.ToDictionary(b=>b.data.plateId,b=>view.World.Position(b));
                bool feasible=view.World.TryShuffle(false);Assert(view.World.Bodies.All(b=>positions[b.data.plateId]==view.World.Position(b)),"shuffle preview moved");
                Assert(feasible&&view.World.TryShuffle(true),"six plates legal shuffle");var bodies=view.World.Bodies.ToArray();
                for(int i=0;i<bodies.Length;i++)
                {
                    var at=view.World.Position(bodies[i]);float r=bodies[i].data.radius;
                    Assert(at.x-r>=-.001&&at.x+r<=420.001&&at.y-r>=303.999&&at.y+r<=828.001,"visible shuffle bounds");
                    Assert(bodies[i].rigidbody.linearVelocity==Vector2.zero,"shuffle velocity");
                    for(int j=i+1;j<bodies.Length;j++)Assert(Vector2.Distance(at,view.World.Position(bodies[j]))>=r+bodies[j].data.radius-.001,"shuffle overlap");
                }
                Assert(composition.ActiveCore.StateHash==hash,"shuffle core change");
                return CanonicalJson.Object("fixture","real inventory; controlled initial positions","count",bodies.Length,"hashBeforeAfter",hash,"positions",bodies.Select(b=>(object)new object[]{b.data.plateId,view.World.Position(b).x,view.World.Position(b).y}).ToArray(),"failureCoverage","V6-I03 impossible-packing reward target and no quota");
            }finally{RestoreAutomatic();}
        }
        static async Task<object> ClearTailCheck()
        {
            var evidence=new List<object>();
            try
            {
                foreach(int count in new[]{1,5})
                {
                    await Fresh(12);ulong taps=100000;
                    for(int i=0;i<count;i++)
                    {
                        var state=State();var kinds=CanonicalJson.Array(state["orders"]).Select(CanonicalJson.Map).Where(o=>(string)o["state"]!="Locked").Select(o=>(string)o["kind"]).ToArray();
                        var item=CanonicalJson.Array(state["items"]).Select(CanonicalJson.Map).First(x=>(string)x["location"]=="ActiveAvailable"&&!kinds.Contains((string)x["kind"]));
                        Assert(composition.ActiveCore.Tap(new TapCommand(CanonicalJson.Int(item["itemId"]),++taps,BoundaryNow,true)).Accepted,"buffer fill");
                    }
                    var before=State();var ids=CanonicalJson.Array(before["buffer"]).Where(x=>x!=null).Select(CanonicalJson.Int).ToArray();Assert(ids.Length==count,"buffer count");
                    string prefix=CanonicalJson.Write(before["pendingPlateIds"]);var kindsById=CanonicalJson.Array(before["items"]).Select(CanonicalJson.Map).ToDictionary(i=>CanonicalJson.Int(i["itemId"]),i=>(string)i["kind"]);
                    Assert(composition.ActiveCore.ClearBuffer(BoundaryNow).Accepted,"clear");
                    var after=State();var pending=CanonicalJson.Array(after["pendingPlateIds"]);int tail=CanonicalJson.Int(pending.Last());
                    Assert(CanonicalJson.Write(pending.Take(pending.Count-1).ToArray())==prefix,"pending prefix");
                    var returned=CanonicalJson.Array(after["items"]).Select(CanonicalJson.Map).Where(i=>CanonicalJson.Int(i["plateId"])==tail).ToArray();
                    Assert(returned.Select(i=>CanonicalJson.Int(i["itemId"])).OrderBy(x=>x).SequenceEqual(ids.OrderBy(x=>x))&&returned.All(i=>kindsById[CanonicalJson.Int(i["itemId"])]==(string)i["kind"]),"identity/kind");
                    Assert(CanonicalJson.Array(after["items"]).Count==183,"total");
                    while(DailyViewMapper.PendingHead(composition.ActiveCore.Snapshot)!=0){ClearGate();SupplyOnce();}
                    var body=view.World.Bodies.Single(b=>b.data.plateId==tail.ToString());Assert(SpawnCount==51,"success sequence includes tail");
                    var rng=new System.Random(601377);float offset=0;for(int i=0;i<51;i++)offset=(float)(rng.NextDouble()*100-50);
                    Assert(Math.Abs(body.data.motion.spawnX-(150+offset))<.0001&&body.data.motion.spawnY==DailyViewMapper.SpawnY(body.data.radius),"tail motion");
                    float dt=Time.fixedDeltaTime,expected=(5/body.rigidbody.mass+9.6f)*dt/(1+body.rigidbody.linearDamping*dt);
                    Invoke(view.World,"FixedUpdate");Assert(Math.Abs(body.rigidbody.linearVelocity.y-expected)<.001f,"tail one-time entry force");
                    evidence.Add(CanonicalJson.Object("bufferCount",count,"ids",ids,"tail",tail,"successCount",SpawnCount,"spawnX",body.data.motion.spawnX,"forcePath","same newly created Rigidbody2D branch measured by V6-P01"));
                }
                return CanonicalJson.Object("fixture","real core and Composition, all gate centers controlled; no dense-packing claim","cases",evidence);
            }finally{RestoreAutomatic();}
        }
        static Task<object> ForceCheck()
        {
            GameObject node;var world=LocalWorld(out node);var samples=new List<object>();
            try
            {
                var snapshot=Fixture(FixturePlate("1",150,500,39));world.Reconcile(snapshot);var body=world.Bodies.Single();
                float dt=Time.fixedDeltaTime,m=body.rigidbody.mass,d=body.rigidbody.linearDamping,tolerance=.001f;
                float expected=(5/m+9.6f)*dt/(1+d*dt);Invoke(world,"FixedUpdate");float velocity=body.rigidbody.linearVelocity.y;
                Assert(Math.Abs(velocity-expected)<tolerance,"initial force model "+velocity+" expected "+expected);
                Assert(tolerance<5/m*dt/(1+d*dt),"tolerance below one force contribution");
                samples.Add(CanonicalJson.Object("stage","created","mass",m,"dt",dt,"damping",d,"expected",expected,"actual",velocity,"position",world.Position(body).y));
                world.Reconcile(snapshot);world.SetSimulating(false);world.SetSimulating(true);
                expected=(velocity+9.6f*dt)/(1+d*dt);Invoke(world,"FixedUpdate");velocity=body.rigidbody.linearVelocity.y;
                Assert(Math.Abs(velocity-expected)<tolerance,"reconcile/pause repeated entry force");
                samples.Add(CanonicalJson.Object("stage","reconcile-pause-resume","expected",expected,"actual",velocity));
                world.Reconcile(Fixture(FixturePlate("1",150,500,39),FixturePlate("2",300,650,39)));Invoke(world,"FixedUpdate");
                Assert(world.TryShuffle(true),"force shuffle fixture");body=world.Bodies.First();expected=9.6f*dt/(1+d*dt);Invoke(world,"FixedUpdate");
                Assert(Math.Abs(body.rigidbody.linearVelocity.y-expected)<tolerance,"shuffle repeats entry force or collision fixture");
                samples.Add(CanonicalJson.Object("stage","shuffle","expected",expected,"actual",body.rigidbody.linearVelocity.y));
                return Task.FromResult<object>(CanonicalJson.Object("fixture","isolated real PhysicsScene2D, manually stepped","tolerance",tolerance,"oneForceVelocity",5/m*dt/(1+d*dt),"samples",samples));
            }finally{UnityEngine.Object.Destroy(node);}
        }
        static Task<object> HiddenPhysicsCheck()
        {
            GameObject node;var world=LocalWorld(out node);var samples=new List<object>();
            try
            {
                world.Reconcile(Fixture(FixturePlate("1",210,203)));var body=world.Bodies.Single();
                Assert(203+63*512f/448f<292&&world.ObserveHeightGate(1).heightGateClear&&body.rim.enabled&&body.rigidbody.simulated,"203 max plate hidden yet gate clear");
                Move(world,body,new Vector2(210,139.999f));Assert(world.ObserveHeightGate(1).entryCount==1&&world.ObserveHeightGate(1).canSupply,"one hidden entry incorrectly blocks");
                world.Reconcile(Fixture(FixturePlate("1",210,139.999f),FixturePlate("2",80,139.999f),FixturePlate("3",340,139.999f)));foreach(var b in world.Bodies)Move(world,b,new Vector2(world.Position(b).x,139.999f));
                Assert(world.ObserveHeightGate(1).entryCount==3&&!world.ObserveHeightGate(1).canSupply,"three hidden entries must block");
                Move(world,world.Bodies.Last(),new Vector2(340,140));Assert(world.ObserveHeightGate(1).entryCount==2&&world.ObserveHeightGate(1).canSupply,"equality must free capacity");
                world.Clear();world.Reconcile(Fixture(FixturePlate("1",210,203)));body=world.Bodies.Single();
                for(int i=0;i<120;i++){Invoke(world,"FixedUpdate");samples.Add(CanonicalJson.Object("step",i,"y",world.Position(body).y,"velocity",body.rigidbody.linearVelocity.y));}
                Assert(world.Position(body).y-63>292,"did not re-emerge");
                world.Clear();world.Reconcile(Fixture(FixturePlate("1",200,500),FixturePlate("2",220,500)));var before=world.Bodies.Select(world.Position).ToArray();float initial=-106;var overlap=new List<object>();
                for(int i=0;i<120;i++)
                {
                    Invoke(world,"FixedUpdate");var pair=world.Bodies.ToArray();float gap=Vector2.Distance(world.Position(pair[0]),world.Position(pair[1]))-126;
                    Assert(pair.Length==2&&pair.All(b=>!float.IsNaN(world.Position(b).x)&&!float.IsInfinity(world.Position(b).y)),"invalid/lost body");
                    Assert(!(world.Position(pair[0])==before[0]&&world.Position(pair[1])==before[1]),"rollback to illegal initial state");
                    overlap.Add(CanonicalJson.Object("step",i,"gap",gap,"boundary",world.StepGeometry.Last().y));
                }
                Assert(world.StepGeometry.Last().x>initial&&world.StepGeometry.Last().x>=-.001f,"overlap did not separate within 120 steps");
                return Task.FromResult<object>(CanonicalJson.Object("fixture","controlled local physics; real colliders","hiddenSteps",samples,"overlapSteps",overlap,"transientSteps",world.TransientGeometrySteps,"rollbacks",world.ConstraintRollbacks));
            }finally{UnityEngine.Object.Destroy(node);}
        }
        static ViewSnapshot ControlledView(params ViewPlate[] plates)
        {
            var snapshot=new ViewSnapshot{sessionId=view.LastSnapshot.sessionId,revision=view.LastSnapshot.revision,eventSeq=view.LastSnapshot.eventSeq,phase=ViewPhase.Running,plates=plates,orders=view.LastSnapshot.orders,buffer=view.LastSnapshot.buffer};
            view.Apply(new ViewUpdate{snapshot=snapshot});Canvas.ForceUpdateCanvases();return snapshot;
        }
        static Vector2 ScreenPoint(Vector2 boardPoint)
        {var board=(RectTransform)Node("GameplayBoard");return RectTransformUtility.WorldToScreenPoint(null,board.TransformPoint(new Vector3(boardPoint.x,-boardPoint.y,0)));}
        static Texture2D FoodTexture(ViewItem item)=>Resources.Load<Texture2D>(view.AssetRoot+"/food/food_"+item.foodId.ToString("00"));
        static Vector2 IndependentOffset(ViewItem item,float x,float y)
        {double a=item.rotationDegrees*Math.PI/180;return new Vector2((float)(x*Math.Cos(a)-y*Math.Sin(a)),(float)(x*Math.Sin(a)+y*Math.Cos(a)));}
        static Rect ItemUv(ViewItem item)=>string.IsNullOrEmpty(item.layoutVersion)?view.VisualArt.FoodUv(item.foodId):new Rect(item.uvX,item.uvY,item.uvWidth,item.uvHeight);
        static Vector2 LowestOpaque(ViewItem item)
        {
            var tex=FoodTexture(item);var uv=ItemUv(item);Vector2 lowest=new Vector2(0,float.NegativeInfinity);
            for(int y=0;y<512;y++)for(int x=0;x<512;x++)
            {
                float u=(x+.5f)/512,v=(y+.5f)/512;
                if(tex.GetPixelBilinear(uv.x+u*uv.width,uv.y+v*uv.height).a<=.95f)continue;
                var offset=IndependentOffset(item,(u-.5f)*item.radius*2,(.5f-v)*item.radius*2);
                if(offset.y>lowest.y)lowest=offset;
            }
            Assert(!float.IsNegativeInfinity(lowest.y),"food has no opaque pixel");return lowest;
        }
        static void PlaceItem(HotpotSort.UnityPhysics.PlatePresentationWorld.PlateBody body,ViewItem item,Vector2 point)
        {Move(view.World,body,point-new Vector2(item.x,item.y));Canvas.ForceUpdateCanvases();}
        static async Task<object> InputCheck()
        {
            var evidence=new List<object>();
            try
            {
                await Fresh();var body=view.World.Bodies.First();var item=body.data.items.Last();PlaceItem(body,item,new Vector2(210,500));
                Action<string,Vector2> reject=(name,point)=>
                {
                    string hash=composition.ActiveCore.StateHash;long events=view.LastEventSequence;
                    Assert(!view.SubmitScreenTap(ScreenPoint(point)),"should reject "+name);
                    Assert(hash==composition.ActiveCore.StateHash&&events==view.LastEventSequence,"rejection mutated "+name);
                    evidence.Add(CanonicalJson.Object("case",name,"hash",hash,"eventSeq",events,"boardX",point.x,"boardY",point.y));
                };
                var tex=FoodTexture(item);Vector2 transparent=Vector2.zero;bool found=false;
                for(int y=1;y<tex.height&&!found;y+=8)for(int x=1;x<tex.width;x+=8)
                {
                    var offset=IndependentOffset(item,(x/(float)tex.width-.5f)*item.radius*2,(.5f-y/(float)tex.height)*item.radius*2);
                    var candidatePoint=view.World.ItemPosition(item.itemId)+offset;
                    if(tex.GetPixelBilinear(ItemUv(item).x+ItemUv(item).width*x/tex.width,ItemUv(item).y+ItemUv(item).height*y/tex.height).a<.01f && view.World.Hit(candidatePoint,(candidate,delta)=>(bool)Invoke(view,"OpaqueHit",candidate,delta))==null)
                    {transparent=offset;found=true;break;}
                }
                Assert(found,"transparent fixture");reject("transparent-alpha",view.World.ItemPosition(item.itemId)+transparent);
                reject("crop-above",new Vector2(210,291.99f));reject("buffer",new Vector2(210,259));reject("order",new Vector2(56,166));reject("button",new Vector2(78,865));
                string before=composition.ActiveCore.StateHash;Assert(!view.SubmitScreenTap(new Vector2(-20,-20))&&before==composition.ActiveCore.StateHash,"outside viewport");
                view.ShowNotice("QA","modal");Canvas.ForceUpdateCanvases();reject("modal",view.World.ItemPosition(item.itemId)+OpaqueOffset(item));Click("知道了");
                var pixel=LowestOpaque(item);PlaceItem(body,item,new Vector2(210,292.1f-pixel.y));var point=view.World.ItemPosition(item.itemId)+pixel;
                string hashBefore=composition.ActiveCore.StateHash;long eventBefore=view.LastEventSequence;
                Assert(point.y>=292&&point.y<292.3f&&view.SubmitScreenTap(ScreenPoint(point)),"opaque thin strip rejected at "+point);
                Assert(!view.World.Bodies.SelectMany(b=>b.data.items).Any(i=>i.itemId==item.itemId)&&composition.ActiveCore.StateHash!=hashBefore&&view.LastEventSequence>eventBefore,"wrong thin-strip item");
                evidence.Add(CanonicalJson.Object("case","opaque-strip","pointY",point.y,"itemId",item.itemId,"hashBefore",hashBefore,"hashAfter",composition.ActiveCore.StateHash,"eventsBefore",eventBefore,"eventsAfter",view.LastEventSequence));
                return CanonicalJson.Object("fixture","real core item IDs, controlled rigidbody; actual SubmitScreenTap","checks",evidence);
            }finally{RestoreAutomatic();}
        }
        static async Task<object> HintCheck()
        {
            try
            {
                await Fresh();int food=view.LastSnapshot.orders.First(o=>o.enabled&&o.foodId>=0).foodId;
                var a=FixturePlate("101",110,203,63,food);var b=FixturePlate("102",310,500,63,food);ControlledView(a,b);
                var first=view.World.Bodies.First();var second=view.World.Bodies.Last();string hash=composition.ActiveCore.StateHash;
                Assert(!view.IsItemClickable("101")&&view.FindClickableHint()=="102","hidden first target");
                var item=first.data.items[0];var low=LowestOpaque(item);var texture=FoodTexture(item);
                int bottomRow=0;while(bottomRow<texture.height&&Enumerable.Range(0,texture.width).All(x=>texture.GetPixel(x,bottomRow).a<=.1f))bottomRow++;
                Assert(bottomRow>1,"transparent lower-margin fixture");
                float exposed=Mathf.Min(.2f,bottomRow/(float)texture.height*item.radius);
                PlaceItem(first,item,new Vector2(110,292-item.radius+exposed));
                Assert(!view.IsItemClickable("101")&&view.FindClickableHint()=="102","only transparent exposed target");
                PlaceItem(first,item,new Vector2(110,292.15f-low.y));Assert(view.IsItemClickable("101")&&view.FindClickableHint()=="101","opaque thin strip hint");
                view.HighlightItem("101");Assert(Node("hint")&&Node("hint").IsChildOf(Node("PlateLayer")),"hint not clipped");
                Move(view.World,first,new Vector2(110,203));await Wait(40);Assert(!Node("hint"),"hidden hint persists");
                Move(view.World,first,new Vector2(310,500)); // Lower plate covered by later plate.
                Assert(!view.IsItemClickable("101")&&view.FindClickableHint()=="102","upper plate occlusion");
                view.ShowNotice("QA","blocking");Canvas.ForceUpdateCanvases();Assert(view.FindClickableHint()==null,"UI blocked hint");Click("知道了");
                var times=new List<double>();for(int i=0;i<5;i++){var clock=System.Diagnostics.Stopwatch.StartNew();view.FindClickableHint();clock.Stop();times.Add(clock.Elapsed.TotalMilliseconds);}
                Assert(composition.ActiveCore.StateHash==hash,"hint auto-tapped");
                return CanonicalJson.Object("fixture","real presentation with synthetic matching IDs; no core tap","transparentRows",bottomRow,"thinStripY",292.15f,"detectMilliseconds",times,"worstMilliseconds",times.Max(),"coreHashUnchanged",hash);
            }finally{await Fresh();RestoreAutomatic();}
        }
        sealed class ControlledReward : IRewardService,IProfileStore
        {
            public bool IsDevelopmentSimulation=>true;public int TotalFirstWins=>0;public int Calls,Used;public TaskCompletionSource<RewardOutcome> Pending;
            readonly HashSet<string> consumed=new HashSet<string>();
            public Task<RewardOutcome> RequestAsync(RewardRequest request){Calls++;Pending=new TaskCompletionSource<RewardOutcome>();return Pending.Task;}
            public int SharesUsed(string day)=>Used;
            public bool TryConsumeShare(string day,string id){if(Used>=3||!consumed.Add(id))return false;Used++;return true;}
            public bool RecordFirstWin(string day)=>false;public PlayerSettings LoadSettings()=>new PlayerSettings();public void SaveSettings(PlayerSettings settings){}
        }
        static async Task<object> RewardCheck()
        {
            var evidence=new List<object>();
            try
            {
                await Fresh();int food=view.LastSnapshot.orders.First(o=>o.enabled&&o.foodId>=0).foodId;
                var a=FixturePlate("101",110,203,63,food);var b=FixturePlate("102",310,203,63,food);ControlledView(a,b);
                var bodies=view.World.Bodies.ToArray();var service=new ControlledReward();var coordinator=new RewardCoordinator(service,service);long generation=10;int effects=0;
                Func<bool> has=()=>view.FindClickableHint()!=null;
                Func<bool> apply=()=>{var id=view.FindClickableHint();if(id==null)return false;view.HighlightItem(id);effects++;return true;};
                Func<RewardRequest> request=()=>new RewardRequest(generation,RewardKind.SwapOrder,RewardRoute.SimulatedShare,"20260921");
                Assert(!await coordinator.RequestAsync(request(),()=>generation,has,apply,_=>{})&&service.Calls==0,"hidden invokes service");
                foreach(var outcome in new[]{RewardOutcome.Cancelled,RewardOutcome.Failed})
                {
                    Move(view.World,bodies[0],new Vector2(110,500));var task=coordinator.RequestAsync(request(),()=>generation,has,apply,_=>{});service.Pending.TrySetResult(outcome);
                    Assert(!await task&&effects==0&&service.Used==0,"cancel/failure consumes");
                }
                var pending=coordinator.RequestAsync(request(),()=>generation,has,apply,_=>{});Move(view.World,bodies[0],new Vector2(110,203));service.Pending.TrySetResult(RewardOutcome.Success);
                Assert(!await pending&&effects==0&&service.Used==0,"target hidden before callback");
                Move(view.World,bodies[0],new Vector2(110,500));var accepted=request();pending=coordinator.RequestAsync(accepted,()=>generation,has,apply,_=>{});
                Move(view.World,bodies[0],new Vector2(110,203));Move(view.World,bodies[1],new Vector2(310,500));var callback=service.Pending;
                Assert(callback.TrySetResult(RewardOutcome.Success)&&!callback.TrySetResult(RewardOutcome.Success),"repeat callback fixture");
                Assert(await pending&&effects==1&&service.Used==1,"alternate visible target once");
                int calls=service.Calls;Assert(!await coordinator.RequestAsync(accepted,()=>generation,has,apply,_=>{})&&service.Calls==calls&&effects==1,"repeat request");
                pending=coordinator.RequestAsync(request(),()=>generation,has,apply,_=>{});generation++;service.Pending.TrySetResult(RewardOutcome.Success);
                Assert(!await pending&&effects==1&&service.Used==1,"stale generation");
                // Deliberately impossible visible packing: no shuffle action or quota.
                var huge=Enumerable.Range(0,20).Select(i=>FixturePlate((200+i).ToString(),210,203,63,food)).ToArray();ControlledView(huge);
                var positions=view.World.Bodies.ToDictionary(x=>x.data.plateId,x=>view.World.Position(x));string hash=composition.ActiveCore.StateHash;
                Assert(!view.World.TryShuffle(false)&&!view.World.TryShuffle(true),"impossible packing accepted");
                Assert(view.World.Bodies.All(x=>positions[x.data.plateId]==view.World.Position(x))&&composition.ActiveCore.StateHash==hash,"shuffle failure changed state");
                var shuffle=new RewardRequest(generation,RewardKind.Shuffle,RewardRoute.SimulatedShare,"20260921");
                calls=service.Calls;Assert(!await coordinator.RequestAsync(shuffle,()=>generation,()=>view.World.TryShuffle(false),()=>view.World.TryShuffle(true),_=>{})&&service.Calls==calls&&service.Used==1,"failed shuffle consumes reward");
                evidence.Add(CanonicalJson.Object("serviceCalls",service.Calls,"effects",effects,"quota",service.Used,"duplicateCallbackRejected",true,"staleGenerationRejected",true,"shuffleFailureNoQuota",true));
                return CanonicalJson.Object("fixture","real RewardCoordinator and real visibility predicate; controllable in-memory service/profile; lifecycle pause separately V6-L01","results",evidence);
            }finally{await Fresh();RestoreAutomatic();}
        }
        static async Task<object> LifecycleCheck()
        {
            var evidence=new List<object>();
            var oldProfile=Field<IProfileStore>(composition,"profile");Field(composition,"profile",new ControlledReward());
            try
            {
                await Fresh(2);ClearGate();composition.enabled=true;view.World.enabled=true;
                controller.Request(SessionAction.Pause);Invoke(controller,"OnLifecycle",PlatformLifecycle.Background);controller.SetRewardPaused(true);
                double time=controller.ActiveSeconds;int count=SpawnCount;var positions=view.World.Bodies.ToDictionary(b=>b.data.plateId,b=>view.World.Position(b));
                await Wait(450);Assert(controller.ActiveSeconds==time&&SpawnCount==count&&view.World.Bodies.All(b=>positions[b.data.plateId]==view.World.Position(b)),"overlapping pause unstable");
                controller.Request(SessionAction.Resume);controller.SetRewardPaused(false);await Wait(250);Assert(controller.ActiveSeconds==time&&SpawnCount==count,"background pause lost");
                Invoke(controller,"OnLifecycle",PlatformLifecycle.Foreground);await Wait(50);Assert(SpawnCount<=count+1,"resume catch-up");
                evidence.Add(CanonicalJson.Object("pauseReasons","User|Background|Reward then Background","activeSeconds",time,"count",count,"resumeCount",SpawnCount));
                foreach(string reason in new[]{"User","Background","Reward"})
                {
                    if(reason=="User")controller.Request(SessionAction.Pause);else if(reason=="Background")Invoke(controller,"OnLifecycle",PlatformLifecycle.Background);else controller.SetRewardPaused(true);
                    double stopped=controller.ActiveSeconds;int stoppedCount=SpawnCount;var stoppedPositions=view.World.Bodies.ToDictionary(b=>b.data.plateId,b=>view.World.Position(b));
                    await Wait(250);Assert(controller.ActiveSeconds==stopped&&SpawnCount==stoppedCount&&view.World.Bodies.All(b=>stoppedPositions[b.data.plateId]==view.World.Position(b)),"individual pause "+reason);
                    evidence.Add(CanonicalJson.Object("pauseReason",reason,"activeSeconds",stopped,"count",stoppedCount));
                    if(reason=="User")controller.Request(SessionAction.Resume);else if(reason=="Background")Invoke(controller,"OnLifecycle",PlatformLifecycle.Foreground);else controller.SetRewardPaused(false);
                }
                await Fresh(0);composition.ActiveCore.Timeout(600000);int terminalCount=SpawnCount;SupplyOnce();Assert(SpawnCount==terminalCount,"timeout supply");
                await Fresh(0);ulong taps=200000;
                // Real core terminal win, using controlled supply and matching-order taps.
                for(int step=0;step<500&&composition.ActiveCore.Snapshot.Status==GameStatus.Running;step++)
                {
                    var state=State();var orders=CanonicalJson.Array(state["orders"]).Select(CanonicalJson.Map).Where(o=>(string)o["state"]!="Locked").Select(o=>(string)o["kind"]).ToArray();
                    var item=CanonicalJson.Array(state["items"]).Select(CanonicalJson.Map).FirstOrDefault(i=>(string)i["location"]=="ActiveAvailable"&&orders.Contains((string)i["kind"]));
                    if(item!=null)Assert(composition.ActiveCore.Tap(new TapCommand(CanonicalJson.Int(item["itemId"]),++taps,BoundaryNow,true)).Accepted,"winning tap");
                    else {ClearGate();SupplyOnce();}
                }
                Assert(composition.ActiveCore.Snapshot.Status==GameStatus.Won,"real win fixture");terminalCount=SpawnCount;SupplyOnce();Assert(SpawnCount==terminalCount,"won supply");
                await Fresh(12);taps=300000;
                for(int i=0;i<6;i++)
                {
                    var state=State();var kinds=CanonicalJson.Array(state["orders"]).Select(CanonicalJson.Map).Where(o=>(string)o["state"]!="Locked").Select(o=>(string)o["kind"]).ToArray();
                    var item=CanonicalJson.Array(state["items"]).Select(CanonicalJson.Map).First(x=>(string)x["location"]=="ActiveAvailable"&&!kinds.Contains((string)x["kind"]));
                    composition.ActiveCore.Tap(new TapCommand(CanonicalJson.Int(item["itemId"]),++taps,BoundaryNow,true));
                }
                Assert(composition.ActiveCore.RevivalPending,"overflow pending fixture");composition.DeclineRevival();Assert(composition.ActiveCore.Snapshot.Status==GameStatus.Failed,"declined overflow fixture");terminalCount=SpawnCount;SupplyOnce();Assert(SpawnCount==terminalCount,"overflow supply");
                controller.Request(SessionAction.Exit);Invoke(composition,"Update");Assert(SpawnCount==0&&composition.ActiveCore==null&&CacheJson()=="[]","exit");
                await Fresh(0);Assert(SpawnCount==0&&CacheJson()=="[]","retry");SupplyOnce();Assert(SpawnCount==1,"retry first");
                return CanonicalJson.Object("fixture","real core terminal flows with controlled supply centers","pauses",evidence,"terminalStates",new[]{"Won","Timeout","Overflow","Exit"},"forceNotRepeated","measured V6-P01");
            }finally{Field(composition,"profile",oldProfile);Invoke(controller,"OnLifecycle",PlatformLifecycle.Foreground);controller.SetRewardPaused(false);controller.Request(SessionAction.Resume);RestoreAutomatic();}
        }
        static async Task<object> VisualCropCheck()
        {
            var evidence=new List<object>();
            try
            {
                await Fresh();int food=view.LastSnapshot.orders.First(o=>o.enabled&&o.foodId>=0).foodId;
                var plate=FixturePlate("901",210,280,63,food);plate.items[0].radius=30;ControlledView(plate);
                // Removing/re-adding creates the actual collider-free remnant path.
                ControlledView();ControlledView(plate);view.HighlightItem("901");
                Assert(view.World.Remnants.Any(),"visual remnant fixture");
                var clipped=Node("PlateLayer");Assert(clipped&&clipped.parent.GetComponent<RectMask2D>(),"missing hard clip");
                Assert(Node("hint")&&Node("hint").IsChildOf(clipped),"hint outside clip");
                foreach(var graphic in view.GetComponentsInChildren<RawImage>().Where(g=>g.name.StartsWith("Plate_")||g.name.StartsWith("Clearing_")||g.name=="hint"))
                    Assert(graphic.transform.IsChildOf(clipped),"plate graphic outside clip "+graphic.name);
                string report=SessionState.GetString(PathKey,"");
                foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1440,3200)})
                {
                    string shown=Path.ChangeExtension(report,"crop-"+size.x+"-shown.png"),hidden=Path.ChangeExtension(report,"crop-"+size.x+"-control.png");
                    Capture(size.x,size.y,shown);clipped.gameObject.SetActive(false);Capture(size.x,size.y,hidden);clipped.gameObject.SetActive(true);
                    var a=new Texture2D(2,2);var b=new Texture2D(2,2);a.LoadImage(File.ReadAllBytes(shown));b.LoadImage(File.ReadAllBytes(hidden));
                    try
                    {
                        float scale=Mathf.Min((size.x-24)/420f,(size.y-88)/900f),top=52+(size.y-88-900*scale)/2;float crop=top+292*scale;
                        var aa=a.GetPixels32();var bb=b.GetPixels32();int above=0,below=0;
                        for(int y=0;y<size.y;y++)for(int x=0;x<size.x;x++)
                        {
                            int p=y*size.x+x;int diff=Math.Abs(aa[p].r-bb[p].r)+Math.Abs(aa[p].g-bb[p].g)+Math.Abs(aa[p].b-bb[p].b);if(diff<=3)continue;
                            float screenTop=size.y-y-.5f;if(screenTop<crop-1)above++;if(screenTop>crop+1)below++;
                        }
                        Assert(above==0&&below>100,"crop pixels above="+above+" below="+below);
                        evidence.Add(CanonicalJson.Object("width",size.x,"height",size.y,"safeRect",new[]{12,36,size.x-24,size.y-88},"cropTopPixel",crop,"aboveDifferencePixels",above,"belowDifferencePixels",below,"shown",shown,"control",hidden));
                    }finally{UnityEngine.Object.Destroy(a);UnityEngine.Object.Destroy(b);}
                }
                Assert(Node("FeedbackLayer")&&!Node("FeedbackLayer").IsChildOf(clipped),"transport layer clipped");
                await Wait(250);await Until(()=>Time.unscaledDeltaTime<.05f,"capture work must finish before timed flight fixture");
                var eventSnapshot=view.LastSnapshot;var flightFeedback=view.GetComponent<GameplayFeedback>();flightFeedback.enabled=false;
                view.Apply(new ViewUpdate{snapshot=eventSnapshot,events=new[]{new ViewEvent{sessionId=view.LastSnapshot.sessionId,sessionGeneration=view.LastSnapshot.sessionGeneration,kind="ItemRoutedToOrder",itemId="901",ingredientId="food_"+food.ToString("00"),targetContainer="Order",targetSlot=0,sourceContainer="Plate",sequence=8000001,transactionId="v6-controlled-flight"}}});
                flightFeedback.Tick(.07f);var flying=Node("FlyingItem");Assert(flying&&flying.IsChildOf(Node("FeedbackLayer"))&&!flying.IsChildOf(clipped),"transport clipped/missing");
                Capture(1080,1920,Path.ChangeExtension(report,"flight.png"));
                flightFeedback.Tick(.1f);flightFeedback.Tick(.08f);Assert(Node("splash")!=null,"transport splash target");flightFeedback.enabled=true;var splash=Node("splash");Assert(((RectTransform)splash).anchoredPosition==new Vector2(56,-166),"transport missed order target");
                return CanonicalJson.Object("fixture","actual Canvas rendering; synthetic plates/remnants/hint; pair differs only plateLayer visibility","captures",evidence,"transportServing","U04 actual serving keyframes; same independent FeedbackLayer","selection","no separate selection effect exists in production; pending-click bookkeeping creates no graphic");
            }finally{await Fresh();RestoreAutomatic();}
        }
        static Vector2 OpaqueOffset(ViewItem item)
        {
            var texture=FoodTexture(item);var uv=ItemUv(item);
            for(int y=8;y<256;y+=8)for(int x=8;x<256;x+=8)
                if(texture.GetPixelBilinear(uv.x+uv.width*x/256f,uv.y+uv.height*y/256f).a>.95f)
                    return IndependentOffset(item,(x/256f-.5f)*item.radius*2,(.5f-y/256f)*item.radius*2);
            throw new InvalidOperationException("No opaque food hit pixel");
        }
        static void Capture(int width,int height,string path)
        {
            var canvas=view.GetComponentInChildren<Canvas>();var previousMode=canvas.renderMode;var previousCamera=canvas.worldCamera;
            var node=new GameObject("Task001CaptureCamera");var camera=node.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=height*.5f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var target=new RenderTexture(width,height,24);target.Create();camera.targetTexture=target;var previousTarget=RenderTexture.active;var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
            try{canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();view.SetViewport(new Rect(12,36,width-24,height-88),height);Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}
            finally{canvas.renderMode=previousMode;canvas.worldCamera=previousCamera;RenderTexture.active=previousTarget;camera.targetTexture=null;UnityEngine.Object.Destroy(node);UnityEngine.Object.Destroy(texture);target.Release();UnityEngine.Object.Destroy(target);view.SetViewport(new Rect(0,0,Screen.width,Screen.height));}
        }
    }
}
#endif
