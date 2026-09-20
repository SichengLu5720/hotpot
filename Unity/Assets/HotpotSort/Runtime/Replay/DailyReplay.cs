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
                if (package == null || package.SchemaVersion != DailySession.ReplaySchema) throw new FormatException("Unknown replay version");
                var root = CanonicalJson.Map(CanonicalJson.Parse(package.CanonicalJson));
                if ((string)root["schemaVersion"] != DailySession.ReplaySchema) throw new FormatException("Unknown replay schema");
                var expected = CanonicalJson.Object("contentDigest", factory.Content.Digest, "catalogDigest", factory.CatalogDigest,
                    "configurationDigest", factory.ConfigurationDigest, "rng", Pcg32.Version, "seed", Pcg32.SeedVersion,
                    "director", DailyContent.DirectorVersion, "canonical", CanonicalJson.Version);
                if (CanonicalJson.Write(root["identities"]) != CanonicalJson.Write(expected)) throw new FormatException("Configuration digest or algorithm identity mismatch");
                var c = CanonicalJson.Map(root["context"]);
                var context = new ChallengeContext((string)c["challengeId"], (string)c["contentVersion"], (string)c["configurationDigest"], (string)c["timeSource"], CanonicalJson.Int(c["retryIndex"]));
                if (context.ContentVersion != factory.Content.ContentVersion || context.ConfigurationDigest != factory.ConfigurationDigest) throw new FormatException("Context configuration mismatch");
                if (CanonicalJson.ReadU64(root["dailySeed"]) != Pcg32.DailySeed(context.ChallengeId, context.ContentVersion)) throw new FormatException("Daily seed mismatch");
                DailyFixture fixture = null;
                if (root["fixture"] != null)
                {
                    var f = CanonicalJson.Map(root["fixture"]);
                    fixture = new DailyFixture(CanonicalJson.Int(f["spawnedPlateCount"]), CanonicalJson.Array(f["buffer"]).Select(x => x == null ? (int?)null : CanonicalJson.Int(x)),
                        CanonicalJson.Array(f["orders"]).Select(x => { var o = CanonicalJson.Map(x); return new FixtureOrder((string)o["kind"], CanonicalJson.Array(o["itemIds"]).Select(CanonicalJson.Int)); }),
                        CanonicalJson.Array(f["completed"]).Select(CanonicalJson.Int));
                }
                session = fixture == null ? factory.CreateDailySession(context) : factory.CreateFixtureSession(context, fixture);
                if (session.Snapshot.Status == GameStatus.Aborted) throw new FormatException("Replay initialization aborted");
                if (session.InitialHash != (string)root["initialHash"]) throw new FormatException("Initial hash mismatch");
                foreach (var raw in CanonicalJson.Array(root["records"]))
                {
                    index++; var record = CanonicalJson.Map(raw); var data = CanonicalJson.Map(record["data"]);
                    ulong boundary = CanonicalJson.ReadU64(data["logicalBoundary"]); CommandResult result;
                    switch ((string)record["type"])
                    {
                        case "Tap":
                            if (!(bool)data["hitAccepted"]) throw new FormatException("Rejected tap in accepted replay");
                            result = session.Tap(new TapCommand(CanonicalJson.Int(data["itemId"]), CanonicalJson.ReadU64(data["inputSeq"]), boundary, true)); break;
                        case "SupplyCommit":
                            result = session.ReplaySupply(CanonicalJson.Int(data["plateId"]), CanonicalJson.Array(data["itemIds"]).Select(CanonicalJson.Int).ToArray(), boundary, CanonicalJson.ReadU64(data["observationSeq"])); break;
                        case "Pause": result = session.Pause(boundary); break;
                        case "Resume": result = session.Resume(boundary); break;
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
