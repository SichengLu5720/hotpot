# Task: 微信小游戏开发版导出与上传

Task ID: TASK-002  
Task Version: 5  
Status: Verified  
Type: Platform  
Risk: High  
Build Mode: Code Only  
Art Gate: Not Required  
Experience Gate: Human Check  
Created By: PM Orchestrator  
Created At: 2026-09-21

---

# Workflow Control

Current Stage: HC-04 Final Experience — Accepted  
Last Accepted Checkpoint: HC-04-r1 Developer Tools Visibility  
Pending Human Check: None  
Next Allowed Action: 本Task已完成；审核、发布、体验版激活仍需新的明确授权。  
Rollback Target: HC-02-r4  
Paused Workstreams: 审核提交、发布与平台生产配置改写。  
Unaffected Workstreams: TASK-001 v5固定C与普通盘供给已Verified；其余项目工作不因本Task自动停止。

## Checkpoint Ledger

| Checkpoint | Stage | Task Version | Artifact / Evidence | Status | Human Decision | Rollback Target | Invalidates | Next Allowed Action |
|---|---|---:|---|---|---|---|---|---|
| HC-01 | Requirement Freeze | 1 | 本Task Frozen Requirement | Accepted | 用户“接受”（2026-09-21） | Draft | None | 只读调查导出阻塞与HC-02方案 |
| HC-02 | Visual / Plan Confirmation | 1 | 平台导出实施计划、QA Intent与无视觉变更说明 | Accepted | 用户“确认”（2026-09-21） | HC-01 | None | 进入导出修复、构建及开发版上传 |
| HC-01-r2 | Requirement Freeze Addendum | 2 | 临时导出包AppID与资源加载边界 | Accepted | 用户“确认”（2026-09-21） | HC-01 | v1中AppID仅进程内、资源策略未定的约束 | 仅本次导出目录允许持久身份与资源引用 |
| HC-02-r2 | Temporary Export Configuration | 2 | r001导出证据与用户确认的最小重导出方案 | Accepted | 用户“确认”（2026-09-21） | HC-02 | r001不完整导出包不可上传 | 恢复Builder重导出、检查并上传 |
| HC-01-r3 | Requirement Freeze Addendum | 3 | 最小上传链路验证包范围与正式资源隔离边界 | Accepted | 用户“没问题 实行”（2026-09-21） | HC-01-r2 | v2要求本次上传包携带完整正式游戏资源 | 仅构建不含完整正式资源的上传验证包 |
| HC-02-r3 | Minimal Upload Package Plan | 3 | 保留启动入口和必要画面、排除正式字体及完整美术的独立导出方案 | Accepted | 用户“没问题 实行”（2026-09-21） | HC-02-r2 | r002超限包 | 恢复Builder实施、验证并上传开发版 |
| HC-01-r4 | Target Identity Replacement | 4 | 当前登录账号可管理的小游戏身份核验 | Accepted | 用户提供新AppID（2026-09-21），官方本地接口确认属于小游戏 | HC-01-r3 | v3普通小程序身份 | 复用已验证最小游戏包 |
| HC-02-r4 | Mini Game Identity Retry Plan | 4 | r003合格包、新小游戏身份、独立重试证据边界 | Accepted | 用户按PM请求提供小游戏AppID（2026-09-21） | HC-02-r3 | r003失败上传尝试 | 恢复Builder执行一次独立开发版上传重试 |
| HC-03-r1 | Implementation Result | 5 | r005零插件原生小游戏空壳与官方上传成功回执 | Accepted | 用户“能通过开发者工具查看即可”（2026-09-21） | HC-02-r4 | None | 在本机开发者工具打开正确项目并核对启动画面 |
| HC-04-r1 | Final Experience | 5 | 微信开发者工具窗口`HotpotUploadFlow`与模拟器启动画面 | Accepted | 用户确认开发者工具可查看即为验收口径；PM已在前台打开并观察到“启动成功”、版本0.0.1（2026-09-21） | HC-03-r1 | 原AC-E-01网页平台可见性要求 | Task Verified；不进入审核或发布 |

## Agent Session Registry

| Role | Exact Agent Name | Dispatch Mode | Thread / Session ID | Status | Task Version | Last Input Checkpoint | Resume / Replacement Note |
|---|---|---|---|---|---:|---|---| 
| Feature Design | `feature_designer` | Not Started | Not Started | Not Started | 1 | None | |
| Code Build | `code_builder` | Compatibility Prompt | `/root/code_builder_task002_wechat` | Completed | 5 | HC-02-r4 | r005官方上传成功；开发者工具本地可见性已完成HC-04验收 |

