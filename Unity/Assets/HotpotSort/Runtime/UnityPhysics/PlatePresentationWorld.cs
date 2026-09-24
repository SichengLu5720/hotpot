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
            internal Vector2 diagnosticAnchor;
            internal float diagnosticStillSeconds;
            internal bool diagnosticAnchored;
            internal string diagnosticClearSource;
            internal float diagnosticClearTime;
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
        private Vector2[] solverPositions=System.Array.Empty<Vector2>();
        public readonly List<Vector2> StepGeometry = new List<Vector2>();
        public int ConstraintRollbacks { get; private set; }
        public int TransientGeometrySteps { get; private set; }
        public bool PhysicsDiagnosticsEnabled=false;
        const int DiagnosticCapacity=32,DiagnosticPlateLimit=6;
        readonly Queue<string> diagnosticRing=new Queue<string>(DiagnosticCapacity);
        readonly ContactPoint2D[] diagnosticContacts=new ContactPoint2D[8];
        float diagnosticTime,diagnosticNextLog;
        int diagnosticSampleCursor;
        [System.Serializable] sealed class ContactDiagnostic { public string collider; public Vector2 point,normal; }
        [System.Serializable] sealed class BodyDiagnostic
        {
            public string id,lastVelocityClear;
            public Vector2 position,velocity;
            public float radius,stillSeconds,floorClearance,lastVelocityClearTime;
            public bool floorContact,lowerContact,contactsMayBeTruncated;
            public ContactDiagnostic[] contacts;
        }
        [System.Serializable] sealed class PhysicsDiagnostic
        {
            public string kind,velocityClearSource,boundary,boundaryPlate,pairA,pairB;
            public float activeSeconds,pairGap,boundaryGap;
            public Vector2 pairPositionA,pairPositionB,boundaryPosition;
            public int rollbackCount,bodyCount;
            public BodyDiagnostic[] bodies;
        }
        // Snapshot only; does not drain logs or expose player/session identity.
        public string[] GetPhysicsDiagnosticSnapshot(){return diagnosticRing.ToArray();}
        void ResetDiagnosticMotion()
        {foreach(var body in plates.Values){body.diagnosticAnchored=false;body.diagnosticStillSeconds=0;}}
        void MarkVelocityClear(PlateBody body,string source)
        {
            if(!PhysicsDiagnosticsEnabled)return;
            body.diagnosticClearSource=source;body.diagnosticClearTime=diagnosticTime;
            if(source!="constraint-rollback"&&DiagnosticReady)
                EmitDiagnostic(new PhysicsDiagnostic{kind="velocity-clear",velocityClearSource=source,bodies=new[]{DescribeBody(body)}});
        }
        BodyDiagnostic DescribeBody(PlateBody body)
        {
            var at=Position(body);int n=body.rim.GetContacts(diagnosticContacts);
            var result=new BodyDiagnostic{id=body.data.plateId,position=at,velocity=body.rigidbody.linearVelocity/Units,
                radius=body.data.radius,stillSeconds=body.diagnosticStillSeconds,floorClearance=828-at.y-body.data.radius,
                lastVelocityClear=body.diagnosticClearSource,lastVelocityClearTime=body.diagnosticClearTime,
                contacts=new ContactDiagnostic[n],contactsMayBeTruncated=n==diagnosticContacts.Length};
            for(int i=0;i<n;i++)
            {
                var contact=diagnosticContacts[i];var other=contact.collider==body.rim?contact.otherCollider:contact.collider;
                var point=(contact.point-origin)/Units;
                result.contacts[i]=new ContactDiagnostic{collider=other?other.name:"missing",point=point,normal=contact.normal};
                result.floorContact|=other&&other.name=="Floor";
                result.lowerContact|=point.y>at.y+.1f;
            }
            return result;
        }
        bool DiagnosticReady=>PhysicsDiagnosticsEnabled&&diagnosticTime>=diagnosticNextLog;
        void EmitDiagnostic(PhysicsDiagnostic record)
        {
            record.activeSeconds=diagnosticTime;record.bodyCount=drawOrder.Count;record.rollbackCount=ConstraintRollbacks;
            string json=JsonUtility.ToJson(record);
            if(diagnosticRing.Count==DiagnosticCapacity)diagnosticRing.Dequeue();diagnosticRing.Enqueue(json);
            diagnosticNextLog=diagnosticTime+3f;Debug.Log("HOTPOT_PHYSICS_DIAG "+json);
        }
        void RecordGeometryFailure(Vector2[] positions,int count,bool rollback)
        {
            if(!DiagnosticReady)return;
            var record=new PhysicsDiagnostic{kind=rollback?"constraint-rollback":"transient-geometry",velocityClearSource=rollback?"constraint-rollback":null,pairGap=420,boundaryGap=828};
            int pairA=-1,pairB=-1,boundaryIndex=-1;
            for(int i=0;i<count;i++)
            {
                var at=positions[i];float r=drawOrder[i].data.radius;
                float gap=at.x-r;string wall="Left";
                if(420-at.x-r<gap){gap=420-at.x-r;wall="Right";}
                if(at.y-HiddenTop-r<gap){gap=at.y-HiddenTop-r;wall="Ceiling";}
                if(828-at.y-r<gap){gap=828-at.y-r;wall="Floor";}
                if(gap<record.boundaryGap){record.boundaryGap=gap;record.boundary=wall;boundaryIndex=i;record.boundaryPlate=drawOrder[i].data.plateId;record.boundaryPosition=at;}
                for(int j=i+1;j<count;j++)
                {
                    gap=Vector2.Distance(at,positions[j])-r-drawOrder[j].data.radius;
                    if(gap>=record.pairGap)continue;
                    record.pairGap=gap;pairA=i;pairB=j;record.pairA=drawOrder[i].data.plateId;record.pairB=drawOrder[j].data.plateId;record.pairPositionA=at;record.pairPositionB=positions[j];
                }
            }
            var samples=new List<BodyDiagnostic>(DiagnosticPlateLimit);
            if(pairA>=0)samples.Add(DescribeBody(drawOrder[pairA]));
            if(pairB>=0)samples.Add(DescribeBody(drawOrder[pairB]));
            if(boundaryIndex>=0&&boundaryIndex!=pairA&&boundaryIndex!=pairB)samples.Add(DescribeBody(drawOrder[boundaryIndex]));
            // Repeated rollbacks share the global log budget. Include rotating
            // stationary witnesses so an unrelated failing pair cannot hide them.
            int nextCursor=diagnosticSampleCursor;
            for(int offset=0;offset<count&&samples.Count<DiagnosticPlateLimit;offset++)
            {
                int index=(diagnosticSampleCursor+offset)%count;nextCursor=(index+1)%count;
                if(index==pairA||index==pairB||index==boundaryIndex||drawOrder[index].diagnosticStillSeconds<2f)continue;
                samples.Add(DescribeBody(drawOrder[index]));
            }
            diagnosticSampleCursor=nextCursor;
            if(!rollback)record.kind=record.pairGap < -1f||record.boundaryGap < -1f?"native-penetration-review":"native-contact-slop";
            record.bodies=samples.ToArray();EmitDiagnostic(record);
        }
        void ObserveDiagnosticMotion()
        {
            if(!PhysicsDiagnosticsEnabled)return;
            foreach(var body in drawOrder)
            {
                var at=Position(body);
                if(!body.diagnosticAnchored||(at-body.diagnosticAnchor).sqrMagnitude>1f)
                {body.diagnosticAnchor=at;body.diagnosticAnchored=true;body.diagnosticStillSeconds=0;}
                else body.diagnosticStillSeconds+=Time.fixedDeltaTime;
            }
            if(!DiagnosticReady)return;
            List<BodyDiagnostic> samples=null;
            for(int offset=0;offset<drawOrder.Count;offset++)
            {
                int index=(diagnosticSampleCursor+offset)%drawOrder.Count;var body=drawOrder[index];
                if(body.diagnosticStillSeconds<2f)continue;
                if(samples==null)samples=new List<BodyDiagnostic>(DiagnosticPlateLimit);
                samples.Add(DescribeBody(body));if(samples.Count==DiagnosticPlateLimit){diagnosticSampleCursor=(index+1)%drawOrder.Count;break;}
            }
            if(samples!=null)EmitDiagnostic(new PhysicsDiagnostic{kind="stationary",bodies=samples.ToArray()});
        }
        private void Awake()
        {
            // Per-view spatial island. No global physics or project settings are changed.
            origin = Vector2.zero;
            localScene=SceneManager.CreateScene("HotpotPlatePhysics_"+nextIsland++,new CreateSceneParameters(LocalPhysicsMode.Physics2D));
            localPhysics=localScene.GetPhysicsScene2D();
            physicsRoot=new GameObject("PlatePhysicsRoot");SceneManager.MoveGameObjectToScene(physicsRoot,localScene);
            material = new PhysicsMaterial2D("PlatePresentationContact") { friction=0f, bounciness=.08f };
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
            if(!value)ResetDiagnosticMotion();
            simulating=value;
            foreach(var body in plates.Values) body.rigidbody.simulated=value;
        }
        private void FixedUpdate()
        {
            if(!simulating)return;
            if(PhysicsDiagnosticsEnabled)diagnosticTime+=Time.fixedDeltaTime;
            int count=drawOrder.Count;
            EnsurePositionBuffers(count);
            // Board Y increases downward. Per-body force preserves global Physics2D.gravity.
            foreach(var body in plates.Values)
                body.rigidbody.AddForce(Vector2.up * (960 * Units * body.rigidbody.mass),ForceMode2D.Force);
            localPhysics.Simulate(Time.fixedDeltaTime);
            for(int i=0;i<count;i++)solverPositions[i]=Position(drawOrder[i]);
            var geometry=MeasureGeometry(solverPositions,count);
            if(geometry.x<-.001f || geometry.y<-.001f)
            {
                // Read-only observation of native contact slop or spawn overlap.
                // Never project positions, replace contact velocities, or rewind bodies.
                RecordGeometryFailure(solverPositions,count,false);
                TransientGeometrySteps++;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(StepGeometry.Count<30000)StepGeometry.Add(geometry);
#endif
            ObserveDiagnosticMotion();
        }
        void EnsurePositionBuffers(int count)
        {
            if(solverPositions.Length>=count)return;
            int capacity=Mathf.NextPowerOfTwo(Mathf.Max(4,count));
            solverPositions=new Vector2[capacity];
        }
        private static void MoveBody(PlateBody body,Vector2 position)
        {body.rigidbody.position=position;body.node.transform.position=position;}
        private void Update()
        {
            if(!simulating)return;
            for(int i=remnants.Count-1;i>=0;i--)
            { remnants[i].remaining-=Time.unscaledDeltaTime; if(remnants[i].remaining<=0)remnants.RemoveAt(i); }
        }
        private Vector2 MeasureGeometry()
        {
            int count=drawOrder.Count;EnsurePositionBuffers(count);
            for(int i=0;i<count;i++)solverPositions[i]=Position(drawOrder[i]);
            return MeasureGeometry(solverPositions,count);
        }
        private Vector2 MeasureGeometry(Vector2[] positions,int count)
        {
            float gap=420,boundary=828;
            for(int i=0;i<count;i++)
            {
                var a=drawOrder[i];var at=positions[i];float r=a.data.radius;
                boundary=Mathf.Min(boundary,Mathf.Min(Mathf.Min(at.x-r,420-at.x-r),Mathf.Min(at.y-HiddenTop-r,828-at.y-r)));
                for(int j=i+1;j<count;j++)gap=Mathf.Min(gap,Vector2.Distance(at,positions[j])-r-drawOrder[j].data.radius);
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
                    MarkVelocityClear(body,"snapshot-correction");
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
                    for(int i=0;i<bodies.Count;i++){MoveBody(bodies[i],Physical(targets[i]));MarkVelocityClear(bodies[i],"shuffle");bodies[i].rigidbody.linearVelocity=Vector2.zero;bodies[i].rigidbody.angularVelocity=0;}
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
                if(commit){for(int i=0;i<bodies.Count;i++){MoveBody(bodies[i],Physical(targets[i]));MarkVelocityClear(bodies[i],"shuffle");bodies[i].rigidbody.linearVelocity=Vector2.zero;bodies[i].rigidbody.angularVelocity=0;}Physics2D.SyncTransforms();}
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
            diagnosticRing.Clear();diagnosticTime=diagnosticNextLog=0;diagnosticSampleCursor=0;
        }
        private void OnDestroy() { Clear();if(localScene.IsValid()&&localScene.isLoaded)SceneManager.UnloadSceneAsync(localScene);if(material)Destroy(material); }
    }
}
