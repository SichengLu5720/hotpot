# Twenty Four Plate Redeal and Simple Demo

Task ID: TASK-048
Status: Review

## Current Change

用户确认首批24盘，不足则全部；打乱介绍仅示意旧盘清空后少量整盘从上方错落落下。

## Work Packages

Work Package ID: WP-048
Goal: 首批24盘与简化介绍动画。
Spec References: SPEC 最新打乱规则与 Tool reward demonstrations。
Must Preserve: 随机安全落点、后续供给无数量上限、整盘/暂存/订单、奖励、可点击保底、计时规则。
Allowed Write Paths: DailySessionShuffle、DailyProductionRedeal、GameplayViewToolDemo、对应诊断、SPEC与本Task。
Forbidden / Shared Paths: 无关物理参数、奖励实现和其他演示。
Depends On: TASK-047
Acceptance: 24盘、防初始重叠、后续供给与演示检查；Windows试玩包。
Integrator: 当前执行代理串行集成。

## Result

- 七布局核心检查420项通过，含24盘、后续第25盘供给与旧12/16盘回放兼容。
- Unity实际奖励/打乱流程通过：24盘生成无初始碰撞体交叠、错落落点、保底可点击目标、计时冻结及输入恢复、后续补盘不设数量上限。
- 演示29项检查通过；1080×1920清空、下落与落稳截图检查通过。证据 `.harness/qa/TASK-048/`。
- Windows Development Player构建成功，0错误；1项既有FindObjectOfType过时警告。试玩入口 `.harness/qa/TASK-048/integrated-r001/Player/HotpotSortTask044.exe`。
- 不上传、不部署；用户试玩验收待定。
