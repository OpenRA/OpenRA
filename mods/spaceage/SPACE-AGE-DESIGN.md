# Space-Age Total Conversion of OpenRA — Engineering Design & Roadmap

*Target repo: `dr-richard-barker/Lunar_Red_Alert` (fork of OpenRA `bleed`). Engine: C# / .NET, SDL2 + OpenGL, GPLv3.*

---

## 0. Reality check — read this before the roadmap

Your 5-part brief is a good decomposition, but one premise in it is not true today and it changes everything downstream, so I'm putting it first rather than burying it in Phase 4.

**OpenRA has no WebAssembly build and cannot run in a browser as-is.** Concretely:

- Rendering goes through `OpenRA.Platforms.Default`, a native SDL2 + **desktop OpenGL** layer (P/Invoke into `SDL2.dll`/`libSDL2`). There is no WebGL/WebGPU renderer and no Emscripten path.
- The engine uses desktop threads (`System.Threading`), blocking file I/O, and a native audio backend. Blazor WebAssembly runs .NET IL in the browser, but it is single-threaded-ish, sandboxed, and has **no OpenGL** — so even a clean `dotnet publish` to Wasm gives you a binary that cannot draw a single frame.
- **GitHub Pages only serves static files.** It can *host* a Wasm bundle, but it cannot *create* one. Pages is not the blocker; the absence of a web renderer is.

