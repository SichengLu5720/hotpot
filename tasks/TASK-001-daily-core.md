# TASK-001 Daily 确定性玩法内核

> 使用 `workspace task` 创建，不要直接复制未绑定模板派发。下方唯一 `harness-state` 是状态、合同、批准和运行记录的机器权威源；正文是 PM 的可读工作表与索引，不能覆盖机器块。
> 本文由 PM 维护，子代理返回结果后由 PM 通过统一入口接收。删除不适用的正文区块时保留原因；未知信息写“待确认”，不能把空白或默认值当作用户决定。
> Task 阶段只维护 QA Intent；版本交付时由 **Designer 制定 QA 计划、用例、断言与接口需求，Builder 编写或复用脚本，普通 Runner 执行**。

```harness-state
{
  "schema_version": 1,
  "kind": "task",
  "id": "TASK-001",
  "workspace": "C:\\Users\\charlielu\\Documents\\ChatGPT\\下锅喽-worktrees\\TASK-001-daily-core",
  "branch": "task/TASK-001-daily-core",
  "base_commit": "2eef7ee6133f7839a632ee8d8d864779281a1107",
  "runs": {
    "run-c7e69831ca2d40ff": {
      "run_id": "run-c7e69831ca2d40ff",
      "step": "designer",
      "role": "feature_designer",
      "status": "ACCEPTED",
      "created_at": "2026-09-20T09:14:27.960538+00:00",
      "inputs": {
        "task_revision": 2,
        "contract_digest": "b745952fb8082122c33803b99d70bd658461ed434eeb353ca8ed28c1f8846e57"
      },
      "model_target": {
        "model": "gpt-6-astra",
        "effort": "low",
        "enabled": true,
        "config_digest": "28a8c3d034818ad41a01baf3f947b864e50cba7de4512f06354977085cc0f02e",
        "actual": "unknown"
      },
      "model_observed": null,
      "allowed_paths": [],
      "stop_confirmed": true,
      "approved_inputs": {},
      "handoff_digest": "0f295bf4c14882693b3fbaf79e57323c9cba29e65e8c0e48422159ed5f7316af",
      "accepted_at": "2026-09-20T09:18:32.299140+00:00",
      "payload": {
        "interface_notes": [
          {
            "id": "IN-001",
            "name": "Compatibility boundary",
            "requirements": "Retain existing ChallengeContext, IGameSession, IGameSessionFactory, GameSnapshot, GameEventBatch, ReplayPackage and public Contracts. Implement Daily-specific Tap/Supply DTOs and versioned JSON under owned Runtime/Core paths. Resolving remains internal. Old XorShift32, mutable collections, and old full-buffer failure behavior are not reused."
          },
          {
            "id": "IN-002",
            "name": "Content import",
            "requirements": "Import fixed skeleton C and exactly 20 Difficulty 3 rows. Convert decimal weights by exact decimal multiplication by 100; reject non-integral conversion. Preserve source hash, importer version and profile. Runtime content has no Difficulty branch. Assign plate IDs from source order and item IDs 1..183 by plate/source index; kinds are A..P. Reject missing, duplicate, negative, all-zero, digest, count and ID errors."
          },
          {
            "id": "IN-003",
            "name": "Seed and PCG32_v1",
            "requirements": "dailySeed = first UInt64 big-endian of SHA256(UTF8 without BOM of HOT_POT_DAILY|ChallengeId|ContentVersion). Derive each stream from SHA256(UInt64BE(dailySeed) + UTF8('|MappingRng'|'|DirectorRng'|'|PresentationRng')), first UInt64 big-endian. PCG32 XSH-RR 64/32 uses multiplier 6364136223846793005, initstate=streamSeed, initseq=54, standard two-step seeding, public drawIndex=0 after seeding. NextBounded uses threshold=unchecked(0u-bound)%bound and counts every raw draw including rejection. NextBounded(1) consumes a draw. Normal weighted Director consumes category then candidate draws; deterministic fallbacks consume only documented tie-pool draws. Opening selection consumes none. Map stable ingredient IDs with descending Fisher-Yates. Expose algorithm version, state, inc, drawIndex and Director raw/bounded values; reject unknown versions."
          },
          {
            "id": "IN-004",
            "name": "Reservations and whole-plate cost",
            "requirements": "External pool is Pending, ActiveAvailable and Buffer only. Reserve other orders' unmet items by slotId, then Buffer index, Active plateId/source index, Pending queue/source index. Never move items during reservation. Candidate legality, b, v and scan use the same reservation view. b is unreserved matching Buffer; v is unreserved matching ActiveAvailable and ignores occlusion. If scanning Pending, finish the whole plate that supplies the final needed target and count every non-target item on all scanned plates, including the rest of the final plate and non-target items reserved for other orders. Only an empty strict legal pool enables duplicate relaxation; strict out-of-band candidates use MinCostOutsideBands. Log reservation item IDs and all cost inputs."
          },
          {
            "id": "IN-005",
            "name": "Session, Tap and Supply",
            "requirements": "DailySessionFactory creates fresh isolated sessions. TapCommand includes itemId, inputSeq, logicalBoundary and hitAccepted. SupplyObservation includes observationSeq, logicalBoundary, canSpawn and cooldownReady. Boundaries are non-negative monotonic injected integers serialized as decimal strings; no wall clock or input lock. Tap transactions synchronously reserve, route, settle and chain under internal Resolving, then publish immutable results. Overflow remains accepted=true and Failed, increments player taps, does not remove or process the item, and releases Reserved before close. Supply commits exactly one Pending head per allowed call and records plate/item IDs; it never drains from one permission. Initialization does not spawn without an observation. No 0.20-second lock and no fixed chain guard."
          },
          {
            "id": "IN-006",
            "name": "Canonical state, events and hash",
            "requirements": "Use daily_state_v1, daily_event_v1 and canonical_json_v1: UTF8 without BOM/whitespace, ordinal object-property ordering, contractual array order, integers/booleans/strings/null only, rational progress, decimal strings for UInt64, stable escaping, lowercase SHA256. Core hash includes content and algorithm identity, dailySeed, mapping, state, Pending/Active/item locations, fixed buffer, four order slots, completed and core statistics, deterministic failure identity, Mapping/Director RNG state/inc/drawIndex, transactionId and eventSeq. Exclude sessionId, retryIndex, timeSource, platform/build/time, Presentation RNG, animation/coordinates, rejected diagnostics, external input/observation sequence and hash field. Core eventSeq starts at 1; rejections do not consume it. TransactionClosed hashAfter is the closed stable state."
          },
          {
            "id": "IN-007",
            "name": "Replay and constructability",
            "requirements": "daily_replay_v1 records context, configuration hashes, algorithm versions, initial hash, accepted taps, actual SupplyCommit records, effective Pause/Resume boundaries and post-commit hashes, plus separate diagnostics. Replay creates a fresh object, validates identities, applies records in order, uses an internal supply replay path that verifies exact queue head and item IDs, never re-solves physics, and reports the first mismatch. Unknown versions, digest mismatch, illegal supply order, non-reproducible tap or hash mismatch are failures. Export is in-memory; persistence belongs to the caller."
          }
        ]
      },
      "summary": "Read-only interface investigation completed. Harness check was ACTIVE at task revision 2. Determinism and implementation interfaces are fixed below; no product ambiguity remains, no files were modified, and no game QA was run.",
      "artifacts": []
    }
  },
  "history": [
    {
      "at": "2026-09-20T09:12:45.042217+00:00",
      "event": "created"
    },
    {
      "at": "2026-09-20T09:14:17.729483+00:00",
      "event": "contract_changed",
      "revision": 2,
      "decision": "User authorized TASK-001 implementation and confirmed Q-A01 through Q-A04 by saying execute; Q-A05 and Q-A06 remove the two protections.",
      "invalidation": "all dependent task completions; unchanged history/artifacts retained"
    },
    {
      "at": "2026-09-20T09:14:27.974808+00:00",
      "event": "dispatched",
      "run_id": "run-c7e69831ca2d40ff",
      "step": "designer"
    },
    {
      "at": "2026-09-20T09:18:32.311141+00:00",
      "event": "accepted",
      "run_id": "run-c7e69831ca2d40ff"
    },
    {
      "at": "2026-09-20T09:18:38.650089+00:00",
      "event": "approved",
      "scopes": [
        "build"
      ],
      "decision": "User explicitly authorized TASK-001 implementation with 实行task1 and confirmed the remaining recommendations with 执行; build scope is the registered revision 2 code-only contract and accepted Designer interface notes."
    }
  ],
  "task_revision": 2,
  "status": "READY",
  "contract": {
    "goal": "Implement a Unity-independent deterministic Daily Challenge gameplay core for fixed skeleton C. Given the same ChallengeContext, versioned content, accepted inputs, supply commits, and logical boundaries, it must reproduce the same mapping, events, replay hash, and final state. The implementation includes content import, PCG32 streams, inventory, orders, buffer, Director, transactions, snapshots, diagnostics, and replay. It excludes Unity scenes, visual assets, physics, WeChat SDK integration, publication, and release QA.",
    "qa_intent": [
      {
        "id": "QI-001",
        "text": "Validate exactly 50 plates, 183 items, 16 kinds, unique continuous plate IDs, item counts divisible by three, and explicit errors for invalid content.",
        "source": "Daily SPEC CFG 01-03 and task draft section 2"
      },
      {
        "id": "QI-002",
        "text": "Import the 20 Difficulty 3 weight rows by exact decimal multiplication by 100, preserve source hash and ratios, and reject missing, duplicate, negative, or all-zero rows.",
        "source": "Daily SPEC CFG 04-06; user decision Q-A01 on 2026-09-20"
      },
      {
        "id": "QI-003",
        "text": "Repeated initialization with identical ChallengeContext and content produces identical mapping, opening orders, initial events, and hashes; all 16 kinds map one-to-one.",
        "source": "Daily SPEC DET 01 and task draft QI-003"
      },
      {
        "id": "QI-004",
        "text": "Mapping, Director, and Presentation use independent versioned PCG32 streams; Presentation draws never alter Director output and draw indices remain observable.",
        "source": "Daily SPEC DET 03 and section 6"
      },
      {
        "id": "QI-005",
        "text": "Reset constructs a fresh state and reproduces the same result for identical accepted inputs, supply commits, pause boundaries, and logical timing boundaries.",
        "source": "Daily SPEC DET 04-05; user decision Q-A03"
      },
      {
        "id": "QI-006",
        "text": "Opening orders use first-ten-plate counts and produce C then H for standard skeleton C without consuming Director RNG.",
        "source": "Daily SPEC section 11"
      },
      {
        "id": "QI-007",
        "text": "Progress and buffer bands honor exact thresholds, with 0.40 in Early and both buffer occupancy 4 and 5 in B4.",
        "source": "Daily SPEC DIR 01-03 and section 12"
      },
      {
        "id": "QI-008",
        "text": "Director costs use reserved external inventory: Pending, Active, and Buffer minus other orders' unmet demand; items already in orders are not counted again.",
        "source": "User decision Q-A02 on 2026-09-20"
      },
      {
        "id": "QI-009",
        "text": "Director filters illegal and empty categories, normalizes remaining weights, and uses stable kind ordering plus versioned bounded PCG32 selection.",
        "source": "Daily SPEC section 13.6; user decision Q-A04"
      },
      {
        "id": "QI-010",
        "text": "All fallback levels are deterministic and diagnostic: zero-weight pressure order, duplicate relaxation, minimum out-of-band cost, legal empty tail slot, and Aborted for unexplained all-empty state.",
        "source": "Daily SPEC section 13.7"
      },
      {
        "id": "QI-011",
        "text": "Buffer occupancy 4 to 5 continues; matching orders accept items at 5 of 5; only a nonmatching tap with no empty buffer slot triggers Failed without removing the source item.",
        "source": "Daily SPEC BUF 01-03 and user decision Q-A05"
      },
      {
        "id": "QI-012",
        "text": "Routing prefers the matching order with more received items then lower slot ID; repeated item submission cannot mutate inventory twice.",
        "source": "Daily SPEC BUF 05-06 and section 9"
      },
      {
        "id": "QI-013",
        "text": "Auto-absorb scans buffer slots 0 through 4 without compaction and chains until stable through finite inventory; no fixed input lock or fixed chain-count guard may change a legal transaction.",
        "source": "User decisions Q-A05 and Q-A06 on 2026-09-20"
      },
      {
        "id": "QI-014",
        "text": "Winning requires 183 completed items plus empty Pending, Active, Buffer, and unresolved order contents; inconsistent counts enter Aborted.",
        "source": "Daily SPEC SUP 03-06 and section 14"
      },
      {
        "id": "QI-015",
        "text": "Every item has one location, totals are conserved, IDs are unique, capacities hold, terminal states are exclusive, and invariant errors produce readable diagnostics.",
        "source": "Daily SPEC section 17"
      },
      {
        "id": "QI-016",
        "text": "Events have stable transaction ordering and sequence IDs; replay records accepted taps, supply commits, and pause/resume input boundaries without re-solving physics.",
        "source": "Daily SPEC sections 15, 18, 25; user decision Q-A03"
      },
      {
        "id": "QI-017",
        "text": "Core hashes use a canonical versioned schema that excludes retry count, platform metadata, Presentation RNG, and rejected-input diagnostics.",
        "source": "User decision Q-A04 on 2026-09-20"
      },
      {
        "id": "QI-018",
        "text": "Long simulations must not lose inventory, create negative counts, loop indefinitely, or produce unexplained empty orders; there is no fixed 65-chain assertion.",
        "source": "Daily SPEC section 19.6; user decision Q-A06"
      },
      {
        "id": "QI-019",
        "text": "Statistics distinguish player taps, rejected diagnostics, processed items, auto-absorbed items, completed orders, peak buffer occupancy, and failure reason.",
        "source": "Daily SPEC section 18.3; user decision Q-A05"
      }
    ],
    "technical_design_required": true,
    "visual_impact": "none",
    "needs_code": true,
    "needs_art": false,
    "owners": {
      "code_builder": [
        "Unity/Assets/HotpotSort/Runtime/Core",
        "Unity/Assets/HotpotSort/Runtime/Determinism",
        "Unity/Assets/HotpotSort/Runtime/Replay",
        "Unity/Assets/HotpotSort/Content/Daily",
        "Unity/Assets/HotpotSort/Editor/ContentImport"
      ],
      "design_art_agent": []
    },
    "references": [
      {
        "path": "docs/task-drafts/TASK-001-daily-core.md",
        "sha256": "52c97c3765bcad3c591d4c74402c9802d6576d1148acceb0aba16bf1cf3ba8f7",
        "bytes": 34454
      },
      {
        "path": "docs/DAILY_UNITY_WECHAT_THREE_PARALLEL_TASKS.md",
        "sha256": "dc1d3e902e37762ab66befc17d341d8d356383c80bef6cc19b7e360e9a3f1d29",
        "bytes": 19284
      },
      {
        "path": "docs/PROTOTYPE_v0.1_REVIEW_AND_TASK_DELTA.md",
        "sha256": "4caa9dd458526fb485a57e3ca4703f72c321a864a36756b8f29c2c77dd59bea9",
        "bytes": 18062
      },
      {
        "path": ".harness/references/prototype-v0.1/HotpotSort_Prototype_v0.1/shared/reference/LevelDifficultyConfig.json",
        "sha256": "40031a2b589537f7c186354b77506d1662179d424b2aa9244ded1edc1a689bbe",
        "bytes": 10863
      },
      {
        "path": ".harness/references/prototype-v0.1/HotpotSort_Prototype_v0.1/shared/reference/skeleton_C.json",
        "sha256": "7fb75bf293a460ac6bd64a436c180a760fef0f0a0f2d49c2b63bf1b4f09bf452",
        "bytes": 26233
      },
      {
        "path": ".harness/references/prototype-v0.1/HotpotSort_Prototype_v0.1/shared/game-data.json",
        "sha256": "55930baff402fed70c618a52144a0d6d828858e7dfe7b78e204fbf5c5deb6982",
        "bytes": 15920
      }
    ],
    "dependencies": [],
    "shared_touchpoints": [
      {
        "resource": "ChallengeContext and session commands",
        "region": "TASK-003 to TASK-001 boundary",
        "coordinator": "PM",
        "resolution": "TASK-001 accepts an injected immutable context and never reads system date or platform APIs."
      },
      {
        "resource": "SupplyObservation and SupplyCommit",
        "region": "TASK-002 to TASK-001 boundary",
        "coordinator": "PM",
        "resolution": "TASK-002 supplies space and hit facts; TASK-001 alone consumes the Pending queue and records replay commits."
      },
      {
        "resource": "GameSnapshot and GameEventBatch",
        "region": "TASK-001 output to TASK-002 and TASK-003",
        "coordinator": "PM",
        "resolution": "Consumers receive immutable DTOs and cannot mutate core inventory or RNG state."
      }
    ],
    "interface_map": [
      {
        "name": "ChallengeContext/CreateSession/ResetSession",
        "direction": "TASK-003 -> TASK-001",
        "semantics": "Injected challenge ID, content and algorithm versions, configuration hashes, time source identity, and retry metadata; reset creates a fresh state."
      },
      {
        "name": "TapCommand/TapResult",
        "direction": "TASK-002 -> TASK-001 -> consumers",
        "semantics": "Core validates item identity and hit fact, performs one atomic transaction, and returns stable result and events."
      },
      {
        "name": "SupplyObservation/SupplyCommit",
        "direction": "TASK-002 -> TASK-001 -> TASK-002",
        "semantics": "Core consumes only the Pending head when allowed and records the actual commit in replay."
      },
      {
        "name": "GameSnapshot/GameEventBatch",
        "direction": "TASK-001 -> consumers",
        "semantics": "Immutable stable IDs, fixed buffer slots, orders, progress, statistics, terminal state, event sequence, and transaction sequence."
      },
      {
        "name": "ReplayPackage/Replay",
        "direction": "TASK-001 -> TASK-003 or runner",
        "semantics": "Versioned context, hashes, accepted taps, supply commits, pause boundaries, and canonical core-state hashes; no physics re-simulation."
      }
    ],
    "asset_contract": [],
    "runtime_isolation": {
      "status": "not_applicable_to_pure_core",
      "decision": "The module must not write persistent files, use static mutable session state, or read platform clocks. Each session is an isolated object; external sinks own persistence."
    },
    "generation_budget": {
      "limit": 0,
      "decision": "No image or external content generation is authorized or required for this code-only task."
    }
  },
  "approvals": {
    "build": {
      "task_revision": 2,
      "contract_digest": "b745952fb8082122c33803b99d70bd658461ed434eeb353ca8ed28c1f8846e57",
      "decision": "User explicitly authorized TASK-001 implementation with 实行task1 and confirmed the remaining recommendations with 执行; build scope is the registered revision 2 code-only contract and accepted Designer interface notes.",
      "artifacts": []
    }
  },
  "artifacts": {},
  "completed": {
    "designer": "run-c7e69831ca2d40ff"
  },
  "block": null
}
```

