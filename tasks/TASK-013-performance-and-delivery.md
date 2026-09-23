# 整包性能优化与双端推送

Task ID: TASK-013  
Status: Review  
Updated At: 2026-09-23

## Requirement Delta

- Goal: 在 TASK-012 冻结版两项勘误完成后优化整包性能，上传微信小游戏开发版并推送 Git。
- Current Behavior: 当前冻结画面和热气修复已有 Windows 候选；入口删除项与玩法边缘装饰层级仍待修复，尚未针对最终候选重新执行整包性能、微信导出上传或 Git 推送。
- Target Behavior: 不改变玩家可感知画面、玩法和平台结果，达到既有性能/包体门槛；微信开发版上传成功；可复现完整候选的项目文件提交并推送到当前远端分支。
- Entry / Trigger: TASK-012 `VIS-FROZEN-ENTRY-EDGE` Completed 后启动。
- State Change: 冻结候选 → 性能优化候选 → 微信开发版已上传 → Git 已推送。
- Boundary / Failure / Cancel: 任一构建、完整性、包体、性能或上传检查失败即停止后续推送并保留证据；不得以 CLI 退出码替代结构化上传成功回执。
- Device Correction: `0.0.3` 真机反馈出现调试优化建议弹窗，并在已校验资源载入后的本地缓存写入失败时落到“食材准备未完成”。关闭正式候选的调试建议弹窗；远程资源的清单、SHA-256、CRC、bundle 和完整资产验证全部通过后，本地持久缓存落盘改为非阻断，失败时当前会话继续使用已验证内存资产、下次重新下载。
- Device Correction 2: `0.0.4` 真机仍无法进入。根因核验为原 CloudFile 所在云存储使用“仅创建者和管理员可读写”，开发机管理凭证可回下载，但小游戏真机用户无读取权限。未放宽该存储桶权限；将当前版本 bundle 部署到同环境静态托管的版本化路径，正式包改用已有 HTTPS 强校验下载路线。公网回下载后的 SHA-256 与字节数必须匹配原清单。
- Device Correction 3: `0.0.5` 真机仍无法进入。用户确认不再依赖运行时远程下载；将完整 AssetBundle 放入微信本地资源分包，启动从本地包读取并继续执行原有清单、SHA-256、CRC 与 34 项资产校验。云端历史文件保留但不参与启动；云存档、好友榜和奖励接口不变。
- Device Correction 4: `0.0.7` 已确认上传包包含完整 bundle，但真机仍落到“食材准备未完成”；截图证明 Unity 入口正常、失败仍位于资源准备阶段。取消从微信分包文件系统直接读取额外文件，改为构建时将相同 bundle 作为 Unity `TextAsset` 纳入 data 包，运行时通过 `Resources.Load` 读取，继续执行原有完整性与资产校验。
- Device Correction 5: 精确预览包在真机仍落到“食材准备未完成”。用户确认改为完整本地整包：微信正式运行时直接使用项目内现存的 Unity 原生 Resources 正式主题，彻底绕过额外 Bundle 的读取、字节复制、CRC、内存解压与激活门槛；历史 Bundle 仅保留为构建证据，不参与游戏启动。重新构建后必须在模拟器进入真实玩法，再上传新微信开发版。
- Device Performance Correction: 完整本地整包已由用户在 iPhone 14 Pro Max 确认可以进入，但玩法全程持续低帧。保持冻结视觉、逻辑坐标、点击区域和玩法结果不变；正式微信导出将 iOS 渲染倍率从设备原生 3× 限制为 2×、启用微信 iOS 高性能模式，并明确请求 60 FPS。模拟器核心路径与包体通过后上传新开发版，由用户真机确认持续帧率。
- Device Performance Correction 2: `0.0.9` 在 iPhone 14 Pro Max 仍持续掉帧。保持玩法和画面不变，移除盘面物理帧的托管数组分配和约束循环中的重复 Unity 刚体读写；使用复用缓冲在内存中完成相同顺序的最多 256 轮约束，然后单次回写和同步。
- Device Performance Correction 3: `0.0.10` 真机仍卡顿。代码核查发现倒计时 `Text` 每帧重复赋值，按钮、盘面与盘子在值未变时仍每帧写入 `RectTransform`，会在移动端持续触发 Canvas 标脏/重建。改为显示秒数或目标 Transform 实际改变时才写入，不改变显示内容、位置、动画和输入区域。
- Device Performance Correction 4: `0.0.11` 真机仍持续掉帧，且用户确认 AppID 已实际开通微信 iOS 高性能模式，排除平台能力未启用导致的普通模式回退。保持 2× 渲染倍率与冻结画面，启用 SDK EmscriptenGLX；不启用当前 Unity 6 导出链强制关闭且未经验证的渲染线程。正式候选必须确认最终框架包含 `wxwebgl/wxwebgl2` 路径且 WASM 符号表包含 `glxInit` 后才上传。

