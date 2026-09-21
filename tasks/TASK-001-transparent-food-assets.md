# Task: 火锅消消高精度每日挑战 Unity 可玩版

Task ID: TASK-001
Task Version: 6
Status: Awaiting Human Check
Type: Feature
Risk: High
Build Mode: Code + Art
Audio Scope: 定制品质原创音乐、环境声与交互音效
Technical Gate: Self Check
Art Gate: Human Art Approval
Experience Gate: Human Check
Created By: PM Orchestrator
Updated At: 2026-09-21

# Workflow Control

Current Stage: HC-03 Implementation Result — Awaiting Human Check
Last Accepted Checkpoint: HC-02-v6 Visual / Plan Confirmation
Pending Human Check: HC-03-v6 Implementation Result；Affected Workstreams：v6普通盘供给、出生/物理、裁剪/点击/提示、打乱兼容与自动QA
Next Allowed Action: 用户仅可对HC-03-v6回复Accepted、Needs Revision或Rejected；Accepted后进入HC-04最终实际试玩/体验，不得把自动QA或受控截图直接视为最终体验接受。
Rollback Target: HC-04-v5（仅受影响分支）；v5历史产物保留，不自动回滚文件
Paused Workstreams: Suno音频生产、音频授权证据和依赖最终音频的绑定仍等待可用入口
Unaffected Workstreams: C骨架内容、订单、四锅、五格暂存、十分钟、道具、胜负、每日周期、既有美术与音频决定保留；v5已验证证据仅在不依赖新供给/遮挡规则的范围内继续有效

## Checkpoint Ledger

| Checkpoint | Stage | Task Version | Artifact / Evidence | Status | Human Decision | Rollback Target | Invalidates | Next Allowed Action |
|---|---|---:|---|---|---|---|---|---|
| HC-001 | Preview-only Draft | 1 | 原预览任务；尚未生成预览或正式资产 | Invalidated | 用户将交付扩大为完整 Unity 可玩版 | Draft | v1 预览限定范围与方案 | 使用 v2 |
| HC-002 | Requirements | 2 | 对话中完整交付复述及用户“确认” | Accepted | 2026-09-21 用户确认 | Requirements | None | 建立完整可玩版 Draft |
| HC-003 | Expanded Draft | 2 | 本 Task | Accepted | 用户回复“确认” | HC-002 | None | 并行调查与预览 |
| HC-004 | Design r001 | 2 | 本 Task Design、Designer 返回真实路径调查 | Invalidated | 用户明确取消盘子堆叠，相关拓扑与QA需修订 | HC-003 | 拓扑及派生接口 | 按v3修订 |
| HC-005 | Preview r001 | 2 | .harness/previews/TASK-001/r001/gameplay-preview.png | Needs Revision | 用户要求盘子不能堆叠并确认同平面互不重叠 | HC-003 | 预览候选 | 制作r002 |
| HC-006 | Single-plane correction | 3 | 用户“盘子不能堆叠”及对同平面碰撞、不遮挡、可混装的“对” | Accepted | 本轮用户明确确认 | HC-003 | v2多层堆叠要求 | 修订方案和预览 |
| HC-007 | Design v3 | 3 | 本 Task v3物理方案增量 | Accepted | 2026-09-21 用户“接受” | HC-006 | None | 按已接受单平面方案制作r004 |
| HC-008 | Preview r002 | 3 | .harness/previews/TASK-001/r002/gameplay-preview.png | Needs Revision | 用户认可方向并要求上方改为1/3计数，继续修订 | HC-006 | 旧圆点进度 | 制作r003 |
| HC-009 | Numeric progress feedback | 3 | 用户要求1/3计数，随后“好的 继续” | Accepted | 保留食材图标，以0/3至3/3替代圆点 | HC-006 | r002圆点表现 | 核验原Visual后迭代 |
| HC-010 | Preview r003 | 3 | .harness/previews/TASK-001/r003/（身份误阻断前产出） | Needs Revision | 身份元数据缺失不再构成阻断；但画面仍有斜俯视、两列过整齐、非严格9:16等视觉偏差 | HC-009 | r003预览候选 | HC-007接受后生成r004 |
| HC-011 | Preview r004 | 3 | .harness/previews/TASK-001/r004/gameplay-preview.png；qa-mobile-360.png；prompt.txt | Accepted | 2026-09-21 用户“接受” | HC-006 | None | 补齐冻结候选的接口、资产合同与QA Intent |
| HC-012 | Freeze-readiness clarifications | 3 | 用户对Design Handoff r002三项问题的明确决定 | Accepted | 保留已逆向确认的权重语义；分享卡仅主题图；授权外部原创音频制作 | HC-006 | None | 恢复feature_designer形成Freeze Candidate |
| HC-01 | Requirement Freeze | 3 | HC-002、HC-006、HC-009、HC-012及Frozen Requirement | Accepted | 汇总既有需求确认；迁移至四里程碑流程 | Draft | 旧需求含义被后续Task Version替代时失效 | 形成HC-02候选 |
| HC-02 | Visual / Plan Confirmation | 3 | HC-011批准预览；Design Handoff r003；Implementation Plan、Code–Art Interface、Asset Contract、QA Intent与回归计划 | Needs Revision | 仅音频合同部分需吸收v4权利模型；其余候选内容保留 | HC-01 | 音频合同、音频生产、音频绑定与相关验收 | 形成HC-02-r2；无关调查和预检继续 |
| HC-01-r2 | Audio Requirement Addendum | 4 | v4音频许可修订：Suno免费版，仅限非商业游戏用途 | Accepted | 2026-09-21 用户“接受” | HC-01 (v3) | None | 形成HC-02-r2候选 |
| HC-02-r2 | Visual / Plan Confirmation | 4 | HC-02既有视觉/代码/资产合同，加上CL-017/CL-018的Suno免费版非商业音频合同 | Accepted | 2026-09-21 用户“接受” | HC-01-r2 | None | 恢复v4代码/视觉资产实施；音频等待可用Suno入口 |

`HC-001` 至 `HC-012` 为迁移前的历史细粒度记录，继续保留作过程证据；自本次同步起只新增 HC-01 至 HC-04 四类里程碑及其修订。

### v5检查点增量

| Checkpoint | Stage | Task Version | Artifact / Evidence | Status | Human Decision | Rollback Target | Invalidates | Next Allowed Action |
|---|---|---:|---|---|---|---|---|---|
| HC-01-v5 | Requirement Freeze | 5 | CL-020、CL-022；用户“实施”及对最后供给定义的“确定” | Accepted | 固定C内容、日期映射、普通队首供给；沿用每0.2秒、完整界内且不重叠的空间门槛，其余玩法保留 | HC-01-r2受影响部分 | 旧原创盘内容及依赖它的方案/验证结论 | Designer只读方案核对，随后按已有实施授权衔接Builder |
| HC-02-v5 | Implementation Scope | 5 | CL-020/CL-022；Design Handoff v5（无新增产品决定、无视觉/资产变更） | Accepted | 依据用户已明确的“实施”及最终“确定”推进同范围技术实施；不将Designer意见冒充额外用户审批 | HC-02-r2受影响部分 | 旧原创生成入口及相关QA预期 | Builder实施与验证，提交HC-03候选 |
| HC-03-v5 | Implementation Result | 5 | `.harness/qa/TASK-001/v5/r001/implementation-result.json`、`post-rules.json`、`post-r2-supply.json`、`post-r2-integration.log`；Designer只读覆盖核对 | Accepted | 用户“确认”（2026-09-21） | HC-02-v5 | 当前实现/脚本/内容若再变化，相关证据须重查 | 进入HC-04固定C实际试玩；WebGL限制单独披露，不发布 |
| HC-04-v5 | Final Experience | 5 | Unity `Boot.unity`实际试玩；用户体验观察 | Accepted | 用户“确认”（2026-09-21） | HC-03-v5 | 当前体验、设备或视觉反馈若要求变更，失效受影响验证 | 固定C与普通供给替换分支Verified；完整Player/WebGL、设备与发布仍另计 |

### v6检查点增量

| Checkpoint | Stage | Task Version | Artifact / Evidence | Status | Human Decision | Rollback Target | Invalidates | Next Allowed Action |
|---|---|---:|---|---|---|---|---|---|
| HC-01-v6 | Requirement Freeze | 6 | CL-023；2026-09-21 完整需求复述及用户“确认 实行” | Invalidated | 用户于HC-01展示后回复“确认”；随后按只读代码调查选择推荐的当前坐标换算语义 | HC-04-v5受影响分支 | 其中`Y>377`字面判断、当前坐标字面`(0,-5)`及未明确顶部物理范围的部分；其余CL-023决定迁入r1 | 使用HC-01-v6-r1 |
| HC-01-v6-r1 | Requirement Freeze Revision | 6 | CL-023未受影响部分＋CL-024坐标/运动/顶部边界修订；spawnGate保持固定377，不绑定可见上边缘 | Accepted | 用户在解释盘中心并讨论动态边界后明确选择“算了 固定的377吧”，随后回复“接受”（2026-09-21） | HC-04-v5受影响分支 | HC-01-v6受影响坐标表达及其下游方案 | 恢复原feature_designer，汇总HC-02候选 |
| HC-02-v6 | Visual / Plan Confirmation | 6 | Design Handoff v6；`.harness/previews/TASK-001/r005/v6-layering/`静态目标状态预览；Implementation Plan、QA Intent、Code–Art Interface与回归计划 | Accepted | 用户在查看遮挡/露出预览与方案摘要后回复“接受”（2026-09-21） | HC-01-v6-r1 | 后续需求或方案含义变化时仅失效受影响实现/测试 | 进入Ready to Build；code_builder实施Code Only增量 |
| HC-03-v6 | Implementation Result | 6 | `.harness/qa/TASK-001/v6/r002/implementation-result.json`、`handoff.md`、run05完整日志/报告/截图；`.harness/previews/TASK-001/r006/`集成视觉QA | Pending | 等待用户回复Accepted / Needs Revision / Rejected | HC-02-v6 | 当前代码/诊断/规范若再变化，相关run05与视觉证据须重查 | Accepted后进入HC-04最终实际试玩与体验判断 |

