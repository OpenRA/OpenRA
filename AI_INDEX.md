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

# AI Index: OpenRA

## Status

- Repository: `Pummelchen/OpenRA`
- Purpose: Libre/free real-time-strategy game engine for early Westwood-style games.
- Operation mode: `bootstrap`
- Indexed commit: `b0b0544d4adae970fc747e9b01241ef6e80032fc`
- Base branch used: `bleed`
- Requested base branch: `main` (`unknown`: connector did not find it)

## Read first

1. `AI_INDEX.md`
2. `AGENTS.md`
3. `.ai/START_HERE.md`
4. `.ai/PROJECT_MAP.md`
5. `.ai/ARCHITECTURE.md`, `.ai/COMMANDS.md`, `.ai/TESTING.md`, `.ai/SECURITY.md`

These files are guidance. Source code, project files, build scripts, CI, and tests remain the source of truth.

## Verified facts

| Area | Details | Evidence |
|---|---|---|
| Runtime | .NET 8, C# language version 12, nullable disabled, unsafe blocks allowed. | `Directory.Build.props` |
| Solution | Projects include `OpenRA.Game`, launchers, server, utility, common/Cnc/D2k mods, platform adapter, and `OpenRA.Test`. | `OpenRA.sln` |
| Build | Unix uses `Makefile`; Windows uses `make.ps1`; both wrap `dotnet`. | `Makefile`, `make.ps1` |
| Tests | NUnit unit tests, MiniYAML checks, Lua syntax checks. | `OpenRA.Test/OpenRA.Test.csproj`, `Makefile`, `.github/workflows/ci.yml` |
| CI | GitHub Actions runs Linux and Windows .NET 8 jobs; Markdown-only changes are ignored. | `.github/workflows/ci.yml` |
| Mods | Official mod data lives under `mods/`; manifests load YAML rules, chrome, assets, localization, server traits, and mod assemblies. | `mods/ra/mod.yaml` |

## Architecture summary

OpenRA is a modular RTS engine. `OpenRA.Game` contains the runtime, settings, mod loading, networking, server implementation, world simulation, rendering/sound integration, and utility abstractions. `OpenRA.Launcher` starts the game with `Game.InitializeAndRun(args)`. `OpenRA.Server` starts a dedicated server for a required `Game.Mod`. `OpenRA.Utility` loads a mod and dispatches implementations of `IUtilityCommand`.

The game is data-driven through `mods/`. A mod manifest such as `mods/ra/mod.yaml` declares package mounting, rules, sequences, chrome layouts, Fluent messages, weapons, hotkeys, server traits, default order generator, and game speeds.

Networked simulation depends on deterministic order processing. `OrderManager` queues orders by client/frame, sends sync hashes, and detects out-of-sync states. `World.SyncHash()` includes actors, synced trait/effect state, shared RNG state, and relevant player state.

## Directory map

| Path | Responsibility |
|---|---|
| `OpenRA.Game/` | Core engine runtime, settings, world/simulation, networking, server implementation. |
| `OpenRA.Launcher/` | Interactive game executable. |
| `OpenRA.Server/` | Dedicated server executable wrapper. |
| `OpenRA.Utility/` | Mod-aware command-line utility host. |
| `OpenRA.Mods.Common/` | Shared gameplay, chrome, widget, utility, and trait code. |
| `OpenRA.Mods.Cnc/`, `OpenRA.Mods.D2k/` | Official mod-specific C# assemblies. |
| `OpenRA.Platforms.Default/` | Platform integration using SDL2/OpenAL/FreeType wrappers. |
| `OpenRA.Test/` | NUnit tests. |
| `mods/` | Official mod manifests, rules, maps, scripts, chrome, localization, assets. |
| `packaging/` | Install and release helper scripts/assets. |
| `.github/workflows/` | CI configuration. |

## Main commands

| Task | Unix-like | Windows |
|---|---|---|
| Build | `make` | `./make.ps1 all` |
| Build with system native libs | `make TARGETPLATFORM=unix-generic` | `unknown` |
| Run game | `./launch-game.sh Game.Mod=ra` | `launch-game.cmd Game.Mod=ra` |
| Code/style checks | `make check` | `./make.ps1 check` |
| Mod YAML checks | `make test` | `./make.ps1 test` |
| Lua syntax checks | `make check-scripts` | `./make.ps1 check-scripts` |
| Unit tests | `make tests` | `./make.ps1 tests` |
| Install | `make install` | `unknown` |

## Common task map

| Task | Start here |
|---|---|
| Game startup | `OpenRA.Launcher/Program.cs`, `OpenRA.Game/Game.cs`, launch scripts. |
| Dedicated server | `OpenRA.Server/Program.cs`, `OpenRA.Game/Server/Server.cs`, `OpenRA.Game/Settings.cs`. |
| Network/order/sync | `OpenRA.Game/Network/`, `OpenRA.Game/World.cs`. |
| Gameplay traits | `OpenRA.Mods.Common/Traits/`, `OpenRA.Mods.Cnc/`, `OpenRA.Mods.D2k/`, `mods/*/rules/`. |
| UI/chrome | `mods/common/chrome/`, `mods/*/chrome/`, `OpenRA.Mods.Common/Widgets/Logic/`, Fluent files. |
| Mod data | `mods/<mod>/mod.yaml`, rules, weapons, sequences, maps, scripts. |
| Utility commands | `OpenRA.Utility/Program.cs`, `IUtilityCommand` implementations. |
| Build/CI | `Makefile`, `make.ps1`, `.github/workflows/ci.yml`, `Directory.Build.props`. |
| Packaging | `packaging/functions.sh`, `packaging/`, install targets. |

## Security-sensitive areas

- Dedicated server auth and admission control: `OpenRA.Game/Settings.cs`, `OpenRA.Game/Server/Server.cs`.
- TCP protocol, handshakes, replay recording, order/sync packets: `OpenRA.Game/Network/`.
- Deterministic simulation and shared RNG: `OpenRA.Game/World.cs`, `OpenRA.Game/Network/OrderManager.cs`.
- External downloads and profiles: `Makefile`, `make.ps1`, `OpenRA.Game/PlayerDatabase.cs`.
- Packaging/install scripts: `packaging/functions.sh`.

## Generated files and do-not-edit zones

Avoid committing generated/local outputs from `.gitignore`: `bin/`, `obj/`, `Release/`, `/Support`, `/lib`, `/TestResult.xml`, `IP2LOCATION-LITE-DB1.IPV6.BIN.ZIP`, `DOCUMENTATION.md`, `WEAPONS.md`, `Lua-API.md`, `Settings.md`, `*.html`, `openra.6`, `update.log`.

## Unknowns

- Requested `main` branch was not found; `bleed` was used because repository metadata reports it as default.
- No `global.json` was found; .NET 8 is inferred from project config, docs, and CI.
- No Dockerfile or compose file was found in checked root paths.
- Full issue/PR template inventory could not be enumerated through the connector directory API.
