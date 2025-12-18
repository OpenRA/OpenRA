---
date: 2025-11-24T22:53:14-06:00
researcher: Claude
git_commit: 6a50452cc03c2f24109ecd71316ccbf1d9827b44
branch: feat/d2k-tutorial
repository: OpenRA
topic: "D2K Tutorial Mode System Research"
tags: [research, codebase, d2k, tutorial, singleplayer, menu, missions, lua]
status: complete
last_updated: 2025-11-24
last_updated_by: Claude
---

# Research: D2K Tutorial Mode System

**Date**: 2025-11-24T22:53:14-06:00
**Researcher**: Claude
**Git Commit**: 6a50452cc03c2f24109ecd71316ccbf1d9827b44
**Branch**: feat/d2k-tutorial
**Repository**: OpenRA

## Research Question

How does the D2K tutorial system work in OpenRA? What files, components, and systems are involved in the singleplayer menu, tutorial button, tutorial map, and mission scripting?

## Summary

The D2K tutorial system consists of several interconnected components:

1. **UI Layer**: A Tutorial button in the singleplayer menu (`mods/d2k/chrome/mainmenu.yaml`) connected to logic in `MainMenuLogic.cs`
2. **Mission Configuration**: The tutorial is defined as a mission in `mods/d2k/missions.yaml`
3. **Map Layer**: A dedicated tutorial map at `mods/d2k/maps/tutorial/` with map.yaml, rules.yaml, and Lua scripting
4. **Scripting Layer**: `tutorial.lua` implements 15 progressive objectives teaching RTS basics
5. **Localization**: Tutorial text strings in `mods/d2k/fluent/tutorial.ftl` and `chrome.ftl`

The tutorial button detects tutorial saves and offers resume/restart options. The game launches the tutorial as a mission (using `MissionSelector` visibility) and executes the Lua script that guides players through objectives.

## Detailed Findings

### 1. Singleplayer Menu UI

**File**: `mods/d2k/chrome/mainmenu.yaml:79-133`

The D2K singleplayer menu (`SINGLEPLAYER_MENU`) contains these buttons:
- `SKIRMISH_BUTTON` (line 91-97) - Y: 60
- `MISSIONS_BUTTON` (line 98-104) - Y: 100
- `TUTORIAL_BUTTON` (line 105-111) - Y: 140
- `LOAD_BUTTON` (line 112-118) - Y: 180
- `ENCYCLOPEDIA_BUTTON` (line 119-125) - Y: 220 (labeled "Mentat")
- `BACK_BUTTON` (line 126-133) - Y: 300

The Tutorial button is defined as:
```yaml
Button@TUTORIAL_BUTTON:
    X: PARENT_WIDTH / 2 - WIDTH / 2
    Y: 140
    Width: 140
    Height: 30
    Text: button-singleplayer-menu-tutorial
    Font: Bold
```

### 2. Main Menu Logic (C#)

**File**: `OpenRA.Mods.Common/Widgets/Logic/MainMenuLogic.cs`

The `MainMenuLogic` class handles all singleplayer menu button interactions.

**Tutorial Button Logic (lines 143-171)**:
```csharp
var tutorialButton = singleplayerMenu.GetOrNull<ButtonWidget>("TUTORIAL_BUTTON");
if (tutorialButton != null)
{
    var tutorialMap = modData.MapCache.FirstOrDefault(m =>
        m.Status == MapStatus.Available &&
        m.Visibility.HasFlag(MapVisibility.MissionSelector) &&
        m.Title == "Tutorial");

    tutorialButton.Disabled = tutorialMap == null;
    tutorialButton.OnClick = () =>
    {
        if (tutorialMap == null)
            return;

        var tutorialSave = FindTutorialSave(modData, tutorialMap.Uid);
        if (tutorialSave != null)
        {
            ConfirmationDialogs.ButtonPrompt(modData,
                title: TutorialResumeTitle,
                text: TutorialResumePrompt,
                onConfirm: () => LoadTutorialSave(tutorialSave, tutorialMap.Uid),
                confirmText: TutorialResumeButton,
                onCancel: () => StartNewTutorial(tutorialMap.Uid),
                cancelText: TutorialStartNewButton);
        }
        else
            StartNewTutorial(tutorialMap.Uid);
    };
}
```

