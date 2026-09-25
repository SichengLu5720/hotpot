using System;
using System.Collections.Generic;
using System.Linq;
using HotpotSort.Contracts;
using HotpotSort.Contracts.RemoteAssets;
using UnityEngine;
using UnityEngine.UI;

namespace HotpotSort.Presentation
{
    // Presentation-only clocks, bounded reusable sprite pool and authoritative event cursor.
    public sealed class GameplayFeedback : MonoBehaviour
    {
        sealed class Effect
        {
            public RectTransform node;public RawImage image;
            public Vector2 from,to;public float age,duration,size,arc,opacity;
            public int mode;public string itemId;public Action completed;public Func<Vector2> follow;
        }
        sealed class Serve
        {
            public RectTransform node;public float age,finishAt;public int slot;public bool exited,routesFlushed,departed;public Vector2 origin;public ServeBatch batch;
        }
        sealed class Arrival
        {
            public float age,duration;public long epoch;public ViewEvent route;
        }
        sealed class ServeBatch
        {
            public ViewEvent completion;public ViewOrder replacement;public bool flushed;public readonly Queue<ViewEvent> routes=new Queue<ViewEvent>();
        }
        sealed class ClearItem
        {
            public RectTransform node;public RawImage image;public int sourceSlot;public Vector2 offset;
        }
        sealed class ClearTransport
        {
            public RectTransform node;public RawImage plate;public ClearItem[] items;public float age;public bool departureAccent;
        }
        const int EffectLimit=64;
        const int ClearTransportLimit=4,BufferSlotLimit=5;
        const float FlightSeconds=.34f,BufferOrderFlightSeconds=.46f;
        const float ClearDepartAt=.60f,ClearDepartSeconds=.36f;
        readonly List<Effect> active=new List<Effect>(),pool=new List<Effect>();
        readonly List<ClearTransport> clears=new List<ClearTransport>();
        readonly List<Serve> serves=new List<Serve>();
        readonly List<Arrival> arrivals=new List<Arrival>();
        readonly Queue<ServeBatch>[] queues={new Queue<ServeBatch>(),new Queue<ServeBatch>(),new Queue<ServeBatch>(),new Queue<ServeBatch>()};
        readonly ServeBatch[] latestBatches=new ServeBatch[4];
        readonly ViewOrder[] visualOrders=new ViewOrder[4];
        public ViewOrder DisplayOrder(int slot,ViewOrder authoritative)=>slot>=0&&slot<4?visualOrders[slot]??authoritative:authoritative;
        static ViewOrder CopyOrder(ViewOrder order)=>order==null?null:new ViewOrder{slot=order.slot,foodId=order.foodId,count=order.count,required=order.required,enabled=order.enabled};
        void HoldOrder(int slot,ViewSnapshot previous)
        {
            if(slot>=0&&slot<4&&visualOrders[slot]==null)visualOrders[slot]=CopyOrder(previous?.orders?.FirstOrDefault(o=>o.slot==slot));
        }
        readonly Dictionary<long,long> routeHapticEpochs=new Dictionary<long,long>();
        readonly Dictionary<long,Vector2> routeSources=new Dictionary<long,Vector2>();
        readonly VisualClock clock=new VisualClock(),revivalClock=new VisualClock();
        readonly PresentationEventCursor cursor=new PresentationEventCursor();
        readonly PotAmbientSchedule ambient=new PotAmbientSchedule();
        readonly PresentationEffects fallbackEffects=new PresentationEffects();
        PresentationEffects Settings=>view?.VisualArt?.Theme.effects??fallbackEffects;
        // Integrator applies replacement positioning; called only after outgoing visuals are hidden.
        public Action<int,float> ReplacementSettling;
        public bool IsServing(int slot)=>serves.Any(s=>s.slot==slot);
        public int PendingServeCount(int slot)=>slot>=0&&slot<4?queues[slot].Count:0;
        public int PendingDeferredRouteCount(int slot)
        {
            if(slot<0||slot>=4)return 0;
            int count=serves.Where(s=>s.slot==slot&&s.batch!=null).Sum(s=>s.batch.routes.Count);
            return count+queues[slot].Sum(batch=>batch.routes.Count);
        }
        readonly Dictionary<string,Texture2D> textureCache=new Dictionary<string,Texture2D>();
        RectTransform layer,plateEffects;
        Texture2D[] foods;
        string root,session;long generation;float impulseAge=1;
        public Vector2 ScreenImpulse=>impulseAge<.1f?new Vector2(Mathf.Sin(impulseAge*125)*1.1f*(1-impulseAge/.1f),0):Vector2.zero;
        ViewSnapshot snapshot;
        GameplayView view;
        RevivalTransferBatch transfer;
        RectTransform transport;
        RawImage transportPlate;
        RectTransform[] transferFoods;
        float transferAge;
        bool completedTransfer;
        string finishedTransferToken;
        public int ActiveEffectCount=>active.Count;
        public int PoolSize=>pool.Count+active.Count;
        public float RevivalElapsed=>transferAge;
        public bool RevivalActive=>transfer!=null;
        public bool WarmupTransitionBusy=>arrivals.Count>0||serves.Count>0||queues.Any(q=>q.Count>0)||clears.Count>0||active.Any(f=>f.mode==1)||transfer!=null;
        public int ClearTransportCount=>clears.Count;
        public GameplayAudio Audio {get;private set;}
        public void Initialize(Transform parent,Canvas canvas){view=GetComponent<GameplayView>();Audio=gameObject.AddComponent<GameplayAudio>();}
        public void ConfigureAssets(string path,Texture2D[] textures){root=path;foods=textures;textureCache.Clear();}
        // Asset selection is separate from the stable runtime binding interface.
        public void ConfigureAudio(string approvedAudioRoot){}
        public void SetAudioSettings(PlayerSettings settings){Audio?.SetSettings(settings);}
        public void Button(){}
        public void SetBoard(RectTransform board,RectTransform clippedPlates=null)
        {
            plateEffects=clippedPlates;
            if(!layer){var go=new GameObject("FeedbackLayer",typeof(RectTransform));layer=(RectTransform)go.transform;}
            layer.SetParent(board,false);layer.anchorMin=layer.anchorMax=layer.pivot=new Vector2(0,1);layer.anchoredPosition=Vector2.zero;layer.sizeDelta=new Vector2(420,900);layer.SetAsLastSibling();
        }
        public void ResetFeedback()
        {
            Audio?.ResetTransient();
            clock.Cancel();revivalClock.Cancel();cursor.Cancel();
            foreach(var fx in active)if(fx.node)Destroy(fx.node.gameObject);
            foreach(var fx in pool)if(fx.node)Destroy(fx.node.gameObject);
            active.Clear();pool.Clear();
            routeHapticEpochs.Clear();routeSources.Clear();arrivals.Clear();
            Array.Clear(visualOrders,0,4);
            foreach(var serve in serves){if(serve.node){serve.node.gameObject.SetActive(false);Destroy(serve.node.gameObject);}ReplacementSettling?.Invoke(serve.slot,1);view?.SetOrderServing(serve.slot,false);}serves.Clear();
            foreach(var q in queues)q.Clear();Array.Clear(latestBatches,0,latestBatches.Length);
            ambient.Reset();CancelClears();CancelTransfer();snapshot=null;session=null;generation=0;impulseAge=1;finishedTransferToken=null;
        }
        void CancelTransfer(){if(transport)Destroy(transport.gameObject);transport=null;transfer=null;transferFoods=null;transferAge=0;completedTransfer=false;revivalClock.Cancel();}
        public void Apply(ViewUpdate update,ViewSnapshot previous,Func<string,Vector2> position)
        {
            var state=update.snapshot;
            if(session!=state.sessionId||generation!=state.sessionGeneration)
            {
                ResetFeedback();session=state.sessionId;generation=state.sessionGeneration;
                clock.Reset(session,generation);cursor.Reset(session,generation);
            }
            snapshot=state;
            if(transfer!=null&&(!state.revivalPending||state.revivalTransfer==null||!transfer.token.Matches(state.revivalTransfer.token)))CancelTransfer();
            // Revival emits the same queue-return event before its own exact-token transfer.
            bool revivalReturn=state.revivalPending||(update.events??Array.Empty<ViewEvent>()).Any(e=>e!=null&&e.kind=="RevivalTransferStarted"&&e.sessionId==session&&e.sessionGeneration==generation);
            AudioCue? terminalCue=null;
            foreach(var evt in update.events??Array.Empty<ViewEvent>())
            {
                if(!cursor.TryAccept(evt))continue;
                switch(evt.kind)
                {
                    case "ItemRoutedToOrder":case "ItemRoutedToBuffer":case "BufferAutoAbsorbed":
                        if(evt.targetContainer=="Order")HoldOrder(evt.targetSlot,previous);
                        routeHapticEpochs[evt.sequence]=view.CanPlayHaptic?view.HapticEpoch:-1;
                        if(evt.targetContainer=="Order"&&evt.targetSlot>=0&&evt.targetSlot<4&&latestBatches[evt.targetSlot]!=null&&!latestBatches[evt.targetSlot].flushed)
                        {
                            if(evt.sourceContainer!="Buffer"&&position!=null)routeSources[evt.sequence]=position(evt.itemId);
                            latestBatches[evt.targetSlot].routes.Enqueue(evt);
                        }
                        else StartRouteFlight(evt,position);
                        break;
                    case "BufferReturnedToQueue":if(!revivalReturn)BeginClear(previous);break;
                    case "OrderCompleted":
                        if(evt.slot>=0&&evt.slot<4)
                        {
                            HoldOrder(evt.slot,previous);
                            var batch=new ServeBatch{completion=evt};latestBatches[evt.slot]=batch;
                            queues[evt.slot].Enqueue(batch);StartServing(evt.slot);
                        }
                        break;
                    case "OrderCreated":
                        if(evt.slot>=0&&evt.slot<4&&latestBatches[evt.slot]!=null)
                            latestBatches[evt.slot].replacement=new ViewOrder{slot=evt.slot,foodId=FoodId(evt.ingredientId),count=0,required=3,enabled=true};
                        break;
                    case "PotUnlocked":
                        var unlocked=Pot(evt.slot);Pulse(unlocked,"ignition",.32f,96);
                        Spawn("UnlockSteam",Load(SteamKey(evt.slot)),unlocked+new Vector2(0,-10),unlocked+new Vector2(8,-48),80,.82f,2,0,.28f);
                        break;
                    case "ChallengeWon":if(!state.isWarmup){terminalCue=AudioCue.Win;Pulse(new Vector2(210,350),"victory_accent",.7f,360);}break;
                    case "ChallengeFailed":terminalCue=AudioCue.Lose;break;
                    case "RevivalTransferStarted":BeginTransfer(evt.revivalTransfer);break;
                }
            }
            if(terminalCue.HasValue)
                Audio?.BeginTerminal(session,generation,
                    arrivals.Any(a=>a.route.targetContainer=="Order")||queues.Any(q=>q.Any(b=>b.routes.Count>0)),
                    queues.Any(q=>q.Count>0),
                    queues.Any(q=>q.Count>0)||serves.Any(s=>!s.departed),terminalCue.Value);
            // Snapshot convergence also starts the exact pending token after a view remount.
            if(state.revivalPending&&state.revivalUsed&&state.revivalTransfer!=null&&transfer==null&&state.revivalTransfer.token.completionToken!=finishedTransferToken)BeginTransfer(state.revivalTransfer);
            if(state.phase==ViewPhase.Entry||state.phase==ViewPhase.Aborted||state.phase==ViewPhase.Won||state.phase==ViewPhase.Overflow)
            {
                for(int i=active.Count-1;i>=0;i--)if(state.phase!=ViewPhase.Won||!active[i].node||active[i].node.name!="victory_accent")Release(i);
                foreach(var serve in serves){if(serve.node){serve.node.gameObject.SetActive(false);Destroy(serve.node.gameObject);}ReplacementSettling?.Invoke(serve.slot,1);view.SetOrderServing(serve.slot,false);}serves.Clear();foreach(var q in queues)q.Clear();Array.Clear(latestBatches,0,latestBatches.Length);
                CancelClears();
                arrivals.Clear();Array.Clear(visualOrders,0,4);
            }
            if(state.phase==ViewPhase.Won)CancelClears();
            view.RefreshOrderPresentation();
        }
        void Update(){Tick(Time.unscaledDeltaTime);}
        // Deterministic visual tick also used by Editor-only capture, with no core state mutation.
        public void Tick(float delta)
        {
            if(!view||!view.PresentationForeground||snapshot==null||!layer)return;
            float dt=(float)clock.Advance(Mathf.Min(delta,.1f),snapshot);
            if(snapshot.pauseReasons==ViewPauseReasons.Tutorial)dt=Mathf.Min(delta,.1f);
            // Preserve the existing one-shot victory accent; terminal ambient/serve effects were cleared in Apply.
            if(snapshot.phase==ViewPhase.Won&&snapshot.pauseReasons==ViewPauseReasons.None)dt=Mathf.Min(delta,.1f);
            if(dt>0)
            {
                impulseAge+=dt;
                for(int i=active.Count-1;i>=0;i--)
                {
                    var fx=active[i];if(!fx.node){active.RemoveAt(i);continue;}fx.age+=dt;float t=Mathf.Clamp01(fx.age/fx.duration);
                    if(fx.follow!=null){var point=fx.follow();if(point==Vector2.zero){Release(i);continue;}At(fx.node,point);}
                    else
                    {
                        if(fx.mode==1)At(fx.node,FlightPoint(fx.from,fx.to,t,fx.arc));
                        else{var point=Vector2.Lerp(fx.from,fx.to,t);point.y-=Mathf.Sin(t*Mathf.PI)*fx.arc;At(fx.node,point);}
                    }
                    if(fx.mode==0){fx.image.color=new Color(1,1,1,Mathf.Sin(Mathf.PI*t)*fx.opacity);fx.node.localScale=Vector3.one*(.85f+t*.28f);}
                    else if(fx.mode==1)
                    {
                        // The routed item itself supplies the immediate lift accent before its path settles.
                        float lift=Mathf.Sin(Mathf.Clamp01(t/.24f)*Mathf.PI);
                        fx.node.localScale=Vector3.one*(1+lift*.14f);
                    }
                    else if(fx.mode==3)
                    {
                        float envelope=Mathf.Sin(Mathf.PI*t);
                        fx.image.color=new Color(1,1,1,fx.opacity*envelope*envelope);
                        fx.node.localScale=Vector3.one*Mathf.Lerp(.92f,1.12f,t);
                    }
                    else if(fx.mode==2)
                    {
                        // Steam uses a broad cross-fade envelope instead of the short pulse curve.
                        float fadeIn=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.30f));
                        float fadeOut=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.62f)/.38f));
                        fx.image.color=new Color(1,1,1,fx.opacity*fadeIn*fadeOut);
                        fx.node.localScale=Vector3.one*Mathf.Lerp(.92f,1.12f,t);
                    }
                    if(fx.age>=fx.duration){var done=fx.completed;Release(i);done?.Invoke();}
                }
                for(int i=arrivals.Count-1;i>=0;i--)
                {
                    var arrival=arrivals[i];arrival.age+=dt;if(arrival.age<arrival.duration)continue;
                    arrivals.RemoveAt(i);var route=arrival.route;
                    bool order=route.targetContainer=="Order";
                    if(order&&route.targetSlot>=0&&route.targetSlot<4&&visualOrders[route.targetSlot]!=null)
                    {visualOrders[route.targetSlot].count=Math.Max(visualOrders[route.targetSlot].count,route.filledAfter);view.RefreshOrderPresentation();}
                    if(!order)view.PulseBufferArrival(route.targetSlot,route.itemId);
                    if(arrival.epoch==view.HapticEpoch&&view.CanPlayHaptic)Audio?.Play(order?AudioCue.PotArrival:AudioCue.PlateArrival);
                    if(arrival.epoch==view.HapticEpoch&&view.CanPlayHaptic)view.RequestHaptic(order&&route.filledAfter==3?GameplayHapticKind.Medium:GameplayHapticKind.Light);
                    if(order)view.NotifyTutorialFoodArrived(route.itemId);
                }
                for(int i=serves.Count-1;i>=0;i--)
                {
                    var serve=serves[i];serve.age+=dt;
                    float lift=Mathf.Max(.01f,Settings.serveLift),hold=Mathf.Max(0,Settings.serveHold),exit=Mathf.Max(.01f,Settings.serveExit),settle=Mathf.Max(.01f,Settings.serveSettle);
                    if(!serve.departed&&serve.age>=lift+hold){serve.departed=true;Audio?.Play(AudioCue.Serve);}
                    if(serve.node&&!serve.exited)
                    {
                        float lifted=Mathf.SmoothStep(0,1,Mathf.Clamp01(serve.age/lift));
                        float launch=Mathf.Clamp01((serve.age-lift-hold)/exit);
                        float height=Mathf.Lerp(0,12,lifted)+launch*launch*(Mathf.Max(300,Settings.serveExitDistance)-12);
                        serve.node.anchoredPosition=serve.origin+Vector2.up*height;
                        serve.node.localScale=Vector3.one*Mathf.Lerp(1+.055f*lifted,.82f,launch);
                    }
                    float settledAt=lift+hold+exit+settle;
                    if(serve.age>=lift+hold+exit)
                    {
                        if(!serve.exited){serve.exited=true;Audio?.Play(AudioCue.NewPot);if(serve.node){serve.node.gameObject.SetActive(false);Destroy(serve.node.gameObject);}visualOrders[serve.slot]=CopyOrder(serve.batch.replacement)??CopyOrder(snapshot.orders.FirstOrDefault(o=>o.slot==serve.slot));view.SetOrderServing(serve.slot,false);view.RefreshOrderPresentation();}
                        ReplacementSettling?.Invoke(serve.slot,Mathf.Clamp01((serve.age-lift-hold-exit)/settle));
                    }
                    if(serve.age>=settledAt&&!serve.routesFlushed)
                    {
                        serve.routesFlushed=true;
                        int routeCount=FlushDeferredRoutes(serve.batch);
                        serve.finishAt=settledAt+(routeCount>0?BufferOrderFlightSeconds:0);
                    }
                    if(serve.routesFlushed&&serve.age>=serve.finishAt)
                    {
                        if(latestBatches[serve.slot]==serve.batch)latestBatches[serve.slot]=null;
                        serves.RemoveAt(i);StartServing(serve.slot);
                    }
                }
                for(int slot=0;slot<4;slot++)
                {
                    StartServing(slot);
                    if(visualOrders[slot]!=null&&!IsServing(slot)&&queues[slot].Count==0&&!arrivals.Any(a=>a.route.targetContainer=="Order"&&a.route.targetSlot==slot)){visualOrders[slot]=null;view.RefreshOrderPresentation();}
                }
                for(int i=clears.Count-1;i>=0;i--)
                {
                    var clear=clears[i];
                    if(!clear.node){clears.RemoveAt(i);continue;}
                    clear.age+=dt;PlaceClear(clear);
                    if(clear.age>=ClearDepartAt+ClearDepartSeconds)RemoveClear(i);
                }
                if(snapshot.phase==ViewPhase.Running)
                {
                    foreach(var order in snapshot.orders)
                    {
                        int variant;float drift;if(!ambient.TryEmit(order.slot,clock.Elapsed,order.enabled,IsServing(order.slot),Settings,out variant,out drift))continue;
                        string key=SteamKey(variant);
                        var p=Pot(order.slot)+new Vector2(drift*8,-12);
                        Spawn("Steam_"+order.slot,Load(key)??Load("steam"),p,p+new Vector2(drift*Settings.steamDrift,-Settings.steamRise),Settings.steamSize,Mathf.Max(.1f,Settings.steamLifetime),3,0,Settings.steamOpacity);
                        // Existing accents remain alive but no longer flash on every steam emission.
                        if(variant==0)Spawn("bubble",Load("bubble"),p+new Vector2(-12,18),p+new Vector2(-12,18),18,.55f,0,0,.16f);
                        else if(variant==1)Spawn("oil_glint",Load("oil_glint"),p+new Vector2(14,5),p+new Vector2(14,5),24,.75f,0,0,.14f);
                    }
                }
            }
            if(transfer!=null)
            {
                float revivalDt=(float)revivalClock.Advance(Mathf.Min(delta,.1f),snapshot,true);
                if(revivalDt>0)AdvanceTransfer(revivalDt);
            }
        }
        void StartServing(int slot)
        {
            if(!layer||serves.Any(s=>s.slot==slot)||queues[slot].Count==0)return;
            if(arrivals.Any(a=>a.route.targetContainer=="Order"&&a.route.targetSlot==slot))return;
            var batch=queues[slot].Dequeue();var evt=batch.completion;view.SetOrderServing(slot,true);
            Audio?.Play(AudioCue.OrderComplete);
            impulseAge=0;
            var node=view.CreateServingPot(layer,slot,FoodId(evt.ingredientId));
            if(node)foreach(var graphic in node.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
            serves.Add(new Serve{slot=slot,node=node,origin=node?node.anchoredPosition:Vector2.zero,batch=batch});
            var at=Pot(slot);Pulse(at,"complete_glow",Mathf.Max(.16f,Settings.serveLift+Settings.serveHold),124);
            Spawn("ServeSteamA",Load(SteamKey(slot)),at+new Vector2(-14,-6),at+new Vector2(-24,-58),86,.82f,2,0,.34f);
            Spawn("ServeSteamB",Load(SteamKey(slot+1)),at+new Vector2(16,-4),at+new Vector2(28,-52),78,.76f,2,0,.28f);
        }
        int FlushDeferredRoutes(ServeBatch batch)
        {
            if(batch==null)return 0;
            batch.flushed=true;
            int count=0;
            while(batch.routes.Count>0){StartRouteFlight(batch.routes.Dequeue(),null);count++;}
            return count;
        }
        void StartRouteFlight(ViewEvent evt,Func<string,Vector2> position)
        {
            long routeEpoch; if(!routeHapticEpochs.TryGetValue(evt.sequence,out routeEpoch))routeEpoch=-1;
            routeHapticEpochs.Remove(evt.sequence);
            if(routeEpoch==view.HapticEpoch&&view.CanPlayHaptic)Audio?.Play(AudioCue.Flight);
            Vector2 target=evt.targetContainer=="Buffer"?Buffer(evt.targetSlot):Pot(evt.targetSlot);
            Vector2 from=evt.sourceContainer=="Buffer"?Buffer(evt.sourceSlot):(position!=null?position(evt.itemId):Vector2.zero);
            if(routeSources.TryGetValue(evt.sequence,out var savedSource)){from=savedSource;routeSources.Remove(evt.sequence);}
            int food=FoodId(evt.ingredientId);
            bool bufferToOrder=evt.sourceContainer=="Buffer"&&evt.targetContainer=="Order";
            float duration=bufferToOrder?BufferOrderFlightSeconds:FlightSeconds;
            // Arrival feedback is authoritative even when the bounded VFX pool cannot allocate a flight.
            arrivals.Add(new Arrival{duration=duration,epoch=routeEpoch,route=evt});
            // Food entering a pot follows a direct line; buffer auto-absorb is intentionally slower.
            var flight=Spawn("FlyingItem",Food(food),from,target,36,duration,1,0);
            if(flight!=null&&view.VisualArt!=null&&food>=0)flight.image.uvRect=view.VisualArt.FoodUv(food);
            if(flight==null)return;
            flight.itemId=evt.itemId;
            flight.completed=()=>
            {
                bool order=evt.targetContainer=="Order";
                Spawn(order?"PotLanding":"DishLanding",Load("splash_ripple"),target,target,order?82:44,order ? .18f : .14f,0,0,order ? .62f : .26f);
            };
        }
        public void Highlight(Func<Vector2> position,float diameter=72,float rotation=0)
        {
            if(!layer)return;var fx=Spawn("Hint",Load("hint"),position(),position(),diameter*1.15f,1.5f,0,0);if(fx!=null){fx.follow=position;fx.node.localRotation=Quaternion.Euler(0,0,-rotation);if(plateEffects)fx.node.SetParent(plateEffects,false);}
        }
        void Pulse(Vector2 at,string key,float seconds,float size){Spawn(key,Load(key),at,at,size,seconds,0,0);}
        Effect Spawn(string name,Texture2D texture,Vector2 from,Vector2 to,float size,float duration,int mode,float arc,float opacity=.8f)
        {
            if(!layer||!texture||active.Count>=Mathf.Clamp(Settings.concurrentEffects,1,EffectLimit))return null;
            Effect fx=null;
            while(pool.Count>0&&fx==null){int last=pool.Count-1;var item=pool[last];pool.RemoveAt(last);if(item.node)fx=item;}
            if(fx==null)
            {
                var go=new GameObject(name,typeof(RectTransform),typeof(RawImage));fx=new Effect{node=(RectTransform)go.transform,image=go.GetComponent<RawImage>()};fx.image.raycastTarget=false;
            }
            fx.node.name=name;fx.node.SetParent(layer,false);fx.node.anchorMin=fx.node.anchorMax=new Vector2(0,1);fx.node.pivot=new Vector2(.5f,.5f);fx.node.sizeDelta=Vector2.one*size;fx.node.localScale=Vector3.one;fx.node.gameObject.SetActive(true);
            // Spawn happens after this frame's effect update. Starting at white exposed
            // every new steam/bubble/glint at full opacity for one rendered frame.
            fx.node.localRotation=Quaternion.identity;fx.image.texture=texture;fx.image.uvRect=new Rect(0,0,1,1);fx.image.color=new Color(1,1,1,mode==2||mode==3||name=="bubble"||name=="oil_glint"?0:1);fx.age=0;fx.duration=duration;fx.from=from;fx.to=to;fx.mode=mode;fx.arc=arc;fx.size=size;fx.opacity=opacity;fx.itemId=null;fx.completed=null;fx.follow=null;At(fx.node,from);active.Add(fx);return fx;
        }
        void Release(int i){var fx=active[i];active.RemoveAt(i);if(fx.node){fx.node.gameObject.SetActive(false);fx.node.SetParent(layer,false);pool.Add(fx);}fx.completed=null;fx.follow=null;}
        static Vector2 FlightPoint(Vector2 from,Vector2 to,float progress,float arc)
        {
            float t=Mathf.Clamp01(progress),motion=t*t*(3-2*t);
            var point=Vector2.Lerp(from,to,motion);
            // A perpendicular bend remains visible even when source and pot align vertically.
            var delta=to-from;
            var bend=delta.sqrMagnitude>.001f?new Vector2(-delta.y,delta.x).normalized:Vector2.up;
            if(bend.y>0)bend=-bend;
            return point+bend*(Mathf.Sin(motion*Mathf.PI)*arc);
        }
        void BeginClear(ViewSnapshot previous)
        {
            if(!layer||previous?.buffer==null||previous.sessionId!=session||previous.sessionGeneration!=generation)return;
            var sources=previous.buffer.Select((item,slot)=>new{item,slot}).Where(x=>x.item!=null&&x.slot<BufferSlotLimit).ToArray();
            if(sources.Length==0)return;
            // A fast clear can happen before an earlier inbound flight has landed.
            for(int i=active.Count-1;i>=0;i--)if(active[i].itemId!=null&&sources.Any(s=>s.item.itemId==active[i].itemId))Release(i);
            if(clears.Count>=ClearTransportLimit)RemoveClear(0);
            var go=new GameObject("ClearBufferTransport_VisualOnly",typeof(RectTransform));
            var clear=new ClearTransport{node=(RectTransform)go.transform,items=new ClearItem[sources.Length]};
            clear.node.SetParent(layer,false);clear.node.anchorMin=clear.node.anchorMax=clear.node.pivot=new Vector2(0,1);clear.node.sizeDelta=Vector2.zero;
            clear.plate=ImageNode(clear.node,"ClearGatherPlate_NoPhysicsNoRaycast",view.VisualArt?.Texture(AssetKey.Plate),Vector2.zero,160).GetComponent<RawImage>();
            var anchors=view.VisualArt?.Theme.anchors;
            for(int i=0;i<sources.Length;i++)
            {
                var source=sources[i];
                var node=ImageNode(clear.node,"ClearBufferItem_"+source.item.itemId,Food(source.item.foodId),Buffer(source.slot),48);
                var image=node.GetComponent<RawImage>();if(view.VisualArt!=null)image.uvRect=view.VisualArt.FoodUv(source.item.foodId);
                var target=anchors!=null&&i<anchors.gatherItemSlots.Length?anchors.gatherItemSlots[i]:new PresentationPoint((i%3-1)*32,(i/3)*30-15);
                clear.items[i]=new ClearItem{node=node,image=image,sourceSlot=source.slot,offset=new Vector2(target.x,target.y)};
            }
            clears.Add(clear);PlaceClear(clear);
        }
        void PlaceClear(ClearTransport clear)
        {
            var anchors=view.VisualArt?.Theme.anchors;
            var gather=anchors==null?new Vector2(210,390):new Vector2(anchors.revivalGatherPlate.x,anchors.revivalGatherPlate.y);
            var entry=anchors==null?new Vector2(210,205):new Vector2(anchors.supplyEntryVisual.x,anchors.supplyEntryVisual.y);
            float depart=Mathf.SmoothStep(0,1,Mathf.Clamp01((clear.age-ClearDepartAt)/ClearDepartSeconds));
            if(depart>0&&!clear.departureAccent){clear.departureAccent=true;Pulse(gather,"complete_glow",.22f,176);}
            if(depart>0&&clear.node.parent==layer){clear.node.SetParent(layer.parent,false);clear.node.SetSiblingIndex(1);}
            var center=Vector2.Lerp(gather,entry,depart);float scale=Mathf.Lerp(1,.12f,depart);
            float alpha=1-Mathf.Clamp01((clear.age-(ClearDepartAt+ClearDepartSeconds-.1f))/.1f);
            At((RectTransform)clear.plate.transform,center);clear.plate.transform.localScale=Vector3.one*scale;clear.plate.color=new Color(1,1,1,alpha);
            for(int i=0;i<clear.items.Length;i++)
            {
                var item=clear.items[i];float t=Mathf.SmoothStep(0,1,Mathf.Clamp01((clear.age-.06f-i*.055f)/.24f));
                var point=Vector2.Lerp(Buffer(item.sourceSlot),gather+item.offset,t);point.y-=Mathf.Sin(t*Mathf.PI)*24;
                if(depart>0)point=center+item.offset*scale;
                At(item.node,point);item.node.localScale=Vector3.one*scale;item.image.color=new Color(1,1,1,alpha);
            }
        }
        void RemoveClear(int index)
        {
            var clear=clears[index];clears.RemoveAt(index);
            if(clear.node){clear.node.gameObject.SetActive(false);Destroy(clear.node.gameObject);}
        }
        void CancelClears(){for(int i=clears.Count-1;i>=0;i--)RemoveClear(i);}
        void BeginTransfer(RevivalTransferBatch batch)
        {
            if(batch?.token==null||!layer||batch.token.sessionId!=session||batch.token.sessionGeneration!=generation)return;
            if(transfer!=null&&transfer.token.Matches(batch.token))return;
            CancelClears();CancelTransfer();transfer=batch;revivalClock.Reset(session,generation);
            var go=new GameObject("RevivalTransport_VisualOnly",typeof(RectTransform));transport=(RectTransform)go.transform;transport.SetParent(layer,false);transport.anchorMin=transport.anchorMax=transport.pivot=new Vector2(0,1);transport.sizeDelta=Vector2.zero;
            transportPlate=ImageNode(transport,"GatherPlate_NoPhysicsNoRaycast",view.VisualArt?.Texture(AssetKey.Plate),Vector2.zero,160).GetComponent<RawImage>();
            transferFoods=new RectTransform[batch.items.Length];
            for(int i=0;i<batch.items.Length;i++)
            {
                var item=batch.items[i];transferFoods[i]=ImageNode(transport,"BufferTransfer_"+item.itemId,Food(FoodId(item.ingredientId)),Buffer(item.sourceSlot),48);
                if(view.VisualArt!=null)transferFoods[i].GetComponent<RawImage>().uvRect=view.VisualArt.FoodUv(FoodId(item.ingredientId));
            }
            AdvanceTransfer(0);
        }
        void AdvanceTransfer(float dt)
        {
            if(transfer==null||!transport)return;transferAge+=dt;
            var anchors=view.VisualArt?.Theme.anchors;
            var gather=anchors==null?new Vector2(210,390):new Vector2(anchors.revivalGatherPlate.x,anchors.revivalGatherPlate.y);
            var entry=anchors==null?new Vector2(210,205):new Vector2(anchors.supplyEntryVisual.x,anchors.supplyEntryVisual.y);
            float depart=Mathf.SmoothStep(0,1,Mathf.Clamp01((transferAge-.52f)/.28f));
            if(depart>0&&transport.parent==layer){transport.SetParent(layer.parent,false);transport.SetSiblingIndex(1);}
            var center=Vector2.Lerp(gather,entry,depart);
            float scale=Mathf.Lerp(1,.12f,depart);
            transportPlate.gameObject.SetActive(transferAge>=.1f);
            At((RectTransform)transportPlate.transform,center);transportPlate.transform.localScale=Vector3.one*scale;
            for(int i=0;i<transfer.items.Length;i++)
            {
                var item=transfer.items[i];var target=anchors!=null&&item.targetIndex<anchors.gatherItemSlots.Length?anchors.gatherItemSlots[item.targetIndex]:new PresentationPoint(0,0);
                Vector2 offset=new Vector2(target.x,target.y),from=Buffer(item.sourceSlot),to=gather+offset;
                float t=Mathf.Clamp01((transferAge-.16f-i*.04f)/.16f);
                Vector2 position=Vector2.Lerp(from,to,t);position.y-=Mathf.Sin(t*Mathf.PI)*24;
                if(depart>0)position=center+offset*scale;
                At(transferFoods[i],position);transferFoods[i].localScale=Vector3.one*(depart>0?scale:1);
            }
            // Fade behind the supply boundary, never display a new active physical plate.
            float alpha=1-Mathf.Clamp01((transferAge-.70f)/.1f);
            transportPlate.color=new Color(1,1,1,alpha);foreach(var item in transferFoods)item.GetComponent<RawImage>().color=new Color(1,1,1,alpha);
            if(transferAge>=V7Art.RevivalSeconds&&!completedTransfer)
            {
                completedTransfer=true;var token=transfer.token;finishedTransferToken=token.completionToken;
                if(transport)transport.gameObject.SetActive(false);
                // Callback consumes only this generation's exact token; stale/cancelled callbacks cannot resume a session.
                view.RevivalPort?.CompleteRevivalTransfer(token);
                CancelTransfer();
            }
        }
        RectTransform ImageNode(Transform parent,string name,Texture2D texture,Vector2 at,float size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(RawImage));go.transform.SetParent(parent,false);var n=(RectTransform)go.transform;n.anchorMin=n.anchorMax=new Vector2(0,1);n.pivot=new Vector2(.5f,.5f);n.sizeDelta=Vector2.one*size;At(n,at);
            var image=go.GetComponent<RawImage>();image.texture=texture;image.raycastTarget=false;return n;
        }
        Vector2 Pot(int slot)=>view.VisualArt!=null?view.VisualArt.Pot(slot):new Vector2(56+slot*103,166);
        Vector2 Buffer(int slot)=>view.VisualArt!=null?view.VisualArt.Buffer(slot):new Vector2(75+slot*67,259);
        static void At(RectTransform node,Vector2 p){node.anchoredPosition=new Vector2(p.x,-p.y);}
        static int FoodId(string value){int n;return value!=null&&value.StartsWith("food_")&&int.TryParse(value.Substring(5),out n)?n:-1;}
        Texture2D Food(int id)=>id>=0&&foods!=null&&id<foods.Length?foods[id]:null;
        string SteamKey(int seed)
        {
            var keys=Settings.steamKeys;
            return keys!=null&&keys.Length>0?keys[Mathf.Abs(seed)%keys.Length]:"steam";
        }
        Texture2D Load(string key)
        {
            Texture2D texture;if(textureCache.TryGetValue(key,out texture))return texture;
            try{texture=view.VisualArt?.Texture("fx."+key);}
            catch(AssetFailure) when(key.StartsWith("steam_soft_",StringComparison.Ordinal)){texture=view.VisualArt?.Texture("fx.steam");}
            if(view.VisualArt==null&&!string.IsNullOrEmpty(root))texture=PresentationAssets.Load<Texture2D>(root+"/fx/"+key);
            textureCache[key]=texture;return texture;
        }
        void OnDestroy(){ResetFeedback();}
        public void ReleaseAssetReferences(){ResetFeedback();textureCache.Clear();foods=null;}
    }
}
