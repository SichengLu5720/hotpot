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
