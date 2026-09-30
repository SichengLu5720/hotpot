# Plate Collision Settling

Task ID: TASK-038  
Status: Working  
Updated At: 2026-09-28

## Current Change

- 稳定盘群清除微小残余速度并休眠，真实扰动可再次唤醒。

## Work Package

Work Package ID: WP-038-CODE  
Goal: 消除密集碰撞挤压抽搐且保持可推动的物理手感。  
Spec References: `Must Preserve`  
Must Preserve: 零摩擦、现有弹性、半径、边界、不可重叠与点击映射。  
Allowed Write Paths: `Unity/Assets/HotpotSort/Runtime/UnityPhysics/PlatePresentationWorld.cs`; 专用物理诊断；`.harness/qa/TASK-038/**`。  
Forbidden / Shared Paths: 其他运行时代码、SPEC、其他 Task。  
Depends On: None  
Acceptance: 密集盘面稳定后无持续抽搐；新盘、打乱和校正可唤醒；无新增穿插。  
Integrator: `code_agent`

## Result

- Status: Review
- Work Package: Completed
- Commit: `4f9c224`
- Implemented: 稳定窗口后清零微小速度并休眠；新碰撞、打乱和校正可重新唤醒，保留原有零摩擦、弹性、边界与不可重叠约束。
- Checks: 专用盘面稳定诊断和最终 Unity 集成检查通过。
- User Review: 仍需在实际密集盘面中确认手感与抽搐是否完全消失。
