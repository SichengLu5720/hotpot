# 数据格式与接口

这些格式用于机械检查，不是对用户意图的自动判决。所有示例 ID/接口都是示意；实际值取自派发和项目，不照抄为真实证据。

## 1. 唯一 Task 记录

Task Markdown 只允许一个 `harness-state` JSON 块。`contract` 是确认需求/QA Intent/接口/资产要求的来源。`runs`、`completed`、`approvals`、`history` 由 `harness.py` 操作。追加 Notes 不属于合同，不改变 task_revision。

合同完整结构见 `examples/contract.example.json`。机器必须字段：

| 字段 | 含义 |
|---|---|
| goal / qa_intent | 确认目标；Intent 每项只有 id、text、source |
| technical_design_required | 是否确实需要独立 Designer 调查；不是默认必需 |
| visual_impact | none / asset / screen / major |
| needs_code / needs_art | 是否需要代码实现、美术生产；至少一项为真 |
| owners | code_builder / design_art_agent 的明确相对路径列表；不重叠 |
| references | path、sha256，加来源/视口/状态等描述；screen/major 必须有参考 |
| dependencies | 固定 task_id、task_revision、完整 commit |
| shared_touchpoints | resource、region、coordinator、resolution；就绪前解决共享区域问题 |
| interface_map | 真实入口与调用方式、来源、前置状态；未知明确记录 |
| asset_contract | 唯一 id 和生产规格/用途/状态/pivot 等语义；无资产用空数组 |
| runtime_isolation | 实际运行数据隔离适配情况；默认 unconfigured 不能当作已隔离 |
| generation_budget | 约定预算与依据；不是对第三方生成服务的硬费用限制 |

示例中的 code 所有权只覆盖 game/；真实项目需改成自己的模块。即使 Art Only，最终绑定也需要给 Code Builder 明确的路径范围。

产物实际摘要由程序计算。不要使用“latest”覆盖已批准图；新的文件内容要对应新的返回/批准。Python 的 `hashlib.sha256(path.read_bytes()).hexdigest()` 可用于初次录入已有参考；它只证明内容，不证明来自实机。

## 2. 子代理交接

所有子代理使用同一信封，不自己更新 Task：

```json
{
  "run_id": "派发返回的run_id",
  "role": "code_builder",
  "status": "READY",
  "summary": "只总结本次已经完成的工作",
  "game_qa": "NOT_RUN",
  "payload": {
    "implementation_facts": {
      "entrypoints": ["项目真实入口与来源"],
      "known_errors": [],
      "untested": ["尚未进行版本集中QA"]
    }
  },
  "artifacts": [
    {"path": "game/实际修改文件", "kind": "file"}
  ]
}
```

必需 payload：Task 的 designer → `interface_notes`；Release 的 interfaces → `qa_plan`（内联对象）；design → `visual_mapping`；code/release_fix → `implementation_facts`；art → `assets`；integration → `binding_notes`；qa_scripts → `qa_execution`（执行清单相对路径）；qa_report → `report`。它们是现有角色任务，不是新的运行模式或角色配置。

`design` 中，screen/major 需要 kind=`preview` 和 `editable_source` 或 `composition_recipe` 的文件；有正式资产时每个 asset_contract ID 都要对应 concept 文件：

```json
{
  "visual_mapping": {
    "asset_concepts": [
      {"asset_id": "daily-icon", "concept_path": ".harness/concepts/TASK-001/c001/icon.png"}
    ],
    "elements": ["已有场景复用，文字走原生控件"]
  }
}
```

一个概念图可以覆盖多项资产，但应有足以生产的明确规格。代码只能检查映射和内容身份，无法证明图片一定高精度；用户批准与实际视觉验收仍是独立事实。

生产时不用手写 SHA 或尺寸，返回明确资产列表即可：

```json
{
  "assets": [
    {"asset_id": "daily-icon", "path": "assets/ui/daily-icon.png", "action": "upsert"},
    {"asset_id": "old-icon", "path": "assets/ui/old-icon.png", "action": "delete"}
  ]
}
```

资产语义来自对应 asset_contract。程序生成 Manifest；integration 会核对清单及实际正式文件，防止只验证清单本身而遗漏资源已被改写。PNG/GIF 图像头元数据可以读取，其他格式为 unknown；这不是完整解码或画质检查。

遇到歧义以同一 run_id 返回 `status: NEEDS_CLARIFICATION`、`reason`、冲突/选项。接收后整 Task 阻断。协议不把新产品假设自动填入合同。

## 3. 固定版本输入

`release prepare --inputs file.json` 接收数组：

```json
[
  {"id": "TASK-001", "commit": "完整的已集成提交摘要", "document": "tasks/TASK-001-feature.md"}
]
```

这些提交必须是当前 release HEAD 的祖先，Task 已到 READY_FOR_RELEASE，已接收产物必须原样提交。版本输入只抽取目标、QA Intent、接口、实现事实和拥有路径，不复制全部对话历史。

程序生成 `releases/<id>/QA_BACKLOG.md`；该文件是视图，Designer 计划的权威输入身份为固定快照 digest。PM 核对来源，不在视图里独立改标准。

## 4. Designer 的 QA Plan

计划由只读 **Feature Designer** 通过 `interfaces` 交接内联返回，由程序固定摘要并生成 `QA_PLAN.md` 视图。计划定义用例、断言和接口需求，不包含命令、脚本路径或执行超时。示例中的 digest、接口与具体阈值必须取自项目：

