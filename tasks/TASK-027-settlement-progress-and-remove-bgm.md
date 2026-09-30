# 结算进度动画与当前 BGM 移除

Status: Review

## Current Change

State: r3 implemented and awaiting visual acceptance (2026-09-25)

- Goal: 所有局内终局先播放本局订单进度动画，再显示结算按钮；同时从项目与构建输入中删除当前 BGM，保留音乐设置接口及现有效果/环境音。
- Spec References: `docs/SPEC.md` 的 Current Product Direction、Win / Lose、UX and Input Principles、Confirmed Audio Direction。
- Current Behavior: 结算按钮立即显示；胜利与失败结算信息量不一致；当前 `Music.ogg` 作为循环 BGM 加载。
- Target Behavior: 胜利、暂存满和时间到结算均显示从 0% 到最终进度的约 1.5 秒慢—快—慢动画；动画前不显示按钮，完成后显示现有按钮；删除当前 BGM 资源但保留音乐设置 UI、音量与存档字段。
- Failure Copy: 暂存满与时间到的结算标题均统一为“失败”；下方原因说明继续按失败原因区分。
- Current Change r2: 胜利和失败统一改为压暗真实盘面的无白卡轻结算层；显示标题、结果说明、带移动火锅标识的进度条、五项现有统计，动画完成后显示“重新挑战 / 分享 / 返回首页”。普通分享携带胜负与最终百分比，复用现有主题图，不进入奖励、不发奖、不耗额度，结束后留在结算页。
- Current Change r3: 用户在 r2 Accepted Baseline 后局部解冻结算进度条填充配色。复用 TASK-026 已确认 A「强旺火」固定全宽渐变 `#781C14 → #B52618 → #E43B1F → #FF7626`，按实际进度裁切、不拉伸；未完成轨道、小火锅标识、动画、布局、数据和按钮规则保持不变。
- r3 Alignment Repair: 用户指出 100% 时小火锅标识越过进度条右端；修复为标识中心限制在左右各半个图标宽度的安全范围内，使 0% 左边缘、100% 右边缘分别与轨道边缘对齐，中间进度继续跟随填充前端。
- Boundary / Failure / Cancel: 重复终局快照不重播；后台冻结后续播；退出、重试、新会话取消；百分比限制 0–100%，胜利固定 100%；不新增奖励节点或局外系统。
- Must Preserve: A+「沉浸餐馆」结算视觉、现有结算统计、玩法结果、重试/退出语义、店内远景、汤锅沸腾、操作/完成/胜负音效、音乐设置接口及所有 TASK-026 未提交修改。
- Non-goals: 新 BGM、活动奖励、上传、推送、提审、发布。

## Work Packages

### WP-027-CODE

Work Package ID: WP-027-CODE
Goal: 提供稳定的本局总订单/最终进度数据，删除当前 BGM 资源并调整音频加载与定向检查，使缺少 BGM 成为合法状态。
Spec References: 本 Task Current Change；SPEC Win / Lose、Confirmed Audio Direction。
Must Preserve: 核心结算、存档、奖励、环境声、音效、音乐设置字段和 TASK-026 现有修改。
Allowed Write Paths: `Unity/Assets/HotpotSort/Runtime/Bootstrap/DailyViewMapper.cs`; `Unity/Assets/HotpotSort/Runtime/Presentation/PresentationPort.cs`; `Unity/Assets/HotpotSort/Runtime/Audio/GameplayAudio.cs`; `Unity/Assets/HotpotSort/Runtime/Presentation/Editor/AudioRuntimeDiagnostic.cs`; `Unity/Assets/HotpotSort/Resources/HotpotSort/Audio/TASK009/Music.ogg`; `Unity/Assets/HotpotSort/Resources/HotpotSort/Audio/TASK009/Music.ogg.meta`; 本任务专用代码诊断文件。
Forbidden / Shared Paths: `GameplayView.cs`; `GameplayViewV7.cs`; 其他核心、平台、存档、资产与文档。
Depends On: None.
Acceptance: 终局快照能给出本局总订单与完成数；音乐资源不再存在或进入加载；没有 BGM 时环境声和全部单次音效仍可工作；音乐设置数据结构不变。
Integrator: No.

### WP-027-VISUAL

