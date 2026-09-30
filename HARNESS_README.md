# GameDev Harness Lite v1.0

这是一个面向持续迭代、小目标和多 Agent 并行生产的轻量 Harness。

它保留：

- PM 逐轮需求访谈；
- 用户确认后的 SPEC；
- Code / Visual 工作包；
- 按需只读 Explorer 调查；
- PM 通信中继；
- 独占写入路径与共享文件串行集成；
- 编译、启动和当前改动的最低检查；
- 用户最终体验验收与 Baseline。

它删除：

- Strict Mode；
- 固定 Feature Designer；
- QA Intent 与独立 QA 阶段；
- Checkpoint Ledger；
- 多阶段 Human Gate；
- 每次小改的版本升级与回退文档；
- 重复的 Code / Art / Integration 报告；
- 发布专用流程。

## 工作方式

```text
你提出想法
→ PM 逐轮询问，直到需求和逻辑完全明确
→ PM 完整复述，你明确确认
→ PM 只更新 SPEC 的受影响章节
→ 创建一张轻量 Task 执行卡
→ 单个 Agent 直接实施，或多个工作包并行
→ 一个 Integrator 合并共享文件
→ 编译、启动并检查当前变化
→ 你体验并决定接受或继续调整
```

小变化不会触发另一套流程。微信、存档、奖励、真机和发布也按同一链路执行，只将其必要条件写进本次 Acceptance。

## 三个角色，按需派发

- PM 是主对话：与用户锁定玩法和目标效果，维护 SPEC、拆包、协调；不兼任 Integrator。
- `code_agent`：在效果明确后实现逻辑、数据、平台、配置、工具和技术修改。
- `visual_agent`：风格与整体预览、Figma 布局、正式资产、UI、动画、材质、VFX、表现绑定和最终视觉调优。先确认方向与视觉方案，再制作和接入；已有接受的基线时复用。
- `explorer_agent`：按需只读调查当前实现、入口、依赖和风险，返回路径证据与未知项；需求确认前也可使用。已知入口的小改可跳过，不固定新增调试或审查角色。

Integrator 由 PM 在派发前指定到具体的 Code / Visual 工作包，不由 PM 或 Explorer 承担。混合任务按最终共享修改面选择：核心与接口为主选 Code，Scene / Prefab / UI 绑定为主选 Visual；另一角色提供必要的专业检查。

同一角色可以按业务包并行启动多个实例，例如：

```text
VIS-FOOD  → visual_agent → Assets/Food/
VIS-UI    → visual_agent → Assets/UI/
VIS-FX    → visual_agent → Assets/VFX/
CODE-CORE → code_agent   → Runtime/Core/
```

每个实例必须有独立 Work Package ID 和不重叠的写入路径。共享 Scene、Prefab、UI 根节点和绑定文件最后由一个 Integrator 串行处理。

## SPEC 与 Task

`docs/SPEC.md` 是长期产品事实，只有用户确认后的规则才能进入。

`tasks/*.md` 只是本轮执行卡，建议控制在一到两个屏幕内。Task 引用 SPEC 章节，不复制全部需求，也不保存 Agent 对话和历史审计记录。

## 最低检查

每次合并后只确认：

- 当前目标能编译或构建；
- 能启动到受影响入口；
- 当前功能能走通；
- 没有明显新增错误或缺失引用；
- 修改没有越过允许路径。

是否好看、是否自然、是否符合体验目标，由用户查看实际结果后决定。

## 文件结构

```text
AGENTS.md
HARNESS_README.md
HarnessModelDashboard.exe
models.toml
.codex/
  config.toml
  agents/
    code-agent.toml
    visual-agent.toml
    explorer-agent.toml
docs/
  SPEC.md
tasks/
  _TEMPLATE.md
.harness/
  previews/
  artifacts/
  builds/
  baselines/
tools/
  native-dashboard/
```

## 安装

将目录内容复制到项目根目录。项目已有 `AGENTS.md` 或 SPEC 时应合并，不要直接覆盖已有有效规则。

双击 `HarnessModelDashboard.exe` 可以设置三个 Subagent 的模型与推理强度；保存只影响之后启动的 Agent。Code / Visual 保留 Astra low，Explorer 默认 Luna high；PM 使用主对话的模型设置。

旧的 Code Builder、Feature Designer、Visual Design 配置移至 `.harness/migrations/roles-20260929/` 留存，不放在自动发现角色的 `.codex/agents/` 内。当前角色格式与只读 Explorer 参考 [Codex 官方 Subagents 文档](https://learn.chatgpt.com/docs/agent-configuration/subagents)；角色职责与 Integrator 归属是本项目的设计选择。

任何 Commit、Push、上传、提审或发布仍需要用户明确授权。
