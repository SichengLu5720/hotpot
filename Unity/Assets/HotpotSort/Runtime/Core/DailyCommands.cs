using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using HotpotSort.Contracts;

namespace HotpotSort.Core
{
    public sealed class TapCommand
    {
        public int ItemId { get; }
        public ulong InputSeq { get; }
        public ulong LogicalBoundary { get; }
        public bool HitAccepted { get; }
        public TapCommand(int itemId, ulong inputSeq, ulong logicalBoundary, bool hitAccepted)
        { ItemId = itemId; InputSeq = inputSeq; LogicalBoundary = logicalBoundary; HitAccepted = hitAccepted; }
    }
    public sealed class SupplyObservation
    {
        public ulong ObservationSeq { get; }
        public ulong LogicalBoundary { get; }
        public bool CanSpawn { get; }
        public bool CooldownReady { get; }
        public SupplyObservation(ulong observationSeq, ulong logicalBoundary, bool canSpawn, bool cooldownReady)
        { ObservationSeq = observationSeq; LogicalBoundary = logicalBoundary; CanSpawn = canSpawn; CooldownReady = cooldownReady; }
    }
    public sealed class CommandResult
    {
        public bool Accepted { get; }
        public string Reason { get; }
        public string HashAfter { get; }
        public GameSnapshot Snapshot { get; }
        public GameEventBatch Events { get; }
        internal CommandResult(bool accepted, string reason, string hash, GameSnapshot snapshot, GameEventBatch events)
        { Accepted = accepted; Reason = reason; HashAfter = hash; Snapshot = snapshot; Events = events; }
    }
    public sealed class FixtureOrder
    {
        public string Kind { get; }
        public IReadOnlyList<int> ItemIds { get; }
        public FixtureOrder(string kind, IEnumerable<int> itemIds)
        { Kind = kind; ItemIds = new ReadOnlyCollection<int>(new List<int>(itemIds)); }
    }
    // Explicit in-memory fixture, never reads environment variables or shared storage.
    // All omitted spawned items remain ActiveAvailable; the unspawned suffix is Pending.
    public sealed class DailyFixture
    {
        public int SpawnedPlateCount { get; }
        public IReadOnlyList<int?> Buffer { get; }
        public IReadOnlyList<FixtureOrder> Orders { get; }
        public IReadOnlyList<int> Completed { get; }
        public DailyFixture(int spawnedPlateCount, IEnumerable<int?> buffer, IEnumerable<FixtureOrder> orders, IEnumerable<int> completed)
        {
            SpawnedPlateCount = spawnedPlateCount;
            Buffer = new ReadOnlyCollection<int?>(new List<int?>(buffer));
            Orders = new ReadOnlyCollection<FixtureOrder>(new List<FixtureOrder>(orders));
            Completed = new ReadOnlyCollection<int>(new List<int>(completed));
        }
    }
    internal sealed class CoreItem
    {
        internal int Id, PlateId, SourceIndex, Slot = -1;
        internal string Kind, Location = "Pending";
    }
    internal sealed class CoreOrder
    {
        internal string Kind;
        internal readonly List<int> Items = new List<int>();
    }
}
