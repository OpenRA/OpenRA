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

# Commands

## Prerequisites

| Platform | Verified prerequisites | Evidence |
|---|---|---|
| Windows | Windows PowerShell >= 4.0 and .NET 8 SDK. | `INSTALL.md` |
| Linux | .NET 8 SDK; system libraries if using `TARGETPLATFORM=unix-generic`. | `INSTALL.md`, `Makefile` |
| macOS | .NET 8 SDK and `make`. | `INSTALL.md` |
| Lua checks | Lua 5.1 / `luac`. | `Makefile`, `.github/workflows/ci.yml` |

System-library builds need SDL2, FreeType, OpenAL, and Lua 5.1 according to `INSTALL.md`.

## Build

```sh
make
```

```powershell
./make.ps1 all
```

Notes:
- Unix `make` builds with `dotnet build -c ${CONFIGURATION} -p:TargetPlatform=$(TARGETPLATFORM)` and then runs `fetch-geoip.sh`.
- Windows `make.ps1 all` builds with `TargetPlatform=win-x64` and downloads the GeoIP artifact if missing/stale.

## Build using system native libraries

```sh
make TARGETPLATFORM=unix-generic
```

`unknown`: no Windows equivalent for `unix-generic` was identified.

## Run the game

```sh
./launch-game.sh Game.Mod=ra
./launch-game.sh Game.Mod=cnc
./launch-game.sh Game.Mod=d2k
./launch-game.sh Game.Mod=ts
```

```cmd
launch-game.cmd Game.Mod=ra
```

If no mod is supplied, wrappers prompt for one where possible.

## Run the dedicated server

Verified entrypoint:

```sh
dotnet bin/OpenRA.Server.dll Engine.EngineDir=".." Game.Mod=ra
```

Use current server docs/source before relying on exact production flags. `OpenRA.Server.Program` requires `Game.Mod`; it also reads `Engine.EngineDir`, `Engine.SupportDir`, and `MOD_SEARCH_PATHS`.

## Utility commands

```sh
./utility.sh ra --help
./utility.sh ra --check-yaml
```

```cmd
utility.cmd ra --check-yaml
```

The first utility argument is the mod id or mod path. Later arguments select a command discovered from `IUtilityCommand` implementations.

## Validation commands

| Purpose | Unix-like | Windows |
|---|---|---|
| Code/style/engine checks | `make check` | `./make.ps1 check` |
| NUnit tests | `make tests` | `./make.ps1 tests` |
| Mod YAML checks | `make test` | `./make.ps1 test` |
| Lua syntax checks | `make check-scripts` | `./make.ps1 check-scripts` |
| CI-equivalent broad check | `make check && make tests && make check-scripts && make TREAT_WARNINGS_AS_ERRORS=true test` | see `.github/workflows/ci.yml` |

## Focused tests

The Makefile runs:

```sh
dotnet build OpenRA.Test/OpenRA.Test.csproj -c Debug --nologo -p:TargetPlatform=$(TARGETPLATFORM)
dotnet test bin/OpenRA.Test.dll --test-adapter-path:.
```

For narrower NUnit runs, inspect current test names and pass supported `dotnet test` filters to `bin/OpenRA.Test.dll`. Do not assume a filter without checking the tests.

## Lint, typecheck, and format

- `make check` is the closest verified lint/typecheck command: it runs a Debug clean/build with warnings as errors and utility checks for interface issues.
- `.editorconfig` and analyzers enforce formatting/style during Debug builds.
- No separate formatter command was found.

## Database migrations

No database migration tooling was detected. Do not invent migration commands.

## Docker/local services

No `Dockerfile` or `docker-compose.yml` was found in checked root paths. Do not invent Docker commands.

## Release/install

```sh
make install
make install-linux-shortcuts
make install-linux-appdata
make install-man
```

Packaging helpers are in `packaging/functions.sh`. Do not run install/publish targets unless the task explicitly requires them and the destination paths are safe.

## CI notes

`.github/workflows/ci.yml` runs Linux and Windows jobs on .NET 8. Markdown-only changes are ignored by CI on push and pull request triggers, so documentation-only AI onboarding branches may not receive full CI automatically.
