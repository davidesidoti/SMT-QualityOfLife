---
name: deploy
description: Build the mod in Release and copy the DLL to the BepInEx plugins folder for in-game testing
disable-model-invocation: true
---

Run:

1. `dotnet build SMTQualityOfLife -c Release`
2. Ask the user for their game path if not already known (check memory), then copy `SMTQualityOfLife/bin/Release/netstandard2.1/SMTQualityOfLife.dll` to `<GamePath>/BepInEx/plugins/SMTQualityOfLife.dll`
3. Confirm the copy succeeded and remind the user to relaunch the game.
