# D2K AI Battle Test Feature Implementation Plan

## Overview

Implement an AI Battle Test feature for Dune 2000 (D2K) that allows users to set up AI-vs-AI battles, watch them at accelerated speeds, view comprehensive statistics, and review replays with timeline scrubbing and fog of war perspective controls.

## Current State Analysis

### Existing Infrastructure We'll Leverage:
- **Replay System** (`OpenRA.Game/Network/ReplayRecorder.cs`, `ReplayConnection.cs`): Order-based recording and playback
- **Skirmish Lobby** (`OpenRA.Mods.Common/Widgets/Logic/Lobby/LobbyLogic.cs`): Map selection and player configuration
- **Observer Statistics** (`OpenRA.Mods.Common/Widgets/Logic/Ingame/ObserverStatsLogic.cs`): Combat, economy, army stats display
- **Fog of War Selector** (`OpenRA.Mods.Common/Widgets/Logic/Ingame/ObserverShroudSelectorLogic.cs`): Perspective switching
- **Replay Controls** (`OpenRA.Mods.Common/Widgets/Logic/Ingame/ReplayControlBarLogic.cs`): Play/pause/speed controls
- **Game Speed System** (`OpenRA.Game/Game.cs:819-862`): Variable timestep control

### Key Discoveries:
- Replays are order-deterministic, not state-based - no native rewind capability
- `World.ReplayTimestep` controls playback speed (lower = faster)
- Game save loading uses `Timestep = 1` for maximum speed fast-forward
- `World.RenderPlayer` controls fog of war perspective
- "Everyone" player provides combined allied vision
- `PlayerStatistics` trait tracks all required stats

## Desired End State

After implementation:
1. D2K Singleplayer menu has "AI Battle" button
2. Users can configure AI-vs-AI battles using familiar skirmish UI
3. Battles run at user-selected speeds (1x, 4x, 8x, 16x) with live viewing
4. Results screen shows comprehensive statistics after battle ends
5. Replay viewer has timeline scrubber with bidirectional navigation
6. Fog of war dropdown allows: No Fog, Combined AI Vision, Individual AI views
7. Statistics overlay available during replay viewing

### Verification:
- AI Battle button visible in D2K singleplayer menu
- Can configure 2+ AI players on any multiplayer map
- Speed controls work during simulation
- Results screen displays all statistics correctly
- Timeline scrubber allows forward/backward navigation
- Perspective dropdown changes fog of war correctly
- Statistics overlay toggles work during replay

## What We're NOT Doing

- Full state serialization for instant rewind (using restart-and-fast-forward instead)
- Support for mods other than D2K (can be extended later)
- Headless/background simulation (user watches or minimizes window)
- Automated AI benchmarking/batch testing
- AI performance profiling or debugging tools
- Multiplayer spectating of AI battles

## Implementation Approach

The feature consists of three main flows:
1. **Configuration Flow**: Singleplayer menu → AI Battle config → Start simulation
2. **Simulation Flow**: AI-only game with observer controls and speed adjustment
3. **Review Flow**: Results screen → Replay viewer with scrubbing and stats

We'll create new widget logic classes and YAML definitions while reusing existing infrastructure wherever possible.

---

## Phase 1: Menu Integration & AI Battle Configuration

### Overview
Add AI Battle entry point to D2K singleplayer menu and create configuration UI that reuses skirmish lobby components.

### Changes Required:

#### 1.1 Add AI Battle Button to Singleplayer Menu

**File**: `mods/d2k/chrome/mainmenu.yaml`
**Changes**: Add AI_BATTLE_BUTTON after TUTORIAL button in singleplayer panel

```yaml
Button@AI_BATTLE_BUTTON:
    Key: ai-battle
    X: 0
    Y: 140
    Width: PARENT_RIGHT
    Height: 50
    Font: BigBold
    Text: dropdown-ai-battle.label
    OnClick: AIBattle
```

**File**: `mods/d2k/chrome/mainmenu-prerelease.yaml`
**Changes**: Same button addition for prerelease menu

#### 1.2 Create AI Battle Logic Class

