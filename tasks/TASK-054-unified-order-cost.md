# Unified D1 Supply Cost
Task ID: TASK-054
Status: Review

## Current Change
用户授权新版现成判定、成本A全部食材计数、D归一化；原暂存边界与全部权重不变。

## Work Packages
Work Package ID: WP-054-CODE
Goal: 贯通观察、选单和回放，重写成本并验证。
Spec References: SPEC TASK-054。
Must Preserve: 开局两单、权重表、阶段边界、概率保护及其降级、换单限制、生成与物理、无关脏修改。
Allowed Write Paths: Runtime/Core/DailyCommands.cs; DailyDirector.cs; DailySession.cs; DailySessionSwapOrder.cs; Runtime/Presentation/GameplayViewClickability.cs; Runtime/Bootstrap/DailyProductionComposition.cs; Runtime/Replay/DailyReplay.cs; Tests/Diagnostics~/Task054*; Editor/TASK001V7/Task054*; docs/SPEC.md; 本Task; .harness/qa/TASK-054/**。
Forbidden / Shared Paths: 其他产品文件，配置及资产。
Depends On: TASK-053。
Acceptance: 成本与五类边界案例、原权重一致、预留与观察边界、回放、Unity运行及Windows试玩。
Integrator: 主Agent。

## Result
已实现新版场面/暂存分类、屏幕外观察、全部供给食材成本和D归一化，接入原边界与权重；命令记录及回放包含成本观察版本。当前策略跳过不用的旧actionable统计，历史观察仍可回放。观察未确认或场面阻挡目标无法给出可靠距离时保留分类外候选，不误标远距离、不因此永久丢失合法尾单；前瞻仅使用同盘一层证据并检验全部前置食材的已有订单容量，不做深层搜索。

检查：108项成本/分类/20行权重/预留/新版回放检查通过；110789项原保护、失败补偿与回放检查通过；Unity真实启动、观察有效性及可点击目标脚本完成订单后补单，共9项断言通过，新算法标识director_supply_cost_v1已出现在事件中。1080×1920与1080×1440截图已目视检查，未见新增明显画面异常。Windows构建0错误、1条既有警告。相关git diff --check通过。

证据位于.harness/qa/TASK-054/，试玩入口integrated/Player/HotpotSortTask044.exe。尚未真人整局难度验收或真机性能检查，未Commit/Push/上传/发布，未打ZIP。
独立Player启动通过，未见脚本异常；本机音频设备缺失，FMOD无声回退。已核对成品Core DLL包含新成本观察契约。