## Must Preserve

- TASK-012 冻结画面及两项勘误，不再改变资产、布局、视觉尺寸或位置。
- 当前玩法、存档、奖励、好友榜、远程资源完整性与 fallback。
- 当前工作区既有修改；不得重置、stash、覆盖或把无关文件混入提交。
- AppID、云环境、密钥、Token 与私密路径不进入 Git 或公开日志。

## Non-goals

- 新玩法、难度、视觉重设计、音频、提审、发布、自动设置体验版。
- 提交 `.harness/qa`、`Builds`、本地工具产物或无关配置修改。

## Work Packages

### CODE-PERF

Work Package ID: CODE-PERF  
Goal: 对最终冻结候选进行基线分析和整包性能优化。  
Spec References: docs/SPEC.md#platform-and-persistence；docs/SPEC.md#animation-and-vfx。  
Must Preserve: 本 Task 全部 Must Preserve。  
Allowed Write Paths: 经基线证据证明必要的 Runtime/Presentation、资源导入设置、构建配置；独立 QA 证据。  
Forbidden / Shared Paths: 正式图片内容、玩法规则、平台业务语义、私密配置。  
Depends On: TASK-012 VIS-FROZEN-ENTRY-EDGE。  
Acceptance: 三档基准平均帧率 ≥30 FPS、P95 ≤33.4 ms；主包 ≤4096 KB；编译、启动和核心路径无新增错误。  
Integrator: code_agent。

### CODE-DELIVER

Work Package ID: CODE-DELIVER  
Goal: 串行导出并上传微信小游戏开发版，随后提交并推送 Git。  
Spec References: docs/SPEC.md#platform-and-persistence。  
Must Preserve: CODE-PERF 已验证候选、凭证隔离、无提审/发布。  
Allowed Write Paths: 导出目录、上传证据、Task 结果；Git 暂存仅限复现当前完整候选所需源码、正式资产、配置、SPEC 与 Task。  
Forbidden / Shared Paths: `.harness/qa`、`Builds`、本地工具产物、无关配置、凭证；不得提交或推送失败候选。  
Depends On: CODE-PERF。  
Acceptance: 微信结构化回执成功；Git 推送到 `origin/feat/auto-push-skill` 成功并记录 commit SHA；不提审、不发布。
Integrator: code_agent。

### CODE-LOCAL-ASSETS

Work Package ID: CODE-LOCAL-ASSETS
Goal: 将正式主题完整 AssetBundle 纳入微信本地资源分包，移除进入游戏前的网络资源依赖。
Spec References: docs/SPEC.md#platform-and-persistence。
Must Preserve: 冻结画面、玩法、资源字节与完整性校验；主包低于 4096 KiB；云存档、好友榜和奖励接口不变。
Allowed Write Paths: 远程资源契约/加载适配、Bootstrap 资源配置、微信构建与导出守卫、直接诊断、SPEC 与本 Task。
Forbidden / Shared Paths: 正式 PNG/字体/主题 JSON、Core 玩法、Session 结果、云端历史文件；不得放宽校验或删除云资源。
Depends On: Device Correction 3 确认。
Acceptance: 正式包配置不包含 CloudFile/HTTPS 启动源；完整 bundle 位于本地数据分包；清单、SHA-256、CRC、34 项资产验证通过；主包 ≤4096 KiB；官方 CLI 上传开发版成功。
Integrator: code_agent。

