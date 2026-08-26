# Engine notes

The verified-facts ledger. Every claim this project makes about how the OpenRA
engine behaves lives here, with a source you can open and check.

**Read this before deriving an engine fact yourself.** Add an entry whenever you
verify something new. This is how a wrong assumption gets caught by the next
contributor instead of by a bug six weeks later.

All entries below were verified against the commit in `ENGINE_BASE`:

```
f7dbaa1b6c3f27bda002f878cb121e507a10c6b5   (upstream bleed, 2026-04-24)
```

Line numbers are valid for that commit only. When `ENGINE_BASE` moves, every
entry is re-checked as part of the `upstream-sync` PR. An entry that no longer
holds is **corrected, not deleted** — note what changed and when.

### Entry format

```markdown
## Short claim as a heading

- **Claim:** what is true, precisely.
- **Source:** path/to/File.cs:LINE
- **Verified against:** <commit>
- **Verified by:** @who — YYYY-MM-DD
- **Used by:** which feature depends on this
```

---

## Selection.Combine is virtual and safe to override

- **Claim:** `Selection.Combine(World, IEnumerable<Actor>, bool, bool)` is
  declared `public virtual`, so a subclass can intercept every selection change.
  `SelectionInfo : TraitInfo` (line 18) is an ordinary trait info, and
  `Selection` carries `[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]`
  (line 23), so it can be swapped out in mod rules like any other world trait.
  `Add` (line 49) and `Remove` (line 62) are also virtual.
- **Source:** `OpenRA.Mods.Common/Traits/World/Selection.cs:91`
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** F1 (persistent squads)

## The Selection trait is attached to the world actor in RA's rules

- **Claim:** `^BaseWorld` lists `Selection:` and `ControlGroups:` as plain world
  traits, so replacing `Selection` with our subclass is a two-line YAML change.
- **Source:** `mods/ra/rules/world.yaml:5` (`Selection:`) and `:6` (`ControlGroups:`)
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** F1 (persistent squads)

## Clicking takes only the first actor, box-select takes all

- **Claim:** inside `Combine`, when `isClick` is true the new selection is
  reduced with `.Take(1)`; when false the whole collection is used. Passing an
  already-resolved list with `isClick: false` is therefore how you select a
  group programmatically.
- **Source:** `OpenRA.Mods.Common/Traits/World/Selection.cs:91` (method body)
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** F1 (persistent squads)

## ControlGroups is the reference pattern for client-side group state

- **Claim:** `ControlGroups` is a world trait holding `List<Actor>[]`, implements
  `IControlGroups, ITick, IGameSaveTraitData`, and calls
  `world.Selection.Combine(...)` to apply a group. It is client-side and does not
  participate in the simulation. Our `SquadManager` follows the same shape,
  including save/restore via `IGameSaveTraitData`.
- **Source:** `OpenRA.Mods.Common/Traits/World/ControlGroups.cs`
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** F1 (persistent squads), L8 (determinism boundary)

## Hotkeys bind to C# via SingleHotkeyBaseLogic

- **Claim:** a hotkey handler subclasses `SingleHotkeyBaseLogic`, is annotated
  `[ChromeLogicArgsHotkeys("SomeKey")]`, and is registered by name on a `Logic:`
  line in the mod's chrome YAML. Key definitions themselves live in
  `mods/*/hotkeys/*.yaml` and `mods/ra/hotkeys.yaml`.
- **Source:** `OpenRA.Mods.Common/Widgets/Logic/SingleHotkeyBaseLogic.cs`;
  example handler
  `OpenRA.Mods.Common/Widgets/Logic/Ingame/Hotkeys/RemoveFromControlGroupHotkeyLogic.cs`;
  registration at `mods/ra/chrome/ingame-player.yaml:6`
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** F1, F2, F3 (all hotkeys)

## The in-game command bar is a chrome container we can extend

- **Claim:** `Container@COMMAND_BAR` holds the attack-move / force-move / guard /
  stop buttons, is driven by `CommandBarLogic`, and its buttons are 34×26 with
  24×24 icons drawn from the `command-icons` sprite collection.
- **Source:** `mods/ra/chrome/ingame-player.yaml:51`; icon regions at
  `mods/ra/chrome.yaml:221`
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** F1, F2, F3 (UI buttons)
- **Note:** new icons need new 24×24 regions in the sprite sheet, or a new
  collection pointing at our own PNG. Deferred to sprint 07.

## Mods load extra assemblies through mod.yaml

- **Claim:** `Assemblies:` is a single comma-separated line listing the DLLs a
  mod loads. RA currently loads `OpenRA.Mods.Common.dll, OpenRA.Mods.Cnc.dll`;
  we append `OpenRA.Mods.Tcd.dll`.
- **Source:** `mods/ra/mod.yaml:112`
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** sprint 01 (project wiring)