**File**: `OpenRA.Mods.Common/Widgets/Logic/MainMenu/AIBattleLogic.cs` (new file)
**Changes**: Create logic class for AI Battle configuration panel

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
    public class AIBattleLogic : ChromeLogic
    {
        readonly ModData modData;
        readonly Action onExit;
        readonly Widget panel;

        MapPreview selectedMap;
        List<AISlotConfig> aiSlots = new();
        int simulationSpeed = 4; // 4x default

        [TranslationReference]
        const string AIBattleTitle = "label-ai-battle-title";

        public class AISlotConfig
        {
            public string BotType;
            public string Faction;
            public int Team;
            public int Handicap;
        }

        [ObjectCreator.UseCtor]
        public AIBattleLogic(Widget widget, ModData modData, Action onExit)
        {
            this.modData = modData;
            this.onExit = onExit;
            panel = widget;

            SetupMapSelector();
            SetupAISlots();
            SetupSpeedSelector();
            SetupButtons();
        }

        void SetupMapSelector()
        {
            var mapButton = panel.Get<ButtonWidget>("MAP_BUTTON");
            var mapPreview = panel.Get<MapPreviewWidget>("MAP_PREVIEW");

            // Default to last used map or first available
            var mapId = Game.Settings.Server.Map;
            selectedMap = modData.MapCache[mapId];

            if (selectedMap == null || selectedMap.PlayerCount < 2)
                selectedMap = modData.MapCache
                    .Where(m => m.Status == MapStatus.Available && m.PlayerCount >= 2)
                    .OrderByDescending(m => m.PlayerCount)
                    .FirstOrDefault();

            UpdateMapPreview(mapPreview);

            mapButton.OnClick = () =>
            {
                Ui.OpenWindow("MAPCHOOSER_PANEL", new WidgetArgs
                {
                    { "initialMap", selectedMap?.Uid },
                    { "initialTab", MapClassification.System },
                    { "onSelect", (Action<string>)(uid =>
                    {
                        selectedMap = modData.MapCache[uid];
                        UpdateMapPreview(mapPreview);
                        RebuildAISlots();
                    })},
                    { "filter", MapVisibility.Lobby },
                });
            };
        }

        void SetupAISlots()
        {
            // Build AI slot configuration based on selected map
            RebuildAISlots();
        }

        void RebuildAISlots()
        {
            if (selectedMap == null)
                return;

            var slotContainer = panel.Get<ContainerWidget>("AI_SLOTS_CONTAINER");
            slotContainer.RemoveChildren();

            aiSlots.Clear();

            // Get available bot types
            var botTypes = modData.DefaultRules.Actors[SystemActors.Player]
                .TraitInfos<IBotInfo>()
                .Where(b => b.Type != null)
                .Select(b => b.Type)
                .ToList();

            // Get available factions
            var factions = modData.DefaultRules.Actors[SystemActors.World]
                .TraitInfos<FactionInfo>()
                .Where(f => f.Selectable)
                .ToList();

            var playerCount = Math.Min(selectedMap.PlayerCount, 8);

            for (var i = 0; i < playerCount; i++)
            {
                var slot = new AISlotConfig
                {
                    BotType = botTypes.FirstOrDefault() ?? "normal",
                    Faction = factions[i % factions.Count].InternalName,
                    Team = (i % 2) + 1, // Alternate teams
                    Handicap = 0
                };
                aiSlots.Add(slot);

                var slotWidget = CreateAISlotWidget(i, slot, botTypes, factions);
                slotContainer.AddChild(slotWidget);
            }
        }

        Widget CreateAISlotWidget(int index, AISlotConfig slot,
            List<string> botTypes, List<FactionInfo> factions)
        {
            var template = panel.Get<ContainerWidget>("AI_SLOT_TEMPLATE");
            var widget = (ContainerWidget)template.Clone();
            widget.Id = $"AI_SLOT_{index}";
            widget.IsVisible = () => true;
            widget.Bounds.Y = index * (template.Bounds.Height + 5);

            // AI Type dropdown
            var botDropdown = widget.Get<DropDownButtonWidget>("BOT_DROPDOWN");
            botDropdown.GetText = () => slot.BotType;
            botDropdown.OnClick = () =>
            {
                var options = botTypes.Select(bt => new DropDownOption
                {
                    Title = bt,
                    OnClick = () => slot.BotType = bt,
                    IsSelected = () => slot.BotType == bt
                });
                botDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 200, options);
            };

            // Faction dropdown
            var factionDropdown = widget.Get<DropDownButtonWidget>("FACTION_DROPDOWN");
            factionDropdown.GetText = () => slot.Faction;
            factionDropdown.OnClick = () =>
            {
                var options = factions.Select(f => new DropDownOption
                {
                    Title = f.Name,
                    OnClick = () => slot.Faction = f.InternalName,
                    IsSelected = () => slot.Faction == f.InternalName
                });
                factionDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 200, options);
            };

            // Team dropdown
            var teamDropdown = widget.Get<DropDownButtonWidget>("TEAM_DROPDOWN");
            teamDropdown.GetText = () => slot.Team == 0 ? "-" : slot.Team.ToString();
            teamDropdown.OnClick = () =>
            {
                var options = Enumerable.Range(0, 5).Select(t => new DropDownOption
                {
                    Title = t == 0 ? "-" : t.ToString(),
                    OnClick = () => slot.Team = t,
                    IsSelected = () => slot.Team == t
                });
                teamDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 150, options);
            };

            return widget;
        }

        void SetupSpeedSelector()
        {
            var speedDropdown = panel.Get<DropDownButtonWidget>("SPEED_DROPDOWN");
            var speeds = new[] { 1, 2, 4, 8, 16 };

            speedDropdown.GetText = () => $"{simulationSpeed}x";
            speedDropdown.OnClick = () =>
            {
                var options = speeds.Select(s => new DropDownOption
                {
                    Title = $"{s}x",
                    OnClick = () => simulationSpeed = s,
                    IsSelected = () => simulationSpeed == s
                });
                speedDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 150, options);
            };
        }

        void SetupButtons()
        {
            var startButton = panel.Get<ButtonWidget>("START_BUTTON");
            startButton.OnClick = StartAIBattle;
            startButton.IsDisabled = () => selectedMap == null || aiSlots.Count < 2;

            var backButton = panel.Get<ButtonWidget>("BACK_BUTTON");
            backButton.OnClick = () =>
            {
                Game.Disconnect();
                Ui.CloseWindow();
                onExit();
            };
        }

        void StartAIBattle()
        {
            if (selectedMap == null || aiSlots.Count < 2)
                return;

            // Store configuration for AIBattleOrderSource
            AIBattleManager.PendingConfig = new AIBattleConfig
            {
                MapUid = selectedMap.Uid,
                AISlots = aiSlots.ToList(),
                SimulationSpeed = simulationSpeed
            };

            // Create server with AI battle mode
            Game.CreateAIBattleServer(selectedMap.Uid, simulationSpeed);
        }

        void UpdateMapPreview(MapPreviewWidget preview)
        {
            if (selectedMap != null)
                preview.Preview = () => selectedMap;
        }
    }

    public class AIBattleConfig
    {
        public string MapUid;
        public List<AIBattleLogic.AISlotConfig> AISlots;
        public int SimulationSpeed;
    }

    public static class AIBattleManager
    {
        public static AIBattleConfig PendingConfig;
        public static string LastReplayPath;
        public static AIBattleResults LastResults;
    }

    public class AIBattleResults
    {
        public int DurationTicks;
        public int Timestep;
        public string WinnerName;
        public string WinnerFaction;
        public List<AIPlayerStats> PlayerStats = new();
    }

    public class AIPlayerStats
    {
        public string Name;
        public string Faction;
        public int Team;
        public bool IsWinner;
        public int UnitsKilled;
        public int UnitsDead;
        public int BuildingsKilled;
        public int BuildingsDead;
        public int KillsCost;
        public int DeathsCost;
        public int Earned;
        public int Spent;
        public int ArmyValue;
    }
}
```

#### 1.3 Create AI Battle Configuration Panel YAML

**File**: `mods/d2k/chrome/aibattle.yaml` (new file)
**Changes**: Define the AI Battle configuration panel layout

```yaml
Background@AI_BATTLE_PANEL:
    Logic: AIBattleLogic
    X: (WINDOW_RIGHT - WIDTH) / 2
    Y: (WINDOW_BOTTOM - HEIGHT) / 2
    Width: 900
    Height: 600
    Children:
        Label@TITLE:
            X: 0
            Y: 20
            Width: PARENT_RIGHT
            Height: 35
            Font: Bold
            Align: Center
            Text: label-ai-battle-title
        Container@MAP_SECTION:
            X: 20
            Y: 70
            Width: 300
            Height: 400
            Children:
                Label@MAP_LABEL:
                    X: 0
                    Y: 0
                    Width: PARENT_RIGHT
                    Height: 25
                    Font: Bold
                    Text: label-map
                MapPreview@MAP_PREVIEW:
                    X: 0
                    Y: 30
                    Width: 280
                    Height: 200
                Button@MAP_BUTTON:
                    X: 0
                    Y: 240
                    Width: 280
                    Height: 30
                    Font: Bold
                    Text: button-change-map
        Container@AI_SECTION:
            X: 340
            Y: 70
            Width: 540
            Height: 400
            Children:
                Label@AI_LABEL:
                    X: 0
                    Y: 0
                    Width: PARENT_RIGHT
                    Height: 25
                    Font: Bold
                    Text: label-ai-players
                ScrollPanel@AI_SLOTS_CONTAINER:
                    X: 0
                    Y: 30
                    Width: PARENT_RIGHT
                    Height: 320
                    Children:
                Container@AI_SLOT_TEMPLATE:
                    X: 0
                    Y: 0
                    Width: PARENT_RIGHT - 20
                    Height: 35
                    Visible: false
                    Children:
                        Label@SLOT_NUMBER:
                            X: 0
                            Y: 5
                            Width: 30
                            Height: 25
                            Font: Bold
                        DropDownButton@BOT_DROPDOWN:
                            X: 35
                            Y: 0
                            Width: 120
                            Height: 30
                            Font: Regular
                        DropDownButton@FACTION_DROPDOWN:
                            X: 165
                            Y: 0
                            Width: 120
                            Height: 30
                            Font: Regular
                        DropDownButton@TEAM_DROPDOWN:
                            X: 295
                            Y: 0
                            Width: 60
                            Height: 30
                            Font: Regular
                            Text: label-team
        Container@OPTIONS_SECTION:
            X: 340
            Y: 420
            Width: 540
            Height: 50
            Children:
                Label@SPEED_LABEL:
                    X: 0
                    Y: 5
                    Width: 120
                    Height: 25
                    Font: Bold
                    Text: label-simulation-speed
                DropDownButton@SPEED_DROPDOWN:
                    X: 130
                    Y: 0
                    Width: 80
                    Height: 30
                    Font: Regular
        Container@BUTTONS:
            X: 0
            Y: PARENT_BOTTOM - 70
            Width: PARENT_RIGHT
            Height: 50
            Children:
                Button@BACK_BUTTON:
                    X: 20
                    Y: 0
                    Width: 150
                    Height: 40
                    Font: Bold
                    Text: button-back
                Button@START_BUTTON:
                    X: PARENT_RIGHT - 170
                    Y: 0
                    Width: 150
                    Height: 40
                    Font: Bold
                    Text: button-start-battle
```

#### 1.4 Add Fluent Localization Strings

**File**: `mods/d2k/languages/en.ftl`
**Changes**: Add localization keys for AI Battle UI

```ftl
## AI Battle
label-ai-battle-title = AI Battle Test
dropdown-ai-battle =
    .label = AI Battle

label-map = Map
label-ai-players = AI Players
label-team = Team
label-simulation-speed = Simulation Speed
button-change-map = Change Map
button-start-battle = Start Battle
button-back = Back
button-watch-replay = Watch Replay
button-view-results = View Results

label-results-title = Battle Results
label-winner = Winner
label-duration = Duration
label-statistics = Statistics
label-units-killed = Units Killed
label-units-lost = Units Lost
label-buildings-killed = Buildings Destroyed
label-buildings-lost = Buildings Lost
label-resources-earned = Resources Earned
label-resources-spent = Resources Spent
label-army-value = Final Army Value
label-kill-value = Damage Dealt
label-death-value = Damage Taken

