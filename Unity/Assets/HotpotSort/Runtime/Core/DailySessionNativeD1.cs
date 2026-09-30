using System;
using System.Collections.Generic;
using System.Linq;
using HotpotSort.Determinism;
namespace HotpotSort.Core
{
    public sealed partial class DailySession
    {
        int nativeCollected;
        float NativeProgress => (float)nativeCollected/TotalItems;
        DailyDirector.Decision ChooseNative(int slot,bool select,string swapFromKind=null)
        {
            var external=items.Where(x=>x.Location=="Buffer"||x.Location=="ActiveAvailable"||x.Location=="Pending").ToArray();
            var existing=new HashSet<string>(orders.Where((o,i)=>i!=slot&&o.Enabled&&o.Kind!=null).Select(o=>o.Kind));
            var reserved=new HashSet<int>();
            foreach(var o in orders.Where((o,i)=>i!=slot&&o.Enabled&&o.Kind!=null))
                foreach(var item in external.Where(x=>x.Kind==o.Kind&&!reserved.Contains(x.Id)).OrderBy(x=>x.Location=="Buffer"?0:x.Location=="ActiveAvailable"?1:2).ThenBy(x=>x.Id).Take(Math.Max(0,3-o.Items.Count)))reserved.Add(item.Id);
            Func<CoreItem,bool> supported=x=>x.Location=="Buffer"||x.Location=="ActiveAvailable"&&((commandClickability?.Contains(x.Id)??false)||(commandNextLayer?.Contains(x.Id)??false));
            var legal=external.Where(x=>!reserved.Contains(x.Id)).GroupBy(x=>x.Kind).Where(g=>g.Count()>=3&&!existing.Contains(g.Key)).Select(g=>g.Key).ToArray();
            Func<string,int> support=k=>external.Count(x=>x.Kind==k&&!reserved.Contains(x.Id)&&supported(x));
            var available=external.Where(supported).GroupBy(x=>x.Kind).ToDictionary(g=>g.Key,g=>g.Count());
            int danger=0;
            foreach(var o in orders.Where((o,i)=>i!=slot&&o.Enabled&&o.Kind!=null&&o.Items.Count<3))
            {
                int count=available.TryGetValue(o.Kind,out var n)?n:0,need=3-o.Items.Count;
                if(count<need)danger++;
                available[o.Kind]=Math.Max(0,count-need);
            }
            int threshold=Math.Max(0,NormalPotCount-1),roll=-1;
            bool firstTen=CumulativeCompletedOrders<10;
            string branch="Base";HashSet<string> allowed=null;
            if(swapFromKind!=null)
            {
                allowed=new HashSet<string>(legal.Where(k=>k!=swapFromKind&&support(k)>=3));branch="SwapComplete";
            }
            else if(select&&danger>=threshold&&(firstTen||(roll=(int)directorRng.NextBounded(100))<orderProtectionPercentAtStart))
            {
                var complete=legal.Where(k=>support(k)>=3).ToArray();
                if(complete.Length>0){allowed=new HashSet<string>(complete);branch="Complete";}
                else
                {
                    int best=legal.Length==0?0:legal.Max(support);
                    if(best>0){allowed=new HashSet<string>(legal.Where(k=>support(k)==best));branch="MostSupported";}
                    else branch="AllZeroBase";
                }
            }
            var choice=nativeDirector.Choose(slot,items,orders,buffer,pending,active,commandNativeAbove,directorRng,NativeProgress,allowed,select,plateSizes);
            CanonicalJson.Map(choice.Diagnostic)["protection"]=CanonicalJson.Object("percent",orderProtectionPercentAtStart,"firstTen",firstTen,"dangerCount",danger,"threshold",threshold,"roll",roll,"branch",branch);
            return choice;
        }
        void ExchangeNative(string kind,int slot)
        {
            if(kind==null||commandNativeAbove==null)return;
            foreach(var exchange in NativeD1Director.Exchange(kind,slot,items,orders,buffer,pending,active,commandNativeAbove))
                Emit("NativeTargetExchanged",exchange);
        }
    }
}