## 0. 任务标识

| 字段 | 内容 |
|---|---|
| Task ID | <从机器块读取，不在此另立值> |
| task_revision | <从机器块读取> |
| Status | <从机器块读取> |
| 负责 PM／会话 | <唯一业务协调者> |
| 类型／风险 | <Feature / Bug / Tuning / Tooling / Art>；<Low / Medium / High，说明实际风险> |
| 实现内容 | <Code Only / Art Only / Code + Art> |
| 视觉影响 | <None / Asset Only / Screen or Layout / Major Redesign> |
| 目标版本 | <尚未指定或 Release ID；最终纳入以版本记录为准> |
| 创建日期 | <YYYY-MM-DD> |

Status 只由机器块和 `harness.py` 维护：`DRAFT / READY / BUILDING / BLOCKED / READY_FOR_RELEASE / CANCELLED`；本节仅作可读索引。
`READY` 表示必要合同与构建授权齐全；`READY_FOR_RELEASE` 只表示实现交接完成，不表示 QA 通过或获得发布授权。

## 1. 需求、范围与约束

### 1.1 用户请求与目标

- 原始请求／忠实摘要：<保留真正提出的需求，不自行补充新目标>
- 来源：<用户消息、批注或已批准需求的可定位引用>
- 当前问题：<现在发生什么，为什么需要修改>
- 目标与用户可观察结果：<完成后用户能看到或做到什么>