## Result

- Status: Review
- Current Result: `CODE-PERF` Completed。最终冻结候选已完成 Unity 编译、真实 Bootstrap 入口与核心路径、三档分辨率性能、热气池与边缘装饰节点稳定性、v7 fallback 和正式微信导出检查。基线已大幅超过门槛，因此未为追求无依据的数值变化修改运行时代码、正式纹理或导入设置。
- Performance: 15 秒预热、120 秒采样；720×1280 平均 714.41 FPS / P95 9.03 ms，1080×1920 平均 586.84 FPS / P95 9.23 ms，1440×3200 平均 479.36 FPS / P95 9.52 ms。以上为 Windows RTX 5060 编辑器/运行环境证据，不替代微信真机体验。
- Package: 本地 AssetBundle 8,798,144 B 已写入 `data-package`，文件 SHA-256 `456a67af5c4f256a5eb3a660ae6476684e2c861961b700488b94c5e352e3f6a3` 与冻结清单一致；正式配置 `sourceMode=Packaged`，不含启动用 HTTPS 或 CloudFile。微信上传统计主包 2,418,676 B（2361.99 KiB），低于 4096 KiB；data 与 wasm 分包合计 20,218,683 B。
- WeChat Upload: Completed。`0.0.7` 已上传，描述“完整本地资源包启动修复”。官方 CLI 返回 `upload` 成功标记，总包 22,637,359 B、主包 2,418,676 B、data 分包 15,780,360 B、wasm 分包 4,438,323 B。`0.0.6` 上传统计未包含未识别的 `.bundle` 扩展名，已判定为无效候选；正式 `0.0.7` 改用微信实际纳入分包的资源扩展名并在上传前后均核对为 22,637,359 B。未提审、未发布、未设置体验版。
- Device Performance Result: `0.0.10` 已上传并生成精确自动预览。正式 SDK 导出完成、保存面板未改；物理专项 `V6-P01` / `V6-P02` 通过，保持速度、碰撞、分离和边界结果。官方 CLI `upload` 成功，总包 20,730,345 B、主包 2,418,619 B、data 分包 13,874,592 B、wasm 分包 4,437,134 B；未提审、未发布、未设置体验版。本地不能代替 iPhone 真机持续帧率验收。
- Device Performance Result 2: `0.0.11` 已上传并生成精确自动预览。Unity 编译、正式 SDK 完成与保存面板未改检查通过；官方 CLI `upload` 成功，总包 20,728,893 B、主包 2,418,619 B、data 分包 13,869,569 B、wasm 分包 4,440,705 B。未提审、未发布、未设置体验版；真机结果待用户确认。
- Device Performance Result 3: `0.0.12` 已上传并生成自动预览。正式产物已确认包含 `wxwebgl/wxwebgl2`、`glxInit` 与 `glxInitBufferDataAndGlState`，证明 GLX 桥接和原生符号均进入最终包；官方 CLI `upload` 成功，总包 20,834,348 B、主包 2,438,964 B、data 分包 13,868,986 B、wasm 分包 4,526,398 B。Unity 6 下 SDK 对插件 `.meta` 的重复兼容改写产生非阻断警告，但最终插件链接和导出成功。未提审、未发布、未设置体验版；真机持续帧率待用户确认。
- Git Push: 首个候选提交 `293b48b` 已推送；Device Correction 通过远程资源 8 组定向诊断与正式 SDK 导出后追加提交并推送。
- Evidence: `.harness/qa/TASK-013/result.json`；`.harness/qa/TASK-013/delivery/export-result.json`；`.harness/qa/TASK-013/delivery/preview-0.0.7.json`；`.harness/qa/TASK-013/delivery/wechat-upload-0.0.7.json`；`.harness/qa/TASK-013/export-r012/export.log`；`.harness/qa/TASK-013/delivery/wechat-upload-0.0.12.json`；`.harness/qa/TASK-013/delivery/preview-0.0.12.json`。
