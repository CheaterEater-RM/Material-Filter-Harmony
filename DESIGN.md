# Material Filter — Design Document

## Overview

Allows filtering items in stockpiles, shelves, bills, and outfit policies by the material they are made from (wood, steel, cloth, devilstrand, etc.). Fork of Kami Katze's Material Filter, rewritten to remove HugsLib dependency and fix performance issues.

## Core Mechanics

### Material Filter Generation

At startup (in `[StaticConstructorOnStartup]`), the mod scans all `ThingDef`s with `IsStuff == true` and generates a `SpecialThingFilterDef` for each material. Each filter uses a shared `MaterialSpecialThingFilterWorker` class — the original mod dynamically emitted a unique IL class per material via `AssemblyBuilder`, which was extremely slow and fragile.

The generated filters are hidden from the vanilla `ThingFilterUI` via a prefix patch and instead displayed in a custom popup window.

### Filter Window

A custom `MaterialFilterWindow` (subclass of `Window`) displays all material filters as a scrollable checklist with Clear All / Allow All buttons. The window is opened by "Filter>>" buttons injected into three UI locations via postfix patches.

## Architecture

### Harmony Patches

| Patch Target | Type | Purpose |
|---|---|---|
| `ITab_Storage.FillTab` | Postfix | Adds "Filter>>" button to storage tab |
| `ThingFilterUI.DoThingFilterConfigWindow` | Prefix | Hides generated filters from vanilla UI |
| `Dialog_BillConfig.DoWindowContents` | Postfix | Adds "Filter>>" button to bill config (only if recipe has stuff ingredients) |
| `Dialog_ManagePolicies<ApparelPolicy>.DoWindowContents` | Postfix | Adds "Filter>>" button to apparel policy window |

### Key Classes

| Class | Purpose |
|---|---|
| `MaterialFilter_Init` | `[StaticConstructorOnStartup]` entry point; generates filter defs and applies Harmony patches |
| `MaterialSpecialThingFilterWorker` | Shared worker class; checks if `Thing.Stuff` matches the bound material |
| `MaterialFilterWindow` | Custom popup window with material checklist UI |

## Compatibility

### Hard Dependencies
- Harmony (runtime patching)

### Save Key Compatibility
The `saveKey` format (`"allow" + defName`) matches the original mod, so saves from the original mod should transfer cleanly.

## Performance Considerations

### Startup
The original mod created hundreds of `AssemblyBuilder` + `TypeBuilder` instances at startup — one per material. Each involved JIT compilation of dynamically emitted IL. The rewrite uses a single shared class with per-instance field binding, eliminating this overhead entirely.

### Runtime
- `MaterialSpecialThingFilterWorker.Matches()` is a simple reference comparison (`t.Stuff == stuffDef`), which is optimal
- The filter def list is sorted once at startup and reused for every window open
- The `ThingFilterUI` prefix patch passes the pre-built list directly instead of calling `FindAll` + `StartsWith` on every frame

### Window UI
- Filter label widths are measured each frame (required since `Text.CalcSize` depends on current font settings) but uses a simple loop with no allocations
- Scroll view uses indexed for-loop instead of foreach to avoid enumerator allocation

## Implementation Notes

- `SpecialThingFilterDef.Worker` creates the instance via `Activator.CreateInstance()` and doesn't pass a back-reference. We force-access `.Worker` immediately after def creation and call `SetStuffDef()` on the resulting instance to bind it.
- The `ITab_Storage` patch uses `AccessTools.Property` to access the protected `SelStoreSettingsParent` property, since `___` injection only works for fields, not properties.
- `Dialog_BillConfig` has a private `bill` field — accessed via `___bill` injection.
- `Dialog_ManagePolicies<T>` is generic; the patch targets the specific closed generic `Dialog_ManagePolicies<ApparelPolicy>`.
- The `ThingFilterUI.DoThingFilterConfigWindow` has many parameters — the prefix only modifies `forceHiddenFilters` via `ref`.