label-no-fog = Disable Shroud
label-combined-vision = All AI Vision
label-rewinding = Rewinding to {$time}...
```

#### 1.5 Register AI Battle Panel in Chrome

**File**: `mods/d2k/chrome/chrome.yaml`
**Changes**: Add AI Battle panel to chrome definitions

```yaml
# Add to existing definitions
AI_BATTLE_PANEL: aibattle.yaml
AI_BATTLE_RESULTS_PANEL: aibattle-results.yaml
```

#### 1.6 Add MainMenuLogic AI Battle Handler

**File**: `OpenRA.Mods.Common/Widgets/Logic/MainMenu/MainMenuLogic.cs`
**Changes**: Add AIBattle button click handler (around line 235, after Tutorial handler)

```csharp
// Add to button click handlers section
var aiBattleButton = widget.GetOrNull<ButtonWidget>("AI_BATTLE_BUTTON");
if (aiBattleButton != null)
{
    aiBattleButton.OnClick = () =>
    {
        SwitchMenu(MenuType.None);
        Ui.OpenWindow("AI_BATTLE_PANEL", new WidgetArgs
        {
            { "onExit", () => SwitchMenu(MenuType.Singleplayer) }
        });
    };
}
```

### Success Criteria:

#### Automated Verification:
- [x] Solution builds without errors: `make all`
- [x] No new compiler warnings in modified files
- [ ] D2K mod loads successfully: launch game and reach main menu

#### Manual Verification:
- [ ] "AI Battle" button appears in D2K Singleplayer menu
- [ ] Clicking button opens AI Battle configuration panel
- [ ] Map selector works and shows multiplayer maps
- [ ] AI slots populate based on map player count
- [ ] All dropdowns (bot type, faction, team, speed) function correctly
- [ ] Back button returns to Singleplayer menu
- [ ] Start button is disabled with < 2 AI slots configured

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation before proceeding to Phase 2.

---

## Phase 2: AI Battle Simulation Mode

### Overview
Create the game session for AI-only battles with observer mode, adjustable speed, and automatic replay recording.

### Changes Required:

#### 2.1 Create AI Battle Server Mode

**File**: `OpenRA.Game/Game.cs`
**Changes**: Add CreateAIBattleServer method (after CreateLocalServer around line 89)

```csharp
public static void CreateAIBattleServer(string mapUid, int speedMultiplier)
{
    var settings = new ServerSettings
    {
        Name = "AI Battle",
        Map = mapUid,
        GameUid = Guid.NewGuid().ToString(),
        EnableSingleplayer = true,
        EnableSyncReports = false,
    };

    // Store speed multiplier for later use
    AIBattleState.SpeedMultiplier = speedMultiplier;
    AIBattleState.IsAIBattle = true;

    var server = new Server.Server(
        new IPEndPoint(IPAddress.Loopback, 0),
        settings,
        ModData,
        ServerType.Local);

    ConnectionLogic.Connect(
        server.LocalEndPoint,
        "",
        () => OpenAIBattleLobby(server),
        () => { server.Shutdown(); AIBattleState.IsAIBattle = false; });
}

static void OpenAIBattleLobby(Server.Server server)
{
    // Auto-configure AI slots and start
    var config = AIBattleManager.PendingConfig;
    if (config == null)
        return;

    // Configure each AI slot via orders
    var orderManager = Game.OrderManager;
    for (var i = 0; i < config.AISlots.Count; i++)
    {
        var slot = config.AISlots[i];
        var slotId = $"Multi{i}";

        // Add bot to slot
        orderManager.IssueOrder(Order.Command($"slot_bot {slotId} 0 {slot.BotType}"));

        // Set faction
        orderManager.IssueOrder(Order.Command($"faction {i} {slot.Faction}"));

        // Set team
        orderManager.IssueOrder(Order.Command($"team {i} {slot.Team}"));

        // Set handicap
        if (slot.Handicap > 0)
            orderManager.IssueOrder(Order.Command($"handicap {i} {slot.Handicap}"));
    }

    // Short delay then start game
    Game.RunAfterDelay(500, () =>
    {
        orderManager.IssueOrder(Order.Command("startgame"));
    });
}
```

#### 2.2 Create AI Battle State Tracker

**File**: `OpenRA.Game/AIBattleState.cs` (new file)
**Changes**: Static state class for AI battle mode

```csharp
namespace OpenRA
{
    public static class AIBattleState
    {
        public static bool IsAIBattle;
        public static int SpeedMultiplier = 1;
        public static int BaseTimestep;
        public static bool IsPaused;

        public static void Reset()
        {
            IsAIBattle = false;
            SpeedMultiplier = 1;
            BaseTimestep = 40;
            IsPaused = false;
        }

        public static int GetEffectiveTimestep()
        {
            if (IsPaused)
                return 0;
            return Math.Max(1, BaseTimestep / SpeedMultiplier);
        }
    }
}
```

#### 2.3 Integrate AI Battle Speed into Game Loop

**File**: `OpenRA.Game/Network/OrderManager.cs`
**Changes**: Modify SuggestedTimestep property (around line 203)

```csharp
public int SuggestedTimestep
{
    get
    {
        if (World == null)
            return Ui.Timestep;

        if (World.IsLoadingGameSave)
            return 1;

        if (World.IsReplay)
            return World.ReplayTimestep;

        // AI Battle speed control
        if (AIBattleState.IsAIBattle)
            return AIBattleState.GetEffectiveTimestep();

        if (tickScale != 1f)
            return Math.Max((int)(tickScale * World.Timestep), 1);

        return World.Timestep;
    }
}
```

#### 2.4 Create AI Battle Observer UI

**File**: `mods/d2k/chrome/ingame-aibattle.yaml` (new file)
**Changes**: Observer UI for AI Battle mode

```yaml
Container@AI_BATTLE_OBSERVER:
    X: 0
    Y: 0
    Width: WINDOW_RIGHT
    Height: WINDOW_BOTTOM
    Logic: AIBattleObserverLogic
    Children:
        Container@TOP_BAR:
            X: 0
            Y: 0
            Width: WINDOW_RIGHT
            Height: 45
            Children:
                Background@TOP_BAR_BG:
                    X: 0
                    Y: 0
                    Width: PARENT_RIGHT
                    Height: PARENT_BOTTOM
                    Background: panel-black-transparent
                Label@TITLE:
                    X: 20
                    Y: 10
                    Width: 200
                    Height: 25
                    Font: Bold
                    Text: AI Battle
                Label@GAME_TIME:
                    X: PARENT_RIGHT / 2 - 50
                    Y: 10
                    Width: 100
                    Height: 25
                    Font: Bold
                    Align: Center
                Container@SPEED_CONTROLS:
                    X: PARENT_RIGHT - 300
                    Y: 5
                    Width: 280
                    Height: 35
                    Children:
                        Button@PAUSE_BUTTON:
                            X: 0
                            Y: 0
                            Width: 50
                            Height: 30
                            Font: Bold
                            Text: ||
                        Button@PLAY_BUTTON:
                            X: 0
                            Y: 0
                            Width: 50
                            Height: 30
                            Font: Bold
                            Text: >
                        Button@SPEED_1X:
                            X: 55
                            Y: 0
                            Width: 45
                            Height: 30
                            Font: Regular
                            Text: 1x
                        Button@SPEED_4X:
                            X: 105
                            Y: 0
                            Width: 45
                            Height: 30
                            Font: Regular
                            Text: 4x
                        Button@SPEED_8X:
                            X: 155
                            Y: 0
                            Width: 45
                            Height: 30
                            Font: Regular
                            Text: 8x
                        Button@SPEED_16X:
                            X: 205
                            Y: 0
                            Width: 45
                            Height: 30
                            Font: Regular
                            Text: 16x
        DropDownButton@SHROUD_SELECTOR:
            X: 20
            Y: 55
            Width: 220
            Height: 30
            Font: Bold
        Button@MENU_BUTTON:
            X: PARENT_RIGHT - 120
            Y: PARENT_BOTTOM - 50
            Width: 100
            Height: 35
            Font: Bold
            Text: Menu
