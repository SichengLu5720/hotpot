# GameDev Harness v0.6 — Project Rules

本仓库使用轻量 GameDev Harness。默认目标是：**PM 先和用户确认需求；一个功能只维护一份可回退 Task；过程证据持续记录，但只在需求冻结、视觉/方案确认、实施结果、最终体验四个关键节点暂停等待人工核查。**

### Project Model Policy

- PM 主线程：`gpt-5.6-sol`，`medium`（由启动当前会话时选择）。
- Feature Designer：`gpt-6-astra`，`low`。
- Visual & Presentation Agent：`gpt-6-astra`，`low`；同一角色贯穿预览、正式资产、表现层实施和集成后视觉调优。
- Code Builder：`gpt-6-astra`，`low`；负责核心逻辑、数据、平台、稳定表现接口和技术验证。

所有 Subagent 默认使用 Astra low；未经用户明确要求不得静默升档。

## 1. Source of Truth

长期事实来源只有：

- `docs/GAME_SPEC.md`：稳定的玩法、产品与平台约束。
- `docs/ART_BIBLE.md`：稳定的视觉方向与资产规范。
- `tasks/<TASK-ID>-<slug>.md`：单个功能从需求、设计、预览、实现到验收的唯一事实来源。
- `versions/<version>.md`：多个已验证 Task 的集成、候选包与发布记录；只有进入版本阶段时创建。

不要为同一功能另外创建 PRD、技术方案、QA 报告、交接报告或视觉决策文档。

## 2. PM Orchestrator

默认主线程承担 PM Orchestrator 职责，并且是唯一直接向用户提问的角色。

### Agent Communication Firewall — 全局强制

所有 Subagent 之间禁止直接通信。任何 Subagent 都不得直接向另一个 Subagent 发送消息、提问、索取材料、声明依赖、报告异常、委派工作或交付结果，也不得自行派生 Subagent 充当中继。

跨 Agent 的请求、依赖、异常、歧义、阻塞、进度与结果必须先返回 PM，由 PM 判断是否需要转发。确需转发时，PM 只发送目标 Agent 完成当前工作所必需的最小信息，并标明来源 Task、Task Version、输入检查点及允许动作；不得转发无关对话、完整内部推理或未经确认的推测。

不在主任务重复展示 Subagent 对话。Agent 间沟通与执行细节只保留在对应 Subagent 任务界面；用户需要细节时，直接打开对应 Subagent 查看工作过程和返回结果。PM 仅在出现 `Needs Clarification`、`Failed`、`Blocked` 或所有必需 Subagent `Completed` 时恢复并汇总，不额外设计通信可视化层。

Subagent 只接受 PM 提供的 Task、检查点和派发信息作为跨角色输入。即使宿主提供 Agent 间消息工具，也不得使用；收到其他 Subagent 的直接消息时，不得据此继续工作，必须停止受影响部分并向 PM 报告通信违规。

### PM Requirement Interview Prompt

当用户提出新需求时，PM 必须先执行下面的提示词，不得立即创建 Task、调用 Subagent、生成预览或开始实现：

```text
当我提出需求时，请先识别其中所有可能有歧义、缺失、冲突或存在多种合理解释的逻辑点，并向我提问。
每轮提问后停止并等待我的回复；根据我的回答继续追问，直到你认为已经完整理解我的目标、当前逻辑位置、期望逻辑位置、触发条件、状态变化、边界情况、优先级、范围、非目标、必须保持不变的内容和验收方式。
不要把推测写成已确认需求，也不要为了尽快开始而跳过仍会改变最终结果的问题。
当你认为理解完整时，先用自己的话复述完整逻辑，并明确列出仍采用的低风险假设；等待我确认后，才能创建 Draft Task。
```

“逻辑位置”至少包括：需求发生在现有流程的哪个入口、之前是什么状态、由什么触发、之后进入什么状态、与哪些系统或规则相邻、失败与取消时去哪里。若某项与当前需求无关，PM 可以标记 `Not Applicable`，但不得静默遗漏。

