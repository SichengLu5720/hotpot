#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Determinism;
using HotpotSort.Presentation;
using HotpotSort.Session;
using UnityEngine;
namespace HotpotSort.Bootstrap
{
    public static class Task033IntegrationProbe
    {
        static int checks;
        static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
        static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;Debug.Log("TASK033 INTEGRATION PASS "+message);}
        static async Task Until(Func<bool> predicate,string label){for(int i=0;i<600;i++){if(predicate())return;await Task.Delay(25);}throw new Exception("Timeout "+label);}
        static System.Collections.Generic.Dictionary<string,object>[] Items(DailySession s)=>CanonicalJson.Array(CanonicalJson.Map(CanonicalJson.Parse(s.Snapshot.CanonicalStateJson))["items"]).Select(CanonicalJson.Map).ToArray();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static async void PlayerEntry()
        {
            if(Application.isEditor||!Environment.GetCommandLineArgs().Contains("-task033Smoke"))return;
            int code=await Run();Application.Quit(code);
        }
        public static async Task<int> Run()
        {
            string root=Environment.GetEnvironmentVariable("HOTPOT_TASK033_INTEGRATION");Directory.CreateDirectory(root);int exit=0;
            Bootstrap boot=null;
            try
            {
                boot=UnityEngine.Object.FindFirstObjectByType<Bootstrap>();boot.enabled=false;
                var composition=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();
                var local=new LocalDevelopmentServices("Hotpot.Task033.Probe."+Guid.NewGuid().ToString("N"),"test");local.CompleteTutorial(false);local.CompleteTutorial(true);
                composition.ConfigureServices(local,local,local,local);boot.enabled=true;
                await Until(()=>boot.IsConfigured,"Boot configuration");var c=boot.Controller;var view=composition.PlayerView;
                composition.ConfigureServices(local,local,local,local);view.SetForeground(true);
                composition.SessionAction(ViewAction.StartToday);await Until(()=>composition.ActiveCore!=null,"real asset-gated entry");
                var context=c.Resolved.Context;Call(c,"Release");Call(c,"Create",context,ChallengeStage.Formal,0);
                await Until(()=>c.Stage==ChallengeStage.Formal&&composition.ActiveCore!=null,"formal real composition");
                composition.enabled=false;var core=composition.ActiveCore;ulong seq=1000;
                for(int i=0;i<25;i++)if(DailyViewMapper.PendingHead(core.Snapshot)!=0)Check(core.Supply(new SupplyObservation(++seq,0,true,true)).Accepted,"controlled production supply");
                typeof(DailyProductionComposition).GetField("supplySequence",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(composition,seq);
                var state=CanonicalJson.Map(CanonicalJson.Parse(core.Snapshot.CanonicalStateJson));var orders=CanonicalJson.Array(state["orders"]).Select(CanonicalJson.Map).ToArray();
                string original=(string)orders[0]["kind"],other=(string)orders[1]["kind"];
                var all=Items(core);var originalIds=all.Where(i=>(string)i["kind"]==original&&(string)i["location"]=="ActiveAvailable").Take(2).Select(i=>CanonicalJson.Int(i["itemId"])).ToArray();
                foreach(int id in originalIds)Check(core.Tap(new TapCommand(id,++seq,0,true)).Accepted,"two portions collected");
                var candidate=Items(core).Where(i=>(string)i["location"]=="ActiveAvailable"&&(string)i["kind"]!=original&&(string)i["kind"]!=other).GroupBy(i=>(string)i["kind"]).Where(g=>g.Count()>=3).OrderBy(g=>g.Select(i=>CanonicalJson.Int(i["plateId"])).Distinct().Count()).First();
                var candidatePlates=candidate.Select(i=>CanonicalJson.Int(i["plateId"])).Distinct().Take(4).ToArray();
                void Arrange(bool visible)
                {
                    foreach(var body in view.World.Bodies)
                    {
                        int id=int.Parse(body.data.plateId),index=Array.IndexOf(candidatePlates,id);
                        var p=visible&&index>=0?new Vector2(1.0f+(index%2)*2.0f,4.0f+(index/2)*1.7f):new Vector2(20+id,5);
                        body.rigidbody.position=p;body.rigidbody.linearVelocity=Vector2.zero;body.node.transform.position=p;
                    }
                    Physics2D.SyncTransforms();
                }
                await Task.Delay(600);Arrange(true);await Task.Delay(250);
                var stock=local.Collection.ReadCollection();stock.tools[0]=3;stock.serverRevision+=100;local.Collection.ApplyAuthoritativeSnapshot(stock);view.RefreshCollectionToolStock();
                c.StartChallengeTimer();
                Debug.Log("TASK033 DEBUG phase="+view.LastSnapshot.phase+" pauses="+c.Pauses+" input="+c.CanAcceptInput+" candidate="+candidate.Key+" plates="+string.Join(",",candidatePlates)+" observed="+JsonUtility.ToJson(view.CaptureClickability())+" slots="+string.Join(",",composition.ReadSwapOrderOffer().legalSlots));
                foreach(var body in view.World.Bodies.Where(b=>candidatePlates.Contains(int.Parse(b.data.plateId))))Debug.Log("TASK033 BODY "+body.data.plateId+" "+view.World.Position(body)+" ids="+string.Join(",",body.data.items.Select(i=>i.itemId)));
                await Until(()=>composition.ReadSwapOrderOffer().legalSlots.Contains(0),"real geometric eligible order");
                int beforeStock=local.Collection.ReadCollection().tools[0];
                Check(composition.BeginSwapOrderSelection(),"real selection begins");
                composition.CancelSwapOrderSelection();Check(local.Collection.ReadCollection().tools[0]==beforeStock&&!composition.ReadSwapOrderOffer().selecting,"cancel no charge");
                Check(composition.BeginSwapOrderSelection(),"selection reopens");Arrange(false);await Task.Delay(30);
                Check(await composition.SelectSwapOrderAsync(0,RewardRoute.SimulatedAd)==RewardApplicationResult.Unavailable&&local.Collection.ReadCollection().tools[0]==beforeStock,"geometry invalidation no charge");
                composition.CancelSwapOrderSelection();Check(!composition.BeginSwapOrderSelection(),"no targets cannot enter");
                Arrange(true);await Until(()=>composition.ReadSwapOrderOffer().legalSlots.Contains(0),"target reacquired");
                Call(view,"BeginSwapPresentation");Check(composition.ReadSwapOrderOffer().selecting,"visual binds real selection");
                composition.enabled=true;double selectionTime=c.ChallengeSeconds;int spawned=view.World.Bodies.Count();
                var tracked=view.World.Bodies.First(b=>candidatePlates.Contains(int.Parse(b.data.plateId)));var pos=tracked.rigidbody.position;
                await Task.Delay(230);
                Check(c.Pauses==PauseReasons.None&&c.ChallengeSeconds>selectionTime+.1,"selection timer continues");
                Check(tracked.rigidbody.position!=pos,"selection physics continues");
                Check(view.World.Bodies.Count()>spawned,"selection supply continues");
                // Reposition only controlled test geometry immediately before the real UI selection.
                Arrange(true);await Task.Delay(30);Check(composition.ReadSwapOrderOffer().legalSlots.Contains(0),"selection target still certified");
                double returnTime=c.ChallengeSeconds;int returnSpawned=view.World.Bodies.Count();pos=tracked.rigidbody.position;Call(view,"ChooseSwapTarget",0);
                Check(core.SwapOrderPending&&core.ReadSwapOrderTransfer().items.Length==2,"real two-portion transfer begins");
                await Task.Delay(100);
                Check(core.SwapOrderPending&&c.Pauses==PauseReasons.None&&c.ChallengeSeconds>returnTime+.04,"return timer continues before 0.34s");
                Check(tracked.rigidbody.position!=pos,"return physics continues");
                Arrange(true);bool suppliedDuringReturn=false;
                for(int i=0;i<25&&core.SwapOrderPending;i++){if(view.World.Bodies.Count()>returnSpawned)suppliedDuringReturn=true;await Task.Delay(10);}
                Check(suppliedDuringReturn,"supply commits during return when gate clear");
                await Until(()=>!core.SwapOrderPending,"visual transfer callback");
                Check(local.Collection.ReadCollection().tools[0]==beforeStock-1,"one successful inventory debit");
                Check(Items(core).Length==183&&core.Snapshot.Status==GameStatus.Running,"successful swap preserves 183 items");
                Check(!composition.ReadSwapOrderOffer().selecting&&c.CanAcceptInput,"selection input gate released");
                await Task.Delay(200);
            }
            catch(Exception ex){exit=1;Debug.LogException(ex);}
            finally
            {
                File.WriteAllText(Path.Combine(root,Application.isEditor?"editor-result.json":"player-result.json"),"{\"exitCode\":"+exit+",\"checks\":"+checks+"}");
                if(boot){var view=UnityEngine.Object.FindFirstObjectByType<GameplayView>();if(view)view.World.Clear();boot.Controller?.Exit();}
            }
            return exit;
        }
    }
}
#endif
