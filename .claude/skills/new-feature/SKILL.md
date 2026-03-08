---
name: new-feature
description: Scaffold a new BepInEx mod feature class following the project pattern (config + GUI window + Harmony patch namespace)
---

Create a new feature module for this BepInEx mod. The user will provide the feature name.

Follow the pattern in `SMTQualityOfLife/src/NPCAdder.cs`:

1. Create `SMTQualityOfLife/src/<FeatureName>.cs` with:
   - A public class `<FeatureName>` in namespace `SMTQualityOfLife`
   - Constructor taking `(ConfigFile config, MainManager manager, GUIUtilities guiUtilities)`
   - `SetWindowVisibility(bool)`, `DrawWindow()`, `DrawWindowContent(int)` methods
   - A nested `namespace SMTQualityOfLife.Patches` with Harmony patch stubs
2. Register it in `Plugin.cs` (field, constructor instantiation, `OnGUI` call, window enabled config)
3. Add a section entry in `MainManager.cs` `DrawWindowContent`
