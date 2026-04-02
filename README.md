# Material Filter

Filter stored items by the material they are made from — in stockpiles, shelves, bills, and outfit policies.

A RimWorld 1.6 mod by CheaterEater. Fork of [Kami Katze's Material Filter](https://steamcommunity.com/sharedfiles/filedetails/?id=1541305730), rewritten to use Harmony directly (no HugsLib dependency) with performance improvements.

## Features

- Adds a "Filter>>" button to stockpile/shelf, bill, and apparel policy windows
- Popup lists every material in the game with checkboxes
- Works with modded materials automatically (anything registered as stuff)
- Significantly faster startup than the original (no dynamic IL assembly generation)

## Installation

1. Subscribe on Steam Workshop, or download and extract to your RimWorld `Mods` folder
2. Enable **Harmony** and **Material Filter** in the mod list
3. Harmony must load before this mod

## Compatibility

- **RimWorld 1.6** (Odyssey)
- Safe to add to existing saves
- Compatible with Combat Extended
- No HugsLib required

## Changes from Original

- **Removed HugsLib dependency** — uses Harmony directly with `[StaticConstructorOnStartup]`
- **Eliminated dynamic assembly generation** — the original created a new `AssemblyBuilder` + `TypeBuilder` for every material (~100+ dynamic assemblies). Now uses a single `MaterialSpecialThingFilterWorker` class with per-instance state binding
- **Pre-sorted filter list** — built once at startup, no repeated LINQ queries
- **HashSet-based filter hiding** — O(1) lookup instead of string prefix scanning
- **Replaced Traverse with AccessTools** — proper field injection in patches
- **Updated for RimWorld 1.6** — verified all method signatures against decompiled source

## Building from Source

1. Create `RimWorld.Paths.props` in the parent directory of this repo:

```xml
<Project>
  <PropertyGroup>
    <RimWorldManaged>C:\path\to\RimWorld\RimWorldWin64_Data\Managed</RimWorldManaged>
    <HarmonyAssemblies>C:\path\to\workshop\content\294100\2009463077\Current\Assemblies</HarmonyAssemblies>
  </PropertyGroup>
</Project>
```

2. Open `Source\MaterialFilter.sln` in Visual Studio
3. Build (Ctrl+Shift+B) — output goes to `1.6\Assemblies\`

## License

GPL-3.0 (same as the original mod)
