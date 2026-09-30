using HotpotSort.Contracts;
namespace HotpotSort.Core
{
    public sealed partial class DailySession
    {
        readonly int toolLimitVersion,initialToolUseMask;
        int usedToolMask;
        public int UsedToolMask { get { lock(gate)return usedToolMask; } }
        static int ToolBit(RewardKind kind)=>kind==RewardKind.SwapOrder?1:kind==RewardKind.ClearBuffer?2:kind==RewardKind.Shuffle?4:0;
        // Only answers the round allowance; target and session eligibility remain in each command.
        public bool CanUseTool(RewardKind kind) { lock(gate)return toolLimitVersion==0||(usedToolMask&ToolBit(kind))==0; }
        void MarkToolUsed(RewardKind kind) { if(toolLimitVersion>0)usedToolMask|=ToolBit(kind); }
    }
}
