# Swap Introduction and Retained Credit

Task ID: TASK-042  
Status: Draft  
Updated At: 2026-09-28

## Current Change

- 换单先介绍与领取，奖励形成库存，成功换单后才扣除。

## Work Package

Work Package ID: WP-042-VIS  
Goal: 完成换单介绍、领取、选择、取消保留和视觉反馈。  
Spec References: `UI and Visual Behavior`  
Must Preserve: A+冻结视觉、计时/供给/物理继续、合法目标复核、0.34秒退回动画。  
Allowed Write Paths: 换单 Presentation partial、换单视觉诊断、`.harness/qa/TASK-042/**`。  
Forbidden / Shared Paths: Profile、核心事务、Bootstrap、正式资产、SPEC、其他 Task。  
Depends On: TASK-037  
Acceptance: 库存/分享/广告入口、取消保留、目标失效、成功扣除和旧回调均通过。  
Integrator: `visual_agent`

## Result

- Status: Review
- Work Package: Completed
- Commit: `2de0e54`
- Implemented: 换单先进入介绍窗，提供库存、分享、广告和取消入口；奖励成功后进入选单，取消或目标失效保留库存，仅成功事务扣除。
- Checks: 核心换单事务、账户库存、共享绑定编译和最终 Player 构建通过。
- Known Issue: 独立 Editor 诊断因 EditMode 视图未完成 `Awake` 初始化而空引用退出；未确认是运行时问题。
- User Review: 需在 Player 中实际确认介绍、领取、取消保留、目标失效与成功扣除的完整交互。
