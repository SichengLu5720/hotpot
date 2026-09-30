using System;

namespace HotpotSort.Core
{
    // Driven exclusively by accumulated active time, never wall time or network time.
    public sealed class ActiveSupplySchedule
    {
        public const long IntervalMilliseconds=300;
        public long NextTick { get; private set; }
        public void Reset()=>NextTick=0;
        public bool TryTake(double activeSeconds,bool running)
        {
            if(!running||double.IsNaN(activeSeconds)||double.IsInfinity(activeSeconds)||activeSeconds<0)return false;
            long tick=(long)Math.Floor(activeSeconds*1000/IntervalMilliseconds);
            if(tick<NextTick)return false;
            // Consume the current tick even if capacity or the pending queue blocks.
            NextTick=tick+1;
            return true;
        }
    }
}
