# 外部配置参考

核对日期：2026-09-20。只用于原生宿主配置字段的事实核对，不把文档存在当作本机实跑。

OpenAI 官方 Subagents 文档：自定义 Agent 必需字段为 name、description、developer_instructions；可使用 model、model_reasoning_effort、sandbox_mode。未设置模型/effort 时有继承行为，显式派发及其他层配置也会影响实际运行。

```text
https://developers.openai.com/codex/subagents
（本次读取重定向到 https://learn.chatgpt.com/docs/agent-configuration/subagents ）
```

OpenAI 官方 Config basics：宿主有命令行、项目、用户等配置层；项目配置还受项目信任和管理策略影响。

```text
https://developers.openai.com/codex/config-basic
（本次读取重定向到 https://learn.chatgpt.com/docs/config-file/config-basic ）
```

`models.toml`、enabled 标志和本地 Dashboard 是本 Harness 的功能，不是宣称 Codex 原生自动加载同名文件。同步和记录通过工具实现；实际模型可用性、effort 支持、宿主重载与真实消费没有在本环境验证。
