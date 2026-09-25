using System;
using System.Collections.Generic;
using System.Linq;
using HotpotSort.Contracts;
using HotpotSort.UnityPhysics;
using UnityEngine;

namespace HotpotSort.Presentation
{
    public sealed class ViewClickabilityObservation
    {
        public string[] clickable,unknown;
    }
    public sealed partial class GameplayView
    {
        const float FoodHitTolerance=10f;
        struct FoodEdge {public Vector2 a,b,inside;}
        struct FoodHitCandidate {public Vector2 point,inside;public ViewItem item;public float distance;public int layer;}
        readonly Dictionary<(Texture2D,Rect),List<FoodEdge>> foodEdges=new Dictionary<(Texture2D,Rect),List<FoodEdge>>();
        readonly List<FoodHitCandidate> foodHitCandidates=new List<FoodHitCandidate>();
        public int TolerantHitSearches {get;private set;}
        public double TolerantHitMilliseconds {get;private set;}
        // Texture-local alpha contours are immutable for an installed texture/UV pair.
        // March each source-texel cell as four bilinear-centre triangles; holes and
        // concavities remain intact (this is deliberately not the layout convex hull).
        List<FoodEdge> FoodEdges(Texture2D texture,Rect uv)
        {
            var key=(texture,uv);if(foodEdges.TryGetValue(key,out var edges))return edges;
            if(foodEdges.Count>=32)foodEdges.Clear();
            edges=new List<FoodEdge>();foodEdges.Add(key,edges);
            if(!texture||!texture.isReadable)return edges;
            int w=Mathf.Max(1,Mathf.CeilToInt(texture.width*uv.width)),h=Mathf.Max(1,Mathf.CeilToInt(texture.height*uv.height));
            var previous=new float[w+1];var next=new float[w+1];
            for(int x=0;x<=w;x++)previous[x]=texture.GetPixelBilinear(uv.x+uv.width*x/w,uv.y+uv.height).a;
            void triangle(Vector2 a,float aa,Vector2 b,float ab,Vector2 c,float ac)
            {
                bool ia=aa>.1f,ib=ab>.1f,ic=ac>.1f;if(ia==ib&&ib==ic)return;
                Vector2 first=Vector2.zero,second=Vector2.zero;int count=0;
                void cross(Vector2 p,float ap,Vector2 q,float aq){if((ap>.1f)==(aq>.1f))return;var at=Vector2.LerpUnclamped(p,q,(.1f-ap)/(aq-ap));if(count++==0)first=at;else second=at;}
                cross(a,aa,b,ab);cross(b,ab,c,ac);cross(c,ac,a,aa);
                edges.Add(new FoodEdge{a=first,b=second,inside=ia?a:ib?b:c});
            }
            for(int y=0;y<h;y++)
            {
                for(int x=0;x<=w;x++)next[x]=texture.GetPixelBilinear(uv.x+uv.width*x/w,uv.y+uv.height*(1f-(float)(y+1)/h)).a;
                for(int x=0;x<w;x++)
                {
                    float a=previous[x],b=previous[x+1],c=next[x+1],d=next[x];
                    if((a>.1f)==(b>.1f)&&(a>.1f)==(c>.1f)&&(a>.1f)==(d>.1f))continue;
                    var p=new Vector2((float)x/w-.5f,(float)y/h-.5f);var q=p+new Vector2(1f/w,0);var r=p+new Vector2(1f/w,1f/h);var s=p+new Vector2(0,1f/h);var m=(p+r)*.5f;float am=(a+b+c+d)*.25f;
                    triangle(p,a,q,b,m,am);triangle(q,b,r,c,m,am);triangle(r,c,s,d,m,am);triangle(s,d,p,a,m,am);
                }
                var swap=previous;previous=next;next=swap;
            }
            // An opaque UV edge is also a visible boundary of the displayed square.
            for(int side=0;side<4;side++)for(int j=0,n=side%2==0?w:h;j<n;j++)
            {
                Vector2 edgePoint(float t)=>side==0?new Vector2(t-.5f,-.5f):side==1?new Vector2(.5f,t-.5f):side==2?new Vector2(t-.5f,.5f):new Vector2(-.5f,t-.5f);
                var a=edgePoint((float)j/n);var b=edgePoint((float)(j+1)/n);var m=(a+b)*.5f;
                if(texture.GetPixelBilinear(uv.x+uv.width*(m.x+.5f),uv.y+uv.height*(.5f-m.y)).a>.1f)edges.Add(new FoodEdge{a=a,b=b,inside=m*.999f});
            }
            return edges;
        }
        string TolerantFoodHit(Vector2 point,int pointerId)
        {
            TolerantHitSearches++;var watch=System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var plate=World.TopPlateAt(point);if(plate==null)return null;
                foodHitCandidates.Clear();var plateAt=World.Position(plate);
                for(int layer=plate.data.items.Length-1;layer>=0;layer--)
                {
                    var item=plate.data.items[layer];if(pending.Contains(item.itemId)||!TutorialAllowsFood(item.itemId))continue;
                    var center=plateAt+new Vector2(item.x,item.y);var offset=PlateItemTransform.ToLocalOffset(item,point.x-center.x,point.y-center.y);var local=new Vector2(offset.x,offset.y);
                    if(Mathf.Abs(local.x)>item.radius+FoodHitTolerance+.001f||Mathf.Abs(local.y)>item.radius+FoodHitTolerance+.001f)continue;
                    Vector2 world(Vector2 p){var rotated=PlateItemTransform.Rotate(p.x,p.y,item.rotationDegrees);return center+new Vector2(rotated.x,rotated.y);}
                    foreach(var edge in FoodEdges(foods[item.foodId],ItemUV(item)))
                    {
                        var a=edge.a*(item.radius*2);var b=edge.b*(item.radius*2);var delta=b-a;
                        // Bound visibility sampling on long source cells; normal assets are finer.
                        int steps=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/.25f));
                        for(int step=0;step<steps;step++)
                        {
                            var start=a+delta*((float)step/steps);var segment=delta/steps;
                            float t=segment.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(local-start,segment)/segment.sqrMagnitude):0;
                            var nearest=start+segment*t;float distance=(nearest-local).sqrMagnitude;
                            if(distance>(FoodHitTolerance+.05f)*(FoodHitTolerance+.05f))continue;
                            foodHitCandidates.Add(new FoodHitCandidate{point=world(nearest),inside=world(edge.inside*(item.radius*2)),item=item,distance=distance,layer=layer});
                        }
                    }
                }
                // Squared-distance epsilon covers float screen/board round trips
                // (<0.0005 logical pixels), not an additional touch margin.
                string selected=null;float best=FoodHitTolerance*FoodHitTolerance+.01f;int bestLayer=-1;
                foreach(var candidate in foodHitCandidates)
                {
                    // Recheck the real bilinear alpha predicate; move just inside its
                    // threshold rather than accepting a transparent marching edge.
                    var witness=candidate.point;var center=World.ItemPosition(candidate.item.itemId);
                    bool opaque(Vector2 p){var local=PlateItemTransform.ToLocalOffset(candidate.item,p.x-center.x,p.y-center.y);return Mathf.Abs(local.x)<=candidate.item.radius&&Mathf.Abs(local.y)<=candidate.item.radius&&OpaqueHit(candidate.item,new Vector2(local.x,local.y));}
                    // Refine against the original predicate, not the cached linear
                    // approximation, so a true contour at 10 is in and 10.1 is out.
                    var inside=witness;
                    for(float inset=.002f;!opaque(inside)&&inset<2f;inset*=2)inside=Vector2.MoveTowards(witness,candidate.inside,inset);
                    if(!opaque(inside))continue;
                    var outside=point;
                    for(int iteration=0;iteration<18;iteration++){var middle=(outside+inside)*.5f;if(opaque(middle))inside=middle;else outside=middle;}
                    witness=inside;float distance=(witness-point).sqrMagnitude;
                    if(distance>best||Mathf.Abs(distance-best)<.001f&&candidate.layer<=bestLayer)continue;
                    if(World.Hit(witness,OpaqueHit)!=candidate.item.itemId)
                    {
                        // Collider edge arithmetic can place the exact boundary just
                        // outside its box. Validate an interior witness without adding
                        // that inward offset to the contour-distance threshold.
                        witness+=(candidate.inside-witness).normalized*.001f;
                        if(World.Hit(witness,OpaqueHit)!=candidate.item.itemId)continue;
                    }
                    if(World.TopPlateAt(witness)!=plate||!PlateCrop.Contains(witness))continue;
                    var screen=RectTransformUtility.WorldToScreenPoint(null,board.TransformPoint(new Vector3(witness.x,-witness.y,0)));
                    if(!viewport.Contains(screen)||ScreenBlockedByUI(screen,pointerId))continue;
                    selected=candidate.item.itemId;best=distance;bestLayer=candidate.layer;
                }
                return selected;
            }
            finally{TolerantHitMilliseconds=watch.Elapsed.TotalMilliseconds;}
        }
        sealed class ClickCache
        {
            public ViewItem item;
            public PlatePresentationWorld.PlateBody body;
            public readonly List<Vector2> witnesses=new List<Vector2>(3);
            public int stamp;
            public bool initialized,negative;
            public float left,right,top,bottom,x,y,dx,dy;
        }
        readonly Dictionary<string,ClickCache> clickCache=new Dictionary<string,ClickCache>();
        readonly List<ClickCache> clickWork=new List<ClickCache>();
        string clickSession;
        long clickGeneration;
        int clickCursor;
        public int ClickCacheLastSamples {get;private set;}
        public double ClickCacheLastMilliseconds {get;private set;}
        public int ClickReleaseFallbacks {get;private set;}
        public double ClickReleaseFallbackMilliseconds {get;private set;}
        bool ClickObservationAvailable=>LastSnapshot!=null&&!ShuffleFeedbackActive&&!FriendBoardVisible&&foreground&&canvasRoot&&canvasRoot.gameObject.activeInHierarchy&&LastSnapshot.phase==ViewPhase.Running&&LastSnapshot.pauseReasons==ViewPauseReasons.None&&!(modal&&modal.gameObject.activeInHierarchy);
        void RefreshClickWork()
        {
            if(clickSession!=LastSnapshot.sessionId||clickGeneration!=LastSnapshot.sessionGeneration){clickCache.Clear();clickSession=LastSnapshot.sessionId;clickGeneration=LastSnapshot.sessionGeneration;clickCursor=0;}
            clickWork.Clear();
            foreach(var body in World.Bodies)foreach(var item in body.data.items)
            {
                if(pending.Contains(item.itemId))continue;
                if(!clickCache.TryGetValue(item.itemId,out var entry)){entry=new ClickCache();clickCache.Add(item.itemId,entry);}
                entry.item=item;entry.body=body;clickWork.Add(entry);
            }
            if(clickCache.Count>256){var live=new HashSet<string>(clickWork.Select(e=>e.item.itemId));foreach(var key in clickCache.Keys.ToArray())if(!live.Contains(key))clickCache.Remove(key);}
        }
        // Only overlapping plate geometry can invalidate a certificate. Translation
        // inside the crop keeps same-plate occlusion certificates useful while falling.
        int ClickGeometryStamp(ClickCache e,Vector2 center)
        {
            unchecked
            {
                int h=17;void add(float v){h=h*31+v.GetHashCode();}
                var ext=PlateItemTransform.HalfExtents(e.item);
                add(Mathf.Max(-ext.x,PlateCrop.xMin-center.x));add(Mathf.Min(ext.x,PlateCrop.xMax-center.x));
                add(Mathf.Max(-ext.y,PlateCrop.yMin-center.y));add(Mathf.Min(ext.y,PlateCrop.yMax-center.y));
                foreach(var body in World.Bodies)
                {
                    var relative=body==e.body?-new Vector2(e.item.x,e.item.y):World.Position(body)-center;
                    if(body!=e.body&&(Mathf.Abs(relative.x)>body.data.radius+ext.x||Mathf.Abs(relative.y)>body.data.radius+ext.y))continue;
                    h=h*31+body.data.plateId.GetHashCode();add(relative.x);add(relative.y);add(body.data.radius);
                    foreach(var item in body.data.items){h=h*31+item.itemId.GetHashCode();add(item.x);add(item.y);add(item.radius);add(item.rotationDegrees);h=h*31+item.foodId;var uv=ItemUV(item);add(uv.x);add(uv.y);add(uv.width);add(uv.height);var tex=foods[item.foodId];h=h*31+(tex?tex.GetInstanceID():0);}
                }
                return h;
            }
        }
        void PrepareClickScan(ClickCache e,Vector2 center)
        {
            int stamp=ClickGeometryStamp(e,center);if(e.initialized&&stamp==e.stamp)return;
            e.initialized=true;e.stamp=stamp;e.negative=false;
            var ext=PlateItemTransform.HalfExtents(e.item);
            e.left=Mathf.Max(-ext.x,PlateCrop.xMin-center.x);e.right=Mathf.Min(ext.x,PlateCrop.xMax-center.x);
            e.top=Mathf.Max(-ext.y,PlateCrop.yMin-center.y);e.bottom=Mathf.Min(ext.y,PlateCrop.yMax-center.y);
            var tex=foods[e.item.foodId];var uv=ItemUV(e.item);
            e.dx=2*e.item.radius/Mathf.Max(1,(tex?tex.width:32)*uv.width);e.dy=2*e.item.radius/Mathf.Max(1,(tex?tex.height:32)*uv.height);
            e.x=e.left;e.y=e.top;e.negative=e.left>=e.right||e.top>=e.bottom;
            // Identical later food has the identical alpha mask: every accepted
            // point of this food necessarily belongs to the later one instead.
            bool after=false;
            foreach(var other in e.body.data.items)
            {
                if(other.itemId==e.item.itemId){after=true;continue;}
                if(after&&other.foodId==e.item.foodId&&other.x==e.item.x&&other.y==e.item.y&&other.radius==e.item.radius&&other.rotationDegrees==e.item.rotationDegrees&&ItemUV(other)==ItemUV(e.item))e.negative=true;
            }
        }
        bool CachedWitness(ClickCache e,Vector2 center)
        {
            for(int i=0;i<e.witnesses.Count;i++)if(ClickablePoint(e.item.itemId,center+e.witnesses[i]))return true;
            return false;
        }
        void RememberClickWitness(ClickCache e,Vector2 relative)
        {
            if(e.witnesses.Contains(relative))return;
            if(e.witnesses.Count==3)e.witnesses.RemoveAt(0);e.witnesses.Add(relative);
        }
        void TickClickabilityCache()
        {
            ClickCacheLastSamples=0;ClickCacheLastMilliseconds=0;
            if(!ClickObservationAvailable)return;
            var watch=System.Diagnostics.Stopwatch.StartNew();RefreshClickWork();
            int visits=0;
            while(clickWork.Count>0&&visits<clickWork.Count*8&&ClickCacheLastSamples+6<=128&&watch.Elapsed.TotalMilliseconds<1.5)
            {
                clickCursor%=clickWork.Count;var e=clickWork[clickCursor];clickCursor=(clickCursor+1)%clickWork.Count;visits++;
                var center=World.Position(e.body)+new Vector2(e.item.x,e.item.y);PrepareClickScan(e,center);
                if(e.negative)continue;
                // Conservative bound: three witnesses, middle, cell, geometry.
                ClickCacheLastSamples+=6;
                if(CachedWitness(e,center))continue;
                var middle=new Vector2((e.left+e.right)*.5f,(e.top+e.bottom)*.5f);
                if(ClickablePoint(e.item.itemId,center+middle)){RememberClickWitness(e,middle);continue;}
                var point=new Vector2((e.x+Mathf.Min(e.right,e.x+e.dx))*.5f,(e.y+Mathf.Min(e.bottom,e.y+e.dy))*.5f);
                if(ClickablePoint(e.item.itemId,center+point)){RememberClickWitness(e,point);continue;}
                // UI occlusion is transient; never certify a geometric negative from it.
                if(World.Hit(center+point,OpaqueHit)==e.item.itemId)continue;
                e.x+=e.dx;if(e.x>=e.right){e.x=e.left;e.y+=e.dy;}
                if(e.y>=e.bottom)e.negative=true;
            }
            ClickCacheLastMilliseconds=watch.Elapsed.TotalMilliseconds;
        }
        public ViewClickabilityObservation CaptureClickability()
        {
            if(!ClickObservationAvailable)return null;
            RefreshClickWork();var yes=new List<string>();var unknown=new List<string>();
            foreach(var e in clickWork)
            {
                var center=World.Position(e.body)+new Vector2(e.item.x,e.item.y);PrepareClickScan(e,center);
                if(CachedWitness(e,center)){yes.Add(e.item.itemId);continue;}
                // One bounded current candidate also covers newly spawned visible food.
                var middle=new Vector2((e.left+e.right)*.5f,(e.top+e.bottom)*.5f);
                if(!e.negative&&ClickablePoint(e.item.itemId,center+middle)){RememberClickWitness(e,middle);yes.Add(e.item.itemId);}
                else if(!e.negative)unknown.Add(e.item.itemId);
            }
            return new ViewClickabilityObservation{clickable=yes.ToArray(),unknown=unknown.ToArray()};
        }
        bool ValidateReleasedFood(PressedItem press)
        {
            if(!ClickObservationAvailable||pending.Contains(press.itemId))return false;
            var center=World.ItemPosition(press.itemId);
            if(ClickablePoint(press.itemId,center+press.hitOffset))return true;
            if(clickCache.TryGetValue(press.itemId,out var e)&&CachedWitness(e,center))return true;
            var watch=System.Diagnostics.Stopwatch.StartNew();ClickReleaseFallbacks++;
            bool accepted=IsItemClickable(press.itemId);ClickReleaseFallbackMilliseconds+=watch.Elapsed.TotalMilliseconds;return accepted;
        }
    }
}