---

# Clarifications and Decisions

## Requirement Interview Summary

- Original request: 用户要求“上传至微信开发者平台”，随后在对话中提供目标AppID。
- Current logical position / entry state: 本机微信开发者工具已登录；AppID仅作为本次导出进程环境变量使用，不写入项目配置、Task或日志。项目当前没有可上传的微信小游戏导出目录，`build/wechat`仅含脚本诊断输出。
- Confirmed target: 跑通从Unity导出到微信开发者工具上传的最小开发版流程，使目标AppID在开发者平台可见一条上传记录；不追求设备体验、审核质量或发布质量。
- Known export blocker: 当前完整WebGL脚本编译在未修改的微信插件处报`CS0103 WebGLInput`；先前未建立该项基线，不能标注为确认既有问题。
- Must remain unchanged: `MiniGameConfig.asset`当前普通面板值，尤其`UseFriendRelation: 0`；AppID与Node路径保持环境变量/运行时输入，不固化到资产或日志；固定C内容版本和既有玩法不随导出变更。
- Acceptance intent: 产生可由微信开发者工具识别的开发版包，工具上传返回成功信息和平台可见记录；不将成功上传视为审核、发布、完整构建、设备或玩家体验验收。
- User confirmed complete understanding: Yes，待HC-01接受。

| ID | Question / Unclear Item | Confirmed Decision | Confirmed By | Task Version Impact | Affected Work |
|---|---|---|---|---|---|
| CL-001 | AppID | 用户已在对话中提供目标AppID；仅本次进程环境变量使用，不写入仓库记录 | User | v1 | 导出与开发版上传 |
| CL-002 | 上传类型 | 开发版上传；不提交审核或正式发布 | User | v1 | CLI上传 |
| CL-003 | 版本号与上传说明 | `0.0.1` / `流程验证包`；只表达平台链路验证，不宣称玩法或设备验收 | PM依据用户“目标是跑通流程，让我在开发者平台可见即可”设定的低风险上传元数据 | v1 | 微信开发者工具上传元数据 |
| CL-004 | WebGL编译错误修复范围 | 先只读定位，再在最小必要范围修复并验证完整微信导出；不得上传未完成诊断输出 | User目标与既有错误证据 | v1 | 平台插件、导出与构建 |
| CL-005 | 临时AppID持久化 | 允许将目标AppID仅写入本次唯一`minigame/project.config.json`等开发者工具必需的导出包身份文件；不得写入Unity源工程、Task、证据日志或其他配置 | User“确认” | v2 | 导出包与CLI上传 |
| CL-006 | 临时资源加载配置 | 允许仅在本次导出过程中选择能让小游戏包正确引用约48MB data的资源加载/分包配置；完成后恢复Unity源工程面板和SDK自动改写，最终包须通过引用完整性检查 | User“确认” | v2 | Unity导出与小游戏输出 |
| CL-007 | SDK容量超限后的路线 | 采用仅验证上传链路的最小包：保留可识别启动入口与必要画面，不携带完整正式字体和正式美术；正式Skeleton C工程资源不得删除、降质或被此包替代 | User“没问题 实行” | v3 | 独立导出入口、导出检查与开发版上传 |
| CL-008 | 目标AppID的平台类型 | 微信开发者工具官方本地接口确认：目标身份存在于当前登录用户可管理的普通小程序列表，不存在于小游戏列表；因此小游戏包上传返回`800059 /app.json not found` | PM本机只读核验 | Pending | 开发版上传路线 |
| CL-009 | 替换目标小游戏身份 | 用户提供新的目标AppID；微信开发者工具官方本地接口确认其存在于当前登录用户可管理的小游戏列表 | User + PM本机只读核验 | v4 | 生成包身份与开发版上传 |
| CL-010 | “可见”的最终验收位置 | 能通过本机微信开发者工具打开并看到流程验证包即可；不再要求用户到公众平台网页确认开发版本记录 | User“能通过开发者工具查看即可” | v5 | AC-E-01与最终Human Check |

未解决的阻断问题：None。

---

<!-- FROZEN_START -->

# Frozen Requirement

# Frozen Requirement

## Original Request

