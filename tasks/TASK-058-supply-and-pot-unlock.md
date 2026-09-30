# Supply boundary and extra pots
Task ID: TASK-058
Status: Review

## Current Change
执行SPEC TASK-058已确认规则，交付Windows试玩。既有dirty工作保留。

## Work Packages
Work Package ID: WP-058-CORE
Goal: 新局31单开第三锅、第四锅奖励专属；版本化旧回放与保护阈值。
Spec References: SPEC TASK-058。
Must Preserve: D1/P/前十保护、热身、奖励继承、历史回放。
Allowed Write Paths: Runtime/Core/**；Runtime/Replay/DailyReplay.cs；Runtime/Bootstrap/DailyViewMapper.cs；Tests/Diagnostics~/Task058Core*；.harness/qa/TASK-058/core/**。
Forbidden / Shared Paths: 表现、奖励服务、SPEC、其他Bootstrap。
Depends On: 无。
Acceptance: 30/31/38/61边界、奖励第四锅、回放与保护阈值。
Integrator: 主Agent。

Work Package ID: WP-058-VIEW
Goal: 第三锅31进度；第四锅无自动进度，单奖励按钮分享优先、不可用时转广告。
Spec References: SPEC TASK-058。
Must Preserve: 既有锅与胶囊视觉、模态和点击安全。
Allowed Write Paths: Runtime/Presentation/GameplayViewV7.cs；Runtime/Presentation/GameplayView.cs（仅兼容无资产锅UI）；Runtime/Presentation/Editor/Task058*。
Forbidden / Shared Paths: Core、Bootstrap、契约、资产。
Depends On: Mapper第四锅threshold=0代表不自动解锁。
Acceptance: 第三锅广告专属；第四锅单奖励按钮加取消，分享优先、广告后备；无剩余订单文案与进度条。
Integrator: 主Agent。

Work Package ID: WP-058-INTEGRATION
Goal: 停止线72、奖励路由兼容、实际运行与Windows试玩。
Spec References: SPEC TASK-058。
Must Preserve: 两工作包边界及既有物理/布局。
Allowed Write Paths: Presentation/Contracts/PlateItemLayout.cs；Runtime/Session/RewardCoordinator.cs；Runtime/Platform/WeChat/WeChatRewardService.cs；奖励契约/相关本地服务必要兼容；Editor/TASK001V7/Task058*；Tests/Diagnostics~/Task058Reward*；.harness/qa/TASK-058/**；SPEC与本Task。
Forbidden / Shared Paths: 两Agent所有权文件仅待交接后集成。
Depends On: CORE、VIEW。
Acceptance: 编译、真实运行、回放、试玩构建。
Integrator: 主Agent。

## Result
三个工作包已完成，等待用户体验接受。
- 停止线偏移72；第三锅31单自动开，第四锅只奖励解锁；第四锅最终采用单奖励按钮（分享优先，否则广告）加取消。已同步SPEC与用户授权的记忆更新。
- Core 6227项检查通过：七布局完整61单、30/31/38/61边界、奖励继承、危险阈值、旧回放及实际Unity回放。奖励协调65项、微信适配80项通过；均为本地模拟，未宣称微信真机广告/分享验收。
- 实际Boot运行17项通过：新停止线、稳定2秒停止供给、新版D1、第四锅分享取消/失败不解锁、成功解锁与持久化、第三锅广告。两尺寸截图已目视检查。
- 最终UI诊断通过：31单进度、第四锅无进度/剩余文案、单按钮分享优先与广告后备、渠道不可用、取消、过期弹窗关闭。修复解锁同帧旧进度条被刷新重新激活的问题。
- Windows最终构建Succeeded，0错误、1警告；入口 `.harness/qa/TASK-058/integrated/Player/HotpotSortTask058.exe`。
- 证据位于 `.harness/qa/TASK-058/`（core、visual、view-final、integrated）。历史失败诊断保留；最终结果见view-final与build-final。
- 独立Player启动和输入初始化通过；当前机器FMOD无法初始化音频设备，仍为无声输出。后台启动检查已结束。
- 未提交、上传或发布；试玩分享/广告使用本地模拟。

