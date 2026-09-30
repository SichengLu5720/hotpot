using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Determinism;
using HotpotSort.Replay;

static class Task050OrderSupportQa
{
    sealed class Account : IAccountRewardProfileStore
    {
        readonly HashSet<string> results=new HashSet<string>(StringComparer.Ordinal);
        public int ConsecutiveFormalFailures { get; set; }
        public int SwapOrderRewardInventory => 0;
        public AccountMutationResult RecordFormalResult(string resultId,string sessionId,string challengeDay,FormalChallengeResultKind result,DateTimeOffset completedUtc)
        {
            if(!results.Add(resultId))return AccountMutationResult.Duplicate;
            ConsecutiveFormalFailures=result==FormalChallengeResultKind.Won?0:ConsecutiveFormalFailures+1;
            return AccountMutationResult.Applied;
        }
        public AccountMutationResult GrantSwapOrderRewards(string grantId,int amount,DateTimeOffset effectiveUtc)=>throw new NotSupportedException();
        public AccountMutationResult ConsumeSwapOrderReward(string spendId,DateTimeOffset effectiveUtc)=>throw new NotSupportedException();
    }

    static int checks;
    static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
    static Dictionary<string,object> Support(DailyDirector.Decision value)=>CanonicalJson.Map(CanonicalJson.Map(value.Diagnostic)["recentSupport"]);
    static GameSnapshot Snapshot(ChallengeContext context,string id,GameStatus status,string failure)
    {
        var state=CanonicalJson.Object("statistics",CanonicalJson.Object("failureCode",failure));
        return new GameSnapshot(id,context,status,"1","1",DailySession.StateSchema,CanonicalJson.Write(state));
    }

    static void Probability(DailySessionFactory factory)
    {
        var director=new DailyDirector(factory.Content);
        var items=Enumerable.Range(1,15).Select(id=>new CoreItem{Id=id,PlateId=1,SourceIndex=id-1,Kind=((char)('A'+(id-1)/3)).ToString(),Location="ActiveAvailable"}).ToList();
        var orders=new[]{new CoreOrder{Enabled=true},new CoreOrder{Enabled=true,Kind="B"},new CoreOrder(),new CoreOrder()};
        var buffer=new int?[5];var pending=new List<int>();var dangerVisible=new HashSet<int>{1,2,3};
        int[] failures={0,1,2,3,20},expected={20,30,40,50,50};
        for(int mode=0;mode<failures.Length;mode++)
        {
            int taken=0;
            for(ulong seed=1;seed<=10000;seed++)
            {
                var decision=director.Choose(0,items,orders,buffer,pending,new Pcg32(seed),clickable:dangerVisible,policyVersion:6,consecutiveFormalFailures:failures[mode]);
                var support=Support(decision);
                Check(CanonicalJson.Int(support["takeoverPercent"])==expected[mode],"takeover percent boundary");
                if((string)support["branch"]=="Complete")taken++;
            }
            double percent=taken/100.0;
            Check(Math.Abs(percent-expected[mode])<1.5,"distribution "+expected[mode]+" observed "+percent);
        }
        for(ulong seed=1;seed<=200;seed++)
        {
            var visible=new HashSet<int>{1,2,3,4,5,6};
            var baseline=new Pcg32(seed);var adaptive=new Pcg32(seed);
            var basic=director.Choose(0,items,orders,buffer,pending,baseline,clickable:visible,policyVersion:6);
            var helped=director.Choose(0,items,orders,buffer,pending,adaptive,clickable:visible,policyVersion:6,consecutiveFormalFailures:3);
            Check(basic.Kind==helped.Kind&&CanonicalJson.Write(baseline.Snapshot())==CanonicalJson.Write(adaptive.Snapshot()),"below threshold exact Difficulty 1");
            Check(Support(helped)["roll"]==null&&(string)Support(helped)["branch"]=="BelowThresholdBase","below threshold no roll");
        }
        for(ulong seed=1;seed<=200;seed++)
        {
            var capped=director.Choose(0,items,orders,buffer,pending,new Pcg32(seed),clickable:dangerVisible,policyVersion:6,consecutiveFormalFailures:3);
            var excessive=director.Choose(0,items,orders,buffer,pending,new Pcg32(seed),clickable:dangerVisible,policyVersion:6,consecutiveFormalFailures:999);
            Check(CanonicalJson.Write(capped.Diagnostic)==CanonicalJson.Write(excessive.Diagnostic),"50 percent cap exact");
        }
        var queryRng=new Pcg32(9);string before=CanonicalJson.Write(queryRng.Snapshot());
        director.Choose(0,items,orders,buffer,pending,queryRng,false,dangerVisible,policyVersion:6,consecutiveFormalFailures:3);
        Check(before==CanonicalJson.Write(queryRng.Snapshot()),"query consumes no probability draw");
    }

