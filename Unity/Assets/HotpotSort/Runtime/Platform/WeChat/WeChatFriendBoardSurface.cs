using System;
using System.Globalization;
using HotpotSort.Contracts;
using UnityEngine;

namespace HotpotSort.Platform
{
    // Injectable only for diagnostics. A transport never returns relation-chain data.
    public interface IWeChatFriendBoardTransport
    {
        void PostMessage(string json);
        void Show(Texture texture, FriendBoardViewport viewport);
        void Hide();
    }

    public sealed class WeChatFriendBoardSurface : IWeChatFriendBoardSurface
    {
        public const string ScoreKey = "hotpot_first_wins_v1";
        public const int ProtocolVersion = 1;
        public Texture2D SharedTexture { get; private set; }
        // WX SDK maps its shared texture upside down. Apply this UV rectangle only when drawing it.
        public static Rect SharedTextureUv => new Rect(0, 1, 1, -1);
        private readonly IWeChatFriendBoardTransport transport;
        private FriendBoardViewport viewport;
        // The open-data domain outlives a disposed surface; replacing an adapter must not reset IDs.
        private static int lastEpoch, lastRequestId;
        private int epoch;
        private bool opened, disposed;

        public WeChatFriendBoardSurface(IWeChatFriendBoardTransport transport = null)
        { this.transport = transport ?? new NativeTransport(); epoch=lastEpoch; }

        public void Open(FriendBoardViewport value)
        {
            ThrowIfDisposed(); Validate(value);
            if (opened) { UpdateViewport(value); return; }
            epoch=lastEpoch=checked(lastEpoch+1); viewport=value;
            SharedTexture = new Texture2D(1,1,TextureFormat.RGBA32,false) { name="HotpotFriendSharedCanvas" };
            try
            {
                transport.Show(SharedTexture, viewport);
                Send("open", ViewportJson());
                opened=true;
            }
            catch { try { transport.Hide(); } finally { ReleaseTexture(); } throw; }
        }
        public void Refresh()
        { ThrowIfDisposed(); if (opened) Send("refresh", ViewportJson()); }
        public void UpdateViewport(FriendBoardViewport value)
        {
            ThrowIfDisposed(); Validate(value); viewport=value;
            if (!opened) return;
            transport.Show(SharedTexture, value);
            Send("refresh", ViewportJson());
        }
        public void Close()
        {
            if (disposed || !opened) return;
            opened=false;
            try { Send("close", "{}"); }
            finally { try { transport.Hide(); } finally { ReleaseTexture(); } }
        }
        public void PublishOwnScore(int firstWins, string ownerMarker, DateTimeOffset updatedAtUtc)
        {
            ThrowIfDisposed();
            if (firstWins < 0) throw new ArgumentOutOfRangeException(nameof(firstWins));
            if (string.IsNullOrEmpty(ownerMarker) || ownerMarker.Length > 128) throw new ArgumentException("Opaque owner marker required", nameof(ownerMarker));
            foreach (char c in ownerMarker)
                if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9') && c != '_' && c != '-')
                    throw new ArgumentException("Invalid opaque owner marker", nameof(ownerMarker));
            Send("publishScore", "{\"firstWins\":"+firstWins.ToString(CultureInfo.InvariantCulture)+
                ",\"ownerMarker\":\""+ownerMarker+"\",\"updatedAtUtc\":\""+
                updatedAtUtc.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'",CultureInfo.InvariantCulture)+"\"}");
        }
        public void Dispose() { if (disposed) return; try { Close(); } finally { disposed=true; } }
        private void Send(string type,string payload)
        {
            int requestId=lastRequestId=checked(lastRequestId+1);
            transport.PostMessage("{\"version\":1,\"type\":\""+type+"\",\"viewEpoch\":"+
                epoch.ToString(CultureInfo.InvariantCulture)+",\"requestId\":"+requestId.ToString(CultureInfo.InvariantCulture)+",\"payload\":"+payload+"}");
        }
        private string ViewportJson() => "{\"x\":"+viewport.X+",\"y\":"+viewport.Y+",\"width\":"+viewport.Width+
            ",\"height\":"+viewport.Height+",\"dpr\":"+viewport.DevicePixelRatio.ToString("R",CultureInfo.InvariantCulture)+"}";
        private static void Validate(FriendBoardViewport v)
        { _ = new FriendBoardViewport(v.X,v.Y,v.Width,v.Height,v.DevicePixelRatio); }
        private void ThrowIfDisposed() { if (disposed) throw new ObjectDisposedException(nameof(WeChatFriendBoardSurface)); }
        private void ReleaseTexture() { if (SharedTexture != null) UnityEngine.Object.Destroy(SharedTexture); SharedTexture=null; }

        private sealed class NativeTransport : IWeChatFriendBoardTransport
        {
            public void PostMessage(string json)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                WeChatWASM.WX.GetOpenDataContext().PostMessage(json);
#else
                throw new PlatformNotSupportedException("Real friend board requires WeChat");
#endif
            }
            public void Show(Texture texture,FriendBoardViewport v)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                WeChatWASM.WX.GetOpenDataContext();
                WeChatWASM.WX.ShowOpenData(texture,v.X,v.Y,v.Width,v.Height);
#else
                throw new PlatformNotSupportedException("Real friend board requires WeChat");
#endif
            }
            public void Hide()
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                WeChatWASM.WX.HideOpenData();
#endif
            }
        }
    }
}
