# Version: <v0.1.0>

Status: Planning  
Release Channel: Local | Internal | Test | Production  
Target Platform: Not Selected  
Created At:  
Base Branch:  
Base Commit:  
Previous Stable Version / Commit:

---

## Version Workflow Control

Current Stage: Planning  
Last Accepted Checkpoint: None  
Pending Human Check: VH-001 Version Scope  
Next Allowed Action: Present version scope to user and stop  
Rollback Target: Planning  

只允许执行 `Next Allowed Action`。只要 `Pending Human Check` 不是 `None`，不得创建下游构建、RC、发布决定或执行外部发布动作。

## Version Checkpoint Ledger

| Checkpoint | Stage | Artifact / Evidence | Status | Human Decision | Rollback Target | Invalidates | Next Allowed Action |
|---|---|---|---|---|---|---|---|
| VH-001 | Version Scope | This Version | Pending | | Planning | None | Present to user and stop |

Status 只能使用：`Pending`、`Accepted`、`Needs Revision`、`Rejected`、`Invalidated`。

Version 集成基线、集成结果、Release Candidate、Human Release Check 和发布计划必须分别建立检查点。拒绝任一检查点时，保留原记录，回到最近的 Accepted 检查点，并将其后的 RC、测试结果和发布决定标记为 `Invalidated`。

---

<!-- VERSION_SCOPE_START -->

## Version Goal

说明这个版本作为完整构建要达到什么结果，而不是只罗列功能。

## Included Tasks

| Task | Task Version | Status | Change Boundary | Integration Notes |
|---|---:|---|---|---|
| `tasks/TASK-001-example.md` | 1 | Verified | commit / range / working tree | |

Release Candidate 创建前，所有 Included Tasks 必须为 `Verified`。

## Excluded Scope

- 
- 

## Version Acceptance Criteria

- VA-01：所有 Included Tasks 已 Verified。
- VA-02：核心流程能够从启动运行到成功或失败。
- VA-03：Task 组合后没有阻断性冲突。
- VA-04：目标配置能够成功构建。
- VA-05：正式构建不含非预期调试内容或测试凭证。
- VA-06：回滚点和已知问题已记录。

<!-- VERSION_SCOPE_END -->

---

## Scope Change Log

版本进入 Integration 后，范围变化必须记录并重跑受影响检查。

| Date | Change | Reason | Impacted Checks | Approved By |
|---|---|---|---|---|
| | | | | |

## Integration Baseline

- Branch:
- Base Commit:
- Git Status:
- Existing Uncommitted Changes:
- Previous Build / Test Result:
- Previous Stable Point:

## Integration Risks

- 
- 

## Version Regression Checklist

### Pre-delivery QA Gate

- [ ] 每个 Included Task 的 Implementation Plan 与 QA Intent 已总结
- [ ] Designer 已给出 Script Test Scope 和 Human Check Scope
- [ ] Code Builder 已按范围更新测试脚本
- [ ] 自动测试已无监控运行完成，记录退出码、报告和最终日志
- [ ] 运行中未使用轮询、日志尾随或额外监控 Agent
- [ ] 自动测试不能覆盖的人工 Gate 仍保留

### Startup

- [ ] 应用能够启动
- [ ] 正确初始场景加载
- [ ] 无阻断运行时错误
- [ ] 正确配置被加载

### Core Flow

- [ ] 能开始一局或进入主要功能
- [ ] 核心输入正常
- [ ] 主要系统共同运行
- [ ] 成功路径可达到
- [ ] 失败路径可达到
- [ ] 重开或返回流程正常

### Integration

- [ ] Included Tasks 之间无阻断冲突
- [ ] 共享状态和生命周期正常
- [ ] 场景和资源引用完整
- [ ] 配置没有互相覆盖
- [ ] 存档 / 数据兼容符合预期
- [ ] 未引入未解决的 Introduced Regression

### Release Configuration

- [ ] 版本号与构建配置正确
- [ ] 不含非预期 Debug UI / 测试账号 / 开发地址
- [ ] 正式依赖与平台配置正确

## Integration Result

Status: Not Started

| Area | Result | Evidence |
|---|---|---|
| Startup | Not Run | |
| Core Flow | Not Run | |
| Integration | Not Run | |
| Release Configuration | Not Run | |

## Release Candidate

Status: Not Created

- RC Identifier:
- Version Number:
- Branch / Commit / Tag:
- Build Configuration:
- Build Command:
- Engine / Toolchain Version:
- Artifact Path:
- Checksum, when practical:
- Created At:
- Build Result:

任何代码、资产、配置、依赖或构建设置变化都会使当前 RC 失效，必须重新集成和构建。

## Human Release Check

Status: Pending | Accepted | Rejected | Not Required

- [ ] 版本内容与 Version Goal 一致
- [ ] 核心流程完整可用
- [ ] 主要操作、视觉和声音无阻断问题
- [ ] 已知问题处于可接受范围
- [ ] RC 可进入目标渠道

Feedback:

## Platform Release Procedure

Status: Not Required | Not Integrated | Draft | Verified  
Target Platform:  
Official Documentation Verified At:  
SDK / Toolchain Version:

首次接入平台时，由 Agent 查询当时最新官方文档，再填写以下内容：

- Account and access prerequisites
- Project integration requirements
- Build and packaging
- Signing and credential handling
- Sandbox / test distribution
- Submission and review
- Rollout and publication
- Platform rollback or hotfix

平台 SDK、代码和构建适配应作为普通 Task 开发。凭证、私钥和 Token 不得写入本文件或 Git。

外部上传、提交审核、正式发布和生产回滚必须获得用户明确授权。

## Known Release Issues

| Issue | Severity | User Impact | Accepted | Mitigation |
|---|---|---|---|---|
| | | | | |

## Rollback Plan

- Previous Stable Version:
- Previous Stable Commit / Tag:
- Rollback Artifact:
- Save / Data Compatibility:
- Irreversible Migration: No | Yes | Unknown
- Procedure:
- Limitations:

## Release Decision

Status: Pending | Approved | Rejected

- Approved RC:
- Approved Commit / Artifact:
- Approved Target:
- Approved By / At:

## Release Result

Status: Not Released | Released | Failed | Rolled Back

- Actual Version:
- Channel / Platform:
- Released RC / Commit / Tag:
- Released Artifact:
- Released At:
- Result / Evidence:

## Post-Release Observation

Status: Not Started | Observing | Stable | Issue Detected | Closed

- Observation Window:
- Startup / loading:
- Core flow:
- Platform interfaces:
- Crash / error findings:
- Analytics findings, if applicable:
- Follow-up Tasks:
- Decision: Continue | Hotfix Required | Rollback Required | Complete