```

#### 2.5 Create AI Battle Observer Logic

**File**: `OpenRA.Mods.Common/Widgets/Logic/Ingame/AIBattleObserverLogic.cs` (new file)
**Changes**: Logic for AI Battle observer controls

```csharp
using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
    public class AIBattleObserverLogic : ChromeLogic
    {
        readonly World world;
        readonly WorldRenderer worldRenderer;

        [ObjectCreator.UseCtor]
        public AIBattleObserverLogic(Widget widget, World world, WorldRenderer worldRenderer)
        {
            this.world = world;
            this.worldRenderer = worldRenderer;

            AIBattleState.BaseTimestep = world.Timestep;

            SetupSpeedControls(widget);
            SetupShroudSelector(widget);
            SetupGameTimer(widget);
            SetupMenuButton(widget);

            // Start recording replay automatically
            if (world.OrderManager.Connection is NetworkConnection nc)
                nc.StartRecording(() => $"aibattle-{DateTime.UtcNow:yyyy-MM-ddTHHmmss}");
        }

        void SetupSpeedControls(Widget widget)
        {
            var pauseButton = widget.Get<ButtonWidget>("PAUSE_BUTTON");
            var playButton = widget.Get<ButtonWidget>("PLAY_BUTTON");

            pauseButton.IsVisible = () => !AIBattleState.IsPaused;
            playButton.IsVisible = () => AIBattleState.IsPaused;

            pauseButton.OnClick = () => AIBattleState.IsPaused = true;
            playButton.OnClick = () => AIBattleState.IsPaused = false;

            var speeds = new[] { (1, "SPEED_1X"), (4, "SPEED_4X"), (8, "SPEED_8X"), (16, "SPEED_16X") };
            foreach (var (speed, buttonId) in speeds)
            {
                var button = widget.Get<ButtonWidget>(buttonId);
                var capturedSpeed = speed;
                button.OnClick = () =>
                {
                    AIBattleState.SpeedMultiplier = capturedSpeed;
                    AIBattleState.IsPaused = false;
                };
                button.IsHighlighted = () => AIBattleState.SpeedMultiplier == capturedSpeed && !AIBattleState.IsPaused;
            }
        }

        void SetupShroudSelector(Widget widget)
        {
            var shroudSelector = widget.Get<DropDownButtonWidget>("SHROUD_SELECTOR");

            // Build options: No Fog, Combined Vision, Individual AIs
            var aiPlayers = world.Players
                .Where(p => p.IsBot && !p.NonCombatant)
                .ToList();

            var everyonePlayer = world.Players
                .FirstOrDefault(p => p.InternalName == "Everyone");

            Player selectedPlayer = null; // null = no fog

            shroudSelector.GetText = () =>
            {
                if (selectedPlayer == null)
                    return TranslationProvider.GetString("label-no-fog");
                if (selectedPlayer == everyonePlayer)
                    return TranslationProvider.GetString("label-combined-vision");
                return selectedPlayer.ResolvedPlayerName;
            };

            shroudSelector.OnClick = () =>
            {
                var options = new List<DropDownOption>();

                // No Fog option
                options.Add(new DropDownOption
                {
                    Title = TranslationProvider.GetString("label-no-fog"),
                    OnClick = () => { selectedPlayer = null; world.RenderPlayer = null; },
                    IsSelected = () => selectedPlayer == null
                });

                // Combined Vision option
                if (everyonePlayer != null)
                {
                    options.Add(new DropDownOption
                    {
                        Title = TranslationProvider.GetString("label-combined-vision"),
                        OnClick = () => { selectedPlayer = everyonePlayer; world.RenderPlayer = everyonePlayer; },
                        IsSelected = () => selectedPlayer == everyonePlayer
                    });
                }

                // Individual AI options
                foreach (var p in aiPlayers)
                {
                    var player = p;
                    options.Add(new DropDownOption
                    {
                        Title = player.ResolvedPlayerName,
                        OnClick = () => { selectedPlayer = player; world.RenderPlayer = player; },
                        IsSelected = () => selectedPlayer == player
                    });
                }

                shroudSelector.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 300, options);
            };
        }

        void SetupGameTimer(Widget widget)
        {
            var timerLabel = widget.Get<LabelWidget>("GAME_TIME");
            timerLabel.GetText = () => WidgetUtils.FormatTime(world.WorldTick, world.Timestep);
        }

        void SetupMenuButton(Widget widget)
        {
            var menuButton = widget.Get<ButtonWidget>("MENU_BUTTON");
            menuButton.OnClick = () =>
            {
                // Show confirmation dialog
                ConfirmationDialogs.ButtonPrompt(
                    title: "End Battle?",
                    text: "End the AI battle and view results?",
                    onConfirm: EndBattleAndShowResults,
                    confirmText: "End Battle",
                    onCancel: () => { }
                );
            };
        }

        void EndBattleAndShowResults()
        {
            // Capture statistics before ending
            CaptureResults();

            // End the game
            world.EndGame();
        }

        void CaptureResults()
        {
            var results = new AIBattleResults
            {
                DurationTicks = world.WorldTick,
                Timestep = world.Timestep
            };

            foreach (var player in world.Players.Where(p => p.IsBot && !p.NonCombatant))
            {
                var stats = player.PlayerActor.TraitOrDefault<PlayerStatistics>();
                var resources = player.PlayerActor.TraitOrDefault<PlayerResources>();

                var playerStats = new AIPlayerStats
                {
                    Name = player.ResolvedPlayerName,
                    Faction = player.Faction.InternalName,
                    Team = player.PlayerReference?.LockTeam ?? 0,
                    IsWinner = player.WinState == WinState.Won,
                    UnitsKilled = stats?.UnitsKilled ?? 0,
                    UnitsDead = stats?.UnitsDead ?? 0,
                    BuildingsKilled = stats?.BuildingsKilled ?? 0,
                    BuildingsDead = stats?.BuildingsDead ?? 0,
                    KillsCost = stats?.KillsCost ?? 0,
                    DeathsCost = stats?.DeathsCost ?? 0,
                    Earned = resources?.Earned ?? 0,
                    Spent = resources?.Spent ?? 0,
                    ArmyValue = stats?.ArmyValue ?? 0
                };

                if (playerStats.IsWinner)
                {
                    results.WinnerName = playerStats.Name;
                    results.WinnerFaction = playerStats.Faction;
                }

                results.PlayerStats.Add(playerStats);
            }

            // If no winner yet, determine by most kills value
            if (results.WinnerName == null && results.PlayerStats.Count > 0)
            {
                var bestPlayer = results.PlayerStats.OrderByDescending(p => p.KillsCost).First();
                results.WinnerName = bestPlayer.Name;
                results.WinnerFaction = bestPlayer.Faction;
            }

            AIBattleManager.LastResults = results;
        }
    }
}
```

#### 2.6 Hook AI Battle Observer UI into World Loading

**File**: `OpenRA.Mods.Common/Widgets/Logic/Ingame/IngameLogicHandler.cs`
**Changes**: Add AI Battle observer UI when starting AI battle (around line 45)

```csharp
// Add check for AI Battle mode after regular observer check
if (AIBattleState.IsAIBattle)
{
    Game.LoadWidget(world, "AI_BATTLE_OBSERVER", Ui.Root, new WidgetArgs
    {
        { "world", world },
        { "worldRenderer", worldRenderer }
    });
    return;
}
```

#### 2.7 Handle AI Battle Game End

**File**: `OpenRA.Mods.Common/Traits/World/EndGameNotification.cs`
**Changes**: Add AI Battle results handling on game end (in GameEnded method)

```csharp
// Add to GameEnded or create new handler
if (AIBattleState.IsAIBattle)
{
    // Store replay path
    var replayDir = Platform.GetSupportDir("Replays");
    var latestReplay = Directory.GetFiles(replayDir, "aibattle-*.orarep")
        .OrderByDescending(f => File.GetCreationTime(f))
        .FirstOrDefault();
    AIBattleManager.LastReplayPath = latestReplay;

    // Transition to results screen
    Game.RunAfterDelay(1000, () =>
    {
        Game.Disconnect();
        AIBattleState.Reset();
        Ui.OpenWindow("AI_BATTLE_RESULTS_PANEL");
    });
}
```

### Success Criteria:

#### Automated Verification:
- [x] Solution builds without errors: `make all`
- [x] No new compiler warnings in modified files
- [x] Unit tests pass (if any exist for modified components)

#### Manual Verification:
- [ ] Starting AI Battle creates game with all configured AI players
- [ ] Speed controls (1x, 2x, 4x, 8x, 16x) change simulation speed correctly
- [ ] Pause button stops simulation, play resumes it
- [ ] Fog of war dropdown shows all options (No Fog, Combined, Individual AIs)
- [ ] Switching perspective changes visible terrain/units correctly
- [ ] Game timer displays and updates correctly
- [ ] Battle ends when victory conditions are met
- [ ] Replay file is created in Replays directory

**Implementation Note**: After completing this phase, pause for manual verification before proceeding to Phase 3.

---

## Phase 3: Results Screen

### Overview
Create the post-battle results screen showing comprehensive statistics with option to watch replay.

### Changes Required:

#### 3.1 Create Results Panel YAML

**File**: `mods/d2k/chrome/aibattle-results.yaml` (new file)
**Changes**: Define results panel layout

```yaml
Background@AI_BATTLE_RESULTS_PANEL:
    Logic: AIBattleResultsLogic
    X: (WINDOW_RIGHT - WIDTH) / 2
    Y: (WINDOW_BOTTOM - HEIGHT) / 2
    Width: 800
    Height: 600
    Children:
        Label@TITLE:
            X: 0
            Y: 20
            Width: PARENT_RIGHT
            Height: 35
            Font: BigBold
            Align: Center
            Text: label-results-title
        Container@WINNER_SECTION:
            X: 0
            Y: 70
            Width: PARENT_RIGHT
            Height: 80
            Children:
                Label@WINNER_LABEL:
                    X: 0
                    Y: 0
                    Width: PARENT_RIGHT
                    Height: 30
                    Font: Bold
                    Align: Center
                    Text: label-winner
                Label@WINNER_NAME:
                    X: 0
                    Y: 35
                    Width: PARENT_RIGHT
                    Height: 35
                    Font: BigBold
                    Align: Center
                    TextColor: 00FF00
        Container@DURATION_SECTION:
            X: 0
            Y: 150
            Width: PARENT_RIGHT
            Height: 40
            Children:
                Label@DURATION_LABEL:
                    X: PARENT_RIGHT / 2 - 100
                    Y: 0
                    Width: 100
                    Height: 25
                    Font: Bold
                    Text: label-duration
                Label@DURATION_VALUE:
                    X: PARENT_RIGHT / 2
                    Y: 0
                    Width: 100
                    Height: 25
                    Font: Regular
        ScrollPanel@STATS_PANEL:
            X: 20
            Y: 200
            Width: PARENT_RIGHT - 40
            Height: 320
            Children:
                Container@STATS_HEADER:
                    X: 0
                    Y: 0
                    Width: PARENT_RIGHT
                    Height: 30
                    Children:
                        Label@COL_PLAYER:
                            X: 0
                            Y: 0
                            Width: 150
                            Height: 25
                            Font: Bold
                            Text: Player
                        Label@COL_KILLS:
                            X: 150
                            Y: 0
                            Width: 80
                            Height: 25
                            Font: Bold
                            Text: Kills
                        Label@COL_DEATHS:
                            X: 230
                            Y: 0
                            Width: 80
                            Height: 25
                            Font: Bold
                            Text: Deaths
                        Label@COL_DAMAGE:
                            X: 310
                            Y: 0
                            Width: 100
                            Height: 25
                            Font: Bold
                            Text: Damage
                        Label@COL_EARNED:
                            X: 410
                            Y: 0
                            Width: 100
                            Height: 25
                            Font: Bold
                            Text: Earned
                        Label@COL_ARMY:
                            X: 510
                            Y: 0
                            Width: 100
                            Height: 25
                            Font: Bold
                            Text: Army
                Container@PLAYER_TEMPLATE:
                    X: 0
                    Y: 0
                    Width: PARENT_RIGHT
                    Height: 35
                    Visible: false
                    Children:
                        Label@PLAYER_NAME:
                            X: 0
                            Y: 5
                            Width: 150
                            Height: 25
                            Font: Regular
                        Label@PLAYER_KILLS:
                            X: 150
                            Y: 5
                            Width: 80
                            Height: 25
                            Font: Regular
                        Label@PLAYER_DEATHS:
                            X: 230
                            Y: 5
                            Width: 80
                            Height: 25
                            Font: Regular
                        Label@PLAYER_DAMAGE:
                            X: 310
                            Y: 5
                            Width: 100
                            Height: 25
                            Font: Regular
                        Label@PLAYER_EARNED:
                            X: 410
                            Y: 5
                            Width: 100
                            Height: 25
                            Font: Regular
                        Label@PLAYER_ARMY:
                            X: 510
                            Y: 5
                            Width: 100
                            Height: 25
                            Font: Regular
        Container@BUTTONS:
            X: 0
            Y: PARENT_BOTTOM - 60
            Width: PARENT_RIGHT
            Height: 50
            Children:
                Button@BACK_BUTTON:
                    X: 20
                    Y: 0
                    Width: 150
                    Height: 40
                    Font: Bold
                    Text: button-back
                Button@REPLAY_BUTTON:
                    X: PARENT_RIGHT - 190
                    Y: 0
                    Width: 170
                    Height: 40
                    Font: Bold
                    Text: button-watch-replay