    static void FrozenAndReplay(DailySessionFactory factory,ChallengeContext context)
    {
        var account=new Account{ConsecutiveFormalFailures=1};
        var adaptive=new AdaptiveOrderPityFactory(factory,account,()=>DateTimeOffset.UnixEpoch);
        using(var warm=(DailySession)adaptive.CreateStage(context,ChallengeStage.Warmup,0))
        {
            Check(warm.ConsecutiveFormalFailuresAtStart==1,"warmup freezes account count");
            account.ConsecutiveFormalFailures=3;
            using var formal=(DailySession)adaptive.CreateStage(context,ChallengeStage.Formal,0);
            Check(formal.ConsecutiveFormalFailuresAtStart==1,"formal reuses run-start count");
            var replay=DailyReplay.Run(factory,formal.ExportReplay());
            Check(replay.Success&&replay.Session.ConsecutiveFormalFailuresAtStart==1,"adaptive replay preserves frozen count "+replay.Error);
        }
        using(var direct=(DailySession)adaptive.CreateStage(context,ChallengeStage.Formal,0))
        {
            Check(direct.ConsecutiveFormalFailuresAtStart==3,"direct formal captures current count");
            Check(direct.Timeout(600000).Accepted,"formal timeout accepted");
            Check(account.ConsecutiveFormalFailures==4&&adaptive.LastRecordError==null,"factory records final timeout once");
        }
    }

    static void Results(ChallengeContext context)
    {
        var account=new Account{ConsecutiveFormalFailures=2};var now=DateTimeOffset.UnixEpoch;
        Check(!AdaptiveOrderPity.RecordFormalResult(account,ChallengeStage.Formal,Snapshot(context,"revival",GameStatus.Paused,"BufferOverflow"),now).HasValue&&account.ConsecutiveFormalFailures==2,"revival pending is not final failure");
        Check(!AdaptiveOrderPity.RecordFormalResult(account,ChallengeStage.Warmup,Snapshot(context,"warmup",GameStatus.Failed,"BufferOverflow"),now).HasValue&&account.ConsecutiveFormalFailures==2,"warmup failure ignored");
        var failed=Snapshot(context,"failed",GameStatus.Failed,"BufferOverflow");
        Check(AdaptiveOrderPity.RecordFormalResult(account,ChallengeStage.Formal,failed,now)==AccountMutationResult.Applied&&account.ConsecutiveFormalFailures==3,"final buffer failure increments");
        Check(AdaptiveOrderPity.RecordFormalResult(account,ChallengeStage.Formal,failed,now)==AccountMutationResult.Duplicate&&account.ConsecutiveFormalFailures==3,"final failure idempotent");
        Check(!AdaptiveOrderPity.RecordFormalResult(account,ChallengeStage.Formal,Snapshot(context,"exit",GameStatus.Aborted,null),now).HasValue&&account.ConsecutiveFormalFailures==3,"exit or abort ignored");
        Check(AdaptiveOrderPity.RecordFormalResult(account,ChallengeStage.Formal,Snapshot(context,"win",GameStatus.Won,null),now)==AccountMutationResult.Applied&&account.ConsecutiveFormalFailures==0,"formal win resets");
        Check(AdaptiveOrderPity.RecordFormalResult(account,ChallengeStage.Formal,Snapshot(context,"timeout",GameStatus.Failed,"Timeout"),now)==AccountMutationResult.Applied&&account.ConsecutiveFormalFailures==1,"final timeout increments");
    }