**Tutorial Detection**: The system searches for a map with:
- `MapStatus.Available`
- `MapVisibility.MissionSelector` flag
- `Title == "Tutorial"`

**FindTutorialSave Method (lines 575-599)**:
```csharp
static string FindTutorialSave(ModData modData, string tutorialMapUid)
{
    var baseSavePath = Path.Combine(Platform.SupportDir, "Saves", modData.Manifest.Id, modData.Manifest.Metadata.Version);
    if (!Directory.Exists(baseSavePath))
        return null;

    var savePaths = Directory.GetFiles(baseSavePath, "*.orasav", SearchOption.AllDirectories)
        .OrderByDescending(File.GetLastWriteTime);

    foreach (var savePath in savePaths)
    {
        try
        {
            var save = new GameSave(savePath);
            if (save.GlobalSettings.Map == tutorialMapUid)
                return savePath;
        }
        catch { /* Skip invalid save files */ }
    }
    return null;
}
```

**StartNewTutorial Method (lines 601-612)**:
```csharp
void StartNewTutorial(string tutorialMapUid)
{
    SwitchMenu(MenuType.None);
    Game.BeforeGameStart += OnTutorialStart;

    var orders = new List<Order>
    {
        Order.Command($"state {Session.ClientState.Ready}")
    };

    Game.CreateAndStartLocalServer(tutorialMapUid, orders);
}
```

**LoadTutorialSave Method (lines 614-626)**:
```csharp
void LoadTutorialSave(string savePath, string tutorialMapUid)
{
    SwitchMenu(MenuType.None);
    Game.BeforeGameStart += OnTutorialStart;

    var orders = new List<Order>
    {
        Order.FromTargetString("LoadGameSave", Path.GetFileName(savePath), true),
        Order.Command($"state {Session.ClientState.Ready}")
    };

    Game.CreateAndStartLocalServer(tutorialMapUid, orders);
}
```

### 3. Mission Configuration

**File**: `mods/d2k/missions.yaml`

The tutorial is registered as the first mission:
```yaml
Tutorial:
    tutorial

Atreides Campaign:
    atreides-01a
    atreides-01b
    ...
```

This file lists all missions grouped by campaign. The `Tutorial` entry points to the `tutorial` map directory.

### 4. Tutorial Map Structure

**Directory**: `mods/d2k/maps/tutorial/`

Contains:
- `map.yaml` - Map configuration and actors
- `map.bin` - Binary map data
- `map.png` - Map preview image
- `rules.yaml` - Tutorial-specific game rules
- `tutorial.lua` - Mission scripting logic

**File**: `mods/d2k/maps/tutorial/map.yaml`

Key configuration:
```yaml
MapFormat: 12
RequiresMod: d2k
Title: Tutorial
Author: OpenRA Team
Tileset: ARRAKIS
MapSize: 52,72
Bounds: 2,2,48,68
Visibility: MissionSelector
Categories: Tutorial
LockPreview: True

Players:
    PlayerReference@Neutral:
        Name: Neutral
        OwnsWorld: True
        NonCombatant: True
        Faction: Random
    PlayerReference@Creeps:
        Name: Creeps
        NonCombatant: True
        Faction: Random
        Enemies: Atreides, Harkonnen
    PlayerReference@Atreides:
        Name: Atreides
        Playable: True
        Faction: atreides
        LockFaction: True
        Color: 2A6DC8
        LockColor: True
        Enemies: Harkonnen, Creeps
    PlayerReference@Harkonnen:
        Name: Harkonnen
        Faction: harkonnen
        LockFaction: True
        Color: C82A2A
        LockColor: True
        Enemies: Atreides, Creeps
        Bot: campaign

Actors:
    PlayerSpawn: mpspawn
        Location: 9,11
        Owner: Neutral

    PlayerMCV: mcv
        Location: 10,12
        Owner: Atreides
    PlayerInfantry1: light_inf
        Location: 8,14
        Owner: Atreides
    # ... more infantry

    # Harkonnen base (passive)
    EnemyConyard: construction_yard
        Location: 38,55
        Owner: Harkonnen
    # ... enemy buildings and units

Rules: d2k|rules/campaign-rules.yaml, d2k|rules/campaign-tooltips.yaml, d2k|rules/campaign-palettes.yaml, rules.yaml

FluentMessages: d2k|fluent/lua.ftl, d2k|fluent/tutorial.ftl
```

