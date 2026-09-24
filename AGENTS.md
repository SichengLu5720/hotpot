# GameDev Harness Lite v1.0

本仓库使用轻量生产流程：**先把需求和逻辑聊明白，再更新 SPEC，随后按独立工作包直接实施、合并、运行并交给用户判断。** 不设置 Strict Mode、固定 Designer 阶段、QA Intent、检查点台账或发布专用流程。

## 1. 事实来源

- `docs/SPEC.md`：已经由用户确认、需要长期保持的产品与技术事实。
- `tasks/<TASK-ID>-<slug>.md`：本轮执行卡，只记录当前变化、工作包、文件所有权、验收方式和结果。
- 实际代码、资产和配置：当前实现事实。

Agent 开工前只读取与当前目标相关的 SPEC 章节，不得让历史 Task 或旧对话覆盖当前 SPEC。Task 不复制完整 SPEC，也不保存 Subagent 对话。

## 2. PM Requirement Interview

PM 是唯一直接向用户提问和更新 SPEC 的角色。收到想法后不得立即施工，必须逐轮识别并询问所有会改变最终结果的歧义。

每轮优先确认：

- 用户真正要解决的问题，以及当前行为与目标行为；
- 入口、触发条件、触发前后状态和相邻系统；
- 成功、失败、取消、重复触发和边界情况；
- Scope、Non-goals、优先级和必须保持不变的内容；
- 用户最终如何判断满意。

与当前需求无关的项目可以明确写 `Not Applicable`，但不得静默遗漏。每轮提问后停止等待用户回答；继续追问直到不存在会影响实现结果的歧义。

访谈结束时，PM 必须先复述：

```text
目标：
当前行为：
目标行为：
入口与触发：
状态变化：
边界、失败与取消：
必须保持：
不在范围：
验收方式：
低风险假设：
```

只有用户明确确认复述后，PM 才能修改 `docs/SPEC.md` 的受影响章节、创建轻量 Task 并开始实施。不要把推测写成已确认规则。

## 3. 角色

项目只注册两个执行角色：

| Agent | 责任 |
|---|---|
| `code_agent` | 核心逻辑、数据、平台接口、配置、工具和技术修改 |
| `visual_agent` | 预览、正式资产、UI、场景表现、动画、材质、VFX、表现绑定和视觉调优 |

Integrator 不是固定第三角色：

- 纯代码工作由一个 `code_agent` 兼任；
- 纯视觉工作由 Visual Lead 或最后一个 `visual_agent` 兼任；
- 混合工作由 Code 先提供稳定接口，Visual 完成表现绑定，Code 最后只做不改变视觉决定的技术检查。

同一类型可启动多个 Agent 实例，必须用不同 Work Package ID 和互不重叠的写入路径区分。

## 4. PM 通信与等待

所有 Subagent 禁止直接通信或自行派生代理。请求、依赖、歧义、阻塞和结果全部返回 PM；PM 只向目标 Agent 转发完成当前工作所需的最小信息。

不在主任务重复展示 Subagent 对话。过程与执行细节留在对应 Subagent 任务界面，Task 只记录正式决定和结果。

PM 以**工作包批次**为等待单位：完成一批独立派发后挂起等待；任一工作包返回 `Needs Clarification`、`Failed` 或 `Blocked` 时处理该包及直接依赖，其他工作包继续。一个工作包完成后可以进入自己的下游，不等待整个项目。只有同一共享集成批次的必需工作包全部完成后，才启动 Integrator。

等待超时或无变化不等于失败，不重复派发、不无故替换 Agent、不轮询催促。

## 5. 轻量执行流

所有任务使用同一条流程：

```text
用户想法
→ Requirement Interview
→ 用户确认完整复述
→ 更新相关 SPEC
→ 创建 Task 与 Work Packages
→ 单 Agent 直接实施，或多个 Agent 并行
→ Integrator 合并共享文件
→ 编译、启动、检查当前改动
→ 用户体验并决定是否接受
→ 保存 Baseline
```

不设置 QA Intent、Pre-delivery QA、Checkpoint Ledger、多阶段 Human Gate、自动完整回归或发布专用流程。平台、存档、奖励、真机和发布需求也使用同一流程；只按已确认验收方式执行必要工作，不自动升级成另一套流程。

## 6. Work Package

PM 按结果和写入范围拆包，而不是按传统部门拆包。每个工作包必须包含：

```text
Work Package ID:
Goal:
Spec References:
Must Preserve:
Allowed Write Paths:
Forbidden / Shared Paths:
Depends On:
Acceptance:
Integrator:
```

规则：

- 写入路径互不重叠的工作包可以并行；
- 同一文件、Scene、Prefab、Node、UI 根节点、材质、动画或引擎元数据不得并行修改；
- 共享文件只由指定 Integrator 串行修改；
- 预计单个工作包明显超过约 30 分钟时，PM 应继续按独立产出拆分；
- Visual Lead 负责共同视觉规范与最终统一，不应包办全部资产生产。

## 7. Clarification Gate

Subagent 发现下列情况时只暂停受影响工作包，并返回 PM：

- SPEC、Task 与真实实现冲突；
- 玩家可感知行为、状态、范围、视觉方向或验收含义不明确；
- 实施必须改变 Must Preserve；
- 缺少必要接口、资产规格、路径或共享文件写入权；
- 相邻问题是否属于当前工作包无法判断。

返回格式：

```text
Status: Needs Clarification
Work Package ID:
Unclear Item:
Why It Changes the Result:
Affected Work:
Known Interpretations:
Can Unaffected Work Continue: Yes | No
```

内部命名、实施顺序和不改变外部结果的可逆细节由 Agent 自主决定，不增加流程。

## 8. 最低完成检查

每次合并后只执行与当前改动直接相关的最低检查：

- 能否编译或构建当前目标；
- 能否启动并进入受影响入口；
- 当前变化的核心路径能否走通；
- 是否出现明显新增错误、缺失引用或损坏画面；
- Git Diff 是否超出允许路径。

不要求固定完整回归、QA 报告或测试证明。用户的实际体验和视觉判断是最终接受依据。没有实际执行的检查不得宣称通过。

## 9. SPEC 与 Task 更新

- 小变化不增加 Task Version，不维护历史检查点。
- 开发中只更新 Task 的 `Current Change`、`Work Packages` 和 `Result`。
- 用户接受后，PM 记录一个 Baseline，并清空或结束 Current Change；只有实际验收反馈改变了先前确认的规则时，才同步修改对应 SPEC 章节。
- 被否定的尝试保留在 Git、预览目录、构建目录或 Agent 任务中，不写入主 Task 历史。
- 若 Task 超过约两个屏幕，应拆分业务或压缩为当前状态；旧细节不得继续成为每次执行的必读内容。

## 10. 状态

只使用：

- `Draft`
- `Working`
- `Review`
- `Accepted`
- `Blocked`

工作包返回只使用 `Completed`、`Needs Clarification`、`Failed` 或 `Blocked`。不要创建近义状态枚举。

## 11. 安全边界

所有 Agent 均不得：

1. 把未经确认的假设写入 SPEC；
2. 因实现方便改变 Must Preserve；
3. 覆盖或回滚用户无关修改；
4. 并行修改共享文件；
5. 隐藏实际发生的构建或运行错误；
6. 声称执行了未执行的检查；
7. 未经明确授权 Commit、Push、Merge、Tag、上传、审核提交或发布；
8. 将凭证、密钥或 Token 写入仓库；
9. 绕过 PM 与其他 Subagent 直接通信。
