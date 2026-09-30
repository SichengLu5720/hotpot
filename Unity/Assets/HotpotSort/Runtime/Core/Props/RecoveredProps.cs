using System;
using System.Collections.Generic;
using System.Linq;

namespace HotpotSort.Core
{
    public enum RecoveredPropId { Tip = 20001, Clear = 20002, Refresh = 20004 }

    public sealed class PropFish { public int Kind; public bool IsMoveToTemp; }
    public sealed class PropBubble
    {
        public readonly List<int> Fish = new List<int>();
        public readonly List<PropFish> Slots = new List<PropFish>();
        public bool IsFrozen, IsUnknown, IsKey, IsObstacle, IsMore;
        public int LockCount, MaskTargetFish, Lock2Count, Obstacle2Count;
        public readonly List<int> UnknownFish = new List<int>();
        public readonly List<int> BindFish = new List<int>();
        public readonly List<int> IceFish = new List<int>();
        public readonly List<int> KeyFish = new List<int>();
        public readonly List<int> LockFish = new List<int>();
    }
    public sealed class PropTarget
    {
        public int Kind, Need;
        public bool Unlocked, Full, FlagC0, FlagC1;
    }
    public sealed class PropState
    {
        public int NativeGameState = 1;
        public bool TipBusy, RefreshBusy;
        public readonly List<PropBubble> Active = new List<PropBubble>();
        public readonly List<PropBubble> Pending = new List<PropBubble>();
        public readonly List<PropFish> Conveyor = new List<PropFish>();
        public readonly List<PropFish> Temp = new List<PropFish>();
        public readonly Dictionary<int, int> Outside = new Dictionary<int, int>();
        public readonly List<PropTarget> Targets = new List<PropTarget>();
    }

    // Recovered semantic rules. Unity animation, pooling and UI remain adapter responsibilities.
    public static class RecoveredProps
    {
        public static int UnlockLevel(RecoveredPropId id) { return id == RecoveredPropId.Tip ? 3 : id == RecoveredPropId.Clear ? 5 : 7; }
        static bool Ready(PropState s) { return s.NativeGameState == 1 && !s.TipBusy && !s.RefreshBusy; }
        static bool PendingEligible(PropBubble b) { return !b.IsFrozen && !b.IsUnknown && b.LockCount <= 0 && !b.IsKey && !b.IsObstacle && !b.IsMore && b.MaskTargetFish <= 0 && b.Lock2Count <= 0 && b.Obstacle2Count <= 0; }
        static bool CanCollect(PropBubble b) { return !b.IsFrozen && !b.IsUnknown && b.LockCount <= 0 && !b.IsObstacle && b.MaskTargetFish <= 0 && b.Lock2Count <= 0 && b.Obstacle2Count <= 0; }
        static bool SlotEligible(PropBubble b, int i) { return !(i < b.UnknownFish.Count && b.UnknownFish[i] != 0) && !(i < b.KeyFish.Count && b.KeyFish[i] != 0) && !(i < b.BindFish.Count && b.BindFish[i] != 0) && !(i < b.IceFish.Count && b.IceFish[i] > 0) && !(i < b.LockFish.Count && b.LockFish[i] > 0); }

        public static int ClearTemp(PropState s, Action<PropFish> moveOutside)
        {
            if (!Ready(s) || !s.Temp.Any(f => f != null && f.IsMoveToTemp)) return 0;
            int moved = 0;
            for (int i = 0; i < s.Temp.Count; i++)
            {
                var fish = s.Temp[i]; if (fish == null || !fish.IsMoveToTemp) continue;
                int count; s.Outside.TryGetValue(fish.Kind, out count); s.Outside[fish.Kind] = count + 1;
                if (moveOutside != null) moveOutside(fish); s.Temp[i] = null; moved++;
            }
            return moved;
        }

        public static List<PropFish> Gather(PropState s, int kind, int need)
        {
            var selected = new List<PropFish>();
            foreach (var fish in s.Conveyor) if (need > 0 && fish != null && fish.Kind == kind) { selected.Add(fish); need--; }
            foreach (var bubble in s.Active) if (CanCollect(bubble)) for (int i = 0; i < bubble.Slots.Count && need > 0; i++)
                if (bubble.Slots[i] != null && bubble.Slots[i].Kind == kind && SlotEligible(bubble, i)) { selected.Add(bubble.Slots[i]); need--; }
            var reserved = new List<Tuple<int, int>>();
            for (int bi = 0; bi < s.Pending.Count && need > 0; bi++) { var b = s.Pending[bi]; if (!PendingEligible(b)) continue; for (int fi = 0; fi < b.Fish.Count && need > 0; fi++) if (b.Fish[fi] == kind && SlotEligible(b, fi)) { reserved.Add(Tuple.Create(bi, fi)); need--; } }
            if (need > 0) return null;
            for (int i = reserved.Count - 1; i >= 0; i--) { var r = reserved[i]; var b = s.Pending[r.Item1]; selected.Add(new PropFish { Kind = b.Fish[r.Item2] }); b.Fish.RemoveAt(r.Item2); if (b.Fish.Count == 0) s.Pending.RemoveAt(r.Item1); }
            return selected;
        }

        public static bool Tip(PropState s, Action<PropTarget, List<PropFish>, Action> animateAndMove)
        {
            if (!Ready(s) || s.Active.Count == 0) return false; s.TipBusy = true; s.NativeGameState = 2;
            foreach (var target in s.Targets) { if (!target.Unlocked || target.Full || target.FlagC0 || target.FlagC1 || target.Kind == 0) continue; var fish = Gather(s, target.Kind, target.Need); if (fish == null) continue; animateAndMove(target, fish, () => { s.TipBusy = false; s.NativeGameState = 1; }); return true; }
            s.TipBusy = false; s.NativeGameState = 1; return false;
        }

        public static bool Refresh(PropState s, Func<int, int, int> range)
        {
            if (!Ready(s) || s.Active.Count == 0) return false;
            var fish = new List<PropFish>(); var kinds = new List<int>();
            foreach (var b in s.Active) for (int i = 0; i < b.Slots.Count; i++) { var f = b.Slots[i]; if (f != null && f.Kind != 10000 && f.Kind != 10001 && SlotEligible(b, i)) { fish.Add(f); kinds.Add(f.Kind); } }
            if (fish.Count == 0) return false; s.RefreshBusy = true; s.NativeGameState = 2;
            for (int n = kinds.Count; n >= 2; n--) { int j = range(0, n); var t = kinds[n - 1]; kinds[n - 1] = kinds[j]; kinds[j] = t; }
            var target = s.Targets.FirstOrDefault(t => t.Unlocked && !t.Full && kinds.Count(k => k == t.Kind) < t.Need);
            if (target != null) { int quota = target.Need; foreach (var b in s.Pending) { if (quota < 1 || !PendingEligible(b)) break; for (int i = 0; i < b.Fish.Count && quota > 0; i++) if (b.Fish[i] == target.Kind && SlotEligible(b, i)) { int j = kinds.FindIndex(k => k != target.Kind); if (j < 0) continue; var t = b.Fish[i]; b.Fish[i] = kinds[j]; kinds[j] = t; quota--; } } }
            for (int i = 0; i < fish.Count; i++) fish[i].Kind = kinds[i]; s.RefreshBusy = false; s.NativeGameState = 1; return true;
        }
    }
}