### 1.2 功能范围

- 必须实现：<包含哪些行为、页面、状态和资源>
- 明确不做：<排除的功能、页面、重构或变体>
- 必须保留：<不允许改变的既有行为、数据、布局或美术元素>
- 硬约束：<平台、性能、兼容性、预算、依赖等；每项标明来源>
- 已确认前提：<前提及用户确认依据；没有则写“无”>

### 1.3 业务规则与状态变化

| 规则／场景 | 触发条件与输入 | 预期行为／状态变化 | 异常、空值或限制 | 确认依据 |
|---|---|---|---|---|
| <规则> | | | | |

尚未决定的产品行为记入第 6 节，不作为隐含默认值交给 Builder。

## 2. 验收意图／QA Intent

本节是 QA Intent 的人工起草／可读视图，由 PM 忠实汇总；确认后必须通过 `task contract` 写入机器块的 `contract.qa_intent`，后者才是唯一权威源。这里只定义“要验证什么”，不提前写完整测试步骤、坐标、等待时长或脚本。

| Intent ID | 场景／前置条件 | 应验证的结果或受保护行为 | 来源／决定依据 | 版本验证性质 |
|---|---|---|---|---|
| QI-001 | <场景> | <可观察预期> | <用户确认或既有规则> | <自动化候选 / 人工 / 待评估> |

