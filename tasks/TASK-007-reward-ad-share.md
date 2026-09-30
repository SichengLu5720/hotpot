# Task: 正式激励广告与奖励分享

Task ID: TASK-007  
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
Pending Human Check: HC-02-v1-Visual；HC-03-v1真实平台结果等待AppID/广告位与真机；Affected Workstreams为正式状态皮肤、视觉绑定和真实平台闭环  
Next Allowed Action: 保留已通过的分享/广告/复活状态机与25例组合验证；AppID/广告位就绪后执行真实平台验证，不修改最终UI皮肤。  
Rollback Target: Draft（仅移除本 Draft 与索引项）  
Paused Workstreams: 平台接入、正式表现绑定和验证等待HC-02；只读设计与预览继续  
Unaffected Workstreams: TASK-001、TASK-002、TASK-006 及其他独立 Task 继续

## Checkpoint Ledger

| Checkpoint | Stage | Task Version | Artifact / Evidence | Status | Human Decision | Rollback Target | Invalidates | Next Allowed Action |
|---|---|---:|---|---|---|---|---|---|
| HC-01-v1 | Requirement Freeze | 1 | 分享、广告与奖励生命周期完整复述；用户“全部正确”并要求实施 | Accepted | 分享面板成功拉起且Hide→Show即弱校验成功；每天三次与5/15分钟不变；第四锅广告专属；失败不发奖可重试 | Draft | 旧未访谈状态 | 形成HC-02方案、状态预览与接口合同 |
| HC-02-v1 | Visual / Plan Confirmation | 1 | `.harness/previews/TASK-007/r001/`十二组状态与合同；Feature Designer平台状态机、取消、去重和QA Intent | Pending | 等待用户随合并HC-02确认；HTML静态检查通过但PM浏览器策略阻止本地渲染，未宣称视觉通过 | HC-01-v1 | 接受前无平台实现可用 | Accepted后实施真实适配与重试状态 |
| HC-02-v1-Code | Technical Plan Confirmation | 1 | 平台状态机与QA Intent；用户“暂停美术迭代 先完成其他的” | Accepted | 技术分支继续，视觉候选不视为接受 | HC-01-v1 | HC-02-v1中技术分支Pending状态 | 实施平台适配；缺ID走真实Unavailable |
| HC-03-v1-Code | Technical Implementation Result | 1 | `.harness/qa/TASK-007/v1/qa-r001/final-002/report.json`、`.harness/qa/TASK-001/v9/revival-qa-r001/run-002/flow.json`、统一回归 | Pending | 适配器、弱校验、广告关闭、取消/旧回调、同offer重试与复活搬盘协议已通过；真实平台未验证 | HC-02-v1-Code | 相关实现或测试范围变化时失效 | 等待外部配置与真机；视觉保持暂停 |

## Agent Session Registry

| Role | Exact Agent Name | Dispatch Mode | Thread / Session ID | Status | Task Version | Last Input Checkpoint | Resume / Replacement Note |
|---|---|---|---|---|---:|---|---|
| Feature Design | `feature_designer` | Native | Not Started | Not Started | 1 | None | Draft 阶段禁止启动 |
| Visual & Presentation | `visual_design_agent` | Native | Not Started | Not Started | 1 | None | Draft 阶段禁止启动 |
| Code Build | `code_builder` | Native Custom Agent | `/root/code_builder_reward_platform` | Completed — Technical Candidate | 1 | HC-02-v1-Code | 适配器、25例真实组合流程及统一回归完成；真实平台待外部配置 |

---

# Clarifications and Decisions

## Requirement Interview Summary

- User-confirmed problem: Unity 中的开发模拟需在未来替换为真实微信激励视频与奖励分享生命周期。
- Confirmed boundary: 复用 TASK-001 的单按钮、每局一次复活和失败回调规则；不新增广告位或普通结算分享。
- Dependencies: TASK-002 真机基线；TASK-006 稳定额度接口；商业启用还受 TASK-009 与 TASK-010 约束。
- User confirmed complete understanding: Yes；普通分享只保留微信菜单；普通与奖励分享共用静态主题图；当前缺广告位ID，真实广告闭环分阶段验收。