Work Package ID: WP-027-VISUAL
Goal: 在冻结 A+ 结算卡中实现进度条动画、按钮延迟显示、生命周期取消，并产出真实运行预览图。
Spec References: 本 Task Current Change；SPEC UX and Input Principles、Confirmed Audio Direction。
Must Preserve: 现有结算标题、统计、按钮动作、A+ 视觉及 TASK-026 同文件修改。
Allowed Write Paths: `Unity/Assets/HotpotSort/Runtime/Presentation/GameplayView.cs`; `Unity/Assets/HotpotSort/Runtime/Presentation/GameplayViewV7.cs`; `Unity/Assets/HotpotSort/Runtime/Presentation/Editor/Task027SettlementProgressCapture.cs`; `.harness/previews/TASK-027/`; 本任务专用表现诊断文件。
Forbidden / Shared Paths: Core、Bootstrap、Contracts、Platform、Replay、音频资源、SPEC 与 Task。
Depends On: WP-027-CODE 的稳定快照字段；在该字段落地前可先使用既有 `completedOrders` 与约定字段名实施表现结构，但不得自行修改共享接口。
Acceptance: 三类终局均从 0% 播放至正确终点；约 1.5 秒慢—快—慢；动画前按钮不存在/不可见，完成后出现；不可跳过；后台冻结恢复、重复快照、退出/重试/新会话安全；输出至少一张 1080×1920 或等比例真实运行预览图。
Integrator: Yes（仅表现文件）。

## Result

- User-visible result: 三类终局均已加入约 1.5 秒慢—快—慢进度动画；动画期间不显示结算按钮，完成后显示。胜利固定 100%，失败使用实际完成订单比例；两类失败标题统一为“失败”，原因说明继续区分。当前 BGM 资源及 Unity meta 已删除；音乐设置接口保留，环境声、操作及胜负音效保留。
- Changed Files / Areas: `DailyViewMapper.cs`; `PresentationPort.cs`; `GameplayAudio.cs`; `GameplayView.cs`; `GameplayViewV7.cs`; `AudioRuntimeDiagnostic.cs`; 本任务专用进度诊断/捕获；删除 `Music.ogg` 与 `.meta`；`.harness/previews/TASK-027/`。
- Minimum checks actually run: `LockedPotQa` 49 项通过；`Task027ProgressQa` 覆盖 0/61、23/61、Timeout；真实 Boot 音频诊断完成并验证 11 个保留音频、环境连续播放、后台/奖励暂停恢复、独立设置与终局队列；Unity PlayMode 表现诊断覆盖三种终局、中途/最终进度、按钮隐藏/出现、重复快照、后台续播、新会话取消和零分母，日志标记 `TASK027_SETTLEMENT_CAPTURE_PASS`，无 C# 编译错误或未处理异常；定向 `git diff --check` 通过。
- Preview: `.harness/previews/TASK-027/{overflow,timeout,won}-{mid,final}.png`，六张 1080×1920 Unity 运行截图。
- Boundary: IMPLEMENTED / 本地自动范围 VERIFIED；未进行微信真机试听、自然通关或用户视觉接受；未 Commit、Push、Upload、Review 或 Release。
- r2 User-visible result: 胜利、暂存满失败、时间到失败已统一为压暗真实盘面的无白卡轻结算层；显示胜负标题、对应说明、带 A+ 火锅标识的进度动画和五项现有统计。动画结束后显示“重新挑战 / 分享 / 返回首页”。普通分享文案携带胜负与整数百分比并复用现有分享图，与奖励分享/额度隔离；本地分享模拟关闭后恢复原结算且不重播动画。
- r2 Minimum checks actually run: `Task027ShareQa` 18 项通过；既有 `WeChatRewardQa` 76 项通过；Unity 编译及 62 项 PlayMode 表现断言通过，覆盖三类终局、无白卡、五项统计、火锅标识动画与 0/100% 边界、按钮延迟、普通分享事件与返回结算、重复快照/后台/新会话生命周期；`.harness/previews/TASK-027/unity-r2.log` 标记 `TASK027_SETTLEMENT_CAPTURE_PASS`；定向 `git diff --check` 通过。
- r2 Preview: `.harness/previews/TASK-027/{overflow,timeout,won}-{mid,final}.png`，六张 1080×1920 Unity 运行截图。
- r2 Boundary: IMPLEMENTED / 本地自动范围 VERIFIED；未进行微信真机普通分享或自然通关验证，普通分享发起成功不代表用户实际发送；HUMAN-ACCEPTED / UPLOADED / RELEASED 均未完成。
- r3 User-visible result: 结算进度条填充已复用 TASK-026 A「强旺火」四段渐变，图形保持固定全宽并按实际进度从左向右裁切；小火锅继续跟随裁切前端，米金轨道与其余 r2 Baseline 均未改变。
- r3 Minimum checks actually run: Unity 编译及 89 项表现断言通过，覆盖 0/中途/最终进度的固定全宽、水平裁切、四个精确 HEX 色标、移动火锅、既有动画与分享返回；`.harness/previews/TASK-027/unity-r3.log` 标记 `TASK027_SETTLEMENT_CAPTURE_PASS`；定向 `git diff --check` 通过。
- r3 Preview: `.harness/previews/TASK-027/overflow-mid.png`; `overflow-final.png`; `won-final.png` 已更新为真实 Unity 运行截图。
- r3 Boundary: IMPLEMENTED / 本地自动范围 VERIFIED；等待用户对渐变实际观感确认，未做微信真机、上传或发布。
- r3 Alignment Repair Result: 小火锅改为中心锚点并按实际轨道宽度与图标半宽限制移动范围；0% 左边缘、100% 右边缘分别与轨道边缘对齐。Unity 编译与 93 项断言通过，包含世界坐标端点对齐；`unity-r3-alignment.log` 与更新后的 `won-final.png` 为证据。等待用户确认修复后的对齐观感。
- Accepted Baseline: 2026-09-25 用户确认接受当前 Unity 预览中的统一轻结算层、移动火锅进度标识、五项统计、三按钮布局与普通分享规则。该确认属于视觉/交互方案的人审接受；微信真机分享、自然通关、上传和发布仍未执行。