### 5. Tutorial Lua Script

**File**: `mods/d2k/maps/tutorial/tutorial.lua`

The script implements 15 progressive objectives:

| # | Objective | Completion Condition |
|---|-----------|---------------------|
| 1 | Camera Movement | Time-based (10 seconds) |
| 2 | Unit Selection | Time-based (12 seconds) |
| 3 | Unit Movement | Time-based (10 seconds) |
| 4 | Control Groups | Time-based (10 seconds) |
| 5 | Deploy MCV | Player has construction_yard |
| 6 | Place Concrete | Time-based (18 seconds) |
| 7 | Build Wind Trap | Player has wind_trap |
| 8 | Build Refinery | Player has refinery |
| 9 | Harvesting | Player gains 700+ credits |
| 10 | Build Barracks | Player has barracks |
| 11 | Train Infantry | Player has 7+ light_inf |
| 12 | Build Light Factory | Player has light_factory |
| 13 | Build Vehicles | Player has 3+ trikes |
| 14 | Combat | All enemy combat units destroyed |
| 15 | Victory | Auto-completes, triggers win |

**Key Script Functions**:

```lua
-- Called when map loads
WorldLoaded = function()
    Atreides = Player.GetPlayer("Atreides")
    Harkonnen = Player.GetPlayer("Harkonnen")
    Mentat = UserInterface.GetFluentMessage("mentat")
    Camera.Position = PlayerMCV.CenterPosition

    -- Make enemy units passive
    EnemyUnits = Harkonnen.GetActorsByTypes({ "light_inf", "trike", "combat_tank_h" })
    for _, unit in ipairs(EnemyUnits) do
        if unit.HasProperty("Stance") then
            unit.Stance = "HoldFire"
        end
        Trigger.OnDamaged(unit, EnemyAttacked)
    end

    -- Start first objective after delay
    Trigger.AfterDelay(DateTime.Seconds(2), function()
        StartObjective(1)
    end)
end

-- Called when enemy units are attacked
EnemyAttacked = function(self, attacker)
    for _, unit in ipairs(EnemyUnits) do
        if not unit.IsDead then
            if unit.HasProperty("Stance") then
                unit.Stance = "AttackAnything"
            end
            if unit.HasProperty("Hunt") then
                unit.Hunt()
            end
        end
    end
end

-- Tick function checks objective completion
Tick = function()
    CheckObjectiveCompletion()
end
```

**Example Objective Implementation** (Deploy MCV):
```lua
Objective5_DeployMCV = function()
    Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-mcv-intro"), Mentat)
    Trigger.AfterDelay(DateTime.Seconds(3), function()
        Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-mcv-deploy"), Mentat)
    end)
    Trigger.AfterDelay(DateTime.Seconds(6), function()
        Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-mcv-creates"), Mentat)
        UserInterface.SetMissionText(UserInterface.GetFluentMessage("tutorial-mission-text-mcv"))
    end)
end

Check5_DeployMCV = function()
    local conyards = Atreides.GetActorsByType("construction_yard")
    if #conyards > 0 then
        Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-mcv-complete"), Mentat)
        CompleteObjective()
    end
end
```

### 6. Localization

**File**: `mods/d2k/fluent/tutorial.ftl` (107 lines)

Contains all tutorial message strings organized by objective:

```fluent
## Tutorial Mission
tutorial-title = Tutorial
tutorial-briefing = Welcome to Dune 2000! This tutorial will teach you the basics...
tutorial-complete-objective = Complete the tutorial

## Objective 1: Camera Movement
tutorial-welcome = Welcome to Arrakis, Commander!
tutorial-camera-scroll = Use the ARROW KEYS to scroll around the map.
tutorial-camera-home = Press H to quickly jump back to your base.
tutorial-camera-complete = Good! You've learned to navigate the battlefield.
tutorial-mission-text-camera = Scroll around with Arrow Keys, then press H

## ... (continues for all 15 objectives)
```

**File**: `mods/d2k/fluent/chrome.ftl` (lines 1-12)

Contains UI strings for the tutorial button and resume dialog:
```fluent
button-singleplayer-menu-tutorial = Tutorial

dialog-tutorial-resume =
    .title = Tutorial
    .prompt = Resume your previous tutorial or start over?
    .resume = Resume
    .start-new = Start New
```

### 7. Widget/UI System

