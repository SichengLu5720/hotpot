# 微信全流程候选与 Lite Harness 迁移

Task ID: TASK-011  
Status: Working  
Updated At: 2026-09-23

## Requirement Delta

- Goal: 形成可直接在微信开发者工具完成全流程测试、并可上传后台供用户设置体验版的正式候选。
- Current Behavior: 授权、正式资源、登录云同步、奖励账本、暖缓存及 UI 极短点击已实测；道具/飞行动效完成 Unity 定向检查，正在重建正式微信候选并验证完整对局。好友榜后台声明已填写，重编译后出现微信用户隐私授权弹窗，需用户本人作出选择后继续真实数据验证。
- Target Behavior: 完成冷/暖资源、登录同步、完整一局、奖励、好友榜、复活与重试；修复明显错误后上传。
- Entry / Trigger: 微信开发者工具打开最终 minigame 包。
- State Change: 技术候选 → DevTools 全流程候选 → 已上传后台。
- Boundary / Failure / Cancel: 不提审、不发布；平台失败不得伪装成功。

## Spec References

- docs/SPEC.md#product-goal
- docs/SPEC.md#core-user-flow
- docs/SPEC.md#platform-and-persistence
- docs/SPEC.md#ui-and-visual-behavior

## Must Preserve

- 当前玩法规则、点击/视觉一致性、r017 视觉方向、真实平台数据与奖励安全边界。

## Non-goals

- 微信审核提交、正式发布、Banner/插屏、全服榜、群榜。

## Current Change

- 当前定向修复：每局倒计时等待玩家第一次成功把可操作食材移入订单锅或暂存碟后再开始；等待期间盘子继续按现有0.30秒节拍生成、下落和碰撞。空白、遮挡、不可操作食材、暂停、设置、道具与奖励入口不启动倒计时；重试和新局重置等待状态。此前顶部计时器、原生短点击、资源、分享和身份修复均保留。

## Work Packages

| ID | Agent | Goal | Allowed Write Paths | Forbidden / Shared Paths | Depends On | Status | Integrator |
|---|---|---|---|---|---|---|---|
| CODE-CLOUD | code_agent | 远程资源与云档案 | Platform/Profile, RemoteAssets, cloudfunctions | Presentation | None | Completed | CODE-INTEGRATE |
| CODE-REWARD | code_agent | 分享与激励视频 | Platform/WeChat reward modules | Bootstrap, Presentation | None | Completed | CODE-INTEGRATE |
| CODE-BOARD | code_agent | 开放数据域好友榜 | WeChatOpenData, board tests | Bootstrap, Presentation | None | Completed | CODE-INTEGRATE |
| VIS-R017 | visual_agent | 现代轻量视觉与好友榜容器 | Presentation, preview r017 | Core, Platform | None | Completed | CODE-INTEGRATE |
| CODE-INTEGRATE | code_agent | 共享接线、正式构建与真实包 | Bootstrap, WeChatBuild, build output | Visual decisions | 上述四包 | Completed | Same Agent |
| ACCOUNT-CAPABILITY | User / WeChat backend | 添加好友榜 Layout 插件 | 微信公众平台账号能力 | 仓库、提审、发布 | CODE-INTEGRATE | Completed | N/A |
| PACKAGE-SIZE | code_agent | 主包降至 4096 KB 以下并保留静态分享图 | WeChatBuild、隔离导出目录 | 正式原图、玩法、视觉决定 | CODE-INTEGRATE | Completed | CODE-INTEGRATE |
| DEVTOOLS | code_agent | 开发者工具全流程与修复 | 定向代码、隔离构建、证据 | 发布 | PACKAGE-SIZE | Working | Same Agent |
| CODE-HUD-SAFE | code_agent | 删除进度条并实现顶部居中倒计时与动态胶囊保护 | Contracts/SessionContracts.cs、Platform/WeChat/WeChatPlatform.cs、Bootstrap/DailyProductionComposition.cs、Presentation、v7进度条专用资源 | Core玩法、Scene、其他视觉资产、旧构建与证据 | DEVTOOLS | Completed | Same Agent |
| CODE-FIRST-ACTION-TIMER | code_agent | 首次成功食材路由后启动倒计时，未启动时供给继续 | Session/SessionController.cs、Bootstrap/DailyProductionComposition.cs、直接相关诊断 | Core订单/库存/随机、供给节拍、视觉 | DEVTOOLS | Completed | Same Agent |
| UPLOAD | code_agent | 上传验证版本 | 微信后台版本 | 提审、发布 | DEVTOOLS | Draft | Same Agent |

## Acceptance

- User-visible result: 微信开发者工具可走通目标全流程，r017 画面可供用户体验。
- Minimum runtime check: 编译、启动、当前核心流程、明显错误、Diff 范围。
- User review method: 用户设置体验版并判断玩法与风格；接受后保存 Baseline。

## Result

