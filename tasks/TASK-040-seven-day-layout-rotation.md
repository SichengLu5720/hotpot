# Seven-day Layout Rotation

Task ID: TASK-040  
Status: Working  
Updated At: 2026-09-28

## Current Change

- 制作七组配对热身/正式布局和内容校验，运行时日期选择留给 Integrator。

## Work Package

Work Package ID: WP-040-CODE  
Goal: 提供七组结构合法、同一难度带的版本化布局内容。  
Spec References: `Rules and Data`  
Must Preserve: 正式 50盘/183份/61单；热身18份/3类各6份/6单及教程可点击。  
Allowed Write Paths: `Unity/Assets/HotpotSort/Content/Daily/**`; `Unity/Assets/HotpotSort/Editor/ContentImport/**`; 内容模型中仅布局格式所需文件；布局诊断。  
Forbidden / Shared Paths: Bootstrap、Profile、Presentation、SPEC、其他 Task。  
Depends On: None  
Acceptance: 七组数量、符号库存、盘型范围、开局合法、教程目标和内容摘要均通过。  
Integrator: `code_agent`

## Result

- Status: Review
- Work Package: Completed
- Commits: `fbeb639`, `beb617f`
- Implemented: 保留骨架 C，并新增六套随机化正式布局及七套配套热身布局；扩展导入、摘要和结构校验。
- Checks: 七套正式布局均为 50 盘、183 份、61 单；七套热身均为 18 份、3 类各 6 份、6 单且教程目标开局可点击；14 张 1080×1920 代表截图通过可见性检查。
