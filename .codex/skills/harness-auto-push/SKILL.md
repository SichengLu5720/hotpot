---
name: harness-auto-push
description: Create a scoped Git commit and push for an explicitly authorized Harness change, using a dedicated branch when requested and refusing unsafe history rewrites.
---

# Harness Auto Push

Use this skill only when the user explicitly asks to commit, push, or automatically push a completed Harness change. It does not grant permission to publish, release, deploy, force-push, or modify unrelated files.

## Workflow

1. Inspect the current branch, remote, worktree status, and the exact files in scope. Preserve unrelated user changes; do not reset, stash, clean, or overwrite them.
2. If the user requests a branch, create or switch to a dedicated branch before committing. Use a lowercase descriptive name such as `feat/harness-auto-push`; never invent a requested branch name that was not provided. Do not commit feature work directly to `main` when a branch was requested.
3. Run the smallest meaningful validation for the change. For Harness configuration, parse TOML, run `git diff --check`, and run the relevant Harness checks. Do not claim that a game build, device check, release, or deployment passed unless it was actually run.
4. Stage only the scoped files. Review the staged name/status and diff before committing. Use a concise imperative commit message that describes the change.
5. Fetch the target remote before pushing. If the remote branch has commits not present locally, stop and report the divergence; do not rebase, merge, or overwrite it without a new user decision.
6. Push with a normal non-force command, using `-u` for a new branch. Never use `--force`, delete remote branches, create tags, or publish releases as part of this skill.
7. Verify the final local and remote commit IDs and report the branch, commit, push result, validation evidence, and any remaining manual or device checks.

For this repository, keep the Harness model configuration source and agent model fields synchronized through the repository's Harness commands. A saved model configuration is not permission to alter unrelated product tasks, release records, or host approval settings.
