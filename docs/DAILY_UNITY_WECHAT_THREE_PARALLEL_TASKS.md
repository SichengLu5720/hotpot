# 开锅啦：恢复证据对照与 Unity／微信三任务并行方案

日期：2026-09-20。当前交付：任务方案及接口草案；未创建正式 Harness Task、工作区或派发实现。

已确认：使用 Unity；用户于 2026-09-20 进一步明确目标为在微信内独立运行的微信小游戏。Unity 具体版本及适配工具链版本尚未确认。

“三个需求”在此理解为同一个 Daily Challenge 产品的三个并行功能任务：核心玩法、场景交互、微信端会话与平台。三份资料不是三个独立游戏需求。

完整模板内容草案（未绑定 Harness 工作区，不可直接派发）：

- [TASK-001：Daily 确定性玩法内核](C:/Users/charlielu/Documents/ChatGPT/下锅喽/docs/task-drafts/TASK-001-daily-core.md)
- [TASK-002：Unity 可玩场景与玩家界面](C:/Users/charlielu/Documents/ChatGPT/下锅喽/docs/task-drafts/TASK-002-unity-playview.md)
- [TASK-003：微信小游戏平台与每日会话](C:/Users/charlielu/Documents/ChatGPT/下锅喽/docs/task-drafts/TASK-003-wechat-session.md)

## 1. 恢复包的核对结果

包：`FishSort_Recovered_Unified_v1.zip`。

本次计算 SHA256 为 `96b0ff5f2f86b12134f85fb52b1ddebb637b18f9db7a46df453f235214302cff`，与用户提供的 SHA256 文本一致。

阅读了总览、CoreReadingGuide、Unrecovered、RecoveryPolicy、GameplayFlow、TargetDirector 及 semantic_coverage；抽查了高度判断和累计权重抽签的原生反汇编。没有重新拆 APK、运行恢复包测试或宣称原生等价验证。

恢复包 `Configs/LevelDifficultyConfig.json` 的摘要为 `40031A2B589537F7C186354B77506D1662179D424B2AA9244DED1EDC1A689BBE`，与 v0.1 原型的权重源文件完全一致。因此是同一份来源数据的可追溯收录，不是新的独立难度证据。

恢复包是阅读工程。Source/Semantic 是独立 .NET 规则模型，Source/Signatures 是签名占位；两者都不能整体复制到 Unity 当作完整游戏。包内声称的编译、40 项断言、1,858 布局检查属于随包历史报告，本次未重跑。

## 2. 原始证据、原型与 Daily 的对应关系

这里的“证据”指包内指定作用域的反汇编或语义映射，不把整个函数乃至整套玩法都标成完全恢复。

