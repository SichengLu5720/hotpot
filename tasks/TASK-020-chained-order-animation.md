# Chained Order Animation

Task ID: TASK-020
Status: Review

## Current Change

- 用户反馈暂存满足新订单时连续换单太快，锅和订单牌来回跳。
- 已定位核心同步结算A完成→B创建/暂存填满→B完成→C创建，表现队列出锅A时渲染最终C，随后播放B形成C→B→C。
- 采用同锅位按订单身份串行显示：入锅到达→出锅→下单。保持既有飞行时长、核心即时结算、库存奖励、视觉资产及交互规则。

## Work Packages

- Work Package ID: CHAIN-PRESENTATION
- Goal: 消除最终快照提前显示和重复/提前出锅。
- Spec References: docs/SPEC.md 暂存自动入锅连续完成表现
- Must Preserve: 核心规则/事件结算、.34/.46秒飞行、视觉资产、按压1.5、物理与其他工作。
- Allowed Write Paths: Unity/Assets/HotpotSort/Runtime/Presentation/GameplayFeedback.cs; Unity/Assets/HotpotSort/Runtime/Presentation/GameplayViewV7.cs; Unity/Assets/HotpotSort/Runtime/Presentation/Editor/*ChainOrder*; .harness/qa/TASK-020/**
- Forbidden / Shared Paths: GameplayView.cs/PresentationPort.cs/Core/Bootstrap由ORDER-RELIEF code_agent持有；如需绑定，由PM转交串行集成。
- Additional Allowed Write Path: Unity/Assets/HotpotSort/Runtime/Presentation/Editor/*ServeChain*及配套.meta，用于连续换单诊断。
- Depends On: 既有ViewEvent orderIdentity/ingredientId事件；最终与TASK019串行验证导出。
- Acceptance: A→B→C顺序、目标牌不提前C、食材到达再出锅、重复事件/暂停/新局清理、正常单次流程；Unity编译运行。
- Integrator: visual_agent负责表现文件；code_agent合并必要共享绑定并最终导出。

## Result

- Completed：逐锅位显示中间订单A→B→C，首出锅等待.34秒入锅到达，暂存飞行.46秒保持；对应替换锅落稳后才播放其延迟食材。
- Unity编译和PlayMode11项通过：不同订单身份、到达等待、暂存时序、暂停、重复事件、队列排空、新会话清理。
- .harness/qa/TASK-020/chain-order.log及chain-B-entering.png、chain-B-serving.png、chain-C-settled.png记录合成场景运行证据，非真机验收。
- 与TASK019串行集成导出成功并打开开发者工具，新包20260924T085628-4252a698e8d04aa3b16cf739023669ac；旧版显示分支也接入DisplayOrder。未上传发布，待用户体验。