## Agent Session Registry

后台追踪用途；缺失、过期或客户端昵称变化不构成推进门禁。

v5会话：`/root/feature_designer_c_v5`，Compatibility Prompt，已返回Design Ready；`/root/code_builder_c_v5`同配置已返回Implementation Result Ready，证据`.harness/qa/TASK-001/v5/r001/implementation-result.json`。均使用仓库目标gpt-6-astra/high。旧会话不属于当前父会话代理树，无法直接恢复，故建立本次实例。宿主内置同名角色属于另一套策略且锁定low，本次显式按仓库TOML兼容派发。

v6会话：`/root/feature_designer_v6`，Compatibility Prompt，gpt-6-astra/low，只读；先因坐标语义返回Needs Clarification，HC-01-v6-r1接受后恢复同线程并返回Design Ready。`/root/visual_design_agent_v6`，Compatibility Prompt，gpt-6-astra/low，仅写`.harness/previews/TASK-001/r005/v6-layering/`，返回Preview Candidate Ready。两者未直接通信；PM分别派发并合并结果。

v6实施会话：`/root/code_builder_v6`，Compatibility Prompt，gpt-6-astra/low，唯一代码写入者；已返回Implementation Ready for Pre-delivery QA Scope。基础证据：`.harness/qa/TASK-001/v6/r001/implementation-preqa.md`；实际改动7个源/规范文件，未修改核心队列、内容、场景或资产。

同一`/root/code_builder_v6`按最终Script Test Scope纠正CL-025并完成run05全量重跑，返回HC-03 Candidate Ready。原`/root/visual_design_agent_v6`随后只读核对真实截图，并仅写`.harness/previews/TASK-001/r006/`，返回Visual QA Evidence Ready。

| Exact Agent Name | Config Source | Thread ID | Identity Verification | Scope |
|---|---|---|---|---|
| feature_designer | .codex/agents/feature-designer.toml | 01a0c362-1ebd-7552-a769-f92dcfafdb3d | 先前按精确类型启动；新版身份声明待下次恢复核验 | 已停止，只读设计 |
| visual_design_agent | .codex/agents/visual-design-agent.toml | 01a0c362-1f8d-75e3-9d55-5dd0c8ea796c | 历史线程；恢复返回agent not found，无法继续 | r001/r002历史 |
| visual_design_agent | .codex/agents/visual-design-agent.toml | 01a0c382-6991-70d3-8726-718ca203188b | Compatibility Prompt；角色声明匹配；客户端不提供配置来源元数据，按兼容规则不阻断 | r003需视觉修订；HC-007接受后可继续r004 |
| visual_design_agent | /root/visual_design_agent_r004 | Compatibility Prompt；角色声明、TOML与models.toml（gpt-6-astra/high）匹配 | r004预览；替换原因：此前会话使用旧low配置快照，不能按当前模型目标恢复 |
| feature_designer | /root/feature_designer_freeze | Compatibility Prompt；角色声明、TOML与models.toml（gpt-6-astra/high）匹配 | Freeze-readiness只读调查；返回Needs Clarification，待恢复 |

当前调度规则：`visual_design_agent` 是同一端到端视觉与表现责任人，贯穿预览、正式资产、表现层实施和最终视觉QA；`code_builder` 负责核心逻辑、稳定表现接口与不改变视觉决定的技术验证。共享文件按 Code → Visual → 技术验证串行交接，不得用通用Worker替代或覆盖模型设置。下文旧 Revision 中对独立 Art Builder 的记载仅保留为历史证据，当前及后续执行以本规则和 AGENTS.md 为准。

HC-02 接受后才生产正式拆分资产并集成。方案草稿、预览修订、代码、资产批次、音频和测试结果作为过程证据持续记录，并在 HC-03 合并核查；不将需求确认等同于尚未产出的视觉或音频验收。局部修改只暂停 `Paused Workstreams` 及其直接依赖，无关进程继续，到共享集成点再等待。
回退保留历史 Revision，标记派生输出失效；禁止破坏性 Git 回退和覆盖无关修改。

# Clarifications and Decisions

## Requirement Interview Summary

- User-confirmed problem: 用户要求高精度、完全实现已确认要求的 Unity 可玩游戏。
- Entry: 启动页进入当日挑战。
- Result: 可完成开始、游玩、暂停、道具、失败、重试、胜利、首通记录与设置的完整 Unity 流程。
- Scope: 仅最高难度每日挑战；完整美术、动画、音频；微信能力仅接口。
- Acceptance: 自动验证、Unity 实际试玩、人审视觉与定制音频品质、难度和爽感。
- User confirmed complete understanding: Yes。

| ID | Topic | Confirmed Decision |
|---|---|---|
| CL-001 | 交付 | Unity 可玩；只有每日挑战，无普通关卡。 |
| CL-002 | 骨架 | 50 盘、183 食材单位、16 种，每种数量为 3 的倍数；复现 Fish Sort 高难骨架规律，使用本项目日期种子与食材映射，不直接复制恢复包原始配置文件。 |
| CL-003 | 清空暂存 | 暂存食材组成一个可混合的新盘，追加生成队列末尾；立即腾空五小碟，食材不销毁。 |
| CL-004 | 其他道具 | 提示高亮当前可点击且匹配订单的食材，不代点；打乱仅改变当前盘子位置，不改盘内内容和队列。 |
| CL-005 | 激励 / 分享 | 开发模拟激励；三个道具共用每天三次分享替代额度；提前第四锅仅激励。 |
| CL-006 | 每日周期 | 北京时间 06:00 更新，可无限重试；每天首通计入累计，退出不保存残局。 |
| CL-007 | 难度 | 允许依赖道具解围，不要求无道具求解验证通过。 |
| CL-008 | 平台 | 云开发存档、分享、开放数据域好友榜只做接口，不接真实数据；Unity 使用开发模拟状态。 |
| CL-009 | 美术 | 全部重做；先确认整屏预览，再拆分正式资产并集成。 |
| CL-010 | 音频 | 必须达到定制品质；采用前述传统火锅店原创配乐、环境声与完整操作音效方向，并人工试听。 |
| CL-011 | 工程 | 允许调整占位工程、场景和资源绑定；允许自动检查、Unity 试玩和用户人工验收。 |
| CL-012 | 跨日局 | 用户确认：当前局跨06:00继续归属开局日，下一次开始/重试进入新日；分享按发起领取时的挑战日历日期归属。 |
| CL-013 | 高难权重 | 允许保留已逆向确认的高难权重与难度阶段语义；盘面内容、日期种子和食材映射必须为本项目原创，不复制恢复包原始配置文件。 |
| CL-014 | 分享卡 | 仅静态主题图；不显示日期、用时、首通、累计或其他动态结果字段。 |
| CL-015 | 定制音频 | 用户授权采用外部原创制作。实际供应方、授权/交付证据、源工程与混音成品须在音频生产前登记，不得以本仓库正弦提示音替代。 |
| CL-016 | 无效道具 | 用户确认：无有效目标不允许领取；取消/失败不发奖、不扣额度；成功且实际生效才扣分享次数。 |
| CL-017 | AI音频权利 | 用户选择Suno免费版。游戏及其音频仅限非商业、非盈利使用；不得上架收费、接广告、商业宣传、出售、授权或用于客户项目。每个最终文件须保留生成平台、免费套餐、生成日期、提示词/版本、导出文件与当时许可证据；不以独占、商用或权利转让对外宣称。 |
| CL-018 | AI音频平台 | 使用Suno免费版：BGM与环境/操作音效均采用该平台当前免费能力。若未来发生任何商业化或平台条款不允许该类使用，必须停止发布/商业使用并重制替换全部该批次音频；不得静默沿用。 |
| CL-019 | 集成画面比例 | 用户选择仅放大运行时食材视觉尺寸，以提升小屏辨识；不调整盘群位置、供给规则、物理范围、碰撞边界或可见盘数。 |
| CL-020 | 当前骨架与复刻范围 | 2026-09-21 用户先回复“确定 这里有三种骨架 总结共性”，再明确“请你记录这些规律，但现在的流程完全复刻骨架C”。当前每天固定使用提供的C内容结构，逐盘保留数量、同类关系和配置顺序，使用本项目挑战日期种子做全局一一食材映射、同日重试复现；全部按普通盘处理，不引入特殊机制或额外交换/洗乱盘序。A/B仅作规律参考，不轮换、不混合、不按共性另造C。此决定取代CL-002/CL-013中要求原创盘面内容的部分；仅替换盘面预处理与普通盘队列供给链。 |
| CL-021 | 记录与实施边界 | 本轮明确授权记录规律与当前方向；四锅、五小碟、十分钟、清空暂存、打乱、音频及其余玩法规则保留。高度/拥挤门槛及其在现有同平面盘子中的具体含义尚待逐轮澄清，未授权本次代码、资产或测试修改。 |
| CL-022 | 供给定案与实施授权 | 用户随后明确“实施”，并对PM最后的供给建议回复“确定”：复刻C全部盘面内容、项目日期映射和逐盘队列；保留现有同平面盘子，沿用每0.2秒检查队首入场位置，完整放得下且不与现有盘重叠才生成，否则等待且不跳号；不新增源游戏高度线或固定可见盘数。此次确认解除CL-021的待确认/未授权限制，授权本范围实施及必要验证。四锅、五格、十分钟、清空暂存追加队尾、打乱仅位置、音频与其他玩法不变。 |
| CL-023 | v6普通盘供给与前景遮挡 | 用户确认并要求实行：当前每日挑战中的普通盘供给分支以食材盘映射Bubble、盘内食材映射鱼；运行且队列非空时每0.2秒最多处理一个队首盘；基础横坐标为150/260，按已生成数量模2轮换，并加入−0.5至+0.5世界单位水平随机偏移；纵坐标及盘尺寸沿用当前参数。供给前仅检查所有在场盘根对象中心Y，任意Y>spawnGate(377)则本次不生成且队列不动，否则生成队首；不再检查生成位完整界内或无重叠。新盘创建后一次性调用AddForce((0,-5), ForceMode2D.Force)。盘清空后立即移出场上列表；暂停、终局和退出不供给，继续后恢复检查，重开重置队列、计数和轮换。显示采用暂存栏下沿硬裁剪加前景UI覆盖；盘、食材、边框、高光和选中效果统一裁剪；遮挡区域不穿透点击，露出部分仍可点，盘在遮挡后继续物理运动。只修改当前每日挑战使用的普通盘路径，不新增普通关卡模式，不处理源游戏第二关教学暂停或传送带特殊分支。此决定取代CL-022的旧空间门槛和三生成位相关含义，其余玩法保持。 |

