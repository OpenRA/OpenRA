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

# Generic Agent Instructions for OpenRA

## Start every session this way

1. Read `AI_INDEX.md`, then this file, then `.ai/START_HERE.md`.
2. Read the source/config files for the task area before proposing edits.
3. Summarize what is verified, what is inferred, and what is unknown.
4. Plan changes before editing; keep the plan scoped to the smallest safe diff.
5. Validate with the narrowest useful commands first, then broader checks when feasible.

## Source-of-truth order

1. Current source code.
2. Build/test/deployment configuration.
3. CI workflows.
4. Project files and package references.
5. Tests.
6. Current README and docs.
7. Older comments or historical notes.
8. Inference.

When generated onboarding docs disagree with code/config, trust code/config and update these docs in the same PR if the change affects future sessions.

## Repository-specific rules

- Do not edit product/source code for documentation-only tasks.
- Do not add vendor-specific AI instruction files. Keep AI guidance in `AI_INDEX.md`, `AGENTS.md`, and `.ai/` generic files.
- Do not add generated/local outputs ignored by `.gitignore`.
- For C#, follow `.editorconfig`: LF, final newline, trim trailing whitespace, tabs for C# and project/YAML/Lua/script files, no top-level statements, nullable disabled by repo config.
- Preserve deterministic simulation. Be cautious with `SharedRandom`, order processing, `ISync` data, `World.SyncHash()`, and networking changes.
- Preserve mod data loading semantics. Manifest, rules, chrome, Fluent, hotkeys, and scripts are part of runtime behavior.
- Avoid broad rewrites in `OpenRA.Game/Network/`, `OpenRA.Game/Server/`, `OpenRA.Game/World.cs`, packaging scripts, and mod manifests unless the task clearly requires them.

## Planning checklist

Before editing, identify:

- Runtime surface: game, dedicated server, utility, mod data, packaging, or tests.
- Files to inspect first.
- Expected validation commands.
- Risks to determinism, network compatibility, save/replay compatibility, mod loading, or platform packaging.
- Whether docs or AI-onboarding files need updates.

## Validation expectations

Use task-specific validation:

- C# engine or server change: build target plus focused tests; prefer `make check` / `./make.ps1 check` when feasible.
- Unit-test change: `make tests` or `./make.ps1 tests`.
- YAML mod data change: `make test` or `./make.ps1 test`.
- Lua script change: `make check-scripts` or `./make.ps1 check-scripts`.
- Build/CI change: inspect `Makefile`, `make.ps1`, `.github/workflows/ci.yml`; run safe local equivalents where possible.
- Documentation-only change: validate links/JSON/format manually; CI ignores Markdown-only changes.

Never claim a command passed unless it was actually run.

## Commit and PR expectations

- Keep commits focused and descriptive.
- Do not push to the default branch.
- For code changes, include changed files, tests run, and remaining risks in the PR notes.
- For this project, `CONTRIBUTING.md` asks contributors to rebase against `bleed`, add themselves to `AUTHORS` when appropriate, and propose a changelog entry in PR comments.

## Safety rules

- Never store secrets or access tokens in files, logs, comments, manifests, or commit messages.
- Treat server passwords, auth signatures, profile fingerprints, IP addresses, ban/whitelist settings, and replay/network data as sensitive.
- Do not run destructive clean/install/deploy commands unless explicitly requested and safe for the environment.
- Do not run production migrations; this repository has no database migration system detected.

## Refresh policy for AI-onboarding files

Update these files when meaningful changes touch:

- `README.md`, `INSTALL.md`, `CONTRIBUTING.md`
- `OpenRA.sln`, `Directory.Build.props`, project files
- `Makefile`, `make.ps1`, `.github/workflows/**`
- `OpenRA.Game/**`, `OpenRA.Server/**`, `OpenRA.Utility/**`, `OpenRA.Launcher/**`
- `OpenRA.Mods.*/*`, `mods/**`, `packaging/**`, `OpenRA.Test/**`

Refresh should preserve correct human edits, remove stale generated claims, and record changes in `.ai/CHANGELOG.md` and `.ai/MANIFEST.json`.