So "compile the mod to Wasm and load it via the VFS in a browser" is **not a Phase-4 task — it is an unsolved engine port** worth many engineer-months on its own (you'd be writing a WebGL backend for `IPlatform`/`IGraphicsContext`, replacing SDL input/audio, and reworking the threading + VFS for the browser sandbox). No fork of OpenRA has shipped this.

That leaves a genuine fork in the road. Both are legitimate; they optimise for different halves of your sentence *"a new game that runs online from a github page in the browser."*

| | **Path A — Faithful OpenRA conversion** | **Path B — Browser-native lunar RTS** |
|---|---|---|
| Engine | Real OpenRA mod (C#) | New HTML5 Canvas/WebGL + JS/TS |
| Runs in browser? | **No** — desktop download (Win/Linux/Mac) | **Yes** — playable on GitHub Pages today |
| GitHub Pages role | Landing + download/patch-notes site | Hosts the actual game |
| The C# below | All real & shippable | Reference/design only |
| Effort to "playable" | Weeks (mod), months+ (any web port) | Days for a prototype |
| Fidelity to OpenRA gameplay | 100% | You rebuild what you want |

**Recommendation (hybrid):** Build the total conversion as a proper OpenRA mod (Path A) — that's where all the C# in this document is directly usable — and *separately* ship a lightweight browser prototype (Path B) on GitHub Pages so there is something playable online now and a home for the project. Don't try to force OpenRA-in-the-browser; it will consume the whole budget with nothing to show.

Everything below answers your 5 questions for **Path A** (the real engine work), with an honest Phase-4, plus a Path-B appendix.

---

## 1. Architecture analysis — `OpenRA.Game` vs `OpenRA.Mods.Common`

Key principle of OpenRA's design that works in your favour: **almost nothing about "the world is Earth" is hard-coded in `OpenRA.Game`.** Physics, gravity, and "atmosphere" are not engine concepts at all — they are emergent from *traits* and *projectile/weapon definitions*, which live in `OpenRA.Mods.Common` and in YAML. That means you make a "cold vacuum world" mostly by *adding traits*, not by forking the core.

Where each concern lives:

- **`OpenRA.Game`** (touch sparingly): the trait/actor framework (`Actor`, `TraitInfo`, `ITick`, condition system), the `World` tick loop, `WPos`/`WDist`/`WVec` fixed-point world geometry, the VFS (`FileSystem`), and the `IPlatform` rendering seam. You only modify this if you need a **truly global** hook that no per-actor trait can express — e.g. a world-wide gravity constant that projectiles read. Even then, prefer a **world actor trait** over a core edit.
- **`OpenRA.Mods.Common`** (your main workspace): `Mobile` (movement), the `ISpeedModifier` aggregation, projectile classes (`Projectiles/Bullet.cs`, `Missile.cs`, `GravityBomb.cs`), `DamagedByTerrain`, the condition traits (`GrantConditionOnTerrain`, `ProximityExternalCondition`, `ExternalCondition`), health (`IHealth`, `Damage`). **Every new mechanic you want is a new class here.**
- **`mods/` YAML** (no compile step): actor stats, weapon/projectile parameters, terrain templates, sequences. The gravity/O2 *numbers* live here; the *behaviour* lives in traits.

Design rule: **prefer a new `ConditionalTrait<T>` in Mods.Common over any edit to OpenRA.Game.** It keeps you rebaseable against upstream `bleed` (important — OpenRA moves fast) and it's how the engine is meant to be extended.

The three "non-Earth" concepts map cleanly onto existing engine machinery:

| Space-age concept | Existing OpenRA machinery to reuse | New code needed |
|---|---|---|
| Vacuum damages exposed units | `DamagedByTerrain` (ITick + `InflictDamage`) | A near-clone gated on a `pressurized` condition |
| Pressurised domes / airlocks | `ProximityExternalCondition` (grants a condition in-range) | Config only |
| Oxygen as a consumable | none native — new resource trait | `Oxygen : PausableConditionalTrait, ITick` |
| Low-gravity movement | `ISpeedModifier` interface | `GravitySpeedModifier : ISpeedModifier` |
| Ballistic arcs / lob weapons | `Bullet.LaunchAngle`, `GravityBomb.Acceleration` | Scale vertical accel by a world gravity factor |

---

## 2. Environment logic — atmosphere & gravity

### 2a. Atmospheric mechanics (oxygen instead of fuel)

Model O2 as a per-unit resource that (a) drains each tick, (b) refills inside a pressurised zone, and (c) grants a `no-oxygen` condition when empty. Other traits (the vacuum-damage trait, engine-shutdown, speed penalty) then *react* to that condition. This is the idiomatic OpenRA composition: **one trait owns the resource, conditions broadcast the state, many traits consume it.**

```csharp
// OpenRA.Mods.Common/Traits/SpaceAge/Oxygen.cs
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
    [Desc("Unit carries a finite oxygen supply that drains over time and refills",
          "while inside a pressurised zone. Grants a condition when depleted.")]
    public class OxygenInfo : PausableConditionalTraitInfo
    {
        [Desc("Maximum oxygen units.")]
        public readonly int Capacity = 6000;

        [Desc("Oxygen consumed per tick while exposed to vacuum.")]
        public readonly int DrainRate = 1;

        [Desc("Oxygen restored per tick while pressurised.")]
        public readonly int RefillRate = 25;

        [GrantedConditionReference]
        [Desc("Condition granted while oxygen is fully depleted.")]
        public readonly string DepletedCondition = "no-oxygen";

        [ConsumedConditionReference]
        [Desc("The unit is treated as pressurised while it has this condition",
              "(granted externally, e.g. by a dome's ProximityExternalCondition).")]
        public readonly BooleanExpression PressurisedCondition = null;

        public override object Create(ActorInitializer init) { return new Oxygen(this); }
    }

    public class Oxygen : PausableConditionalTrait<OxygenInfo>, ITick, ISync, INotifyCreated
    {
        [Sync] public int Current;
        int depletedToken = Actor.InvalidConditionToken;
        bool pressurised;

        public Oxygen(OxygenInfo info) : base(info) { Current = info.Capacity; }

        // Wire the pressurised condition through the variable-observer system so the
        // dome/airlock grant flips `pressurised` without us polling every actor.
        protected override void Created(Actor self)
        {
            base.Created(self);
            if (Info.PressurisedCondition != null)
            {
                var consumer = self.TraitsImplementing<IConditionConsumer>(); // observed via VariableObserver
            }
        }

        void ITick.Tick(Actor self)
        {
            if (IsTraitDisabled || IsTraitPaused)
                return;

            // Pressurised state is read from the granted condition each tick.
            pressurised = Info.PressurisedCondition == null ? false
                : Info.PressurisedCondition.Evaluate(self.TraitsImplementing<IObservesVariables>()
                    .Any() ? self.CurrentConditions() : self.CurrentConditions());

            if (pressurised)
                Current = System.Math.Min(Info.Capacity, Current + Info.RefillRate);
            else
                Current = System.Math.Max(0, Current - Info.DrainRate);

            var depleted = Current <= 0;
            if (depleted && depletedToken == Actor.InvalidConditionToken)
                depletedToken = self.GrantCondition(Info.DepletedCondition);
            else if (!depleted && depletedToken != Actor.InvalidConditionToken)
                depletedToken = self.RevokeCondition(depletedToken);
        }

        public int Percent => Info.Capacity == 0 ? 0 : Current * 100 / Info.Capacity;
    }
}
```

> Note: the exact `PressurisedCondition` evaluation should go through OpenRA's `IObservesVariables` / `VariableObserverNotifier` plumbing (the same mechanism `GrantConditionOnPrerequisite` etc. use) rather than the sketch above — I've flagged it inline. The load-bearing pattern (own the resource → grant a condition → let others react) is correct and idiomatic.

**Wiring it in YAML** (`mods/spaceage/rules/infantry.yaml`) — no recompile for the numbers:

```yaml
E1:
    Oxygen:
        Capacity: 6000
        DrainRate: 2
        RefillRate: 40
        DepletedCondition: no-oxygen
        PressurisedCondition: pressurised
    # a dome grants `pressurised` to units in range:
```
```yaml
# mods/spaceage/rules/structures.yaml  — the dome
DOME:
    ProximityExternalCondition@LIFESUPPORT:
        Condition: pressurised
        Range: 8c0          # 8 cells
```

### 2b. Vacuum environmental damage (the trait you specifically asked for)

Directly modelled on the confirmed real `DamagedByTerrain` (`ConditionalTrait<...>, ITick`, `self.InflictDamage(self.World.WorldActor, new Damage(...))`). The difference: instead of keying on *terrain type*, it keys on **being un-pressurised AND out of oxygen**, and it exempts sealed unit types (they can pass a `sealed-hull` condition).

```csharp
// OpenRA.Mods.Common/Traits/SpaceAge/DamagedByVacuum.cs
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
    [Desc("Periodically damages the actor while it is exposed to vacuum",
          "(no pressurised condition) and its oxygen is depleted. Sealed",
          "hulls are immune. Model of environmental hazard, styled after DamagedByTerrain.")]
    public class DamagedByVacuumInfo : ConditionalTraitInfo, Requires<IHealthInfo>
    {
        [Desc("Damage received per DamageInterval ticks while exposed.")]
        public readonly int Damage = 250;

        [Desc("Delay in ticks between damage applications.")]
        public readonly int DamageInterval = 16;

        [Desc("Damage types for armour/warhead interaction (e.g. Prone, Vacuum).")]
        public readonly BitSet<DamageType> DamageTypes = default;

        [ConsumedConditionReference]
        [Desc("While the unit has this condition it is protected (pressurised cabin / dome).")]
        public readonly BooleanExpression SafeCondition = null;

        [Desc("If true, only damage once oxygen is fully depleted (needs the Oxygen trait).")]
        public readonly bool RequireOxygenDepleted = true;

        public override object Create(ActorInitializer init) { return new DamagedByVacuum(this); }
    }

    public class DamagedByVacuum : ConditionalTrait<DamagedByVacuumInfo>, ITick
    {
        int ticks;

        public DamagedByVacuum(DamagedByVacuumInfo info) : base(info) { }

        void ITick.Tick(Actor self)
        {
            if (IsTraitDisabled)
                return;

            // Protected by a dome/sealed hull? Reset the clock and bail.
            if (Info.SafeCondition != null &&
                Info.SafeCondition.Evaluate(self.CurrentConditions()))
            {
                ticks = 0;
                return;
            }

            if (Info.RequireOxygenDepleted)
            {
                var o2 = self.TraitOrDefault<Oxygen>();
                if (o2 != null && o2.Current > 0)
                {
                    ticks = 0;
                    return;   // still has air in the tank
                }
            }

            if (--ticks <= 0)
            {
                ticks = Info.DamageInterval;
                // Same call shape confirmed in DamagedByTerrain:
                self.InflictDamage(self.World.WorldActor,
                    new Damage(Info.Damage, Info.DamageTypes));
            }
        }
    }
}
```

```yaml
# mods/spaceage/rules/infantry.yaml
E1:
    DamagedByVacuum:
        Damage: 300
        DamageInterval: 16
        DamageTypes: Vacuum, ExplosionDeath
        SafeCondition: pressurised
        RequireOxygenDepleted: true
```

Because it's a `ConditionalTrait`, you can switch whole factions' vacuum-immunity on/off with a condition (e.g. a researched "Sealed Suits" upgrade grants `sealed-hull`, and you set `SafeCondition: pressurised || sealed-hull`).

### 2c. Gravity modifiers

Two separate effects, two different seams:

**(i) Movement speed** — clean, no core edit. `Mobile` already multiplies its base speed by *every* trait implementing `ISpeedModifier`. So low-gravity locomotion is just:

```csharp
// OpenRA.Mods.Common/Traits/SpaceAge/GravitySpeedModifier.cs
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
    [Desc("Scales move speed to simulate low gravity. Can be gated by a condition",
          "so wheeled vs. legged units differ, or so it only applies off-road.")]
    public class GravitySpeedModifierInfo : ConditionalTraitInfo
    {
        [Desc("Speed multiplier as a percentage. >100 = faster (bounding strides).")]
        public readonly int Modifier = 140;

        public override object Create(ActorInitializer init) { return new GravitySpeedModifier(this); }
    }

    public class GravitySpeedModifier : ConditionalTrait<GravitySpeedModifierInfo>, ISpeedModifier
    {
        public GravitySpeedModifier(GravitySpeedModifierInfo info) : base(info) { }

        int ISpeedModifier.GetSpeedModifier()
        {
            return IsTraitDisabled ? 100 : Info.Modifier;
        }
    }
}
```

That's the whole thing — `Mobile` aggregates it automatically. Legged infantry get `Modifier: 140` (moon-bounding), tracked vehicles get `110`, hover units unchanged.

**(ii) Projectile trajectory** — this *does* need engine-side work, and I won't pretend a global hook exists. OpenRA projectiles carry their own physics in their `Info`: `Bullet` uses `LaunchAngle` + airborne interpolation; `GravityBomb` applies a per-tick `Acceleration` (WDist) downward — that acceleration *is* gravity. There is no world-level gravity constant.

Two honest options:

- **YAML-only (cheapest):** just re-tune every arcing weapon in `weapons.yaml` — lower `GravityBomb.Acceleration`, flatter `Bullet.LaunchAngle`, longer `Range`. No C#. Ships immediately. Downside: it's per-weapon bookkeeping, not a physically-unified "the moon has 1/6 g."
- **One world trait + a few projectile edits (correct):** add a `LowGravity` trait on the world actor exposing `int GravityPercent = 16;`, then in the ~3 projectile `Tick` methods that model vertical accel (`GravityBomb`, arcing `Bullet`, `Missile` vertical component) scale the downward term by `world.WorldActor.Trait<LowGravity>().GravityPercent / 100`. This is a small, localized engine change (a handful of lines in Mods.Common/Projectiles), keeps one authoritative gravity value, and rebases cleanly.

```csharp
// OpenRA.Mods.Common/Traits/World/LowGravity.cs  (attach to the World actor in defaults.yaml)
using OpenRA.Traits;
namespace OpenRA.Mods.Common.Traits
{
    [Desc("World-wide gravity as a percentage of Earth. Read by arcing projectiles.")]
    public class LowGravityInfo : TraitInfo
    {
        [Desc("100 = Earth, 16 = Moon, 38 = Mars.")]
        public readonly int GravityPercent = 16;
        public override object Create(ActorInitializer init) { return new LowGravity(this); }
    }
    public class LowGravity { public readonly int GravityPercent;
        public LowGravity(LowGravityInfo info) { GravityPercent = info.GravityPercent; } }
}
```
Then inside `GravityBomb.Tick` (Mods.Common/Projectiles), the one edit:
```csharp
// was: velocity -= new WVec(WDist.Zero, WDist.Zero, info.Acceleration);
var g = args.SourceActor.World.WorldActor.Trait<LowGravity>().GravityPercent;
velocity -= new WVec(WDist.Zero, WDist.Zero,
    new WDist(info.Acceleration.Length * g / 100));
```

---

## 3. Asset pipeline — Martian/Lunar terrain

OpenRA is a 2.5D sprite/tileset engine, **not** a 3D heightmap engine. That's a crucial constraint: there is no runtime heightmap displacement. "Craters" are *authored tiles + cliff ramps + a lookup-table of terrain types*, not procedural geometry. So "procedural Martian heightmaps" becomes "procedurally *generate the tileset and maps*, then bake to OpenRA's tile format."

Pipeline that stays YAML-compatible:

1. **Define the terrain types** in `mods/spaceage/tilesets/lunar.yaml` — e.g. `Regolith`, `CraterFloor`, `CraterWall` (impassable/cliff), `Basalt`, `IceDeposit`, `Vacuum`. Each terrain type carries movement class + your custom flags (the `DamagedByVacuum.SafeCondition` and `Oxygen.PressurisedCondition` do **not** need terrain changes, but you can add a `GrantConditionOnTerrain` so, say, ice deposits grant a slow condition).
2. **Author/generate the tile sheet.** Two sub-options:
   - *Hand/AI-generated sprites* baked into a `.png` sheet, referenced by the tileset templates. Highest quality.
   - *Procedural*: run an offline Python/`noise` step (Perlin/Worley for regolith + crater rings), render to top-down sprite tiles at OpenRA's tile pixel size, and emit the tileset template YAML. This is a **content-generation script that runs on your machine**, not in the engine — output is ordinary OpenRA assets.
3. **Map generation.** OpenRA maps are `.oramap` (zip of `map.yaml` + a tile/resource layer binary). Write a small generator (C# via `OpenRA.Utility`, or Python emitting the binary layer) that stamps craters as clusters of `CraterWall` ring + `CraterFloor` centre, scatters `IceDeposit` (your new "ore"/resource), and places dome-buildable flat zones. Because actor placement and rules are **plain YAML**, your generated maps reference the same `E1`, `DOME`, etc. definitions — full compatibility, no engine change.
4. **Resource = ice/Helium-3** instead of ore: OpenRA's `ResourceLayer`/`ResourceRenderer` are configurable in YAML; reskin the ore sprites to ice and rename in the tileset. No C#.

Net: the "heightmap" language is a mismatch for a 2.5D sprite engine; the *deliverable* is a **procedural tileset+map generator** whose output is standard OpenRA assets referenced by unchanged actor YAML.

---

## 4. Web portability — the honest version

Restating §0 in engineering terms, since this is where the brief and reality diverge most:

- **You cannot `dotnet publish -r browser-wasm` OpenRA and get a game.** Blazor WASM gives you .NET-in-the-browser, but the process dies the moment it reaches `OpenRA.Platforms.Default` — there is no `IGraphicsContext` implementation for WebGL, no SDL input/audio in the sandbox, and the threading/VFS assume desktop. There is no "custom mods loaded via VFS" story because there is no running renderer to load them *into*.
- **What a real web port would require** (scoping it so you can decide, not so you'll do it): a new `OpenRA.Platforms.Web` implementing `IPlatform`/`IGraphicsContext`/`IWindow` against **WebGL 2** (translating the `glsl/` shaders), an input/audio backend over browser APIs, reworking `System.Threading` usage for the browser's cooperative model, and adapting the VFS to fetch mod packages over HTTP into an in-memory filesystem. That is a multi-month engine subproject and, to be clear, **no one has shipped it for OpenRA.** The rendering-loop concern in your brief is real: the browser owns the frame via `requestAnimationFrame`, so OpenRA's own `Game.Loop` would have to be inverted to be driven *by* rAF rather than driving its own timer — a structural change, not a flag.
- **The mods themselves are the *easy* part and are web-agnostic.** Everything in §1–3 (traits + YAML + assets) is pure managed code and data; none of it is what blocks the browser. So building the mod now loses you nothing even if a web port never happens.

**Pragmatic recommendation:** decouple "runs in a browser" from "is OpenRA."
- Ship the **OpenRA total conversion** as a desktop mod (real, weeks-scale) — that's Path A and all the code above.
- Ship a **browser-native lunar RTS prototype** (Path B, HTML5 Canvas/WebGL) that actually deploys to GitHub Pages *now* and carries the same fantasy: 1/6-g bounding movement, an O2 meter that drains in vacuum and refills near domes, crater terrain that blocks LoS/pathing, ice as the resource. It reuses your design (§2 mechanics) but in ~1–2k lines of TS instead of an engine port. GitHub Pages hosts it directly (static `index.html` + bundle).
- Use GitHub Pages as the **project hub** either way: landing page, lore, desktop-build download links, and the embedded web prototype.

I can build the Path-B prototype and have it live on a GitHub Page — say the word and I'll scaffold it.

---

## 5. Four-phase roadmap (rewritten honestly)

**Phase 1 — Core hooks (gravity/physics).** `mods/spaceage` skeleton (fork `ra` mod as base). Add `LowGravity` world trait + the ≤10-line projectile-accel edits in `GravityBomb`/arcing `Bullet`. Add `GravitySpeedModifier : ISpeedModifier`. Deliverable: units bound in low-g, artillery lobs float — verifiable in a test map. *No changes to OpenRA.Game.*

**Phase 2 — Actor traits (O2 + hazards).** Implement `Oxygen`, `DamagedByVacuum`, wire domes via `ProximityExternalCondition`, add a "Sealed Suits" upgrade granting `sealed-hull`. UI: an O2 bar (a `SelectionDecoration`-style trait or pip provider). Deliverable: infantry suffocate in vacuum, survive under domes, vehicles are sealed. Balance in YAML.

**Phase 3 — Visuals & terrain.** Build the procedural tileset+map generator (§3), author/generate regolith + crater + ice sprites, reskin resource to ice/He-3, new sequences/palette (cold, high-contrast, black sky). Deliverable: playable lunar skirmish maps.

**Phase 4 — Distribution (not "compile to Wasm").** Two tracks: (a) **Desktop**: use OpenRA's existing `packaging/` to produce Win/Linux/Mac installers of the total conversion. (b) **Web**: build & deploy the Path-B browser prototype to GitHub Pages as the public face. *Only pursue a true OpenRA→WebGL port here if it becomes a funded goal of its own — it is not a packaging step.*

---

## Appendix A — File manifest (Path A)

```
OpenRA.Mods.Common/Traits/SpaceAge/
    Oxygen.cs
    DamagedByVacuum.cs
    GravitySpeedModifier.cs
OpenRA.Mods.Common/Traits/World/
    LowGravity.cs
OpenRA.Mods.Common/Projectiles/     # small edits, not new files
    GravityBomb.cs   (scale Acceleration by LowGravity)
    Bullet.cs        (scale arc by LowGravity)
mods/spaceage/
    mod.yaml
    rules/{infantry,vehicles,structures,defaults}.yaml
    weapons/*.yaml
    tilesets/lunar.yaml
    maps/*.oramap
    tools/gen_tileset.py, gen_map.py   # offline procedural generators
```

## Appendix B — Path B (browser prototype) sketch

A single static site deployable to GitHub Pages: `index.html` + a `<canvas>` + one TS bundle. Core loop driven by `requestAnimationFrame`; entity-component objects for units; a tile grid with crater/ice/regolith; per-unit `o2` field draining in vacuum cells and refilling within a dome radius; movement speed scaled by a `gravity` constant. No build server required — Pages serves it as-is. This is the fastest route to "runs online in the browser," and it reuses every mechanic designed in §2.
