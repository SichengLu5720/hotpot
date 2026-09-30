# Account and Reward Foundation

Task ID: TASK-037  
Status: Working  
Updated At: 2026-09-28

## Current Change

- 奖励分享改为每日 7 次、无冷却、北京时间 06:00 重置。
- 提供连续正式失败与换单奖励库存的账号级幂等持久化接口。

## Work Package

Work Package ID: WP-037-CODE  
Goal: 冻结并实现奖励额度、正式结果与换单奖励库存公共协议。  
Spec References: `Rules and Data`; `Platform and Persistence`  
Must Preserve: 失败不伪造成功、平台回调幂等、普通分享与活动邀请独立。  
Allowed Write Paths: `Unity/Assets/HotpotSort/Contracts/**`; `Unity/Assets/HotpotSort/Runtime/Session/**`; `Unity/Assets/HotpotSort/Runtime/Platform/Profile/**`; `cloudfunctions/hotpotProfileSync/**`; 直接相关诊断。  
Forbidden / Shared Paths: Presentation、物理、布局、Bootstrap、SPEC、其他 Task、线上部署。  
Depends On: None  
Acceptance: 7 次连续分享与第 8 次拒绝、跨日重置、结果幂等、失败数与换单库存持久化协议测试通过。  
Integrator: `code_agent`

## Result

- Status: Review
- Work Package: Completed
- Commit: `e640422`
- Implemented: 每日共享奖励额度改为 7 次、无冷却、06:00 重置；新增正式结果幂等协议和可持久化换单奖励库存的授予/预留/提交/取消接口，并同步云函数源码。
- Checks: 公共奖励、档案和云端协议直接检查 58 项通过；最终集成编译与 Player 构建通过。
- Not Performed: 未部署云函数、未上传微信版本、未做真机平台回调验证。
