# Harness 可复用组件

来源：群岛 TASK-008 微信好友排行榜、TASK-006 开发调参看板。
当前 Windows 看板直接编译 Core 下的源码；导出包与看板使用同一份规则。
源码无第三方包依赖。Unity 侧依赖 UnityEngine；微信侧另需项目已有的微信小游戏 SDK。
两个子页面右下角可分别导出源码 ZIP。Core 文件直接复制到工程，无需安装本看板。

## 勾选后导入 Codex 自动交付

主窗口默认打开「组件接入」页：选择目标项目 → 勾选排行榜和/或开发者控制台 → 点击「导入 Codex 并自动交付」。
程序自动发现本机 Codex，使用已有登录与默认模型启动 `codex exec`；无需手动开浏览器、复制提示词或配置 API Key。
每个组件生成独立请求单 `tasks/imports/TASK-日期-批次-组件.md`，避免与已有 Harness 机器 Task 混淆。
目标项目使用 Harness 时，Codex 按该项目约束建立正式 Task，再回写关联路径。尚无 Git 基线或引擎时可能阻塞，不会擅自提交或创建游戏工程。
素材、提示词、事件日志、进程状态和交付结果保存在 `.harness/component-imports/批次/`。
Codex 会复用源码、连接项目真实入口和数据、编译并产出 DELIVERY.md。界面依据结构化结果显示交付/阻塞/未完成，不把进程退出等同于成功。
用户可停止任务；停止会终止本次进程树并保留已有修改。运行时请保持窗口开启或最小化。
本地同一项目的此类接入任务串行执行。若有其他正在写该项目的工作，应在其完成后启动。
模型配置页仍配置 Harness Subagent；自动接入的主 Codex 会话继承用户当前默认模型，实际调用角色由项目工作流决定。
这里使用 Codex CLI 持久化会话；不保证自动加入桌面应用的任务侧栏。界面显示会话 ID，完整日志保存在交付目录。
本实现未为验证按钮启动付费模型任务；是否真实完成游戏适配，由用户点击后的实际执行报告确定。
官方非交互执行说明：https://learn.chatgpt.com/docs/non-interactive-mode

## 1. 排行榜：自有后端 / 本地数据

复制 Core/Leaderboard.cs；Unity 项目再复制 Unity/LeaderboardOverlay.cs。
实现 ILeaderboardProvider.LoadAsync 一个方法，返回稳定 ID、昵称、成绩、首次达成毫秒时间。
界面会处理排序、加载、失败、超时和重试；关闭/新请求后迟到结果失效。

```csharp
using Harness.Reusable;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// 直接可编译的接入示例：把 Fetch 换成你的服务函数。
public sealed class GameRankingProvider : ILeaderboardProvider
{
    public System.Func<CancellationToken, Task<IReadOnlyList<ScoreEntry>>> Fetch;
    public Task<IReadOnlyList<ScoreEntry>> LoadAsync(CancellationToken cancel)
        => Fetch(cancel);
}
// Unity: 给对象添加 LeaderboardOverlay 后调用
// overlay.Bind(new GameRankingProvider { Fetch = MyApi.FetchRanking }, accountId);
// 普通 C#: new LeaderboardController(provider, accountId); await controller.RefreshAsync();
```

成绩高者优先，同分按首次达成时间、稳定 ID 排序；名次为顺序名次。
LeaderboardRules.Merge(saved, incoming) 可保留最高成绩和首次时间。
重复 ID 被拒绝；不使用昵称或头像识别本人。数据源未包含本人时不伪造名次。
Core 支持非负 long 成绩；微信适配保留原协议的 completedCount 名称及默认 20 上限。
服务端仍需校验成绩与处理多设备并发；本库的本地合并不等于云端原子事务。

## 2. 微信好友榜：现成单文件开放数据域

微信好友数据不得传回 Unity/桌面进程；不要使用上面的 Provider 读取微信好友榜。

1. 复制 WeChat/Unity/WeChatFriendBoard.cs 到 Unity 项目，添加到对象。
2. 使用已有微信小游戏 SDK，微信目标定义 WECHAT_MINIGAME。
3. 复制 WeChat/open-data 文件夹到小游戏导出根目录，合并 game.json 的字段：
   "openDataContext": "open-data"
   保留 game.json 其他配置。构建导出后需重复部署，或加入项目的导出钩子。
4. 内置主线真实通关时调用 board.RecordBest(completedCount)。失败、重玩、每日挑战、自定义关卡不应冒充主线进度。
5. 打开时 board.Open(pixelRect)，关闭 board.Close()；安全区或分辨率变化后 board.Relayout(pixelRect)。
   pixelRect 使用 GUI 左上原点的实际屏幕像素。GUI.matrix 保持单位矩阵，避免重复缩放。
6. 游戏输入路由在榜单打开期间拦截底层操作。微信开放域自行处理榜内滚动/重试。

默认 projectKey=my-game（本地存储隔离），protocol=harness-friends-v1，云字段 harness_campaign_rank_v1。
默认通关上限 maximumScore=20。适配另一个项目时修改组件 projectKey；同一个 AppID 中使用多个榜单时也要换云字段。
如果修改上限、云字段或消息类型，执行以下命令再复制产物，C# 中 maximumScore/protocol 必须一致：

