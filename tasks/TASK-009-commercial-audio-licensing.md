# Task: 商业音频与授权替换

Task ID: TASK-009  
Task Version: 1  
Status: Draft  
Type: Art  
Risk: High  
Build Mode: Code + Art  
Art Gate: Human Art Approval  
Experience Gate: Human Check  
Created By: PM Orchestrator  
Created At: 2026-09-22

---

# Workflow Control

Current Stage: Draft / Backlog Only  
Last Accepted Checkpoint: None  
Pending Human Check: HC-01 Requirement Freeze  
Next Allowed Action: TASK-001 Verified 后执行独立音频 Requirement Interview；此前不得派发 Agent、生产或绑定音频  
Rollback Target: Draft（仅移除本 Draft 与索引项）  
Paused Workstreams: 全部音频选型、生产、采购、绑定与验证  
Unaffected Workstreams: TASK-001 v7 无音频工作流及其他 Task 继续

## Checkpoint Ledger

| Checkpoint | Stage | Task Version | Artifact / Evidence | Status | Human Decision | Rollback Target | Invalidates | Next Allowed Action |
|---|---|---:|---|---|---|---|---|---|
| HC-01 | Requirement Freeze | 1 | 本文件的 Draft 候选边界 | Pending | | Draft | None | TASK-001 Verified 后先完成需求访谈 |

## Agent Session Registry

| Role | Exact Agent Name | Dispatch Mode | Thread / Session ID | Status | Task Version | Last Input Checkpoint | Resume / Replacement Note |
|---|---|---|---|---|---:|---|---|
| Feature Design | `feature_designer` | Native | Not Started | Not Started | 1 | None | Draft 阶段禁止启动 |
| Visual & Presentation | `visual_design_agent` | Native | Not Started | Not Started | 1 | None | 仅在音频职责明确后调度 |
| Code Build | `code_builder` | Native | Not Started | Not Started | 1 | None | Draft 阶段禁止启动 |

---

# Clarifications and Decisions

## Requirement Interview Summary

- User-confirmed problem: TASK-001 历史 Suno 免费版方向不允许商业广告或商业发行，需要独立替换。
- Confirmed boundary: 本 Task 不影响当前 v7 无音频范围；保存来源、授权、提示词、版本、导出和许可证据。
- Dependencies: TASK-001 Verified；TASK-007 的商业广告启用受本 Task 完成约束。
- User confirmed complete understanding: No；音乐数量、风格、采购/生成渠道和预算仍待本 Task 访谈。

---

<!-- FROZEN_START -->

# Frozen Requirement Candidate

## Goal and Player / User Outcome

游戏拥有与最终视觉一致、可长期商业使用且授权证据完整的音乐、环境和交互音效，在微信生命周期中稳定播放。

## Core Rules and Confirmed Decisions

- 全部不可商用或授权不足的音频必须替换，不得静默沿用。
- 每个最终文件保存来源、授权主体、生成/采购日期、版本、提示词或订单、导出文件和条款证据。
- 未完成授权验收前不得开启商业广告或对外宣称可商业发行。

## Scope

- BGM、环境、操作、入锅、端锅、解锁、胜利和失败音频的商业授权替换。
- Unity/微信音量、静音、后台中断、恢复、循环和并发验证。
- 许可台账与资产哈希。

## Non-goals

- 修改 v7 视觉、玩法、广告逻辑、配音、外部发布或延续不可商用音频。
- 在本 Draft 阶段生成、采购或导入任何音频。

## Constraints and Interface Ownership

- 本 Task 拥有商业音频资产、许可证据和音频表现绑定；不拥有玩法或广告状态。
- 正式文件写入独立版本资源根，不覆盖历史资产；运行接口保持稳定。

## Acceptance Criteria

| ID | Type | Criterion | Verification Owner | Required Evidence |
|---|---|---|---|---|
| AC-F-01 | Functional | 所有冻结场景使用正确音频，静音、后台、恢复和并发行为正确 | Builder | Unity/微信运行记录 |
| AC-T-01 | Technical | 每个交付文件可追溯且许可覆盖预期商业用途 | PM / Human | 授权台账、条款快照和哈希 |
| AC-E-01 | Experiential | 音色、节奏、响度和重复疲劳达到人工接受 | Human | 完整试玩与对比试听 |

<!-- FROZEN_END -->