- 需覆盖的边界与失败路径：<已知边界及对应 Intent ID；没有则说明>
- 跨功能交互与兼容性：<关联 Task、共享状态、存档或公开接口及对应 Intent ID>
- 用户特别关注项：<引用 Intent ID，不再复制另一套断言>
- 人工体验／视觉判断：<引用 Intent ID，并说明不能仅由数值或比图代替的部分>
- 待确认的回归候选：<技术建议、依据及待决定内容；未确认前不视为新增验收门槛>
- 已知历史问题：<症状、来源证据及其与本次需求的关系；未知不得写成“历史遗留”>

不得凭空增加阈值、设备范围或产品规则。对验收含义有歧义时，由 PM 询问用户；Builder 不得根据“当前代码恰好如此”反推正确预期。

## 3. 技术设计与接口（按需）

技术调查是否需要：<需要及原因 / 不需要及理由>。简单任务可引用已确认接口，不强制为每个 Task 再启动一次 Designer。

### 3.1 当前实现与推荐方案

- 已调查的入口、模块、场景与资源：<路径、符号和必要的固定提交>
- 当前状态／数据流及确认缺口：<区分代码事实、运行观察和未验证推断>
- 推荐实现：<最小可行方案及选择理由>
- 可逆的内部技术取舍：<仅记录影响实现的重要选择>
- 数据／存档／公开接口兼容性：<迁移、版本兼容和回退要求>
- 仍待用户决定的产品变化：<无；或引用第 6 节问题>

