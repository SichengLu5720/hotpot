using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using HotpotSort.Contracts;
using HotpotSort.Collection;
using HotpotSort.Core;
using HotpotSort.Determinism;

static class Task030CollectionQa
{
    sealed class Memory:ICollectionPersistence
    {public CollectionDocument value;public bool fail;public CollectionDocument Load()=>value==null?null:CollectionStore.Copy(value);public void Save(CollectionDocument d){if(fail)throw new Exception("disk full");value=CollectionStore.Copy(d);}}
    sealed class Sequence:ICollectionRandom
    {public int n;public int Next(int max)=>n++%max;}
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);}
    static int Main()
    {
        var memory=new Memory();var rng=new Sequence();var store=new CollectionStore("test","user",memory,rng);
        Check(store.ReadCollection().unlocked.Count==16&&!store.ReadCollection().entryUnlocked,"defaults");
        Check(!store.TrySaveSelection(new[]{"food_00"}),"reject short");Check(store.BeginSelectionDraft().Count==16,"draft recovery");
        Check(!store.TrySaveSelection(Enumerable.Range(16,16).Select(CollectionCatalog.Id)),"reject locked");
        Check(store.CreateSessionSelection().Length==16&&rng.n==0,"exact selection consumes no random");
        var boundary=new DateTimeOffset(2026,9,25,22,0,0,TimeSpan.Zero);
        Check(CollectionStore.RewardDay(boundary.AddTicks(-1))=="20260925"&&CollectionStore.RewardDay(boundary)=="20260926","06 boundary");
        var reward=store.RecordWin("run1",boundary,true);Check(reward.ingredientId=="food_00"&&!reward.firstUnlock,"duplicate reward");
        Check(store.RecordWin("run2",boundary,true)==null&&store.ReadCollection().duplicates[0]==1,"daily idempotence");
        Check(store.RecordWin("run1",boundary.AddDays(1),true)==null,"old session after day boundary");
        Check(new CollectionStore("test","user",memory).ReadCollection().entryUnlocked,"persistent entry");
        for(int i=16;i<32;i++){rng.n=i;Check(store.RecordWin("day"+i,boundary.AddDays(i),true).firstUnlock,"first unlock");}
        rng.n=0;reward=store.RecordWin("complete",boundary.AddDays(50),true);Check(reward.tools.Count==2&&reward.ingredientId==null,"completion rewards");
        Check(store.TryConsumeTool(RewardKind.Hint,"use1")&&!store.TryConsumeTool(RewardKind.Hint,"use1"),"tool idempotence");
        Check(store.TrySaveSelection(Enumerable.Range(0,32).Select(CollectionCatalog.Id)),"full pool");
        var first=store.CreateSessionSelection();var second=store.CreateSessionSelection();Check(first.Length==16&&first.Distinct().Count()==16&&!first.SequenceEqual(second),"new session sample");
        Check(store.TryExchangeDuplicate("trade1","food_00","food_16")&&!store.TryExchangeDuplicate("trade1","food_00","food_16"),"atomic trade");
        Check(store.ReadCollection().unlocked.Count==32&&store.ReadCollection().duplicates[16]==1,"trade preserves unlock");
        var offline=new CollectionStore("test","offline",new Memory());offline.RecordWin("off1",boundary,false);offline.RecordWin("off2",boundary,false);
        Check(offline.ReadCollection().pendingWins.Count==1&&offline.ReadCollection().rewards.Count==0,"offline pending");
        var server=CollectionStore.NewDocument("test","offline");server.serverRevision=1;offline.ApplyAuthoritativeSnapshot(server);Check(offline.ReadCollection().pendingWins.Count==1,"sync retains pending");
        server.rewards.Add(new CollectionReward{day="20260926",ingredientId="food_16",firstUnlock=true});server.unlocked.Add("food_16");server.serverRevision=2;
        offline.ApplyAuthoritativeSnapshot(server);offline.ApplyAuthoritativeSnapshot(server);Check(offline.ReadCollection().pendingWins.Count==0&&offline.ReadCollection().unlocked.Count==17,"server receipt idempotence");
        memory.fail=true;var before=store.ReadCollection();try{store.TrySaveSelection(Enumerable.Range(0,16).Select(CollectionCatalog.Id));throw new Exception("expected failure");}catch(Exception e){Check(e.Message=="disk full","save failure");}Check(store.ReadCollection().selected.Count==before.selected.Count,"failed write rollback");
        var disk=new Dictionary<string,string>();var options=new JsonSerializerOptions{IncludeFields=true};
        var persistence=new CollectionPersistence("key","test","user",k=>disk.TryGetValue(k,out var v)?v:"",(k,v)=>disk[k]=v,d=>JsonSerializer.Serialize(d,options),s=>JsonSerializer.Deserialize<CollectionDocument>(s,options));
        persistence.Save(before);persistence.Save(before);disk["key"]="corrupt";Check(persistence.Load().selected.Count==32,"backup recovery");
        var factory=DailySessionFactory.FromProductionJson(File.ReadAllText("Unity/Assets/HotpotSort/Content/Daily/"+DailyContent.RuntimeFileName));
        var selected=factory.WithIngredientSelection(Enumerable.Range(16,16).Select(CollectionCatalog.Id));
        var replayContext=new ChallengeContext("20260925",selected.Content.ContentVersion,selected.ConfigurationDigest,"diagnostic",0);
        var replay=HotpotSort.Replay.DailyReplay.Run(factory,selected.CreateDailySession(replayContext).ExportReplay());
        Check(replay.Success,"replay restores actual selected catalog: "+replay.Error);
        var context=new ChallengeContext("20260925",factory.Content.ContentVersion,factory.ConfigurationDigest,"diagnostic",0);
        var changedContext=new ChallengeContext("20260925",selected.Content.ContentVersion,selected.ConfigurationDigest,"diagnostic",0);
        var baseline=CanonicalJson.Map(CanonicalJson.Parse(factory.CreateDailySession(context).Snapshot.CanonicalStateJson));
        var mapped=CanonicalJson.Map(CanonicalJson.Parse(selected.CreateDailySession(changedContext).Snapshot.CanonicalStateJson));
        Check(CanonicalJson.Write(baseline["mappingRng"])==CanonicalJson.Write(mapped["mappingRng"])&&CanonicalJson.Write(baseline["directorRng"])==CanonicalJson.Write(mapped["directorRng"]),"core random unchanged");
        Check(CanonicalJson.Write(baseline["items"])==CanonicalJson.Write(mapped["items"])&&CanonicalJson.Write(baseline["pendingPlateIds"])==CanonicalJson.Write(mapped["pendingPlateIds"]),"core structure unchanged");
        Check(CanonicalJson.Map(mapped["mapping"]).Values.Cast<string>().OrderBy(x=>x).SequenceEqual(Enumerable.Range(16,16).Select(CollectionCatalog.Id)),"snapshot actual subset");
        Console.WriteLine("PASS TASK030 defaults, selection, boundary, reward, idempotence, tools, random, offline merge, atomic persistence, backup recovery");return 0;
    }
}
