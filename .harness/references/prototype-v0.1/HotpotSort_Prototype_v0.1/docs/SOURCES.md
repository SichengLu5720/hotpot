# 来源与实现依据

## 用户提供和此前已交付的分析

本原型的容量来自用户提供的实机截图：5 个暂存格、2 个开放目标、4 个目标位置、每单显示 0/3。火锅包装和 Bubble→盘子为用户确定方向。

A/B/C 来自 `shared/reference/skeleton_A.json`、`skeleton_B.json`、`skeleton_C.json`。来源关号和哈希见 `shared/provenance.json`。60 行难度表保存在 `shared/reference/LevelDifficultyConfig.json`。

`docs/reference/` 是此前的分析报告副本。副本中的历史 sandbox 引用保留了原会话路径；完整原生反汇编未重复塞入本工程，且它们不是游戏运行依赖。无法通过本包单独重新证明全部原包行为，因此 SPEC 继续保留“静态依据/替代实现/待确认”的区分。

## 新编写的部分

全部 Unity C#、网页 JavaScript/CSS/HTML、圆形物理、自动测试、生成/导出工具与占位美术均在本次交付中重新编写。它们没有使用反编译方法体占位异常来冒充实现。

本次目标成本、物理参数、并列顺序、回退策略和失败时点是清楚标明的原型实现，不来自某个已完全恢复的原包源码。

## Unity API 核对

实施时核对了 Unity 官方 2022.3 文档中的运行时初始化、即时 GUI、纹理绘制、JSON 数据和操作系统字体 API。相关文档路径：

```text
https://docs.unity3d.com/2022.3/Documentation/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html
https://docs.unity3d.com/2022.3/Documentation/ScriptReference/MonoBehaviour.OnGUI.html
https://docs.unity3d.com/2022.3/Documentation/ScriptReference/GUI.DrawTexture.html
https://docs.unity3d.com/2022.3/Documentation/ScriptReference/JsonUtility.html
https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Font.CreateDynamicFontFromOSFont.html
```

API 核对不等于工程编译测试。Unity 本次状态为未编译、未进入 Editor、未构建。

## 项目版本

项目版本文件采用 `2022.3.62f2`，不是 `62f1`。Unity 官方安全公告把 `2022.3.62f2` 列为 2022.3 LTS 对 CVE-2025-59489 的修正版，changeset 为 `6896052288fd`。这只是所选目标版本的依据，不是“目前最新版本”或“没有其他问题”的保证。

```text
https://unity.com/security/sept-2025-01
```