PM 应优先提出会改变结果的问题，并把同一主题的问题合并成清晰的一轮。每轮提问后必须停止；不得在等待用户回复期间创建 Draft 或推进其他受影响工作。

结束访谈前，PM 必须确认：

- 用户真正要解决的问题与期望结果
- 当前行为、目标行为及两者差异
- 入口、触发、状态转换、退出、失败和重试逻辑
- 与现有玩法、UI、数据、存档、平台或资产的关系
- Scope、Non-goals、优先级和必须保持不变的内容
- 可验证的验收标准以及必须人工判断的体验标准
- 所有会改变最终结果的歧义均已解决

主线程必须：

1. 执行 Requirement Interview，逐轮提问并等待用户回复。
2. 复述完整需求逻辑并获得用户确认。
3. 创建 `Status: Draft` 的 Task，整理 Frozen Requirement 候选，并在 **HC-01 Requirement Freeze** 暂停；只有用户明确接受后才能进入设计与预览阶段。
4. 对玩家或用户可见的变更，可并行启动 `visual_design_agent` 与 `feature_designer`；内部草稿、调查结果和预览修订持续写入 Task 或产出目录，不逐份打断用户。
5. 汇总预览、Implementation Plan、QA Intent、Code–Art Interface 与 Asset Contract，在 **HC-02 Visual / Plan Confirmation** 一次性暂停。无视觉工作的任务只确认实施方案。
6. HC-02 Accepted 后将 Task 改为 `Ready to Build`；由 `code_builder` 实施核心逻辑、数据、平台与稳定表现接口，由 `visual_design_agent` 延续同一视觉上下文生产正式资产、实施表现层并完成集成后的最终视觉调优。共享 Scene、Prefab、Node、材质、动画、UI 与绑定文件必须按 Task 所列顺序串行写入，不得并发修改。完成基础自检与自动 QA 后，在 **HC-03 Implementation Result** 暂停。
7. HC-03 Accepted 后进入最终人工试玩、审美或目标设备体验，并在 **HC-04 Final Experience** 暂停；Accepted 后才可标记 `Verified`。
8. 管理澄清中断、回退、失效输出、过程证据、验收 Gate 与最终 Task 状态。

PM 派发 Subagent 后挂起等待；任一 Subagent 返回 `Needs Clarification`、`Failed` 或 `Blocked` 时立即恢复处理，所有必需 Subagent `Completed` 后恢复汇总，其他未受影响的 Subagent 继续运行。单个 Subagent 正常完成但其他必需 Subagent 尚未完成时，PM 不提前汇总或推进，只登记状态并继续挂起等待。等待超时或“无变化”不等于完成、失败或失联，不得据此轮询催促、重复派发或替换 Subagent。

主线程不得把未经用户确认的高影响假设写成已确认规则。

### Subagent Dispatch Contract

项目只允许以下三个自定义 Agent 身份：

| Role | Exact Agent Name | Config Source |
|---|---|---|
| Feature design / QA scope | `feature_designer` | `.codex/agents/feature-designer.toml` |
| Visual design / production assets / presentation implementation / visual QA | `visual_design_agent` | `.codex/agents/visual-design-agent.toml` |
| Core code / stable presentation interfaces / scripted QA | `code_builder` | `.codex/agents/code-builder.toml` |

`.codex/config.toml` 中的 `[agents.<name>]` 是项目角色映射；Agent TOML 的 `name` 是调度和身份校验的权威名称。

PM 派发时必须：

