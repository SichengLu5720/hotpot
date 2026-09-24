# Press Scale

Task ID: TASK-016
Status: Review

## Current Change

- 用户要求点击放大到 1.5 倍：调整已有有效食材按压倍率 1.12 → 1.5。
- 保持 0.08 秒过渡、松手提交、取消恢复、提层、点击边界及其他动效。

## Work Packages

- Work Package ID: PRESS-SCALE
- Goal: 修改现有按压参数和直接诊断。
- Spec References: docs/SPEC.md / Press / arrival feedback
- Must Preserve: 触发及取消逻辑、物理、触觉、到达缩放。
- Allowed Write Paths: GameplayView.cs; Editor/PressHapticDiagnostic.cs（均位于 Runtime/Presentation）；.harness/qa/TASK-016/**
- Forbidden / Shared Paths: 其他实现文件；SPEC/Task 由 PM 更新。
- Depends On: None
- Acceptance: 现有按压运行诊断通过，参数达到 1.5 且取消恢复。
- Integrator: 当前执行者，单参数修改。

## Result

- 已更新倍率及对应断言；Unity 编译与隔离按压运行检查通过，PRESS_HAPTIC_RUNTIME_PASS checks=30，包含 1.5 倍及取消恢复。证据 .harness/qa/TASK-016/press-scale.log。真机手感待体验。
- 已与初速度 8 一起导出并打开开发者工具，CLI preview 成功；二维码 preview-qr.png、回执 preview-info.json 在 .harness/qa/TASK-016。未上传开发版或发布。
