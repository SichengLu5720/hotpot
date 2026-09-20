# Harness 1.1 验证记录

日期：2026-09-20。验证对象仅为本工具包；没有游戏项目、真实模型会话、实际账号费用或平台发布参与。

## 已执行

| 检查 | 实际结果 |
|---|---|
| Python unittest（临时 Git 仓库 / worktree） | **73 tests，全部通过；275.109 秒** |
| Agent TOML + models.toml | **6 份 Agent + 1 份配置**均通过解析；目标与原生受管字段同步 |
| Windows 原生看板构建 | .NET 10 `win-x64` 单文件 publish 通过；已在活动项目替换并启动 |
| 原生看板与后端绑定 | 六个稳定 role key 与 `models.toml`、Agent TOML 一一对应；保存使用统一事务入口 |
| HTML 看板清理 | `dashboard.py`、Web 静态文件、浏览器检查和 `dashboard --port` 入口均不存在 |
| 已安装 Git 仓库根目录自检 | 测试夹具排除宿主 `.git`、`bin`、`obj` 等构建目录；Task worktree 创建通过 |
| 详细 Task 模板兼容性 | 唯一 `harness-state` 可解析；需求、常规开发和 Release 流程回归通过 |

实际 Python 执行使用 Codex 工作区自带运行时。原始输出见 `validation/unittest.log`。原生看板构建输出来自 `dotnet publish tools/native-dashboard/HarnessModelDashboard.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true`。

## 低算力 PM 与按需 Requirement Analyst

- `pm_coordinator` 是唯一常驻入口，默认 `economy/low`；负责登记、路由、用户决定、合同与状态协调。
- `requirement_analyst` 默认 `quality/high`，但不是主入口；只在新需求、真实产品歧义或产品定义变化时按有效 `requirements` run 启动。
- 派发回执明确要求 Analyst 在 READY / NEEDS_CLARIFICATION 交接后结束；禁止空闲轮询、后台循环和为了状态汇报重复唤起。
- PM 不是新增流水线步骤。测试确认 `pm_coordinator` 不属于 `STEPS`，因此每个 Task 不会额外产生一次 PM 模型工序。
- 新 Task 登记草案后只开放 `requirements`；分析 READY 后仍停在合同确认门，`task confirm-requirements` 将用户确认绑定到分析 run、合同摘要和 task_revision。
- READY handoff 同时包含只读 `contract_proposal`，且机器只允许 goal/qa_intent；PM 使用 `task confirm-requirements` 按 run 确认，程序自动合并并保留技术路由字段。
- 新增负向测试确认 Analyst proposal 不能写 `needs_code`、owners 等技术／调度字段，PM 无需再生成第二份完整合同。
- 后续实质合同变更清除需求确认及下游门禁并重新要求分析；普通 Notes 不触发失效。
- 活动项目配置验证：PM `gpt-5.6-luna/low`、Analyst `gpt-6-astra/high`，六行均 `synced: true`。这些具体模型是活动项目选择，不写死到通用包默认 model。

## 正常开发与 QA 分工回归

常规 Task 流程保持为：PM 登记与派发 → Analyst 有界需求分析 → PM 用户确认与合同固定 → 按需 Designer／Design-Art → 用户批准 → Code／Art → 最终绑定。回归覆盖独立 worktree、按需技术调查、Code/Art 并行、无 Art 时不增加绑定步骤、视觉产物覆盖、暂停／停止／恢复、旧回执拒绝、篡改和路径越界。

正式 Release 的职责保持为：

- Feature Designer 固定 QA Plan、用例、断言、fixture/reset 和接口需求，不写脚本。
- Code Builder 只把固定计划绑定到 execution、脚本和 fixture；检查集合与 assertion ID 必须完全一致。
- 普通 Runner 执行并校验逐断言结果；无报告、零断言、错误身份、SKIP、超时、篡改和候选变化均不能当作通过。
- QA Reporter 默认关闭，仅在 PM 需要时只读总结。

机器能证明计划身份、覆盖映射和 assertion ID 集合未被 execution manifest 改写；不能静态证明脚本观察逻辑与自然语言期望完全等价，仍需代码审查与运行证据。

## 原生看板验证

原生模型页显示 PM Coordinator、Requirement Analyst、Feature Designer、Design-Art、Code Builder、QA Reporter 六行。页面文字明确 PM 为低算力常驻入口、Analyst 仅按需短时派发。后端模型测试覆盖 preview 无写入、etag 并发保护、手工漂移、事务恢复、回滚保留角色指令、禁用角色拒绝新派发和运行中快照不被新配置覆盖。

包内不再提供 HTML/HTTP 看板，因此不需要端口、token、浏览器服务或 Playwright；也没有把静态 HTML 截图当成原生界面证据。

## 未验证 / 边界

没有验证真实宿主是否会自动结束子 Agent，也没有真实模型调用、token/费用计量或模型 ID/effort 兼容性测试；本包以角色配置、run 身份和宿主动作要求约束生命周期，实际终止仍由宿主执行。

没有运行游戏引擎、实机截图采集、真实测试夹具、产品质量、美术验收、发布或平台操作。Runner 不是权限容器。测试证明有限的程序行为，不证明需求理解和自然语言断言必然正确。

## 打包复现

最终包从用户提供的 v1.0 ZIP 建立干净基线，生成二进制 Git 升级补丁；在独立副本执行 `git apply --check`、实际应用与逐文件 SHA-256 比对。完整 ZIP 同样逐文件比对。摘要和文件数写入 `PACKAGE_VERIFICATION.json`，ZIP／补丁摘要写入外部 checksums 文件，避免循环哈希。