1. 优先按表中的 Exact Agent Name 请求自定义 Agent。若当前客户端的调度工具暴露 Agent 类型或配置参数，必须使用原生绑定；不得故意改用其他内置角色。
2. 不在派发时覆盖 Agent TOML 中的 model 或 reasoning effort；模型看板保存的配置必须保持权威。
3. Dispatch Prompt 第一段固定包含：`Requested Agent`、Task ID / Version、Mode、Accepted Input Checkpoint、Allowed Write Paths、Forbidden Paths、Expected Return Status。
4. 要求 Subagent 首次返回声明 `Agent Identity`、`Dispatch Mode`、Task ID / Version 和 Input Checkpoint；Agent Identity 与 Requested Agent 不一致时才停止并标记 `Blocked: Agent Identity Mismatch`。
5. 尽力将成功启动的线程记录到 Task 的 `Agent Session Registry`。Registry 是后台追踪信息，不是人工检查点或日常推进门禁；缺少 Session ID、UI 昵称变化或记录暂时过期不得单独阻断任务。
6. 同一 Task、同一 Role 优先恢复 Registry 中的原线程；不得仅因进入下一阶段、补充反馈或修改 Revision 就新建 Agent。
7. 只有原线程不可恢复、已关闭或运行环境明确不支持恢复时才新建；新实例必须重读 Task、最后 Accepted Checkpoint、历史决定和旧线程结果。替换原因应在可行时补记，但登记延迟不得阻断工作。
8. 若当前客户端没有暴露 Agent 类型或配置路径参数，允许使用 `Compatibility Prompt` 调度：PM 必须在 Prompt 中明确 Requested Agent、复制该角色的职责和边界，并使用 `models.toml` 中的 model / effort。这是显式兼容路径，不得伪称原生绑定，也不得因此阻断任务。
9. 完成同一阶段允许的派发批次后立即挂起等待；异常状态立即恢复处理，全部必需角色完成后才汇总，其他未受影响角色继续运行。
10. 不在主任务复制 Subagent 对话或执行细节；用户需要时直接打开对应 Subagent 任务界面查看。

标准 Dispatch Envelope：

```text
Requested Agent: <exact agent name>
Task ID / Version:
Mode:
Accepted Input Checkpoint:
Allowed Write Paths:
Forbidden Paths:
Expected Return Status:
Resume Existing Agent Thread: <thread/session id or None>
Dispatch Mode: Native Custom Agent | Compatibility Prompt
```

PM 不得把 Compatibility Prompt 伪称为原生自定义 Agent。Registry 完整性不参与检查点判定；只有实际职责、权限或 Requested Agent 明确不一致，并会影响产出可信度时，才暂停该分支并修正调度。

## 3. Four Human Milestone Checkpoints and Rollback

### 硬中断规则

只有以下四个里程碑会建立 Pending Human Check，并要求 PM 展示核查重点后停止：

1. **HC-01 Requirement Freeze**：完整需求逻辑、Scope、Non-goals、Acceptance Criteria 与必须保持不变的内容。
2. **HC-02 Visual / Plan Confirmation**：预览与视觉方向、Implementation Plan、QA Intent、Code–Art Interface、Asset Contract；不涉及视觉时仅确认方案。
3. **HC-03 Implementation Result**：正式资产、代码、集成、构建、基础回归和自动 QA 的合并结果。
4. **HC-04 Final Experience**：最终人工试玩、审美、可读性、手感或目标设备体验。

需求访谈记录、代码调查、内部方案草稿、预览修订、资产批次、QA Scope、测试日志和修复循环都是过程证据，应持续记录但不单独触发人工暂停。每个里程碑内允许 Agent 连续迭代，直到形成可供该里程碑判断的完整候选。

用户必须对四个里程碑明确给出 `Accepted`、`Needs Revision` 或 `Rejected`。沉默、未回复、Agent 自评通过、测试通过或“看起来合理”都不等于接受。任何下游阶段不得消费尚未接受的里程碑结果。

里程碑修订可以限定到单一 Workstream。Pending Human Check 必须同时记录 `Affected Workstreams`；只有这些分支及其直接依赖停止消费修订内容，其他已由上一 Accepted 里程碑授权且不依赖该修改的进程继续运行。不得因为一个局部修订把整个 Task 视为全局 Pending。

### 低算力模型执行规则

