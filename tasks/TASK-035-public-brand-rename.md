# 玩家可见品牌改名为一锅又一锅

Task ID: TASK-035  
Status: Accepted  
Updated At: 2026-09-26

## Current Change

- State: Completed；用户已于 2026-09-26 接受当前“一锅又一锅”入口视觉与品牌改名结果。
- Goal: 将微信注册名和全部玩家可见品牌从“火锅消消”统一改为“一锅又一锅”。
- Current Behavior: 入口标题、兼容界面、微信分享标题及相关字体/截图诊断仍使用“火锅消消”。
- Target Behavior: 入口与兼容界面显示“一锅又一锅”；微信分享标题显示“来《一锅又一锅》，一起开锅！”；相关字体门禁和视觉捕获使用新名称。
- Entry / Trigger: 主页入口、兼容表现路径、微信分享配置及直接相关诊断/捕获。
- Boundary / Failure / Cancel: 五字标题必须完整显示且不改变现有标题牌尺寸、位置或A+布局；正式字体缺字时只允许沿用同一字体的既有最小补字流程。
- Must Preserve: 玩法、存档、账号数据、云函数、平台接口、A+冻结画面、全部现有未提交修改及TASK-034并行工作。
- Non-goals: 不重命名仓库目录、`HotpotSort`命名空间/程序集、云函数、存档键、资源ID、分析事件、Git历史或旧Task；不Commit、Push、Merge、Tag、上传、提审或发布。

## Work Packages

### WP-035-CODE

Work Package ID: WP-035-CODE  
Goal: 修改玩家可见名称、分享文案及直接相关字体/视觉诊断，并完成最低技术检查。  
Spec References: `docs/SPEC.md#product-goal`、`docs/SPEC.md#ui-and-visual-behavior`、`docs/SPEC.md#must-preserve`。  
Must Preserve: 当前A+布局与正式资产；全部未提交修改；TASK-034的核心文件与产物；所有内部技术标识。  
Allowed Write Paths: `Unity/Assets/HotpotSort/Runtime/Presentation/GameplayView.cs`; `Unity/Assets/HotpotSort/Runtime/Presentation/GameplayViewV7.cs`; `Unity/Assets/HotpotSort/Runtime/Platform/WeChat/WeChatRuntimeConfig.cs`; `Unity/Assets/HotpotSort/Runtime/Bootstrap/Editor/Task001SmokeDiagnostic.cs`; `Unity/Assets/HotpotSort/Editor/TASK001V7/FullVisualCapture.cs`; `Unity/Assets/HotpotSort/Runtime/Presentation/Editor/Task012VisualCapture.cs`; 本Task专用直接诊断；`.harness/qa/TASK-035/**`。  
Forbidden / Shared Paths: Core、Session、Replay、Contracts、Bootstrap生产代码、云函数、正式图片资产、SPEC、其他Task、构建与发布配置。  
Depends On: None。  
Acceptance: 玩家可见源码不再包含旧名称；入口新标题无裁切；分享标题精确匹配；相关字体门禁覆盖“来《一锅又一锅》，一起开锅！”；受影响代码编译或直接诊断通过；Diff不超出允许路径。  
Integrator: code_agent。

## Result

- Status: Accepted
- Changed Files / Areas: `docs/SPEC.md`、本Task；入口与兼容标题、微信分享默认文案、字体字符集与视觉捕获/直接诊断。
- Build / Launch: Unity 编译通过；PlayMode 直接诊断通过。
- Current Change Check: current、V7、legacy 三套入口在 720×1280、1080×1920、1440×2560 共 9 个组合通过单行、完整字符与无裁切检查；默认分享标题精确匹配 `来《一锅又一锅》，一起开锅！`；生产玩家可见路径未检出旧名称。
- Screenshot / Artifact: `.harness/qa/TASK-035/entry-current-1080x1920.png`、`.harness/qa/TASK-035/result.txt`、`.harness/qa/TASK-035/unity-final.log`。
- Known Issues: 当前证据为 Unity 离屏 PlayMode 画面，不代表微信真机验证；备案截图仍需在后续真机包中重新截取。本Task未执行上传、提审或发布。
- Baseline: 2026-09-26 用户确认接受。玩家可见品牌统一为“一锅又一锅”，默认微信分享标题为“来《一锅又一锅》，一起开锅！”；现有 A+ 入口布局与玩法保持不变。该接受基于 Unity 入口预览与自动检查，微信真机截图、上传、提审和发布仍未执行。