```text
python WeChat/build_open_data.py --cloud-key your_game_rank_v1 --protocol your-game-friends-v1 --maximum-score 100
```

导出包已经包含默认生成的 open-data/index.js，可直接使用；不会要求子域加载 require('./model')。
Source 中保留排序、单调合并、本人 marker 识别、先读后写、超时、未决写入保护、字符串消息解码、头像降级、滚动与重试实现。
Unity 显示共享画布使用 Y 轴 UV 翻转，保留源项目的设备方向修复。
新增 close 指令使关闭立即失效旧查询。旧存档迁移由项目显式实现，避免把当前关卡误算为已完成关数。

本包未连接真实好友数据；来源 Task 的设备验收仍有待确认项。迁移后应确认好友读写、共享画布、触摸与前后台。

## 3. 开发者控制台：自定义想调试的参数

在「开发者控制台」子页点击“添加参数”，填写名称、作用说明、默认值、最小值、最大值、步长。
例如“圆球大小 / 玩家圆球视觉缩放倍率 / 1 / 0.1 / 3 / 0.01”，
“特效速度 / 命中特效粒子播放速度倍率 / 1 / 0.1 / 5 / 0.01”。
不默认添加行走速度、平台或人物参数。可用“填入圆球 / 特效示例”快速添加后修改。
名称必填；对象说明可以留空，Codex 会按名称检查项目，有歧义时报告具体待确认项。
保存的列表在 .harness/custom-console-parameters.json；导入时自动校验、保存并写入 request.json、Task 和源码包。
每个参数保留稳定 key；重命名不改变 key，删除后重新添加会生成新的 key。

自动接入：回到「组件接入」勾选控制台，点击导入。Codex 按参数名称/说明查找真实字段或对象，
生成实际绑定，并在交付报告列出每项参数与代码/场景对象的映射。

手动接入：复制 Core/DeveloperConsole.cs、Unity/UnityParameterStore.cs、Unity/DeveloperConsoleOverlay.cs
和导出时生成的 Unity/DeveloperConsoleParameters.json。添加 DeveloperConsoleOverlay 并把 JSON 绑定到 parameterConfiguration。
配置 bindings，每个 key 对应一个真实绑定：
- VisualScale：绑定纯可见子节点 visual，数值是相对初始 localScale 的倍率。
- ParticleSpeed：绑定 particles，数值是相对初始 main.simulationSpeed 的倍率。
- Custom：通过 Inspector 的 apply 事件绑定接受 float 的项目方法，数值直接传入该方法。

缩放碰撞体、物理效果、速度单位等业务含义由参数说明决定；不应盲目缩放 Collider 根节点或修改 Time.timeScale。
配置不完整会显示错误，不把没有实际绑定的滑块视为交付完成。
面板按自定义列表动态生成滑块，支持滚动、即时应用、显式保存和撤销。

独立于 Unity UI 使用：
```csharp
var parameters = new[] {
    new Harness.Reusable.ParameterDefinition("ball_size", "圆球大小", .1, 3, .01, 1, 1),
    new Harness.Reusable.ParameterDefinition("effect_speed", "特效速度", .1, 5, .01, 1, 1)
};
var tuning = new Harness.Reusable.DeveloperConsole(true, parameters, myStore);
tuning.Changed += () => ApplyToGame(tuning.Get("ball_size"), tuning.Get("effect_speed"));
tuning.Set("ball_size", 1.5);
tuning.Save();    // 只有显式保存才写持久化
tuning.Discard(); // 撤销未保存修改
```

Unity 组件使用 UNITY_EDITOR || DEVELOPMENT_BUILD 门控；正式版不读取配置、不绘制面板、不调用调试绑定。
Core 的 DefaultParameters 仅保留为旧接口兼容，新面板和自动接入不使用该固定列表。

## 输入与生命周期接入

Unity 示例使用轻量 IMGUI，无需 prefab。BlocksPointer(Input.mousePosition) 提供覆盖区域判断。
在游戏输入系统开始路由前调用，并在起点被拦截后捕获整个手势直至全部触点松开/取消，防止拖出面板穿透。
项目已用 uGUI/UI Toolkit 时可只复用 Core，将各 Set/Save/Discard 方法绑定现有控件。
示例 Overlay 的布局与触摸密度需按目标设备适配；未承诺原游戏 0 B/frame 性能指标。
LeaderboardOverlay 被禁用时释放控制器，重新显示后再次 Bind；WeChatFriendBoard 关闭会释放显示，销毁会释放纹理。

## 文件与构建

Core 为 C# 共享业务逻辑；Unity 为直接可挂载的接入层；WeChat 为开放域与 Unity SDK 桥接。
provenance.json 记录来源路径、Git 基线、提取时的文件摘要及已知限制。当前源仓库包含未提交修改，摘要指向提取快照，不声称等于该 Git 提交。
这里只提取代码到当前看板工程，未修改 D:\群岛-harness。
新窗口源码：tools/native-dashboard；构建输出仍为根目录 HarnessModelDashboard.exe。