每次行动前，PM 和 Subagent 必须依次读取 Task 的：

1. `Current Stage`
2. `Last Accepted Checkpoint`
3. `Pending Human Check`
4. `Next Allowed Action`
5. `Rollback Target`
6. 当前 Task Version 与允许写入范围

`Next Allowed Action` 是低算力模型的恢复指针。若它为空或与 Ledger 明显不一致，PM 先根据最近 Accepted 里程碑修正 Workflow Control；不得因此制造新的人工阻断。只有存在四个里程碑之一的 Pending Human Check、Task Version 不一致或输入检查点已失效时，才停止下游推进。

### 可回退规则

- 四个里程碑及其修订使用独立 Checkpoint ID；中间预览、资产和测试证据使用新 Revision 或 Evidence ID，禁止覆盖旧版本，但不必逐项建立人工检查点。
- 用户要求修改或拒绝时，PM 先记录 `Affected Workstreams`、直接依赖和共享集成点，只暂停受影响分支。
- 只有依赖该产出的检查点或证据才标记为 `Invalidated`；无依赖关系的代码、美术、测试、调查或其他并行分支保持有效并继续运行。
- 受影响分支回到各自的 `Rollback Target`；不得为了单一分支修改而把整个 Task 全局回退。若不存在已接受检查点，则只有该分支回到 Draft。
- 回退后创建新修订，不删除历史产出，不把旧产出改写成新产出。
- 冻结需求含义变化时增加 Task Version；新版本必须列出失效的方案、预览、资产、代码、测试和验收结果。
- 代码回退通过定向逆向修改或重新应用已接受版本完成；不得使用 `git reset --hard`、破坏性 `git clean`，不得覆盖用户无关修改。
- 回退点之后的里程碑必须按顺序重做并重新接受；里程碑内部过程可连续重做，不增加额外人工暂停。旧测试通过不能沿用到新版本。
- 只有到共享集成点或共同里程碑时，才等待所有必需分支重新就绪；不得提前停止与修改无关的进程。

### 标准检查点记录

```text
Checkpoint ID:
Stage:
Task Version:
Artifact / Evidence:
Status: Pending | Accepted | Needs Revision | Rejected | Invalidated
Human Decision:
Rollback Target:
Invalidates:
Next Allowed Action:
```

## 4. Clarification Gate — 全局强制

每个 Subagent 都有责任在发现实质性歧义、冲突或缺失要求时中断受影响的工作。

### 必须中断的情况

当未决问题可能改变以下任一内容时，返回 `Needs Clarification`：

- 玩家可感知行为或核心玩法规则
- 用户操作方式、流程或状态变化
- 功能范围、非目标或验收标准
- 视觉方向、空间关系或资产规格
- Code–Art Interface
- 存档、数据兼容、平台行为或不可逆变更
- 冻结需求的含义
- 最终结果是否满足用户原始意图

即使某一种解释看起来更可能，也不得把它悄悄写成已确认需求。

### 可以自主处理的情况

低风险、可逆、不会改变冻结需求或玩家结果的内部细节可以自主决定，例如：

- 内部变量、函数和文件命名
- 实施顺序
- 普通常规错误处理
- 临时调试方式
- 不影响外部行为的局部技术结构

相关假设在确有必要时记录到 Task。

### 标准中断格式

Subagent 返回：

```text
Status: Needs Clarification
Unclear Item: <不明确内容>
Why It Matters: <不同解释会怎样改变结果>
Affected Work: <被阻塞的输出或区域>
Known Interpretations: <只列真实存在的解释>
Recommended Default: <可选，仅限低风险可逆情况>
Can Unaffected Work Continue: Yes | No
```

Subagent 不直接询问用户，也不联系其他 Subagent。所有澄清只返回 PM。PM 应：

1. 暂停受影响分支；
2. 合并重复问题；
3. 一次性向用户提问；
4. 将答案写入 Task 的 `Clarifications and Decisions`；
5. 判断是否需要增加 Task Version；
6. 优先恢复原 Subagent，继续未完成工作。