上传至微信开发者平台；目标是跑通流程，让开发者平台可见即可。

## Goal and User Outcome

为已提供的目标AppID导出一个微信小游戏开发版，通过已登录的本机微信开发者工具上传，并能在本机微信开发者工具中打开和看到版本 `0.0.1`、说明“流程验证包”的启动画面。

## Core Rules and Confirmed Decisions

- AppID只从本次命令进程环境取得；依据CL-005，仅允许写入本次唯一导出目录内开发者工具必需的项目身份文件，不写入Unity源工程、Task、证据日志或其他配置。
- 只上传开发版；不提交审核、不发布、不启用真实云数据、广告、好友关系或平台生产功能。
- 保留MiniGameConfig当前普通面板值，包括`UseFriendRelation: 0`；不因上传修改它。
- 先通过完整微信导出和最终输出检查，确认SDK导出完成且data被小游戏包正确引用后才调用上传；不把脚本诊断目录、webgl中间目录或r001不完整包上传。
- WebGLInput已证实为旧诊断未指定WebGL目标；正确目标下11个程序集编译通过，不修改SDK输入代码。
- 临时资源加载配置只作用于本次导出，完成后恢复源工程普通面板和SDK自动改写；最终差异检查必须证明源工程未残留AppID或临时配置。
- 本次开发版使用独立的最小上传链路验证包，只保留微信开发者工具可识别的启动入口与必要画面，不携带完整正式字体和正式美术；不得删除、覆盖、降质或改绑正式Skeleton C内容。
- 最小包仅证明导出与上传链路，平台记录不得解释为正式游戏内容、目标设备体验或玩法验收。

## Scope

- 定位并修复导出所必需的WebGL编译阻塞。
- Unity微信小游戏导出、导出物检查、本机微信开发者工具开发版上传及最终日志核对。
- 为本次上传建立可回退、与正式内容隔离的最小构建入口和必要占位画面。
- 记录实际命令、导出目录身份、上传结果与未验证边界。

## Non-goals

- 审核提交、体验版/正式版发布、微信真机验收、云数据、广告、好友关系、玩法或美术改动。
- 以成功上传声称完整构建、设备运行或玩家体验通过。
- 在本次上传包中交付或验证完整Skeleton C玩法、字体、美术和正式资源质量。

## Acceptance Criteria

| ID | Type | Criterion | Verification Owner | Required Evidence |
|---|---|---|---|---|
| AC-F-01 | Functional | 本机微信开发者工具接受生成的项目目录并开发版上传成功，版本为0.0.1、说明为流程验证包 | Builder / PM | CLI信息输出与最终上传日志 |
| AC-T-01 | Technical | Unity完整微信导出完成，导出目录含可识别项目配置，SDK导出完成信号出现；AppID未写入仓库 | Builder | Unity导出日志、目录检查、Git diff |
| AC-T-02 | Technical | `WebGLInput`阻塞得到最小修复并通过本次导出所需编译 | Builder | 修改前后日志与定向编译/导出结果 |
| AC-E-01 | Experiential | 本机微信开发者工具可打开本次流程验证包，模拟器显示启动成功与版本0.0.1 | PM / Human | 开发者工具窗口与模拟器画面 |

## Test Proxies

| Proxy ID | Proxy | Supports Criterion | Limitation |
|---|---|---|---|
| TP-001 | CLI上传成功信息 | AC-F-01 | 不替代用户在平台中的可见性确认 |
| TP-002 | Unity导出和SDK完成日志 | AC-T-01 | 不替代微信真机运行或体验验收 |

<!-- FROZEN_END -->

# Build and Verification Results

## r001结果

- 正确指定WebGL目标后，Unity 6000.0.26f1编译通过，11个程序集，exit 0；未修改SDK键盘输入代码。
- 完整SDK导出通过，Unity exit 0，`export.log`包含`[Converter] All done!`与`SDK_COMPLETE`；`MiniGameConfig.asset`字节和SHA256前后一致，`UseFriendRelation: 0`保持。SDK自动改写的ProjectSettings和插件meta已恢复。
- 当前导出包不上传：DevTools后端只从磁盘项目配置读取AppID，空值直接拒绝，CLI在指定`--project`时忽略`--appid`；因此“AppID只在进程内”与当前工具上传要求冲突。
- 当前面板`assetLoadType=0`、CDN为空，导出的48,287,702字节data文件只在`webgl`中间目录；`minigame`包配置为`loadDataPackageFromSubpackage:false`且`DATA_CDN:''`，未引用该数据，不能当作完整可用包上传。
- 未执行上传、审核、体验版激活或发布。
- 证据：`.harness/qa/TASK-002/v1/r001/result.md`、`package-check.json`、`compile-webgl.log`、`export.log`。