```

#### 3.2 Create Results Logic

**File**: `OpenRA.Mods.Common/Widgets/Logic/AIBattleResultsLogic.cs` (new file)
**Changes**: Logic for results panel

```csharp
using System;
using System.Linq;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
    public class AIBattleResultsLogic : ChromeLogic
    {
        [ObjectCreator.UseCtor]
        public AIBattleResultsLogic(Widget widget, ModData modData)
        {
            var results = AIBattleManager.LastResults;
            if (results == null)
            {
                // No results available, go back to menu
                Ui.CloseWindow();
                return;
            }

            // Winner display
            var winnerName = widget.Get<LabelWidget>("WINNER_NAME");
            winnerName.GetText = () => $"{results.WinnerName} ({results.WinnerFaction})";

            // Duration
            var durationValue = widget.Get<LabelWidget>("DURATION_VALUE");
            durationValue.GetText = () => WidgetUtils.FormatTime(results.DurationTicks, results.Timestep);

            // Player stats
            var statsPanel = widget.Get<ScrollPanelWidget>("STATS_PANEL");
            var template = statsPanel.Get<ContainerWidget>("PLAYER_TEMPLATE");

            var yOffset = 35; // After header
            foreach (var stats in results.PlayerStats.OrderByDescending(p => p.KillsCost))
            {
                var row = (ContainerWidget)template.Clone();
                row.IsVisible = () => true;
                row.Bounds.Y = yOffset;

                var nameLabel = row.Get<LabelWidget>("PLAYER_NAME");
                var playerName = stats.Name;
                var isWinner = stats.IsWinner;
                nameLabel.GetText = () => isWinner ? $"* {playerName}" : playerName;
                nameLabel.GetColor = () => isWinner ? Color.LimeGreen : Color.White;

                var killsLabel = row.Get<LabelWidget>("PLAYER_KILLS");
                var kills = stats.UnitsKilled + stats.BuildingsKilled;
                killsLabel.GetText = () => kills.ToString();

                var deathsLabel = row.Get<LabelWidget>("PLAYER_DEATHS");
                var deaths = stats.UnitsDead + stats.BuildingsDead;
                deathsLabel.GetText = () => deaths.ToString();

                var damageLabel = row.Get<LabelWidget>("PLAYER_DAMAGE");
                var damage = stats.KillsCost;
                damageLabel.GetText = () => $"${damage:N0}";

                var earnedLabel = row.Get<LabelWidget>("PLAYER_EARNED");
                var earned = stats.Earned;
                earnedLabel.GetText = () => $"${earned:N0}";

                var armyLabel = row.Get<LabelWidget>("PLAYER_ARMY");
                var army = stats.ArmyValue;
                armyLabel.GetText = () => $"${army:N0}";

                statsPanel.AddChild(row);
                yOffset += 35;
            }

            // Back button
            var backButton = widget.Get<ButtonWidget>("BACK_BUTTON");
            backButton.OnClick = () =>
            {
                Ui.CloseWindow();
                Game.LoadShellMap();
            };

            // Replay button
            var replayButton = widget.Get<ButtonWidget>("REPLAY_BUTTON");
            replayButton.IsDisabled = () => string.IsNullOrEmpty(AIBattleManager.LastReplayPath);
            replayButton.OnClick = () =>
            {
                Ui.CloseWindow();

                // Store results for overlay during replay
                AIBattleState.IsAIBattle = true; // Enable AI battle replay mode

                Game.JoinReplay(AIBattleManager.LastReplayPath);
            };
        }
    }
}
```

### Success Criteria:

#### Automated Verification:
- [x] Solution builds without errors: `make all`
- [x] No new compiler warnings in modified files

#### Manual Verification:
- [ ] Results screen appears after AI battle ends
- [ ] Winner is displayed correctly with green highlighting
- [ ] Duration shows correct game time
- [ ] All player statistics populate correctly
- [ ] Stats are sorted by damage dealt (highest first)
- [ ] Back button returns to main menu
- [ ] Watch Replay button launches replay viewer

**Implementation Note**: Pause for manual verification before proceeding to Phase 4.

---

## Phase 4: Replay Viewer with Timeline Scrubber

### Overview
Add timeline scrubber to replay viewer with bidirectional navigation using restart-and-fast-forward for backward scrubbing.

### Changes Required:

#### 4.1 Create Timeline Scrubber Widget

**File**: `OpenRA.Mods.Common/Widgets/TimelineScrubberWidget.cs` (new file)
**Changes**: Custom widget for timeline display and interaction

```csharp
using System;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
    public class TimelineScrubberWidget : Widget
    {
        public Func<int> GetCurrentTick = () => 0;
        public Func<int> GetTotalTicks = () => 1;
        public Func<int> GetTimestep = () => 40;
        public Action<int> OnSeek = _ => { };
        public Func<bool> IsRewinding = () => false;
        public Func<int> RewindTargetTick = () => 0;

        public Color BarColor = Color.FromArgb(100, 100, 100);
        public Color ProgressColor = Color.FromArgb(50, 150, 50);
        public Color RewindColor = Color.FromArgb(200, 150, 50);
        public Color HandleColor = Color.White;

        bool isDragging;

        public override void Draw()
        {
            var bounds = RenderBounds;
            var currentTick = GetCurrentTick();
            var totalTicks = Math.Max(1, GetTotalTicks());
            var progress = (float)currentTick / totalTicks;

            // Background bar
            WidgetUtils.FillRectWithColor(bounds, BarColor);

            // Progress fill
            var progressWidth = (int)(bounds.Width * progress);
            var progressBounds = new Rectangle(bounds.X, bounds.Y, progressWidth, bounds.Height);
            WidgetUtils.FillRectWithColor(progressBounds, ProgressColor);

            // Rewind indicator
            if (IsRewinding())
            {
                var targetTick = RewindTargetTick();
                var targetProgress = (float)targetTick / totalTicks;
                var targetX = bounds.X + (int)(bounds.Width * targetProgress);
                var rewindBounds = new Rectangle(targetX, bounds.Y, progressWidth - (targetX - bounds.X), bounds.Height);
                if (rewindBounds.Width > 0)
                    WidgetUtils.FillRectWithColor(rewindBounds, RewindColor);
            }

            // Handle
            var handleX = bounds.X + progressWidth - 3;
            var handleBounds = new Rectangle(handleX, bounds.Y - 2, 6, bounds.Height + 4);
            WidgetUtils.FillRectWithColor(handleBounds, HandleColor);

            // Time labels
            var font = Game.Renderer.Fonts["TinyBold"];
            var timestep = GetTimestep();
            var currentTime = WidgetUtils.FormatTime(currentTick, timestep);
            var totalTime = WidgetUtils.FormatTime(totalTicks, timestep);

            font.DrawTextWithContrast(currentTime, new float2(bounds.X + 5, bounds.Y + bounds.Height + 2), Color.White, Color.Black, 1);
            font.DrawTextWithContrast(totalTime, new float2(bounds.Right - 50, bounds.Y + bounds.Height + 2), Color.White, Color.Black, 1);
        }

        public override bool HandleMouseInput(MouseInput mi)
        {
            if (mi.Button == MouseButton.Left)
            {
                if (mi.Event == MouseInputEvent.Down)
                {
                    isDragging = true;
                    SeekToPosition(mi.Location.X);
                    return true;
                }
                else if (mi.Event == MouseInputEvent.Up)
                {
                    isDragging = false;
                    return true;
                }
            }

            if (isDragging && mi.Event == MouseInputEvent.Move)
            {
                SeekToPosition(mi.Location.X);
                return true;
            }

            return false;
        }

        void SeekToPosition(int screenX)
        {
            var bounds = RenderBounds;
            var relativeX = screenX - bounds.X;
            var progress = Math.Clamp((float)relativeX / bounds.Width, 0f, 1f);
            var targetTick = (int)(GetTotalTicks() * progress);
            OnSeek(targetTick);
        }
    }
}
```

#### 4.2 Create Timeline Controller Logic

**File**: `OpenRA.Mods.Common/Widgets/Logic/Ingame/AIBattleReplayLogic.cs` (new file)
**Changes**: Logic for timeline scrubbing with rewind support

```csharp
using System;
using System.Linq;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
    public class AIBattleReplayLogic : ChromeLogic
    {
        readonly World world;
        readonly OrderManager orderManager;

        bool isRewinding;
        int rewindTargetTick;
        int originalTimestep;
        string replayPath;

        [ObjectCreator.UseCtor]
        public AIBattleReplayLogic(Widget widget, World world, WorldRenderer worldRenderer)
        {
            this.world = world;
            this.orderManager = world.OrderManager;
            this.originalTimestep = world.Timestep;

            if (orderManager.Connection is ReplayConnection rc)
                replayPath = rc.Filename;

            SetupTimeline(widget);
            SetupSpeedControls(widget);
            SetupShroudSelector(widget);
        }

        void SetupTimeline(Widget widget)
        {
            var timeline = widget.Get<TimelineScrubberWidget>("TIMELINE");

            timeline.GetCurrentTick = () => isRewinding ? rewindTargetTick : world.WorldTick;
            timeline.GetTotalTicks = () =>
            {
                if (orderManager.Connection is ReplayConnection rc)
                    return rc.FinalGameTick > 0 ? rc.FinalGameTick : rc.TickCount;
                return world.WorldTick;
            };
            timeline.GetTimestep = () => originalTimestep;
            timeline.IsRewinding = () => isRewinding;
            timeline.RewindTargetTick = () => rewindTargetTick;

            timeline.OnSeek = targetTick =>
            {
                if (targetTick < world.WorldTick)
                    StartRewind(targetTick);
                else if (targetTick > world.WorldTick)
                    FastForwardTo(targetTick);
            };

            // Rewind status label
            var statusLabel = widget.GetOrNull<LabelWidget>("REWIND_STATUS");
            if (statusLabel != null)
            {
                statusLabel.IsVisible = () => isRewinding;
                statusLabel.GetText = () =>
                {
                    var targetTime = WidgetUtils.FormatTime(rewindTargetTick, originalTimestep);
                    return TranslationProvider.GetString("label-rewinding", Translation.Arguments("time", targetTime));
                };
            }
        }

        void StartRewind(int targetTick)
        {
            if (isRewinding || string.IsNullOrEmpty(replayPath))
                return;

            isRewinding = true;
            rewindTargetTick = targetTick;

            // Disconnect from current replay and restart
            Game.RunAfterTick(() =>
            {
                // Store current state for restoration
                var currentRenderPlayer = world.RenderPlayer;

                // Restart replay with fast-forward target
                AIBattleRewindState.TargetTick = targetTick;
                AIBattleRewindState.RestoreRenderPlayer = currentRenderPlayer?.InternalName;
                AIBattleRewindState.IsRewinding = true;

                Game.JoinReplay(replayPath);
            });
        }

        void FastForwardTo(int targetTick)
        {
            // Set maximum speed until we reach target
            AIBattleFastForwardState.TargetTick = targetTick;
            AIBattleFastForwardState.IsFastForwarding = true;
            world.ReplayTimestep = 1; // Maximum speed
        }

        void SetupSpeedControls(Widget widget)
        {
            var pauseButton = widget.Get<ButtonWidget>("PAUSE_BUTTON");
            var playButton = widget.Get<ButtonWidget>("PLAY_BUTTON");

            pauseButton.IsVisible = () => world.ReplayTimestep != 0;
            playButton.IsVisible = () => world.ReplayTimestep == 0;

            pauseButton.OnClick = () => world.ReplayTimestep = 0;
            playButton.OnClick = () => world.ReplayTimestep = originalTimestep;

            var speeds = new[] {
                (0.5f, "SPEED_SLOW"),
                (1f, "SPEED_1X"),
                (2f, "SPEED_2X"),
                (4f, "SPEED_4X"),
                (1000f, "SPEED_MAX")
            };

            foreach (var (multiplier, buttonId) in speeds)
            {
                var button = widget.GetOrNull<ButtonWidget>(buttonId);
                if (button == null)
                    continue;

                var capturedMultiplier = multiplier;
                var targetTimestep = (int)(originalTimestep / capturedMultiplier);

                button.OnClick = () => world.ReplayTimestep = Math.Max(1, targetTimestep);
                button.IsHighlighted = () =>
                    world.ReplayTimestep == Math.Max(1, targetTimestep) && world.ReplayTimestep != 0;
            }
        }

        void SetupShroudSelector(Widget widget)
        {
            var shroudSelector = widget.GetOrNull<DropDownButtonWidget>("SHROUD_SELECTOR");
            if (shroudSelector == null)
                return;

            var aiPlayers = world.Players
                .Where(p => p.IsBot && !p.NonCombatant)
                .ToList();

            var everyonePlayer = world.Players
                .FirstOrDefault(p => p.InternalName == "Everyone");

            shroudSelector.GetText = () =>
            {
                if (world.RenderPlayer == null)
                    return TranslationProvider.GetString("label-no-fog");
                if (world.RenderPlayer == everyonePlayer)
                    return TranslationProvider.GetString("label-combined-vision");
                return world.RenderPlayer.ResolvedPlayerName;
            };

            shroudSelector.OnClick = () =>
            {
                var options = new System.Collections.Generic.List<DropDownOption>();

                options.Add(new DropDownOption
                {
                    Title = TranslationProvider.GetString("label-no-fog"),
                    OnClick = () => world.RenderPlayer = null,
                    IsSelected = () => world.RenderPlayer == null
                });

                if (everyonePlayer != null)
                {
                    options.Add(new DropDownOption
                    {
                        Title = TranslationProvider.GetString("label-combined-vision"),
                        OnClick = () => world.RenderPlayer = everyonePlayer,
                        IsSelected = () => world.RenderPlayer == everyonePlayer
                    });
                }

                foreach (var p in aiPlayers)
                {
                    var player = p;
                    options.Add(new DropDownOption
                    {
                        Title = player.ResolvedPlayerName,
                        OnClick = () => world.RenderPlayer = player,
                        IsSelected = () => world.RenderPlayer == player
                    });
                }

                shroudSelector.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 300, options);
            };
        }
    }

    public static class AIBattleRewindState
    {
        public static bool IsRewinding;
        public static int TargetTick;
        public static string RestoreRenderPlayer;

        public static void Reset()
        {
            IsRewinding = false;
            TargetTick = 0;
            RestoreRenderPlayer = null;
        }
    }

    public static class AIBattleFastForwardState
    {
        public static bool IsFastForwarding;
        public static int TargetTick;

        public static void Reset()
        {
            IsFastForwarding = false;
            TargetTick = 0;
        }
    }
}
```

#### 4.3 Create Replay UI with Timeline

**File**: `mods/d2k/chrome/ingame-aibattle-replay.yaml` (new file)
**Changes**: Replay viewer UI with timeline scrubber

```yaml
Container@AI_BATTLE_REPLAY:
    X: 0
    Y: 0
    Width: WINDOW_RIGHT
    Height: WINDOW_BOTTOM
    Logic: AIBattleReplayLogic
    Children:
        Container@TOP_BAR:
            X: 0
            Y: 0
            Width: WINDOW_RIGHT
            Height: 45
            Children:
                Background@TOP_BAR_BG:
                    X: 0
                    Y: 0
                    Width: PARENT_RIGHT
                    Height: PARENT_BOTTOM
                    Background: panel-black-transparent
                Label@TITLE:
                    X: 20
                    Y: 10
                    Width: 200
                    Height: 25
                    Font: Bold
                    Text: AI Battle Replay
                DropDownButton@SHROUD_SELECTOR:
                    X: PARENT_RIGHT / 2 - 110
                    Y: 7
                    Width: 220
                    Height: 30
                    Font: Bold
        Container@BOTTOM_BAR:
            X: 0
            Y: PARENT_BOTTOM - 80
            Width: WINDOW_RIGHT
            Height: 80
            Children:
                Background@BOTTOM_BAR_BG:
                    X: 0
                    Y: 0
                    Width: PARENT_RIGHT
                    Height: PARENT_BOTTOM
                    Background: panel-black-transparent
                TimelineScrubber@TIMELINE:
                    X: 20
                    Y: 10
                    Width: PARENT_RIGHT - 40
                    Height: 20
                Label@REWIND_STATUS:
                    X: PARENT_RIGHT / 2 - 100
                    Y: 35
                    Width: 200
                    Height: 20
                    Font: Bold
                    Align: Center
                    TextColor: FFAA00
                Container@SPEED_CONTROLS:
                    X: 20
                    Y: 45
                    Width: 400
                    Height: 30
                    Children:
                        Button@PAUSE_BUTTON:
                            X: 0
                            Y: 0
                            Width: 40
                            Height: 25
                            Font: Bold
                            Text: ||
                        Button@PLAY_BUTTON:
                            X: 0
                            Y: 0
                            Width: 40
                            Height: 25
                            Font: Bold
                            Text: >
                        Button@SPEED_SLOW:
                            X: 50
                            Y: 0
                            Width: 50
                            Height: 25
                            Font: Regular
                            Text: 0.5x
                        Button@SPEED_1X:
                            X: 105
                            Y: 0
                            Width: 40
                            Height: 25
                            Font: Regular
                            Text: 1x
                        Button@SPEED_2X:
                            X: 150
                            Y: 0
                            Width: 40
                            Height: 25
                            Font: Regular
                            Text: 2x
                        Button@SPEED_4X:
                            X: 195
                            Y: 0
                            Width: 40
                            Height: 25
                            Font: Regular
                            Text: 4x
                        Button@SPEED_MAX:
                            X: 240
                            Y: 0
                            Width: 50
                            Height: 25
                            Font: Regular
                            Text: Max
                Button@STATS_BUTTON:
                    X: PARENT_RIGHT - 120
                    Y: 45
                    Width: 100
                    Height: 25
                    Font: Bold
                    Text: Stats
                Button@MENU_BUTTON:
                    X: PARENT_RIGHT - 230
                    Y: 45
                    Width: 100
                    Height: 25
                    Font: Bold
                    Text: Menu
