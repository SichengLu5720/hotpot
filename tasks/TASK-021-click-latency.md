# Click Latency

Task ID: TASK-021
Status: Review

## Current Change

- 用户确认多帧维护可点击候选缓存，补单时少量最终验证；同时解决普通点击和补单点击的同步全盘扫描停顿。
- 80/20规则、真实裁剪/遮挡/Alpha语义、观察回放、原生物理、动画顺序、按压1.5及并行用户修改保持。
- 处理缓存过期、移动/删除/裁剪/新局；后台工作必须有界，不能把整个逐像素检查从点击简单搬进单帧。

## Work Packages

- Work Package ID: CLICK-CACHE
- Goal: 有界多帧可点击缓存与补单验证，测量原点击/补单延迟前后。
- Spec References: docs/SPEC.md 补单可点击观察性能
- Must Preserve: 80/20条件/预留/兜底、库存和回放、核心同步结算、动画和物理及其他用户修改。
- Allowed Write Paths: Unity/Assets/HotpotSort/Runtime/Presentation/GameplayView.cs; Unity/Assets/HotpotSort/Runtime/Presentation/*Clickability*; Unity/Assets/HotpotSort/Runtime/Bootstrap/DailyProductionComposition.cs; Unity/Assets/HotpotSort/Runtime/Core/**; Unity/Assets/HotpotSort/Runtime/Replay/DailyReplay.cs; Unity/Assets/HotpotSort/Runtime/Bootstrap/Editor/*OrderRelief*; Unity/Assets/HotpotSort/Runtime/Presentation/Editor/*ClickLatency*; Unity/Assets/HotpotSort/Runtime/Presentation/Diagnostics~/*OrderRelief*; .harness/qa/TASK-021/**; .harness/qa/TASK-015/export-r015/**（当前相关补丁暂存/新包）
- Forbidden / Shared Paths: 其他源代码/资产/用户并行改动，SPEC/Task PM负责。
- Depends On: 已实现TASK019和TASK020。
- Acceptance: 普通/补单点击无同步全盘像素扫描；每帧预算与候选复核有界；移动/遮挡/删除/裁剪/暂停/会话正确；80/20及回放不退化；前后耗时与保护可用率证据；Unity运行与预览。
- Integrator: code_agent

## Result

- 实施及桌面验证完成，待整合预览和用户体验。普通点击懒采集；补单使用有界缓存与当前见证点复核。保留罕见单食材穷举兜底，不再同步全盘穷举。
- Core 5097 项、真实 Boot 11 项及裁剪/遮挡删除/暂停/会话 generation/窄缝兜底检查通过。证据 .harness/qa/TASK-021/。
- 合成压力场景原观察32.8003ms；优化热态均值0.1761ms、最大0.2921ms，后台最大1.5018ms（1.5ms软预算，最多126保守样本）。真实 Boot 观察均值0.1704ms，普通输入全链13.181ms、补单17.6916ms；无旧版全链基线，不能声称全链同倍率提升。
- 手机性能未测；暂缓单独导出，随TASK022一次整合。
