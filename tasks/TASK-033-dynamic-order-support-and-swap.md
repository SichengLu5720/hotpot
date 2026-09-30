# Dynamic Order Support and Swap Tool

Task ID: TASK-033  
Status: Review  
Updated At: 2026-09-26

## Requirement Delta

- Goal: 在保留 Difficulty 1 波动和可赌局面的同时，避免多个订单同时缺少近期可获得食材；用玩家主动选择的换单替代提示道具。
- Current Behavior: 新局使用累计前30单等概率保护；提示只寻找匹配食材。实际锅位越多时保护仍固定，广告提前开锅可能提前增加订单压力。
- Target Behavior: 平时完全使用 Difficulty 1；危险单数量达到“常规流程应解锁锅数减一”时，下一次补单按近期支持接管。提示直接替换为换单，由玩家选择订单，系统保证替换目标可由近期支持完成。
- Entry / Trigger: 每次补单或新锅生成首单时重算危险单和正常解锁阶段；点击换单时计算合法订单，选择时再次复核。
- State Change: 接管只约束新订单候选，不自动替换已有订单；换单成功时把原订单已收集食材退回暂存，再改变目标。
- Boundary / Failure / Cancel: 接管无完整候选时选择近期支持最多者并以D1处理并列；全部为零时退化为D1。换单无目标或复核失效不消耗；非订单点击和系统返回取消。

## Spec References

- `docs/SPEC.md#core-rules`
- `docs/SPEC.md#world--board--level`
- `docs/SPEC.md#animation-and-vfx`

## Must Preserve

- 16种当局食材、骨架C 50盘/183份/61正式单、6热身单、供给顺序、五格暂存、倒计时、物理、现有37/55累计正常解锁节点。
- Difficulty 1 原始候选权重、订单去重、库存守恒、其他订单预留与确定性。
- 广告提前开锅继续生效并继承，但不提高动态接管阈值。
- 清空暂存、打乱、奖励来源与分享额度规则不变。
- A+冻结视觉；只增加已确认的换单选择遮罩、退回动画和替换演示。

## Non-goals

- 不自动替换危险单，不暂停倒计时，不冻结供给或物理。
- 不改变食材分布、盘序、暂存容量、倒计时或正常锅解锁节点。
- 不维护旧测试存档、旧提示库存或旧回放兼容；不提审、不设体验版、不发布、不提交或推送。用户已另行授权完成后上传匹配的微信开发版。

## Current Change

- 用近期支持动态接管替换TASK-032的累计前30单保护，并在核心层绑定正常锅解锁状态。
- 将Hint奖励身份和交互直接替换为SwapOrder；新测试版本允许旧本地数据失效。
- 更新奖励卡演示及直接相关诊断，产出本地可运行版本供体验。

## Work Packages

### WP-033-CODE

Work Package ID: WP-033-CODE  
Goal: 实现近期支持观察、危险单判定、正常解锁阶段接管、换单事务与稳定表现接口。  
Spec References: `Core Rules`; `World / Board / Level`; `Stable State Model`  
Must Preserve: 核心确定性、库存守恒、37/55正常解锁、广告锅继承、D1权重、计时/供给/物理不中断。  
Allowed Write Paths: `Unity/Assets/HotpotSort/Contracts/**`; `Unity/Assets/HotpotSort/Runtime/Core/**`; `Unity/Assets/HotpotSort/Runtime/Session/**`; `Unity/Assets/HotpotSort/Runtime/Replay/**`; `Unity/Assets/HotpotSort/Runtime/Collection/**`; `Unity/Assets/HotpotSort/Runtime/Bootstrap/**`; `Unity/Assets/HotpotSort/Runtime/Platform/WeChat/WeChatIngredientTradeService.cs`; `cloudfunctions/hotpotIngredientTrade/**`; `Unity/Assets/HotpotSort/Editor/ContentImport/**`; `Unity/Assets/HotpotSort/Runtime/Presentation/Diagnostics~/*Order*`; `.harness/qa/TASK-033/core/**`  
Forbidden / Shared Paths: `Runtime/Presentation/**`除上述诊断外只读；`docs/SPEC.md`与`tasks/**`由PM维护；不得修改正式视觉资产、上传或Git提交。  
Depends On: None  
Acceptance: 覆盖0/1/2危险单、常规解锁阶段与广告提前锅分离、一层前瞻、无完整候选降级、换单库存守恒/取消/失效/确定性；免费云库存使用预留/确认/撤销或等价事务保证复核失效不扣库存，并编译直接相关代码与云函数测试。  
Integrator: `code_agent`

### WP-033-VIS

