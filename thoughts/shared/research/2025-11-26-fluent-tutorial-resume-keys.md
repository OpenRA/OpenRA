---
date: 2025-11-26T20:14:34Z
researcher: Yibin Long
git_commit: 6a50452cc03c2f24109ecd71316ccbf1d9827b44
branch: feat/d2k-tutorial
repository: OpenRA
topic: "Missing Fluent Keys for Tutorial Resume Dialog"
tags: [research, codebase, fluent, translation, tutorial, ci-failure]
status: complete
last_updated: 2025-11-26
last_updated_by: Yibin Long
---

# Research: Missing Fluent Keys for Tutorial Resume Dialog

**Date**: 2025-11-26T20:14:34Z
**Researcher**: Yibin Long
**Git Commit**: 6a50452cc03c2f24109ecd71316ccbf1d9827b44
**Branch**: feat/d2k-tutorial
**Repository**: OpenRA

## Research Question

CI failing with Fluent validation errors for missing `dialog-tutorial-resume.*` keys in mod ftl files. Need to understand the system and locate relevant files.

## Summary

The CI test failure occurs because `MainMenuLogic.cs` defines four FluentReference constants for the tutorial resume dialog, but the Tiberian Sun mod's `chrome.ftl` file is missing these translation keys. The D2K mod has these keys defined, but TS does not.

## CI Error Details

```
Error: Missing key `dialog-tutorial-resume.prompt` in mod ftl files required by MainMenuLogic.TutorialResumePrompt
Error: Missing key `dialog-tutorial-resume.resume` in mod ftl files required by MainMenuLogic.TutorialResumeButton
Error: Missing key `dialog-tutorial-resume.start-new` in mod ftl files required by MainMenuLogic.TutorialStartNewButton
Error: Missing key `dialog-tutorial-resume.title` in mod ftl files required by MainMenuLogic.TutorialResumeTitle
```

The errors appear during "Testing mod: Tiberian Sun" phase.

## Detailed Findings

### MainMenuLogic FluentReference Constants

**File**: `OpenRA.Mods.Common/Widgets/Logic/MainMenuLogic.cs`

The tutorial resume functionality defines four Fluent key constants at lines 40-49:

```csharp
[FluentReference]
const string TutorialResumeTitle = "dialog-tutorial-resume.title";          // Line 40-41

[FluentReference]
const string TutorialResumePrompt = "dialog-tutorial-resume.prompt";        // Line 43-44

[FluentReference]
const string TutorialResumeButton = "dialog-tutorial-resume.resume";        // Line 46-47

[FluentReference]
const string TutorialStartNewButton = "dialog-tutorial-resume.start-new";   // Line 48-49
```

These constants are used in the tutorial button click handler at lines 160-166:

```csharp
ConfirmationDialogs.ButtonPrompt(modData,
    title: TutorialResumeTitle,
    text: TutorialResumePrompt,
    onConfirm: () => LoadTutorialSave(tutorialSave, tutorialMap.Uid),
    confirmText: TutorialResumeButton,
    onCancel: () => StartNewTutorial(tutorialMap.Uid),
    cancelText: TutorialStartNewButton);
```

### D2K Fluent Definitions (Existing)

**File**: `mods/d2k/fluent/chrome.ftl:8-12`

```fluent
dialog-tutorial-resume =
    .title = Tutorial
    .prompt = Resume your previous tutorial or start over?
    .resume = Resume
    .start-new = Start New
```

### Tiberian Sun Fluent Files (Missing Keys)

**File**: `mods/ts/fluent/chrome.ftl`

This file does NOT contain the `dialog-tutorial-resume` keys.

### Fluent Validation System

**Lint Pass**: `OpenRA.Mods.Common/Lint/CheckFluentReferences.cs`

The validation system works as follows:

1. **Key Discovery** (line 185-229): `ExtractModFluentKeys()` scans for all required Fluent keys
2. **Chrome Logic Scanning** (lines 304-382): `ExtractChromeFluentKeys()` processes widget logic classes
3. **Static Field Extraction** (lines 249-267): `ExtractConstFluentKeys()` finds fields with `[FluentReference]` attribute
4. **Validation** (lines 88-91): Compares required keys against available keys in `.ftl` files
5. **Error Generation** (line 91): Emits warning for each missing key with context

