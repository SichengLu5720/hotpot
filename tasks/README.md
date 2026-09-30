# 当前 Task 汇总

更新时间：2026-09-22

本文件只提供任务索引、依赖和当前状态；具体需求、决定、证据和回退点以各 Task 正文为准。新增的 TASK-003 至 TASK-010 仅为 Draft 业务队列，不代表需求已冻结或获准实施。

| Task | 当前版本 | 状态 | 当前边界 | 下一步 / 依赖 |
|---|---:|---|---|---|
| [TASK-001：火锅消消高精度每日挑战 Unity 可玩版](TASK-001-transparent-food-assets.md) | 7 | Awaiting Human Check | r010完整风格、页面、状态和动效候选已通过PM监修，HC-02-v7-r1待用户只判断风格 | 用户回复Accepted/接受、Needs Revision或Rejected；接受后进入正式资产与Unity实施 |
| [TASK-002：微信小游戏开发版导出与上传](TASK-002-wechat-development-upload.md) | 8 | Blocked | 完整 Unity 游戏的微信开发预览、导出和开发版上传 | 核对新目标小游戏“快适配”为已开通/使用中并等待权限实际生效；不扩充至生产服务或正式发布 |
| [TASK-003：微信包体与冷启动优化](TASK-003-wechat-package-startup.md) | 1 | Draft | 首资源包、Wasm、分包、缓存预载、图集和压缩纹理 | TASK-001 Verified 且 TASK-002 完整游戏导出成功后访谈并冻结需求 |
| [TASK-004：微信真机性能与兼容](TASK-004-wechat-device-performance.md) | 1 | Draft | 安卓、iOS、鸿蒙的性能、触摸、安全区和生命周期 | TASK-003 产生可运行候选包后访谈并冻结需求 |
| [TASK-005：运营数据与运行监控](TASK-005-analytics-observability.md) | 1 | Draft | 核心漏斗、加载性能、错误和运行质量事件 | TASK-001 Verified 后访谈；正式平台服务接入前实施更合适 |
| [TASK-006：云端档案、挑战日与共享额度](TASK-006-cloud-profile-quota.md) | 1 | Draft | 云档案、06:00 日界、5/15 分钟冷却、额度与迁移 | TASK-001 复活/奖励接口冻结后访谈并冻结需求 |
| [TASK-007：正式激励广告与奖励分享](TASK-007-reward-ad-share.md) | 1 | Draft | 微信激励广告和奖励分享生产适配 | 依赖 TASK-002 真机基线与 TASK-006 稳定额度接口 |
| [TASK-008：微信好友榜与开放数据域](TASK-008-friend-board-open-data.md) | 1 | Draft | 真实好友数据、空态、失败降级和开放数据域 | 依赖 TASK-002 真机基线与 TASK-006 用户档案语义 |
| [TASK-009：商业音频与授权替换](TASK-009-commercial-audio-licensing.md) | 1 | Draft | 替换不可商用音频并固化授权证据 | TASK-001 Verified 后访谈；完成前不得开启商业广告 |
| [TASK-010：微信上线合规与发布准备](TASK-010-wechat-launch-compliance.md) | 1 | Draft | 隐私、域名、广告配置、数据保留、审核材料与回滚准备 | 必要的 TASK-003 至 TASK-009 完成后访谈；发布另建 Version |

## 调度与隔离

- TASK-003 至 TASK-010 均为 `Version 1 / Draft / HC-01 Pending`；没有启动任何 Agent、预览、实现、构建或测试。
- TASK-001 完成前，新 Task 不得消费其未接受预览、接口或正式资产，也不得写入 TASK-001、其预览目录或 Unity 共享文件。
- 后续共享 Unity 文件必须串行集成；平台业务通过稳定适配层接入，不反向重写 TASK-001 玩法。
- TASK-002 保持完整开发预览与上传边界，不承载包体性能、生产广告、云档案、好友榜或发布业务。

## 当前交付边界

- 新手教学和练习模式只保留为未来机会，本轮不创建 Task。
- 真正的集成候选与发布使用 `versions/<version>.md`；不创建重复的发布 Task。
- 审核提交、体验版激活、正式发布、外部上传、Push、Merge 和远程 Tag 均需用户另行明确授权。
