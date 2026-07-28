# Lunar Red Alert — Space-Age total conversion

A "Space-Age" total conversion of this OpenRA fork: units manage **oxygen** instead of
fuel, take **vacuum damage** when exposed and out of air, **bound in low gravity**, and
survive only inside the **pressurised bubbles** projected by base structures.

This is a **desktop** OpenRA mod (`spaceage`). OpenRA has no browser/WebAssembly build —
see `mods/spaceage/SPACE-AGE-DESIGN.md` §0/§4 for why, and use GitHub Pages for the
project's landing/download site rather than trying to run the engine in a browser.

> **Status: wired, not yet built.** This machine has no .NET SDK, so the code was written
> and cross-checked against this exact `bleed` checkout's API but **not compiled or
> lint-run**. Build it and run the checklist below.

## What was added / changed

**Engine (compiled into `OpenRA.Mods.Common`):**
- `OpenRA.Mods.Common/Traits/SpaceAge/Oxygen.cs` — O2 resource: drains in vacuum, refills
  while `pressurised`, grants `no-oxygen` at empty (event-driven `IObservesVariables`).
- `OpenRA.Mods.Common/Traits/SpaceAge/DamagedByVacuum.cs` — periodic vacuum damage unless
  safe; a near-clone of the engine's `DamagedByTerrain`.
- `OpenRA.Mods.Common/Traits/SpaceAge/GravitySpeedModifier.cs` — `ISpeedModifier` low-g move.
- `OpenRA.Mods.Common/Traits/SpaceAge/OxygenBar.cs` — `ISelectionBar` cyan O2 meter.
- `OpenRA.Mods.Common/Traits/World/LowGravity.cs` — world gravity constant.
- `OpenRA.Mods.Common/Projectiles/PATCH-projectile-gravity.md` — optional ~10-line edits to
  wire gravity into arcing projectiles (not applied; movement gravity works without it).

**Mod `mods/spaceage/`** — a *thin* mod (~48 KB): it mounts the whole `ra` mod by id
(`$ra: ra`) and mounts itself (`$spaceage: spaceage`), so all of RA's rules/art/maps load
from `mods/ra` with **no duplication**. Only four overlay files are physical, registered
in `mod.yaml` *after* the base `ra|` rules so they merge on top:
- `rules/spaceage-world.yaml` — `LowGravity` on the World actor (16% g = Moon).
- `rules/spaceage-defaults.yaml` — O2 / vacuum / low-g traits on `^Soldier`; low-g on `^Vehicle`.
- `rules/spaceage-infantry.yaml` — per-unit O2 tuning (E1/E3/E6).
- `rules/spaceage-structures.yaml` — `FACT`/`POWR`/`APWR` project the `pressurised` bubble.

> **The one unverified piece:** the thin cross-mod mount (`$ra: ra` from inside `spaceage`).
> If `--check-yaml` reports it can't resolve `ra|...`, the fallback is a self-contained
> copy: `cp -r mods/ra mods/spaceage-full` then re-apply the four overlays — but try the
> thin mount first; it uses the same `$id: id` directive RA already uses to mount itself.

**Not yet loaded** (need art / a base-weapon-name pass): `tilesets/lunar-template.yaml`,
`weapons/spaceage-template.yaml`. Procedural asset generators are in `mods/spaceage/tools/`.
`Makefile` `test:` now also lint-checks the `spaceage` mod.

## Condition flow

```
FACT/POWR/APWR ─ProximityExternalCondition─▶ pressurised (units w/ ExternalCondition@PRESSURE)
   Oxygen: refills if pressurised else drains ─▶ grants no-oxygen at 0
   DamagedByVacuum: if !pressurised && oxygen==0 ─▶ damage every 16 ticks
   GravitySpeedModifier@LOWG: +40% infantry / +10% vehicles
   GravitySpeedModifier@SUFFOCATING (RequiresCondition: no-oxygen): −50% speed
```
Vehicles are vacuum-proof simply by not having the Oxygen/DamagedByVacuum traits.

## Build, lint, run

```bash
make                                   # build the engine (needs the .NET SDK)
./utility.sh spaceage --check-yaml     # lint the mod's MiniYAML (or: make test)
./launch-game.sh Game.Mod=spaceage     # first run downloads the RA content it reuses
```

## Verify (do after it builds)

- [ ] `make` compiles; `--check-yaml` passes for `spaceage`.
- [ ] Infantry in the open: **O2 bar drains**, then they take damage and die.
- [ ] Near a Construction Yard/Power Plant: O2 **refills**, damage stops.
- [ ] Infantry move visibly faster than baseline; **slow to a crawl when O2 hits zero**.
- [ ] Vehicles never suffocate.

Full rationale, C# walkthrough, asset pipeline and roadmap: `mods/spaceage/SPACE-AGE-DESIGN.md`.
