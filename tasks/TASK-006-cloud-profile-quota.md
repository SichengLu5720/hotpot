# Task: 云端档案、挑战日与共享额度

Task ID: TASK-006  
Task Version: 1  
Status: Implementation Result Ready — Native Cloud Function / Client Bridge；Composition Decision Pending  
Type: Platform  
Risk: High  
Build Mode: Code Only  
Art Gate: Not Required  
Experience Gate: None  
Created By: PM Orchestrator  
Created At: 2026-09-22

---

# Workflow Control

Current Stage: HC-03-v1 Technical Candidate Ready；WeChat Environment Creation and Account Adoption Decision Pending  
Last Accepted Checkpoint: HC-02-v1 Technical Plan  
Pending Human Check: HC-03-v1完整云端结果等待账号分区采用时机确认、微信侧云环境创建、函数部署与跨设备验证  
Next Allowed Action: 用户确认登录晚于开局时的本地→微信账号切换时机；随后组合已完成的真实callFunction适配器，部署hotpotProfileSync并执行真实跨设备验证。  
Rollback Target: Draft（仅移除本 Draft 与索引项）  
Paused Workstreams: 生产组合只等待账号分区采用时机；外部部署等待微信侧云环境创建  
Unaffected Workstreams: TASK-001、TASK-002 及其他独立 Task 继续

## Checkpoint Ledger

| Checkpoint | Stage | Task Version | Artifact / Evidence | Status | Human Decision | Rollback Target | Invalidates | Next Allowed Action |
|---|---|---:|---|---|---|---|---|---|
| HC-01-v1 | Requirement Freeze | 1 | 云档案、离线与跨设备合并完整复述；用户“全部正确”并要求实施 | Accepted | 仅同步首通日期与奖励账本；只增不减；离线本地先判；不保存残局和设置 | Draft | 旧未访谈状态 | 形成HC-02方案与稳定接口 |
| HC-02-v1 | Plan Confirmation | 1 | Feature Designer只读Implementation Plan、schema、merge、时间、迁移与QA Intent | Pending | 等待用户随合并HC-02确认 | HC-01-v1 | 接受前无云实现可用 | Accepted后实施；缺云环境只做替身与错误态验证 |
| HC-02-v1-Code | Technical Plan Confirmation | 1 | 同上；用户“暂停美术迭代 先完成其他的” | Accepted | 明确授权非美术分支继续 | HC-01-v1 | HC-02-v1旧Pending技术状态 | 实施；真实云闭环等待环境ID |
| HC-03-v1-Code | Technical Implementation Result | 1 | `.harness/qa/TASK-006/v1/qa-r001/result.json`、`.harness/qa/integrated/v9-r002/aggregate-manifest.json`、`.harness/qa/TASK-006/v1/cloud-function-r001/final-result.json` | Pending | 本地事务、真实wx.cloud桥、可信身份握手、事务云函数和定向测试已完成；生产账号分区组合、部署和跨设备未验证 | HC-02-v1-Code | 对应源码、schema、云函数或账号采用策略变化时失效 | 确认账号采用时机后组合；创建微信侧环境并部署真实闭环 |

## Agent Session Registry

| Role | Exact Agent Name | Dispatch Mode | Thread / Session ID | Status | Task Version | Last Input Checkpoint | Resume / Replacement Note |
|---|---|---|---|---|---:|---|---|
| Feature Design | `feature_designer` | Native | Not Started | Not Started | 1 | None | Draft 阶段禁止启动 |
| Visual & Presentation | `visual_design_agent` | Native | Not Started | Not Required | 1 | None | 当前候选无视觉工作 |
| Code Build | `code_builder` | Native Custom Agent | `/root/code_builder_v7` | Completed — Technical Candidate | 1 | HC-02-v1-Code | 本地事务、outbox、登录/云边界及统一回归完成；真实服务端待外部环境 |

---

# Clarifications and Decisions

## Requirement Interview Summary

- User-confirmed problem: 正式档案、挑战日与分享额度不能继续只信任本机 PlayerPrefs。
- Confirmed boundary: 接管首通、每日记录、三次共享额度、5/15 分钟冷却、北京时间 06:00 日界和旧档迁移；保留离线降级。
- Dependency: TASK-001 复活和奖励接口冻结。
- User confirmed complete understanding: Yes；启动后静默登录、不阻塞游玩，失败排队同步；当前缺云环境ID，真实跨设备闭环分阶段验收。

---

## HC-02 Implementation Contract

