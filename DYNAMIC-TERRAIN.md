# Runtime terrain geometry and mod-owned height rules

Related upstream work: [terrain deformation #13951](https://github.com/OpenRA/OpenRA/issues/13951)
and [template rendering #21826](https://github.com/OpenRA/OpenRA/pull/21826).

The engine supplies geometry notifications and optional consumer policies. Mods own
shared-corner validation, crater and ice rules, bridge stages and repair, falling
motion, occupant consequences and synchronized save/replay state. None of these
TS-specific rules is enabled in stock mods by adding the APIs.

The activity/cargo policies below are additional generic API proposals motivated
by the downstream TS consumer. The linked upstream discussions do not establish
agreement on their exact shape, and this change does not close all of #13951.

## Changing the surface

`Map.Tiles` and `Map.Height` remain the authoritative layers. Ramp geometry is derived
from the tile's `RampType`; `Map.Ramp` must not be edited independently. The existing
terrain renderer, projection, terrain-cost observers and hierarchical pathfinder
consume their change notifications. Do not implement a crater as a sprite override.

A mod should build and validate a detached plan, including neighboring shared corners,
allowed tiles, height limits and protected cells. Apply all final tile/height pairs in
one `World.AddFrameEndTask`, in deterministic coordinate order. Indexer notifications
remain per-cell: this is not a transactional snapshot API for event subscribers.
Prepare ordinary rejection paths before writing; callbacks should invalidate caches
rather than query a partly applied multi-cell plan.

`Map.TerrainGeometryRevision` is a cache version, not a serialized terrain snapshot.
It changes when cell projection is updated. Consumers compare for inequality; one
logical edit can increment it multiple times. Ground `Mobile` actors previously on
the surface follow its new Z and ramp orientation on their next simulation tick.
Their XY, subcell, reservations and activities are retained. Raised actors and custom
movement layers are not snapped onto the default surface.

Height changes also refresh stationary shroud sources, height-dependent radar colors
and retained smudge geometry. Shroud/grounding can lag the frame-end edit by one
simulation tick. Terrain-derived visibility retains the existing OpenRA model; this
is not a new ray-traced visibility implementation.

The pathfinder already invalidates affected hierarchical grids. Adjacent-cell fast
paths and cached live routes additionally validate the current height edge. An
already committed movement segment finishes; future forbidden edges cause repathing.
Newly opened shorter routes do not proactively interrupt every existing movement.

The mod must protect foundations, fixed bridge/tunnel geometry and endpoints, and
must not turn occupied/reserved cells into impassable terrain without implementing
an explicit occupant policy. Deterministic writers should include terrain state and
pending operations in an `ISync` trait. Native saves replay orders: do not restore a
second terrain snapshot on top of replayed changes. `IGameSaveTraitData` can verify
that replay reconstructed the expected terrain and pending queue.

## Optional weapon range policy

Implement `IArmamentRangePolicyInfo` on a mod trait info and `IArmamentRangePolicy`
on its runtime trait. At most one policy is allowed per actor; it receives the
armament so that it can distinguish weapons.

```csharp
public interface IArmamentRangePolicy
{
    WDist GetMaximumRange(Armament armament, WDist nominalRange);
    bool IsInRange(Armament armament, WPos origin, in Target target,
        WDist nominalMinRange, WDist nominalMaxRange);
    int RangeRevision { get; }
}
```

- Nominal maximum range already includes existing `IRangeModifier` percentages.
  Do not apply those modifiers twice. `nominalMinRange` is the weapon minimum.
- `GetMaximumRange` returns a nonnegative conservative horizontal search bound for
  **all prospective firing positions**, not just the actor's current height.
- `IsInRange` is the exact, side-effect-free predicate, including minimum range.
  `origin` is an actor-center firing origin; candidate movement cells supply their
  own surface/subcell center. Never substitute the actor's current position.
- Use only the supplied target/snapshot. Do not query hidden actors' live positions.
- Increment `RangeRevision` when private mod state changes the predicate without
  changing its bound. Terrain revisions, bounds and enabled/paused armaments are
  tracked by the shared adapter. Revision/hash calculations must be deterministic.

`Armament.MaxRange()` exposes the conservative bound; `Armament.IsInRange()` performs
an exact check. Weapon selection, aiming, firing and auto-target checks use the exact
predicate. Ground attack movement uses `TargetRange` to share it with candidate
selection, stopping and repathing. Moving targets and terrain/policy changes invalidate
candidate decisions. When the target is hidden, attack activities retain their existing
last-known-position behavior rather than obtaining new information from the policy.

`Mobile` implements optional `IMoveWithinTargetRange` and its info marker. Existing
`IMove` signatures remain unchanged. Policies on unsupported moving actors (including
aircraft) are rejected by lint/runtime validation; there is no silent scalar fallback.
Stationary actors can use the firing predicate without a mover.

Without a policy the old range calculation is preserved. RA/TD rules add no policies.
Existing `IFirepowerModifier`, `IInaccuracyModifier`, `IDamageModifier` and
`IRevealsShroudModifier` remain the extension points for other mod-owned bonuses.
For example, a height-sight trait can retain its actor and return a percentage from
`GetRevealsShroudModifier`; absent modifiers retain the normal sight radius.

These are targeting/movement hooks, not projectile-physics overrides. Increasing a
search/firing bound does not increase missile fuel/lifetime or a beam's own range
limit. Configure a compatible projectile in the mod. A ballistic reachability formula
belongs in the mod policy and must agree with that projectile's trajectory model.

## Mod-owned mutable movement layers and bridge segments

These optional interfaces let mods publish movement-layer changes through the
existing Locomotor and hierarchical pathfinder. TS-specific damage thresholds,
tile roles, occupant consequences and repair algorithms belong in the mod.

- `ICustomMovementLayerTerrainChanges.CellTerrainChanged`: publish affected cells
  after updating provider state. Locomotor normalizes coordinates to the provider's
  Index and refreshes costs.
- `ICustomMovementLayerConnectivityChanges.CellConnectivityChanged`: publish changed
  entry/exit endpoints even when cell costs are unchanged. HPF rebuilds the affected
  domains using its existing projection invalidation.
- `ICustomMovementLayerMoveOrders.SupportsMoveOrders`: opt in to retaining the target
  layer in `NearestMoveableCell` and its nearest-cell candidates. Legacy layers keep
  their ground fallback. Target picking and order serialization remain mod-owned.
- `Mobile.SetPosition(CPos)` retains the custom provider's Z. Only default-layer
  placement is projected onto the ground.
- Public `IBridgeSegment` and `BridgeLayer.Add/Remove(actor, cells)` allow repairable
  mod segments without a Building footprint to use the existing hut/repair activities.
  The Building overloads continue to use `BuildingInfo.PathableTiles`.

Publish notifications after the simulation patch is complete. They are synchronous
and do not provide a transactional multi-cell snapshot. Providers own their hashes,
save/replay state and occupant policy. These hooks do not independently move a
custom layer's height or replace mod validation of fixed deck geometry.

## Graphics-only cell overrides

`ITerrainCellSpriteOverride.TerrainCellSprite(CPos, TerrainTile)` allows world traits
to replace a cell's sprite without changing its terrain identity. First non-null
result wins; null keeps the normal sprite. The existing palette, TerrainSpriteLayer
and depth renderer are retained. Providers must not mutate simulation state and
must call `TerrainRenderer.UpdateCell` after their own graphics state changes.
With no provider the normal path is unchanged. Radar uses the existing
`IRadarTerrainLayer`; a graphics override is not terrain deformation.

## Mod-owned falling and activity/cargo policies

Implement `OpenRA.Traits.IPreventActivity` explicitly on an actor trait:

```csharp
bool IPreventActivity.PreventActivity(Actor self) => falling;
```

Any provider returning true suspends the actor's activity tree and idle callbacks
without discarding the queue. When all return false the existing activity resumes.
Trait ticks, rendering, damage and disposal continue; the policy does not reject
orders or prevent other traits from changing position. Actors without a provider
follow the existing activity path. The mod must synchronize policy state.

Normal activity cancellation lets an uninterruptible movement half-step finish.
A mod moving a falling actor must suspend that half-step, otherwise it can overwrite
the actor's XY/Z with its old movement position. The mod must separately release
obsolete movement reservations through `Mobile`'s APIs while preserving the visual
position, and decide whether the retained activity is still valid after landing.

Implement `OpenRA.Mods.Common.Traits.ICargoEjectionPolicy` explicitly on the carrier:

```csharp
bool ICargoEjectionPolicy.CanEjectOnDeath(Actor self, AttackInfo attack) => !falling;
```

All providers must allow death ejection, in addition to the existing `EjectOnDeath`,
ground-level and terrain requirements. With no provider the previous behavior is
preserved. A veto uses `Cargo`'s normal passenger-death path and notifications.
This hook does not change voluntary unloading or define a fall or damage event.

The TS consumer owns loss-of-support classification, trajectory, native-frame
clock, landing consequences, cargo policy and save/replay verification. No fall
constants or TS casualty rules enter these engine interfaces.

## Reproduce the native regression

Run from this engine checkout after installing its normal build/runtime dependencies.
The fixture needs a .NET 10 SDK, Python 3, installed TS content, Xvfb and the default
graphics platform with software GL. Cold-load automation also needs xdotool to
resume the normal load-game menu. ImageMagick's `import` is optional for screenshots.
No original assets are committed. The fixture uses stock TS rules and requires
no downstream TS module.

The runner creates a private mod and Support directory, links existing content,
and never installs into a user's game. Each `--out` directory must not exist yet.
It links engine binaries: do not rebuild that checkout during a run. The Linux
commands below run serially with a 4 GiB / zero-swap cgroup limit; the runner does
not enforce a resource limit itself. They require a working user systemd manager.

```sh
dotnet build -c Release
dotnet build OpenRA.Test/Fixtures/TerrainGeometry/Probe.csproj -c Release
engine="$PWD"
content=/path/to/Support/Content
out="$(mktemp -d /tmp/openra-terrain.XXXXXX)"

systemd-run --user --scope -p MemoryMax=4G -p MemorySwapMax=0 -- \
  python3 OpenRA.Test/Fixtures/TerrainGeometry/run.py \
  --engine "$engine" --content "$content" --out "$out/cold" \
  --actor-policies --cold

systemd-run --user --scope -p MemoryMax=4G -p MemorySwapMax=0 -- \
  python3 OpenRA.Test/Fixtures/TerrainGeometry/run.py \
  --engine "$engine" --content "$content" --out "$out/network-large" \
  --actor-policies --network --large-map
```

`--actor-policies` enables nine additional assertions; the default fixture retains
207, and the combined fixture has 216. Build-time `EnableActorPolicyProbe` defaults
to true, but the runtime checks require the flag. Do not combine `--network` with
`--cold`: these exercise different process/save paths. Omit `--large-map` to use
the ordinary 60 by 120 map (7,200 allocated cells); the larger map is 128 by 256
(32,768 cells).

Coverage includes:

- Adjacent and cached live height edges; stationary and moving infantry/vehicle
  grounding; an elevated control; shared-corner geometry; hierarchical gates
  inside/across grids compared with an independent reachability traversal.
- Stationary visibility and a mod range policy that makes a ground shooter move
  to a valid elevated firing position before shooting.
- Mutable-layer endpoint changes with unchanged costs, terrain removal/restoration,
  provider Z, direct/nearest targets, sprite replacement and null fallback without
  changing simulation geometry.
- With `--actor-policies`: uninterruptible activity suspension/resume, idle
  suspension/resume, continued trait ticks, ordinary cargo ejection with no policy,
  allowed/denied ejection and a veto among multiple providers. These are independent
  test actors; they do not test TS falling rules.

### Results and artifacts

Every run produces `result.json`, `inputs.json` with binary/map hashes,
`command.json`, runtime logs and JSONL simulation traces. A nonzero runner exit or
`passed: false` is a failure. Input hashes are checked again during verification;
changing an engine binary invalidates the run.

Cold mode saves through the normal game path, starts a new process and compares the
complete subsequent simulation events, including sync hashes, after save catch-up.
Inspect `cold.passed` and `coldEventsEqual` in addition to the top-level `passed`.
Fresh events are retained in `baseline.jsonl`; the cold trace remains in
`Support/Logs/terrain.jsonl`. This qualifies replay with the tested binaries/content,
not migration from earlier saves.

Network mode starts two real clients through the normal lobby, network order
manager and lockstep protocol. It records `World.SyncHash()` each simulation tick
and requires both hashes and the complete event streams to match, with no exception
or sync-report logs. Inspect `checks`, `syncHashTicks`, `syncHashesEqual` and
`eventsEqual`; commands are saved in `network-commands.json`. Save requests are
disabled in this mode.

Local acceptance on 27 October 2026 at implementation commit `dce538d62822`:

| Check | Observed result |
| --- | --- |
| Native `--actor-policies --cold` | 216 fresh + 216 cold assertions; subsequent events and hashes match |
| Native `--actor-policies --network --large-map` | 216 assertions per client; matching complete events and hashes for all 425 ticks |
| `make check` | Pass: Debug build with warnings as errors, explicit-interface and conditional-trait checks |
| `make tests` | 509 passed, 2 skipped |
| `make TREAT_WARNINGS_AS_ERRORS=true test` | Pass: Release build and stock RA/CNC/D2K/TS content/rules checks |

The native cold and larger network runs peaked at 587.50 MiB and 1,159.85 MiB
combined process-group RSS respectively under a serial 4 GiB / zero-swap guard.
These are host-specific bounded regressions, not a long-match throughput benchmark
or cross-platform certification. Local results do not imply GitHub Actions passed;
the workflow status is reported separately in the PR.

The downstream TS consumer separately tests source road/rail/low bridge repair,
source event 31 through Lua, ice masks, real crater geometry/LAT and delayed falling
with cargo. Those are mod acceptance results, not additional assertions of this
standalone engine fixture or a qualification of the full TS campaign.

### Historical negative controls

The pre-extension engine `3e79a1f` already has the range API, but lacks the mutable
layer, sprite and activity/cargo APIs. Build the current probe against that archive
with the corresponding compile switches disabled, and use its engine binaries:

```sh
baseline_engine=/path/to/built/pre-extension-engine
dotnet build OpenRA.Test/Fixtures/TerrainGeometry/Probe.csproj -c Release \
  -p:OpenRAPath="$baseline_engine" -p:OutputPath=bin/pre-extension/ \
  -p:EnableLayerApi=false -p:EnableSpriteApi=false \
  -p:EnableActorPolicyProbe=false
systemd-run --user --scope -p MemoryMax=4G -p MemorySwapMax=0 -- \
  python3 OpenRA.Test/Fixtures/TerrainGeometry/run.py \
  --engine "$baseline_engine" --content "$content" --out "$out/pre-extension" \
  --probe "$engine/OpenRA.Test/Fixtures/TerrainGeometry/bin/pre-extension/OpenRA.TerrainProbe.dll"
```

The recorded result was 197 passes and 10 expected failures, without an exception.
The runner correctly returns failure for this control; it must not be counted as a
passing run. This historical control demonstrates the earlier layer/sprite changes,
not the newer activity/cargo policies. Omit `--actor-policies` because its interfaces
are absent. Do not pass `--baseline`: that flag removes the range-policy actor and
is reserved for pre-range archives, which also need `-p:EnableRangeProbe=false`.