```json
{
  "schema_version": 2,
  "release_id": "v0.1.0",
  "input_digest": "release prepare返回的input_digest",
  "interface_requirements": [
    {
      "id": "IF-VOLUME-READ",
      "requirement": "可在隔离存档重启后读取当前音量值",
      "source": "TASK-001 implementation facts + AC-1",
      "intent_ids": ["TASK-001:AC-1"]
    }
  ],
  "checks": [
    {
      "id": "VOLUME-PERSIST",
      "case": "在隔离存档写入音量、重启并读取恢复值",
      "fixture_reset": "每次测试使用独立临时存档；结束后清理，不共享可变用户状态",
      "interface_requirement_ids": ["IF-VOLUME-READ"],
      "assertions": [
        {
          "id": "volume-restored",
          "expectation": "读取的音量等于先前写入的值，来自已确认验收项",
          "intent_ids": ["TASK-001:AC-1"]
        }
      ]
    }
  ],
  "manual_checks": []
}
```

每个 Intent 至少有一个自动断言或人工项覆盖；每个断言必须有来源。接口需求也映射到来源 Intent。不同断言不能因实现方便被合并、删除或改写；人工项由 Designer 明确，Builder 不能把主观验收擅自改成自动通过。

## 5. Builder 的 QA Execution

Code Builder 读取固定 Designer 计划，复用或编写脚本、fixture 与适配器，并返回 `qa_execution` 文件。执行清单只绑定计划，不能重述或更改 expectation / intent_ids：

```json
{
  "schema_version": 2,
  "release_id": "v0.1.0",
  "input_digest": "release prepare返回的input_digest",
  "qa_plan_digest": "派发context中的固定Designer plan摘要",
  "files": ["tests/release/check_volume.py", "tests/release/fixture.json"],
  "checks": [
    {
      "id": "VOLUME-PERSIST",
      "command": ["{python}", "tests/release/check_volume.py"],
      "timeout_seconds": 60,
      "fixture_implementation": "测试入口把HARNESS_RUNTIME_ROOT绑定为存档根并在结束后清理",
      "assertion_ids": ["volume-restored"]
    }
  ]
}
```

机器要求 execution 的 check 集合和每组 assertion IDs 与 Designer 计划完全相等，并绑定计划 digest。超时只是执行保护，不自动成为新的产品性能要求。`files` 列出脚本、fixture、必要包装器/资源；已有测试可复用，不必复制出另一套。测试框架已有 JSON/JUnit 输出时由 Builder 复用适配器转成本格式；本包不假装原生支持所有游戏框架。

命令是 argv 数组，`{python}` 替换为当前解释器；没有 shell 拼接。运行有副作用：只在已准备且授权的 release 工作区用 `qa run --apply`。测试是受信项目代码，不是沙箱，禁止下载/执行不可信脚本。

## 6. 普通 Runner 的检查结果

Runner 为每个命令设置：

| 环境变量 | 内容 |
|---|---|
| HARNESS_RUN_ID / HARNESS_CHECK_ID | 本次 Runner 与检查 ID |
| HARNESS_RESULT_PATH | 本次新建的结构化报告文件位置 |
| HARNESS_RUNTIME_ROOT | 本检查独立运行目录；游戏仍需真实接入 |
| HARNESS_CANDIDATE | 固定候选提交 |

脚本向 HARNESS_RESULT_PATH 写入：

```json
{
  "schema_version": 1,
  "run_id": "读取环境变量，不复用旧值",
  "check_id": "读取环境变量",
  "assertions": [
    {"id": "volume-restored", "status": "PASS", "actual": 0.5}
  ],
  "evidence": [".harness/qa/实际本次证据相对路径"]
}
```

断言状态为 PASS / FAIL / ERROR / SKIP。必须全部与计划匹配并给 actual；未执行不能写 PASS。退出码非零不能被 JSON 里的 PASS 覆盖。没有结果、零断言、重复或缺失断言、旧 run_id 都无法通过。

每次结果目录唯一，不复用旧报告。基础报告保留日志与证据摘要；`qa report` 合并同一候选/脚本上的结果，后来失败不会被更早成功覆盖。结果/证据被修改会拒绝汇总。

人工项需要 ID、instruction、intent_ids。用 `qa manual --id ... --status PASS|FAIL --evidence ... --decision ...` 记录真实人工结论；工具仅验证文件和身份，不认证是谁作出决定。

脚本、产品或配置变更后重新固定候选和授权，旧候选结果不可用于宣称新版本通过。定向复测不等于全版本覆盖已完成。Runner 不依赖模型逐帧操作游戏。

## 7. 模型配置、事务与宿主接口

`models.toml` 的 profiles 提供 model/effort，agents 可以选择 profile 或覆盖两项。空覆盖继承 profile，profile 的空 model/effort 继承宿主；默认档名没有价格保证。`enabled` 只控制 Harness 新派发，不是独立 Codex 角色权限属性。

原生 TOML 中两个标记之间的字段由同步器维护，其外角色指令和配置保留。不能把额外 model 键放到别处造成双定义。Apply 使用全文件预览 etag 拒绝并发覆盖；配置事务含 before/after，崩溃恢复不能越过外部人工修改。

`observe --doc ... --run ... --model ... --effort ... --evidence ...` 是 **PM 转录宿主观测**，不是模型 API 查询、签名证明或自动费用统计；无可信宿主记录就不要调用它冒充实测。

宿主适配点是：派发后真正 spawn、返回 handoff、block 后 cancel 与 stopped 证据、实际模型/费用观测。当前包没有实现某个客户端的专用事件桥。直接绕过本地入口的文件写入/Agent 启动不受本工具强制管控。