## Production is queued with Order.StartProduction

- **Claim:** `Order.StartProduction(Actor subject, string item, int count, bool queued = true)`
  builds a `StartProduction` order carrying the count in `ExtraData` and the
  actor type in `TargetString`. `ProductionQueue.CanBuild(ActorInfo)` (line 330)
  and `CanQueue(ActorInfo, out string, out string)` (line 404) gate it, so cash
  and prerequisite handling come for free.
- **Source:** `OpenRA.Game/Network/Order.cs:295`;
  `OpenRA.Mods.Common/Traits/Player/ProductionQueue.cs:330`, `:404`
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** F3 (squad reproduction)

## Production completion is observable via INotifyProduction

- **Claim:** `INotifyProduction.UnitProduced(Actor self, Actor other, CPos exit)`
  fires on the producing actor. `INotifyOtherProduction.UnitProducedByOther(...)`
  (line 152) fires more broadly and carries the production type and init data.
  `INotifyUnitProduced` **does not exist** — a plausible name that is not real.
- **Source:** `OpenRA.Mods.Common/TraitsInterfaces.cs:151`, `:152`
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** F3 (squad reproduction)

## Rally points already exist and are order-driven

- **Claim:** `RallyPoint` is a building trait with its own order, and produced
  units are sent to it deterministically by the producer. Reusing it is the
  cheap, sync-safe way to gather newly produced units (F3 v1).
- **Source:** `OpenRA.Mods.Common/Traits/Buildings/RallyPoint.cs`
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** F3 (squad reproduction)

## The engine targets .NET 10

- **Claim:** `TargetFramework` is `net10.0` for non-Mono builds, so a .NET 10 SDK
  is required. The last tagged release (`release-20250330`) targets `net6.0`,
  which is why this fork is based on a pinned `bleed` commit instead of that tag.
- **Source:** `Directory.Build.props`; `INSTALL.md`
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** sprint 01 (toolchain)

## Native libraries are downloaded, not system-installed

- **Claim:** on x86_64 the default build fetches precompiled SDL2, FreeType,
  OpenAL and Lua 5.1 via NuGet. System packages are only needed for
  `make DEPENDENCIES=system` or non-x86_64 architectures.
- **Source:** `INSTALL.md`
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** sprint 01 (toolchain), CONTRIBUTING quick start

## Hotkey definitions are "KEY Modifier, Modifier"

- **Claim:** `Hotkey.TryParse` splits the value on a space into at most two
  fields: the `Keycode`, then the `Modifiers`. Multiple modifiers therefore go in
  the second field comma-separated (`NUMBER_1 Ctrl, Shift`). Space-separating
  them (`Q Ctrl Shift`) fails to parse and aborts mod loading.
- **Source:** OpenRA.Game/Input/Hotkey.cs:27; `Modifiers` enum at
  OpenRA.Game/Input/IInputHandler.cs:38; example at
  mods/common/hotkeys/control-groups.yaml:121
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel - 2026-08-26
- **Used by:** F1 (squad hotkeys)

## Chrome sheets must have power-of-two dimensions

- **Claim:** any PNG loaded as a chrome sheet becomes an OpenGL texture, and
  texture creation throws `InvalidDataException: Non-power-of-two array WxH`
  unless both dimensions are powers of two. The crash surfaces at render time
  inside `WidgetUtils.DrawPanel`, not at mod load, so a bad sheet looks like a
  graphics bug rather than an asset bug. Pad the sheet and anchor the content at
  the origin; the unused area costs nothing. Engine sheets follow this:
  glyphs.png is 256x256, glyphs-2x.png 512x512, glyphs-3x.png 1024x1024.
- **Source:** OpenRA.Platforms.Default/Texture.cs:84;
  OpenRA.Game/Graphics/Sheet.cs:53
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel - 2026-08-26
- **Used by:** F1 (squad command bar icons)

## The right mouse button only reaches the order generator on release

- **Claim:** `WorldInteractionControllerWidget.HandleMouseInput` calls `ApplyOrders`
  for the right button only when `mi.Event == MouseInputEvent.Up`. Right-button
  `Down` and `Move` events are never passed to `IOrderGenerator.Order` at all, so
  an order generator cannot see a right-drag as it happens. `GetCursor` is the
  hook that does run every frame with the cell under the cursor, so continuous
  tracking has to be done there.
- **Note:** this only applies while the active generator *is* a
  `UnitOrderGenerator`. A generator that is not one takes an earlier branch that
  forwards every event, which is why a standalone `OrderGenerator` behaves
  differently from a `UnitOrderGenerator` subclass.
- **Source:** OpenRA.Mods.Common/Widgets/WorldInteractionControllerWidget.cs:93
  (the `is not UnitOrderGenerator` branch) and :170 (the right-button Up branch)
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel - 2026-08-26
- **Used by:** F2 (drawn formations)

