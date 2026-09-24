# Difficulty 1 With First 15 Relief

Task ID: TASK-025
Status: Review

## Current Change

- 用户确认新局基础表Difficulty1；只保留完成数<15时的可完成保护池等概率选单，完成>=15全部基础算法。撤除新局80/20及暂存分组权重。
- 无可靠候选回基础算法；原预留/去重/暂存/近似成本、供给、动画、物理、点击优化和历史回放保持。

## Work Packages

- Work Package ID: DIFFICULTY1-FIRST15
- Goal: 新内容身份与策略切换，保留可验证的旧内容回放路径。
- Spec References: docs/SPEC.md 基础选单/前15单/回放段。
- Must Preserve: 开局、库存、缓存语义、动画、物理、历史策略1/2/3及Difficulty3回放、并行用户修改。
- Allowed Write Paths: Unity/Assets/HotpotSort/Runtime/Core/**; Unity/Assets/HotpotSort/Runtime/Replay/**; Unity/Assets/HotpotSort/Runtime/Bootstrap/DailyProductionComposition.cs; Unity/Assets/HotpotSort/Runtime/Bootstrap/Editor/*OrderRelief*; Unity/Assets/HotpotSort/Runtime/Presentation/Diagnostics~/*OrderRelief*; Unity/Assets/HotpotSort/Content/Daily/**; .harness/qa/TASK-025/**。
- Forbidden / Shared Paths: 其他代码和资产，SPEC/Task归PM；共享stage/export未经协调不得写。
- 必要依赖扩权：Unity/Assets/HotpotSort/Editor/ContentImport/DailyContentImporter.cs、ImportCli.cs；仅导入Difficulty1及记录正确内容身份，不更改其他构建门禁或Boot布局。
- Depends On: TASK024源代码；与TASK023共享构建协调。
- Acceptance: Difficulty1全部20行精确一致；14/15和同事务切换；新策略均匀抽样且后期无保护RNG；空池/未知回基础；新旧内容及策略回放；编译Boot。
- Integrator: code_agent，最终共享导出由协调后的唯一责任人执行。

## Result

- 已实现Difficulty1内容版本hotpot_daily_task025_difficulty1_v1及策略4：前15单等概率保护，之后完全基础算法。旧策略代码仅为历史回放保留。
- 核心20961项、实际Boot13项通过；Difficulty1原表20行逐项一致，14/15、均匀抽样、后期基础RNG、新旧内容及无观察/策略1/2/3历史hash验证通过，ContentBuildGuard通过。
- 新canonical digest e4f20831a07608f6c298e41217309471e11dbdc14a5367fe716a23d315961904。保持原资源路径/GUID，旧Difficulty3另按原字节归档。
- 证据 .harness/qa/TASK-025/difficulty1-first15/。共享staging未写；已移交TASK023唯一Integrator统一构建。未真机验证；本任务未提交、上传或发布。
