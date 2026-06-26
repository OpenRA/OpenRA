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

# AI Onboarding Changelog

## 2026-06-26 — bootstrap

Indexed commit: `b0b0544d4adae970fc747e9b01241ef6e80032fc`

### Added

- `AI_INDEX.md` as the root first-read repository map.
- `AGENTS.md` as generic instructions for future AI coding agents.
- `.ai/START_HERE.md` as a reusable first-session prompt.
- `.ai/PROJECT_MAP.md` for repository layout and project/module responsibilities.
- `.ai/ARCHITECTURE.md` for runtime, server, networking, utility, and mod-loading flows.
- `.ai/COMPONENTS.md` for component cards.
- `.ai/COMMANDS.md` for build, run, test, lint, install, and unsupported command notes.
- `.ai/TESTING.md` for validation strategy.
- `.ai/SECURITY.md` for security-sensitive code paths and agent rules.
- `.ai/PLAYBOOKS.md` for repeatable development workflows.
- `.ai/KNOWN_UNKNOWNS.md` for limitations and unresolved questions.
- `.ai/MANIFEST.json` as a machine-readable index.

### README

- Added a vendor-neutral AI-agent onboarding block near the top of `README.md`.

### Source areas used

- `README.md`, `INSTALL.md`, `CONTRIBUTING.md`
- `OpenRA.sln`, `Directory.Build.props`, project files
- `Makefile`, `make.ps1`, `.github/workflows/ci.yml`
- launch and utility wrapper scripts
- `OpenRA.Game/Game.cs`, `OpenRA.Game/World.cs`, `OpenRA.Game/Settings.cs`
- `OpenRA.Game/Network/Connection.cs`, `OpenRA.Game/Network/OrderManager.cs`
- `OpenRA.Game/Server/Server.cs`, `OpenRA.Server/Program.cs`, `OpenRA.Utility/Program.cs`
- `mods/ra/mod.yaml`, `packaging/functions.sh`, `.editorconfig`, `.gitignore`

### Model-specific file migration

No prior model-specific AI files were detected, so no migration, deprecation, preservation, or deletion was performed.

### Known risks

- Requested base branch `main` was not found; `bleed` was used because repository metadata reports it as default.
- Markdown-only changes are ignored by CI, so this documentation-only branch may not run the full workflow automatically.
