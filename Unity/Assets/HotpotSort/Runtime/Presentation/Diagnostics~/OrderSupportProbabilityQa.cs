using System;
using System.Collections.Generic;
using System.Linq;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Determinism;
using HotpotSort.Replay;
static partial class OrderSupportQa
{
    static Pcg32 ForcedSupportRng(){for(ulong s=1;;s++)if(new Pcg32(s).NextBounded(5)==0)return new Pcg32(s);}
    static void ProbabilityChecks(DailySessionFactory factory)
    {
        var started=DateTime.UtcNow;var director=new DailyDirector(factory.Content);
        var items=Enumerable.Range(1,15).Select(id=>new CoreItem{Id=id,PlateId=1,SourceIndex=id-1,Kind=((char)('A'+(id-1)/3)).ToString(),Location="ActiveAvailable"}).ToList();
        var buffer=new int?[5];var pending=new List<int>();
        foreach(var mode in new[]{"Below","Threshold","AllDanger","AdIntermediate","NormalThree"})
        {
            var orders=new[]{new CoreOrder{Enabled=true},new CoreOrder{Enabled=true,Kind="B"},new CoreOrder{Enabled=mode=="AllDanger"||mode=="AdIntermediate"||mode=="NormalThree",Kind="C"},new CoreOrder()};
            var visible=new HashSet<int>{1,2,3};
            if(mode=="Below")visible.UnionWith(new[]{4,5,6});
            if(mode=="AdIntermediate"||mode=="NormalThree")visible.UnionWith(new[]{7,8,9});
            int triggered=0,normal=mode=="NormalThree"?3:2;
            for(int index=0;index<10000;index++)
            {
                ulong seed=Pcg32.DailySeed(new DateTime(2000,1,1).AddDays(index).ToString("yyyy-MM-dd"),factory.Content.ContentVersion);
                seed=Pcg32.StreamSeed(seed,"DirectorRng");
                var rng=new Pcg32(seed);var before=CanonicalJson.Write(rng.Snapshot());
                director.Choose(0,items,orders,buffer,pending,rng,false,visible,policyVersion:6,normalPotCount:normal);
                Check(before==CanonicalJson.Write(rng.Snapshot()),"query consumes no probability RNG");
                var d=director.Choose(0,items,orders,buffer,pending,rng,clickable:visible,policyVersion:6,normalPotCount:normal);
                var support=Support(d);bool below=mode=="Below"||mode=="NormalThree";
                var baselineRng=new Pcg32(seed);
                if(!below)
                {
                    int roll=(int)baselineRng.NextBounded(5);Check(CanonicalJson.Int(support["roll"])==roll,"recorded exact deterministic roll");
                    if(roll==0){triggered++;Check((string)support["branch"]=="Complete"&&d.Kind=="A","20% support branch");}
                    else Check((string)support["branch"]=="ProbabilityBase","no all-danger override");
                }
                else Check(support["roll"]==null&&(string)support["branch"]=="BelowThresholdBase","below threshold zero probability draws");
                if(below||CanonicalJson.Int(support["roll"])!=0)
                {
                    var pure=director.Choose(0,items,orders,buffer,pending,baselineRng);
                    Check(pure.Kind==d.Kind&&CanonicalJson.Write(baselineRng.Snapshot())==CanonicalJson.Write(rng.Snapshot()),"base branch exact D1 after probability draw");
                }
                var retry=director.Choose(0,items,orders,buffer,pending,new Pcg32(seed),clickable:visible,policyVersion:6,normalPotCount:normal);
                Check(CanonicalJson.Write(retry.Diagnostic)==CanonicalJson.Write(d.Diagnostic),"same-day full decision reproducible");
                var swap=director.Choose(0,items,orders,buffer,pending,new Pcg32(seed),clickable:visible,policyVersion:6,normalPotCount:normal,swapFromKind:"D");
                Check(Support(swap)["roll"]==null&&(string)Support(swap)["branch"]=="SwapComplete"&&swap.Kind=="A","swap never rolls and always supported");
            }
            Check(mode=="Below"||mode=="NormalThree"?triggered==0:triggered>=1850&&triggered<=2150,"large sample trigger rate");
            Console.WriteLine("TASK034 distribution "+mode+" samples=10000 triggered="+triggered+" percent="+(triggered/100.0));
        }
        // Actual completion -> refill command replay, not just direct director calls.
        var all=factory.Content.Plates.SelectMany(p=>p.Kinds).Select((kind,i)=>new{kind,id=i+1}).ToArray();
        var a=all.Where(x=>x.kind=="A").Select(x=>x.id).ToArray();var c=all.Where(x=>x.kind=="C").Take(3).Select(x=>x.id).ToArray();
        for(int day=0;day<30;day++)
        {
            var context=new ChallengeContext(new DateTime(2026,1,1).AddDays(day).ToString("yyyy-MM-dd"),factory.Content.ContentVersion,factory.ConfigurationDigest,"test",0);
            var fixture=new DailyFixture(50,new int?[5],new[]{new FixtureOrder("A",a.Take(2)),new FixtureOrder("B",Array.Empty<int>())},Array.Empty<int>());
            string expected=null;
            for(int retry=0;retry<2;retry++)
            {
                var session=factory.CreateFixtureSession(context,fixture);
                var obs=Observe(session,c.Append(a[2]));
                if(retry==1)for(int query=0;query<5;query++)session.GetSwapOrderTargets(obs);
                Check(session.Tap(new TapCommand(a[2],1,1,true,obs)).Accepted,"refill trigger accepted");
                var replay=DailyReplay.Run(factory,session.ExportReplay());
                Check(replay.Success&&replay.Session.StateHash==session.StateHash,"probability refill replay "+replay.Error);
                if(expected!=null)Check(expected==session.StateHash,"repeat with queries identical");expected=session.StateHash;
            }
        }
        Console.WriteLine("TASK034 probability checks elapsedSeconds="+(DateTime.UtcNow-started).TotalSeconds.ToString("F2"));
    }
}
