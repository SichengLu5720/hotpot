using System;
using System.Threading.Tasks;
namespace HotpotSort.Contracts
{
    [Serializable] public sealed class SwapOrderItem
    { public string itemId,ingredientId; public int sourceIndex,bufferIndex; }
    [Serializable] public sealed class SwapOrderTransfer
    { public string sessionId,token,oldIngredientId,newIngredientId; public int slot; public SwapOrderItem[] items=Array.Empty<SwapOrderItem>(); }
    [Serializable] public sealed class SwapOrderOffer
    {
        public string sessionId,snapshotRevision;
        public bool selecting,busy;
        public int[] legalSlots=Array.Empty<int>();
        public SwapOrderTransfer transfer;
    }
    // Selection and transfer lock gameplay inputs only. They are not pause reasons.
    public interface ISwapOrderActions
    {
        SwapOrderOffer ReadSwapOrderOffer();
        bool BeginSwapOrderSelection();
        void CancelSwapOrderSelection();
        Task<RewardApplicationResult> SelectSwapOrderAsync(int slot,RewardRoute route);
        bool CompleteSwapOrderTransfer(string sessionId,string token);
    }
}