### 3.2 功能接口与归属

| 接口／事件／绑定键 | 提供方 → 使用方 | 参数／前置状态 | 返回／可观察变化／失败行为 | 读写性质与授权 | 实现或文档引用 |
|---|---|---|---|---|---|
| <接口> | | | | <查询 / 会修改状态> | |

查询接口与修改接口分开。领取奖励、写存档、改数据库等操作不得作为无副作用查询直接执行；缺少接口由 Builder 在确认范围内实现。

### 3.3 实现需要保留的可测试性

- 稳定的 UI／节点／事件标识：<标识及必要性>
- 页面状态、测试数据或存档的构造入口：<接口与限制>
- 随机种子／时钟控制：<需要的最小范围或不适用原因>
- 可观察输出：<供版本断言读取的字段、事件或日志>
- 测试入口在生产中的限制：<禁用、隔离或权限要求>
- 已有测试／Runner 的复用线索：<路径；尚未调查则注明>

这些是实现接口要求，不是本 Task 提前编写或运行完整测试套件的任务。

## 4. 功能隔离与并行协作

### 4.1 工作区身份（只读索引）

- Worktree 根目录：<从机器块读取>
- 分支：<从机器块读取>
- 基线提交：<从机器块读取真实完整 commit hash>
- 本 Task 文件：<路径>
- 本地 Git 操作授权：<未授权 / 已授权的提交、合并等操作与范围>