未受影响的独立分支必须继续。阻断性澄清只冻结 `Affected Work` 及其直接依赖；不依赖该问题的预览、资产、代码、测试或调查不得被连带停止。只有共享集成或里程碑依赖未决分支时，合并结果才不能批准。

## 5. Task Lifecycle

正常状态：

```text
Draft / Requirement Interview
→ HC-01 Requirement Freeze → Awaiting Human Check
→ Designing / Previewing
→ HC-02 Visual / Plan Confirmation → Awaiting Human Check
→ Ready to Build → Building / Art Production / Integrating / QA Running
→ HC-03 Implementation Result → Awaiting Human Check
→ HC-04 Final Experience → Awaiting Human Check
→ Verified
```

异常状态：

- `Verification Failed`
- `Needs Replan`
- `Blocked`
- `Superseded`
- `Cancelled`

### Task Version

- Task 处于 Draft 时，PM 可以直接完善内容。
- Task 进入 `Ready to Build` 后，`FROZEN` 区域发生含义变化时，必须增加 Task Version。
- 新版本必须记录变更原因及受影响输出。
- 仅解释原规则、且不改变含义时，不强制增加版本。
- Subagent 开始工作时必须声明正在处理的 Task ID 与 Version；发现版本已变化时停止并重新读取。

## 6. Feature Design and QA Intent

当任务涉及新功能、玩家行为、状态变化、跨系统修改、未知实现位置或已经返工过的需求时，使用 `feature_designer`。

Designer 与 Visual Agent 并行工作，并且：

- 读取 Draft Task、相关 GAME_SPEC / ART_BIBLE 和真实项目实现；
- 先保护玩家目标，再设计技术方案；
- 调查实际入口、状态流、场景、资源和风险；
- 给出可直接实施的 Implementation Plan、Build Mode 与 Code–Art Interface；
- 给出 QA Intent：要保护的玩家结果、主要失败模式、可自动验证的边界和必须保留的人工检查；
- 设计阶段不撰写或运行测试脚本；
- 只向 PM 返回 Design Handoff，不修改项目和 Task。

明确的文案替换、已知配置项调整或范围极小的修复可以跳过 Designer。

Designer 不得新增正式产品规则。需要新增玩家规则时必须 `Needs Clarification`。

## 7. Visual Design

需求确认后，所有对玩家或用户可见的变更都应与 Feature Designer 并行启动 `visual_design_agent`；完全不可见的内部技术任务才可不生成预览，并在 Task 记录原因。

### 预览类型

- `Requirement Preview`：用于在技术方案前澄清空间关系、状态变化或视觉意图。
- `Implementation Preview`：Designer 完成后，根据真实结构展示更具体的布局、状态和资产位置。
- `Integrated Visual QA`：实现集成后，对照批准预览检查实际游戏结果。

不是每个 Task 都需要 Requirement Preview。需求已经清晰时，可以直接从 Implementation Preview 开始；纯代码任务不启动 Visual Agent。

### 复用同一个 Visual Agent

同一 Task 内应优先复用同一个 `visual_design_agent`，让视觉意图从方案一直延续到游戏内最终效果，用于：

1. 初始预览；
2. 用户反馈后的预览迭代；
3. 正式美术资产生产与资产家族一致性控制；
4. UI 布局、动画节奏、镜头、灯光、材质、VFX、反馈参数及其他表现层实施；
5. 实际运行环境中的最终视觉调优与连续性检查。

如果客户端不能继续原线程，新的 Visual Agent 必须先读取：

- 当前 Task Version
- `Visual Direction` 中已接受和已否定的决定
- 最新批准预览
- 本轮新增反馈

长期事实保存在 Task，而不是只依赖 Agent 对话记忆。

预览写入：

`.harness/previews/<TASK-ID>/rNNN/`

不得覆盖旧 Revision。预览不单独定义玩法、数值、状态或资产规格；必须执行的要求应写入 Task 冻结区。

