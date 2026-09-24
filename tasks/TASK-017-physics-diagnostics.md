# Physics Diagnostics

Task ID: TASK-017
Status: Review

## Current Change

- 用户授权添加浮空诊断，保持当前持续向下力、零摩擦、0.08阻尼/弹性及按压1.5。
- 记录停滞盘位置、速度、半径、碰撞接触、底部余量，回退前失败几何及相关盘/边界；记录速度清零来源以区别打乱/纠正/回退。
- 限频且有界；暂停不累计停滞；不采集用户身份，不改变运动。
- 新预览启用诊断，普通生产默认不启用；日志前缀 HOTPOT_PHYSICS_DIAG。

## Work Packages

- Work Package ID: PHYSICS-DIAG
- Goal: 实现可观察的只读物理诊断并生成预览。
- Spec References: docs/SPEC.md 浮空排查诊断
- Must Preserve: 所有物理参数/解算/玩法/视觉及现有未提交修改。
- Allowed Write Paths: Unity/Assets/HotpotSort/Runtime/UnityPhysics/PlatePresentationWorld.cs; Unity/Assets/HotpotSort/Runtime/Bootstrap/Editor/Task001V9Diagnostic.cs; .harness/qa/TASK-017/**; .harness/qa/TASK-015/export-r015/**（仅暂存诊断源码及生成新包）
- Forbidden / Shared Paths: 其他源码与资产；SPEC/Task 由 PM 更新。
- Depends On: None
- Acceptance: 编译与物理诊断通过，实际捕获停滞/回退等记录，限频和暂停有效；新预览可输出日志。
- Integrator: code_agent

## Result

- Completed：结构化诊断包含停滞/接触/清零来源、回退前最小间隙盘对和边界，以及轮转停滞见证盘。
- 2秒低位移触发，全局3秒限频；每条最多6盘/每盘8接触，最近32条内存缓存。普通源码默认关闭，仅本次预览暂存启用；最终IL2CPP确认开关为true。
- Unity编译与诊断通过；6500步开关诊断位置速度一致，暂停/会话重置、接触、32条容量与限频通过；12盘2200步物理检查通过。回退记录用合成输入验证，真实浮空原因尚未确认。
- 证据 .harness/qa/TASK-017/physics.json、unity.log、export.log、preview-activation.json。SDK导出成功，开发者工具已打开诊断包。
- 复现后保留控制台，筛选 HOTPOT_PHYSICS_DIAG，复制或保存完整JSON；GetPhysicsDiagnosticSnapshot()提供最近32条。不写入本地持久日志，刷新/新局会丢失内存记录。未上传发布，待现场复现。
