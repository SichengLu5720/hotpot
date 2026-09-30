// SDK/engine boundary fake only. Contracts and production WebGL adapter sources are real.
using System;
namespace UnityEngine
{
    public class Object
    {
        public static int Destroyed;
        public static void DontDestroyOnLoad(Object value){}
        public static void Destroy(Object value){Destroyed++;}
    }
    public class MonoBehaviour:Object{public GameObject gameObject;}
    public class GameObject:Object
    {
        public GameObject(string name){}
        public T AddComponent<T>() where T:MonoBehaviour,new()=>new T{gameObject=this};
    }
    public static class Time{public static int frameCount;public static double realtimeSinceStartupAsDouble;}
    public static class JsonUtility{public static void FromJsonOverwrite(string json,object target)=>throw new NotSupportedException();}
}
namespace WeChatWASM
{
    public class GeneralCallbackResult{}
    public class OnShowListenerResult{}
    public class WXADErrorResponse{}
    public class WXTextResponse{}
    public class WXRewardedVideoAdOnCloseResponse{public bool isEnded;}
    public class ShareAppMessageOption{public string title,imageUrl;}
    public class WXShareAppMessageParam{public string title,imageUrl;}
    public class ShowShareMenuOption{public string[] menus;public bool? withShareTicket;}
    public class WXCreateRewardedVideoAdParam{public string adUnitId;public bool multiton;}
    public class FileSystem{public string ReadFileSync(string path,string encoding)=>throw new NotSupportedException();}
    public static class WX
    {
        public static event Action<GeneralCallbackResult> Hide;
        public static event Action<OnShowListenerResult> Show;
        public static int Menus,ShareCalls,Creates;
        public static bool ThrowOffHide;
        public static ShowShareMenuOption Menu;
        public static WXShareAppMessageParam ShareTemplate;
        public static ShareAppMessageOption LastShare;
        public static WXRewardedVideoAd LastVideo;
        public static int Listeners=>(Hide?.GetInvocationList().Length??0)+(Show?.GetInvocationList().Length??0);
        public static FileSystem GetFileSystemManager()=>new FileSystem();
        public static void OnHide(Action<GeneralCallbackResult> action)=>Hide+=action;
        public static void OffHide(Action<GeneralCallbackResult> action){if(ThrowOffHide)throw new Exception("SDK cleanup injection");Hide-=action;}
        public static void OnShow(Action<OnShowListenerResult> action)=>Show+=action;
        public static void OffShow(Action<OnShowListenerResult> action)=>Show-=action;
        public static void HideNow()=>Hide?.Invoke(new GeneralCallbackResult());
        public static void ShowNow()=>Show?.Invoke(new OnShowListenerResult());
        public static void ShareAppMessage(ShareAppMessageOption option){LastShare=option;ShareCalls++;}
        public static void OnShareAppMessage(WXShareAppMessageParam option)=>ShareTemplate=option;
        public static void ShowShareMenu(ShowShareMenuOption option){Menu=option;Menus++;}
        public static WXRewardedVideoAd CreateRewardedVideoAd(WXCreateRewardedVideoAdParam option){Creates++;return LastVideo=new WXRewardedVideoAd();}
    }
    public class WXRewardedVideoAd
    {
        public Action<WXADErrorResponse> Error;
        public Action<WXRewardedVideoAdOnCloseResponse> Close;
        public Action<WXTextResponse> Loaded,ShowFailed;
        public int Destroyed,Shows;
        public void OnError(Action<WXADErrorResponse> a)=>Error+=a;
        public void OffError(Action<WXADErrorResponse> a)=>Error-=a;
        public void OnClose(Action<WXRewardedVideoAdOnCloseResponse> a)=>Close+=a;
        public void OffClose(Action<WXRewardedVideoAdOnCloseResponse> a)=>Close-=a;
        public void Load(Action<WXTextResponse> loaded,Action<WXADErrorResponse> failed){Loaded=loaded;}
        public void Show(Action<WXTextResponse> success,Action<WXTextResponse> failed){Shows++;ShowFailed=failed;}
        public void Destroy()=>Destroyed++;
    }
}