### 4.2 文件和功能归属

| 负责方 | 交付内容 | 允许写入的路径 | 禁止写入／交接边界 |
|---|---|---|---|
| PM | Task、决定与交接记录 | <文档路径> | <其他 Task 的记录> |
| Designer | 技术方案；交付阶段的 QA 计划 | <返回 PM 归档；工具写入另按授权> | 业务代码、可执行测试脚本、未经授权的状态修改 |
| Design-Art | 预览、概念；批准后的正式资产 | <本 Task 预览／概念目录、明确归属的正式资源路径> | Task、业务代码、最终场景绑定、其他工作区 |
| Code Builder | 代码、最终绑定；交付阶段的 QA 脚本 | <功能路径；Release 测试目录在版本计划中指定> | 未授权资源、其他工作区、擅自修改验收标准 |

同 Task 的并行写入路径不重叠；正式资产交接后，最终场景、导入元数据和资源绑定由 Code Builder 顺序完成。不允许同时操作共享 Git 索引。

### 4.3 依赖与共享触点

| 依赖 Task／模块 | 固定修订／提交 | 所需接口或产物 | 集成顺序／是否共同交付 | 协调者 |
|---|---|---|---|---|
| <无或具体依赖> | | | | |

| 共享文件／页面区域／主题 | 本 Task 的改动与占用范围 | 其他 Task／负责人 | 已固定的共同基准与协调方案 |
|---|---|---|---|
| <含锚点、层级、输入优先级或公共样式> | | | |

不能依赖其他活动工作区的未提交产物或漂移的“最新分支”。文件无冲突不等于页面不冲突；共享布局、主题和接口先协调，再并行生产。

### 4.4 运行数据隔离

| 对象 | 独立位置／命名空间 | 真正采用它的启动参数或适配入口 | 当前情况 |
|---|---|---|---|
| 存档／用户数据 | | | <未配置 / 已适配 / 不使用> |
| 缓存／临时文件／日志 | | | |
| 构建／导出产物 | | | |
| 数据库／账号／端口／外部服务 | | | |

仅创建目录不代表引擎已经使用该目录。未完成适配前可并行编辑，不能并行运行会写同一共享状态的实例；本文件不保存密钥或真实用户凭据。

## 5. 实机预览、资产概念与生产合同（有视觉改动时）

适用范围：<无视觉变化则写“不适用＋原因”；仅资产变化可只保留相关概念与生产合同>。

### 5.1 实机底图与编辑边界

- 底图 artifact_id／固定路径／实际 SHA-256：<引用真实存在的文件；摘要由工具取得>
- 来源：<实机截图 / 当前构建截图 / 录屏帧 / 用户明确批准的替代材料>
- 对应提交或构建、页面／游戏状态：<已知事实；未知项明确标出>
- 原始像素、视口、设备／安全区：<实际资料，不从评审拼图猜测>
- 已进行的裁剪、缩放或合成：<无或具体说明>
- 允许修改的区域与元素：<明确边界>
- 必须保留的区域与元素：<背景、Logo、镜头、文字、既有按钮等>
- 缺失材料／替代方案限制及批准依据：<无或引用第 6 节>
- 是否需要重新取图：<原因、最小范围及授权；优先复用可靠原图>

