# Redeal and Reward Fix

Task ID: TASK-046
Status: Review

## Current Change

用户确认：热身不计开锅，正式20/38单；修复换单/打乱奖励领取；打乱重排所有剩余整盘，排除暂存和已入锅食材，不改变盘内组合。先清盘，最多首批12盘从上方分批下放，每批最多3盘、间隔约0.1秒，拥堵暂停，之后正常补盘。保证至少一份可点击食材能进当前订单，无候选则不扣库存/额度。重排期间暂停计时与操作。底部盘可挤动但不惯性滑行。

## Work Packages

- WP-046-PHYS：code_agent，独占 PlatePresentationWorld 与 Task038 物理诊断；保持摩擦0.08/弹性0；验证底部止滑及碰撞推动。
- WP-046-CORE：code_agent，独占 DailySessionShuffle、回放及核心打乱诊断；DailySession共享文件仅修改队列不变量/State/ReplaySupply块；保持盘内容/库存/订单和随机流隔离。
- WP-046-INT：当前 PM 兼 code_agent，独占 Bootstrap、SessionController、Presentation、开锅属性、SPEC/本Task；串行整合和Unity验证。
- Depends On：物理和核心独立；最终整合依赖两包。禁止改无关资源、回滚他人修改、推送或上传。
- Acceptance：奖励成功/取消与额度，热身与正式开锅边界，打乱库存守恒/最多12盘/可点击保证/暂停恢复，底部止滑；Windows试玩。

## Result

- 核心重排：七布局322项检查通过，包含整盘/暂存/订单守恒、无候选拒绝、拥堵退盘、独立随机流和回放。
- 开锅/映射集成：40项通过；热身进度0、正式20/38门槛。
- Unity物理：底部止滑、碰撞仍可推动、休眠唤醒、密集盘面及支撑移除检查通过。
- Unity真实界面：奖励取消不扣额度、分享成功领取、取消选单保留库存、成功换单扣库存、打乱不超过12盘、重排计时冻结、可点击订单食材保证、恢复操作均通过。1080×1920换单与打乱截图已检查。
- 修复原因：介绍窗遮挡导致目标观察失效；换单领取被旧选单校验取消；LocalDevelopmentServices未转发奖励库存接口；旧打乱依赖拥挤盘面的原地换位可行性。
- 产物：`.harness/qa/TASK-046/`。未验证微信真机回调、未部署/上传。首次Unity编译缺少命名空间引用已修正；首轮界面诊断因截图改变viewport失败，恢复viewport后实际流程通过。
- Windows Development Player 构建成功（0错误、3警告），启动10秒检查正常，日志无异常；试玩入口 `.harness/qa/TASK-046/integrated-r001/Player/HotpotSortTask044.exe`。
