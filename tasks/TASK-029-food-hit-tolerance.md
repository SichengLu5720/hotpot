# 盘内食材透明轮廓点击宽容

Status: Review

## Current Change

- Goal: 解决细长、弯曲、圆环及透明留白较多的食材看得见但难以选中的问题。
- Spec References: `docs/SPEC.md` Basic Gameplay Rules、UX and Input Principles。
- Current Behavior: 首次按下必须命中食材贴图透明度高于约 10% 的真实像素；透明边缘与留白不响应。
- Target Behavior: 原始精确像素命中优先；失败后仅在原触点所在的当前最上层盘内，以 10 个逻辑像素搜索最近的真实可见轮廓，平局优先视觉上层；16 种食材统一适用。
- Boundary / Failure / Cancel: 不跨盘、不越盘子圆形边界、不越盘面裁切、不穿上层盘；暂停、后台、模态、终局、飞行中及明显滑出仍不可接受；无候选保持无响应。
- Must Preserve: 食材图片、透明度、尺寸、位置、层级、物理碰撞、80% 视觉包络、订单/库存/暂存/倒计时/随机、提示与现有按压/触觉/飞行动画。
- Non-goals: 锅、订单牌、暂存区和按钮；重绘/放大食材；改变盘子尺寸或排布。

## Work Packages

### WP-029-CODE

Work Package ID: WP-029-CODE
Goal: 实现盘内食材 10 逻辑像素透明轮廓点击宽容，并保持精确命中、遮挡、裁切和输入生命周期规则。
Spec References: 本 Task Current Change；SPEC Basic Gameplay Rules。
Must Preserve: 所有现有未提交修改，尤其 TASK-026、TASK-027、TASK-028；点击精确命中顺序、按压缩放、触觉、松手滑出取消、玩法提交及 Clickability/Hint 语义。
Allowed Write Paths: `Unity/Assets/HotpotSort/Runtime/Presentation/GameplayView.cs`; `Unity/Assets/HotpotSort/Runtime/Presentation/GameplayViewClickability.cs`; `Unity/Assets/HotpotSort/Runtime/UnityPhysics/PlatePresentationWorld.cs`; 本任务专用 `Unity/Assets/HotpotSort/Runtime/Presentation/Editor/Task029*` 与 `Unity/Assets/HotpotSort/Tests/Diagnostics~/Task029*`; `.harness/qa/TASK-029/`。
Forbidden / Shared Paths: Core、Session、Bootstrap、Contracts、Platform、正式图片资产、SPEC、其他 Task、现有预览目录。
Depends On: None.
Acceptance: 使用真实正式食材资产覆盖 16 种精确命中与透明边缘宽容；验证 10/10.1 边界、最近轮廓、视觉上层平局、同盘遮挡、上层盘阻挡、原触点盘边、盘面裁切、UI/暂停/后台/终局门禁、明显滑出取消；宽容命中触发既有按压/触觉/提交且不改变任何数据与几何。
Integrator: Yes。

### WP-029-WECHAT

Work Package ID: WP-029-WECHAT
Goal: 将包含 WP-029-CODE 点击宽容改动的当前整合候选导出并上传为微信小游戏开发版 `0.0.18`，供用户真机体验。
Spec References: 本 Task Current Change；SPEC Platform、Build and Delivery。
Must Preserve: 当前 `0.0.17` 的正式配置、身份、分包、GLX、2× 渲染倍率及全部已整合内容；不得从旧包直接上传或遗漏三项生产代码改动。
Allowed Write Paths: `.harness/qa/TASK-029/` 下本工作包专用 staging、导出、校验与上传证据。
Forbidden / Shared Paths: 当前工作树生产源码、正式资产、SPEC、其他 Task、微信后台体验版/审核/发布状态。
Depends On: WP-029-CODE Completed。
Acceptance: 从当前整合源码形成独立 staging；Unity/微信 SDK 导出成功；最终小游戏包可证明包含本次三项生产改动；官方 CLI 上传 `0.0.18` 成功并取得结构化回执；CLI 自动预览成功。
Integrator: Yes。

## Result

- User-visible result: 已实现。精确透明像素命中仍优先；精确命中失败后，盘内食材可在真实可见轮廓外 10 个逻辑像素内选中，最近轮廓优先，平局优先视觉上层。
- Minimum checks: Unity 编译与 TASK-029 定向诊断通过，最终日志 `.harness/qa/TASK-029/food-hit-r11.log` 记录 `TASK029_FOOD_HIT_PASS checks=136`；覆盖 16 种正式食材、10/10.1 边界、最近/平局、遮挡、盘边、裁切、UI 与输入门禁、释放和滑出取消；定向 `git diff --check` 通过。
- Performance observation: 桌面诊断中单食材首次轮廓缓存最慢约 27.28 ms，热缓存搜索最慢约 2.79 ms；仅在精确命中失败时执行，不进入逐帧逻辑。
- Remaining review: 微信真机点击手感与性能尚未验证，等待用户体验判断。
- Development upload: 微信开发版 `0.0.18` 已上传成功，说明“食材点击宽容真机体验”；官方 CLI 自动预览成功。主包 2,445,377 B，数据分包 14,665,639 B，WASM 分包 4,644,734 B，总包 21,755,750 B。独立 staging 的 323 个源码/配置文件与当前输入哈希一致，三项生产改动已在 IL2CPP 与最终压缩数据中确认；GLX、2× 渲染倍率、正式分包、运行配置与身份保持不变，BGM 不存在。证据位于 `.harness/qa/TASK-029/`。
- Device boundary: CLI 上传与自动预览成功不证明手机实际收到或打开；点击手感及真机性能等待用户确认。
- Boundary: 未授权 Commit、Push、Upload、Review 或 Release。
