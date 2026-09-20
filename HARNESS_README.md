# GameDev Harness 1.1

一个本地、文档驱动的游戏开发交接工具包。它不包含游戏源码、模型服务、无人值守宿主适配或真实游戏测试。

低算力 PM Coordinator 是唯一常驻入口；Requirement Analyst 只在新需求、真实歧义或产品定义变化时按需运行，返回后结束。Designer 在正式交付时确定 QA 计划、用例、断言和接口需求；同一个 Code Builder 严格按固定计划复用/编写可执行测试脚本；普通 Runner 执行；QA Reporter 按需只读总结。一个常驻 PM + 四个按需核心子代理 + 可选 Reporter，没有 Workflow Monitor 或新增工作模式。

PM 主会话是唯一用户入口和合同写入者，但不承担重需求推理，也不在每轮对话重复唤起 Analyst。新需求先登记全部文档、聊天、纪要、截图说明和补充材料，再按需派发只读 Requirement Analyst 形成背景、价值、用户、场景、状态、边界、多义解释、产品方案比较和待确认问题；Agent 交接后结束。PM 确认后再写机器合同并进入技术／视觉／实现流程。推断不能冒充用户决定，产品分析也不提前代写技术方案。

## 安装 / 升级

需要 Python **3.11+**、Git，以及能够加载项目 Agent TOML 的宿主。Harness 命令只用 Python 标准库；随包提供 Windows 原生看板 EXE，不需要 Web 服务、浏览器、npm、pip 或 Playwright。

先保存自己的修改并在独立分支/工作区升级。本包的 AGENTS、docs、tasks、versions 与项目已有文件应合并，不能整目录覆盖已有业务资料。已经配置的工具/模型/用户审批不能因升级而丢弃。

从未定制 v1.0 可用提供的升级补丁，在项目根先 `git apply --check` 再 `git apply`。定制项目先审阅 diff。升级后旧 release 记录不自动继承 Builder 制作的 plan：应停止旧派发并重新 `release prepare`，由 Designer 固定新 QA Plan，再由 Builder 提交 execution。原有独立 `harness_gate.py`、`harness_workflow.py`、`harness_workspace.py`、Workflow Monitor、Release QA Builder 配置仍应移除，避免双入口/旧角色继续派发。

活动 Task 需要人工迁移到新结构，不自动把旧审批、epoch、QA_SPEC 变成新授权。先停止所有旧派发并确认停止，再创建新的隔离工作区，把确认后的 QA 意图、接口/资产合同录入；旧文件作为历史保留，不能直接运行。每个活动 worktree 单独升级；主仓库升级不等于全部工作区升级。

项目引擎测试框架、图像生成工具、接口/MCP、可运行构建由真实项目提供，本包不伪造这些能力。

## 快速使用

命令从项目根执行；外部运行加 `--repo /absolute/worktree`（放在子命令前）。工具帮助：

```bash
python tools/harness.py --help
python tools/harness.py workflow-table
python tools/harness.py models show
```

`_TEMPLATE.md` 的人类说明段会保留；机器默认由状态机初始化，不通过手改模板增加状态规则。

先将合并后的 harness **由你明确提交到选定基线**。工具不会替你提交或 stash。`main` 替换为实际稳定分支。创建两个独立需求：

```bash
# 不加 --apply 只预览
python tools/harness.py workspace task TASK-001 volume-settings --base main --apply
python tools/harness.py workspace task TASK-002 daily-entry --base main --apply
```

工具在主仓库旁建立 `<repo>-worktrees/TASK-...`，绑定独立分支和 Task。分别从对应目录启动 PM 会话。当前工作区检查：

```bash
python tools/harness.py workspace check
python tools/harness.py plan --doc tasks/TASK-001-volume-settings.md
```

Task 的原始请求／合同草案从输入 JSON 录入；`examples/contract.example.json` 是结构示例，不是你游戏的已确认需求：

```bash
python tools/harness.py task contract --doc tasks/TASK-001-volume-settings.md --input /path/to/draft-contract.json --decision "登记原始请求与全部材料"
```

首次 `task contract` 登记原始请求／草案后，必须先完成需求分析，再用同一命令确认最终合同：

```bash
python tools/harness.py dispatch --doc tasks/TASK-001-volume-settings.md --step requirements
python tools/harness.py accept --doc tasks/TASK-001-volume-settings.md --handoff /path/to/requirements-handoff.json
python tools/harness.py task confirm-requirements --doc tasks/TASK-001-volume-settings.md --run <requirements-run-id> --decision "用户确认后的产品定义引用"
```

第二次调用会把确认绑定到 Requirement Analyst run、合同摘要和 task_revision；没有该绑定，技术、视觉、实现和构建批准都不会开放。需求实质变化后必须重新分析。`models.toml` 默认把 PM 放在 `economy/low`、Requirement Analyst 放在 `quality/high`；空 model 仍表示继承宿主，实际模型可用性由宿主确认。

主流程见 `workflows/WORKFLOW.md`，数据格式与完整 handoff 见 `workflows/FORMATS.md`。`dispatch` 会返回精简派发 context，不需要每个子代理重读全部 Task 历史。

