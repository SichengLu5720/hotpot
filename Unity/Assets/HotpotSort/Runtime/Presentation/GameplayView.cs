using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HotpotSort.UnityPhysics;

namespace HotpotSort.Presentation
{
    /// <summary>Mount using Bind from the platform/session bootstrap. No automatic core or daily seed creation.</summary>
    public sealed partial class GameplayView : MonoBehaviour
    {
        public MonoBehaviour portComponent;
        public Font playerFont;
        private Font displayFont;
        public event Action<ViewAction> ActionRequested;
        public event Action<RewardKind,RewardRoute> RewardRequested;
        public event Action SettingsRequested,FriendsRequested,ShareRequested;
        public ViewSnapshot LastSnapshot { get; private set; }
        public long LastEventSequence { get; private set; }
        public string LastTransactionId { get; private set; }
        public PlatePresentationWorld World { get; private set; }
        private GameplayFeedback feedback;
        private IPresentationPort port;
        private RectTransform canvasRoot, content;
        private RectTransform board,hud,modal,timerPlate;
        private RectTransform plateClip,plateLayer;
        public Rect PlateCrop { get; private set; } = new Rect(0,292,420,536);
        private Text timerLabel;
        private readonly RectTransform[] orderNodes=new RectTransform[4],bufferNodes=new RectTransform[5];
        private readonly string[] orderSignatures=new string[4],bufferSignatures=new string[5];
        private readonly bool[] serving=new bool[4];
        private readonly Dictionary<string,string> plateSignatures=new Dictionary<string,string>();
        private ViewPhase renderedPhase=(ViewPhase)(-1);
        private Texture2D dish,table,potBody,potUnlit,potBroth,potRim;
        private RawImage backgroundImage;
        private string assetRoot;
        private readonly Dictionary<string,Sprite> uiSprites=new Dictionary<string,Sprite>();
        private TaskCompletionSource<RewardOutcome> simulation;
        private readonly Texture2D[] foods = new Texture2D[16];
        private Texture2D plate;
        private Sprite rounded;
        private Texture2D roundedTexture;
        private Rect viewport,menuButtonPixels;
        private float viewportScreenHeight;
        private Vector2Int viewportAppliedAt;
        public string AssetRoot => assetRoot;
        private bool externalViewport, foreground = true;
        private long inputSeq;
        private readonly HashSet<string> pending = new HashSet<string>();
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        // A platform event source may replace frame-polled taps without changing hit testing.
        public bool UsesExternalTapInput { get; set; }
        private readonly Dictionary<string,RectTransform> plateVisuals = new Dictionary<string,RectTransform>();
        private readonly Dictionary<PlatePresentationWorld.Remnant,RectTransform> ghostVisuals = new Dictionary<PlatePresentationWorld.Remnant,RectTransform>();
        private static readonly Color Cream = new Color32(248,235,208,255), Ink = new Color32(77,40,23,255), Coral = new Color32(149,36,26,255), Green = new Color32(221,183,119,255);
        private void Awake()
        {
            playerFont = displayFont = TaskAssetValidation.LoadModernFont();
            roundedTexture = new Texture2D(64,64,TextureFormat.RGBA32,false);
            for (int y=0;y<64;y++) for (int x=0;x<64;x++)
            {
                float dx=Mathf.Max(0,Mathf.Abs(x-31.5f)-15.5f), dy=Mathf.Max(0,Mathf.Abs(y-31.5f)-15.5f);
                roundedTexture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(16.5f-Mathf.Sqrt(dx*dx+dy*dy))));
            }
            roundedTexture.Apply(); rounded = Sprite.Create(roundedTexture,new Rect(0,0,64,64),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(20,20,20,20));
            var root = new GameObject("PlayerCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            root.transform.SetParent(transform,false); canvasRoot = (RectTransform)root.transform;
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var background = Node(canvasRoot,"CreamBackground",new Rect(0,0,Screen.width,Screen.height));
            backgroundImage = background.gameObject.AddComponent<RawImage>(); backgroundImage.color = new Color32(96,52,28,255); backgroundImage.raycastTarget = false;
            background.anchorMin=Vector2.zero; background.anchorMax=Vector2.one; background.offsetMin=background.offsetMax=Vector2.zero;
            content = Node(canvasRoot,"SafeContent",new Rect());
            World = gameObject.AddComponent<PlatePresentationWorld>();
            feedback = gameObject.AddComponent<GameplayFeedback>();
            feedback.Initialize(canvasRoot, root.GetComponent<Canvas>());
            feedback.ReplacementSettling=SetReplacementSettling;
            if (!FindObjectOfType<EventSystem>())
            { var events = new GameObject("ViewEventSystem",typeof(EventSystem),typeof(StandaloneInputModule)); events.transform.SetParent(transform,false); }
            viewport = Screen.safeArea;
            LastSnapshot = new ViewSnapshot { phase = ViewPhase.Entry };
            if (portComponent is IPresentationPort) Bind((IPresentationPort)portComponent);
        }
        public void Bind(IPresentationPort source)
        {
            CancelShuffleFeedback();
            if(FriendBoardVisible)CloseFriendBoardView();
            simulation?.TrySetResult(RewardOutcome.Cancelled);simulation=null;
            if (port != null) port.Updated -= Apply;
            port = source; pending.Clear(); LastSnapshot = null;
            if (port != null) { port.Updated += Apply; Apply(new ViewUpdate { snapshot = port.Read() }); }
            else { feedback.ResetFeedback(); LastSnapshot = new ViewSnapshot { phase = ViewPhase.Entry }; World.Clear(); Render(); }
        }
        public void SetViewport(Rect screenPixelSafeArea,float pixelScreenHeight=0)
        { SetViewport(screenPixelSafeArea,pixelScreenHeight,new Rect()); }
        public void SetViewport(Rect screenPixelSafeArea,float pixelScreenHeight,Rect screenPixelMenuButton)
        { externalViewport = true; viewport = screenPixelSafeArea;viewportScreenHeight=pixelScreenHeight;menuButtonPixels=screenPixelMenuButton;viewportAppliedAt=new Vector2Int(Screen.width,Screen.height);Render();UpdateFriendBoardPresentation(); }
        public void SetForeground(bool value) { foreground = value; UpdateSimulation(); }
        public void Hide(bool hidden) { canvasRoot.gameObject.SetActive(!hidden); UpdateSimulation(); }
        private void UpdateSimulation() { World.SetSimulating(foreground && canvasRoot.gameObject.activeInHierarchy && !FriendBoardVisible && LastSnapshot.phase==ViewPhase.Running && LastSnapshot.pauseReasons==ViewPauseReasons.None); }
        public void ResetView() { CancelShuffleFeedback(); simulation?.TrySetResult(RewardOutcome.Cancelled);simulation=null;pending.Clear();Array.Clear(serving,0,4); LastEventSequence=0; LastTransactionId=null; feedback.ResetFeedback(); World.Clear(); LastSnapshot=new ViewSnapshot { phase=ViewPhase.Entry }; Render(); }
        public void ShowError(string message)
        { CancelShuffleFeedback(); feedback.ResetFeedback(); LastSnapshot = new ViewSnapshot { sessionId=LastSnapshot?.sessionId, phase=ViewPhase.Aborted, message=message }; World.Clear(); Render(); }
        public void DestroyView() { Destroy(gameObject); }
        public void Apply(ViewUpdate update)
        {
            if (update == null || update.snapshot == null) return;
            var s = update.snapshot;
            if (LastSnapshot != null && LastSnapshot.sessionId == s.sessionId && s.revision < LastSnapshot.revision) return;
            if (LastSnapshot == null || LastSnapshot.sessionId != s.sessionId || LastSnapshot.sessionGeneration!=s.sessionGeneration) { simulation?.TrySetResult(RewardOutcome.Cancelled);simulation=null;pending.Clear(); LastEventSequence=0; }
            var previous = LastSnapshot;
            if (ShuffleFeedbackActive && (s.sessionId != shuffleSession || s.sessionGeneration != shuffleGeneration ||
                s.phase == ViewPhase.Entry || s.phase == ViewPhase.Aborted || s.phase == ViewPhase.Overflow || s.phase == ViewPhase.Won)) CancelShuffleFeedback();
            var sourcePositions=new Dictionary<string,Vector2>();
            foreach(var body in World.Bodies)foreach(var item in body.data.items)sourcePositions[item.itemId]=World.ItemPosition(item.itemId);
            foreach (var e in update.events ?? new ViewEvent[0]) if (e.sequence > LastEventSequence)
            { LastEventSequence=e.sequence; LastTransactionId=e.transactionId; }
            LastEventSequence = Math.Max(LastEventSequence,s.eventSeq);
            // Final authoritative snapshot is the convergence point, including gaps/reconnects/long chains.
            LastSnapshot = s; pending.Clear(); UpdateSimulation(); World.Reconcile(s); Render();
            feedback.Apply(update, previous, id=>sourcePositions.TryGetValue(id,out var value)?value:World.ItemPosition(id));
        }
        private void LateUpdate()
        {
            TickShuffleFeedback(Time.unscaledDeltaTime);
            UpdateFriendBoardPresentation();
            if(art!=null&&board){float scale=Mathf.Min(viewport.width/420,viewport.height/900);board.anchoredPosition=new Vector2((viewport.width-420*scale)/2,-(viewport.height-900*scale)/2)+feedback.ScreenImpulse*scale;}
            foreach(var body in World.Bodies)
            {
                RectTransform node;
                if(plateVisuals.TryGetValue(body.data.plateId,out node) && node)
                { PositionPlateVisual(body,node); }
            }
            foreach(var pair in ghostVisuals)
            {
                if(!pair.Value)continue;
                float t=Mathf.Clamp01(pair.Key.remaining/.18f);
                pair.Value.gameObject.SetActive(t>0);
                pair.Value.localScale=Vector3.one*(.75f+.25f*t);
                pair.Value.GetComponent<RawImage>().color=new Color(1,1,1,t);
            }
        }
        private void Update()
        {
            if(art!=null&&Application.platform==RuntimePlatform.WindowsPlayer&&viewportScreenHeight<=0&&viewportAppliedAt!=new Vector2Int(Screen.width,Screen.height))
            {viewportAppliedAt=new Vector2Int(Screen.width,Screen.height);viewport=Screen.safeArea;Render();}
            if (!externalViewport && viewport != Screen.safeArea) { viewport=Screen.safeArea; Render(); }
            if (UsesExternalTapInput || !foreground || !canvasRoot.gameObject.activeInHierarchy || LastSnapshot.phase != ViewPhase.Running || port == null) return;
            if (Input.touchCount>0) { var t=Input.GetTouch(0); if(t.phase==TouchPhase.Began) SubmitScreenTap(t.position,t.fingerId); }
            else if (Input.GetMouseButtonDown(0)) SubmitScreenTap(Input.mousePosition,-1);
        }
        public bool SubmitScreenTap(Vector2 screen, int pointerId = -1)
        {
            if (ShuffleFeedbackActive) return false;
            if (port == null || !foreground || FriendBoardVisible || LastSnapshot.phase != ViewPhase.Running || LastSnapshot.pauseReasons!=ViewPauseReasons.None || (modal&&modal.gameObject.activeInHierarchy) || !viewport.Contains(screen)) return false;
            if (EventSystem.current)
            {
                var data=new PointerEventData(EventSystem.current) { position=screen, pointerId=pointerId };
                uiHits.Clear(); EventSystem.current.RaycastAll(data,uiHits); if(uiHits.Count>0)return false;
            }
            Vector2 local;if(!board || !RectTransformUtility.ScreenPointToLocalPointInRectangle(board,screen,null,out local))return false;
            var point = new Vector2(local.x,-local.y);
            if (!PlateCrop.Contains(point)) return false;
            string id=World.Hit(point,OpaqueHit); if(id==null || !pending.Add(id))return false;
            try { port.Tap(new ViewTap { itemId=id,inputSeq=++inputSeq,snapshotRevision=LastSnapshot.revision,boardX=point.x,boardY=point.y }); }
            catch { pending.Remove(id); throw; }
            return true;
        }
        // Explicit acknowledgement for rejection/no-state-change; does not alter inventory.
        public void AcknowledgeTap(string itemId) { pending.Remove(itemId); }
        public ViewSupplyObservation ObserveSupply(Vector2 center, float radius)
        {
            var value = World.ObserveHeightGate(LastSnapshot.revision);
            value.x=center.x;value.y=center.y;value.radius=radius;
            port?.ObserveSupply(value); return value;
        }
        private bool OpaqueHit(ViewItem item,Vector2 offset)
        {
            var texture=foods[item.foodId];
            var uv=ItemUV(item);
            return !texture || !texture.isReadable || texture.GetPixelBilinear(uv.x+uv.width*(.5f+offset.x/(item.radius*2)),uv.y+uv.height*(.5f-offset.y/(item.radius*2))).a>.1f;
        }
        private bool ClickablePoint(string id,Vector2 point)
        {
            if(FriendBoardVisible || !PlateCrop.Contains(point) || World.Hit(point,OpaqueHit)!=id || !board || (modal&&modal.gameObject.activeInHierarchy))return false;
            var screen=RectTransformUtility.WorldToScreenPoint(null,board.TransformPoint(new Vector3(point.x,-point.y,0)));
            if(!viewport.Contains(screen))return false;
            if(EventSystem.current)
            {
                uiHits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=screen},uiHits);
                if(uiHits.Count>0)return false;
            }
            return true;
        }
        public bool IsItemClickable(string id)
        {
            if (ShuffleFeedbackActive) return false;
            if(FriendBoardVisible || !foreground || !canvasRoot.gameObject.activeInHierarchy || LastSnapshot.phase!=ViewPhase.Running || pending.Contains(id) || (modal&&modal.gameObject.activeInHierarchy))return false;
            foreach(var body in World.Bodies)foreach(var item in body.data.items)
            {
                if(item.itemId!=id)continue;
                var center=World.ItemPosition(id);float r=item.radius;
                var extents=PlateItemTransform.HalfExtents(item);
                float left=Mathf.Max(PlateCrop.xMin,center.x-extents.x),right=Mathf.Min(PlateCrop.xMax,center.x+extents.x);
                float top=Mathf.Max(PlateCrop.yMin,center.y-extents.y),bottom=Mathf.Min(PlateCrop.yMax,center.y+extents.y);
                if(left>=right || top>=bottom)return false;
                var middle=new Vector2((left+right)*.5f,(top+bottom)*.5f);
                if(ClickablePoint(id,middle))return true;
                var texture=foods[item.foodId];int width=texture?texture.width:32,height=texture?texture.height:32;
                // Sample every intersecting source texel cell, including a sliver at
                // the crop edge, through the very same rim/collider/alpha predicate.
                var uv=ItemUV(item);float dx=2*r/Mathf.Max(1,width*uv.width),dy=2*r/Mathf.Max(1,height*uv.height);
                for(float y=top;y<bottom;y+=dy)
                {
                    float y1=Mathf.Min(bottom,y+dy);
                    for(float x=left;x<right;x+=dx)
                    {
                        float x1=Mathf.Min(right,x+dx);
                        if(ClickablePoint(id,new Vector2((x+x1)*.5f,(y+y1)*.5f)))return true;
                    }
                }
                return false;
            }
            return false;
        }
        public string FindClickableHint()
        {
            return LastSnapshot.plates.SelectMany(p=>p.items)
                .Where(i=>LastSnapshot.orders.Any(o=>o.enabled && o.foodId==i.foodId && o.count<o.required))
                .OrderBy(i=>int.Parse(i.itemId)).Select(i=>i.itemId).FirstOrDefault(IsItemClickable);
        }
        private void Action(ViewAction action) { feedback.Button(); ActionRequested?.Invoke(action); port?.SessionAction(action); }
        private void Render()
        {
            if(art!=null){RenderV7();return;}
            if (!content) return;
            if (viewport.width<=0 || viewport.height<=0) viewport=new Rect(0,0,Screen.width,Screen.height);
            Place(content,new Rect(viewport.x,(viewportScreenHeight>0?viewportScreenHeight:Screen.height)-viewport.yMax,viewport.width,viewport.height));
            if(table){float aspect=canvasRoot.rect.width/canvasRoot.rect.height,source=table.width/(float)table.height;float u=aspect<source?aspect/source:1,v=aspect>source?source/aspect:1;backgroundImage.uvRect=new Rect((1-u)/2,(1-v)/2,u,v);}
            bool entry=LastSnapshot.phase==ViewPhase.Entry;
            if(entry || !board)
            {
                ClearChildren(content);plateVisuals.Clear();plateSignatures.Clear();ghostVisuals.Clear();board=null;
                Array.Clear(orderSignatures,0,4);Array.Clear(bufferSignatures,0,5);
                if(entry){Entry();renderedPhase=LastSnapshot.phase;return;}
                board=Node(content,"GameplayBoard",new Rect(0,0,420,900));
                plateClip=Node(board,"PlateClip",PlateCrop);plateClip.gameObject.AddComponent<RectMask2D>();
                plateLayer=Node(plateClip,"PlateLayer",new Rect(0,-PlateCrop.yMin,420,900));
                hud=Node(board,"FixedHUD",new Rect(0,0,420,64));
                Button(hud,"暂停",new Rect(12,6,54,54),()=>Action(ViewAction.Pause));
                timerPlate=Panel(hud,"TimerPlate",new Rect(153,8,114,44),Coral);
                timerLabel=Label(hud,"10:00",new Rect(162,9,96,40),26);timerLabel.color=Cream;
                if(string.IsNullOrEmpty(assetRoot))Label(board,"开发占位 · 正式美术/音频待集成",new Rect(70,54,335,16),10);
                for(int i=0;i<4;i++)orderNodes[i]=Node(board,"Order_"+i,new Rect(6+i*103,72,100,140));
                for(int i=0;i<5;i++)bufferNodes[i]=Node(board,"Buffer_"+i,new Rect(43+i*67,228,64,64));
                float bufferBottom=bufferNodes.Max(n=>-n.anchoredPosition.y+n.rect.height);
                PlateCrop=new Rect(0,bufferBottom,420,828-bufferBottom);
                Place(plateClip,PlateCrop);Place(plateLayer,new Rect(0,-bufferBottom,420,900));
                var bufferBlock=Node(board,"BufferInputBlock",new Rect(0,228,420,bufferBottom-228));
                var blocker=bufferBlock.gameObject.AddComponent<Image>();blocker.color=Color.clear;blocker.raycastTarget=true;
                Button(board,"提示",new Rect(47,834,62,62),()=>ChooseReward(RewardKind.Hint));
                Button(board,"清空暂存",new Rect(179,834,62,62),()=>ChooseReward(RewardKind.ClearBuffer));
                Button(board,"打乱",new Rect(311,834,62,62),()=>ChooseReward(RewardKind.Shuffle));
                feedback.SetBoard(board,plateLayer);
            }
            float scale=Mathf.Min(viewport.width/420,viewport.height/900);
            board.anchoredPosition=new Vector2((viewport.width-420*scale)/2,-(viewport.height-900*scale)/2);
            board.localScale=Vector3.one*scale;
            LayoutTopTimer();
            for(int slot=0;slot<4;slot++)
            {
                orderNodes[slot].gameObject.SetActive(!serving[slot]);
                ViewOrder order=Array.Find(LastSnapshot.orders,o=>o.slot==slot);
                bool enabled=order!=null && order.enabled;
                string signature=enabled+":"+(order==null?"null":order.foodId+":"+order.count);
                if(orderSignatures[slot]==signature)continue;
                orderSignatures[slot]=signature;var box=orderNodes[slot];ClearChildren(box);
                Picture(box,(enabled?potBody:potUnlit)?(enabled?potBody:potUnlit):plate,new Rect(-14,30,128,128),"PotBody",true);
                if(enabled)
                {
                    if(potBroth)Picture(box,potBroth,new Rect(-14,30,128,128),"Broth");
                    if(potRim)Picture(box,potRim,new Rect(-14,30,128,128),"PotRim");
                    if(order.foodId>=0){Panel(box,"OrderTag",new Rect(3,0,94,38),Cream);Icon(box,"../ui/order_pointer",new Rect(42,35,16,8));Food(box,order.foodId,new Rect(6,2,34,34));Label(box,order.count+"/3",new Rect(40,0,55,38),22);}
                    else Label(box,"已结清",new Rect(0,0,100,36),16);
                }
                else {Panel(box,"OrderTag",new Rect(3,0,94,32),Cream);Label(box,slot==2?"31单开火":"49单开火",new Rect(0,0,100,32),14);Icon(box,"lock",new Rect(37,67,26,26));if(slot==3)Button(box,"提前开锅",new Rect(9,97,83,32),()=>RewardRequested?.Invoke(RewardKind.FourthPot,RewardRoute.SimulatedAd));}
            }
            for(int slot=0;slot<5;slot++)
            {
                var bufferItem=slot<LastSnapshot.buffer.Length?LastSnapshot.buffer[slot]:null;
                string signature=bufferItem==null?"empty":bufferItem.itemId+":"+bufferItem.foodId;
                if(bufferSignatures[slot]==signature)continue;
                bufferSignatures[slot]=signature;var cell=bufferNodes[slot];ClearChildren(cell);
                Picture(cell,dish?dish:plate,new Rect(0,0,64,64),"Dish");
                if(slot<LastSnapshot.buffer.Length && LastSnapshot.buffer[slot]!=null) Food(cell,LastSnapshot.buffer[slot].foodId,new Rect(5,5,51,51));
            }
            var alive=new HashSet<string>();
            foreach(var body in World.Bodies)
            {
                var p=body.data; var at=World.Position(body);
                alive.Add(p.plateId);RectTransform node;
                if(!plateVisuals.TryGetValue(p.plateId,out node)){node=Node(plateLayer,"PlateVisual_"+p.plateId,new Rect(at.x,at.y,0,0));plateVisuals.Add(p.plateId,node);}
                string signature=string.Join("|",p.items.Select(i=>i.itemId+":"+i.foodId));
                if(!plateSignatures.ContainsKey(p.plateId) || plateSignatures[p.plateId]!=signature)
                {
                    ClearChildren(node);plateSignatures[p.plateId]=signature;
                    float extent=string.IsNullOrEmpty(assetRoot)?p.radius:p.radius*512f/448f;
                    Picture(node,plate,new Rect(-extent,-extent,extent*2,extent*2),"Plate_"+p.plateId);
                    foreach(var item in p.items) Food(node,item.foodId,new Rect(item.x-item.radius,item.y-item.radius,item.radius*2,item.radius*2));
                }
            }
            foreach(string id in plateVisuals.Keys.ToArray())if(!alive.Contains(id)){Destroy(plateVisuals[id].gameObject);plateVisuals.Remove(id);plateSignatures.Remove(id);}
            foreach(var pair in ghostVisuals.ToArray())if(pair.Key.remaining<=0 || !World.Remnants.Contains(pair.Key)){if(pair.Value)Destroy(pair.Value.gameObject);ghostVisuals.Remove(pair.Key);}
            foreach(var ghost in World.Remnants)
            {
                if(ghostVisuals.ContainsKey(ghost))continue;
                var node=Node(plateLayer,"Clearing_"+ghost.plateId,new Rect(ghost.position.x-ghost.radius,ghost.position.y-ghost.radius,ghost.radius*2,ghost.radius*2));
                var image=node.gameObject.AddComponent<RawImage>(); image.texture=plate; image.raycastTarget=false;
                ghostVisuals.Add(ghost,node);
            }
            feedback.SetBoard(board,plateLayer);
            hud.SetAsLastSibling();
            if(LastSnapshot.phase!=renderedPhase)
            {
                CloseModal();if(LastSnapshot.phase!=ViewPhase.Running)Overlay();renderedPhase=LastSnapshot.phase;
            }
        }
        private void Entry()
        {
            float w=viewport.width,h=viewport.height;
            bool wide=w/h>.6f, narrow=!wide && w<350;
            float d=wide?Mathf.Min(w*.47143f,h*.4f):(narrow?w*.59375f:w*.51795f);
            float y=h*(wide?.308f:narrow?.30014f:.33649f), x=(w-d)/2;
            Picture(content,plate,new Rect(x,y,d,d),"EntryHeroPlate");
            Food(content,0,new Rect(x+d*.15f,y+d*.16f,d*.34f,d*.34f));
            Food(content,5,new Rect(x+d*.54f,y+d*.20f,d*.33f,d*.33f));
            Food(content,4,new Rect(x+d*.34f,y+d*.54f,d*.35f,d*.35f));
            float bw=Mathf.Min(w-44,wide?420:300);
            Button(content,"开始下火锅",new Rect((w-bw)/2,h-90,bw,56),()=>Action(ViewAction.StartToday));
            var title=Label(content,"火锅消消 · 每日挑战",new Rect(20,35,w-40,44),28);if(displayFont)title.font=displayFont;title.color=Cream;
            Label(content,"北京时间06:00更新 · 本地开发模拟",new Rect(20,86,w-40,30),15).color=Cream;
            Button(content,"设置",new Rect(24,135,(w-60)/2,42),()=>SettingsRequested?.Invoke());
            Button(content,"好友榜",new Rect(w/2+6,135,(w-60)/2,42),()=>FriendsRequested?.Invoke());
        }
        private void Overlay()
        {
            if(art!=null){OverlayV7();return;}
            modal=Panel(content,"FlowOverlay",new Rect(0,0,viewport.width,viewport.height),Cream);
            string title=LastSnapshot.phase==ViewPhase.Paused?"已暂停":LastSnapshot.phase==ViewPhase.Won?"挑战完成":LastSnapshot.phase==ViewPhase.Overflow?(LastSnapshot.message=="Timeout"?"时间到":"暂存已满"):"挑战已中止";
            Label(modal,title,new Rect(15,viewport.height*.19f,viewport.width-30,48),28);
            float y=viewport.height*.30f;
            foreach(var fact in LastSnapshot.facts) { Label(modal,fact.label+"："+fact.value,new Rect(20,y,viewport.width-40,32),18); y+=34; }
            if(!string.IsNullOrEmpty(LastSnapshot.message)) Label(modal,LastSnapshot.message,new Rect(20,y,viewport.width-40,70),16);
            bool paused=LastSnapshot.phase==ViewPhase.Paused;
            Button(modal,paused?"继续":"重新挑战",new Rect(35,viewport.height-156,viewport.width-70,52),()=>Action(paused?ViewAction.Resume:ViewAction.RetrySameDay));
            Button(modal,"退出",new Rect(35,viewport.height-92,viewport.width-70,48),()=>Action(ViewAction.Exit));
            Button(modal,paused?"设置":"分享主题图",new Rect(35,viewport.height-218,viewport.width-70,46),()=>{if(paused)SettingsRequested?.Invoke();else ShareRequested?.Invoke();});
        }
        public void ConfigureAssets(string root)
        {
            if(PresentationAssets.IsThemeRoot(root)){ConfigureV7(root);return;}
            assetRoot=root;
            feedback.ConfigureAssets(root,foods);
            if(string.IsNullOrEmpty(root))return;
            var missing=TaskAssetValidation.Validate(root);if(missing.Length>0)throw new InvalidOperationException(string.Join("; ",missing));
            for(int i=0;i<16;i++)foods[i]=PresentationAssets.Load<Texture2D>(root+"/food/food_"+i.ToString("00"));
            plate=PresentationAssets.Load<Texture2D>(root+"/containers/plate_main");dish=PresentationAssets.Load<Texture2D>(root+"/containers/dish_buffer");
            table=PresentationAssets.Load<Texture2D>(root+"/background/table");potBody=PresentationAssets.Load<Texture2D>(root+"/pots/pot_body");potUnlit=PresentationAssets.Load<Texture2D>(root+"/pots/pot_unlit");potBroth=PresentationAssets.Load<Texture2D>(root+"/pots/pot_broth");potRim=PresentationAssets.Load<Texture2D>(root+"/pots/pot_rim");
            playerFont=displayFont=TaskAssetValidation.LoadModernFont();uiSprites.Clear();
            foreach(string name in new[]{"panel","button","button_round","order_plate","progress_track","timer_plate"})
            {
                uiSprites[name]=PresentationAssets.Load<Sprite>(root+"/ui/"+name);
            }
            backgroundImage.texture=table;backgroundImage.color=Color.white;Array.Clear(orderSignatures,0,4);Array.Clear(bufferSignatures,0,5);plateSignatures.Clear();Render();
        }
        public void SetRemainingTime(double seconds){remainingSeconds=seconds;if(timerLabel){int s=Mathf.CeilToInt((float)seconds);timerLabel.text=(s/60).ToString("00")+":"+(s%60).ToString("00");}}
        void LayoutTopTimer()
        {
            if(!board||!timerPlate||!timerLabel)return;
            var timer=new Rect(153,8,114,44);
            float scale=Mathf.Min(viewport.width/420,viewport.height/900);
            if(scale>0&&menuButtonPixels.width>0&&menuButtonPixels.height>0)
            {
                float screenHeight=viewportScreenHeight>0?viewportScreenHeight:Screen.height;
                float boardLeft=viewport.x+(viewport.width-420*scale)*.5f;
                float boardTop=screenHeight-viewport.yMax+(viewport.height-900*scale)*.5f;
                var guard=new Rect(
                    (menuButtonPixels.xMin-boardLeft)/scale-12,
                    (screenHeight-menuButtonPixels.yMax-boardTop)/scale-12,
                    menuButtonPixels.width/scale+24,
                    menuButtonPixels.height/scale+24);
                if(timer.Overlaps(guard,true))timer.y=Mathf.Max(timer.y,guard.yMax);
            }
            Place(timerPlate,timer);
            var labelRect=new Rect(timer.x+9,timer.y+1,96,40);
            var labelTransform=timerLabel.rectTransform;
            labelTransform.anchorMin=labelTransform.anchorMax=new Vector2(0,1);
            labelTransform.pivot=new Vector2(0,1);
            labelTransform.anchoredPosition=new Vector2(labelRect.x,-labelRect.y);
            // Label() renders at 4x and downsamples with a .25 transform scale.
            // Preserve that raster size when the capsule guard repositions the timer.
            labelTransform.sizeDelta=labelRect.size*4;
            labelTransform.localScale=Vector3.one*.25f;
            timerLabel.horizontalOverflow=HorizontalWrapMode.Overflow;
        }
        public void SetAudioSettings(PlayerSettings settings)=>feedback.SetAudioSettings(settings);
        public void HighlightItem(string id)
        {
            if(!IsItemClickable(id))return;
            var item=World.Bodies.SelectMany(b=>b.data.items).FirstOrDefault(i=>i.itemId==id);
            if(item!=null)feedback.Highlight(()=>IsItemClickable(id)?World.ItemPosition(id):Vector2.zero,item.radius*2,item.rotationDegrees);
        }
        public void SetOrderServing(int slot,bool value){serving[slot]=value;if(orderNodes[slot])orderNodes[slot].gameObject.SetActive(!value);}
        private void SetReplacementSettling(int slot,float progress)
        {
            if(slot<0||slot>=orderNodes.Length||!orderNodes[slot])return;
            float t=Mathf.Clamp01(progress),ease=1-Mathf.Pow(1-t,3);
            float settle=Mathf.Sin(t*Mathf.PI)*4;
            var node=orderNodes[slot];
            node.anchoredPosition=new Vector2(6+slot*103,-72+70*(1-ease)-settle);
            node.localScale=Vector3.one*Mathf.Lerp(.86f,1,ease);
            node.gameObject.SetActive(!serving[slot]);
        }
        public RectTransform CreateServingPot(Transform parent,int slot,int foodId)
        {
            var node=Node(parent,"ServingWholePot_"+slot,new Rect(6+slot*103,72,100,140));
            Picture(node,potBody?potBody:plate,new Rect(-14,30,128,128),"WholePotBody");
            if(potBroth)Picture(node,potBroth,new Rect(-14,30,128,128),"WholePotBroth");
            if(potRim)Picture(node,potRim,new Rect(-14,30,128,128),"WholePotRim");
            Panel(node,"OrderTag",new Rect(3,0,94,38),Cream).GetComponent<Image>().raycastTarget=false;
            Food(node,foodId,new Rect(6,2,34,34));Label(node,"3/3",new Rect(40,0,55,38),22);
            return node;
        }
        void ChooseReward(RewardKind kind)
        {
            if(art!=null){ChooseRewardV7(kind);return;}
            ShowDialog("领取道具 · 开发模拟","三种道具共用每天3次分享额度。",new[]{"模拟激励","模拟分享","取消"},index=>{CloseModal();if(index<2)RewardRequested?.Invoke(kind,index==0?RewardRoute.SimulatedAd:RewardRoute.SimulatedShare);});
        }
        public Task<RewardOutcome> ShowRewardSimulationAsync(RewardRequest request)
        {
            if(art!=null)return RewardSimulationV7(request);
            simulation?.TrySetResult(RewardOutcome.Cancelled);simulation=new TaskCompletionSource<RewardOutcome>();var pendingSimulation=simulation;
            ShowDialog(request.Route==RewardRoute.SimulatedAd?"模拟激励视频":"模拟分享","仅用于Unity开发验证，不连接真实广告或分享服务。",new[]{"模拟成功","取消","模拟失败"},i=>{CloseModal();pendingSimulation.TrySetResult(i==0?RewardOutcome.Success:i==1?RewardOutcome.Cancelled:RewardOutcome.Failed);});
            return pendingSimulation.Task;
        }
        public Task<RewardOutcome> ShowThemeShareAsync(string address)
        {
            var completion=new TaskCompletionSource<RewardOutcome>();
            ShowDialog("奖励分享 · 开发模拟","不会发送至外部平台。",new[]{"关闭"},i=>{CloseModal();completion.TrySetResult(RewardOutcome.Cancelled);});
            return completion.Task;
        }
        public void ShowNotice(string title,string message)=>ShowDialog(title,message,new[]{"知道了"},i=>{CloseModal();if(LastSnapshot.phase!=ViewPhase.Running && LastSnapshot.phase!=ViewPhase.Entry)Overlay();});
        public void ShowSettings(PlayerSettings settings,Action<PlayerSettings> save)
        {
            if(art!=null){SettingsV7(settings,save);return;}
            ShowDialog("音乐与音效","正式音频尚未交付时保持静音；设置保存在本机。",new[]{"完成"},i=>{save(settings);CloseModal();if(LastSnapshot.phase==ViewPhase.Paused)Overlay();});
            float w=viewport.width,y=viewport.height*.4f;
            Button(modal,"音乐开关",new Rect(30,y,w-60,38),()=>{settings.MusicEnabled=!settings.MusicEnabled;save(settings);ShowSettings(settings,save);});
            Label(modal,settings.MusicEnabled?"音乐：开":"音乐：关",new Rect(30,y+40,w-60,24),16);
            Volume(modal,new Rect(45,y+75,w-90,22),settings.MusicVolume,v=>{settings.MusicVolume=v;save(settings);});
            Button(modal,"音效开关",new Rect(30,y+115,w-60,38),()=>{settings.EffectsEnabled=!settings.EffectsEnabled;save(settings);ShowSettings(settings,save);});
            Label(modal,settings.EffectsEnabled?"音效：开":"音效：关",new Rect(30,y+155,w-60,24),16);
            Volume(modal,new Rect(45,y+190,w-90,22),settings.EffectsVolume,v=>{settings.EffectsVolume=v;save(settings);});
        }
        void ShowDialog(string title,string message,string[] buttons,Action<int> select)
        {
            if(art!=null){DialogV7(title,message,buttons,select);return;}
            CloseModal();modal=Panel(content,"Dialog",new Rect(0,0,viewport.width,viewport.height),Cream);
            Label(modal,title,new Rect(20,viewport.height*.16f,viewport.width-40,60),24);
            Label(modal,message,new Rect(25,viewport.height*.28f,viewport.width-50,85),17);
            for(int i=0;i<buttons.Length;i++){int index=i;Button(modal,buttons[i],new Rect(35,viewport.height-70-(buttons.Length-i-1)*60,viewport.width-70,46),()=>select(index));}
        }
        void CloseModal(){if(modal){modal.gameObject.SetActive(false);Destroy(modal.gameObject);modal=null;}}
        static void ClearChildren(Transform parent){foreach(Transform child in parent){child.gameObject.SetActive(false);Destroy(child.gameObject);}}
        void Volume(Transform parent,Rect rect,float value,Action<float> changed)
        {
            if(art!=null){VolumeV7(parent,rect,value,changed);return;}
            var node=Panel(parent,"Volume",rect,Green);var slider=node.gameObject.AddComponent<Slider>();
            var handle=Panel(node,"Handle",new Rect(0,-4,22,30),Coral);slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();slider.direction=Slider.Direction.LeftToRight;slider.minValue=0;slider.maxValue=1;slider.value=value;slider.onValueChanged.AddListener(v=>changed(v));
        }
        private static RectTransform Node(Transform parent,string name,Rect rect)
        { var node=new GameObject(name,typeof(RectTransform)); node.transform.SetParent(parent,false); var rt=(RectTransform)node.transform; Place(rt,rect); return rt; }
        private static void Place(RectTransform rt,Rect r)
        { rt.anchorMin=rt.anchorMax=new Vector2(0,1); rt.pivot=new Vector2(0,1); rt.anchoredPosition=new Vector2(r.x,-r.y); rt.sizeDelta=r.size; }
        private RectTransform Panel(Transform parent,string name,Rect r,Color color)
        {
            if(art!=null)return Skin(parent,name,r,name=="OrderTag"?"ui.order_card":"ui.panel");
            var rt=Node(parent,name,r);var im=rt.gameObject.AddComponent<Image>();
            string key=name.StartsWith("Action_")?(r.width==r.height?"button_round":"button"):name=="OrderTag"?"order_plate":name=="TimerPlate"?"timer_plate":"panel";
            Sprite sprite;bool formal=uiSprites.TryGetValue(key,out sprite);im.sprite=formal?sprite:rounded;im.type=key=="button_round"?Image.Type.Simple:Image.Type.Sliced;
            if(formal)im.pixelsPerUnitMultiplier=Mathf.Max(.1f,Mathf.Max(sprite.rect.width/Mathf.Max(1,r.width),sprite.rect.height/Mathf.Max(1,r.height)));
            im.color=formal?Color.white:color;return rt;
        }
        private Text Label(Transform parent,string text,Rect r,int size)
        {
            // Rasterize above the largest supported board scale, then downsample.
            // Logical layout stays unchanged while dynamic glyphs remain crisp.
            var rt=Node(parent,"Label",r);rt.sizeDelta=r.size*4;rt.localScale=Vector3.one*.25f;
            var label=rt.gameObject.AddComponent<Text>();label.font=playerFont;label.text=text;label.fontSize=size*4;label.color=art!=null?ThemeInk:Ink;label.alignment=TextAnchor.MiddleCenter;label.verticalOverflow=VerticalWrapMode.Overflow;label.raycastTarget=false;return label;
        }
        private void Button(Transform parent,string text,Rect r,UnityEngine.Events.UnityAction action)
        {
            if(art!=null){VButton(parent,text,r,action);return;}
            var rt=Panel(parent,"Action_"+text,r,Coral);var button=rt.gameObject.AddComponent<Button>();button.targetGraphic=rt.GetComponent<Image>();button.onClick.AddListener(action);
            string icon=text=="暂停"?"pause":text=="提示"?"hint":text=="清空暂存"?"clear":text=="打乱"?"shuffle":text=="设置"?"settings":text=="好友榜"?"leaderboard":text=="退出"?"home":text=="重新挑战"?"retry":text.Contains("分享")?"share":text.Contains("音乐")?"music":text.Contains("音效")?"sound":text=="继续"||text=="开始下火锅"?"play":text=="模拟激励"||text=="提前开锅"?"ad":text=="取消"||text=="关闭"?"close":null;
            bool round=r.width==r.height&&!string.IsNullOrEmpty(assetRoot);
            if(!round&&r.width<120)icon=null;
            float side=round?r.width*.65f:Mathf.Min(25,r.height*.55f);
            if(icon!=null)Icon(rt,icon,new Rect(round?(r.width-side)/2:12,(r.height-side)/2,side,side));
            if(!round){float inset=icon!=null&&!string.IsNullOrEmpty(assetRoot)?40:0;var label=Label(rt,text,new Rect(inset,0,r.width-inset,r.height),Mathf.Min(22,(int)(r.height*.5f)));label.color=Cream;}
        }
        private void Icon(Transform parent,string id,Rect rect)
        {if(art!=null){var t=art.Texture(id.StartsWith("../ui/")?"ui."+id.Substring(6):"icon."+id);if(t)Picture(parent,t,rect,"Icon_"+id);return;}if(string.IsNullOrEmpty(assetRoot))return;string path=id.StartsWith("../")?assetRoot+"/"+id.Substring(3):assetRoot+"/icons/"+id;var texture=PresentationAssets.Load<Texture2D>(path);if(texture)Picture(parent,texture,rect,"Icon_"+id);}
        private static void Picture(Transform parent,Texture texture,Rect r,string name,bool raycast=false)
        { var rt=Node(parent,name,r); var image=rt.gameObject.AddComponent<RawImage>(); image.texture=texture; image.raycastTarget=raycast; }
        private void Food(Transform parent,int id,Rect r)
        {
            if(id<0||id>=foods.Length)return;
            var rt=Node(parent,"Food_"+id,r);var image=rt.gameObject.AddComponent<RawImage>();image.texture=foods[id];image.raycastTarget=false;
            if(art!=null)image.uvRect=art.FoodUv(id);
        }
        private Rect ItemUV(ViewItem item)=>!string.IsNullOrEmpty(item.layoutVersion)
            ?new Rect(item.uvX,item.uvY,item.uvWidth,item.uvHeight)
            :art!=null?art.FoodUv(item.foodId):new Rect(0,0,1,1);
        private void PlateFood(Transform parent,ViewItem item)
        {
            if(item.foodId<0||item.foodId>=foods.Length)return;
            var rt=Node(parent,"PlateFood_"+item.itemId,new Rect(item.x-item.radius,item.y-item.radius,item.radius*2,item.radius*2));
            rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=new Vector2(item.x,-item.y);
            rt.localRotation=Quaternion.Euler(0,0,-item.rotationDegrees);
            var image=rt.gameObject.AddComponent<RawImage>();image.texture=foods[item.foodId];image.uvRect=ItemUV(item);image.raycastTarget=false;
        }
        private void OnDestroy()
        { CloseFriendBoardView();ReleaseAssetReferences();simulation?.TrySetResult(RewardOutcome.Cancelled);if(port!=null)port.Updated-=Apply;if(rounded)Destroy(rounded); if(roundedTexture)Destroy(roundedTexture); }
        public void ReleaseAssetReferences()
        {
            CancelShuffleFeedback();
            feedback?.ReleaseAssetReferences();World?.Clear();
            foreach(var image in GetComponentsInChildren<RawImage>(true))image.texture=null;
            foreach(var image in GetComponentsInChildren<Image>(true))image.sprite=null;
            art?.Dispose();art=null;Array.Clear(foods,0,foods.Length);plate=dish=table=potBody=potUnlit=potBroth=potRim=null;uiSprites.Clear();
        }
    }
}
