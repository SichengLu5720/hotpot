# TASK-003 implementation notes
Diagnostic evidence: compile-01 failed on missing official assetbundle module; manifest now explicitly supplies 1.0.0. compile-02 completed editor compilation. compile-03 exposed a diagnostic-only collection API typo, fixed from Length to Count. compile-04-player completed WebGL player script compilation with seven assemblies including all four HotpotSort assemblies and exit code 0. No gameplay QA or complete WebGL export was executed. Unity-generated settings may contain serializer trailing whitespace.
Unity 6000.0.26f1; official WXSDK exact upstream and compatibility patch in Unity/Packages/WXSDK_COMPATIBILITY.md.
No upload/review/publication is performed by this task.

Boot deliberately has no ProductionComposition until fixed real A/B factories are integrated. It reports production-core-view-factories-missing. Concrete integration must provide content/config/build identity, a per-workspace RuntimeNamespace, real factories, diagnostic binding and session-observation binding. The controller exposes activity time to the view integration without rewriting A's canonical snapshot. No development doubles are included.

Desktop runtime paths default to this worktree's build/wechat/runtime/<namespace>; HOTPOT_RUNTIME_ROOT can override the parent. WeChat uses WX.env.USER_DATA_PATH/<namespace>. LocalDiagnostics actually uses data/cache roots; it returns success only after local write success. No remote upload exists. Build export uses this worktree's build/wechat/export-<unique identity>. SDK-owned cache policy remains upstream.

Trusted UTC is injectable; absent trusted provider records device fallback. A live session freezes its date, configuration and content version. Exit then StartToday re-resolves; Retry creates new core/view instances and disposes the old ones. The initial K0 has no input/tap or historical replay execution port, so those remain A/B integration responsibilities and are not simulated by C.

Build entry: HotpotSort.Build.WeChatBuild.Export, WECHAT_APP_ID environment variable required. Production definitions HOTPOT_DEVELOPMENT, TUANJIE*, UNITY_INSTANTGAME are rejected. AppID is temporarily passed to official ProjectConf during export and restored afterward; generated export contains the platform-required AppID and is local output, not source. No export was executed by writing this entry.
