# TASK-031：好友助力三选一锅底

- Status: Working
- Current Change: 已按确认方案完成逻辑、云函数源码、正式资产与表现集成；正在联合校验、整合推送并上传匹配的微信开发版。

## Goal

通过 1 名不同微信账号的有效助力，让玩家在清汤、番茄、菌锅中永久三选一；助力者获得提示、清空暂存、打乱各 1 次，并将锅底收藏并入现有备菜收藏页。

## Confirmed Behavior

- 红汤默认永久拥有；本活动只解锁清汤、番茄、菌锅中的一种，二次确认后不可改选，其他两种可由未来活动获得。
- 另一微信账号必须通过发起者专属分享链接进入小游戏并在活动落地页主动确认；只验证不同账号，不宣称校验真实微信好友关系。被邀请账号无需先通关即可落地确认；同一账号对同一发起者只能有效一次，禁止自助，可帮助不同玩家但北京时间自然日 00:00 重置且每日最多 3 人。
- 发起者获得 1 名有效助力后取得永久保留的三选一资格；已有有效助力后不再接收额外计数或发放额外助力奖励。
- 助力者获得三种道具各 1 次，进入现有跨局、跨天免费库存；助力与奖励由云端幂等事务同时结算。
- 收藏页使用“食材 / 锅底”页签；锅底页只显示名称、拥有/锁定/当前使用和切换，不接入助力。主页侧栏使用带文字标签的独立锅底活动入口，活动页保留必要文字并内嵌活动详情；当前锅底统一用于四口锅、热身与正式挑战，并跨设备保存。
- 用户提供的《羊了个羊》截图仅用于参考侧栏独立图标、文字标签与提醒红点的信息层级，不复制品牌、彩虹配色、中央大型活动场景或整页多入口布局。
- 锅底活动入口与备菜共用账号首次通关门槛，但状态表现不同：开放前不渲染，首次通关后永久出现；出现时只使用 TASK-030 已确认的无底板图文一体彩色入口，不制作可见锁定态。
- “锅底活动”整个图文 icon 必须与“备菜”入口同轴垂直排列，位于备菜正下方；文字自身仍保持正常横排。
- 红点只表示“已取得本活动三选一资格但尚未选择”，取得资格出现、永久选择后消失；打开活动页不清除。
- 新锅底只替换汤色、少量漂浮配料与气泡细节；现有铜锅、点火、蒸汽、订单牌、HUD、布局和玩法保持。

## Must Preserve

- A+「沉浸餐馆」冻结视觉、正式铜锅及红汤默认状态。
- TASK-030 食材收藏、配置、奖励、免费道具库存与交换规则。
- 普通分享、奖励分享与本活动助力相互隔离；不得用分享面板返回伪造助力。
- 当前未提交代码、资产和其他 Task 的修改不得覆盖或回滚。

## Non-goals

- 锅底玩法属性、食材、订单、难度或收益变化。
- 四口锅混搭、三套完整 UI 主题、活动期限、排行榜、组队或额外发起者奖励。
- 提审、正式发布或体验版设置；本任务完成并验证后，用户已授权提交并推送范围内源码及上传匹配的微信开发版。

## Work Packages

### WP-031-PREVIEW

Work Package ID: WP-031-PREVIEW
Goal: 基于当前正式画面制作清汤、番茄、菌锅汤面预览，以及纯锅底收藏页、主页侧栏活动入口、独立活动页与详情说明预览。
Spec References: `docs/SPEC.md` Rules and Data、UI and Visual Behavior、Platform and Persistence、Art Bible。
Must Preserve: 正式铜锅、A+ 视觉、四锅布局、食材页既有决定；预览不得被误作正式资产。
Allowed Write Paths: `.harness/previews/TASK-031/`；本 Task 的 Result 区由 PM 更新。
Forbidden / Shared Paths: `Unity/Assets/`、`cloudfunctions/`、`docs/SPEC.md`、其他 Task、正式资产与构建目录。
Depends On: None。
Acceptance: 保留 r001 三种真实玩法汤面方向；锅底页不含任何助力内容；主页使用与 TASK-030 一致的无底板图文一体“锅底活动”入口，并展示开放前完全隐藏、开放后彩色可点击及红点示例；独立活动页保留必要操作文字并提供可展开的活动详情；无布局漂移、无玩法误导、无正式接入。
Integrator: Visual Lead（本包独立，无共享代码集成）。

### WP-031-CODE-01

