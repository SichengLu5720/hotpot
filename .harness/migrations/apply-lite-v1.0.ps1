param(
    [string]$SourceRoot = 'D:\Harness调试\gamedev-harness-lite-v1.0'
)

$ErrorActionPreference = 'Stop'
$workspace = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$source = [System.IO.Path]::GetFullPath($SourceRoot)
$backup = [System.IO.Path]::GetFullPath((Join-Path $workspace '.harness\backups\harness-before-lite-v1.0-20260923'))

if (-not (Test-Path -LiteralPath (Join-Path $workspace '.git'))) {
    throw "Workspace validation failed: $workspace"
}
if (-not (Test-Path -LiteralPath (Join-Path $source 'AGENTS.md'))) {
    throw "Lite Harness source validation failed: $source"
}
if (-not $backup.StartsWith([System.IO.Path]::GetFullPath((Join-Path $workspace '.harness\backups')), [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Backup path escaped workspace: $backup"
}

New-Item -ItemType Directory -Path $backup -Force | Out-Null

$backupFiles = @(
    'AGENTS.md',
    'HARNESS_README.md',
    'models.toml',
    'HarnessModelDashboard.exe',
    '.codex\config.toml',
    'tasks\_TEMPLATE.md',
    'docs\GAME_SPEC.md',
    'docs\ART_BIBLE.md'
)

foreach ($relative in $backupFiles) {
    $from = Join-Path $workspace $relative
    if (-not (Test-Path -LiteralPath $from)) { continue }
    $to = Join-Path $backup $relative
    New-Item -ItemType Directory -Path (Split-Path -Parent $to) -Force | Out-Null
    Copy-Item -LiteralPath $from -Destination $to -Force
}

$agentBackup = Join-Path $backup '.codex\agents'
New-Item -ItemType Directory -Path $agentBackup -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $workspace '.codex\agents') -File -ErrorAction SilentlyContinue |
    ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $agentBackup $_.Name) -Force }

$replaceFiles = @(
    'AGENTS.md',
    'HARNESS_README.md',
    'models.toml',
    'HarnessModelDashboard.exe',
    '.codex\config.toml',
    '.codex\agents\code-agent.toml',
    '.codex\agents\visual-agent.toml',
    'tasks\_TEMPLATE.md'
)

foreach ($relative in $replaceFiles) {
    $from = Join-Path $source $relative
    $to = Join-Path $workspace $relative
    if (-not (Test-Path -LiteralPath $from)) {
        throw "Missing Lite Harness payload: $relative"
    }
    New-Item -ItemType Directory -Path (Split-Path -Parent $to) -Force | Out-Null
    Copy-Item -LiteralPath $from -Destination $to -Force
}

$toolFiles = @('Program.cs', 'HarnessModelDashboard.csproj')
foreach ($name in $toolFiles) {
    $from = Join-Path $source "tools\native-dashboard\$name"
    $to = Join-Path $workspace "tools\native-dashboard\$name"
    New-Item -ItemType Directory -Path (Split-Path -Parent $to) -Force | Out-Null
    Copy-Item -LiteralPath $from -Destination $to -Force
}

$gameSpec = Get-Content -LiteralPath (Join-Path $workspace 'docs\GAME_SPEC.md') -Raw
$artBible = Get-Content -LiteralPath (Join-Path $workspace 'docs\ART_BIBLE.md') -Raw
$spec = @"
# 火锅消消 Project SPEC

本文件由用户确认切换至 GameDev Harness Lite v1.0 时建立。它合并既有稳定产品与美术事实；旧 `GAME_SPEC.md`、`ART_BIBLE.md` 保留为迁移来源和历史证据，后续新事实只更新本文件。

## Product Goal

- 制作可在微信小游戏中稳定运行的《火锅消消》每日挑战。
- 当前交付目标：并行完成玩法、平台与美术整合，使正式微信包在微信开发者工具中完整走通冷启动、远程资源、静默登录与云档案、完整一局、奖励、好友榜、失败复活和重试；修复明显运行错误后上传微信后台，由用户设置体验版并做最终风格判断。
- 未经新的明确授权，不提审、不正式发布。

## Core User Flow

- Entry：本地入口页立即可用；远程正式资源在点击开始后准备。
- Trigger：用户点击开始，资源完整校验并激活后才创建当日挑战会话。
- State Changes：登录和档案同步不阻塞入口；若登录在一局开始后完成，当前局保持原本地档案，回到入口再合并并切换微信档案。
- Success：完成当日挑战，写入首通并排队同步；好友榜刷新真实累计首通次数。
- Failure / Cancel：资源、分享、广告、网络或云同步失败不得伪造成功；允许按现有入口重试。终局、退出、重试和旧会话回调必须取消或去重。

## Rules and Data

- 当前玩法、订单、库存、供给、遮挡、点击、失败复活、奖励额度和每日挑战规则以本文件下方合并的既有规则为准。
- 分享奖励每日三次：首次立即、第一次生效后等待 5 分钟、第二次生效后等待 15 分钟，北京时间 06:00 重置。
- 失败复活将所有暂存食材移入一只大盘，时间到不弹暂存满复活。

## UI and Visual Behavior

- 使用单一现代轻量候选：暖白、珊瑚红、茶绿、轻卡片与线性图标；不恢复旧牌匾、厚金边和红织物框。
- 食材保持已实现的 80% 可点击几何、盘内轻微压叠与有限随机旋转；通过材质、对比与层级提升饱满感，不使显示与点击几何分离。
- 用户最终只对风格与整体体验作人工判断；接受后保存 Baseline。

## Platform and Persistence

