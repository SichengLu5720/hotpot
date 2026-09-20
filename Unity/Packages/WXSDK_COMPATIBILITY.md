# Fixed SDK compatibility patch
Upstream: https://github.com/wechat-miniprogram/minigame-tuanjie-transform-sdk
Commit: a09d4b29daa1dd8358b09b5b5639554ab08cfdc2
Embedded package: com.qq.weixin.minigame. Downloaded the exact commit archive without changing shared PackageCache.

Original SHA256:
- Runtime/WxWasmSDKRuntime.asmdef: D74BE1BF532FAD11C02691D2260CBE77D57619D63243FF289EA163D2790CF258
- Editor/WxWasmSDKEditor.asmdef: C66F404BD4BCE9175A5F92A23D48BFC50E07C9F6DCE737DB0411B4898DC115D3

Patch: replace the unresolved Runtime GUID reference with UnityEngine.UI (the actual assembly supplied by Unity 6 UGUI 2.0.0); remove the unresolved Unity.InstantGame.Editor reference from WxEditor. This does not identify the old GUID. UNITY_INSTANTGAME and TUANJIE must remain undefined. AutoStreaming is not supported on this chosen route.

Reproduction: extract the exact commit archive to Packages/com.qq.weixin.minigame, apply the two reference edits above, install com.unity.ugui 2.0.0 and import with Unity 6000.0.26f1. No SDK runtime logic is altered by these two edits. Compiler logs under build/wechat document the actual diagnostic result; this file alone does not prove compilation or device execution.

The project also explicitly enables com.unity.modules.assetbundle 1.0.0 because WXBase uses AssetBundle. Unity editor compilation and WebGL player-script compilation succeeded after these dependency changes; see build/wechat/compile-02.log and compile-04-player.log. This is not an export, device run or gameplay QA result.
