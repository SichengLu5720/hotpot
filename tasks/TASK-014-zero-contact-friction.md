# Zero Contact Friction

Task ID: TASK-014
Status: Review
Updated At: 2026-09-24

## Requirement Delta

- Goal: 去除盘面全部实体接触摩擦。
- Current Behavior: 盘—盘、盘—侧墙和盘—底板共用摩擦系数 `0.08`。
- Target Behavior: 上述接触统一使用摩擦系数 `0`。
- Entry / Trigger: 进入玩法并创建盘面物理世界时生效。
- State Change: 仅移除接触摩擦；不移除刚体自身的线性阻尼。
- Boundary / Failure / Cancel: 无运行时开关；新局、重试和重新进入均使用零摩擦。

## Spec References

- `docs/SPEC.md#world--board--level`

## Must Preserve

- `linearDamping=0.08`、`bounciness=0.08`。
- 持续向下力、碰撞分离、盘面边界、供给、点击与玩法规则。
- 当前冻结视觉与全部无关用户修改。

## Non-goals

- 不调整阻尼、弹性、质量、速度、重力、碰撞半径或视觉表现。
- 不增加设置开关，不修改平台、存档、奖励或发布流程。

## Current Change

- 将 `PlatePresentationWorld` 共享接触材质的摩擦系数改为 `0`，同步更新直接断言。

## Work Packages

| ID | Agent | Goal | Allowed Write Paths | Forbidden / Shared Paths | Depends On | Status | Integrator |
|---|---|---|---|---|---|---|---|
| WP-01 | `code_agent` | 实施零接触摩擦并更新直接诊断 | `Unity/Assets/HotpotSort/Runtime/UnityPhysics/PlatePresentationWorld.cs`; `Unity/Assets/HotpotSort/Runtime/Bootstrap/Editor/Task001V9Diagnostic.cs` | `docs/SPEC.md`; `tasks/**`; 其他代码、资产与配置 | None | Completed | Same Agent |

## Acceptance

- User-visible result: 所有盘面实体接触不再产生摩擦，其他已确认物理参数和玩法保持不变。
- Minimum runtime check: 相关代码可编译；共享材质 `friction=0`、`linearDamping=0.08`、`bounciness=0.08` 的直接诊断通过；检查 diff 未越界。
- User review method: 用户在实际玩法中体验盘子碰撞与滑动手感后决定是否接受。

## Result

- Status: Review
- Changed Files / Areas: `PlatePresentationWorld` 共享接触材质；`Task001V9Diagnostic` 物理参数断言及无参反射选择；相关 SPEC。
- Build / Launch: Unity 6000.0.26f1 BatchMode 编译成功；隔离 V9 物理诊断退出码 `0`，日志包含 `V9_PHYSICS_PASS`。
- Current Change Check: 运行时共享材质 `friction=0`，直接断言同时确认 `linearDamping=0.08`、`bounciness=0.08`；12 盘、2200 步碰撞/边界检查通过。
- Development Upload: 微信开发版 `0.0.12` 已上传，说明为“去除盘面摩擦”；未提审、未发布、未设置体验版。
- Screenshot / Artifact: `.harness/qa/TASK-014/zero-friction-r002.json`、`.harness/qa/TASK-014/zero-friction-r002.log`（本地验证证据）；`.harness/qa/TASK-014/export-r014/export.log`（导出证据）；`.harness/qa/TASK-014/delivery/wechat-upload-0.0.12-zero-friction.json`（上传回执）。
- Known Issues: 首次运行暴露既有诊断对同名重载方法选择不明确，已限定为无参方法后复跑通过；尚未由用户实际体验手感。
- Baseline: Not Saved
