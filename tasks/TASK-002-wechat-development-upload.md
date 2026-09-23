# Task: 微信小游戏开发版导出与上传

Task ID: TASK-002  
Task Version: 10  
Status: Implementation Result Ready — Awaiting CloudBase Environment / Bundle Deployment；Art Paused  
Type: Platform  
Risk: High  
Build Mode: Code Only  
Art Gate: Not Required  
Experience Gate: Human Check  
Current QA Mode: Lightweight Builder Self Check — compile、启动、资源准备核心Smoke、包体门禁；不新增独立QA等待点或每轮全量回归  
Created By: PM Orchestrator  
Created At: 2026-09-21

---

# Workflow Control

Current Stage: HC-03-v10 Technical Candidate Ready；CloudBase External Deployment Pending  
Last Accepted Checkpoint: HC-02-v10-Code Technical / Loading-State Plan  
Pending Human Check: HC-03-v10完整结果等待CloudBase环境绑定、固定Bundle部署、真实配置重导出与DevTools/设备；合成配置下正式SDK转换已通过；视觉加载/失败状态绑定随TASK-001视觉恢复处理  
Next Allowed Action: 用户完成微信公众平台登录与开发者工具“信任并运行”；随后管理员确认或开通关联CloudBase环境并上传固定Bundle，外部注入真实WECHAT_CLOUD_ENV_ID、HOTPOT_REMOTE_CLOUD_FILE_ID、HOTPOT_REMOTE_MANIFEST与AppID后重导出并运行核心检查。  
Rollback Target: HC-03-v8；保留已验证插件与完整包启动链，失效v7零字体约束及其不可读结论  
Paused Workstreams: 最终加载状态皮肤与字形审美等待TASK-001视觉恢复；CloudBase环境创建/计费选择、外部资源上传、审核提交与发布未获授权。  
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
| HC-01-v6 | Requirement Freeze | 6 | 当前TASK-001 v6完整游戏候选的微信开发预览范围 | Accepted | 用户“确认 请整合”（2026-09-22） | HC-04-r1 | v5仅展示流程验证空壳的最终交付口径 | Designer只读形成真实导出与开发者工具预览方案 |
| HC-02-v6 | Visual / Plan Confirmation | 6 | 完整候选固定、插件权限预检、完整导出、容量/资源路线、包校验与DevTools实际运行方案 | Invalidated | 用户接受总体方案但明确“不加入字体”（2026-09-22） | HC-01-v6 | v6要求正式字体完整进入预览包 | 使用v7字体排除修订 |
| HC-01-v7 | Requirement Freeze Revision | 7 | 当前完整游戏开发预览不携带正式自定义字体；原工程字体不删除 | Accepted | 用户“好的 不加入字体 接受”（2026-09-22） | HC-01-v6 | v6预览包必须包含正式字体的要求 | 形成并接受HC-02-v7 |
| HC-02-v7 | Visual / Plan Confirmation | 7 | HC-02-v6其余方案＋独立构建副本排除字体、可读性阻断条件 | Accepted | 用户“好的 不加入字体 接受”（2026-09-22） | HC-01-v7 | HC-02-v6字体部分 | 恢复Code Builder实施、校验并打开DevTools |