---

## HC-02 Implementation Contract

- `RewardRoute`保留模拟值并追加`WeChatShare/WeChatRewardedVideo`；RewardCoordinator仍唯一负责目标复验、效果、额度与暂停，平台适配器只返回结果。
- 奖励分享状态：订阅请求级生命周期→调用→10秒WaitingHide→WaitingShow→完成。只接受当前请求首次Hide→Show；普通菜单、调用前事件、重复事件和旧会话不参与。Show结果延迟到生命周期暂停更新后再交付。
- 广告缺ID/开关关闭即Unavailable；每次请求独立绑定Load/Show/Error/Close，只有`isEnded==true`成功。加载看门狗15秒；观看阶段不按时长推断。旧实例晚到回调只清理不发奖。
- 退出、重试、销毁或会话失效必须取消平台请求并完成等待Task，避免永久悬挂。失败解除Reward暂停但保留Revival暂停；同一offer回到同一路线单按钮，重试新建requestId，不自动切渠道。
- 微信菜单分享注册一次，只开启发送给朋友；菜单与奖励分享使用同一包内静态图，禁止朋友圈、结算分享与动态结果字段。
- QA覆盖10秒边界、先Show、重复/迟到回调、跨日、目标失效、广告true/false/null、无库存、失败、复活同offer重试和明确结束。

<!-- FROZEN_START -->

# Frozen Requirement Candidate

## Goal and Player / User Outcome

玩家在明确、可取消且不会重复扣取的流程中完成激励广告或奖励分享，并只在奖励真实满足条件后获得效果。

## Core Rules and Confirmed Decisions

- 仅接入 TASK-001 已存在的道具、提前解锁和每局一次复活奖励入口。
- 复活弹窗仍一次只显示一种奖励路线；分享可用时优先，否则广告。
- 取消、失败、无库存、旧会话和重复回调不得发奖或造成软锁。普通道具与第四锅返回原入口；复活保持暂停并回到同一个单按钮弹窗，可重试或主动结束本局，不因一次平台失败自动终局。
- 普通胜利、失败和其他结算不新增独立分享入口。
- 奖励分享采用明确标注的弱校验：调用未同步报错后，仅接受绑定到该请求的首次`Hide→Show`作为成功候选。调用后10秒内未观察到`Hide`则失败并返回原入口；一旦已`Hide`则等待正常`Show`，不按后台停留时长判失败。普通菜单分享不创建奖励请求。

## Scope

- 激励广告的加载、展示、关闭、完整观看、取消、失败和重试。
- 奖励分享的发起、返回、取消/失败和请求幂等。
- 后台、暂停、重试、退出与会话切换期间的生命周期安全。

## Non-goals

- Banner、插屏、金币、付费、额外广告位、分享裂变活动或正式发布。
- 自行定义额度、挑战日、档案存储或修改 TASK-001 玩法。

## Constraints and Interface Ownership

- 本 Task 拥有微信版 `IRewardService` 与 `IThemeShare` 适配器及生产开关。
- TASK-006 拥有额度和时间；TASK-001 拥有奖励效果与 UI 语义。
- TASK-009 完成前商业广告开关必须保持关闭。

## Acceptance Criteria

| ID | Type | Criterion | Verification Owner | Required Evidence |
|---|---|---|---|---|
| AC-F-01 | Functional | 完整观看/有效分享只发奖一次，取消、失败、无库存和旧回调不发奖 | Builder | 平台回调矩阵与自动用例 |
| AC-T-01 | Technical | 暂停原因、请求锁、会话代次和额度提交在后台/重试中保持幂等 | Builder | 故障注入与状态日志 |
| AC-E-01 | Experiential | 单按钮、等待、取消和失败状态清楚且不会让玩家误以为已获奖 | Human | 微信真机流程检查 |

<!-- FROZEN_END -->
