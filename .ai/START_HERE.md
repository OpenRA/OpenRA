<!--
AI onboarding file.
Mode: bootstrap
Indexed commit: b0b0544d4adae970fc747e9b01241ef6e80032fc
Last generated: 2026-06-26T11:08:43Z
Generator: generic high-end AI coding agent
Purpose: Help future AI sessions understand this repository quickly.
Audience: Any high-capability AI coding agent, regardless of vendor or model family.
Human edits are allowed. Future refreshes should preserve valid human edits.
-->

# Start Here

## First-session prompt

Paste this into a fresh AI coding session:

```text
You are working in the OpenRA repository. Before making changes, read `AI_INDEX.md`, `AGENTS.md`, `.ai/PROJECT_MAP.md`, `.ai/ARCHITECTURE.md`, `.ai/COMMANDS.md`, `.ai/TESTING.md`, and `.ai/SECURITY.md` as needed for the task.

Treat those files as guidance, not source of truth. Inspect current source files, project files, build scripts, CI, and tests before editing. Separate verified facts, assumptions, inferences, and unknowns. Summarize your understanding and a focused implementation plan before changing files. Keep changes small, source-grounded, and validated. Report changed files and commands actually run.
```

## Reading order

1. `AI_INDEX.md` for the overview and task map.
2. `AGENTS.md` for working rules.
3. `.ai/PROJECT_MAP.md` for file layout.
4. `.ai/ARCHITECTURE.md` for runtime and trust boundaries.
5. `.ai/COMMANDS.md` and `.ai/TESTING.md` before running validation.
6. `.ai/SECURITY.md` before touching server, networking, auth, downloads, packaging, or user data.
7. `.ai/PLAYBOOKS.md` for common workflows.
8. `.ai/KNOWN_UNKNOWNS.md` before making assumptions.

## How to work

- Start from the current task, not from a broad rewrite.
- Inspect task-relevant source files before editing.
- Explain uncertainty explicitly.
- Prefer small patches that preserve existing conventions.
- Validate with task-appropriate commands.
- Do not overload context: read the overview first, then focused source areas.

## Response checklist for future agents

Before finalizing a change, report:

- What changed.
- Files changed.
- Source evidence used.
- Tests or checks run.
- Tests or checks skipped and why.
- Remaining risks or unknowns.

## Non-negotiable source rule

Generated onboarding files may be stale. If they conflict with current source, build scripts, CI, or tests, trust the current repository and update the onboarding files if the discrepancy matters.
