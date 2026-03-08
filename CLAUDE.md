# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A BepInEx 5.x mod (netstandard2.1) for the Unity game **Supermarket Together**. The mod provides quality-of-life features via Harmony patches and an in-game GUI accessed with `Ctrl+H`.

## Build Commands

```bash
# Debug build
dotnet build SMTQualityOfLife -c Debug

# Release build
dotnet build SMTQualityOfLife -c Release
```

Output: `SMTQualityOfLife/bin/<Config>/netstandard2.1/SMTQualityOfLife.dll`

**Deploy for testing:** Copy the DLL to `<Game>/BepInEx/plugins/SMTQualityOfLife/` then launch the game.

There are no automated tests — validation is manual in-game (`Ctrl+H` to open mod UI).

## Architecture

### Entry Point
`Plugin.cs` — `BaseUnityPlugin` subclass. Registers BepInEx `ConfigEntry<T>` values, instantiates feature modules, applies Harmony patches via `Harmony.PatchAll()`, handles keyboard shortcuts in `Update()`, and routes `OnGUI()` calls to active modules.

### Feature Modules (`src/`)
Each feature is a self-contained class with three responsibilities:
1. **Config** — binds its own `ConfigEntry<T>` settings in the constructor
2. **GUI** — `DrawWindow()` / `DrawWindowContent()` using Unity IMGUI
3. **Harmony Patches** — defined in a nested `namespace SMTQualityOfLife.Patches` within the same file

| File | Module | Status |
|------|--------|--------|
| `NPCAdder.cs` | Raises the in-game NPC employee cap beyond 10 (up to 15) | Active |
| `LowCountProducts.cs` | Injects an "Add Low Count Products" button into the manager blackboard UI | Active |
| `MainManager.cs` | Main mod selection window; routes to sub-windows | Active |
| `NpcUpgradeGating.cs` | Checks via reflection whether all Extra Employee upgrades are purchased | Helper |
| `GUIUtilities.cs` | Shared IMGUI helper methods (styles, section drawers) | Shared |
| `DebugBlackboard.cs` | Debug dump utilities triggered by `Ctrl+F7–F11` | Debug |

### GUI Flow
- `Ctrl+H` toggles `Plugin.IsMainWindowEnabled`
- `MainManager` window shows module toggles; "Mod Settings" buttons switch to sub-windows
- Each sub-window has a `< Back` button returning to `MainManager`
- Windows use unique integer IDs: `0` = MainManager, `1` = NPCAdder / LowCountProducts

### Harmony Patch Pattern
Patches live in a nested namespace at the bottom of each feature file:
```csharp
namespace SMTQualityOfLife.Patches
{
    [HarmonyPatch(typeof(SomeGameClass))]
    internal class MyPatch
    {
        [HarmonyPatch("MethodName")]
        [HarmonyPostfix]
        public static void Postfix(SomeGameClass __instance) { ... }
    }
}
```
Use `AccessTools` for accessing private game methods/fields by reflection rather than hard-coding method calls that may break on game updates.

### Config
All settings persist in `BepInEx/config/SMTQualityOfLife.cfg`. Use `ConfigEntry<T>` for every user-configurable value. Gate new features as disabled by default.

## Coding Conventions
- Namespace: `SMTQualityOfLife`
- Indent: 4 spaces; Allman braces
- Naming: PascalCase for types/methods, camelCase for locals, `_underscored` for private fields
- One public type per file; filename matches type name
- Do not commit game assemblies; only the reference DLLs required for compilation live in `lib/`

## Versioning
Bump `<Version>` in `SMTQualityOfLife/SMTQualityOfLife.csproj` for any user-visible change. Use short imperative commit messages (e.g., `Add NPCAdder threshold check`).
