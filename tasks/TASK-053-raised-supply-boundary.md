# Raise Supply Stop Boundary
Task ID: TASK-053
Status: Review

## Current Change
用户确认顶部基本铺满，允许少量盘子露出约四分之一盘直径；仅上移共享停止生成线。视觉比例为调试目标，不是物理逐盘裁切约束。

## Work Packages
Work Package ID: WP-053-CODE
Goal: 上移停止线并运行检查、更新本机试玩。
Spec References: SPEC TASK-053。
Must Preserve: 出生位置、每帧最多一盘、安全空间、物理、D1和其他脏修改。
Allowed Write Paths: Runtime/Presentation/Contracts/PlateItemLayout.cs; Editor/TASK001V7/Task053SupplyDiagnostic.cs及meta; .harness/qa/TASK-053/**; docs/SPEC.md; 本Task。
Forbidden / Shared Paths: 其他产品代码。
Depends On: TASK-052。
Acceptance: 两尺寸真实运行截图，稳定后无持续生成，编译和本机启动。
Integrator: 主Agent。

## Result
共享停止线偏移24→48，其他产品逻辑不变。运行12项检查通过，稳定后两秒未继续供给；1080×1920及1080×1440截图已目视检查，顶部有部分盘子裁切，露出比例随物理堆叠变化。Windows构建成功，0错误、1警告。证据：.harness/qa/TASK-053/visual/runtime-result.json及截图、integrated/player-build-result.json。试玩：.harness/qa/TASK-053/integrated/Player/HotpotSortTask044.exe。等待用户体验接受，未提交、上传或发布。
独立Player启动完成，日志未见脚本异常；本机无音频设备，使用无声回退。
