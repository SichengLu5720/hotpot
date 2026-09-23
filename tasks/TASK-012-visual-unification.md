# 整体视觉统一升级

Task ID: TASK-012  
Status: Review  
Updated At: 2026-09-23

## Requirement Delta

- Goal: 将当前 r017 从功能完整但存在拼装感的画面，升级为统一、精致、有食欲的克制国潮视觉。
- Current Behavior: 入口页写实感较强；玩法内白色通用卡片、锅盘食材、木桌和工具栏缺少统一资产语言，顶部拥挤，盘量变化时中下部易出现空洞或机械网格感。
- Target Behavior: 采用精致 2.5D 插画覆盖全部玩家可见页面和关键动效；先完成 A/B 预览并由用户选定，再生产正式资产和接入 Unity。
- Entry / Trigger: 启动、加载、首页进入对局，以及暂停、设置、奖励、好友榜、胜负、复活和重试。
- State Change: r017 当前候选 → A/B 预览待选择 → 选中方向的正式候选 → 用户体验接受。
- Boundary / Failure / Cancel: 预览未选定前不实施正式资产或 Unity 接入；加载失败、资源缺失、好友榜空态/失败、奖励不可用均需统一表现；动效暂停时冻结，退出、重试和会话切换时安全取消。

## Spec References

- docs/SPEC.md#ui-and-visual-behavior
- docs/SPEC.md#ui-style
- docs/SPEC.md#animation-and-vfx

## Must Preserve

- 玩法、订单、库存、奖励和平台结果。
- 订单—暂存—盘面三段结构、物理坐标、点击边界和遮挡规则。
- 16 类食材身份和轮廓语义。
- 食材入锅和入暂存均为直线；盘面直接入锅及入暂存约 0.34 秒，暂存自动匹配入锅约 0.46 秒。
- 首次有效操作启动倒计时、微信胶囊安全区、远程资源链与 4096 KB 主包门禁。
- 当前工作区全部既有修改；不得覆盖、回滚、自动提交、上传或发布。

## Non-goals

- 新玩法、难度、关卡、音乐音效、广告奖励规则、云存档或好友数据逻辑。
- 微信上传、提审与发布。
- 复制其他游戏的布局、品牌或具体资产。

## Current Change

- 2026-09-23 用户确认当前 A+「沉浸餐馆」正式画面版本已经合适并冻结。此前未被接受的 `edge_cloth_r2` 实验与 Windows r004 视口适配均已停止，不得恢复；入口、玩法构图、边框图、HUD、锅盘食材、色彩、材质和资产均不得继续调整或替换。
- 热气稳定性修复已完成。当前冻结版勘误仅包含两项：删除入口“每日挑战”牌和“北京时间 06:00 更新”；开始游戏后将左右装饰的完整图片图层置于全部玩法层之下并保证整图展示。除图层父级、Sibling 顺序、裁切归属及删除两处入口节点外，不调整任何图片、尺寸、位置、透明度或其他布局。

## Work Packages

### VIS-PREVIEW

Work Package ID: VIS-PREVIEW  
Goal: 产出可直接比较的 A/B 高保真拥挤玩法预览。  
Spec References: UI and Visual Behavior；UI Style。  
Must Preserve: 相同玩法状态、四锅、五暂存、密集盘面、16 类食材身份；两版不得混合。  
Allowed Write Paths: `.harness/previews/TASK-012/r001/`。  
Forbidden / Shared Paths: `Unity/`、`docs/`、其他 Task、正式资源目录。  
Depends On: None。  
Acceptance: 两张 1080×1920 图片内联展示；用户明确选定 A 或 B。  
Integrator: visual_agent。

### CODE-THEME

Work Package ID: CODE-THEME  
Goal: 建立 `Hotpot/TASK001/v10/r001` 版本化主题接口并保留 v7 fallback。  
Spec References: UI and Visual Behavior；Platform and Persistence。  
Must Preserve: 所有玩法和平台公共接口、远程资源完整性、旧根目录可回退。  
Allowed Write Paths: `Runtime/Presentation` 的主题加载与验证、独立 Editor 诊断。  
Forbidden / Shared Paths: 正式视觉资产、Core、Session、Platform 业务逻辑、共享 Bootstrap。  
Depends On: 用户选定 A 或 B。  
Acceptance: 新旧根均可验证加载，缺失新主题时不切换正式根。  
Integrator: code_agent。

### VIS-FORMAL

Work Package ID: VIS-FORMAL  
Goal: 按选中方向生产全套正式资产、界面表现和动效绑定。  
Spec References: UI and Visual Behavior；Art Bible；Animation and VFX。  
Must Preserve: Must Preserve 全部条目以及未选方向不混入正式版本。  
Allowed Write Paths: 新 v10 资源根、Presentation 表现文件、TASK-012 预览与视觉证据。  
Forbidden / Shared Paths: Core、Session、Platform 业务逻辑、共享 Bootstrap。  
Depends On: CODE-THEME；用户选定 A 或 B。  
Acceptance: 全玩家可见状态统一，关键动效符合冻结语义，三档分辨率可读。  
Integrator: visual_agent。