| HC-03-v8 | New Identity Plugin Preflight | 8 | `build/wechat/TASK-002-v8-preflight/final-result.json`与脱敏开发者工具控制台日志 | Passed | 用户开通快适配并信任当前项目；新身份已成功加载UnityPlugin 1.3.14（2026-09-22） | HC-02-v7 | 旧目标身份的权限结论；不失效v7方案 | 构建并运行完整无字体候选 |
| HC-04-v8 | Fontless DevTools Runtime | 8 | `build/wechat/TASK-002-v8-full/fontless-simulator.png`、`fontless-package-check.json`与脱敏启动日志 | Invalidated | 用户接受精简中文字体，明确替代零字体限制 | HC-03-v8 | v8零字体可读性结论；插件与启动链证据保留 | 使用HC-01-v9 |
| HC-01-v9 | Requirement Freeze | 9 | 微信接入完整逻辑复述；用户“全部正确”并要求实施 | Accepted | 精简运行时中文字库；AppID/广告位/云环境仅外部注入；本轮不上传、不提审、不发布 | HC-03-v8 | HC-01-v7与HC-02-v7的零字体约束 | 汇总字体、导出与TASK-006/007/008接口方案至HC-02-v9 |
| HC-02-v9 | Visual / Plan Confirmation | 9 | `.harness/previews/TASK-002/r001/`字形/安全区证据；完整包、外部配置、缺字检查与回归合同 | Pending | 等待用户随合并HC-02确认 | HC-01-v9 | 接受前无v9字体或构建结果可用 | Accepted后实施精简字库和完整微信基线 |
| HC-02-v9-Code | Technical Plan Confirmation | 9 | 字体工程、配置、完整包合同及用户“暂停美术迭代 先完成其他的” | Accepted | 先完成非美术工程；最终字形审美仍待视觉门槛 | HC-01-v9 | HC-02-v9中技术分支Pending状态 | 实施字体、配置与导出基础，不上传 |
| HC-03-v9-Code | Technical Implementation Result | 9 | `.harness/qa/TASK-002/v9/qa-r001/final-result.json`、`staging-final/Unity`、`.harness/qa/integrated/v9-r002/aggregate-manifest.json` | Pending | 精简字体346字、导出工程20项、Boot/BuildGuard与统一回归通过；真实SDK转换和DevTools未运行 | HC-02-v9-Code | 字体、导出脚本或合并源码变化时失效 | 外部注入AppID后执行真实转换与包检查；不上传 |
| HC-01-v10 | Requirement Freeze | 10 | 用户确认采用保真远程资源路线；`.harness/qa/TASK-002/v9/package-repair-r001/`实测仍超限4,161,559字节；Feature Designer v10候选 | Accepted | 用户确认推荐流程：点击开始后下载；完整校验后创建挑战；失败留启动页可重试/取消；同版本完整缓存直接开始 | HC-02-v9-Code | v9“全部正式资源包内自包含”的完整包结论；不失效v9本地技术实现与回归 | 形成HC-02-v10 provider、缓存、包体与QA合同 |
| HC-02-v10-Code | Technical / Loading-State Plan Confirmation | 10 | Feature Designer完整v10 Design Handoff：34本地/31远程、固定版本AssetBundle、provider/cache/启动门禁、安全、实施顺序与Script Test Scope | Accepted | 用户回复“接受”；不含视觉皮肤、上传、托管部署或发布 | HC-01-v10 | 后续含义变化只失效受影响的provider、包与验证 | Code Builder实施技术分支；Visual继续暂停 |
| HC-03-v10-Code | Technical Implementation Result | 10 | `.harness/qa/TASK-002/v10/runtime-r001/`、`bundle-r001/`、`integration-r001/`、`local-runtime-r001/`、`linker-repair-r001/`、`cloudbase-transport-r001/`、`post-cloudbase-build-r001/`、`sdk-conversion-r001/`；14,718,885字节bundle；CloudBase直连；SDK转换包23,275,755字节；12程序集与核心Smoke | Pending | provider/cache、开始门禁、34/31 bundle、`wx.cloud.downloadFile`进度/取消桥、GLX官方初始化修复及合成Cloud配置下正式SDK转换已完成；外部CloudBase部署、真实配置重导出与设备未验证 | HC-02-v10-Code | 相关运行时、bundle清单、云传输或构建脚本变化时失效 | 用户完成登录/信任；管理员绑定环境并部署固定bundle，使用真实配置重导出并完成DevTools/设备验证；不上传小游戏版本 |

过程证据：`.harness/qa/TASK-002/v9/sdk-export-r001/final-result.json`记录真实SDK转换因Data.br与Wasm.br合计53,781,022字节超过30,408,704字节门槛而退出1；面板与身份保持安全，未上传。`.harness/qa/TASK-002/v9/package-repair-r001/`已在新隔离副本排除v3历史资源并完成22组回归，实测降至34,570,263字节，仍超限4,161,559字节。用户随后确认采用推荐的保真远程资源路线；v10不得降质、删除正式v7资产或绕过SDK门禁。

### v10 CloudBase Transport Addendum（2026-09-22）

