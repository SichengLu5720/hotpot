# TASK-036：真实食材交换与三日锅底助力

- Status: Review
- Current Change: 将 TASK-030 食材交换与 TASK-031 锅底助力从本地/未部署状态接成真实微信账号、真实分享落地和真实云端事务；因当前工作区已有 `0.0.21` 开发版记录，本轮防回退顺延交付 `0.0.22`。

## Goal

部署并接通现有 `hotpotIngredientTrade`：食材一份换一份可由分享接收者真实接受或拒绝；锅底助力者开始时立即领取三种道具，72 小时内正式挑战胜利后才为发起者解锁三选一资格。

## Confirmed Behavior

- 食材交换使用专属分享链接；接收者无需首次通关即可进入确认页，但必须拥有需求食材的可交换副本。请求可拒绝、撤回，24 小时失效，成交原子且幂等。
- 锅底开始助力时以原子事务立即给助力者换单、清空暂存、打乱各 1 次，同时绑定为唯一助力者并开始连续 72 小时期限；发起者此时仍无资格。
- 只有该助力者在期限内完成一次正式挑战胜利，云端才给发起者永久三选一资格。失败、退出、热身和过期后胜利不生效。
- 到期通过云端读取/操作自动解绑；已发道具不回收。同一账号开始过后不能再次帮助同一发起者；每日最多开始帮助 3 人，北京时间 00:00 重置。
- 支持冷启动、热启动、后台恢复和幂等重试；普通分享、奖励分享、食材交换和锅底助力互相隔离。

## Must Preserve

- 当前工作区 TASK-032 至 TASK-035 及其他未提交修改；不得覆盖或回滚。
- 已确认的收藏、活动、侧栏、锅底视觉和玩法布局。
- 现有微信可信身份、运行配置、免费道具库存、旧档案兼容与失败关闭规则。

## Non-goals

- 不验证真实微信好友关系，不增加赠送、多份交换、公开市场或额外奖励。
- 不提审、不正式发布、不设置体验版。

## Work Packages

### WP-036-CLOUD

Work Package ID: WP-036-CLOUD  
Goal: 扩展现有云函数的真实食材交换与两阶段锅底助力权威事务。  
Spec References: `docs/SPEC.md` Rules and Data、Platform and Persistence。  
Must Preserve: 当前未提交 `trade.js`、`package.json` 与 TASK-033 道具改动；可信微信身份和现有账本。  
Allowed Write Paths: `cloudfunctions/hotpotIngredientTrade/**`；本任务专用 Node 测试。  
Forbidden / Shared Paths: Unity、SPEC、其他 Task；不部署。  
Depends On: None。  
Acceptance: 交换原子成交及全部失败边界；助力立即发三项、唯一绑定、72 小时、过期解绑、胜利完成、每日上限、幂等/并发/回滚覆盖。  
Integrator: `code_agent`。

### WP-036-CLIENT

Work Package ID: WP-036-CLIENT  
Goal: 接通真实食材分享落地/确认与锅底开始、等待、胜利回传及恢复流程。  
Spec References: `docs/SPEC.md` Core User Flow、UI and Visual Behavior、Platform and Persistence。  
Must Preserve: 当前 TASK-032 至 TASK-035 未提交代码；已确认界面，不做视觉重设计。  
Allowed Write Paths: 相关 Contracts、WeChat 平台服务、Bootstrap/Composition、`Runtime/Collection/CollectionStore.cs`、`BrothActivityStore.cs`、现有收藏/锅底表现状态、TASK-036 诊断。  
Forbidden / Shared Paths: 云函数、正式图片资产、SPEC、其他 Task；共享文件由本包单 Agent 串行修改。  
Depends On: WP-036-CLOUD 稳定动作与字段语义。  
Acceptance: 冷/热链接、无需首通落地、实际接受/拒绝/撤回、助力等待与倒计时、正式胜利一次回传、账号/环境/请求/世代过滤、重启恢复通过。  
Integrator: `code_agent`。

### WP-036-DELIVERY

Work Package ID: WP-036-DELIVERY  
Goal: 整合、最低验证、部署当前绑定云函数、推送匹配源码并上传微信开发版 `0.0.22`。  
Spec References: 当前 Task、SPEC Build and Delivery。  
Must Preserve: 当前绑定身份、正式运行配置、完整本地整包、GLX、2× 渲染倍率与所有有效工作区更新。  
Allowed Write Paths: 本 Task Result、`.harness/qa/TASK-036/` 证据。  
Forbidden / Shared Paths: 不提审、不发布、不设置体验版；不提交本地构建/日志/缓存。  
Depends On: WP-036-CLOUD、WP-036-CLIENT Completed。  
Acceptance: Node/C#/Unity 定向检查；云函数部署成功；最终包包含当前逻辑；Git 推送；官方 CLI 上传 `0.0.22` 成功。  
Integrator: PM。

## Result

- Requirement: Confirmed on 2026-09-26.
- WP-036-CLOUD: Completed. Four Node suites passed; the authoritative function was created in the bound development environment with Node.js 20.19, `index.main`, 15-second timeout and 256 MB memory. The collection now denies direct client read/write. Downloaded deployed source matched all 10 local source files by SHA-256.
- WP-036-CLIENT: Completed. TASK-036 social checks passed 18/18, existing TASK-031 platform checks passed 23/23, full Runtime + Contracts WeChat conditional compile completed with 0 errors, and Unity batch Boot/activity/helper/countdown smoke passed.
- WP-036-DELIVERY: Completed. Reproducible font subset was regenerated with fontTools 4.60.1 (533 glyphs); Unity WeChat export and final package verification passed, including current social IL2CPP markers, GLX and 2x render ratio. Source commit `d73c2a2` was pushed to `feat/auto-push-skill`.
- Official WeChat CLI uploaded development version `0.0.22`, description `真实食材交换与三日锅底助力`; total package 23,768,142 bytes, main package 2,442,491 bytes. Automatic preview also succeeded.
- Preview follow-up: the first live broth request exposed that the collection structure probe had incorrectly reported an absent collection as present. The actual `hotpot_ingredient_trade_v1` collection was created in the bound environment, direct client access was reset to deny/deny, a live transaction health probe passed, and the final cloud source was redeployed. Internal server failures now emit a sanitized diagnostic without account data; expected business rejections remain quiet.
- Not performed: two-account physical-device share/trade/challenge verification, review submission, release, or experience-version activation.
