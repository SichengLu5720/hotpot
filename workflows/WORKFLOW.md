# Workflow — 使用说明，不另立政策

政策以 AGENTS.md 为准。`python tools/harness.py workflow-table` 从代码生成状态/步骤表；本文件只解释如何交接。

## Task

```text
低算力 PM 常驻入口：登记原始请求、全部需求输入与来源
        ↓
按需 Requirement Analyst：完整研读并返回一次结果后结束
        ↓
PM 核对来源；真实歧义向用户确认
        ↓
PM confirm-requirements：按 run 确认，程序自动合并 goal / QA Intent
        ↓
[有需要] Designer 技术调查、查询接口
        ↓
[有可见改动] Design-Art 实机预览 / 必需资产概念
        ↓
PM 记录用户对具体产物的批准与开工授权
        ↓
Code ║ Art（仅拥有路径不重叠时）
        ↓
[有 Art] Code 顺序最终绑定；否则并入原 code 交接
        ↓
READY_FOR_RELEASE（不代表已运行游戏 QA）
```

PM 是唯一常驻入口，但只承担轻量路由，不持续运行需求深挖。正常流程不会从用户一句话直接跳到实现：PM 建立独立工作区、登记原始请求和材料后，按需派发一个 `requirements` run 给只读 Requirement Analyst。Agent 逐段读完输入，补齐产品背景、用户、场景、关键旅程、规则、异常、回退和成功结果；多义请求至少列出两种解释，多种合理产品定义比较后再确认。推断和建议留在分析文档与 Task 待确认区，只有 PM 与用户已确认的内容才进入机器合同。

Requirement Analyst 返回 READY 或 NEEDS_CLARIFICATION 后，该子任务立即结束，不轮询、不驻留。READY 时 Harness 固定 `.harness/requirements/<TASK-ID>/<run_id>.md` 与 `<run_id>.contract.json`，但 Task 仍停在合同确认门。proposal 只包含产品 goal 与有来源 QA Intent，技术路由字段沿用 Task 草案。PM 核对来源、处理用户决定后调用 `task confirm-requirements`；程序自动合并 proposal 并把确认绑定到分析 run、合同摘要和 task_revision，PM 不再重写完整合同。

### 一个机械交接循环

1. `plan --doc ...` 读取现有事实给下一步；新 Task 会先给出 `requirements`，需求确认后才给技术／视觉／实现步骤。
2. PM 使用 `dispatch --doc ... --step ...`；工具一次写入运行记录，返回 run_id、角色、允许路径、模型目标快照和短 context。
3. PM 在宿主中按需启动对应角色，原样传递派发身份/context。子代理开始/恢复/重要写入前 `check --doc ... --run ...`，交接后结束；禁止空闲轮询或常驻循环。
4. 子代理只向 PM 返回 handoff。PM 保存为短临时 JSON，通过 `accept --doc ... --handoff ...` 校验并登记。临时 handoff 不是第二份 Task 真源，不再手填 in_flight。
5. 如需用户决策，PM 记录具体决定后再推进。命令成功不等于获得宿主/用户授权。

Task 阶段 Designer 交接不以完整 QA Plan 为输出，只返回所需接口的查询结果、参数、返回值、来源和缺口；Builder 使用这些信息实施。技术方案按需，不能因为“每 Task 都要 QA”在功能开发期多开一次 Designer。正式 Release 阶段则由同一个只读 Designer 统一确定 QA Plan。

需求分析交接和技术 Designer 交接不是同一职责：Requirement Analyst 只分析产品定义，不查代码方案；Feature Designer 只在需求已确认后按需调查技术结构，并在正式 Release 统一确定 QA Plan。

### 命令片段

```bash
python tools/harness.py task contract --doc tasks/TASK-001-volume-settings.md --input /path/to/draft-contract.json --decision "登记原始请求与材料"
python tools/harness.py dispatch --doc tasks/TASK-001-volume-settings.md --step requirements
python tools/harness.py accept --doc tasks/TASK-001-volume-settings.md --handoff /path/to/requirements-handoff.json
# PM 处理确认后，只引用已接收的 Analyst run：
python tools/harness.py task confirm-requirements --doc tasks/TASK-001-volume-settings.md --run <requirements-run-id> --decision "用户确认后的产品定义引用"
python tools/harness.py dispatch --doc tasks/TASK-001-volume-settings.md --step code
python tools/harness.py check --doc tasks/TASK-001-volume-settings.md --run <run_id>
python tools/harness.py accept --doc tasks/TASK-001-volume-settings.md --handoff /path/to/handoff.json
```

