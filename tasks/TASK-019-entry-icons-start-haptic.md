# 入口图标与开始震动

Task ID: TASK-019
Status: Accepted
Updated At: 2026-09-24

## Current Change

- State: Completed；用户已于 2026-09-24 接受当前入口视觉。
- Goal: 优化开始界面底部操作区，将设置与好友榜入口改成图标加下方文字，并在有效开始点击时提供一次微信轻震。
- Current Behavior: “开始下火锅”下方为“设置”“好友榜”两个纯文字按钮；入口开始点击没有专属震动。
- Target Behavior: 设置使用齿轮图标并显示“设置”；好友榜使用奖杯/好友排行语义图标并显示“排行榜”，功能仍进入现有好友榜；有效开始点击被接受并实际发起流程时在微信真机轻震一次。
- Entry / Trigger: ViewPhase.Entry 的底部操作区；设置、排行榜及开始按钮点击。
- Boundary / Failure / Cancel: 开始按钮不可用、重复点击未被接受、取消或失败重试不震动；设置与排行榜不震动；编辑器和 Windows 静默跳过。
- Confirmed Size Adjustment: 用户在首版截图后确认放大两个入口图标；圆底由 40×40 调整为 52×52，内部图形由 24×24 调整为 32×32，文字字号保持 18。放大版出现文字与底部铜边穿插后，用户确认底部木质操作面板仅向下延长 16 个逻辑单位；面板顶边、开始按钮与其他入口内容不移动，图标和文字完整位于扩展后的面板内，点击区域只增不减。

## Spec References

- `docs/SPEC.md#ui-and-visual-behavior`
- `docs/SPEC.md#animation-and-vfx`

## Must Preserve

- 当前 A+「沉浸餐馆」入口其余构图、正式图片、标题、火锅、食材、开始按钮视觉、文字与位置。
- 现有设置与微信好友榜功能、资源准备门禁、重复开始防护、开始过场约定、玩法与平台结果。
- 微信胶囊安全区；图标化后实际点击区域不得缩小。
- 工作区全部既有修改，尤其 `GameplayView.cs` 与 `PressHapticDiagnostic.cs` 中已存在的按压放大调整，不得覆盖或回滚。
- 不 Commit、Push、Merge、Tag、上传、提审或发布。

## Work Packages

### CODE-START-HAPTIC

Work Package ID: CODE-START-HAPTIC
Goal: 在有效入口开始操作被接受并实际发起开始流程时发出一次轻震请求，沿用现有微信震动桥接并阻止无效或重复请求。
Spec References: UI and Visual Behavior；Animation and VFX。
Must Preserve: 现有食材/到达/完成订单震动语义、资源门禁、重复开始防护、编辑器与 Windows 静默行为；保留相关文件现有未提交修改。
Allowed Write Paths: `Unity/Assets/HotpotSort/Runtime/Presentation/GameplayView.cs`、`Unity/Assets/HotpotSort/Runtime/Bootstrap/Bootstrap.cs`、`Unity/Assets/HotpotSort/Runtime/Bootstrap/WeChatGameplayTapInput.cs`、新建或现有的直接定向 Editor 诊断。
Forbidden / Shared Paths: `GameplayViewV7.cs`、正式资源、Core、Session、其他 Platform、Scene、SPEC、其他 Task。
Depends On: None。
Acceptance: 一次有效开始仅发出一次 Light；按钮不可用、忙碌状态、重复请求、设置、排行榜、取消和失败重试不新增震动；微信桥接沿用 `WX.VibrateShort`，非微信目标静默；编译与定向诊断通过。
Integrator: code_agent。

### VIS-ENTRY-ICONS

Work Package ID: VIS-ENTRY-ICONS
Goal: 将入口的设置和好友榜纯文字按钮改成并排的图标加下方文字按钮。
Spec References: UI and Visual Behavior。
Must Preserve: A+ 沉浸餐馆视觉、开始按钮、入口其余画面和现有功能；点击区域不得缩小。
Allowed Write Paths: `Unity/Assets/HotpotSort/Runtime/Presentation/GameplayViewV7.cs`、必要的 Presentation 定向视觉诊断和 `.harness/qa/TASK-019/` 证据；优先复用现有 `icons/settings.png`、`icons/friends.png` 与 `ui/icon_disc.png`，确有必要时仅新增 TASK-019 专用入口图标资产及 `.meta`。
Forbidden / Shared Paths: `GameplayView.cs`、Bootstrap、Core、Session、Platform、Scene、其他正式图片与主题配置、SPEC、其他 Task。
Depends On: CODE-START-HAPTIC 的稳定接口不要求视觉文件改动；可在其完成后串行实施。
Acceptance: 设置显示齿轮图标和下方“设置”；好友榜显示奖杯/好友排行语义图标和下方“排行榜”；三档竖屏无裁切、清晰且间距协调，点击仍进入原功能，实际点击区域不小于当前次级按钮。
Integrator: visual_agent。

### CODE-INTEGRATE

Work Package ID: CODE-INTEGRATE
Goal: 在视觉包完成后执行不改变视觉决定的共享技术检查。
Spec References: 本 Task 全部。
Must Preserve: 两个已完成工作包的行为与视觉决定；全部既有工作区修改。
Allowed Write Paths: 仅在发现由本 Task 引入的编译或接口错误时，串行修改上述两个工作包已授权代码路径及直接诊断。
Forbidden / Shared Paths: 正式资产重设计、Core、Session、其他 Platform、Scene、SPEC、其他 Task。
Depends On: CODE-START-HAPTIC；VIS-ENTRY-ICONS。
Acceptance: 编译、真实入口启动、设置/排行榜/开始核心路径、三档竖屏和 Git Diff 范围完成最低检查；如无法执行微信真机则明确保留真机震感待验。
Integrator: code_agent。

## Result

- Status: Accepted
- Current Result: `CODE-START-HAPTIC`、`VIS-ENTRY-ICONS` 与 `CODE-INTEGRATE` 均 Completed。入口圆底为 52×52、内部图形为 32×32，文字保持 18；底部木质面板仅向下延长 16 个逻辑单位，图标与文字已和底部铜边分离并完整位于面板内。面板顶边、开始按钮、其他入口内容未移动，按钮点击区保持 133×70。有效开始通过既有门禁、首次实际发起流程时请求一次 Light，重复、忙碌、取消和失败重试路径不新增震动。
- Minimum Checks: Unity 编译和真实 Boot 入口通过；开始轻震定向诊断 12 项通过；最终图标尺寸、逻辑字号、回调、射线、点击区域和面板边界定向诊断 16 项通过；720×1280、1080×1920、1440×3200 三档截图重新生成并检查；相关 Diff 检查通过。
- Evidence: `.harness/qa/TASK-019/entry-720x1280.png`、`entry-1080x1920.png`、`entry-1440x3200.png`、`settings.png`、`friends.png`。
- Known Boundary: 未执行微信真机震感与真实好友数据验证；未提交、上传、提审或发布。
- Human Check: 用户已接受入口图标、尺寸与底部面板视觉；微信真机开始轻震手感仍属于未执行的设备验证。
- Baseline: 2026-09-24 用户确认接受。入口使用 52×52 铜边圆底、32×32 图形和 18 号下方文字“设置”“排行榜”；按钮点击区 133×70；底部木质面板只向下延长 16 个逻辑单位，顶边与开始按钮不移动。有效开始通过既有门禁后请求一次 Light，重复、忙碌、取消和失败重试不新增震动。