| 规则 | 恢复包证据及范围 | v0.1 原型 | Daily 的执行依据 |
|---|---|---|---|
| 空间高度阻止生成 | method_40431，RVA 0x123F664；严格 worldY > limit，已抽查 fcmp/cset gt | 画布坐标向下，用 y < spawnGate；另加防重叠 | 保留空间守卫语义；Unity 坐标不能照搬网页比较方向 |
| 普通顺序供给 | method_40332，普通运行分支有映射；教学/传送带不在恢复范围 | 队首逐盘进入 | 按固定 C 队列，不能重新排序或跳号 |
| 初始两订单 | method_40413 的普通初始化为 Approximation | 两开、四容量 | 按 Daily 的两启用槽执行 |
| 前十盘开局统计 | method_40359 为 Approximation；最大值比较 method_40467/40433 有局部恢复 | 标准 C 得到 C/H | C/H 保留；kindId 升序并列是 Daily 确定性定案，不是已证实的原生枚举顺序 |
| 同种订单优先 | method_40317 普通目标按剩余需求，特殊分支未覆盖 | 已实现 | 复用路由思路；slotId 并列规则按 Daily |
| Progress/TempCount 查表 | method_40180，范围限制和顺序查找为 Recovered | 保留 Difficulty 1/2/3，60 行 | 只保留 Difficulty 3 的 20 行；不恢复局外难度入口 |
| 权重抽签 | method_40176，RVA 0x12462BC；float32 累加、严格小于、最终未命中返回枚举 0，已抽查 | XorShift32、先抽类、空类最小成本 | Daily 使用 PCG32、先过滤空类再归一化；不能为了“还原”改回原生抽样 |
| 五类初筛 | method_40446 的谓词有局部恢复；允许条件重叠 | 多个 bool 可同时命中 | Daily 明确按优先级唯一分类，覆盖 N=0/1 边界 |
| 外层类别回退 | method_40336 有局部恢复，取决于抽中类别及 progress | 空类直接最小成本，未复刻全部外层回退 | Daily 自己定义的压力优先序和六级 fallback 优先 |
| d 成本 | BasicCost 只覆盖 buffer+ordinary 足够三件的路径；完整分层和预留仍缺失 | 截断 foreign 的近似 | Daily 的 -b、ceil(interference/3) 是透明近似，不称原版已恢复 |
| 自动吸收 | method_41007/40983 支持清原槽、装入目标、移动回调与后续检查的局部顺序 | 逻辑先结算，表现随后播放 | Daily 的原子事务、逐索引吸收和动画不掌握库存按产品规则落实 |
| 暂存失败时点 | AttemptTemp / CheckIsFull 仍 Stub | 默认第五格失败，另有 overflow 开关 | 溢出才失败是 Daily 定案；恢复包没有新证据推翻它 |
| 日期、每日映射、PCG32、183 严格胜利 | 恢复包不提供 Daily 产品逻辑 | 日期/每日映射缺失，固定种子重开 | 直接以 Daily SPEC 为来源实现 |
| 隐藏优先覆盖、完整兜底 | IUnrecoveredDirector 多处必需依赖抛占位异常 | 自写近似兜底 | 不接入 Stub，不补猜测的隐藏救场机制 |

另一个不能直接复用的部分是 LevelPreparation 的前段重排、补充池和特殊标记。Daily 要求固定 C 次序、仅全局一一映射，不能因恢复包包含这些方法就将其全部接入。

结论：源数据与局部规则证据可以复用；可玩的基础来自 v0.1；最终业务预期来自 Daily SPEC。三者各有用途，不互相替代。

## 3. 三个可并行功能任务

方案编号 DC-A / DC-B / DC-C，不是已登记的 TASK-ID。

| 编号 | 需求 | 单独交付的能力 | 不承担 |
|---|---|---|---|
| DC-A | Daily 确定性玩法内核 | 给定每日上下文和输入，完成合法、可解释、可回放的单局 | Unity 画面、碰撞、微信 SDK、页面布局 |
| DC-B | Unity 可玩场景与玩家界面 | 展示盘子/订单/暂存，处理触摸、物理供给与入口/结果界面 | Director、库存改写、日期生成、平台配置 |
| DC-C | 微信端启动、会话与平台适配 | 创建每日会话、连接平台生命周期/时间/日志出口、接入最终构建 | 食材路由、订单策略、玩法场景资源制作 |

先形成一个共同基线提交 K0，然后 A/B/C 均只依赖 K0 开始开发，而不是 B 等 A 全部做完、C 再等 B 全部做完。替身用于开发接线，不作为产品验收结果；最后仍需要顺序集成真实模块。

### DC-A：Daily 确定性玩法内核

**目标**：独立于 Unity 场景和微信 SDK，提供一份权威玩法状态。

实现范围：

- 固定 C 内容和 Difficulty 3 权重的正式导入/校验，源哈希与转换记录。
- Daily Seed、PCG32 与 Mapping/Director/Presentation 三流的确定性管理；接收 C 提供的日期上下文，不直接访问系统时间。
- 16 种抽象 kind 与内容目录 ID 的一一映射；资源图像地址由 B 的视图目录负责。
- 唯一库存模型、开局、Progress/TempCount、Director 全部策略、固定暂存格。
- 点击接受/拒绝、预留、路由、结算、补单、吸收、逻辑清盘、严格胜负、Aborted。
- 规范化事件和状态快照、统计、核心回放编解码和旧算法选择入口。
- 供给、暂停/恢复、重试初始化和只读快照接口；核心是唯一状态写入者。

