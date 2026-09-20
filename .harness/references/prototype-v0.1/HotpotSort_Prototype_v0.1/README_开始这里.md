# 开锅啦 · 食材整理原型 v0.1

**交付：SPEC 草稿 + 完整重写原型源文件。不是 Fish Sort 原始 Unity 源工程，也不是反编译出来的空脚本。**

本版将 Bubble 换成盘子、Fish 换成食材。保留 5 格暂存、初始 2 个订单、4 个目标位置、每单 3 件同类食材；不包含冻结、锁、隐藏、烹饪、火候、顾客耐心、广告、付费或复活。

## 直接试玩

打开 `Web/standalone.html`。这是包含所有 JavaScript、数据和样式的独立 HTML，游戏本身不需要安装依赖、不需要登录、不需要联网。某些聊天预览器不执行 JavaScript，需要把文件保存到本机后用浏览器打开。

也可以打开 `Web/index.html`，保留相邻 `js/`、`css/` 目录即可。受管理的浏览器可能禁止本地文件，届时在工程根目录执行：

```bash
python tools/serve.py --port 8080
```

再打开终端输出的本机地址。服务只绑定 `127.0.0.1`，不是公网部署。

**默认进入骨架 A、种子 7。** 点击盘中单件食材；有匹配订单时优先进入订单，否则进入暂存。订单收满三件后独立换单，并自动吸收暂存中的同类食材。按右上角暂停按钮切换 A/B/C/练习、种子和失败时点。

`Esc` 暂停/继续，`R` 重开同一配置，`D` 显示调试信息。桌面侧栏和手机暂停菜单均能导出 JSON 日志。扩展订单位只展示，不会解锁。

## Unity 工程

`Unity/` 是针对 **Unity 2022.3** 编写的独立工程，项目版本文件使用 `2022.3.62f2`。版本采用官方公布的安全修正版本，而不是旧的 62f1。它不是原包的 `2022.3.62f3c1` 工程副本。没有使用第三方收费插件、原包 SDK 或原包美术。

用 Unity Hub 添加 `Unity/` 目录。编辑器完成导入后，打开 `Assets/HotpotSort/Scenes/Boot.unity`，执行菜单 `Hotpot > Apply Portrait Settings`，然后进入 Play。场景已经包含入口组件，无需自己挂脚本、绑对象或拼 Prefab。

运行 `Hotpot > Run Core Validation` 会在本机执行 11 组与网页实现对应的逻辑回放样本，并执行四组物理冒烟检查。只有本机执行成功后，才会写出 `qa/unity-validation.json`。

选择已经安装构建支持的目标平台，再执行 `Hotpot > Build Current Target`，可触发开发构建；该命令先执行上述校验。**本次没有提供预编译 APK、EXE 或 Unity WebGL 构建。**

Unity 菜单内使用 A/B/C/练习按钮、种子加减、失败规则切换。桌面/安卓日志写入 `Application.persistentDataPath`，具体路径输出到 Console；WebGL 构建含日志下载桥接源码。Unity 对应平台构建及这些原生平台流程尚未在本环境验证。

## 已验证与未验证

| 项目 | 本次实际状态 |
|---|---|
| JavaScript 核心与自定义物理 | 29 项测试通过；其中包含 180 个种子化随机操作样本、80 个全可见逻辑清盘样本 |
| 保留供给高度限制的自动点击 | A/B/C/练习各 3 个种子，共 12 局完成；使用真实命中检测，不直接清空数据 |
| 浏览器 UI | 15 项检查通过，包括实际指针点击、五格失败、完整 A 局胜利、日志下载、390×844 触屏命中 |
| 网页截图 | `qa/browser_game.png` 与 `qa/browser_mobile.png` 为本次运行画面 |
| Unity C# / Editor / 原生平台 | **未编译、未打开 Editor、未构建、未做安卓/iOS 真机测试**；本环境没有 Unity 或 C# 编译器 |
| 原版等价与全局可解性 | **未验证；不能据此承诺原版难度或全部种子可解** |

浏览器测试使用 Headless Chromium 将单文件 HTML 注入页面执行；本环境限制本地文件和 localhost 导航。因此，“HTML/JS 已执行”与“本环境双击文件导航已测试”不是同一项结论。

## 源码入口

```text
SPEC_v0.1_草稿.md                 玩法与实现约定
shared/game-data.json            四套内容与规则、60 行目标权重
Web/js/core.js                   纯逻辑、订单、暂存、守恒、回放
Web/js/physics.js                队列供给与圆形盘子堆积
Web/js/game.js                   交互、动画、HUD、菜单、调试
Web/js/art.js                    新绘制的程序化食材/盘子源代码
Unity/Assets/HotpotSort/Runtime/
  Core/GameData.cs               数据、状态、随机数与回放结构
  Core/GameSession.cs            C# 玩法逻辑与目标选择
  Core/PlateWorld.cs             C# 圆形盘子堆积与命中
  View/HotpotApp.cs              完整运行入口与交互/UI
  View/PrimitiveArt.cs           程序化 UI 与贴图绘制
Unity/Assets/HotpotSort/Editor/   配置、验证、构建菜单
Unity/Assets/HotpotSort/Resources/Hotpot/
                                JSON、回放样本、16 张食材与盘子贴图
```

修改公共数据后执行：

```bash
python tools/sync_data.py
python tools/build_standalone.py
node tests/test_core.cjs
```

可选浏览器回归测试：

```bash
python -m pip install playwright
python -m playwright install chromium
python tests/test_browser.py
```

可选美术贴图再导出：

```bash
python tools/export_art.py
```

以上 Python 依赖只用于测试或导出，不是游戏运行依赖。`tools/prepare_data.py` 可以从随包保留的原始分析样本重新生成初始数据；它会覆盖 `shared/game-data.json`，不应用于覆盖已经编辑的新关卡。

## 最重要的三个草案决定

默认在第 5 格占满后先进行逻辑结算，再判负；暂停菜单另提供“下一件需要暂存却无空位时判负”的对比开关。此处原版尚未确认。

后续目标使用已知“进度 × 暂存占用 × 类别权重”的结构，但成本、候选回退、并列处理是本版明确的新实现，不是原方法体的逐行复刻。

本版用圆形盘子堆积近似原版空间供给，不做整盘搬运、不强制先清前一盘，也不把全部关卡一次性铺开。

详见 `SPEC_v0.1_草稿.md`、`docs/DECISIONS.md` 和 `docs/QA_CHECKLIST.md`。