**File**: `OpenRA.Game/Widgets/Widget.cs`

Base widget system that handles:
- Widget hierarchy (Parent, Children)
- Visibility (`IsVisible` delegate)
- Input handling
- Window stack management via `Ui` static class

**File**: `OpenRA.Game/Widgets/WidgetLoader.cs`

Loads widgets from YAML definitions:
1. Parses Chrome YAML files
2. Creates widget instances via `NewWidget()`
3. Sets properties via `FieldLoader`
4. Attaches ChromeLogic classes via `PostInit()`

**File**: `OpenRA.Mods.Common/Widgets/ButtonWidget.cs`

Button widget implementation with `OnClick` action delegate.

### 8. Map Loading and Game Initialization

**File**: `OpenRA.Game/Map/MapCache.cs`

Manages map discovery and caching:
- `LoadMaps()` - scans directories
- Indexer `[string uid]` - retrieves `MapPreview`
- `FirstOrDefault()` used to find tutorial map

**File**: `OpenRA.Game/Map/Map.cs`

Contains `MapVisibility` enum:
```csharp
public enum MapVisibility
{
    Lobby = 1,
    Shellmap = 2,
    MissionSelector = 4
}
```

**File**: `OpenRA.Game/Game.cs`

Game startup methods:
- `CreateAndStartLocalServer(mapUid, orders)` - creates local server and starts game
- `StartGame(Map, WorldType)` - internal game initialization

### 9. Lua Scripting API

**Global APIs** (available to mission scripts):

| File | API | Purpose |
|------|-----|---------|
| `Scripting/Global/PlayerGlobal.cs` | `Player.GetPlayer()` | Get player reference |
| `Scripting/Global/CameraGlobal.cs` | `Camera.Position` | Control camera |
| `Scripting/Global/UserInterfaceGlobal.cs` | `UserInterface.GetFluentMessage()` | Get localized strings |
| `Scripting/Global/UserInterfaceGlobal.cs` | `UserInterface.SetMissionText()` | Set mission objective text |
| `Scripting/Global/MediaGlobal.cs` | `Media.DisplayMessage()` | Show on-screen message |
| `Scripting/Global/MediaGlobal.cs` | `Media.PlaySpeechNotification()` | Play audio notification |
| `Scripting/Global/TriggerGlobal.cs` | `Trigger.AfterDelay()` | Schedule delayed action |
| `Scripting/Global/TriggerGlobal.cs` | `Trigger.OnDamaged()` | React to damage |
| `Scripting/Global/TriggerGlobal.cs` | `Trigger.OnObjectiveCompleted()` | React to objective completion |
| `Scripting/Global/TriggerGlobal.cs` | `Trigger.OnPlayerWon()` | React to player victory |
| `Scripting/Global/DateTimeGlobal.cs` | `DateTime.Seconds()` | Convert seconds to game ticks |
| `Scripting/Global/DateTimeGlobal.cs` | `DateTime.GameTime` | Current game time |

**Player Properties** (`Scripting/Properties/PlayerProperties.cs`):
- `player.GetActorsByType(type)` - get all actors of type
- `player.GetActorsByTypes(types)` - get actors of multiple types
- `player.Resources` - current credits
- `player.AddObjective(description)` - add mission objective
- `player.MarkCompletedObjective(id)` - complete objective

**Actor Properties** (`Scripting/Properties/`):
- `actor.IsDead` - check if destroyed
- `actor.HasProperty(name)` - check trait availability
- `actor.Stop()` - stop current activity
- `actor.Stance` - get/set unit stance
- `actor.Hunt()` - order to hunt enemies
- `actor.CenterPosition` - get world position

### 10. Mission System Integration

**File**: `OpenRA.Mods.Common/Traits/Player/MissionObjectives.cs`

Trait for tracking mission objectives. Used via Lua API:
```lua
TutorialObjective = Atreides.AddObjective("Complete the tutorial")
Atreides.MarkCompletedObjective(TutorialObjective)
```

**File**: `OpenRA.Mods.Common/Widgets/Logic/MissionBrowserLogic.cs`

Handles mission selection panel. Filters maps by `MapVisibility.MissionSelector`.

## Code References

### UI Definition
- `mods/d2k/chrome/mainmenu.yaml:79-133` - Singleplayer menu container
- `mods/d2k/chrome/mainmenu.yaml:105-111` - Tutorial button widget

