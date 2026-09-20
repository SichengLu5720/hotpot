# Release 模板

使用 workspace release 创建。Task 固定快照由 release prepare 生成，QA 计划由 Designer 只读交接，测试脚本由 Builder 写，Runner 执行。

```harness-state
{
  "schema_version": 1,
  "kind": "release",
  "id": "<VERSION>",
  "workspace": "<helper binds workspace>",
  "branch": "<helper binds branch>",
  "base_commit": "<full commit hash>",
  "status": "PREPARING",
  "inputs": [],
  "input_digest": null,
  "integrated_commit": null,
  "prepared": false,
  "qa_plan_handoff": null,
  "qa_paths": ["tests/release", "releases/<VERSION>"],
  "fix_request": null,
  "script_handoff": null,
  "authorization": null,
  "results": [],
  "manual_results": [],
  "block": null,
  "completed": {},
  "runs": {},
  "history": []
}
```

## Publication / Rollback

QA_COMPLETE 不等于已发布。记录人工发布授权、平台、产物摘要、上一可用版本、回滚方式与发布后观察。