- 用户提供CloudBase官方小游戏原生API指南。经本地SDK与代码核对，固定AssetBundle改为优先通过`wx.cloud.downloadFile(cloud://...)`下载到临时文件；HTTPS保留为显式备用模式，不在云权限或下载失败后自动切换。
- 冷下载使用外部注入的`WECHAT_CLOUD_ENV_ID`与`HOTPOT_REMOTE_CLOUD_FILE_ID`；热缓存命中时不初始化云、不联网。包内固定manifest、长度、SHA-256、CRC、事务缓存、generation去重与Ready后开局门禁保持不变。
- 项目自有桥接保留下载任务、进度、真实Abort、回调去重及临时文件清理，不修改vendored微信SDK。测试与源码证据见`.harness/qa/TASK-002/v10/cloudbase-transport-r001/final-result.json`。
- 构建工具已补齐微信SDK官方`PreInit()`，解决错误启用`libemscriptenglx.a`造成的Unity 6000链接失败；见`.harness/qa/TASK-002/v10/linker-repair-r001/final-result.json`。
- 修改后完整集成WebGL bootstrap实测Data.br 18,871,348字节、Wasm.br 3,875,566字节，合计22,746,914字节，低于30,408,704字节门槛7,661,790字节；见`.harness/qa/TASK-002/v10/post-cloudbase-build-r001/final-result.json`。该结果证明包体与原始WebGL构建，不替代正式微信转换、云下载或真机证据。
- 合成非生产Cloud配置下，已使用当前授权小游戏身份完成真实微信SDK转换；Data+Wasm为23,275,755字节，低于门槛7,132,949字节。导出副本WebGL2/OpenGLES3一致性修复位于`WeChatBuild.cs`，源面板与286个受保护文件未变。证据见`.harness/qa/TASK-002/v10/sdk-conversion-r001/final-result.json`。该包不得作为真实云下载候选，仍须用实际环境ID/fileID重新导出。

## Agent Session Registry

| Role | Exact Agent Name | Dispatch Mode | Thread / Session ID | Status | Task Version | Last Input Checkpoint | Resume / Replacement Note |
|---|---|---|---|---|---:|---|---| 
| Feature Design | `feature_designer` | Not Started | Not Started | Not Started | 1 | None | |
| Code Build | `code_builder` | Compatibility Prompt | `/root/code_builder_task002_wechat` | Completed | 5 | HC-02-r4 | r005官方上传成功；开发者工具本地可见性已完成HC-04验收 |
| Code Build | `code_builder` | Compatibility Legacy Run | `/root/code_builder_task002_v8` / `run-task002-v8-appid-retry-20260922` | Completed — NEEDS_CLARIFICATION | 8 | HC-02-v7 | 新身份独立插件预检；插件未开通，按停止条件未构建完整包 |
| Code Build | `code_builder` | Compatibility Legacy Resume | `/root/code_builder_task002_v8_resume` / `run-task002-v8-plugin-enabled-resume-20260922` | Completed — BLOCKED | 8 | HC-03-v8 | 用户报告已开通后立即重试并追加3次传播重试；平台仍拒绝插件，未构建完整包 |
| Feature Design | `feature_designer` | Compatibility Legacy Read-only | `/root/feature_designer_task002_v8_boot` / `run-task002-v8-boot-investigation-20260922` | Completed — READY | 8 | HC-03-v8 | 固定v6微信候选启动链、随机源、WebGL2与无字体校验的有界技术交接；未写文件或运行测试 |
| Code Build | `code_builder` | Compatibility Legacy Resume | `/root/code_builder_task002_v8_resume` / `run-task002-v8-fontless-validation-20260922` | Completed — BLOCKED | 8 | HC-03-v8 | 隔离固定v6候选已构建并在DevTools进入主界面；系统回退对象存在但关键中文不可读，按v7停止条件停工 |

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
| CL-011 | 汇总后游戏的含义与授权边界 | 将当前TASK-001 v6 HC-03候选整合为微信开发预览版，让用户在本机微信开发者工具查看实际游戏；该预览不等于接受TASK-001 HC-03，不提交审核、不发布 | User“确认 请整合” | v6 | 完整Unity游戏导出、微信开发者工具预览 |
| CL-012 | 微信预览包字体 | 本轮独立微信开发预览包不携带正式自定义字体文件；不得从原Unity工程删除字体。使用已有非自定义回退能力；若关键中文不可读则停止并报告，不把缺字画面视为成功 | User“好的 不加入字体 接受” | v7 | 构建副本、容量与DevTools可读性 |

