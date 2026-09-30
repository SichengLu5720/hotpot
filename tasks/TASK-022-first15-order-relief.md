# First 15 Order Relief

Task ID: TASK-022
Status: Review

## Current Change

- 用户要求先完成点击优化，再实行第二方案：前15单至少保留一口可完成的推进锅。
- completedOrders<15时补单，其他有效锅都不能由当前可点击+暂存补齐其剩余需求，且存在合法三份候选，则直接保护池等权选单。沿用预留/去重。
- 缺可靠观察/空保护池原算法兜底；不改供给或补造食材。completedOrders>=15后新补单使用现有80/20；已发订单不变。开局不变。
- 第一阶段优化与第二阶段规则分别验证，最后导出一次整合预览。

## Work Packages

- Work Package ID: FIRST15-RELIEF
- Goal: 在已优化可点击观察上增加前15单推进保护。
- Spec References: docs/SPEC.md 前15单推进保护
- Must Preserve: 库存/供给/两单开局/锅位解锁/5暂存/物理/动画/点击优化/回放/后续80/20。
- Allowed Write Paths: Unity/Assets/HotpotSort/Runtime/Core/DailyDirector.cs; Unity/Assets/HotpotSort/Runtime/Core/DailySession.cs; Unity/Assets/HotpotSort/Runtime/Presentation/Diagnostics~/*OrderRelief*; Unity/Assets/HotpotSort/Runtime/Bootstrap/Editor/*OrderRelief*; .harness/qa/TASK-022/**; .harness/qa/TASK-015/export-r015/**（当前相关补丁暂存及新包）
- Forbidden / Shared Paths: 其他源码/资产/配置，SPEC和Task仅PM更新。
- Depends On: TASK-021点击延迟优化验证通过。
- Acceptance: 前期有一份可推进但不够完成仍保护；能补齐剩余则原规则；0/14/15完成数边界；预留/缺候选/未知观察；80/20后期回归；回放与守恒。
- Integrator: code_agent

## Result

- 规则已实现。Core 5297项通过：0/14完成数、剩余1/2/3份、未知观察、同事务14→15切换及回放；后期5000种子4015保护/985原算法。
- 整合Boot 11项通过；观察均值0.1622ms，普通释放全链14.0586ms，补单命令全链15.8779ms，释放兜底0次。证据 .harness/qa/TASK-022/results.json。
- 最终核心5300项通过；补充策略版本区分旧观察回放，旧观察fixture通过。最终微信导出成功：TASK-002-v9-export-20260924T092623-0944cc8f861f41d1ae775d383a69d4c9/minigame。证据 .harness/qa/TASK-022/final-export.log。
- 保留原资源、物理及按压1.5，未混入并行音频/入口改动。手机手感与用户接受待确认；后续保护池加权由TASK023接续。