### Open Clarification v6-C1

已解决。只读代码调查确认：当前盘面Y向屏幕下方增大；物理`(0,-5)`会使盘向屏幕上方移动，现有每物理帧持续正Y力则使盘向屏幕下方移动；盘中心当前被顶部墙与几何纠正限制在`304+radius`以下，无法进入暂存栏下沿292之后。用户对PM提出的三个推荐处理回复“推荐”，形成CL-024。

| ID | Topic | Confirmed Decision |
|---|---|---|
| CL-024 | 当前坐标中的原版语义换算 | 高度门槛按“画面上方拥挤暂停供给”的玩家可见语义实现：当前盘面坐标中任意在场盘中心`Y < 377`时阻塞，等于377不阻塞；不再字面使用`Y > 377`。原版一次性`(0,-5)`按屏幕运动方向换算为当前物理坐标中的等效向下初始力，量级保持5，即当前坐标调用`AddForce((0,+5), ForceMode2D.Force)`；保留现有持续向画面下方的物理力。顶部物理墙与几何纠正上移，允许盘部分或完全进入暂存栏后方、继续碰撞并可能再次露出；精确隐藏物理边界由Designer依据现有暂存栏228…292布局提出，但不得阻止已确认的完整隐藏/再露出结果。画面仍以暂存栏下沿硬裁剪，订单区不得漏图或接收盘面点击。 |
| CL-025 | 打乱范围兼容性纠偏 | Pre-delivery只读核对发现实现把打乱目标从既有304…828扩大到140…828。HC-01/HC-02均要求其他道具规则保持，且Design Handoff原始兼容性结论明确保留304…828；因此PM判定为实现偏差而非新产品选择。打乱目标恢复304+radius…828−radius，镜像/回退结果也必须逐项满足同范围；无合法目标时沿用不生效、不扣额度。仅自然物理运动可把盘带入隐藏区。 |

既有需求保留；v6仅按CL-023替换普通盘供给门槛、生成位置、入场力以及暂存栏遮挡/点击边界。v5旧检查结果不能作为v6受影响分支已实现或已验收的依据。

## A/B/C骨架共性记录（2026-09-21，用户要求保存）

来源目录：`C:/Users/charlielu/Documents/Codex/2026-09-20/referenced-chatgpt-conversation-this-is-an-2/outputs/FishSort_Recovered_Unified_v1.1/Configs/NormalizedSkeletons/`。以下为完整读取三个JSON并按bubbles条目重算的观察，不是新增生成规则、难度分档或未来启用A/B的授权。这里将源气泡称为盘。

| 指标 | skeleton_A.json | skeleton_B.json | skeleton_C.json |
|---|---:|---:|---:|
| 盘数 / 食材单位 / 类别 | 33 / 123 / 15 | 44 / 162 / 15 | 50 / 183 / 16 |
| 按同类三件算术配组 | 41 | 54 | 61 |
| 混装盘数及比例 | 31 / 93.9% | 43 / 97.7% | 48 / 96.0% |
| 每盘平均食材数 | 3.727 | 3.682 | 3.660 |
| 装1/2/3/4/5件的盘数 | 0/5/9/9/10 | 1/6/12/12/13 | 1/7/14/14/14 |
| 含至少三件同类的盘数 | 4 | 1 | 1 |
| 前十个配置项覆盖类别数 | 11 | 10 | 11 |
| 最多一类的总数 / 最少一类的总数 | 24 / 3 | 24 / 3 | 27 / 3 |

共同规律：

1. 每类全局数量都是3的倍数；并不要求每盘数量为3的倍数，也不要求每盘独立凑齐。
2. 3、4、5件盘占主体，三种盘数近乎均衡；少量两件盘穿插，单件盘不是三者必备项。
3. 绝大多数盘混装；每一类都分布在至少两盘。同类单件、两件及三件同盘均允许，不可总结成禁止三件同类。
4. 类别数量明显不均，既有高频类也有稀少类，不能改成各类平均分配。
5. 按配置顺序观察，类别分批引入、交错延续；前面出现的部分种类持续到尾部，不是清完一种再切换另一种。B的最后三类分别于第38、40、42项首次出现。
6. 骨架保存每盘装载量、同类关系与跨盘分布；全局一一映射可改变食材身份而保留组合结构。三个文件中的字母独立定义，不直接跨文件对应同一类原始鱼种。

证据边界：三个文件均为运行时预处理之前的content-only提取，不含特殊机制标记、目标、时间或坐标，且明确未断言配置顺序就是原游戏最终出场顺序。项目采用C配置顺序是本次产品决定；不能据此宣称原游戏完整等价、必可解，或A/B/C分别代表低/中/高难。空间门槛应另行明确，不能从数量表推导。

源文件SHA-256（完整归一化JSON，与JSON内原始来源entry_sha256不同）：

- A：`4382e7879bceaf4d38e9a20e1288a6ceb3d40f64aec576782f14ad5f11917d0f`
- B：`ad746b01426a9bdf9e99e1d9a60f70b60fb34dbd410b93f186a255a1f402c039`
- C：`2d0cc7dc3061937efd09832b8635558d6b861bbdcbbc793a92d70f25c554ef54`

当前采用：只完整复刻C的内容结构及已确认的预处理/普通队列规则；不从共性重新随机造盘。普通生成每次仅取当前队首、创建后出队，不一次性生成全部，也不保证消掉一盘立即补一盘。CL-023取代CL-022的空间门槛：每0.2秒仅以场上盘根对象中心Y与spawnGate(377)判断是否供给，使用150/260双生成位和原版水平随机偏移，不再要求生成位完整界内且不重叠。清空暂存追加混合盘与打乱仅调整当前盘位置的既有规则保留。

<!-- FROZEN_START -->

# Frozen Requirement

## Goal and Player Outcome

交付高精度的 Unity 每日挑战可玩版。玩家在传统火锅桌上点击整理食材，观察当前可用食材与订单，体验下锅、连续完成订单及清理盘面的爽感。完成全部已确认功能，并以实际游戏画面与声音验收。

## Core Rules and Confirmed Decisions

- 正俯视、竖屏、点击操作，无拖拽；下方白瓷圆盘在同一物理平面自由碰撞，不设计上下堆叠层；盘内允许混合食材，空盘收走。前景暂存栏可按CL-023遮挡盘面内容；v6不再以“生成位无重叠”作为供给门槛，接触与可能的初始交叠由物理系统处理。
- 50 个初始盘、183 个食材单位、16 种食材，各种总数为三的倍数，共 61 个订单；清空暂存新增的盘不增加总食材和订单数。
- 参考 Fish Sort 生成队列、有限可见窗口和高难度节奏；恢复工程是逆向语义材料，不能当成原版源码或完整等价证明。
- 可保留已逆向确认的高难权重和难度阶段语义；依据CL-020，当前完整采用提供的骨架C内容结构（50盘、183单位、16类），保留逐盘数量、同类关系和配置顺序，使用本项目挑战日期种子及一一食材映射。全部为普通盘，不增加特殊机制或额外盘序交换，不使用A/B、不依据共性另造内容。
- 依据CL-023及CL-024，运行且队列非空时每0.2秒尝试一次，每次最多生成当前队首一个盘；暂停、胜利、失败或退出不供给，继续后恢复检查。基础横坐标为150/260，按已生成数量模2轮换，并加入−0.5至+0.5世界单位水平随机偏移；纵坐标与盘尺寸沿用当前参数。供给前只检查所有在场盘根对象中心Y；按当前向下增大的盘面坐标，任意Y<spawnGate(377)表示画面上方拥挤，队列不动且本次结束；等于377不阻塞，不存在阻挡盘时才生成队首。不比较盘边缘，不检查生成位完整界内或无重叠，不设置固定可见盘数。盘清空后立即从场上列表移除；重开重置队列、已生成数量和生成位轮换。
- 原版新盘一次性`AddForce((0,-5), ForceMode2D.Force)`按屏幕方向换算为当前坐标中的`AddForce(new Vector2(0, 5), ForceMode2D.Force)`，保留现有持续向画面下方的物理力；不得用速度赋值或Impulse静默替代。顶部物理墙与几何纠正须上移，使盘可部分或完全进入暂存栏后方、继续碰撞并可能再次露出；精确边界在HC-02方案中冻结，但不得阻止该玩家结果。
- 玩法内容受暂存栏下沿硬裁剪，同时暂存小碟、计数与按钮作为前景覆盖；食材盘、盘内食材、边框、高光和选中效果统一受裁剪。UI优先接收点击，被遮挡区域不得穿透点击后方食材；露出的有效像素仍可点击。盘进入遮挡区域后继续物理运动，不因遮挡销毁；不同屏幕比例下边界跟随现有暂存栏布局且不漏图。
- 每日种子与食材映射可复现；北京时间 06:00 换日；同日无限重试，退出后重新开始，不保存残局。
- 初始两口订单锅。三个同类食材完成订单，整锅端走并补单；暂存食材遇到匹配订单自动入锅。
- 进度按完成订单数 / 61 计算；达到 50%（31 单）开第三锅，80%（49 单）开第四锅。第四锅可提前通过模拟激励解锁。
- 未开锅位显示未开火铜锅；开锅点火、蒸汽、订单出现。每口锅少量食材、红汤留白，订单图标和进度清楚。
- 暂存固定五格。已满时再点击不匹配食材直接失败；已满仍可点击匹配订单的食材。
- 倒计时十分钟；超时直接失败，无续时。手动暂停、后台和激励期间暂停计时。
- 提示只高亮一个可点击且匹配当前订单的食材。
- 清空暂存把全部暂存食材移到队尾的新混合盘，再按正常生成和点击规则处理。
- 打乱只重新排列当前剩余盘子的位置，保留盘内内容、生成队列及剩余总量。
- 三道具通过模拟激励或分享替代获得；每天分享总额三次，共用额度。第四锅提前解锁仅激励。
- 可依赖道具解围；不保证无道具可解。
- 全部食材、订单、暂存结清才胜利；每天首次胜利增加累计通关次数，同日重复胜利不增加。
- 云开发、分享、开放数据域好友榜仅接口；不接真实云数据或生产服务。Unity 开发模拟明确区分于真实服务。
- 最终游戏实现必要的中文操作与状态反馈；“无具体文案”只适用于首次美术概念预览。
- 订单牌保留目标食材图标，进度改为0/3、1/3、2/3、3/3数字计数，替代三个圆点；r003也显示数字。此为既有收集规则的表现澄清，Task Version仍为3。
- 顶部倒计时改用阿拉伯数字分:秒（MM:SS，10:00开始），替代指针时钟图标；锅上订单收集计数仍为n/3，两者同时保留。用户本轮明确要求，已通知Visual。

