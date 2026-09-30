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
        public double DevicePixelRatio { get; private set; }=1;
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
            WX.InitSDK(code => CompleteInitialization(completion, code));
            return completion.Task;
#else
            return Task.CompletedTask;
#endif
        }
        private static void CompleteInitialization(TaskCompletionSource<bool> completion, int code)
        {
            // Vendored unity-sdk sends Inited 200; retain the previously supported 0.
            // Do not treat arbitrary HTTP-like 2xx or nonnegative errors as success.
            if (code == 0 || code == 200) completion.TrySetResult(true);
            else completion.TrySetException(new InvalidOperationException("wx-init-failed:" + code));
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
            DevicePixelRatio=info.windowWidth>0?Math.Max(.5,Math.Min(8,Screen.width/info.windowWidth)):1;
            Viewport next;
            try
            {
                var menu=WX.GetMenuButtonBoundingClientRect();
                next=FromTopLeft(Screen.width,Screen.height,info.windowWidth,info.windowHeight,
                    safe.left,safe.top,safe.width,safe.height,
                    menu.left,menu.top,menu.width,menu.height);
            }
            catch
            {
                // Older/exceptional clients retain the normal safe-area layout.
                next=FromTopLeft(Screen.width,Screen.height,info.windowWidth,info.windowHeight,
                    safe.left,safe.top,safe.width,safe.height);
            }
            ViewportChanged?.Invoke(next);
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
        public static Viewport FromTopLeft(int pixelsWide,int pixelsHigh,double windowWidth,double windowHeight,
            double left,double top,double width,double height,
            double menuLeft,double menuTop,double menuWidth,double menuHeight)
        {
            var safe=FromTopLeft(pixelsWide,pixelsHigh,windowWidth,windowHeight,left,top,width,height);
            if(menuWidth<=0||menuHeight<=0)return safe;
            double sx=pixelsWide/windowWidth,sy=pixelsHigh/windowHeight;
            return new Viewport(pixelsWide,pixelsHigh,safe.SafeX,safe.SafeY,safe.SafeWidth,safe.SafeHeight,
                (float)(menuLeft*sx),(float)((windowHeight-menuTop-menuHeight)*sy),(float)(menuWidth*sx),(float)(menuHeight*sy));
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