可复用：v0.1 Token/Plate/Order 结构思路、外部库存减需求、优先订单、buffer 空洞、守恒检查和 spawn/tap 回放模型。重写或对齐：随机流、Director、状态机、连锁边界、事件身份。

**QA Intent**：CFG 01–06、DET 01–05、BUF 01–06、DIR 01–10、SUP 03–06；来源 Daily §5–7、§9–18、§19–20、§25。Task 阶段记录这些意图与真实观察接口，版本阶段才固定测试计划和脚本。

交付：实际 Core 实现、正式内容数据、快照/事件/回放接口和已知限制。开发替身不需要原生场景即可输入命令；不以命令行演示声称完成真机验收。

### DC-B：Unity 可玩场景与玩家界面

**目标**：完成用户能看见和操作的游戏，包括玩法场景和所有页面的视觉实现。

实现范围：

- 有来源截图或获准替代基准上的原尺寸高精度预览；主场景、今日入口、暂停、结果和异常状态的必要画面。
- 食材、盘子及必要 UI 的概念、正式源资源、规格和最终绑定；复用资产需明确选择，不能把旧占位美术当成已批准终稿。
- Unity Physics、盘子生成/清除、空间采样与供给守卫、触摸命中、安全区布局。
- 两订单、五格暂存、保留空洞、连锁动画、固定短锁下的视觉同步。
- 页面按钮发出语义命令；日期、结果和重试数据由 C/A 提供。
- 视图状态回放/替身数据源，以合同覆盖显示状态，不依赖 A 尚未完成的源码。

可复用：原型版式参考、盘内固定位置、命中思路、事件展示、素材生产源。原型圆形物理与 Daily 的 Unity Physics 不相同，需要显式适配。旧 HotpotApp 拆分后不再由 B/C 共同修改。

角色：Design-Art 负责预览、必要概念及独立资源生产；Code Builder 负责场景、交互和最终绑定。美术路径和代码/场景/导入元数据路径分开；资源交接后顺序绑定。

**QA Intent**：SUP 01–02、BUF 04/06 的表现与命中保护点、§8–10、§18.3、§20.4；暂停/结果操作来自 §4.1/§7.1。体验、识别度和安全区保留人工证据；不自行添加帧率、包体、点击成功率阈值。

交付：Gameplay 场景、页面 Prefab/视图、正式资源与 Manifest、已绑定的界面接口。无批准参考时，只能推进明确独立的技术调查和合同工作，不能宣称正式视觉生产已经可以开工。

### DC-C：微信端启动、会话与平台适配

**目标**：让既定玩法与界面成为可在确认后的微信目标形态中启动、暂停、重试和诊断的应用。

实现范围：

- 接管 Unity 工程；确认引擎版本与平台适配工具链的兼容组合，固定 SDK/依赖版本；不凭恢复包 .NET 项目版本决定 Unity 版本。
- 平台启动、资源就绪、加载/错误状态、前后台与中断事件转换。
- 获取可信时间或设备回退，生成含 timeSource 的每日上下文；跨日/重试策略按已确认规则执行。
- 会话控制器：创建、暂停、恢复、终局、同 ChallengeId/ContentVersion 重试及旧动画/物理实例销毁协调。
- 实现日志持久化/导出或已配置上报出口；不假设浏览器 Blob 下载在目标端可直接复用。
- 运行目录、缓存和构建输出隔离；构建配置及最终唯一组合入口。
- 在 A/B 交付前，用明确的内核/视图替身验证接线与平台能力；最终切换到固定真实提交。

可复用：原型重开与清理思路、Boot 的入口用途、日志结构的来源。旧 Editor 菜单会混合改设置、旧测试和构建，正式流程需拆开接入。

**QA Intent**：DET 02/04、§4.1、§6、§7.1、§18、§20.3–20.4、§25；微信实际设备上的启动、前后台恢复、触控/安全区与日志能力作为平台适配验证。账号、广告、支付、排行榜、云存档不随平台接入自动增加。

