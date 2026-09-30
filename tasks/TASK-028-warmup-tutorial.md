# 热身关、新手提示与两关累计锅位进度

Task ID: TASK-028
Status: Review
Updated At: 2026-09-25

## Requirement Delta

- Goal: 每次开始或重试先完成一个可复现的 6 单热身关，再无缝进入原有 61 单正式挑战；首次热身提供极简引导，并统一提示道具表现。
- Current Behavior: 点击开始后直接创建原有 61 单正式挑战；第三、第四锅按正式局 31/49 单解锁；提示道具使用食材高亮框；没有热身与永久教学标记。
- Target Behavior: 一次挑战由 6 单无计时热身和 61 单正式挑战组成；热身、正式挑战共享锅位提前解锁并以累计 37/55 单计算自动解锁；热身结清后显示“最后一关！”，文字消失后才开始正式盘子供给。首次热身只引导一次点击与订单牌，暂存 4/5 另提示一次；提示道具改为食材放大加手指。
- Entry / Trigger: 每次点击开始或任何正式挑战重试均从当天热身进入；热身失败重开当天同一内容。
- State Change: 热身内容使用独立挑战日种子；同日重试完全一致；热身完成将累计订单数 6、提前开锅状态和永久提示状态传入正式挑战，但不传入热身盘面、订单、暂存、动画、输入锁或随机流。
- Boundary / Failure / Cancel: 热身无倒计时，其他玩法、道具、暂停、复活和奖励与正式挑战一致；切换提示期间禁止输入与正式供给；退出/重试/新会话取消旧表现和回调；不改变正式 61 单内容及其通关条件。

## Spec References

- `docs/SPEC.md#Core-User-Flow`
- `docs/SPEC.md#Rules-and-Data`
- `docs/SPEC.md#UI-and-Visual-Behavior`
- `docs/SPEC.md#Platform-and-Persistence`
- `docs/SPEC.md#World--Board--Level`
- `docs/SPEC.md#Stable-State-Model`

## Must Preserve

- 原有正式挑战 61 单配置、挑战日复现、首次有效操作启动 10 分钟倒计时、物理、点击、奖励、复活和通关结果。
- A+「沉浸餐馆」正式视觉、现有锅盘食材、HUD、结算、TASK-026 与 TASK-027 未提交修改。
- 正式挑战内第三、第四锅仍分别在正式第 31、49 单自动解锁；热身随机流不得消费正式随机流。
- 永久标记沿用现有本地/微信云档案合并与旧档兼容；旧档缺少字段时默认未显示。

## Non-goals

- 关卡选择、章节地图、完整教学系统、正式挑战内容重做、新美术资产、提审、发布、体验版启用、Merge 或 Tag。

## Current Change

- 已由用户确认完整需求复述；进入实现。
- 热身固定 18 份、3 类且每类 6 份；算法生成 4–8 个盘、每盘 1–5 份，两口活动订单不重复并保证首次引导目标开局可点。
- 首次引导两步文案为“点击食材，放入火锅。”与“集满 3 个相同食材，即可完成订单。”；暂存 4/5 文案为“暂存区放满会导致挑战失败。”。
- 引导与暂存警告不使用白色弹窗或确认按钮：首步只保留目标食材正常亮度并限制点击；第二步只保留对应订单牌正常亮度；暂存警告保留整个五格暂存区正常亮度，暂存区以外全部变暗，不得只突出第五码。第二步与暂存警告均点击屏幕任意位置关闭。提示道具不使用全屏压暗。
- 热身最后一单完整结算并清空后显示约 0.8 秒“最后一关！”，无整屏渐变；文字消失后正式供给才开始。
- 首次热身的初始盘子必须先全部落下并停止运动；稳定前锁定食材点击与所有道具，稳定后才显示第一步食材教程。非首次热身不增加此等待锁定。

## Work Packages