Work Package ID: WP-033-VIS  
Goal: 接入换单选择态、直接变暗、订单选择与食材退回表现，并替换奖励卡演示。  
Spec References: `UI and Visual Behavior`; `Animation and VFX`; `UX and Input Principles`  
Must Preserve: A+冻结布局与正式资产；倒计时、供给、物理继续；换单约0.34秒直线退回；演示隔离。  
Allowed Write Paths: `Unity/Assets/HotpotSort/Runtime/Presentation/**`; `Unity/Assets/HotpotSort/Editor/*ToolDemo*`; `.harness/qa/TASK-033/visual/**`  
Forbidden / Shared Paths: 核心与Contracts只读；`Runtime/Bootstrap/**`由Code Integrator维护；不得生成或替换正式美术资产、上传或Git提交。  
Depends On: WP-033-CODE提供稳定接口  
Acceptance: 合法订单正常、非法订单和玩法区变暗；非订单/返回取消；退回期间输入锁定而系统继续；无目标反馈；换单演示按1秒静态+约3秒动作循环且不强调可立即完成。  
Integrator: `visual_agent`

### WP-033-INT

Work Package ID: WP-033-INT  
Goal: 串行完成共享绑定、编译、启动及当前改动的最低检查，不改变视觉决定。  
Spec References: 本Task全部  
Must Preserve: Code与Visual已完成行为；不扩大回归或发布范围。  
Allowed Write Paths: `Unity/Assets/HotpotSort/Runtime/Bootstrap/**`; `Unity/Assets/HotpotSort/Editor/TASK001V8/VisualIntegrationCapture.cs`仅机械替换奖励枚举；直接相关诊断；`.harness/qa/TASK-033/integration/**`; Task Result由PM更新  
Forbidden / Shared Paths: 不修改已确认视觉决定或无关功能；不得上传、提交、推送、提审或发布。  
Depends On: WP-033-CODE; WP-033-VIS  
Acceptance: Unity编译无新增错误；本地启动进入受影响入口；动态接管和换单核心路径走通；Git diff未超出任务范围。  
Integrator: `code_agent`

### WP-033-UPLOAD

Work Package ID: WP-033-UPLOAD  
Goal: 使用通过集成检查的同一源码制作微信包并上传开发版 `0.0.21`，说明“动态订单保护与换单”。  
Spec References: 本Task全部  
Must Preserve: 上传包必须与本地验收源码一致；保留私密配置注入边界。  
Allowed Write Paths: `.harness/qa/TASK-033/delivery/**`; 独立staging/export输出  
Forbidden / Shared Paths: 不修改当前源码；不设体验版、不提审、不发布、不提交、不推送、不部署云函数。  
Depends On: WP-033-INT完成  
Acceptance: Unity微信构建/导出成功，核对最终包包含SwapOrder与策略6身份，官方CLI开发版上传成功并执行自动预览；明确自动预览不等于手机实际收到或完成真机体验。  
Integrator: `code_agent`制作与核对包；PM执行外部上传

## Acceptance

- User-visible result: 普通局面保持D1；接近全锅危险时补单转向近期可完成目标。广告提前开锅不提前减弱保护。玩家可主动选择一张合法订单换单，并看到原食材退回暂存。
- Minimum runtime check: 热身与正式开局两锅阈值1、正常37/55节点后的阈值2/3、广告锅不提升阈值、下一层前瞻、降级候选、换单成功/取消/无目标/复核失效、计时与供给持续、奖励演示隔离。
- User review method: 本地试玩热身、两锅、正常三锅及广告提前三锅阶段，判断难度波动和换单手感。

## Result

- Status: Review
- Changed Files / Areas: Core/Contracts/Bootstrap/Replay实现策略6近期支持、正常锅阶段接管与换单事务；Presentation实现一层前瞻、选择遮罩、退回动画和新演示；WeChat库存客户端与`hotpotIngredientTrade`源码增加预留/确认/撤销协议。
- Build / Launch: `OrderSupportQa` 452项通过；云函数三组Node测试通过；Unity真实Composition诊断44项、ToolDemo 26项、视觉捕获15项通过；Windows Development Player构建并实际启动通过。独立微信staging由Unity 6000.0.26f1导出成功；最终revision-b修正文案后再次导出，官方CLI预览、`0.0.21`开发版覆盖上传与自动预览成功。
- Current Change Check: 普通补单保持D1；危险单达到正常应解锁锅数减一时接管，广告锅只计危险单不提高阈值；下一层只含移走当前项后同盘直接露出的食材。换单覆盖合法选择、取消、无目标、复核失效、0–2份退回、0.34秒完成、防重回调、计时/供给/物理持续和183份库存守恒。
- Screenshot / Artifact: `.harness/qa/TASK-033/core/`、`.harness/qa/TASK-033/visual/`、`.harness/qa/TASK-033/integration/`、`.harness/qa/TASK-033/delivery/revision-b/`；本地Player为 `.harness/qa/TASK-033/integration/player/HotpotSort.exe`。
- Known Issues: 微信开发版`0.0.21`已上传，说明“动态订单保护与换单”；官方CLI自动预览成功，但不等于手机已实际收到、打开或完成真机试玩。新增免费云库存换单依赖尚未部署的`reserveTool/commitTool/cancelTool`云函数协议，旧线上云函数下该路径可能失败；本轮未获授权部署云函数。未验证微信真机震动、真实广告或分享。
- Baseline: Not Saved
