using System;
using System.Linq;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Session;
using UnityEngine;

namespace HotpotSort.Bootstrap
{
    public sealed partial class DailyProductionComposition
    {
        bool redealing;
        int redealSupplied;
        bool redealFilling;
        Vector2 normalSupplySpawn;
        float redealNext,revealWait;
        Vector2 redealSpawn;
        bool TryChooseRedealSpawn(float radius)
        {
            // Choose an irregular horizontal position, but never create a collider
            // inside another plate. If the entrance is full, wait for native motion.
            float y=HotpotSort.Presentation.PlateSupplyGeometry.SpawnY(radius,view.PlateCrop.yMin);
            for(int attempt=0;attempt<48;attempt++)
            {
                float x=radius+2+(float)spawnRandom.NextDouble()*(416-2*radius);
                var point=new Vector2(x,y);
                if(!view.World.HasSpawnSpace(point,radius))continue;
                redealSpawn=point;return true;
            }
            // Exhaust the row as well: random misses must not be mistaken for a full entrance.
            for(float x=radius+2;x<=418-radius;x+=4)
            {
                var point=new Vector2(x,y);if(!view.World.HasSpawnSpace(point,radius))continue;
                redealSpawn=point;return true;
            }
            return false;
        }
        bool BeginRedeal()
        {
            if(redealing||current==null||!current.CanRedeal)return false;
            normalSpawnHead=0;redealing=true;redealFilling=true;redealSupplied=0;redealNext=revealWait=0;
            view.RedealActive=true;controller.SetRedealActive(true);ResetSpawnPresentation();
            spawnRandom=new System.Random(Guid.NewGuid().GetHashCode());
            var result=current.Redeal(Boundary);
            if(!result.Accepted||result.Reason!=null){EndRedeal();return false;}
            view.World.ConfigureSupply(view.PlateCrop.yMin);
            return true;
        }
        void EndRedeal()
        {
            redealing=false;if(view)view.RedealActive=false;controller?.SetRedealActive(false);
        }
        void ResetRedeal()
        {redealing=false;redealSupplied=0;if(view)view.RedealActive=false;}
        bool RedealHasClickableTarget()
        {
            var observed=view.CaptureClickability();if(observed==null)return false;
            var needs=shown.orders.Where(o=>o.enabled&&o.count<o.required).Select(o=>o.foodId).ToArray();
            return shown.plates.SelectMany(p=>p.items).Any(i=>needs.Contains(i.foodId)&&observed.clickable.Contains(i.itemId));
        }
        void TickRedeal()
        {
            if(!redealing||current==null||controller==null)return;
            if(current.Snapshot.Status!=GameStatus.Running||controller.Pauses!=PauseReasons.None||!view.PresentationForeground)return;
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);redealNext-=dt;
            view.World.ConfigureSupply(view.PlateCrop.yMin);
            if(redealFilling)
            {
                if(redealNext>0)return;
                int sent=0;
                // Gate once per batch: a sibling born this frame must not block the batch.
                bool gateClear=view.World.ObserveHeightGate(shown.revision).canSupply;
                while(gateClear&&sent<3)
                {
                    int next=DailyViewMapper.PendingHead(current.Snapshot);if(next==0)break;
                    if(!TryChooseRedealSpawn(DailyViewMapper.PlateRadius(current.PlateSize(next))))break;
                    var result=current.Supply(new SupplyObservation(++supplySequence,Boundary,true,true));
                    if(!result.Accepted||result.Reason!=null)break;
                    sent++;redealSupplied++;
                }
                if(sent>0){redealNext=.1f;revealWait=0;return;}
                bool exhausted=DailyViewMapper.PendingHead(current.Snapshot)==0;
                // Input is paused, physics is not. A moving incoming batch is not a full board.
                if(!exhausted&&!view.World.HasStableSupplyBoard(shown.plates.Length))return;
                redealFilling=false;revealWait=0;
            }
            revealWait+=dt;
            if(revealWait<.5f)return;
            if(!view.World.HasStableSupplyBoard(shown.plates.Length)&&revealWait<4f)return;
            if(RedealHasClickableTarget()){EndRedeal();return;}
            if(view.TryExposeRedealTarget(current.RedealGuaranteedItemId.ToString()))
            {if(RedealHasClickableTarget()){EndRedeal();return;}}
            // A crowded layout may need fewer physical plates. Keep
            // whole plates in the data queue until there is room for the target.
            var other=shown.plates.LastOrDefault(p=>p.plateId!=current.RedealGuaranteedPlateId.ToString());
            if(other!=null)
            {current.ReturnRedealPlate(int.Parse(other.plateId),Boundary);revealWait=.4f;return;}
            Debug.LogError("Redeal target could not be exposed");EndRedeal();
            view.ShowNotice("打乱未完成","目标食材暂时无法显示，请重试。");
        }
    }
}
