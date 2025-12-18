---
date: 2025-11-26T21:04:20Z
researcher: Claude
git_commit: 0211fd9ef0f8b47009da06dafa6d0ae3bacabdaf
branch: ai-battle-d2k
repository: OpenRA
topic: "AI Battle Feature Linter Errors"
tags: [research, codebase, linter, StyleCop, AI-Battle, D2K]
status: complete
last_updated: 2025-11-26
last_updated_by: Claude
---

# Research: AI Battle Feature Linter Errors

**Date**: 2025-11-26T21:04:20Z
**Researcher**: Claude
**Git Commit**: 0211fd9ef0f8b47009da06dafa6d0ae3bacabdaf
**Branch**: ai-battle-d2k
**Repository**: OpenRA

## Research Question
Understanding the linter errors reported in FIX_LINTER_2.md and mapping them to the relevant codebase locations.

## Summary
The linter errors consist of two categories:
1. **Linux/C# Linter Errors**: 33 errors from StyleCop and Roslyn analyzers across 5 C# files
2. **Windows/YAML/Fluent Errors**: 13 errors related to unused/missing fluent translation keys and YAML references

## Detailed Findings

### AIBattleLogic.cs (21 errors)

**File**: `OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs`

| Line | Error Code | Description |
|------|------------|-------------|
| 38:16 | SA1203 | Constant `AIBattleTitle` placed after non-constant fields - should appear before |
| 38:16 | CA1823/IDE0051 | Unused field `AIBattleTitle` - declared but never used |
| 88:6 | SA1500 | Opening brace on line 88 for `onSelect` action shares line with statement |
| 92:7 | SA1009 | Missing space after closing parenthesis on line 92 |
| 92:8 | SA1013 | Missing space before closing brace on line 92 |
| 92:8 | SA1500 | Closing brace shares line with content |
| 92:8 | SA1137 | Indentation inconsistent on line 92 |
| 132:13 | IDE0047 | Redundant parentheses in team calculation expression |
| 221:13 | CA1304 | `char.ToUpper(Type[0])` needs `CultureInfo` parameter |
| 238:24 | IDE0200 | Lambda `() => slotNum.ToString()` can be simplified |
| 238:30 | CA1305 | `slotNum.ToString()` needs `IFormatProvider` |
| 284:56 | CA1305 | `slot.Team.ToString()` needs `IFormatProvider` |
| 292:37 | CA1305 | `t.ToString()` needs `IFormatProvider` |
| 365:17 | IDE0028 | Collection initialization can be simplified |
| 368:4 | IDE0028 | Collection initialization can be simplified |
| 371:4 | IDE0028 | Collection initialization can be simplified |
| 372:4 | IDE0028 | Collection initialization can be simplified |
| 373:4 | IDE0028 | Collection initialization can be simplified |
| 374:4 | IDE0028 | Collection initialization can be simplified |
| 377:4 | IDE0028 | Collection initialization can be simplified |
| 400:2 | SA1508 | Blank line before closing brace at line 400 |

**Specific Code Locations**:

Line 38 - Unused constant field:
```csharp
[FluentReference]
const string AIBattleTitle = "label-ai-battle-title";
```

Lines 86-92 - Formatting issues in `SetupMapSelector()`:
```csharp
{ "onSelect", (Action<string>)(uid =>
{
    selectedMap = modData.MapCache[uid];
    RebuildAISlots();
})},
```

Line 132 - Redundant parentheses:
```csharp
Team = (i % maxTeams) + 1,
```

Line 221 - Culture-insensitive char operation:
```csharp
return char.ToUpper(Type[0]) + Type[1..];
```

Line 238 - Lambda and ToString issues:
```csharp
slotLabel.GetText = () => slotNum.ToString();
```

Lines 365-377 - Order commands construction in `StartAIBattle()`:
```csharp
var orders = new List<Order>();
// ...
orders.Add(Order.Command("option singleplayer True"));
orders.Add(Order.Command($"option explored {exploredMap}"));
// etc.
```

---

### AIBattleResultsLogic.cs (6 errors)

**File**: `OpenRA.Mods.Common/Widgets/Logic/AIBattleResultsLogic.cs`

| Line | Error Code | Description |
|------|------------|-------------|
| 58:15 | IDE0004 | Redundant cast `(ContainerWidget)template.Clone()` |
| 68:26 | IDE0200 | Lambda `() => kills.ToString()` can be simplified |
| 68:32 | CA1305 | `kills.ToString()` needs `IFormatProvider` |
| 72:27 | IDE0200 | Lambda `() => deaths.ToString()` can be simplified |
| 72:33 | CA1305 | `deaths.ToString()` needs `IFormatProvider` |

**Specific Code Locations**:

Line 58 - Redundant cast:
```csharp
var row = (ContainerWidget)template.Clone();
```

Lines 66-72 - ToString issues:
```csharp
var killsLabel = row.Get<LabelWidget>("PLAYER_KILLS");
var kills = stats.UnitsKilled + stats.BuildingsKilled;
killsLabel.GetText = () => kills.ToString();

var deathsLabel = row.Get<LabelWidget>("PLAYER_DEATHS");
var deaths = stats.UnitsDead + stats.BuildingsDead;
deathsLabel.GetText = () => deaths.ToString();
```

---

### AIBattleObserverLogic.cs (2 errors)

**File**: `OpenRA.Mods.Common/Widgets/Logic/Ingame/AIBattleObserverLogic.cs`

| Line | Error Code | Description |
|------|------------|-------------|
| 40:65 | IDE0028 | Collection initialization `new WidgetArgs()` can be simplified |
| 81:8 | CA1822 | Method `SetupSpeedButton` can be marked as static |

**Specific Code Locations**:

