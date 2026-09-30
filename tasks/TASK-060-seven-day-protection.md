# Seven-day protection tuning
Task ID: TASK-060
Status: Review

## Current Change
实现SPEC TASK-060的新手与回流七天单一P参数；交付Windows试玩。通关率仅为待校准目标。

## Work Packages
Work Package ID: WP-060-INTEGRATION
Goal: 生命周期接入、P范围与回放兼容、串行集成、必要检查和Windows试玩。
Spec References: SPEC TASK-060；保留TASK-057/058/059其余规则。
Must Preserve: 当前D1、单一P、首十保护、锅位、物理、道具、旧档与回放、用户无关修改。
Allowed Write Paths: Unity/Assets/HotpotSort/Runtime/Core/**；Runtime/Bootstrap/**；必要Runtime/Platform/WeChat/**；Runtime/Replay/**；Editor/TASK001V7/Task060*；Tests/Diagnostics~/Task060Integration*；.harness/qa/TASK-060/**（profile包工作时排除profile目录）；交接后可串行集成PROFILE文件。
Forbidden / Shared Paths: SPEC、Task、所有视觉文件与资产、其他工具及用户配置；不得提交、上传、发布。
Depends On: WP-060-PROFILE合同与实现交接。
Acceptance: 首局退出与首次结果、日期和回流边界、P增减夹取、同步与迁移、冻结和回放；构建启动试玩。不得把参数测试当通关率证明。
Integrator: code_agent / WP-060-INTEGRATION（task060_code，唯一集成实例）。

Work Package ID: WP-060-PROFILE
Goal: 新手/回流周期持久化、幂等结果、旧档迁移和同步兼容。
Spec References: SPEC TASK-060。
Must Preserve: 单一P、现有档案奖励事实及账号隔离、用户dirty修改。
Allowed Write Paths: Unity/Assets/HotpotSort/Contracts/**；Runtime/Platform/Profile/**；cloudfunctions/hotpotProfileSync/**；Tests/Diagnostics~/Task060Profile*；.harness/qa/TASK-060/profile/**。
Forbidden / Shared Paths: Core、Bootstrap、Replay、表现、SPEC与Task；不启动Unity；不部署。
Depends On: 通过PM约定周期接口。
Acceptance: 首局退出/跨日、结果增减、14天回流、重复乱序合并、旧档迁移的本地检查。
Integrator: code_agent / WP-060-INTEGRATION（task060_code）。

## Result
已实现并由code_agent / WP-060-INTEGRATION串行集成。正式开局冻结P；首局退出保留、七天上限、14天回流、结果关联和同步迁移生效。D1/供给/锅位/道具保持。

已执行：Profile32项、云原有17组和新增同步检查、核心集成41项（含旧与本轮实际回放）、道具61项、Unity实际流程77项通过；Windows构建0错误、3项现有警告。独立EXE启动响应，未发现托管异常。相关Diff检查通过。

证据：.harness/qa/TASK-060/integration/delivery-result.json、visual/runtime-result.json、profile/。试玩：.harness/qa/TASK-060/integrated/Player/HotpotSortTask060.exe。

通关率50%/10%尚未实测，待用户试玩校准与接受；当前机器无音频设备，声音未验证。云源码已本地验证但未部署，未微信真机验证；未提交、上传、发布。
