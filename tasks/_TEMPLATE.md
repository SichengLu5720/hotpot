# Task: <任务名称>

Task ID: <TASK-001>  
Task Version: 1  
Status: Draft  
Type: Feature | Bug | Tuning | Tooling | Art | Platform  
Risk: Low | Medium | High  
Build Mode: Code Only | Art Only | Code + Art  
Art Gate: Not Required | Asset QA | Human Art Approval  
Experience Gate: None | Human Check  
Created By: PM Orchestrator  

`Art` 是兼容既有 Task 的模式名，表示由 `visual_design_agent` 承担的正式资产与表现层工作，不对应独立 Art Agent。
Created At: <YYYY-MM-DD>

---

# Workflow Control

Current Stage: Draft  
Last Accepted Checkpoint: None  
Pending Human Check: HC-01 Requirement Freeze  
Next Allowed Action: Present HC-01 to user and stop  
Rollback Target: Draft  
Paused Workstreams: None  
Unaffected Workstreams: Continue unless directly dependent on a paused output  

执行规则：`Next Allowed Action` 是恢复指针。四个里程碑检查点之一处于 Pending 时，只停止 `Paused Workstreams` 及其直接依赖；上一 Accepted 里程碑已授权且不依赖该修改的进程继续。里程碑内部的调查、修订、资产生产、集成和自动测试可以连续完成。指针缺失或过期时由 PM 根据 Ledger 修正，不额外请求人工解锁。

## Checkpoint Ledger

| Checkpoint | Stage | Task Version | Artifact / Evidence | Status | Human Decision | Rollback Target | Invalidates | Next Allowed Action |
|---|---|---:|---|---|---|---|---|---|
| HC-01 | Requirement Freeze | 1 | Frozen Requirement candidate | Pending | | Draft | None | Present requirement freeze and stop |

## Agent Session Registry

后台追踪用途；不是人工检查点，也不是日常推进门禁。

| Role | Exact Agent Name | Dispatch Mode | Thread / Session ID | Status | Task Version | Last Input Checkpoint | Resume / Replacement Note |
|---|---|---|---|---|---:|---|---|
| Feature Design | `feature_designer` | Native / Compatibility | Not Started | Not Started | 1 | None | |
| Visual & Presentation | `visual_design_agent` | Native / Compatibility | Not Started | Not Started | 1 | None | |
| Code Build | `code_builder` | Native / Compatibility | Not Started | Not Started | 1 | None | |

客户端未暴露原生角色参数时可登记为 `Compatibility`。Lovelace、Bernoulli 等临时昵称只作为显示信息。同一 Task、同一 Role 应优先恢复原线程；新建替代线程时尽力记录原因。Registry 缺失、Session ID 不可见、昵称变化或记录延迟均不得单独阻断工作；只有实际职责或权限错误并影响产出可信度时才修正调度。

---

# Clarifications and Decisions

## Requirement Interview Summary

- User-confirmed problem:
- Current logical position / entry state:
- Trigger:
- Expected logical position / resulting state:
- State transitions, exit, failure and retry:
- Scope and non-goals:
- Must remain unchanged:
- Acceptance intent:
- Remaining low-risk assumptions:
- User confirmed complete understanding: Yes | No

只有最后一项为 `Yes`，才允许形成 HC-01 Requirement Freeze 候选并进入后续流程。

| ID | Question / Unclear Item | Confirmed Decision | Confirmed By | Task Version Impact | Affected Work |
|---|---|---|---|---|---|
| CL-001 | | | User | None / v2 | |

未解决的阻断问题：

- None.

---

<!-- FROZEN_START -->

# Frozen Requirement

## Original Request

保留用户原话或忠实摘要。

## Problem

说明真正需要解决的问题。

## Goal and Player / User Outcome

完成后，玩家或开发者实际应该看到、感受到或能够完成什么：

- 
- 

## Core Rules and Confirmed Decisions

- 
- 

## Scope

本轮包含：

- 
- 

## Non-goals

本轮明确不处理：

- 
- 

## Constraints

必须遵守的产品、体验、平台、技术或项目约束：

- 
- 

## Accepted Assumptions

只记录 PM 已向用户明确展示、且低风险可逆的假设：

- None.

## Acceptance Criteria

| ID | Type | Criterion | Verification Owner | Required Evidence |
|---|---|---|---|---|
| AC-F-01 | Functional | | Builder | |
| AC-T-01 | Technical | | Builder | |
| AC-E-01 | Experiential | | Human | Comparative playtest / human judgement |

Type 只能使用：

- Functional
- Technical
- Experiential
- Comparative

## Test Proxies

技术代理指标只能辅助验收，不能独立替代 Experiential 或 Comparative 标准。

| Proxy ID | Proxy | Supports Criterion | Limitation |
|---|---|---|---|
| TP-001 | | AC-E-01 | Cannot independently pass the criterion |

<!-- FROZEN_END -->

---

# Design

## Current Implementation

由 Designer 调查真实项目：

- Entry point:
- Owning system:
- Related code:
- Related scenes / prefabs / nodes:
- Related art / resources:
- Current state or data flow:
- Confirmed current behavior:
- Confirmed gap or defect:

## Recommended Design

1. 
2. 
3. 

## QA Intent

