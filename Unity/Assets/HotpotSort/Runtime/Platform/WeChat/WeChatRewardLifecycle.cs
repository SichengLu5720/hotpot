using System;
using System.Collections.Generic;
using HotpotSort.Contracts;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using WeChatWASM;
#endif

namespace HotpotSort.Platform
{
    // Own one instance for the platform lifetime and dispose it after its reward/share services.
    public sealed class WeChatRewardLifecycle : IWeChatRewardRuntime, IDisposable
    {
        public event Action Hidden;
        public event Action Shown;
        public event Action Tick;
        public event Action Stopping;
        readonly Queue<KeyValuePair<int,Action>> deferred=new Queue<KeyValuePair<int,Action>>();
        WeChatRewardPump pump;
        bool disposed;
        public double MonotonicSeconds=>Time.realtimeSinceStartupAsDouble;
        public bool Available {
            get {
#if UNITY_WEBGL && !UNITY_EDITOR
                return !disposed;
#else
                return false;
#endif
            }
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        readonly Action<GeneralCallbackResult> hide;
        readonly Action<OnShowListenerResult> show;
        static bool menuRegistered;
        static NativeVideo nativeOwner;
        NativeVideo ownedVideo;
        string cachedShareSource;
        string cachedShareFile;
        string ResolveShareImage(string packagedImagePath)
        {
            if(cachedShareSource==packagedImagePath && cachedShareFile!=null)return cachedShareFile;
            // Native sharing in DevTools cannot render a package-relative image,
            // though createImage can. Keep the exact bytes in an app-owned file.
            string destination=WX.env.USER_DATA_PATH+"/hotpot-static-share-v1.png";
            string result=WX.GetFileSystemManager().CopyFileSync(packagedImagePath,destination);
            if(result!="copyFile:ok")throw new InvalidOperationException("Share image preparation failed");
            cachedShareSource=packagedImagePath;cachedShareFile=destination;
            return destination;
        }
#endif
        public WeChatRewardLifecycle()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            hide=_=>{if(!disposed)Hidden?.Invoke();}; show=_=>{if(!disposed)Shown?.Invoke();};
            WX.OnHide(hide);
            try {WX.OnShow(show);} catch {WX.OffHide(hide);throw;}
#endif
            var host=new GameObject("WeChat Reward Lifecycle");
            UnityEngine.Object.DontDestroyOnLoad(host);
            pump=host.AddComponent<WeChatRewardPump>();pump.Owner=this;
        }
        public void Post(Action action)
        {
            if(disposed)throw new ObjectDisposedException(nameof(WeChatRewardLifecycle));
            deferred.Enqueue(new KeyValuePair<int,Action>(Time.frameCount+1,action??throw new ArgumentNullException(nameof(action))));
        }
        internal void Advance()
        {
            if(disposed)return;
            // Take the batch before ticking: even synchronously raised callbacks defer one frame.
            int count=deferred.Count;
            Tick?.Invoke();
            for(int i=0;i<count&&!disposed;i++)
            {
                if(deferred.Peek().Key>Time.frameCount)break;
                deferred.Dequeue().Value();
            }
        }
        public void Share(string title,string packagedImagePath)
        {
            if(!Available)throw new InvalidOperationException("WeChat sharing unavailable");
#if UNITY_WEBGL && !UNITY_EDITOR
            WX.ShareAppMessage(new ShareAppMessageOption{title=title,imageUrl=ResolveShareImage(packagedImagePath)});
#endif
        }
        public void RegisterMenu(string title,string packagedImagePath)
        {
            if(!Available)throw new InvalidOperationException("WeChat menu unavailable");
#if UNITY_WEBGL && !UNITY_EDITOR
            if(menuRegistered)return;
            WX.OnShareAppMessage(new WXShareAppMessageParam{title=title,imageUrl=ResolveShareImage(packagedImagePath)});
            WX.ShowShareMenu(new ShowShareMenuOption{menus=new[]{"shareAppMessage"},withShareTicket=false});
            menuRegistered=true;
#endif
        }
        public IWeChatRewardVideo CreateVideo(string adUnitId)
        {
            if(!Available)return null;
#if UNITY_WEBGL && !UNITY_EDITOR
            if(nativeOwner!=null)return null;
            var native=WX.CreateRewardedVideoAd(new WXCreateRewardedVideoAdParam{adUnitId=adUnitId,multiton=false});
            if(native==null)return null;
            try {return ownedVideo=nativeOwner=new NativeVideo(native);}
            catch {native.Destroy();throw;}
#else
            return null;
#endif
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;
            // A failed observer must not strand another service's pending reward task.
            var stopping=Stopping;Stopping=null;
            if(stopping!=null)foreach(Action observer in stopping.GetInvocationList())Cleanup(observer);
#if UNITY_WEBGL && !UNITY_EDITOR
            Cleanup(()=>WX.OffHide(hide));Cleanup(()=>WX.OffShow(show));
            if(ownedVideo!=null){Cleanup(ownedVideo.Dispose);ownedVideo=null;}
#endif
            deferred.Clear();Hidden=null;Shown=null;Tick=null;
            if(pump!=null){pump.Owner=null;UnityEngine.Object.Destroy(pump.gameObject);pump=null;}
        }
        static void Cleanup(Action action)
        {
            try{action();}catch{ /* Continue releasing independent native resources on shutdown. */ }
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        sealed class NativeVideo : IWeChatRewardVideo
        {
            readonly WXRewardedVideoAd native;
            readonly Action<WXADErrorResponse> error;
            readonly Action<WXRewardedVideoAdOnCloseResponse> close;
            bool disposed;
            public event Action Loaded;
            public event Action Failed;
            public event Action<bool?> Closed;
            public NativeVideo(WXRewardedVideoAd native)
            {
                this.native=native;
                error=_=>{if(!disposed)Failed?.Invoke();};
                close=result=>{if(!disposed)Closed?.Invoke(result==null?(bool?)null:result.isEnded);};
                native.OnError(error);
                try{native.OnClose(close);}catch{native.OffError(error);throw;}
            }
            public void Load()=>native.Load(_=>{if(!disposed)Loaded?.Invoke();},error);
            public void Show()=>native.Show(_=>{},_=>{if(!disposed)Failed?.Invoke();});
            public void Dispose()
            {
                if(disposed)return;disposed=true;
                try{native.OffClose(close);}finally
                {
                    try{native.OffError(error);}finally
                    {
                        try{native.Destroy();}finally
                        {
                            if(ReferenceEquals(nativeOwner,this))nativeOwner=null;
                            Loaded=null;Failed=null;Closed=null;
                        }
                    }
                }
            }
        }
#endif
    }
    internal sealed class WeChatRewardPump : MonoBehaviour
    {
        internal WeChatRewardLifecycle Owner;
        void Update()=>Owner?.Advance();
        void OnDestroy()=>Owner?.Dispose();
    }
}
