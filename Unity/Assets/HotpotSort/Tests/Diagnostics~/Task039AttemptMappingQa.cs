using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Determinism;
using HotpotSort.Replay;

static class Task039AttemptMappingQa
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static Dictionary<string,object> State(DailySession session)=>CanonicalJson.Map(CanonicalJson.Parse(session.Snapshot.CanonicalStateJson));
    static string Json(Dictionary<string,object> state,string key)=>CanonicalJson.Write(state[key]);
    static ChallengeContext Context(DailySessionFactory factory,int retry=0,string seed=null)=>new ChallengeContext("20260928",factory.Content.ContentVersion,factory.ConfigurationDigest,"task039",retry,seed);
    static void Replay(DailySessionFactory factory,DailySession session,string label)
    {
        var result=DailyReplay.Run(factory,session.ExportReplay());
        Check(result.Success,label+" replay: "+result.Error);
        result.Session.Dispose();
    }
    static int Main()
    {
        var baseFactory=DailySessionFactory.FromProductionJson(File.ReadAllText("Unity/Assets/HotpotSort/Content/Daily/"+DailyContent.RuntimeFileName));
        var pool=Enumerable.Range(0,32).Select(i=>"food_"+i.ToString("00"));
        var factory=baseFactory.WithIngredientSelection(pool);
        var context=Context(factory);
        using var warmup=factory.CreateStage(context,ChallengeStage.Warmup);
        using var formal=factory.CreateStage(context,ChallengeStage.Formal);
        var warmState=State(warmup);var formalState=State(formal);
        Check(warmup.Snapshot.Challenge.SessionSeed!=null,"warmup effective context missing seed");
        Check(warmup.Snapshot.Challenge.SessionSeed==formal.Snapshot.Challenge.SessionSeed,"stages do not share seed");
        Check(Json(warmState,"mapping")==Json(formalState,"mapping"),"stages do not share mapping");
        Check(Json(warmState,"ingredientSelection")==Json(formalState,"ingredientSelection"),"stages do not share subset");
        var mapping=CanonicalJson.Map(warmState["mapping"]);
        Check(mapping.Count==16&&mapping.Keys.OrderBy(x=>x).SequenceEqual(Enumerable.Range(0,16).Select(i=>((char)('A'+i)).ToString())),"mapping keys are not A..P");
        Check(mapping.Values.Cast<string>().Distinct(StringComparer.Ordinal).Count()==16,"mapping is not bijective");
        Check(CanonicalJson.Array(warmState["ingredientSelection"]).Select(x=>(string)x).OrderBy(x=>x).SequenceEqual(mapping.Values.Cast<string>().OrderBy(x=>x)),"actual subset not recorded");
        Replay(factory,warmup,"warmup");Replay(factory,formal,"formal");

        using var retry=factory.CreateStage(Context(factory,1),ChallengeStage.Warmup);
        Check(retry.Snapshot.Challenge.SessionSeed!=warmup.Snapshot.Challenge.SessionSeed,"retry reused session seed");
        Check(Json(State(retry),"directorRng")==Json(warmState,"directorRng"),"new attempt changed gameplay stream");
        string recordedSeed=warmup.Snapshot.Challenge.SessionSeed;
        var restoredFactory=baseFactory.WithIngredientSelection(pool);
        using var restored=restoredFactory.CreateStage(Context(restoredFactory,0,recordedSeed),ChallengeStage.Warmup);
        Check(Json(State(restored),"mapping")==Json(warmState,"mapping"),"explicit seed did not restore mapping");
        Check(Json(State(restored),"ingredientSelection")==Json(warmState,"ingredientSelection"),"explicit seed did not restore subset");

        var oldFactory=baseFactory.WithIngredientSelection(Enumerable.Range(0,16).Select(i=>"food_"+i.ToString("00")));
        var oldContext=Context(oldFactory);
        using var previous=oldFactory.CreateHistoricalSession(oldContext,null,DailyRulesVersion.RevivalV3,ChallengeStage.Legacy,0);
        Check(previous.ExportReplay().SchemaVersion==DailySession.PreviousReplaySchema,"previous schema export");Replay(oldFactory,previous,"v3");
        using var previousWarmup=oldFactory.CreateHistoricalSession(oldContext,null,DailyRulesVersion.RevivalV3,ChallengeStage.Warmup,0);
        Check(previousWarmup.ExportReplay().SchemaVersion==DailySession.PreviousReplaySchema,"previous warmup schema export");Replay(oldFactory,previousWarmup,"v3 warmup");
        using var legacy=oldFactory.CreateDailySession(oldContext,DailyRulesVersion.LegacyV2);
        Check(legacy.ExportReplay().SchemaVersion==DailySession.LegacyReplaySchema,"legacy schema export");Replay(oldFactory,legacy,"v2");
        Console.WriteLine("TASK039_ATTEMPT_MAPPING_QA_PASS new seed, shared stages, 16-way bijection, recorded subset/mapping, replay restore, RNG isolation, v2/v3 compatibility");
        return 0;
    }
}