| CL-013 | 新目标小游戏身份重试 | 用户提供新的正式小游戏AppID并要求加入开发者工具跑通流程；身份只进入独立预检包必需字段，不写入Unity源工程或Task正文。先验证UnityPlugin能力，通过后才允许完整构建 | User（2026-09-22） | v8 | 插件预检、后续完整Unity开发预览 |

未解决的阻断问题：平台能力、插件加载、完整构建和Unity启动链均已通过；但微信WebGL运行时无法在零字体文件方案下可靠绘制关键中文。继续需要用户在“加入精简汉字字体”与“把固定文字改为图片资产”之间确认方向。

---

## v9 HC-02 Implementation Contract

- 字体角色统一为现代圆润黑体，优先Noto Sans CJK；精简子集覆盖全部固定文案、数字、标点、动态格式、加载/离线/奖励/错误状态。完整源字体不覆盖，子集字符清单、哈希、许可和生成参数留证。
- 好友昵称由开放数据域系统字体绘制，不进入Unity子集。`HasCharacter`仅作代理，必须用实际WebGL/DevTools中文截图验证。
- 外部配置包含环境、AppID、可选云环境/函数、可选广告位、能力开关、协议版本与分享图包内路径；AppID只进入独立导出身份文件，禁止AppSecret、Token或凭证写库/日志。
- 从当前v9生成候选，只定向迁入已证实的SDK成功码、等价出生序列和WebGL2兼容修复，不复制旧v6 Composition。
- 导出后校验JS/Wasm/Data/分包/字体/正式资源/开放域/分享图引用并等待SDK真实完成；源面板前后哈希保持。打开DevTools验证正式入口、开始、点击、暂停/继续、重试、退出；不上传、不提审、不发布。

<!-- FROZEN_START -->

# Frozen Requirement

## v6 Addendum — 当前完整游戏开发预览

- 以当前TASK-001 v6 HC-03候选为唯一内容基线，整合真实Unity游戏并导出为微信小游戏开发预览版。
- 目标是用户可在本机微信开发者工具中打开并查看实际游戏，不再以原生流程验证空壳作为本轮交付内容。
- 必须保留TASK-001当前状态：该候选仍待HC-03人工决定；成功导出、上传或在开发者工具运行均不得替代TASK-001验收。
- 仅允许开发版预览；不提交审核、不发布、不激活体验版，不启用真实云数据、广告、好友关系或其他生产功能。
- AppID仍只允许存在于本次独立导出包的开发者工具必需配置中，不写入Unity源工程、Task正文或公开证据。
- 若完整包仍受容量、插件权限或导出链阻断，必须返回真实阻断与最小可选方案，不得再次用空壳冒充汇总后的游戏。

## v7 Addendum — 无自定义字体预览

- 本轮独立微信开发预览包排除`Hotpot/TASK001/v3/r001/fonts/`下的正式自定义字体文件；原Unity工程和TASK-001正式资产保持不变。
- 不得以重新压缩、裁剪字形或降低字体质量替代该决定；构建副本只能使用已有的非自定义回退能力。
- DevTools中关键中文必须仍可辨认。若平台/Unity运行时无法在不携带字体文件的前提下显示关键中文，返回阻断并由用户决定，不擅自重新加入字体。

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

# Design Handoff — HC-02-v6 Candidate

Status: Design Ready。Feature Designer仅只读核对，未运行测试、构建、开发者工具或上传；未提出新的产品决定。

## 实施计划