## Art and Audio

- 16 种食材：肥牛卷、羊肉卷、午餐肉、毛肚、虾、鱼片、鱿鱼、蟹柳、鱼丸、豆腐、鸭血、鹌鹑蛋、玉米段、土豆片、金针菇、青菜。
- 正俯视 2D/2.5D，暖色深木桌、传统红汤铜锅、白瓷盘与五小碟、细深棕描边，偏写实简化纹理、有食欲。
- 首版无人物、灯笼、招牌、窗景等桌外布景。独立食材不烘焙整体投影。
- 全部重做食材、锅开启/未开火状态、器皿、桌面、按钮、弹窗、进度、计时、订单标记、启动/结算/好友榜界面及分享图。
- 分享图仅为静态主题图，不显示日期、用时、首通、累计或其他动态结果字段。
- 下锅水花、红油波纹、蒸汽、完成金光、飞行与端锅动画保持清晰且不遮挡操作。
- 参考图片：C:/Users/charlielu/Documents/WXWork/1688856730932575/Cache/Image/2026-09/92003150-c5e4-4538-80d1-acc34cafb5f6.jpg。仅参考食材与器皿质感，不复制其素材或界面。
- 首张整屏预览采用 9:16、无具体文案；用户接受后才制作正式拆分资产。
- 音乐：轻快传统火锅店，民乐打击与拨弦结合现代休闲节奏，90–105 BPM、无人声、60–90 秒无缝循环。
- 环境：低音量沸腾和远景店内氛围。交互：陶瓷轻碰、弹起/飞行、下锅扑通/滋响、上行完成音和端锅、胜负与按钮反馈；无语音播报。
- 音乐、音效独立开关及音量。定制品质需要可试听成品和用户认可，不能以简单合成提示音或文件格式检查代替。
- 定制音频采用Suno免费版生成，游戏及其音频仅限非商业、非盈利使用；不得上架收费、接广告、商业宣传、出售、授权或用于客户项目。BGM、环境和操作音效均使用该平台当前免费能力；每个最终文件须登记平台、免费套餐、生成日期、提示词/版本、导出文件与许可截图/条款快照。未来发生商业化或条款不允许时，必须停止使用并重制替换全部该批次音频；不得宣称独占、商用或权利转让，且不能以运行时正弦提示音降级交付。

## Scope

Unity 完整每日挑战、骨架C内容与供给/订单调度、道具、每日首通本地开发状态、平台接口、正式美术、动画特效、音频、适配与验证。v6本次变更仅涉及当前每日挑战的普通盘供给链、双生成位、入场力、暂存栏遮挡/裁剪与点击边界，其余范围保留。

## Non-goals

新增普通关卡模式、源游戏第二关教学暂停、传送带特殊分支、全服榜、真实云部署/数据、正式微信广告接入、微信导出或真机验收、金币/体力/养成、助力/组队/纪念品、外部发布。

## Constraints

- Unity 6000.0.26f1；竖屏参考 720×1280 至 1440×3200，处理刘海及安全区。
- 性能目标稳定 30 FPS、争取 60 FPS；本轮只能报告 Unity 实测，不能声称完成手机真机性能验证。
- 保留无关已有改动；资源替换通过明确绑定和版本化生产目录，不清理用户历史文件。
- 预览保存 .harness/previews/TASK-001/rNNN/，不得覆盖旧版本。
- Code + Art 文件写入范围与接口必须在实施前明确；代码集成只有一个写入者。

## Acceptance Criteria

| ID | Type | Criterion | Required Evidence |
|---|---|---|---|
| AC-F-01 | Functional | 初始 50 盘/183 单位/16 种、61 单；队列与清空暂存全过程数量守恒 | 数据检查及规则用例 |
| AC-F-02 | Functional | 盘子保持同一物理平面并正确碰撞；有效区域点击、匹配、暂存回填、31/49 单解锁及终局准确 | 定向检查及 Unity 试玩 |
| AC-F-03 | Functional | 三道具按定义执行，分享共用三次，激励结果不重复发奖 | 边界用例 |
| AC-F-04 | Functional | 06:00 换日、同日复现、首通幂等、退出重开 | 时间边界与重复回调用例 |
| AC-F-05 | Functional | 平台接口可注入模拟实现，未接入真实数据时反馈真实状态 | 接口与 Unity 流程检查 |
| AC-F-06 | Functional | 每0.2秒最多供给一个队首盘；150/260双位置轮换并带−0.5至+0.5世界单位水平随机偏移；当前盘面坐标中任意在场盘中心Y<377时不出队、Y=377不阻塞，条件恢复后继续同一队首；不使用旧完整界内/无重叠门槛 | 确定性规则用例、供给事件与运行日志 |
| AC-F-07 | Functional | 新盘按当前坐标仅施加一次(0,+5) Force并保留持续向画面下方的物理力；顶部边界允许盘完整隐藏和再露出；盘清空即退出高度检查；暂停/终局不供给，继续恢复，重开重置队列与轮换 | 物理定向检查、生命周期用例及Unity运行证据 |
| AC-F-08 | Functional | 暂存栏下沿硬裁剪与前景覆盖同时生效；盘及其内容/边框/高光/选中效果不漏图；UI和遮挡区不穿透，露出有效像素可点击 | 多比例截图、输入边界脚本检查及Unity人工点击 |
| AC-T-01 | Technical | Unity 编译成功，主要状态和资源绑定完整，无阻断运行错误 | 编译、日志和运行证据 |
| AC-T-02 | Technical | 竖屏与安全区适配、性能及音频循环检查 | Unity 各比例截图及采样 |
| AC-E-01 | Experiential | 整屏预览与正式整合画面达到高精度、统一且有食欲 | 用户预览与集成验收 |
| AC-E-02 | Experiential | 定制品质音乐与音效、混音舒适、循环自然 | 音频试听及游戏内人审 |
| AC-E-03 | Experiential | 高难挑战与整理爽感兼具，飞行/碰撞/端锅连贯 | 用户完整试玩 |
| AC-E-04 | Experiential | 盘靠近暂存栏时遮挡自然，半露内容清楚可点，供给和堆积节奏符合目标表现 | 用户对比试玩与最终体验确认 |

Test Proxies: 计数、帧率、文件属性、编译成功不能替代审美、音频品质或爽感验收。

<!-- FROZEN_END -->

# Design

## Design Handoff v6 — Height Gate / Occlusion（HC-02-v6 Candidate）

Status: Design Ready；无新增产品决定。v6增量Build Mode为 **Code Only**，复用现有正式图片；Visual继续负责集成后视觉QA，本增量不需要新的正式资产或表现实施。整体Task历史Code + Art范围及音频暂停状态保留。

### Current Entry and Frozen Coordinate Contract

真实入口为`SessionController`创建当日会话，`DailyProductionComposition.Update`按活动时间每0.2秒观察一次，当前经`GameplayView.ObserveSupply`与`PlatePresentationWorld.SpaceAvailable`进入`DailySession.Supply`，核心每次只提交一个队首盘。队列、成功生成计数、暂停/终局保护、清空暂存追加队尾和空盘立即移除已经存在，应保留。

| Parameter | v6 Contract |
|---|---|
| Board / physics | 盘面420×900，Y向屏幕下方增大；世界坐标=盘面坐标×0.01 |
| Spawn X | 成功生成前计数n为偶数取150，奇数取260 |
| Horizontal offset | 世界−0.5…+0.5，即盘面−50…+50；只在成功生成时消耗一次表现随机值并缓存 |
| Spawn Y / radius | 保留`304+radius`；半径保留`min(63,33+count*6)` |
| Supply gate | 任意有效在场盘根中心盘面Y<固定377阻塞；Y=377允许；残影不参与 |
| Entry force | 新刚体创建时一次`AddForce((0,+5), ForceMode2D.Force)`；不乘0.01或质量；保留现有持续屏幕向下力 |
| Visual crop | 暂存栏下沿，当前盘面Y=292；与固定377供给线独立 |
| Hidden physics top | 方案采用盘面Y=140；顶部墙厚10、中心Y=135；左右墙延伸至该边界，底边828及左右0/420保留 |