- Status: Working
- Changed Files / Areas: 见各工作包证据。
- Build / Launch: native-tap-r006 Prepare/Split/Export 及官方 preview 已成功（主包 3248906 字节），导出句柄 39082 已结束。r005 首次开始后正式远程食材盘面可操作。
- Current Change Check: r005 系统菜单与奖励分享均显示正确火锅图，未发送消息；奖励分享取消返回后按既有弱校验显示提示效果并进入冷却。第四锅及分享冷却期间均明确提示广告不可用、不发奖不扣分享次数。奖励检查 75 项、SDK 编译通过，新字体 364 字通过构建。r003 自然时间到仅显示失败/重试（不复活），重试回到 10:00；此前暂存满分享复活为单只大盘、暂存清空并继续同局。完整胜利局尚未验证。
- HUD Safe Check: 已删除进度条和专用 `progress_fill` 位图/Meta；倒计时固定顶部水平居中，并按真实微信胶囊矩形加12逻辑像素保护，仅在相交时向下避让。修复高清字体 RectTransform 被重置后产生的竖排换行，强制保持4倍栅格、0.25缩放和单行 Overflow。Unity 编译通过，35项实际界面检查通过，依赖守卫 PASS（89 texture / 16 alpha / 91 must-keep）。新候选 `capsule-hud-r009` 已完成 SDK 导出及官方 CLI preview（主包 3249309 字节），并已在微信开发者工具打开等待用户目视确认。
- First Action Timer Check: 运行时间与挑战倒计时已拆分；等待首次有效操作时供给仍按0.30秒节拍运行，核心逻辑边界与10分钟超时保持0。只有 `ItemRoutedToOrder` 或 `ItemRoutedToBuffer` 成功事件启动挑战时钟；暂停冻结，重试/新局重置。定向真实 Composition 用例 PASS，WebGL 12个程序集编译 PASS，依赖守卫 PASS。微信候选 `first-action-timer-r010` 已完成 SDK 导出、官方 CLI preview（主包3249309字节）并在开发者工具打开；尚未上传后台。
- Screenshot / Artifact: .harness/previews/TASK-001/r017/ 与正式 minigame 包。
- r006 Runtime Check: 原生分享面板取消返回后按既有弱校验获得提示，真实账号奖励账本增加至 1；对局中 pending=1，回到首页后 pending=0、confirmed=true。保留缓存重新编译启动后奖励仍为 1、pending=0；进入正式食材盘面期间临时计数确认 wx.cloud.downloadFile/wx.downloadFile 调用合计 0，随后还原诊断包装。好友榜重新打开仍显示开发者未完成微信隐私声明。
- Next Runtime Focus: r007 Prepare/Split/Export 已完成（句柄 27995 退出 0），导出目录 staging/build/wechat/TASK-002-v9-export-20260923T061106-c68ed725d85f4a1fbc2726fa69ebdedd/minigame；下一步 official preview/open。实际 UGUI 检查 7 项通过（短点击、去重、取消、移出、禁用、恢复、禁用重复鼠标源），正式包按钮仍待验证。用户新截图再次确认好友榜隐私声明阻挡，并显示重复 read only canvas 警告；后者独立排查，不归咎 Layout 权限。完整胜利/首通云写入仍待验证。
- Known Issues: 已实测云下载成功，但 DevTools 2.02.2608070 / 基础库 3.16.3 整包 readFileSync 报 InvalidCharacterError。分段读取全部 14718885 字节、57 段成功。已实现云下载、HTTPS 下载和缓存统一 256KB 分段读取，保留原 SHA/CRC 校验；8 组托管测试和真实 SDK 编译通过。缺少真实广告位 ID 时保持不可用。尚未上传后台版本。
- Resume Pointer: 当前 r006 开发者工具窗口 57740836（须重新观察）。云函数 V2 的 tcbContext 信封修复已部署；13 组服务端检查及下载代码 SHA 比对通过。r005 实测 identity 错带 document 对象导致 InvalidPayload，独立 IdentityRequest DTO 修复及客户端 6 组检查通过；r006 冷启动后只读档案摘要已见 signedIn=true、confirmed=true、pending=0（原 local 分区保留），证明可信身份采用及初次云同步完成。下一步验证奖励账本云端确认、暖缓存、完整胜利局与首通同步。好友榜此前实际返回 `getFriendCloudStorage:fail please go to mp to announce your privacy usage`，需用户完成后台隐私声明，不是 Layout 权限问题。尚未上传。
- Baseline: Not Saved
- Latest Runtime Check: r007 新窗口 42405930 已启动；实际单次点击完成设置打开/关闭、开始游戏、暂停与恢复，正式食材正常显示。此项仅证明上述按钮路径，不代表完整胜利局或全部输入边界通过。用户反馈清空暂存和打乱没有特效，并要求入锅弧线、入小碟直线且放慢；新动效方案正在等待用户确认，尚未实施、未上传。
- Motion Check: 用户“继续”确认动效方案；`.harness/qa/TASK-011/motion-r001/` 中飞行/清空实际 Unity 51 项、打乱实际 Unity 9 项通过。当前工作区对应动效、HUD 与输入源码哈希均与 `capsule-hud-r009` staging 一致；该候选已完成微信 SDK 导出与官方 CLI preview，主包 3249309 字节、data-package 18847844 字节、wasmcode 4422378 字节，正式包位于 `.harness/qa/TASK-002/v10/formal-integration-r002/capsule-hud-r009/staging/build/wechat/TASK-002-v9-export-20260923T070756-ac01bed7fdb24f0f903dabc06caab6a7/minigame`。开发者工具运行目视检查与完整对局仍待完成。
- Friend Privacy Check: 用户已完成后台填写；旧候选重编译后出现微信“用户隐私保护提示”，证明新声明已被平台读取。该隐私授权须用户本人点击，当前尚未验证真实好友数据返回。
