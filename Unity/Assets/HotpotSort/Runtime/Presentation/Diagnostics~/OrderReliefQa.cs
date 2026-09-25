using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HotpotSort.Core;
using HotpotSort.Contracts;
using HotpotSort.Determinism;
using HotpotSort.Replay;
static class OrderReliefQa
{
    static int checks;
    static ClickableObservation Observation(string session,string revision,IEnumerable<int> ids,IEnumerable<int> unknown=null,int policyVersion=3)=>new ClickableObservation(session,revision,ids,unknown,policyVersion);
    static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
    static DailySession PlayStage(DailySessionFactory factory,ChallengeContext context,ChallengeStage stage,int policy)
    {
        var session=factory.CreateStage(context,stage);ulong boundary=1,seq=1;
        Check(session.CumulativeCompletedOrders==(stage==ChallengeStage.Formal?6:0),"stage initial cumulative count");
        for(int p=0;p<(stage==ChallengeStage.Warmup?WarmupContent.Generate(context.ChallengeId,context.ContentVersion).Count:50);p++)
            Check(session.Supply(new SupplyObservation(seq++,boundary++,true,true)).Accepted,"stage supply");
        int protected29=0,base30=0;
        for(int step=0;step<183&&session.Snapshot.Status==GameStatus.Running;step++)
        {
            var state=CanonicalJson.Map(CanonicalJson.Parse(session.Snapshot.CanonicalStateJson));
            var live=CanonicalJson.Array(state["items"]).Select(CanonicalJson.Map).Where(i=>(string)i["location"]=="ActiveAvailable").ToArray();
            var open=CanonicalJson.Array(state["orders"]).Select(CanonicalJson.Map).FirstOrDefault(o=>(string)o["state"]=="Active");
            if(open==null)break;
            var item=live.First(i=>(string)i["kind"]==(string)open["kind"]);
            var otherKinds=CanonicalJson.Array(state["orders"]).Select(CanonicalJson.Map).Where(o=>(string)o["state"]=="Active").Select(o=>(string)o["kind"]).ToArray();
            var visible=live.Where(i=>!otherKinds.Contains((string)i["kind"])).Select(i=>CanonicalJson.Int(i["itemId"]));
            var result=session.Tap(new TapCommand(CanonicalJson.Int(item["itemId"]),seq++,boundary++,true,new ClickableObservation(session.Snapshot.SessionId,session.Snapshot.TransactionId,visible,policyVersion:policy)));
            Check(result.Accepted,"stage completion tap");
            foreach(var raw in result.Events.CanonicalEvents)
            {
                var e=CanonicalJson.Map(CanonicalJson.Parse(raw));if((string)e["type"]!="DirectorEvaluated")continue;
                var d=CanonicalJson.Map(e["data"]);
                if(stage==ChallengeStage.Formal&&policy==5)
                {
                    if(session.CumulativeCompletedOrders==29){Check(d.ContainsKey("clickableRelief"),"formal 23 + warmup 6 protected");protected29++;}
                    if(session.CumulativeCompletedOrders==30){Check(!d.ContainsKey("clickableRelief"),"formal 24 + warmup 6 base");base30++;}
                }
                if(stage==ChallengeStage.Formal&&policy==4)
                {
                    if(session.CumulativeCompletedOrders==20){Check(d.ContainsKey("clickableRelief"),"archived formal14 still protected");protected29++;}
                    if(session.CumulativeCompletedOrders==21){Check(!d.ContainsKey("clickableRelief"),"archived formal15 uses base");base30++;}
                }
            }
        }
        Check(session.CumulativeCompletedOrders==(stage==ChallengeStage.Warmup?6:67),"stage completed cumulative count");
        if(stage==ChallengeStage.Formal)Check(protected29>0&&base30>0,"production stage boundary exercised policy "+policy);
        var replay=DailyReplay.Run(factory,session.ExportReplay());
        Check(replay.Success&&replay.Session.StateHash==session.StateHash&&replay.Session.CoreEventsJson==session.CoreEventsJson,"full stage exact replay policy "+policy);
        return session;
    }
    static void Main(string[] args)
    {
        var factory=DailySessionFactory.FromProductionJson(File.ReadAllText(args[0]));var director=new DailyDirector(factory.Content);
        var items=Enumerable.Range(1,12).Select(id=>new CoreItem{Id=id,PlateId=1,SourceIndex=id-1,Kind=((char)('A'+(id-1)/3)).ToString(),Location="ActiveAvailable"}).ToList();
        var orders=new[]{new CoreOrder{Enabled=true},new CoreOrder{Enabled=true,Kind="B"},new CoreOrder(),new CoreOrder()};
        var buffer=new int?[5];var pending=new List<int>();var visible=new HashSet<int>(new[]{1,2,3,7,8,9});int protectedCount=0,originalCount=0,a=0,c=0;
        for(ulong seed=1;seed<=5000;seed++)
        {
            var rng=new Pcg32(seed);var expected=new Pcg32(seed);uint roll=expected.NextBounded(5);
            var choice=director.Choose(0,items,orders,buffer,pending,rng,clickable:visible);
            if(roll<4){protectedCount++;Check(choice.Fallback=="VisibleRelief80"&&(choice.Kind=="A"||choice.Kind=="C"),"protected legal kind");if(choice.Kind=="A")a++;else c++;}
            else {originalCount++;var legacy=director.Choose(0,items,orders,buffer,pending,expected);Check(choice.Kind==legacy.Kind&&CanonicalJson.Write(rng.Snapshot())==CanonicalJson.Write(expected.Snapshot()),"20 percent original path after branch ticket");}
        }
        Check(protectedCount>3800&&protectedCount<4200&&Math.Abs(a-c)<250,"80/20 and equal pool distribution");
        int weightedA=0,weightedC=0,weightedD=0;
        items[0].Location="Buffer";items[0].Slot=0;buffer[0]=1;
        items[6].Location="Buffer";items[6].Slot=1;buffer[1]=7;
        items[7].Location="Buffer";items[7].Slot=2;buffer[2]=8;
        var weightedVisible=new HashSet<int>(new[]{2,3,9,10,11,12});var row=factory.Content.Rows[3].Weights;
        for(ulong seed=1;seed<=10000;seed++)
        {
            var choice=director.Choose(0,items,orders,buffer,pending,new Pcg32(seed),clickable:weightedVisible,completedOrders:0,policyVersion:3);
            Check(choice.Fallback=="VisibleReliefFirst15","weighted branch retains early trigger");
            if(choice.Kind=="A")weightedA++;else if(choice.Kind=="C")weightedC++;else if(choice.Kind=="D")weightedD++;else Check(false,"weighted pool legal");
        }
        double expectedBuffered=(double)row[0]/(row[0]+row[1]);
        Check(Math.Abs((weightedA+weightedC)/10000.0-expectedBuffered)<.025,"actual table group distribution");
        Check(Math.Abs(weightedA-weightedC)<250,"one versus two buffer portions remains equal within group");
        foreach(int completed in new[]{14,15})for(ulong seed=1;seed<=100;seed++)
        {
            var actualRng=new Pcg32(seed);var expectedRng=new Pcg32(seed);uint ticket=completed<15?0:expectedRng.NextBounded(5);
            var choice=director.Choose(0,items,orders,buffer,pending,actualRng,clickable:weightedVisible,completedOrders:completed,policyVersion:3);
            if(ticket<4)Check(choice.Fallback==(completed<15?"VisibleReliefFirst15":"VisibleRelief80"),"new policy14/15 trigger unchanged");
            else {var legacy=director.Choose(0,items,orders,buffer,pending,expectedRng);Check(choice.Kind==legacy.Kind&&CanonicalJson.Write(actualRng.Snapshot())==CanonicalJson.Write(expectedRng.Snapshot()),"new policy20 percent untouched rng");}
        }
        foreach(var spec in new[]{new[]{1,0,0,99,0},new[]{0,1,99,0,1},new[]{1,1,0,0,-1},new[]{1,1,0,7,1},new[]{1,1,7,0,0}})
        {
            var rng=new Pcg32(1);var before=CanonicalJson.Write(rng.Snapshot());
            Check(DailyDirector.ReliefGroup(spec[0]==1,spec[1]==1,spec[2],spec[3],rng,new List<object>())==spec[4],"group empty/zero rule");
            if(spec[0]==0||spec[1]==0||spec[2]+spec[3]==0)Check(before==CanonicalJson.Write(rng.Snapshot()),"group fallback no needless draw");
        }
        var unknownWeighted=director.Choose(0,items,orders,buffer,pending,new Pcg32(13),clickable:weightedVisible,unknown:new HashSet<int>{4},completedOrders:15,policyVersion:3);
        var unknownLegacy=director.Choose(0,items,orders,buffer,pending,new Pcg32(13));
        Check(CanonicalJson.Write(unknownWeighted.Diagnostic)==CanonicalJson.Write(unknownLegacy.Diagnostic),"weighted unknown ambiguity original rng and diagnostic");
        items[0].Location=items[6].Location=items[7].Location="ActiveAvailable";items[0].Slot=items[6].Slot=items[7].Slot=-1;Array.Clear(buffer,0,buffer.Length);
        var reservedItems=Enumerable.Range(1,12).Select(id=>new CoreItem{Id=id,PlateId=1,SourceIndex=id-1,Kind=id<=6?"A":id<=9?"C":"B",Location=id==1||id==7?"Buffer":"ActiveAvailable",Slot=id==1?0:id==7?1:-1}).ToList();
        var reservedOrders=new[]{new CoreOrder{Enabled=true},new CoreOrder{Enabled=true,Kind="B"},new CoreOrder{Enabled=false,Kind="A"},new CoreOrder()};
        for(ulong seed=1;seed<=100;seed++)
        {
            var choice=director.Choose(0,reservedItems,reservedOrders,new int?[]{1,7,null,null,null},pending,new Pcg32(seed),clickable:new HashSet<int>{4,5,6,8,9},completedOrders:0,policyVersion:3);
            var detail=CanonicalJson.Map(choice.Diagnostic);var group=CanonicalJson.Map(CanonicalJson.Map(detail["clickableRelief"])["groupSelection"]);
            Check((string)group["selectedGroup"]==(choice.Kind=="A"?"Plain":"Buffered"),"reserved buffer does not qualify buffered group");
        }
        foreach(int completed in new[]{0,14})for(ulong seed=1;seed<=100;seed++)
        {
            var choice=director.Choose(0,items,orders,buffer,pending,new Pcg32(seed),clickable:new HashSet<int>(visible.Concat(new[]{4})),completedOrders:completed);
            Check(choice.Fallback=="VisibleReliefFirst15"&&(choice.Kind=="A"||choice.Kind=="C"),"first15 one actionable is not completable");
        }
        foreach(int filled in new[]{0,1,2})
        {
            orders[1].Items.Clear();orders[1].Items.AddRange(Enumerable.Range(90,filled));
            var available=new HashSet<int>(visible.Concat(new[]{4,5,6}.Take(3-filled)));
            var actual=director.Choose(0,items,orders,buffer,pending,new Pcg32(1),clickable:available,completedOrders:14);
            var legacy=director.Choose(0,items,orders,buffer,pending,new Pcg32(1));
            Check(CanonicalJson.Write(actual.Diagnostic)==CanonicalJson.Write(legacy.Diagnostic),"remaining requirement already completable original path");
        }
        orders[1].Items.Clear();
        var earlyUnknown=director.Choose(0,items,orders,buffer,pending,new Pcg32(1),clickable:new HashSet<int>(visible.Concat(new[]{4})),unknown:new HashSet<int>{5,6},completedOrders:0);
        Check(CanonicalJson.Write(earlyUnknown.Diagnostic)==CanonicalJson.Write(director.Choose(0,items,orders,buffer,pending,new Pcg32(1)).Diagnostic),"early unknown cannot prove other pots blocked");
        Action<ISet<int>,string> unchanged=(ids,label)=>{var x=new Pcg32(91);var y=new Pcg32(91);var actual=director.Choose(0,items,orders,buffer,pending,x,clickable:ids);var legacy=director.Choose(0,items,orders,buffer,pending,y);Check(CanonicalJson.Write(actual.Diagnostic)==CanonicalJson.Write(legacy.Diagnostic)&&CanonicalJson.Write(x.Snapshot())==CanonicalJson.Write(y.Snapshot()),label);};
        unchanged(null,"missing observation unchanged");unchanged(new HashSet<int>(),"hidden generated items excluded and empty pool unchanged");
        foreach(var unknown in new[]{new HashSet<int>{4},new HashSet<int>{10,11,12}})
        {
            var x=new Pcg32(91);var y=new Pcg32(91);
            var actual=director.Choose(0,items,orders,buffer,pending,x,clickable:visible,unknown:unknown);var legacy=director.Choose(0,items,orders,buffer,pending,y);
            Check(CanonicalJson.Write(actual.Diagnostic)==CanonicalJson.Write(legacy.Diagnostic)&&CanonicalJson.Write(x.Snapshot())==CanonicalJson.Write(y.Snapshot()),"unknown actionable or new pool member exact fallback");
        }
        for(ulong seed=1;seed<30;seed++)
        {
            var x=new Pcg32(seed);var y=new Pcg32(seed);
            var actual=director.Choose(0,items,orders,buffer,pending,x,clickable:visible,unknown:new HashSet<int>{10,11});var known=director.Choose(0,items,orders,buffer,pending,y,clickable:visible);
            Check(CanonicalJson.Write(actual.Diagnostic)==CanonicalJson.Write(known.Diagnostic),"irrelevant unknown cannot disable exact pool");
        }
        unchanged(new HashSet<int>(visible.Concat(new[]{4})),"one receivable item prevents relief");orders[1].Items.Add(99);orders[1].Items.Add(100);
        unchanged(new HashSet<int>(visible.Concat(new[]{4})),"partially filled receivable pot prevents relief");orders[1].Items.Clear();
        items[3].Location="Buffer";items[3].Slot=0;buffer[0]=4;unchanged(visible,"buffer progress prevents relief");items[3].Location="ActiveAvailable";buffer[0]=null;
        var all=factory.Content.Plates.SelectMany(p=>p.Kinds).Select((kind,i)=>new{kind,id=i+1}).ToArray();
        Func<string,int[]> ids=k=>all.Where(x=>x.kind==k).Select(x=>x.id).ToArray();
        var fixture=new DailyFixture(50,new int?[5],new[]{new FixtureOrder("A",ids("A").Take(2)),new FixtureOrder("B",new int[0])},new int[0]);
        var context=new ChallengeContext("2026-09-24",factory.Content.ContentVersion,factory.ConfigurationDigest,"test",0);
        Func<DailySession> create=()=>factory.CreateFixtureSession(context,fixture);
        var session=create();var observation=Observation(session.Snapshot.SessionId,session.Snapshot.TransactionId,ids("C").Take(3).Concat(new[]{ids("A")[2]}));
        var result=session.Tap(new TapCommand(ids("A")[2],1,1,true,observation));Check(result.Accepted&&session.Snapshot.Status==GameStatus.Running,"valid observed command accepted and conserves inventory");
        var unknownSession=create();
        Check(unknownSession.MayRefillOrdersOnTap(ids("A")[2])&&!unknownSession.MayRefillOrdersOnTap(ids("B")[0]),"lazy capture only completion input");
        unknownSession.Tap(new TapCommand(ids("A")[2],1,1,true,Observation(unknownSession.Snapshot.SessionId,unknownSession.Snapshot.TransactionId,ids("C").Take(3),ids("D").Take(2))));
        Check(unknownSession.ExportReplay().CanonicalJson.Contains("unknownItemIds")&&DailyReplay.Run(factory,unknownSession.ExportReplay()).Success,"unknown observation recorded and replays");
        Check(session.ExportReplay().CanonicalJson.Contains("clickability"),"observation recorded");Check(DailyReplay.Run(factory,session.ExportReplay()).Success,"observed tap replay");
        var historic=create();historic.Tap(new TapCommand(ids("A")[2],1,1,true,Observation(historic.Snapshot.SessionId,historic.Snapshot.TransactionId,ids("C").Take(3),policyVersion:1)));
        Check(!historic.ExportReplay().CanonicalJson.Contains("policyVersion")&&!historic.CoreEventsJson.Contains("First15Guaranteed"),"historical missing-policy observation remains80/20");
        Check(DailyReplay.Run(factory,historic.ExportReplay()).Success,"historical observed replay fixture exact");
        Check(session.ExportReplay().CanonicalJson.Contains("policyVersion"),"new replay carries explicit first15 policy");
        foreach(int policy in new[]{1,2,3})
        {
            var generation=create();generation.Tap(new TapCommand(ids("A")[2],1,1,true,Observation(generation.Snapshot.SessionId,generation.Snapshot.TransactionId,ids("C").Take(3),policyVersion:policy)));
            Check(DailyReplay.Run(factory,generation.ExportReplay()).Success,"policy generation replay "+policy);
            Check(generation.CoreEventsJson.Contains("groupSelection")== (policy==3),"only strategy3 draws weighted group");
        }
        foreach(var stale in new[]{"revision","session"})
        {
            var x=create();var y=create();var bad=Observation(stale=="session"?"other":x.Snapshot.SessionId,stale=="revision"?"0":x.Snapshot.TransactionId,ids("C").Take(3));
            x.Tap(new TapCommand(ids("A")[2],1,1,true,bad));y.Tap(new TapCommand(ids("A")[2],1,1,true));
            Check(x.StateHash==y.StateHash&&x.CoreEventsJson==y.CoreEventsJson,"stale observation fallback "+stale);Check(DailyReplay.Run(factory,x.ExportReplay()).Success,"legacy replay fallback");
        }
        var unlock=create();unlock.UnlockFourth(1,Observation(unlock.Snapshot.SessionId,unlock.Snapshot.TransactionId,ids("C").Take(3)));Check(DailyReplay.Run(factory,unlock.ExportReplay()).Success,"unlock observation replay");
        int chained=0;
        for(int trial=0;trial<30;trial++)
        {
            var chainFixture=new DailyFixture(50,ids("C").Take(3).Select(id=>(int?)id).Concat(new int?[]{null,null}),new[]{new FixtureOrder("A",ids("A").Take(2)),new FixtureOrder("B",new int[0])},new int[0]);
            var chainContext=new ChallengeContext("2026-10-"+(trial+1).ToString("00"),factory.Content.ContentVersion,factory.ConfigurationDigest,"test",0);
            var chain=factory.CreateFixtureSession(chainContext,chainFixture);
            chain.Tap(new TapCommand(ids("A")[2],1,1,true,Observation(chain.Snapshot.SessionId,chain.Snapshot.TransactionId,ids("D").Take(3).Concat(new[]{ids("A")[2]}))));
            var evaluations=CanonicalJson.Array(CanonicalJson.Parse(chain.CoreEventsJson)).Select(CanonicalJson.Map).Where(e=>(string)e["type"]=="DirectorEvaluated").Select(e=>CanonicalJson.Map(e["data"])).ToArray();
            if(evaluations.Length>1)
            {
                chained++;
                foreach(var e in evaluations.Skip(1))if(e.TryGetValue("clickableRelief",out var relief))
                    Check(!CanonicalJson.Array(CanonicalJson.Map(relief)["candidateKinds"]).Contains("C")&&!CanonicalJson.Array(CanonicalJson.Map(relief)["candidateKinds"]).Contains("A"),"chain filters consumed buffer and tapped food");
            }
            Check(DailyReplay.Run(factory,chain.ExportReplay()).Success,"chain replay identity");
        }
        Check(chained>0,"buffer triple actually creates synchronous chain");
        bool crossed=false;
        for(int trial=0;trial<30&&!crossed;trial++)
        {
            var done=all.Where(x=>x.kind!="A"&&x.kind!="B"&&x.kind!="C"&&x.kind!="D").GroupBy(x=>x.kind).SelectMany(g=>g.Take(g.Count()/3*3)).Take(39).Select(x=>x.id);
            var crossFixture=new DailyFixture(50,ids("C").Take(3).Select(id=>(int?)id).Concat(new int?[]{null,null}),new[]{new FixtureOrder("A",ids("A").Take(2)),new FixtureOrder("B",new int[0])},done);
            var cross=factory.CreateFixtureSession(new ChallengeContext("2026-10-"+(trial+1).ToString("00"),factory.Content.ContentVersion,factory.ConfigurationDigest,"test",0),crossFixture);
            cross.Tap(new TapCommand(ids("A")[2],1,1,true,Observation(cross.Snapshot.SessionId,cross.Snapshot.TransactionId,ids("D").Take(3))));
            var evaluations=CanonicalJson.Array(CanonicalJson.Parse(cross.CoreEventsJson)).Select(CanonicalJson.Map).Where(e=>(string)e["type"]=="DirectorEvaluated").Select(e=>CanonicalJson.Map(e["data"])).ToArray();
            if(evaluations.Length>1)
            {
                crossed=true;Check((string)CanonicalJson.Map(evaluations[0]["clickableRelief"])["branch"]=="First15Guaranteed","ordinal14 protected");
                Check((string)CanonicalJson.Map(evaluations[1]["clickableRelief"])["branch"]!="First15Guaranteed","same transaction ordinal15 uses80/20");
                Check(DailyReplay.Run(factory,cross.ExportReplay()).Success,"threshold chain replay");
            }
        }
        Check(crossed,"14 to15 synchronous threshold exercised");
        var currentFactory=factory.ForReplayContent(DailyContent.ProductionDigest);
        var archived=DailySessionFactory.FromProductionJson(File.ReadAllText(Path.Combine(Path.GetDirectoryName(args[0]),"hotpot_daily_task001_v5_fixed_c_1.legacy_d3.json")));
        Check(currentFactory.ForReplayContent(DailyContent.LegacyProductionDigest).ConfigurationDigest==archived.ConfigurationDigest,"archived D3 catalog/config reconstructed exactly");
        var sourceRows=CanonicalJson.Array(CanonicalJson.Parse(File.ReadAllText(Path.Combine(Path.GetDirectoryName(args[0]),"LevelDifficultyConfig.source.json")))).Select(CanonicalJson.Map).Where(r=>CanonicalJson.Int(r["Difficulty"])==1).ToArray();
        for(int rowIndex=0;rowIndex<20;rowIndex++)Check(currentFactory.Content.Rows[rowIndex].Weights.SequenceEqual(Enumerable.Range(1,5).Select(i=>(int)(Convert.ToDecimal(sourceRows[rowIndex]["Order"+i])*100m))),"Difficulty1 exact source row "+rowIndex);
        foreach(int policy in new[]{0,1,2,3})
        {
            var oldContext=new ChallengeContext("2026-09-24",archived.Content.ContentVersion,archived.ConfigurationDigest,"test",0);
            var oldSession=archived.CreateFixtureSession(oldContext,fixture);
            var oldObservation=policy==0?null:Observation(oldSession.Snapshot.SessionId,oldSession.Snapshot.TransactionId,ids("C").Take(3),policyVersion:policy);
            oldSession.Tap(new TapCommand(ids("A")[2],1,1,true,oldObservation));
            var replayed=DailyReplay.Run(currentFactory,oldSession.ExportReplay());
            Check(replayed.Success&&replayed.Session.StateHash==oldSession.StateHash&&replayed.Session.CoreEventsJson==oldSession.CoreEventsJson,"old D3 from new factory exact replay policy "+policy);
        }
        var currentDirector=new DailyDirector(currentFactory.Content);int d1A=0,d1C=0;
        items[0].Location="Buffer";items[0].Slot=0;buffer[0]=1;
        for(ulong seed=1;seed<=5000;seed++)
        {
            var choice=currentDirector.Choose(0,items,orders,buffer,pending,new Pcg32(seed),clickable:visible,completedOrders:14,policyVersion:4);
            Check(choice.Fallback=="VisibleReliefFirst15"&&!CanonicalJson.Write(choice.Diagnostic).Contains("groupSelection"),"new first15 no group weighting");if(choice.Kind=="A")d1A++;else if(choice.Kind=="C")d1C++;else Check(false,"D1 pool exclusion");
        }
        Check(Math.Abs(d1A-d1C)<250,"D1 first15 equal kinds despite buffer");
        Check(currentDirector.Choose(0,items,orders,buffer,pending,new Pcg32(1),clickable:visible,completedOrders:0,policyVersion:4).Fallback=="VisibleReliefFirst15","new first15 boundary0");
        foreach(var observedIds in new[]{new HashSet<int>(),visible})
        {
            var x=new Pcg32(13);var y=new Pcg32(13);var actual=currentDirector.Choose(0,items,orders,buffer,pending,x,clickable:observedIds,unknown:observedIds.Count==0?null:new HashSet<int>{4,5,6},completedOrders:0,policyVersion:4);var basic=currentDirector.Choose(0,items,orders,buffer,pending,y);
            Check(CanonicalJson.Write(actual.Diagnostic)==CanonicalJson.Write(basic.Diagnostic)&&CanonicalJson.Write(x.Snapshot())==CanonicalJson.Write(y.Snapshot()),"new empty/unknown exact base");
        }
        foreach(int completed in new[]{15,16,61})for(ulong seed=1;seed<=100;seed++)
        {
            var x=new Pcg32(seed);var y=new Pcg32(seed);var observed=currentDirector.Choose(0,items,orders,buffer,pending,x,clickable:visible,completedOrders:completed,policyVersion:4);var basic=currentDirector.Choose(0,items,orders,buffer,pending,y);
            Check(CanonicalJson.Write(observed.Diagnostic)==CanonicalJson.Write(basic.Diagnostic)&&CanonicalJson.Write(x.Snapshot())==CanonicalJson.Write(y.Snapshot()),"D1>=15 exact base without relief RNG");
        }
        items[0].Location="ActiveAvailable";items[0].Slot=-1;buffer[0]=null;
        bool newCross=false;
        for(int trial=0;trial<30&&!newCross;trial++)
        {
            var done=all.Where(x=>x.kind!="A"&&x.kind!="B"&&x.kind!="C"&&x.kind!="D").GroupBy(x=>x.kind).SelectMany(g=>g.Take(g.Count()/3*3)).Take(39).Select(x=>x.id);
            var crossFixture=new DailyFixture(50,ids("C").Take(3).Select(id=>(int?)id).Concat(new int?[]{null,null}),new[]{new FixtureOrder("A",ids("A").Take(2)),new FixtureOrder("B",new int[0])},done);
            var cross=currentFactory.CreateFixtureSession(new ChallengeContext("2026-10-"+(trial+1).ToString("00"),currentFactory.Content.ContentVersion,currentFactory.ConfigurationDigest,"test",0),crossFixture);
            cross.Tap(new TapCommand(ids("A")[2],1,1,true,new ClickableObservation(cross.Snapshot.SessionId,cross.Snapshot.TransactionId,ids("D").Take(3),policyVersion:4)));
            var evaluations=CanonicalJson.Array(CanonicalJson.Parse(cross.CoreEventsJson)).Select(CanonicalJson.Map).Where(e=>(string)e["type"]=="DirectorEvaluated").Select(e=>CanonicalJson.Map(e["data"])).ToArray();
            if(evaluations.Length>1){newCross=true;Check(evaluations[0].ContainsKey("clickableRelief")&&!evaluations[1].ContainsKey("clickableRelief"),"new same transaction14->15 disables protection");Check(DailyReplay.Run(archived,cross.ExportReplay()).Success,"new D1 replay resolves from legacy factory");}
        }
        Check(newCross,"new threshold chain exercised");
        bool first30Cross=false;
        for(int trial=0;trial<30&&!first30Cross;trial++)
        {
            var done=all.Where(x=>x.kind!="A"&&x.kind!="B"&&x.kind!="C"&&x.kind!="D").GroupBy(x=>x.kind).SelectMany(g=>g.Take(g.Count()/3*3)).Take(84).Select(x=>x.id);
            var crossFixture=new DailyFixture(50,ids("C").Take(3).Select(id=>(int?)id).Concat(new int?[]{null,null}),new[]{new FixtureOrder("A",ids("A").Take(2)),new FixtureOrder("B",new int[0])},done);
            var cross=currentFactory.CreateFixtureSession(new ChallengeContext("2026-10-"+(trial+1).ToString("00"),currentFactory.Content.ContentVersion,currentFactory.ConfigurationDigest,"test",0),crossFixture);
            cross.Tap(new TapCommand(ids("A")[2],1,1,true,new ClickableObservation(cross.Snapshot.SessionId,cross.Snapshot.TransactionId,ids("D").Take(3))));
            var evaluations=CanonicalJson.Array(CanonicalJson.Parse(cross.CoreEventsJson)).Select(CanonicalJson.Map).Where(e=>(string)e["type"]=="DirectorEvaluated").Select(e=>CanonicalJson.Map(e["data"])).ToArray();
            if(evaluations.Length>1){first30Cross=true;Check((string)CanonicalJson.Map(evaluations[0]["clickableRelief"])["branch"]=="First30Guaranteed"&&!evaluations[1].ContainsKey("clickableRelief"),"same transaction29->30 disables protection");Check(DailyReplay.Run(archived,cross.ExportReplay()).Success,"policy5 replay resolves from legacy factory");}
        }
        Check(first30Cross,"first30 synchronous threshold chain exercised");
        Check(new ClickableObservation("s","r",Array.Empty<int>()).PolicyVersion==5,"production default policy5");
        int first30A=0,first30C=0;
        for(int completed=0;completed<=31;completed++)for(ulong seed=1;seed<=100;seed++)
        {
            var x=new Pcg32(seed+(ulong)completed*100);var y=new Pcg32(seed+(ulong)completed*100);
            var actual=currentDirector.Choose(0,items,orders,buffer,pending,x,clickable:visible,completedOrders:completed,policyVersion:5);
            if(completed<30){Check(actual.Fallback=="VisibleReliefFirst30","0 through29 protected");if(actual.Kind=="A")first30A++;else if(actual.Kind=="C")first30C++;else Check(false,"first30 pool");}
            else {var basic=currentDirector.Choose(0,items,orders,buffer,pending,y);Check(CanonicalJson.Write(actual.Diagnostic)==CanonicalJson.Write(basic.Diagnostic)&&CanonicalJson.Write(x.Snapshot())==CanonicalJson.Write(y.Snapshot()),"30 onwards exact base rng");}
        }
        Check(Math.Abs(first30A-first30C)<400,"first30 uniform pool");
        foreach(var observedIds in new[]{new HashSet<int>(),visible})
        {
            var x=new Pcg32(13);var y=new Pcg32(13);
            var actual=currentDirector.Choose(0,items,orders,buffer,pending,x,clickable:observedIds,unknown:observedIds.Count==0?null:new HashSet<int>{4,5,6},completedOrders:29,policyVersion:5);
            var basic=currentDirector.Choose(0,items,orders,buffer,pending,y);
            Check(CanonicalJson.Write(actual.Diagnostic)==CanonicalJson.Write(basic.Diagnostic)&&CanonicalJson.Write(x.Snapshot())==CanonicalJson.Write(y.Snapshot()),"first30 empty/unknown exact fallback");
        }
        var newContext=new ChallengeContext("2026-09-25",currentFactory.Content.ContentVersion,currentFactory.ConfigurationDigest,"test",0);
        PlayStage(currentFactory,newContext,ChallengeStage.Warmup,5);
        var formal=PlayStage(currentFactory,newContext,ChallengeStage.Formal,5);
        var retry=PlayStage(currentFactory,newContext,ChallengeStage.Formal,5);
        Check(formal.StateHash==retry.StateHash&&formal.CoreEventsJson==retry.CoreEventsJson,"same day policy5 deterministic");
        PlayStage(currentFactory,newContext,ChallengeStage.Formal,4);
        Console.WriteLine("DIFFICULTY1 uniformA="+d1A+" uniformC="+d1C+" digest="+currentFactory.Content.Digest);
        Console.WriteLine(CanonicalJson.Write(CanonicalJson.Object("status","PASS","checks",checks,"protected",protectedCount,"original",originalCount,"poolA",a,"poolC",c,"weightedAOneBuffer",weightedA,"weightedCTwoBuffer",weightedC,"weightedDPlain",weightedD,"groupWeights",new[]{row[0],row[1]})));
    }
}