交付：平台适配、SessionController、Bootstrap、固定构建说明与平台限制。开发工具能运行不等于微信手机端已通过；构建产物不等于上传/提审/发布授权。

## 4. 共同基线 K0：并行前只做一次

这是三个任务的共享启动准备，不增加第四个业务需求。

1. 选择性接管 v0.1 Unity 工程、固定 Daily SPEC 和资料摘要；不将恢复包签名、SDK 骨架或全部原始包塞入游戏 Assets。
2. 固定工程目录、Unity/平台兼容约束、抽象食材 ID、内容版本与下面的接口定义。
3. 拆开旧 HotpotApp 的职责；建立 Contracts 程序集、不可变 DTO、开发替身放置规则，以及 B/C 各自独立开发入口。
4. PM 确认实际产品歧义和共享触点后，由 Builder 建立这一最小基线；当前仓库无 HEAD，不能伪造已有提交或依赖 SHA。
5. K0 固定后，用统一入口为 A/B/C 建独立分支/worktree；例如 `codex/daily-core`、`codex/unity-playview`、`codex/wechat-session`。正式状态/派发/接收由 Harness 写入。

Contracts 为共享只读输入。后续变更由 PM 收集影响，单一 Builder 按授权修改并固定新提交，再依 Harness 合同修订规则使相关任务重新确认；不能三条线各自加一份同名接口。

## 5. 接口草案：用它消除先后等待

下列名称为拟定合同，不声称已经实现。

| 接口/消息 | 生产者 → 消费者 | 最小语义 |
|---|---|---|
| ChallengeContext | C → A | ChallengeId、ContentVersion、配置摘要、timeSource、retryIndex；A 不自行读取日期 |
| CreateSession / ResetSession | C → A | 同日重试重建全新逻辑状态和 RNG；返回初始快照/事件 |
| TapCommand / TapResult | B → A → B/C | itemId、inputSeq、逻辑边界、命中事实；A 决定接受/拒绝及库存事务，返回原因与事件 |
| SupplyObservation / SupplyCommit | B → A → B | B 提供空间许可及冷却边界；A 消费队首并返回真实 plateId/items；B 不私自更改队列 |
| PresentationRandom | A 的表现流端口 → B | 表现随机抽取与其他流独立；核心回放如何记录供给许可需冻结，不能把表现消费塞进 Director |
| GameSnapshot / GameEventBatch | A → B/C | 稳定 ID、订单、固定 buffer、进度、统计、终局、eventSeq/transactionId；动画只消费，不修改库存 |
| SessionAction | B → C | 开始今日挑战、暂停、恢复、重试、退出；B 只发意图，C 调用 A |
| PlatformLifecycle / Viewport | C → B及会话 | 前后台、启动/中断、安全区与视口事实；布局由 B 负责，暂停命令由 C 协调 |
| ReplayPackage / DiagnosticSink | A → C | A 负责内容与规范化、C 负责目标平台存储/传输；文件保存不是回放算法 |
| IGameView | C → B | 绑定会话端口、挂载页面、重置/销毁实例、显示错误；唯一组合入口在 C |

供给与回放、短锁时钟、溢出输入统计、跨午夜、Seed 派生字节协议、库存公式仍需在 K0 合同中明确。恢复包没有替产品作出这些决定。方案可完成，但不能把草案接口静默当成已批准业务规则。

## 6. 文件所有权与场景边界

以下为接管后的建议目录，正式任务创建前应落实为精确拥有路径；当前不据此移动源码。