## r002结果

- 临时压缩分包已执行，但data压缩后36,630,559字节、wasm 4,365,730字节，合计40,996,289字节，超过当前SDK 30,408,704字节限制。
- SDK明确要求将资源文件上传到CDN并回退CDN路线；由于没有CDN目的地和授权，入口正确exit 1，当前包不可上传。
- 源工程面板配置、SDK meta、固定C、Runtime、Content和Scenes均恢复或保持原哈希；未注入AppID、未上传，无遗留Unity进程。
- 证据：`.harness/qa/TASK-002/v2/r002/result.md`、`package-check.json`、`export-result.json`、`export.log`。

## r003结果

- 独立最小上传链路验证包已完成，SDK真实完成、Unity exit 0；压缩data与wasm合计5,011,941字节，低于30,408,704字节限制，引用完整且正式Skeleton C资源未进入此包。
- 已实际调用开发版上传，但平台返回`800059 /app.json not found`；CLI exit 0不作为成功，且没有成功结果JSON，因此当前未上传。
- 微信开发者工具官方本地接口进一步确认：目标AppID属于当前用户可管理的普通小程序，不属于小游戏；包为`compileType=game`且含`game.json`，平台类型不匹配是已确认根因。
- 证据：`.harness/qa/TASK-002/v3/r003/result.md`、`package-check.json`、`upload.log`、`upload-result.json`、`export-result.json`。

## r004结果

- 新目标身份已由微信开发者工具官方本地接口确认属于当前账号可管理的小游戏；r003合格包被复制为独立尝试，包体与引用检查保持通过。
- 官方WechatIDE MCP真实上传返回`80082: UnityPlugin 1.3.14 permission deny`；没有成功结果或待轮询taskId，因此当前仍未上传。
- 该插件依赖不属于“平台可见即可”的最小空壳必要功能；依据已接受的HC-02-r3最小空壳范围，Builder可在独立新包中去除插件依赖并以原生小游戏启动页重试，不修改正式Unity工程。
- 证据：`.harness/qa/TASK-002/v4/r004/result.md`、`mcp-upload.json`、`final-result.json`。

## r005结果

- 已建立与正式Unity工程隔离的零插件原生小游戏空壳；不含UnityPlugin、SDK、正式资源或外部依赖。
- 微信开发者工具官方接口上传成功，结构化回执为`success=true`；版本`0.0.1`、说明“流程验证包”，平台接收包大小1,191字节。
- 本地包共3个文件、2,143字节；包校验、上传后完整性与源工程差异检查通过，未把目标身份写入源工程或证据正文。
- 未提交审核、未发布、未激活体验版；平台记录可见性仍需用户在开发者平台人工确认。本结果不代表正式游戏内容或真机体验验收。
- 证据：`.harness/qa/TASK-002/v4/r005/result.md`、`mcp-upload.json`、`final-result.json`。

## 开发者工具可见性核验

- 已通过本机微信开发者工具打开独立原生小游戏目录，窗口标题为`HotpotUploadFlow`，项目文件树显示`game.js`、`game.json`和`project.config.json`。
- 模拟器已实际渲染“火锅消消”“流程验证包”“启动成功”“仅验证开发版上传链路”“版本 0.0.1”。
- 用户明确将“能通过开发者工具查看”定为最终验收口径；公众平台网页记录不再是本Task的阻断项。

## r006操作性重传

- 用户要求在汇总当前Task的同时再次上传微信开发版；复用已验证且内容未变的原生空壳，执行一次开发版上传。
- 官方WechatIDE结构化回执为`success=true`；上传元数据为版本`0.0.2`、说明“当前Task汇总”，平台包大小1,191字节。
- 上传前后3个包文件哈希全部一致；AppID未进入证据，未提交审核、发布或激活体验版。
- 空壳画面内部仍显示“版本 0.0.1”；`0.0.2`只代表本次开发版上传元数据，不代表新的正式游戏内容版本。
- 证据：`.harness/qa/TASK-002/v5/r006/result.md`、`mcp-upload.json`、`final-result.json`。