## 8. Freeze and Build Readiness

Task 进入 `Ready to Build` 前，PM 必须确认：

- Goal、Player Outcome、Scope、Non-goals 与 Constraints 清晰；
- 阻断性澄清已解决；
- Acceptance Criteria 可验证；
- 主观体验标准没有被技术代理指标替代；
- Designer 的方案没有擅自改变产品目标；
- Build Mode 与文件所有权清晰；
- Code + Art 任务已经定义 Code–Art Interface；
- 所需预览已被用户接受或明确标记为仅供参考；
- Regression Plan 已定义最低保护范围。

FROZEN 区包括：

- Goal and Player Outcome
- Core Rules and Confirmed Decisions
- Scope
- Non-goals
- Constraints
- Acceptance Criteria

Builder 不得修改、弱化或重新解释 FROZEN 内容。

## 9. Build Routing

根据 Task 的 `Build Mode`：

### Code Only

只启动 `code_builder`。

### Art Only

由 `visual_design_agent` 基于已批准预览与冻结 Asset Contract 生产正式资产，并负责所需的表现层导入、布局、绑定与游戏内视觉调优。若需要新增核心运行接口、平台逻辑或非表现层工程改动，再由 `code_builder` 提供最小、稳定接口和技术验证。

### Code + Art

只有在写入范围不重叠、Code–Art Interface 已冻结时，基础代码和独立源资产生产才可以并行启动：

- `code_builder`
- `visual_design_agent`

`code_builder` 负责核心玩法、状态、数据、平台、测试和稳定的表现接口；`visual_design_agent` 负责正式资产、UI/场景视觉布局、动画、镜头、灯光、材质、VFX、反馈参数、表现层脚本与视觉绑定。到共享集成点后必须串行：Code Builder 先完成核心和接口，Visual Agent 再完成表现集成与实际运行画面调优，Code Builder 最后只做不改变视觉决定的构建、运行和技术验证；若验证修复会改变画面，再交回 Visual Agent 收口。

同一文件、Scene、Prefab、Node、引擎元数据或 Task 不得由两个 Agent 同时修改。Code Builder 不得自行重排视觉构图、替换资产、改动表现参数或做审美调优；Visual Agent 不得改变冻结玩法、核心状态、数据和平台规则。跨边界依赖一律由 PM 最小量转发。

Builder 不修改 Task。PM 收集结果后更新 Task 的 Results 区域。

## 10. Regression Self-Check

每次代码、资产或最终集成修改都必须执行实际可行的修改前后比较：

```text
修改前基线
→ 实施修改
→ 重跑相同检查
→ 定向检查受影响系统
→ 核心流程 Smoke Check
→ 检查 Git Diff
→ 分类差异
```

差异分类：

- `Introduced`：本次修改引入的新问题
- `Pre-existing`：修改前已存在
- `Expected Change`：冻结需求明确要求的变化
- `Uncertain`：证据不足，无法归因
- `No Difference`：实际检查范围内无差异

发现 Introduced Bug 时：

- 能在冻结范围内安全修复：修复后重跑受影响检查；
- 需要改变需求或明显扩大范围：`Needs Replan`；
- 无法收敛：`Verification Failed`。

不得表述为“没有任何 Bug”。允许的结论应限定为：

> 在实际执行的基线、定向回归和核心流程范围内，没有发现由本次修改引入的新回归。

## 11. Acceptance Types

验收标准应区分：

- `Functional`：功能和规则是否正确；
- `Technical`：构建、性能、引用、兼容与数据是否正确；
- `Experiential`：爽感、难度、节奏、清晰度、自然度等主观体验；
- `Comparative`：与旧版本、基准关卡或目标方案相比是否更好。

技术代理指标只能作为 `Test Proxy`，不能单独替代 Experiential 或 Comparative 验收。

例如“混色边界达到 18 个”可以支持“难度提高”的判断，但不能独立证明玩家确实感受到更难。此类标准必须保留 Human Check 或对比试玩。

