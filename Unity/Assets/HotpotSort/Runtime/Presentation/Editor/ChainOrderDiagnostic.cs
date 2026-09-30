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
        sealed class InteractivePort:IPresentationPort
        {
            public ViewSnapshot state;public int taps;
            public InteractivePort(ViewSnapshot value){state=value;}
            public event Action<ViewUpdate> Updated;
            public ViewSnapshot Read()=>state;
            public void Emit(ViewEvent[] events){state.revision++;Updated?.Invoke(new ViewUpdate{snapshot=state,events=events});}
            public void SessionAction(ViewAction action){}
            public void ObserveSupply(ViewSupplyObservation observation){}
            public void Tap(ViewTap tap){taps++;state.orders[1].count=taps;Emit(Array.Empty<ViewEvent>());}
        }
        static void Execute()
        {
            int exit=0;GameObject owner=null;
            try
            {
                owner=new GameObject("ChainOrderFixture");var view=owner.AddComponent<GameplayView>();view.ConfigureAssets(V7Art.Root);view.SetForeground(true);
                var f=view.GetComponent<GameplayFeedback>();f.enabled=false;
                var s=new ViewSnapshot{sessionId="chain",sessionGeneration=1,phase=ViewPhase.Running,orders=Enumerable.Range(0,4).Select(i=>new ViewOrder{slot=i,enabled=i<2,foodId=0,count=i==0?2:0,required=3}).ToArray(),buffer=new ViewItem[5]};
                s.plates=Capture.Fixture().plates;var port=new InteractivePort(s);view.Bind(port);view.World.SetSimulating(false);
                string output=System.IO.Path.GetFullPath("../.harness/qa/TASK-049/copper-order-r004");System.IO.Directory.CreateDirectory(output);
                foreach(int height in new[]{1920,1440})Capture.Capture(view,1080,height,output+"/normal-1080x"+height+".png");
                var potNodes=view.GetComponentsInChildren<RectTransform>().Where(n=>n.name=="PotBody").ToArray();
                var potPositions=potNodes.Select(n=>n.anchoredPosition).ToArray();var potSizes=potNodes.Select(n=>n.sizeDelta).ToArray();
                Check(view.GetComponentsInChildren<UnityEngine.UI.RawImage>().Count(n=>n.name=="OrderPaper"&&n.texture!=null)==2,"cleaned paper signs are bound");
                long seq=0;
                Func<string,int,ViewEvent> evt=(kind,food)=>new ViewEvent{sessionId=s.sessionId,sessionGeneration=s.sessionGeneration,sequence=++seq,kind=kind,slot=0,ingredientId="food_"+food.ToString("00"),targetSlot=0,targetContainer="Order",sourceContainer=kind=="BufferAutoAbsorbed"?"Buffer":"Plate",sourceSlot=0,itemId="route-"+seq,filledAfter=3};
                var route=evt("ItemRoutedToOrder",0);var a=evt("OrderCompleted",0);var b=evt("OrderCreated",1);
                var r1=evt("BufferAutoAbsorbed",1);r1.filledAfter=1;var r2=evt("BufferAutoAbsorbed",1);r2.filledAfter=2;var r3=evt("BufferAutoAbsorbed",1);
                var doneB=evt("OrderCompleted",1);var c=evt("OrderCreated",2);
                // New snapshot instance is required: previous must retain order A.
                var final=JsonUtility.FromJson<ViewSnapshot>(JsonUtility.ToJson(s));final.buffer=new ViewItem[5];final.orders[0].foodId=2;final.orders[0].count=0;port.state=final;
                // A previous plate-to-buffer flight for a now-consumed item must disappear too.
                var old=new ViewEvent{sessionId=s.sessionId,sessionGeneration=s.sessionGeneration,sequence=0,kind="ItemRoutedToBuffer",itemId=r1.itemId,ingredientId="food_01",targetContainer="Buffer",targetSlot=0,sourceContainer="Plate"};
                // Emit with sequence 1, then shift the complete transaction forward.
                old.sequence=1;foreach(var e in new[]{route,a,b,r1,r2,r3,doneB,c})e.sequence++;seq++;
                port.Emit(new[]{old});Check(f.ActiveEffectCount>0,"prior buffer flight started");
                var events=new[]{route,a,b,r1,r2,r3,doneB,c};var rngBefore=JsonUtility.ToJson(UnityEngine.Random.state);port.Emit(events);
                Check(rngBefore==JsonUtility.ToJson(UnityEngine.Random.state),"paper feedback does not consume Unity random stream");
                Check(view.GetComponentsInChildren<RectTransform>().Where(n=>n.name=="CopperClip").All(n=>n.anchoredPosition==new Vector2(38,-32)&&n.localScale==Vector3.one),"clip mounting transforms remain fixed");
                Check(view.GetComponentsInChildren<RectTransform>().First(n=>n.name=="PaperOrderSign_0").anchoredPosition.y==28,"new paper immediately begins drop into clip");
                Check(f.DisplayOrder(0,final.orders[0])==final.orders[0]&&f.ActiveServeCount==1,"same-slot chain immediately shows final C with one merged departing sign");
                Check(!view.GetComponentsInChildren<UnityEngine.UI.RawImage>().Any(n=>n.name=="FlyingItem"),"consumed intermediate food never replays");
                var completed=view.GetComponentsInChildren<RectTransform>().Single(n=>n.name=="DepartingOrderSign_0");
                Check(completed.Find("Food_1")&&completed.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text=="3/3")&&!completed.Find("BlackScratch"),"outgoing copy keeps completed food 1 and 3/3 without a completion mark rather than new food 2");
                Check(!view.GetComponentsInChildren<RectTransform>().Where(n=>n.name.StartsWith("PaperOrderSign_")).Any(n=>n.Find("BlackScratch")),"live new orders have no completion scratch");
                port.Emit(events);Check(f.ActiveServeCount==1&&f.PendingServeCount(0)==0,"duplicate batch ignored without queue");
                foreach(int height in new[]{1920,1440})Capture.Capture(view,1080,height,output+"/chain-immediate-1080x"+height+".png");
                Tick(f,.12f);
                var cross=evt("OrderCompleted",0);cross.slot=1;port.Emit(new[]{cross});Check(f.ActiveServeCount==2,"another pot completes during active animation");
                var again=evt("OrderCompleted",2);final=JsonUtility.FromJson<ViewSnapshot>(JsonUtility.ToJson(final));final.buffer=new ViewItem[5];final.orders[0].foodId=3;final.orders[0].count=0;port.state=final;port.Emit(new[]{again});Check(f.ActiveServeCount==2,"same pot completes again without waiting");
                view.SetViewport(new Rect(0,0,Screen.width,Screen.height),Screen.height);Canvas.ForceUpdateCanvases();
                var board=view.GetComponentsInChildren<RectTransform>().First(n=>n.name=="GameplayBoard");
                foreach(var plate in final.plates.Take(2))
                {
                    var id=plate.items[0].itemId;var at=view.World.ItemPosition(id);Canvas.ForceUpdateCanvases();
                    var screen=RectTransformUtility.WorldToScreenPoint(null,board.TransformPoint(new Vector3(at.x,-at.y,0)));
                    Check(view.SubmitScreenTap(screen),"screen tap accepted during concurrent completion "+id);
                }
                Check(view.GetComponentsInChildren<UnityEngine.UI.RawImage>().Where(n=>n.name=="CopperClip").All(n=>n.uvRect==new Rect(338f/1254,228f/1254,656f/1254,628f/1254)),"rebuilt clips retain cropped UV after live count updates");
                Check(port.taps==2,"two live screen taps reach presentation port during animation");
                final.orders[0].count=1;var fresh=evt("ItemRoutedToOrder",2);fresh.itemId=final.plates[0].items[0].itemId;fresh.ingredientId="food_00";fresh.filledAfter=1;
                final=JsonUtility.FromJson<ViewSnapshot>(JsonUtility.ToJson(final));final.buffer=new ViewItem[5];final.buffer=new ViewItem[5];
                final.plates[0].items=final.plates[0].items.Where(item=>item.itemId!=fresh.itemId).ToArray();port.state=final;port.Emit(new[]{fresh});
                Check(f.DisplayOrder(0,final.orders[0]).count==1,"new matching-food order is immediately authoritative");
                var nodes=view.GetComponentsInChildren<RectTransform>().Where(n=>n.name.StartsWith("DepartingOrderSign_")).ToArray();var positions=nodes.Select(n=>n.anchoredPosition).ToArray();var scales=nodes.Select(n=>n.localScale).ToArray();var alphas=nodes.Select(n=>n.GetComponent<CanvasGroup>().alpha).ToArray();
                foreach(var pause in new[]{ViewPauseReasons.User,ViewPauseReasons.Background,ViewPauseReasons.Reward})
                {final.pauseReasons=pause;port.Emit(Array.Empty<ViewEvent>());Tick(f,1);Check(nodes.Select(n=>n.anchoredPosition).SequenceEqual(positions)&&nodes.Select(n=>n.localScale).SequenceEqual(scales)&&nodes.Select(n=>n.GetComponent<CanvasGroup>().alpha).SequenceEqual(alphas),"pause freezes concurrent feedback "+pause);}
                final.pauseReasons=ViewPauseReasons.None;port.Emit(Array.Empty<ViewEvent>());
                Tick(f,.12f);
                foreach(int height in new[]{1920,1440})Capture.Capture(view,1080,height,output+"/lift-1080x"+height+".png");
                Check(!view.GetComponentsInChildren<RectTransform>().Any(n=>n.name.StartsWith("ServingWholePot_")),"no small or departing pot is created");
                Check(view.GetComponentsInChildren<RectTransform>().Where(n=>n.name=="PotBody").All(n=>n.localScale==Vector3.one&&n.sizeDelta==new Vector2(128,128)&&n.anchoredPosition==new Vector2(-14,-30)),"all pots keep original size and location");
                Check(nodes.SelectMany(n=>n.GetComponentsInChildren<UnityEngine.UI.Graphic>()).All(g=>!g.raycastTarget),"checks never intercept input");
                Check(nodes.All(n=>n.localScale==Vector3.one&&n.GetComponent<CanvasGroup>().alpha<1),"paper departure preserves scale and fades");
                var clip=view.GetComponentsInChildren<RectTransform>().Single(n=>n.name=="OrderDepartureClip");
                Check(clip.GetComponent<UnityEngine.UI.RectMask2D>()&&clip.anchoredPosition.y==-54&&clip.GetSiblingIndex()<board.Find("FixedHUD").GetSiblingIndex()&&clip.GetSiblingIndex()<board.Find("Order_0").GetSiblingIndex(),"departure clips below top controls behind live orders");
                Tick(f,.18f);
                foreach(int height in new[]{1920,1440})Capture.Capture(view,1080,height,output+"/leaving-1080x"+height+".png");
                Check(nodes.All(n=>n.localScale==Vector3.one&&n.GetComponent<CanvasGroup>().alpha<1)&&nodes.Select((n,i)=>n.anchoredPosition.y>positions[i].y&&n.anchoredPosition.x<positions[i].x).All(v=>v),"exit follows short upper-left arc and fades");
                final.orders[0].count=0;port.Emit(Array.Empty<ViewEvent>());Tick(f,.18f);Check(final.orders[0].count==0&&f.DisplayOrder(0,final.orders[0]).count==0,"old arrival never overwrites current count");
                foreach(int height in new[]{1920,1440})Capture.Capture(view,1080,height,output+"/departed-1080x"+height+".png");
                Tick(f,.5f);Check(view.GetComponentsInChildren<RectTransform>().Where(n=>n.name.StartsWith("PaperOrderSign_")).All(n=>n.anchoredPosition==Vector2.zero),"new paper settles fully into fixed clip");Check(f.ActiveServeCount==0&&f.PendingDeferredRouteCount(0)==0,"all concurrent feedback drains promptly");
                Capture.Capture(view,1080,1920,output+"/settled-1080x1920.png");
                // Preview transactions publish a fresh authoritative snapshot together with
                // the events that produced it, matching DailyViewMapper/Composition semantics.
                // A=food_03, B=food_04, C=food_05: no completion event reuses the current target.
                final=JsonUtility.FromJson<ViewSnapshot>(JsonUtility.ToJson(final));final.buffer=new ViewItem[5];
                final.orders[0].foodId=3;final.orders[0].count=2;port.state=final;port.Emit(Array.Empty<ViewEvent>());
                string frames=output+"/identity-frames";System.IO.Directory.CreateDirectory(frames);int frame=0;
                Action captureFrame=()=>Capture.Capture(view,720,1280,frames+"/frame-"+(frame++).ToString("D3")+".png");
                Action<int,int> checkIdentity=(oldFood,newFood)=>
                {
                    var outgoing=view.GetComponentsInChildren<RectTransform>().Single(n=>n.name=="DepartingOrderSign_0");
                    var live=view.GetComponentsInChildren<RectTransform>().Single(n=>n.name=="PaperOrderSign_0");
                    Check(outgoing.Find("Food_"+oldFood)&&!outgoing.Find("BlackScratch")&&outgoing.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text=="3/3"),"outgoing identity is completed food "+oldFood);
                    Check(live.Find("Food_"+newFood)&&!live.Find("BlackScratch")&&live!=outgoing,"independent live identity remains new food "+newFood);
                };
                for(int i=0;i<10;i++)captureFrame();
                foreach(int nextFood in new[]{4,5})
                {
                    int oldFood=final.orders[0].foodId;
                    var previous=final;
                    final=JsonUtility.FromJson<ViewSnapshot>(JsonUtility.ToJson(final));final.buffer=new ViewItem[5];final.orders[0].foodId=nextFood;final.orders[0].count=0;
                    port.state=final;port.Emit(new[]{evt("OrderCompleted",oldFood),evt("OrderCreated",nextFood)});
                    Check(previous.orders[0].foodId==oldFood&&final.orders[0].foodId==nextFood,"atomic old-to-new snapshot identity "+oldFood+" -> "+nextFood);
                    checkIdentity(oldFood,nextFood);
                    Debug.Log("IDENTITY_TRACE completed=food_"+oldFood.ToString("00")+" count=3 current=food_"+nextFood.ToString("00")+" count=0");
                    for(int i=0;i<16;i++){captureFrame();Tick(f,.04f);}
                    Check(f.ActiveServeCount==0&&view.GetComponentsInChildren<RectTransform>().Single(n=>n.name=="PaperOrderSign_0").Find("Food_"+nextFood),"new food "+nextFood+" remains after old paper disappears");
                    for(int i=0;i<10;i++)captureFrame();
                    if(nextFood==4)
                    {
                        for(int count=1;count<=2;count++)
                        {
                            final=JsonUtility.FromJson<ViewSnapshot>(JsonUtility.ToJson(final));final.buffer=new ViewItem[5];final.orders[0].count=count;port.state=final;port.Emit(Array.Empty<ViewEvent>());
                            for(int i=0;i<7;i++)captureFrame();
                        }
                    }
                }
                System.IO.File.WriteAllText(output+"/identity-trace.txt","Preview is an atomic presentation fixture, not a live core recording.\nA food_03 2/3 -> completed A 3/3 plus new B food_04 0/3 -> B remains -> B 1/3 -> B 2/3 -> completed B 3/3 plus new C food_05 0/3 -> C remains.\nReal source audit: DailySession.Settle emits before clearing Kind; Emit enriches ingredientId from old slot; DailyViewMapper.Map copies ingredientId per event; GameplayFeedback.StartServing uses event ingredientId.\nSame-batch A/B/C coalescing is covered earlier in chain checks: outgoing B and live C.\n");
                final=JsonUtility.FromJson<ViewSnapshot>(JsonUtility.ToJson(final));final.buffer=new ViewItem[5];final.orders[0].foodId=6;port.state=final;port.Emit(new[]{evt("OrderCompleted",5),evt("OrderCreated",6)});
                final.sessionGeneration=2;port.Emit(Array.Empty<ViewEvent>());Check(f.ActiveServeCount==0&&f.ActiveEffectCount==0,"new session cancels old feedback");
                Debug.Log("CHAIN_ORDER_ALL_PASS");
            }
            catch(Exception e){exit=1;Debug.LogException(e);}
            finally{if(owner){owner.GetComponent<GameplayView>().World.Clear();UnityEngine.Object.Destroy(owner);}SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
        }
    }
}
#endif
