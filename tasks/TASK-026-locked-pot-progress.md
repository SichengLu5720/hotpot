# 锁定锅进度与激励提前解锁

Task ID: TASK-026
Status: Review
Updated At: 2026-09-25

## Requirement Delta

- Goal: 让第三、第四口未解锁锅清楚表达订单解锁进度，并允许玩家点击锅体通过完整激励视频提前解锁。
- Current Behavior: 第三锅第31单自动解锁，第四锅第49单自动解锁；锁定锅只显示文字门槛，且只有第四锅存在直接提前开锅入口。
- Target Behavior: 两口锁定锅均显示无数字文字的胶囊进度条与右上角广告标识；点击任一锁定锅显示剩余订单弹窗，弹窗只提供激励视频解锁。
- Entry / Trigger: 进行中点击第三或第四口未解锁锅体；已解锁锅不触发。
- State Change: 完整观看一次激励视频后立即解锁所点击锅；正常第31/49单自动解锁保持。
- Boundary / Failure / Cancel: 分享入口不存在且分享不能解锁；取消、未看完、失败、无库存、旧会话或重复回调不解锁或重复生效。

## Spec References

- `docs/SPEC.md#UI-and-Visual-Behavior`
- `docs/SPEC.md#World--Board--Level`
- `docs/SPEC.md#Progression--Economy`

## Must Preserve

- 第三锅第31单、第四锅第49单自动解锁门槛。
- 订单、库存、暂存、倒计时、分享额度、其他奖励和解锁后的锅体表现。
- 冻结 A+「沉浸餐馆」整体画面；只增加本次确认的锁定锅元素与弹窗。
- 奖励暂停、完整观看判定、失败/取消、重复请求和旧回调安全规则。

## Non-goals

- 分享提前解锁、修改分享额度或其他奖励规则。
- 修改总订单数、订单生成、难度、锅位布局或其他冻结画面。
- 上传、提审、发布、Push、Commit 或 Tag。
- 部署线上云函数；本轮只更新并验证仓库内兼容代码。

## Current Change

- 实现第三、第四锅独立的锁定进度、广告标识、点击弹窗和激励提前解锁。
- 胶囊填充分别为 `completedOrders / 31`、`completedOrders / 49`，无数字和文字；弹窗显示对应剩余订单数。
- 用户已确认无穿模 v2 示意图，正式接入：移除两口锁定锅右上角 `AD`，中央锁改为加号；三个底部道具右上角增加完整位于按钮安全区内的圆形加号。
- 当前仅研究胶囊进度条的“火力递增”红色渐变；先制作强旺火 A 与克制深红 B 两张同进度示意图，用户选择前不接入正式界面。
- 用户已选择并确认 A「强旺火」：固定全宽渐变 `#781C14 → #B52618 → #E43B1F → #FF7626`，按实际进度裁切，不拉伸、不加动画；正式接入待完成。

## Work Packages

