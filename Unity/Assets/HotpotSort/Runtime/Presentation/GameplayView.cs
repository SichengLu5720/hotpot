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
    public enum GameplayHapticKind { Light, Medium }

    /// <summary>Mount using Bind from the platform/session bootstrap. No automatic core or daily seed creation.</summary>
    public sealed partial class GameplayView : MonoBehaviour
    {
        sealed class PressedItem
        {
            public int pointerId,itemSibling,plateSibling;
            public string itemId;
            public Vector2 startScreen,hitOffset;
            public Vector3 originalScale;
            public float age;
            public RectTransform item,plate;
        }

        const float PressScaleSeconds=.08f,PressedScale=1.5f,PressSlopBoardUnits=14f;
        const float BufferLandingScaleSeconds=.14f;
        public MonoBehaviour portComponent;
        public Font playerFont;
        private Font displayFont;
        public event Action<ViewAction> ActionRequested;
        public event Action<RewardKind,RewardRoute> RewardRequested;
        public event Action SettingsRequested,FriendsRequested,ShareRequested;
        public event Action<GameplayHapticKind> HapticRequested;
        public event Action EntryStartHapticRequested;
        public event Action ButtonHapticRequested;
        public bool CanPlayButtonHaptic=>isActiveAndEnabled&&foreground&&canvasRoot&&canvasRoot.gameObject.activeInHierarchy;
        public void NotifyButtonAccepted()
        {
            if(!CanPlayButtonHaptic)return;
            double now=Time.realtimeSinceStartupAsDouble;
            if(now-lastLightHapticAt<.12)return;
            lastLightHapticAt=now;ButtonHapticRequested?.Invoke();
        }
        void BindButtonClick(Button button,UnityEngine.Events.UnityAction action,bool acceptedStart=false)
        {
            button.onClick.AddListener(()=>
            {
                if(!button||!button.isActiveAndEnabled||!button.IsInteractable()||!CanPlayButtonHaptic)return;
                if(WarmupControlsLocked&&(!warmupOverlay||!button.transform.IsChildOf(warmupOverlay)))return;
                if(!acceptedStart)NotifyButtonAccepted();
                action();
            });
        }
        public GameplayAudio Audio=>feedback?feedback.Audio:null;
        public bool CanPlayEntryHaptic=>isActiveAndEnabled&&PresentationForeground&&LastSnapshot?.phase==ViewPhase.Entry&&!(modal&&modal.gameObject.activeInHierarchy);
        public void NotifyEntryStartAccepted()
        {
            if(!CanPlayEntryHaptic)return;
            double now=Time.realtimeSinceStartupAsDouble;
            if(now-lastLightHapticAt<.12)return;
            lastLightHapticAt=now;EntryStartHapticRequested?.Invoke();
        }
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
        private readonly Dictionary<string,RectTransform> itemVisuals = new Dictionary<string,RectTransform>();
        private readonly RectTransform[] bufferFoodNodes=new RectTransform[5];
        private readonly float[] bufferLandingAges=new float[5];
        private readonly bool[] bufferLandingActive=new bool[5];
        private readonly Dictionary<PlatePresentationWorld.Remnant,RectTransform> ghostVisuals = new Dictionary<PlatePresentationWorld.Remnant,RectTransform>();
        private PressedItem pressedItem;
        private long hapticEpoch;
        private double lastLightHapticAt=double.NegativeInfinity;
        public long HapticEpoch=>hapticEpoch;
        public bool CanPlayHaptic=>isActiveAndEnabled&&PresentationForeground&&LastSnapshot!=null&&LastSnapshot.phase==ViewPhase.Running&&LastSnapshot.pauseReasons==ViewPauseReasons.None&&!(modal&&modal.gameObject.activeInHierarchy);
        private void CancelInteractionFeedback()
        {
            CancelScreenPress();hapticEpoch++;
            for(int i=0;i<bufferLandingActive.Length;i++){bufferLandingActive[i]=false;if(bufferFoodNodes[i])bufferFoodNodes[i].localScale=Vector3.one;}
        }
        private void OnDisable(){CancelInteractionFeedback();}
        private bool settlementAppFocused=true,settlementAppPaused;
        private void OnApplicationFocus(bool focused){settlementAppFocused=focused;if(!focused)CancelInteractionFeedback();}
        private void OnApplicationPause(bool paused){settlementAppPaused=paused;if(paused)CancelInteractionFeedback();}
        private float settlementElapsed,settlementTarget,settlementBarWidth;
        private bool settlementStarted;
        private RectTransform settlementFill,settlementMarker;
        private Text settlementPercent;
        private System.Action settlementShowButtons;
        private bool IsSettlement=>LastSnapshot!=null&&(LastSnapshot.phase==ViewPhase.Won||LastSnapshot.phase==ViewPhase.Overflow)&&!LastSnapshot.revivalPending;
        private void CancelSettlement()
        {settlementStarted=false;settlementElapsed=0;settlementFill=null;settlementMarker=null;settlementPercent=null;settlementShowButtons=null;}
        private void PrepareSettlement(ViewSnapshot next)
        {
            bool terminal=(next.phase==ViewPhase.Won||next.phase==ViewPhase.Overflow)&&!next.revivalPending;
            bool same=LastSnapshot!=null&&LastSnapshot.sessionId==next.sessionId&&LastSnapshot.sessionGeneration==next.sessionGeneration&&LastSnapshot.phase==next.phase;
            if(!terminal||!same)CancelSettlement();
            if(terminal&&!settlementStarted)
            {
                settlementStarted=true;settlementTarget=next.phase==ViewPhase.Won?1:Mathf.Clamp01(next.totalOrders>0?(float)next.completedOrders/next.totalOrders:0);
                renderedPhase=(ViewPhase)(-1);
            }
        }
        private void BuildSettlementProgress(RectTransform parent,Rect area)
        {
            var progress=Node(parent,"SettlementProgress",area);
            var caption=Label(progress,"本局进度",new Rect(0,0,area.width-90,32),19);caption.alignment=TextAnchor.MiddleLeft;caption.color=Cream;
            settlementPercent=Label(progress,"0%",new Rect(area.width-90,-3,90,36),28);settlementPercent.alignment=TextAnchor.MiddleRight;settlementPercent.color=Cream;
            var track=PotCapsule(progress,"ProgressTrack",new Rect(0,70,area.width,16),new Color32(216,196,163,255));
            settlementBarWidth=area.width-4;
            EnsureFireGradient();
            settlementFill=Node(track,"ProgressFill",new Rect(2,2,settlementBarWidth,12));
            var fill=settlementFill.gameObject.AddComponent<Image>();
            fill.sprite=fireGradient;fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.fillOrigin=0;fill.raycastTarget=false;
            settlementMarker=Node(progress,"ProgressHotpot",new Rect(0,52,52,52));
            settlementMarker.pivot=new Vector2(.5f,1);
            Picture(settlementMarker,potBody,new Rect(0,0,52,52),"PotBody");
            Picture(settlementMarker,potBroth,new Rect(0,0,52,52),"PotBroth");
            Picture(settlementMarker,potRim,new Rect(0,0,52,52),"PotRim");
            PaintSettlement();
        }
        private void PaintSettlement()
        {
            float t=Mathf.Clamp01(settlementElapsed/1.5f),value=settlementTarget*t*t*(3-2*t);
            if(settlementFill){settlementFill.GetComponent<Image>().fillAmount=value;settlementFill.gameObject.SetActive(value>0);}
            if(settlementPercent)settlementPercent.text=Mathf.FloorToInt(value*100+.5f)+"%";
            if(settlementMarker&&settlementFill)
            {
                var track=(RectTransform)settlementFill.parent;
                float left=track.anchoredPosition.x, right=left+track.rect.width;
                float half=settlementMarker.rect.width*.5f;
                float front=left+settlementFill.anchoredPosition.x+settlementFill.rect.width*value;
                settlementMarker.anchoredPosition=new Vector2(Mathf.Clamp(front,left+half,right-half),settlementMarker.anchoredPosition.y);
            }
        }
        private void SettlementButtons(System.Action create)
        {if(!IsSettlement||settlementElapsed>=1.5f)create();else settlementShowButtons=create;}
        private void TickSettlement(float delta)
        {
            if(!settlementStarted||!IsSettlement||settlementElapsed>=1.5f||!foreground||!settlementAppFocused||settlementAppPaused||!canvasRoot.gameObject.activeInHierarchy)return;
            settlementElapsed=Mathf.Min(1.5f,settlementElapsed+Mathf.Max(0,delta));PaintSettlement();
            if(settlementElapsed>=1.5f){var create=settlementShowButtons;settlementShowButtons=null;create?.Invoke();}
        }
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
            CancelWarmupPresentation();
            CancelSettlement();renderedPhase=(ViewPhase)(-1);
            CancelInteractionFeedback();
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
        public void SetForeground(bool value) { if(!value)CancelInteractionFeedback();foreground = value;Audio?.SetForeground(value); UpdateSimulation(); }
        public void Hide(bool hidden) { if(hidden)CancelInteractionFeedback();canvasRoot.gameObject.SetActive(!hidden); UpdateSimulation(); }
        private void UpdateSimulation() { World.SetSimulating(foreground && canvasRoot.gameObject.activeInHierarchy && !FriendBoardVisible && LastSnapshot.phase==ViewPhase.Running && LastSnapshot.pauseReasons==ViewPauseReasons.None); }
        public void ResetView() { CancelWarmupPresentation();CancelScreenPress();CancelShuffleFeedback(); simulation?.TrySetResult(RewardOutcome.Cancelled);simulation=null;pending.Clear();Array.Clear(serving,0,4); LastEventSequence=0; LastTransactionId=null; feedback.ResetFeedback(); World.Clear(); LastSnapshot=new ViewSnapshot { phase=ViewPhase.Entry }; Render(); }
        public void ShowError(string message)
        { CancelWarmupPresentation();CancelScreenPress();CancelShuffleFeedback(); feedback.ResetFeedback(); LastSnapshot = new ViewSnapshot { sessionId=LastSnapshot?.sessionId, phase=ViewPhase.Aborted, message=message }; World.Clear(); Render(); }
        public void DestroyView() { Destroy(gameObject); }
        public void Apply(ViewUpdate update)
        {
            if (update == null || update.snapshot == null) return;
            var s = update.snapshot;
            if (LastSnapshot != null && LastSnapshot.sessionId == s.sessionId && s.revision < LastSnapshot.revision) return;
            if(LastSnapshot==null||LastSnapshot.sessionId!=s.sessionId||LastSnapshot.sessionGeneration!=s.sessionGeneration||s.phase!=ViewPhase.Running||s.pauseReasons!=ViewPauseReasons.None)CancelInteractionFeedback();
            if (LastSnapshot == null || LastSnapshot.sessionId != s.sessionId || LastSnapshot.sessionGeneration!=s.sessionGeneration) { CancelWarmupPresentation();CancelScreenPress();simulation?.TrySetResult(RewardOutcome.Cancelled);simulation=null;pending.Clear(); LastEventSequence=0; }
            var previous = LastSnapshot;
            PrepareSettlement(s);
            if (ShuffleFeedbackActive && (s.sessionId != shuffleSession || s.sessionGeneration != shuffleGeneration ||
                s.phase == ViewPhase.Entry || s.phase == ViewPhase.Aborted || s.phase == ViewPhase.Overflow || s.phase == ViewPhase.Won)) CancelShuffleFeedback();
            var sourcePositions=new Dictionary<string,Vector2>();
            foreach(var body in World.Bodies)foreach(var item in body.data.items)sourcePositions[item.itemId]=World.ItemPosition(item.itemId);
            foreach (var e in update.events ?? new ViewEvent[0]) if (e.sequence > LastEventSequence)
            { LastEventSequence=e.sequence; LastTransactionId=e.transactionId; }
            LastEventSequence = Math.Max(LastEventSequence,s.eventSeq);
            // Final authoritative snapshot is the convergence point, including gaps/reconnects/long chains.
            LastSnapshot = s; pending.Clear(); UpdateSimulation(); World.Reconcile(s); Render();
            if(pressedItem!=null&&(s.phase!=ViewPhase.Running||s.pauseReasons!=ViewPauseReasons.None||!IsItemClickable(pressedItem.itemId)))CancelScreenPress();
            feedback.Apply(update, previous, id=>sourcePositions.TryGetValue(id,out var value)?value:World.ItemPosition(id));
        }
        private void LateUpdate()
        {
            TickSettlement(Time.unscaledDeltaTime);
            TickClickabilityCache();
            TickShuffleFeedback(Time.unscaledDeltaTime);
            TickPressAndLanding(Time.unscaledDeltaTime);
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
            TickWarmupPresentation(Time.unscaledDeltaTime);
        }
        private void Update()
        {
            if(art!=null&&Application.platform==RuntimePlatform.WindowsPlayer&&viewportScreenHeight<=0&&viewportAppliedAt!=new Vector2Int(Screen.width,Screen.height))
            {viewportAppliedAt=new Vector2Int(Screen.width,Screen.height);viewport=Screen.safeArea;Render();}
            if (!externalViewport && viewport != Screen.safeArea) { viewport=Screen.safeArea; Render(); }
            if (UsesExternalTapInput || !foreground || !canvasRoot.gameObject.activeInHierarchy || LastSnapshot.phase != ViewPhase.Running || port == null) return;
            if (Input.touchCount>0)
            {
                var t=Input.GetTouch(0);
                if(t.phase==TouchPhase.Began)BeginScreenPress(t.position,t.fingerId);
                else if(t.phase==TouchPhase.Moved||t.phase==TouchPhase.Stationary)UpdateScreenPress(t.position,t.fingerId);
                else if(t.phase==TouchPhase.Ended)EndScreenPress(t.position,t.fingerId);
                else if(t.phase==TouchPhase.Canceled)CancelScreenPress(t.fingerId);
            }
            else
            {
                if(Input.GetMouseButtonDown(0))BeginScreenPress(Input.mousePosition,-1);
                else if(Input.GetMouseButton(0))UpdateScreenPress(Input.mousePosition,-1);
                if(Input.GetMouseButtonUp(0))EndScreenPress(Input.mousePosition,-1);
            }
        }
        public bool SubmitScreenTap(Vector2 screen, int pointerId = -1)
        {
            if(TryConfirmWarmupPrompt())return true;
            Vector2 point;string id;
            if(!TryScreenHit(screen,pointerId,out point,out id))return false;
            RequestHaptic(GameplayHapticKind.Light);
            Audio?.Play(AudioCue.FoodPress);
            return DispatchTap(id,point);
        }
        public bool BeginScreenPress(Vector2 screen,int pointerId=-1)
        {
            if(TryConfirmWarmupPrompt())return true;
            if(pressedItem!=null)return false;
            Vector2 point;string id;
            if(!TryScreenHit(screen,pointerId,out point,out id))return false;
            RectTransform item;if(!itemVisuals.TryGetValue(id,out item)||!item)return false;
            var plateNode=item.parent as RectTransform;if(!plateNode)return false;
            pressedItem=new PressedItem{pointerId=pointerId,itemId=id,startScreen=screen,hitOffset=point-World.ItemPosition(id),item=item,plate=plateNode,originalScale=item.localScale,itemSibling=item.GetSiblingIndex(),plateSibling=plateNode.GetSiblingIndex()};
            item.SetAsLastSibling();plateNode.SetAsLastSibling();
            RequestHaptic(GameplayHapticKind.Light);
            Audio?.Play(AudioCue.FoodPress);
            return true;
        }
        public bool UpdateScreenPress(Vector2 screen,int pointerId=-1)
        {
            if(pressedItem==null||pressedItem.pointerId!=pointerId)return false;
            Vector2 local,start;
            if(!CanPlayHaptic||ShuffleFeedbackActive||!board||!RectTransformUtility.ScreenPointToLocalPointInRectangle(board,screen,null,out local)||!RectTransformUtility.ScreenPointToLocalPointInRectangle(board,pressedItem.startScreen,null,out start)||Vector2.Distance(start,local)>PressSlopBoardUnits||!viewport.Contains(screen)||!PlateCrop.Contains(new Vector2(local.x,-local.y))||ScreenBlockedByUI(screen,pointerId))
            {CancelScreenPress(pointerId);return false;}
            return true;
        }
        public bool EndScreenPress(Vector2 screen,int pointerId=-1)
        {
            if(!UpdateScreenPress(screen,pointerId)||pressedItem==null)return false;
            var press=pressedItem;pressedItem=null;
            RestorePressedVisual(press);
            if(!ValidateReleasedFood(press))return false;
            return DispatchTap(press.itemId,World.ItemPosition(press.itemId));
        }
        public void CancelScreenPress(int pointerId=-1)
        {
            if(pressedItem==null||(pointerId!=-1&&pressedItem.pointerId!=pointerId))return;
            var press=pressedItem;pressedItem=null;RestorePressedVisual(press);
        }
        private bool TryScreenHit(Vector2 screen,int pointerId,out Vector2 point,out string id)
        {
            point=Vector2.zero;id=null;
            if (ShuffleFeedbackActive||port == null || !CanPlayHaptic || !viewport.Contains(screen)||ScreenBlockedByUI(screen,pointerId)) return false;
            Vector2 local;if(!board || !RectTransformUtility.ScreenPointToLocalPointInRectangle(board,screen,null,out local))return false;
            point = new Vector2(local.x,-local.y);
            if (!PlateCrop.Contains(point)) return false;
            id=World.Hit(point,OpaqueHit);return id!=null&&!pending.Contains(id)&&TutorialAllowsFood(id);
        }
        private bool DispatchTap(string id,Vector2 point)
        {
            if(id==null||!TutorialAllowsFood(id)||!pending.Add(id))return false;
            if(hintItemId==id)ClearFoodPointer();
            try { port.Tap(new ViewTap { itemId=id,inputSeq=++inputSeq,snapshotRevision=LastSnapshot.revision,boardX=point.x,boardY=point.y }); }
            catch { pending.Remove(id); throw; }
            return true;
        }
        private bool ScreenBlockedByUI(Vector2 screen,int pointerId)
        {
            if(!EventSystem.current)return false;
            uiHits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=screen,pointerId=pointerId},uiHits);return uiHits.Count>0;
        }
        public void RequestHaptic(GameplayHapticKind kind)
        {
            if(!CanPlayHaptic)return;
            double now=Time.realtimeSinceStartupAsDouble;
            if(kind==GameplayHapticKind.Light&&now-lastLightHapticAt<.12)return;
            lastLightHapticAt=now;HapticRequested?.Invoke(kind);
        }
        public void PulseBufferArrival(int slot,string itemId)
        {
            if(!CanPlayHaptic||slot<0||slot>=bufferFoodNodes.Length||slot>=LastSnapshot.buffer.Length||LastSnapshot.buffer[slot]?.itemId!=itemId||!bufferFoodNodes[slot])return;
            bufferLandingAges[slot]=0;bufferLandingActive[slot]=true;bufferFoodNodes[slot].localScale=Vector3.one*.92f;
        }
        private void TickPressAndLanding(float delta)
        {
            if(!CanPlayHaptic){CancelInteractionFeedback();return;}
            if(pressedItem!=null)
            {
                if(!PresentationForeground||LastSnapshot==null||LastSnapshot.phase!=ViewPhase.Running||LastSnapshot.pauseReasons!=ViewPauseReasons.None||ShuffleFeedbackActive)CancelScreenPress();
                else
                {
                    if(itemVisuals.TryGetValue(pressedItem.itemId,out var replacement)&&replacement&&replacement!=pressedItem.item){RestorePressedVisual(pressedItem);pressedItem.item=replacement;pressedItem.plate=replacement.parent as RectTransform;pressedItem.originalScale=replacement.localScale;pressedItem.itemSibling=replacement.GetSiblingIndex();pressedItem.plateSibling=pressedItem.plate?pressedItem.plate.GetSiblingIndex():0;if(pressedItem.plate)pressedItem.plate.SetAsLastSibling();replacement.SetAsLastSibling();}
                    pressedItem.age+=Mathf.Clamp(delta,0,.1f);
                    if(pressedItem.item)pressedItem.item.localScale=pressedItem.originalScale*Mathf.Lerp(1,PressedScale,Mathf.SmoothStep(0,1,Mathf.Clamp01(pressedItem.age/PressScaleSeconds)));
                }
            }
            for(int slot=0;slot<bufferLandingActive.Length;slot++)
            {
                if(!bufferLandingActive[slot])continue;
                var node=bufferFoodNodes[slot];if(!node){bufferLandingActive[slot]=false;continue;}
                bufferLandingAges[slot]+=Mathf.Clamp(delta,0,.1f);float t=Mathf.Clamp01(bufferLandingAges[slot]/BufferLandingScaleSeconds);
                float scale=t<.43f?Mathf.Lerp(.92f,1.08f,Mathf.SmoothStep(0,1,t/.43f)):Mathf.Lerp(1.08f,1,Mathf.SmoothStep(0,1,(t-.43f)/.57f));node.localScale=Vector3.one*scale;
                if(t>=1){node.localScale=Vector3.one;bufferLandingActive[slot]=false;}
            }
        }
        private static void RestorePressedVisual(PressedItem press)
        {
            if(press==null)return;
            if(press.item&&press.item.parent){press.item.localScale=press.originalScale;press.item.SetSiblingIndex(Mathf.Clamp(press.itemSibling,0,Mathf.Max(0,press.item.parent.childCount-1)));}
            if(press.plate&&press.plate.parent)press.plate.SetSiblingIndex(Mathf.Clamp(press.plateSibling,0,Mathf.Max(0,press.plate.parent.childCount-1)));
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
        // Compatibility projection; unknown IDs are intentionally not claimed visible.
        public string[] CaptureClickableItemIds()
        {
            return CaptureClickability()?.clickable;
        }
        private void Action(ViewAction action) { if(action==ViewAction.Exit||action==ViewAction.RetrySameDay||action==ViewAction.StartToday)CancelSettlement();ActionRequested?.Invoke(action); port?.SessionAction(action); }
        private void Render()
        {
            if(!IsSettlement)CancelSettlement();
            Audio?.SetState(LastSnapshot);
            if(art!=null){RenderV7();if(warmupOverlay)warmupOverlay.SetAsLastSibling();return;}
            if (!content) return;
            if (viewport.width<=0 || viewport.height<=0) viewport=new Rect(0,0,Screen.width,Screen.height);
            Place(content,new Rect(viewport.x,(viewportScreenHeight>0?viewportScreenHeight:Screen.height)-viewport.yMax,viewport.width,viewport.height));
            if(table){float aspect=canvasRoot.rect.width/canvasRoot.rect.height,source=table.width/(float)table.height;float u=aspect<source?aspect/source:1,v=aspect>source?source/aspect:1;backgroundImage.uvRect=new Rect((1-u)/2,(1-v)/2,u,v);}
            bool entry=LastSnapshot.phase==ViewPhase.Entry;
            if(entry || !board)
            {
                ClearChildren(content);plateVisuals.Clear();itemVisuals.Clear();plateSignatures.Clear();ghostVisuals.Clear();Array.Clear(bufferFoodNodes,0,bufferFoodNodes.Length);Array.Clear(bufferLandingActive,0,bufferLandingActive.Length);board=null;
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
                ViewOrder order=feedback.DisplayOrder(slot,Array.Find(LastSnapshot.orders,o=>o.slot==slot));
                bool enabled=order!=null && order.enabled;
                RefreshFireProgress(slot);
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
                else {RewardPlus(box,"PotPlus",new Rect(37,75,26,26));if(slot>=2)LockedPotControls(box,slot);}
            }
            for(int slot=0;slot<5;slot++)
            {
                var bufferItem=slot<LastSnapshot.buffer.Length?LastSnapshot.buffer[slot]:null;
                string signature=bufferItem==null?"empty":bufferItem.itemId+":"+bufferItem.foodId;
                if(bufferSignatures[slot]==signature)continue;
                bufferSignatures[slot]=signature;var cell=bufferNodes[slot];ClearChildren(cell);bufferFoodNodes[slot]=null;bufferLandingActive[slot]=false;
                Picture(cell,dish?dish:plate,new Rect(0,0,64,64),"Dish");
                if(slot<LastSnapshot.buffer.Length && LastSnapshot.buffer[slot]!=null) bufferFoodNodes[slot]=Food(cell,LastSnapshot.buffer[slot].foodId,new Rect(5,5,51,51));
            }
            var alive=new HashSet<string>();var aliveItems=new HashSet<string>();
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
                    foreach(var item in p.items) PlateFood(node,item);
                }
                foreach(var item in p.items)aliveItems.Add(item.itemId);
            }
            foreach(string id in plateVisuals.Keys.ToArray())if(!alive.Contains(id)){Destroy(plateVisuals[id].gameObject);plateVisuals.Remove(id);plateSignatures.Remove(id);}
            foreach(string id in itemVisuals.Keys.ToArray())if(!aliveItems.Contains(id))itemVisuals.Remove(id);
            foreach(var pair in ghostVisuals.ToArray())if(pair.Key.remaining<=0 || !World.Remnants.Contains(pair.Key)){if(pair.Value)Destroy(pair.Value.gameObject);ghostVisuals.Remove(pair.Key);}
            foreach(var ghost in World.Remnants)
            {
                if(ghostVisuals.ContainsKey(ghost))continue;
                var node=Node(plateLayer,"Clearing_"+ghost.plateId,new Rect(ghost.position.x-ghost.radius,ghost.position.y-ghost.radius,ghost.radius*2,ghost.radius*2));
                var image=node.gameObject.AddComponent<RawImage>(); image.texture=plate; image.raycastTarget=false;
                ghostVisuals.Add(ghost,node);
            }
            RefreshLockedPotDialog();
            feedback.SetBoard(board,plateLayer);
            hud.SetAsLastSibling();
            if(LastSnapshot.phase!=renderedPhase)
            {
                CloseModal();if(LastSnapshot.phase!=ViewPhase.Running&&LastSnapshot.pauseReasons!=ViewPauseReasons.Tutorial)Overlay();renderedPhase=LastSnapshot.phase;
            }
            if(warmupOverlay)warmupOverlay.SetAsLastSibling();
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
            if(IsSettlement){SettlementOverlay();return;}
            if(art!=null){OverlayV7();return;}
            modal=Panel(content,"FlowOverlay",new Rect(0,0,viewport.width,viewport.height),Cream);
            string title=LastSnapshot.phase==ViewPhase.Paused?"已暂停":LastSnapshot.phase==ViewPhase.Won?"挑战完成":LastSnapshot.phase==ViewPhase.Overflow?"失败":"挑战已中止";
            Label(modal,title,new Rect(15,viewport.height*.19f,viewport.width-30,48),28);
            float y=viewport.height*.30f;
            if(IsSettlement){BuildSettlementProgress(modal,new Rect(35,y,viewport.width-70,58));y+=76;}
            foreach(var fact in LastSnapshot.facts) { Label(modal,fact.label+"："+fact.value,new Rect(20,y,viewport.width-40,32),18); y+=34; }
            if(!string.IsNullOrEmpty(LastSnapshot.message)) Label(modal,LastSnapshot.message,new Rect(20,y,viewport.width-40,70),16);
            bool paused=LastSnapshot.phase==ViewPhase.Paused;
            SettlementButtons(()=>{
            Button(modal,paused?"继续":"重新挑战",new Rect(35,viewport.height-156,viewport.width-70,52),()=>Action(paused?ViewAction.Resume:ViewAction.RetrySameDay));
            Button(modal,"退出",new Rect(35,viewport.height-92,viewport.width-70,48),()=>Action(ViewAction.Exit));
            Button(modal,paused?"设置":"分享主题图",new Rect(35,viewport.height-218,viewport.width-70,46),()=>{if(paused)SettingsRequested?.Invoke();else ShareRequested?.Invoke();});
            });
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
            timerPlate.gameObject.SetActive(!LastSnapshot.isWarmup);timerLabel.gameObject.SetActive(!LastSnapshot.isWarmup);
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
            ClearFoodPointer();hintItemId=id;
        }
        RectTransform warmupOverlay,guideHand,guideFood,spotlightFood;
        Vector3 guideFoodScale;
        string hintItemId,guideItemId,warmupOverlayKey;
        float guideAge,warmupMessageAge;
        bool warmupMessageStarted,warmupCompletionSent;
        IWarmupPresentationPort WarmupPort=>port as IWarmupPresentationPort;
        bool WarmupControlsLocked=>LastSnapshot!=null&&(LastSnapshot.warmupComplete||LastSnapshot.bufferWarning||LastSnapshot.tutorialStep!=ViewTutorialStep.None);
        bool TutorialAllowsFood(string id)=>LastSnapshot!=null&&!LastSnapshot.warmupComplete&&!LastSnapshot.bufferWarning&&
            (LastSnapshot.tutorialStep==ViewTutorialStep.None||(LastSnapshot.tutorialStep==ViewTutorialStep.SelectFood&&LastSnapshot.tutorialItemId==id));
        void ClearFoodPointer()
        {
            if(guideFood&&pressedItem?.item!=guideFood)guideFood.localScale=guideFoodScale;
            guideFood=null;guideItemId=null;hintItemId=null;
            if(guideHand){guideHand.gameObject.SetActive(false);Destroy(guideHand.gameObject);guideHand=null;}
        }
        void ClearWarmupOverlay()
        {
            if(warmupOverlay){warmupOverlay.gameObject.SetActive(false);Destroy(warmupOverlay.gameObject);warmupOverlay=null;}
            warmupOverlayKey=null;
            spotlightFood=null;
        }
        void CancelWarmupPresentation()
        {
            ClearFoodPointer();ClearWarmupOverlay();guideAge=0;warmupMessageAge=0;warmupMessageStarted=false;warmupCompletionSent=false;
        }
        // Called only by the real flight-arrival queue; never by an estimated timer.
        public void NotifyTutorialFoodArrived(string itemId)
        {
            var s=LastSnapshot;
            if(s!=null&&s.tutorialStep==ViewTutorialStep.FoodInFlight&&s.tutorialItemId==itemId)
                WarmupPort?.TutorialFoodArrived(s.sessionId,s.sessionGeneration,itemId);
        }
        RectTransform CreateGuideHand(Transform parent)
        {
            var hand=Node(parent,"TutorialHand",new Rect(0,0,46,61));
            // Rounded UI geometry keeps the pointer legible without a font glyph or new bitmap.
            HandPart(hand,"PalmOutline",new Rect(12,25,30,28),Ink);
            HandPart(hand,"FingerOutline",new Rect(13,0,12,38),Ink);
            HandPart(hand,"ThumbOutline",new Rect(5,31,20,19),Ink);
            HandPart(hand,"Palm",new Rect(14,27,26,24),Cream);
            HandPart(hand,"Index",new Rect(15,2,8,36),Cream);
            HandPart(hand,"Thumb",new Rect(7,33,18,15),Cream);
            HandPart(hand,"Middle",new Rect(24,21,6,18),Cream);
            HandPart(hand,"Ring",new Rect(30,24,6,16),Cream);
            HandPart(hand,"Cuff",new Rect(16,49,23,9),Coral);
            hand.localRotation=Quaternion.Euler(0,0,18);
            return hand;
        }
        void HandPart(Transform parent,string name,Rect rect,Color color,float angle=0)
        {
            var part=Node(parent,name,rect);var image=part.gameObject.AddComponent<Image>();
            image.sprite=rounded;image.type=Image.Type.Sliced;image.color=color;image.raycastTarget=false;
            part.localRotation=Quaternion.Euler(0,0,angle);
        }
        void EnsureWarmupOverlay(string key,string message,bool confirm,bool block)
        {
            if(warmupOverlay&&warmupOverlayKey==key){warmupOverlay.SetAsLastSibling();return;}
            ClearWarmupOverlay();warmupOverlayKey=key;
            warmupOverlay=Node(board,"WarmupPresentation_"+key,new Rect(0,0,420,900));
            if(key=="select")AddTutorialShade(new Rect(-2000,-2000,4420,4900));
            else if(key=="order"||key=="warning")
            {
                Rect hole=key=="order"?new Rect(9+Mathf.Clamp(LastSnapshot.tutorialOrderSlot,0,3)*103,72,94,39):new Rect(40,225,338,68);
                AddTutorialShade(new Rect(-2000,-2000,4420,hole.yMin+2000));
                AddTutorialShade(new Rect(-2000,hole.yMax,4420,2900-hole.yMax));
                AddTutorialShade(new Rect(-2000,hole.yMin,hole.xMin+2000,hole.height));
                AddTutorialShade(new Rect(hole.xMax,hole.yMin,2420-hole.xMax,hole.height));
            }
            if(block)
            {
                var shield=Node(warmupOverlay,"TutorialAnyTap",new Rect(-2000,-2000,4420,4900));
                var image=shield.gameObject.AddComponent<Image>();image.color=Color.clear;image.raycastTarget=true;
                if(confirm){var button=shield.gameObject.AddComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.None;BindButtonClick(button,()=>TryConfirmWarmupPrompt());}
            }
            if(string.IsNullOrEmpty(message))return;
            float top=key=="last"?382:key=="order"?170:key=="warning"?320:680;
            var label=Label(warmupOverlay,message,new Rect(25,top,370,70),key=="last"?32:22);
            label.name="TutorialFloatingText";label.color=Cream;
            var shadow=label.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.85f);shadow.effectDistance=new Vector2(3,-5);
            if(key=="order")label.text="集满 3 个相同食材，\n即可完成订单。";
        }
        void AddTutorialShade(Rect rect)
        {
            var node=Node(warmupOverlay,"TutorialShade",rect);var image=node.gameObject.AddComponent<Image>();
            image.color=new Color(0,0,0,.68f);image.raycastTarget=false;
        }
        bool TryConfirmWarmupPrompt()
        {
            var s=LastSnapshot;
            if(!warmupOverlay||!warmupOverlay.gameObject.activeInHierarchy||!PresentationForeground||!settlementAppFocused||settlementAppPaused||s==null||(s.pauseReasons&~ViewPauseReasons.Tutorial)!=0)return false;
            if(warmupOverlayKey=="warning"&&s.bufferWarning)return WarmupPort?.CompleteBufferWarning(s.sessionId,s.sessionGeneration)==true;
            if(warmupOverlayKey=="order"&&s.tutorialStep==ViewTutorialStep.OrderExplanation)return WarmupPort?.CompleteOpeningTutorial(s.sessionId,s.sessionGeneration)==true;
            return false;
        }
        void TickWarmupPresentation(float delta)
        {
            var s=LastSnapshot;
            if(s==null||s.phase==ViewPhase.Entry||s.phase==ViewPhase.Aborted||s.phase==ViewPhase.Won||s.phase==ViewPhase.Overflow)
            {CancelWarmupPresentation();return;}
            bool visible=PresentationForeground&&settlementAppFocused&&!settlementAppPaused&&(s.pauseReasons&~ViewPauseReasons.Tutorial)==0&&!(modal&&modal.gameObject.activeInHierarchy);
            if(warmupOverlay)warmupOverlay.gameObject.SetActive(visible);
            if(guideHand)guideHand.gameObject.SetActive(visible);
            if(!visible||!board)return;
            guideAge+=Mathf.Clamp(delta,0,.1f);
            if(s.warmupComplete)
            {
                ClearFoodPointer();
                if(!warmupMessageStarted)
                {
                    EnsureWarmupOverlay("waiting",null,false,true);
                    if(feedback.WarmupTransitionBusy||World.Bodies.Any()||LastSnapshot.buffer.Any(i=>i!=null))return;
                    // Empty-plate remnants are presentation-only; the last serve has finished.
                    foreach(var ghost in ghostVisuals.Values)if(ghost){ghost.gameObject.SetActive(false);Destroy(ghost.gameObject);}
                    ghostVisuals.Clear();World.Clear();
                    warmupMessageStarted=true;warmupMessageAge=0;
                    EnsureWarmupOverlay("last","最后一关！",false,true);
                    return;
                }
                if(warmupCompletionSent)return;
                warmupMessageAge+=Mathf.Clamp(delta,0,.1f);
                if(warmupMessageAge<.8f)return;
                // Hide the text and discard all old visual state before core starts formal supply.
                warmupCompletionSent=true;ClearWarmupOverlay();CancelInteractionFeedback();CancelShuffleFeedback();
                feedback.ResetFeedback();World.Clear();CloseModal();Array.Clear(serving,0,serving.Length);
                if(board){board.gameObject.SetActive(false);Destroy(board.gameObject);board=null;}
                plateVisuals.Clear();itemVisuals.Clear();plateSignatures.Clear();ghostVisuals.Clear();
                Array.Clear(orderSignatures,0,4);Array.Clear(bufferSignatures,0,5);Array.Clear(bufferFoodNodes,0,5);
                WarmupPort?.CompleteWarmup(s.sessionId,s.sessionGeneration);
                return;
            }
            if(s.bufferWarning)
            {ClearFoodPointer();EnsureWarmupOverlay("warning","暂存区放满会导致挑战失败。",true,true);return;}
            if(s.tutorialStep==ViewTutorialStep.OrderExplanation)
            {
                if(guideFood||hintItemId!=null)ClearFoodPointer();
                EnsureWarmupOverlay("order","集满 3 个相同食材，即可完成订单。",true,true);
                if(!guideHand)guideHand=CreateGuideHand(warmupOverlay);
                int slot=Mathf.Clamp(s.tutorialOrderSlot,0,3);
                Place(guideHand,new Rect(6+slot*103+36,106+Mathf.Sin(guideAge*5)*3,46,61));return;
            }
            if(s.tutorialStep==ViewTutorialStep.FoodInFlight)
            {ClearFoodPointer();EnsureWarmupOverlay("flight",null,false,true);return;}
            if(s.tutorialStep==ViewTutorialStep.SelectFood)
            {
                if(string.IsNullOrEmpty(s.tutorialItemId)||!IsItemClickable(s.tutorialItemId))
                {
                    ClearFoodPointer();ClearWarmupOverlay();
                    var candidate=FindClickableHint();if(candidate!=null)WarmupPort?.SelectTutorialFood(s.sessionId,s.sessionGeneration,candidate);
                    return;
                }
                EnsureWarmupOverlay("select","点击食材，放入火锅。",false,false);
                ShowFoodPointer(s.tutorialItemId);
                // Keep the short caption away from the pointed food.
                var caption=warmupOverlay.Find("TutorialFloatingText") as RectTransform;
                if(caption)caption.anchoredPosition=new Vector2(25,World.ItemPosition(s.tutorialItemId).y>590?-330:-680);
                return;
            }
            ClearWarmupOverlay();
            if(hintItemId!=null)
            {
                var item=s.plates.SelectMany(p=>p.items).FirstOrDefault(i=>i.itemId==hintItemId);
                if(item==null||!IsItemClickable(hintItemId)||!s.orders.Any(o=>o.enabled&&o.foodId==item.foodId&&o.count<o.required))ClearFoodPointer();
                else ShowFoodPointer(hintItemId);
            }
            else if(guideHand)ClearFoodPointer();
        }
        void ShowFoodPointer(string id)
        {
            if(!itemVisuals.TryGetValue(id,out var item)||!item)return;
            if(guideFood!=item)
            {
                string hint=hintItemId;ClearFoodPointer();hintItemId=hint;
                guideFood=item;guideFoodScale=item.localScale;guideItemId=id;
            }
            if(pressedItem?.item!=item)item.localScale=guideFoodScale*1.22f;
            bool tutorial=LastSnapshot.tutorialStep==ViewTutorialStep.SelectFood&&warmupOverlay;
            if(tutorial)
            {
                if(!spotlightFood)
                {
                    var clip=Node(warmupOverlay,"TutorialFoodClip",PlateCrop);clip.gameObject.AddComponent<RectMask2D>();
                    spotlightFood=Node(clip,"TutorialBrightFood",new Rect(0,0,1,1));spotlightFood.pivot=new Vector2(.5f,.5f);
                    var copy=spotlightFood.gameObject.AddComponent<RawImage>();copy.raycastTarget=false;
                }
                var source=item.GetComponent<RawImage>();var bright=spotlightFood.GetComponent<RawImage>();
                bright.texture=source.texture;bright.uvRect=source.uvRect;bright.color=source.color;
                var center=World.ItemPosition(id);spotlightFood.anchoredPosition=new Vector2(center.x,-center.y+PlateCrop.yMin);
                spotlightFood.sizeDelta=item.sizeDelta;spotlightFood.localScale=item.localScale;spotlightFood.localRotation=item.localRotation;
            }
            if(!guideHand)guideHand=CreateGuideHand(tutorial?warmupOverlay:plateLayer);
            if(tutorial&&guideHand.parent!=warmupOverlay)guideHand.SetParent(warmupOverlay,false);
            guideHand.SetAsLastSibling();
            Vector2 at=World.ItemPosition(id);
            float x=Mathf.Clamp(at.x-15,4,365),y=Mathf.Clamp(at.y+8+Mathf.Sin(guideAge*5)*3,PlateCrop.yMin+4,PlateCrop.yMax-64);
            Place(guideHand,new Rect(x,y,46,61));
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
            ShowDialog("普通分享 · 开发模拟","不会发送至外部平台。",new[]{"关闭"},i=>{CloseModal();if(IsSettlement)Overlay();completion.TrySetResult(RewardOutcome.Cancelled);});
            return completion.Task;
        }
        public void ShowNotice(string title,string message)=>ShowDialog(title,message,new[]{"知道了"},i=>{CloseModal();if(LastSnapshot.phase!=ViewPhase.Running && LastSnapshot.phase!=ViewPhase.Entry)Overlay();});
        public void ShowSettings(PlayerSettings settings,Action<PlayerSettings> save)
        {
            if(art!=null){SettingsV7(settings,save);Audio?.SetSettingsOpen(true);return;}
            ShowDialog("音乐与音效","音乐与音效可分别调节；设置保存在本机。",new[]{"完成"},i=>{save(settings);CloseModal();if(LastSnapshot.phase==ViewPhase.Paused)Overlay();});
            Audio?.SetSettingsOpen(true);
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
        void CloseModal(){settlementFill=null;settlementMarker=null;settlementPercent=null;settlementShowButtons=null;lockedPotDialogSlot=-1;lockedPotRemainingLabel=null;Audio?.SetSettingsOpen(false);if(modal){modal.gameObject.SetActive(false);Destroy(modal.gameObject);modal=null;}}
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
            var rt=Panel(parent,"Action_"+text,r,Coral);var button=rt.gameObject.AddComponent<Button>();button.targetGraphic=rt.GetComponent<Image>();BindButtonClick(button,action,text=="开始下火锅");
            string icon=text=="暂停"?"pause":text=="提示"?"hint":text=="清空暂存"?"clear":text=="打乱"?"shuffle":text=="设置"?"settings":text=="好友榜"?"leaderboard":text=="退出"?"home":text=="重新挑战"?"retry":text.Contains("分享")?"share":text.Contains("音乐")?"music":text.Contains("音效")?"sound":text=="继续"||text=="开始下火锅"?"play":text=="模拟激励"||text=="提前开锅"?"ad":text=="取消"||text=="关闭"?"close":null;
            bool round=r.width==r.height&&!string.IsNullOrEmpty(assetRoot);
            bool rewardTool=text=="提示"||text=="清空暂存"||text=="打乱";
            if(!round&&r.width<120)icon=null;
            float side=round?r.width*.65f:Mathf.Min(25,r.height*.55f);
            if(icon!=null)Icon(rt,icon,rewardTool?new Rect(12,24,30,30):new Rect(round?(r.width-side)/2:12,(r.height-side)/2,side,side));
            if(!round){float inset=icon!=null&&!string.IsNullOrEmpty(assetRoot)?40:0;var label=Label(rt,text,new Rect(inset,0,r.width-inset,r.height),Mathf.Min(22,(int)(r.height*.5f)));label.color=Cream;}
            if(rewardTool)
            {
                RewardPlus(rt,"ToolPlus",new Rect(r.width-18,4,14,14));
                if(!round){var caption=rt.GetComponentInChildren<Text>();Place(caption.rectTransform,new Rect(0,23,r.width*4,36*4));caption.fontSize=14*4;}
            }
        }
        private void Icon(Transform parent,string id,Rect rect)
        {if(art!=null){var t=art.Texture(id.StartsWith("../ui/")?"ui."+id.Substring(6):"icon."+id);if(t)Picture(parent,t,rect,"Icon_"+id);return;}if(string.IsNullOrEmpty(assetRoot))return;string path=id.StartsWith("../")?assetRoot+"/"+id.Substring(3):assetRoot+"/icons/"+id;var texture=PresentationAssets.Load<Texture2D>(path);if(texture)Picture(parent,texture,rect,"Icon_"+id);}
        private static void Picture(Transform parent,Texture texture,Rect r,string name,bool raycast=false)
        { var rt=Node(parent,name,r); var image=rt.gameObject.AddComponent<RawImage>(); image.texture=texture; image.raycastTarget=raycast; }
        private RectTransform Food(Transform parent,int id,Rect r)
        {
            if(id<0||id>=foods.Length)return null;
            var rt=Node(parent,"Food_"+id,r);var image=rt.gameObject.AddComponent<RawImage>();image.texture=foods[id];image.raycastTarget=false;
            rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition+=new Vector2(r.width*.5f,-r.height*.5f);
            if(art!=null)image.uvRect=art.FoodUv(id);
            return rt;
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
            itemVisuals[item.itemId]=rt;
        }
        private void OnDestroy()
        { CancelScreenPress();CloseFriendBoardView();ReleaseAssetReferences();simulation?.TrySetResult(RewardOutcome.Cancelled);if(port!=null)port.Updated-=Apply;if(rounded)Destroy(rounded); if(roundedTexture)Destroy(roundedTexture);if(rewardCircle)Destroy(rewardCircle);if(rewardCircleTexture)Destroy(rewardCircleTexture);if(fireGradient)Destroy(fireGradient);if(fireGradientTexture)Destroy(fireGradientTexture); }
        public void ReleaseAssetReferences()
        {
            CancelScreenPress();CancelShuffleFeedback();
            feedback?.ReleaseAssetReferences();World?.Clear();
            foreach(var image in GetComponentsInChildren<RawImage>(true))image.texture=null;
            foreach(var image in GetComponentsInChildren<Image>(true))image.sprite=null;
            art?.Dispose();art=null;Array.Clear(foods,0,foods.Length);plate=dish=table=potBody=potUnlit=potBroth=potRim=null;uiSprites.Clear();
        }
    }
}
