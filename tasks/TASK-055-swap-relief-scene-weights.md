# Swap Relief and Scene Weights
Task ID: TASK-055
Status: Review

## Current Change
用户明确指定互换解压单与场面单权重，并按已讨论方案将K从3改为4，交付本机试玩。

## Work Packages
Work Package ID: WP-055-CODE
Goal: 新成本版本K=4，20行权重前两列对调，保留旧版回放。
Spec References: SPEC TASK-055。
Must Preserve: 推进三列、阶段/暂存边界、首两单、热身、保护触发及降级、物理与生成、无关修改。
Allowed Write Paths: Runtime/Core/DailyDirector.cs; DailyCommands.cs; DailySession.cs; DailySessionSwapOrder.cs; Runtime/Replay/DailyReplay.cs; Tests/Diagnostics~/Task055*; Tests/Diagnostics~/Task054OrderCostQa.cs（显式指定历史版本1）; Editor/TASK001V7/Task055*; docs/SPEC.md; 本Task; .harness/qa/TASK-055/**。
Forbidden / Shared Paths: 其他产品文件及资产。
Depends On: TASK-054。
Acceptance: 20行前两列交换、推进列不变、K边界、新旧版本回放、运行补单、构建启动。
Integrator: 主Agent。

## Result
已实装全部20行前两列交换与K=4，以成本版本2记录，版本1仍可回放。156项新版检查、108项历史成本及回放检查通过；Unity运行9项断言通过，真实补单事件显示权重[70,0,30,0,0]，过滤后[0,0,30,0,0]并抽中近推进类别3。两尺寸截图目视检查未见明显新增异常。构建成功，0错误0警告；相关git diff --check通过。
证据：.harness/qa/TASK-055/。试玩：integrated/Player/HotpotSortTask044.exe。等待用户试玩难度接受，未上传/发布/提交，未打ZIP。
独立Player启动通过，未见脚本异常；本机音频设备缺失，仍使用无声回退。
