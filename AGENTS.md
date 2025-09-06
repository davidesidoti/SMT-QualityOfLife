# Repository Guidelines

## Project Structure & Module Organization
- Root: repository docs, `README.md`, license, and GitHub templates.
- Code: `SMTQualityOfLife/` (C# BepInEx plugin)
  - `src/` feature modules and helpers (e.g., `MainManager.cs`, `GUIUtilities.cs`).
  - `Plugin.cs` plugin entrypoint and config.
  - `lib/` Unity/game reference assemblies (do not edit).
  - Build output: `bin/<Config>/netstandard2.1/SMTQualityOfLife.dll`.

## Build, Test, and Development Commands
- Restore + build (Debug): `dotnet build SMTQualityOfLife -c Debug`
- Release build: `dotnet build SMTQualityOfLife -c Release`
- Install for testing: copy `SMTQualityOfLife/bin/Release/netstandard2.1/SMTQualityOfLife.dll` to `<Game>/BepInEx/plugins/SMTQualityOfLife/`.
- Run: launch the game; open the mod UI with `Ctrl+H`.

## Coding Style & Naming Conventions
- Language: C# (netstandard2.1). Indent with 4 spaces; Allman braces.
- Namespace: `SMTQualityOfLife`.
- Naming: PascalCase for types/methods; camelCase for locals; `_underscored` private fields.
- Files: one public type per file; filename matches type (e.g., `LowCountProducts.cs`).
- Harmony patches: keep targeted and minimal. Prefer feature flags via BepInEx `ConfigEntry<T>` and expose toggles in the UI.
- Do not commit game assets; keep only required reference DLLs under `lib/`.

## Testing Guidelines
- No unit tests yet; rely on manual in‑game validation.
- Smoke test flow:
  1) Build Release; 2) copy DLL; 3) launch game; 4) toggle features (`Ctrl+H`).
- Validate logs for warnings/errors and verify feature behavior (NPC limits, low‑count actions).

## Commit & Pull Request Guidelines
- Commits: short, imperative subject lines (e.g., "Add NPCAdder threshold check"). Reference issues when applicable. Bump version in `SMTQualityOfLife.csproj` for user‑visible changes.
- PRs: use `.github/pull_request_template.md`. Include description, linked issues, build status (`dotnet build -c Release`), manual test steps, and screenshots/GIFs for UI changes.
- Scope PRs narrowly; avoid unrelated refactors. Ensure compatibility with BepInEx 5.x and `netstandard2.1`.

## Security & Configuration Tips
- Never commit game files or secrets. Keep only minimal binaries under `lib/` required for compilation.
- When adding features, gate them with config and default to disabled unless safe.
