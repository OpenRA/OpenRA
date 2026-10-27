# Runtime terrain geometry and mod-owned height rules

Related upstream work: [terrain deformation #13951](https://github.com/OpenRA/OpenRA/issues/13951)
and [template rendering #21826](https://github.com/OpenRA/OpenRA/pull/21826).

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

## Reproduce the native regression

The fixture is an ordinary TS-derived mod and map. It needs installed TS content,
.NET, Xvfb and the default graphics platform; it uses software GL and a private
Support directory. Cold-load automation also needs xdotool to resume the normal
load-game menu. No original assets are committed.

```sh
dotnet build -c Release
dotnet build OpenRA.Test/Fixtures/TerrainGeometry/Probe.csproj -c Release
python3 OpenRA.Test/Fixtures/TerrainGeometry/run.py \
  --engine "$PWD" --content /path/to/Support/Content --out /tmp/terrain-test --cold
```

The output contains `result.json`, `command.json`, runtime logs, `terrain.jsonl` and
an optional screenshot, and `inputs.json` with binary hashes. Assertions cover adjacent/live height edges, standing and
moving infantry/vehicle grounding, a raised control, HPF gates inside/across grids
compared to an independent reachability traversal, stationary visibility and a mod
policy that forces a shooter to choose a higher firing position before shooting.

For a pre-change engine build the probe with `-p:EnableRangeProbe=false
-p:OpenRAPath=/path/to/baseline -p:OutputPath=bin/baseline/` and run with `--baseline
--probe /absolute/path/to/baseline/OpenRA.TerrainProbe.dll`. The fixture reports real
failures rather than treating an unmodified build as a passing test.


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

## Mutable-layer and sprite regression

The native fixture above also primes a disconnected layer, changes endpoints with
unchanged costs, removes/restores terrain and checks provider Z plus direct/nearest
targets. Sprite checks cover null fallback, replacement and unchanged geometry.
There are 207 assertions in a fresh process and another 207 after cold load;
subsequent simulation events must match. Engine NUnit has 509 passes and two skips.

To exercise the negative control, build the same probe against pre-extension HEAD
`3e79a1f` using `-p:EnableLayerApi=false -p:EnableSpriteApi=false` and run its private
engine archive via `--engine` and `--probe`. This gives 197 passes and 10 expected
failures without an exception. Do not pass `--baseline` for that archive: it already
has the range API. That flag is only for the pre-range engine described above.

The fixture needs installed TS content, Xvfb and a .NET SDK; cold load also needs
xdotool. No original assets are included. To bound native memory on Linux, run it
under `systemd-run --user --scope -p MemoryMax=4G -p MemorySwapMax=0 -- python3 ...`.
This is scoped API regression evidence. The additional network runs below cover
lockstep behavior; they do not qualify a full TS campaign or every mod mechanic.


## Network acceptance

The fixture can start two real clients through the normal lobby, network order
manager and lockstep protocol. It records `World.SyncHash()` every simulation tick
and requires both hashes and the complete assertion/event streams to match. Save
requests are disabled in this mode; cold-load remains a separate process pair.

```sh
python3 OpenRA.Test/Fixtures/TerrainGeometry/run.py \
  --engine "$PWD" --content /path/to/Support/Content \
  --out /tmp/terrain-network --network
python3 OpenRA.Test/Fixtures/TerrainGeometry/run.py \
  --engine "$PWD" --content /path/to/Support/Content \
  --out /tmp/terrain-network-large --network --large-map
```

The ordinary map has 7,200 allocated cells; `--large-map` uses 128 by 256
(32,768 cells). Both runs pass 207 assertions on each client, with identical
hashes and events across all 425 ticks. Combined process-group RSS peaks at
1,160.34 MiB for the larger run, under a serial 4 GiB / zero-swap guard. This
is a bounded regression test, not a long-match throughput benchmark.

The final fresh/cold run also passes 207 assertions per process with matching
subsequent events, including sync hashes. `make check`, `make tests` (509 passed,
2 skipped), and `make TREAT_WARNINGS_AS_ERRORS=true test` for all four stock mods
pass. The TS consumer additionally exercises source road/rail/low bridge repair,
source event31 through Lua, cargo deaths, ice growth masks, geometry and LAT.
The API does not require a TS-specific hook for those mechanics. The TS campaign
VM and game content remain downstream work, separate from this API change.
