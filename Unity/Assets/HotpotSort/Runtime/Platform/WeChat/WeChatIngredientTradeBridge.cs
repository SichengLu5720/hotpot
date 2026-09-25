using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace HotpotSort.Platform
{
    // Reuses the existing generic cloud-call native bridge; function name is passed
    // separately. IDs are allocated from the collection service's own range.
    public sealed class WeChatIngredientTradeBridge:IWeChatCloudFunctionBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Callback(int id,int kind,IntPtr json);
        [DllImport("__Internal")] static extern void HotpotProfileFunction_Start(int id,string environment,string functionName,string json,Callback callback);
        [DllImport("__Internal")] static extern void HotpotProfileFunction_Cancel(int id);
        static readonly Callback callback=Receive;
        static readonly Dictionary<int,Action<int,string>> receivers=new Dictionary<int,Action<int,string>>();
        [AOT.MonoPInvokeCallback(typeof(Callback))] static void Receive(int id,int kind,IntPtr json)
        {if(!receivers.TryGetValue(id,out var receive))return;receivers.Remove(id);receive(kind,Marshal.PtrToStringAnsi(json));}
#endif
        public void Start(int id,string environment,string functionName,string json,Action<int,string> completion)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            receivers.Add(id,completion);HotpotProfileFunction_Start(id,environment,functionName,json,callback);
#else
            completion(1,"");
#endif
        }
        public void Cancel(int id)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            receivers.Remove(id);HotpotProfileFunction_Cancel(id);
#endif
        }
    }
}
