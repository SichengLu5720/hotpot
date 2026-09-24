using System;
using System.Collections.Generic;
using System.Linq;
using HotpotSort.Contracts;
using HotpotSort.Determinism;

namespace HotpotSort.Core
{
    public sealed class DailySession : IGameSession, IRevivalSessionState
    {
        public const string StateSchema = "daily_state_v3", EventSchema = "daily_event_v3", ReplaySchema = "daily_replay_v3";
        public const string LegacyStateSchema = "daily_state_v2", LegacyEventSchema = "daily_event_v2", LegacyReplaySchema = "daily_replay_v2";
        readonly DailyRulesVersion rules;
        string CurrentStateSchema => rules == DailyRulesVersion.LegacyV2 ? LegacyStateSchema : StateSchema;
        string CurrentEventSchema => rules == DailyRulesVersion.LegacyV2 ? LegacyEventSchema : EventSchema;
        string CurrentReplaySchema => rules == DailyRulesVersion.LegacyV2 ? LegacyReplaySchema : ReplaySchema;
        readonly object gate = new object();
        readonly DailySessionFactory factory;
        readonly ChallengeContext context;
        readonly string sessionId = Guid.NewGuid().ToString("N");
        readonly List<CoreItem> items = new List<CoreItem>();
        readonly Dictionary<int,int> plateSizes = new Dictionary<int,int>();
        readonly List<int> pending = new List<int>(), active = new List<int>();
        readonly int?[] buffer = new int?[5];
        readonly CoreOrder[] orders = { new CoreOrder(), new CoreOrder(), new CoreOrder(), new CoreOrder() };
        readonly Dictionary<string, object> mapping = new Dictionary<string, object>(StringComparer.Ordinal);
        readonly List<string> allEvents = new List<string>(), batch = new List<string>(), records = new List<string>(), diagnostics = new List<string>();
        readonly DailyDirector director;
        readonly Pcg32 mappingRng, directorRng, presentationRng;
        readonly ulong seed;
        readonly DailyFixture fixture;
        GameStatus status = GameStatus.Ready;
        GameSnapshot snapshot;
        ulong eventSeq, transactionId, boundary, inputSeq, observationSeq, elapsed;
        bool hasInputSeq, hasObservationSeq, resolving, disposed;
        int taps, processed, completedOrders, absorbed, maxBuffer, rejectedTaps, spawned, nextOrderIdentity;
        string failureCode;
        int? failureItem;
        string initialHash;
        bool revivalPending, revivalUsed;
        string revivalOfferId, revivalTransferToken;
        object revivalTransfer;
        HashSet<int> commandClickability;
        HashSet<int> commandUnknownClickability;
        int commandClickabilityPolicy;
        public bool RevivalPending { get { lock(gate) return revivalPending; } }
        public bool RevivalUsed { get { lock(gate) return revivalUsed; } }
        public string RevivalOfferId { get { lock(gate) return revivalOfferId; } }
        public string RevivalTransferToken { get { lock(gate) return revivalTransferToken; } }
        public GameSnapshot Snapshot { get { lock (gate) return snapshot; } }
        public string StateHash { get { lock (gate) return Hash(); } }
        public string InitialHash => initialHash;
        public string CoreEventsJson { get { lock (gate) return "[" + string.Join(",", allEvents) + "]"; } }
        public string DiagnosticsJson { get { lock (gate) return "[" + string.Join(",", diagnostics) + "]"; } }
        public event Action<GameSnapshot, GameEventBatch> Changed;

