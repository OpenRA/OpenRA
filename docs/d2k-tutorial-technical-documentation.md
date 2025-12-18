# D2K Tutorial - Technical Documentation

## Architecture

```
MainMenuLogic.cs          →  Tutorial button + save/resume logic
    ↓
mods/d2k/maps/tutorial/
├── map.yaml              →  Map config, player/enemy actors
├── rules.yaml            →  Starting cash (3000), Lua reference
└── tutorial.lua          →  15-objective progression logic
    ↓
mods/d2k/fluent/
├── chrome.ftl            →  Button text, resume dialog
└── tutorial.ftl          →  Mentat dialogue (106 lines)
```

## Key Files

| File | What It Does |
|------|--------------|
| `MainMenuLogic.cs:133-180` | Tutorial button handler, `FindTutorialSave()`, resume dialog |
| `mainmenu.yaml:105-111` | TUTORIAL_BUTTON widget at Y:140 in singleplayer menu |
| `missions.yaml:1-2` | Registers tutorial map |
| `tutorial.lua` | 15 objectives, enemy wake-on-damage behavior |

## 15 Objectives

| # | Objective | Detection |
|---|-----------|-----------|
| 1-4 | Camera, Selection, Movement, Control Groups | Time-based (can't detect in Lua) |
| 5 | Deploy MCV | `construction_yard` exists |
| 6 | Place Concrete | Time-based (actors self-destruct) |
| 7-8 | Wind Trap, Refinery | Building exists |
| 9 | Harvesting | Gain 700 credits |
| 10-13 | Barracks, Infantry×4, Factory, Trikes×3 | Unit/building counts |
| 14 | Combat | All enemy units dead |
| 15 | Victory | Auto-complete |

## Enemy Behavior

- Start passive (`HoldFire` stance)
- On any damage → all enemies set to `AttackAnything` + `Hunt()`

## Save/Resume Flow

```
Click Tutorial → FindTutorialSave() scans ~/.config/openra/Saves/d2k/
    ↓
Save exists?  →  Yes → Show "Resume / Start New" dialog
              →  No  → StartNewTutorial() directly
```

## Decision Log

| Decision | Why |
|----------|-----|
| Title == "Tutorial" matching | Simple, no special metadata needed |
| Time-based for camera/selection/control groups | Lua can't detect these actions |
| 700 credit harvest goal | ~1 harvester load, validates understanding |
| 3000 starting credits | Build initial base without waiting |
| Passive enemies | Practice building, then combat when ready |

## Testing

1. `make all && ./OpenRA.exe Game.Mod=d2k`
2. Singleplayer → Tutorial
3. Verify: button works, objectives advance, save/resume works, enemies wake on attack