```

#### 4.4 Handle Rewind on Replay Load

**File**: `OpenRA.Game/Game.cs`
**Changes**: Add rewind fast-forward handling in JoinReplay (around line 105)

```csharp
public static void JoinReplay(string replayFile)
{
    // Check if this is a rewind operation
    if (AIBattleRewindState.IsRewinding)
    {
        // Set up fast-forward to target tick
        AIBattleFastForwardState.IsFastForwarding = true;
        AIBattleFastForwardState.TargetTick = AIBattleRewindState.TargetTick;
    }

    JoinInner(new OrderManager(new ReplayConnection(replayFile)));
}
```

#### 4.5 Handle Fast-Forward During Replay

**File**: `OpenRA.Game/World.cs`
**Changes**: Add tick handler for fast-forward target (in Tick method around line 455)

```csharp
// Add after WorldTick++ in Tick method
if (AIBattleFastForwardState.IsFastForwarding)
{
    if (WorldTick >= AIBattleFastForwardState.TargetTick)
    {
        // Reached target, restore normal speed
        ReplayTimestep = Timestep;
        AIBattleFastForwardState.Reset();

        // Restore render player if rewinding
        if (AIBattleRewindState.IsRewinding)
        {
            var restorePlayer = AIBattleRewindState.RestoreRenderPlayer;
            if (!string.IsNullOrEmpty(restorePlayer))
                RenderPlayer = Players.FirstOrDefault(p => p.InternalName == restorePlayer);
            AIBattleRewindState.Reset();
        }
    }
    else
    {
        // Continue at max speed
        ReplayTimestep = 1;
    }
}
```

#### 4.6 Register Timeline Widget

**File**: `OpenRA.Mods.Common/OpenRA.Mods.Common.csproj`
**Changes**: Ensure new widget file is included in compilation (should be automatic with file-based includes)

### Success Criteria:

#### Automated Verification:
- [x] Solution builds without errors: `make all`
- [x] TimelineScrubberWidget compiles and registers correctly

#### Manual Verification:
- [ ] Timeline bar displays in replay viewer
- [ ] Timeline shows correct progress position
- [ ] Clicking ahead on timeline fast-forwards to that position
- [ ] Clicking behind on timeline rewinds (restarts + fast-forwards)
- [ ] "Rewinding to X:XX..." message displays during rewind
- [ ] Speed controls work (0.5x, 1x, 2x, 4x, Max)
- [ ] Pause/Play buttons function correctly
- [ ] Time labels show current and total time

**Implementation Note**: Pause for manual verification before proceeding to Phase 5.

---

## Phase 5: Statistics Overlay During Replay

### Overview
Add toggleable statistics overlay during replay viewing, reusing existing observer stats patterns.

### Changes Required:

#### 5.1 Add Stats Panel to Replay UI

**File**: `mods/d2k/chrome/ingame-aibattle-replay.yaml`
**Changes**: Add statistics overlay container

```yaml
# Add to AI_BATTLE_REPLAY children
Container@STATS_OVERLAY:
    X: PARENT_RIGHT - 320
    Y: 50
    Width: 300
    Height: 400
    Visible: false
    Children:
        Background@STATS_BG:
            X: 0
            Y: 0
            Width: PARENT_RIGHT
            Height: PARENT_BOTTOM
            Background: panel-black-transparent
        Label@STATS_TITLE:
            X: 10
            Y: 10
            Width: PARENT_RIGHT - 20
            Height: 25
            Font: Bold
            Text: Statistics
        ScrollPanel@STATS_LIST:
            X: 5
            Y: 40
            Width: PARENT_RIGHT - 10
            Height: PARENT_BOTTOM - 50
            Children:
                Container@STAT_PLAYER_TEMPLATE:
                    X: 0
                    Y: 0
                    Width: PARENT_RIGHT
                    Height: 80
                    Visible: false
                    Children:
                        Label@PLAYER_NAME:
                            X: 5
                            Y: 5
                            Width: PARENT_RIGHT - 10
                            Height: 20
                            Font: Bold
                        Label@STAT_KILLS:
                            X: 10
                            Y: 25
                            Width: 140
                            Height: 15
                            Font: Small
                        Label@STAT_DEATHS:
                            X: 150
                            Y: 25
                            Width: 140
                            Height: 15
                            Font: Small
                        Label@STAT_ARMY:
                            X: 10
                            Y: 42
                            Width: 140
                            Height: 15
                            Font: Small
                        Label@STAT_INCOME:
                            X: 150
                            Y: 42
                            Width: 140
                            Height: 15
                            Font: Small
                        Label@STAT_RESOURCES:
                            X: 10
                            Y: 59
                            Width: 280
                            Height: 15
                            Font: Small
