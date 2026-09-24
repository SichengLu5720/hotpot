# TASK-023：当前更新整合、Push 与微信开发版上传

Status: Working

## Current Change

整合 2026-09-24 当前已实现并具备直接验证的游戏更新：零摩擦/持续下落与原生碰撞、按压 1.5、入口图标和全按钮点击震动、Difficulty 1 基础选单与前 15 单等概率推进保护、可点击缓存、连锁订单表现、A+ 正式音乐/环境/操作音效。新局完成 15 单后完全使用 Difficulty 1 基础算法，不使用此前 80/20 或保护池暂存分组；旧 Difficulty 3 与旧策略仅保留回放兼容。Git 同步已采用的 Lite Harness 配置与对应工具源码。生成与所提交源一致的微信开发包，上传为 `0.0.15` 并执行 CLI 自动预览。

## Must Preserve

- 保留工作区所有无关用户修改；只提交明确的源代码、正式资产、SPEC/Task 和已采用 Harness 文件。
- 不提交 `.harness/**`、`build/**`、`Builds/**`、Unity 缓存、测试 `bin/obj`、下载依赖、预览、日志或临时导出。
- 从已验证且裁剪过的 staging 资产集继续整合，不用 `git archive` 重建。正式视觉资产字节保持；若当前已确认文案触发字体门禁，只允许从同一正式字体最小扩充缺失字形并同步 glyph 清单/manifest，不得换字体、改变字形风格或跳过门禁。
- 上传包必须与本次 push 的游戏源和正式音频一致；检查最终包原生震动、GLX、音频资源与关键规则符号。
- 授权仅限 Commit、Push、微信开发版上传和 CLI 自动预览；不得 Merge、Tag、提审、发布或设置体验版。

## Work Packages

### WP-INT-01：共享代码与正式资产整合验证

Work Package ID: WP-INT-01
Goal: 审计当前游戏改动，串行运行直接相关最低检查，并生成与当前源一致的正式微信 staging/export。
Spec References: `docs/SPEC.md` 当前规则、视觉、震动与音频章节
Must Preserve: 当前各 Task 已验证行为、已裁剪 staging 资产集、面板配置与无关脏改。
Allowed Write Paths: 当前已改游戏源的直接集成缺陷；`.harness/qa/TASK-023/**`；已验证 staging 内对应源码/正式音频覆盖
Forbidden / Shared Paths: 冻结视觉重设计、玩法规则变更、未授权平台业务、发布状态
Depends On: TASK-009、TASK-014 至 TASK-022、TASK-024 与 TASK-025 当前实现
Acceptance: Unity/WebGL 编译；核心/Boot/物理/入口/音频关键检查通过；正式 SDK `exportDone`、面板不变；最终包关键符号与正式音频哈希存在；包体可供上传。
Integrator: code_agent

### WP-INT-02：精确提交与 Push

Work Package ID: WP-INT-02
Goal: 用显式路径 staging 提交当前已验证源、正式资产、SPEC/Task 与 Harness Lite 文件，并 push 当前分支。
Spec References: 本 Task Must Preserve
Must Preserve: 不纳入生成证据、缓存、构建、预览或未验证临时文件。
Allowed Write Paths: Git index、当前分支提交历史、`origin/feat/auto-push-skill`
Forbidden / Shared Paths: 其他分支、Merge、Tag、远端发布
Depends On: WP-INT-01
Acceptance: staged diff 范围正确；Commit 成功；push 后本地 HEAD 与远端分支一致；明确报告排除项。
Integrator: PM

### WP-INT-03：微信开发版 0.0.15 上传

Work Package ID: WP-INT-03
Goal: 使用 WP-INT-01 最终 minigame 包执行官方 CLI 上传及自动预览。
Spec References: 本 Task 授权边界
Must Preserve: AppID/私密配置不写入仓库或日志；上传前后核对包路径、版本、大小与关键符号。
Allowed Write Paths: 微信开发者平台开发版本；`.harness/qa/TASK-023/delivery/**` 回执
Forbidden / Shared Paths: 体验版激活、提审、发布、生产切换
Depends On: WP-INT-01、WP-INT-02
Acceptance: CLI `upload` 成功，版本 `0.0.15`；`auto-preview` 成功；分别报告上传成功、CLI 预览成功与未确认手机实际收到。
Integrator: PM

## Result

Working：WP-INT-01 已按 TASK-025 冻结源码重新完成。核心 20961 项、Boot 13 项、ContentBuildGuard、20 行 Difficulty 1 原表、新旧内容/策略回放兼容、物理、点击、按压、入口、音频和连锁订单检查通过；正式 SDK 导出 `exportDone=true` 且面板配置未改变。唯一最终包位于 `.harness/qa/TASK-015/export-r015/build/wechat/TASK-002-v9-export-20260924T101137-2dd79149d0b84881b752482e8d829d4e/minigame`，主包 2,107,995 B、wasm 分包 4,625,829 B、data 分包 16,116,856 B。311 条源码/内容记录匹配；包内已确认 `hotpot_daily_task025_difficulty1_v1`、canonical digest `e4f20831a07608f6c298e41217309471e11dbdc14a5367fe716a23d315961904`、policy 4、旧 D3 归档、原生震动、GLX、12 个正式音频及关键规则符号。字体仍仅从同一正式字体最小补入 9 个缺失字形，原 364 个字形轮廓与度量未改变。等待精确提交/Push 和微信开发版上传。