- Protected player / user outcome:
- Main failure modes:
- Automatic-test boundaries:
- Required human checks:

## Main Change Areas

- 
- 

## Technical Options

只记录不改变玩家结果的可替换实现方案：

- None.

## Proposed Product Decisions

进入 `Ready to Build` 前必须为空。任何会改变玩法、操作、范围或体验的新增规则都应先回到 PM 澄清。

- None.

## Workstream Ownership

| Workstream | Required | Allowed Write Paths | Forbidden Paths / Shared Files |
|---|---:|---|---|
| Code Builder — Core / Interfaces / Technical QA | Yes / No | | Task, production art paths, visual composition and tuning |
| Visual & Presentation — Preview / Assets / Presentation / Visual QA | Yes / No | | Task, frozen gameplay, core state/data/platform logic |
| Shared Integration Sequence | Yes / No | Code then Visual then technical verification | Concurrent writes to shared files |

Code + Art 只有在核心代码与独立源资产的 Allowed Write Paths 不重叠时才并行。共享 Scene、Prefab、Node、UI、材质、动画、引擎导入元数据和绑定必须写明串行顺序：Code Builder 先提供核心与稳定表现接口，Visual Agent 再完成表现集成与最终视觉调优，Code Builder 最后只做不改变视觉决定的技术验证。

## Code–Art Interface

Build Mode 为 `Code Only` 时填写 `Not Required`。

| Asset ID | Purpose | Final Path | Runtime Role | Consuming Scene / System | Binding Slot / Key | Required Specs | Fallback | Integration Owner |
|---|---|---|---|---|---|---|---|---|
| ART-001 | | | | | | | | Visual & Presentation Agent |

## Asset Contract

Art Bible: `docs/ART_BIBLE.md`  
Approved Preview: <path or Not Required>  
Asset Manifest: Required | Not Required

| Asset ID | Type | Dimensions / Aspect | Format | Alpha / Background | View / Composition | Style / Must Preserve | States / Frames | Prohibited Elements |
|---|---|---|---|---|---|---|---|---|
| ART-001 | | | | | | | | |

---

# Visual Direction

Preview Status: Not Required | Draft | Pending Approval | Accepted | Needs Revision  
Latest Approved Preview: <path or Not Required>

## Accepted Visual Decisions

- 
- 

## Rejected Directions

- 
- 

## Open Visual Decisions

- None.

## Preview History

| Revision | Type | Artifact | Decision | Feedback / Changes |
|---:|---|---|---|---|
| r001 | Requirement / Implementation | | Pending | |

---

# Regression Plan

## Protected Behaviors

本次修改不应破坏：

- RB-01：
- RB-02：

## Impacted Systems

- 
- 

## Baseline Checks

修改前应记录：

- 
- 

## Targeted Regression Checks

修改后必须重测：

- 
- 

## Core Smoke Path

最小完整流程：

1. 
2. 
3. 

## Visual Regression Checks

无视觉变化时填写 `Not Required`。

- 
- 

## Known Pre-existing Issues

- None known.

---

# Build and Verification Results

> 本区域由 PM 根据 Subagent 返回结果更新。Builder 不直接修改 Task。

## Pre-delivery QA Scope

Status: Not Started | Pending Human Check | Accepted | Not Required

- Script Test Scope:
- Human Check Scope:
- Out of Scope:
- Automatic Run Evidence:

## Code Result

Status: Not Started

- Implementation Summary:
- Changed Areas:
- Baseline Result:
- Commands / Tests Run:
- Plan Deviations:
- Waiting for Art / Integration:
- Remaining Risks:

## Visual / Presentation Result

Status: Not Required | Not Started

- Assets / Presentation Produced:
- Asset Paths:
- Asset Manifest:
- Technical Asset Checks:
- Presentation Integration:
- Runtime Visual Tuning:
- Visual Deviations:
- Waiting for Core Interface / Technical Verification:
- Remaining Risks:

## Integration Result

Status: Not Started

- Integration Summary:
- Build / Runtime Result:
- Scene / Resource Binding Result:
- Core Smoke Result:
- Git Diff Review:
- Remaining Risks:

## Acceptance Results

| Criterion | Result | Evidence | Owner |
|---|---|---|---|
| AC-F-01 | Not Verified | | Builder |
| AC-T-01 | Not Verified | | Builder |
| AC-E-01 | Pending Human Check | | Human |

Result 只能使用：Pass / Fail / Not Verified / Not Applicable / Pending Human Check。

## Regression Result

Overall Result: Not Run

允许结论：

- No New Regression Found
- Regression Found and Fixed
- Regression Found - Verification Failed
- Baseline Inconclusive

| Finding | Classification | Evidence | Resolution |
|---|---|---|---|
| | Introduced / Pre-existing / Expected Change / Uncertain / No Difference | | |

## Human Art Approval

Status: Not Required | Pending | Accepted | Needs Revision

- Build / Asset Context:
- Feedback:

## Human Experience Check

Status: Not Required | Pending | Accepted | Needs Tuning | Needs Redesign

- Test Setup:
- Feedback:
- Accepted Values:

---

# Final Decision

Status: Draft

Verified only when all required gates pass.

## Task Version History

| Version | Date | Change | Reason | Invalidated Outputs |
|---:|---|---|---|---|
| 1 | | Initial draft | | None |