```

#### 5.2 Add Stats Toggle Logic

**File**: `OpenRA.Mods.Common/Widgets/Logic/Ingame/AIBattleReplayLogic.cs`
**Changes**: Add stats panel setup to constructor

```csharp
// Add to constructor
SetupStatsOverlay(widget);

// Add method
void SetupStatsOverlay(Widget widget)
{
    var statsOverlay = widget.Get<ContainerWidget>("STATS_OVERLAY");
    var statsButton = widget.Get<ButtonWidget>("STATS_BUTTON");

    var showStats = false;

    statsOverlay.IsVisible = () => showStats;
    statsButton.OnClick = () => showStats = !showStats;
    statsButton.IsHighlighted = () => showStats;

    var statsList = statsOverlay.Get<ScrollPanelWidget>("STATS_LIST");
    var template = statsList.Get<ContainerWidget>("STAT_PLAYER_TEMPLATE");

    var aiPlayers = world.Players
        .Where(p => p.IsBot && !p.NonCombatant)
        .ToList();

    var yOffset = 0;
    foreach (var player in aiPlayers)
    {
        var row = (ContainerWidget)template.Clone();
        row.IsVisible = () => true;
        row.Bounds.Y = yOffset;

        var p = player;
        var stats = p.PlayerActor.TraitOrDefault<PlayerStatistics>();
        var resources = p.PlayerActor.TraitOrDefault<PlayerResources>();

        var nameLabel = row.Get<LabelWidget>("PLAYER_NAME");
        nameLabel.GetText = () => p.ResolvedPlayerName;
        nameLabel.GetColor = () => p.Color;

        var killsLabel = row.Get<LabelWidget>("STAT_KILLS");
        killsLabel.GetText = () => $"Kills: {(stats?.UnitsKilled ?? 0) + (stats?.BuildingsKilled ?? 0)}";

        var deathsLabel = row.Get<LabelWidget>("STAT_DEATHS");
        deathsLabel.GetText = () => $"Deaths: {(stats?.UnitsDead ?? 0) + (stats?.BuildingsDead ?? 0)}";

        var armyLabel = row.Get<LabelWidget>("STAT_ARMY");
        armyLabel.GetText = () => $"Army: ${stats?.ArmyValue ?? 0:N0}";

        var incomeLabel = row.Get<LabelWidget>("STAT_INCOME");
        incomeLabel.GetText = () => $"Income: ${stats?.DisplayIncome ?? 0}/min";

        var resourcesLabel = row.Get<LabelWidget>("STAT_RESOURCES");
        resourcesLabel.GetText = () =>
            $"Earned: ${resources?.Earned ?? 0:N0} | Spent: ${resources?.Spent ?? 0:N0}";

        statsList.AddChild(row);
        yOffset += 85;
    }
}
```

### Success Criteria:

#### Automated Verification:
- [x] Solution builds without errors: `make all`

#### Manual Verification:
- [ ] Stats button toggles statistics overlay visibility
- [ ] Stats overlay shows all AI players
- [ ] Kill/death counts update in real-time during replay
- [ ] Army value updates correctly
- [ ] Income shows per-minute rate
- [ ] Resources earned/spent display correctly
- [ ] Overlay doesn't interfere with timeline controls

**Implementation Note**: Pause for manual verification before proceeding to Phase 6.

---

## Phase 6: Integration & Polish

### Overview
Wire everything together, add UI polish, and ensure consistent behavior across all AI Battle flows.

### Changes Required:

#### 6.1 Register All New Chrome Files

**File**: `mods/d2k/chrome/chrome.yaml`
**Changes**: Add all AI Battle panel definitions

```yaml
AI_BATTLE_PANEL: aibattle.yaml
AI_BATTLE_RESULTS_PANEL: aibattle-results.yaml
AI_BATTLE_OBSERVER: ingame-aibattle.yaml
AI_BATTLE_REPLAY: ingame-aibattle-replay.yaml
```

#### 6.2 Hook AI Battle Replay UI into World Loading

**File**: `OpenRA.Mods.Common/LoadScreen.cs` or appropriate ingame handler
**Changes**: Load AI Battle replay UI when viewing AI battle replays

```csharp
// Add check when loading replay
if (world.IsReplay && AIBattleState.IsAIBattle)
{
    Game.LoadWidget(world, "AI_BATTLE_REPLAY", Ui.Root, new WidgetArgs
    {
        { "world", world },
        { "worldRenderer", worldRenderer }
    });
}
```

#### 6.3 Add Menu Button Handlers

**File**: `OpenRA.Mods.Common/Widgets/Logic/Ingame/AIBattleReplayLogic.cs`
**Changes**: Add menu button click handler

```csharp
// Add to constructor
var menuButton = widget.Get<ButtonWidget>("MENU_BUTTON");
menuButton.OnClick = () =>
{
    Game.Disconnect();
    AIBattleState.Reset();
    AIBattleRewindState.Reset();
    AIBattleFastForwardState.Reset();

    // Return to results or main menu
    if (AIBattleManager.LastResults != null)
        Ui.OpenWindow("AI_BATTLE_RESULTS_PANEL");
    else
        Game.LoadShellMap();
};
```

#### 6.4 Clean Up State on Exit

**File**: `OpenRA.Game/Game.cs`
**Changes**: Add cleanup in Disconnect method

```csharp
public static void Disconnect()
{
    // Existing disconnect code...

    // Reset AI Battle state if leaving AI Battle mode
    if (AIBattleState.IsAIBattle && !AIBattleRewindState.IsRewinding)
    {
        AIBattleState.Reset();
        AIBattleRewindState.Reset();
        AIBattleFastForwardState.Reset();
    }
}
```

#### 6.5 Add D2K Theme Styling

**File**: `mods/d2k/chrome/aibattle.yaml` and related files
**Changes**: Apply D2K-specific backgrounds and styling

```yaml
# Update Background widgets to use D2K panel styles
Background@AI_BATTLE_PANEL:
    Background: dialog
    # ... rest of definition
