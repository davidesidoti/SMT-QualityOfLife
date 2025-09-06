# Pull Request Checklist

## Summary
- What does this PR change and why?
- Any context for reviewers (design decisions, tradeoffs)?

## Type of Change
- [ ] Bug fix
- [ ] Feature
- [ ] Refactor/cleanup
- [ ] Documentation
- [ ] Other

## Checklist
- [ ] Linked issue(s) or clear rationale provided
- [ ] Scope is focused; no unrelated changes
- [ ] Builds locally: `dotnet build -c Release`
- [ ] Manual test steps included and validated in-game
- [ ] Screenshots/GIFs added for UI changes (windows, buttons)
- [ ] Coding style followed (namespaces, fields, access modifiers)
- [ ] Harmony patches are minimal and targeted
- [ ] Feature is config-gated and disabled by default when appropriate
- [ ] No game assets or large binaries committed (only refs under `lib/`)
- [ ] Compatible with BepInEx 5.x and `netstandard2.1`
- [ ] Version updated in `SMTQualityOfLife.csproj` if user-visible changes

## Test Plan (steps)
- Build: `dotnet build -c Release`
- Install: copy `bin/Release/netstandard2.1/SMTQualityOfLife.dll` to `<Game>/BepInEx/plugins/SMTQualityOfLife/`
- Launch game and open main window (`Ctrl+H`)
- Validate feature(s):
  - NPCAdder: adjust max NPC and verify limits/behavior
  - LowCountProducts: set threshold; verify button and cart additions
- Review logs for errors/warnings

## Screenshots
_(Attach images/videos for UI-facing changes)_

## Breaking Changes
- [ ] None
- If any, describe migration/compatibility considerations:

