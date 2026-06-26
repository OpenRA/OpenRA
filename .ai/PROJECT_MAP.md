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

# Project Map

## Top-level map

| Path | Role | Evidence |
|---|---|---|
| `OpenRA.sln` | Visual Studio/.NET solution with engine, mods, launchers, server, utility, platform, and tests. | `OpenRA.sln` |
| `Directory.Build.props` | Shared build settings: .NET 8, C# 12, output to `bin`, analyzers, nullable disabled. | `Directory.Build.props` |
| `Makefile` | Unix build/check/test/install entrypoint. | `Makefile` |
| `make.ps1` | Windows build/check/test entrypoint. | `make.ps1` |
| `OpenRA.Game/` | Core runtime and reusable engine APIs. | project file and source entrypoints |
| `OpenRA.Launcher/` | Game executable project. | `OpenRA.Launcher/Program.cs` |
| `OpenRA.Server/` | Dedicated server executable project. | `OpenRA.Server/Program.cs` |
| `OpenRA.Utility/` | Utility executable project. | `OpenRA.Utility/Program.cs` |
| `OpenRA.Mods.Common/` | Shared gameplay/mod logic used by official mods. | project references and mod manifests |
| `OpenRA.Mods.Cnc/` | C&C-family mod assembly. | `OpenRA.Mods.Cnc/OpenRA.Mods.Cnc.csproj` |
| `OpenRA.Mods.D2k/` | Dune 2000 mod assembly. | `OpenRA.Mods.D2k/OpenRA.Mods.D2k.csproj` |
| `OpenRA.Platforms.Default/` | Default platform adapter/native wrapper dependencies. | `OpenRA.Platforms.Default/OpenRA.Platforms.Default.csproj` |
| `OpenRA.Test/` | NUnit test project. | `OpenRA.Test/OpenRA.Test.csproj` |
| `mods/` | Data-driven mod definitions: manifests, rules, chrome, maps, scripts, localization. | `mods/ra/mod.yaml` |
| `packaging/` | Install/release scripts and platform assets. | `packaging/functions.sh` |
| `.github/workflows/` | CI workflow definitions. | `.github/workflows/ci.yml` |

## Project dependencies

- `OpenRA.Launcher`, `OpenRA.Server`, `OpenRA.Utility`, `OpenRA.Platforms.Default`, and `OpenRA.WindowsLauncher` reference `OpenRA.Game`.
- `OpenRA.Mods.Common` references `OpenRA.Game` and adds shared third-party packages for mod/game features.
- `OpenRA.Mods.Cnc` and `OpenRA.Mods.D2k` reference `OpenRA.Game` and `OpenRA.Mods.Common`.
- `OpenRA.Test` references `OpenRA.Game` and `OpenRA.Mods.Common` and uses NUnit packages.

## Important external dependencies

- Build/runtime: .NET 8 SDK.
- Native platform libraries or wrappers: SDL2, OpenAL, FreeType; Lua 5.1 for script validation.
- NuGet packages include `Linguini.Bundle`, `OpenRA-Eluant`, `Mono.NAT`, `SharpZipLib`, `NUnit`, `StyleCop.Analyzers`, and Roslynator analyzers.
- External runtime downloads include the GeoIP database and forum-hosted player profile/badge data.

## Runtime entrypoints

| Runtime | Entrypoint | Notes |
|---|---|---|
| Game | `OpenRA.Launcher/Program.cs` | Calls `Game.InitializeAndRun(args)`. |
| Dedicated server | `OpenRA.Server/Program.cs` | Requires `Game.Mod`; uses `MOD_SEARCH_PATHS` if present. |
| Utility | `OpenRA.Utility/Program.cs` | First argument selects mod; later argument selects utility command. |
| Unix wrapper | `launch-game.sh` | Runs `dotnet bin/OpenRA.dll`. |
| Windows wrapper | `launch-game.cmd` | Runs `bin\OpenRA.exe`. |
| Utility wrappers | `utility.sh`, `utility.cmd` | Set engine directory and run utility executable. |

## Mod package map

- `mods/common/`: shared chrome, scripts, localization, hotkeys, metrics, and other common data.
- `mods/ra/`, `mods/cnc/`, `mods/d2k/`, `mods/ts/`: official mod data.
- `mods/*-content/`: content installer/packaging support for game assets.
- Manifests (`mods/*/mod.yaml`) wire together assemblies, package formats, file system mounts, rules, chrome, map folders, and server traits.

## Important config files

- `.editorconfig`: formatting and C# style rules.
- `.gitignore`: generated output and local support directories.
- `.github/workflows/ci.yml`: Linux/Windows validation matrix.
- `INSTALL.md`: platform setup and run instructions.
- `CONTRIBUTING.md`: contribution process.

## Not detected in checked root paths

- `global.json`
- `Directory.Packages.props`
- `Directory.Build.targets`
- `Dockerfile`
- `docker-compose.yml`
- database migration config