- 保留同步`IProfileStore/IShareQuotaStore`为即时本地事务，外围新增异步`ReadSnapshot/SyncAsync/ProfileChanged/TimeSnapshot`；核心、回放和奖励效果不直接await网络。
- schema包含版本、环境/账号分区、首通日期集合、已生效奖励事件`requestId/quotaDay/rewardKind/route/effectiveUtc/timeSource`、旧档兼容基线、待同步操作和确认游标；设置与残局排除。
- 首通取并集；奖励事件按requestId并集去重且保留原时间。合并后事件数达到或超过3则当天封顶但不追回；未封顶时used=1按最后事件+5分钟、used=2按最后事件+15分钟。
- 实际效果生效后先在同一本地串行边界持久化成功记录与待同步upsert，再报告完成；预留不计次。重试复用ID，服务端确认后出队；账号、环境或会话变化使旧回调失效。
- 在线时间以服务端UTC加单调时钟锚定；离线明确记录设备时间降级。活跃对局与领取日不因迟到同步改桶。
- 缺云环境时只允许实现、迁移、属性/故障注入和NotConfigured验证，不宣称部署或跨设备通过。

## Native Cloud Function Implementation Addendum（2026-09-22）

- 项目自有WebGL桥已实现`wx.cloud.callFunction`请求关联、超时、取消、重复/过期回调隔离和脱敏错误；不修改vendored微信SDK。
- `cloudfunctions/hotpotProfileSync`已实现可信CloudBase上下文OPENID、事务读合并写、首通并集、奖励requestId去重、原始生效时间保留、schema/大小/枚举/UTC校验与旧dataVersion迁移。
- 服务端不存储设置、未完成对局或客户端outbox；客户端account字段不决定云端分区。定向C#、JS、Node测试及12程序集WebGL编译通过，证据见`.harness/qa/TASK-006/v1/cloud-function-r001/`。
- 生产Bootstrap组合尚未启用：需要先确认静默登录在对局开始后完成时，何时把`account="local"`档案合并并切换到可信微信账号分区；不得在未确认前自行重绑活跃ProfileStore。

<!-- FROZEN_START -->

# Frozen Requirement Candidate

## Goal and Player / User Outcome

玩家在重启、换设备、跨日和短时离线情况下获得一致的档案与奖励额度结果，不因重复回调或本机时间变化重复获奖或丢失进度。

## Core Rules and Confirmed Decisions

- 生产环境使用云端可信档案；客户端保留明确的离线读取与降级能力。
- 保持 TASK-001 的每日三次共享池、首次立即、随后 5/15 分钟以及北京时间 06:00 日界。
- 旧档迁移保留既有次数、首通和设置，不把缺失时间戳伪造成新成功时间。
- 多设备离线合并保留所有已经实际生效的独立奖励事实。若合并后当天记录达到或超过三次，不追回已发奖励，但当天立即封顶；请求ID继续全局去重。迟到事件保留原`effectiveUtc`，不在同步时重启5/15分钟冷却；当合并结果尚未封顶时，以合并后最后一次有效事件时间计算下一次开放。

## Scope

- 用户档案、挑战日、额度预留/提交/释放、幂等请求和版本迁移。
- 云端/本地冲突、断网、重试、跨日和设备时间异常。
- 开发、测试、生产环境隔离和最小可恢复备份。

## Non-goals

- 广告展示、分享 UI、好友榜、金币/体力、全服榜、付费或正式发布。
- 改变 TASK-001 已冻结的复活次数和冷却规则。

## Constraints and Interface Ownership

- 本 Task 拥有 `IProfileStore` 生产实现、共享额度服务、可信时间边界与迁移 schema。
- TASK-007 只消费稳定的额度结果，不自行保存次数或解释挑战日。
- 核心回放不得直接访问云、壁钟或平台 SDK。

## Acceptance Criteria

| ID | Type | Criterion | Verification Owner | Required Evidence |
|---|---|---|---|---|
| AC-F-01 | Functional | 首通、每日档案、额度、5/15 分钟冷却和 06:00 日界在重启/跨日后保持一致 | Builder | 可控时钟与存档用例 |
| AC-F-02 | Functional | 重复、迟到、取消和旧会话请求不会重复提交奖励或覆盖新档 | Builder | 并发与幂等用例 |
| AC-T-01 | Technical | 旧存档可迁移，云失败有明确降级且不会静默丢数据 | Builder | 迁移样本与故障注入 |

<!-- FROZEN_END -->
