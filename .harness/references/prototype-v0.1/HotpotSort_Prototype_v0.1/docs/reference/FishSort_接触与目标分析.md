# Fish Sort：玩家接触顺序与顶部鱼缸选择

## 结论

本轮静态分析支持的结构是：**处理后的气泡队列依次供给 → 玩家在实际可命中的鱼中选择 → 开局两缸按前十泡数量选鱼 → 后续由完成事件和当前局面触发补缸。**

因此，既不是“必须按配置编号依次点完”，也不是“顶部鱼缸预先写死一条从头到尾的颜色序列”。这里沿用上一轮的纯内容骨架，讨论基础玩法，不把冻结、锁、倒计时等机制混进来。原生程序中的这些分支仍存在，不能把下述无机制推演称为原版实机复现。

### 本轮新增的明确证据

- 待生成列表 `pending[0]` 被用于实例化，随后 `RemoveAt(0)`。
- 场上存在高于 `GenRoot/MaxHeightPos` 的气泡时，不继续生成。
- 落点按 `GenRoot/GenPos` 子节点循环，并加水平随机偏移；气泡初始化施加向下的二维力。
- 初始只解锁内部序号 0、1 两个目标槽，单缸使用长度为 3 的鱼数组。
- 初始选鱼读取待生成列表前 10 泡，扣除已存在目标的未满足需求，优先选不同鱼种中的最大数量者。这一步不是权重随机抽样。
- 补缸阶段的 `LevelTest=5/6` 分支会读取暂存占用和进度，按 `Order1–Order5` 抽取评分类别，并有优先覆盖及候选不足回退。
- 同种鱼能进入多个目标时，按剩余需要鱼数升序尝试；新目标出现后会自动吸收暂存中的同种鱼。

## 1. 气泡先后进入场景，但不是强制点击队列

### 1.1 真实的生成规则

`Type_05123.method_40332 @ 0x122db7c` 的普通气泡路径可整理为以下阅读伪代码：

```text
当游戏处于运行状态，且 pending 非空：
    如果任一场上气泡的 y > MaxHeightPos.y：返回
    config = pending[0]
    anchor = genAnchors[spawnedCount % genAnchors.Length]
    position = anchor.position + (Random.Range(-0.5, 0.5), 0, 0)
    bubble.OnInit(config, spawnedCount)
    activeBubbles.Add(bubble)
    spawnedCount += 1
    pending.RemoveAt(0)
```

第二关另有教学暂停条件，传送带另走独立分支；上面只表示基础路径。一次调用创建至多一项，不能由此推导每秒生成数量。

生成点来自场景 `GenRoot/GenPos` 的子节点顺序，而不是 JSON 中直接填写的固定地图坐标。`BubbleItem.OnInit` 存储生成索引，并调用 `Rigidbody2D.AddForce`，其向量为 `(0,-5)`。这证明位置还受物理运行过程影响，不是一张静态的“第几行第几列”鱼表。

证据：[生成方法](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05123.method_40332.arm64.txt)、[高度谓词](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05123.method_40431.arm64.txt)、[节点绑定](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05123.method_40406.arm64.txt)、[气泡初始化](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/BubbleItem.OnInit.arm64.txt)、[随机包装函数](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_04979.method_39462.arm64.txt)。

### 1.2 配置顺序对应什么

它对应**处理后的供给顺序**。更前面的配置先创建，更后面的配置需等供给门槛允许后才创建。场上并不只有一个气泡，玩家也不必把第一泡全部处理完再碰第二泡。

空泡会触发回收，场上对象继续运动；只有高度条件满足后才会推进后续供给。所以“消掉一泡就固定出一泡”和“每点击三次固定出下一泡”都不是本次代码所证明的规则。

读取配置前还存在上一轮已还原的前部机制项交换。若真正去除全部相关机制标记，这种“普通项与特殊项交换”不会改动纯普通队列；若只是分析时忽略标记、实际仍保留机制，则队列仍可能被交换。下文三骨架例子明确采用保留原装鱼顺序的无机制假设。

### 1.3 玩家点击的是实际命中的鱼，而不是下一个数组下标

点击链是：

```text
PointerEventData.position
→ 按 Input 层进行 Physics2D.Raycast
→ 命中碰撞体 GetComponent<FishItem>()
→ FishItem.OnHandleClick()
→ FishItem.OnHandle()
```

普通鱼点击路径没有“必须是最小 BubbleID”“必须先拿 FishStr 第一项”的门槛。`ABCDD` 只是同泡装鱼关系，不表示必须依次点击 A、B、C、D、D。