缺少可用底图先交 PM 索取或确认替代材料，不自动从空白重画。默认保留原图、局部制作、分层合成。

### 5.2 高精度预览交付

| artifact_id | 页面／状态与目标像素 | 引用底图 | 修改点与布局规则 | 固定文件／SHA-256 | 可编辑源／合成配方 |
|---|---|---|---|---|---|
| <PREVIEW-001> | | | <锚点、安全区、动态文字规则> | | |

主交付是可独立查看的原尺寸高精度画面；评审拼图仅为辅助。模拟尺寸标为“设计合成”，不能作为该设备实际适配通过的证据。未变化内容不重复生成。

### 5.3 资产概念与生产规格

| 概念 artifact_id | 对应资产／家族 | 需要确认的造型、配色、材质与状态 | 固定文件／SHA-256 | 尚未决定的内容 |
|---|---|---|---|---|
| <CONCEPT-001> | <ART-001> | | | <无或引用问题> |

每个新建／修改资产，或明确共用规格的资产家族，复制填写以下规格块：

#### <ART-001：资产名称／家族>

- 用途与生产方式：<复用现有 / 原生 UI / 修改资产 / 新建资产；说明运行用途>
- 关联概念与样式依据：<固定 artifact_id；复用或原生 UI 可写不需要概念及原因>
- 尺寸、宽高比与缩放规则：<明确数值／范围和单位>
- 格式、透明度与导出要求：<PNG / SVG / 序列帧等；是否 Alpha>
- 视角、构图、风格与必须保留项：<足以指导生产的要求>
- 状态与动画：<状态名称、帧数／时序；不需要则说明>
- 锚点、留白、切片／裁切边界：<所需规格或不适用>
- 文字、动态数值与本地化：<由控件渲染；烘焙文字必须有确认依据>
- 正式资源路径、源文件与导出方式：<项目资源目录；独立于整屏预览>
- 消费接口／绑定槽位、回退方案与集成者：<明确到 Code Builder 可接入>
- 来源／使用限制：<原创、已有资源或授权材料的可定位依据>
- 禁止项：<不允许的改动、风格或交付方式>

概念须足以确定生产方向，不能以未定占位图代替。正式资产在构建阶段独立生产，不默认从整屏预览裁图交付。

### 5.4 生成预算与交接

- 本次必需产物／主方向：<只列必要预览、概念与正式资产>
- 生成次数、额外变体、重试与并发上限：<有限预算及批准依据；未确定不得无限重试>
- 局部迭代与复用策略：<反馈改哪里；哪些已有产物不重新生成>
- 正式资产 Manifest：<生产后由工具生成的清单路径；无正式资产则不适用>

工具生成清单中的路径、摘要、格式和可识别尺寸；Art 提供用途、状态、锚点等语义，并声明删除／替换关系。清单不证明美术合格或绑定通过。

## 6. 澄清、批准与变更

### 6.1 未决问题与用户决定

| 问题／冲突 | 受影响范围与可选方案 | PM 向用户反馈的引用 | 用户决定与依据 | 对合同／已有产物的影响 |
|---|---|---|---|---|
| <无或具体问题> | | | <待决定不能写成默认方案> | |

任何真实业务歧义：子代理立即停止并反馈 PM，不直接询问用户、不自行派生代理；PM 暂停整个相关 Task，向用户反馈并等待选择。无依赖的其他 Task 可以继续。

### 6.2 当前批准依据（机器 approvals 的可读索引）

| 批准范围 | 对应 task_revision | 具体产物 artifact_id／固定 SHA-256 或合同修订 | 用户决定引用 | 当前结论 |
|---|---:|---|---|---|
| 页面／场景预览 | | | | <待确认 / 已批准 / 不需要及原因> |
| 必需资产概念 | | <逐项列明，不以一个“通过”代替不同概念> | | |
| 构建授权 | | <明确授权的工作范围与合同修订> | | <待确认 / 已授权> |

同一条明确用户回复可以覆盖多项批准；批准布局不自动批准全部资产或开工。实际批准只通过 `harness.py approve` 写入机器块，本表不单独授予权限。哈希只用于固定文件内容，不代替用户同意。

### 6.3 暂停与恢复（发生时填写）

- 暂停原因与原阶段：<引用问题，不再维护第二个主 Status>
- 受影响的在途 run_id／外部作业：<调度记录引用；没有工具时记录实际作业身份>
- 停止请求与实际停止情况：<已确认停止 / 仍在运行 / 无法确认；附依据>
- 已写入文件与晚到产物处理：<保留、撤销或隔离；不能自动当作当前交付>
- 用户决定与明确恢复授权：<来源及范围>
- 重新派发前处置：<旧写入者已停止或可靠隔离；新运行使用新 run_id>

