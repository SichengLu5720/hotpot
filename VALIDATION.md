# Harness 1.1 验证记录

日期：2026-09-20。验证对象仅为本工具包；没有游戏项目、真实模型会话、实际账号费用或平台发布参与。

## 已执行

| 检查 | 实际结果 |
|---|---|
| Python unittest（临时 Git 仓库 / worktree） | **72 tests，全部通过；222.803 秒** |
| Agent TOML + models.toml | 4 份 Agent + 1 份配置均通过 tomllib 解析 |
| Python 3.11 语法检查 | 11 个 Python 文件通过 AST `feature_version=(3,11)` 解析 |
| JavaScript 语法 | `node --check tools/harnesslib/web/app.js` 通过 |
| HTTP / 看板后端 | 标准测试实际启动 127.0.0.1 服务，验证 token、Origin、静态资源、安全响应头、无任意命令接口、四角色矩阵、预览/确认应用 |
| 看板名称与后端绑定 | 友好名称由后端返回；role key、配置键、Agent TOML 和 dispatch steps 一一核对，Designer 绑定 `designer/interfaces` |
| 离线 Chromium 界面检查 | **未运行**：当前验证运行时未安装 Python `playwright`；见 `validation/browser.json` |
| 详细 Task 模板兼容性 | 唯一 `harness-state` 可解析；workspace 创建会绑定身份且保留人类工作表；正常 Task 与 Release 派发测试通过 |

实际 Python 执行为 **Windows / Python 3.12.14**。测试显式使用 UTF-8 模式；同时修正 Runner 证据相对路径的 Windows POSIX 化，确保记录格式跨平台稳定。

原始 unittest 记录见 `validation/unittest.log`；界面检查状态见 `validation/browser.json`。包内 `docs/dashboard-preview.png` 是基线素材，不作为本次浏览器实测证据。

## 正常开发流程回归

本次没有删除或改写正常 Task 流程。详细 Task 工作表保留需求、QA Intent、接口、隔离、视觉资产、批准和 Release Handoff 的可读栏目，但状态、合同、批准、run 与修订仍以机器块为唯一真源。回归覆盖：独立 worktree 文件与索引、源工作区未提交修改保护、重复任务与错误分支；按需 Designer 技术调查、构建授权、Code/Art 并行接收、无 Art 时不增加绑定步骤、Art 后 Code 顺序最终绑定；实机预览/概念覆盖与实际资产校验；整 Task 暂停、确认停止后恢复、新 run 与旧回执拒绝；批准图篡改、非拥有路径和符号链接越界。

## QA 分工与门禁回归

新增验证覆盖：

- Release 必须先派发既有 `interfaces` 给只读 Feature Designer；Designer 内联提交 QA Plan、用例、断言、fixture/reset 要求和接口需求。
- Designer Plan 必须覆盖全部有来源 QA Intent，且不允许包含执行命令或脚本路径。
- Code Builder 的 `qa_scripts` 只能提交绑定固定 plan digest 的 execution 与脚本；check 集合和 assertion IDs 必须逐项完全一致。
- Builder 改写 assertion ID 会被拒绝；`release reopen-scripts` 保留 Designer Plan，只重开脚本实现。
- 普通 Runner 继续验证无报告、零断言、错误 run_id、SKIP、非零退出、超时、脚本/候选变化、产品文件被测试修改、证据篡改与执行中暂停隔离。
- 人工项、部分复测累计、当前候选完整覆盖及按需只读 Reporter 的边界保持不变。

机器能够证明的是计划身份、覆盖映射和 ID 集合未被 execution manifest 改写；它不能静态证明脚本内部观察逻辑与自然语言 expectation 语义完全等价。该责任仍由 Builder 指令、代码审查与 Runner 证据共同约束。

## 模型与配置回归

配置预览无写入、etag 并发保护、手工漂移、保留角色指令的回滚、崩溃事务恢复、禁用角色拒绝新派发、运行中模型目标快照不被新配置覆盖均通过。实际模型观测为人工转录接口的合成记录，未调用真实模型。

## 未验证 / 未提供

没有在用户的 Codex/Work 客户端加载角色或实际启动/取消 Subagent；没有自动宿主事件桥；没有验证模型 ID、effort 兼容性、token 或费用节省。

没有游戏引擎、实机截图采集、网络接口连接、真实测试夹具/脚本、产品质量或美术风格验收。Runner 的环境目录需要游戏实际接入；它不是权限容器。Windows 外部/脱离进程组作业停止仍需要真实宿主确认。

测试证明有限的程序行为，不证明需求理解、用户授权来源或测试断言语义一定正确。契约语义变化保守失效、旧候选证据不自动沿用，是本版明确边界。

## 打包复现

最终阶段从用户提供的 v1.0 ZIP 建立干净基线，生成二进制 Git 升级补丁；在独立副本执行 `git apply --check`、实际应用与逐文件 SHA-256 比对。完整 ZIP 同样做逐文件比对。摘要与文件数记录在 `PACKAGE_VERIFICATION.json`，ZIP/补丁本身摘要记录在外部 checksums 文件，避免循环哈希。