        internal DailySession(DailySessionFactory factory, ChallengeContext context, DailyFixture fixture, DailyRulesVersion rules)
        {
            if(!Enum.IsDefined(typeof(DailyRulesVersion),rules))throw new ArgumentOutOfRangeException(nameof(rules));
            this.rules=rules;
            this.factory = factory; this.context = context ?? throw new ArgumentNullException(nameof(context)); this.fixture = fixture;
            seed = Pcg32.DailySeed(context.ChallengeId, context.ContentVersion);
            mappingRng = new Pcg32(Pcg32.StreamSeed(seed, "MappingRng")); directorRng = new Pcg32(Pcg32.StreamSeed(seed, "DirectorRng")); presentationRng = new Pcg32(Pcg32.StreamSeed(seed, "PresentationRng"));
            director = new DailyDirector(factory.Content);
            Begin(0);
            try
            {
                if (context.ContentVersion != factory.Content.ContentVersion || context.ConfigurationDigest != factory.ConfigurationDigest)
                    throw new ArgumentException("ConfigurationIdentityMismatch");
                int id = 1;
                foreach (var plate in factory.Content.Plates)
                {
                    plateSizes.Add(plate.PlateId, plate.Kinds.Count);
                    pending.Add(plate.PlateId);
                    for (int source = 0; source < plate.Kinds.Count; source++) items.Add(new CoreItem { Id = id++, PlateId = plate.PlateId, SourceIndex = source, Kind = plate.Kinds[source] });
                }
                var catalogIds = factory.Catalog.Select(x => x.IngredientId).ToArray();
                for (int i = catalogIds.Length - 1; i > 0; i--) { int j = (int)mappingRng.NextBounded((uint)(i + 1)); string tmp = catalogIds[i]; catalogIds[i] = catalogIds[j]; catalogIds[j] = tmp; }
                for (int i = 0; i < 16; i++) mapping.Add(((char)('A' + i)).ToString(), catalogIds[i]);
                status = GameStatus.Running;
                Emit("ChallengeInitialized", CanonicalJson.Object("challengeId", context.ChallengeId, "contentVersion", context.ContentVersion, "dailySeed", CanonicalJson.U64(seed), "identities", Identities()));
                Emit("IngredientMappingCreated", CanonicalJson.Object("mapping", mapping, "rng", mappingRng.Snapshot()));
                OpeningOrders();
                if (fixture != null) ApplyFixture(fixture);
            }
            catch (ArgumentException e) { Abort("InitializationError", e.Message); }
            Close(); initialHash = Hash(); resolving = false;
        }
        object Identities() => CanonicalJson.Object("contentDigest", factory.Content.Digest, "catalogDigest", factory.CatalogDigest,
            "configurationDigest", factory.ConfigurationDigest, "rng", Pcg32.Version, "seed", Pcg32.SeedVersion,
            "director", DailyContent.DirectorVersion, "canonical", CanonicalJson.Version);
        void OpeningOrders()
        {
            var counts = new Dictionary<string, object>(StringComparer.Ordinal);
            for (char kind = 'A'; kind <= 'P'; kind++) counts.Add(kind.ToString(), items.Count(x => x.PlateId <= 10 && x.Kind == kind.ToString()));
            for (int slot = 0; slot < 2; slot++)
            {
                var legal = Enumerable.Range(0, 16).Select(i => ((char)('A' + i)).ToString())
                    .Where(k => items.Count(x => x.Kind == k) >= 3 && !orders.Any(o => o.Kind == k)).ToList();
                bool expanded = !legal.Any(k => (int)counts[k] > 0);
                var ranked = legal.OrderByDescending(k => expanded ? items.Count(x => x.Kind == k) : (int)counts[k]).ThenBy(k => k, StringComparer.Ordinal).ToList();
                orders[slot].Kind = ranked.FirstOrDefault();
                orders[slot].Enabled = true; orders[slot].Identity = ++nextOrderIdentity;
                Emit("OpeningOrderSelected", CanonicalJson.Object("slotId", slot, "kind", orders[slot].Kind, "lookaheadCounts", counts,
                    "lookaheadPlates", expanded ? 50 : 10, "tieBreak", "kindIdAscending", "configurationError", orders[slot].Kind == null));
            }
        }
        void ApplyFixture(DailyFixture f)
        {
            if (f.SpawnedPlateCount < 0 || f.SpawnedPlateCount > 50 || f.Buffer.Count != 5 || f.Orders.Count < 2 || f.Orders.Count > 4 || f.Completed.Count % 3 != 0)
                throw new ArgumentException("Invalid fixture shape");
            spawned = f.SpawnedPlateCount; pending.RemoveAll(p => p <= spawned);
            foreach (var item in items) if (item.PlateId <= spawned) item.Location = "ActiveAvailable";
            var used = new HashSet<int>();
            Action<int, string, int> place = (id, location, slot) =>
            {
                if (id < 1 || id > items.Count || !used.Add(id) || items[id - 1].PlateId > spawned) throw new ArgumentException("Invalid fixture item assignment");
                items[id - 1].Location = location; items[id - 1].Slot = slot;
            };
            foreach (int id in f.Completed) place(id, "Completed", -1);
            for (int i = 0; i < 5; i++) { buffer[i] = f.Buffer[i]; if (buffer[i].HasValue) place(buffer[i].Value, "Buffer", i); }
            for (int i = 0; i < f.Orders.Count; i++)
            {
                var o = f.Orders[i]; orders[i].Kind = o.Kind;
                orders[i].Enabled = true; orders[i].Identity = ++nextOrderIdentity;
                if (o.Kind != null && (o.Kind.Length != 1 || o.Kind[0] < 'A' || o.Kind[0] > 'P')) throw new ArgumentException("Invalid fixture order kind");
                foreach (int id in o.ItemIds) { place(id, "Order", i); if (items[id - 1].Kind != o.Kind) throw new ArgumentException("Fixture order kind mismatch"); orders[i].Items.Add(id); }
            }
            active.AddRange(items.Where(x => x.Location == "ActiveAvailable").Select(x => x.PlateId).Distinct().OrderBy(x => x));
            completedOrders = f.Completed.Count / 3; processed = items.Count(x => x.Location == "Buffer" || x.Location == "Order" || x.Location == "Completed");
            maxBuffer = buffer.Count(x => x.HasValue);
            Emit("FixtureInitialized", FixtureJson(f));
            Settle();
        }
        internal static object FixtureJson(DailyFixture f) => f == null ? null : CanonicalJson.Object("spawnedPlateCount", f.SpawnedPlateCount,
            "buffer", f.Buffer, "orders", f.Orders.Select(o => CanonicalJson.Object("kind", o.Kind, "itemIds", o.ItemIds)).ToArray(), "completed", f.Completed);
        void Begin(ulong nextBoundary)
        {
            resolving = true; batch.Clear(); transactionId = checked(transactionId + 1);
            if (status == GameStatus.Running) elapsed = checked(elapsed + (nextBoundary - boundary));
            boundary = nextBoundary;
        }
        void Emit(string type, object data)
        {
            eventSeq = checked(eventSeq + 1);
            var fields = CanonicalJson.Map(data);
            if (fields.ContainsKey("itemId"))
            {
                var item = items[CanonicalJson.Int(fields["itemId"]) - 1];
                fields["ingredientId"] = mapping.ContainsKey(item.Kind) ? mapping[item.Kind] : null;
                fields["plateId"] = item.PlateId;
            }
            if (fields.ContainsKey("slotId"))
            {
                int slot = CanonicalJson.Int(fields["slotId"]);
                fields["orderIdentity"] = orders[slot].Identity;
                if (!fields.ContainsKey("ingredientId") && orders[slot].Kind != null) fields["ingredientId"] = mapping[orders[slot].Kind];
            }
            var json = CanonicalJson.Write(CanonicalJson.Object("schemaVersion", CurrentEventSchema, "eventSeq", CanonicalJson.U64(eventSeq),
                "transactionId", CanonicalJson.U64(transactionId), "type", type, "data", data));
            batch.Add(json); allEvents.Add(json);
        }
        void Abort(string code, string detail)
        {
            status = GameStatus.Aborted; failureCode = code;
            Emit("InvariantViolation", CanonicalJson.Object("code", code, "detail", detail, "stateSnapshot", State()));
        }
        void Close(bool evaluate = true)
        {
            string error = InvariantError();
            if (error != null && status != GameStatus.Aborted) Abort("InvariantViolation", error);
            if (evaluate && status == GameStatus.Running)
            {
                int completed = items.Count(x => x.Location == "Completed");
                if (completed == 183)
                {
                    if (pending.Count != 0 || active.Count != 0 || buffer.Any(x => x.HasValue) || orders.Any(x => x.Items.Count > 0)) Abort("InconsistentWin", "Containers not empty");
                    else { status = GameStatus.Won; Emit("ChallengeWon", CanonicalJson.Object("tapCount", taps, "maxBuffer", maxBuffer, "durationBoundaries", CanonicalJson.U64(elapsed))); }
                }
                else if (orders.Where(o=>o.Enabled).All(o => o.Kind == null)) Abort("NoLegalOrder", "Unfinished items with all orders empty");
            }
            // Reserve the closing sequence before hashing: hashAfter identifies the exact
            // stable snapshot observers receive, not the state before TransactionClosed.
            eventSeq = checked(eventSeq + 1);
            string hash = Hash();
            string closed = CanonicalJson.Write(CanonicalJson.Object("schemaVersion", CurrentEventSchema, "eventSeq", CanonicalJson.U64(eventSeq),
                "transactionId", CanonicalJson.U64(transactionId), "type", "TransactionClosed", "data", CanonicalJson.Object("hashAfter", hash)));
            batch.Add(closed); allEvents.Add(closed); RefreshSnapshot();
        }
        CommandResult Publish(bool accepted, string reason)
        {
            var result = new CommandResult(accepted, reason, Hash(), snapshot, new GameEventBatch(sessionId, CanonicalJson.U64(transactionId), batch));
            try { Changed?.Invoke(snapshot, result.Events); } finally { resolving = false;commandClickability=null; }
            return result;
        }
        CommandResult Reject(string type, string reason, object details)
        {
            if (type == "TapRejected") rejectedTaps++;
            diagnostics.Add(CanonicalJson.Write(CanonicalJson.Object("type", type, "reason", reason, "details", details)));
            return new CommandResult(false, reason, Hash(), snapshot, new GameEventBatch(sessionId, CanonicalJson.U64(transactionId), new string[0]));
        }
        string Guard(ulong nextBoundary) => disposed ? "Disposed" : resolving ? "Resolving" : nextBoundary < boundary ? "NonMonotonicBoundary" : null;
        HashSet<int> ValidateClickability(ClickableObservation observation,Dictionary<string,object> record)
        {
            commandUnknownClickability=null;
            commandClickabilityPolicy=1;
            if(observation==null||observation.SessionId!=sessionId||observation.SnapshotRevision!=snapshot.TransactionId)return null;
            if(observation.PolicyVersion<1||observation.PolicyVersion>4)return null;
            if(observation.ItemIds.Concat(observation.UnknownItemIds).Any(id=>id<1||id>items.Count||items[id-1].Location!="ActiveAvailable"))return null;
            var valid=new HashSet<int>(observation.ItemIds);
            if(observation.UnknownItemIds.Any(valid.Contains))return null;
            commandUnknownClickability=new HashSet<int>(observation.UnknownItemIds);
            commandClickabilityPolicy=observation.PolicyVersion;
            var data=CanonicalJson.Object("version",1,"snapshotRevision",observation.SnapshotRevision,"itemIds",valid.OrderBy(id=>id).ToArray());
            if(commandClickabilityPolicy>=2)data.Add("policyVersion",commandClickabilityPolicy);
            if(commandUnknownClickability.Count>0)data.Add("unknownItemIds",commandUnknownClickability.OrderBy(id=>id).ToArray());
            record.Add("clickability",data);
            return valid;
        }
        public bool MayRefillOrdersOnTap(int itemId)
        {
            lock(gate)return itemId>=1&&itemId<=items.Count&&items[itemId-1].Location=="ActiveAvailable"&&orders.Any(o=>o.Enabled&&o.Kind==items[itemId-1].Kind&&o.Items.Count==2);
        }
        public CommandResult Tap(TapCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            lock (gate)
            {
                var detail = CanonicalJson.Object("itemId", command.ItemId, "inputSeq", CanonicalJson.U64(command.InputSeq), "logicalBoundary", CanonicalJson.U64(command.LogicalBoundary), "hitAccepted", command.HitAccepted);
                string reason = Guard(command.LogicalBoundary);
                if (reason == null && status != GameStatus.Running) reason = "NotRunning";
                if (reason == null && hasInputSeq && command.InputSeq <= inputSeq) reason = "NonIncreasingInputSequence";
                if (reason == null && !command.HitAccepted) reason = "HitRejected";
                if (reason == null && (command.ItemId < 1 || command.ItemId > items.Count || items[command.ItemId - 1].Location != "ActiveAvailable")) reason = "ItemUnavailable";
                if (reason != null) return Reject("TapRejected", reason, detail);
                if (command.LogicalBoundary >= 600000) return Timeout(command.LogicalBoundary);
                commandClickability=ValidateClickability(command.Clickability,detail);
                inputSeq = command.InputSeq; hasInputSeq = true;
                string before = Hash(); Begin(command.LogicalBoundary); taps++;
                var item = items[command.ItemId - 1]; item.Location = "Reserved";
                Emit("TapAccepted", CanonicalJson.Object("itemId", item.Id, "plateId", item.PlateId, "kind", item.Kind, "stateHashBefore", before));
                int target = Enumerable.Range(0, 4).Where(i => orders[i].Enabled && orders[i].Kind == item.Kind && orders[i].Items.Count < 3)
                    .OrderByDescending(i => orders[i].Items.Count).ThenBy(i => i).DefaultIfEmpty(-1).First();
                int free = System.Array.FindIndex(buffer, x => !x.HasValue);
                if (target < 0 && free < 0)
                {
                    item.Location = "ActiveAvailable"; item.Slot = -1; failureCode = "BufferOverflow"; failureItem = item.Id;
                    Emit("BufferOverflowAttempted", CanonicalJson.Object("itemId", item.Id, "kind", item.Kind, "buffer", buffer));
                    if(rules==DailyRulesVersion.RevivalV3 && !revivalUsed)
                    {
                        revivalPending=true;revivalOfferId="revival-"+CanonicalJson.U64(transactionId);status=GameStatus.Paused;
                        Emit("RevivalOffered",CanonicalJson.Object("revivalOfferId",revivalOfferId,"itemId",item.Id));
                    }
                    else
                    {
                        status = GameStatus.Failed;
                        Emit("ChallengeFailed", CanonicalJson.Object("reason", failureCode, "itemId", item.Id, "durationBoundaries", CanonicalJson.U64(elapsed), "stateHash", Hash()));
                    }
                }
                else
                {
                    processed++;
                    if (target >= 0)
                    {
                        int filled = orders[target].Items.Count; item.Location = "Order"; item.Slot = target; orders[target].Items.Add(item.Id);
                        Emit("ItemRoutedToOrder", CanonicalJson.Object("itemId", item.Id, "slotId", target, "sourceContainer", "Plate", "sourceSlot", item.SourceIndex, "targetContainer", "Order", "targetSlot", target, "filledBefore", filled, "filledAfter", filled + 1));
                    }
                    else
                    {
                        item.Location = "Buffer"; item.Slot = free; buffer[free] = item.Id; maxBuffer = Math.Max(maxBuffer, buffer.Count(x => x.HasValue));
                        Emit("ItemRoutedToBuffer", CanonicalJson.Object("itemId", item.Id, "bufferIndex", free, "sourceContainer", "Plate", "sourceSlot", item.SourceIndex, "targetContainer", "Buffer", "targetSlot", free, "filledBefore", 0, "filledAfter", 1, "buffer", buffer));
                    }
                    if (!items.Any(x => x.PlateId == item.PlateId && x.Location == "ActiveAvailable"))
                    { active.Remove(item.PlateId); Emit("PlateCleared", CanonicalJson.Object("plateId", item.PlateId)); }
                    Settle();
                }
                Close(); Record("Tap", detail); return Publish(true, status == GameStatus.Failed ? "BufferOverflow" : null);
            }
        }
        void Settle()
        {
            while (status == GameStatus.Running)
            {
                if (completedOrders >= 31 && !orders[2].Enabled) UnlockOrder(2);
                if (completedOrders >= 49 && !orders[3].Enabled) UnlockOrder(3);
                int slot = System.Array.FindIndex(orders, o => o.Items.Count == 3);
                if (slot < 0) break;
                var order = orders[slot]; string kind = order.Kind;
                foreach (int id in order.Items) { items[id - 1].Location = "Completed"; items[id - 1].Slot = -1; }
                order.Items.Clear(); completedOrders++;
                Emit("OrderCompleted", CanonicalJson.Object("slotId", slot, "kind", kind, "filledBefore", 3, "filledAfter", 3, "completionOrdinal", completedOrders));
                order.Kind = null;
                FillOrder(slot);
            }
        }
        void UnlockOrder(int slot)
        {
            orders[slot].Enabled = true;
            Emit("PotUnlocked", CanonicalJson.Object("slotId",slot));
            FillOrder(slot);
        }
        void FillOrder(int slot)
        {
                var order=orders[slot];
                var choice = director.Choose(slot, items, orders, buffer, pending, directorRng,clickable:commandClickability,unknown:commandUnknownClickability,completedOrders:commandClickabilityPolicy>=2?completedOrders:15,policyVersion:commandClickabilityPolicy);
                Emit("DirectorEvaluated", choice.Diagnostic);
                order.Kind = choice.Kind; order.Identity = ++nextOrderIdentity;
                if (order.Kind == null) return;
                Emit("OrderCreated", CanonicalJson.Object("slotId", slot, "kind", order.Kind, "filledBefore", 0, "filledAfter", 0, "selectionReason", choice.Fallback));
                for (int index = 0; index < 5 && order.Items.Count < 3; index++)
                {
                    if (!buffer[index].HasValue) continue; var item = items[buffer[index].Value - 1];
                    if (item.Kind != order.Kind) continue;
                    int before=order.Items.Count;
                    buffer[index] = null; item.Location = "Order"; item.Slot = slot; order.Items.Add(item.Id); absorbed++;
                    Emit("BufferAutoAbsorbed", CanonicalJson.Object("itemId", item.Id, "slotId",slot, "fromIndex", index, "toSlotId", slot,"sourceContainer","Buffer","sourceSlot",index,"targetContainer","Order","targetSlot",slot,"filledBefore",before,"filledAfter",before+1));
                }
                // Each iteration permanently completes three unique items. No timing lock
                // or fixed chain count is needed; conservation is checked at transaction close.
        }
        public int PlateSize(int plateId) { lock(gate) return plateSizes[plateId]; }
        public int FindHintItem()
        {
            lock(gate) return status == GameStatus.Running ? items.Where(i=>i.Location=="ActiveAvailable" && orders.Any(o=>o.Enabled && o.Kind==i.Kind && o.Items.Count<3)).Select(i=>i.Id).DefaultIfEmpty(0).First() : 0;
        }
        public bool CanClearBuffer { get { lock(gate) return status==GameStatus.Running && buffer.Any(i=>i.HasValue); } }
        public bool CanUnlockFourth { get { lock(gate) return status==GameStatus.Running && !orders[3].Enabled; } }
        public CommandResult ClearBuffer(ulong logicalBoundary)
        {
            lock(gate)
            {
                if(Guard(logicalBoundary)!=null || !CanClearBuffer)return Reject("ClearBufferRejected","NoTarget",null);
                if(logicalBoundary>=600000)return Timeout(logicalBoundary);
                Begin(logicalBoundary);ReturnBufferToQueue();
                Close();Record("ClearBuffer",CanonicalJson.Object("logicalBoundary",CanonicalJson.U64(logicalBoundary)));return Publish(true,null);
            }
        }
        int ReturnBufferToQueue()
        {
            int plate=plateSizes.Keys.Max()+1,index=0;
            var ids=buffer.Where(i=>i.HasValue).Select(i=>i.Value).ToArray();plateSizes.Add(plate,ids.Length);pending.Add(plate);
            foreach(int id in ids){var item=items[id-1];item.PlateId=plate;item.SourceIndex=index++;item.Location="Pending";item.Slot=-1;}
            System.Array.Clear(buffer,0,buffer.Length);processed-=ids.Length;
            Emit("BufferReturnedToQueue",CanonicalJson.Object("plateId",plate,"itemIds",ids));
            return plate;
        }
        public CommandResult ResolveRevival(ResolveRevivalCommand command)
        {
            if(command==null)throw new ArgumentNullException(nameof(command));
            lock(gate)
            {
                string reason=Guard(command.LogicalBoundary);
                if(reason==null && (rules!=DailyRulesVersion.RevivalV3 || !revivalPending || revivalUsed || status!=GameStatus.Paused || command.OfferId!=revivalOfferId))reason="StaleRevivalOffer";
                if(reason==null && command.Success && string.IsNullOrEmpty(command.RequestId))reason="MissingRewardRequest";
                if(reason!=null)return Reject("ResolveRevivalRejected",reason,null);
                var data=CanonicalJson.Object("revivalOfferId",command.OfferId,"requestId",command.RequestId,"success",command.Success,"logicalBoundary",CanonicalJson.U64(command.LogicalBoundary));
                Begin(command.LogicalBoundary);
                if(command.Success)
                {
                    // Capture source-slot facts BEFORE clearing. Item IDs need not be sorted.
                    var transfers=buffer.Select((id,slot)=>new { id,slot }).Where(x=>x.id.HasValue).Select((x,index)=>
                        CanonicalJson.Object("itemId",x.id.Value,"ingredientId",mapping[items[x.id.Value-1].Kind],"sourceSlot",x.slot,"targetIndex",index)).ToArray();
                    int plate=ReturnBufferToQueue();revivalUsed=true;
                    revivalTransferToken="revival-transfer-"+CanonicalJson.U64(transactionId);
                    revivalTransfer=CanonicalJson.Object("schemaVersion","revival_transfer_v1","revivalOfferId",revivalOfferId,"requestId",command.RequestId,
                        "completionToken",revivalTransferToken,"newPlateId",plate,"transactionId",CanonicalJson.U64(transactionId),"eventSeq",CanonicalJson.U64(eventSeq+1),"items",transfers);
                    failureCode=null;failureItem=null;
                    Emit("RevivalTransferStarted",revivalTransfer);
                }
                else
                {
                    revivalPending=false;status=GameStatus.Failed;
                    Emit("ChallengeFailed",CanonicalJson.Object("reason","BufferOverflow","revivalOfferId",revivalOfferId,"requestId",command.RequestId,"itemId",failureItem.Value));
                }
                Close();Record("ResolveRevival",data);return Publish(true,null);
            }
        }
        public CommandResult CompleteRevivalTransfer(string offerId,string token,ulong logicalBoundary)
        {
            lock(gate)
            {
                if(Guard(logicalBoundary)!=null || !revivalPending || !revivalUsed || status!=GameStatus.Paused || offerId!=revivalOfferId || token!=revivalTransferToken || string.IsNullOrEmpty(token))
                    return Reject("RevivalTransferCompletionRejected","StaleCompletion",null);
                var data=CanonicalJson.Object("revivalOfferId",offerId,"completionToken",token,"logicalBoundary",CanonicalJson.U64(logicalBoundary));
                Begin(logicalBoundary);revivalPending=false;revivalTransferToken=null;revivalTransfer=null;
                Emit("RevivalTransferCompleted",data);Close();Record("CompleteRevivalTransfer",data);return Publish(true,null);
            }
        }
        public CommandResult UnlockFourth(ulong logicalBoundary,ClickableObservation clickability=null)
        {
            lock(gate)
            {
                if(Guard(logicalBoundary)!=null || !CanUnlockFourth)return Reject("UnlockRejected","NoTarget",null);
                if(logicalBoundary>=600000)return Timeout(logicalBoundary);
                var detail=CanonicalJson.Object("logicalBoundary",CanonicalJson.U64(logicalBoundary));commandClickability=ValidateClickability(clickability,detail);
                Begin(logicalBoundary);UnlockOrder(3);Settle();Close();Record("UnlockFourth",detail);return Publish(true,null);
            }
        }
        public CommandResult Timeout(ulong logicalBoundary)
        {
            lock(gate)
            {
                if(Guard(logicalBoundary)!=null || status!=GameStatus.Running || logicalBoundary<600000)return Reject("TimeoutRejected","NotDue",null);
                Begin(logicalBoundary);status=GameStatus.Failed;failureCode="Timeout";
                Emit("ChallengeFailed",CanonicalJson.Object("reason",failureCode));Close();Record("Timeout",CanonicalJson.Object("logicalBoundary",CanonicalJson.U64(logicalBoundary)));return Publish(true,"Timeout");
            }
        }
        public CommandResult Supply(SupplyObservation observation)
        {
            if (observation == null) throw new ArgumentNullException(nameof(observation));
            lock (gate)
            {
                var data = CanonicalJson.Object("observationSeq", CanonicalJson.U64(observation.ObservationSeq), "logicalBoundary", CanonicalJson.U64(observation.LogicalBoundary), "canSpawn", observation.CanSpawn, "cooldownReady", observation.CooldownReady);
                string reason = Guard(observation.LogicalBoundary);
                if (reason == null && status != GameStatus.Running) reason = "NotRunning";
                if (reason == null && hasObservationSeq && observation.ObservationSeq <= observationSeq) reason = "NonIncreasingObservationSequence";
                if (reason == null && (!observation.CanSpawn || !observation.CooldownReady)) reason = "SupplyBlocked";
                if (reason == null && pending.Count == 0) reason = "PendingEmpty";
                if (reason != null) return Reject("SupplyRejected", reason, data);
                if (observation.LogicalBoundary >= 600000) return Timeout(observation.LogicalBoundary);
                observationSeq = observation.ObservationSeq; hasObservationSeq = true;
                return CommitSupply(observation.LogicalBoundary, observation.ObservationSeq);
            }
        }
        CommandResult CommitSupply(ulong nextBoundary, ulong seq)
        {
            Begin(nextBoundary); int plate = pending[0]; pending.RemoveAt(0); active.Add(plate); spawned++;
            var ids = items.Where(x => x.PlateId == plate && x.Location == "Pending").Select(x => x.Id).ToArray();
            foreach (int id in ids) items[id - 1].Location = "ActiveAvailable";
            // Presentation draws are intentionally absent from core events and core hashes.
            Emit("PlateSpawned", CanonicalJson.Object("plateId", plate, "spawnIndex", spawned, "itemIds", ids));
            Close(); Record("SupplyCommit", CanonicalJson.Object("plateId", plate, "itemIds", ids, "logicalBoundary", CanonicalJson.U64(nextBoundary), "observationSeq", CanonicalJson.U64(seq)));
            return Publish(true, null);
        }
        internal CommandResult ReplaySupply(int plateId, int[] itemIds, ulong nextBoundary, ulong seq)
        {
            lock (gate)
            {
                if (Guard(nextBoundary) != null || status != GameStatus.Running || pending.Count == 0 || pending[0] != plateId ||
                    !items.Where(x => x.PlateId == plateId).Select(x => x.Id).SequenceEqual(itemIds) || (hasObservationSeq && seq <= observationSeq))
                    throw new ArgumentException("Illegal replay supply order or item IDs");
                observationSeq = seq; hasObservationSeq = true; return CommitSupply(nextBoundary, seq);
            }
        }
        public void Pause() { lock (gate) Pause(boundary); }
        public void Resume() { lock (gate) Resume(boundary); }
        public CommandResult Pause(ulong logicalBoundary) => ChangePause(true, logicalBoundary);
        public CommandResult Resume(ulong logicalBoundary) => ChangePause(false, logicalBoundary);
        CommandResult ChangePause(bool pause, ulong nextBoundary)
        {
            lock (gate)
            {
                string type = pause ? "Pause" : "Resume"; var data = CanonicalJson.Object("logicalBoundary", CanonicalJson.U64(nextBoundary));
                string reason = Guard(nextBoundary);
                if(reason==null && !pause && revivalPending)reason="RevivalPending";
                if (reason == null && status != (pause ? GameStatus.Running : GameStatus.Paused)) reason = "IneffectiveBoundary";
                if (reason != null) return Reject(type + "Rejected", reason, data);
                Begin(nextBoundary); status = pause ? GameStatus.Paused : GameStatus.Running;
                Emit(pause ? "ChallengePaused" : "ChallengeResumed", data); Close(); Record(type, data); return Publish(true, null);
            }
        }
        public uint NextPresentation(uint bound)
        {
            lock (gate) { if (disposed) throw new ObjectDisposedException(nameof(DailySession)); return presentationRng.NextBounded(bound); }
        }
        public string PresentationRngJson { get { lock (gate) return CanonicalJson.Write(presentationRng.Snapshot()); } }
        public string InspectDirector(int slotId)
        {
            lock (gate)
            {
                if (slotId < 0 || slotId > 3 || !orders[slotId].Enabled) throw new ArgumentOutOfRangeException(nameof(slotId));
                return CanonicalJson.Write(director.Choose(slotId, items, orders, buffer, pending, directorRng, false).Diagnostic);
            }
        }
        void Record(string type, object data)
        {
            records.Add(CanonicalJson.Write(CanonicalJson.Object("type", type, "data", data, "hashAfter", Hash(),
                "eventsHash", CanonicalJson.Hash("[" + string.Join(",", batch) + "]"))));
        }
        public ReplayPackage ExportReplay()
        {
            lock (gate)
            {
                var json = CanonicalJson.Write(CanonicalJson.Object("schemaVersion", CurrentReplaySchema,
                    "context", CanonicalJson.Object("challengeId", context.ChallengeId, "contentVersion", context.ContentVersion, "configurationDigest", context.ConfigurationDigest, "timeSource", context.TimeSource, "retryIndex", context.RetryIndex),
                    "identities", Identities(), "dailySeed", CanonicalJson.U64(seed), "initialHash", initialHash, "fixture", FixtureJson(fixture),
                    "records", records.Select(CanonicalJson.Parse).ToArray(), "diagnostics", diagnostics.Select(CanonicalJson.Parse).ToArray(), "finalHash", Hash(),
                    "coreEventsHash", CanonicalJson.Hash("[" + string.Join(",", allEvents) + "]")));
                return new ReplayPackage(sessionId, CurrentReplaySchema, json);
            }
        }
        public string StatisticsJson
        {
            get { lock (gate) return CanonicalJson.Write(CanonicalJson.Object("core", Statistics(), "rejectedTapCount", rejectedTaps, "retryIndex", context.RetryIndex, "logicalBoundary", CanonicalJson.U64(boundary), "timeSource", context.TimeSource)); }
        }
        object Statistics() => CanonicalJson.Object("acceptedTapCount", taps, "processedItemCount", processed, "completedOrderCount", completedOrders,
            "completedItemCount", items.Count(x => x.Location == "Completed"), "bufferAutoAbsorbedCount", absorbed, "maxBufferCount", maxBuffer,
            "durationBoundaries", CanonicalJson.U64(elapsed), "failureCode", failureCode, "failureItemId", failureItem);
        object State()
        {
            var state=CanonicalJson.Object("schemaVersion", CurrentStateSchema, "identities", Identities(), "challengeId", context.ChallengeId, "contentVersion", context.ContentVersion,
            "dailySeed", CanonicalJson.U64(seed), "mapping", mapping, "status", status.ToString(), "logicalBoundary", CanonicalJson.U64(boundary), "pendingPlateIds", pending, "activePlateIds", active,
            "items", items.Select(x => CanonicalJson.Object("itemId", x.Id, "plateId", x.PlateId, "sourceIndex", x.SourceIndex, "kind", x.Kind, "location", x.Location, "slot", x.Slot)).ToArray(),
            "plateSizes", plateSizes.OrderBy(p=>p.Key).Select(p=>CanonicalJson.Object("plateId",p.Key,"size",p.Value)).ToArray(),
            "buffer", buffer, "orders", orders.Select((o, i) => CanonicalJson.Object("slotId", i, "state", !o.Enabled ? "Locked" : o.Kind == null ? "Empty" : "Active", "orderIdentity",o.Identity,"kind", o.Kind, "itemIds", o.Items, "filled", o.Items.Count)).ToArray(),
            "progressNumerator", items.Count(x => x.Location == "Completed" || x.Location == "Order"), "progressDenominator", 183, "statistics", Statistics(),
            "mappingRng", mappingRng.Snapshot(), "directorRng", directorRng.Snapshot(), "transactionId", CanonicalJson.U64(transactionId), "eventSeq", CanonicalJson.U64(eventSeq));
            if(rules==DailyRulesVersion.RevivalV3)
            {
                var fields=CanonicalJson.Map(state);
                fields["revivalPending"]=revivalPending;fields["revivalUsed"]=revivalUsed;fields["revivalOfferId"]=revivalOfferId;fields["revivalTransfer"]=revivalTransfer;
            }
            return state;
        }
        string Hash() => CanonicalJson.Hash(CanonicalJson.Write(State()));
        void RefreshSnapshot() { snapshot = new GameSnapshot(sessionId, context, status, CanonicalJson.U64(eventSeq), CanonicalJson.U64(transactionId), CurrentStateSchema, CanonicalJson.Write(State())); }
        string InvariantError()
        {
            if (items.Count != 183 || items.Select(x => x.Id).Distinct().Count() != 183) return "Item count/identity";
            if (pending.Distinct().Count() != pending.Count || !pending.SequenceEqual(pending.OrderBy(x => x)) || active.Distinct().Count() != active.Count || pending.Intersect(active).Any()) return "Plate identity/order";
            if (orders.Where(o=>!o.Enabled).Any(o => o.Kind != null || o.Items.Count != 0)) return "Locked order occupied";
            var located = new HashSet<int>();
            foreach (var item in items)
            {
                if (item.Id < 1 || item.Id > 183 || items[item.Id - 1] != item) return "Item ID indexing";
                if (item.Location == "Pending" && pending.Contains(item.PlateId) && item.Slot == -1) located.Add(item.Id);
                else if (item.Location == "ActiveAvailable" && active.Contains(item.PlateId) && item.Slot == -1) located.Add(item.Id);
                else if (item.Location == "Completed" && item.Slot == -1) located.Add(item.Id);
                else if (item.Location != "Buffer" && item.Location != "Order") return "Illegal item location";
            }
            for (int i = 0; i < 5; i++) if (buffer[i].HasValue)
            {
                int id = buffer[i].Value; if (id < 1 || id > 183 || !located.Add(id) || items[id - 1].Location != "Buffer" || items[id - 1].Slot != i) return "Buffer location mismatch";
            }
            for (int i = 0; i < 4; i++)
            {
                var order = orders[i]; if (order.Items.Count > 3 || (order.Kind == null && order.Items.Count > 0)) return "Order capacity/state";
                foreach (int id in order.Items)
                    if (id < 1 || id > 183 || !located.Add(id) || items[id - 1].Location != "Order" || items[id - 1].Slot != i || items[id - 1].Kind != order.Kind) return "Order location mismatch";
            }
            if (located.Count != 183) return "Inventory conservation";
            if (active.Any(p => !items.Any(x => x.PlateId == p && x.Location == "ActiveAvailable"))) return "Empty active plate";
            if (pending.Any(p => items.Any(x => x.PlateId == p && x.Location != "Pending"))) return "Non-pending queue item";
            if (items.Count(x => x.Location == "Completed") != completedOrders * 3) return "Completed count";
            if (processed != items.Count(x => x.Location == "Order" || x.Location == "Buffer" || x.Location == "Completed")) return "Processed count";
            for (char kind = 'A'; kind <= 'P'; kind++)
            {
                string k = kind.ToString();
                if (items.Count(x => x.Kind == k && x.Location == "Completed") % 3 != 0) return "Completed kind multiplicity";
                int external = items.Count(x => x.Kind == k && (x.Location == "Pending" || x.Location == "ActiveAvailable" || x.Location == "Buffer"));
                int unmet = orders.Where(o => o.Kind == k).Sum(o => 3 - o.Items.Count);
                if (unmet > external) return "Orders exceed allocatable inventory";
            }
            return null;
        }
        public void Dispose() { lock (gate) { disposed = true; Changed = null; } }
    }
}
