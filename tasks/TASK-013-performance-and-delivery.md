# 整包性能优化与双端推送

Task ID: TASK-013  
Status: Review  
Updated At: 2026-09-23

## Requirement Delta

- Goal: 在 TASK-012 冻结版两项勘误完成后优化整包性能，上传微信小游戏开发版并推送 Git。
- Current Behavior: 当前冻结画面和热气修复已有 Windows 候选；入口删除项与玩法边缘装饰层级仍待修复，尚未针对最终候选重新执行整包性能、微信导出上传或 Git 推送。
- Target Behavior: 不改变玩家可感知画面、玩法和平台结果，达到既有性能/包体门槛；微信开发版上传成功；可复现完整候选的项目文件提交并推送到当前远端分支。
- Entry / Trigger: TASK-012 `VIS-FROZEN-ENTRY-EDGE` Completed 后启动。
- State Change: 冻结候选 → 性能优化候选 → 微信开发版已上传 → Git 已推送。
- Boundary / Failure / Cancel: 任一构建、完整性、包体、性能或上传检查失败即停止后续推送并保留证据；不得以 CLI 退出码替代结构化上传成功回执。

## Must Preserve

- TASK-012 冻结画面及两项勘误，不再改变资产、布局、视觉尺寸或位置。
- 当前玩法、存档、奖励、好友榜、远程资源完整性与 fallback。
- 当前工作区既有修改；不得重置、stash、覆盖或把无关文件混入提交。
- AppID、云环境、密钥、Token 与私密路径不进入 Git 或公开日志。

## Non-goals

- 新玩法、难度、视觉重设计、音频、提审、发布、自动设置体验版。
- 提交 `.harness/qa`、`Builds`、本地工具产物或无关配置修改。

## Work Packages

### CODE-PERF

Work Package ID: CODE-PERF  
Goal: 对最终冻结候选进行基线分析和整包性能优化。  
Spec References: docs/SPEC.md#platform-and-persistence；docs/SPEC.md#animation-and-vfx。  
Must Preserve: 本 Task 全部 Must Preserve。  
Allowed Write Paths: 经基线证据证明必要的 Runtime/Presentation、资源导入设置、构建配置；独立 QA 证据。  
Forbidden / Shared Paths: 正式图片内容、玩法规则、平台业务语义、私密配置。  
Depends On: TASK-012 VIS-FROZEN-ENTRY-EDGE。  
Acceptance: 三档基准平均帧率 ≥30 FPS、P95 ≤33.4 ms；主包 ≤4096 KB；编译、启动和核心路径无新增错误。  
Integrator: code_agent。

### CODE-DELIVER

Work Package ID: CODE-DELIVER  
Goal: 串行导出并上传微信小游戏开发版，随后提交并推送 Git。  
Spec References: docs/SPEC.md#platform-and-persistence。  
Must Preserve: CODE-PERF 已验证候选、凭证隔离、无提审/发布。  
Allowed Write Paths: 导出目录、上传证据、Task 结果；Git 暂存仅限复现当前完整候选所需源码、正式资产、配置、SPEC 与 Task。  
Forbidden / Shared Paths: `.harness/qa`、`Builds`、本地工具产物、无关配置、凭证；不得提交或推送失败候选。  
Depends On: CODE-PERF。  
Acceptance: 微信结构化回执成功；Git 推送到 `origin/feat/auto-push-skill` 成功并记录 commit SHA；不提审、不发布。
Integrator: code_agent。

## Result

- Status: Review
- Current Result: `CODE-PERF` Completed。最终冻结候选已完成 Unity 编译、真实 Bootstrap 入口与核心路径、三档分辨率性能、热气池与边缘装饰节点稳定性、v7 fallback 和正式微信导出检查。基线已大幅超过门槛，因此未为追求无依据的数值变化修改运行时代码、正式纹理或导入设置。
- Performance: 15 秒预热、120 秒采样；720×1280 平均 714.41 FPS / P95 9.03 ms，1080×1920 平均 586.84 FPS / P95 9.23 ms，1440×3200 平均 479.36 FPS / P95 9.52 ms。以上为 Windows RTX 5060 编辑器/运行环境证据，不替代微信真机体验。
- Package: 正式主包 2,056,137 B（2007.95 KiB），低于 4096 KiB；远程 bundle 未打入主包，正式云文件绑定与清单哈希一致。
- WeChat Upload: Completed。微信开发者工具官方 CLI 已上传开发版 `0.0.3`，描述“整包性能优化候选”；返回 `upload` 成功标记并生成 13,838,678 B 总包明细。未提审、未发布、未设置体验版。
- Git Push: Pending；等待本 Task 结果更新与候选文件边界检查完成后提交。
- Evidence: `.harness/qa/TASK-013/result.json`；`.harness/qa/TASK-013/delivery/export-result.json`；`.harness/qa/TASK-013/delivery/wechat-upload-0.0.3.json`。
