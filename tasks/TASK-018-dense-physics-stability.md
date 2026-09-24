# Dense Physics Stability

Task ID: TASK-018
Status: Review

## Current Change

- 用户最新要求删除逐帧位置修正；取消手工分离、边界夹回与配套接触速度投影，交由Unity原生碰撞处理。
- 现场日志：回退计数126→276→426→576，每3秒150次；边界误差约-0.00247。另一局25盘持续transient-geometry约150秒，盘位置基本不变而速度仍高。
- 目标：不再整盘回退；原生碰撞处理边界、接触和下落。生成/打乱/快照纠正保持。只读记录原生接触容差，检查无严重穿透及移除支撑后自然落下。
- 保留持续向下力960盘面单位/秒²、摩擦0、阻尼/弹性0.08、原生成推力、按压1.5、供给和道具语义。无自动解卡/随机补推。

## Work Packages

- Work Package ID: DENSE-STABILITY
- Goal: 修正约束实现，复现密集盘面并验证，更新诊断预览。
- Spec References: docs/SPEC.md 密集盘面约束修正
- Must Preserve: 上述物理参数、边界/半径、可见无穿透、点击及其他用户修改。
- Allowed Write Paths: Unity/Assets/HotpotSort/Runtime/UnityPhysics/PlatePresentationWorld.cs; Unity/Assets/HotpotSort/Runtime/Bootstrap/Editor/Task001V9Diagnostic.cs; .harness/qa/TASK-018/**; .harness/qa/TASK-015/export-r015/**（暂存及新包）
- Forbidden / Shared Paths: 其他源码资产，SPEC/Task仅PM更新。
- Depends On: None
- Acceptance: 22–25盘密集测试、无逐帧位置/速度投影或全局回退、堆积稳定性、移除支撑后下落、原生接触间隙/边界容差实测、暂停/供给/打乱检查；诊断开关不影响运动；导出新预览。
- Integrator: code_agent

## Result

- Completed：依用户最新指示移除逐帧位置修正、手工速度投影和全局回退，使用Unity原生接触；力学参数和显式定位保持。
- native-final.json exitCode0：22/25盘最后20秒范围/路径/反转0，回退0；诊断开关逐步一致；支撑移除下落78.08；独立空中盘、地板滑行、12盘供给/暂停/打乱通过。
- 原生接触容差：稳定盘间压入约0.5004盘面单位；动态最深盘间1.613、边界0.545（1080宽约4.15/1.40px）。不满足旧.001零穿透标准，真机可见效果待用户体验。
- 用户追加旧包日志证实：右边界4号盘误差-.001434触发反复回退，计数3941→4243，7/11号无接触盘冻结约38秒。新版本无此全局回退路径。
- 新诊断包 TASK-002-v9-export-20260924T082345-000c6ab15d1a40368dccd5385b174107 已导出并打开开发者工具；旧手工投影候选不交付。证据 .harness/qa/TASK-018/native-final.json、native-export.log、preview-activation.json。
- 未上传发布，Baseline未保存，待用户体验。
