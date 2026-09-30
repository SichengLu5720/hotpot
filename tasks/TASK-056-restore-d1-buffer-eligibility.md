# Restore D1 With Buffer Eligibility Fix
Task ID: TASK-056
Status: Review

## Current Change
恢复原D1权重、active口径、非目标食材/3成本、暂存边界及兜底；只修复暂存单资格。保留当前前十单保护、失败补偿与供给。

## Work Packages
Work Package ID: WP-056-CODE
Goal: 成本版本3恢复D1，只在V<3且V+B>=3时判暂存单。
Spec References: SPEC TASK-056。
Must Preserve: 两个开局订单、阶段/暂存分类、保护触发及降级、热身、生成物理、无关修改。
Allowed Write Paths: Runtime/Core/DailyDirector.cs; DailyCommands.cs; DailySession.cs; Runtime/Replay/DailyReplay.cs; Tests/Diagnostics~/Task055OrderCostQa.cs（冻结版本2）; Tests/Diagnostics~/Task056*; Editor/TASK001V7/Task056*; docs/SPEC.md; 本Task; .harness/qa/TASK-056/**。
Forbidden / Shared Paths: 其他产品文件与资产。
Depends On: TASK-055。
Acceptance: 全20行原权重，暂存资格与不足场景，原距离/压力，旧版本回放，新版运行补单及试玩构建。
Integrator: 主Agent。

## Result
已恢复全部20行D1原权重、active口径、非目标数/3成本与原边界；当前成本版本3仅修改暂存资格并扣除已有暂存目标的扫描缺口。版本1/2保留供历史回放。219项针对性/版本3回放检查通过，156项版本2历史检查通过；Unity真实启动和补单9项断言通过，事件algorithm=director_d1_buffer_ready_v3，前期空暂存权重为[0,70,30,0,0]。两尺寸截图目视无明显新增异常；构建0错误0警告，相关git diff --check通过。
证据：.harness/qa/TASK-056/；试玩入口integrated/Player/HotpotSortTask044.exe。等待用户体验判断，未Commit/Push/上传/发布，未打ZIP。
独立Player启动通过，未见脚本异常；本机无音频设备，使用无声回退。