预览、概念和开工可以依据同一条明确用户回复一次记录：

```bash
python tools/harness.py approve --doc tasks/TASK-001-volume-settings.md \
  --scope preview --scope concept --scope build \
  --artifact <preview-artifact-id> --artifact <concept-artifact-id> \
  --decision "具体用户决定的会话引用"
```

需要多个概念时，每个资产必须能映射到已批准的概念。纯代码任务只需 `--scope build`，不得伪造不存在的视觉产物。

### 歧义与恢复

```bash
python tools/harness.py block --doc tasks/TASK-001-volume-settings.md --reason "待用户决定的问题"
# PM 请求宿主停止，获得证据；对每个已撤销作业分别记录：
python tools/harness.py stopped --doc tasks/TASK-001-volume-settings.md --run <old-run-id> --decision "宿主停止回执或确认从未启动的证据"
# 用户决定后，必要时 task contract 更新要求，再：
python tools/harness.py resume --doc tasks/TASK-001-volume-settings.md --decision "用户回复引用"
```

工具不代替宿主取消，不会回滚已经写下的产品文件。跨 worktree 的依赖暂停由 PM 根据依赖记录分别处理，没有隐式跨进程全局 kill。

## Release

```text
PM 集中集成固定任务提交
    ↓ release prepare
QA_BACKLOG（来自固定 Task 的 QA Intent + 实现事实，PM 核对）
    ↓ interfaces
Designer：确定 QA plan / cases / assertions / interface requirements
    ↓ 固定计划摘要
Code Builder：按计划复用/编写 execution + scripts
    ↓ accept + 固定提交 + release authorize
普通 Runner：集中执行，输出逐断言结果和证据
    ↓ qa report
PM：直接总结；失败很多才可选 qa_reporter
    ↓ 所有必需自动/人工检查覆盖
QA_COMPLETE → 另行用户授权发布
```

Designer 不写脚本，不切换沙箱、不分读写模式。`interfaces` 是现有只读 Designer 的版本 QA 计划交接，不是新的 Agent 或工作模式。

如果项目测试位于默认 `tests/release` 以外，脚本派发前由 PM 显式指定写入范围：

```bash
python tools/harness.py release scope --doc versions/v0.1.0.md --path ExistingTests --path releases/v0.1.0 --decision "沿用项目测试目录"
```

脚本需要维修：`release reopen-scripts --decision ...`，它保留 Designer 计划，只重新派发同一个 Builder 的 `qa_scripts`。需要修业务实现或补 Designer 指明的接口：PM 使用 `release fix-request --path <已固定Task的代码拥有路径> --decision ...`，然后 `dispatch --step release_fix`；接收后重新固定候选/授权。用例、断言、验收含义、正式美术方向或产品要求变化不通过脚本修复绕过，应回到 PM→用户并重新准备/固定计划。

定向复测可用 `qa run --checks C1 C2 --apply`。同一候选、同一固定脚本的多次结果可累计必需覆盖；缺失、SKIP、环境错误、人工待评均不构成完整通过。候选或脚本变化后旧候选证据不自动沿用；1.1 没有基于语义影响推断的证据豁免。

## 成本边界

所有新需求至少经过一个有界的 Requirement Analyst run；只有真实歧义或产品定义变化才重新派发。正常代码 Task 仍不需要 Feature Designer、Visual、Reporter 或 Monitor 的固定调用，Feature Designer 的必经 QA 计划发生在正式 Release。PM 使用低算力模型常驻做来源核对、用户沟通和状态协调，需求深挖由按需高推理档 Agent 承担；实际模型价格与可用性仍由宿主确认。

QA_BACKLOG 和派发 context 都是从固定源生成的短输入，不重复读全部历史。它们仍需消耗实际上下文，不能宣称“零成本”。模型成本、游戏启动次数和返工量是否降低需要真实项目数据；本包不内置未测的节省百分比。
