# 食材收集、配置与好友交换

Task ID: TASK-030
Status: Review
Updated At: 2026-09-25

## Requirement Delta

- Goal: 在现有 16 种正式食材基础上新增 16 种可收集食材，并增加每日首次通关奖励、16 位出战配置、重复副本与微信好友定向交换。
- Current Behavior: 当前只有 16 种固定食材映射，没有图鉴、食材奖励、出战配置、副本库存或交换。
- Target Behavior: 总图鉴 32 种；现有 16 种默认解锁，新 16 种通过每日首次通关或交换获得；玩家自由选择不少于 16 种，正好 16 种时全用，超过 16 种时每次进入对局重新随机抽 16 种；配置只替换身份映射；集齐后每日首次通关随机增加两个免费道具。
- Entry / Trigger: 主页左侧边栏锁定的“备菜”食材篮 icon，账号首次通关后永久解锁；每日 06:00 周期首次通关；微信好友分享交换请求。
- State Change: 永久解锁、重复副本、16 位配置、每日领取状态、免费道具库存与交换请求进入云端权威档案。
- Boundary / Failure / Cancel: 已选不足 16 种时拦截页面返回、关闭和系统返回并提示“食材不够吃啦～请再多选点”，强制关闭后恢复最近一次有效配置；交换一份换一份、不可赠送、24 小时失效、可撤回，成交原子且幂等；离线通关记录待领取，联网后补领。

## Spec References

- `docs/SPEC.md#rules-and-data`
- `docs/SPEC.md#ui-and-visual-behavior`
- `docs/SPEC.md#platform-and-persistence`
- `docs/SPEC.md#must-preserve`

## Must Preserve

- 现有 16 种正式图片、A+ 冻结视觉、玩法结构、点击和物理边界不变。
- 骨架 C 的 50 盘、183 份、61 单、逐盘内容、订单和供给规则不变；配置只替换 16 种身份映射。
- 热身与正式挑战共用当前配置，同一日期与同一配置稳定复现。
- 不覆盖或回滚 TASK-029 及其他未提交修改。

## Non-goals

- 不增加关卡制、稀有度、货币、商店、赠送、公开市场、付费抽取或一次多份交换。
- 本 Task 不授权 Commit、Push、Merge、Tag、微信云部署、开发版上传、提审或发布。
- 素材审核通过前不导入正式主题、不修改现有 `food_00..15`。

## Current Change

- 16 种新增食材、主页整体式“备菜”入口和卡片式四列图鉴预览均已通过用户审核，开始正式接入 Unity。
- 实装批次先由 CODE-01 建立收藏、配置、每日奖励与会话抽选的稳定接口，VIS-10 同期只导入已审核资产；接口完成后 VIS-10 绑定主页、图鉴、配置和奖励表现，CODE-02 接入微信云端交换源代码，最后 CODE-03 串行集成并执行最低检查。

## Work Packages