Line 40 - Collection initialization:
```csharp
Game.LoadWidget(world, "AI_BATTLE_REPLAY_OVERLAY", Ui.Root, new WidgetArgs());
```

Line 81 - Non-static method without instance data:
```csharp
void SetupSpeedButton(Widget container, string buttonId, int speed)
```

---

### IngameMenuLogic.cs (2 errors)

**File**: `OpenRA.Mods.Common/Widgets/Logic/Ingame/IngameMenuLogic.cs`

| Line | Error Code | Description |
|------|------------|-------------|
| 341:4 | SA1515 | Comment not preceded by blank line |
| 354:25 | IDE0200 | Lambda `() => latestReplay` can be simplified |

**Specific Code Locations**:

Line 341 - Comment without blank line:
```csharp
AIBattleManager.LastResults = results;
// Note: Replay path will be captured later in ShowAIBattleResults after Disconnect()
```

Line 354 - Lambda simplification (inside `CaptureAIBattleReplayPath`):
```csharp
.OrderByDescending(f => System.IO.File.GetCreationTime(f))
```

---

### LoadIngamePlayerOrObserverUILogic.cs (2 errors)

**File**: `OpenRA.Mods.Common/Widgets/Logic/Ingame/LoadIngamePlayerOrObserverUILogic.cs`

| Line | Error Code | Description |
|------|------------|-------------|
| 173:25 | IDE0200 | Lambda expression can be removed |
| 179:2 | SA1518 | File not ending with single newline character |

**Specific Code Locations**:

Line 173 - Lambda in `CaptureAIBattleReplayPath()`:
```csharp
.OrderByDescending(f => System.IO.File.GetCreationTime(f))
```

Line 179/180 - File ends with extra blank line (2 newlines instead of 1)

---

### D2K Fluent/YAML Errors (13 errors)

**Files**:
- `mods/d2k/chrome/aibattle.yaml:111`
- `mods/d2k/fluent/chrome.ftl`

**YAML Error**:

Line 111 of `aibattle.yaml`:
```yaml
Label@HEADER_SLOT:
    X: 10
    Width: 25
    Height: 20
    Font: Bold
    Text: #
```

The `Text: #` is being interpreted incorrectly. The widget references an empty key.

**Unused Fluent Keys in chrome.ftl**:

| Line | Key | Issue |
|------|-----|-------|
| - | `tooltip-ai-battle-game-speed` | Unused key |
| - | `button-ai-battle-1x.tooltip` | Unused attribute |
| - | `button-ai-battle-2x.tooltip` | Unused attribute |
| - | `button-ai-battle-8x.tooltip` | Unused attribute |
| - | `button-ai-battle-16x.tooltip` | Unused attribute |
| - | `button-ai-battle-32x.tooltip` | Unused attribute |
| - | `button-ai-battle-64x.tooltip` | Unused attribute |
| - | `button-ai-battle-128x.tooltip` | Unused attribute |
| - | `label-ai-battle-rewinding` | Unused key |
| - | `label-ai-battle-seeking` | Unused key |
| - | `label-no-fog` | Unused key |
| - | `label-combined-vision` | Unused key |

These keys are defined in `chrome.ftl` (lines 220-277) but not referenced by any widget in the YAML files.

## Code References

### C# Files
- `OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs:38` - Unused constant
- `OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs:86-92` - Brace formatting
- `OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs:132` - Redundant parentheses
- `OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs:221` - Culture-insensitive char
- `OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs:238` - Lambda/ToString
- `OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs:284` - ToString needs format
- `OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs:292` - ToString needs format
- `OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs:365-377` - Collection init
- `OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs:400` - Blank line before brace
- `OpenRA.Mods.Common/Widgets/Logic/AIBattleResultsLogic.cs:58` - Redundant cast
- `OpenRA.Mods.Common/Widgets/Logic/AIBattleResultsLogic.cs:68-72` - Lambda/ToString
- `OpenRA.Mods.Common/Widgets/Logic/Ingame/AIBattleObserverLogic.cs:40` - Collection init
- `OpenRA.Mods.Common/Widgets/Logic/Ingame/AIBattleObserverLogic.cs:81` - Static method
- `OpenRA.Mods.Common/Widgets/Logic/Ingame/IngameMenuLogic.cs:341` - Comment spacing
- `OpenRA.Mods.Common/Widgets/Logic/Ingame/IngameMenuLogic.cs:354` - Lambda simplification
- `OpenRA.Mods.Common/Widgets/Logic/Ingame/LoadIngamePlayerOrObserverUILogic.cs:173` - Lambda
- `OpenRA.Mods.Common/Widgets/Logic/Ingame/LoadIngamePlayerOrObserverUILogic.cs:179` - File ending

### YAML/Fluent Files
- `mods/d2k/chrome/aibattle.yaml:111` - Empty key reference
- `mods/d2k/fluent/chrome.ftl:220-277` - Unused translation keys

## Architecture Documentation

The errors span across the AI Battle feature implementation which includes:

1. **Configuration UI** (`AIBattleLogic.cs`) - Main panel for setting up AI battles
2. **Results Display** (`AIBattleResultsLogic.cs`) - Post-battle results screen
3. **Observer Controls** (`AIBattleObserverLogic.cs`) - Speed control panel during battles
4. **Game Integration** (`IngameMenuLogic.cs`, `LoadIngamePlayerOrObserverUILogic.cs`) - Integration with the existing game flow
5. **Localization** (`aibattle.yaml`, `chrome.ftl`) - UI definitions and translation strings

## Open Questions

- Are the unused fluent keys in chrome.ftl intended for future features (replay UI), or should they be removed?
- Should the `Text: #` in aibattle.yaml use a quoted string or a fluent reference?