| 所有者 | 独占路径/对象 |
|---|---|
| K0 单写者，之后三方只读 | `Unity/Assets/HotpotSort/Contracts/`、共同标识及接口定义、公共目录 `.meta` |
| A Code Builder | `Runtime/Core/`、`Runtime/Determinism/`、`Runtime/Replay/`、`Content/Daily/`、`Editor/ContentImport/` |
| B Code Builder | `Runtime/Presentation/`、`Runtime/UnityPhysics/`、`UI/`、`Scenes/Gameplay.unity`、`Prefabs/`；B 资源导入/绑定元数据 |
| B Design-Art | `Art/Source/`、`Art/Generated/` 的正式艺术源/导出；不编辑 Unity 场景或最终绑定元数据 |
| C Code Builder | `Runtime/Session/`、`Runtime/Platform/WeChat/`、`Runtime/Bootstrap/`、`Scenes/Boot.unity`、平台插件、构建工具、`Packages/`、`ProjectSettings/` |

- B 独占 Gameplay 场景及所有玩家界面视觉；C 通过固定 IGameView 挂载，不修改其二进制场景或布局。
- C 独占 Boot 与组合入口；A 不注册隐藏全局入口，B 不在 RuntimeInitialize 方法里另起第二个会话。
- A 独占逻辑目录；B 独占视图目录。食材 ID/目录 schema 在 K0 固定，A 映射 ID，B 提供资源地址和碰撞规格，不共同修改一个巨型 JSON。
- 字体、主题、锚点与页面区域归 B；平台视口事实归 C。PM 是跨任务共享触点协调者。
- 各自有独立开发替身/演示场景，替身必须在各自拥有路径内，不进入生产组合；它们不生成假的通过证据。
- 编译依赖仅指向 Contracts 与自己模块，最终组合才连接 A/B/C，避免 B 引用 A 未提交实现。

## 7. 并行时序与集成

```text
资料/产品缺口确认 + K0 公共基线固定
                       │
         ┌─────────────┼─────────────┐
         ▼             ▼             ▼
 A 核心/内容/RNG    B 预览与场景     C 微信/会话/工具链
 用输入记录开发    用快照替身开发   用内核/视图替身开发
         │             │             │
         └─────────────┼─────────────┘
                       ▼
        PM 固定三方提交 → release worktree
                       ▼
        C 的唯一组合入口顺序绑定真实模块
                       ▼
        Designer QA Plan → Builder 脚本 → Runner
                       ▼
        微信实际设备人工项 → 完整版本结论
```

独立开发并行，最后绑定和发布准备串行。三份代码同时完成不等于集成完成，也不能把预览批准当作整个实现授权。

实际运行隔离要绑定到游戏：每个 worktree 的日志、缓存、构建输出和微信开发工具项目目录分别命名；没有真正适配前，不并行运行会写同一数据根的实例。开发可并行编辑，不靠设置环境变量假称已隔离。

## 8. 与旧 P01–P09 的映射和集中 QA

| 原任务 | 新归属 |
|---|---|
| P01 工程基础 | K0 接管 + C 平台工具链 |
| P02 内容数据 | A；资源绑定目录归 B |
| P03 每日与 RNG | C 提供日期上下文；A 拥有 Seed/随机/映射 |
| P04 核心状态 | A；共享 DTO 在 K0 固定 |
| P05 Director | A |
| P06 点击与闭环 | A；命中/动画归 B，平台暂停协调归 C |
| P07 场景与资源 | B |
| P08 用户流程 | B 负责全部页面；C 负责会话控制；A 执行状态变更 |
| P09 诊断回放 | A 负责编码/重放/哈希；C 负责存储/上报出口 |

每条原 QA Intent 在新 Task 中有唯一主责任与协作边界，不因为合并工作包丢失断言。正式交付仍由 PM 汇总 QA_BACKLOG、只读 Designer 固定一份版本计划、Builder 实现脚本、普通 Runner 执行。用户启动版本交付前，不提前执行整套验收。

保留：100 次初始化；365 日期；合法/随机/压力策略各至少 10,000 局；Windows、Android 与目标平台黄金回放；低端和主流设备真人体验。微信内 Android 设备证据和独立 Android 构建不能未经说明互相替代，具体覆盖由固定版本计划明确；不静默删除 SPEC 的既有平台要求。

不在本轮上传、提审、发布、激活体验版或执行新工程实现。当前完成的是可审阅的三任务并行方案；开工时应先解决相关合同缺口和视觉批准，再通过 Harness 建立真实任务。