1. 固定当前TASK-001 v6 HC-03候选的Assets、Packages、ProjectSettings、Boot、固定C内容及正式资源哈希；原工程只读，在独立构建目录工作，不使用`MinimalUploadEntry`或native-shell。
2. 先核验当前小游戏身份是否仍拒绝必需的`UnityPlugin 1.3.14`。若仍为`80082 permission deny`或需要开通平台能力，停止并返回真实阻断；不得删插件后冒充Unity可运行。
3. 以Unity 6000.0.26f1和`HotpotSort.Build.WeChatBuild.ExportPackage`执行完整WebGL/微信导出，等待真实SDK完成事件，核对源工程配置前后不变。
4. 除用户在v7明确排除的正式自定义字体外，保留完整正式内容。若实际包仍超过SDK约30.4MB限制，不再删资源或降画质；仅允许在本机DevTools预览中使用受限于本次资源目录的临时静态服务，并逐项验证请求、响应及data哈希。需要公网CDN、域名或安全策略变更时停止。
5. 完整校验项目JSON、启动imports、wasm、data、分包或外部资源引用后，打开本次准确的`minigame`目录；必须等到Unity正式入口，不以SDK加载页或“项目打开成功”代替。
6. 在DevTools点击开始进入真实每日挑战，观察正式食材、锅、五格暂存、中文、一次有效点击、暂停/继续、v6裁剪/露出/点击和自然供给。本轮只提供开发预览，不替代TASK-001 HC-03/HC-04。

## 已确认平台事实与停止条件

- r001仅证明旧候选在明确WebGL目标下11个程序集通过，不是当前v6完整构建结果。
- r002完整压缩data+wasm为40,996,289字节，超过SDK 30,408,704字节检查；新候选以本次实际构建为准。
- r004的`UnityPlugin 1.3.14 permission deny`仍是正式Unity运行的前置风险；r005/r006空壳成功不证明权限恢复。
- 插件仍拒绝、完整数据引用缺失、启动卡在插件/wasm/SDK、需要删减正式内容或修改生产服务时立即停止，不回退为空壳。

## v7 r001预检阻断

- 微信开发者工具已打开准确的独立插件预检项目并处于小游戏模式，但出现“您信任此项目的代码吗？ / 信任并运行”安全确认。
- 该确认必须由用户在开发者工具中手动完成；Agent未点击、未使用CLI参数绕过。
- 本轮尚未执行完整Unity构建、未生成完整游戏包，也尚未验证UnityPlugin权限是否恢复；历史`80082`不能冒充本轮结果。
- 1,148个源文件哈希保持不变，证据已脱敏；未上传、审核、发布或激活体验版。
- 证据：`.harness/qa/TASK-002/v7/r001/result.md`、`final-result.json`、`devtools-trust-prompt.txt`。
- 用户于2026-09-22回复“已信任”；该宿主安全阻断已解除，允许恢复同一Builder继续HC-02-v7实施。
- 信任生效后的官方本地日志确认必需的`UnityPlugin 1.3.14`加载失败，提示“插件需要申请才可使用”，随后模块未定义、`UnityManager is not a constructor`并黑屏。
- BUILD未运行，PACKAGE未生成，DEVTOOLS为插件初始化阻断；1,148个源文件哈希仍保持不变。
- 恢复条件：目标小游戏身份获得该插件能力。申请或开通属于平台权限变更，需用户明确授权；未授权前不得代为操作。
- 补充证据：`.harness/qa/TASK-002/v7/r001/mcp-call-1790045951491.json`、`resume-final-result.json`、`resume-result.md`。

## v8新身份插件预检结果

- 使用用户新提供的正式小游戏身份创建独立预检目录：`build/wechat/TASK-002-v8-preflight/minigame`；仅导出包必需身份字段发生变化，Unity源工程未写入目标身份。
- 微信开发者工具成功打开并普通编译该小游戏项目；没有登录、扫码、信任或安全确认弹窗。
- 官方本地控制台仍报告“插件需要申请才可使用”、`requirePlugin module is not defined`及`UnityManager is not a constructor`，模拟器黑屏。
- 按已确认停止条件未运行完整Unity构建、未生成完整游戏包、未上传、未审核、未发布、未激活体验版，也未改变平台权限。
- Builder核对1,148个Unity源文件SHA-256，变更数为0。状态：BUILD `NOT_RUN`；PACKAGE `NOT_BUILT`；DEVTOOLS `BLOCKED_REQUIRED_PLUGIN`。
- 脱敏证据：`build/wechat/TASK-002-v8-preflight/final-result.json`、`build/wechat/TASK-002-v8-preflight/mcp-call-1790057668855.json`。
- 用户随后报告“已开通”。Builder立即重新编译，并在北京时间14:23:51、14:24:52、14:25:57追加三次间隔传播重试；每次官方新控制台仍返回相同插件未授权与构造失败，未进入完整构建。
- 当前证据不能区分“平台权限尚在传播”与“后台仅完成申请、仍待审核确认”。需核对快适配页面的真实状态文字；不以用户操作完成推断平台已经授权。
- 传播重试证据：`build/wechat/TASK-002-v8-preflight/propagation-final-result.json`、`build/wechat/TASK-002-v8-preflight/mcp-call-1790058357120.json`。

