# TASK-011 道具与食材飞行动效

Status: Review

## Current Change

用户于 2026-09-23 确认进行核心特效品质升级：采用中等爽感，覆盖开放锅常驻热气、点击与落点、锅位解锁、订单完成与整锅向上端走、旧锅完全离场后的补锅、清空暂存和整盘打乱。端锅先抬起、短暂停顿、再向上拿走，从抬起到完全离场约 0.5 秒。只新增 2–3 种透明热气变体，其余素材优先复用。不得改变玩法、首次有效操作计时、库存、队列、奖励额度、锅面、物理半径或微信能力；不制作音频、胜利全屏特效，不上传或发布。

Review feedback：用户指出旧特效素材会周期性闪烁出现。常驻热气改用专用长交叉淡化；气泡和油光不得跟随每次热气同时脉冲，只保留低频、低透明反馈。

Review feedback：旧锅完全离场后，新锅必须从上方向锅位缓慢推入；暂存区自动匹配食材须等待新锅推入完成后再播放入锅飞行动画。核心库存与事件结算时序保持不变。

Review feedback：食材飞入火锅恢复直线轨迹；盘面直接入锅速度保持不变，暂存区自动匹配入锅单独放慢。

Current change：为提示、清空暂存和打乱的奖励卡片增加约 3 秒循环示意。每轮末尾淡出并销毁重建；提示只高亮，不使用指向线。只重组 `v7/r001` 新版正式素材，不生成或覆盖图片资产；示意与真实游戏状态完全隔离。

Review feedback：提示高亮范围收紧到单个食材；清空暂存的食材到达大盘后必须与大盘作为一个整体离场；打乱统一改为盘子先聚集、短时洗混、再重新排序展开。

Review feedback：清空暂存的食材上大盘后改为真实混合盘式分散、轻微重叠和有限角度，不使用规则网格；演示区域增加硬裁剪，离框元素不得穿出卡片。

Review feedback：三个道具每轮演示前增加 1 秒完整静止展示，之后继续播放原有约 3 秒动画；总周期延长至约 4 秒。

Current change：用户参考 28.8 秒竖屏收集视频确认食材按压与触觉增量。快速点按或按住松开均在松手时提交；按下锁定食材并用约 0.08 秒放大至 1.12 倍，轻微漂移允许，明显滑出取消且不形成拖拽。到达暂存区播放约 0.14 秒的 0.92→1.08→1.0 缩放，到锅沿用既有表现。微信真机在有效按压、到达暂存或火锅时轻震；完成锅以一次中等强度短震替代到达轻震。无效、取消、暂停、后台、退出、重试、终局和旧回调不新增震动；编辑器与 Windows 静默跳过。不增加设置开关，不改玩法、物理、点击边界、飞行时长、冻结画面、音效、上传或发布。

## Work Packages

### CODE-VFX-RUNTIME
- Goal: 提供版本化特效参数、独立锅热气调度、分阶段端锅补锅、对象复用及生命周期安全。
- Spec References: Animation and VFX；Core Rules。
- Must Preserve: `IPresentationPort`、`ViewSnapshot`、`ViewEvent`、首次有效操作计时、锅面、核心状态和事件语义。
- Allowed Write Paths: Runtime/Presentation/Contracts/PresentationLifecycle.cs；Runtime/Presentation/GameplayFeedback.cs；对应独立 Editor 诊断。
- Forbidden / Shared Paths: GameplayView.cs、GameplayViewShuffle.cs、Core、Bootstrap、Scene、正式资源与主题 JSON。
- Depends On: None。
- Acceptance: 四锅独立错相、锁定锅无热气；完成事件分阶段且旧锅完全离场后才补锅；暂停/后台/重试/退出/旧会话安全；编译及核心诊断通过。
- Integrator: PM。

### VIS-STEAM-ASSETS
- Goal: 生成并筛选 2–3 个柔和、真实透明 Alpha 的热气变体。
- Spec References: Animation and VFX；Asset Production Rules。
- Must Preserve: 现有 `steam.png` 回退、传统热锅食欲感、无文字水印和多余物体。
- Allowed Write Paths: Resources/Hotpot/TASK001/v7/r001/fx/steam_soft_*.png 及对应 `.meta`。
- Forbidden / Shared Paths: 既有资产、主题 JSON、脚本、Scene、Prefab、Core。
- Depends On: None。
- Acceptance: 真透明、边缘干净、手机缩放可读、三款同一家族但轮廓不同。
- Integrator: PM。

