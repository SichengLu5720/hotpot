using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Determinism;
using HotpotSort.Presentation;

namespace HotpotSort.Bootstrap
{
    // Versioned real-core transport adapter. No fallback to prototype inventory or fixture state.
    public static class DailyViewMapper
    {
        public static float PlateRadius(int count) { return Math.Min(63, 33 + count * 6); }
        public static float SpawnX(int successfulCount) { return successfulCount % 2 == 0 ? 150 : 260; }
        public static int PendingHead(GameSnapshot snapshot)
        {
            var state = CanonicalJson.Map(CanonicalJson.Parse(snapshot.CanonicalStateJson));
            var ids = CanonicalJson.Array(state["pendingPlateIds"]);
            return ids.Count == 0 ? 0 : CanonicalJson.Int(ids[0]);
        }
        public static ViewUpdate Map(GameSnapshot snapshot, GameEventBatch batch, DailyContent content, double activeSeconds)
        {
            if(snapshot.SchemaVersion != DailySession.StateSchema) throw new InvalidOperationException("Unsupported core view schema");
            var state=CanonicalJson.Map(CanonicalJson.Parse(snapshot.CanonicalStateJson));
            var mapping=CanonicalJson.Map(state["mapping"]);
            var raw=CanonicalJson.Array(state["items"]).Select(CanonicalJson.Map).ToArray();
            Func<string,int> food = kind => kind==null ? -1 : int.Parse(((string)mapping[kind]).Substring(5),CultureInfo.InvariantCulture);
            var byId=raw.ToDictionary(i=>CanonicalJson.Int(i["itemId"]));
            Func<int,ViewItem> item = id => new ViewItem { itemId=id.ToString(CultureInfo.InvariantCulture), foodId=food((string)byId[id]["kind"]),radius=16 };
            var view=new ViewSnapshot
            {
                sessionId=snapshot.SessionId,
                revision=long.Parse(snapshot.TransactionId,CultureInfo.InvariantCulture),
                eventSeq=long.Parse(snapshot.EventSequence,CultureInfo.InvariantCulture),
                phase=Phase(snapshot.Status),
                buffer=CanonicalJson.Array(state["buffer"]).Select(x=>x==null?null:item(CanonicalJson.Int(x))).ToArray(),
                orders=CanonicalJson.Array(state["orders"]).Select(x=>
                { var o=CanonicalJson.Map(x); return new ViewOrder { slot=CanonicalJson.Int(o["slotId"]),foodId=food((string)o["kind"]),enabled=(string)o["state"]!="Locked",count=CanonicalJson.Int(o["filled"]),required=3 }; }).ToArray(),
                plates=CanonicalJson.Array(state["activePlateIds"]).Select(x=>
                {
                    int id=CanonicalJson.Int(x), count=CanonicalJson.Array(state["plateSizes"]).Select(CanonicalJson.Map).Where(p=>CanonicalJson.Int(p["plateId"])==id).Select(p=>CanonicalJson.Int(p["size"])).Single();
                    float radius=PlateRadius(count);
                    var items=raw.Where(i=>CanonicalJson.Int(i["plateId"])==id && (string)i["location"]=="ActiveAvailable").Select(i=>
                    {
                        var v=item(CanonicalJson.Int(i["itemId"])); int index=CanonicalJson.Int(i["sourceIndex"]);
                        double angle=2*Math.PI*index/count-Math.PI/2;
                        float orbit=count==1?0:radius*.47f;
                        v.x=(float)Math.Cos(angle)*orbit; v.y=(float)Math.Sin(angle)*orbit; return v;
                    }).ToArray();
                    return new ViewPlate { plateId=id.ToString(CultureInfo.InvariantCulture),x=SpawnX(id),y=304+radius,radius=radius,items=items };
                }).ToArray()
            };
            var stats=CanonicalJson.Map(state["statistics"]);
            view.message=(string)stats["failureCode"];
            view.facts=new[]
            {
                new ViewFact { label="已完成订单",value=Convert.ToString(stats["completedOrderCount"],CultureInfo.InvariantCulture) },
                new ViewFact { label="已处理食材",value=Convert.ToString(stats["processedItemCount"],CultureInfo.InvariantCulture) },
                new ViewFact { label="有效点击",value=Convert.ToString(stats["acceptedTapCount"],CultureInfo.InvariantCulture) },
                new ViewFact { label="最大暂存",value=Convert.ToString(stats["maxBufferCount"],CultureInfo.InvariantCulture) },
                new ViewFact { label="用时",value=activeSeconds.ToString("0.0",CultureInfo.InvariantCulture)+" 秒" }
            };
            var events=batch==null?new ViewEvent[0]:batch.CanonicalEvents.Select(text=>
            {
                var e=CanonicalJson.Map(CanonicalJson.Parse(text)); var d=CanonicalJson.Map(e["data"]);
                Func<string,int> number=key=>d.ContainsKey(key)?CanonicalJson.Int(d[key]):-1;
                Func<string,string> value=key=>d.ContainsKey(key)?Convert.ToString(d[key],CultureInfo.InvariantCulture):null;
                return new ViewEvent { sessionId=snapshot.SessionId, sequence=long.Parse((string)e["eventSeq"],CultureInfo.InvariantCulture),transactionId=(string)e["transactionId"],kind=(string)e["type"],itemId=value("itemId"),ingredientId=value("ingredientId"),plateId=value("plateId"),sourceContainer=value("sourceContainer"),sourceSlot=number("sourceSlot"),targetContainer=value("targetContainer"),targetSlot=number("targetSlot"),slot=number("slotId"),filledBefore=number("filledBefore"),filledAfter=number("filledAfter"),orderIdentity=number("orderIdentity") };
            }).ToArray();
            return new ViewUpdate { snapshot=view,events=events };
        }
        private static ViewPhase Phase(GameStatus status)
        {
            switch(status)
            {
                case GameStatus.Running:return ViewPhase.Running;
                case GameStatus.Paused:return ViewPhase.Paused;
                case GameStatus.Won:return ViewPhase.Won;
                case GameStatus.Failed:return ViewPhase.Overflow;
                case GameStatus.Aborted:return ViewPhase.Aborted;
                default:return ViewPhase.Entry;
            }
        }
    }
}
