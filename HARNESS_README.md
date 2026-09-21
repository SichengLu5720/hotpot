# GameDev Harness v0.6

这是一个可以直接复制到游戏项目根目录的 **Codex 原生、单 Task、轻量多 Agent Harness**。

v0.6 的重点不是增加更多文件，而是把前几版收敛成一条可实际运行的开发链路：

```text
你 ↔ PM 主线程
      ↓
Requirement Interview / Draft Task
      ↓
HC-01 Requirement Freeze → 人工核查
      ↓
Visual & Presentation Agent 预览图 ∥ Feature Designer 查看代码/接口
      ↓
HC-02 Visual / Plan Confirmation → 人工核查
      ↓
Code Builder 实施核心/接口 ∥ Visual & Presentation Agent 生产资产和表现层
      ↓
集成、基础回归、QA Scope 与自动测试
      ↓
HC-03 Implementation Result → 人工核查
      ↓
HC-04 Final Experience → 人工核查
      ↓
Verified
```

任何一步被要求修改时，回到上一个已接受检查点；其后的派生产出全部失效并按顺序重做。旧预览、旧资产和旧结果保留为历史，不覆盖、不冒充最新版。

## PM 中继与等待

所有 Subagent 之间禁止直接通信。跨 Agent 的请求、依赖、异常与结果必须先返回 PM，由 PM 判断并只向目标 Agent 转发完成当前工作所需的最小信息。

PM 派发 Subagent 后挂起等待；任一 Subagent 返回 `Needs Clarification`、`Failed` 或 `Blocked` 时立即恢复处理，所有必需 Subagent `Completed` 后恢复汇总，其他未受影响的 Subagent 继续运行。单个正常完成但其余必需角色尚未完成时，PM 不提前汇总或推进，只登记状态并继续等待。

