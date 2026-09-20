using System;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Session;
using UnityEngine;
#if UNITY_WEBGL || UNITY_EDITOR
using WeChatWASM;
#endif
namespace HotpotSort.Platform
{
    public sealed class UnityClock : IMonotonicClock { public double Seconds => Time.realtimeSinceStartupAsDouble; }
    public sealed class WeChatPlatform : IPlatformLifecycleAdapter
    {
        public event Action<PlatformLifecycle> Changed;
        public event Action<Viewport> ViewportChanged;
        private bool started, disposed;
#if UNITY_WEBGL || UNITY_EDITOR
        private readonly Action<GeneralCallbackResult> hide;
        private readonly Action<OnShowListenerResult> show;
        public WeChatPlatform() { hide = _ => Changed?.Invoke(PlatformLifecycle.Background); show = _ => { Changed?.Invoke(PlatformLifecycle.Foreground); RefreshViewport(); }; }
#else
        public WeChatPlatform() { }
#endif
        public static Task InitializeAsync()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var completion = new TaskCompletionSource<bool>();
            WX.InitSDK(code => { if (code == 0) completion.TrySetResult(true); else completion.TrySetException(new InvalidOperationException("wx-init-failed:" + code)); });
            return completion.Task;
#else
            return Task.CompletedTask;
#endif
        }
        public void Start()
        {
            if (disposed) throw new ObjectDisposedException(nameof(WeChatPlatform));
            if (started) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            WX.OnHide(hide);
            try { WX.OnShow(show); } catch { WX.OffHide(hide); throw; }
#endif
            started = true; RefreshViewport();
        }
        public void NotifyEditorPause(bool paused)
        {
#if UNITY_EDITOR || !UNITY_WEBGL
            if (started && !disposed) Changed?.Invoke(paused ? PlatformLifecycle.Background : PlatformLifecycle.Foreground);
#endif
        }
        public void RefreshViewport()
        {
            if (disposed) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            var info = WX.GetSystemInfoSync();
            var safe = info.safeArea;
            ViewportChanged?.Invoke(FromTopLeft(Screen.width, Screen.height, info.windowWidth, info.windowHeight,
                safe.left, safe.top, safe.width, safe.height));
#else
            var safe = Screen.safeArea;
            ViewportChanged?.Invoke(new Viewport(Screen.width, Screen.height, safe.x, safe.y, safe.width, safe.height));
#endif
        }
        public static Viewport FromTopLeft(int pixelsWide, int pixelsHigh, double windowWidth, double windowHeight,
            double left, double top, double width, double height)
        {
            if (windowWidth <= 0 || windowHeight <= 0) throw new ArgumentOutOfRangeException("viewport");
            double sx = pixelsWide / windowWidth, sy = pixelsHigh / windowHeight;
            return new Viewport(pixelsWide, pixelsHigh, (float)(left*sx), (float)((windowHeight-top-height)*sy), (float)(width*sx), (float)(height*sy));
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (started) { try { WX.OffHide(hide); } finally { WX.OffShow(show); } }
#endif
            started = false;
        }
    }
}
