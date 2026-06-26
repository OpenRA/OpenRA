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

# Playbooks

## Change gameplay or add a trait

1. Inspect the relevant mod rules under `mods/<mod>/rules/` and C# trait code under `OpenRA.Mods.Common/Traits/`, `OpenRA.Mods.Cnc/`, or `OpenRA.Mods.D2k/`.
2. Check whether behavior must be synced. If yes, preserve deterministic state and update `ISync` handling if needed.
3. Add or update YAML rules and localization keys as required.
4. Run `make check` for C# and `make test` for YAML.

## Change UI/chrome

1. Inspect affected chrome YAML under `mods/common/chrome/` or `mods/<mod>/chrome/`.
2. Inspect widget logic under `OpenRA.Mods.Common/Widgets/Logic/`.
3. Update Fluent strings under `mods/*/fluent/` if labels/tooltips change.
4. Run `make test`; run `make check` if C# changed.

## Add or change a utility command

1. Inspect `OpenRA.Utility/Program.cs` to understand command discovery.
2. Find existing `IUtilityCommand` implementations for patterns.
3. Keep argument validation explicit.
4. Test through `./utility.sh <mod> <command>` after building.
5. Run `make check` and relevant focused tests.

## Change dedicated server behavior

1. Inspect `OpenRA.Server/Program.cs`, `OpenRA.Game/Server/Server.cs`, and `OpenRA.Game/Settings.cs`.
2. Identify whether the change affects authentication, passwords, bans, profile verification, IP sharing, GeoIP, replay recording, or map validation.
3. Preserve protocol/version checks unless the task explicitly changes them.
4. Run `make check`; add focused tests where available.

## Change networking/order/sync behavior

1. Inspect `OpenRA.Game/Network/Connection.cs`, `OpenRA.Game/Network/OrderManager.cs`, and `OpenRA.Game/World.cs`.
2. Identify synced vs unsynced paths.
3. Avoid nondeterministic inputs in synced logic.
4. Consider replay compatibility and desync reports.
5. Run `make check` and unit tests; add tests if a regression can be isolated.

## Change mod data

1. Inspect `mods/<mod>/mod.yaml` first to understand package/rule/chrome/load order.
2. Update the smallest affected YAML files.
3. Keep localization keys synchronized with Fluent files.
4. Run `make test`; add `make check-scripts` for Lua changes.

## Change build, CI, or packaging

1. Inspect `Directory.Build.props`, `Makefile`, `make.ps1`, `.github/workflows/ci.yml`, and `packaging/functions.sh`.
2. Preserve Unix/Windows parity where applicable.
3. Avoid destructive install/publish commands unless explicitly requested.
4. Run safe local equivalents of changed commands.

## Update AI onboarding docs

1. Determine whether the change affects architecture, commands, tests, security, components, or unknowns.
2. Update `AI_INDEX.md`, `AGENTS.md`, and relevant `.ai/` files.
3. Update `.ai/MANIFEST.json` with the current indexed commit and changed files.
4. Record the change in `.ai/CHANGELOG.md`.
5. Verify JSON validity and README links.
