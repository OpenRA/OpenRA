# Projectile gravity patch

`GravitySpeedModifier` needs no engine edit (Mobile aggregates `ISpeedModifier`).
Projectile arcs **do** need a small edit, because OpenRA projectiles carry their own
physics in their `Info` and there is no world-level gravity hook. This patch wires the
`LowGravity` world trait into the two projectile types that model vertical acceleration.

> Do these edits in your `bleed` checkout. Field/variable names below are the current
> upstream names but **verify them against your exact revision** — projectile internals
> drift between releases. Grep first: `git grep -n "Acceleration" OpenRA.Mods.Common/Projectiles`.

---

## 1. `GravityBomb.cs`

`GravityBomb` applies a constant downward acceleration each tick — that term *is* gravity.
Find the line in `Tick(World world)` that subtracts the acceleration from velocity, e.g.:

```csharp
// BEFORE
velocity -= new WVec(WDist.Zero, WDist.Zero, info.Acceleration);
```

Scale it by the world gravity percentage:

```csharp
// AFTER
var gravity = args.SourceActor.World.WorldActor.TraitOrDefault<LowGravity>();
var accel = gravity != null ? gravity.Scale(info.Acceleration.Length) : info.Acceleration.Length;
velocity -= new WVec(WDist.Zero, WDist.Zero, new WDist(accel));
```

(Cache the `LowGravity` reference in the ctor rather than looking it up every tick if you
prefer — it never changes during a game.)

## 2. `Bullet.cs` (arcing shots)

`Bullet` uses `LaunchAngle` + a height interpolation across the flight rather than a raw
acceleration, so "gravity" here means *how high the arc peaks*. The lowest-touch option is
to scale the interpolated height by inverse gravity so shots float higher and travel flatter
on the Moon. Locate where the per-tick `z`/height offset is computed from the launch angle and
multiply the vertical component by `100 * 100 / gravityPercent` (i.e. `Scale` inverted), guarded
the same way as above.

If you would rather not touch `Bullet`, leave it and just re-tune `LaunchAngle`/`Range` per
weapon in YAML (see `weapons/weapons.yaml`) — visually similar, zero engine change.

---

## Alternative: YAML-only, zero engine edits

Skip this patch entirely and re-tune arcing weapons in `weapons/weapons.yaml`:
lower `GravityBomb.Acceleration`, flatten `Bullet.LaunchAngle`, extend `Range`.
You lose a single authoritative gravity constant, but you ship immediately with no C#.
Start with the YAML route; adopt this patch only when you want physically-unified gravity.