### PM-VFX-INTEGRATION
- Goal: 串行绑定资产、补齐主题参数并统一点击、落点、解锁、端锅、清空和打乱节奏。
- Spec References: Animation and VFX。
- Must Preserve: 玩法与平台逻辑、点击映射、物理和 30 FPS 目标。
- Allowed Write Paths: presentation-theme.json；GameplayView.cs；GameplayViewShuffle.cs；GameplayFeedback.cs；必要资源导入设置。
- Forbidden / Shared Paths: Core、微信平台接口、Scene、Prefab。
- Depends On: CODE-VFX-RUNTIME；VIS-STEAM-ASSETS。
- Acceptance: 编译启动，受影响核心路径可见可走通，无明显新增错误或射线拦截。
- Integrator: PM。

### PM-TOOL-DEMO
- Goal: 在三种道具奖励卡片中加入 3 秒循环、淡出后重建的功能示意。
- Spec References: Animation and VFX；Tool reward demonstrations。
- Must Preserve: 道具效果、奖励路由与额度、倒计时、核心状态、物理和真实随机流。
- Allowed Write Paths: GameplayViewV7.cs；新增独立 Presentation 演示脚本；对应 Editor 定向检查；本 Task 与 SPEC。
- Forbidden / Shared Paths: Core、Session、微信平台接口、Scene、Prefab、现有 PNG 资产与主题 JSON。
- Depends On: 当前 `v7/r001` 盘子、食材、小碟和提示高光素材。
- Acceptance: 三种演示均约 3 秒循环，末尾淡出后重建；关闭、重试和会话变化无残留；所有示意节点不接收射线。
- Integrator: PM。

### CODE-PRESS-HAPTIC
- Goal: 增加盘内食材的按下锁定、松手提交、取消恢复、暂存到位缩放和微信轻/中强度触觉反馈。
- Spec References: Core Loop；Core Rules / Player；UX and Input Principles；Animation and VFX。
- Must Preserve: Alpha/裁剪/UI 前景点击边界、核心点击命令及库存结果、0.34/0.46 秒直线飞行、锅面、冻结视觉资产和现有特效。
- Allowed Write Paths: GameplayView.cs；GameplayViewV7.cs；GameplayFeedback.cs；WeChatGameplayTapInput.cs；对应独立 Editor 诊断；本 Task 与 SPEC。
- Forbidden / Shared Paths: Core、Session、Scene、Prefab、正式图片与主题 JSON、设置 UI、音频、微信奖励/存档/好友接口。
- Depends On: 当前 GameplayView 点击映射、PlatePresentationWorld 命中、GameplayFeedback 路由事件及微信触摸桥。
- Acceptance: 点按与按住松手均只提交一次；轻微漂移成功、明显滑出取消；按压缩放/层级安全恢复；暂存到位缩放正确；震动事件强度与去重正确；暂停、后台、重试、退出和会话变化无残留；编译、受影响入口、定向诊断及 Diff 边界通过。
- Integrator: PM。

## Result