# Design Handoff — HC-02 Candidate

Status: Design Ready。Feature Designer仅只读调查，未执行编译、导出或上传；未提出新的产品决定。

## 实施计划

1. 先以Unity 6000.0.26f1、明确`-buildTarget WebGL`重跑定向编译。此前`WebGLInput`错误的脚本诊断没有明确目标启动，编译引用缺少`UnityEngine.WebGLModule.dll`；安装的Unity本身具有该模块与`WebGLInput.mobileKeyboardSupport` API。此步骤恢复后不改SDK键盘输入文件。
2. 仅在明确WebGL目标仍失败时，继续调查编译引用生成，不以空实现、删除键盘支持、虚构package或人为添加`TUANJIE`/`WEIXINMINIGAME`等宏绕过。
3. 加固`WeChatBuild.Export`：唯一导出目录、前后面板值核对、真实SDK完成信号、失败/超时退出与输出目录身份检查。`DoExport()`返回成功或进程exit 0均不单独等于导出完成。
4. AppID保持进程环境输入。先验证微信开发者工具的运行时AppID参数是否可识别目标项目且不改写输出配置；若工具必然要求持久化AppID，暂停并说明该约束冲突，不自动写入。
5. 仅当目录中的`minigame`子目录包含完整可识别项目配置、`game.json`、`game.js`、所引资源和SDK完成证据时，使用本机已登录的开发者工具上传开发版`0.0.1`、说明“流程验证包”。上传后检查CLI最终结果JSON，用户再在平台查看记录。

## 代码与范围

- Build Mode: Code Only。唯一Code Builder负责`Unity/Assets/HotpotSort/Editor/WeChatBuild/WeChatBuild.cs`及确有必要的同目录辅助文件；SDK Editor源码只有在真实完成事件无可用订阅入口时才允许最小改动。
- 不改固定C内容、玩法、场景、美术、音频、`UseFriendRelation: 0`或其他普通MiniGameConfig面板值；不把AppID、Node路径或密钥写入资产、Task、日志或仓库。
- 无视觉或资产变更，不启动Visual/Art角色。

## QA Intent

| AC | 验证 |
|---|---|
| AC-T-02 | 定向WebGL编译通过，或有已证实的最小兼容修复；不以禁用输入为通过。 |
| AC-T-01 | 完整Unity微信导出、SDK真实完成信号、`minigame`输出项目和所有引用资源检查通过；普通面板值与敏感输入约束保持。 |
| AC-F-01 | DevTools CLI对本次`minigame`目录上传开发版成功，版本和说明一致，结果JSON与最终日志一致。 |
| AC-E-01 | 用户在开发者平台看见本次开发版记录；CLI成功只作代理证据。 |

未包含：真机运行、完整体验、审核、发布、云数据、广告、好友关系。固定C玩法不另做游戏QA，只检查本Task没有修改其内容与生产绑定。

# Final Decision

Status: Verified（Task Version 5）
Next checkpoint: None。本Task到此完成；审核、发布或体验版激活不在授权范围内。

## Task Version History

| Version | Date | Change | Reason | Invalidated Outputs |
|---|---|---|---|---|
| 1 | 2026-09-21 | 最小微信开发版流程验证与初次完整导出 | 用户要求平台可见；导出发现工具身份与资源引用约束 | r001导出包不可上传 |
| 2 | 2026-09-21 | 允许AppID仅写入本次导出包；允许临时资源加载配置并恢复源工程 | 用户“确认”PM提出的明确边界 | v1中AppID仅进程内的绝对约束；HC-01/HC-02由r2增量取代相关部分 |
| 3 | 2026-09-21 | 改为独立最小上传链路验证包；正式Skeleton C资源保持不变 | r002容量超限后，用户接受“平台可见优先”的最小包方案并要求实行 | r002超限包及v2本次上传需携带完整正式资源的路径 |
| 4 | 2026-09-21 | 替换为当前账号可管理的小游戏身份，复用r003合格包重试 | 用户提供新AppID；官方本地接口确认身份类型匹配 | v3普通小程序身份及其失败上传尝试，不失效r003包体与导出验证 |
| 5 | 2026-09-21 | 将最终可见性口径明确为本机微信开发者工具可打开并看到启动画面 | 用户明确“能通过开发者工具查看即可”；本机已打开正确项目并看到启动成功 | 原AC-E-01公众平台网页人工查看要求 |
