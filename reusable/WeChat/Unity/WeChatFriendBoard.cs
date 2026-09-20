using System;
using UnityEngine;

namespace Harness.Reusable.Unity
{
    // Assign pixel coordinates in GUI (top-left) space after your layout/safe-area calculation.
    public sealed class WeChatFriendBoard : MonoBehaviour
    {
        public string projectKey = "my-game";
        public string protocol = "harness-friends-v1";
        public int maximumScore = 20;
        [Serializable] public sealed class Record { public int version = 1, completedCount; public long achievedAtMs; public string marker; }
        [Serializable] private sealed class Message { public string type, command; public Record record; public float width, height; }
        private Record own;
        private Texture2D texture;
        private Rect pixels;
        private bool visible;
        public static bool Available {
            get {
#if UNITY_WEBGL && !UNITY_EDITOR && WECHAT_MINIGAME
                return true;
#else
                return false;
#endif
            }
        }
        private void Awake()
        {
            var key = projectKey + ".friend-score.v1";
            if (PlayerPrefs.HasKey(key)) own = JsonUtility.FromJson<Record>(PlayerPrefs.GetString(key));
            if (own == null) own = new Record { marker = Guid.NewGuid().ToString("N"), achievedAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
            if (own.version != 1 || own.completedCount < 0 || own.completedCount > maximumScore || own.achievedAtMs <= 0 ||
                own.marker == null || !System.Text.RegularExpressions.Regex.IsMatch(own.marker, "^[a-f0-9]{32}$"))
                throw new InvalidOperationException("Invalid saved leaderboard record; explicit migration required.");
            Persist();
        }
        private void Persist() { PlayerPrefs.SetString(projectKey + ".friend-score.v1", JsonUtility.ToJson(own)); PlayerPrefs.Save(); }
        public void RecordBest(int completedCount)
        {
            if (completedCount < 0 || completedCount > maximumScore) throw new ArgumentOutOfRangeException(nameof(completedCount));
            if (completedCount <= own.completedCount) return;
            own.completedCount = completedCount;
            own.achievedAtMs = Math.Max(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), checked(own.achievedAtMs + 1));
            Persist(); Send("sync");
        }
        private void Send(string command)
        {
#if UNITY_WEBGL && !UNITY_EDITOR && WECHAT_MINIGAME
            WeChatWASM.WX.GetOpenDataContext().PostMessage(JsonUtility.ToJson(new Message {
                type = protocol, command = command, record = own, width = pixels.width, height = pixels.height }));
#endif
        }
        public void Open(Rect pixelRect)
        {
            if (!Available) throw new NotSupportedException("Use WeChat device/open-data context for friend records.");
            Close(); pixels = pixelRect;
            if (!texture) texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
#if UNITY_WEBGL && !UNITY_EDITOR && WECHAT_MINIGAME
            WeChatWASM.WX.ShowOpenData(texture, (int)pixels.x, (int)pixels.y, (int)pixels.width, (int)pixels.height);
#endif
            visible = true; Send("layout"); Send("open");
        }
        public void Relayout(Rect pixelRect) { if (visible) Open(pixelRect); }
        public void Close()
        {
            if (!visible) return;
            Send("close");
#if UNITY_WEBGL && !UNITY_EDITOR && WECHAT_MINIGAME
            WeChatWASM.WX.HideOpenData();
#endif
            visible = false;
        }
        private void OnGUI() { if (visible && texture) GUI.DrawTextureWithTexCoords(pixels, texture, new Rect(0, 1, 1, -1)); }
        private void OnApplicationPause(bool paused) { if (!paused && visible) Open(pixels); }
        private void OnDisable() { Close(); }
        private void OnDestroy() { Close(); if (texture) Destroy(texture); }
    }
}