- 新增版本化 `PresentationEffects`、确定性的四锅错相热气调度和三张透明热气变体；原 `steam.png` 保留为回退。新素材按 512 上限、Clamp、Bilinear、Alpha Is Transparency 导入。常驻热气改成长交叉淡化，气泡与油光仅低频弱化出现，消除旧素材随每次脉冲闪现的问题。
- 端锅现为约 0.16 秒抬起、0.10 秒停顿、0.24 秒向上离场；旧锅完全隐藏后，新锅再用约 0.45 秒从上方推入。暂存区自动匹配食材等待新锅落稳后才入锅。
- 点击飞行增加即时抬起感；所有入锅轨迹为直线，盘面直接入锅保持约 0.34 秒，暂存自动入锅放慢至约 0.46 秒；锅与暂存落点分别使用强/弱涟漪；解锁补入点火与热气；清空暂存增加聚盘离场强调；打乱改为更轻的弧线、缩放和收束旋转。所有表现节点不接收射线。
- Unity 6000.0.26f1 定向结果：VFX Runtime 21 项通过（含推入期间延迟、落稳后放行及慢速暂存入锅）；Shuffle 9 项沿用前次通过；实际 Boot 的 `U04` 端锅/替换/层级/输入穿透 Smoke 通过。`U01` 仍按旧规则期待空闲直接超时，与已确认的“首次有效操作才计时”冲突，未为通过旧用例回退产品规则。
- Windows Development Player 构建成功，0 个构建错误，包体 341292977 字节；`HotpotSortV8` 已启动且进程正常响应，启动日志无新增脚本异常。微信上传、提审和发布未执行。
- 三种道具奖励卡片已加入独立缩略演示：提示仅循环高亮；清空暂存表现五碟食材汇入一只大盘并离场；打乱表现四只整盘带着盘内食材交换位置。每轮约 3 秒，末尾淡出后停用旧节点并重新生成下一种预设布局。
- 演示只使用当前 `v7/r001` 新版正式盘子、食材、暂存碟和提示高光素材，没有生成、复制或覆盖 PNG。Tool Demo Runtime 15 项通过；更新后的 Windows Development Player 构建成功，0 个构建错误，包体 401344792 字节，启动进程正常响应且日志无新增脚本异常。
- 根据试玩反馈收紧提示高亮至单个食材主体；清空暂存改为食材到达后与大盘共享同一运输组；打乱改为中央聚拢、短时交叉洗混、再展开排序。更新后的 Tool Demo Runtime 17 项通过，Windows Development Player 构建成功，0 个构建错误，包体 401345457 字节，启动正常且无新增脚本异常。
- 清空暂存上盘排布进一步改为分散、轻微重叠和有限角度的真实混合盘样式；演示根节点加入硬裁剪，所有离框盘子和食材立即隐藏。Tool Demo Runtime 21 项通过；Windows Development Player 再次构建成功，0 个构建错误，包体 401346681 字节，启动正常且无新增脚本异常。
- 三个道具的每轮演示前统一增加 1 秒静止展示，之后播放原有约 3 秒动画，总周期约 4 秒。Tool Demo Runtime 24 项通过；Windows Development Player 构建成功，0 个构建错误，包体 410059265 字节，启动正常且无新增脚本异常。
- `CODE-PRESS-HAPTIC` 已实现：盘内食材按下后约 0.08 秒放大至 1.12 倍并临时提层，松手只提交一次；14 逻辑单位内漂移允许，超出后取消并恢复尺寸与层级。快速点按与按住松手共用同一生命周期，不形成拖拽。暂存食材到位使用独立约 0.14 秒的 0.92→1.08→1.0 缩放；到锅不增加缩放，既有 0.34/0.46 秒直线飞行保持。
- 微信触觉桥已接入项目内真实 SDK：有效按压及普通到达使用轻型短震，完成锅的到达以一次中等短震替代轻震；轻震最小间隔 120ms，中震不被节流。强度参数失败时仅在同一有效会话内回退默认短震；编辑器与 Windows 不调用平台震动。暂停、后台、终局、会话代次变化及旧飞行会取消待触发的到达反馈。
- Unity 6000.0.26f1 定向按压/触觉检查 30 项通过，覆盖按住放大、快速点按、单次提交、漂移取消、层级恢复、暂存缩放、轻/中震替代、重复事件、暂停/后台/终局/代次取消，以及当前主题特效池饱和且没有飞行对象时仍按原时长触发到达反馈。真实 Boot→开始→松手收集 Smoke 6 项通过，核心食材恰好减少一份且重复松手被拒绝；微信条件分支使用项目真实 SDK 独立编译通过。两次 Unity 进程退出码均为 0，完整日志未发现 `AssetFailure`、`MissingReference`、异常或编译错误；证据为 `.harness/press-haptic-code.log` 与 `.harness/press-haptic-boot-focused.log`。
- 状态进入 Review：代码与本地自动范围已完成，等待用户实际试玩按压缩放和暂存弹性感；微信真机震动强弱、连续操作手感及设备兼容仍未 DEVICE-TESTED / HUMAN-ACCEPTED。本工作包已获授权 Commit / Push 当前分支；未执行上传、提审、发布或 Baseline 保存。
