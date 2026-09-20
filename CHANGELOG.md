# Changelog

## 1.1 — 2026-09-20

### QA 职责重新划分

- PM 继续只汇总并核对有来源的 QA Intent，不另立验收标准。
- Feature Designer 保持同一只读角色，在正式 Release 的既有 `interfaces` 步骤负责 QA Plan、用例、断言、fixture/reset 要求及接口需求。
- Code Builder 在既有 `qa_scripts` 步骤只复用/编写 execution manifest、脚本与 fixture；不得增删、合并、改名或改写 Designer 的验收项。
- 普通 Runner 继续执行 argv 并核验逐断言结构化结果；QA Reporter 仍默认关闭，仅按需只读总结。

### 机器门禁

- Designer Plan 与 Builder Execution 分离固定摘要；execution 必须绑定 plan digest。
- Builder execution 的 check 集合和各组 assertion IDs 必须与 Designer Plan 完全一致。
- `release reopen-scripts` 只重开 Builder 脚本交接并保留 Designer Plan，防止脚本维修顺带改变标准。
- 沿用现有四份 Agent 配置、六个 Task 主状态和现有 release 步骤；没有新增 Agent 或工作模式。

### 升级说明

v1.0 活动 release 的单体 Builder plan 不自动转换为 Designer 授权。停止并结清旧 run 后重新 `release prepare`，依次派发 `interfaces` 和 `qa_scripts`。Task 合同和已确认 QA Intent 结构不变。

### Task 模板

将精简机器模板替换为完整的需求、QA Intent、技术接口、隔离并行、视觉资产、批准与 Release Handoff 工作表，同时保留唯一 `harness-state` 块。状态、合同、批准、run 与 task_revision 仍只由机器块和统一入口维护；正文是可读起草区/索引，不能成为第二份流程真源。

### 看板名称与后端绑定

看板更名为 **Subagent 配置与后端绑定看板**。角色友好名称和说明改由 Python 后端提供，不再硬编码在前端；每行明确展示不可变 role key、`models.toml` 配置键、Agent TOML 路径及实际可派发步骤。后端绑定从 `models.ROLES` 与 `engine.STEPS` 生成，避免只改前端名称而仍指向旧角色。

## 1.0 — 2026-09-20

### 最终职责调整

交付 QA 脚本由同一个 **Code Builder** 统一复用/编写；Designer 始终只读，根据 QA 需要定位、调用已授权非变更型查询接口，返回参数/前置状态/观察字段，不写脚本、不区分读写模式。不再让每个 Task 强制调用 Designer 编写 QA_SPEC。

PM 维护 Task 内轻量 QA Intent；版本阶段由程序生成单份带来源 QA_BACKLOG，PM 核对；Builder 集中生成/适配 QA Plan 与脚本；普通 Runner 执行；基础报告程序生成，qa_reporter 默认关闭、按需只读总结。

### 工作流减法

- 公开入口统一为 tools/harness.py，Gate、Workflow、Workspace 合入内部模块。不是把所有逻辑塞进一个大文件。
- 删除固定 Workflow Monitor 和独立 Release QA Builder 配置，四份子代理配置 = 三个核心角色 + 可选 Reporter。
- 六个 Task 主状态，批准和执行结果不再膨胀成额外状态。
- PM 不再手抄 in_flight/dispatch_epoch；程序持久化 run_id 和身份，幂等接收、加锁写入、拒绝撤销/旧输入。
- 派发给精简 context，不包含全 Task 历史。
- 资产事实清单自动生成；Art 只维护机器不能推断的语义。

### 新的可执行组件

- 一个普通 Runner，固定候选/脚本、逐断言报告、缺失/零执行拒绝、部分复测覆盖汇总、人工项待评、证据摘要。
- 单源 models.toml，同步器带 diff / etag / Apply / 事务恢复 / 回滚；保留未受管的角色指令。
- 本地 Subagent Model Dashboard：档位、单角色覆盖、启停、批量草稿、目标/原生/观测区分。不访问模型或收费接口。

### 仍保留

实机局部改图与实现前视觉确认；整 Task 歧义暂停；独立 worktree；Code/Art 受控并行与最终顺序绑定；版本集中 QA，不恢复每 Task 冒烟。

### 迁移和未实现项

不是 v0.9 状态文件的原地兼容升级。旧任务/回执先停止、归档和人工迁移，不机械继承旧授权。原生宿主启动/停止、实际模型/费用遥测、游戏接口与平台发布仍需项目适配。

1.0 对语义合同变更保守清空该 Task 的门槛，不做自动局部语义失效推断；不自动沿用旧候选的测试证据。路径范围和子代理禁止事项是协议与入口检查，不构成容器权限隔离。

未宣称已经测得降低多少 token、时间或费用。验证范围见 VALIDATION.md。