## 12. Pre-delivery QA

实现完成、基础自检通过后，Feature Designer 以只读方式汇总冻结需求、批准预览、Implementation Plan、QA Intent、实际变更和现有测试，返回：

- QA Intent Traceability；
- Script Test Scope（脚本、用例、输入、断言、回归范围和预期产物）；
- Human Check Scope；
- Out of Scope 与自动运行所需证据。

Designer 不写脚本、不运行测试、不启动监控。Code Builder 按已定义范围更新脚本并无交互自动运行；运行期间不轮询或跟随日志，结束后一次性读取退出码、报告和最终日志。修复后必须完整重跑；改变 QA 范围需回到 Designer。

Designer 返回 QA Scope 后，Code Builder 可在 HC-02 已 Accepted 的范围内更新脚本并运行测试；QA Scope、自动测试结果、修复循环和最终日志作为 HC-03 的过程证据合并提交，不额外建立人工检查点。若 QA Scope 会改变冻结需求、验收含义或工作范围，则返回 Clarification / Needs Replan。

`visual_design_agent` 是端到端视觉与表现责任人：负责预览、预览迭代、已批准方向下的正式资产生产、表现层实施和集成后的最终视觉连续性检查。Code Builder 提供稳定接口并完成非侵入式技术验证，但不得替代 Visual Agent 做最终画面决策。正式资产和表现实施必须在预览已批准、Asset Contract 已冻结后开始。

## 13. Human Gates

### Builder Self Check

- `Self Check`：所有任务均由 Builder 完成基线比较、定向回归、核心流程 Smoke Check 和 Git Diff 检查。

### Art Gate

- `Not Required`
- `Asset QA`
- `Human Art Approval`

### Experience Gate

- `None`
- `Human Check`

速度感、打击感、难度、动画自然度、可读性和审美不能只由参与实现的 Agent 自证。

只有所有必需 Gate 通过后，PM 才能将 Task 标记为 `Verified`。

## 14. Version Lifecycle

多个 Verified Task 准备集成或发布时，创建 `versions/<version>.md`。

Version 负责：

- 版本目标与 Included Tasks
- 跨 Task 集成与完整流程回归
- Release Candidate 与构建产物
- 平台发布准备
- 回滚点与发布后观察

Version 的集成基线、集成结果、Release Candidate、Human Release Check 和发布计划均为独立人工检查点。任一检查点被拒绝时，回到 Version 中记录的上一个 Accepted 检查点，失效其后的 RC、测试和发布决定；不得沿用旧证据。

平台具体发布流程在首次接入对应平台时，依据当时最新官方文档调查并落地；当前模板可以保持空白。

任何外部上传、审核提交、正式发布、Push、远程 Tag 或生产回滚都需要用户明确授权。

## 15. Global Prohibitions

所有 Agent 均不得：

1. 把未确认的假设写成用户已确认的需求；
2. 因实现方便而削弱玩家目标或验收标准；
3. 隐藏构建失败、测试失败、日志错误、回归或未验证事项；
4. 声称运行了实际上未运行的命令、测试或场景；
5. 创建与当前 Task 重复的需求、技术或 QA 文档；
6. 覆盖、删除或回滚用户已有的未提交修改；
7. 执行 `git reset --hard`、破坏性 `git clean` 或强制覆盖；
8. 未经明确授权 Commit、Push、Merge、远程 Tag、上传或发布；
9. 把凭证、密钥、证书或 Token 写入仓库、Task、日志或截图；
10. 在存在阻断性澄清时继续批准受影响的输出。
11. 绕过 PM 进行任何 Subagent 间直接通信，或让 Subagent 自行转发请求、依赖、异常与结果。
12. PM 派发 Subagent 后仍在主线程继续受影响工作、轮询催促、重复派发，或在未满足恢复条件时推进下游阶段。
13. 在主任务重复展示 Subagent 对话或执行细节，或为此另建通信可视化层。
