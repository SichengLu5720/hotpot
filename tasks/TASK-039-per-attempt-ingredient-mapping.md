# Per-attempt Ingredient Mapping

Task ID: TASK-039  
Status: Working  
Updated At: 2026-09-28

## Current Change

- 每次主页开始或重试生成新会话种子，热身与正式共用实际映射并可回放。

## Work Package

Work Package ID: WP-039-CODE  
Goal: 将日期映射改为可记录、可复现的每次挑战流程映射。  
Spec References: `Rules and Data`  
Must Preserve: 出战选择、16 位双射、核心随机流隔离与历史回放。  
Allowed Write Paths: 挑战上下文合约；`Runtime/Core/**`中映射相关文件；`Runtime/Replay/**`; 映射直接诊断。  
Forbidden / Shared Paths: 布局内容、保底算法、Bootstrap、SPEC、其他 Task。  
Depends On: None  
Acceptance: 开始与重试使用新种子；热身/正式一致；快照、恢复与回放复现；旧回放不变。  
Integrator: `code_agent`

## Result

- Status: Review
- Work Package: Completed
- Commit: `b17e9cc`
- Implemented: 挑战流程使用独立随机种子；主页开始与重试生成新种子，热身和正式共享映射；快照记录 16 种子集、映射与种子，并兼容旧回放。
- Checks: 映射、快照和回放直接检查通过；最终七日集成检查确认热身/正式映射一致。
