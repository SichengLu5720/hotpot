# Task: 微信真机性能与兼容

Task ID: TASK-004  
Task Version: 1  
Status: Waiting HTTPS Deployment / Visual Candidate  
Type: Platform  
Risk: High  
Build Mode: Code + Art  
Art Gate: Human Art Approval  
Experience Gate: Human Check  
Created By: PM Orchestrator  
Created At: 2026-09-22

---

# Workflow Control

Current Stage: HC-02-v1 Plan Confirmation  
Last Accepted Checkpoint: HC-01-v1 Requirement Freeze  
Pending Human Check: HC-02-v1 — 真机矩阵、负载、指标与分阶段证据边界  
Next Allowed Action: TASK-001/002/006/007/008本地技术候选及TASK-002 v10远程资源实现已就绪；等待HTTPS部署、TASK-004 HC-02和视觉候选后执行DevTools与真机矩阵。  
Rollback Target: Draft（仅移除本 Draft 与索引项）  
Paused Workstreams: 真机执行与性能调优等待HTTPS部署、HC-02及恢复后的视觉候选；只读测试方案与本地技术证据已就绪  
Unaffected Workstreams: TASK-001、TASK-002、TASK-003 按各自恢复指针继续

## Checkpoint Ledger

| Checkpoint | Stage | Task Version | Artifact / Evidence | Status | Human Decision | Rollback Target | Invalidates | Next Allowed Action |
|---|---|---:|---|---|---|---|---|---|
| HC-01-v1 | Requirement Freeze | 1 | 微信分阶段验证、最拥挤盘面30 FPS与生命周期范围；用户“全部正确”并要求实施 | Accepted | 当前仅有AppID：先真机验证登录/分享/好友榜；广告与云端真实闭环等待ID | Draft | 旧未访谈状态 | 等待集成候选后执行验证 |
| HC-02-v1 | Plan Confirmation | 1 | 可用真机冷启动/10分钟整局/60秒最拥挤四锅三盘入场/20次榜单刷新/后台恢复；30 FPS与帧时间、内存、触摸、安全区证据合同 | Pending | 等待用户随合并HC-02确认 | HC-01-v1 | 接受前无设备执行授权 | Accepted后等待集成候选 |

## Agent Session Registry

| Role | Exact Agent Name | Dispatch Mode | Thread / Session ID | Status | Task Version | Last Input Checkpoint | Resume / Replacement Note |
|---|---|---|---|---|---:|---|---|
| Feature Design | `feature_designer` | Native | Not Started | Not Started | 1 | None | Draft 阶段禁止启动 |
| Visual & Presentation | `visual_design_agent` | Native | Not Started | Not Started | 1 | None | Draft 阶段禁止启动 |
| Code Build | `code_builder` | Native | Not Started | Not Started | 1 | None | Draft 阶段禁止启动 |

---

# Clarifications and Decisions

## Requirement Interview Summary

- User-confirmed problem: Unity/Windows 的性能代理不能替代微信真机结论。
- Confirmed boundary: 覆盖安卓、iOS、鸿蒙的帧率、内存、发热、触摸、安全区和后台恢复；不擅自削弱批准画面。
- Dependency: TASK-003 可运行候选包。
- User confirmed complete understanding: Yes；本轮以当前可用真机与开发者工具验证核心流程，最拥挤盘面稳定30 FPS为硬目标；未覆盖设备不得宣称通过。

---

## HC-02 Verification Contract

- 每台实际可用设备执行冷启动、完整10分钟对局、60秒最拥挤盘面＋四锅＋三盘连续入场/反馈、20次好友榜开关/刷新、后台30秒和2分钟恢复。
- 记录型号、系统、微信与基础库版本、帧时间P50/P95/P99、1秒窗口FPS、>50/100ms长帧、内存、GC、温升、触摸与最终错误日志；稳定30 FPS为硬目标，不能只报平均值。
- 人工检查胶囊安全区、DPR、sharedCanvas方向、头像昵称、滚动/点击映射、复活失败后的暂停与重试感受。
- DevTools与Windows只作为接口和负载代理，不替代真机。未覆盖的安卓/iOS/鸿蒙、真实广告或云环境分别标记Not Run/Not Configured，不得推断通过。

<!-- FROZEN_START -->

# Frozen Requirement Candidate

## Goal and Player / User Outcome

玩家在目标微信设备上能够稳定、准确、连续地完成完整对局，不因卡顿、触摸偏差、安全区遮挡或后台恢复错误破坏体验。

## Core Rules and Confirmed Decisions

- 最拥挤盘面、四锅开启和连续反馈时以稳定 30 FPS 为硬目标。
- 覆盖安全区、微信胶囊、DPR、触摸、暂停/恢复、内存警告与温升降频。
- 性能调优不能静默降低 TASK-001 已批准的视觉效果。

## Scope

- 安卓、iOS、鸿蒙的分档真机矩阵与持续性能采样。
- 帧时间、内存、GC、透明叠加、触摸精度、后台恢复和长时运行。
- 必要的对象复用、并发上限、缓存和平台质量配置。

## Non-goals

- 改变玩法、物理、难度、资产风格、广告、云服务或正式发布。
- 用平均 FPS 单一指标宣称体验通过。

## Constraints and Interface Ownership

- 本 Task 拥有设备质量档、性能采样与平台生命周期适配；不拥有玩法和美术方向。
- 可见效果参数由 Visual 串行调优并经人工比较；Code Builder 只提供稳定开关与测量接口。

## Acceptance Criteria

| ID | Type | Criterion | Verification Owner | Required Evidence |
|---|---|---|---|---|
| AC-F-01 | Functional | 点击、暂停、切后台、恢复、重试与结算在设备矩阵上行为一致 | Builder | 真机用例记录 |
| AC-T-01 | Technical | 复杂组合场景达到冻结后的帧时间、内存与稳定性预算 | Builder | 性能采样与最终日志 |
| AC-C-01 | Comparative | 调优前后玩法结果一致且无未经批准的画质下降 | Visual / Human | 同状态截图和对局比较 |
| AC-E-01 | Experiential | 操作响应、动画连续性和长局体感达到人工接受 | Human | 目标设备试玩 |

<!-- FROZEN_END -->
