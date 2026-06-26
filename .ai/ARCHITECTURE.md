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

# Architecture

## High-level architecture

OpenRA is a .NET 8 RTS engine with data-driven mods. C# engine assemblies provide runtime services, simulation, networking, rendering/sound integration, utility commands, and official mod code. YAML, Lua, Fluent, and asset package metadata define much of the game behavior under `mods/`.

Evidence:
- `OpenRA.sln`
- `Directory.Build.props`
- `OpenRA.Game/Game.cs`
- `mods/ra/mod.yaml`

## Runtime startup flow

1. Platform wrapper starts the game:
   - Unix: `launch-game.sh` runs `dotnet bin/OpenRA.dll`.
   - Windows: `launch-game.cmd` runs `bin\OpenRA.exe`.
2. `OpenRA.Launcher.Program.Main` calls `Game.InitializeAndRun(args)`.
3. `Game.Initialize` reads engine/support directory overrides, initializes settings/log channels, NAT, mod search paths, installed mods, platform, renderer, and sound.
4. `Game.InitializeMod` clears old static/runtime state, resets UI, loads `ModData`, initializes loaders/fonts/maps/cursor, then starts the mod load screen.

Evidence:
- `launch-game.sh`
- `launch-game.cmd`
- `OpenRA.Launcher/Program.cs`
- `OpenRA.Game/Game.cs`

## World and simulation flow

- `Game.StartGame` prepares the map, creates a `World`, creates a `WorldRenderer`, calls `World.LoadComplete`, starts the `OrderManager`, then calls `World.PostLoadComplete`.
- `World` owns actors, effects, players, actor maps, screen maps, order validators, local/shared RNG, and world ticks.
- Deterministic state is protected by `World.SyncHash()`, which hashes actors, synced trait/effect state, shared RNG state, and selected player state.

Evidence:
- `OpenRA.Game/Game.cs`
- `OpenRA.Game/World.cs`
- `OpenRA.Game/Network/OrderManager.cs`

## Networking and order flow

- `IConnection` abstracts local echo, network, and replay-style order sources.
- `OrderManager` accepts local and immediate orders, sends orders/sync packets through the connection, receives remote orders, and advances frames only when expected order packets are available.
- `NetworkConnection` uses TCP, performs protocol-version checks, receives order/sync/ack/tick-scale/disconnect packets, and can record replays.
- Desync handling writes sync reports and marks the game out-of-sync.

Evidence:
- `OpenRA.Game/Network/Connection.cs`
- `OpenRA.Game/Network/OrderManager.cs`

## Dedicated server flow

1. `OpenRA.Server.Program.Run` reads arguments and settings.
2. It requires `Game.Mod`, initializes settings/NAT/mod search paths, loads `InstalledMods`, creates `ModData`, and starts `Server` on IPv4/IPv6 endpoints for `settings.ListenPort`.
3. `Server` creates TCP listeners, validates handshakes, checks mod/version/protocol, passwords, bans, profile authentication, whitelist/blacklist rules, and server traits.
4. Dedicated server instances restart after a game finishes with no clients.

Evidence:
- `OpenRA.Server/Program.cs`
- `OpenRA.Game/Server/Server.cs`
- `OpenRA.Game/Settings.cs`

## Utility flow

- `utility.sh`/`utility.cmd` run `OpenRA.Utility` with an engine directory override.
- `OpenRA.Utility.Program` loads installed mods, resolves the selected mod, creates `ModData`, discovers `IUtilityCommand` implementations through the object creator, validates args, and runs the command.

Evidence:
- `utility.sh`
- `utility.cmd`
- `OpenRA.Utility/Program.cs`

## Mod loading model

Mod manifests define what the engine loads. For Red Alert, `mods/ra/mod.yaml` declares:

- package formats and file-system mounts
- required content files and content installer mod
- map folders
- rules, sequences, tile sets, cursors, chrome, chrome layout, Fluent messages
- weapons, voices, notifications, music, hotkeys
- assemblies: `OpenRA.Mods.Common.dll`, `OpenRA.Mods.Cnc.dll`
- server traits, default order generator, game speeds

Evidence:
- `mods/ra/mod.yaml`

## Trust boundaries and risks

| Boundary | Risk |
|---|---|
| Network clients to server | untrusted handshake data, passwords, IPs, profile signatures, order packets. |
| Mod YAML/Lua/assets to runtime | malformed data can break load, lint, UI, or gameplay. |
| Synced vs unsynced code | use of nondeterministic data in synced paths can desync multiplayer/replays. |
| External downloads | GeoIP database, player profile/badge URLs, NuGet packages, original game asset packages. |
| Packaging scripts | install/publish targets write outside build directories when invoked with install paths. |

## Architectural unknowns

- No database or migration layer was detected.
- No Docker/container runtime setup was detected in checked root paths.
- Full map of all utility commands and traits requires broader code enumeration beyond this bootstrap scan.
