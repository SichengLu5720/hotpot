using System;
using System.IO;
using System.Linq;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Bootstrap;
using HotpotSort.Presentation;

static class Task027ProgressQa
{
    static void Main()
    {
        var factory=DailySessionFactory.FromProductionJson(File.ReadAllText("Unity/Assets/HotpotSort/Content/Daily/"+DailyContent.RuntimeFileName));
        var context=new ChallengeContext("20260925",factory.Content.ContentVersion,factory.ConfigurationDigest,"task027",0);
        using(var session=factory.CreateDailySession(context))
        {
            var initial=DailyViewMapper.Map(session.Snapshot,null,factory.Content,0).snapshot;
            if(initial.totalOrders!=61||initial.completedOrders!=0)throw new Exception("Initial progress must be 0/61");
            session.UnlockThird(600000);
            var terminal=DailyViewMapper.Map(session.Snapshot,null,factory.Content,600).snapshot;
            if(terminal.phase!=ViewPhase.Overflow||terminal.totalOrders!=initial.totalOrders||terminal.completedOrders!=0)throw new Exception("Timeout must retain progress denominator");
        }
        var all=factory.Content.Plates.SelectMany(p=>p.Kinds).Select((kind,index)=>new{kind,id=index+1}).ToArray();
        var triples=all.GroupBy(i=>i.kind).SelectMany(g=>g.Select((x,i)=>new{x,i}).GroupBy(x=>x.i/3).Select(t=>t.Select(x=>x.x.id).ToArray())).ToArray();
        using(var partial=factory.CreateFixtureSession(context,new DailyFixture(50,new int?[5],new[]{new FixtureOrder(null,new int[0]),new FixtureOrder(null,new int[0])},triples.Take(23).SelectMany(x=>x))))
        {
            var snapshot=DailyViewMapper.Map(partial.Snapshot,null,factory.Content,0).snapshot;
            if(snapshot.totalOrders!=61||snapshot.completedOrders!=23)throw new Exception("Partial progress must be 23/61");
        }
        Console.WriteLine("TASK027_PROGRESS_QA_PASS initial=0/61 partial=23/61 timeout=0/61");
    }
}
