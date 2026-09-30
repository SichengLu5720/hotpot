# Task: 微信好友榜与开放数据域

Task ID: TASK-008  
Task Version: 1  
Status: Implementation Result Ready — Code; Art Paused  
Type: Platform  
Risk: High  
Build Mode: Code + Art  
Art Gate: Asset QA  
Experience Gate: Human Check  
Created By: PM Orchestrator  
Created At: 2026-09-22

---

# Workflow Control

Current Stage: HC-03-v1 Technical Candidate Ready；Visual Iteration Paused  
Last Accepted Checkpoint: HC-02-v1-Code Technical Plan  
Pending Human Check: HC-02-v1-Visual；HC-03-v1真实关系链/sharedCanvas结果等待AppID与真机；Affected Workstreams为正式皮肤、sharedCanvas视觉绑定和真实平台闭环  
Next Allowed Action: 保留已通过的正式开放数据域、协议、DPR/触摸/生命周期与导出校验；AppID就绪后执行真实关系链和sharedCanvas验证。  
Rollback Target: Draft（仅移除本 Draft 与索引项）  
Paused Workstreams: 开放数据域正式接入与验证等待HC-02；只读设计与预览继续  
Unaffected Workstreams: TASK-001、TASK-002、TASK-006 及其他独立 Task 继续

## Checkpoint Ledger

| Checkpoint | Stage | Task Version | Artifact / Evidence | Status | Human Decision | Rollback Target | Invalidates | Next Allowed Action |
|---|---|---:|---|---|---|---|---|---|
| HC-01-v1 | Requirement Freeze | 1 | 微信好友榜完整复述；用户“全部正确”并要求实施 | Accepted | 仅好友榜；累计每日首通降序；同分同名次；真实头像昵称；本人＋邀请空态；不做群榜/全服榜 | Draft | 旧未访谈状态 | 形成开放数据域协议、状态预览与HC-02方案 |
| HC-02-v1 | Visual / Plan Confirmation | 1 | `.harness/previews/TASK-008/r001/`加载/本人/空态/失败状态与合同；Feature Designer主域/开放域协议、成绩托管与QA Intent | Pending | 等待用户随合并HC-02确认；HTML静态检查通过但PM浏览器策略阻止本地渲染，未宣称视觉通过 | HC-01-v1 | 接受前无开放域实现可用 | Accepted后替换样例并实施真实好友榜 |
| HC-02-v1-Code | Technical Plan Confirmation | 1 | 开放数据域协议、成绩托管与QA Intent；用户“暂停美术迭代 先完成其他的” | Accepted | 技术分支继续，视觉候选不视为接受 | HC-01-v1 | HC-02-v1中技术分支Pending状态 | 实施正式开放域与导出校验 |
| HC-03-v1-Code | Technical Implementation Result | 1 | `.harness/qa/TASK-008/v1/qa-r001/verification.json`与统一回归 | Pending | C# 12例、JS 28例、79条真实序列化协议、SDK/WebGL编译通过；真实好友数据与sharedCanvas未验证 | HC-02-v1-Code | 相关协议、源码或测试范围变化时失效 | 等待AppID、真实关系链和真机；视觉保持暂停 |

## Agent Session Registry

| Role | Exact Agent Name | Dispatch Mode | Thread / Session ID | Status | Task Version | Last Input Checkpoint | Resume / Replacement Note |
|---|---|---|---|---|---:|---|---|
| Feature Design | `feature_designer` | Native | Not Started | Not Started | 1 | None | Draft 阶段禁止启动 |
| Visual & Presentation | `visual_design_agent` | Native | Not Started | Not Started | 1 | None | Draft 阶段禁止启动 |
| Code Build | `code_builder` | Native Custom Agent | `/root/code_builder_friend_open_data` | Completed — Technical Candidate | 1 | HC-02-v1-Code | 开放数据域、12/28用例、SDK/WebGL编译及统一回归完成；真机待AppID |