## 启动模型看板

双击项目根目录的 `HarnessModelDashboard.exe`。这是唯一支持的看板，为本地 Windows 原生窗口；HTML/HTTP 看板、静态网页和 `dashboard --port` 命令已经删除。

「模型配置」页显示 PM Coordinator、Requirement Analyst、Feature Designer、Design-Art、Code Builder、QA Reporter，可直接设置模型与推理强度并保存。PM 行控制常驻协调入口，默认低强度；Requirement Analyst 行控制后续按需派发，默认高强度。保存操作通过 `models update-agents` 的 etag 与事务入口同步 `models.toml` 和对应 Agent TOML，仅影响后续新派发。

原生看板还保留「组件接入」「排行榜」「开发者控制台」页面；不启动 Web 服务、不监听端口、不生成浏览器 token。没有主模型服务调用，费用不会因为打开看板而产生。

**默认 model 留空**，表示继承宿主，不代表已经替你选了便宜模型。填入账号实际支持的模型 ID；effort 与模型的兼容性在宿主确认。看板不自动请求模型列表或你的账单。

目标配置、原生配置、运行观测分开。实际模型记录由 PM 从宿主证据转录，未记录显示未知，不以配置猜测。不同 worktree 的目标配置独立。

仅本包派发器会尊重 enabled；直接绕过 Harness 从宿主启动仍受宿主规则，而不是本包的硬权限锁。现有线程不被 Apply 重启，宿主可能需要重新加载 Agent 定义才影响新派发。

配置文件编辑方式也可用：

```bash
python tools/harness.py models preview --input /path/to/proposed-models.toml
python tools/harness.py models apply --input /path/to/proposed-models.toml --etag <preview返回的etag>
python tools/harness.py models rollback --transaction <models事务ID> --apply
```

发现手工漂移时先读差异；明确同意重新同步时加 `--reconcile`。写入中断时用 `models recover`；文件存在额外人工修改会拒绝恢复，不猜测覆盖。

## 正式版本

创建 release worktree，PM 在明确授权下集成选定任务（工具不自动 merge）。固定输入 JSON 使用任务文档对应的完整提交摘要，不用浮动分支名：

```bash
python tools/harness.py workspace release v0.1.0 --base main --apply
# 切到 release worktree，完成用户授权的集成后：
python tools/harness.py release prepare --doc versions/v0.1.0.md --inputs /path/to/pinned-tasks.json
python tools/harness.py dispatch --doc versions/v0.1.0.md --step interfaces
python tools/harness.py dispatch --doc versions/v0.1.0.md --step qa_scripts
```

PM 先把 `interfaces` 的 run_id/context 交给只读 **Feature Designer**，接收并固定其 QA Plan；再把 `qa_scripts` 派发给 **Code Builder**，由其按计划提交 execution、脚本和 fixture。Designer 定义接口需求，Builder 不能改用例、断言或验收标准。

脚本与夹具由 PM 在授权下提交并固定候选；然后：

```bash
python tools/harness.py release authorize --doc versions/v0.1.0.md --decision "本候选的测试执行授权引用"
python tools/harness.py qa run --doc versions/v0.1.0.md              # 只预览命令
python tools/harness.py qa run --doc versions/v0.1.0.md --apply      # 真实执行普通程序
python tools/harness.py qa report --doc versions/v0.1.0.md
python tools/harness.py release complete --doc versions/v0.1.0.md
```

这不是游戏测试示例的一键通过按钮。真实脚本必须对接项目入口并遵守报告格式；示例脚本未适配时只返回 ERROR。`release complete` 只记录 QA 完成，不发布、不推送、不签名。

## 自检

```bash
python -m unittest discover -s tools/tests -v
dotnet build tools/native-dashboard/HarnessModelDashboard.csproj -c Release
```

测试在临时 Git 仓库内进行，不运行你的游戏、不调用真实模型。夹具会排除宿主 `.git`、常见虚拟环境、`node_modules` 与缓存，因此可从已安装到现有 Git 仓库根目录的 Harness 执行。测试记录和边界见 VALIDATION.md；迁移语义见 CHANGELOG.md。

## 文件布局

```text
AGENTS.md                   人工维护的唯一流程政策
models.toml                 目标模型配置
.codex/agents/              4 个核心角色 + 默认关闭的 qa_reporter
HarnessModelDashboard.exe  Windows 原生开发看板
reusable/                   原生看板的组件接入素材与共享实现
workflows/                  交接说明与数据格式
tasks/                     每功能一份 Task（唯一合同与机器记录）
versions/                  每版本固定输入、授权、执行证据引用
releases/<version>/        运行时生成的 QA Backlog / Designer Plan / Builder Execution 视图
tools/harness.py           唯一公开命令入口
tools/native-dashboard/    Windows 原生看板源码
tools/harnesslib/          内部状态机、IO、模型、Runner、资产、workspace
.harness/                  参考、预览、概念与辅助产物；运行证据默认本地保存
```

发布前另行归档 `.harness/qa` 的证据（它默认被 Git 忽略）。不能只交一份引用本机路径的报告，宣称接收者已经拿到截图/日志。
