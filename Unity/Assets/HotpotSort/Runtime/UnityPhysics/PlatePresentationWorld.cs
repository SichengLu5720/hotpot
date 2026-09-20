using System.Collections.Generic;
using UnityEngine;
using HotpotSort.Presentation;

namespace HotpotSort.UnityPhysics
{
    // Physical presentation of committed inventory only. Never allocates IDs or consumes supply.
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
        private static int nextIsland;
        private Vector2 origin;
        private GameObject boundaries;
        private PhysicsMaterial2D material;
        private bool simulating;
        private string sessionId;
        private void Awake()
        {
            // Per-view spatial island. No global physics or project settings are changed.
            origin = new Vector2(1000 + 20 * nextIsland++, 1000);
            material = new PhysicsMaterial2D("PlatePresentationContact") { friction=.35f, bounciness=.08f };
            boundaries = new GameObject("PlateBounds"); boundaries.transform.SetParent(transform,false);
            Boundary("Left",new Vector2(-5,566),new Vector2(10,544));
            Boundary("Right",new Vector2(425,566),new Vector2(10,544));
            Boundary("Floor",new Vector2(210,833),new Vector2(440,10));
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
            // Board Y increases downward. Per-body force preserves global Physics2D.gravity.
            foreach(var body in plates.Values)
                body.rigidbody.AddForce(Vector2.up * (960 * Units * body.rigidbody.mass),ForceMode2D.Force);
        }
        private void Update()
        {
            if(!simulating)return;
            for(int i=remnants.Count-1;i>=0;i--)
            { remnants[i].remaining-=Time.unscaledDeltaTime; if(remnants[i].remaining<=0)remnants.RemoveAt(i); }
        }
        public void Reconcile(ViewSnapshot snapshot)
        {
            if(sessionId!=snapshot.sessionId) { Clear(); sessionId=snapshot.sessionId; }
            var alive=new HashSet<string>(); drawOrder.Clear();
            foreach(var p in snapshot.plates)
            {
                if(p.items.Length==0)continue;
                alive.Add(p.plateId);
                PlateBody body;
                long correction=p.motion==null?0:p.motion.correctionRevision;
                if(!plates.TryGetValue(p.plateId,out body))
                {
                    var node=new GameObject("Plate_"+p.plateId); node.transform.SetParent(transform,false);
                    bool animate=p.motion==null || p.motion.animateEntry;
                    var start=animate?new Vector2(Mathf.Clamp(p.x,p.radius,420-p.radius),304+p.radius):new Vector2(p.x,p.y);
                    if(animate && p.motion!=null && p.motion.hasSpawnPosition)start=new Vector2(p.motion.spawnX,p.motion.spawnY);
                    node.transform.position=Physical(start);
                    var rb=node.AddComponent<Rigidbody2D>(); rb.bodyType=RigidbodyType2D.Dynamic;
                    rb.gravityScale=0; rb.freezeRotation=true; rb.linearDamping=.25f; rb.interpolation=RigidbodyInterpolation2D.None;
                    rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous; rb.simulated=simulating;
                    var rim=node.AddComponent<CircleCollider2D>(); rim.radius=p.radius*Units; rim.sharedMaterial=material;
                    body=new PlateBody { node=node,rigidbody=rb,rim=rim,correctionRevision=correction };
                    plates.Add(p.plateId,body);
                }
                else if(body.data.x!=p.x || body.data.y!=p.y || body.correctionRevision!=correction)
                {
                    body.rigidbody.position=Physical(new Vector2(p.x,p.y));
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
                        collider=node.AddComponent<CircleCollider2D>(); collider.isTrigger=true; body.items.Add(item.itemId,collider);
                    }
                    collider.transform.localPosition=new Vector3(item.x*Units,item.y*Units,0);
                    ((CircleCollider2D)collider).radius=item.radius*Units;
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
        public string Hit(Vector2 board)
        {
            var point=Physical(board);
            for(int p=drawOrder.Count-1;p>=0;p--)
            {
                var body=drawOrder[p]; if(!body.rim.OverlapPoint(point))continue;
                for(int i=body.data.items.Length-1;i>=0;i--)
                { var item=body.data.items[i]; if(body.items[item.itemId].OverlapPoint(point))return item.itemId; }
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
        public void Clear()
        {
            foreach(var body in plates.Values) { body.node.SetActive(false); Destroy(body.node); }
            plates.Clear(); drawOrder.Clear(); remnants.Clear();
        }
        private void OnDestroy() { if(material)Destroy(material); }
    }
}
