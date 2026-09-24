# TASK-009：完整商业音频与点击震动

Status: Review

## Current Change

一次性完成可商业化音频：1 首 60–90 秒无缝循环 BGM、店内远景与汤锅沸腾环境声、食材弹起/飞行/入碟/下锅、订单完成、端锅与新锅、胜利和失败音效。按钮与道具不播放音效；所有可用按钮仅在点击动作正式触发时轻震一次，其余食材、到达和订单完成震动沿用现有规则。音源接受非独占商业授权，零采购成本优先；免费方案不满足质量或授权时先向用户提交付费候选，不自行消费。

## Must Preserve

- 不改变玩法结果、点击边界、飞行时长、动画节奏、广告奖励规则、视觉冻结版本或现有食材/到达/完成震动强度。
- 保留当前工作区全部无关未提交修改，不回滚、不覆盖。
- 禁用、未接受或未触发的点击不震；道具结果不追加第二次震动或音效。
- 音乐与音效继续使用现有独立开关和音量保存。
- 未经用户另行授权，不购买音源，不 Commit、Push、上传、提审或发布。

## Work Packages

### WP-AUD-01：商业音频候选、母版与授权证据

Work Package ID: WP-AUD-01
Goal: 找到或制作符合 SPEC 的零成本优先商业音频候选，交付可试听母版、运行候选和完整授权证据。
Spec References: `docs/SPEC.md` → Confirmed Audio Direction
Must Preserve: 不使用授权不明、仅个人/非商用或要求在游戏外单独再分发的来源；按钮和道具无音效。
Allowed Write Paths: `Unity/Assets/HotpotSort/Audio/TASK009/**`、`.harness/previews/TASK-009/**`、`.harness/artifacts/TASK-009/**`
Forbidden / Shared Paths: `docs/SPEC.md`、`tasks/**`、`Unity/Assets/HotpotSort/Runtime/**`、Scene、Prefab 和现有视觉资产
Depends On: None
Acceptance: 至少提供 BGM 候选和代表性 SFX 的真实可试听文件；最终候选具备来源、条款快照、获取日期、加工记录、哈希、采样率、声道、时长与循环点。
Integrator: visual_agent（资产侧）

### WP-AUD-02：音频运行接口、事件绑定与按钮点击震动

Work Package ID: WP-AUD-02
Goal: 建立稳定音频运行接口，绑定冻结事件并把全部有效按钮点击接入微信轻震；处理设置、并发、暂停、后台、广告与恢复。
Spec References: `docs/SPEC.md` → Haptics、Confirmed Audio Direction
Must Preserve: 当前共享文件中的入口震动、连锁订单、物理和视觉修改；不得用轮询猜测音频事件。
Allowed Write Paths: `Unity/Assets/HotpotSort/Runtime/Audio/**`、`Unity/Assets/HotpotSort/Runtime/Audio.meta`、`Unity/Assets/HotpotSort/Runtime/Bootstrap/Bootstrap.cs`、`Unity/Assets/HotpotSort/Runtime/Bootstrap/DailyProductionComposition.cs`、`Unity/Assets/HotpotSort/Runtime/Bootstrap/WeChatGameplayTapInput.cs`、`Unity/Assets/HotpotSort/Runtime/Presentation/GameplayFeedback.cs`、`Unity/Assets/HotpotSort/Runtime/Presentation/GameplayView.cs`、`Unity/Assets/HotpotSort/Runtime/Presentation/GameplayViewV7.cs`、与本工作包直接对应的 Editor 诊断新文件
Forbidden / Shared Paths: `docs/SPEC.md`、`tasks/**`、核心玩法/物理/奖励逻辑、现有视觉资产
Depends On: WP-AUD-01 仅用于最终资产绑定；稳定接口与点击震动可先实施
Acceptance: Unity 编译；有效按钮点击恰好请求一次轻震；禁用/未触发点击不震；音频按冻结事件播放并遵守设置、并发和生命周期规则。
Integrator: code_agent（最终共享文件集成与技术检查）

## Result

