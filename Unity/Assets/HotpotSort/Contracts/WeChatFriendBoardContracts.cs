using System;

namespace HotpotSort.Contracts
{
    // Physical screen pixels, top-left origin. Caller clips this rectangle to the safe area.
    public readonly struct FriendBoardViewport
    {
        public readonly int X, Y, Width, Height;
        public readonly double DevicePixelRatio;
        public FriendBoardViewport(int x, int y, int width, int height, double devicePixelRatio)
        {
            if (x < 0 || y < 0 || x > 16384 || y > 16384 || width < 120 || height < 160 ||
                width > 4096 || height > 8192 || (long)width * height > 16777216 ||
                double.IsNaN(devicePixelRatio) || double.IsInfinity(devicePixelRatio) ||
                devicePixelRatio < .5 || devicePixelRatio > 8)
                throw new ArgumentOutOfRangeException(nameof(width), "Invalid friend board viewport");
            X=x; Y=y; Width=width; Height=height; DevicePixelRatio=devicePixelRatio;
        }
    }

    // No friend records or identity data cross back into the main domain.
    public interface IWeChatFriendBoardSurface : IDisposable
    {
        void Open(FriendBoardViewport viewport);
        void Refresh();
        void Close();
        void UpdateViewport(FriendBoardViewport viewport);
        void PublishOwnScore(int firstWins, string ownerMarker, DateTimeOffset updatedAtUtc);
    }
}
