using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HotpotSort.Core;
using HotpotSort.Contracts;
using HotpotSort.Determinism;
using HotpotSort.Replay;
static class Task051ShuffleQa
{
    static int checks;
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    static Dictionary<string,object> State(DailySession s)=>CanonicalJson.Map(CanonicalJson.Parse(s.Snapshot.CanonicalStateJson));
    static Dictionary<string,object>[] Items(Dictionary<string,object> state)=>CanonicalJson.Array(state["items"]).Select(CanonicalJson.Map).ToArray();
    static string Inventory(Dictionary<string,object> state)=>CanonicalJson.Write(Items(state).Select(x=>CanonicalJson.Object("id",x["itemId"],"plate",x["plateId"],"source",x["sourceIndex"],"kind",x["kind"])));
    static int Main(string[] args)
    {
        try
        {
            var f=DailySessionFactory.FromProductionJson(File.ReadAllText(args[0]),File.ReadAllText(args[1]));
            for(int day=0;day<7;day++)
            {
                string date=new DateTime(2026,9,28).AddDays(day).ToString("yyyyMMdd");var selected=f.WithLayoutForChallenge(date);
                var s=selected.CreateStage(new ChallengeContext(date,selected.Content.ContentVersion,selected.ConfigurationDigest,"qa",0,"12345"),ChallengeStage.Formal);
                ulong seq=0;
                for(int i=0;i<12;i++)Check(s.Supply(new SupplyObservation(++seq,0,true,true)).Accepted,"initial supply");
                var initial=State(s);var kinds=CanonicalJson.Array(initial["orders"]).Select(CanonicalJson.Map).Select(o=>o["kind"]).ToArray();
                int bufferId=CanonicalJson.Int(Items(initial).First(x=>(string)x["location"]=="ActiveAvailable"&&!kinds.Contains(x["kind"]))["itemId"]);
                Check(s.Tap(new TapCommand(bufferId,1,0,true)).Accepted,"buffer fixture tap");
                var before=State(s);string inv=Inventory(before),buf=CanonicalJson.Write(before["buffer"]),orders=CanonicalJson.Write(before["orders"]),rng=CanonicalJson.Write(before["directorRng"]);
                Check(s.Pause(0).Accepted&&s.CanRedeal,"paused reward validation");Check(!s.Redeal(0).Accepted,"paused cannot commit");Check(s.Resume(0).Accepted,"resume");
                for(int i=0;i<37;i++)s.NextPresentation(100);
                Check(s.Redeal(0).Accepted,"redeal accepted");var after=State(s);
                Check(s.ExportReplay().CanonicalJson.Contains("\"firstBatchSize\":0"),"dynamic fill mode recorded for replay");
                Check(s.Snapshot.Status==GameStatus.Running,"invariants");Check(Inventory(after)==inv,"identities and plate contents preserved");
                Check(CanonicalJson.Write(after["buffer"])==buf&&CanonicalJson.Write(after["orders"])==orders,"buffer and orders unchanged");
                Check(CanonicalJson.Write(after["directorRng"])==rng,"director rng isolated");Check(CanonicalJson.Array(after["activePlateIds"]).Count==0,"active cleared");
                var queue=CanonicalJson.Array(after["pendingPlateIds"]).Select(CanonicalJson.Int).ToArray();
                Check(queue[0]==s.RedealGuaranteedPlateId,"guaranteed plate first to prevent starvation");
                Check(kinds.Contains(Items(after).Single(x=>CanonicalJson.Int(x["itemId"])==s.RedealGuaranteedItemId)["kind"]),"guaranteed item matches order");
                for(int i=0;i<24;i++)Check(s.Supply(new SupplyObservation(++seq,0,true,true)).Accepted,"redeal supply");
                Check(CanonicalJson.Array(State(s)["activePlateIds"]).Count==24,"core accepts 24 explicit supplies without imposing presentation cap");
                Check(s.Supply(new SupplyObservation(++seq,0,true,true)).Accepted&&CanonicalJson.Array(State(s)["activePlateIds"]).Count==25,"normal supply can exceed twenty-four");
                Check(!s.ReturnRedealPlate(s.RedealGuaranteedPlateId,0).Accepted,"guaranteed plate cannot be returned");
                Check(s.ReturnRedealPlate(queue[1],0).Accepted,"congestion returns non-guaranteed plate");
                Check(CanonicalJson.Array(State(s)["activePlateIds"]).Count==24,"congestion reduces active count");
                Check(CanonicalJson.Int(CanonicalJson.Array(State(s)["pendingPlateIds"]).Last())==queue[1],"returned plate appended");
                Check(Inventory(State(s))==inv&&CanonicalJson.Write(State(s)["buffer"])==buf,"return preserves inventory");
                string preDrawHash=s.StateHash;for(int i=0;i<19;i++)s.NextPresentation(100);
                Check(s.StateHash==preDrawHash,"render draws do not change core hash");
                Check(s.Redeal(0,16).Accepted,"legacy sixteen-plate record remains replayable");
                Check(s.Redeal(0,12).Accepted,"legacy twelve-plate record remains replayable");
                var replay=DailyReplay.Run(f,s.ExportReplay());Check(replay.Success&&replay.Session.StateHash==s.StateHash,"replay "+replay.Error);
                // No valid order candidate must reject without changing state or RNG.
                var coreOrders=(CoreOrder[])typeof(DailySession).GetField("orders",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(s);
                foreach(var order in coreOrders)order.Enabled=false;
                string hash=s.StateHash;Check(!s.CanRedeal&&!s.Redeal(0).Accepted&&s.StateHash==hash,"no candidate no mutation");
            }
            Console.WriteLine("TASK051_SHUFFLE_QA_PASS checks="+checks);return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
