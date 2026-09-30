# Buffer Weighted Relief

Task ID: TASK-024
Status: Review

## Current Change

- 用户确认保留推进保护，保护池复用原作暂存分类权重，不完整回退原算法。
- 保护池按扣除预留后 B>0 / B=0 分组，用当前原表 Order1:Order2 抽组、组内等权；单组直接选、有效权重全零整个池等权。
- 不变更触发、合法性、预留、去重、供给、物理、动画、点击缓存与历史回放。

## Work Packages

- Work Package ID: BUFFER-RELIEF
- Goal: 替换保护池选种方式并验证，交给TASK023唯一共享构建责任人集成。
- Spec References: docs/SPEC.md 保护池暂存分类权重。
- Must Preserve: 非保护算法及RNG路径、前15/后期80-20触发、库存、旧策略回放、并行用户修改。
- Allowed Write Paths: Unity/Assets/HotpotSort/Runtime/Core/DailyDirector.cs; Unity/Assets/HotpotSort/Runtime/Core/DailyCommands.cs; Unity/Assets/HotpotSort/Runtime/Core/DailySession.cs; Unity/Assets/HotpotSort/Runtime/Replay/DailyReplay.cs; Unity/Assets/HotpotSort/Runtime/Presentation/Diagnostics~/*OrderRelief*; Unity/Assets/HotpotSort/Runtime/Bootstrap/Editor/*OrderRelief*; .harness/qa/TASK-024/**。
- Forbidden / Shared Paths: 其他代码、资产与配置，SPEC/Task由PM负责。
- Depends On: TASK-021/022实现。
- Acceptance: 两组权重分布；单组/零权重/空池/未知/预留；0/14/15边界与80-20；策略1/2旧回放和新策略回放；Boot及导出。
- Integrator: code_agent。

## Result

- 源码已完成：DailyDirector、DailyCommands、DailySession、DailyReplay和OrderReliefQa；新观察策略3，旧策略1/2兼容。
- 核心15631项和真实Boot11项通过。80:20配置10000种子暂存组8054/盘面1946；暂存1份/2份种类4065/3989。覆盖预留、空组、双零权重、未知观察、14/15及三代回放。
- 证据 .harness/qa/TASK-024/results.json、production.log。未真机验证。
- 并行TASK023唯一Integrator已接收本次修改，负责同步源码、统一导出和其已授权的开发版交付。本任务未写共享stage，未提交、上传或发布；此任务改号TASK024避免编号冲突。
