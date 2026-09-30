# Irregular Sixteen Plate Redeal

Task ID: TASK-047
Status: Review

## Current Change

打乱采用随机错落落点，移除固定三列布局；一次打乱投放16盘（不足时全部），入口拥堵等待空位，不重叠生成。删除持续的场内数量上限，打乱后恢复正常供给。

## Work Package

Work Package ID: WP-047-CODE
Goal: 错落落盘、首批16盘与正常后续补盘。
Spec References: 最新打乱规则。
Must Preserve: 整盘组合、暂存/订单、保底可点击目标、计时暂停、摩擦0.08/弹性0。
Allowed Write Paths: Bootstrap重排/供给绑定；打乱Core/Replay；直接诊断；SPEC及本Task。
Forbidden / Shared Paths: 无关奖励、资产及物理参数。
Depends On: TASK-046
Acceptance: 首批16、随机落点无初始交叠、正常补盘可超过16、保底目标及回放通过；Windows试玩。
Integrator: 当前code_agent。

## Result

- 核心七布局357项通过，包含16盘供给、超过16后的供给、库存/暂存/订单守恒及旧12盘重排记录回放。
- Unity实际奖励及打乱流程通过：首批16盘、全部生成时无碰撞体交叠、随机横向落点、可点击订单目标、计时冻结/恢复、后续补盘超过16。
- 1080×1920截图检查通过，盘子错落排列；证据 `.harness/qa/TASK-047/`。
- 未上传微信或部署云函数。
- Windows Development Player 构建成功，0错误、0警告；入口 `.harness/qa/TASK-047/integrated-r001/Player/HotpotSortTask044.exe`。