Y=140按最大物理半径63及正式图最大显示半径72核算：盘贴顶时中心203、图片下沿275，低于裁剪线292，可完全隐藏并留17单位余量。此隐藏空间只改变物理可达范围；订单区仍由292硬裁剪和输入边界保护。

### Implementation Plan

1. `PlatePresentationWorld`新增中心高度观察，只遍历有效盘根；不比较半径、候选位置、碰撞、完整界内或残影。观察证据携带固定门槛、最小中心Y、阻挡plateId和snapshot revision。
2. `DailyProductionComposition`保留0.2秒活动时间调度、队首提交和无catch-up批量；以高度观察替换`SpaceAvailable`。阻塞不出队、不递增成功计数、不消耗出生随机。
3. 以成功生成序号而非尝试次数或plateId选择150/260；独立表现随机流生成一次水平偏移，以plateId缓存出生参数并通过`ViewPlateMotion`传入。重复快照不得重抽样或传送运行中的盘；重开/退出清缓存。
4. 仅在刚体创建分支施加一次当前坐标`(0,+5)` Force；Reconcile、内容减少、暂停继续、打乱和重复快照不重施。保留持续力、阻尼、冻结旋转和局部物理场景。
5. 物理顶部边界改为Y=140并同步墙体、`ConstrainGeometry`和诊断常数。新盘允许暂态初始交叠；只允许回滚到经测量合法且对象集合一致的历史状态，没有合法历史状态时继续物理分离并记录暂态，不销毁盘、不取消已提交供给、不恢复旧空间门槛。
6. `GameplayView`建立盘面`RectMask2D`根，范围为实际暂存栏下沿至盘面下沿828；盘、食材、盘边、残影、选中与提示高光全部在裁剪根内。暂存、订单、按钮和模态为明确前景层。运输/入锅/端锅反馈保留在对应非盘内层，不能被统一裁掉。
7. UI raycast优先；以同一裁剪矩形替换旧304输入上界。裁剪外拒绝盘面点击，裁剪内继续碰撞与食材alpha命中，保证露出有效像素可点。前景遮挡区提供真实raycast阻挡。
8. 提示候选使用共享可见alpha命中条件；领取前及奖励生效时再次确认存在可点击且匹配订单的食材。完全隐藏目标无效，部分可见且有有效像素者可用；不修改核心订单顺序。

### Ownership and Code–Art Interface

唯一写入者为code_builder。候选代码范围：`DailyProductionComposition.cs`、`DailyViewMapper.cs`、`PresentationPort.cs`、`GameplayView.cs`、`GameplayFeedback.cs`、`PlatePresentationWorld.cs`及相关现有诊断；如观察结构影响开发端口，仅做最小兼容。原则上不修改`DailySession`、固定C内容JSON、Director、存档、平台插件或Boot场景资源。

复用现有中心pivot、盘沿半径448/512比例、食材alpha和局部坐标。裁剪层负责所有盘相关绘制，前景UI负责遮挡及输入阻断。预览不授权新数值、资产或机制；实现必须以FROZEN规则和真实布局为准。

### QA Intent and Preliminary Script Test Scope

- Supply：空场可生成；中心376.999阻塞、377/377.001允许；任意一盘阻塞即不出队；半径/边缘不参与；解除后只生成原队首一盘；无catch-up。
- Spawn：按成功序号150/260交替；世界偏移±0.5/盘面±50；出生Y/半径不变；拒绝观察不消耗随机；重复快照稳定。
- Force / hidden physics：仅创建时一次正Y 5 Force，持续力保留；暂停/Reconcile/打乱不重施；最大盘可完整隐藏、继续碰撞并再露出；空盘立即退出高度集合，残影不阻塞。
- Render / input / hint：多比例下盘相关视觉统一裁剪；隐藏区和前景UI点击无核心变化；露出alpha像素可命中；完全隐藏目标不可提示，部分可点目标可提示，失效不扣奖励额度。
- Lifecycle：手动/后台/激励暂停、终局、退出停止供给；恢复单次节奏；重试清缓存与轮换。
- Preserved：固定C 50/183/16/61、日期映射、清空暂存队尾与ID守恒、打乱内容不变、31/49开锅、奖励幂等、首通和暂停计时。

实施前记录dirty工作区差异、相关文件哈希及实际可行的规则/编辑器编译/Boot基线；修改后重跑相同检查、v6定向范围和核心Smoke，检查Diff并按Introduced、Pre-existing、Expected Change、Uncertain、No Difference分类。旧C05/C06完整界内/无重叠断言属于Expected Change，必须替换为v6高度语义。

人工检查保留：实际入场与拥挤等待、隐藏/再露出自然度、暂态交叠消解、手机可读与真实点击、提示/飞行/暂存/订单反馈、与v5相比的节奏和难度。数值、截图、alpha命中和脚本通过不能替代HC-04体验判断。

### Implementation Preview v6

路径：`.harness/previews/TASK-001/r005/v6-layering/`。`occluded-1080.png`展示盘与食材在五格暂存栏下沿被全宽硬裁剪；`emerging-1080.png`展示同一对象继续向下露出；`layering-detail.png`标注裁剪与点击意图。另有360×640可读性图与`spec-and-qa.md`。

预览复用现有Unity截图和正式资产，是确定性目标状态合成，不是v6运行截图、物理模拟或实现通过证据。r004既有美术方向继续有效；本候选只确认层级、裁剪和交互意图。

## Pre-delivery QA Scope v6（Designer read-only）

Status: QA Scope Ready after CL-025；Designer不写脚本、不运行测试。Builder必须无交互运行一次完整范围，结束后一次读取退出码、报告和最终日志；任何修复后完整重跑并保留失败证据。

- 规则层：完整重跑现有14组，保留固定C、订单、日期、奖励与回放断言；Task Version写6、checkpoint写HC-02-v6。旧C04只证明核心CanSpawn门控，不冒充真实高度验证。
- `V6-S01`：真实刚体中心376.999/377/377.001分别阻塞/允许/允许；空场、多盘任一阻塞、不同半径、边缘越线、候选位重叠/越界、残影排除及固定377不随裁剪变化。
- `V6-S02`：真实Composition与活动时钟；连续阻塞保持队首、成功计数、缓存与随机序列；解除只供同一队首一盘；落后多个间隔也无catch-up；空盘移除后残影不阻塞；陈旧observation拒绝。替换旧C05空间门槛断言。
- `V6-S03`：至少六个成功盘核对150/260、盘面偏移−50…+50、出生Y/半径、阻塞不消耗随机、重复快照/食材减少不重抽或传送、重试/退出重置；表现随机不改变核心hash/订单/映射。
- `V6-P01`：局部物理场景测一次正Y 5 Force与持续力模型；Reconcile、暂停恢复、打乱不重复；记录质量、阻尼、步长、速度/位移和小于一次力贡献的容差。
- `V6-P02`：最大盘中心203时完整隐藏且仍参与碰撞/高度门槛，持续力可使其重新露出；两个初始交叠盘在120固定步内保持有限数值、不删除、不回到已知非法前态并尝试分离。替换旧U03瞬间绝无交叠要求。
- `U03 Shuffle`：按CL-025恢复目标304+radius…828−radius；内容、队列、核心hash不变，预检查不移动，成功结果无旧速度、两两不交叠且均在既有范围；失败不改状态、不扣奖励。
- `C06`：清空1件/5件暂存后盘尾、itemId、种类、总量和pending前缀守恒；新增盘经真实高度供给链入场并延续成功序号、缓存偏移和一次力。
- `V6-V01`：720×1280、1080×1920、1440×3200及非零安全区；实际Canvas截图与像素检查证明盘/食材/残影/提示/选中在292上方无漏图、下方存在露出内容，运输/端锅仍到订单目标。
- `V6-I01`：真实SubmitScreenTap覆盖不透明点、透明角、跨边界细条、裁剪上方、暂存/订单/按钮/模态/viewport外；比较核心hash与事件，UI优先且露出有效alpha只命中正确食材。
- `V6-I02`：完全隐藏、仅透明部分露出、仅不透明细条露出、上层盘/UI遮挡及多个匹配目标；提示只选首个真正可点者，目标移入遮挡后效果被裁剪/终止且不代点；记录最坏可点击检测耗时。
- `V6-I03`：真实RewardCoordinator与可控服务；请求前无可见目标不调用服务，回调前目标隐藏/失效不发提示不扣额度，有其他可见目标时仅生效一次；覆盖取消、失败、重复回调和旧generation。
- `V6-L01`：手动/后台/激励及叠加暂停时活动时钟、供给计数和刚体稳定；恢复无catch-up/重复力；胜利、超时、溢出、退出无供给；重试重置缓存/轮换。保留U01/U02/U04/U05和BuildGuard/VerifyBoot。

证据必须包含每例输入/预期/实际/PASS或FAIL、实际命令与退出码、无filter漏跑证明、规则/Unity报告、最终日志、逐步物理与供给数据、出生缓存/计数对比、输入前后hash/事件、奖励调用/额度、多比例PNG、最终源/诊断哈希和Git Diff分类。受控摆盘/合成事件与真实Boot自然流程必须区分。

Human Check：入场与拥挤等待节奏、隐藏/再露出自然度、暂态交叠、手机辨识与边缘点击、提示/飞行/回填/端锅反馈、相对v5难度与整理体验。Out of Scope：音频生产、Player/WebGL修复、微信导出/上传、目标设备验收、发布、固定C重设计、无道具必解。

