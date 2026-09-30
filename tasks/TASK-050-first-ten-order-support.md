# First Ten Order Support

Task ID: TASK-050
Status: Review

## Current Change

用户追加授权：与 TASK-049 并发订单结算集成，构建 Windows 试玩包；沿用当前工作区已确认的 24 盘打乱等基线。

正式完成数 0–9 时达到危险阈值必定接管；完成 10 单后使用 20/23/26/29/30% 失败补偿，4 次及以上封顶。热身不计入、不应用前十单保护。沿用既有候选降级，不保证严格可解。

## Work Packages

Work Package ID: WP-050-CODE
Goal: 实现前十单保护、温和失败补偿及历史回放兼容。
Spec References: SPEC 近期支持、连续正式失败补偿。
Must Preserve: 危险阈值、候选和预留、换单、开锅、失败计数及随机流隔离；所有现有未提交修改。
Allowed Write Paths: Runtime/Core/DailyDirector.cs、AdaptiveOrderPity.cs、DailySession.cs、DailySessionFactory.cs；Runtime/Replay/DailyReplay.cs；Tests/Diagnostics~/Task050* 和 Task041AdaptiveOrderPityQa.cs 的当前概率断言；.harness/qa/TASK-050/**。PM 串行更新 SPEC 与本 Task。
Forbidden / Shared Paths: 表现、Bootstrap、云端与其他任务；不修改无关现有诊断。
Depends On: 无。
Acceptance: 0/9/10 边界、失败阶梯和封顶、热身排除、危险阈值、降级、换单和新旧回放；Unity 编译及核心运行检查。
Integrator: 当前执行代理兼任 code_agent，串行实施。

## Result

- Work Package: Completed。
- 已实现正式完成数 0–9 时达到危险阈值 100% 接管；之后 20/23/26/29/30% 封顶。热身维持原策略，失败计数与通关清零不变。
- 新局快照和回放记录 orderSupportVersion=2；缺少该字段的旧回放保留原概率与随机流，失败次数冻结支持 4 次封顶。
- 核心编译与运行检查 110,789 项通过，覆盖概率边界/分布、真实会话跨第10单、热身排除、候选降级、阈值、换单、结果幂等及新旧阶段回放。证据 `.harness/qa/TASK-050/core-checks.log`。
- Unity 编译通过、Boot 与七布局热身/正式入口运行检查 43 项通过。运行检查完成后退出编辑器时出现一次 MissingReferenceException，栈位于未修改的 GameplayView.OnDestroy → PlatePresentationWorld.SetSimulating；未认定零运行错误，未扩大到表现层修复。证据 `.harness/qa/TASK-050/unity-runtime.log` 和 `runtime/integration-result.json`。
- Git diff 范围核对完成，保留原有未提交改动；未 Commit、Push、上传或发布。用户体验验收待定。

- 2026-09-28 用户授权 049+050 集成试玩：当前工作区两项改动共同构建，保留24盘打乱等基线；并发结算复查通过，Windows Build Succeeded（0错误/0警告），Direct3D Player启动无脚本异常。当前测试环境音频设备初始化失败，未验证声音。证据 `.harness/qa/TASK-050/integrated-with-049/`；试玩包 `Builds/HotpotSort-T049-T050-20260928.zip`。未上传微信、提交或发布，保持 Review。