为满足单包约 30 分钟限制，`VIS-FORMAL` 拆为以下互不重叠的正式资产包；均完成后由原 `visual_agent` 作为 Visual Lead 串行执行 `VIS-INTEGRATE`：

- `VIS-ASSETS-SCENE`：只写 v10 `background/`、`pots/`、`containers/`、`ui/` 及独立生产证据；负责木桌边缘画框、铜锅红汤、白瓷盘碟和 HUD 基础皮肤。
- `VIS-ASSETS-FOOD`：只写 v10 `food/` 及独立生产证据；负责 16 类透明食材，不带盘、文字、背景或整体投影。
- `VIS-ASSETS-STATE`：只写 v10 `icons/`、`hero/`、`fx/`、`brand/` 及独立生产证据；负责入口/胜负/分享、状态图标和关键特效。
- `VIS-INTEGRATE`：在三包完成后独占 v10 `presentation-theme.json`、Presentation 视觉绑定、预览与 QA 证据；不得重新设计已完成资产，只做统一、接入和运行时调优。

### CODE-INTEGRATE

Work Package ID: CODE-INTEGRATE  
Goal: 串行接入选中主题、构建并执行最低相关检查。  
Spec References: UI and Visual Behavior；Platform and Persistence。  
Must Preserve: 视觉决定、旧 fallback、包体和远程资源门禁。  
Allowed Write Paths: 共享 Bootstrap/资源配置、构建与定向诊断；`Boot.unity` 仅允许修改序列化 `approvedAssetRoot` 单字段，不得改变场景层级、对象或构图。  
Forbidden / Shared Paths: 不得重新设计或覆盖视觉资产。  
Depends On: CODE-THEME；VIS-FORMAL。  
Acceptance: 编译、启动、状态矩阵、动效、三档分辨率、30 FPS/P95 33.4 ms、主包低于 4096 KB；不上传。  
Integrator: code_agent。

### CODE-REMOTE-COMPAT

Work Package ID: CODE-REMOTE-COMPAT  
Goal: 移除远程清单对旧主题 31 项的硬编码，使 v10 的 34 项远程资源仍按可信主题集合严格验证。  
Spec References: UI and Visual Behavior；Platform and Persistence。  
Must Preserve: 清单非空、数量与可信 `ExpectedAssets` 完全相等，并继续逐地址、类型、哈希、版本和签名验证；不得放宽下载或完整性规则。  
Allowed Write Paths: `Runtime/Platform/RemoteAssets/RemoteAssetValidation.cs` 及直接定向诊断。  
Forbidden / Shared Paths: 传输、缓存、身份、玩法、正式资产与上传配置。  
Depends On: CODE-INTEGRATE 的 v10 可信资产集合。  
Acceptance: 旧 v7 31 项与新 v10 34 项合法清单均通过，各类缺失、重复、额外或篡改清单继续失败。  
Integrator: code_agent。

### VIS-START-TRANSITION-PREVIEW

Work Package ID: VIS-START-TRANSITION-PREVIEW  
Goal: 产出一张四帧分镜，预览 2 秒不可跳过的开始游戏过场。  
Spec References: docs/SPEC.md#ui-and-visual-behavior。  
Must Preserve: A+ 沉浸餐馆方向、真实竖屏构图、现有玩法盘面身份；退出和重试不触发，不改变资源门禁与倒计时规则。  
Allowed Write Paths: `.harness/previews/TASK-012/r002-start-transition/`。  
Forbidden / Shared Paths: `Unity/`、正式资源目录、Core、Session、Platform、Bootstrap。  
Depends On: 用户已确认 2 秒、不可跳过、四帧节奏与触发规则。  
Acceptance: 一张可内联查看的四帧高保真分镜图，由用户判断节奏、镜头与风格。  
Integrator: visual_agent。

### CODE-STEAM-STABILITY

Work Package ID: CODE-STEAM-STABILITY  
Goal: 修复已开启火锅环境热气循环中的周期性闪烁，不改变冻结画面版本。  
Spec References: docs/SPEC.md#animation-and-vfx。  
Must Preserve: 冻结版本全部视觉资产与布局；热气位置、尺寸、整体透明度方向、上升方向；锁定锅无热气；暂停与会话清理语义。  
Allowed Write Paths: `Runtime/Presentation/GameplayFeedback.cs`、`Runtime/Presentation/Contracts/PresentationLifecycle.cs`、必要的定向诊断与 QA 证据；只有经诊断确有必要时才可微调 v10 `presentation-theme.json` 的热气时间参数。  
Forbidden / Shared Paths: 入口、边框、HUD、锅盘食材与其他正式资产；Core、Session、Platform、Bootstrap、Boot Scene；不得恢复 `edge_cloth_r2` 或 Windows r004 视口适配。  
Depends On: 当前冻结版本。  
Acceptance: 四个已开启锅连续运行时热气无周期性消失或亮度突跳；低频辅助效果不造成闪烁误读；锁定锅无热气；暂停冻结、恢复无突发补播，退出/重试/终局/会话切换无残影；编译和受影响入口启动通过。  
Integrator: code_agent。