画面上具体哪些鱼能被命中仍取决于场景位置、碰撞体、显示/遮挡和当前动画状态。目标选择代码还把鱼的 y 坐标与 `ShowFishPos.y` 比较，这说明“已创建”和“纳入当前可用供给统计”也是两层概念；不能把全部 activeBubbles 内的鱼无条件视为当前都可用。

**未恢复生成点坐标及实机逐帧状态，因此本轮不提供虚构的屏幕左右位置、首屏气泡数量或唯一点击路线。**

证据：[事件入口](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05123.method_40504.arm64.txt)、[鱼命中](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05123.method_40489.arm64.txt)、[射线检测](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05123.method_40415.arm64.txt)、[点击条件](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/FishItem.OnHandleClick.arm64.txt)、[供给分层](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05123.method_40405.arm64.txt)。

## 2. 开局两缸的选鱼规则已还原

### 2.1 初始两槽和后续补缸是两套入口

`Type_05123.method_40413 @ 0x123e9c4` 遍历 `TargetItem`，调用 `OnInit(i < 2)`；再对索引 0、1 各执行一次初始选鱼 `method_40359`，随后设置鱼种及显示。

这里只确认内部列表序号，不能把索引 0、1 擅自翻译成屏幕左、右。初始两个解锁目标也不等于场景里总共只有两个目标节点。

启动协程 `Type_05120.MoveNext` 在等待配置加载完成后先调用 `method_40413`，随后才切到运行状态。生成函数本身要求运行状态，因此正常启动路径上的初始选鱼发生在普通供给开始之前。

证据：[初始目标初始化](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05123.method_40413.arm64.txt)、[单缸三鱼数组](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/TargetItem.OnInit.arm64.txt)、[启动时序](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05120.MoveNext.arm64.txt)。

### 2.2 初始选择伪代码

```text
counts = 统计 pending 前 min(10, pending.Count) 泡的鱼种数量
existingTypes = 当前各目标的鱼种集合

对每个已有目标：
    若 counts 包含该鱼种：
        counts[鱼种] -= 目标当前还缺的鱼数
        数量 <= 0 则删除该键

若 counts 为空：
    改为统计整个 pending 列表

distinct = counts 中没有被现有目标使用的鱼种
若 distinct 非空：
    返回 distinct 内数量最大的鱼种
否则：
    返回 counts 内数量最大的鱼种
```

`method_40467` 和 `method_40433` 都是逐项比较最大数量，不调用随机函数；只在严格更大时替换当前选择。因此并列时保留该次枚举中先遇到的鱼种。本轮没有模拟原生 Dictionary 内部枚举顺序，不把并列写成固定先后。

证据：[完整初始选择](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05123.method_40359.arm64.txt)、[排除现有鱼种谓词](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05100.method_40564.arm64.txt)、[不同鱼种中取最大值](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05123.method_40467.arm64.txt)、[兜底取最大值](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05123.method_40433.arm64.txt)。

### 2.3 三份纯骨架的实际推演

假设：去掉全部机制，保留上一轮逐泡顺序，字母仅表示同类关系。不是宣称原版第 51、52、54 关每次实机都显示这些字母鱼种。

| 骨架 | 前十泡的主要鱼量 | 初始目标组合 | 从供给数量看，最早在哪个前缀出现三条 |
|---|---|---|---|
| A | A=9、F=7、D=4、E=4 | 内部首槽 A，次槽 F | A：前4泡；F：前6泡 |
| B | A=7、G=7、J=6 | A 与 G；谁先取决于并列枚举 | A：前3泡；G：前5泡 |
| C | C=8、H=7、B=6 | 内部首槽 C，次槽 H | C：前3泡；H：前6泡 |

这里“前4泡”指配置供给前缀，不是第四次点击，也不保证第四泡刚创建就全部进入可点区域。

例如 A 骨架前六泡：

```text
1: ABCDD
2: EE
3: AF
4: AF
5: CCD
6: FGEE
```

A 的前三条分布于第 1、3、4 泡；F 的前三条分布于第 3、4、6 泡。这解释了为什么研究初始接触节奏，必须看鱼在队列前缀中的分散位置，而不只是整关 A、F 的总量。

原始骨架在 [skeletons/](sandbox:/mnt/data/fishsort_order_analysis/delivery/skeletons)；可复核推演在 [opening_cases.json](sandbox:/mnt/data/fishsort_order_analysis/delivery/derived/opening_cases.json)。运行：

```bash
python code/opening_analysis.py
```

这段脚本只计算无机制的初始目标候选及供给前缀，不模拟物理、后续随机或完整通关过程。

## 3. 点击后进哪个鱼缸

`FishItem.OnHandle` 先找匹配目标：有可接收目标则进入目标；没有则尝试转入暂存位。暂存满等情况下另有失败或拒绝路径，不能视为无限容量。

