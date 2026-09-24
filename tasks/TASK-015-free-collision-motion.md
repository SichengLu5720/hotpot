# Restore Downward Force

Task ID: TASK-015
Status: Review

## Current Change

- 用户确认恢复持续向下力，参数参考 TASK-014 零摩擦版。
- 摩擦 0、阻尼 0.08、弹性 0.08；每物理帧向下力 960 × 0.01 × mass。
- 新刚体一次 AddForce((0,+5), Force)，替换固定初速度 8；暂停及重复快照不重复施加生成推力。
- 保留点击放大 1.5 倍、边界、分离、供给、暂停、打乱及纠正语义；不增加自动解卡。

## Work Packages

- Work Package ID: RESTORE-FORCE
- Goal: 恢复物理参数并生成开发者工具预览。
- Spec References: docs/SPEC.md / World / Board / Level
- Must Preserve: 零摩擦、点击倍率1.5、碰撞分离、供给边界及其他用户修改。
- Allowed Write Paths: Unity/Assets/HotpotSort/Runtime/UnityPhysics/PlatePresentationWorld.cs; Unity/Assets/HotpotSort/Runtime/Bootstrap/Editor/Task001V9Diagnostic.cs; .harness/qa/TASK-015/**
- Forbidden / Shared Paths: 其他源码资产；SPEC/Task 由 PM 更新。
- Depends On: None
- Acceptance: 连续向下加速、暂停保持、纠正归零后恢复下落；12盘/2200步供给边界检查；编译导出并打开预览。
- Integrator: code_agent

## Result

- Completed：物理源码与 TASK-014 零摩擦版哈希一致，保留按压 1.5 倍。
- Unity 编译与隔离 PlayMode 通过；physics-restored.json exitCode 0，持续加速、暂停/快照保持、纠正归零后恢复下落、12盘/2200步供给边界及打乱检查通过。
- 新包 20260924T070013-485cad98a43b409098a3cb528a6fc088 导出成功；CLI open/preview 成功。证据 .harness/qa/TASK-015/preview-restored-info.json；二维码 preview-restored-qr.png；导出日志 export-r015/export.log。
- 旧预览保留；未上传或发布，真机手感待用户体验。
