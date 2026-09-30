using System;
using System.Linq;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Determinism;

namespace HotpotSort.Replay
{
    public sealed class ReplayResult
    {
        public bool Success { get; }
        // -1 denotes package/header/initialization, otherwise zero-based first record.
        public int RecordIndex { get; }
        public string Error { get; }
        public DailySession Session { get; }
        internal ReplayResult(bool success, int index, string error, DailySession session)
        { Success = success; RecordIndex = index; Error = error; Session = session; }
    }
    public static class DailyReplay
    {
        public static ReplayResult Run(DailySessionFactory factory, ReplayPackage package)
        {
            DailySession session = null; int index = -1;
            try
            {
                if (package == null || (package.SchemaVersion != DailySession.ReplaySchema && package.SchemaVersion != DailySession.PreviousReplaySchema && package.SchemaVersion != DailySession.LegacyReplaySchema)) throw new FormatException("Unknown replay version");
                var root = CanonicalJson.Map(CanonicalJson.Parse(package.CanonicalJson));
                if ((string)root["schemaVersion"] != package.SchemaVersion) throw new FormatException("Unknown replay schema");
                var c = CanonicalJson.Map(root["context"]);
                factory=factory.ForReplayContent((string)CanonicalJson.Map(root["identities"])["contentDigest"],(string)c["challengeId"]);
                bool perAttempt=package.SchemaVersion==DailySession.ReplaySchema;
                if(root.TryGetValue(perAttempt?"ingredientPool":"ingredientSelection",out var selected))factory=factory.WithIngredientSelection(CanonicalJson.Array(selected).Select(x=>(string)x));
                var rules=package.SchemaVersion==DailySession.LegacyReplaySchema?DailyRulesVersion.LegacyV2:DailyRulesVersion.RevivalV3;
                var expected = CanonicalJson.Object("contentDigest", factory.Content.Digest, "catalogDigest", factory.CatalogDigest,
                    "configurationDigest", factory.ConfigurationDigest, "rng", Pcg32.Version, "seed", Pcg32.SeedVersion,
                    "director", DailyContent.DirectorVersion, "canonical", CanonicalJson.Version);
                if (CanonicalJson.Write(root["identities"]) != CanonicalJson.Write(expected)) throw new FormatException("Configuration digest or algorithm identity mismatch");
                string replaySeed=perAttempt?(string)c["sessionSeed"]:null;
                var context = new ChallengeContext((string)c["challengeId"], (string)c["contentVersion"], (string)c["configurationDigest"], (string)c["timeSource"], CanonicalJson.Int(c["retryIndex"]),replaySeed);
                if (context.ContentVersion != factory.Content.ContentVersion || context.ConfigurationDigest != factory.ConfigurationDigest) throw new FormatException("Context configuration mismatch");
                var stage=root.ContainsKey("challengeStage")?(ChallengeStage)CanonicalJson.Int(root["challengeStage"]):ChallengeStage.Legacy;
                int inherited=root.ContainsKey("inheritedPotMask")?CanonicalJson.Int(root["inheritedPotMask"]):0;
                int toolLimitVersion=root.ContainsKey("toolLimitVersion")?CanonicalJson.Int(root["toolLimitVersion"]):0;
                int initialToolUseMask=root.ContainsKey("initialToolUseMask")?CanonicalJson.Int(root["initialToolUseMask"]):0;
                if(toolLimitVersion<0||toolLimitVersion>1||initialToolUseMask<0||initialToolUseMask>7||toolLimitVersion==0&&initialToolUseMask!=0||toolLimitVersion>0&&!perAttempt)throw new FormatException("Invalid tool limit header");
                int potUnlockVersion=root.ContainsKey("potUnlockVersion")?CanonicalJson.Int(root["potUnlockVersion"]):1;
                if(potUnlockVersion<1||potUnlockVersion>2||potUnlockVersion>=2&&!perAttempt)throw new FormatException("Unknown pot unlock version");
                int supportVersion=root.ContainsKey("orderSupportVersion")?CanonicalJson.Int(root["orderSupportVersion"]):1;
                if(supportVersion<1||supportVersion>4)throw new FormatException("Unknown order support version");
                int protection=root.ContainsKey("orderProtectionPercentAtStart")?CanonicalJson.Int(root["orderProtectionPercentAtStart"]):0;
                if(supportVersion>=3&&(!root.ContainsKey("orderProtectionPercentAtStart")||!root.ContainsKey("nativeD1Version")||CanonicalJson.Int(root["nativeD1Version"])!=1||protection<0||protection>(supportVersion>=4?60:20)))throw new FormatException("Invalid native D1 protection header");
                int consecutiveFailures=root.ContainsKey("consecutiveFormalFailuresAtStart")?CanonicalJson.Int(root["consecutiveFormalFailuresAtStart"]):0;
                if (CanonicalJson.ReadU64(root["dailySeed"]) != (stage==ChallengeStage.Warmup?WarmupContent.Seed(context.ChallengeId,context.ContentVersion):Pcg32.DailySeed(context.ChallengeId, context.ContentVersion))) throw new FormatException("Daily seed mismatch");
                if(perAttempt&&CanonicalJson.ReadU64(root["sessionSeed"]).ToString(System.Globalization.CultureInfo.InvariantCulture)!=context.SessionSeed)throw new FormatException("Session seed mismatch");
                DailyFixture fixture = null;
                if (root["fixture"] != null)
                {
                    var f = CanonicalJson.Map(root["fixture"]);
                    fixture = new DailyFixture(CanonicalJson.Int(f["spawnedPlateCount"]), CanonicalJson.Array(f["buffer"]).Select(x => x == null ? (int?)null : CanonicalJson.Int(x)),
                        CanonicalJson.Array(f["orders"]).Select(x => { var o = CanonicalJson.Map(x); return new FixtureOrder((string)o["kind"], CanonicalJson.Array(o["itemIds"]).Select(CanonicalJson.Int)); }),
                        CanonicalJson.Array(f["completed"]).Select(CanonicalJson.Int));
                }
                if(stage!=ChallengeStage.Legacy&&(fixture!=null||rules!=DailyRulesVersion.RevivalV3))throw new FormatException("Unsupported stage fixture/rules");
                session=perAttempt?factory.CreateReplaySession(context,fixture,stage,inherited,consecutiveFailures,supportVersion,protection,potUnlockVersion,toolLimitVersion,initialToolUseMask):factory.CreateHistoricalSession(context,fixture,rules,stage,inherited);
                if (session.Snapshot.Status == GameStatus.Aborted) throw new FormatException("Replay initialization aborted");
                if(perAttempt&&CanonicalJson.Write(CanonicalJson.Map(CanonicalJson.Parse(session.Snapshot.CanonicalStateJson))["mapping"])!=CanonicalJson.Write(root["ingredientMapping"]))throw new FormatException("Ingredient mapping mismatch");
                if (session.InitialHash != (string)root["initialHash"]) throw new FormatException("Initial hash mismatch");
                foreach (var raw in CanonicalJson.Array(root["records"]))
                {
                    index++; var record = CanonicalJson.Map(raw); var data = CanonicalJson.Map(record["data"]);
                    ulong boundary = CanonicalJson.ReadU64(data["logicalBoundary"]); CommandResult result;
                    ClickableObservation clickability=null;
                    if(data.TryGetValue("clickability",out var observed))
                    {
                        var observation=CanonicalJson.Map(observed);
                        if(CanonicalJson.Int(observation["version"])!=1)throw new FormatException("Unknown clickability observation version");
                        int policy=observation.TryGetValue("policyVersion",out var policyValue)?CanonicalJson.Int(policyValue):1;
                        if(policy<1||policy>6)throw new FormatException("Unknown clickability policy version");
                        if(observation.TryGetValue("costVersion",out var recordedCostVersion)&&(CanonicalJson.Int(recordedCostVersion)<1||CanonicalJson.Int(recordedCostVersion)>4))throw new FormatException("Unknown supply cost version");
                        clickability=new ClickableObservation(session.Snapshot.SessionId,(string)observation["snapshotRevision"],CanonicalJson.Array(observation["itemIds"]).Select(CanonicalJson.Int),observation.TryGetValue("unknownItemIds",out var unknown)?CanonicalJson.Array(unknown).Select(CanonicalJson.Int):null,policy,observation.TryGetValue("nextLayerItemIds",out var nextLayer)?CanonicalJson.Array(nextLayer).Select(CanonicalJson.Int):null,observation.TryGetValue("costVersion",out var costVersion)?CanonicalJson.Array(observation["offscreenItemIds"]).Select(CanonicalJson.Int):null,observation.TryGetValue("activePlateOrder",out var activeOrder)?CanonicalJson.Array(activeOrder).Select(CanonicalJson.Int):null,observation.ContainsKey("costVersion")?CanonicalJson.Int(observation["costVersion"]):1,observation.TryGetValue("nativeAboveItemIds",out var nativeAbove)?CanonicalJson.Array(nativeAbove).Select(CanonicalJson.Int):null);
                    }
                    switch ((string)record["type"])
                    {
                        case "BeginSwapOrder": result=session.BeginSwapOrder(CanonicalJson.Int(data["slotId"]),CanonicalJson.Int(data["orderIdentity"]),boundary,clickability);break;
                        case "CompleteSwapOrder": result=session.CompleteSwapOrder((string)data["token"],boundary,clickability);break;
                        case "Tap":
                            if (!(bool)data["hitAccepted"]) throw new FormatException("Rejected tap in accepted replay");
                            result = session.Tap(new TapCommand(CanonicalJson.Int(data["itemId"]), CanonicalJson.ReadU64(data["inputSeq"]), boundary, true,clickability)); break;
                        case "SupplyCommit":
                            result = session.ReplaySupply(CanonicalJson.Int(data["plateId"]), CanonicalJson.Array(data["itemIds"]).Select(CanonicalJson.Int).ToArray(), boundary, CanonicalJson.ReadU64(data["observationSeq"])); break;
                        case "Pause": result = session.Pause(boundary); break;
                        case "Resume": result = session.Resume(boundary); break;
                        case "ClearBuffer": result = session.ClearBuffer(boundary); break;
                        case "Redeal": result = session.Redeal(boundary,data.ContainsKey("firstBatchSize")?CanonicalJson.Int(data["firstBatchSize"]):12); break;
                        case "ReturnRedealPlate": result = session.ReturnRedealPlate(CanonicalJson.Int(data["plateId"]),boundary); break;
                        case "UnlockFourth": result = session.UnlockFourth(boundary,clickability); break;
                        case "UnlockThird": result = session.UnlockThird(boundary,clickability); break;
                        case "Timeout": result = session.Timeout(boundary); break;
                        case "ResolveRevival": result=session.ResolveRevival(new ResolveRevivalCommand((string)data["revivalOfferId"],(string)data["requestId"],(bool)data["success"],boundary));break;
                        case "CompleteRevivalTransfer": result=session.CompleteRevivalTransfer((string)data["revivalOfferId"],(string)data["completionToken"],boundary);break;
                        default: throw new FormatException("Unknown replay record type");
                    }
                    if (!result.Accepted) throw new FormatException("Non-reproducible command: " + result.Reason);
                    if (result.HashAfter != (string)record["hashAfter"]) throw new FormatException("Post-commit hash mismatch");
                    if (CanonicalJson.Hash("[" + string.Join(",", result.Events.CanonicalEvents) + "]") != (string)record["eventsHash"]) throw new FormatException("Core event hash mismatch");
                }
                if (session.StateHash != (string)root["finalHash"] || CanonicalJson.Hash(session.CoreEventsJson) != (string)root["coreEventsHash"])
                    throw new FormatException("Final state/events digest mismatch");
                return new ReplayResult(true, index, null, session);
            }
            catch (Exception e) when (e is ArgumentException || e is FormatException || e is InvalidCastException || e is OverflowException || e is System.Collections.Generic.KeyNotFoundException)
            { return new ReplayResult(false, index, e.Message, session); }
        }
    }
}