---

# Clarifications and Decisions

## Requirement Interview Summary

- User-confirmed problem: 当前好友榜仅为本地模拟，不能冒充微信好友数据。
- Confirmed boundary: 接入真实好友数据、真实空态、加载失败和隐私降级；禁止假头像和假排行。
- Dependencies: TASK-002 真机基线；TASK-006 用户档案和成绩语义。
- User confirmed complete understanding: Yes；启动/恢复/首通后刷新，失败可重试；主域不得读取或伪造好友数据，昵称由开放数据域系统字体绘制。

---

## HC-02 Implementation Contract

- 生产合同改为不返回好友行的`Open/Refresh/Close/UpdateViewport/PublishOwnScore`；旧`IFriendBoard.LoadAsync`仅保留编辑器模拟。主域不得接收好友列表。
- 主域/开放域消息使用`{version:1,type,viewEpoch,requestId,payload}`字符串协议；类型仅open/refresh/close/publishScore。拒绝坏JSON、未知版本与非法尺寸，异步结果必须匹配当前viewEpoch。
- 成绩key为`hotpot_first_wins_v1`，value仅含累计首通、更新时间、schema及不透明ownerMarker；不上传首通明细、奖励账本或昵称。发布时取已知远端与TASK-006本地累计最大值，失败不以0覆盖。
- 开放域自行读取、排序和绘制真实头像/昵称/成绩；同分`1,1,3`并保留返回顺序。本人marker不能唯一匹配时独立显示本人行，不猜测榜内身份。
- 正式开放域整体替换SDK随机成绩、群榜、示例邀请和好友日志；导出校验openDataContext、无随机写入、正式bundle可独立执行。
- QA覆盖协议畸形/迟到、长昵称、头像失败、只有本人、权限/网络失败、marker缺失/重复、DPR/安全区、sharedCanvas方向、关闭后停止刷新与成绩不回退。

<!-- FROZEN_START -->

# Frozen Requirement Candidate

## Goal and Player / User Outcome

玩家看到的好友榜只展示真实可用的微信关系链数据；没有权限、没有好友或加载失败时得到诚实且可理解的状态。

## Core Rules and Confirmed Decisions

- 使用微信开放数据域，不在主域伪造好友数据。
- 空态、未授权、无数据、加载中和失败必须可区分。
- 不使用假头像、假昵称或本地排行冒充微信服务。
- 累计每日首通次数降序；同分采用标准竞赛排名`1、1、3`，同分行保留开放数据域返回顺序，不新增隐藏胜负条件。

## Scope

- `IFriendBoard` 微信生产实现、开放数据域子工程和主域消息协议。
- 真实数据、空态、失败降级、缓存刷新与后台恢复。
- 已批准好友榜皮肤的真实数据绑定和真机可读性检查。

## Non-goals

- 全服榜、陌生人榜、社交聊天、假数据填充、助力/组队或正式发布。
- 改变 TASK-001 的成绩定义或每日挑战规则。

## Constraints and Interface Ownership

- 本 Task 拥有 `IFriendBoard` 微信适配、开放数据域工程和消息协议。
- TASK-006 提供用户档案/成绩语义；TASK-001 页面只消费稳定视图模型。
- 主域与开放数据域的资源和性能预算分别记录。

## Acceptance Criteria

| ID | Type | Criterion | Verification Owner | Required Evidence |
|---|---|---|---|---|
| AC-F-01 | Functional | 真实数据、空态、未授权和失败路径显示正确且可恢复 | Builder | 开放数据域用例与真机记录 |
| AC-T-01 | Technical | 主域/子域通信、刷新、缓存与生命周期无重复或越权数据 | Builder | 消息日志与故障注入 |
| AC-E-01 | Experiential | 好友榜信息清晰，不会把空态或本地数据误认为真实排行 | Human | 微信真机视觉检查 |

<!-- FROZEN_END -->
