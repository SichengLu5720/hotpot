# Adaptive Order Pity

Task ID: TASK-041  
Status: Draft  
Updated At: 2026-09-28

## Current Change

- 按账号连续正式失败数将近期支持概率调整为 20/30/40/50%。

## Work Package

Work Package ID: WP-041-CODE  
Goal: 实现局首冻结的失败阶梯保底与结果记录。  
Spec References: `Rules and Data`  
Must Preserve: 危险阈值、Difficulty 1、候选降级、广告锅统计与随机流隔离。  
Allowed Write Paths: `Runtime/Core/DailyDirector.cs`; 保底相关核心扩展与直接诊断。  
Forbidden / Shared Paths: Profile实现、UI、布局、Bootstrap、SPEC、其他 Task。  
Depends On: TASK-037  
Acceptance: 20/30/40/50边界、50%封顶、复活不计、最终失败幂等、通关清零与历史策略通过。  
Integrator: `code_agent`

## Result

- Status: Review
- Work Package: Completed
- Commit: `cc423a5`
- Implemented: 保底概率按连续正式失败冻结为 20/30/40/50%，50% 封顶；正式最终失败加一、通关清零，复活成功、热身失败和退出不计。
- Checks: 概率边界、分布、随机流隔离、结果幂等和历史策略直接检查 50,620 项通过；最终集成确认账户失败数在局内冻结。