## Work Packages r2

### WP-027-SHARE

Work Package ID: WP-027-SHARE
Goal: 为结算普通分享提供胜负与最终百分比文案，复用现有主题图并与奖励分享完全隔离。
Spec References: Current Change r2；SPEC Platform Constraints。
Must Preserve: 奖励分享额度、奖励请求、普通分享无可靠发送成功证明、当前存档和平台配置。
Allowed Write Paths: `Unity/Assets/HotpotSort/Contracts/DevelopmentServices.cs`; `Unity/Assets/HotpotSort/Runtime/Bootstrap/DailyProductionComposition.cs`; `Unity/Assets/HotpotSort/Runtime/Bootstrap/LocalDevelopmentServices.cs`; `Unity/Assets/HotpotSort/Runtime/Platform/WeChat/WeChatShareService.cs`; 本任务专用普通分享诊断/fixture。
Forbidden / Shared Paths: `GameplayView.cs`; `GameplayViewV7.cs`; Core、RewardCoordinator、Profile、资源、SPEC 与 Task。
Depends On: None.
Acceptance: 胜利/失败分享文案包含结果与整数百分比；复用现有分享图；不创建奖励请求、不扣额度；失败/不可用不改终局；返回仍在结算。
Integrator: No.

### WP-027-VISUAL-R2

Work Package ID: WP-027-VISUAL-R2
Goal: 实现胜负统一轻结算布局、五项统计、移动火锅标识和三个结算按钮，并更新真实 Unity 预览。
Spec References: Current Change r2；SPEC UI and Visual Behavior。
Must Preserve: 1.5 秒动画及生命周期、A+ 正式资产、按钮动作、TASK-026 同文件修改、当前无 BGM 状态。
Allowed Write Paths: `Unity/Assets/HotpotSort/Runtime/Presentation/GameplayView.cs`; `Unity/Assets/HotpotSort/Runtime/Presentation/GameplayViewV7.cs`; `Unity/Assets/HotpotSort/Runtime/Presentation/Editor/Task027SettlementProgressCapture.cs`; `.harness/previews/TASK-027/`。
Forbidden / Shared Paths: Contracts、Bootstrap、Platform、Core、音频资源、SPEC 与 Task。
Depends On: WP-027-SHARE 保持现有 `ShareRequested` 表现接口可用；可先完成布局，最终检查时使用已落地普通分享。
Acceptance: 三类终局无白卡并压暗盘面；标题/说明/五项统计清晰；火锅标识跟随进度且边界不裁切；动画前无按钮，完成后出现重试/分享/返回首页；三类 1080×1920 真实 Unity 预览无重叠裁切。
Integrator: Yes（仅表现文件）。

## Work Package r3

Work Package ID: WP-027-GRADIENT
Goal: 将结算进度条填充替换为已确认的固定全宽“强旺火”渐变并按进度裁切。
Spec References: Current Change r3；SPEC UI and Visual Behavior。
Must Preserve: r2 Accepted Baseline 的布局、轨道、移动火锅标识、动画时序、五项统计、三按钮、普通分享和所有 TASK-026 同文件修改。
Allowed Write Paths: `Unity/Assets/HotpotSort/Runtime/Presentation/GameplayView.cs`; `Unity/Assets/HotpotSort/Runtime/Presentation/GameplayViewV7.cs`; `Unity/Assets/HotpotSort/Runtime/Presentation/Editor/Task027SettlementProgressCapture.cs`; `.harness/previews/TASK-027/`。
Forbidden / Shared Paths: Core、Bootstrap、Contracts、Platform、音频、正式图片资产、SPEC 与 Task。
Depends On: r2 Accepted Baseline；TASK-026 已确认 A 配色事实。
Acceptance: 0/中途/最终进度均使用固定全宽四段渐变并按进度裁切，不随当前填充宽度重映射；小火锅与裁切前端同步；胜负预览无裁切、重叠或其他视觉变化。
Integrator: Yes（仅表现文件）。
