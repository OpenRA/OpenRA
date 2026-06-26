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

# Components

## `OpenRA.Game`

- Responsibility: core engine runtime, settings, mod discovery/loading, world simulation, networking, server implementation, rendering/sound integration, utility abstractions.
- Key files: `OpenRA.Game/Game.cs`, `OpenRA.Game/World.cs`, `OpenRA.Game/Settings.cs`, `OpenRA.Game/Network/`, `OpenRA.Game/Server/`.
- Public interfaces: engine runtime APIs, `IConnection`, `IUtilityCommand`, trait/world systems.
- Risks: deterministic simulation, networking protocol compatibility, static runtime state, replay/sync behavior.
- Tests: `OpenRA.Test` plus build/style checks.

## `OpenRA.Launcher`

- Responsibility: interactive game executable.
- Key files: `OpenRA.Launcher/Program.cs`, `OpenRA.Launcher/OpenRA.Launcher.csproj`.
- Public interface: command-line args passed to `Game.InitializeAndRun`.
- Risks: exception handling, log flushing, mod arguments, platform launch behavior.

## `OpenRA.Server`

- Responsibility: dedicated server process wrapper.
- Key files: `OpenRA.Server/Program.cs`, `OpenRA.Server/OpenRA.Server.csproj`.
- Public interface: command-line args including required `Game.Mod`; environment `MOD_SEARCH_PATHS`.
- Dependencies: `OpenRA.Game` server implementation.
- Risks: network listen endpoints, server lifecycle, support directory settings, log files.

## `OpenRA.Utility`

- Responsibility: mod-aware command-line utility host.
- Key files: `OpenRA.Utility/Program.cs`, `utility.sh`, `utility.cmd`.
- Public interface: `OpenRA.Utility.exe [MOD] [COMMAND] ...`.
- Dependencies: `OpenRA.Game`, mod-provided `IUtilityCommand` implementations.
- Risks: command discovery, argument validation, file-writing utility commands.

## `OpenRA.Mods.Common`

- Responsibility: shared official mod code: traits, commands, widget logic, gameplay systems, utility logic.
- Key files: `OpenRA.Mods.Common/OpenRA.Mods.Common.csproj`, `OpenRA.Mods.Common/Traits/`, `OpenRA.Mods.Common/Widgets/Logic/`.
- Dependencies: `OpenRA.Game` plus NuGet packages for mod/game features.
- Risks: shared behavior affects multiple official mods; trait changes can affect deterministic simulation.

## `OpenRA.Mods.Cnc` and `OpenRA.Mods.D2k`

- Responsibility: official mod-specific C# assemblies.
- Key files: project files and mod-specific source trees.
- Dependencies: `OpenRA.Game`, `OpenRA.Mods.Common`.
- Risks: changes may require matching YAML/rules/localization updates under `mods/`.

## `mods/`

- Responsibility: data-driven mod manifests, packages, rules, weapons, sequences, chrome, hotkeys, maps, Lua scripts, and localization.
- Key files: `mods/*/mod.yaml`, `mods/*/rules/`, `mods/*/chrome/`, `mods/*/fluent/`, `mods/*/maps/`, `mods/*/scripts/`.
- Public interfaces: mod IDs such as `ra`, `cnc`, `d2k`, `ts`; manifest-defined assemblies and runtime data.
- Risks: YAML validation, asset availability, localization keys, scripted mission behavior.
- Tests: `make test`, `make check-scripts`.

## `OpenRA.Platforms.Default`

- Responsibility: default platform integration.
- Key files: `OpenRA.Platforms.Default/OpenRA.Platforms.Default.csproj`.
- External dependencies: OpenRA wrapper packages for FreeType, OpenAL, SDL2.
- Risks: native library availability and target platform selection.

## `OpenRA.Test`

- Responsibility: NUnit tests.
- Key files: `OpenRA.Test/OpenRA.Test.csproj`.
- Dependencies: `OpenRA.Game`, `OpenRA.Mods.Common`, NUnit packages.
- Commands: `make tests`, `./make.ps1 tests`.

## Build, CI, and packaging

- Responsibility: restore/build/check/test/install/release workflows.
- Key files: `Directory.Build.props`, `Makefile`, `make.ps1`, `.github/workflows/ci.yml`, `packaging/functions.sh`.
- Risks: platform-specific target selection, analyzer behavior, native dependencies, install destinations, CI parity.
