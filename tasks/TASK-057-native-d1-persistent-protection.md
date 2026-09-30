# Native D1 and Persistent Protection
Task ID: TASK-057
Status: Review

## Current Change
用户确认同时接入已核对包内 D1 与单一跨天保护概率 P，交付 Windows 试玩入口。P 下限0、上限20；新用户20，旧档首次迁移0；正式最终失败+3，通关-5；退出、热身、复活成功、奖励失败不变更。局首冻结 P，前十单达到既定危险阈值仍100%保护。普通布局接入原包逐份成本、先抽后降级、类别内去重、顺序选择、优先状态及目标交换；保持生成、物理、热身和两开局订单。

## Work Packages
Work Package ID: WP-057-PROFILE
Goal: 单一 P 的持久化、幂等结果、账号合并与云协议源码兼容。
Spec References: SPEC TASK-057。
Must Preserve: 现有奖励/账号事实、旧回放及无关修改；不部署云函数。
Allowed Write Paths: Contracts/DevelopmentServices.cs；Runtime/Platform/Profile/**；Runtime/Bootstrap/LocalDevelopmentServices.cs；cloudfunctions/hotpotProfileSync/**；Tests/Diagnostics~/Task057Profile*；.harness/qa/TASK-057/profile/**。
Forbidden / Shared Paths: Core、Replay、其他 Bootstrap、表现、SPEC。
Depends On: 无。
Acceptance: 新/旧用户、上下限、混合输赢、重复结果、跨天重启、合并与账号分区测试。
Integrator: 主Agent。

Work Package ID: WP-057-CORE
Goal: 接入 D1、P冻结与回放，构建运行试玩。
Spec References: SPEC TASK-057；研究 AUDIT.md。
Must Preserve: 上述保护、出生物理、热身、道具、七布局、无关修改。
Allowed Write Paths: Runtime/Core/**；Runtime/Replay/DailyReplay.cs；Runtime/Bootstrap/DailyProductionComposition.cs；Runtime/Presentation/GameplayViewClickability.cs 与观察契约；Tests/Diagnostics~/Task057*（Profile除外）；Editor/TASK001V7/Task057*；.harness/qa/TASK-057/**（profile除外）；docs/SPEC.md；本Task。
Forbidden / Shared Paths: WP-057-PROFILE 文件、正式资产。
Depends On: Profile 提供 IOrderProtectionProfileStore.OrderProtectionPercent。
Acceptance: 原包普通布局分支对照、保护P与前十边界、观察/交换/回放、Unity运行与Windows构建。
Integrator: 主Agent。

## Result
- WP-PROFILE、WP-CORE 与观察适配已完成。新生产入口使用 supportVersion=3 / nativeD1Version=1；旧分支仅保留历史API/回放兼容。食材入锅累计进度、优先状态、几何观察、交换与P局首值均可重放。
- 核对通过：Native专项1118项（含250组独立参考局面）；核心集成12135项（七布局、P0/20、前十边界、查询无副作用、换单/交换、历史与实际Unity回放）；Profile36项；云协议源码17组。真实Unity运行10项检查通过，1080×1920与1080×1440截图已生成；主界面截图无明显破损。
- Windows构建 Succeeded，0 errors/0 warnings；独立Player已启动、窗口响应正常。当前机器FMOD音频设备初始化失败，使用无声软件输出；该环境问题未隐藏。入口 `.harness/qa/TASK-057/integrated/Player/HotpotSortTask057.exe`。包未压缩。用户手感验收待进行。
- 适配边界：使用已核对五类D1的a分支优先状态；不能认定原设备远程实验分组。随机源保留项目PCG与稳定候选顺序，不承诺原包逐次随机一致。当前普通盘类型按初始/重铺容量映射；未引入冻结/钥匙等原包特殊关卡。查询保留项目纯查询约定。
- 订单统计线使用食材中心与可见区域上沿，区别于出生停止线及整份离屏证书。跨Mono/CoreCLR诊断小数已规范化，实际Unity记录复放通过。
- 云函数仅修改并本地验证源码，未部署；Windows本地试玩可用，微信旧云函数尚不接受新字段。无提交、上传或发布。