| ID | Agent | Goal | Allowed Write Paths | Forbidden / Shared Paths | Depends On | Status | Integrator |
|---|---|---|---|---|---|---|---|
| VIS-01 | `visual_agent` | 虾滑、牛肉丸候选 | `.harness/previews/TASK-030/VIS-01/**` | `Unity/**`, `docs/**`, `tasks/**`, 其他 WP | None | Completed | VIS-09 |
| VIS-02 | `visual_agent` | 牛肉滑、鸭肠候选 | `.harness/previews/TASK-030/VIS-02/**` | 同上 | None | Completed | VIS-09 |
| VIS-03 | `visual_agent` | 黄喉、响铃卷候选 | `.harness/previews/TASK-030/VIS-03/**` | 同上 | None | Completed | VIS-09 |
| VIS-04 | `visual_agent` | 小郡肝、藕片候选 | `.harness/previews/TASK-030/VIS-04/**` | 同上 | None | Completed | VIS-09 |
| VIS-05 | `visual_agent` | 娃娃菜、豆皮候选（娃娃菜单片叶迭代） | `.harness/previews/TASK-030/VIS-05/**` | 同上 | None | Completed | VIS-09 |
| VIS-06 | `visual_agent` | 贡菜、川粉候选 | `.harness/previews/TASK-030/VIS-06/**` | 同上 | None | Completed | VIS-09 |
| VIS-07 | `visual_agent` | 冻豆腐、海带结候选 | `.harness/previews/TASK-030/VIS-07/**` | 同上 | None | Completed | VIS-09 |
| VIS-08 | `visual_agent` | 香菇、鲜竹笋候选 | `.harness/previews/TASK-030/VIS-08/**` | 同上 | None | Completed | VIS-09 |
| VIS-09 | `visual_agent` | 统一核查并生成 16 种审核图（已含娃娃菜单片叶） | `.harness/previews/TASK-030/review/**` | 原始候选只读；`Unity/**` | VIS-01..08 | Completed | Yes |
| VIS-11 | `visual_agent` | 主页无底板的整体式拟真食材篮入口与卡片式四列图鉴预览迭代 | `.harness/previews/TASK-030/ui/**` | `Unity/**`、正式食材候选只读 | VIS-09, User asset approval | Completed | VIS-11 |
| CODE-01 | `code_agent` | 收藏、有效配置、每局16种抽选、每日首次通关奖励、免费道具库存与本地持久化稳定接口 | `Unity/Assets/HotpotSort/Contracts/Collection*`; `Unity/Assets/HotpotSort/Runtime/Collection/**`; `Unity/Assets/HotpotSort/Runtime/Platform/Profile/ProfileStore.cs`; `Unity/Assets/HotpotSort/Runtime/Bootstrap/LocalDevelopmentServices.cs`; `Unity/Assets/HotpotSort/Runtime/Core/DailySessionFactory.cs`; `Unity/Assets/HotpotSort/Tests/Diagnostics~/Task030Collection*` | `GameplayView*`; `DailyProductionComposition.cs`; `Bootstrap.cs`; TASK-029 文件；正式视觉资产 | User approval | Completed | CODE-03 |
| VIS-10 | `visual_agent` | 导入16种审核资产与整体式入口，接入主页、卡片式四列图鉴、配置拦截、交换和奖励表现 | `Unity/Assets/HotpotSort/Resources/Hotpot/TASK001/v10/r001/food/food_16..31*`; `.../icons/ingredient_entry*`; `Unity/Assets/HotpotSort/Runtime/Presentation/GameplayViewCollection.cs*`; `Unity/Assets/HotpotSort/Runtime/Presentation/Editor/Task030*` | 现有 `food_00..15`; `GameplayView.cs`; `GameplayViewV7.cs`; TASK-029 文件；核心与平台语义；已冻结 A+ 其他画面 | CODE-01 | Completed | VIS-10 |
| CODE-02 | `code_agent` | 微信云权威档案与好友定向一换一原子交换源代码 | `cloudfunctions/hotpotIngredientTrade/**`; `Unity/Assets/HotpotSort/Runtime/Platform/WeChat/WeChatIngredientTrade*`; `Unity/Assets/HotpotSort/Runtime/Presentation/Diagnostics~/IngredientTrade*` | 正式视觉资产；现有云函数；不部署 | CODE-01 | Completed | CODE-03 |
| CODE-03 | `code_agent` | 串行连接 Composition/Bootstrap/主题资产索引并执行最低技术检查 | `Unity/Assets/HotpotSort/Runtime/Bootstrap/DailyProductionComposition.cs`; `Unity/Assets/HotpotSort/Runtime/Bootstrap/Bootstrap.cs`; `Unity/Assets/HotpotSort/Runtime/Presentation/Contracts/PresentationLifecycle.cs`; `Unity/Assets/HotpotSort/Runtime/Presentation/GameplayViewV7.cs`; `Unity/Assets/HotpotSort/Runtime/Presentation/PresentationThemeValidation.cs`; `Unity/Assets/HotpotSort/Runtime/Presentation/TaskAssetValidation.cs`; `Unity/Assets/HotpotSort/Editor/WeChatBuild/Task002V10BundleTool.cs`; `Unity/Assets/HotpotSort/Editor/WeChatBuild/PackageDependencyGuard.cs`; 必要 asmdef/meta 与 TASK-030 诊断 | TASK-029 逻辑；未授权发布配置 | CODE-01, VIS-10, CODE-02 | Completed | Yes |

## Acceptance

- User-visible result: 第一阶段提供 16 张独立透明食材候选及一张统一对照审核图；最终阶段在主页完成图鉴、16 位替换配置、每日奖励与好友一换一。
- Minimum runtime check: 第一阶段逐图检查 Alpha、单主体、无盘/字/水印、正俯视和缩小辨识；最终阶段编译、启动到入口并走通配置、领取与本地交换模拟。
- User review method: 用户先逐项审核 16 种食材审核图；未明确通过前不进入正式主题集成。

## Result

- Status: Review
- Changed Files / Areas: 收藏 Contracts/Store、本地分账户持久化、16 种新增正式资源、主页与图鉴表现、会话抽选/回放、微信云函数与交易客户端源代码、主题与资源门禁、TASK-030 诊断。
- Build / Launch: Unity batch 编译与 TASK-030 PlayMode 通过；真实 Boot 已覆盖锁定/解锁主页、32 项图鉴、不足拦截、奖励层及新增食材热身盘面。
- Current Change Check: 32 种收藏、有效配置、超过 16 每次主页进局重抽、每日 06:00 首次通关奖励、重复副本、集齐后两个免费道具、整体式主页入口、四列图鉴、退出拦截与回放选集恢复均已本地实装；云端收藏与原子交换源码已完成但未部署。
- Screenshot / Artifact: `.harness/artifacts/TASK-030/integration/home-locked.png`; `.harness/artifacts/TASK-030/integration/home-unlocked.png`; `.harness/artifacts/TASK-030/integration/collection.png`; `.harness/artifacts/TASK-030/integration/under-minimum.png`; `.harness/artifacts/TASK-030/integration/reward.png`; `.harness/artifacts/TASK-030/integration/new-foods-gameplay.png`。
- Known Issues: 未部署云函数、未上传、未做微信真机验证；当前没有可信微信好友身份选择与安全分享入站路由，因此交换入口 fail-closed，不伪造可用；微信系统返回事件桥尚未接入，页面返回、Escape 与 Composition Exit 已拦截；生产领取/消费的网络与后台恢复待真机验收。
- Baseline: Not Saved