v5适用说明：下列既有设计与实施计划作为历史保留，其中原创盘内容生成及相关QA预期以CL-020/CL-022及本次Designer增量为准；其余内容保留。用户已确认供给沿用现有实现并授权实施，无新资产、布局或视觉风格设计，本次按Code Only增量处理并保留既有视觉批准。

## Design Handoff v5 — fixed C（当前有效增量）

Designer: `/root/feature_designer_c_v5`；Status: Design Ready；只读，未运行测试。Proposed Product Decisions: None。

真实入口：Production Composition目前从原创配置创建内容，DailySession还按IsOriginal调用OriginalDailyContent.Plates(challengeId)重新造盘；两处均须改为唯一固定C来源。DailyContent身份校验、Importer和BuildGuard须同步，不能只换JSON。项目旧C源与指定新源的bubbles一致，但文件哈希不同，须准确标记来源。普通供给链、几何、日期/映射算法、独立RNG、Director及道具逻辑保留。

实施步骤：

1. 建立项目内可独立运行的固定C来源，保留指定文件字节身份；生产内容逐盘等于已确认的50盘，不依赖用户Documents路径。统一运行时生产内容入口，避免存在两套有效选择。
2. Session统一消费传入Content.Plates，移除开局随机库存重建。保持plateId 1..50、itemId 1..183及sourceIndex源顺序，继续用原日期种子和全局一一映射。
3. 同步内容版本、来源哈希、导入校验、canonical metadata及生产绑定，保留权重源/20行权重、现有资源引用与布局。旧文件可保留为历史，但不能误报为当前生产版本。
4. 更新纯规则和Unity诊断，使其验证真实生产factory和Boot绑定；测试独立黄金序列来自指定源，不用被测provider自身产生期望。
5. 先记录基线，修改后重跑必要检查；真实PlayMode定向验证供给阻塞/恢复与冷却，实际无法运行则明确记录，不能以core.Supply(true,true)代替空间链证据。

Build Mode: Code Only（本次增量）。Code Builder唯一写入受影响Core内容/Session、ContentImport、Content/Daily、Bootstrap生产装配及相关诊断/测试、必需的项目内容元数据和绑定；报告写入`.harness/qa/TASK-001/v5/`或同版本新revision。仅必要时调整Boot的内容引用，不改场景布局、视觉、资产批次、音频。PM单独拥有Task及GAME_SPEC。禁止Commit/Push/发布，禁止清理已有dirty改动。

Designer QA Intent（固定为此次检查范围，检查问题与产品问题分别上报）：

| ID | 断言与实际观察 |
|---|---|
| C01 | 项目内新源字节SHA256匹配；全部50盘ID、长度、每个符号与源一致；真实factory/session的183条item plateId/sourceIndex/kind逐项匹配。 |
| C02 | 多日期原始C结构完全相同；映射为A–P到16食材双射；同日重试映射、初始状态和队列复现，跨日使用相应日期种子，不强求所有相邻日期的排列都不同。 |
| C03 | Presentation随机消费不影响Mapping、Director或核心状态；固定内容不再抽取随机库存。 |
| C04 | 空间拒绝、冷却拒绝、暂停、重复观察序号均不动队列/库存；恢复只供原队首一盘，50次保持1..50，空队列拒绝。 |
| C05 | 真实Boot/World观察完整界内与非重叠；跨多个0.2秒检查阻塞不动队首，清除阻挡后Update恢复原队首。有效运行时间间隔0.2秒、暂停无供给，恢复不一次补发漏过次数。 |
| C06 | 清空暂存追加混合盘且保留itemId/总量/原pending前缀，原C剩余盘先出；打乱只改位置，核心hash、盘内容、队列不变。 |
| C07 | 保护20行权重和阶段边界、初始两开两锁、31/49单开锅、满五格匹配可收/不匹配失败、600秒、06:00/跨日归属、同日重试和183单位/61单结清。 |
| C08 | 新内容导入、身份拒绝、实际生产绑定/编译与核心流程smoke；旧候选replay不能冒充新内容身份。 |

黄金盘序（独立源提取）：

```text
ABCDE|BDFGG|EHICC|EJD|AAECH|HHB|BGHCC|CJFD|KHBC|HGB|EEBC|DDJF|DJIL|JELCI|JDICC|FEKL|CJHIG|FGJA|DBBG|HHI|KJHGC|JJJHG|CK|MMC|CF|KFJN|KHC|EAHI|LOIC|ENI|HEI|PKECG|BHM|II|LJEP|DAFI|DJP|PC|NPO|NNC|FOM|EAMH|CCA|P|JN|MH|CB|CCEAI|KLGJJ|GDIKB
```

既有Task001RuleQa P01的每日盘面变化断言应改为固定结构+日期映射，P10旧版本篡改值也须使篡改实际发生；fixture不得继续造原创盘。真实空间验证使用Task001SmokeDiagnostic的定向入口。现有IntegrationDiagnostic含WebGL脚本编译，不等于完整发布构建。全部新证据带v5/当前内容身份，不沿用历史通过。几何/脚本检查不能替代难度、爽感、完整人工试玩或设备验收。

Status: Draft handoff received; pending human review

## Current Implementation and Plan r001

- 启动入口为 Boot.unity；DailySession.cs 有五格暂存与守恒，但点击匹配仅前两锅，后两锅永久 Locked。需要统一开启锅集合、补单与31/49单解锁。
- TimeResolver.cs 当前零点换日；SessionController.cs 有手动/后台暂停。改06:00挑战日期、固定开局日归属、重试重新取日；叠加管理暂停原因和十分钟无输入超时。
- RecoveredProps.cs 的提示代收、刷新内容和清空Outside均不符合本需求，不能直接接线。
- 为清空暂存引入运行时盘注册表并保留 itemId；DailyViewMapper.cs 与供给入口原有 content.Plates[id-1] 假设需要移除，防止第51盘越界。
- DailyContentImporter.cs / DailyContentBuildGuard.cs 绑定旧来源哈希。使用本项目版本化生成配置并更新导入校验与场景数据绑定，保留历史材料。
- PlatePresentationWorld.cs 有圆形刚体碰撞和命中阻挡；用户v3确认沿用同平面不重叠方向，不引入多层遮挡。复核碰撞半径与可视盘沿，防止视觉穿插；不能宣称原版等价。
- GameplayView.cs 当前 Render 重建界面。使用持续视图节点及完整表现事件，实现飞行、回填、端锅、解锁和VFX。
- 新增可注入激励、分享、档案及好友榜接口，开发模拟保存本地首通、额度和设置，不保存残局；奖励以请求ID及会话代次防重复/过期回调。
- GameplayFeedback.cs 只有正弦提示音；未发现成品音频或嵌入字体。定制音频生产来源与字体来源需要落实。
- 当前未运行测试、未启动Unity。常见安装目录未找到Unity.exe，UnityHub路径指向D:/Unity/Editors且当前为空，实际Editor位置仍待定位。

## QA Intent r001

### v3 物理方案增量（Designer只读调查；待核查）

- 复用 PlatePresentationWorld 的 Rigidbody2D、圆形碰撞、边界及空盘收走。碰撞半径与正式资产盘沿一致，透明留白不算有效盘沿；显示等比缩放。
- 打乱先按各盘半径计算完整界内、两两不重叠位置，整组验证后原子提交，清除旧速度；不能简单交换不同半径盘的中心，也不能用穿越盘子的直线动画。
- 供给候选：运行态、供给间隔满足、队首盘完全落在供给区域且不重叠才入场。拥挤时保留队首等待，空盘移除后恢复；不跳队、不强塞。
- 无有效打乱布局时沿用已确认的无目标规则，不允许领取或扣额度。
- 后续QA覆盖生成/碰撞/打乱/补给无可见重叠、满场等待与腾空继续、数量守恒；几何断言不能替代自然碰撞的人审。
- 此增量未编辑代码、未运行测试；同一Visual提供盘沿、透明边距和食材安全区，Code Builder负责唯一引擎集成。

- 自动边界候选：183单位守恒、重复追加盘、30→31/48→49、满暂存匹配与不匹配、打乱只改位置、提示不代点、600秒超时、叠加暂停、06:00跨日归属、首通幂等、分享成功/取消/失败/重复回调。
- 接口合同候选：ingredientId与itemId分离；食材图共用于订单/盘内/暂存；锅底/红汤/前沿与蒸汽分层；明确pivot、命中轮廓、UI九宫格；VFX不拦截输入。像素尺寸待批准预览后冻结。
- 人审保留：盘子不重叠、碰撞自然与可点范围、高精度画面、16种识别、原创音频试听及循环、难度和爽感。
- 计划写入：唯一Code Builder负责Runtime、Contracts、ContentImport、场景和Unity导入绑定；Visual负责新revision预览和视觉QA；Art Asset Builder负责批准资产；音频生产待来源落实。
Code–Art Interface: 待调查后定义。
Asset Contract: 待预览与实际消费接口确定；须含尺寸、命名、alpha、pivot、UI 切片、状态和音频契约。
Audio production feasibility: 待核实原创定制音乐/音效制作能力与可交付证据；不可提前宣称达到定制品质。

## Design Handoff r002 — Freeze-readiness clarification (HC-012 Accepted)

Status: Clarifications accepted; waiting for the resumed Designer to form a Freeze Candidate。已读取真实核心、会话、表现、物理和导入代码及已接受r004；未修改文件、运行测试或启动Unity。