Work Package ID: WP-031-CODE-01
Goal: 实现锅底稳定 ID、所有权/当前选择/活动资格/选择结果的数据合同、本地存储与表现中立服务接口。
Spec References: `docs/SPEC.md` 对应新增规则。
Must Preserve: TASK-030 现有收藏合同、免费道具库存与未提交修改；旧档案归一化为仅拥有红汤。
Allowed Write Paths: `Unity/Assets/HotpotSort/Contracts/BrothContracts.cs*`；`Unity/Assets/HotpotSort/Contracts/CollectionContracts.cs`；`Unity/Assets/HotpotSort/Runtime/Collection/CollectionStore.cs`；`Unity/Assets/HotpotSort/Runtime/Collection/BrothActivityStore.cs*`；`Unity/Assets/HotpotSort/Tests/Diagnostics~/Task031Broth*`。
Forbidden / Shared Paths: UI、正式资产、云函数、Bootstrap/Composition、SPEC、其他 Task；共享收藏文件由本包单 Agent 串行修改。
Depends On: WP-031-PREVIEW Accepted；以 TASK-030 当前未提交收藏源码为保留基线。
Acceptance: 默认红汤、旧档案恢复、资格永久保留、已拥有锅底才能选择、本活动选择不可重复或改选、旧版本回调不得回滚当前状态。
Integrator: `code_agent`。

### WP-031-CODE-02

Work Package ID: WP-031-CODE-02
Goal: 在现有收藏权威云函数中实现专属邀请、助力、三项免费库存发奖和三选一原子幂等事务。
Spec References: `docs/SPEC.md` Rules and Data、Platform and Persistence。
Must Preserve: 微信可信身份、TASK-030 原子事务/操作回执、现有免费库存、普通分享与奖励分享隔离。
Allowed Write Paths: `cloudfunctions/hotpotIngredientTrade/trade.js`；`cloudfunctions/hotpotIngredientTrade/broth.js`；`cloudfunctions/hotpotIngredientTrade/broth.test.js`；必要时该函数 `package.json`。
Forbidden / Shared Paths: 不部署；不修改 `hotpotProfileSync`；不另建免费库存。
Depends On: WP-031-CODE-01 稳定合同语义；以 TASK-030 当前云函数为保留基线。
Acceptance: 覆盖自助、同账号重复、每日第四次、并发争抢唯一助力、异常回滚、三项各 +1、三选一并发不可改选和旧链接不重复发奖。
Integrator: `code_agent`。

### WP-031-CODE-03

Work Package ID: WP-031-CODE-03
Goal: 实现微信专属分享/冷热启动落地、云函数适配、开发模拟与应用集成。
Spec References: `docs/SPEC.md` 对应分享与账号规则。
Must Preserve: 普通分享、奖励分享与其额度/冷却；分享返回不得计助力；被邀请账号无需首通即可落地。
Allowed Write Paths: `Unity/Assets/HotpotSort/Runtime/Platform/WeChat/WeChatBrothActivityService.cs*`；`WeChatActivityLinkService.cs*`；`WeChatServiceScope.cs`；`Runtime/Bootstrap/Bootstrap.cs`；`DailyProductionComposition.cs`；`LocalDevelopmentServices.cs`；`Tests/Diagnostics~/Task031Broth*`。
Forbidden / Shared Paths: 不修改玩法核心、视觉决定或正式资产；Bootstrap/Composition 由本包 Integrator 串行修改。
Depends On: WP-031-CODE-01、WP-031-CODE-02。
Acceptance: C# 编译；普通分享返回不计助力；冷/热链接落地确认；账号/环境/请求/版本过滤；禁用或失败不伪造成功；开发模拟明确标记。
Integrator: `code_agent`。

### WP-031-VISUAL-INTEGRATION

Work Package ID: WP-031-VISUAL-INTEGRATION
Goal: 在预览获用户确认且 Code 提供稳定接口后，生产正式锅底资产并完成收藏/活动表现绑定。
Spec References: `docs/SPEC.md` 对应新增 UI 与视觉规则。
Must Preserve: 用户确认预览、A+ 冻结视觉和 Code 稳定接口。
Allowed Write Paths: `Unity/Assets/HotpotSort/Resources/Hotpot/TASK031/`；`Runtime/Presentation/GameplayViewBroth.cs*`；`Runtime/Presentation/GameplayViewCollection.cs`；`Runtime/Presentation/GameplayViewV7.cs`；本任务专用表现诊断。
Forbidden / Shared Paths: `GameplayView.cs` 现有 TASK-029 修改不得触碰；不修改核心、云函数、SPEC、其他 Task；如必须新增共享钩子先返回 PM。
Depends On: WP-031-PREVIEW Accepted；WP-031-CODE-01 稳定接口；WP-031-CODE-03 可调用服务。
Acceptance: 按 r001/r002/r004 实现；首通前入口不存在，开放后与备菜 X=102 同轴；锅底页无助力；活动/详情/好友确认完整；四锅、热身与正式统一汤面；无缺图/遮挡/引用错误。
Integrator: Visual Lead；Code 最后仅做不改变视觉决定的技术检查。

### WP-031-CODE-04

