# Immediate Normal Supply

Task ID: TASK-052
Status: Review

## Current Change

取消普通补盘300ms轮询，每帧轻量检查，条件满足即补一盘；每帧最多一盘。用户授权实装并检查运行压力，更新本机试玩入口。

## Work Packages

Work Package ID: WP-052-CODE
Goal: 不解析快照的队首访问、短路高度检查和每帧普通供给。
Spec References: SPEC TASK-052。
Must Preserve: 当前脏工作区、缓存随机落点、高度与空间门槛、暂停后台、打乱、物理、D1。
Allowed Write Paths: Runtime/Core/DailySession.cs; Runtime/Bootstrap/DailyProductionComposition.cs; Runtime/UnityPhysics/PlatePresentationWorld.cs; .harness/qa/TASK-052/**。
Forbidden / Shared Paths: 其他Runtime；SPEC/Task由PM更新。
Depends On: TASK-051。
Acceptance: 条件开放即供给、每帧最多一盘、阻塞路径分配及运行耗时、Windows启动。
Integrator: PM兼任code_agent。

Work Package ID: WP-052-VIS-QA
Goal: 真实运行和受控门槛/性能检查，截图。
Spec References: 同上。
Must Preserve: 所有产品实现和其他修改。
Allowed Write Paths: Editor/TASK001V7/Task052SupplyDiagnostic.cs及meta；.harness/qa/TASK-052/visual/**。
Forbidden / Shared Paths: 所有Runtime、SPEC、Task。
Depends On: WP-052-CODE。
Acceptance: 无300ms等待、同帧限制、阻塞无分配、暂停、空间检查与截图。
Integrator: PM；visual_agent独占Unity诊断后交还。

## Result

已实装普通补盘每帧轻量检查，保留高度与空间门槛、缓存落点和同帧最多一盘。打乱、物理和D1未改。

Unity运行诊断11项通过，两尺寸截图未见明显新增异常。预热后10000次高度阻塞检查1.8513ms、空间阻塞检查18.3138ms，均0B新增托管分配；仅Windows编辑器受控测量，不代表手机或玩家实际消除路径。

Windows构建成功，0错误、1条既有弃用API警告；独立Player启动通过，未见脚本异常。环境无音频输出设备，FMOD使用无声回退。构建前后3个Runtime文件指纹一致，成品DLL包含新供给方法。Git diff --check通过。

试玩入口：`.harness/qa/TASK-052/integrated/Player/HotpotSortTask044.exe`。诊断证据：`.harness/qa/TASK-052/visual/runtime-result.json`；构建结果：`.harness/qa/TASK-052/integrated/player-build-result.json`；启动日志：`.harness/qa/TASK-052/player-smoke.log`。

等待用户体验接受；未打ZIP，未Commit/Push/上传/发布。