- 当前 `DailySession`、`DailyDirector` 的点击、预留和重复订单排除只覆盖前两锅；`DailyViewMapper` 等以 `content.Plates[id-1]` 索引，清空暂存新增盘时需运行态盘注册表。`PlatePresentationWorld` 的圆形刚体与边界可复用，但显示、碰撞与生成必须共用可视盘沿几何。
- 当前恢复配置固定原始骨架/权重哈希、20行权重和按已收集食材/183的难度阶段；已确认订单计数是完成单数/61。CL-013允许保留权重与阶段语义，但盘面内容、日期种子和食材映射必须原创。
- CL-014确定通关分享卡仅为静态主题图，不含动态结果字段。
- CL-015授权外部原创音频制作；供应方、授权、源工程和成品证据仍是音频生产批次的前置登记项，不能以正弦提示音降级交付。
- 可冻结的技术候选（不含上述产品决定）：Code + Art；Code Builder唯一集成，Art Builder在Unity Assets外按版本批次生产源图与manifest，Code Builder导入；食材稳定映射`food_00…15`按用户食材表，`itemId`和种类ID分离；盘/锅为正俯视真圆、中心pivot、RGBA、可视盘沿决定碰撞半径，透明边距不扩大命中；UI数字动态排版、不烘焙；正式字体、分享卡和音频合同待决定。
- QA候选保留183单位/61单守恒、四锅一致选择与补给、无重叠供给/打乱、30→31/48→49、暂停与600秒、06:00跨日、首通/分享幂等，以及Unity内安全区和体验人审。数量、距离、PNG和音频属性仅是Test Proxy，不能代替难度、自然碰撞、美术、授权、音频品质或真机验收。

## Design Handoff r003 — Visual / Plan Candidate (HC-02 Pending)

Status: Design Ready。目标里程碑为 HC-02；输入为HC-007、HC-011、HC-012；未修改文件、运行测试、启动Unity或联系外部供应方。

### Frozen implementation plan

1. 定位Unity 6000.0.26f1，记录当前编译、Boot场景、资源绑定与Git基线；无法运行即标未验证。
2. 用本项目命名空间、内容版本、挑战日期生成原创盘内容、日期种子和食材映射；保留已确认的高难权重、阶段、候选分类与fallback语义。完成订单数/61驱动开锅；Director阶段继续使用已收集食材/183，二者不得混用。
3. 四锅统一匹配、预留、去重、补单、回填及结清；新增运行态盘注册表，清空暂存保留全部`itemId`并队尾追加新`plateId`，移除运行期`content.Plates[id-1]`假设。
4. 北京时间减6小时确定挑战日期；重试重取日期；单调时钟累计600秒有效游玩时间；暂停原因可叠加；本地模拟激励、分享、档案与好友榜真实标注为模拟。
5. 道具以`requestId/sessionGeneration/rewardKind/quotaChallengeDate`防重复与过期回调；成功且实际生效才扣分享额度。供给和打乱依HC-007先验证完整界内且不重叠，再原子提交。
6. 持续视图以完整事件表现飞行、回填、0/3至3/3、端锅、补单、点火和VFX；逻辑与动画分离。正式资产/音频各自人审后，由唯一Code Builder导入绑定。

### Ownership and Code–Art Interface

- Build Mode: Code + Art，含外部原创音频依赖。Code Builder唯一写入Unity Runtime、Contracts、Content、Scene、导入资源及`.meta`；Art Asset Builder只写`.harness/artifacts/TASK-001/`批次与manifest；外部音频供应方无仓库写入权；Visual仅预览/集成视觉QA。
- Unity导入根：`Unity/Assets/HotpotSort/Resources/Hotpot/TASK001/v3/rNNN/`，由单一目录配置选批次。`ingredientId`（种类）、`itemId`（唯一食材）、`plateId`（运行容器）严格分离。
- `food_00`至`food_15`按CL-013前的16种用户清单顺序固定映射；订单/盘内/暂存共用种类资产。
- 所有Sprite中心pivot `(0.5,0.5)`；盘子的可视外沿半径同时决定显示缩放和碰撞，透明边距与阴影不计入；食材命中和显示共用局部变换；VFX不拦截输入。
- 锅状态：`Unlit → Open → Completing/Serving → Open/EmptyTail`。事件至少携带`sessionId,eventSeq,transactionId,eventType,itemId/ingredientId,source/target container+slot,filledBefore/After,orderIdentity,plateId`，不得从最终快照猜中间状态。

### Asset and audio contract

| Asset group | Contract |
|---|---|
| `food/food_00…15` | 512×512 RGBA PNG，主体居中、至少8%边距、真透明；无盘/文字/水印/整体投影。 |
| `containers/plate_main` / `dish_buffer` | 1024² / 512² RGBA；可视盘沿半径448 / 224px，食材安全区半径360 / 176px；真圆正俯视。 |
| `pots/{body,unlit,broth,rim}` | 同坐标1024² RGBA；主圆外沿384px、汤面安全区300px，正俯视。 |
| `background/table` | 1440×3200 RGB；仅背景可裁切，不烘焙交互物。 |
| UI / icons / FX | 面板1024²、按钮512×256、订单牌512×256、图标256²、FX512² RGBA；数字动态排版。九宫格：panel L/B/R/T=128px；button=64px；order plate=48px且尖角另图；progress/timer L/B/R/T=48/32/48/32px。图标稳定ID为`pause/settings/close/play/retry/home/music/sound/hint/clear/shuffle/ad/share/leaderboard/lock`；FX为单帧纹理由代码动画。 |
| `share/share_theme` | 1200×960 RGB静态主题图，不含任何动态字段；仅Unity模拟分享预览。 |
| fonts | 合法可嵌入的手写展示字体与可读数字/正文搭配，须交付字体文件和许可证，禁止正式版依赖OS字体。 |
| audio | `bgm_main`、`amb_boiling/store`及十项操作/胜负SFX；WAV PCM 48kHz/24bit，音乐/环境立体声、点状音效单声道；提供源工程、分轨、循环点、试听、授权和母版。 |

manifest必填：`assetId,taskVersion,revision,relativePath,sha256,format,width,height,alpha,pivot,visibleBounds,foodSafeRegion,hitGeometry,nineSliceBorder,source,license`；音频另记采样率、声道、时长和循环点。外部音频供应方、委托范围、权利与交付路径必须在音频生产开始前登记；未登记不阻断代码/图像，但阻断音频生产与最终Verified。

### QA and regression intent

- 自动/技术：183单位、每类三倍数、61订单、唯一ID和清空暂存守恒；四锅与30→31/48→49；供给/打乱几何；600秒、暂停、06:00、首通和分享幂等；Unity编译/Boot/绑定/安全区/音频循环。
- 人工：真实点击和自然碰撞、16种识别/土豆形态/统一美术、动画可读性、外部原创音频试听与游戏混音、高难整理爽感。数量/哈希、圆距离、PNG规格、音频元数据和Unity帧率都不能分别替代这些体验或真机结论。
- 回归：相同基线→规则/表现/接口定向检查→启动至设置完整smoke→Git Diff分类。实施后由Designer另行输出并经人审接受Script Test Scope；旧摘要与旧测试不得沿用。

Remaining execution dependencies: Unity实际路径、字体实际来源、外部音频供应方/授权尚待实施前查证；它们不是未决产品规则。r004轻微椭圆/土豆放射纹不改变正俯视真圆和食材识别合同，正式资产仍须按合同并经人审。

## Workstream Ownership

| Role | Current scope | Writes |
|---|---|---|
| PM | Task 和已确认长期规范同步、汇总证据 | 本 Task、docs/GAME_SPEC.md、docs/ART_BIBLE.md |
| Feature Designer | Draft 接受后只读实现调查与 QA Intent | None |
| Visual & Presentation Agent | 整屏预览、迭代；批准预览与冻结资产合同后的正式美术和表现层实施；集成后视觉调优与QA | 本 Task 新预览 Revision、`.harness/artifacts/TASK-001/`及经PM串行交接的表现层路径；不改核心规则/数据/平台逻辑 |
| Code Builder | v5固定C内容、稳定表现接口与必要技术验证；不做视觉重排和审美调优 | 见当前Design Handoff v5的限定路径；允许实施 |

# Visual Direction

Preview Status: r004 accepted as visual direction (HC-011)

r003草图：.harness/previews/TASK-001/r003/gameplay-preview.png；同目录prompt.txt、qa-mobile-360.png。顶部09:59，订单1/3和2/3已显示；941×1672。兼容调度已核验，但该版仍有斜俯视、排列规整及土豆纹理问题，保持Needs Revision，不能用于正式资产生产。
Latest Approved Preview: .harness/previews/TASK-001/r004/gameplay-preview.png（HC-011 Accepted；仅作为视觉方向，不替代正式资产合同）
Rejected: .harness/previews/TASK-006/hotpot-food-plates-regenerated-v2.png 不作为输入。
Preview History: r001 已保存 .harness/previews/TASK-001/r001/gameplay-preview.png；完整生成提示词在同目录 prompt.txt。由Visual使用内置image_gen制作。
Preview r001 review: 四锅/五小碟和16种素材方向可见，但盘面分类整齐、没有混装堆叠，摄影写实过强、细描边不足、锅为斜俯视，左右裁切，缺少通用UI构图。941×1672，非严格9:16。PM不建议据此生产正式资产，用户已纠正：取消堆叠，保留同平面碰撞混装；r001需要修订。
所有新增高影响视觉决定回 PM 确认。

Preview r002: .harness/previews/TASK-001/r002/gameplay-preview.png；完整prompt同目录prompt.txt，360px检查图qa-mobile-360.png。内置image_gen生成，941×1672。
Review r002: 四锅两开启两未开火、五小碟、八个混装示意盘及三道具图标；未见明显盘子重叠。仍有斜俯视/椭圆盘、两列过于规整、顶部安全边距不足、木桌偏浅、土豆片误画为柠檬状及牛羊肉区分不足，非严格9:16。不满足最终视觉标准，未批准正式生产。