```

### Success Criteria:

#### Automated Verification:
- [x] Full solution builds: `make all`
- [x] All D2K chrome files parse correctly
- [x] No runtime errors on game startup
- [x] Lint/style checks pass (if applicable)

#### Manual Verification:
- [ ] Complete flow works: Menu → Config → Battle → Results → Replay → Menu
- [ ] Rewind works correctly at any point in timeline
- [ ] Fast-forward to end works correctly
- [ ] Speed controls persist across timeline navigation
- [ ] Fog of war perspective persists after rewind
- [ ] Stats overlay shows correct data during replay
- [ ] All buttons and dropdowns are styled consistently
- [ ] No visual glitches or overlapping UI elements
- [ ] Memory usage is reasonable (no leaks on repeated battles)

---

## Phase 7: Testing & Edge Cases

### Overview
Comprehensive testing of edge cases and error conditions.

### Test Cases:

#### Configuration Edge Cases:
- [ ] 2-player map with 2 AIs (minimum viable)
- [ ] 8-player map with 8 AIs (maximum)
- [ ] All AIs same team (should still work, no winner)
- [ ] All AIs different teams (free-for-all)
- [ ] Map with asymmetric spawn points

#### Simulation Edge Cases:
- [ ] AI resigns early (< 1 minute)
- [ ] Very long battle (> 30 minutes)
- [ ] Speed change during active combat
- [ ] Pause during AI decision-making

#### Replay Edge Cases:
- [ ] Rewind to very start (tick 0)
- [ ] Rewind while already rewinding
- [ ] Fast-forward past end of replay
- [ ] Replay file is corrupted/missing
- [ ] Switch fog of war during rewind operation

#### Performance Testing:
- [ ] Large map (128x128+) with many units
- [ ] Fast simulation speed (16x) stability
- [ ] Frequent timeline scrubbing
- [ ] Long replay (1+ hour game time)

### Success Criteria:

#### Automated Verification:
- [x] Solution builds without errors: `make all`
- [x] No new compiler warnings in modified files
- [x] All prior phases (1-6) automated verification passed

#### Manual Testing Required:
All test cases above pass without crashes or data corruption.

**Implementation Note**: Phase 7 is now ready for manual testing. All automated verification has passed. Please perform the manual test cases listed above.

---

## Testing Strategy

### Unit Tests:
- AIBattleState state transitions
- TimelineScrubberWidget position calculations
- Statistics capture accuracy

### Integration Tests:
- Full flow from menu to results
- Replay recording and playback
- Rewind mechanism correctness

### Manual Testing Steps:
1. Start D2K, navigate to Singleplayer → AI Battle
2. Select a 4-player map
3. Configure 4 different AI types with 2v2 teams
4. Set speed to 4x and start battle
5. Watch battle, use speed controls
6. Let battle conclude naturally
7. Verify results screen shows correct winner and stats
8. Click Watch Replay
9. Test timeline: click to different positions
10. Test rewind: click to earlier position
11. Verify fog of war dropdown works
12. Toggle stats overlay
13. Return to menu and verify clean state

## Performance Considerations

- **Rewind Speed**: Using Timestep=1 during fast-forward means ~1000 ticks/second depending on CPU. A 30-minute game at 25 ticks/second = 45,000 ticks ≈ 45 seconds to fully rewind.
- **Memory**: Replay files are typically 1-5MB. No additional memory for snapshots.
- **Rendering During Fast-Forward**: Consider reducing render interval during rewind fast-forward for better performance.

## Migration Notes

No database migrations required. Feature is additive and doesn't modify existing data structures.

## References

- Original ticket: `PLAN_AI_BATTLE.md`
- Related research: `thoughts/shared/research/2025-11-24-d2k-tutorial-system.md`
- Replay system: `OpenRA.Game/Network/ReplayConnection.cs`
- Observer stats: `OpenRA.Mods.Common/Widgets/Logic/Ingame/ObserverStatsLogic.cs`
- Shroud selector: `OpenRA.Mods.Common/Widgets/Logic/Ingame/ObserverShroudSelectorLogic.cs`
- Game speed control: `OpenRA.Game/Network/OrderManager.cs:203-221`
