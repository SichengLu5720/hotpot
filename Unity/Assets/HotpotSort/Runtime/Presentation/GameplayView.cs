using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HotpotSort.UnityPhysics;

namespace HotpotSort.Presentation
{
    /// <summary>Mount using Bind from the platform/session bootstrap. No automatic core or daily seed creation.</summary>
    public sealed class GameplayView : MonoBehaviour
    {
        public MonoBehaviour portComponent;
        public Font playerFont;
        public event Action<ViewAction> ActionRequested;
        public ViewSnapshot LastSnapshot { get; private set; }
        public long LastEventSequence { get; private set; }
        public string LastTransactionId { get; private set; }
        public PlatePresentationWorld World { get; private set; }
        private GameplayFeedback feedback;
        private IPresentationPort port;
        private RectTransform canvasRoot, content;
        private readonly Texture2D[] foods = new Texture2D[16];
        private Texture2D plate;
        private Sprite rounded;
        private Texture2D roundedTexture;
        private Rect viewport;
        private bool externalViewport, foreground = true;
        private long inputSeq;
        private readonly HashSet<string> pending = new HashSet<string>();
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private readonly Dictionary<string,RectTransform> plateVisuals = new Dictionary<string,RectTransform>();
        private readonly Dictionary<PlatePresentationWorld.Remnant,RectTransform> ghostVisuals = new Dictionary<PlatePresentationWorld.Remnant,RectTransform>();
        private static readonly Color Cream = new Color32(248,245,229,255), Ink = new Color32(47,80,72,255), Coral = new Color32(203,105,86,255), Green = new Color32(229,234,215,255);
        private void Awake()
        {
            for (int i = 0; i < foods.Length; i++) foods[i] = Resources.Load<Texture2D>("Hotpot/food_" + i.ToString("00"));
            plate = Resources.Load<Texture2D>("Hotpot/plate");
            if (!playerFont) playerFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "PingFang SC", "Noto Sans CJK SC", "Arial Unicode MS" }, 28);
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
            var image = background.gameObject.AddComponent<Image>(); image.color = Cream; image.raycastTarget = false;
            background.anchorMin=Vector2.zero; background.anchorMax=Vector2.one; background.offsetMin=background.offsetMax=Vector2.zero;
            content = Node(canvasRoot,"SafeContent",new Rect());
            World = gameObject.AddComponent<PlatePresentationWorld>();
            feedback = gameObject.AddComponent<GameplayFeedback>();
            feedback.Initialize(canvasRoot, root.GetComponent<Canvas>());
            if (!FindObjectOfType<EventSystem>())
            { var events = new GameObject("ViewEventSystem",typeof(EventSystem),typeof(StandaloneInputModule)); events.transform.SetParent(transform,false); }
            viewport = Screen.safeArea;
            LastSnapshot = new ViewSnapshot { phase = ViewPhase.Entry };
            if (portComponent is IPresentationPort) Bind((IPresentationPort)portComponent); else Render();
        }
        public void Bind(IPresentationPort source)
        {
            if (port != null) port.Updated -= Apply;
            port = source; pending.Clear(); LastSnapshot = null;
            if (port != null) { port.Updated += Apply; Apply(new ViewUpdate { snapshot = port.Read() }); }
            else { LastSnapshot = new ViewSnapshot { phase = ViewPhase.Entry }; World.Clear(); Render(); }
        }
        public void SetViewport(Rect screenPixelSafeArea)
        { externalViewport = true; viewport = screenPixelSafeArea; Render(); }
        public void SetForeground(bool value) { foreground = value; UpdateSimulation(); }
        public void Hide(bool hidden) { canvasRoot.gameObject.SetActive(!hidden); UpdateSimulation(); }
        private void UpdateSimulation() { World.SetSimulating(foreground && canvasRoot.gameObject.activeInHierarchy && LastSnapshot.phase==ViewPhase.Running); }
        public void ResetView() { pending.Clear(); LastEventSequence=0; LastTransactionId=null; feedback.ResetFeedback(); World.Clear(); LastSnapshot=new ViewSnapshot { phase=ViewPhase.Entry }; Render(); }
        public void ShowError(string message)
        { LastSnapshot = new ViewSnapshot { sessionId=LastSnapshot?.sessionId, phase=ViewPhase.Aborted, message=message }; World.Clear(); Render(); }
        public void DestroyView() { Destroy(gameObject); }
        public void Apply(ViewUpdate update)
        {
            if (update == null || update.snapshot == null) return;
            var s = update.snapshot;
            if (LastSnapshot != null && LastSnapshot.sessionId == s.sessionId && s.revision < LastSnapshot.revision) return;
            if (LastSnapshot == null || LastSnapshot.sessionId != s.sessionId) { pending.Clear(); LastEventSequence=0; }
            var previous = LastSnapshot;
            feedback.Apply(update, previous);
            foreach (var e in update.events ?? new ViewEvent[0]) if (e.sequence > LastEventSequence)
            { LastEventSequence=e.sequence; LastTransactionId=e.transactionId; }
            LastEventSequence = Math.Max(LastEventSequence,s.eventSeq);
            // Final authoritative snapshot is the convergence point, including gaps/reconnects/long chains.
            LastSnapshot = s; pending.Clear(); UpdateSimulation(); World.Reconcile(s); Render();
        }
        private void LateUpdate()
        {
            foreach(var body in World.Bodies)
            {
                RectTransform node;
                if(plateVisuals.TryGetValue(body.data.plateId,out node) && node)
                { var at=World.Position(body); node.anchoredPosition=new Vector2(at.x,-at.y); }
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
            if (!externalViewport && viewport != Screen.safeArea) { viewport=Screen.safeArea; Render(); }
            if (!foreground || !canvasRoot.gameObject.activeInHierarchy || LastSnapshot.phase != ViewPhase.Running || port == null) return;
            if (Input.touchCount>0) { var t=Input.GetTouch(0); if(t.phase==TouchPhase.Began) SubmitScreenTap(t.position,t.fingerId); }
            else if (Input.GetMouseButtonDown(0)) SubmitScreenTap(Input.mousePosition,-1);
        }
        public bool SubmitScreenTap(Vector2 screen, int pointerId = -1)
        {
            if (port == null || !foreground || LastSnapshot.phase != ViewPhase.Running || !viewport.Contains(screen)) return false;
            if (EventSystem.current)
            {
                var data=new PointerEventData(EventSystem.current) { position=screen, pointerId=pointerId };
                uiHits.Clear(); EventSystem.current.RaycastAll(data,uiHits); if(uiHits.Count>0)return false;
            }
            var board = new Vector2((screen.x-viewport.x)*420/viewport.width,(viewport.yMax-screen.y)*900/viewport.height);
            if (board.y<304 || board.y>828) return false;
            string id=World.Hit(board); if(id==null || !pending.Add(id))return false;
            try { port.Tap(new ViewTap { itemId=id,inputSeq=++inputSeq,snapshotRevision=LastSnapshot.revision,boardX=board.x,boardY=board.y }); }
            catch { pending.Remove(id); throw; }
            return true;
        }
        // Explicit acknowledgement for rejection/no-state-change; does not alter inventory.
        public void AcknowledgeTap(string itemId) { pending.Remove(itemId); }
        public ViewSupplyObservation ObserveSupply(Vector2 center, float radius)
        {
            var value = new ViewSupplyObservation { spaceAvailable=World.SpaceAvailable(center,radius),x=center.x,y=center.y,radius=radius,snapshotRevision=LastSnapshot.revision };
            port?.ObserveSupply(value); return value;
        }
        private void Action(ViewAction action) { feedback.Button(); ActionRequested?.Invoke(action); port?.SessionAction(action); }
        private void Render()
        {
            if (!content) return;
            plateVisuals.Clear(); ghostVisuals.Clear();
            foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            if (viewport.width<=0 || viewport.height<=0) viewport=new Rect(0,0,Screen.width,Screen.height);
            Place(content,new Rect(viewport.x,Screen.height-viewport.yMax,viewport.width,viewport.height));
            if (LastSnapshot.phase==ViewPhase.Entry) { Entry(); return; }
            float sx=viewport.width/420, sy=viewport.height/900;
            var board=Node(content,"GameplayBoard",new Rect(0,0,420,900)); board.localScale=new Vector3(sx,sy,1);
            Label(board,"下锅喽",new Rect(18,12,260,38),28);
            Button(board,"暂停",new Rect(320,14,82,38),()=>Action(ViewAction.Pause));
            for(int slot=0;slot<4;slot++)
            {
                var box=Panel(board,"Order_"+slot,new Rect(12+slot*101,70,94,138),Green);
                ViewOrder order=Array.Find(LastSnapshot.orders,o=>o.slot==slot);
                bool enabled=slot<2 && order!=null && order.enabled;
                if(enabled) { Food(box,order.foodId,new Rect(19,12,56,56)); Label(box,order.count+" / "+order.required,new Rect(0,83,94,35),20); }
                else Label(box,"未启用",new Rect(0,48,94,40),16);
            }
            for(int slot=0;slot<5;slot++)
            {
                var cell=Panel(board,"Buffer_"+slot,new Rect(45+slot*67,228,61,62),Green);
                if(slot<LastSnapshot.buffer.Length && LastSnapshot.buffer[slot]!=null) Food(cell,LastSnapshot.buffer[slot].foodId,new Rect(5,5,51,51));
            }
            foreach(var body in World.Bodies)
            {
                var p=body.data; var at=World.Position(body);
                var node=Node(board,"PlateVisual_"+p.plateId,new Rect(at.x,at.y,0,0)); plateVisuals.Add(p.plateId,node);
                Picture(node,plate,new Rect(-p.radius,-p.radius,p.radius*2,p.radius*2),"Plate_"+p.plateId);
                foreach(var item in p.items) Food(node,item.foodId,new Rect(item.x-item.radius,item.y-item.radius,item.radius*2,item.radius*2));
            }
            foreach(var ghost in World.Remnants)
            {
                var node=Node(board,"Clearing_"+ghost.plateId,new Rect(ghost.position.x-ghost.radius,ghost.position.y-ghost.radius,ghost.radius*2,ghost.radius*2));
                var image=node.gameObject.AddComponent<RawImage>(); image.texture=plate; image.raycastTarget=false;
                ghostVisuals.Add(ghost,node);
            }
            if(LastSnapshot.phase!=ViewPhase.Running) Overlay();
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
        }
        private void Overlay()
        {
            var modal=Panel(content,"FlowOverlay",new Rect(0,0,viewport.width,viewport.height),Cream);
            string title=LastSnapshot.phase==ViewPhase.Paused?"已暂停":LastSnapshot.phase==ViewPhase.Won?"挑战完成":LastSnapshot.phase==ViewPhase.Overflow?"暂存溢出":"挑战已中止";
            Label(modal,title,new Rect(15,viewport.height*.19f,viewport.width-30,48),28);
            float y=viewport.height*.30f;
            foreach(var fact in LastSnapshot.facts) { Label(modal,fact.label+"："+fact.value,new Rect(20,y,viewport.width-40,32),18); y+=34; }
            if(!string.IsNullOrEmpty(LastSnapshot.message)) Label(modal,LastSnapshot.message,new Rect(20,y,viewport.width-40,70),16);
            bool paused=LastSnapshot.phase==ViewPhase.Paused;
            Button(modal,paused?"继续":"同日重试",new Rect(35,viewport.height-156,viewport.width-70,52),()=>Action(paused?ViewAction.Resume:ViewAction.RetrySameDay));
            Button(modal,"退出",new Rect(35,viewport.height-92,viewport.width-70,48),()=>Action(ViewAction.Exit));
        }
        private static RectTransform Node(Transform parent,string name,Rect rect)
        { var node=new GameObject(name,typeof(RectTransform)); node.transform.SetParent(parent,false); var rt=(RectTransform)node.transform; Place(rt,rect); return rt; }
        private static void Place(RectTransform rt,Rect r)
        { rt.anchorMin=rt.anchorMax=new Vector2(0,1); rt.pivot=new Vector2(0,1); rt.anchoredPosition=new Vector2(r.x,-r.y); rt.sizeDelta=r.size; }
        private RectTransform Panel(Transform parent,string name,Rect r,Color color)
        { var rt=Node(parent,name,r); var im=rt.gameObject.AddComponent<Image>(); im.sprite=rounded; im.type=Image.Type.Sliced; im.color=color; return rt; }
        private void Label(Transform parent,string text,Rect r,int size)
        { var rt=Node(parent,"Label",r); var label=rt.gameObject.AddComponent<Text>(); label.font=playerFont; label.text=text; label.fontSize=size; label.color=Ink; label.alignment=TextAnchor.MiddleCenter; label.raycastTarget=false; }
        private void Button(Transform parent,string text,Rect r,UnityEngine.Events.UnityAction action)
        { var rt=Panel(parent,"Action_"+text,r,Coral); var button=rt.gameObject.AddComponent<Button>(); button.targetGraphic=rt.GetComponent<Image>(); button.onClick.AddListener(action); Label(rt,text,new Rect(0,0,r.width,r.height),22); rt.GetComponentInChildren<Text>().color=Cream; }
        private static void Picture(Transform parent,Texture texture,Rect r,string name)
        { var rt=Node(parent,name,r); var image=rt.gameObject.AddComponent<RawImage>(); image.texture=texture; image.raycastTarget=false; }
        private void Food(Transform parent,int id,Rect r)
        { if(id>=0 && id<foods.Length)Picture(parent,foods[id],r,"Food_"+id); }
        private void OnDestroy()
        { if(port!=null)port.Updated-=Apply; if(rounded)Destroy(rounded); if(roundedTexture)Destroy(roundedTexture); }
    }
}