匹配目标选择 `method_40317` 会先按 `TargetItem.get_NeedFishCount()` 升序排列，再排除未解锁、已满和处于不接收状态的目标。普通鱼要求鱼种相同。故同种鱼同时存在多个可接收目标时，优先填剩余需求少的目标。例如分别为 2/3 与 0/3 时，下一条优先送往 2/3 的目标，而不是固定优先左侧。

证据：[普通鱼去向](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/FishItem.OnHandle.arm64.txt)、[暂存路径](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/FishItem.method_40902.arm64.txt)、[匹配目标选择](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05123.method_40317.arm64.txt)、[排序键](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05093.method_40538.arm64.txt)。

## 4. 后续鱼缸是完成后现场选择，不是一条颜色表

### 4.1 补位由该槽的完成事件驱动

`TargetItem.CheckIsFull` 检查三条鱼和状态，随后回收/动画流程调用回调 `method_40983`。该回调清空旧鱼种后选择新鱼种，进入新目标显示动画。

普通补位流程为：

```text
某槽三鱼满足完成检查
→ 旧目标回收/播放动画
→ 选择新目标鱼种
→ 显示新目标
→ 自动取暂存中同种鱼填入
→ 继续检查局面
```

新目标生成动画回调 `Type_05168.method_41026` 明确调用 `TargetItem.method_41007`，后者查找同种暂存鱼并执行 `TempToTarget`。所以暂存区不是孤立存放区，它既影响选鱼，也能在新缸出现后被自动吸收。

证据：[完成检查](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/TargetItem.CheckIsFull.arm64.txt)、[重建目标](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/TargetItem.method_40983.arm64.txt)、[动画后的回调](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05168.method_41026.arm64.txt)、[暂存吸收](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/TargetItem.method_41007.arm64.txt)。

### 4.2 普通目标选择存在版本分支

`TargetItem.method_40973 @ 0x126490c` 检查 `SDKManager.LevelTest`：

```text
LevelTest 为 5 或 6 → Type_05123.method_40343
其他值             → Type_05123.method_40473
```

下面详解的是 5/6 分支。未确定用户设备实际运行时的 LevelTest，也没有把旧分支全部还原。倒计时目标也有独立入口，本轮不展开。

证据：[版本分派](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/TargetItem.method_40973.arm64.txt)、[5/6 主选择](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05123.method_40343.arm64.txt)、[旧分支原生摘录](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05123.method_40473.arm64.txt)。

### 4.3 进度、暂存和当前供给共同决定类别

主选择入口读取关卡难度；`method_40316` 的谓词明确统计 `TempItem.FishItem != null` 的数量，即已占用暂存位，而不是空位。传给难度配置选择函数后，计数限制在 0–4，进度限制在 0–1；使用同难度、同暂存档位且进度阈值覆盖当前进度的第一行。

配置中的进度上界为 0.4、0.65、0.85、1.0。进度分子来自送入目标时递增的计数，不是游戏已经运行的秒数。

之后进行 Order 权重抽样、当前供给重建和成本评分。供给重建还会扣除已有目标的尚欠需求，避免直接把同一批数量全部当作新目标的余量。

证据：[已占暂存位谓词](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05093.method_40543.arm64.txt)、[查表与限制](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05078.method_40180.arm64.txt)、[进度阈值谓词](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05077.method_40193.arm64.txt)、[进度递增](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05123.method_40517.arm64.txt)、[状态统计](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05123.method_40405.arm64.txt)。

### 4.4 Order1–Order5 不是五个固定鱼缸或五种颜色

本轮继续恢复了选择函数的 ARM64 跳表。设：

- `d`：程序为某鱼种计算的三鱼目标供给成本分数；不是经求解器证明的最短操作数。
- `N`：当前空暂存位数，由 `TempItem.FishItem == null` 统计。

五类首先检查以下条件：

| 配置类别 | 原生枚举 | 初次筛选的成本条件 |
|---|---:|---|
| Order1 | 0 | d < 0 |
| Order2 | 1 | d = 0 |
| Order3 | 2 | 1 ≤ d < N−1 |
| Order4 | 3 | d = N−1 |
| Order5 | 4 | d = N 或 d = N+1 |

成本构造会优先消耗三鱼配额中的暂存同种鱼，每用一条计 -1；再尝试普通可用鱼，每用一条在这一分支记 0；不足部分继续考察其它来源和条件。譬如扣除已有目标需求后，1 条同类鱼在暂存、另有 2 条处于普通可用统计内，则这条基础评分路径得 -1。它不是说必须暂存三条才可能进入 Order1。

因此它是在选择不同当前供给成本的鱼种，而不是按“红→黄→蓝”的固定列表出缸。跨类别在某些 N 下可有重叠，不能把这些谓词误当作严格互斥的完整难度标签。