Work Package ID: WP-031-CODE-04
Goal: 串行接入新资源白名单并执行与当前变化直接相关的最低技术检查。
Spec References: `docs/SPEC.md` Resource Delivery、当前 Task。
Must Preserve: Visual 已确认资源与表现；TASK-030 包资源改动；现有本地整包启动规则。
Allowed Write Paths: `Runtime/Presentation/Contracts/PresentationLifecycle.cs`；`PresentationThemeValidation.cs`；`TaskAssetValidation.cs`；`Editor/WeChatBuild/Task002V10BundleTool.cs`；必要时 `PackageDependencyGuard.cs`；Task Result。
Forbidden / Shared Paths: 不改变视觉决定、不上传、不提交、不发布。
Depends On: CODE-01/02/03 与 VISUAL-INTEGRATION Completed。
Acceptance: 编译；启动至入口；受影响路径 Smoke；缺失引用/明显错误检查；定向 Diff 范围检查。
Integrator: `code_agent`。

## Result

- Requirement: Confirmed on 2026-09-25.
- Accepted Preview Baseline: 2026-09-25 用户确认 r004；入口采用 TASK-030 图文一体悬浮逻辑，首次通关前隐藏、通关后彩色开放，与备菜同轴 X=102 垂直排列；r002 的纯锅底收藏、独立活动、内嵌详情与好友确认结构及 r001 三种汤面方向继续有效。
- Preview: r001 汤面方向保留；r001 将助力放入锅底页的结构已被用户否定并由 r002 替代。
- Preview r002: 已交付 `.harness/previews/TASK-031/r002/`，包含主页侧栏入口、纯锅底页、活动主视图、详情展开、好友确认和已拥有锅底切换示例；所有主屏均提供 1080×1920 与 360×640 检查图。
- Preview r002 checks: 主页新增入口区域外差异像素为 0；锅底页不含助力/邀请/活动/`0/1`；活动保持三种未解锁、`0/1` 与三项各 `×1`；r001 文件哈希保持不变。未运行 Unity、构建或真机测试。
- Preview r003: 已交付 `.harness/previews/TASK-031/r003/`；只迭代主页入口逻辑，其余 r001/r002 已确认方向保持只读。
- Preview r003 checks: 开放前页面与 TASK-030 最新锁定底图逐字节一致且活动入口像素为 0；开放后只在新增入口区域产生差异，无底板、灰化或锁头；既有元素未移动，r001/r002 哈希保持不变。未运行 Unity、构建或真机测试。
- Preview r004: 已交付 `.harness/previews/TASK-031/r004/`；只调整锅底活动入口的水平位置，使其与备菜同轴垂直排列。
- Preview r004 checks: 两入口中轴均为 X=102；隐藏态保持不变，开放态只在新旧入口区域并集内产生差异。入口与左侧红布边缘存在轻微纯视觉重叠，但图标和文字完整可读，按现有 A+ 边缘装饰规则接受；未运行 Unity、构建或真机测试。
- Preview checks: 三种玩法图均保持两锅开启、两锅未开火；中文、`0/1`、`1/1` 与三项 `×1` 奖励可读；未运行 Unity、构建或真机测试。
- CODE-01: Completed；稳定锅底合同、本地收藏/资格/选择状态、开发模拟与红点派生已实现，25 项 TASK-031 数据断言及 TASK-030 收藏诊断通过。
- CODE-02: Completed；现有 `hotpotIngredientTrade` 已增加专属邀请、助力、三项库存原子发奖、每日上限与不可改选事务；TASK-030 既有 Node 测试及 TASK-031 新事务测试通过，未部署。
- CODE-03: Completed；生产微信活动服务、冷/热链接落地、登录前保留、响应过滤与 Bootstrap/Composition 接线已实现；23 项平台断言、25 项数据断言、TASK-030 诊断及真实微信 SDK 条件编译通过，未真机。
- Visual Assets: Completed；`Resources/Hotpot/TASK031/Broths/` 已加入清汤、番茄、菌锅 512×512 RGBA 正式汤面，Alpha 与红汤一致，透明区、缩小辨识、GUID 与导入设置检查通过；未启动 Unity。
- Visual Integration: Completed；独立活动、纯锅底收藏、好友确认、二次确认与四种汤面已接入表现层；Unity 编译和 66 项专项断言通过，23 张真实运行截图完成视觉检查。服务事件接线与资源白名单留给 CODE-04。
- CODE-04: Completed；活动/收藏交互事件、异步操作防重与过期响应过滤已接入，TASK-031 资源加入生命周期、主题、资源与微信导出白名单；数据 25 项、平台 23 项、TASK-030 诊断、Unity 启动接线 17 项、视觉 66 项及本地资源门禁均通过。
- Delivery Authorization: 2026-09-25 用户进一步授权整合当前工作区全部有效产品更新，排除本地构建、日志、缓存、备份、诊断中间产物与无关工具发布目录；联合验证后统一提交/推送，并上传与提交匹配的微信开发版。未授权提审、正式发布或设置体验版。
- Uploaded / Released: Not yet performed / Release not authorized.
