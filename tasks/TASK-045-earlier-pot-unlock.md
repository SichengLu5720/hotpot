# Earlier Pot Unlock

Task ID: TASK-045
Status: Review

## Current Change

用户确认方案 A：正式第 20/38 单开启第三/第四锅，累计门槛 26/44。保底阶段阈值同步，其他概率、内容和奖励行为不变。

## Work Package

Work Package ID: WP-045-CODE
Goal: 提前自动开锅并统一核心和提示门槛。
Spec References: Rules and Data；UI and Visual Behavior
Must Preserve: 两锅开局、61 单正式通关、提前解锁、Legacy 规则。
Allowed Write Paths: DailySession.cs；DailyViewMapper.cs；直接诊断；本 Task。
Forbidden / Shared Paths: 其他视觉、物理、奖励和内容。
Depends On: None
Acceptance: 正式 19/20 与 37/38 单边界、提前解锁及提示一致。
Integrator: 当前 code_agent

## Result

- 当前试玩参数：盘—盘及盘—边界摩擦 0.08，弹性 0。Unity 休眠、唤醒、密集盘面及移除支撑检查通过；Windows r004 构建成功（0 错误、1 警告）。最新入口：`.harness/qa/TASK-045/integrated-r004/Player/HotpotSortTask044.exe`。

- 最新试玩反馈：盘—盘与盘—边界摩擦、弹性均改为 0，保留线性阻尼及休眠唤醒。Unity 专用物理检查通过，证据 `.harness/qa/TASK-045/zero-contact-physics.json`。
- 零摩擦/零弹性 Windows 新包构建成功（0 错误、1 警告）：`.harness/qa/TASK-045/integrated-r003/Player/HotpotSortTask044.exe`。

- Completed：核心累计门槛改为 26/44；显示映射复用核心常量，保底阶段同步读取正常开锅数量。
- 直接 .NET 集成检查 38 项通过，含正式 19/20、37/38 单边界的实际锅位状态、奖励提前解锁和七日布局/回放检查。
- 本次 Windows Development Player 已重新整合构建成功（0 错误、3 警告），包含 TASK-037～045。38 项直接检查再次通过；Player 启动 12 秒仍运行，日志未见异常。
- 新包：`.harness/qa/TASK-045/integrated-r001/Player/HotpotSortTask044.exe`；更新清单：`docs/UPDATE-20260928-integrated.md`。
- 未重新导出或上传微信版本。换单完整交互和微信真机体验仍待验证。
- 后续按用户“合并”指令合入 TASK-038 工作树的 0.35 摩擦、TASK-042 卡片跟进提交及 TASK-043 单奖励入口弹窗。已通过 Unity 编译、物理休眠/唤醒诊断、锁定锅表现诊断和 38 项直接集成检查；之前的 Windows 包不包含这三项后续合并。