| ID | Agent | Goal | Allowed Write Paths | Forbidden / Shared Paths | Depends On | Status | Integrator |
|---|---|---|---|---|---|---|---|
| WP-026-CODE | `code_agent` | 提供第三/第四锁定锅可定向激励解锁的稳定状态与奖励接口，保持自动门槛、分享禁止和幂等安全 | `Unity/Assets/HotpotSort/Contracts/**`; `Unity/Assets/HotpotSort/Runtime/Core/**`; `Unity/Assets/HotpotSort/Runtime/Session/**`; `Unity/Assets/HotpotSort/Runtime/Bootstrap/**`; `Unity/Assets/HotpotSort/Runtime/Replay/DailyReplay.cs`; `Unity/Assets/HotpotSort/Runtime/Platform/Profile/ProfileStore.cs`; `Unity/Assets/HotpotSort/Runtime/Platform/WeChat/WeChatRewardService.cs`; `Unity/Assets/HotpotSort/Runtime/Presentation/PresentationPort.cs`（仅增加 typed completedOrders 传输字段）; `cloudfunctions/hotpotProfileSync/profile.js`; `cloudfunctions/hotpotProfileSync/test.js`; 对应 `Diagnostics~/**` 测试 | `Runtime/Presentation/GameplayView*.cs`; 正式美术资产；SPEC/Task；云函数部署 | None | Completed | Yes，最终技术集成 |
| WP-026-VISUAL | `visual_agent` | 在锁定锅上实现胶囊进度、广告标识、整锅点击与单按钮剩余订单弹窗 | `Unity/Assets/HotpotSort/Runtime/Presentation/GameplayView.cs`; `Unity/Assets/HotpotSort/Runtime/Presentation/GameplayViewV7.cs`; 本任务专用 Presentation Editor/Diagnostics 文件 | Core、Contracts、Session、Bootstrap；正式图片资产；SPEC/Task | WP-026-CODE 的稳定接口 | Completed | No |
| WP-026-PLUS-PREVIEW | `visual_agent` | 基于真实 A+ 玩法截图制作锅体与道具加号调整示意图，不接入正式界面 | `.harness/previews/TASK-026/**` | Unity 代码、正式资产、SPEC/Task | WP-026-VISUAL | Completed | Visual Lead |
| WP-026-PLUS-INTEGRATION | `visual_agent` | 按已确认 v2 示意图接入锅体与三个道具加号，保持奖励逻辑和冻结布局 | `Unity/Assets/HotpotSort/Runtime/Presentation/GameplayView.cs`; `Unity/Assets/HotpotSort/Runtime/Presentation/GameplayViewV7.cs`; `Unity/Assets/HotpotSort/Runtime/Presentation/Editor/Task026LockedPotCapture.cs`; `.harness/qa/TASK-026/**` | Core、Contracts、Session、Bootstrap、正式图片资产、SPEC/Task | WP-026-PLUS-PREVIEW | Completed | Visual Lead |
| WP-026-FIRE-GRADIENT-PREVIEW | `visual_agent` | 基于实际 Unity 截图制作两张互斥红色渐变示意图，只比较配色 | `.harness/previews/TASK-026/**` | Unity 代码、正式资产、SPEC/Task | WP-026-PLUS-INTEGRATION | Completed | Visual Lead |
| WP-026-FIRE-GRADIENT-INTEGRATION | `visual_agent` | 精确接入 A 强旺火固定全宽渐变并按订单进度裁切 | `Unity/Assets/HotpotSort/Runtime/Presentation/GameplayView.cs`; `Unity/Assets/HotpotSort/Runtime/Presentation/GameplayViewV7.cs`; 本任务专用 Presentation 脚本与 `.meta`; `Unity/Assets/HotpotSort/Runtime/Presentation/Editor/Task026LockedPotCapture.cs`; `.harness/qa/TASK-026/**` | Core、Contracts、Session、Bootstrap、正式图片资产、SPEC/Task | WP-026-FIRE-GRADIENT-PREVIEW | Completed | Visual Lead |

## Acceptance

- User-visible result: 两口锁定锅均显示胶囊进度与中央加号且无 `AD`/锁头；三个道具右上角显示不穿模的加号；点击显示正确剩余订单数及唯一激励入口；没有分享提前开锅入口。
- Minimum runtime check: 第三/第四锅分别在第31/49单自动解锁；两口锅均可在完整激励后定向提前解锁；取消、失败、无库存、重复/迟到回调不生效；受影响入口可编译并启动。
- User review method: 提供实际运行截图或可玩构建，由用户判断胶囊、标识、弹窗及点击体验。

## Result

- Status: Review
- Changed Files / Areas: 第三锅奖励类型与定向解锁、两锅分享拒绝、回放/档案/云同步本地 schema、typed `completedOrders`、两套 GameplayView 锁定锅 UI 与专项诊断。
- Build / Launch: Unity 编译与 PlayMode A 强旺火渐变专项已运行；未生成新 Player 或微信包。
- Current Change Check: 火力渐变专项 39 项通过，覆盖精确色停靠、固定全宽裁切、0%/低进度/75%/45%、30/31/48/49临界、解锁清理、无动画和共享纹理复用；此前加号集成26项及核心/奖励/联合/云同步检查通过；`git diff --check` 通过。
- Screenshot / Artifact: `.harness/qa/TASK-026/fire-integration/fire-75-45-fixture.png`; `.harness/qa/TASK-026/fire-integration/progress-15-orders.png`; `.harness/previews/TASK-026/fire-gradient-a.png`; `.harness/qa/TASK-026/joint-integration.json`。
- Known Issues: A 强旺火正式界面已接入，待用户体验验收；未做 GC Profiler 或真机测试；线上 `hotpotProfileSync` 云函数尚未部署新 rewardKind schema；真机激励广告未测试。
- Baseline: Not Saved
