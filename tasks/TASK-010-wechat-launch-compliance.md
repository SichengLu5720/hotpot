# Task: 微信上线合规与发布准备

Task ID: TASK-010  
Task Version: 1  
Status: Draft  
Type: Platform  
Risk: High  
Build Mode: Code Only  
Art Gate: Not Required  
Experience Gate: Human Check  
Created By: PM Orchestrator  
Created At: 2026-09-22

---

# Workflow Control

Current Stage: Draft / Backlog Only  
Last Accepted Checkpoint: None  
Pending Human Check: HC-01 Requirement Freeze  
Next Allowed Action: 必要的 TASK-003 至 TASK-009 完成后执行 Requirement Interview，并依据当时最新官方规则制定候选；此前不得派发 Agent 或实施  
Rollback Target: Draft（仅移除本 Draft 与索引项）  
Paused Workstreams: 全部合规配置、审核材料、候选构建与发布操作  
Unaffected Workstreams: TASK-001 至 TASK-009 按各自恢复指针继续

## Checkpoint Ledger

| Checkpoint | Stage | Task Version | Artifact / Evidence | Status | Human Decision | Rollback Target | Invalidates | Next Allowed Action |
|---|---|---:|---|---|---|---|---|---|
| HC-01 | Requirement Freeze | 1 | 本文件的 Draft 候选边界 | Pending | | Draft | None | 依赖满足后先完成需求访谈与最新规则调查 |

## Agent Session Registry

| Role | Exact Agent Name | Dispatch Mode | Thread / Session ID | Status | Task Version | Last Input Checkpoint | Resume / Replacement Note |
|---|---|---|---|---|---:|---|---|
| Feature Design | `feature_designer` | Native | Not Started | Not Started | 1 | None | Draft 阶段禁止启动 |
| Visual & Presentation | `visual_design_agent` | Native | Not Started | Not Required | 1 | None | 若审核素材需要视觉工作再按冻结方案调度 |
| Code Build | `code_builder` | Native | Not Started | Not Started | 1 | None | Draft 阶段禁止启动 |

---

# Clarifications and Decisions

## Requirement Interview Summary

- User-confirmed problem: 正式上线前需要独立处理隐私、域名、广告、数据、密钥、审核材料和回滚准备。
- Confirmed boundary: 本 Task 只形成发布候选，不授权审核提交、体验版激活或正式发布。
- Dependencies: 必要的 TASK-003 至 TASK-009 完成；正式集成与发布记录使用 `versions/<version>.md`。
- User confirmed complete understanding: No；目标地区、主体、类目、隐私文本和具体发布窗口仍待本 Task 访谈。

---

<!-- FROZEN_START -->

# Frozen Requirement Candidate

## Goal and Player / User Outcome

形成符合当时微信平台要求、配置可追溯、凭证不入库且可安全回退的发布候选，使后续人工发布决定有完整证据。

## Core Rules and Confirmed Decisions

- 依据执行时最新官方规则核验隐私、合法域名、广告、数据保留和审核材料。
- AppID、密钥、Token、证书和环境信息不得写入源码、Task、日志或截图。
- 本 Task 完成不等于已上传、已审核、已激活体验版或已发布。

## Scope

- 隐私最小化、合法域名、广告配置、数据保留、密钥隔离和环境分层。
- 审核素材、版本说明、回滚点、故障开关和发布前检查清单。
- 创建 Version 所需的集成输入与未验证边界。

## Non-goals

- 实际审核提交、体验版激活、正式发布、外部上传、Push、Merge、远程 Tag 或生产回滚。
- 在本 Task 中重新实现 TASK-003 至 TASK-009 的业务。

## Constraints and Interface Ownership

- 本 Task 拥有发布配置边界、合规证据和候选检查清单；各业务实现仍归其原 Task。
- 真正集成候选、发布决定和发布后观察写入新的 `versions/<version>.md`。
- 任何外部状态变更均需用户逐项明确授权。

## Acceptance Criteria

| ID | Type | Criterion | Verification Owner | Required Evidence |
|---|---|---|---|---|
| AC-F-01 | Functional | 发布候选可在开发/测试环境完成完整启动与核心流程，生产开关默认安全 | Builder | 候选构建与配置检查 |
| AC-T-01 | Technical | 隐私、域名、广告、数据和密钥配置符合执行时官方要求且无凭证入库 | Builder / PM | 最新规则引用、扫描和检查清单 |
| AC-T-02 | Technical | 回滚点、功能开关、版本身份和未验证边界完整可追溯 | PM | 候选证据与 Version 输入 |
| AC-E-01 | Experiential | 隐私提示、失败降级和平台提示不阻断或误导正常玩家 | Human | 候选体验检查 |

<!-- FROZEN_END -->

