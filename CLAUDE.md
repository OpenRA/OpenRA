# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a fork of **OpenRA** (open-source RTS engine) with a custom **Cursor-on-Target (CoT)** integration that broadcasts game unit positions as MIL-STD CoT XML messages over UDP to TAK ecosystem applications (TAKX, WinTAK, ATAK). The CoT layer transforms in-game actors (vehicles, aircraft, infantry, ships, buildings) into real-time situational awareness markers on TAK maps.

## Build Commands

**Windows (PowerShell):**
```powershell
# Build (Release)
powershell -File make.ps1 all

# Build (Debug, enables StyleCop/Roslynator analysis)
powershell -File make.ps1 all CONFIGURATION=Debug

# Run YAML validation for all mods
powershell -File make.ps1 test

# Run NUnit tests
powershell -File make.ps1 tests

# Run code style checks (Debug build + interface checks)
powershell -File make.ps1 check

# Clean
powershell -File make.ps1 clean
```

**Linux/macOS:**
```bash
make                    # Build Release
make check              # Debug build with style analysis
make test               # YAML validation
make tests              # NUnit tests
```

**Launch the game:**
```
cmd /c launch-game.cmd Game.Mod=ra
```

**Run a single test (after building):**
```powershell
dotnet test bin\OpenRA.Test.dll --filter "FullyQualifiedName~CoTVisibilityRouterTests"
```

## Architecture

### Engine Structure

OpenRA uses a **trait-based entity-component system**. Game behaviors are modular traits (C# classes implementing `*Info` + runtime class) attached to actors via YAML rules files. Key assemblies:

- **OpenRA.Game** — Core engine: actor system, rendering, map loading, network
- **OpenRA.Mods.Common** — Shared mod framework (traits, activities, widgets). **All CoT code lives here.**
- **OpenRA.Mods.Cnc / D2k** — Game-specific mods (C&C:TD, Dune 2000)
- **OpenRA.Server** — Multiplayer dedicated server
- **OpenRA.Test** — NUnit test suite

All projects output to `bin/`. The solution targets **.NET 8.0 / C# 12**.

### CoT Integration (the custom layer)

All CoT code is in `OpenRA.Mods.Common/`:

**Transport layer:**
- `CotOutputService.cs` — Static async UDP sender with bounded queue (256 msgs, drop-oldest). Supports unicast and multicast. Persists config to `%AppData%/OpenRA/cot-output.json`.

**Emitter traits** (in `Traits/World/`):
- `CoTVehicleEmitter.cs` — Ground vehicles (spawn, damage, kill, periodic heartbeat)
- `CoTAircraftEmitter.cs` — Aircraft with flight-phase detection, altitude profiles, speed/course telemetry
- `CoTInfantryEmitter.cs` — Infantry units
- `CoTShipEmitter.cs` — Naval units
- `CoTBuildingEmitter.cs` — Static structures (longer update intervals)
- `CoTBroadcaster.cs` — Emits on specific orders (e.g., PlaceBeacon)
- `CoTOnSpawnBroadcaster.cs` — Emits once on actor spawn
- `CoTPeriodicBroadcaster.cs` — Periodic heartbeat emitter

**Visibility routing:**
- `CoTVisibilityRouter.cs` — Fog-of-war aware filter: friendly units always emit, hostile units only emit when detected by allied vision. Applies generic MIL-STD-2525C symbols to unidentified hostiles. Stealth gate suppresses cloaked units unless attacking.

### How Traits Wire to Actors

YAML rules in `mods/ra/rules/` attach CoT traits to actor archetypes:
- `defaults.yaml` — Attaches `CoTVehicleEmitter`, `CoTInfantryEmitter`, `CoTShipEmitter`, `CoTAircraftEmitter`, `CoTBuildingEmitter` to base actor types with per-actor callsign/symbol mappings
- `world.yaml` — Attaches `CoTBroadcaster` to the World actor (host `127.0.0.1`, port `4242`)
- `cot-fow.yaml` — Configures `CoTVisibilityRouter` with domain-specific hostile symbols and fog-of-war policy

### CoT Message Flow

1. Game tick fires → Emitter trait checks update interval and position change
2. `CoTVisibilityRouter.ShouldEmit()` checks fog-of-war visibility
3. Emitter builds CoT XML (`<event>` with `<point>`, `<detail>`, milsym, contact, track elements)
4. XML bytes enqueued to `CotOutputService` bounded channel
5. Background task sends UDP packet to configured TAK endpoint

### Key Patterns

- **Trait Info pattern:** Every trait has a `FooInfo` class (YAML-configurable properties) and a `Foo` runtime class. Info is shared; Foo is per-actor instance.
- **Per-actor YAML mappings:** Emitters use dictionary fields (e.g., `ActorCallsigns`, `ActorDamageStateMilsymIds`) for per-unit-type customization, with case-insensitive lookup.
- **CoT domains:** `CoTDomain` enum — `GroundMobile`, `Building`, `Aircraft`, `Vessel` — used by visibility router for domain-specific hostile symbol overrides.
- **Geo-referenced maps:** The `GeoMaps/` folder contains `.oramap` files with real-world coordinates for CoT lat/lon output.

## Code Style

- **Indentation:** Tabs (4-column width)
- **Line endings:** LF (Unix-style)
- **Namespaces:** Block-scoped (not file-scoped)
- **Analyzers:** StyleCop + Roslynator run on Debug builds only; Release builds skip analyzers for performance
- Code style rules enforced via `.editorconfig` — build with `CONFIGURATION=Debug` or run `check` target to validate

## Testing

- Tests use **NUnit** framework
- CoT-specific tests are in `OpenRA.Test/OpenRA.Mods.Common/CoTVisibilityRouterTests.cs`
- `EvaluatePolicy()` and `EvaluateStealthGate()` are static methods on `CoTVisibilityRouter` specifically for testability
- YAML validation (`make test` / `make.ps1 test`) checks all mod YAML for parse errors

## Default CoT Configuration

- UDP host: `127.0.0.1` (localhost), port: `4242` (TAK standard)
- Stale time: 120 seconds
- Update intervals: 25 ticks (vehicles/aircraft/infantry/ships), 150 ticks (buildings)
- Max intervals: 250 ticks (mobile), 1500 ticks (buildings)
