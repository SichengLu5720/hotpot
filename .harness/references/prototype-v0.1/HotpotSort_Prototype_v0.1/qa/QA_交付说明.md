# v0.1 交付检查结论

JavaScript 核心/物理检查：**29 项通过，0 失败**。浏览器交互检查：**15 项通过，0 失败**。文件完整性检查：**12 项通过**。原始结果与具体用例均在相邻 JSON 文件内。

完整 A 局经浏览器真实 PointerEvent 路径达到胜利；移动视口使用触屏事件验证坐标转换；真实导出并解析过一份 JSON 日志。运行截图见 `browser_game.png`、`browser_desktop.png`、`browser_mobile.png`。

保留队列供给、高度限制和命中检测的自动点击脚本，在 A/B/C/练习分别测试种子 7、13、19，共 12 局完成。**这不等于所有种子可解，也不等于达到原版难度。**

这 12 个样本的暂存峰值均未超过 3/5，其中 5 个样本完全没使用暂存；说明这版在这些样本里的压力不强，应先作为闭环验证原型，而非已调好的难度版本。完整逐局数据见 `core-tests.json` 的 `physicalRuns`。

Unity：提供了完整 C# 实现、入口场景、数据、美术、稳定 meta、构建菜单和 11 个 golden 对照样本，但**本环境没有 Unity/C# 编译器，未执行编译、Editor Play、原生构建或真机验证**。文件完整性检查不是编译检查。`unity-validation.json` 刻意不预先生成，必须在用户本机真正运行 `Hotpot > Run Core Validation` 后才产生。

浏览器环境禁止 file/localhost 导航，本次通过 Playwright 将独立 HTML 注入 Headless Chromium 页面执行。因此不宣称已经在本环境验证“双击打开的导航动作”；已执行的是单文件中的实际 UI、脚本、物理和输入流程。
