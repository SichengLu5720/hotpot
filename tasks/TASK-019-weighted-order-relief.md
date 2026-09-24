# Weighted Order Relief

Task ID: TASK-019
Status: Review

## Current Change

- 用户确认后续补单80/20保护及无候选时原算法兜底。
- 其他有效开启锅都无可接收的当前可点击/暂存食材才触发；80%选当前可点击+暂存能凑3份的合法候选，20%原算法。预留其他订单需求，保持避免重复规则；保护池内等权。
- 缺观察或无保护候选不推测，沿用原算法；开局不变；一个同步补单连锁内剔除已消费食材。
- 保持每日随机可复现/回放，观察输入随命令记录；不引入每帧全盘Alpha扫描。
- 本轮仅补单，压缩上方留白仍为待确认布局方案，不在代码包范围。

## Work Packages

- Work Package ID: ORDER-RELIEF
- Goal: 接通真实可点击观察、实现权重规则、必要验证并更新预览。
- Spec References: docs/SPEC.md 后续补单80/20解围权重
- Must Preserve: 开局、供给、锅位解锁、库存守恒、暂存5格、物理、视觉、按压1.5及其他用户修改。
- Allowed Write Paths: Unity/Assets/HotpotSort/Runtime/Core/**; Unity/Assets/HotpotSort/Contracts/**; Unity/Assets/HotpotSort/Runtime/Presentation/PresentationPort.cs; Unity/Assets/HotpotSort/Runtime/Presentation/GameplayView.cs; Unity/Assets/HotpotSort/Runtime/Bootstrap/DailyProductionComposition.cs; Unity/Assets/HotpotSort/Runtime/Bootstrap/Editor/*OrderRelief*; Unity/Assets/HotpotSort/Runtime/Presentation/Diagnostics~/*OrderRelief*; .harness/qa/TASK-019/**; .harness/qa/TASK-015/export-r015/**（暂存及新包）
- Forbidden / Shared Paths: 无关源码/资产/配置；SPEC和Task由PM修改。新增接口可在所属已授权目录，外部消费者修改需协调。
- Additional Allowed Write Path: Unity/Assets/HotpotSort/Runtime/Replay/DailyReplay.cs，用于记录观察输入的回放恢复，旧记录无字段保持兼容。
- Depends On: None
- Acceptance: 有可推进锅不触发，隐藏食材不误计，80/20分支与原算法兜底，预留/去重/守恒，过期观察拒绝/会话隔离、连锁与回放一致，实际点击链路，Unity编译；导出预览。
- Integrator: code_agent

## Result

- Completed：真实可点击观察接入操作，补单80/20保护、合法候选预留去重、缺观察/空池原算法回退、观察输入回放实现。
- .NET5064项通过；5000种子4015保护/985原算法。覆盖隐藏食材、部分订单可推进、暂存可推进、连锁消费排除、过期/跨会话及回放。
- Unity正式Boot入口9项通过：真实可点击捕获、裁剪外排除、按压收集、观察记录与生产回放一致。首次诊断编译缺少Replay引用，改测试反射后复跑通过；一次工程被其他Unity检查占用，待其退出后复跑通过。
- 证据 .harness/qa/TASK-019/core-results.json、production-r2.log。与TASK020动画修复整合导出中。
- 观察在操作时捕获；同次同步连锁中新露出食材保守等下一次观察，已消费ID立即排除。每操作Alpha扫描的真机耗时及实际通关率尚未测。
- 暂存5格、开局、供给、物理及现有视觉保持；未修改顶部留白，未上传发布。
- TASK019+TASK020整合导出成功并打开开发者工具：TASK-002-v9-export-20260924T085628-4252a698e8d04aa3b16cf739023669ac。仅集成本包相关改动，保留并行入口图标/震动源码但未带入预览。