- 平台：微信小游戏；AppID、云环境、广告位及远程文件标识只通过本机私密配置或构建参数注入，不写入仓库。
- 云档案：可信微信身份、首通集合和奖励账本只增不减合并；未完成对局不上传。
- 好友榜：只使用真实微信开放数据域关系链数据，不显示假头像、假昵称或填充排行。
- 普通分享使用微信系统菜单；奖励分享使用弱校验后台往返；激励视频只有明确完整观看才发奖。
- 已授权：将验证后的版本上传微信后台。用户自行设置体验版。未授权：提审、正式发布。

## Must Preserve

- 玩法坐标、物理与点击映射一致，不能出现看得见但点不到。
- 盘子之间不重叠；仅盘内食物可轻微重叠。
- 锅面在入锅动画后不改变。
- 不因平台能力失败免费发奖或自动切换渠道。
- 旧构建、预览、资产和 Harness 规则保留为历史证据，不破坏性删除。

## Accepted Baselines

| Baseline | Date | Scope | Build / Artifact | Notes |
|---|---|---|---|---|
| Pending | 2026-09-23 | 微信全流程与 r017 视觉候选 | 待开发者工具实测及用户体验 | 接受后保存 |

## Migrated Product Rules

$gameSpec

## Migrated Art Bible

$artBible
"@
[System.IO.File]::WriteAllText((Join-Path $workspace 'docs\SPEC.md'), $spec, [System.Text.UTF8Encoding]::new($false))

$task = @"
# 微信全流程候选与 Lite Harness 迁移

Task ID: TASK-011  
Status: Working  
Updated At: 2026-09-23

## Requirement Delta

- Goal: 形成可直接在微信开发者工具完成全流程测试、并可上传后台供用户设置体验版的正式候选。
- Current Behavior: 正式包已构建；Windows 锁屏阻止真实 DevTools 交互。
- Target Behavior: 完成冷/暖资源、登录同步、完整一局、奖励、好友榜、复活与重试；修复明显错误后上传。
- Entry / Trigger: 微信开发者工具打开最终 minigame 包。
- State Change: 技术候选 → DevTools 全流程候选 → 已上传后台。
- Boundary / Failure / Cancel: 不提审、不发布；平台失败不得伪装成功。

## Spec References

- `docs/SPEC.md#product-goal`
- `docs/SPEC.md#core-user-flow`
- `docs/SPEC.md#platform-and-persistence`
- `docs/SPEC.md#ui-and-visual-behavior`

## Must Preserve

- 当前玩法规则、点击/视觉一致性、r017 视觉方向、真实平台数据与奖励安全边界。

## Non-goals

- 微信审核提交、正式发布、Banner/插屏、全服榜、群榜。

## Current Change

- 完成最终 DevTools 交互验证、修复循环、后台上传和 Lite Harness 切换。

## Work Packages

| ID | Agent | Goal | Allowed Write Paths | Forbidden / Shared Paths | Depends On | Status | Integrator |
|---|---|---|---|---|---|---|---|
| CODE-CLOUD | `code_agent` | 远程资源与云档案 | Platform/Profile, RemoteAssets, cloudfunctions | Presentation | None | Completed | CODE-INTEGRATE |
| CODE-REWARD | `code_agent` | 分享与激励视频 | Platform/WeChat reward modules | Bootstrap, Presentation | None | Completed | CODE-INTEGRATE |
| CODE-BOARD | `code_agent` | 开放数据域好友榜 | WeChatOpenData, board tests | Bootstrap, Presentation | None | Completed | CODE-INTEGRATE |
| VIS-R017 | `visual_agent` | 现代轻量视觉与好友榜容器 | Presentation, preview r017 | Core, Platform | None | Completed | CODE-INTEGRATE |
| CODE-INTEGRATE | `code_agent` | 共享接线、正式构建与真实包 | Bootstrap, WeChatBuild, build output | Visual decisions | 上述四包 | Completed | Same Agent |
| DEVTOOLS | `code_agent` | 开发者工具全流程与修复 | 定向代码、隔离构建、证据 | 发布 | CODE-INTEGRATE | Blocked by Windows lock | Same Agent |
| UPLOAD | `code_agent` | 上传验证版本 | 微信后台版本 | 提审、发布 | DEVTOOLS | Draft | Same Agent |

## Acceptance

- User-visible result: 微信开发者工具可走通目标全流程，r017 画面可供用户体验。
- Minimum runtime check: 编译、启动、当前核心流程、明显错误、Diff 范围。
- User review method: 用户设置体验版并判断玩法与风格；接受后保存 Baseline。

## Result

- Status: Working
- Changed Files / Areas: 见各工作包证据。
- Build / Launch: 正式包已生成，等待解锁后的 DevTools 交互。
- Current Change Check: 自动构建与包检查通过；设备交互未完成。
- Screenshot / Artifact: `.harness/previews/TASK-001/r017/` 与正式 minigame 包。
- Known Issues: Windows 锁屏；真实广告位 ID 若缺失则保持不可用。
- Baseline: Not Saved
"@
[System.IO.File]::WriteAllText((Join-Path $workspace 'tasks\TASK-011-wechat-integrated-candidate.md'), $task, [System.Text.UTF8Encoding]::new($false))

$manifest = [ordered]@{
    schema = 1
    migratedAt = '2026-09-23'
    source = $source
    backup = $backup
    registeredAgents = @('code_agent', 'visual_agent')
    preservedLegacySpecs = @('docs/GAME_SPEC.md', 'docs/ART_BIBLE.md')
    currentTask = 'tasks/TASK-011-wechat-integrated-candidate.md'
}
[System.IO.File]::WriteAllText((Join-Path $workspace '.harness\migrations\lite-v1.0-result.json'), ($manifest | ConvertTo-Json -Depth 5), [System.Text.UTF8Encoding]::new($false))

Write-Output 'Lite Harness migration completed.'
