# First 30 Cumulative Order Relief

Task ID: TASK-032  
Status: Review  
Updated At: 2026-09-25

## Requirement Delta

- Goal: 降低对局中找不到当前订单食材的频率。
- Current Behavior: 两关累计已完成订单不足15单时使用现有等概率推进保护；达到15单后全部补单走Difficulty 1基础算法。
- Target Behavior: 将同一保护机制延长到两关累计已完成订单不足30单；热身6单计入累计，正式挑战约前24单继续受保护。
- Entry / Trigger: 每次生成后续订单时读取本次两关挑战流程的累计完成订单数。
- State Change: 累计0–29单使用现有保护判定，累计达到30单后恢复Difficulty 1基础算法。
- Boundary / Failure / Cancel: 保护池为空或观察不足时仍回退Difficulty 1基础算法；不额外换单、不自动点击、不承诺无道具必定可解。

## Spec References

- `docs/SPEC.md#core-rules`
- `docs/SPEC.md#world--board--level`

## Must Preserve

- 保护算法、合法候选、预留、去重、暂存、成本近似和可点击缓存语义不变。
- 热身内容、骨架C的50盘/183份/61单、供给顺序、五格暂存、道具、锅位解锁、随机复现和10分钟倒计时不变。
- 旧Difficulty 3、旧策略1/2/3及前15单策略的历史回放继续使用其原规则，不静默切换为新阈值。

## Non-goals

- 不调整食材分布、盘序、暂存容量、倒计时、提示道具或其他难度参数。
- 不修改视觉、音频、平台、奖励、收藏、锅底或社交功能。

## Current Change

- 新局使用两关累计前30单推进保护；热身6单纳入同一累计计数。
- 只修改保护阈值、必要的新策略/内容身份、历史回放分流和直接相关诊断。

## Work Packages

| ID | Agent | Goal | Allowed Write Paths | Forbidden / Shared Paths | Depends On | Status | Integrator |
|---|---|---|---|---|---|---|---|
| WP-032-CODE | `code_agent` | 实现累计前30单保护并保持旧回放规则，完成边界与确定性检查 | `Unity/Assets/HotpotSort/Runtime/Core/**`; `Unity/Assets/HotpotSort/Runtime/Replay/**`; `Unity/Assets/HotpotSort/Runtime/Bootstrap/**`; `Unity/Assets/HotpotSort/Runtime/Presentation/Diagnostics~/*OrderRelief*`; `Unity/Assets/HotpotSort/Editor/ContentImport/**`; `Unity/Assets/HotpotSort/Content/Daily/**`; `.harness/qa/TASK-032/**` | 其他代码与资产；`docs/SPEC.md`和`tasks/**`由PM维护；不得写共享stage/export | None | Completed | Same Agent |
| WP-032-BUILD | `code_agent` | 从当前源码制作独立staging，编译、导出微信小游戏并核对最终包包含策略5 | `.harness/qa/TASK-032-delivery/**` | 当前工作区源码只读；不得修改、提交、推送、上传、提审或发布 | WP-032-CODE | Completed | Same Agent |
| WP-032-UPLOAD | PM | 上传微信开发版0.0.20并执行官方CLI自动预览 | `.harness/qa/TASK-032-delivery/**` | 不设置体验版、不提审、不发布；不上传不匹配的旧包 | WP-032-BUILD | Completed | PM |

## Acceptance

- User-visible result: 热身6单计入累计，累计0–29单沿用现有保护，累计30单起使用Difficulty 1基础算法。
- Minimum runtime check: 验证29/30边界、热身转正式后的累计口径、空池/未知观察回退、同日确定性及旧前15单回放不变；编译受影响代码或运行现有直接相关诊断。
- User review method: 用户重试同日挑战，体验正式挑战前段找不到当前订单食材的情况是否明显减少。

## Result

- Status: Review
- Changed Files / Areas: `DailyCommands.cs`、`DailyDirector.cs`、`DailySession.cs`、`DailyReplay.cs`、`OrderReliefQa.cs`；SPEC同步为累计前30单规则。
- Build / Launch: 已实际执行 `OrderReliefQa` .NET核心诊断，编译通过并完成24,914项断言；独立staging完成Unity 6000.0.26f1 WebGL/IL2CPP编译及微信导出，官方CLI预览、上传与自动预览均成功。
- Current Change Check: 策略5在累计0–29单使用现有等概率保护、30单起恢复Difficulty 1基础算法；正式关正确继承热身6单；同事务29→30、空池/未知观察回退、同日确定性、完整热身/正式模拟与回放通过。旧策略4仍保持正式关14/15边界，旧Difficulty 3回放通过。
- Screenshot / Artifact: `.harness/qa/TASK-032/`（核心诊断）；`.harness/qa/TASK-032-delivery/`（staging、最终包、哈希、IL2CPP证据、CLI回执）。
- Known Issues: 微信开发版`0.0.20`已上传，说明“累计前30单订单保护”；官方CLI自动预览成功，但不等同于已确认手机实际收到、打开或完成真机试玩。保护仍只在补单时触发，不持续监控订单生成后的盘面可点击状态。
- Baseline: Not Saved