### C# Logic
- `OpenRA.Mods.Common/Widgets/Logic/MainMenuLogic.cs:143-171` - Tutorial button handler
- `OpenRA.Mods.Common/Widgets/Logic/MainMenuLogic.cs:575-599` - FindTutorialSave method
- `OpenRA.Mods.Common/Widgets/Logic/MainMenuLogic.cs:601-612` - StartNewTutorial method
- `OpenRA.Mods.Common/Widgets/Logic/MainMenuLogic.cs:614-626` - LoadTutorialSave method
- `OpenRA.Mods.Common/Widgets/Logic/MainMenuLogic.cs:628-633` - OnTutorialStart method

### Map Files
- `mods/d2k/maps/tutorial/map.yaml` - Map configuration
- `mods/d2k/maps/tutorial/rules.yaml` - Tutorial-specific rules
- `mods/d2k/maps/tutorial/tutorial.lua` - Mission script (524 lines)

### Localization
- `mods/d2k/fluent/tutorial.ftl` - Tutorial message strings (107 lines)
- `mods/d2k/fluent/chrome.ftl:5-12` - Tutorial button and dialog strings

### Mission Configuration
- `mods/d2k/missions.yaml:1-2` - Tutorial mission entry

### Core Engine
- `OpenRA.Game/Widgets/Widget.cs` - Base widget system
- `OpenRA.Game/Widgets/WidgetLoader.cs` - YAML widget loading
- `OpenRA.Game/Map/MapCache.cs` - Map discovery/caching
- `OpenRA.Game/Map/Map.cs` - Map data and MapVisibility enum
- `OpenRA.Game/Game.cs` - Game startup methods

### Lua Scripting Engine
- `OpenRA.Mods.Common/Scripting/LuaScript.cs` - Main script trait
- `OpenRA.Mods.Common/Scripting/Global/UserInterfaceGlobal.cs` - UI API
- `OpenRA.Mods.Common/Scripting/Global/TriggerGlobal.cs` - Trigger API
- `OpenRA.Mods.Common/Scripting/Global/MediaGlobal.cs` - Media API
- `OpenRA.Mods.Common/Scripting/Properties/PlayerProperties.cs` - Player API

## Architecture Documentation

### Tutorial System Flow

```
User clicks Tutorial button
         |
         v
MainMenuLogic.cs checks for tutorial map
(MapVisibility.MissionSelector + Title="Tutorial")
         |
         v
FindTutorialSave() searches for existing save
         |
    +----+----+
    |         |
    v         v
Save found   No save
    |         |
    v         v
Show resume  StartNewTutorial()
dialog            |
    |             v
    +-------->Game.CreateAndStartLocalServer()
                  |
                  v
           Map loads, Lua script runs
                  |
                  v
           WorldLoaded() initializes
                  |
                  v
           15 objectives execute sequentially
                  |
                  v
           Victory triggers on objective 15
```

### Widget System Pattern

```
Chrome YAML (declarative)
         |
         v
WidgetLoader.LoadWidget()
         |
         v
Widget instance created
         |
         v
Properties set from YAML
         |
         v
ChromeLogic attached (controller)
         |
         v
OnClick/IsVisible delegates set
```

### Mission Script Pattern

```
WorldLoaded()
    - Initialize player references
    - Setup triggers and callbacks
    - Start first objective

Tick()
    - Check objective completion conditions
    - Called every game tick

StartObjective(n)
    - Display instruction messages
    - Set mission text
    - Setup objective-specific triggers

CompleteObjective()
    - Mark current objective done
    - Delay then start next objective
```

## Related Research

- None yet - this is the first research document for the D2K tutorial feature.

## Open Questions

1. **Tutorial Save Handling**: The save system finds any save matching the tutorial map UID. What happens if the save is corrupted or from an incompatible version?

2. **Objective Detection**: Several objectives use time-based completion (camera, selection, movement, control groups, concrete) because the Lua API cannot detect these player actions. Are there events that could be exposed to detect these actions?

3. **Enemy AI Behavior**: The enemy units start passive (HoldFire) and activate when damaged. The `OnDamaged` trigger wakes all enemy units. This is a simple but effective system for a tutorial.

4. **Other Mods**: RA and CNC don't have dedicated tutorials - only campaign missions. D2K is the first mod with a dedicated tutorial button and map.