Review：用户已接受 A+「暖锅小馆热闹版」、九项操作音效与两层环境声。WP-AUD-04 已固化 12 个正式 OGG（约 1.3 MB），与接受的试听源哈希一致；WP-AUD-05 已从 Resources 加载并绑定全部 cue，落实音乐/环境循环、点状音效不循环、初始混音、独立音量/静音、暂停、后台、广告恢复、并发与终局尾音。Unity 编译通过，正式音频诊断 46 项和入口震动诊断通过，12 个资源哈希匹配且无按钮/道具 cue。当前为 IMPLEMENTED 与 EDITOR-VERIFIED；尚未进行微信真机主观混音验收，未 Commit、Push、上传或发布。

### WP-AUD-03：A+ 热闹版试听候选

Work Package ID: WP-AUD-03
Goal: 在 A「暖锅小馆」基础上生成更热闹但不拥挤的 A+ 真实试听候选。
Spec References: `docs/SPEC.md` → Confirmed Audio Direction → 音乐候选选择
Must Preserve: 96 BPM、A 的温暖旋律与整体结构、60–90 秒无缝循环、无人声、操作音效留白、零采购和既有权利证据；保留 A/B 原候选，不覆盖。
Allowed Write Paths: `.harness/previews/TASK-009/**`、`.harness/artifacts/TASK-009/**`
Forbidden / Shared Paths: `Unity/Assets/**`、`docs/SPEC.md`、`tasks/**`、所有运行代码与正式资产
Depends On: WP-AUD-01
Acceptance: 提供 A+ WAV/OGG、循环接缝试听、更新后的 manifest 与试听页；成功解码、无削波；用户实际试听接受后才可正式绑定。
Integrator: visual_agent（音频资产侧）

### WP-AUD-04：正式 Unity 音频资产

Work Package ID: WP-AUD-04
Goal: 将用户接受的 A+、九项操作音效与两层环境声固化为正式 Unity 资产，生成稳定路径、导入配置和资产清单。
Spec References: `docs/SPEC.md` → Confirmed Audio Direction → 2026-09-24 正式音乐选择
Must Preserve: 只采用已试听 A+ 和现有 SFX/环境候选；不得混入原 A/B、按钮/道具/解锁音效或开发依赖；母版与权利证据保持可追溯。
Allowed Write Paths: `Unity/Assets/HotpotSort/Resources/HotpotSort/Audio/TASK009/**` 及对应 `.meta`
Forbidden / Shared Paths: 所有运行代码、Scene、Prefab、SPEC、Task、现有视觉资产和 `.harness` 候选源
Depends On: WP-AUD-03 Accepted
Acceptance: 12 个正式 cue 文件及 Unity `.meta` 完整；BGM/环境循环，点状音效不循环；音乐/环境立体声、点状音效单声道；路径与哈希清单可供代码绑定。
Integrator: visual_agent（资产侧）

### WP-AUD-05：正式绑定、混音与运行检查

Work Package ID: WP-AUD-05
Goal: 将 WP-AUD-04 正式资产绑定至稳定 AudioCue，完成初始混音及游戏内生命周期验证。
Spec References: `docs/SPEC.md` → Haptics、Confirmed Audio Direction
Must Preserve: WP-AUD-02 已验证接口、当前共享文件全部既有修改、按钮/道具无声音、冻结事件与终局尾音顺序。
Allowed Write Paths: `Unity/Assets/HotpotSort/Runtime/Audio/**`、`Unity/Assets/HotpotSort/Runtime/Bootstrap/Bootstrap.cs`、`Unity/Assets/HotpotSort/Runtime/Presentation/GameplayFeedback.cs`、`Unity/Assets/HotpotSort/Runtime/Presentation/GameplayView.cs`、`Unity/Assets/HotpotSort/Runtime/Presentation/GameplayViewV7.cs`、与本包直接对应的 Editor 诊断
Forbidden / Shared Paths: 核心玩法/物理/奖励逻辑、Scene、Prefab、正式音频内容、SPEC、Task
Depends On: WP-AUD-04
Acceptance: 12 个 cue 全部从正式资源加载并绑定；Unity 编译；首页到对局、音乐连续、环境切换、音量/静音、暂停、后台、广告恢复、并发、终局顺序和缺失资源失败可诊断；无按钮/道具声音；实际微信听感保留给用户判断。
Integrator: code_agent
