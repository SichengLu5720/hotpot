# Concurrent Order Settlement

Task ID: TASK-049
Status: Review

## Current Change

用户明确要求删除订单完成虚线（当前斜划痕）并直接实装；保留铜夹纸单、28px/0.30秒入场、旧纸离场和即时操作。随后进入算法讨论，不擅自修改算法。

## Work Packages

Work Package ID: WP-049-NO-MARK
Goal: 移除完成划线显示与不再使用的随机偏角绑定，验证实装。
Spec References: SPEC订单牌规则。
Must Preserve: 落点、落定幅度与时长、旧纸离场、核心结算、多单并发和可操作、其他未提交修改。
Allowed Write Paths: Unity/Assets/HotpotSort/Runtime/Presentation/GameplayViewOrderSign.cs; Unity/Assets/HotpotSort/Runtime/Presentation/GameplayFeedback.cs; Unity/Assets/HotpotSort/Runtime/Presentation/Editor/ChainOrderDiagnostic.cs; .harness/qa/TASK-049/copper-order-r004/**。
Forbidden / Shared Paths: Core、其他无关文件；SPEC/Task仅PM写。
Depends On: 当前铜夹纸单实现及r002身份一致诊断。
Acceptance: Unity编译运行、旧新单均无划线，身份与并发保持，运行截图验证。
Integrator: visual_agent实施与验证，PM复核。

## Result

Completed：已移除完成划线显示、纹理加载、随机角度和参数，无替代标记。28px/0.30秒新纸入场、夹口落定、旧纸弧线渐隐与即时操作保持。Unity编译运行及no-mark.log 38项检查通过，PM已核对日志及无划线截图，证据位于.harness/qa/TASK-049/copper-order-r004/。未改算法、提交或上传；当前实现可供用户体验。
