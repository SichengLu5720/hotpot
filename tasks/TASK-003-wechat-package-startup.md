# Task: 微信包体与冷启动优化

Task ID: TASK-003  
Task Version: 1  
Status: Draft  
Type: Platform  
Risk: High  
Build Mode: Code + Art  
Art Gate: Asset QA  
Experience Gate: Human Check  
Created By: PM Orchestrator  
Created At: 2026-09-22

---

# Workflow Control

Current Stage: Draft / Backlog Only  
Last Accepted Checkpoint: None  
Pending Human Check: HC-01 Requirement Freeze  
Next Allowed Action: TASK-001 Verified 且 TASK-002 完整游戏导出成功后，执行独立 Requirement Interview；此前不得派发 Agent 或实施  
Rollback Target: Draft（仅移除本 Draft 与索引项，不需要项目回滚）  
Paused Workstreams: 全部设计、实现、构建与验证  
Unaffected Workstreams: TASK-001 与 TASK-002 按各自恢复指针继续

## Checkpoint Ledger

| Checkpoint | Stage | Task Version | Artifact / Evidence | Status | Human Decision | Rollback Target | Invalidates | Next Allowed Action |
|---|---|---:|---|---|---|---|---|---|
| HC-01 | Requirement Freeze | 1 | 本文件的 Draft 候选边界 | Pending | | Draft | None | 依赖满足后先完成需求访谈 |

## Agent Session Registry

| Role | Exact Agent Name | Dispatch Mode | Thread / Session ID | Status | Task Version | Last Input Checkpoint | Resume / Replacement Note |
|---|---|---|---|---|---:|---|---|
| Feature Design | `feature_designer` | Native | Not Started | Not Started | 1 | None | Draft 阶段禁止启动 |
| Visual & Presentation | `visual_design_agent` | Native | Not Started | Not Started | 1 | None | Draft 阶段禁止启动 |
| Code Build | `code_builder` | Native | Not Started | Not Started | 1 | None | Draft 阶段禁止启动 |

---

# Clarifications and Decisions

## Requirement Interview Summary

- User-confirmed problem: 完整 Unity 游戏存在首资源包、Wasm 与正式资源体积风险，需要从上传任务中独立治理。
- Confirmed boundary: 不删除、替换或降质 TASK-001 已批准美术；不以空壳或裁掉正式内容冒充优化完成。
- Dependencies: TASK-001 Verified；TASK-002 完整游戏导出成功并形成真实基线。
- User confirmed complete understanding: No；仅业务拆分获确认，具体预算、加载时序和设备指标仍待本 Task 访谈。

---

<!-- FROZEN_START -->

# Frozen Requirement Candidate

## Goal and Player / User Outcome

玩家从微信入口进入后，以可接受的等待时间看到完整启动画面并尽快进入可操作首局；后续资源按明确顺序加载且不缺失。

## Core Rules and Confirmed Decisions

- 治理首资源包、Wasm、分包、缓存预载、图集与压缩纹理。
- 以首局可操作时间、资源完整性和视觉无损为核心，而不是只追求包体数字。
- TASK-001 的玩法、坐标、正式资产和批准画面是只读输入。

## Scope

- 完整构建的体积归因、启动依赖图、分包与预载方案。
- 图集、纹理压缩及缓存策略的视觉对比与技术验证。
- 弱网、缓存命中/未命中、增量更新和加载失败降级。

## Non-goals

- 玩法、美术重设计、广告、云档案、好友榜、上传、审核或发布。
- 通过删除正式页面、字体或食材降低体积。

## Constraints and Interface Ownership

- 本 Task 拥有构建资源布局、分包清单、加载编排和缓存配置；不拥有玩法或表现设计。
- 任何纹理或图集参数由 Visual 审核后再落地，Code Builder 不得单独决定可见质量折损。
- 共享 Boot、Resources、导出配置只能在 TASK-001 Verified 后串行修改。

## Acceptance Criteria

| ID | Type | Criterion | Verification Owner | Required Evidence |
|---|---|---|---|---|
| AC-F-01 | Functional | 冷启动、缓存启动和弱网路径都能进入完整首局，延迟资源最终齐备 | Builder | 真机构建日志与流程记录 |
| AC-T-01 | Technical | 最终包符合当时平台限制，分包、Wasm、缓存和资源哈希可复现 | Builder | 包体报告、清单和哈希 |
| AC-C-01 | Comparative | 与 TASK-001 批准基线相比无缺图、错图或未经批准的可见降质 | Visual / Human | 同状态前后截图 |
| AC-E-01 | Experiential | 玩家不会因加载顺序或占位表现误以为游戏卡死 | Human | 真机体验检查 |

<!-- FROZEN_END -->