不在主任务重复展示 Subagent 对话。Agent 间沟通与执行细节只保留在对应 Subagent 任务界面；用户需要细节时，直接打开对应 Subagent 查看工作过程和返回结果。PM 不增加额外通信可视化层。桌面端已经提供 Subagent 任务的过程与结果检查入口，参见 [OpenAI Docs — Subagents](https://learn.chatgpt.com/docs/agent-configuration/subagents)。

## v0.6 相比 v0.5 的核心变化

### 1. 从“多合同文件”收敛为“单一 Task”

删除默认依赖的：

- Requirement Packet JSON
- Design Packet JSON
- Dispatch Manifest
- Visual Thread 文件
- Preview Manifest
- Code Result JSON
- Integration Result JSON
- 全阶段 Digest 链

需求、设计、视觉决策、接口、实现结果与验收统一保存在：

`tasks/<TASK-ID>-<slug>.md`

资产很多或 Code + Art 并行时，Visual Agent 可按需输出一个简洁的 `asset-manifest.json`，但不强制使用 Schema。

### 2. PM 先和用户把需求聊清楚

PM 是主线程，不是隐藏 Subagent。它负责：

- 识别用户真正目标；
- 主动寻找需求中可能存在的歧义、缺失、冲突和多种合理解释；
- 每轮提问后停止等待用户回复，持续追问到完整理解用户的逻辑位置；
- 复述目标、入口、状态变化、边界、范围、非目标和验收方式，并获得用户确认；
- 创建 Draft Task；
- 将同一 Task 交给 Designer 和 Visual Agent；
- 汇总结果并冻结 Ready to Build Task；
- 处理所有 Subagent 的中断和用户沟通。

### 3. 每个 Subagent 都有 Clarification Gate

Designer、Visual Agent 和 Code Builder 发现实质性歧义时，必须暂停受影响工作并返回：

`Needs Clarification`

只有 PM 向用户提问。用户回答写回 Task 后，优先恢复原 Subagent。

### 4. 预览与方案并行，在合并里程碑统一人工核查

视觉流程不再强制每次都跑：

- PM 与用户确认需求后，玩家/用户可见的变更由 Visual Agent 生成预览图，同时 Designer 开始查看真实代码和接口。
- 只有完全不可见的内部技术任务可标记 `Preview: Not Required`。
- Visual Agent 与 Designer 可在 HC-01 Accepted 后连续调查和迭代；每版预览、方案草稿与 QA Intent 都作为过程证据保存，不逐份打断用户。
- PM 合并预览、Implementation Plan、QA Intent、Code–Art Interface 与 Asset Contract，在 HC-02 一次性等待人工接受。
- 如果是美术需求，预览批准且 Asset Contract 冻结后，由同一个 Visual Agent 延续视觉上下文，生产正式资产并实施 UI、布局、动画、镜头、灯光、材质、VFX、反馈参数与视觉绑定。
- 集成后由 Visual Agent 在实际运行画面中完成最终调优和 Integrated Visual QA。

同一 Task 内尽量复用同一个 Visual Agent 贯穿预览、正式资产、表现层实施与集成后视觉检查。跨会话或无法继续线程时，通过 Task 中的：

- Accepted Visual Decisions
- Rejected Directions
- Latest Approved Preview

恢复上下文。

### 5. Build 按冻结方案实施

Task 的 Build Mode：

- `Code Only`
- `Art Only`
- `Code + Art`

保留这些模式名以兼容已有 Task；其中 `Art` 指由 `visual_design_agent` 负责的视觉与表现工作。Code + Art 只有在核心代码与独立源资产写入范围完全分离时才并行。到共享 Scene、Prefab、Node、UI、材质、动画或绑定文件时改为串行：Code Builder 先完成核心和稳定接口，Visual Agent 再完成表现集成与最终画面调优，Code Builder 最后只做不改变视觉决定的技术验证。所有结果在 HC-03 合并核查，不为每批产出单独暂停。

### 6. 所有修改都做变更诱发 Bug 自检

Builder 必须先记录修改前基线，再重跑相同检查并验证受影响系统。差异区分：

- Introduced
- Pre-existing
- Expected Change
- Uncertain
- No Difference

“功能能跑”不等于“没有回归”。

### 7. 交付前才进入 QA 脚本阶段

Build 完成后，Feature Designer 只读总结实现方案与 QA Intent，给出明确的 Script Test Scope。Code Builder 再更新脚本并启动一次无交互自动运行。运行期间不轮询、不 tail 日志、不启动额外监控 Agent；结束后一次性读取退出码、报告和最终日志。

Script Test Scope、测试脚本、自动运行结果与修复循环都作为 HC-03 的过程证据，不再单独触发人工暂停。若测试范围会改变冻结需求或验收含义，则返回 PM 澄清。

## 人工检查点与回退

Task 顶部的 Workflow Control 是恢复入口。模型读取 `Current Stage`、`Last Accepted Checkpoint`、`Pending Human Check`、`Next Allowed Action` 和 `Rollback Target`；指针过期时由 PM 根据 Ledger 修正，不能因此制造额外人工门禁。

Checkpoint Ledger 只记录四个强制人工里程碑：需求冻结、视觉/方案确认、实施结果、最终体验。其余产出作为过程证据记录。用户选择修改或拒绝时：

1. 保留当前产出并标记为 `Needs Revision` 或 `Rejected`；
2. 先识别受影响分支和直接依赖，只将这些派生结果标记为 `Invalidated`；
3. 只让受影响分支回到自己的 `Rollback Target`，无关分支继续运行；
4. 生成新 Revision；
5. 从该点重新进行受影响的里程碑核查；里程碑内部修订不重复打断用户。

回退不使用破坏性 Git 操作，不覆盖用户已有修改，也不删除旧预览或旧资产。

修改隔离原则：代码、美术、测试、调查或其他并行进程只要不依赖被修改内容，就不得被连带暂停。只有到共享集成点或共同里程碑时，才等待所有必需分支重新就绪。

## 目录结构

```text
<project>/
├── AGENTS.md
├── HARNESS_README.md
├── HarnessModelDashboard.exe
├── models.toml
├── .codex/
│   ├── config.toml
│   └── agents/
│       ├── feature-designer.toml
│       ├── visual-design-agent.toml
│       └── code-builder.toml
├── .harness/
│   ├── .gdignore
│   ├── previews/
│   └── artifacts/
├── docs/
│   ├── GAME_SPEC.md
│   └── ART_BIBLE.md
├── tasks/
│   └── _TEMPLATE.md
├── versions/
│   └── _TEMPLATE.md
└── tools/
    └── native-dashboard/
```

## 启动模型看板

双击项目根目录的 `HarnessModelDashboard.exe`。看板只参考 hotpot 仓库的 Windows 原生白色紧凑布局与固定行交互；角色、Task 结构和交付逻辑仍以当前 v0.6 和本文档为准。
看板为 `win-x64` 单文件程序，需要本机安装 .NET 10 Desktop Runtime；不影响 Harness 文档和 Agent 本身的使用。

看板可编辑现有 Agent 的 model 和 reasoning effort，保存后同步 `models.toml` 与 `.codex/agents/*.toml` 中的受管配置块。空 model 表示继承宿主。看板不请求模型列表、不调用模型、不监听端口。

## Subagent 调度方式

本 Harness 使用 `.codex/config.toml` 显式注册三个项目级自定义 Agent。PM 优先按 `feature_designer`、`visual_design_agent`、`code_builder` 的精确名称原生派发。

若当前客户端没有暴露 Agent 类型或配置路径参数，PM 使用显式的 `Compatibility Prompt`：完整传入角色、职责、边界及看板模型配置。每个 Subagent 启动后声明角色身份、Dispatch Mode、Task Version 和输入检查点；同一 Task 的同一角色后续优先恢复该线程。

Codex 客户端可能在运行中显示 Lovelace、Bernoulli 等临时昵称，也可能在完成后显示任务标题；这些只是 UI 标签。是否正确运行 Harness Agent，以 Exact Agent Name、身份声明和 Session Registry 为准。

Session Registry 只用于后台追踪和恢复，不参与日常门禁。缺少 Session ID、临时昵称变化、记录延迟或客户端缺少原生类型参数均不构成阻断；只有实际职责或权限错误并影响产出可信度时才暂停该分支。不得把兼容调度伪称为原生绑定。

## 接入步骤

1. 将压缩包内容复制到项目根目录。
2. 项目已有 `AGENTS.md` 时合并规则，不要直接覆盖已有仓库约定。
3. 填写 `docs/GAME_SPEC.md` 与 `docs/ART_BIBLE.md` 中已经确认的内容；未知项写 `Not documented`。
4. 在 Codex 中从项目根目录打开会话。
5. 用一个小型真实 Task 试跑。

## 启动一个需求

直接对 Codex 主线程说：

```text
按照项目中的 GameDev Harness 处理下面需求。
当我提出需求时，请先识别其中所有可能有歧义、缺失、冲突或存在多种合理解释的逻辑点，并向我提问。
每轮提问后停止并等待我的回复；根据我的回答继续追问，直到你认为已经完整理解我的目标、当前逻辑位置、期望逻辑位置、触发条件、状态变化、边界情况、优先级、范围、非目标、必须保持不变的内容和验收方式。
不要把推测写成已确认需求，也不要为了尽快开始而跳过仍会改变最终结果的问题。
当你认为理解完整时，先用自己的话复述完整逻辑，并明确列出仍采用的低风险假设；等待我确认后，才能创建 Draft Task，再按 AGENTS.md 调用需要的 Subagent。

需求：
<你的需求>
```

PM 应逐轮和你讨论并等待回复，而不是马上创建 Task、预览或代码。

## 推荐首个测试 Task

```text
把玩家移动速度从硬编码提取为统一可调参数。
默认配置保持当前实际行为，本轮不制作完整调试看板。
```

理想流程：

```text
PM 澄清默认行为和验收方式
→ Draft Task（此任务完全不可见，记录 Preview: Not Required）
→ Designer 查看移动入口，输出 Implementation Plan + QA Intent
→ 人工核查方案
→ PM 冻结 Ready to Build Task
→ 人工核查冻结候选
→ Code Builder 建立基线、按方案实现和回归自检
→ 人工核查代码结果
→ Designer 交付前给出 Script Test Scope
→ 人工核查测试范围
→ Code Builder 更新脚本并无监控自动运行
→ 人工核查测试结果
→ Human Check 确认移动体验
→ Verified
```

## Subagent 中断示例

Code Builder 发现：

```text
角色到达十字平台中心后，Task 没写自动直行还是等待玩家选择。
```

它必须返回 `Needs Clarification`，而不是自己选择一种行为。PM 再向你提问，并将答案记录到 Task。

## Task 是唯一事实来源

一份 Task 中包含：

- 澄清与已确认决定
- 冻结需求
- 设计与真实实现调查
- Visual Decisions 与批准预览
- Build Mode 与 Code–Art Interface
- Regression Plan
- Code / Visual / Presentation / Integration 结果
- Acceptance Results
- 人工验收

不要为同一功能复制出多份文档。

## 视觉预览目录

```text
.harness/previews/TASK-001/
├── r001/
│   └── preview.png
├── r002/
│   └── preview.png
└── r003/
    └── preview.png
```

旧 Revision 不覆盖。Task 中明确记录哪一版被批准，以及哪些视觉方向已接受或拒绝。

## 资产交付

当 Task 涉及多个正式资产或 Code + Art 并行时，生成预览的原 Visual Agent 线程可以输出：

`.harness/artifacts/<TASK-ID>/asset-manifest.json`

Manifest 只需记录：

- Asset ID
- 最终路径
- 尺寸、格式、Alpha
- 帧数或状态
- 技术检查
- 已知偏差

小型单资产任务可以直接在 Task 的 Asset Contract 与 Visual / Presentation Result 中记录，不必创建 Manifest。

## 验收边界

技术检查可以验证：

- 是否构建成功
- 配置是否生效
- 引用是否正确
- 状态与数据是否正确
- 是否出现新日志错误

以下内容仍需要人工验收：

- 是否更爽、更难、更直观
- 速度和震动是否合适
- 动画是否自然
- 角色是否有记忆点
- 美术是否达到预期品质

技术代理指标只能作为辅助证据，不能替代主观体验验收。

## 进入版本阶段

多个 Task 达到 `Verified` 后，才创建：

`versions/<version>.md`

版本层负责组合回归、Release Candidate、平台发布准备和回滚。具体平台流程在首次接入时，根据当时最新官方要求调查并填写。

## 当前版本刻意不包含

- MCP
- Codex command rules
- 通用 Harness 调度命令（测试脚本由具体项目的 Build 按 QA Scope 维护）
- Skills
- Agents API
- 自动 Commit / Push / Tag
- 自动上传或正式发布
- 默认 JSON Schema 与 Digest 链

先让 v0.6 在真实项目中跑通数个 Task，再根据实际摩擦决定是否升级自动化。
