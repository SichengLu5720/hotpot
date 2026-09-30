# Locked Pot Share Unlock

Task ID: TASK-043  
Status: Draft  
Updated At: 2026-09-28

## Current Change

- 第三、第四锅同时提供广告与分享提前解锁。

## Work Package

Work Package ID: WP-043-VIS  
Goal: 接入锁定锅双渠道弹窗与状态反馈。  
Spec References: `Rules and Data`; `UI and Visual Behavior`  
Must Preserve: 37/55节点、冻结锅视觉、完整广告条件与迟到回调防护。  
Allowed Write Paths: 锁定锅 Presentation partial 与直接视觉诊断、`.harness/qa/TASK-043/**`。  
Forbidden / Shared Paths: 分享额度实现、物理、其他正式视觉、Bootstrap、SPEC、其他 Task。  
Depends On: TASK-037  
Acceptance: 两锅广告/分享/取消、额度耗尽回退广告、失败与重复回调不解锁。  
Integrator: `visual_agent`

## Result

- Status: Review
- Work Package: Completed
- Commit: `1ebd7a5`
- Implemented: 第三、第四锅弹窗提供广告、分享和取消；分享使用同一每日额度，额度耗尽后广告仍可用；失败、取消、不完整、重复和迟到回调不解锁。
- Checks: 锁定锅表现诊断和最终 Unity 集成检查通过。
- User Review: 微信真机广告与分享回调尚未验证。