    static void NewSupport(DailySessionFactory factory)
    {
        var director=new DailyDirector(factory.Content);
        var items=Enumerable.Range(1,15).Select(id=>new CoreItem{Id=id,PlateId=1,SourceIndex=id-1,Kind=((char)('A'+(id-1)/3)).ToString(),Location="ActiveAvailable"}).ToList();
        var orders=new[]{new CoreOrder{Enabled=true},new CoreOrder{Enabled=true,Kind="B"},new CoreOrder(),new CoreOrder()};
        var visible=new HashSet<int>{1,2,3};
        foreach(int progress in new[]{0,9,10})
        foreach(int failures in new[]{0,1,2,3,4,999})
        {
            int taken=0,expected=progress<10?100:AdaptiveOrderPity.TakeoverPercent(failures);
            for(ulong seed=1;seed<=2000;seed++)
            {
                var d=director.Choose(0,items,orders,new int?[5],new List<int>(),new Pcg32(seed),clickable:visible,completedOrders:progress,policyVersion:6,consecutiveFormalFailures:failures,supportVersion:2,formalStage:true);
                var s=Support(d);Check(CanonicalJson.Int(s["takeoverPercent"])==expected,"new probability exact boundary");
                if((string)s["branch"]=="Complete")taken++;
                if(progress<10)Check(d.Kind=="A"&&(string)s["branch"]=="Complete","every early draw selects complete candidate");
            }
            Check(Math.Abs(taken/20.0-expected)<3,"new probability distribution "+progress+"/"+failures);
        }
        foreach(int normal in new[]{2,3,4})
        {
            orders[2]=new CoreOrder{Enabled=true,Kind="C"};orders[3]=new CoreOrder{Enabled=true,Kind="D"};
            var d=director.Choose(0,items,orders,new int?[5],new List<int>(),new Pcg32(1),clickable:visible,completedOrders:9,policyVersion:6,normalPotCount:normal,supportVersion:2,formalStage:true);
            Check((string)Support(d)["branch"]=="Complete","danger threshold includes reward pots "+normal);
        }
        orders[2]=new CoreOrder();orders[3]=new CoreOrder();
        foreach(var entry in new[]{new{Ids=new[]{1},Branch="MostSupported"},new{Ids=Array.Empty<int>(),Branch="AllZeroBase"},new{Ids=new[]{1,2,3,4,5,6},Branch="BelowThresholdBase"}})
        {
            var d=director.Choose(0,items,orders,new int?[5],new List<int>(),new Pcg32(2),clickable:new HashSet<int>(entry.Ids),completedOrders:0,policyVersion:6,supportVersion:2,formalStage:true);
            Check((string)Support(d)["branch"]==entry.Branch,"preserved fallback/threshold "+entry.Branch);
        }
        var query=new Pcg32(9);var before=CanonicalJson.Write(query.Snapshot());
        director.Choose(0,items,orders,new int?[5],new List<int>(),query,false,visible,completedOrders:0,policyVersion:6,supportVersion:2,formalStage:true);
        Check(before==CanonicalJson.Write(query.Snapshot()),"new query does not draw");
        var swap=director.Choose(0,items,orders,new int?[5],new List<int>(),new Pcg32(9),clickable:visible,completedOrders:0,policyVersion:6,swapFromKind:"C",supportVersion:2,formalStage:true);
        Check(swap.Kind=="A"&&(string)Support(swap)["branch"]=="SwapComplete","swap complete target unchanged");
    }
    static Dictionary<string,object> State(DailySession session)=>CanonicalJson.Map(CanonicalJson.Parse(session.Snapshot.CanonicalStateJson));
    static void SessionPath(DailySessionFactory factory,ChallengeContext context)
    {
        foreach(var stage in new[]{ChallengeStage.Warmup,ChallengeStage.Formal})
        foreach(int version in new[]{1,2})
        {
            using var session=version==2?factory.CreateStage(context,stage,3,4):factory.CreateReplaySession(new ChallengeContext(context.ChallengeId,context.ContentVersion,context.ConfigurationDigest,"test",0,"123"),null,stage,3,3);
            Check(session.ConsecutiveFormalFailuresAtStart==(version==2?4:3),"frozen count cap version "+version);
            ulong tick=1,seq=1;
            for(int i=0;i<60;i++)session.Supply(new SupplyObservation((ulong)i+1,tick++,true,true));
            for(int i=0;i<42&&session.Snapshot.Status==GameStatus.Running;i++)
            {
                var state=State(session);var all=CanonicalJson.Array(state["items"]).Select(CanonicalJson.Map).ToArray();
                var kinds=CanonicalJson.Array(state["orders"]).Select(CanonicalJson.Map).Where(o=>o["kind"]!=null).Select(o=>(string)o["kind"]).ToArray();
                var active=all.Where(x=>(string)x["location"]=="ActiveAvailable").ToArray();
                var item=active.FirstOrDefault(x=>kinds.Contains((string)x["kind"]));if(item==null)break;
                var observation=new ClickableObservation(session.Snapshot.SessionId,session.Snapshot.TransactionId,active.Select(x=>CanonicalJson.Int(x["itemId"])));
                Check(session.Tap(new TapCommand(CanonicalJson.Int(item["itemId"]),seq++,tick++,true,observation)).Accepted,"session tap accepted");
            }
            var package=session.ExportReplay();var root=CanonicalJson.Map(CanonicalJson.Parse(package.CanonicalJson));
            Check(root.ContainsKey("orderSupportVersion")== (version==2),"version header presence");
            var replay=DailyReplay.Run(factory,package);Check(replay.Success&&replay.Session.StateHash==session.StateHash,"new/old stage replay "+stage+"/"+version+" "+replay.Error);
            Check(CanonicalJson.Int(CanonicalJson.Map(State(session)["statistics"])["completedOrderCount"])>0,"stage completed orders");
            // Refill diagnostics emitted by the real session must use the correct stage-specific policy.
            string json=session.CoreEventsJson;
            Check(json.Contains("FirstTen100FailureStreak20To30_v2")== (version==2&&stage==ChallengeStage.Formal),"formal only uses new probability policy");
            if(version==2&&stage==ChallengeStage.Formal)
            {
                var chances=CanonicalJson.Array(CanonicalJson.Parse(session.CoreEventsJson)).Select(CanonicalJson.Map).Where(e=>(string)e["type"]=="DirectorEvaluated").Select(e=>CanonicalJson.Map(e["data"])).Where(d=>d.ContainsKey("recentSupport")).Select(d=>CanonicalJson.Int(CanonicalJson.Map(d["recentSupport"])["takeoverPercent"])).ToArray();
                Check(chances.Contains(100)&&chances.Contains(30)&&chances.All(c=>c==100||c==30),"real session crosses first-ten boundary into capped probability");
            }
            if(stage==ChallengeStage.Warmup)Check(session.CumulativeCompletedOrders==0,"warmup excluded from progress");
        }
    }

    static void Main(string[] args)
    {
        var factory=DailySessionFactory.FromProductionJson(File.ReadAllText(args[0]));
        var context=new ChallengeContext("2026-09-28",factory.Content.ContentVersion,factory.ConfigurationDigest,"test",0);
        Check(AdaptiveOrderPity.TakeoverPercent(0)==20&&AdaptiveOrderPity.TakeoverPercent(1)==23&&AdaptiveOrderPity.TakeoverPercent(2)==26&&AdaptiveOrderPity.TakeoverPercent(3)==29&&AdaptiveOrderPity.TakeoverPercent(4)==30&&AdaptiveOrderPity.TakeoverPercent(int.MaxValue)==30,"20 23 26 29 30 boundaries");
        Probability(factory);NewSupport(factory);SessionPath(factory,context);FrozenAndReplay(factory,context);Results(context);
        Console.WriteLine("PASS TASK050 checks="+checks+" adaptive order pity, frozen run state, replay and formal results");
    }
}