| ID | Agent | Goal | Allowed Write Paths | Forbidden / Shared Paths | Depends On | Status | Integrator |
|---|---|---|---|---|---|---|---|
| WP-028-CODE | `code_agent` | 实现两关状态、确定性热身内容、累计 37/55 解锁、锅位继承、永久档案标记、真实到达/反馈清空接口及定向检查 | `Unity/Assets/HotpotSort/Runtime/Core/`; `Runtime/Bootstrap/`; `Runtime/Session/`; `Runtime/Platform/Profile/`; `Runtime/Replay/`; `Contracts/`; `Runtime/Presentation/PresentationPort.cs`; `Unity/Assets/HotpotSort/Runtime/Presentation/GameplayFeedback.cs`; `Unity/Assets/HotpotSort/Tests/Diagnostics~/Task028*`; `cloudfunctions/hotpotProfileSync/profile.js`; `cloudfunctions/hotpotProfileSync/test.js` | `GameplayView.cs`; `GameplayViewV7.cs`; 正式资产；SPEC；Task；不得覆盖其他 Task 未提交修改 | None | Completed | Yes（最终技术集成） |
| WP-028-VISUAL | `visual_agent` | 绑定首次两步引导、4/5 警告、提示道具放大手指和“最后一关！”锁定表现 | `Unity/Assets/HotpotSort/Runtime/Presentation/GameplayView.cs`; `GameplayViewV7.cs`; `Runtime/Presentation/Editor/Task028*`; `.harness/previews/TASK-028/` | Core、Bootstrap、Session、Contracts、Profile、Replay、正式图片资产、SPEC 与 Task | WP-028-CODE 稳定接口 | Completed | Yes（仅表现文件） |
| WP-028-INTEGRATE | `code_agent` | 核对表现绑定不改变规则，修复仅技术问题并执行最低编译/启动/核心路径检查 | WP-028-CODE 与 WP-028-VISUAL 已列路径；本任务专用检查输出 | 其他 Task 与正式资产；不得改变视觉决定 | WP-028-VISUAL | Completed | Yes |
| WP-028-STABLE-TUTORIAL | `code_agent` | 将首次教程入口延迟到全部初始盘子稳定，等待期间锁定食材和道具，并补充真实启动路径检查 | `Unity/Assets/HotpotSort/Runtime/`; `Unity/Assets/HotpotSort/Tests/Diagnostics~/Task028*`; 本任务专用检查输出 | 正式图片资产；其他 Task；SPEC 与 Task | WP-028-INTEGRATE | Completed | Yes |

## Acceptance

- User-visible result: 每次开始/重试运行“6 单热身 → 最后一关提示 → 61 单正式挑战”；同日热身一致；首次引导与 4/5 警告分别永久一次；提示道具使用放大食材加手指。
- Minimum runtime check: 编译；首次热身初始盘全部停止前食材和道具不可操作、教程不出现，稳定后教程出现；启动到热身；同日复现；热身成功切换；热身失败重开；正式供给延迟到提示消失；累计 37/55 与广告继承；永久标记本地/云兼容；退出/重试/旧回调清理；Git Diff 不越界。
- User review method: 提供可运行本地候选与关键真实运行截图/日志；用户实际体验热身节奏、引导可读性、手指提示和无缝切关后决定是否接受。

## Result

- Status: Review
- Changed Files / Areas: Core 热身内容/会话/Director；Bootstrap/Session 两关编排；累计解锁与奖励继承；本地/云档案；PresentationPort/GameplayFeedback；GameplayView 两套表现；云函数 schema/合并；定向诊断与测试；SPEC/Task。
- Build / Launch: Unity 真实 Boot→Composition→Core→Mapper→GameplayView 集成通过，日志标记 `TASK028_INTEGRATION_PASS`；独立表现检查 22 项通过，日志标记 `TASK028_VISUAL_CAPTURE_PASS`。
- Current Change Check: 热身 18 份/3 类/6 单、4–8 盘同日复现、无计时、失败重试、广告开锅继承、累计 37/55、首次真实到锅引导、整排五格警告、任意点击关闭、提示道具放大手指、最后提示消失后正式供给、正式首点计时与旧回调拒绝均通过定向检查。
- Screenshot / Artifact: `.harness/previews/TASK-028/{first-food,order-explanation,buffer-warning,hint-pointer,last-stage}.png`; `unity-visual.log`; `unity-integration.log`。
- Source Delivery: 游戏源码提交 `25da68c`、字体子集提交 `498201a`、稳定盘面教程修复提交 `c476f7d` 已推送到 `origin/feat/auto-push-skill`；最新微信包由 `c476f7d` 构建。
- Cloud / Upload: `hotpotProfileSync` 已部署到开发云环境并处于 `Active`；线上下载源码的 4 个文件 SHA-256 与本地全部一致。最新微信开发版本 `0.0.17` 上传成功，包总计 21,751,653 字节（主包 2,445,054；数据分包 14,666,365；WASM 分包 4,640,234），CLI 自动预览成功；该版本替代 `0.0.16` 作为当前开发候选。
- Stable Tutorial Follow-up: 首次热身新增 `WaitingForBoard`；初始供给完成且全部真实盘子连续 0.3 秒保持稳定后才显示食材教程。等待期间食材与全部道具锁定；非首次热身不增加等待。Unity 真实 Boot 集成与定向检查通过，日志标记 `TASK028_INTEGRATION_PASS`。
- Known Issues: 微信真机是否收到并打开自动预览、真机体验和用户最终视觉判断尚未确认；未提审、未发布、未启用体验版。
- Baseline: Not Saved