证据：[成本累积](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05123.method_40412.arm64.txt)、[类别选择及跳表](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05123.method_40446.arm64.txt)、[空暂存位谓词](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05093.method_40544.arm64.txt)。

### 4.5 权重趋势是真实的，但不是最终概率保证

基础难度 1、进度不超过 0.4 的原始表：

| 已占暂存位（查表档位） | Order1 | Order2 | Order3 | Order4 | Order5 |
|---:|---:|---:|---:|---:|---:|
| 0 | 0% | 70% | 30% | 0% | 0% |
| 1 | 40% | 50% | 10% | 0% | 0% |
| 2 | 60% | 30% | 10% | 0% | 0% |
| 3 | 80% | 20% | 0% | 0% | 0% |
| 4及以上 | 90% | 10% | 0% | 0% | 0% |

这里可以读出：在这组配置下，占用越多，Order1 的抽样权重越高。不能写成“有 90% 保证立刻清空暂存”，因为还取决于该类候选是否存在，以及下述覆盖和回退。

证据：[原始难度配置](sandbox:/mnt/data/fishsort_order_analysis/delivery/derived/LevelDifficultyConfig.json)、[权重抽样实现](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/LevelDifficultyConfig.method_40176.arm64.txt)。

### 4.6 优先覆盖、避免重复及回退必须保留

主入口在成本计算后先调用 `method_40404(out fish)`；满足其条件时直接返回该鱼种，覆盖刚抽中的 Order。这个分支读取额外标志、进度区间和次数等条件，本轮没有全部恢复这些参数的设定来源，不能把它草率命名成“保证救场”或“故意卡关”。

普通类别筛选常先避开现有目标的鱼种；部分分支第二遍允许重复，因此不能说任何时候都禁止两个缸同种。

类别不足的外层回退 `method_40336`：

```text
抽中 Order1：尝试 1 → 2 → 3 → 4 → 5 → 全局兜底
抽中 Order2：先尝试 2；
            进度 ≤ 0.4 时再尝试 1；
            进度 > 0.4 时再尝试 3 → 4 → 5；之后全局兜底
抽中 Order3：3 → 2 → 1 → 全局兜底
抽中 Order4：4 → 3 → 2 → 1 → 全局兜底
抽中 Order5：5 → 4 → 3 → 2 → 1 → 全局兜底
```

这是外层调用序列；类别函数内部还有重复候选等处理。不要把类别初始权重直接当成最终各鱼种出现概率。

证据：[优先覆盖](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05123.method_40404.arm64.txt)、[外层回退](sandbox:/mnt/data/fishsort_order_analysis/delivery/native/Type_05123.method_40336.arm64.txt)。

## 5. 对制作关卡的含义

以下是依据上述代码的设计推断，而非恢复出的作者意图。

无机制内容层至少需要同时看三个变量：**队列前缀的同类鱼量、物理供给窗口、目标选择策略**。

初始前十泡的多数鱼种控制开局目标，而第 1/3/4 等具体分布决定该种三鱼配额多早在供给中出现。后段鱼种并非永远不可选，但不能只凭其全关总数判断当前目标是否容易完成。

玩家先取哪种鱼、留下哪些未清空气泡、往暂存里存什么，会改变当前供给、空位和完成事件时间，从而改变下一次目标选择输入。同样的装鱼骨架并不对应唯一的点击序列或唯一的完整鱼缸序列。

若要验证一种无机制复刻，至少要同时记录每次生成的队列索引、场上鱼与 ShowFishPos 的关系、可点击集合、各缸需求、暂存内容、选中类别及回退结果。仅重放原始 JSON 无法验证完整体验等价。

## 6. 本次验证与边界

本报告来自上传安装包的静态 IL2CPP 分析，以及已提取配置的离线计数。没有运行 Android 游戏、没有录屏逐帧核对，也没有做原生方法与重建脚本的等价测试。

随包 48 份原生摘录，3 份纯骨架，初始候选推演脚本与结果。原生片段边界使用下一已知方法起点，注释是辅助阅读，不代表已完成完整控制流反编译。三个骨架的开局组合及前缀计数有脚本断言检查；五类成本比较的跳表地址也附在检查记录。

尚未确认：场景生成点的实际坐标和数量、首屏精确可点集合、原生字典并列枚举顺序、设备实际 LevelTest、旧选择分支及优先覆盖参数的完整业务语义。**这些缺口不影响已核实的队列供给、初始前十泡最大数量选择、以及后续按局面补缸的主结论。**

校验记录：[delivery_checks.json](sandbox:/mnt/data/fishsort_order_analysis/delivery/derived/delivery_checks.json)。
