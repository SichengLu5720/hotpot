# Visible Supply and Fill Redeal

Task ID: TASK-051
Status: Review

## Current Change

按用户确认，将普通供给门槛与出生位置绑定可见上沿附近；打乱改为安全并行小批次补满，取消固定24盘。不改D1。

## Work Packages

Work Package ID: WP-051-CODE
Goal: 共用中心高度门槛、无重叠并行补满与试玩集成。
Spec References: SPEC 2026-09-29供给与打乱更新。
Must Preserve: 现有未提交修改、队列/组合/物理参数、奖励与目标保护、暂停/前后台、D1。
Allowed Write Paths: Runtime/Presentation/Contracts/PlateItemLayout.cs; PresentationPort.cs; GameplayView.cs; Runtime/UnityPhysics/PlatePresentationWorld.cs; Runtime/Bootstrap/DailyProductionComposition.cs; DailyProductionRedeal.cs; Runtime/Core/DailySessionShuffle.cs; TASK051诊断与meta; .harness/qa/TASK-051/**; Builds/TASK051*。
Forbidden / Shared Paths: 其他玩法和表现；SPEC与Task由当前PM串行更新。
Depends On: 现有049/050与工作区基线。
Acceptance: Unity编译、真实运行与多尺寸截图；严格中心门槛、同批并行安全、填满/临时阻塞、队列耗尽、目标保护、暂停恢复；Windows试玩。
Integrator: PM负责代码侧串行集成，visual_agent负责表现运行验证；Unity进程单一所有者。

Work Package ID: WP-051-VIS-QA
Goal: 定位普通填满等待超时并验证安全并行落盘和实际画面。
Spec References: 同上。
Must Preserve: 产品逻辑、物理参数和其他修改。
Allowed Write Paths: Editor/TASK001V7/Task051SupplyDiagnostic.cs及meta；.harness/qa/TASK-051/visual/**。
Forbidden / Shared Paths: 所有Runtime、SPEC、Task；问题报PM，不自行改产品逻辑。
Depends On: WP-051-CODE运行候选。
Acceptance: 普通供给与打乱核心路径、暂停/恢复/队列耗尽及多尺寸截图。
Integrator: PM。

## Result

- WP-051-CODE / WP-051-VIS-QA: Completed；等待玩家体验，未保存Accepted Baseline。
- 普通供给使用可见上沿减24的严格中心门槛、安全出生空间与缓存的单次横向偏移；出生美术外缘距可见上沿8单位。保留物理参数。
- 打乱每批最多3盘、批前检查门槛、同批逐盘检查空间；取消固定24盘，队列耗尽或稳定入口满时完成。只读0.5秒位置稳定观察不改变速度/物理。
- 核心守恒、订单/暂存保持、随机流隔离和动态/历史回放427项通过。Unity实际运行18项通过：含热身队列耗尽、严格中心门槛边界、出生位置、同帧多盘、安全间隙、临时堵塞、暂停/计时冻结、可点目标和输入恢复。本次几何实际填满20盘，不是固定20盘。
- visual_agent及PM检查1080×1920、1080×1440截图，未见新增画面异常。恢复供给检查使用人工释放空间，只证明供给响应，真实玩家消除后的体验仍待验收。
- 首次普通满盘超时源于测试只认高度阻塞，漏掉安全出生空间不足；实测确认后修正。早期日志与位置证据保留，最终runtime-v3.log无脚本异常。
- Windows构建Succeeded，0错误、1条现有FindObjectOfType过时警告。Direct3D启动无脚本异常；检查环境无可用音频设备，不宣称声音已验证。压缩包CRC与程序集指纹核对通过，构建期间本次Runtime源码指纹未变化。
- 证据 `.harness/qa/TASK-051/`；试玩包 `Builds/TASK051-HotpotSort-20260929.zip`（171,463,664字节）。包含现有049/050及工作区基线；本轮未实施新版D1。未Commit/Push/上传/发布。