撤销结果接收资格、停止进程、撤销已写文件是不同操作；没有实际证据不能声称均已完成。

## 7. 实现交付与 Release Handoff

由 PM 根据 Builder／Design-Art 返回的事实更新，不把未执行的 QA 写成通过。

### 7.1 已交付内容

- 实现摘要与对应需求／Intent ID：<完成了什么，不复制全部需求历史>
- 主要修改文件与功能入口：<路径／符号>
- 正式资源、源文件、Manifest：<固定引用；无则说明>
- 实际采用的预览／概念：<引用第 6 节已批准产物；存在偏离则明确列出>
- 场景导入、绑定与集成情况：<完成项和未完成项，不以“有文件”代替已接入>
- 真实实现提交与工作区情况：<完整提交／尚未授权提交；未提交修改如实记录>
- 依赖／共享修改交接：<固定产物、集成顺序和协调结论>
- 派发／接收记录：<工具记录引用或实际交接依据，不手工复制一套运行状态>

### 7.2 交付阶段 QA 所需的实现事实

| Intent ID | 实际接口／稳定标识／状态构造入口 | 可观察字段与相关路径 | 复用线索／限制／缺失项 |
|---|---|---|---|
| QI-001 | | | |

Builder 只补事实和实现限制，不据此改变验收含义。仅补路径无需重新设计全部 QA；预期、边界或覆盖含义变化交 PM 协调 Designer，涉及产品取舍由用户决定。

### 7.3 已知问题与未验证内容

| 问题／未验证项 | 已知证据或缺失证据 | 影响与归因 | 后续处理／是否阻断 |
|---|---|---|---|
| <具体内容> | | <本次引入 / 已有证据的历史问题 / 未确定> | |

默认不运行固定冒烟、完整回归、重复基线或多分辨率 QA。确有实现阻断、明确请求或必要风险诊断时，只记录实际进行的最小检查：

| 原因与授权 | 实际命令／范围 | 结果与证据 | 仍未覆盖的内容 |
|---|---|---|---|
| <未执行则写“不适用：交付阶段集中 QA”> | | | |

### 7.4 实现就绪交接

进入 `READY_FOR_RELEASE` 前，只确认合同与实现交接，不新增游戏测试门槛：

- [ ] 必需实现及绑定已交接，范围偏离已解决；未把未完成项标为完成。
- [ ] 预览、概念与构建授权覆盖实际采用的内容；没有未解决歧义或未经处置的旧写入者。
- [ ] QA Intent 有来源，实际入口、已知错误与未验证项已经披露。
- [ ] 实现快照、依赖、共享触点与必要资产信息明确，可供版本集成固定输入。

PM 交接依据／日期：<引用证据；Status 由工具更新机器块，第 0 节仅同步展示，不另存“已验证”结论>。

## 8. 版本 QA 关联（纳入版本后填写）

| 版本产物 | 责任归属 | 固定引用 |
|---|---|---|
| 纳入的 Task 修订与最终输入提交 | PM 在版本记录中固定 | <Release ID／记录路径> |
| QA_BACKLOG | 工具汇总本 Task 第 2 节，PM 核对来源 | <带修订和 Intent ID 的快照> |
| QA_PLAN | Designer：用例、断言、覆盖映射、接口／夹具需求 | <计划修订／固定引用> |
| 测试脚本与夹具实现 | Code Builder：依据计划编写、复用或适配 | <脚本／夹具及固定版本> |
| 执行结果与证据 | 普通 Runner；PM 总结，Reporter 仅按需辅助 | <候选版本、运行结果与报告引用> |

QA 计划不在本 Task 再复制一份。Builder 不擅自放宽断言、删减覆盖或修改标准；计划问题交 Designer，产品歧义经 PM 向用户确认。脚本编写不等于已获执行授权。

公共 setup 和已有测试可以复用，但被修改的运行状态须重置或隔离。零用例、遗漏必需项、跳过项或旧候选结果都不能冒充全部通过；版本结论以版本记录为准。

## 9. 合同修订说明（机器 history 的可读索引）

| task_revision | 日期 | 有效内容变化与原因 | 用户／PM 决定依据 | 受影响产物、批准和在途工作 |
|---:|---|---|---|---|
| 1 | <YYYY-MM-DD> | 初始草案 | | 无 |

需求范围、验收含义、接口合同、资产规格或写入边界改变时，通过 `task contract` 更新机器合同与 task_revision；本表只解释原因和影响，不手工推进修订。仅追加日志或非语义实现事实不制造新需求版本。

重要限制和决定必须进入对应合同区，不能只藏在随手备注、聊天历史或交接日志中。
