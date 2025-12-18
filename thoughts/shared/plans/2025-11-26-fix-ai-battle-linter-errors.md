# Fix AI Battle Linter Errors Implementation Plan

## Overview

Fix 33 C# linter errors and 13 YAML/Fluent errors in the AI Battle feature files for the D2K mod. These errors prevent `make check` from passing.

## Current State Analysis

The linter reports errors in 5 C# files and 2 localization files:
- `AIBattleLogic.cs` - 21 errors (formatting, unused field, culture-insensitive operations)
- `AIBattleResultsLogic.cs` - 6 errors (redundant cast, formatting)
- `AIBattleObserverLogic.cs` - 2 errors (collection init, static method)
- `IngameMenuLogic.cs` - 2 errors (comment spacing, lambda)
- `LoadIngamePlayerOrObserverUILogic.cs` - 2 errors (lambda, file ending)
- `aibattle.yaml` - 1 error (empty key reference)
- `chrome.ftl` - 12 errors (unused keys/attributes)

### Key Discoveries:
- Fluent keys reported as "unused" (`label-no-fog`, etc.) ARE used by C# code but via string literals
- The linter only detects fluent key usage via `[FluentReference]` annotated static fields
- The `.tooltip` attributes on speed buttons are duplicates of separate `-tooltip` keys
- `Text: #` in YAML is interpreted as empty (# starts a comment)

## Desired End State

All linter errors are resolved:
- `make check` passes with no errors in the AI Battle files
- Code follows OpenRA style guidelines
- Fluent keys used programmatically are properly annotated with `[FluentReference]`

## What We're NOT Doing

- Adding new features or functionality
- Changing any behavior or logic
- Refactoring beyond what's needed to fix linter errors

## Implementation Approach

Fix errors file-by-file, starting with C# files then YAML/Fluent files. Each phase addresses one file completely.

---

## Phase 1: Fix AIBattleLogic.cs (21 errors)

### Overview
Fix formatting issues, remove unused field, fix culture-insensitive operations, simplify lambdas, and use collection initializers.

### Changes Required:

#### 1.1 Remove Unused Field

**File**: `OpenRA.Mods.Common/Widgets/Logic/Mainmenu/AIBattleLogic.cs`
**Lines**: 30-31

Remove the unused `AIBattleTitle` constant.

```csharp
// REMOVE these lines:
[FluentReference]
const string AIBattleTitle = "label-ai-battle-title";
```

#### 1.2 Fix Brace Formatting (lines 86-92)

The closing brace should be on its own line, not followed by `else` on the same line.

**Current**:
```csharp
if (...)
{
    ...
} else
{
```

**Fix to**:
```csharp
if (...)
{
    ...
}
else
{
```

#### 1.3 Remove Redundant Parentheses (line 132)

**Current**:
```csharp
if (!(mapPreview.Map?.Visibility.HasFlag(MapVisibility.MissionSelector) ?? false))
```

**Fix to**:
```csharp
if (!mapPreview.Map?.Visibility.HasFlag(MapVisibility.MissionSelector) ?? false)
```

Note: Need to verify the exact logic here - may need to restructure.

#### 1.4 Fix Culture-Insensitive char.ToUpper (line 221)

**Current**:
```csharp
char.ToUpper(team[0])
```

**Fix to**:
```csharp
char.ToUpper(team[0], CultureInfo.InvariantCulture)
```

Add `using System.Globalization;` at top if not present.

#### 1.5 Fix ToString() Calls (lines 293, 296, 305, 308)

**Current**:
```csharp
.ToString("N0")
```

**Fix to**:
```csharp
.ToString("N0", CultureInfo.InvariantCulture)
```

#### 1.6 Simplify Lambda Expressions (lines 314, 317, 323, 351, 357, 363)

Convert single-statement lambdas from block form to expression form.

**Current**:
```csharp
someField.GetText = () => { return value; };
```

**Fix to**:
```csharp
someField.GetText = () => value;
```

#### 1.7 Use Collection Initializer (lines 365-377)

**Current**:
```csharp
var list = new List<Type>();
list.Add(item1);
list.Add(item2);
```

**Fix to**:
```csharp
var list = new List<Type>
{
    item1,
    item2
};
```

#### 1.8 Remove Blank Line Before Closing Brace (line 400)

Remove the empty line immediately before the final `}`.

### Success Criteria:

#### Automated Verification:
- [x] `make check` shows no errors for AIBattleLogic.cs
- [x] `dotnet build OpenRA.Mods.Common/OpenRA.Mods.Common.csproj` compiles successfully

---

## Phase 2: Fix AIBattleResultsLogic.cs (6 errors)

### Overview
Remove redundant cast, fix ToString() calls, and simplify lambda expressions.

### Changes Required:

#### 2.1 Remove Redundant Cast (line 58)

**File**: `OpenRA.Mods.Common/Widgets/Logic/Mainmenu/AIBattleResultsLogic.cs`

Identify and remove unnecessary type cast.

#### 2.2 Fix ToString() Calls (lines 109, 118, 125)

Same pattern as Phase 1 - add `CultureInfo.InvariantCulture`.

#### 2.3 Simplify Lambda Expressions (lines 96, 97)

Convert block-form lambdas to expression form.

### Success Criteria:

#### Automated Verification:
- [x] `make check` shows no errors for AIBattleResultsLogic.cs
- [x] Build compiles successfully

---

## Phase 3: Fix AIBattleObserverLogic.cs (2 errors)

### Overview
Simplify collection initialization and make method static.

### Changes Required:

#### 3.1 Simplify Collection Initialization (line 40)

**File**: `OpenRA.Mods.Common/Widgets/Logic/Ingame/AIBattleObserverLogic.cs`

Use collection initializer syntax instead of repeated Add() calls.

#### 3.2 Make SetupSpeedButton Static

The method doesn't use any instance state, so it should be marked `static`.

### Success Criteria:

#### Automated Verification:
- [x] `make check` shows no errors for AIBattleObserverLogic.cs
- [x] Build compiles successfully

---

## Phase 4: Fix IngameMenuLogic.cs (2 errors)

### Overview
Add blank line before comment and simplify lambda.

### Changes Required:

#### 4.1 Add Blank Line Before Comment (line 341)

**File**: `OpenRA.Mods.Common/Widgets/Logic/Ingame/IngameMenuLogic.cs`

Add an empty line before a comment block.

#### 4.2 Simplify Lambda Expression (line 354)

Convert block-form lambda to expression form.

### Success Criteria:

#### Automated Verification:
- [x] `make check` shows no errors for IngameMenuLogic.cs
- [x] Build compiles successfully

---

## Phase 5: Fix LoadIngamePlayerOrObserverUILogic.cs (2 errors)

### Overview
Simplify lambda and fix file ending.

### Changes Required:

#### 5.1 Simplify Lambda Expression (line 173)

**File**: `OpenRA.Mods.Common/Widgets/Logic/Ingame/LoadIngamePlayerOrObserverUILogic.cs`

Convert block-form lambda to expression form.

#### 5.2 Fix File Ending

Ensure file ends with exactly one newline (no trailing blank lines or missing newline).

### Success Criteria:

#### Automated Verification:
- [x] `make check` shows no errors for LoadIngamePlayerOrObserverUILogic.cs
- [x] Build compiles successfully

---

## Phase 6: Fix AIBattleReplayLogic.cs Fluent References

### Overview
Add `[FluentReference]` annotations for fluent keys used programmatically, so the linter knows they are in use.

### Changes Required:

#### 6.1 Add FluentReference Constants

**File**: `OpenRA.Mods.Common/Widgets/Logic/Ingame/AIBattleReplayLogic.cs`

Add after line 25 (after class declaration):

```csharp
public class AIBattleReplayLogic : ChromeLogic
{
    [FluentReference("time")]
    const string LabelRewinding = "label-ai-battle-rewinding";

    [FluentReference("time")]
    const string LabelSeeking = "label-ai-battle-seeking";

    [FluentReference]
    const string LabelNoFog = "label-no-fog";

    [FluentReference]
    const string LabelCombinedVision = "label-combined-vision";

    readonly World world;
    // ... rest of existing fields
```

#### 6.2 Update FluentProvider.GetMessage Calls

Replace string literals with the constants:

**Line 84**:
```csharp
// From:
return FluentProvider.GetMessage("label-ai-battle-rewinding", "time", targetTime);
// To:
return FluentProvider.GetMessage(LabelRewinding, "time", targetTime);
```

**Line 90**:
```csharp
// From:
return FluentProvider.GetMessage("label-ai-battle-seeking", "time", targetTime);
// To:
return FluentProvider.GetMessage(LabelSeeking, "time", targetTime);
```

**Lines 195, 208**:
```csharp
// From:
FluentProvider.GetMessage("label-no-fog")
// To:
FluentProvider.GetMessage(LabelNoFog)
```

**Lines 197, 219**:
```csharp
// From:
FluentProvider.GetMessage("label-combined-vision")
// To:
FluentProvider.GetMessage(LabelCombinedVision)
```

### Success Criteria:

#### Automated Verification:
- [x] `make check` no longer reports these keys as unused
- [x] Build compiles successfully

---

## Phase 7: Fix D2K YAML Errors

### Overview
Fix the empty key reference in aibattle.yaml.

### Changes Required:

#### 7.1 Add Fluent Key for Header Slot

**File**: `mods/d2k/fluent/chrome.ftl`

Add after line 206 (after other header labels):

```ftl
label-ai-battle-header-slot = #
```

#### 7.2 Update YAML to Reference Fluent Key

**File**: `mods/d2k/chrome/aibattle.yaml`
**Line**: 116

```yaml
# From:
Text: #
# To:
Text: label-ai-battle-header-slot
```

### Success Criteria:

#### Automated Verification:
- [x] `make check` no longer reports "Empty key" error for HEADER_SLOT

---

## Phase 8: Fix D2K Fluent Unused Keys

### Overview
Remove truly unused fluent keys and attributes.

### Changes Required:

#### 8.1 Remove Unused Key

**File**: `mods/d2k/fluent/chrome.ftl`

Remove line 214:
```ftl
tooltip-ai-battle-game-speed = Base game tick rate. Affects AI decision timing and game pacing. Normal is recommended for fair AI battles.
```

#### 8.2 Remove Unused .tooltip Attributes

Remove the `.tooltip` attribute lines from each button definition (keep the `.label` attributes):

**Lines to remove** (the `.tooltip = ...` lines only):
- Line 230: `    .tooltip = Real-time playback (1x speed)`
- Line 234: `    .tooltip = Fast playback (2x speed)`
- Line 242: `    .tooltip = Very fast playback (8x speed)`
- Line 246: `    .tooltip = Ultra fast playback (16x speed)`
- Line 250: `    .tooltip = Extreme playback (32x speed)`
- Line 254: `    .tooltip = Near maximum playback (64x speed)`
- Line 258: `    .tooltip = Maximum playback speed (128x)`

**Keep** the separate `-tooltip` keys (lines 220-227) as they ARE used by the YAML.

### Success Criteria:

#### Automated Verification:
- [x] `make check` shows no unused key/attribute errors for chrome.ftl

---

## Phase 9: Final Verification

### Overview
Run full linter check and test suite to verify all fixes.

### Success Criteria:

#### Automated Verification:
- [x] `make check` passes with no errors related to AI Battle files
- [x] `make test` passes (no regressions)

#### Manual Verification:
- [ ] AI Battle mode launches correctly in D2K
- [ ] AI Battle replay viewer works
- [ ] All UI text displays correctly

---

## Testing Strategy

### Unit Tests:
- Existing test suite should pass - no new tests needed as this is a linter-only fix

### Integration Tests:
- `make check` validates all linter rules
- `make test` runs full test suite

### Manual Testing Steps:
1. Launch D2K mod
2. Start an AI Battle
3. Verify all text labels display correctly
4. Watch a replay and verify timeline/speed controls work
5. Verify shroud selector shows correct options

## References

- Issue file: `FIX_LINTER_2.md`
- Research document: `thoughts/shared/research/2025-11-26-ai-battle-linter-errors.md`
- Linter implementation: `OpenRA.Mods.Common/Lint/CheckFluentReferences.cs`
- FluentReference attribute: `OpenRA.Game/FluentBundle.cs:26-49`