Preview r004: .harness/previews/TASK-001/r004/gameplay-preview.png（1080×1920）；手机核查图 .harness/previews/TASK-001/r004/qa-mobile-360.png（360×640）；提示词和核查记录在同目录prompt.txt。Compatibility Prompt的visual_design_agent以当前Astra/high配置生成。
Review r004: 已严格9:16；顶部09:59、订单1/3/2/3以本地字体确定性排版；盘子错位散列且静态画面未见盘间重叠。仍有轻微椭圆盘/斜俯视倾向、土豆柠檬式放射纹、木桌偏亮，顶部安全边距尚未设备核验。预览不定义正式字体、盘数、碰撞参数、资产尺寸或安全区合同；待HC-011人工决定，不能进入正式资产生产。

# Regression Plan

- 基线：记录当前 Unity 编译/场景运行、现有资源与 Git 改动；无法执行的项目标为未验证。
- 修改后重跑相同检查，再检查受影响规则与核心流程。
- Smoke: 启动→每日挑战→点击匹配/暂存→道具→解锁→暂停继续→胜负→重试/退出→设置/接口页面。
- 定向保护：食材守恒、订单上限、队列末尾补盘、分享额度、跨日与首通幂等、暂停计时。
- 差异归类 Introduced / Pre-existing / Expected Change / Uncertain / No Difference。
- Designer 在实施后定义实际 Script Test Scope；Builder 按范围运行和自检；按最新AGENTS仅使用四种自定义角色。
- 人工保留整屏/资产、定制音频、集成画面、难度和爽感验收。

# Build and Verification Results

## v6高度门槛与暂存栏遮挡增量（HC-03候选）

IMPLEMENTED：固定盘中心Y<377高度门槛、0.2秒单队首、成功序号150/260双位与缓存随机偏移、一次当前坐标(0,+5) Force、隐藏物理顶部140、暂存栏下沿292硬裁剪、前景点击阻挡、露出alpha点击、可见提示筛选及CL-025打乱范围304…828纠偏。修改六个运行时代码文件、`docs/GAME_SPEC.md`及两个既有诊断文件；未修改固定C内容、核心队列/订单、场景、Prefab、正式资产或平台插件。

VERIFIED（自动范围）：最终`.harness/qa/TASK-001/v6/r002/run05/`完整通过14组规则与16个必需Unity用例，missingCases为空、runtimeErrors为空；BuildGuard / VerifyBoot通过；三个进程退出码均为0；owned-path Diff检查通过且当前源/诊断哈希与报告一致。run01–run04失败证据和修复后完整重跑均保留，r001基线/基础自检证据未覆盖。

覆盖：U01–U05、V6-S01/S02/S03、C06、V6-P01/P02、V6-V01、V6-I01/I02/I03、V6-L01及更新后的U03/C05-C06语义。实测376.999/377/377.001为阻塞/允许/允许；一次力速度贡献与模型匹配；最大盘可受控完整隐藏并再露出；初始交叠夹具120步内分离且无删除/非法回退；三比例裁剪边界以上差异像素为0；真实SubmitScreenTap只接受边界下方有效alpha像素。

集成视觉QA：`.harness/previews/TASK-001/r006/integrated-visual-qa.md`及两张复核板确认三比例裁剪、五小碟/四锅/UI前景连续、提示/运输/端锅反馈无阻断偏差。它们是受控夹具截图，不是自然盘面节奏证明。

Regression classification：Introduced and resolved——打乱区域意外扩大已按CL-025恢复；run01–run04诊断/夹具问题已修复并全量重跑。Expected Change——v6供给、力、隐藏物理、裁剪、输入与提示。No Difference（已执行保护范围）——固定C、映射/Director、订单、额度、重试、暂停和设置。Pre-existing——FindObjectOfType过时警告及既有dirty worktree。

Remaining Human / Risk：提示候选最坏编辑器测量约101.4ms，需试玩/设备性能关注；半露食材在手机缩放下较小；端锅途中锅与订单牌经过顶部HUD时暂停按钮保持前景，是否自然需人审。自然供给/拥挤节奏、隐藏再露出手感、暂态接触、真实手指点击与相对v5难度仍未验收。

Not Verified / Out of Scope：Player/WebGL构建、微信导出/上传、目标设备测试、音频生产/审批、发布及HC-04最终体验。自动通过不得扩大为这些结论。

## v5固定C增量（当前结果；Designer覆盖核对已完成）

- IMPLEMENTED：Boot绑定项目内canonical内容，通过`DailySessionFactory.FromProductionJson`进入`Content.Plates`，移除开局随机库存重建；内容版本`hotpot_daily_task001_v5_fixed_c_1`。原生成器源码/meta归档为Content/Daily文本，可恢复；旧配置和源材料保留。
- 当前内容SHA256：`99fb452bf75693f7978afe32c5bf52e3b1293b36e55669068c9fa84065ba6f61`；configurationDigest：`c6a3139cf879e1c0138805ab9f4295eb793574c5a14629a5b8770e0050d9af22`。
- 基线：12组规则通过，旧内容导入退出0，真实Unity U01/U03通过。首次Unity基线未捕获退出码，但完整报告/最终日志零失败且进程已结束，不能伪称该次退出码0。
- 修改后：14组规则通过（退出0），包含50盘/183条独立黄金比对、365日期/重试、随机流隔离、队首及保留玩法；Unity 6000.0.26f1编辑器编译、BuildGuard、真实Boot/factory验证通过（退出0）；PlayMode U01/U03/C05-C06通过（退出0、无运行错误）。
- 真实供给：队首2跨三次约0.2秒检查保持阻塞；暂停无供给；解除阻挡恢复队首2且单次只供一盘；清空暂存追加盘51保留itemId；打乱不改核心状态。几何采用既有0.001盘面单位诊断容差，不替代人审。
- 明确限制：WebGL脚本编译在未修改的微信插件出现`CS0103 WebGLInput`（DisableKeyboardInput.cs:19，WXTouchInputOverride.cs:158/170）。没有修改前同项基线，分类Uncertain external build limitation，不能断言是既存错误；本轮不修改平台插件。后续独立VerifyBoot通过不覆盖该失败。
- 证据根：`.harness/qa/TASK-001/v5/r001/`。`implementation-result.json`含命令及C01-C08映射；`changed-files.json`记录25个新增/修改/归档路径相对本次前dirty工作区的哈希；规则`post-rules.json`，实机编辑器供给`post-r2-supply.json`，Boot日志`post-r2-integration.log`，WebGL失败`post-r1-integration.log`。
- 未运行完整玩家构建、微信导出/上传/设备验收或人工体验，不提交、不发布。供给/物理、RNG算法、Director、道具、音频、美术及布局代码保持本次前行为；盘尺寸分布改变引起可见盘数样本14→13属于Expected Change，不是新增盘数阈值。

### Designer pre_delivery_qa（已接收）

`/root/feature_designer_c_v5`返回QA Scope Ready，只读核对实际实现、脚本及证据；未运行测试或修改文件。C01–C08覆盖完整，无需补写断言、修复业务或重复执行。固定Script Test Scope为现有规则14组、Unity U01/U03/C05-C06、BuildGuard及VerifyBoot。当前25条变更/归档记录与文件after哈希一致；Director、Pcg32、TimeResolver、SessionController、DailyViewMapper、GameplayView及PlatePresentationWorld与本次前哈希一致。来源、canonical及生产configuration身份在规则、Boot和PlayMode证据中一致。

当前结论：IMPLEMENTED；本范围自动规则及Unity编辑器定向运行 VERIFIED；完整Player/WebGL构建未通过，DEVICE-TESTED / HUMAN-ACCEPTED / RELEASED均未完成。HC-03-v5只提交当前分支实施结果，不宣称全项目完成或无Bug。

## 历史全项目结果（不代表v5增量当前状态）

Code: Not Started
Art: r001 concept produced; formal assets Not Started
Audio: Not Started
Integration: Not Started
Tests: Not Run
Human Art Approval: Pending
Human Audio Approval: Pending
Human Experience Check: Pending
v5固定C与普通供给替换分支已完成自动验证和用户HC-04体验接受；不将其扩大表述为完整玩家构建、设备、音频、美术或发布验收。

# Final Decision

Status: Awaiting Human Check（HC-03-v6 Implementation Result）
Next checkpoint: 用户对HC-03-v6回复Accepted、Needs Revision或Rejected；Accepted后进入HC-04最终实际试玩与体验判断。

## Task Version History

| Version | Date | Change | Reason | Invalidated Outputs |
|---|---|---|---|---|
| 1 | 2026-09-21 | 透明食材任务扩为整屏预览 | 用户确认参考及 16 种清单 | 无生产输出 |
| 2 | 2026-09-21 | 升级高精度每日挑战 Unity 可玩版，Code + Art，含定制音频 | 用户确认完整交付复述 | v1 仅预览范围、非目标、方案及接口；无已生成资产或代码 |
| 3 | 2026-09-21 | 盘子同平面碰撞、互不重叠遮挡，同盘允许混装 | 用户明确修正并确认 | v2多层堆叠要求、相关方案、r001预览方向；其余规则保留 |
| 5 | 2026-09-21 | 记录A/B/C共性；当前固定完整采用C内容结构，替换预处理与普通盘队列供给链；继续空间门槛访谈 | 用户“请你记录这些规律，但现在的流程完全复刻骨架C”；前序“确定” | v4及更早原创盘内容要求、相应生成方案，以及受影响的供给方案/实现符合性结论/QA预期不再沿用；HC-01/HC-02相关部分须重新确认。保留历史文件、其他玩法和美术/音频决定，不执行代码回滚 |
| 6 | 2026-09-21 | 普通盘供给改为中心Y高度门槛，采用150/260双生成位、原版水平随机偏移与(0,-5) Force；增加暂存栏硬裁剪、前景覆盖及可见像素点击边界 | 用户确认完整需求复述并明确“确认 实行” | HC-01-v5/HC-02-v5中旧完整界内无重叠门槛和三生成位定义，以及HC-03-v5/HC-04-v5中依赖旧供给、旧入场力、旧显示/点击边界的证据；其余玩法、美术与音频决定保留 |
