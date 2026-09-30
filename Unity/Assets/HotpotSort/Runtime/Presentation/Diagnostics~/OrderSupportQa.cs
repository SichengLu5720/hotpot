using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Determinism;
using HotpotSort.Replay;
static partial class OrderSupportQa
{
    static int checks;
    static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
    static Dictionary<string,object> Support(DailyDirector.Decision d)=>CanonicalJson.Map(CanonicalJson.Map(d.Diagnostic)["recentSupport"]);
    static ClickableObservation Observe(DailySession s,IEnumerable<int> ids,IEnumerable<int> next=null)=>new ClickableObservation(s.Snapshot.SessionId,s.Snapshot.TransactionId,ids,nextLayerItemIds:next);
    static void Main(string[] args)
    {
        var factory=DailySessionFactory.FromProductionJson(File.ReadAllText(args[0]));var director=new DailyDirector(factory.Content);
        ProbabilityChecks(factory);
        var items=Enumerable.Range(1,12).Select(id=>new CoreItem{Id=id,PlateId=1,SourceIndex=id-1,Kind=((char)('A'+(id-1)/3)).ToString(),Location="ActiveAvailable"}).ToList();
        var orders=new[]{new CoreOrder{Enabled=true},new CoreOrder{Enabled=true,Kind="B"},new CoreOrder(),new CoreOrder()};
        var buffer=new int?[5];var pending=new List<int>();
        for(ulong seed=1;seed<=200;seed++)
        {
            var x=new Pcg32(seed);var y=new Pcg32(seed);
            var d=director.Choose(0,items,orders,buffer,pending,x,clickable:new HashSet<int>{4,5,6},policyVersion:6);
            var b=director.Choose(0,items,orders,buffer,pending,y);
            Check(d.Kind==b.Kind&&CanonicalJson.Write(x.Snapshot())==CanonicalJson.Write(y.Snapshot()),"zero danger exact D1 draws");
            Check((string)Support(d)["branch"]=="BelowThresholdBase","zero danger base");
        }
        var one=director.Choose(0,items,orders,buffer,pending,ForcedSupportRng(),clickable:new HashSet<int>{1},nextLayer:new HashSet<int>{2,3},policyVersion:6);
        Check(one.Kind=="A"&&(string)Support(one)["branch"]=="Complete","direct next layer completes A");
        Check(CanonicalJson.Int(Support(one)["dangerCount"])==1,"one danger counted");
        var partial=director.Choose(0,items,orders,buffer,pending,ForcedSupportRng(),clickable:new HashSet<int>{1,7},nextLayer:new HashSet<int>{2},policyVersion:6);
        Check(partial.Kind=="A"&&(string)Support(partial)["branch"]=="MostSupported","no complete maximum supported");
        var zero=director.Choose(0,items,orders,buffer,pending,ForcedSupportRng(),clickable:new HashSet<int>(),policyVersion:6);
        Check((string)Support(zero)["branch"]=="AllZeroBase","all zero D1");
        orders[2].Enabled=true;orders[2].Kind="C";
        var two=director.Choose(0,items,orders,buffer,pending,ForcedSupportRng(),clickable:new HashSet<int>{1,2,3},policyVersion:6,normalPotCount:3);
        Check(CanonicalJson.Int(Support(two)["dangerCount"])==2&&(string)Support(two)["branch"]=="Complete","two danger normal three pots");
        var ad=director.Choose(0,items,orders,buffer,pending,ForcedSupportRng(),clickable:new HashSet<int>{1,2,3,7,8,9},policyVersion:6,normalPotCount:2);
        Check(CanonicalJson.Int(Support(ad)["dangerCount"])==1&&CanonicalJson.Int(Support(ad)["threshold"])==1&&ad.Kind=="A","advertised pot does not raise threshold");
        var normal=director.Choose(0,items,orders,buffer,pending,ForcedSupportRng(),clickable:new HashSet<int>{1,2,3,7,8,9},policyVersion:6,normalPotCount:3);
        Check((string)Support(normal)["branch"]=="BelowThresholdBase","normal third raises threshold");
        var all=factory.Content.Plates.SelectMany(p=>p.Kinds.Select(k=>new{kind=k,plate=p.PlateId})).Select((x,i)=>new{x.kind,x.plate,id=i+1}).ToArray();
        Func<string,int[]> ids=k=>all.Where(x=>x.kind==k&&x.plate<50).Select(x=>x.id).ToArray();
        var context=new ChallengeContext("2026-09-26",factory.Content.ContentVersion,factory.ConfigurationDigest,"test",0);
        foreach(var stage in new[]{ChallengeStage.Warmup,ChallengeStage.Formal})
        {
            var s=factory.CreateStage(context,stage,3);Check(s.NormalPotCount==2&&s.UnlockedExtraPotMask==3,"advertised inherited pots preserve normal2");
        }
        foreach(int formalDone in new[]{30,31,48,49})
        {
            var done=all.GroupBy(x=>x.kind).SelectMany(g=>g.Take(g.Count()/3*3)).Take(formalDone*3).Select(x=>x.id).ToArray();
            var f=new DailyFixture(50,new int?[5],new[]{new FixtureOrder(null,Array.Empty<int>()),new FixtureOrder(null,Array.Empty<int>())},done);
            var s=new DailySession(factory,context,f,DailyRulesVersion.RevivalV3,ChallengeStage.Formal);
            Check(s.NormalPotCount==(formalDone<31?2:formalDone<49?3:4),"shared 37/55 progression");
        }
        var fixture=new DailyFixture(49,new int?[]{ids("D")[0],null,null,null,null},new[]{new FixtureOrder("A",ids("A").Take(2)),new FixtureOrder("B",Array.Empty<int>())},Array.Empty<int>());
        string expectedHash=null;
        foreach(int count in new[]{0,1,2})
        {
            var f=new DailyFixture(50,new int?[5],new[]{new FixtureOrder("A",ids("A").Take(count)),new FixtureOrder("B",Array.Empty<int>())},Array.Empty<int>());
            var s=factory.CreateFixtureSession(context,f);var before=s.StateHash;
            s.GetSwapOrderTargets(Observe(s,ids("C").Take(3)));Check(before==s.StateHash,"selection cancellation no core mutation");
            Check(s.BeginSwapOrder(0,s.OrderIdentity(0),1,Observe(s,ids("C").Take(3))).Accepted,"0/1/2 portion swap");
            var t=s.ReadSwapOrderTransfer();Check(t.items.Length==count,"0/1/2 return count");
            Check(s.CompleteSwapOrder(t.token,2,Observe(s,ids("C").Take(3))).Accepted,"0/1/2 target completion");
        }
        for(int retry=0;retry<2;retry++)
        {
            var s=factory.CreateFixtureSession(context,fixture);var observed=Observe(s,ids("C").Take(3));
            string before=s.StateHash;
            Check(s.GetSwapOrderTargets(observed).Contains(0)&&s.StateHash==before,"query legal no state/rng change");
            Check(!s.BeginSwapOrder(0,s.OrderIdentity(0),1,observed,()=>false).Accepted&&s.StateHash==before,"failed charge has no effect");
            int charged=0;var result=s.BeginSwapOrder(0,s.OrderIdentity(0),1,observed,()=>{charged++;return true;});
            Check(result.Accepted&&charged==1&&s.Snapshot.Status==GameStatus.Running,"valid swap exactly one charge no pause");
            var transfer=s.ReadSwapOrderTransfer();Check(transfer.items.Select(i=>i.itemId).SequenceEqual(ids("A").Take(2).Select(i=>i.ToString()))&&transfer.items.Select(i=>i.bufferIndex).SequenceEqual(new[]{1,2}),"ordered left-to-right return");
            Check(!s.Tap(new TapCommand(ids("C")[0],1,2,true)).Accepted&&!s.CanClearBuffer,"transfer input locks");
            Check(s.Supply(new SupplyObservation(1,2,true,true)).Accepted,"supply runs during transfer");
            Check(s.CompleteSwapOrder(transfer.token,3,Observe(s,ids("C").Take(3))).Accepted&&!s.SwapOrderPending,"complete installs target");
            Check(!s.CompleteSwapOrder(transfer.token,4).Accepted,"completion idempotent");
            var state=CanonicalJson.Map(CanonicalJson.Parse(s.Snapshot.CanonicalStateJson));
            Check(CanonicalJson.Array(state["items"]).Count==183,"inventory conserved");
            Check((string)CanonicalJson.Map(CanonicalJson.Array(state["orders"])[0])["kind"]=="C","guaranteed different recent target");
            var replay=DailyReplay.Run(factory,s.ExportReplay());Check(replay.Success&&replay.Session.StateHash==s.StateHash,"swap replay "+replay.Error);
            if(expectedHash!=null)Check(expectedHash==s.StateHash,"same-day retry deterministic");expectedHash=s.StateHash;
        }
        var stale=factory.CreateFixtureSession(context,fixture);var staleObservation=Observe(stale,ids("C").Take(3));stale.Supply(new SupplyObservation(1,1,true,true));
        Check(!stale.BeginSwapOrder(0,stale.OrderIdentity(0),2,staleObservation).Accepted&&!stale.SwapOrderPending,"stale observation no consumption");
        Check(stale.GetSwapOrderTargets(Observe(stale,Array.Empty<int>())).Length==0,"no support no swap");
        var fullFixture=new DailyFixture(50,ids("D").Take(4).Select(i=>(int?)i).Concat(new int?[]{null}),new[]{new FixtureOrder("A",ids("A").Take(2)),new FixtureOrder("B",Array.Empty<int>())},Array.Empty<int>());
        var full=factory.CreateFixtureSession(context,fullFixture);Check(!full.GetSwapOrderTargets(Observe(full,ids("C").Take(3))).Contains(0),"insufficient buffer slots rejects two portion return");
        var timeout=factory.CreateFixtureSession(context,fixture);timeout.BeginSwapOrder(0,timeout.OrderIdentity(0),1,Observe(timeout,ids("C").Take(3)));var token=timeout.ReadSwapOrderTransfer().token;
        Check(timeout.Timeout(600000).Accepted&&!timeout.SwapOrderPending&&!timeout.CompleteSwapOrder(token,600001).Accepted,"timer failure clears lock and rejects stale transfer");
        Console.WriteLine("PASS TASK034 checks="+checks+" policy6 dynamic support, 37/55, swap and replay");
    }
}
