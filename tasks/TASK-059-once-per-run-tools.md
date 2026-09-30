# Once-per-run tools
Task ID: TASK-059
Status: Review

## Current Change
按SPEC TASK-059实施：换单、清空暂存、打乱整轮各一次，热身继承到正式，只计成功。交付试玩。

## Work Packages
Work Package ID: WP-059-CORE
Goal: 核心用量、三道具拦截、提交计次、回放版本与工厂接口。
Spec References: SPEC TASK-059。
Must Preserve: 现有效果、D1/P/锅位、复活、历史回放、无关dirty修改。
Allowed Write Paths: Runtime/Core/**；Runtime/Replay/DailyReplay.cs；Runtime/Bootstrap/DailyViewMapper.cs；Tests/Diagnostics~/Task059Core*；.harness/qa/TASK-059/core/**。
Forbidden / Shared Paths: Composition、表现和契约、其他测试。
Depends On: 主Agent添加ViewSnapshot.usedToolMask(int)。
Acceptance: 三道具成功/重复/失败、继承/重开、复活与回放。
Integrator: 主Agent。

Work Package ID: WP-059-VIEW
Goal: 已使用道具灰显并禁用所有入口，保留单奖励按钮。
Spec References: SPEC TASK-059。
Must Preserve: 现有视觉资产与布局、无目标换单反馈例外（只限未使用）。
Allowed Write Paths: Presentation/GameplayView.cs；GameplayViewV7.cs；GameplayViewToolDemo.cs；必要GameplayViewSwapOrder.cs；Presentation/Editor/Task059*。
Forbidden / Shared Paths: Core、Bootstrap、契约、资产。
Depends On: ViewSnapshot.usedToolMask，Swap1/Clear2/Shuffle4。
Acceptance: 正常、已用、重开状态及无奖励/选择触发。
Integrator: 主Agent。

Work Package ID: WP-059-INTEGRATION
Goal: 整轮传递、奖励入口前后复核、实际运行与试玩。
Spec References: SPEC TASK-059。
Must Preserve: 三道具与各渠道原有消费时点。
Allowed Write Paths: Bootstrap/DailyProductionComposition.cs；DailyProductionSwapOrder.cs；DailyProductionRedeal.cs；Presentation/PresentationPort.cs；Editor/TASK001V7/Task059*；Tests/Diagnostics~/Task059Integration*；.harness/qa/TASK-059/**；SPEC和本Task。
Forbidden / Shared Paths: Agent所有权文件待交接后才能集成修改。
Depends On: CORE与VIEW。
Acceptance: 热身到正式、各渠道、取消/失败、实际按钮、构建入口。
Integrator: 主Agent。

## Result
已实现并集成：三道具成功后独立计次、整轮继承、重开重置、入口和奖励回调复核、用后灰显；历史回放保持旧规则。

已执行：61项核心检查；Unity界面mask0/1/2/4/7与过期弹窗检查；实际热身清空→正式继承→打乱→换单动画完成→重开恢复流程；取消/失败不计次；热身与正式实际回放精确复算，均通过。初次运行诊断直接调用换单核心入口，未建立表现状态而超时；诊断改走实际界面选择路径后通过，无对应产品代码修补。

Windows构建成功，0错误、1条现有弃用API警告。独立Player启动响应正常；当前机器FMOD没有音频设备，声音未验证。相关Diff空白检查通过。证据：`.harness/qa/TASK-059/{core,view,visual,integrated}`。

试玩：`.harness/qa/TASK-059/integrated/Player/HotpotSortTask059.exe`。待用户体验接受；未提交、上传或发布，未做微信真机验证。