The error format is generated at line 91:
```csharp
emitWarning($"Missing key `{group.Key}` in mod ftl files required by {context}");
```

### Fluent File Organization

Fluent translation files are organized by mod:

| Mod | Chrome FTL Path |
|-----|-----------------|
| Common | `mods/common/fluent/chrome.ftl` |
| D2K | `mods/d2k/fluent/chrome.ftl` |
| CNC | `mods/cnc/fluent/chrome.ftl` |
| RA | `mods/ra/fluent/chrome.ftl` |
| TS | `mods/ts/fluent/chrome.ftl` |

### Dialog Fluent Key Pattern

Standard dialog keys follow this pattern:

```fluent
dialog-{name} =
    .title = {Dialog Title}
    .prompt = {Message Text}
    .confirm = {Confirm Button Text}
    .cancel = {Cancel Button Text}
```

The tutorial resume dialog uses custom button names `.resume` and `.start-new` instead of `.confirm` and `.cancel`.

## Code References

### Primary Files

- `OpenRA.Mods.Common/Widgets/Logic/MainMenuLogic.cs:40-49` - FluentReference constant definitions
- `OpenRA.Mods.Common/Widgets/Logic/MainMenuLogic.cs:160-166` - ConfirmationDialogs.ButtonPrompt usage
- `OpenRA.Mods.Common/Widgets/Logic/MainMenuLogic.cs:143-170` - Tutorial button initialization and click handler
- `mods/d2k/fluent/chrome.ftl:8-12` - Existing D2K translation (working)
- `mods/ts/fluent/chrome.ftl` - Missing TS translation (failing)

### Validation System Files

- `OpenRA.Mods.Common/Lint/CheckFluentReferences.cs:29` - Lint pass class definition
- `OpenRA.Mods.Common/Lint/CheckFluentReferences.cs:72-92` - Main validation logic
- `OpenRA.Mods.Common/Lint/CheckFluentReferences.cs:88-91` - Error generation loop
- `OpenRA.Mods.Common/Lint/CheckFluentReferences.cs:249-267` - Static const field scanning
- `OpenRA.Mods.Common/Lint/CheckFluentReferences.cs:304-382` - Chrome/widget key extraction
- `OpenRA.Mods.Common/UtilityCommands/CheckYaml.cs:93-104` - Lint pass invocation

### Supporting Files

- `OpenRA.Mods.Common/Widgets/ConfirmationDialogs.cs:19-30` - ButtonPrompt method signature
- `OpenRA.Mods.Common/Widgets/ConfirmationDialogs.cs:38-39` - Title translation resolution
- `OpenRA.Mods.Common/Widgets/ConfirmationDialogs.cs:70` - Confirm button text resolution
- `OpenRA.Mods.Common/Widgets/ConfirmationDialogs.cs:87` - Cancel button text resolution
- `OpenRA.Game/FluentBundle.cs:26-49` - FluentReferenceAttribute definition

## Architecture Documentation

### Fluent Translation System

1. **Attribute-Based Discovery**: `[FluentReference]` attribute marks string constants as translation key references
2. **Compile-Time Validation**: The lint pass runs during `make test` to verify all referenced keys exist
3. **Per-Mod Translation**: Each mod defines its own `.ftl` files; keys must exist in each mod that uses them
4. **Runtime Resolution**: `FluentProvider.GetMessage()` resolves keys to localized strings at runtime

### Key Requirement Scope

The `MainMenuLogic` class is part of `OpenRA.Mods.Common`, which is shared by all mods. Therefore:
- Any FluentReference constants in shared code must have corresponding keys in ALL mods' `.ftl` files
- The D2K mod has the keys (chrome.ftl:8-12)
- The TS mod is missing the keys
- CNC and RA mods may also need these keys if they test MainMenuLogic

## Related Research

None yet.

## Open Questions

1. Should TS, CNC, and RA mods also have these tutorial resume keys, or should the keys be moved to common?
2. Does each mod that uses MainMenuLogic need tutorial support, or should non-tutorial mods handle this differently?
