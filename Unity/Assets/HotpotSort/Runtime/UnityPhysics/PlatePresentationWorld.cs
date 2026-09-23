using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using HotpotSort.Presentation;

namespace HotpotSort.UnityPhysics
{
    // Physical presentation of committed inventory only. Never allocates IDs or consumes supply.
    [DefaultExecutionOrder(-100)]
    public sealed class PlatePresentationWorld : MonoBehaviour
    {
        public sealed class PlateBody
        {
            public ViewPlate data;
            public GameObject node;
            public Rigidbody2D rigidbody;
            public CircleCollider2D rim;
            public long correctionRevision;
            public readonly Dictionary<string, Collider2D> items = new Dictionary<string, Collider2D>();
        }
        public sealed class Remnant { public string plateId; public Vector2 position; public float radius, remaining = .18f; }
        private readonly Dictionary<string, PlateBody> plates = new Dictionary<string, PlateBody>();
        private readonly List<PlateBody> drawOrder = new List<PlateBody>();
        private readonly List<Remnant> remnants = new List<Remnant>();
        public IEnumerable<PlateBody> Bodies { get { return drawOrder; } }
        public IEnumerable<Remnant> Remnants { get { return remnants; } }
        public int OccupiedPlateCount { get { return plates.Count; } }
        private const float Units = .01f;
        public const float HiddenTop = PlateSupplyGeometry.Top, SupplyGate = PlateSupplyGeometry.Gate;
        private static int nextIsland;
        private Vector2 origin;
        private GameObject boundaries;
        private GameObject physicsRoot;
        private Scene localScene;
        private PhysicsScene2D localPhysics;
        private PhysicsMaterial2D material;
        private bool simulating;
        private string sessionId;
        private long sessionGeneration,observationSequence;
        public readonly List<Vector2> StepGeometry = new List<Vector2>();
        public int ConstraintRollbacks { get; private set; }
        public int TransientGeometrySteps { get; private set; }
        private void Awake()
        {
            // Per-view spatial island. No global physics or project settings are changed.
            origin = Vector2.zero;
            localScene=SceneManager.CreateScene("HotpotPlatePhysics_"+nextIsland++,new CreateSceneParameters(LocalPhysicsMode.Physics2D));
            localPhysics=localScene.GetPhysicsScene2D();
            physicsRoot=new GameObject("PlatePhysicsRoot");SceneManager.MoveGameObjectToScene(physicsRoot,localScene);
            material = new PhysicsMaterial2D("PlatePresentationContact") { friction=.08f, bounciness=.08f };
            boundaries = new GameObject("PlateBounds"); boundaries.transform.SetParent(physicsRoot.transform,false);
            Boundary("Left",new Vector2(-5,347),new Vector2(10,982));
            Boundary("Right",new Vector2(425,347),new Vector2(10,982));
            Boundary("Floor",new Vector2(210,833),new Vector2(440,10));
            Boundary("Ceiling",new Vector2(210,-139),new Vector2(440,10));
        }
        private Vector2 Physical(Vector2 board) { return origin + board * Units; }
        public Vector2 Position(PlateBody body) { return (body.rigidbody.position-origin)/Units; }
        private void Boundary(string name, Vector2 center, Vector2 size)
        {
            var node=new GameObject(name); node.transform.SetParent(boundaries.transform,false);
            node.transform.position=Physical(center);
            var box=node.AddComponent<BoxCollider2D>(); box.size=size*Units; box.sharedMaterial=material;
        }
        public void SetSimulating(bool value)
        {
            simulating=value;
            foreach(var body in plates.Values) body.rigidbody.simulated=value;
        }
        private void FixedUpdate()
        {
            if(!simulating)return;
            var beforeGeometry=MeasureGeometry();
            bool previousLegal=beforeGeometry.x>=-.001f && beforeGeometry.y>=-.001f;
            var previous=new Vector2[drawOrder.Count];
            for(int i=0;i<drawOrder.Count;i++)previous[i]=drawOrder[i].rigidbody.position;
            // Board Y increases downward. Per-body force preserves global Physics2D.gravity.
            foreach(var body in plates.Values)
                body.rigidbody.AddForce(Vector2.up * (960 * Units * body.rigidbody.mass),ForceMode2D.Force);
            localPhysics.Simulate(Time.fixedDeltaTime);
            ConstrainGeometry();
            var geometry=MeasureGeometry();
            if(geometry.x<-.001f || geometry.y<-.001f)
            {
                // The synchronous step keeps the same object set. A new spawn may
                // overlap, so only a measured legal pre-step state can be restored.
                if(previousLegal)
                {
                    for(int i=0;i<drawOrder.Count;i++){MoveBody(drawOrder[i],previous[i]);drawOrder[i].rigidbody.linearVelocity=Vector2.zero;}
                    Physics2D.SyncTransforms();ConstraintRollbacks++;geometry=MeasureGeometry();
                }
                else TransientGeometrySteps++;
            }
            if(StepGeometry.Count<30000)StepGeometry.Add(geometry);
        }
        private static void MoveBody(PlateBody body,Vector2 position)
        {body.rigidbody.position=position;body.node.transform.position=position;}
        private void Update()
        {
            if(!simulating)return;
            for(int i=remnants.Count-1;i>=0;i--)
            { remnants[i].remaining-=Time.unscaledDeltaTime; if(remnants[i].remaining<=0)remnants.RemoveAt(i); }
        }
        private void ConstrainGeometry()
        {
            if(!simulating)return;
            // Box2D permits a small contact slop. Remove that slop before rendering so
            // the accepted visible rims never overlap; velocities remain physics-owned.
            for(int pass=0;pass<256;pass++)
            {
                bool moved=false;
                for(int i=0;i<drawOrder.Count;i++)
                {
                    var a=drawOrder[i];float radius=a.data.radius;var at=Position(a);
                    var clamped=new Vector2(Mathf.Clamp(at.x,radius,420-radius),Mathf.Clamp(at.y,HiddenTop+radius,828-radius));
                    if((clamped-at).sqrMagnitude>.0000001f){MoveBody(a,Physical(clamped));moved=true;}
                    for(int j=i+1;j<drawOrder.Count;j++)
                    {
                        var b=drawOrder[j];var delta=Position(b)-Position(a);float length=delta.magnitude,required=a.data.radius+b.data.radius+.02f;
                        if(length>=required)continue;
                        var direction=length>.001f?delta/length:Vector2.right;var shift=direction*((required-length)*.5f);
                        MoveBody(a,Physical(Position(a)-shift));MoveBody(b,Physical(Position(b)+shift));moved=true;
                    }
                }
                if(!moved)break;
            }
            Physics2D.SyncTransforms();
        }
        private Vector2 MeasureGeometry()
        {
            float gap=420,boundary=828;
            for(int i=0;i<drawOrder.Count;i++)
            {
                var a=drawOrder[i];var at=Position(a);float r=a.data.radius;
                boundary=Mathf.Min(boundary,Mathf.Min(Mathf.Min(at.x-r,420-at.x-r),Mathf.Min(at.y-HiddenTop-r,828-at.y-r)));
                for(int j=i+1;j<drawOrder.Count;j++)gap=Mathf.Min(gap,Vector2.Distance(at,Position(drawOrder[j]))-r-drawOrder[j].data.radius);
            }
            return new Vector2(gap,boundary);
        }
        public void Reconcile(ViewSnapshot snapshot)
        {
            if(sessionId!=snapshot.sessionId) { Clear(); sessionId=snapshot.sessionId; }
            sessionGeneration=snapshot.sessionGeneration;
            var alive=new HashSet<string>(); drawOrder.Clear();
            foreach(var p in snapshot.plates)
            {
                if(p.items.Length==0)continue;
                alive.Add(p.plateId);
                PlateBody body;
                long correction=p.motion==null?0:p.motion.correctionRevision;
                if(!plates.TryGetValue(p.plateId,out body))
                {
                    var node=new GameObject("Plate_"+p.plateId);SceneManager.MoveGameObjectToScene(node,localScene); node.transform.SetParent(physicsRoot.transform,false);
                    bool animate=p.motion==null || p.motion.animateEntry;
                    var start=animate?new Vector2(Mathf.Clamp(p.x,p.radius,420-p.radius),PlateSupplyGeometry.SpawnY(p.radius)):new Vector2(p.x,p.y);
                    if(animate && p.motion!=null && p.motion.hasSpawnPosition)start=new Vector2(p.motion.spawnX,p.motion.spawnY);
                    node.transform.position=Physical(start);
                    var rb=node.AddComponent<Rigidbody2D>(); rb.bodyType=RigidbodyType2D.Dynamic;
                    rb.gravityScale=0; rb.freezeRotation=true; rb.linearDamping=.08f; rb.interpolation=RigidbodyInterpolation2D.None;
                    rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous; rb.simulated=simulating;
                    var rim=node.AddComponent<CircleCollider2D>(); rim.radius=p.radius*Units; rim.sharedMaterial=material;
                    rb.AddForce(new Vector2(0,5),ForceMode2D.Force);
                    body=new PlateBody { node=node,rigidbody=rb,rim=rim,correctionRevision=correction };
                    plates.Add(p.plateId,body);
                }
                else if(body.data.x!=p.x || body.data.y!=p.y || body.correctionRevision!=correction)
                {
                    MoveBody(body,Physical(new Vector2(p.x,p.y)));
                    body.rigidbody.linearVelocity=Vector2.zero; body.rigidbody.angularVelocity=0;
                    body.correctionRevision=correction;
                }
                body.data=p; drawOrder.Add(body); body.rim.radius=p.radius*Units;
                var ids=new HashSet<string>();
                foreach(var item in p.items)
                {
                    ids.Add(item.itemId); Collider2D collider;
                    if(!body.items.TryGetValue(item.itemId,out collider))
                    {
                        var node=new GameObject("Item_"+item.itemId); node.transform.SetParent(body.node.transform,false);
                        collider=node.AddComponent<BoxCollider2D>(); collider.isTrigger=true; body.items.Add(item.itemId,collider);
                    }
                    collider.transform.localPosition=new Vector3(item.x*Units,item.y*Units,0);
                    collider.transform.localRotation=Quaternion.Euler(0,0,item.rotationDegrees);
                    // Broadphase encloses the complete displayed square. The shared
                    // texture alpha predicate removes transparent corners precisely.
                    ((BoxCollider2D)collider).size=Vector2.one*(item.radius*2*Units);
                }
                foreach(var id in new List<string>(body.items.Keys))if(!ids.Contains(id))
                { body.items[id].enabled=false; Destroy(body.items[id].gameObject); body.items.Remove(id); }
            }
            foreach(var id in new List<string>(plates.Keys))if(!alive.Contains(id))
            {
                var body=plates[id];
                remnants.Add(new Remnant { plateId=id,position=Position(body),radius=body.data.radius });
                // Ghost art is collider-free and absent from occupied capacity immediately.
                body.rim.enabled=false; body.rigidbody.simulated=false; body.node.SetActive(false);
                Destroy(body.node); plates.Remove(id);
            }
            Physics2D.SyncTransforms();
        }
        public string Hit(Vector2 board,System.Func<ViewItem,Vector2,bool> opaqueHit=null)
        {
            var point=Physical(board);
            for(int p=drawOrder.Count-1;p>=0;p--)
            {
                var body=drawOrder[p]; if(!body.rim.OverlapPoint(point))continue;
                for(int i=body.data.items.Length-1;i>=0;i--)
                { var item=body.data.items[i];var offset=board-Position(body)-new Vector2(item.x,item.y);var local=PlateItemTransform.ToLocalOffset(item,offset.x,offset.y);
                    if(body.items[item.itemId].OverlapPoint(point) && (opaqueHit==null || opaqueHit(item,new Vector2(local.x,local.y))))return item.itemId; }
                return null; // Top opaque plate/rim blocks lower food.
            }
            return null;
        }
        public bool SpaceAvailable(Vector2 center,float radius)
        {
            if(radius<=0 || center.x-radius<0 || center.x+radius>420 || center.y-radius<304 || center.y+radius>828)return false;
            foreach(var body in plates.Values)
                if(Vector2.Distance(center,Position(body))<radius+body.data.radius)return false;
            return true;
        }
        public ViewSupplyObservation ObserveHeightGate(long revision)
        {
            var result=new ViewSupplyObservation { version=ViewSupplyObservation.CurrentVersion,sessionGeneration=sessionGeneration,observationSequence=++observationSequence,minimumCenterY=float.PositiveInfinity,snapshotRevision=revision };
            foreach(var body in drawOrder)
            {
                if(!body.node || !body.node.activeSelf || body.data.items.Length==0)continue;
                float y=Position(body).y;result.minimumCenterY=Mathf.Min(result.minimumCenterY,y);
                if(y<SupplyGate){result.entryCount++;if(result.blockingPlateId==null)result.blockingPlateId=body.data.plateId;}
            }
            if(result.canSupply)result.blockingPlateId=null;
            return result;
        }
        public bool TryShuffle(bool commit)
        {
            if(drawOrder.Count<2)return false;
            // Largest first prevents a small plate from occupying the only feasible large slot.
            var bodies=new List<PlateBody>(drawOrder);bodies.Sort((a,b)=>b.data.radius.CompareTo(a.data.radius));
            for(int attempt=0;attempt<8;attempt++)
            {
                var targets=new List<Vector2>();bool valid=true;
                for(int i=0;i<bodies.Count;i++)
                {
                    float r=bodies[i].data.radius;bool placed=false;
                    for(int trial=0;trial<180;trial++)
                    {
                        var at=new Vector2(Random.Range(r,420-r),Random.Range(304+r,828-r));bool clear=true;
                        for(int j=0;j<targets.Count;j++)if(Vector2.Distance(at,targets[j])<r+bodies[j].data.radius+1){clear=false;break;}
                        if(clear){targets.Add(at);placed=true;break;}
                    }
                    if(!placed){valid=false;break;}
                }
                if(!valid)continue;
                if(commit)
                {
                    // Atomic reposition, no paths that sweep through neighbouring plates.
                    for(int i=0;i<bodies.Count;i++){MoveBody(bodies[i],Physical(targets[i]));bodies[i].rigidbody.linearVelocity=Vector2.zero;bodies[i].rigidbody.angularVelocity=0;}
                    Physics2D.SyncTransforms();
                }
                return true;
            }
            // Dense legal packings may have no room for rejection-sampled points.
            // Reflect the complete packing as an isometry: mixed radii, rim gaps and
            // boundaries are preserved exactly, without moving through neighbours.
            if(MeasureGeometry().x<-.001f||MeasureGeometry().y<-.001f)return false;
            for(int mode=0;mode<3;mode++)
            {
                var targets=new List<Vector2>();bool changed=false;
                bool inVisibleBounds=true;
                foreach(var body in bodies)
                {
                    var at=Position(body);var next=new Vector2(mode!=1?420-at.x:at.x,mode!=0?1132-at.y:at.y);float r=body.data.radius;
                    targets.Add(next);changed|=Vector2.Distance(at,next)>.01f;
                    inVisibleBounds &= next.x-r>=-.001f && next.x+r<=420.001f && next.y-r>=303.999f && next.y+r<=828.001f;
                }
                if(!changed || !inVisibleBounds)continue;
                if(commit){for(int i=0;i<bodies.Count;i++){MoveBody(bodies[i],Physical(targets[i]));bodies[i].rigidbody.linearVelocity=Vector2.zero;bodies[i].rigidbody.angularVelocity=0;}Physics2D.SyncTransforms();}
                return true;
            }
            return false;
        }
        public Vector2 ItemPosition(string itemId)
        {
            foreach(var body in drawOrder)foreach(var item in body.data.items)if(item.itemId==itemId)return Position(body)+new Vector2(item.x,item.y);
            return Vector2.zero;
        }
        public void Clear()
        {
            foreach(var body in plates.Values) { if(body.node){body.node.SetActive(false); Destroy(body.node);} }
            plates.Clear(); drawOrder.Clear(); remnants.Clear();
            StepGeometry.Clear();ConstraintRollbacks=0;TransientGeometrySteps=0;
            observationSequence=0;
        }
        private void OnDestroy() { Clear();if(localScene.IsValid()&&localScene.isLoaded)SceneManager.UnloadSceneAsync(localScene);if(material)Destroy(material); }
    }
}