### VIS-FROZEN-ENTRY-EDGE

Work Package ID: VIS-FROZEN-ENTRY-EDGE  
Goal: 完成冻结版两项勘误：删除入口两处模块，并修正玩法内左右完整装饰图的层级/裁切。  
Spec References: docs/SPEC.md#ui-and-visual-behavior。  
Must Preserve: 当前全部正式图片、图片尺寸与位置、入口其余内容、玩法层级内部关系、点击与物理。  
Allowed Write Paths: `Runtime/Presentation/GameplayViewTheme.cs`、`Runtime/Presentation/GameplayViewV7.cs`、必要的 Presentation 定向诊断与 QA 证据。  
Forbidden / Shared Paths: 正式 PNG/字体/主题 JSON、Core、Session、Platform、Bootstrap、Boot Scene；不得恢复 R2 边框或 Windows 视口适配。  
Depends On: CODE-STEAM-STABILITY。  
Acceptance: 入口两处目标模块不存在且其余内容不变；三档竖屏玩法中左右装饰完整图片位于全部玩法层下、无裁切、无射线、无玩法遮挡；编译与受影响入口启动通过。  
Integrator: visual_agent。

## Acceptance

- Preview gate: 用户在内联 A/B 预览中选定一个方向。
- Runtime gate: Unity 实际运行覆盖普通、拥挤、四锅、暂存、遮挡、弹窗及异常态。
- WeChat gate: 开发者工具检查安全区、可读性、点击一致性及明显性能/包体回退；不上传。
- Final review: 用户体验完整候选并决定是否接受；接受后保存 Baseline。

## Result

- Status: Review
- Current Result: 当前 A+「沉浸餐馆」画面版本已由用户确认冻结。`CODE-STEAM-STABILITY` Completed：消除特效实例创建时首帧全亮，环境热气改为半生命周期交叠的连续包络。`VIS-FROZEN-ENTRY-EDGE` Completed：入口“每日挑战”与“北京时间 06:00 更新”已删除；玩法左右装饰改为同一张完整图片并置于全部玩法层下，未改图片、尺寸、位置、透明度或其他布局。
- Build / Package: 正式微信 SDK 转换完成；主包 2,056,137 B（2007.95 KiB），低于 4096 KiB。新远程 bundle 8,798,144 B 已部署，并以回下载后的 SHA-256 与字节数完成一致性验证。
- Candidate: `.harness/qa/TASK-012/desktop-r004/HotpotTask012.exe`；只在冻结画面对应的 `desktop-r003` 源状态上增加热气稳定性修复。
- Evidence: `.harness/qa/TASK-012/frozen-entry-edge-r002/`；`.harness/qa/TASK-012/steam-stability-final/steam-checks.txt`；`.harness/qa/TASK-013/result.json`。
- Steam Checks: Unity 编译和 PlayMode 定向诊断通过；四锅连续 60 秒叠加 alpha 为 0.15746–0.16254，最大逐帧变化 0.000162；暂停 60 秒冻结、恢复无补播，锁定锅及 Entry/Aborted/Won/Overflow、新会话和解绑清理共 9 项通过。真实 Bootstrap 入口、开局、首次有效点击计时、暂停恢复、重试退出通过；`desktop-r004` 构建成功并完成 720×1280 启动日志检查。
- Pending Human Check: 冻结版勘误、正式导出与开发版上传已完成，等待用户体验最终候选。
- Preview: `.harness/previews/TASK-012/r001/A-immersive-restaurant-1080x1920.png`；`.harness/previews/TASK-012/r001/B-minimal-oriental-1080x1920.png`。
- Start Transition Preview: `VIS-START-TRANSITION-PREVIEW` Completed；生成 1536×1024 四帧分镜 `.harness/previews/TASK-012/r002-start-transition/start-transition-storyboard-v1.png`，当前仅待用户判断，未制作正式资产、未接入 Unity。
- Selected Reference: `.harness/previews/TASK-012/r001/selected-immersive-restaurant-reference.png`。
- Baseline: 2026-09-23 用户冻结当前 A+「沉浸餐馆」画面；热气稳定性修复为不改变该画面的技术补丁，动态效果仍待用户体验接受。
