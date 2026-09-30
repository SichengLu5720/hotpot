# Probabilistic Order Support

Task ID: TASK-034  
Status: Review  
Updated At: 2026-09-26

## Requirement Delta

- Goal: 将TASK-033动态保护降低为固定20%触发，恢复Difficulty 1的风险波动。
- Current Behavior: 危险单达到正常锅阈值后，每次补单都启用近期支持接管。
- Target Behavior: 阈值以下纯D1；达到或超过阈值时固定20%接管、80%走纯D1；不设置全危险100%保护。
- Entry / Trigger: 每次生成新订单时，先按既有近期支持规则统计危险单，再使用订单确定性随机流判定本次是否接管。
- State Change: 不保存永久概率状态；概率未触发时直接走原D1，触发时沿用TASK-033接管与降级候选规则。
- Boundary / Failure / Cancel: 广告锅计入危险单数量但不提高正常阶段阈值；达到阈值后的所有状态统一使用20%；换单不参与概率，仍保证近期可完成。取消10,000局通关率校准，不宣称20%触发率等于20%玩家通关率。

## Spec References

- `docs/SPEC.md#core-rules`
- `tasks/TASK-033-dynamic-order-support-and-swap.md`

## Must Preserve

- TASK-033的近期支持范围、危险单定义、正常锅阶段绑定、广告锅统计、候选降级、D1权重和换单全部行为。
- 同日相同内容与操作保持确定性，查询和表现不得消费概率随机流。
- 16种食材、50盘/183份、热身与正式订单、供给、物理、暂存、计时和37/55解锁节点不变。

## Non-goals

- 不修改换单UI、演示、动画、库存协议或云函数。
- 不改变保护概率以外的难度参数。
- 不提审、不设体验版、不发布、不提交、不推送、不部署云函数。

## Current Change

- 将策略6的阈值接管从100%改为确定性20%触发，不保留全危险100%兜底。
- 通过直接诊断后重新导出并上传微信开发版`0.0.22`。

## Work Packages

### WP-034-CODE

Work Package ID: WP-034-CODE  
Goal: 实现固定20%概率接管并验证确定性、边界和随机流隔离。  
Spec References: `Core Rules`动态接管条目  
Must Preserve: TASK-033全部既有核心与表现接口。  
Allowed Write Paths: `Unity/Assets/HotpotSort/Runtime/Core/**`; `Unity/Assets/HotpotSort/Runtime/Replay/**`; `Unity/Assets/HotpotSort/Runtime/Presentation/Diagnostics~/*OrderSupport*`; `.harness/qa/TASK-034/core/**`  
Forbidden / Shared Paths: Contracts/Bootstrap/Presentation视觉/云函数只读；SPEC与Task由PM维护；不得上传或Git提交。  
Depends On: None  
Acceptance: 大样本验证阈值以下0%、阈值以上约20%触发、无全危险兜底、广告锅统计、同日重试与回放一致、查询不消费随机流。
Integrator: `code_agent`

### WP-034-FONT

Work Package ID: WP-034-FONT  
Goal: 为用户确认纳入0.0.22的“一锅又一锅”新标题补齐正式裁剪字体缺失的`《`、`》`、`又`字形，并更新字体证据。  
Spec References: 新标题沿用当前正式字体视觉；不得改变标题布局与风格  
Must Preserve: 现有字体外观、字号、布局、包体裁剪与字体门禁；只扩充必要字形。  
Allowed Write Paths: `Unity/Assets/HotpotSort/Resources/**/fonts/**`; `Unity/Assets/HotpotSort/Editor/TASK002V9/**`; `.harness/qa/TASK-034/font/**`  
Forbidden / Shared Paths: Core/Bootstrap/GameplayView与其他正式资产只读；不得绕过门禁、上传或Git提交。  
Depends On: 当前新标题源码  
Acceptance: 新标题三个缺失字形可由正式字体渲染，字体门禁通过；渲染截图确认无缺字框且保持当前字体风格。
Integrator: `visual_agent`

### WP-034-BUILD-UPLOAD

Work Package ID: WP-034-BUILD-UPLOAD  
Goal: 从验证后的当前源码制作匹配微信包并上传开发版`0.0.22`。  
Spec References: 本Task全部  
Must Preserve: 使用与核心验收相同源码；保留TASK-033视觉与包体裁剪。  
Allowed Write Paths: `.harness/qa/TASK-034/delivery/**`; 独立staging/export输出  
Forbidden / Shared Paths: 不修改当前源码；不部署云函数、不提审、不设体验版、不发布、不提交、不推送。  
Depends On: WP-034-CODE; WP-034-FONT  
Acceptance: Unity编译/导出成功，最终包含概率策略身份；官方CLI预览、开发版上传和自动预览成功。
Integrator: `code_agent`制作与核对包；PM执行外部上传

## Acceptance

- User-visible result: 达到危险阈值后只有20%的补单获得保护，允许出现全部订单危险。
- Minimum runtime check: 大样本验证20%触发分布、无全危险兜底、确定性与随机流隔离；本地编译/启动无新增错误；微信最终包与源码一致。
- User review method: 试玩两锅、正常三锅和广告提前锅阶段，判断风险波动是否恢复且不会长时间全单无解。

## Result

- Status: Review
- Changed Files / Areas: `DailyDirector.cs`将阈值接管改为`NextBounded(5)==0`的确定性20%触发；OrderSupport诊断增加概率分布、随机流隔离和回放检查。为并行确认的新标题“一锅又一锅”补齐正式裁剪字体的`《`、`》`、`又`三字形及字体证据。
- Build / Launch: .NET编译及274,688项断言通过；五组各10,000固定种子中阈值以下0%，达到阈值、全危险和广告锅中间态均19.73%。Unity真实Composition 44项、Windows Development构建及Player smoke 44项通过。最终revision-c微信导出成功，官方CLI预览、`0.0.22`开发版上传和自动预览成功。
- Current Change Check: 达到或超过正常锅阈值后固定20%近期支持接管、80%D1，无全危险兜底；查询与换单不消费概率随机流，同日重试与回放一致。最终包同时包含TASK-033换单、nextLayer、正常37/55阶段绑定、新标题及必要字形。
- Screenshot / Artifact: `.harness/qa/TASK-034/core/`、`.harness/qa/TASK-034/font/`、`.harness/qa/TASK-034/delivery/revision-c/`；最终上传包目录记录于revision-c验证报告。
- Known Issues: 19.73%为接管触发率，不代表玩家通关率。官方CLI自动预览成功但不等于手机实际收到、打开或完成真机试玩。TASK-033新增免费云库存协议仍未部署，旧线上云函数下免费库存换单路径可能失败；未提审、设体验版或发布。
- Baseline: Not Saved
