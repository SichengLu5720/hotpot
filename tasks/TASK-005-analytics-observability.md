# Task: 运营数据与运行监控

Task ID: TASK-005  
Task Version: 1  
Status: Draft  
Type: Tooling  
Risk: Medium  
Build Mode: Code Only  
Art Gate: Not Required  
Experience Gate: None  
Created By: PM Orchestrator  
Created At: 2026-09-22

---

# Workflow Control

Current Stage: Draft / Backlog Only  
Last Accepted Checkpoint: None  
Pending Human Check: HC-01 Requirement Freeze  
Next Allowed Action: TASK-001 Verified 后执行 Requirement Interview；此前不得派发 Agent 或实施  
Rollback Target: Draft（仅移除本 Draft 与索引项）  
Paused Workstreams: 全部方案、实现与平台接入  
Unaffected Workstreams: 其他 Task 按各自恢复指针继续

## Checkpoint Ledger

| Checkpoint | Stage | Task Version | Artifact / Evidence | Status | Human Decision | Rollback Target | Invalidates | Next Allowed Action |
|---|---|---:|---|---|---|---|---|---|
| HC-01 | Requirement Freeze | 1 | 本文件的 Draft 候选边界 | Pending | | Draft | None | 依赖满足后先完成需求访谈 |

## Agent Session Registry

| Role | Exact Agent Name | Dispatch Mode | Thread / Session ID | Status | Task Version | Last Input Checkpoint | Resume / Replacement Note |
|---|---|---|---|---|---:|---|---|
| Feature Design | `feature_designer` | Native | Not Started | Not Started | 1 | None | Draft 阶段禁止启动 |
| Visual & Presentation | `visual_design_agent` | Native | Not Started | Not Required | 1 | None | 当前候选无视觉工作 |
| Code Build | `code_builder` | Native | Not Started | Not Started | 1 | None | Draft 阶段禁止启动 |

---

# Clarifications and Decisions

## Requirement Interview Summary

- User-confirmed problem: 缺少判断首局流失、道具、复活、失败和加载质量的统一证据。
- Confirmed boundary: 不采集非必要身份信息，不改变玩法，不让业务逻辑绑定具体分析供应商。
- Dependency: TASK-001 Verified；正式平台服务接入前完成更合适。
- User confirmed complete understanding: No；数据平台、留存周期、采样率和告警阈值仍待本 Task 访谈。

---

<!-- FROZEN_START -->

# Frozen Requirement Candidate

## Goal and Player / User Outcome

团队能够用最少且可解释的数据定位启动、首局、奖励、失败和性能问题，而玩家隐私与玩法结果不受影响。

## Core Rules and Confirmed Decisions

- 覆盖启动、首点、首锅、道具、复活、失败、胜利、重试和加载性能事件。
- 建立供应商无关的统一分析接口；发送失败不得阻断游戏。
- 默认不采集聊天、联系人、精确位置或其他非必要身份数据。

## Scope

- 事件字典、会话/对局关联、错误分级、性能事件和离线缓冲边界。
- 开发/测试/生产环境隔离与敏感字段检查。
- 基础漏斗、运行质量和异常诊断证据。

## Non-goals

- A/B 实验、广告归因、用户画像、营销自动化或玩法调整。
- 在没有隐私确认时接入第三方生产数据平台。

## Constraints and Interface Ownership

- 本 Task 拥有统一 Analytics/Diagnostics Sink、事件 schema 与发送策略。
- 业务模块只发稳定语义事件，不引用具体 SDK，也不因上报结果改变核心状态。

## Acceptance Criteria

| ID | Type | Criterion | Verification Owner | Required Evidence |
|---|---|---|---|---|
| AC-F-01 | Functional | 冻结事件在正确生命周期只产生一次，重复/旧会话不会污染结果 | Builder | 自动用例与事件快照 |
| AC-T-01 | Technical | 离线、超时、限流和平台失败不阻塞主线程或改变对局 | Builder | 故障注入与性能记录 |
| AC-T-02 | Technical | 事件字段满足最小化、环境隔离和敏感信息检查 | Builder / PM | schema 审计与配置检查 |

<!-- FROZEN_END -->