## QA Intent

| ID | 必须观察的结果 |
|---|---|
| V6-PKG-01 | 包身份对应当前TASK-001 v6；Boot、固定C与正式视觉完整，按v7决定排除自定义字体，未使用最小入口或原生空壳。 |
| V6-PKG-02 | 当前完整Unity构建和SDK完成流程成功；源工程配置与候选哈希保持。 |
| V6-PKG-03 | JSON、imports、框架、wasm、data及资源引用完整；压缩data可还原为本次原始data。 |
| V6-PKG-04 | 包内或本机资源路线通过真实容量、请求、响应、hash和无404检查。 |
| V6-RUN-01 | DevTools真实取得UnityPlugin、加载wasm/data、完成SDK初始化并显示正式游戏入口。 |
| V6-RUN-02 | 可进入每日挑战，正式资源和中文正常；一次有效点击、暂停/继续及退出/重开可用。 |
| V6-RUN-03 | 定向观察v6暂存栏裁剪、前景不穿透、露出可点和自然供给；仅作为DevTools观察证据。 |
| V6-BOUND-01 | 平台能力保持模拟；AppID不进入源工程或公开证据；未审核、发布或激活体验版。 |

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

Status: Blocked（Task Version 8）
Next checkpoint: 用户决定是否撤销“不加入字体”的限制并允许仅覆盖当前界面必需汉字的精简字体；若不允许，则先形成并确认固定文字图片化方案。未经决定不得继续改动。

## Task Version History

| Version | Date | Change | Reason | Invalidated Outputs |
|---|---|---|---|---|
| 1 | 2026-09-21 | 最小微信开发版流程验证与初次完整导出 | 用户要求平台可见；导出发现工具身份与资源引用约束 | r001导出包不可上传 |
| 2 | 2026-09-21 | 允许AppID仅写入本次导出包；允许临时资源加载配置并恢复源工程 | 用户“确认”PM提出的明确边界 | v1中AppID仅进程内的绝对约束；HC-01/HC-02由r2增量取代相关部分 |
| 3 | 2026-09-21 | 改为独立最小上传链路验证包；正式Skeleton C资源保持不变 | r002容量超限后，用户接受“平台可见优先”的最小包方案并要求实行 | r002超限包及v2本次上传需携带完整正式资源的路径 |
| 4 | 2026-09-21 | 替换为当前账号可管理的小游戏身份，复用r003合格包重试 | 用户提供新AppID；官方本地接口确认身份类型匹配 | v3普通小程序身份及其失败上传尝试，不失效r003包体与导出验证 |
| 5 | 2026-09-21 | 将最终可见性口径明确为本机微信开发者工具可打开并看到启动画面 | 用户明确“能通过开发者工具查看即可”；本机已打开正确项目并看到启动成功 | 原AC-E-01公众平台网页人工查看要求 |
| 6 | 2026-09-22 | 从流程验证空壳扩展为当前TASK-001 v6完整游戏的微信开发预览 | 用户确认允许将尚待HC-03验收的候选用于开发预览并要求整合 | v5空壳作为当前最终内容的口径；v5上传与证据仍保留 |
| 7 | 2026-09-22 | 微信开发预览包排除正式自定义字体，原工程字体保留 | 用户明确“不加入字体”并接受方案 | v6要求预览包包含正式字体；其余完整游戏预览方案保留 |
| 8 | 2026-09-22 | 使用用户新提供的正式小游戏身份重试UnityPlugin预检 | 用户要求用新wxid加入开发者工具并跑通流程 | 旧目标身份的权限判断；v7无字体完整预览方案与历史证据保留 |
