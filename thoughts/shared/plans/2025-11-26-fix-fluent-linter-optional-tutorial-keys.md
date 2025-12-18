# Fix Fluent Linter: Make Tutorial Dialog Keys Optional

## Overview

The CI is failing because `MainMenuLogic.cs` defines `[FluentReference]` attributes for tutorial resume dialog keys, but only D2K has these keys defined. The linter requires all mods to have these keys, even though only D2K has a tutorial.

The fix is to mark these FluentReferences as `optional: true` so mods without tutorials don't need to define the keys.

## Current State Analysis

### The Problem
- `MainMenuLogic.cs` defines 4 FluentReference constants for the tutorial resume dialog
- These are required by the linter in ALL mods that load MainMenuLogic
- Only D2K has these keys defined in `mods/d2k/fluent/chrome.ftl`
- TS (and other mods) fail the linter check with missing key errors

### Key Discovery
The `FluentReferenceAttribute` class (`OpenRA.Game/FluentBundle.cs:27-48`) supports an `Optional` parameter:
```csharp
public FluentReferenceAttribute(bool optional)
{
    Optional = optional;
}
```

## Desired End State

- The tutorial dialog FluentReference keys are marked as optional
- D2K continues to work with its tutorial (keys remain in `mods/d2k/fluent/chrome.ftl`)
- TS, RA, CNC, and other mods pass the linter without needing to define these keys
- CI passes

## What We're NOT Doing

- NOT adding keys to common or other mods
- NOT removing the tutorial functionality from D2K
- NOT changing any other FluentReference attributes

## Implementation Approach

Make the 4 tutorial-related FluentReference attributes optional by changing `[FluentReference]` to `[FluentReference(optional: true)]`.

## Phase 1: Update FluentReference Attributes

### Overview
Mark the tutorial dialog FluentReference constants as optional in MainMenuLogic.cs.

### Changes Required:

#### 1.1 MainMenuLogic.cs

**File**: `OpenRA.Mods.Common/Widgets/Logic/MainMenuLogic.cs`
**Lines**: 39-49

**Current code:**
```csharp
[FluentReference]
const string TutorialResumeTitle = "dialog-tutorial-resume.title";

[FluentReference]
const string TutorialResumePrompt = "dialog-tutorial-resume.prompt";

[FluentReference]
const string TutorialResumeButton = "dialog-tutorial-resume.resume";

[FluentReference]
const string TutorialStartNewButton = "dialog-tutorial-resume.start-new";
```

**New code:**
```csharp
[FluentReference(optional: true)]
const string TutorialResumeTitle = "dialog-tutorial-resume.title";

[FluentReference(optional: true)]
const string TutorialResumePrompt = "dialog-tutorial-resume.prompt";

[FluentReference(optional: true)]
const string TutorialResumeButton = "dialog-tutorial-resume.resume";

[FluentReference(optional: true)]
const string TutorialStartNewButton = "dialog-tutorial-resume.start-new";
```

### Success Criteria:

#### Automated Verification:
- [ ] Build succeeds: `make all`
- [ ] Linter passes for all mods: `make test`
- [ ] No regression in D2K tutorial functionality

#### Manual Verification:
- [ ] D2K tutorial resume dialog still works correctly
- [ ] Other mods (TS, RA, CNC) load without errors

## Testing Strategy

### Automated Tests:
- Run `make test` to verify linter passes for all mods
- Run `make all` to verify build succeeds

### Manual Testing Steps:
1. Launch D2K mod
2. Start the tutorial
3. Exit mid-tutorial
4. Return to main menu and click Tutorial again
5. Verify the resume dialog appears with correct text

## References

- Issue file: `FIX_LINTER.md`
- Research: `thoughts/shared/research/2025-11-26-fluent-tutorial-resume-keys.md`
- FluentReferenceAttribute: `OpenRA.Game/FluentBundle.cs:27-48`
- MainMenuLogic: `OpenRA.Mods.Common/Widgets/Logic/MainMenuLogic.cs:39-49`
